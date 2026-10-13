using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Event enrolments: Participants enter a category; Organisers view enrolments for their events.</summary>
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrols the logged-in Participant into a category and generates a bib number.</summary>
    /// <remarks>
    /// **Role required:** Participant (identified from the session). **Expects:** nothing in the body.
    /// **Returns:** 201 enrolment (links participant, event and category) with its bib number; 401 not logged in;
    /// 403 not a Participant; 404 category not found;
    /// 409 if already enrolled, the category is full, or the event is not open (cancelled, completed or already past).
    /// </remarks>
    [HttpPost("api/categories/{id:int}/enrol")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFoundError("Category not found.");

        if (category.Event.Status != EventStatuses.Scheduled)
            return ConflictError($"This event is {category.Event.Status.ToLowerInvariant()} and is not open for enrolment.");
        if (category.Event.EventDate <= DateTime.UtcNow)
            return ConflictError("This event has already taken place and is no longer open for enrolment.");

        var participantId = CurrentUserId;
        var existing = await _db.Enrolments
            .FirstOrDefaultAsync(en => en.ParticipantID == participantId && en.CategoryID == id);
        if (existing is not null && existing.Status != EnrolmentStatuses.Cancelled)
            return ConflictError("You are already enrolled in this category.");

        var enrolled = await _db.Enrolments
            .CountAsync(en => en.CategoryID == id && en.Status != EnrolmentStatuses.Cancelled);
        if (enrolled >= category.MaxParticipants)
            return ConflictError("This category is full.");

        // Re-enrolling after a cancellation re-activates the old record (one row per participant+category).
        if (existing is not null)
        {
            existing.Status = EnrolmentStatuses.Confirmed;
            existing.EnrolmentDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await CreatedAsync(existing.EnrolmentID);
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var enrolment = new Enrolment
            {
                ParticipantID = participantId,
                CategoryID = id,
                EnrolmentDate = DateTime.UtcNow,
                BibNumber = await NextBibNumberAsync(),
                Status = EnrolmentStatuses.Confirmed
            };
            _db.Enrolments.Add(enrolment);

            try
            {
                await _db.SaveChangesAsync();
                return await CreatedAsync(enrolment.EnrolmentID);
            }
            catch (DbUpdateException) when (attempt < 3)
            {
                // Another request took the same bib number at the same moment - try the next one.
                _db.Entry(enrolment).State = EntityState.Detached;
            }
        }

        return Error(StatusCodes.Status500InternalServerError, "Could not allocate a bib number. Please try again.");
    }

    /// <summary>Returns the logged-in Participant's own enrolments.</summary>
    /// <remarks>**Role required:** Participant. **Returns:** 200 array of enrolments (soonest event first); 401 not logged in; 403 not a Participant.</remarks>
    [HttpGet("api/enrolments/my")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = CurrentUserId;
        var enrolments = await _db.Enrolments.AsNoTracking()
            .Where(en => en.ParticipantID == participantId)
            .OrderBy(en => en.Category.Event.EventDate)
            .Select(Mappers.EnrolmentProjection)
            .ToListAsync();
        return Ok(enrolments);
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <remarks>
    /// **Role required:** Participant (owner only). **Expects:** nothing.
    /// **Returns:** 204 No Content (the enrolment is marked Cancelled); 401 not logged in; 403 not the owning Participant;
    /// 404 not found; 409 if a result has already been captured.
    /// </remarks>
    [HttpDelete("api/enrolments/{id:int}")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.Include(en => en.Result).FirstOrDefaultAsync(en => en.EnrolmentID == id);
        if (enrolment is null) return NotFoundError("Enrolment not found.");
        if (enrolment.ParticipantID != CurrentUserId) return ForbiddenError("You can only cancel your own enrolments.");
        if (enrolment.Result is not null) return ConflictError("A result has already been captured for this enrolment.");

        if (enrolment.Status != EnrolmentStatuses.Cancelled)
        {
            enrolment.Status = EnrolmentStatuses.Cancelled;
            await _db.SaveChangesAsync();
        }
        return NoContent();
    }

    /// <summary>Returns all enrolments across every category of an event the Organiser owns.</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner only). **Returns:** 200 array of enrolments; 401 not logged in;
    /// 403 not the owning Organiser; 404 event not found.
    /// </remarks>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null) return NotFoundError("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return ForbiddenError("You can only view enrolments for your own events.");

        var enrolments = await _db.Enrolments.AsNoTracking()
            .Where(en => en.Category.EventID == eventId)
            .OrderBy(en => en.Category.DistanceKm).ThenBy(en => en.BibNumber)
            .Select(Mappers.EnrolmentProjection)
            .ToListAsync();
        return Ok(enrolments);
    }

    private async Task<IActionResult> CreatedAsync(int enrolmentId)
    {
        var dto = await _db.Enrolments.AsNoTracking()
            .Where(en => en.EnrolmentID == enrolmentId)
            .Select(Mappers.EnrolmentProjection)
            .FirstAsync();
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>Next free bib number in the form BIB0001, BIB0002, ...</summary>
    private async Task<string> NextBibNumberAsync()
    {
        var last = await _db.Enrolments
            .Where(en => en.BibNumber.StartsWith("BIB"))
            .OrderByDescending(en => en.BibNumber.Length)
            .ThenByDescending(en => en.BibNumber)
            .Select(en => en.BibNumber)
            .FirstOrDefaultAsync();

        var number = 0;
        if (last is not null) int.TryParse(last.AsSpan(3), out number);
        return $"BIB{number + 1:D4}";
    }
}
