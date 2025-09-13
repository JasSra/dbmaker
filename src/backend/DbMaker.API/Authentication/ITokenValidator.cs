using System.Security.Claims;

namespace DbMaker.API.Authentication;

public interface ITokenValidator
{
    Task<string> ValidateIdp(IdpConfiguration configuration);
    Dictionary<string, string> ExtractClaims(string token);
    ClaimsPrincipal? ValidateToken(IdpConfiguration configuration, string token, List<string>? requiredScopes = null);
}