using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : Controller
{
    private readonly IRaceDayApi _api;

    public OrganiserController(IRaceDayApi api) => _api = api;
    public async Task<IActionResult> Index()
    {
        var r = await _api.Get<DashboardDto>("api/dashboard/organiser");
        ViewBag.Error = r.Error;
        return View(r.Data ?? new(0, 0, Array.Empty<DashboardEventDto>()));
    }

    public async Task<IActionResult> Events()
    {
        var r = await _api.Get<List<EventDto>>("api/events?includePast=true");
        ViewBag.Error = r.Error;
        var myId = HttpContext.Session.GetInt32("UserId");
        return View((r.Data ?? new()).Where(e => e.OrganiserID == myId).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new EventVm { EventDate = DateTime.Today.AddDays(14) });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);
        var r = await _api.Post<object, EventDetailDto>(
            "api/events", new { vm.EventName, vm.EventDate, vm.Location, vm.Description, vm.EventType });
        if (!r.Success || r.Data == null)
        {
            ModelState.AddModelError("", r.Error ?? "Could not create event.");
            return View(vm);
        }
        if (vm.Banner != null)
        {
            var upload = await _api.Upload<object>($"api/events/{r.Data.EventID}/banner", vm.Banner);
            if (!upload.Success)
                TempData["Notice"] = "Event created, but banner upload failed: " + upload.Error;
        }
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await _api.Get<EventDetailDto>($"api/events/{id}");
        if (!r.Success || r.Data == null)
            return NotFound();
        if (r.Data.OrganiserID != HttpContext.Session.GetInt32("UserId"))
            return StatusCode(StatusCodes.Status403Forbidden);
        var e = r.Data;
        return View(new EventVm { EventID = id, EventName = e.EventName, EventDate = e.EventDate, Location = e.Location,
                                  Description = e.Description, EventType = e.EventType, Status = e.Status });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);
        var r = await _api.Put<object, EventDetailDto>(
            $"api/events/{vm.EventID}",
            new { vm.EventName, vm.EventDate, vm.Location, vm.Description, vm.EventType, vm.Status });
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "Could not update event.");
            return View(vm);
        }
        if (vm.Banner != null)
        {
            var up = await _api.Upload<object>($"api/events/{vm.EventID}/banner", vm.Banner);
            if (!up.Success)
                TempData["Notice"] = "Event updated, but banner upload failed: " + up.Error;
        }
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _api.Delete($"api/events/{id}");
        TempData["Notice"] = r.Success ? "Event deleted." : r.Error;
        return RedirectToAction(nameof(Events));
    }

    public async Task<IActionResult> Categories(int id)
    {
        var r = await _api.Get<List<CategoryDto>>($"api/events/{id}/categories");
        ViewBag.EventId = id;
        ViewBag.Error = r.Error;
        return View(r.Data ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryVm vm)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Categories), new { id = vm.EventID });
        var body = new { vm.CategoryName, vm.DistanceKm, vm.EntryFee,  vm.MaxParticipants,
                         vm.CategoryType, vm.MinimumAge, vm.MaximumAge };
        var r = await _api.Post<object, CategoryDto>($"api/events/{vm.EventID}/categories", body);
        TempData["Notice"] = r.Success ? "Category added." : r.Error;
        return RedirectToAction(nameof(Categories), new { id = vm.EventID });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(int categoryId, int eventId, CategoryVm vm)
    {
        var body = new { vm.CategoryName, vm.DistanceKm, vm.EntryFee,  vm.MaxParticipants,
                         vm.CategoryType, vm.MinimumAge, vm.MaximumAge };
        var r = await _api.Put<object, CategoryDto>($"api/categories/{categoryId}", body);
        TempData["Notice"] = r.Success ? "Category updated." : r.Error;
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryId, int eventId)
    {
        var r = await _api.Delete($"api/categories/{categoryId}");
        TempData["Notice"] = r.Success ? "Category deleted." : r.Error;
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    public async Task<IActionResult> Enrolments(int id)
    {
        var r = await _api.Get<List<EnrolmentDto>>($"api/events/{id}/enrolments");
        ViewBag.Error = r.Error;
        ViewBag.EventId = id;
        return View(r.Data ?? new());
    }

    public async Task<IActionResult> Results(int id)
    {
        var entries = await _api.Get<List<EnrolmentDto>>($"api/events/{id}/enrolments");
        var results = await _api.Get<List<ResultDto>>($"api/events/{id}/results");
        ViewBag.EventId = id;
        ViewBag.Error = entries.Error ?? results.Error;
        var map = (results.Data ?? new()).ToDictionary(x => x.EnrolmentID);
        return View((entries.Data ?? new())
                        .Where(e => e.Status != "Cancelled")
                        .Select(e => new ResultEntryVm(e, map.GetValueOrDefault(e.EnrolmentID)))
                        .ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(int enrolmentId, int? resultId, string? finishTime, int? position,
                                                string status, int eventId)
    {
        TimeSpan? t = TimeSpan.TryParse(finishTime, out var parsed) ? parsed : null;
        var body = new { finishTime = t, position, status };
        var r = resultId.HasValue ? await _api.Put<object, ResultDto>($"api/results/{resultId}", body)
                                  : await _api.Post<object, ResultDto>($"api/enrolments/{enrolmentId}/result", body);
        TempData["Notice"] = r.Success ? "Result saved." : r.Error;
        return RedirectToAction(nameof(Results), new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var r = await _api.Post<object, object>($"api/events/{id}/results/publish", new {});
        TempData["Notice"] = r.Success ? "Results published." : r.Error;
        return RedirectToAction(nameof(Results), new { id });
    }
}
