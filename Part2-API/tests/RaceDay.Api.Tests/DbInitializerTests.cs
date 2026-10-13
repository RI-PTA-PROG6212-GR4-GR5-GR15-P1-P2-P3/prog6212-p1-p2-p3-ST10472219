using Microsoft.EntityFrameworkCore;
using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>Start-up seeding: the Part 1 sample data plus demo accounts that can actually log in.</summary>
public class DbInitializerTests
{
    [Fact]
    public async Task Seed_EmptyDatabase_AddsThePart1SampleData()
    {
        using var db = NewDb();

        await DbInitializer.SeedAsync(db, Hasher);

        Assert.Equal(2, await db.Organisers.CountAsync());
        Assert.Equal(2, await db.Participants.CountAsync());
        Assert.Equal(4, await db.Enrolments.CountAsync());
        Assert.Equal(2, await db.Results.CountAsync());
        Assert.True(await db.Events.AnyAsync(e => e.EventName == "Pretoria Spring Fun Run"));
        Assert.True(await db.Categories.AnyAsync(c => c.CategoryName == "5km Heritage Walk"));
    }

    [Fact]
    public async Task Seed_PastEventsAreCompleted_AndUpcomingEventsAreOpen()
    {
        using var db = NewDb();

        await DbInitializer.SeedAsync(db, Hasher);

        var events = await db.Events.ToListAsync();
        Assert.All(events.Where(e => e.EventDate < DateTime.UtcNow), e => Assert.Equal(EventStatuses.Completed, e.Status));
        Assert.Contains(events, e => e.EventDate > DateTime.UtcNow && e.Status == EventStatuses.Scheduled);
    }

    [Fact]
    public async Task Seed_RunTwice_DoesNotDuplicateData()
    {
        using var db = NewDb();

        await DbInitializer.SeedAsync(db, Hasher);
        await DbInitializer.SeedAsync(db, Hasher);

        Assert.Equal(2, await db.Organisers.CountAsync());
        Assert.Equal(4, await db.Enrolments.CountAsync());
    }

    [Fact]
    public async Task Seed_DemoAccountsCanLogIn()
    {
        using var db = NewDb();
        await DbInitializer.SeedAsync(db, Hasher);
        var controller = new AuthController(db, Hasher).WithSession();

        var result = await controller.Login(new LoginRequest { Email = "sipho.dlamini@gmail.com", Password = DbInitializer.DemoPassword });

        Assert.Equal(200, Status(result));
        Assert.Equal(Roles.Participant, Body<AuthResponse>(result).Role);
    }

    [Fact]
    public async Task Seed_DatabaseFromPart1Script_ReplacesPlaceholderHashesSoDemoAccountsCanLogIn()
    {
        using var db = NewDb();
        db.Organisers.Add(new Organiser { FullName = "Thabo Mokoena", Email = "thabo.mokoena@raceday.co.za", PasswordHash = "HASHED_PASSWORD_1" });
        db.Participants.Add(new Participant { FullName = "Sipho Dlamini", Email = "sipho.dlamini@gmail.com", PasswordHash = "HASHED_PASSWORD_3" });
        await db.SaveChangesAsync();

        await DbInitializer.SeedAsync(db, Hasher);

        Assert.Equal(1, await db.Organisers.CountAsync()); // existing data is kept, not re-seeded
        var controller = new AuthController(db, Hasher).WithSession();
        var result = await controller.Login(new LoginRequest { Email = "thabo.mokoena@raceday.co.za", Password = DbInitializer.DemoPassword });
        Assert.Equal(200, Status(result));
    }

    [Fact]
    public async Task Seed_RealPasswordHashes_AreLeftAlone()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var original = organiser.PasswordHash;

        await DbInitializer.SeedAsync(db, Hasher);

        Assert.Equal(original, (await db.Organisers.SingleAsync()).PasswordHash);
    }
}
