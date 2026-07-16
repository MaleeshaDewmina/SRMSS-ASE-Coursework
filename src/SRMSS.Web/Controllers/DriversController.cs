using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User")]
    public class DriversController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public DriversController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? depot)
        {
            IQueryable<Driver> drivers = _context.Drivers
                .Include(d => d.Schedules)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim();

                drivers = drivers.Where(d =>
                    d.FullName.Contains(cleanSearch) ||
                    d.NIC.Contains(cleanSearch) ||
                    d.LicenseNumber.Contains(cleanSearch) ||
                    d.Phone.Contains(cleanSearch) ||
                    (d.Email != null && d.Email.Contains(cleanSearch)) ||
                    (d.EmployeeNumber != null && d.EmployeeNumber.Contains(cleanSearch)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                drivers = drivers.Where(d => d.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(depot))
            {
                drivers = drivers.Where(d => d.AssignedDepot == depot);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Depot = depot;
            ViewBag.TotalDrivers = await _context.Drivers.CountAsync();
            ViewBag.AvailableDrivers = await _context.Drivers.CountAsync(d => d.Status == "Available" && d.IsActive);
            ViewBag.AssignedDrivers = await _context.Drivers.CountAsync(d => d.Status == "Assigned" && d.IsActive);
            ViewBag.ExpiredLicenses = await _context.Drivers.CountAsync(d => d.LicenseExpiryDate < DateTime.Today && d.IsActive);
            ViewBag.ExpiringSoon = await _context.Drivers.CountAsync(d => d.LicenseExpiryDate >= DateTime.Today && d.LicenseExpiryDate <= DateTime.Today.AddDays(30) && d.IsActive);
            ViewBag.Depots = await _context.Drivers
                .Where(d => !string.IsNullOrWhiteSpace(d.AssignedDepot))
                .Select(d => d.AssignedDepot)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            return View(await drivers
                .OrderByDescending(d => d.IsActive)
                .ThenBy(d => d.FullName)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.Schedules)
                    .ThenInclude(s => s.TransportRoute)
                .Include(d => d.Schedules)
                    .ThenInclude(s => s.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public IActionResult Create()
        {
            return View(new Driver
            {
                LicenseExpiryDate = DateTime.Today.AddYears(1),
                HireDate = DateTime.Today,
                Status = "Available",
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Create(
            [Bind("Id,FullName,NIC,LicenseNumber,LicenseExpiryDate,Phone,Email,Address,EmployeeNumber,AssignedDepot,ShiftType,EmergencyContactName,EmergencyContactPhone,HireDate,Status,IsActive")]
            Driver driver)
        {
            await ValidateDriver(driver);

            if (ModelState.IsValid)
            {
                driver.CreatedAt = DateTime.Now;
                driver.UpdatedAt = null;

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    "Create Driver",
                    "Drivers",
                    driver.Id,
                    $"Created driver profile for {driver.FullName}.");

                TempData["SuccessMessage"] = "Driver profile created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(driver);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers.FindAsync(id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,FullName,NIC,LicenseNumber,LicenseExpiryDate,Phone,Email,Address,EmployeeNumber,AssignedDepot,ShiftType,EmergencyContactName,EmergencyContactPhone,HireDate,Status,IsActive,CreatedAt")]
            Driver driver)
        {
            if (id != driver.Id)
            {
                return NotFound();
            }

            await ValidateDriver(driver, driver.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    driver.UpdatedAt = DateTime.Now;
                    _context.Update(driver);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DriverExists(driver.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                await _auditLogService.LogAsync(
                    "Update Driver",
                    "Drivers",
                    driver.Id,
                    $"Updated driver profile for {driver.FullName}.");

                TempData["SuccessMessage"] = "Driver profile updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(driver);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.Schedules)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);

            if (driver == null)
            {
                return NotFound();
            }

            driver.IsActive = false;
            driver.Status = "Inactive";
            driver.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Deactivate Driver",
                "Drivers",
                id,
                $"Deactivated driver profile for {driver.FullName}.");

            TempData["SuccessMessage"] = "Driver deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);

            if (driver == null)
            {
                return NotFound();
            }

            driver.IsActive = true;
            driver.Status = "Available";
            driver.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Reactivate Driver",
                "Drivers",
                id,
                $"Reactivated driver profile for {driver.FullName}.");

            TempData["SuccessMessage"] = "Driver reactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateDriver(
            Driver driver,
            int? editingId = null)
        {
            bool duplicateLicense = await _context.Drivers.AnyAsync(d =>
                d.Id != editingId &&
                d.LicenseNumber == driver.LicenseNumber);

            if (duplicateLicense)
            {
                ModelState.AddModelError(
                    "LicenseNumber",
                    "Another driver already uses this license number.");
            }

            bool duplicateNic = await _context.Drivers.AnyAsync(d =>
                d.Id != editingId &&
                d.NIC == driver.NIC);

            if (duplicateNic)
            {
                ModelState.AddModelError(
                    "NIC",
                    "Another driver already uses this NIC number.");
            }
        }

        private bool DriverExists(int id)
        {
            return _context.Drivers.Any(e => e.Id == id);
        }
    }
}
