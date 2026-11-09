using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class MediaController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorage _blobs;
    public MediaController(RaceDayDbContext db, IBlobStorage blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    [HttpGet("{id:int}/banner")]
    public async Task<IActionResult> GetBanner(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
        if (ev?.BannerImageUrl == null)
            return NotFoundError("Event banner not found.");
        var image = await _blobs.DownloadAsync("event-banners", ev.BannerImageUrl);
        if (image == null)
            return NotFoundError("Event banner not found.");
        return File(image.Content, image.ContentType);
    }

    [HttpPost("{id:int}/banner"), SessionAuthorize(Roles.Organiser), RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> Banner(int id, [FromForm] ImageUploadRequest request)
    {
        var file = request?.File;
        if (file == null || file.Length == 0)
            return BadRequestError("Choose an image to upload.");
        if (file.Length > 5_000_000 || !new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
            return BadRequestError("Upload a JPG, PNG or WEBP image smaller than 5 MB.");
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
            return NotFoundError("Event not found.");
        if (ev.OrganiserID != CurrentUserId)
            return ForbiddenError("You can only update your own events.");
        ev.BannerImageUrl = await _blobs.UploadAsync(file, "event-banners", $"event-{id}");
        await _db.SaveChangesAsync();
        return Ok(new { ev.EventID, ev.BannerImageUrl });
    }
}
