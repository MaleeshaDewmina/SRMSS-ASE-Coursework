using Microsoft.AspNetCore.Mvc;
using SRMSS.Web.Filters;
using SRMSS.Web.Utilities;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User", "Customer")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            string? role = HttpContext.Session.GetString(SessionKeys.Role);

            ViewBag.FullName = HttpContext.Session.GetString(SessionKeys.FullName);
            ViewBag.Username = HttpContext.Session.GetString(SessionKeys.Username);
            ViewBag.Role = role;

            return View();
        }
    }
}