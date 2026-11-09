using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApi _api;

    public AccountController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(vm);
        var r = await _api.Post<object, AuthVm>("api/auth/login", new { vm.Email, vm.Password });
        if (!r.Success || r.Data == null)
        {
            ModelState.AddModelError("", r.Error ?? "Login failed.");
            return View(vm);
        }
        SetSession(r.Data);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction("Index", r.Data.Role == "Organiser" ? "Organiser" : "Participant");
    }

    [HttpGet]
    public IActionResult Register() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);
        object body = new { vm.FullName, vm.Email, vm.Password, vm.PhoneNumber, vm.DateOfBirth };
        var path = vm.Role == "Organiser" ? "api/auth/register/organiser" : "api/auth/register/participant";
        var r = await _api.Post<object, AuthVm>(path, body);
        if (!r.Success || r.Data == null)
        {
            ModelState.AddModelError("", r.Error ?? "Registration failed.");
            return View(vm);
        }
        SetSession(r.Data);
        return RedirectToAction("Index", vm.Role == "Organiser" ? "Organiser" : "Participant");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.Post<object, object>("api/auth/logout", new {});
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private void SetSession(AuthVm a)
    {
        HttpContext.Session.SetInt32("UserId", a.User.Id);
        HttpContext.Session.SetString("Role", a.Role);
        HttpContext.Session.SetString("FullName", a.User.FullName);
    }
}
