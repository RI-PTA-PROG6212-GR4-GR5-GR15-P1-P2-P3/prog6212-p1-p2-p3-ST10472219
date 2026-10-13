using Microsoft.AspNetCore.Http;
using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>User registration and login (success and failure scenarios).</summary>
public class AuthTests
{
    private static AuthController Create(Data.RaceDayDbContext db) => new AuthController(db, Hasher).WithSession();

    [Fact]
    public async Task RegisterOrganiser_ValidData_Returns201_HashesPassword_AndStartsSession()
    {
        using var db = NewDb();
        var controller = Create(db);

        var result = await controller.RegisterOrganiser(new RegisterOrganiserRequest
        {
            FullName = "Thabo Mokoena", Email = "Thabo@RaceDay.co.za", Password = "Secret123!", PhoneNumber = "0821234567"
        });

        Assert.Equal(201, Status(result));
        var body = Body<AuthResponse>(result);
        Assert.Equal(Roles.Organiser, body.Role);
        Assert.Equal("thabo@raceday.co.za", body.User.Email);

        var saved = db.Organisers.Single();
        Assert.NotEqual("Secret123!", saved.PasswordHash);   // never stored in its original form
        Assert.StartsWith("v1.", saved.PasswordHash);
        Assert.True(Hasher.Verify("Secret123!", saved.PasswordHash));

        Assert.Equal(Roles.Organiser, controller.HttpContext.Session.GetString(SessionKeys.Role));
        Assert.Equal(saved.OrganiserID, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task RegisterParticipant_ValidData_Returns201_AndStoresDateOfBirth()
    {
        using var db = NewDb();
        var controller = Create(db);

        var result = await controller.RegisterParticipant(new RegisterParticipantRequest
        {
            FullName = "Sipho Dlamini", Email = "sipho@test.com", Password = "Secret123!",
            PhoneNumber = "0715551234", DateOfBirth = new DateOnly(1995, 3, 14)
        });

        Assert.Equal(201, Status(result));
        Assert.Equal(Roles.Participant, Body<AuthResponse>(result).Role);
        Assert.Equal(new DateOnly(1995, 3, 14), db.Participants.Single().DateOfBirth);
        Assert.Equal(Roles.Participant, controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task RegisterParticipant_FutureDateOfBirth_Returns400()
    {
        using var db = NewDb();

        var result = await Create(db).RegisterParticipant(new RegisterParticipantRequest
        {
            FullName = "Time Traveller", Email = "future@test.com", Password = "Secret123!",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        });

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Participants);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var db = NewDb();
        AddOrganiser(db, "taken@test.com");

        var result = await Create(db).RegisterOrganiser(new RegisterOrganiserRequest
        {
            FullName = "Another", Email = "taken@test.com", Password = "Secret123!"
        });

        Assert.Equal(409, Status(result));
        Assert.Single(db.Organisers);
    }

    [Fact]
    public async Task Register_EmailAlreadyUsedByOtherRole_Returns409()
    {
        using var db = NewDb();
        AddParticipant(db, "shared@test.com");

        var result = await Create(db).RegisterOrganiser(new RegisterOrganiserRequest
        {
            FullName = "Org", Email = "SHARED@test.com", Password = "Secret123!"
        });

        Assert.Equal(409, Status(result));
        Assert.Empty(db.Organisers);
    }

    [Fact]
    public async Task Login_ValidOrganiserCredentials_Returns200_WithRole_AndSession()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db, "org@test.com");
        var controller = Create(db);

        var result = await controller.Login(new LoginRequest { Email = "ORG@test.com", Password = "Secret123!" });

        Assert.Equal(200, Status(result));
        Assert.Equal(Roles.Organiser, Body<AuthResponse>(result).Role);
        Assert.Equal(Roles.Organiser, controller.HttpContext.Session.GetString(SessionKeys.Role));
        Assert.Equal(organiser.OrganiserID, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task Login_ValidParticipantCredentials_Returns200_WithParticipantRole()
    {
        using var db = NewDb();
        AddParticipant(db, "part@test.com");
        var controller = Create(db);

        var result = await controller.Login(new LoginRequest { Email = "part@test.com", Password = "Secret123!" });

        Assert.Equal(200, Status(result));
        Assert.Equal(Roles.Participant, Body<AuthResponse>(result).Role);
        Assert.Equal(Roles.Participant, controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401_AndNoSession()
    {
        using var db = NewDb();
        AddOrganiser(db, "org@test.com");
        var controller = Create(db);

        var result = await controller.Login(new LoginRequest { Email = "org@test.com", Password = "WrongPassword!" });

        Assert.Equal(401, Status(result));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var db = NewDb();

        var result = await Create(db).Login(new LoginRequest { Email = "nobody@test.com", Password = "Secret123!" });

        Assert.Equal(401, Status(result));
    }

    [Fact]
    public void Logout_ClearsTheSession()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new AuthController(db, Hasher).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = controller.Logout();

        Assert.Equal(204, Status(result));
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }
}
