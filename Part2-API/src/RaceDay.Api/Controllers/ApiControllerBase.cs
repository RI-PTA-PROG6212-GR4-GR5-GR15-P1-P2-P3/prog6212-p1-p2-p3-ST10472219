using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id (OrganiserID or ParticipantID, depending on role) of the logged-in user, 0 if none.</summary>
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

    protected ObjectResult Error(int statusCode, string message) =>
        StatusCode(statusCode, new ErrorResponse(message));

    protected ObjectResult NotFoundError(string message) => Error(StatusCodes.Status404NotFound, message);
    protected ObjectResult ForbiddenError(string message) => Error(StatusCodes.Status403Forbidden, message);
    protected ObjectResult ConflictError(string message) => Error(StatusCodes.Status409Conflict, message);
    protected ObjectResult BadRequestError(string message) => Error(StatusCodes.Status400BadRequest, message);
}
