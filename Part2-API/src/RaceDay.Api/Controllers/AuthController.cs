using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Registration, login and logout. Authentication state (user id + role) is kept in a
/// server-side session identified by an HttpOnly cookie.
/// </summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _hasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>Registers a new Organiser account (public).</summary>
    /// <remarks>
    /// **Role required:** none (public).
    /// **Expects:** `{ fullName, email, password (min 8 chars), phoneNumber }`.
    /// **Returns:** 201 with the new user and role, and the caller is logged in (session cookie set).
    /// 400 if the data is invalid, 409 if the email is already registered.
    /// </remarks>
    [HttpPost("register/organiser")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterOrganiser([FromBody] RegisterOrganiserRequest request)
    {
        var email = NormaliseEmail(request.Email);
        if (await EmailInUseAsync(email))
            return ConflictError("That email address is already registered.");

        var organiser = new Organiser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.Organisers.Add(organiser);
        await _db.SaveChangesAsync();

        StartSession(organiser.OrganiserID, Roles.Organiser);
        return StatusCode(StatusCodes.Status201Created,
            new AuthResponse(Roles.Organiser, Mappers.ToProfile(organiser)));
    }

    /// <summary>Registers a new Participant account (public).</summary>
    /// <remarks>
    /// **Role required:** none (public).
    /// **Expects:** `{ fullName, email, password (min 8 chars), phoneNumber, dateOfBirth (yyyy-MM-dd) }`.
    /// **Returns:** 201 with the new user and role, and the caller is logged in (session cookie set).
    /// 400 if the data is invalid, 409 if the email is already registered.
    /// </remarks>
    [HttpPost("register/participant")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterParticipant([FromBody] RegisterParticipantRequest request)
    {
        if (!IsValidDateOfBirth(request.DateOfBirth))
            return BadRequestError("Date of birth must be a past date within the last 120 years.");

        var email = NormaliseEmail(request.Email);
        if (await EmailInUseAsync(email))
            return ConflictError("That email address is already registered.");

        var participant = new Participant
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim(),
            DateOfBirth = request.DateOfBirth,
            CreatedAt = DateTime.UtcNow
        };
        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();

        StartSession(participant.ParticipantID, Roles.Participant);
        return StatusCode(StatusCodes.Status201Created,
            new AuthResponse(Roles.Participant, Mappers.ToProfile(participant)));
    }

    /// <summary>Logs in an Organiser or Participant (public).</summary>
    /// <remarks>
    /// **Role required:** none (public).
    /// **Expects:** `{ email, password }`.
    /// **Returns:** 200 with the user and role; a session cookie is set and used for all later requests.
    /// 401 if the credentials are invalid.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = NormaliseEmail(request.Email);

        var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser is not null && _hasher.Verify(request.Password, organiser.PasswordHash))
        {
            StartSession(organiser.OrganiserID, Roles.Organiser);
            return Ok(new AuthResponse(Roles.Organiser, Mappers.ToProfile(organiser)));
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.Email == email);
        if (participant is not null && _hasher.Verify(request.Password, participant.PasswordHash))
        {
            StartSession(participant.ParticipantID, Roles.Participant);
            return Ok(new AuthResponse(Roles.Participant, Mappers.ToProfile(participant)));
        }

        return Error(StatusCodes.Status401Unauthorized, "Invalid email or password.");
    }

    /// <summary>Logs out the current user by clearing the session.</summary>
    /// <remarks>**Role required:** none. **Returns:** 204 No Content.</remarks>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }

    private static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task<bool> EmailInUseAsync(string email) =>
        await _db.Organisers.AnyAsync(o => o.Email == email) ||
        await _db.Participants.AnyAsync(p => p.Email == email);

    private void StartSession(int userId, string role)
    {
        // Clear first so a previous session id is never reused for a new login.
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
        HttpContext.Session.SetString(SessionKeys.Role, role);
    }

    private static bool IsValidDateOfBirth(DateOnly? dob)
    {
        if (dob is null) return true;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return dob.Value < today && dob.Value > today.AddYears(-120);
    }
}
