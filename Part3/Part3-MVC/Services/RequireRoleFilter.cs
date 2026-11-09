using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Session-based role check for MVC pages. Anonymous users go to the login page (and come back afterwards);
/// users with the wrong role get a 403 page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RoleAuthorizeAttribute : TypeFilterAttribute
{
    public RoleAuthorizeAttribute(string role) : base(typeof(RoleAuthorizeFilter))
    {
        Arguments = new object[] { role };
    }
}

public sealed class RoleAuthorizeFilter : IAuthorizationFilter
{
    private readonly string _role;

    public RoleAuthorizeFilter(string role)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var session = http.Session;

        if (session.GetInt32("UserId") is null)
        {
            var returnUrl = HttpMethods.IsGet(http.Request.Method)
                ? http.Request.Path + http.Request.QueryString
                : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (!string.Equals(session.GetString("Role"), _role, StringComparison.OrdinalIgnoreCase))
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
