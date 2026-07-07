using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;

namespace SRMSS.Web.Controllers
{
    [Route("AuditLogs")]
    [RoleAuthorize("SuperAdmin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? actionFilter, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var logsQuery = _context.AuditLogs
                .Include(a => a.AppUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                logsQuery = logsQuery.Where(a => a.Action == actionFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                logsQuery = logsQuery.Where(a =>
                    a.Action.Contains(search) ||
                    a.TableName.Contains(search) ||
                    a.Description.Contains(search) ||
                    (a.AppUser != null && (
                        a.AppUser.FullName.Contains(search) ||
                        a.AppUser.Username.Contains(search)
                    ))
                );
            }

            if (fromDate.HasValue)
            {
                logsQuery = logsQuery.Where(a => a.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                DateTime endDate = toDate.Value.Date.AddDays(1);
                logsQuery = logsQuery.Where(a => a.CreatedAt < endDate);
            }

            ViewBag.ActionFilter = actionFilter;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            ViewBag.Actions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            var logs = await logsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Take(500)
                .ToListAsync();

            return View(logs);
        }
    }
}