using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data;

/// <summary>
/// Applies the EF Core migrations (creating RaceDayDB if needed) and seeds the same sample data as the Part 1 SQL script,
/// plus two upcoming events so there is always something open for enrolment.
/// </summary>
public static class DbInitializer
{
    /// <summary>Password for every seeded demo account.</summary>
    public const string DemoPassword = "Password123!";

    /// <summary>The Part 1 script stores these placeholders instead of real hashes.</summary>
    private const string PlaceholderHashPrefix = "HASHED_PASSWORD";

    public static async Task InitialiseAsync(RaceDayDbContext db, IPasswordHasher hasher)
    {
        if (db.Database.IsRelational())
        {
            await BaselineScriptCreatedDatabaseAsync(db);
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        await SeedAsync(db, hasher);
    }

    public static async Task SeedAsync(RaceDayDbContext db, IPasswordHasher hasher)
    {
        if (await db.Organisers.AnyAsync())
        {
            await ReplacePlaceholderPasswordsAsync(db, hasher);
            return;
        }

        var hash = hasher.Hash(DemoPassword);
        var now = DateTime.UtcNow;

        var thabo = new Organiser { FullName = "Thabo Mokoena", Email = "thabo.mokoena@raceday.co.za", PasswordHash = hash, PhoneNumber = "0821234567" };
        var lindiwe = new Organiser { FullName = "Lindiwe Nkosi", Email = "lindiwe.nkosi@raceday.co.za", PasswordHash = hash, PhoneNumber = "0839876543" };

        var sipho = new Participant { FullName = "Sipho Dlamini", Email = "sipho.dlamini@gmail.com", PasswordHash = hash, PhoneNumber = "0715551234", DateOfBirth = new DateOnly(1995, 3, 14) };
        var anelisa = new Participant { FullName = "Anelisa Botha", Email = "anelisa.botha@gmail.com", PasswordHash = hash, PhoneNumber = "0725559876", DateOfBirth = new DateOnly(1998, 11, 2) };

        // Events from the Part 1 script. Their dates are fixed, so once they have passed they are seeded as Completed.
        var springRun = new Event { Organiser = thabo, EventName = "Pretoria Spring Fun Run", EventDate = new DateTime(2026, 9, 20, 7, 0, 0), Location = "Union Buildings, Pretoria", Description = "A community fun run through the Pretoria CBD.", EventType = EventTypes.Running };
        var cycle = new Event { Organiser = thabo, EventName = "Tshwane Cycle Challenge", EventDate = new DateTime(2026, 10, 11, 6, 30, 0), Location = "Loftus Versfeld, Pretoria", Description = "Road cycling event for all skill levels.", EventType = EventTypes.Cycling };
        var walk = new Event { Organiser = lindiwe, EventName = "Joburg Heritage Walk", EventDate = new DateTime(2026, 9, 24, 8, 0, 0), Location = "Constitution Hill, Johannesburg", Description = "A heritage-themed community walk.", EventType = EventTypes.Walking };
        foreach (var ev in new[] { springRun, cycle, walk })
            ev.Status = ev.EventDate < now ? EventStatuses.Completed : EventStatuses.Scheduled;

        // Upcoming events (relative to the day the database is created) so participants can enrol in a demo.
        var nightRun = new Event { Organiser = thabo, EventName = "Centurion Night Run", EventDate = now.Date.AddDays(30).AddHours(18), Location = "Centurion Lake, Centurion", Description = "An evening road race around Centurion Lake.", EventType = EventTypes.Running };
        var cradleRide = new Event { Organiser = lindiwe, EventName = "Cradle of Humankind Cycle Tour", EventDate = now.Date.AddDays(45).AddHours(6), Location = "Maropeng, Cradle of Humankind", Description = "A scenic road ride through the Cradle of Humankind.", EventType = EventTypes.Cycling };

        var fun5 = new Category { Event = springRun, CategoryName = "5km Fun Run", DistanceKm = 5m, EntryFee = 100m, MaxParticipants = 500 };
        var race10 = new Category { Event = springRun, CategoryName = "10km Race", DistanceKm = 10m, EntryFee = 150m, MaxParticipants = 300 };
        var ride40 = new Category { Event = cycle, CategoryName = "40km Road Ride", DistanceKm = 40m, EntryFee = 250m, MaxParticipants = 200 };
        var ride80 = new Category { Event = cycle, CategoryName = "80km Road Ride", DistanceKm = 80m, EntryFee = 350m, MaxParticipants = 150 };
        var walk5 = new Category { Event = walk, CategoryName = "5km Heritage Walk", DistanceKm = 5m, EntryFee = 50m, MaxParticipants = 400 };
        var night5 = new Category { Event = nightRun, CategoryName = "5km Night Run", DistanceKm = 5m, EntryFee = 120m, MaxParticipants = 400 };
        var night10 = new Category { Event = nightRun, CategoryName = "10km Night Race", DistanceKm = 10m, EntryFee = 180m, MaxParticipants = 250 };
        var cradle60 = new Category { Event = cradleRide, CategoryName = "60km Tour", DistanceKm = 60m, EntryFee = 300m, MaxParticipants = 200 };

        var e1 = new Enrolment { Participant = sipho, Category = fun5, BibNumber = "BIB0001", Status = EnrolmentStatuses.Confirmed };
        var e2 = new Enrolment { Participant = sipho, Category = ride40, BibNumber = "BIB0002", Status = EnrolmentStatuses.Confirmed };
        var e3 = new Enrolment { Participant = anelisa, Category = race10, BibNumber = "BIB0003", Status = EnrolmentStatuses.Confirmed };
        var e4 = new Enrolment { Participant = anelisa, Category = walk5, BibNumber = "BIB0004", Status = EnrolmentStatuses.Confirmed };

        var r1 = new Result { Enrolment = e1, FinishTime = new TimeSpan(0, 28, 45), Position = 12, Status = ResultStatuses.Finished };
        var r3 = new Result { Enrolment = e3, FinishTime = new TimeSpan(0, 52, 10), Position = 5, Status = ResultStatuses.Finished };

        db.AddRange(thabo, lindiwe, sipho, anelisa, springRun, cycle, walk, nightRun, cradleRide,
                    fun5, race10, ride40, ride80, walk5, night5, night10, cradle60, e1, e2, e3, e4, r1, r3);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// If RaceDayDB was already built by running docs/RaceDay_Database.sql, the tables exist but EF has no migration
    /// history, so MigrateAsync would fail trying to create them again. The script builds the same schema as the
    /// InitialCreate migration, so it is recorded as applied instead.
    /// </summary>
    private static async Task BaselineScriptCreatedDatabaseAsync(RaceDayDbContext db)
    {
        if (!await db.Database.CanConnectAsync()) return; // database does not exist yet
        if ((await db.Database.GetAppliedMigrationsAsync()).Any()) return;

        var tables = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Organiser'")
            .SingleAsync();
        if (tables == 0) return;

        var initialMigration = db.Database.GetMigrations().First();
        var efVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

        var history = db.GetService<IHistoryRepository>();
        await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(initialMigration, efVersion)));
    }

    /// <summary>The Part 1 script's accounts can never log in with a placeholder hash, so they get the demo password.</summary>
    private static async Task ReplacePlaceholderPasswordsAsync(RaceDayDbContext db, IPasswordHasher hasher)
    {
        var organisers = await db.Organisers.Where(o => o.PasswordHash.StartsWith(PlaceholderHashPrefix)).ToListAsync();
        var participants = await db.Participants.Where(p => p.PasswordHash.StartsWith(PlaceholderHashPrefix)).ToListAsync();
        if (organisers.Count + participants.Count == 0) return;

        var hash = hasher.Hash(DemoPassword);
        organisers.ForEach(o => o.PasswordHash = hash);
        participants.ForEach(p => p.PasswordHash = hash);
        await db.SaveChangesAsync();
    }
}
