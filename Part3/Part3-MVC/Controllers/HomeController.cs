using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.Models;
using System.Net;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IRaceDayApi _api;

    public HomeController(IRaceDayApi api) => _api = api;
    public async Task<IActionResult> Index(string? type, string? q)
    {
        var r = await _api.Get<List<EventDto>>("api/events");
        if (!r.Success)
        {
            if (r.StatusCode == HttpStatusCode.ServiceUnavailable)
                return View("ApiUnavailable");
            ViewBag.ApiError = r.Error;
            return View(new List<EventDto>());
        }
        var items = r.Data ?? new();
        if (!string.IsNullOrWhiteSpace(type))
            items = items.Where(x => x.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(q))
            items = items
                        .Where(x => x.EventName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                    x.Location.Contains(q, StringComparison.OrdinalIgnoreCase))
                        .ToList();
        ViewBag.Type = type;
        ViewBag.Query = q;
        return View(items);
    }

    public IActionResult Error() => View();
    public IActionResult ApiUnavailable() => View();
    public IActionResult Status(int id) => View("Status", id);
}
