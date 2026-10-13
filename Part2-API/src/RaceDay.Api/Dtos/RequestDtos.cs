using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Body for registering a new Organiser.</summary>
public class RegisterOrganiserRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    /// <summary>Minimum 8 characters. Stored hashed (PBKDF2), never in plain text.</summary>
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
}

/// <summary>Body for registering a new Participant.</summary>
public class RegisterParticipantRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    /// <summary>Minimum 8 characters. Stored hashed (PBKDF2), never in plain text.</summary>
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    /// <summary>Format yyyy-MM-dd.</summary>
    public DateOnly? DateOfBirth { get; set; }
}

/// <summary>Credentials for logging in as an Organiser or a Participant.</summary>
public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

/// <summary>Editable profile fields.</summary>
public class UpdateProfileRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    /// <summary>Participants only (ignored for Organisers). Format yyyy-MM-dd.</summary>
    public DateOnly? DateOfBirth { get; set; }
}

/// <summary>Body for creating an event.</summary>
public class CreateEventRequest
{
    [Required, StringLength(150)] public string EventName { get; set; } = string.Empty;
    [Required] public DateTime? EventDate { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    /// <summary>Running, Cycling or Walking (run / cycle / walk are also accepted).</summary>
    [Required] public string EventType { get; set; } = string.Empty;
}

/// <summary>Body for updating an event. Status is optional; when omitted the current status is kept.</summary>
public class UpdateEventRequest : CreateEventRequest
{
    /// <summary>Scheduled, Cancelled or Completed.</summary>
    public string? Status { get; set; }
}

/// <summary>Body for creating or updating a category (e.g. "10km Race", "Under 20").</summary>
public class CategoryRequest
{
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Range(0.01, 9999.99)] public decimal DistanceKm { get; set; }
    [Range(0, 999999.99)] public decimal EntryFee { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 100;
}

/// <summary>Body for capturing or correcting a result.</summary>
public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss, e.g. 00:52:10. Required when Status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }
    /// <summary>Finishing position (1 = winner).</summary>
    [Range(1, int.MaxValue)] public int? Position { get; set; }
    /// <summary>Finished, DNF or DSQ. Defaults to Finished.</summary>
    public string Status { get; set; } = ResultStatuses.Finished;
}
