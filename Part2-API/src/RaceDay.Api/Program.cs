using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (EF Core, SQL Server, code-first) ----
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// ---- Session-based authentication (user id + role are kept in the server-side session) ----
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});

builder.Services.AddControllers();

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "RESTful API for the RaceDay event management system (South African road running, cycling and walking events).\n\n" +
            "**Roles:** Organiser (manage events, categories, enrolments and results) and Participant (browse, enrol, view own results).\n\n" +
            "**Authentication:** session based. Call `POST /api/auth/login` (or a register endpoint) first - the session cookie is stored " +
            "by the browser and sent automatically when you use *Try it out* below. Role-restricted endpoints return 401 when not logged in " +
            "and 403 for the wrong role."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Create the database (or apply migrations) and seed the sample data on start-up.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbInitializer.InitialiseAsync(db, hasher);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Could not initialise the database. Check the 'RaceDayDb' connection string in appsettings.json.");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "RaceDay API";
});

app.UseHttpsRedirection();
app.UseSession();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Makes the entry point visible to test projects.
public partial class Program { }
