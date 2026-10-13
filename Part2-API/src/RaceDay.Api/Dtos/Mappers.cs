using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public static class Mappers
{
    public static ProfileDto ToProfile(Organiser o) =>
        new(o.OrganiserID, Roles.Organiser, o.FullName, o.Email, o.PhoneNumber, null, o.CreatedAt);

    public static ProfileDto ToProfile(Participant p) =>
        new(p.ParticipantID, Roles.Participant, p.FullName, p.Email, p.PhoneNumber, p.DateOfBirth, p.CreatedAt);

    /// <summary>Requires <c>Enrolments</c> to be loaded so the enrolled count is correct.</summary>
    public static CategoryDto ToCategory(Category c) =>
        new(c.CategoryID, c.EventID, c.CategoryName, c.DistanceKm, c.EntryFee, c.MaxParticipants,
            c.Enrolments.Count(en => en.Status != EnrolmentStatuses.Cancelled));

    /// <summary>Requires <c>Organiser</c> and <c>Categories.Enrolments</c> to be loaded.</summary>
    public static EventDetailDto ToEventDetail(Event e) =>
        new(e.EventID, e.OrganiserID, e.Organiser.FullName, e.EventName, e.EventDate, e.Location,
            e.Description, e.EventType, e.Status,
            e.Categories.OrderBy(c => c.DistanceKm).Select(ToCategory).ToList());

    public static readonly Expression<Func<Event, EventDto>> EventProjection = e =>
        new EventDto(e.EventID, e.OrganiserID, e.Organiser.FullName, e.EventName, e.EventDate,
            e.Location, e.Description, e.EventType, e.Status, e.Categories.Count());

    public static readonly Expression<Func<Enrolment, EnrolmentDto>> EnrolmentProjection = en =>
        new EnrolmentDto(en.EnrolmentID, en.ParticipantID, en.Participant.FullName, en.CategoryID,
            en.Category.CategoryName, en.Category.EventID, en.Category.Event.EventName,
            en.Category.Event.EventDate, en.Category.DistanceKm, en.Category.EntryFee,
            en.BibNumber, en.Status, en.EnrolmentDate);

    public static readonly Expression<Func<Result, ResultDto>> ResultProjection = r =>
        new ResultDto(r.ResultID, r.EnrolmentID, r.Enrolment.Category.EventID,
            r.Enrolment.Category.Event.EventName, r.Enrolment.CategoryID,
            r.Enrolment.Category.CategoryName, r.Enrolment.Participant.FullName,
            r.Enrolment.BibNumber, r.FinishTime, r.Position, r.Status, r.CapturedAt);
}
