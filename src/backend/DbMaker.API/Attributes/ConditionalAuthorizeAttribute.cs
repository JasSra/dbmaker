using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DbMaker.API.Attributes;

public class ConditionalAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Always require authentication - no bypass mechanisms
        if (!context.HttpContext.User?.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
