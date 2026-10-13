using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class ProfileTests
{
    [Fact]
    public async Task Get_AsOrganiser_ReturnsOwnProfile()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db, "org@test.com", "Olivia Organiser");
        AddOrganiser(db, "other@test.com", "Someone Else");
        var controller = new ProfileController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Get();

        Assert.Equal(200, Status(result));
        var profile = Body<ProfileDto>(result);
        Assert.Equal("Olivia Organiser", profile.FullName);
        Assert.Equal(Roles.Organiser, profile.Role);
    }

    [Fact]
    public async Task Get_AsParticipant_ReturnsOwnProfile()
    {
        using var db = NewDb();
        var participant = AddParticipant(db, "part@test.com", "Percy Participant");
        var controller = new ProfileController(db).WithSession(participant.ParticipantID, Roles.Participant);

        var result = await controller.Get();

        Assert.Equal(200, Status(result));
        Assert.Equal("Percy Participant", Body<ProfileDto>(result).FullName);
        Assert.Equal(new DateOnly(1995, 3, 14), Body<ProfileDto>(result).DateOfBirth);
    }

    [Fact]
    public async Task Get_WhenAccountNoLongerExists_Returns401()
    {
        using var db = NewDb();
        var controller = new ProfileController(db).WithSession(999, Roles.Participant);

        var result = await controller.Get();

        Assert.Equal(401, Status(result));
    }

    [Fact]
    public async Task Update_AsParticipant_ChangesNameAndPhone()
    {
        using var db = NewDb();
        var participant = AddParticipant(db);
        var controller = new ProfileController(db).WithSession(participant.ParticipantID, Roles.Participant);

        var result = await controller.Update(new UpdateProfileRequest { FullName = "Percy Updated", PhoneNumber = "0829999999" });

        Assert.Equal(200, Status(result));
        var saved = db.Participants.Single();
        Assert.Equal("Percy Updated", saved.FullName);
        Assert.Equal("0829999999", saved.PhoneNumber);
    }

    [Fact]
    public async Task Update_AsOrganiser_OnlyChangesOwnRecord()
    {
        using var db = NewDb();
        var me = AddOrganiser(db, "me@test.com", "Me");
        var other = AddOrganiser(db, "other@test.com", "Other");
        var controller = new ProfileController(db).WithSession(me.OrganiserID, Roles.Organiser);

        var result = await controller.Update(new UpdateProfileRequest { FullName = "Me Updated", PhoneNumber = "0820000000" });

        Assert.Equal(200, Status(result));
        Assert.Equal("Me Updated", db.Organisers.Single(o => o.OrganiserID == me.OrganiserID).FullName);
        Assert.Equal("Other", db.Organisers.Single(o => o.OrganiserID == other.OrganiserID).FullName);
    }

    [Fact]
    public async Task Update_FutureDateOfBirth_Returns400()
    {
        using var db = NewDb();
        var participant = AddParticipant(db);
        var controller = new ProfileController(db).WithSession(participant.ParticipantID, Roles.Participant);

        var result = await controller.Update(new UpdateProfileRequest
        {
            FullName = "Percy", DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5))
        });

        Assert.Equal(400, Status(result));
    }
}
