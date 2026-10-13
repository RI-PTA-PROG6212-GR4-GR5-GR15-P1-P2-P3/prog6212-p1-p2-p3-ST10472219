using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers;

/// <summary>View and update the logged-in user's own profile (both roles).</summary>
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ProfileController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <remarks>**Role required:** any logged-in user. **Expects:** nothing. **Returns:** 200 profile; 401 if not logged in.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        if (CurrentRole == Roles.Organiser)
        {
            var o = await _db.Organisers.AsNoTracking().FirstOrDefaultAsync(x => x.OrganiserID == CurrentUserId);
            if (o is not null) return Ok(Mappers.ToProfile(o));
        }
        else
        {
            var p = await _db.Participants.AsNoTracking().FirstOrDefaultAsync(x => x.ParticipantID == CurrentUserId);
            if (p is not null) return Ok(Mappers.ToProfile(p));
        }

        HttpContext.Session.Clear(); // account no longer exists
        return Error(StatusCodes.Status401Unauthorized, "Authentication required. Please log in.");
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <remarks>
    /// **Role required:** any logged-in user.
    /// **Expects:** `{ fullName, phoneNumber, dateOfBirth? }` (dateOfBirth applies to Participants only).
    /// **Returns:** 200 updated profile; 400 invalid data; 401 not logged in.
    /// </remarks>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        if (CurrentRole == Roles.Organiser)
        {
            var o = await _db.Organisers.FirstOrDefaultAsync(x => x.OrganiserID == CurrentUserId);
            if (o is not null)
            {
                o.FullName = request.FullName.Trim();
                o.PhoneNumber = request.PhoneNumber?.Trim();
                await _db.SaveChangesAsync();
                return Ok(Mappers.ToProfile(o));
            }
        }
        else
        {
            var p = await _db.Participants.FirstOrDefaultAsync(x => x.ParticipantID == CurrentUserId);
            if (p is not null)
            {
                if (request.DateOfBirth is { } dob)
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    if (dob >= today || dob <= today.AddYears(-120))
                        return BadRequestError("Date of birth must be a past date within the last 120 years.");
                    p.DateOfBirth = dob;
                }
                p.FullName = request.FullName.Trim();
                p.PhoneNumber = request.PhoneNumber?.Trim();
                await _db.SaveChangesAsync();
                return Ok(Mappers.ToProfile(p));
            }
        }

        HttpContext.Session.Clear();
        return Error(StatusCodes.Status401Unauthorized, "Authentication required. Please log in.");
    }
}
