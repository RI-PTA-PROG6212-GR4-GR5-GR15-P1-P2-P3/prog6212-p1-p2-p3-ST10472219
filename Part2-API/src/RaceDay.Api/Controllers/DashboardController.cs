using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers;

[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public DashboardController(RaceDayDbContext db) => _db = db;

    /// <summary>Summary numbers and per-event enrolment counts for the logged-in Organiser's dashboard.</summary>
    /// <remarks>**Role required:** Organiser. **Returns:** 200 dashboard; 401 not logged in; 403 not an Organiser.</remarks>
    [HttpGet("organiser")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardDto>> Organiser()
    {
        var events = await _db.Events.AsNoTracking()
            .Where(e => e.OrganiserID == CurrentUserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new DashboardEventDto(
                e.EventID,
                e.EventName,
                e.EventDate,
                e.Categories.SelectMany(c => c.Enrolments).Count(en => en.Status != EnrolmentStatuses.Cancelled)))
            .ToListAsync();

        return new DashboardDto(events.Count, events.Sum(e => e.Enrolments), events);
    }
}
