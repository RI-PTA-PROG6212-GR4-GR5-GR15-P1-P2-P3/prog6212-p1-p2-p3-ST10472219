using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data;

/// <summary>
/// Creates the database from the code-first model (or applies EF migrations when migration files exist)
/// and seeds the same sample data as the Part 1 SQL script.
/// </summary>
public static class DbInitializer
{
    /// <summary>Password for every seeded demo account.</summary>
    public const string DemoPassword = "Password123!";

    public static async Task InitialiseAsync(RaceDayDbContext db, IPasswordHasher hasher)
    {
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await SeedAsync(db, hasher);
    }

    public static async Task SeedAsync(RaceDayDbContext db, IPasswordHasher hasher)
    {
        if (await db.Organisers.AnyAsync()) return; // already seeded

        var hash = hasher.Hash(DemoPassword);

        var thabo = new Organiser { FullName = "Thabo Mokoena", Email = "thabo.mokoena@raceday.co.za", PasswordHash = hash, PhoneNumber = "0821234567" };
        var lindiwe = new Organiser { FullName = "Lindiwe Nkosi", Email = "lindiwe.nkosi@raceday.co.za", PasswordHash = hash, PhoneNumber = "0839876543" };

        var sipho = new Participant { FullName = "Sipho Dlamini", Email = "sipho.dlamini@gmail.com", PasswordHash = hash, PhoneNumber = "0715551234", DateOfBirth = new DateOnly(1995, 3, 14) };
        var anelisa = new Participant { FullName = "Anelisa Botha", Email = "anelisa.botha@gmail.com", PasswordHash = hash, PhoneNumber = "0725559876", DateOfBirth = new DateOnly(1998, 11, 2) };

        var springRun = new Event { Organiser = thabo, EventName = "Pretoria Spring Fun Run", EventDate = new DateTime(2026, 9, 20, 7, 0, 0), Location = "Union Buildings, Pretoria", Description = "A community fun run through the Pretoria CBD.", EventType = EventTypes.Running };
        var cycle = new Event { Organiser = thabo, EventName = "Tshwane Cycle Challenge", EventDate = new DateTime(2026, 10, 11, 6, 30, 0), Location = "Loftus Versfeld, Pretoria", Description = "Road cycling event for all skill levels.", EventType = EventTypes.Cycling };
        var walk = new Event { Organiser = lindiwe, EventName = "Joburg Heritage Walk", EventDate = new DateTime(2026, 9, 24, 8, 0, 0), Location = "Constitution Hill, Johannesburg", Description = "A heritage-themed community walk.", EventType = EventTypes.Walking };

        var fun5 = new Category { Event = springRun, CategoryName = "5km Fun Run", DistanceKm = 5m, EntryFee = 100m, MaxParticipants = 500 };
        var race10 = new Category { Event = springRun, CategoryName = "10km Race", DistanceKm = 10m, EntryFee = 150m, MaxParticipants = 300 };
        var ride40 = new Category { Event = cycle, CategoryName = "40km Road Ride", DistanceKm = 40m, EntryFee = 250m, MaxParticipants = 200 };
        var ride80 = new Category { Event = cycle, CategoryName = "80km Road Ride", DistanceKm = 80m, EntryFee = 350m, MaxParticipants = 150 };
        var walk5 = new Category { Event = walk, CategoryName = "5km Heritage Walk", DistanceKm = 5m, EntryFee = 50m, MaxParticipants = 400 };

        var e1 = new Enrolment { Participant = sipho, Category = fun5, BibNumber = "BIB0001", Status = EnrolmentStatuses.Confirmed };
        var e2 = new Enrolment { Participant = sipho, Category = ride40, BibNumber = "BIB0002", Status = EnrolmentStatuses.Confirmed };
        var e3 = new Enrolment { Participant = anelisa, Category = race10, BibNumber = "BIB0003", Status = EnrolmentStatuses.Confirmed };
        var e4 = new Enrolment { Participant = anelisa, Category = walk5, BibNumber = "BIB0004", Status = EnrolmentStatuses.Confirmed };

        var r1 = new Result { Enrolment = e1, FinishTime = new TimeSpan(0, 28, 45), Position = 12, Status = ResultStatuses.Finished };
        var r3 = new Result { Enrolment = e3, FinishTime = new TimeSpan(0, 52, 10), Position = 5, Status = ResultStatuses.Finished };

        db.AddRange(thabo, lindiwe, sipho, anelisa, springRun, cycle, walk,
                    fun5, race10, ride40, ride80, walk5, e1, e2, e3, e4, r1, r3);
        await db.SaveChangesAsync();
    }
}
