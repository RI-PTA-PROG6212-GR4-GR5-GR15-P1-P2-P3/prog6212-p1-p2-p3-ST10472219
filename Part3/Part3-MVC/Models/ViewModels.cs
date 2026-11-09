using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required]
    public string Password { get; set; } = "";
}
public class RegisterVm
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";
    [Phone]
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [Required]
    public string Role { get; set; } = "Participant";
}
public class EventVm
{
    public int EventID { get; set; }
    [Required, StringLength(150)]
    public string EventName { get; set; } = "";
    [Required]
    public DateTime EventDate { get; set; }
    [Required, StringLength(200)]
    public string Location { get; set; } = "";
    [StringLength(1000)]
    public string? Description { get; set; }
    [Required]
    public string EventType { get; set; } = "Running";
    public string? Status { get; set; }
    public IFormFile? Banner { get; set; }
}
public class CategoryVm
{
    public int EventID { get; set; }
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = "";
    [Required]
    public string CategoryType { get; set; } = "Distance";
    [Range(0.01, 9999.99)]
    public decimal DistanceKm { get; set; }
    [Range(0, 120)]
    public int? MinimumAge { get; set; }
    [Range(0, 120)]
    public int? MaximumAge { get; set; }
    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }
    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;
}
public class ProfileVm
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = "";
    [Phone]
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public IFormFile? Picture { get; set; }
}
public class EnrolVm
{
    [Required]
    public int CategoryID { get; set; }
}
public record AuthVm(string Role, ProfileDto User);
public record ProfileDto(int Id, string Role, string FullName, string Email, string? PhoneNumber, DateOnly? DateOfBirth,
                         DateTime CreatedAt, string? ProfilePictureUrl = null);
public record EventDto(int EventID, int OrganiserID, string OrganiserName, string EventName, DateTime EventDate,
                       string Location, string? Description, string EventType, string Status, int CategoryCount,
                       string? BannerImageUrl = null);
public record CategoryDto(int CategoryID, int EventID, string CategoryName, decimal DistanceKm, decimal EntryFee,
                          int MaxParticipants, int EnrolledCount, string CategoryType = "Distance",
                          int? MinimumAge = null, int? MaximumAge = null);
public record EventDetailDto(int EventID, int OrganiserID, string OrganiserName, string EventName, DateTime EventDate,
                             string Location, string? Description, string EventType, string Status,
                             IReadOnlyList<CategoryDto> Categories, string? BannerImageUrl = null);
public record EnrolmentDto(int EnrolmentID, int ParticipantID, string ParticipantName, int CategoryID,
                           string CategoryName, int EventID, string EventName, DateTime EventDate, decimal DistanceKm,
                           decimal EntryFee, string BibNumber, string Status, DateTime EnrolmentDate);
public record ResultDto(int ResultID, int EnrolmentID, int EventID, string EventName, int CategoryID,
                        string CategoryName, string ParticipantName, string BibNumber, TimeSpan? FinishTime,
                        int? Position, string Status, DateTime CapturedAt, int TotalFinishers = 0,
                        DateTime EventDate = default);
public record DashboardDto(int EventsCreated, int TotalEnrolments, IReadOnlyList<DashboardEventDto> Events);
public record DashboardEventDto(int EventID, string EventName, DateTime EventDate, int Enrolments);

public record ResultEntryVm(EnrolmentDto Enrolment, ResultDto? Result);
