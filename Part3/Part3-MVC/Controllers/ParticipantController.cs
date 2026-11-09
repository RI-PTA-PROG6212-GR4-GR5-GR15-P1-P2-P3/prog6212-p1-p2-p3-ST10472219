using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RoleAuthorize("Participant")]
public class ParticipantController : Controller
{
    private readonly IRaceDayApi _api;

    public ParticipantController(IRaceDayApi api) => _api = api;
    public async Task<IActionResult> Index(string? type, string? q)
    {
        var r = await _api.Get<List<EventDto>>("api/events");
        ViewBag.Error = r.Error;
        var items = r.Data ?? new();
        if (!string.IsNullOrWhiteSpace(type))
            items = items.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(q))
            items = items
                        .Where(e => e.EventName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                    e.Location.Contains(q, StringComparison.OrdinalIgnoreCase))
                        .ToList();
        ViewBag.Type = type;
        ViewBag.Query = q;
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _api.Get<EventDetailDto>($"api/events/{id}");
        if (!r.Success || r.Data == null)
            return NotFound();
        var mine = await _api.Get<List<EnrolmentDto>>("api/enrolments/my");
        ViewBag.Enrolled = (mine.Data ?? new())
                               .Where(e => e.EventID == id && e.Status != "Cancelled")
                               .ToDictionary(e => e.CategoryID, e => e.EnrolmentID);
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, int categoryId)
    {
        var r = await _api.Post<object, EnrolmentDto>($"api/categories/{categoryId}/enrol", new {});
        TempData["Notice"] = r.Success ? "Your enrolment has been recorded." : r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int enrolmentId)
    {
        var r = await _api.Delete($"api/enrolments/{enrolmentId}");
        TempData["Notice"] = r.Success ? "Your enrolment has been cancelled." : r.Error;
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyEvents()
    {
        var r = await _api.Get<List<EnrolmentDto>>("api/enrolments/my");
        ViewBag.Error = r.Error;
        return View(r.Data ?? new());
    }

    public async Task<IActionResult> MyResults()
    {
        var r = await _api.Get<List<ResultDto>>("api/results/my");
        ViewBag.Error = r.Error;
        return View(r.Data ?? new());
    }

    public async Task<IActionResult> Profile()
    {
        var r = await _api.Get<ProfileDto>("api/profile");
        if (!r.Success || r.Data == null)
            return RedirectToAction("Login", "Account");
        return View(new ProfileVm { FullName = r.Data.FullName, PhoneNumber = r.Data.PhoneNumber,
                                    DateOfBirth = r.Data.DateOfBirth, ProfilePictureUrl = r.Data.ProfilePictureUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);
        var r = await _api.Put<object, ProfileDto>("api/profile", new { vm.FullName, vm.PhoneNumber, vm.DateOfBirth });
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "Profile update failed.");
            return View(vm);
        }
        if (vm.Picture != null)
        {
            var up = await _api.Upload<object>("api/profile/picture", vm.Picture);
            if (!up.Success)
            {
                TempData["Notice"] = "Profile updated, but the picture upload failed: " + up.Error;
                return RedirectToAction(nameof(Profile));
            }
        }
        TempData["Notice"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
}
