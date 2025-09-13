using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DbMaker.API.Authentication;
using System.Security.Claims;

namespace DbMaker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthDebugController : ControllerBase
{
    private readonly ILogger<AuthDebugController> _logger;
    private readonly ITokenValidator _tokenValidator;
    private readonly IdpConfiguration _idpConfiguration;

    public AuthDebugController(ILogger<AuthDebugController> logger, ITokenValidator tokenValidator, IdpConfiguration idpConfiguration)
    {
        _logger = logger;
        _tokenValidator = tokenValidator;
        _idpConfiguration = idpConfiguration;
    }

    /// <summary>
    /// Test endpoint that doesn't require authentication
    /// </summary>
    [HttpGet("anonymous")]
    public ActionResult<object> GetAnonymous()
    {
        return Ok(new 
        { 
            message = "This endpoint works without authentication",
            timestamp = DateTime.UtcNow,
            isAuthenticated = User?.Identity?.IsAuthenticated ?? false
        });
    }

    /// <summary>
    /// Test endpoint that requires authentication
    /// </summary>
    [HttpGet("authenticated")]
    [Authorize]
    public ActionResult<object> GetAuthenticated()
    {
        var claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToArray() ?? Array.Empty<object>();
        
        return Ok(new
        {
            message = "Authentication successful!",
            timestamp = DateTime.UtcNow,
            isAuthenticated = User?.Identity?.IsAuthenticated ?? false,
            userId = User?.FindFirst("oid")?.Value 
                ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value,
            userName = User?.FindFirst("name")?.Value 
                ?? User?.FindFirst(ClaimTypes.Name)?.Value,
            email = User?.FindFirst("email")?.Value 
                ?? User?.FindFirst(ClaimTypes.Email)?.Value,
            allClaims = claims
        });
    }

    /// <summary>
    /// Test custom token validation directly
    /// </summary>
    [HttpPost("validate-token")]
    public ActionResult<object> ValidateTokenDirect([FromBody] TokenValidationRequest request)
    {
        try
        {
            var claimsPrincipal = _tokenValidator.ValidateToken(_idpConfiguration, request.Token);
            
            if (claimsPrincipal == null)
            {
                return BadRequest(new { error = "Token validation failed" });
            }

            var claims = claimsPrincipal.Claims.Select(c => new { c.Type, c.Value }).ToArray();
            
            return Ok(new
            {
                message = "Token validated successfully with custom validator",
                isValid = true,
                claims = claims,
                userId = claimsPrincipal.FindFirst("oid")?.Value,
                userName = claimsPrincipal.FindFirst("name")?.Value,
                email = claimsPrincipal.FindFirst("email")?.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token directly");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get current authentication status and token information
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetAuthStatus()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var hasAuthHeader = !string.IsNullOrEmpty(authHeader);
        var hasBearerToken = hasAuthHeader && authHeader.StartsWith("Bearer ");
        
        _logger.LogInformation("Auth status check - Has header: {HasHeader}, Has bearer: {HasBearer}, Is authenticated: {IsAuth}", 
            hasAuthHeader, hasBearerToken, User?.Identity?.IsAuthenticated ?? false);

        return Ok(new
        {
            hasAuthorizationHeader = hasAuthHeader,
            hasBearerToken = hasBearerToken,
            isAuthenticated = User?.Identity?.IsAuthenticated ?? false,
            authenticationScheme = User?.Identity?.AuthenticationType,
            tokenPreview = hasBearerToken ? authHeader[..Math.Min(50, authHeader.Length)] + "..." : null,
            claimsCount = User?.Claims?.Count() ?? 0,
            timestamp = DateTime.UtcNow,
            idpConfig = new
            {
                _idpConfiguration.Name,
                _idpConfiguration.Issuer,
                _idpConfiguration.Audience,
                _idpConfiguration.IsAzureAdB2C,
                _idpConfiguration.MetaDataEndpoint
            }
        });
    }

    /// <summary>
    /// Get detailed information about the current JWT token
    /// </summary>
    [HttpGet("token-info")]
    public ActionResult<object> GetTokenInfo()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return BadRequest(new { error = "No Bearer token found in Authorization header" });
        }

        var token = authHeader["Bearer ".Length..];
        
        try
        {
            // Use the token validator to extract claims
            var extractedClaims = _tokenValidator.ExtractClaims(token);

            return Ok(new
            {
                extractedClaims = extractedClaims,
                isAuthenticated = User?.Identity?.IsAuthenticated ?? false,
                claimsFromContext = User?.Claims?.Select(c => new { c.Type, c.Value }).ToArray() ?? Array.Empty<object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JWT token");
            return BadRequest(new { error = "Error parsing JWT token", details = ex.Message });
        }
    }
}

public class TokenValidationRequest
{
    public string Token { get; set; } = string.Empty;
}