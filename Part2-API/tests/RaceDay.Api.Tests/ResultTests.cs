using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class ResultTests
{
    private static ResultRequest Finished(int minutes = 45, int position = 3) => new()
    {
        FinishTime = TimeSpan.FromMinutes(minutes), Position = position, Status = "Finished"
    };

    [Fact]
    public async Task Capture_OwningOrganiser_Returns201_AndStoresResult()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var enrolment = AddEnrolment(db, participant, AddCategory(db, AddEvent(db, organiser)));
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Capture(enrolment.EnrolmentID, Finished(52, 5));

        Assert.Equal(201, Status(result));
        var dto = Body<ResultDto>(result);
        Assert.Equal(TimeSpan.FromMinutes(52), dto.FinishTime);
        Assert.Equal(5, dto.Position);
        Assert.Equal("BIB0001", dto.BibNumber);

        var saved = db.Results.Single();
        Assert.Equal(enrolment.EnrolmentID, saved.EnrolmentID);
        Assert.Equal(ResultStatuses.Finished, saved.Status);
    }

    [Fact]
    public async Task Capture_DnfWithoutFinishTime_IsAllowed()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, organiser)));
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Capture(enrolment.EnrolmentID, new ResultRequest { Status = "dnf" });

        Assert.Equal(201, Status(result));
        Assert.Equal(ResultStatuses.DNF, db.Results.Single().Status);
    }

    [Fact]
    public async Task Capture_FinishedWithoutFinishTime_Returns400()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, organiser)));
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Capture(enrolment.EnrolmentID, new ResultRequest { Position = 1, Status = "Finished" });

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Capture_InvalidStatus_Returns400()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, organiser)));
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Capture(enrolment.EnrolmentID, new ResultRequest { FinishTime = TimeSpan.FromMinutes(30), Status = "Winner" });

        Assert.Equal(400, Status(result));
    }

    [Fact]
    public async Task Capture_AnotherOrganisersEvent_Returns403()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, owner)));
        var controller = new ResultsController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        var result = await controller.Capture(enrolment.EnrolmentID, Finished());

        Assert.Equal(403, Status(result));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Capture_UnknownEnrolment_Returns404()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(404, Status(await controller.Capture(999, Finished())));
    }

    [Fact]
    public async Task Capture_ResultAlreadyCaptured_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, organiser)));
        AddResult(db, enrolment);
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(409, Status(await controller.Capture(enrolment.EnrolmentID, Finished())));
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task Capture_CancelledEnrolment_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, organiser)), "BIB0001", EnrolmentStatuses.Cancelled);
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(409, Status(await controller.Capture(enrolment.EnrolmentID, Finished())));
    }

    [Fact]
    public async Task Update_OwningOrganiser_CorrectsTheResult()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, organiser)));
        var existing = AddResult(db, enrolment, position: 9, minutes: 60);
        var controller = new ResultsController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Update(existing.ResultID, Finished(minutes: 41, position: 2));

        Assert.Equal(200, Status(result));
        var saved = db.Results.Single();
        Assert.Equal(TimeSpan.FromMinutes(41), saved.FinishTime);
        Assert.Equal(2, saved.Position);
    }

    [Fact]
    public async Task Update_AnotherOrganiser_Returns403_AndUnknownResultReturns404()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var enrolment = AddEnrolment(db, AddParticipant(db), AddCategory(db, AddEvent(db, owner)));
        var existing = AddResult(db, enrolment, position: 9, minutes: 60);
        var controller = new ResultsController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        Assert.Equal(403, Status(await controller.Update(existing.ResultID, Finished())));
        Assert.Equal(404, Status(await controller.Update(999, Finished())));
        Assert.Equal(9, db.Results.Single().Position);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheParticipantsOwnResults()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var me = AddParticipant(db, "me@test.com");
        var other = AddParticipant(db, "other@test.com");
        var category = AddCategory(db, AddEvent(db, organiser));
        AddResult(db, AddEnrolment(db, me, category, "BIB0001"), position: 4, minutes: 33);
        AddResult(db, AddEnrolment(db, other, category, "BIB0002"), position: 1, minutes: 25);
        var controller = new ResultsController(db).WithSession(me.ParticipantID, Roles.Participant);

        var result = await controller.GetMine();

        Assert.Equal(200, Status(result));
        var mine = Assert.Single(Body<List<ResultDto>>(result));
        Assert.Equal(4, mine.Position);
        Assert.Equal("BIB0001", mine.BibNumber);
    }

    [Fact]
    public async Task GetForEvent_IsPublic_OrderedByPosition_AndUnknownEventIs404()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var category = AddCategory(db, ev);
        AddResult(db, AddEnrolment(db, AddParticipant(db, "a@test.com"), category, "BIB0001"), position: 2, minutes: 40);
        AddResult(db, AddEnrolment(db, AddParticipant(db, "b@test.com"), category, "BIB0002"), position: 1, minutes: 35);
        var controller = new ResultsController(db).WithSession();

        var result = await controller.GetForEvent(ev.EventID);

        Assert.Equal(200, Status(result));
        Assert.Equal(new[] { 1, 2 }, Body<List<ResultDto>>(result).Select(r => r.Position!.Value).ToArray());
        Assert.Equal(404, Status(await controller.GetForEvent(999)));
    }
}
