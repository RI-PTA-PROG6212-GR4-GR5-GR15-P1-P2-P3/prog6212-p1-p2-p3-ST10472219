using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>Minimal in-memory ISession so controllers can be tested without a web host.</summary>
public class TestSession : ISession
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
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}

public static class TestHelpers
{
    public static readonly IPasswordHasher Hasher = new PasswordHasher();

    /// <summary>A fresh, isolated in-memory database for every test.</summary>
    public static RaceDayDbContext NewDb() =>
        new(new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>Gives the controller an HTTP context with a session; optionally "logs in" as the given user and role.</summary>
    public static T WithSession<T>(this T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        if (userId.HasValue)
        {
            http.Session.SetInt32(SessionKeys.UserId, userId.Value);
            http.Session.SetString(SessionKeys.Role, role!);
        }
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    public static int Status(IActionResult result) =>
        result is IStatusCodeActionResult s && s.StatusCode.HasValue
            ? s.StatusCode.Value
            : throw new InvalidOperationException($"{result.GetType().Name} has no status code.");

    public static T Body<T>(IActionResult result) => (T)((ObjectResult)result).Value!;

    // ---- seed helpers ----

    public static Organiser AddOrganiser(RaceDayDbContext db, string email = "org@test.com", string name = "Olivia Organiser")
    {
        var o = new Organiser { FullName = name, Email = email, PasswordHash = Hasher.Hash("Secret123!"), PhoneNumber = "0821234567" };
        db.Organisers.Add(o);
        db.SaveChanges();
        return o;
    }

    public static Participant AddParticipant(RaceDayDbContext db, string email = "part@test.com", string name = "Percy Participant")
    {
        var p = new Participant { FullName = name, Email = email, PasswordHash = Hasher.Hash("Secret123!"), PhoneNumber = "0715551234", DateOfBirth = new DateOnly(1995, 3, 14) };
        db.Participants.Add(p);
        db.SaveChanges();
        return p;
    }

    public static Event AddEvent(RaceDayDbContext db, Organiser organiser, string name = "Test Run", int daysFromNow = 30, string status = EventStatuses.Scheduled)
    {
        var e = new Event
        {
            OrganiserID = organiser.OrganiserID,
            EventName = name,
            EventDate = DateTime.UtcNow.AddDays(daysFromNow),
            Location = "Pretoria",
            Description = "Test event",
            EventType = EventTypes.Running,
            Status = status
        };
        db.Events.Add(e);
        db.SaveChanges();
        return e;
    }

    public static Category AddCategory(RaceDayDbContext db, Event ev, string name = "10km Race", decimal km = 10m, int max = 100)
    {
        var c = new Category { EventID = ev.EventID, CategoryName = name, DistanceKm = km, EntryFee = 150m, MaxParticipants = max };
        db.Categories.Add(c);
        db.SaveChanges();
        return c;
    }

    public static Enrolment AddEnrolment(RaceDayDbContext db, Participant p, Category c, string bib = "BIB0001", string status = EnrolmentStatuses.Confirmed)
    {
        var en = new Enrolment { ParticipantID = p.ParticipantID, CategoryID = c.CategoryID, BibNumber = bib, Status = status };
        db.Enrolments.Add(en);
        db.SaveChanges();
        return en;
    }

    public static Result AddResult(RaceDayDbContext db, Enrolment en, int position = 1, int minutes = 30)
    {
        var r = new Result { EnrolmentID = en.EnrolmentID, FinishTime = TimeSpan.FromMinutes(minutes), Position = position, Status = ResultStatuses.Finished };
        db.Results.Add(r);
        db.SaveChanges();
        return r;
    }
}
