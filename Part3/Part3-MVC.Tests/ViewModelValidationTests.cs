using System.ComponentModel.DataAnnotations;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Tests;

public class ViewModelValidationTests
{
    private static bool IsValid(object value) => Validator.TryValidateObject(value, new ValidationContext(value),
                                                                             new List<ValidationResult>(), true);
    [Fact]
    public void Login_RejectsInvalidEmail() => Assert.False(IsValid(new LoginVm { Email = "not-an-email",
                                                                                  Password = "secret" }));
    [Fact]
    public void Registration_RequiresEightCharacterPassword() =>
        Assert.False(IsValid(new RegisterVm { FullName = "Runner", Email = "runner@example.com", Password = "short" }));
    [Fact]
    public void Event_RequiresNameAndLocation() =>
        Assert.False(IsValid(new EventVm { EventName = "", Location = "", EventDate = DateTime.Today.AddDays(1) }));
    [Fact]
    public void Category_RejectsZeroDistance() => Assert.False(
        IsValid(new CategoryVm { CategoryName = "5km", DistanceKm = 0, EntryFee = 0, MaxParticipants = 100 }));
    [Fact]
    public void Profile_RequiresFullName() => Assert.False(IsValid(new ProfileVm { FullName = "" }));
}
