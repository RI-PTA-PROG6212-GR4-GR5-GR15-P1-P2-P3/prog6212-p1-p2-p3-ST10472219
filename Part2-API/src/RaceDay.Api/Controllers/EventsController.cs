using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (public) and manage events (Organiser, owner only).</summary>
[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists upcoming events so Participants can browse.</summary>
    /// <remarks>
    /// **Role required:** none (public). **Expects:** nothing. Optional query: `includePast=true` also lists past events;
    /// `type=Running|Cycling|Walking` filters by event type.
    /// **Returns:** 200 array of events (soonest first).
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includePast = false, [FromQuery] string? type = null)
    {
        var query = _db.Events.AsNoTracking().AsQueryable();

        if (!includePast)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(e => e.EventDate >= today);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!EventTypes.TryNormalise(type, out var normalised))
                return BadRequestError("type must be Running, Cycling or Walking.");
            query = query.Where(e => e.EventType == normalised);
        }

        var events = await query.OrderBy(e => e.EventDate).Select(Mappers.EventProjection).ToListAsync();
        return Ok(events);
    }

    /// <summary>Retrieves full detail for one event, including its categories.</summary>
    /// <remarks>**Role required:** none (public). **Returns:** 200 event with categories; 404 if the event does not exist.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventID == id);

        return ev is null ? NotFoundError("Event not found.") : Ok(Mappers.ToEventDetail(ev));
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// **Role required:** Organiser.
    /// **Expects:** `{ eventName, eventDate (future), location, description, eventType (Running|Cycling|Walking) }`.
    /// **Returns:** 201 new event; 400 invalid data; 401 not logged in; 403 not an Organiser.
    /// </remarks>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        if (!EventTypes.TryNormalise(request.EventType, out var eventType))
            return BadRequestError("eventType must be Running, Cycling or Walking.");
        if (request.EventDate is null || request.EventDate.Value <= DateTime.UtcNow)
            return BadRequestError("eventDate must be in the future.");

        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate.Value,
            Location = request.Location.Trim(),
            Description = request.Description?.Trim(),
            EventType = eventType,
            Status = EventStatuses.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var created = await _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .FirstAsync(e => e.EventID == ev.EventID);
        return StatusCode(StatusCodes.Status201Created, Mappers.ToEventDetail(created));
    }

    /// <summary>Updates an event the Organiser owns.</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner only).
    /// **Expects:** `{ eventName, eventDate, location, description, eventType, status? }` (status: Scheduled|Cancelled|Completed).
    /// **Returns:** 200 updated event; 400 invalid data; 401 not logged in; 403 not the owning Organiser; 404 not found.
    /// </remarks>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFoundError("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return ForbiddenError("You can only modify your own events.");

        if (!EventTypes.TryNormalise(request.EventType, out var eventType))
            return BadRequestError("eventType must be Running, Cycling or Walking.");

        var status = ev.Status;
        if (request.Status is not null && !StatusMatcher.TryMatch(request.Status, EventStatuses.All, out status))
            return BadRequestError("status must be Scheduled, Cancelled or Completed.");

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventType = eventType;
        ev.Status = status;
        await _db.SaveChangesAsync();

        var updated = await _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .FirstAsync(e => e.EventID == id);
        return Ok(Mappers.ToEventDetail(updated));
    }

    /// <summary>Deletes an event the Organiser owns (and its categories).</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner only). **Expects:** nothing.
    /// **Returns:** 204 No Content; 401 not logged in; 403 not the owning Organiser; 404 not found;
    /// 409 if the event has active (non-cancelled) enrolments.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFoundError("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return ForbiddenError("You can only modify your own events.");

        var enrolments = await _db.Enrolments.Where(en => en.Category.EventID == id).ToListAsync();
        if (enrolments.Any(en => en.Status != EnrolmentStatuses.Cancelled))
            return ConflictError("This event has active enrolments and cannot be deleted. Cancel it instead.");

        // Cancelled enrolments (and their results) are removed first because Enrolment -> Category is RESTRICT.
        _db.Enrolments.RemoveRange(enrolments);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
