using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Tests;

/// <summary>
/// Authenticated vs unauthenticated requests, and role-based access:
/// an Organiser cannot use Participant functionality and vice versa.
/// </summary>
public class RoleAccessTests
{
    private static AuthorizationFilterContext Context(int? userId, string? role)
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        if (userId.HasValue)
        {
            http.Session.SetInt32(SessionKeys.UserId, userId.Value);
            http.Session.SetString(SessionKeys.Role, role!);
        }
        return new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());
    }

    private static int? StatusOf(AuthorizationFilterContext ctx) => (ctx.Result as ObjectResult)?.StatusCode;

    [Theory]
    [InlineData(null)]
    [InlineData(Roles.Organiser)]
    [InlineData(Roles.Participant)]
    public void NotLoggedIn_IsRejectedWith401(string? requiredRole)
    {
        var filter = requiredRole is null ? new SessionAuthorizeAttribute() : new SessionAuthorizeAttribute(requiredRole);
        var ctx = Context(null, null);

        filter.OnAuthorization(ctx);

        Assert.Equal(401, StatusOf(ctx));
    }

    [Fact]
    public void Participant_OnOrganiserOnlyEndpoint_IsRejectedWith403()
    {
        var ctx = Context(1, Roles.Participant);

        new SessionAuthorizeAttribute(Roles.Organiser).OnAuthorization(ctx);

        Assert.Equal(403, StatusOf(ctx));
    }

    [Fact]
    public void Organiser_OnParticipantOnlyEndpoint_IsRejectedWith403()
    {
        var ctx = Context(1, Roles.Organiser);

        new SessionAuthorizeAttribute(Roles.Participant).OnAuthorization(ctx);

        Assert.Equal(403, StatusOf(ctx));
    }

    [Theory]
    [InlineData(Roles.Organiser, Roles.Organiser)]
    [InlineData(Roles.Participant, Roles.Participant)]
    public void CorrectRole_IsAllowed(string userRole, string requiredRole)
    {
        var ctx = Context(1, userRole);

        new SessionAuthorizeAttribute(requiredRole).OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }

    [Theory]
    [InlineData(Roles.Organiser)]
    [InlineData(Roles.Participant)]
    public void AnyRoleEndpoint_AllowsBothRoles(string userRole)
    {
        var ctx = Context(1, userRole);

        new SessionAuthorizeAttribute().OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }

    // ---- every endpoint carries the access rule from the Part 1 endpoint plan ----

    [Theory]
    [InlineData(typeof(EventsController), "Create", Roles.Organiser)]
    [InlineData(typeof(EventsController), "Update", Roles.Organiser)]
    [InlineData(typeof(EventsController), "Delete", Roles.Organiser)]
    [InlineData(typeof(CategoriesController), "Create", Roles.Organiser)]
    [InlineData(typeof(CategoriesController), "Update", Roles.Organiser)]
    [InlineData(typeof(CategoriesController), "Delete", Roles.Organiser)]
    [InlineData(typeof(EnrolmentsController), "GetForEvent", Roles.Organiser)]
    [InlineData(typeof(ResultsController), "Capture", Roles.Organiser)]
    [InlineData(typeof(ResultsController), "Update", Roles.Organiser)]
    [InlineData(typeof(EnrolmentsController), "Enrol", Roles.Participant)]
    [InlineData(typeof(EnrolmentsController), "GetMine", Roles.Participant)]
    [InlineData(typeof(EnrolmentsController), "Cancel", Roles.Participant)]
    [InlineData(typeof(ResultsController), "GetMine", Roles.Participant)]
    public void RoleRestrictedEndpoints_AreDecoratedWithTheCorrectRole(Type controller, string action, string expectedRole)
    {
        var method = controller.GetMethod(action)!;
        var attribute = method.GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRole, attribute!.Role);
    }

    [Theory]
    [InlineData(typeof(EventsController), "GetAll")]
    [InlineData(typeof(EventsController), "GetById")]
    [InlineData(typeof(CategoriesController), "GetForEvent")]
    [InlineData(typeof(CategoriesController), "GetById")]
    [InlineData(typeof(ResultsController), "GetForEvent")]
    [InlineData(typeof(AuthController), "Login")]
    [InlineData(typeof(AuthController), "RegisterOrganiser")]
    [InlineData(typeof(AuthController), "RegisterParticipant")]
    public void PublicEndpoints_AreNotRestricted(Type controller, string action)
    {
        var method = controller.GetMethod(action)!;

        Assert.Null(method.GetCustomAttribute<SessionAuthorizeAttribute>());
        Assert.Null(controller.GetCustomAttribute<SessionAuthorizeAttribute>());
    }

    [Fact]
    public void ProfileEndpoints_RequireAnyLoggedInUser()
    {
        var attribute = typeof(ProfileController).GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Null(attribute!.Role);
    }
}
