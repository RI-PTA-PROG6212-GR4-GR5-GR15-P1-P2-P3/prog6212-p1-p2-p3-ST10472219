using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Filters;

/// <summary>
/// Session-based, role-aware authorisation.
/// <list type="bullet">
/// <item>No logged-in session  -> 401 Unauthorized.</item>
/// <item>Logged in with the wrong role -> 403 Forbidden.</item>
/// </list>
/// Use <c>[SessionAuthorize]</c> for "any logged-in user" or
/// <c>[SessionAuthorize(Roles.Organiser)]</c> / <c>[SessionAuthorize(Roles.Participant)]</c> for role-only endpoints.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; }

    public SessionAuthorizeAttribute() { }

    public SessionAuthorizeAttribute(string role) => Role = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId is null || string.IsNullOrEmpty(role))
        {
            context.Result = new ObjectResult(new ErrorResponse("Authentication required. Please log in."))
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        if (Role is not null && !string.Equals(role, Role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new ErrorResponse($"This action is restricted to the {Role} role."))
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
