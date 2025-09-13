using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DbMaker.API.Authentication;

public class CustomJwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITokenValidator _tokenValidator;
    private readonly IdpConfiguration _idpConfiguration;
    private readonly ILogger<CustomJwtAuthenticationHandler> _logger;

    public CustomJwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        ITokenValidator tokenValidator,
        IdpConfiguration idpConfiguration)
        : base(options, loggerFactory, encoder)
    {
        _tokenValidator = tokenValidator;
        _idpConfiguration = idpConfiguration;
        _logger = loggerFactory.CreateLogger<CustomJwtAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Get the Authorization header
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogDebug("?? No Authorization header found");
                return AuthenticateResult.NoResult();
            }

            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("?? Invalid Authorization header format");
                return AuthenticateResult.Fail("Invalid Authorization header format");
            }

            var token = authHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("?? Empty token in Authorization header");
                return AuthenticateResult.Fail("Empty token");
            }

            _logger.LogInformation("?? Validating JWT token with custom validator");

            // Use your custom token validator
            var claimsPrincipal = _tokenValidator.ValidateToken(_idpConfiguration, token);

            if (claimsPrincipal == null)
            {
                _logger.LogWarning("? Token validation failed");
                return AuthenticateResult.Fail("Token validation failed");
            }

            _logger.LogInformation("? Token validated successfully with custom validator");

            // Create authentication ticket
            var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Exception during authentication");
            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        _logger.LogWarning("?? Authentication challenge triggered");
        
        Response.StatusCode = 401;
        Response.Headers.Add("WWW-Authenticate", "Bearer");
        
        await Response.WriteAsync("Unauthorized");
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        _logger.LogWarning("?? Access forbidden");
        
        Response.StatusCode = 403;
        await Response.WriteAsync("Forbidden");
    }
}