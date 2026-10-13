using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Dtos;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>Request validation rules and password hashing.</summary>
public class ValidationAndSecurityTests
{
    private static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }

    [Fact]
    public void RegisterOrganiser_ValidRequest_Passes() =>
        Assert.True(IsValid(new RegisterOrganiserRequest { FullName = "Thabo", Email = "thabo@test.com", Password = "Secret123!", PhoneNumber = "0821234567" }));

    [Fact]
    public void RegisterOrganiser_ShortPassword_Fails() =>
        Assert.False(IsValid(new RegisterOrganiserRequest { FullName = "Thabo", Email = "thabo@test.com", Password = "short" }));

    [Fact]
    public void RegisterParticipant_InvalidEmail_Fails() =>
        Assert.False(IsValid(new RegisterParticipantRequest { FullName = "Sipho", Email = "not-an-email", Password = "Secret123!" }));

    [Fact]
    public void RegisterParticipant_MissingName_Fails() =>
        Assert.False(IsValid(new RegisterParticipantRequest { FullName = "", Email = "sipho@test.com", Password = "Secret123!" }));

    [Fact]
    public void CreateEvent_MissingDate_Fails() =>
        Assert.False(IsValid(new CreateEventRequest { EventName = "Run", Location = "Pretoria", EventType = "Running" }));

    [Theory]
    [InlineData(0, 100, false)]
    [InlineData(-5, 100, false)]
    [InlineData(5, 0, false)]
    [InlineData(5, 100, true)]
    public void Category_DistanceAndCapacityMustBePositive(double distance, int max, bool expected) =>
        Assert.Equal(expected, IsValid(new CategoryRequest { CategoryName = "5km", DistanceKm = (decimal)distance, EntryFee = 50, MaxParticipants = max }));

    [Fact]
    public void Category_NegativeEntryFee_Fails() =>
        Assert.False(IsValid(new CategoryRequest { CategoryName = "5km", DistanceKm = 5, EntryFee = -1, MaxParticipants = 10 }));

    [Fact]
    public void PasswordHasher_ProducesDifferentHashesForTheSamePassword_AndFitsTheColumn()
    {
        var a = Hasher.Hash("Secret123!");
        var b = Hasher.Hash("Secret123!");

        Assert.NotEqual(a, b);                 // random salt
        Assert.True(a.Length <= 256);          // PasswordHash NVARCHAR(256)
        Assert.DoesNotContain("Secret123!", a);
    }

    [Fact]
    public void PasswordHasher_VerifiesCorrectPasswordOnly()
    {
        var hash = Hasher.Hash("Secret123!");

        Assert.True(Hasher.Verify("Secret123!", hash));
        Assert.False(Hasher.Verify("secret123!", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("HASHED_PASSWORD_1")]
    [InlineData("v1.abc.def.ghi")]
    public void PasswordHasher_RejectsMalformedHashesWithoutThrowing(string stored) =>
        Assert.False(Hasher.Verify("Secret123!", stored));
}
