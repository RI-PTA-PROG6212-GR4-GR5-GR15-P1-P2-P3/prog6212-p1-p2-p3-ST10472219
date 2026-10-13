using Microsoft.EntityFrameworkCore;
using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>Event enrolments - Participants can enrol and the enrolment is correctly recorded.</summary>
public class EnrolmentTests
{
    [Fact]
    public async Task Enrol_AsParticipant_Returns201_AndRecordsParticipantEventAndCategory()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var ev = AddEvent(db, organiser, "Spring Run");
        var category = AddCategory(db, ev, "10km Race", 10m);
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(201, Status(result));
        var dto = Body<EnrolmentDto>(result);
        Assert.Equal(participant.ParticipantID, dto.ParticipantID);
        Assert.Equal(category.CategoryID, dto.CategoryID);
        Assert.Equal(ev.EventID, dto.EventID);
        Assert.Equal("Spring Run", dto.EventName);
        Assert.Equal("10km Race", dto.CategoryName);
        Assert.Equal("BIB0001", dto.BibNumber);
        Assert.Equal(EnrolmentStatuses.Confirmed, dto.Status);

        var saved = db.Enrolments.Single();
        Assert.Equal(participant.ParticipantID, saved.ParticipantID);
        Assert.Equal(category.CategoryID, saved.CategoryID);
        Assert.Equal("BIB0001", saved.BibNumber);
    }

    [Fact]
    public async Task Enrol_TwoParticipants_GetDifferentSequentialBibNumbers()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var first = AddParticipant(db, "a@test.com");
        var second = AddParticipant(db, "b@test.com");
        var category = AddCategory(db, AddEvent(db, organiser));

        await new EnrolmentsController(db).WithSession(first.ParticipantID, Roles.Participant).Enrol(category.CategoryID);
        await new EnrolmentsController(db).WithSession(second.ParticipantID, Roles.Participant).Enrol(category.CategoryID);

        var bibs = db.Enrolments.OrderBy(e => e.EnrolmentID).Select(e => e.BibNumber).ToArray();
        Assert.Equal(new[] { "BIB0001", "BIB0002" }, bibs);
    }

    [Fact]
    public async Task Enrol_BibNumberContinuesAfterExistingHighestBib()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var existing = AddParticipant(db, "a@test.com");
        var newcomer = AddParticipant(db, "b@test.com");
        var category = AddCategory(db, AddEvent(db, organiser));
        AddEnrolment(db, existing, category, "BIB0009");

        var result = await new EnrolmentsController(db)
            .WithSession(newcomer.ParticipantID, Roles.Participant).Enrol(category.CategoryID);

        Assert.Equal("BIB0010", Body<EnrolmentDto>(result).BibNumber);
    }

    [Fact]
    public async Task Enrol_AlreadyEnrolled_Returns409_AndDoesNotDuplicate()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var category = AddCategory(db, AddEvent(db, organiser));
        AddEnrolment(db, participant, category);
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(409, Status(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_CategoryFull_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var taken = AddParticipant(db, "a@test.com");
        var late = AddParticipant(db, "b@test.com");
        var category = AddCategory(db, AddEvent(db, organiser), max: 1);
        AddEnrolment(db, taken, category);
        var controller = new EnrolmentsController(db).WithSession(late.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(409, Status(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_CancelledEnrolmentsDoNotCountTowardsCapacity()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var cancelled = AddParticipant(db, "a@test.com");
        var newcomer = AddParticipant(db, "b@test.com");
        var category = AddCategory(db, AddEvent(db, organiser), max: 1);
        AddEnrolment(db, cancelled, category, "BIB0001", EnrolmentStatuses.Cancelled);
        var controller = new EnrolmentsController(db).WithSession(newcomer.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(201, Status(result));
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        using var db = NewDb();
        var participant = AddParticipant(db);
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        Assert.Equal(404, Status(await controller.Enrol(999)));
    }

    [Theory]
    [InlineData(EventStatuses.Cancelled)]
    [InlineData(EventStatuses.Completed)]
    public async Task Enrol_EventNotOpen_Returns409(string eventStatus)
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var category = AddCategory(db, AddEvent(db, organiser, status: eventStatus));
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        Assert.Equal(409, Status(await controller.Enrol(category.CategoryID)));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_EventAlreadyTakenPlace_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var category = AddCategory(db, AddEvent(db, organiser, "Last Week's Run", daysFromNow: -7));
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        Assert.Equal(409, Status(await controller.Enrol(category.CategoryID)));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_AfterCancelling_ReactivatesTheSameEnrolment()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var category = AddCategory(db, AddEvent(db, organiser));
        var cancelled = AddEnrolment(db, participant, category, "BIB0007", EnrolmentStatuses.Cancelled);
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(201, Status(result));
        var saved = db.Enrolments.Single();
        Assert.Equal(cancelled.EnrolmentID, saved.EnrolmentID);
        Assert.Equal(EnrolmentStatuses.Confirmed, saved.Status);
        Assert.Equal("BIB0007", saved.BibNumber);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var me = AddParticipant(db, "me@test.com");
        var other = AddParticipant(db, "other@test.com");
        var category = AddCategory(db, AddEvent(db, organiser));
        AddEnrolment(db, me, category, "BIB0001");
        AddEnrolment(db, other, category, "BIB0002");
        var controller = new EnrolmentsController(db).WithSession(me.ParticipantID, Roles.Participant);

        var result = await controller.GetMine();

        Assert.Equal(200, Status(result));
        var list = Body<List<EnrolmentDto>>(result);
        Assert.Equal("BIB0001", Assert.Single(list).BibNumber);
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_Returns204_AndMarksItCancelled()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var enrolment = AddEnrolment(db, participant, AddCategory(db, AddEvent(db, organiser)));
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        var result = await controller.Cancel(enrolment.EnrolmentID);

        Assert.Equal(204, Status(result));
        Assert.Equal(EnrolmentStatuses.Cancelled, db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task Cancel_AnotherParticipantsEnrolment_Returns403()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var owner = AddParticipant(db, "owner@test.com");
        var intruder = AddParticipant(db, "intruder@test.com");
        var enrolment = AddEnrolment(db, owner, AddCategory(db, AddEvent(db, organiser)));
        var controller = new EnrolmentsController(db).WithSession(intruder.ParticipantID, Roles.Participant);

        var result = await controller.Cancel(enrolment.EnrolmentID);

        Assert.Equal(403, Status(result));
        Assert.Equal(EnrolmentStatuses.Confirmed, db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task Cancel_UnknownEnrolment_Returns404()
    {
        using var db = NewDb();
        var participant = AddParticipant(db);
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        Assert.Equal(404, Status(await controller.Cancel(999)));
    }

    [Fact]
    public async Task Cancel_WhenResultAlreadyCaptured_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var enrolment = AddEnrolment(db, participant, AddCategory(db, AddEvent(db, organiser)));
        AddResult(db, enrolment);
        var controller = new EnrolmentsController(db).WithSession(participant.ParticipantID, Roles.Participant);

        Assert.Equal(409, Status(await controller.Cancel(enrolment.EnrolmentID)));
    }

    [Fact]
    public async Task GetForEvent_OwningOrganiser_SeesEnrolmentsAcrossAllCategories()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var five = AddCategory(db, ev, "5km", 5m);
        var ten = AddCategory(db, ev, "10km", 10m);
        AddEnrolment(db, AddParticipant(db, "a@test.com"), five, "BIB0001");
        AddEnrolment(db, AddParticipant(db, "b@test.com"), ten, "BIB0002");
        var otherEvent = AddEvent(db, organiser, "Other");
        AddEnrolment(db, AddParticipant(db, "c@test.com"), AddCategory(db, otherEvent), "BIB0003");
        var controller = new EnrolmentsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.GetForEvent(ev.EventID);

        Assert.Equal(200, Status(result));
        Assert.Equal(new[] { "BIB0001", "BIB0002" }, Body<List<EnrolmentDto>>(result).Select(e => e.BibNumber).ToArray());
    }

    [Fact]
    public async Task GetForEvent_AnotherOrganiser_Returns403()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var ev = AddEvent(db, owner);
        var controller = new EnrolmentsController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        Assert.Equal(403, Status(await controller.GetForEvent(ev.EventID)));
    }

    [Fact]
    public async Task GetForEvent_UnknownEvent_Returns404()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new EnrolmentsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(404, Status(await controller.GetForEvent(999)));
    }
}
