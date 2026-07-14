using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SRMSS.Web.Models;
using SRMSS.Web.Utilities;

namespace SRMSS.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        string? role = HttpContext.Session.GetString(SessionKeys.Role);

        return string.IsNullOrWhiteSpace(role)
            ? RedirectToAction("Login", "Account")
            : RedirectToAction("Index", "Dashboard");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
