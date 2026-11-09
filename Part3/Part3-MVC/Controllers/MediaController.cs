using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class MediaController : Controller
{
    private readonly IRaceDayApi _api;

    public MediaController(IRaceDayApi api) => _api = api;
    [HttpGet]
    public async Task<IActionResult> EventBanner(int id)
    {
        var file = await _api.GetFile($"api/events/{id}/banner");
        return file.Success ? File(file.Content!, file.ContentType!) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> ProfilePicture()
    {
        if (HttpContext.Session.GetString("Role") != "Participant")
            return NotFound();
        var file = await _api.GetFile("api/profile/picture");
        return file.Success ? File(file.Content!, file.ContentType!) : NotFound();
    }
}
