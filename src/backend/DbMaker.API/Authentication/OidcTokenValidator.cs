using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace DbMaker.API.Authentication;

public class OidcTokenValidator : ITokenValidator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OidcTokenValidator> _logger;

    public OidcTokenValidator(ILogger<OidcTokenValidator> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient;
    }

    /// <summary>
    /// Validates an IDP configuration (checks if essential fields are provided)
    /// </summary>
    public async Task<string> ValidateIdp(IdpConfiguration configuration)
    {
        if (configuration == null)
        {
            _logger.LogError("IDP configuration is null");
            return "Invalid IDP configuration.";
        }

        if (string.IsNullOrWhiteSpace(configuration.Issuer) ||
            string.IsNullOrWhiteSpace(configuration.Audience) ||
            string.IsNullOrWhiteSpace(configuration.SigningKey))
        {
            _logger.LogError("Missing required fields in IDP configuration: {Name}", configuration.Name);
            return "Issuer, Audience, and SigningKey are required.";
        }

        try
        {
            if (configuration.Issuer.Contains("b2clogin.com"))
            {
                return await ValidateAzureAdB2CCredentials(configuration);
            }
            else
            {
                return await ValidateOidcCredentials(configuration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating IDP {Name}", configuration.Name);
            return $"IDP validation failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Extracts claims from a JWT token
    /// </summary>
    public Dictionary<string, string> ExtractClaims(string token)
    {
        var claims = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Attempted claim extraction from an empty token");
            return claims;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
            _logger.LogInformation("Extracted {ClaimCount} claims from token", claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting claims from token");
        }

        return claims;
    }

    /// <summary>
    /// Validates a JWT token using MSAL (Azure AD B2C) or OIDC.
    /// Supports optional scope validation.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(IdpConfiguration configuration, string token,
        List<string>? requiredScopes = null)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            using var activity = new Activity("ValidateToken").Start();
            activity?.AddTag("Idp.Name", configuration.Name);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var issuer = jwt.Issuer;

                ClaimsPrincipal? claimsPrincipal = configuration.IsAzureAdB2C
                    ? ValidateWithAzureAdB2C(configuration, token)
                    : ValidateWithOidc(configuration, token).Result;

                if (claimsPrincipal != null)
                {
                    if (requiredScopes is { Count: > 0 } && !HasRequiredScopes(claimsPrincipal, requiredScopes, configuration))
                    {
                        _logger.LogWarning("Token missing required scopes: {Scopes}",
                            string.Join(", ", requiredScopes));
                        return null;
                    }

                    _logger.LogInformation("Token validated successfully.");

                    // Extract claims from the token
                    var claims = claimsPrincipal.Claims
                        .Select(c => new Claim(c.Type, c.Value))
                        .ToList();

                    claimsPrincipal =
                        new ClaimsPrincipal(new ClaimsIdentity(claims, claimsPrincipal.Identity?.AuthenticationType));

                    return claimsPrincipal;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate token.");
            }
        }
        else
        {
            _logger.LogWarning("Attempted validation of an empty token.");
        }

        _logger.LogWarning("Token validation failed.");
        return null;
    }

    private async Task<string> ValidateOidcCredentials(IdpConfiguration config)
    {
        _logger.LogInformation("Validating OIDC credentials for {Name}", config.Name);

        var tokenRequest = new Dictionary<string, string>
        {
            { "client_id", config.Audience },
            { "client_secret", config.SigningKey },
            { "grant_type", "client_credentials" },
            { "scope", string.Join(" ", config.Scopes ?? Array.Empty<string>()) }
        };

        var response = await _httpClient.PostAsync($"{config.Issuer}/token", new FormUrlEncodedContent(tokenRequest));
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OIDC authentication failed for {Name}: {Response}", config.Name, responseContent);
            return $"OIDC authentication failed: {responseContent}";
        }

        _logger.LogInformation("OIDC authentication successful for {Name}", config.Name);
        return string.Empty; // Success
    }

    private async Task<string> ValidateAzureAdB2CCredentials(IdpConfiguration config)
    {
        _logger.LogInformation("Validating Azure AD B2C credentials for {Name}", config.Name);

        try
        {
            // For B2C, we'll validate by checking the metadata endpoint
            var metadataEndpoint = config.MetaDataEndpoint ?? 
                $"{config.Issuer}/.well-known/openid_configuration";
            
            var response = await _httpClient.GetAsync(metadataEndpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch B2C metadata for {Name}", config.Name);
                return $"Failed to validate B2C configuration: {response.StatusCode}";
            }

            _logger.LogInformation("Azure AD B2C configuration validated successfully for {Name}", config.Name);
            return string.Empty; // Success
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AD B2C configuration validation failed for {Name}", config.Name);
            return $"Azure AD B2C configuration validation failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Checks if the token contains all required scopes or if role mappings satisfy the required scopes.
    /// </summary>
    private static bool HasRequiredScopes(ClaimsPrincipal? claimsPrincipal, List<string> requiredScopes,
        IdpConfiguration configuration)
    {
        if (claimsPrincipal == null || requiredScopes == null || requiredScopes.Count == 0)
        {
            return requiredScopes == null || requiredScopes.Count == 0;
        }

        var tokenScopes = claimsPrincipal.Claims
            .Where(c => c.Type is "scp" or "scope" or "http://schemas.microsoft.com/identity/claims/scope")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // First, check if token directly contains all required scopes
        if (requiredScopes.All(scope => tokenScopes.Contains(scope, StringComparer.OrdinalIgnoreCase)))
        {
            return true;
        }

        // If direct scope validation fails, check role mappings
        if (configuration?.RoleMappings != null)
        {
            var mappings = configuration.RoleMappings;
            var satisfiedScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tokenScope in tokenScopes)
            {
                foreach (var mapping in mappings)
                {
                    // Check if the mapping key (required scope) matches any of our required scopes
                    // and if the mapping value (role) contains the current token scope
                    if (requiredScopes.Contains(mapping.Key, StringComparer.OrdinalIgnoreCase) &&
                        mapping.Value?.Contains(tokenScope, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        satisfiedScopes.Add(mapping.Key);
                    }
                }
            }

            // Check if all required scopes are satisfied through role mappings
            return requiredScopes.All(scope => satisfiedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase));
        }

        return false;
    }

    private ClaimsPrincipal? ValidateWithAzureAdB2C(IdpConfiguration configuration, string token)
    {
        using var activity = new Activity("ValidateAzureAdB2CToken").Start();

        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token is null or empty.");
                return null;
            }

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                _logger.LogWarning("Token is not in a valid JWT format.");
                return null;
            }

            var jwt = handler.ReadJwtToken(token);

            if (!TryExtractTenantAndPolicy(jwt.Issuer, out var tenant, out var policy))
            {
                _logger.LogWarning("Failed to extract Tenant and Policy from Issuer: {Issuer}", jwt.Issuer);
                return null;
            }

            var metaEndpoint = configuration.MetaDataEndpoint;

            if (string.IsNullOrEmpty(configuration?.MetaDataEndpoint))
            {
                throw new ArgumentException("MetaDataEndpoint is required for Azure AD B2C validation.");
            }

            _logger.LogInformation("Extracted Tenant: {Tenant}, Policy: {Policy}, Authority: {MetaEndpoint}", tenant,
                policy, metaEndpoint);

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metaEndpoint,
                new OpenIdConnectConfigurationRetriever(),
                _httpClient);

            var openIdConfig = configManager.GetConfigurationAsync().Result;

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = configuration.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                RequireAudience = true,
                ClockSkew = TimeSpan.FromMinutes(1) // Allow minor time differences
            };

            var claimsPrincipal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken validatedJwt)
            {
                if (validatedJwt.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token has expired.");
                    return null;
                }

                if (validatedJwt.ValidFrom > DateTime.UtcNow)
                {
                    _logger.LogWarning("Token is not yet valid.");
                    return null;
                }
            }

            _logger.LogInformation("Azure AD B2C token validated successfully.");
            return claimsPrincipal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired.");
            return null;
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Token issuer is invalid.");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token signature is invalid.");
            return null;
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "General security token validation error.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Azure AD B2C token validation.");
            return null;
        }
    }

    /// <summary>
    /// Extracts the Azure AD B2C tenant and policy from the issuer URL.
    /// </summary>
    private static bool TryExtractTenantAndPolicy(string issuer, out string tenant, out string policy)
    {
        tenant = string.Empty;
        policy = string.Empty;

        try
        {
            var uri = new Uri(issuer);
            var segments = uri.AbsolutePath.Trim('/').Split('/');

            if (segments.Length < 2)
            {
                return false;
            }

            tenant = segments[0];
            policy = segments[1];

            return !string.IsNullOrWhiteSpace(tenant) && !string.IsNullOrWhiteSpace(policy);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<ClaimsPrincipal?> ValidateWithOidc(IdpConfiguration configuration, string token)
    {
        using var activity = new Activity("ValidateOidcToken").Start();

        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token is null or empty.");
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                _logger.LogWarning("Token is not in a valid JWT format.");
                return null;
            }

            var jwt = handler.ReadJwtToken(token);
            if (string.IsNullOrWhiteSpace(jwt.Issuer) || !Uri.IsWellFormedUriString(jwt.Issuer, UriKind.Absolute))
            {
                _logger.LogWarning("Invalid or missing issuer: {Issuer}", jwt.Issuer);
                return null;
            }

            IdentityModelEventSource.ShowPII = true;

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = configuration.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                RequireAudience = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    var keys = GetSigningKeysFromJwks(jwt.Issuer).Result;
                    var k = keys.FirstOrDefault(k => k.KeyId == kid);

                    return new List<SecurityKey> { k };
                }
            };

            var claimsPrincipal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken validatedJwt)
            {
                if (validatedJwt.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token has expired.");
                    return null;
                }

                if (validatedJwt.ValidFrom > DateTime.UtcNow)
                {
                    _logger.LogWarning("Token is not yet valid.");
                    return null;
                }
            }

            _logger.LogInformation("OIDC token validated successfully.");
            return claimsPrincipal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired.");
            return null;
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Token issuer is invalid.");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token signature is invalid.");
            return null;
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "General security token validation error.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during OIDC token validation.");
            return null;
        }
    }

    private async Task<List<SecurityKey>> GetSigningKeysFromJwks(string issuer)
    {
        var jwksUri = $"{issuer.TrimEnd('/')}/.well-known/jwks.json";

        var response = await _httpClient.GetStringAsync(jwksUri);
        var jwks = JsonConvert.DeserializeObject<JwksResponse>(response);

        return jwks?.Keys?.Select(k => k.ToSecurityKey()).ToList() ?? new List<SecurityKey>();
    }
}

public sealed record TokenCheck(
    bool IsValid,
    string? Error,
    string? Issuer,
    string? Audience,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ExpiresAt,
    string[]? Scopes);