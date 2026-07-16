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


        // =========================================================
        // CUSTOMER KEY
        // =========================================================

        public async Task<IActionResult> Timetable(
            string? search,
            DateTime? date
        )
        {
            DateTime selectedDate =
                date?.Date ?? DateTime.Today;

            var query =
                _context.Schedules
                    .Include(s => s.TransportRoute)
                    .Where(s =>
                        s.ScheduleDate >= selectedDate
                        &&
                        s.TransportRoute != null
                        &&
                        s.TransportRoute.Status == "Active"
                    );

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword =
                    search.Trim();

                query =
                    query.Where(s =>
                        s.TransportRoute != null
                        &&
                        (
                            s.TransportRoute.RouteName.Contains(keyword)
                            ||
                            s.TransportRoute.StartPoint.Contains(keyword)
                            ||
                            s.TransportRoute.EndPoint.Contains(keyword)
                        )
                    );
            }

            var schedules =
                await query
                    .OrderBy(s => s.ScheduleDate)
                    .ThenBy(s => s.DepartureTime)
                    .Take(80)
                    .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedDate =
                selectedDate.ToString("yyyy-MM-dd");

            return View(schedules);
        }


        public async Task<IActionResult> LiveStatus(
            string? status
        )
        {
            DateTime fromDate =
                DateTime.Today.AddDays(-1);

            DateTime toDate =
                DateTime.Today.AddDays(2);

            var query =
                _context.Schedules
                    .Include(s => s.TransportRoute)
                    .Where(s =>
                        s.ScheduleDate >= fromDate
                        &&
                        s.ScheduleDate <= toDate
                        &&
                        s.TransportRoute != null
                        &&
                        s.TransportRoute.Status == "Active"
                    );

            if (!string.IsNullOrWhiteSpace(status))
            {
                query =
                    query.Where(s =>
                        s.Status == status
                    );
            }

            var schedules =
                await query
                    .OrderBy(s => s.ScheduleDate)
                    .ThenBy(s => s.DepartureTime)
                    .Take(80)
                    .ToListAsync();

            ViewBag.Status = status;

            return View(schedules);
        }

        private string GetCustomerKey()
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    SessionKeys.UserId
                );

            if (userId.HasValue)
            {
                return $"CUSTOMER:{userId.Value}";
            }

            string username =
                HttpContext.Session.GetString(
                    SessionKeys.Username
                )
                ?? "UNKNOWN";

            return $"CUSTOMER:{username}";
        }


        // =========================================================
        // CUSTOMER ROUTE SEARCH
        // =========================================================

        public async Task<IActionResult> Index(
            string? from,
            string? to)
        {
            string customerKey =
                GetCustomerKey();


            // Load all active routes first.
            // Searching is performed in memory so we can correctly
            // validate the order of From and To points along a route.

            var allActiveRoutes =
                await _context.TransportRoutes

                    .Include(route =>
                        route.RouteStops)

                    .Include(route =>
                        route.Schedules)

                        .ThenInclude(schedule =>
                            schedule.Driver)

                    .Include(route =>
                        route.Schedules)

                        .ThenInclude(schedule =>
                            schedule.Vehicle)

                    .Where(route =>
                        route.Status == "Active")

                    .AsSplitQuery()

                    .OrderBy(route =>
                        route.RouteName)

                    .ToListAsync();


            string cleanFrom =
                string.IsNullOrWhiteSpace(from)
                    ? string.Empty
                    : from.Trim();


            string cleanTo =
                string.IsNullOrWhiteSpace(to)
                    ? string.Empty
                    : to.Trim();


            bool searchPerformed =
                !string.IsNullOrWhiteSpace(cleanFrom)
                ||
                !string.IsNullOrWhiteSpace(cleanTo);


            List<TransportRoute> routes;


            if (searchPerformed)
            {
                routes =
                    allActiveRoutes

                        .Where(route =>
                            MatchesJourney(
                                route,
                                cleanFrom,
                                cleanTo
                            )
                        )

                        .OrderBy(route =>
                            route.RouteName)

                        .ToList();
            }
            else
            {
                routes =
                    allActiveRoutes;
            }


            // =====================================================
            // CUSTOMER FAVOURITES
            // =====================================================

            var favoriteRoutes =
                await _context.FavoriteRoutes

                    .Include(favorite =>
                        favorite.TransportRoute)

                    .Where(favorite =>
                        favorite.CustomerKey ==
                        customerKey)

                    .OrderByDescending(favorite =>
                        favorite.SavedAt)

                    .ToListAsync();


            var favoriteRouteIds =
                favoriteRoutes

                    .Select(favorite =>
                        favorite.TransportRouteId)

                    .ToHashSet();


            // =====================================================
            // POPULAR ROUTES
            // =====================================================

            var popularRoutes =
                allActiveRoutes

                    .OrderByDescending(route =>
                        route.Schedules.Count)

                    .ThenByDescending(route =>
                        route.RouteStops.Count)

                    .ThenBy(route =>
                        route.RouteName)

                    .Take(3)

                    .ToList();


            // =====================================================
            // LOCATION SUGGESTIONS
            // =====================================================

            var locationSuggestions =
                allActiveRoutes

                    .SelectMany(route =>
                        BuildRoutePointNames(route))

                    .Where(location =>
                        !string.IsNullOrWhiteSpace(location))

                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )

                    .OrderBy(location =>
                        location)

                    .ToList();


            // =====================================================
            // UPCOMING SCHEDULE STATISTICS
            // =====================================================

            int upcomingScheduleCount =
                allActiveRoutes

                    .SelectMany(route =>
                        route.Schedules)

                    .Count(schedule =>
                        schedule.ScheduleDate.Date >=
                            DateTime.Today

                        &&
                        schedule.Status !=
                            "Cancelled"

                        &&
                        schedule.Status !=
                            "Completed"
                    );


            // =====================================================
            // VIEW DATA
            // =====================================================

            ViewBag.From =
                cleanFrom;

            ViewBag.To =
                cleanTo;

            ViewBag.SearchPerformed =
                searchPerformed;

            ViewBag.FavoriteRoutes =
                favoriteRoutes;

            ViewBag.FavoriteRouteIds =
                favoriteRouteIds;

            ViewBag.PopularRoutes =
                popularRoutes;

            ViewBag.LocationSuggestions =
                locationSuggestions;

            ViewBag.TotalActiveRoutes =
                allActiveRoutes.Count;

            ViewBag.UpcomingSchedules =
                upcomingScheduleCount;


            return View(routes);
        }


        // =========================================================
        // ROUTE DETAILS
        // =========================================================

        public async Task<IActionResult> Details(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            string customerKey =
                GetCustomerKey();


            var route =
                await _context.TransportRoutes

                    .Include(route =>
                        route.RouteStops)

                    .Include(route =>
                        route.Schedules)

                        .ThenInclude(schedule =>
                            schedule.Driver)

                    .Include(route =>
                        route.Schedules)

                        .ThenInclude(schedule =>
                            schedule.Vehicle)

                    .AsSplitQuery()

                    .FirstOrDefaultAsync(route =>
                        route.Id == id
                        &&
                        route.Status == "Active"
                    );


            if (route == null)
            {
                return NotFound();
            }


            ViewBag.IsFavorite =
                await _context.FavoriteRoutes

                    .AnyAsync(favorite =>
                        favorite.CustomerKey ==
                            customerKey

                        &&
                        favorite.TransportRouteId ==
                            route.Id
                    );


            return View(route);
        }


        // =========================================================
        // ADD FAVOURITE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(
            int routeId,
            string? from,
            string? to,
            bool returnToDetails = false)
        {
            string customerKey =
                GetCustomerKey();


            var route =
                await _context.TransportRoutes

                    .FirstOrDefaultAsync(route =>
                        route.Id == routeId
                        &&
                        route.Status == "Active"
                    );


            if (route == null)
            {
                TempData["ErrorMessage"] =
                    "Route could not be found.";

                return RedirectAfterFavoriteAction(
                    routeId,
                    from,
                    to,
                    returnToDetails
                );
            }


            bool alreadySaved =
                await _context.FavoriteRoutes

                    .AnyAsync(favorite =>
                        favorite.CustomerKey ==
                            customerKey

                        &&
                        favorite.TransportRouteId ==
                            routeId
                    );


            if (alreadySaved)
            {
                TempData["ErrorMessage"] =
                    "This route is already saved in your favourites.";

                return RedirectAfterFavoriteAction(
                    routeId,
                    from,
                    to,
                    returnToDetails
                );
            }


            var favoriteRoute =
                new FavoriteRoute
                {
                    CustomerKey =
                        customerKey,

                    TransportRouteId =
                        routeId,

                    SavedAt =
                        DateTime.Now
                };


            _context.FavoriteRoutes.Add(
                favoriteRoute
            );


            await _context.SaveChangesAsync();


            await _auditLogService.LogAsync(
                "Add Favorite Route",
                "FavoriteRoutes",
                favoriteRoute.Id,
                $"Saved route {route.RouteName} to customer favourites"
            );


            TempData["SuccessMessage"] =
                $"{route.RouteName} was saved to your favourites.";


            return RedirectAfterFavoriteAction(
                routeId,
                from,
                to,
                returnToDetails
            );
        }


        // =========================================================
        // REMOVE FAVOURITE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavorite(
            int routeId,
            string? from,
            string? to,
            bool returnToDetails = false)
        {
            string customerKey =
                GetCustomerKey();


            var favoriteRoute =
                await _context.FavoriteRoutes

                    .Include(favorite =>
                        favorite.TransportRoute)

                    .FirstOrDefaultAsync(favorite =>
                        favorite.CustomerKey ==
                            customerKey

                        &&
                        favorite.TransportRouteId ==
                            routeId
                    );


            if (favoriteRoute == null)
            {
                TempData["ErrorMessage"] =
                    "This route is not currently saved in your favourites.";

                return RedirectAfterFavoriteAction(
                    routeId,
                    from,
                    to,
                    returnToDetails
                );
            }


            string routeName =
                favoriteRoute.TransportRoute?.RouteName
                ?? $"Route #{routeId}";


            _context.FavoriteRoutes.Remove(
                favoriteRoute
            );


            await _context.SaveChangesAsync();


            await _auditLogService.LogAsync(
                "Remove Favorite Route",
                "FavoriteRoutes",
                favoriteRoute.Id,
                $"Removed {routeName} from customer favourites"
            );


            TempData["SuccessMessage"] =
                $"{routeName} was removed from your favourites.";


            return RedirectAfterFavoriteAction(
                routeId,
                from,
                to,
                returnToDetails
            );
        }


        // =========================================================
        // SEARCH HELPERS
        // =========================================================

        private bool MatchesJourney(
            TransportRoute route,
            string from,
            string to)
        {
            var routePoints =
                BuildRoutePointNames(route);


            if (
                string.IsNullOrWhiteSpace(from)
                &&
                string.IsNullOrWhiteSpace(to)
            )
            {
                return true;
            }


            // Search only From location.
            if (
                !string.IsNullOrWhiteSpace(from)
                &&
                string.IsNullOrWhiteSpace(to)
            )
            {
                return routePoints.Any(point =>
                    LocationMatches(
                        point,
                        from
                    )
                );
            }


            // Search only To location.
            if (
                string.IsNullOrWhiteSpace(from)
                &&
                !string.IsNullOrWhiteSpace(to)
            )
            {
                return routePoints.Any(point =>
                    LocationMatches(
                        point,
                        to
                    )
                );
            }


            // Both From and To are supplied.
            // Destination must appear after the departure point.

            for (
                int fromIndex = 0;
                fromIndex < routePoints.Count;
                fromIndex++
            )
            {
                if (
                    !LocationMatches(
                        routePoints[fromIndex],
                        from
                    )
                )
                {
                    continue;
                }


                for (
                    int toIndex =
                        fromIndex + 1;

                    toIndex <
                        routePoints.Count;

                    toIndex++
                )
                {
                    if (
                        LocationMatches(
                            routePoints[toIndex],
                            to
                        )
                    )
                    {
                        return true;
                    }
                }
            }


            return false;
        }


        private List<string> BuildRoutePointNames(
            TransportRoute route)
        {
            var points =
                new List<string>();


            AddUniquePoint(
                points,
                route.StartPoint
            );


            if (route.RouteStops != null)
            {
                foreach (
                    var stop in
                    route.RouteStops
                        .OrderBy(stop =>
                            stop.StopOrder)
                )
                {
                    AddUniquePoint(
                        points,
                        stop.StopName
                    );
                }
            }


            AddUniquePoint(
                points,
                route.EndPoint
            );


            return points;
        }


        private void AddUniquePoint(
            List<string> points,
            string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return;
            }


            string cleanLocation =
                location.Trim();


            bool alreadyExists =
                points.Any(existing =>
                    string.Equals(
                        existing,
                        cleanLocation,
                        StringComparison.OrdinalIgnoreCase
                    )
                );


            if (!alreadyExists)
            {
                points.Add(cleanLocation);
            }
        }


        private bool LocationMatches(
            string routeLocation,
            string searchLocation)
        {
            if (
                string.IsNullOrWhiteSpace(routeLocation)
                ||
                string.IsNullOrWhiteSpace(searchLocation)
            )
            {
                return false;
            }


            string routeValue =
                routeLocation.Trim();


            string searchValue =
                searchLocation.Trim();


            return
                routeValue.Contains(
                    searchValue,
                    StringComparison.OrdinalIgnoreCase
                )

                ||

                searchValue.Contains(
                    routeValue,
                    StringComparison.OrdinalIgnoreCase
                );
        }


        // =========================================================
        // REDIRECT AFTER FAVOURITE ACTION
        // =========================================================

        private IActionResult RedirectAfterFavoriteAction(
            int routeId,
            string? from,
            string? to,
            bool returnToDetails)
        {
            if (returnToDetails)
            {
                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = routeId
                    }
                );
            }


            return RedirectToAction(
                nameof(Index),
                new
                {
                    from,
                    to
                }
            );
        }
    }
}
