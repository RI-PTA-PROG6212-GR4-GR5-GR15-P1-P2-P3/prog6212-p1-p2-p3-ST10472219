using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Tests;

public class RaceDayApiClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<HttpRequestMessage> Requests { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(_respond(request));
        }
    }

    private static (RaceDayApiClient Client, DefaultHttpContext Http, StubHandler Handler) Create(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var http = new DefaultHttpContext { Session = new TestSession() };
        var client = new RaceDayApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api/") },
                                          new HttpContextAccessor { HttpContext = http });
        return (client, http, handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Get_ParsesJson()
    {
        var (client, _, _) = Create(_ => Json(HttpStatusCode.OK, "[1,2,3]"));

        var result = await client.Get<List<int>>("api/numbers");

        Assert.True(result.Success);
        Assert.Equal(new[] { 1, 2, 3 }, result.Data);
    }

    [Fact]
    public async Task ApiSessionCookie_IsCaptured_ThenForwardedOnNextRequest()
    {
        var first = true;
        var (client, http, handler) = Create(_ =>
        {
            var response = Json(HttpStatusCode.OK, "{}");
            if (first) response.Headers.Add("Set-Cookie", ".RaceDay.Session=abc123; path=/; httponly");
            first = false;
            return response;
        });

        await client.Get<object>("api/one");
        await client.Get<object>("api/two");

        Assert.Equal(".RaceDay.Session=abc123", http.Session.GetString("RaceDayApiCookie"));
        Assert.Equal(".RaceDay.Session=abc123", handler.Requests[1].Headers.GetValues("Cookie").Single());
    }

    [Fact]
    public async Task ErrorBody_MessageIsSurfaced()
    {
        var (client, _, _) = Create(_ => Json(HttpStatusCode.Conflict, "{\"message\":\"This event is full.\"}"));

        var result = await client.Post<object, object>("api/categories/1/enrol", new { });

        Assert.False(result.Success);
        Assert.Equal("This event is full.", result.Error);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_ClearsTheMvcSession()
    {
        var (client, http, _) = Create(_ => Json(HttpStatusCode.Unauthorized, "{\"message\":\"Please log in.\"}"));
        http.Session.SetInt32("UserId", 7);
        http.Session.SetString("Role", "Organiser");

        await client.Get<object>("api/dashboard/organiser");

        Assert.Null(http.Session.GetInt32("UserId"));
        Assert.Null(http.Session.GetString("Role"));
    }

    [Fact]
    public async Task UnreachableApi_ReturnsFriendly503_InsteadOfThrowing()
    {
        var (client, _, _) = Create(_ => throw new HttpRequestException("connection refused"));

        var result = await client.Get<object>("api/events");

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
    }
}
