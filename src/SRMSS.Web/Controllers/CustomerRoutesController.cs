using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;
using SRMSS.Web.Utilities;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("Customer")]
    public class CustomerRoutesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public CustomerRoutesController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        private string GetCustomerKey()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (userId.HasValue)
            {
                return $"CUSTOMER:{userId.Value}";
            }

            string username = HttpContext.Session.GetString(SessionKeys.Username) ?? "UNKNOWN";
            return $"CUSTOMER:{username}";
        }

        public async Task<IActionResult> Index(string? from, string? to)
        {
            string customerKey = GetCustomerKey();

            var routesQuery = _context.TransportRoutes
                .Include(r => r.RouteStops)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Driver)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Vehicle)
                .Where(r => r.Status == "Active")
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(from))
            {
                string cleanFrom = from.Trim();

                routesQuery = routesQuery.Where(r =>
                    r.StartPoint.Contains(cleanFrom) ||
                    r.RouteName.Contains(cleanFrom) ||
                    r.RouteStops.Any(s => s.StopName.Contains(cleanFrom)));
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                string cleanTo = to.Trim();

                routesQuery = routesQuery.Where(r =>
                    r.EndPoint.Contains(cleanTo) ||
                    r.RouteName.Contains(cleanTo) ||
                    r.RouteStops.Any(s => s.StopName.Contains(cleanTo)));
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

            string customerKey = GetCustomerKey();

            var route = await _context.TransportRoutes
                .Include(r => r.RouteStops)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Driver)
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Vehicle)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.Status == "Active");

            if (route == null)
            {
                return NotFound();
            }

            ViewBag.IsFavorite = await _context.FavoriteRoutes
                .AnyAsync(f =>
                    f.CustomerKey == customerKey &&
                    f.TransportRouteId == route.Id);

            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(
            int routeId,
            string? from,
            string? to,
            bool returnToDetails = false)
        {
            string customerKey = GetCustomerKey();

            var route = await _context.TransportRoutes
                .FirstOrDefaultAsync(r =>
                    r.Id == routeId &&
                    r.Status == "Active");

            if (route == null)
            {
                TempData["ErrorMessage"] = "Route could not be found.";
                return RedirectAfterFavoriteAction(routeId, from, to, returnToDetails);
            }

            bool alreadySaved = await _context.FavoriteRoutes
                .AnyAsync(f =>
                    f.CustomerKey == customerKey &&
                    f.TransportRouteId == routeId);

            if (alreadySaved)
            {
                TempData["ErrorMessage"] = "This route is already saved in favorites.";
                return RedirectAfterFavoriteAction(routeId, from, to, returnToDetails);
            }

            var favoriteRoute = new FavoriteRoute
            {
                CustomerKey = customerKey,
                TransportRouteId = routeId,
                SavedAt = DateTime.Now
            };

            _context.FavoriteRoutes.Add(favoriteRoute);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Add Favorite Route",
                "FavoriteRoutes",
                favoriteRoute.Id,
                $"Saved route {route.RouteName} to customer favorites"
            );

            TempData["SuccessMessage"] = "Route saved to favorites successfully.";
            return RedirectAfterFavoriteAction(routeId, from, to, returnToDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavorite(
            int routeId,
            string? from,
            string? to,
            bool returnToDetails = false)
        {
            string customerKey = GetCustomerKey();

            var favoriteRoute = await _context.FavoriteRoutes
                .Include(f => f.TransportRoute)
                .FirstOrDefaultAsync(f =>
                    f.CustomerKey == customerKey &&
                    f.TransportRouteId == routeId);

            if (favoriteRoute == null)
            {
                TempData["ErrorMessage"] = "This route is not currently saved in favorites.";
                return RedirectAfterFavoriteAction(routeId, from, to, returnToDetails);
            }

            string routeName = favoriteRoute.TransportRoute?.RouteName ?? $"Route #{routeId}";

            _context.FavoriteRoutes.Remove(favoriteRoute);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Remove Favorite Route",
                "FavoriteRoutes",
                favoriteRoute.Id,
                $"Removed {routeName} from customer favorites"
            );

            TempData["SuccessMessage"] = "Route removed from favorites successfully.";
            return RedirectAfterFavoriteAction(routeId, from, to, returnToDetails);
        }

        private IActionResult RedirectAfterFavoriteAction(
            int routeId,
            string? from,
            string? to,
            bool returnToDetails)
        {
            if (returnToDetails)
            {
                return RedirectToAction(nameof(Details), new { id = routeId });
            }

            return RedirectToAction(nameof(Index), new { from, to });
        }
    }
}
