namespace RaceDay.Api.Dtos;

public record ErrorResponse(string Message);

public record ProfileDto(
    int Id,
    string Role,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    DateTime CreatedAt);

public record AuthResponse(string Role, ProfileDto User);

public record EventDto(
    int EventID,
    int OrganiserID,
    string OrganiserName,
    string EventName,
    DateTime EventDate,
    string Location,
    string? Description,
    string EventType,
    string Status,
    int CategoryCount);

public record CategoryDto(
    int CategoryID,
    int EventID,
    string CategoryName,
    decimal DistanceKm,
    decimal EntryFee,
    int MaxParticipants,
    int EnrolledCount);

public record EventDetailDto(
    int EventID,
    int OrganiserID,
    string OrganiserName,
    string EventName,
    DateTime EventDate,
    string Location,
    string? Description,
    string EventType,
    string Status,
    IReadOnlyList<CategoryDto> Categories);

public record EnrolmentDto(
    int EnrolmentID,
    int ParticipantID,
    string ParticipantName,
    int CategoryID,
    string CategoryName,
    int EventID,
    string EventName,
    DateTime EventDate,
    decimal DistanceKm,
    decimal EntryFee,
    string BibNumber,
    string Status,
    DateTime EnrolmentDate);

public record ResultDto(
    int ResultID,
    int EnrolmentID,
    int EventID,
    string EventName,
    int CategoryID,
    string CategoryName,
    string ParticipantName,
    string BibNumber,
    TimeSpan? FinishTime,
    int? Position,
    string Status,
    DateTime CapturedAt);
