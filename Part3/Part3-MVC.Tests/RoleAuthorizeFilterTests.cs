using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Tests;

public class RoleAuthorizeFilterTests
{
    private static AuthorizationFilterContext Context(string method = "GET", int? userId = null, string? role = null)
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        http.Request.Method = method;
        http.Request.Path = "/Organiser/Events";
        if (userId.HasValue) http.Session.SetInt32("UserId", userId.Value);
        if (role != null) http.Session.SetString("Role", role);
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void Anonymous_IsRedirectedToLogin_WithReturnUrl()
    {
        var context = Context();

        new RoleAuthorizeFilter("Organiser").OnAuthorization(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("/Organiser/Events", redirect.RouteValues!["returnUrl"]!.ToString());
    }

    [Fact]
    public void Anonymous_PostRequest_HasNoReturnUrl()
    {
        var context = Context("POST");

        new RoleAuthorizeFilter("Organiser").OnAuthorization(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Null(redirect.RouteValues!["returnUrl"]);
    }

    [Fact]
    public void WrongRole_Gets403()
    {
        var context = Context(userId: 5, role: "Participant");

        new RoleAuthorizeFilter("Organiser").OnAuthorization(context);

        Assert.Equal(403, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    [Fact]
    public void CorrectRole_IsAllowed()
    {
        var context = Context(userId: 5, role: "Organiser");

        new RoleAuthorizeFilter("Organiser").OnAuthorization(context);

        Assert.Null(context.Result);
    }
}

/// <summary>Minimal in-memory session for unit tests.</summary>
public sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;
    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
}
