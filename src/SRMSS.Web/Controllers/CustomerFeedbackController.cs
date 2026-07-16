using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;
using SRMSS.Web.Utilities;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User", "Customer")]
    public class CustomerFeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public CustomerFeedbackController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(string? status, string? category, string? search)
        {
            string role = HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
            string customerKey = GetCustomerKey();

            IQueryable<CustomerFeedback> feedback = _context.CustomerFeedbacks
                .Include(f => f.AppUser)
                .Include(f => f.TransportRoute)
                .AsQueryable();

            if (role == "Customer")
            {
                feedback = feedback.Where(f => f.CustomerKey == customerKey);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                feedback = feedback.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                feedback = feedback.Where(f => f.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim();
                feedback = feedback.Where(f => f.Subject.Contains(cleanSearch) || f.Message.Contains(cleanSearch));
            }

            ViewBag.Status = status;
            ViewBag.Category = category;
            ViewBag.Search = search;
            ViewBag.IsCustomer = role == "Customer";
            ViewBag.OpenFeedback = await feedback.CountAsync(f => f.Status == "Open");
            ViewBag.InReviewFeedback = await feedback.CountAsync(f => f.Status == "In Review");
            ViewBag.ResolvedFeedback = await feedback.CountAsync(f => f.Status == "Resolved");
            ViewBag.AverageRating = await feedback.AverageAsync(f => (decimal?)f.Rating) ?? 0;

            return View(await feedback
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.CustomerFeedbacks
                .Include(f => f.AppUser)
                .Include(f => f.TransportRoute)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null)
            {
                return NotFound();
            }

            if (!CanAccessFeedback(feedback))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(feedback);
        }

        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Create()
        {
            await LoadRouteDropdown();
            return View(new CustomerFeedback { Rating = 5, Category = "General" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Create(
            [Bind("Id,TransportRouteId,Subject,Message,Category,Rating")]
            CustomerFeedback feedback)
        {
            if (ModelState.IsValid)
            {
                feedback.CustomerKey = GetCustomerKey();
                feedback.AppUserId = HttpContext.Session.GetInt32(SessionKeys.UserId);
                feedback.Status = "Open";
                feedback.Priority = feedback.Rating <= 2 ? "High" : "Normal";
                feedback.SubmittedAt = DateTime.Now;

                _context.CustomerFeedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    "Create Feedback",
                    "CustomerFeedbacks",
                    feedback.Id,
                    $"Customer submitted feedback: {feedback.Subject}.");

                TempData["SuccessMessage"] = "Feedback submitted successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadRouteDropdown(feedback.TransportRouteId);
            return View(feedback);
        }

        [RoleAuthorize("SuperAdmin", "Admin", "User")]
        public async Task<IActionResult> Respond(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.CustomerFeedbacks
                .Include(f => f.AppUser)
                .Include(f => f.TransportRoute)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin", "User")]
        public async Task<IActionResult> Respond(int id, string status, string priority, string response)
        {
            var feedback = await _context.CustomerFeedbacks.FindAsync(id);

            if (feedback == null)
            {
                return NotFound();
            }

            feedback.Status = string.IsNullOrWhiteSpace(status) ? "In Review" : status;
            feedback.Priority = string.IsNullOrWhiteSpace(priority) ? "Normal" : priority;
            feedback.Response = string.IsNullOrWhiteSpace(response) ? feedback.Response : response.Trim();
            feedback.RespondedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Respond Feedback",
                "CustomerFeedbacks",
                feedback.Id,
                $"Updated customer feedback #{feedback.Id} to {feedback.Status}.");

            TempData["SuccessMessage"] = "Feedback response updated successfully.";

            return RedirectToAction(nameof(Details), new { id = feedback.Id });
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.CustomerFeedbacks
                .Include(f => f.AppUser)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feedback = await _context.CustomerFeedbacks.FindAsync(id);

            if (feedback == null)
            {
                return NotFound();
            }

            _context.CustomerFeedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Delete Feedback",
                "CustomerFeedbacks",
                id,
                $"Deleted feedback #{id}.");

            TempData["SuccessMessage"] = "Feedback deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private string GetCustomerKey()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (userId.HasValue)
            {
                return $"CUSTOMER:{userId.Value}";
            }

            string username = HttpContext.Session.GetString(SessionKeys.Username) ?? "Guest";
            return $"CUSTOMER:{username}";
        }

        private bool CanAccessFeedback(CustomerFeedback feedback)
        {
            string role = HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;

            if (role == "SuperAdmin" || role == "Admin" || role == "User")
            {
                return true;
            }

            return role == "Customer" && feedback.CustomerKey == GetCustomerKey();
        }

        private async Task LoadRouteDropdown(object? selectedRoute = null)
        {
            var routes = await _context.TransportRoutes
                .Where(r => r.Status == "Active")
                .OrderBy(r => r.RouteName)
                .Select(r => new
                {
                    r.Id,
                    DisplayText = r.RouteName + " | " + r.StartPoint + " to " + r.EndPoint
                })
                .ToListAsync();

            ViewData["TransportRouteId"] = new SelectList(routes, "Id", "DisplayText", selectedRoute);
        }
    }
}
