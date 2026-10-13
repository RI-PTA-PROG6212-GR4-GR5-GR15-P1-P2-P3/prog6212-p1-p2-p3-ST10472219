using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>Event management - Organisers can manage their own events and nobody else's.</summary>
public class EventTests
{
    private static CreateEventRequest NewEvent(string type = "Running", int daysFromNow = 30) => new()
    {
        EventName = "Pretoria 10km", EventDate = DateTime.UtcNow.AddDays(daysFromNow),
        Location = "Union Buildings", Description = "Fun run", EventType = type
    };

    [Fact]
    public async Task Create_AsOrganiser_Returns201_AndEventIsOwnedByOrganiser()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Create(NewEvent("run"));

        Assert.Equal(201, Status(result));
        var dto = Body<EventDetailDto>(result);
        Assert.Equal("Running", dto.EventType);
        Assert.Equal(EventStatuses.Scheduled, dto.Status);
        Assert.Equal(organiser.OrganiserID, db.Events.Single().OrganiserID);
    }

    [Theory]
    [InlineData("Swimming")]
    [InlineData("")]
    public async Task Create_InvalidEventType_Returns400(string type)
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Create(NewEvent(type));

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Create_DateInThePast_Returns400()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Create(NewEvent(daysFromNow: -2));

        Assert.Equal(400, Status(result));
    }

    [Fact]
    public async Task Update_OwningOrganiser_Returns200_AndSavesChanges()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var request = new UpdateEventRequest
        {
            EventName = "Renamed Event", EventDate = DateTime.UtcNow.AddDays(60), Location = "New Venue",
            Description = "Updated", EventType = "Cycling", Status = "cancelled"
        };
        var result = await controller.Update(ev.EventID, request);

        Assert.Equal(200, Status(result));
        var saved = db.Events.Single();
        Assert.Equal("Renamed Event", saved.EventName);
        Assert.Equal(EventTypes.Cycling, saved.EventType);
        Assert.Equal(EventStatuses.Cancelled, saved.Status);
    }

    [Fact]
    public async Task Update_WithoutStatus_KeepsExistingStatus()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Update(ev.EventID, new UpdateEventRequest
        {
            EventName = "Same", EventDate = DateTime.UtcNow.AddDays(10), Location = "Here", EventType = "Running"
        });

        Assert.Equal(200, Status(result));
        Assert.Equal(EventStatuses.Scheduled, db.Events.Single().Status);
    }

    [Fact]
    public async Task Update_InvalidStatus_Returns400()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Update(ev.EventID, new UpdateEventRequest
        {
            EventName = "X", EventDate = DateTime.UtcNow.AddDays(10), Location = "Here", EventType = "Running", Status = "Postponed"
        });

        Assert.Equal(400, Status(result));
    }

    [Fact]
    public async Task Update_AnotherOrganisersEvent_Returns403_AndDoesNotChangeIt()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var ev = AddEvent(db, owner, "Original");
        var controller = new EventsController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        var result = await controller.Update(ev.EventID, new UpdateEventRequest
        {
            EventName = "Hacked", EventDate = DateTime.UtcNow.AddDays(10), Location = "Here", EventType = "Running"
        });

        Assert.Equal(403, Status(result));
        Assert.Equal("Original", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Update_UnknownEvent_Returns404()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Update(999, new UpdateEventRequest
        {
            EventName = "X", EventDate = DateTime.UtcNow.AddDays(10), Location = "Here", EventType = "Running"
        });

        Assert.Equal(404, Status(result));
    }

    [Fact]
    public async Task Delete_OwningOrganiser_Returns204_AndRemovesEvent()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Delete(ev.EventID);

        Assert.Equal(204, Status(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Delete_AnotherOrganisersEvent_Returns403()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var ev = AddEvent(db, owner);
        var controller = new EventsController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        var result = await controller.Delete(ev.EventID);

        Assert.Equal(403, Status(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Delete_EventWithActiveEnrolments_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var ev = AddEvent(db, organiser);
        var category = AddCategory(db, ev);
        AddEnrolment(db, participant, category);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Delete(ev.EventID);

        Assert.Equal(409, Status(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Delete_UnknownEvent_Returns404()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new EventsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(404, Status(await controller.Delete(123)));
    }

    [Fact]
    public async Task GetAll_ByDefault_ListsOnlyUpcomingEvents_SoonestFirst()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        AddEvent(db, organiser, "Past", daysFromNow: -30);
        AddEvent(db, organiser, "Later", daysFromNow: 40);
        AddEvent(db, organiser, "Soon", daysFromNow: 5);
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetAll();

        Assert.Equal(200, Status(result));
        var events = Body<List<EventDto>>(result);
        Assert.Equal(new[] { "Soon", "Later" }, events.Select(e => e.EventName).ToArray());
    }

    [Fact]
    public async Task GetAll_IncludePast_ListsEverything()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        AddEvent(db, organiser, "Past", daysFromNow: -30);
        AddEvent(db, organiser, "Future", daysFromNow: 30);
        var controller = new EventsController(db).WithSession();

        var events = Body<List<EventDto>>(await controller.GetAll(includePast: true));

        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task GetAll_FilterByType_ReturnsMatchingEventsOnly()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        AddEvent(db, organiser, "Run");
        var ride = AddEvent(db, organiser, "Ride");
        ride.EventType = EventTypes.Cycling;
        db.SaveChanges();
        var controller = new EventsController(db).WithSession();

        var events = Body<List<EventDto>>(await controller.GetAll(type: "cycle"));

        Assert.Equal("Ride", Assert.Single(events).EventName);
    }

    [Fact]
    public async Task GetById_ReturnsEventWithItsCategories()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var ev = AddEvent(db, organiser);
        var category = AddCategory(db, ev, "5km", 5m);
        AddCategory(db, ev, "10km", 10m);
        AddEnrolment(db, participant, category);
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetById(ev.EventID);

        Assert.Equal(200, Status(result));
        var dto = Body<EventDetailDto>(result);
        Assert.Equal(2, dto.Categories.Count);
        Assert.Equal(1, dto.Categories.Single(c => c.CategoryName == "5km").EnrolledCount);
    }

    [Fact]
    public async Task GetById_UnknownEvent_Returns404()
    {
        using var db = NewDb();

        Assert.Equal(404, Status(await new EventsController(db).WithSession().GetById(42)));
    }
}
