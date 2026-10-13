using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

/// <summary>
/// Sends real HTTP requests through the whole API (routing, session middleware, filters, JSON) to check that the
/// session cookie keeps a user logged in between requests. Uses an in-memory database seeded at start-up.
/// </summary>
public class ApiPipelineTests : IDisposable
{
    private const string Participant = "sipho.dlamini@gmail.com";
    private const string Organiser = "thabo.mokoena@raceday.co.za";

    private readonly TestApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_SetsSessionCookie_ThatAuthenticatesLaterRequests()
    {
        var client = _factory.CreateClient();

        var login = await LoginAsync(client, Participant);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".RaceDay.Session="));

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal(Participant, profile.GetProperty("email").GetString());
        Assert.Equal("Participant", profile.GetProperty("role").GetString());
    }

    [Fact]
    public async Task NoSession_ProtectedEndpoint_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OnOrganiserEndpoint_Returns403()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, Participant);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Not Allowed Run",
            eventDate = DateTime.UtcNow.AddDays(10),
            location = "Pretoria",
            eventType = "Running"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, Organiser);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task InvalidRegistration_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register/participant", new
        {
            fullName = "Short Password",
            email = "short@test.com",
            password = "abc"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterEnrolAndCaptureResult_EndToEnd()
    {
        // A new participant registers (and is logged in straight away)...
        var runner = _factory.CreateClient();
        var register = await runner.PostAsJsonAsync("/api/auth/register/participant", new
        {
            fullName = "Kagiso Molefe",
            email = "kagiso@test.com",
            password = "Running2026!",
            phoneNumber = "0761234567",
            dateOfBirth = "2001-06-15"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        // ...browses the upcoming events and enrols in a category.
        var events = await runner.GetFromJsonAsync<JsonElement>("/api/events");
        var nightRun = events.EnumerateArray().First(e => e.GetProperty("eventName").GetString() == "Centurion Night Run");
        var eventId = nightRun.GetProperty("eventID").GetInt32();
        var detail = await runner.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}");
        var categoryId = detail.GetProperty("categories")[0].GetProperty("categoryID").GetInt32();

        var enrol = await runner.PostAsync($"/api/categories/{categoryId}/enrol", null);
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);
        var enrolment = await enrol.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("BIB", enrolment.GetProperty("bibNumber").GetString());

        // The organiser who owns the event sees the enrolment and captures a result.
        var organiser = _factory.CreateClient();
        await LoginAsync(organiser, Organiser);
        var enrolments = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Contains(enrolments.EnumerateArray(), e => e.GetProperty("participantName").GetString() == "Kagiso Molefe");

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.GetProperty("enrolmentID").GetInt32()}/result",
            new { finishTime = "00:41:30", position = 3, status = "Finished" });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        // The participant now sees the result in their own history.
        var results = await runner.GetFromJsonAsync<JsonElement>("/api/results/my");
        var result = Assert.Single(results.EnumerateArray());
        Assert.Equal("00:41:30", result.GetProperty("finishTime").GetString());
        Assert.Equal(3, result.GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Swagger_DocumentsEveryEndpoint()
    {
        var client = _factory.CreateClient();

        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");

        var operations = swagger.GetProperty("paths").EnumerateObject().Sum(path => path.Value.EnumerateObject().Count());
        Assert.Equal(24, operations);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password = DbInitializer.DemoPassword });

    /// <summary>Runs the real Program start-up, but swaps SQL Server for a private in-memory database.</summary>
    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"RaceDayPipeline-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
                services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
