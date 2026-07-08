using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Models;

namespace SRMSS.Web.Controllers
{
    public class CustomerRoutesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerRoutesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCustomerKey()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated && !string.IsNullOrWhiteSpace(User.Identity.Name))
            {
                return User.Identity.Name;
            }

            return "DEMO_CUSTOMER";
        }

        public async Task<IActionResult> Index(string? from, string? to)
        {
            var customerKey = GetCustomerKey();

            var routesQuery = _context.TransportRoutes
                .Include(r => r.RouteStops)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Driver)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Vehicle)
                .Where(r => r.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(from))
            {
                routesQuery = routesQuery.Where(r =>
                    r.StartPoint.Contains(from) ||
                    r.RouteName.Contains(from) ||
                    r.RouteStops.Any(s => s.StopName.Contains(from)));
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                routesQuery = routesQuery.Where(r =>
                    r.EndPoint.Contains(to) ||
                    r.RouteName.Contains(to) ||
                    r.RouteStops.Any(s => s.StopName.Contains(to)));
            }

            var routes = await routesQuery
                .OrderBy(r => r.RouteName)
                .ToListAsync();

            var favoriteRoutes = await _context.FavoriteRoutes
                .Include(f => f.TransportRoute)
                .Where(f => f.CustomerKey == customerKey)
                .OrderByDescending(f => f.SavedAt)
                .ToListAsync();

            var favoriteRouteIds = favoriteRoutes
                .Select(f => f.TransportRouteId)
                .ToHashSet();

            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.FavoriteRoutes = favoriteRoutes;
            ViewBag.FavoriteRouteIds = favoriteRouteIds;

            return View(routes);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var route = await _context.TransportRoutes
                .Include(r => r.RouteStops)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Driver)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id && r.Status == "Active");

            if (route == null)
            {
                return NotFound();
            }

            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(int routeId, string? from, string? to)
        {
            var customerKey = GetCustomerKey();

            var routeExists = await _context.TransportRoutes
                .AnyAsync(r => r.Id == routeId && r.Status == "Active");

            if (!routeExists)
            {
                TempData["ErrorMessage"] = "Route could not be found.";
                return RedirectToAction(nameof(Index), new { from, to });
            }

            var alreadySaved = await _context.FavoriteRoutes
                .AnyAsync(f => f.CustomerKey == customerKey && f.TransportRouteId == routeId);

            if (!alreadySaved)
            {
                var favoriteRoute = new FavoriteRoute
                {
                    CustomerKey = customerKey,
                    TransportRouteId = routeId,
                    SavedAt = DateTime.Now
                };

                _context.FavoriteRoutes.Add(favoriteRoute);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Route saved to favorites.";
            }
            else
            {
                TempData["ErrorMessage"] = "This route is already in favorites.";
            }

            return RedirectToAction(nameof(Index), new { from, to });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavorite(int routeId, string? from, string? to)
        {
            var customerKey = GetCustomerKey();

            var favoriteRoute = await _context.FavoriteRoutes
                .FirstOrDefaultAsync(f => f.CustomerKey == customerKey && f.TransportRouteId == routeId);

            if (favoriteRoute != null)
            {
                _context.FavoriteRoutes.Remove(favoriteRoute);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Route removed from favorites.";
            }

            return RedirectToAction(nameof(Index), new { from, to });
        }
    }
}