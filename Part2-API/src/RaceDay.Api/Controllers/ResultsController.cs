using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Race results: Organisers capture them, Participants view their own, anyone can see an event leaderboard.</summary>
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Captures a finish result for a participant's enrolment.</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner of the event only).
    /// **Expects:** `{ finishTime (hh:mm:ss), position, status (Finished|DNF|DSQ) }`. finishTime is required when status is Finished.
    /// **Returns:** 201 result; 400 invalid data; 401 not logged in; 403 not the owning Organiser;
    /// 404 enrolment not found; 409 result already captured or the enrolment was cancelled.
    /// </remarks>
    [HttpPost("api/enrolments/{id:int}/result")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int id, [FromBody] ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentID == id);
        if (enrolment is null) return NotFoundError("Enrolment not found.");
        if (enrolment.Category.Event.OrganiserID != CurrentUserId)
            return ForbiddenError("You can only capture results for your own events.");

        var invalid = Validate(request, out var status);
        if (invalid is not null) return invalid;

        if (enrolment.Status == EnrolmentStatuses.Cancelled)
            return ConflictError("Cannot capture a result for a cancelled enrolment.");
        if (enrolment.Result is not null)
            return ConflictError("A result has already been captured for this enrolment. Use PUT /api/results/{id} to correct it.");

        var result = new Result
        {
            EnrolmentID = id,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = status,
            CapturedAt = DateTime.UtcNow
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadDtoAsync(result.ResultID));
    }

    /// <summary>Corrects an existing captured result.</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner of the event only). **Expects:** `{ finishTime, position, status }`.
    /// **Returns:** 200 updated result; 400 invalid data; 401 not logged in; 403 not the owning Organiser; 404 not found.
    /// </remarks>
    [HttpPut("api/results/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFoundError("Result not found.");
        if (result.Enrolment.Category.Event.OrganiserID != CurrentUserId)
            return ForbiddenError("You can only modify results for your own events.");

        var invalid = Validate(request, out var status);
        if (invalid is not null) return invalid;

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = status;
        await _db.SaveChangesAsync();

        return Ok(await LoadDtoAsync(id));
    }

    /// <summary>Returns the logged-in Participant's own results history.</summary>
    /// <remarks>**Role required:** Participant. **Returns:** 200 array of results; 401 not logged in; 403 not a Participant.</remarks>
    [HttpGet("api/results/my")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = CurrentUserId;
        var results = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.ParticipantID == participantId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(Mappers.ResultProjection)
            .ToListAsync();
        return Ok(results);
    }

    /// <summary>Returns the full results list (leaderboard) for an event.</summary>
    /// <remarks>**Role required:** none (public). **Returns:** 200 array of results ordered by position; 404 if the event does not exist.</remarks>
    [HttpGet("api/events/{eventId:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFoundError("Event not found.");

        var results = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.Category.EventID == eventId)
            .OrderBy(r => r.Enrolment.Category.DistanceKm)
            .ThenBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .ThenBy(r => r.FinishTime)
            .Select(Mappers.ResultProjection)
            .ToListAsync();
        return Ok(results);
    }

    private IActionResult? Validate(ResultRequest request, out string status)
    {
        if (!StatusMatcher.TryMatch(request.Status, ResultStatuses.All, out status))
            return BadRequestError("status must be Finished, DNF or DSQ.");

        if (status == ResultStatuses.Finished && (request.FinishTime is null || request.FinishTime.Value <= TimeSpan.Zero))
            return BadRequestError("finishTime (hh:mm:ss) greater than zero is required for a Finished result.");

        if (request.FinishTime is { } t && (t < TimeSpan.Zero || t >= TimeSpan.FromDays(1)))
            return BadRequestError("finishTime must be between 00:00:00 and 23:59:59.");

        return null;
    }

    private Task<ResultDto> LoadDtoAsync(int resultId) =>
        _db.Results.AsNoTracking()
            .Where(r => r.ResultID == resultId)
            .Select(Mappers.ResultProjection)
            .FirstAsync();
}
