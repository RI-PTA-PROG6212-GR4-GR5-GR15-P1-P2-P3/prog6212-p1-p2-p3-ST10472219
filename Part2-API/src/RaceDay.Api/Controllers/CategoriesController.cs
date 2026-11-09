using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Distance / age categories for events (e.g. 5km, 10km, 21km, Under 20, Senior).</summary>
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all categories for an event.</summary>
    /// <remarks>**Role required:** none (public). **Returns:** 200 array of categories; 404 if the event does not exist.</remarks>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFoundError("Event not found.");

        var categories = await _db.Categories.AsNoTracking()
            .Include(c => c.Enrolments)
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKm).ThenBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories.Select(Mappers.ToCategory).ToList());
    }

    /// <summary>Retrieves detail for one category.</summary>
    /// <remarks>**Role required:** none (public). **Returns:** 200 category; 404 if not found.</remarks>
    [HttpGet("api/categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _db.Categories.AsNoTracking()
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        return category is null ? NotFoundError("Category not found.") : Ok(Mappers.ToCategory(category));
    }

    /// <summary>Adds a category to an event the Organiser owns.</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner only).
    /// **Expects:** `{ categoryName, categoryType (Distance|Age), distanceKm (&gt; 0), entryFee (&gt;= 0), maxParticipants (&gt;= 1), minimumAge/maximumAge (Age categories only) }`.
    /// **Returns:** 201 new category; 400 invalid data; 401 not logged in; 403 not the owning Organiser; 404 event not found.
    /// </remarks>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null) return NotFoundError("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return ForbiddenError("You can only modify your own events.");

        if (!ValidateCategory(request, out var categoryError)) return BadRequestError(categoryError!);
        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm,
            CategoryType = NormaliseType(request.CategoryType),
            MinimumAge = IsAge(request.CategoryType) ? request.MinimumAge : null,
            MaximumAge = IsAge(request.CategoryType) ? request.MaximumAge : null,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, Mappers.ToCategory(category));
    }

    /// <summary>Updates a category on an event the Organiser owns.</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner only).
    /// **Expects:** `{ categoryName, categoryType, distanceKm, entryFee, maxParticipants, minimumAge, maximumAge }`.
    /// **Returns:** 200 updated category; 400 invalid data; 401 not logged in; 403 not the owning Organiser; 404 not found;
    /// 409 if maxParticipants is lower than the current number of enrolments.
    /// </remarks>
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFoundError("Category not found.");
        if (category.Event.OrganiserID != CurrentUserId) return ForbiddenError("You can only modify your own events.");

        if (!ValidateCategory(request, out var categoryError)) return BadRequestError(categoryError!);
        var enrolled = category.Enrolments.Count(en => en.Status != EnrolmentStatuses.Cancelled);
        if (request.MaxParticipants < enrolled)
            return ConflictError($"maxParticipants cannot be lower than the {enrolled} current enrolment(s).");

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.CategoryType = NormaliseType(request.CategoryType);
        category.MinimumAge = IsAge(request.CategoryType) ? request.MinimumAge : null;
        category.MaximumAge = IsAge(request.CategoryType) ? request.MaximumAge : null;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;
        await _db.SaveChangesAsync();

        return Ok(Mappers.ToCategory(category));
    }

    private static bool IsAge(string? type) => string.Equals(type, CategoryTypes.Age, StringComparison.OrdinalIgnoreCase);

    private static string NormaliseType(string? type) => IsAge(type) ? CategoryTypes.Age : CategoryTypes.Distance;

    /// <summary>A category is either a distance category or an age category (which needs a valid age range).</summary>
    private static bool ValidateCategory(CategoryRequest request, out string? error)
    {
        error = null;

        var isDistance = string.Equals(request.CategoryType, CategoryTypes.Distance, StringComparison.OrdinalIgnoreCase);
        if (!isDistance && !IsAge(request.CategoryType))
        {
            error = "Category type must be Distance or Age.";
            return false;
        }

        if (IsAge(request.CategoryType) &&
            (request.MinimumAge is null || request.MaximumAge is null || request.MinimumAge < 0 ||
             request.MaximumAge > 120 || request.MinimumAge > request.MaximumAge))
        {
            error = "Age categories require a valid minimum and maximum age between 0 and 120.";
            return false;
        }

        return true;
    }

    /// <summary>Deletes a category from an event the Organiser owns.</summary>
    /// <remarks>
    /// **Role required:** Organiser (owner only). **Expects:** nothing.
    /// **Returns:** 204 No Content; 401 not logged in; 403 not the owning Organiser; 404 not found;
    /// 409 if the category has active enrolments.
    /// </remarks>
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFoundError("Category not found.");
        if (category.Event.OrganiserID != CurrentUserId) return ForbiddenError("You can only modify your own events.");

        if (category.Enrolments.Any(en => en.Status != EnrolmentStatuses.Cancelled))
            return ConflictError("This category has enrolments and cannot be deleted.");

        // Remove cancelled enrolments first (Enrolment -> Category is RESTRICT).
        _db.Enrolments.RemoveRange(category.Enrolments);
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
