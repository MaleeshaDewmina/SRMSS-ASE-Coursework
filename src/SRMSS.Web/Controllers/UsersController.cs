using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;
using SRMSS.Web.Utilities;
using SRMSS.Web.ViewModels;

namespace SRMSS.Web.Controllers
{
    [Route("Users")]
    [RoleAuthorize("SuperAdmin", "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public UsersController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? roleFilter, string? search)
        {
            string currentRole = HttpContext.Session.GetString(SessionKeys.Role) ?? "";

            var usersQuery = _context.AppUsers.AsQueryable();

            if (currentRole == "Admin")
            {
                usersQuery = usersQuery.Where(u => u.Role == "User" || u.Role == "Customer");
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                usersQuery = usersQuery.Where(u => u.Role == roleFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Username.Contains(search) ||
                    u.Email.Contains(search));
            }

            ViewBag.RoleFilter = roleFilter;
            ViewBag.Search = search;
            ViewBag.CurrentRole = currentRole;

            var users = await usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewBag.CurrentRole = HttpContext.Session.GetString(SessionKeys.Role) ?? "";
            return View(new UserFormViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            string currentRole = HttpContext.Session.GetString(SessionKeys.Role) ?? "";

            string[] allowedRoles = currentRole == "SuperAdmin"
                ? new[] { "Admin", "User", "Customer" }
                : new[] { "User", "Customer" };

            if (!allowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError("Role", "You are not allowed to create an account with the selected role.");
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required");
            }

            bool usernameExists = await _context.AppUsers.AnyAsync(u => u.Username == model.Username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username is already taken");
            }

            bool emailExists = await _context.AppUsers.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email is already registered");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CurrentRole = currentRole;
                return View(model);
            }

            var user = new AppUser
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                PasswordHash = PasswordHasher.HashPassword(model.Password!),
                Role = model.Role,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Create User",
                "AppUsers",
                user.Id,
                $"Created {user.Role} account for {user.FullName}"
            );

            TempData["Success"] = "User account created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            string currentRole = HttpContext.Session.GetString(SessionKeys.Role) ?? "";

            var user = await _context.AppUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (currentRole == "Admin" && (user.Role == "Admin" || user.Role == "SuperAdmin"))
            {
                await _auditLogService.LogAsync(
                    "Access Denied",
                    "AppUsers",
                    user.Id,
                    $"Admin tried to edit protected account: {user.Username}"
                );

                return RedirectToAction("AccessDenied", "Account");
            }

            var model = new UserFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            };

            ViewBag.CurrentRole = currentRole;
            return View(model);
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserFormViewModel model)
        {
            string currentRole = HttpContext.Session.GetString(SessionKeys.Role) ?? "";
            int? currentUserId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            var user = await _context.AppUsers.FindAsync(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            if (currentRole == "Admin" && (user.Role == "Admin" || user.Role == "SuperAdmin"))
            {
                await _auditLogService.LogAsync(
                    "Access Denied",
                    "AppUsers",
                    user.Id,
                    $"Admin tried to update protected account: {user.Username}"
                );

                return RedirectToAction("AccessDenied", "Account");
            }

            string[] editableRoles = currentRole == "SuperAdmin"
                ? new[] { "SuperAdmin", "Admin", "User", "Customer" }
                : new[] { "User", "Customer" };

            if (!editableRoles.Contains(model.Role))
            {
                ModelState.AddModelError("Role", "You are not allowed to assign the selected role.");
            }

            if (currentUserId == user.Id)
            {
                if (!model.IsActive)
                {
                    ModelState.AddModelError("IsActive", "You cannot deactivate your own account.");
                }

                if (model.Role != user.Role)
                {
                    ModelState.AddModelError("Role", "You cannot change your own role.");
                }
            }

            bool usernameExists = await _context.AppUsers
                .AnyAsync(u => u.Username == model.Username && u.Id != model.Id);

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username is already taken");
            }

            bool emailExists = await _context.AppUsers
                .AnyAsync(u => u.Email == model.Email && u.Id != model.Id);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email is already registered");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CurrentRole = currentRole;
                return View(model);
            }

            string oldRole = user.Role;
            bool oldStatus = user.IsActive;

            user.FullName = model.FullName;
            user.Username = model.Username;
            user.Email = model.Email;
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = PasswordHasher.HashPassword(model.Password);
            }

            await _context.SaveChangesAsync();

            string passwordMessage = string.IsNullOrWhiteSpace(model.Password)
                ? "Password not changed"
                : "Password reset";

            await _auditLogService.LogAsync(
                "Update User",
                "AppUsers",
                user.Id,
                $"Updated user {user.Username}. Role: {oldRole} to {user.Role}. Status: {oldStatus} to {user.IsActive}. {passwordMessage}."
            );

            TempData["Success"] = "User account updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ToggleStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            string currentRole = HttpContext.Session.GetString(SessionKeys.Role) ?? "";
            int? currentUserId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            var user = await _context.AppUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (currentRole == "Admin" && (user.Role == "Admin" || user.Role == "SuperAdmin"))
            {
                await _auditLogService.LogAsync(
                    "Access Denied",
                    "AppUsers",
                    user.Id,
                    $"Admin tried to change status of protected account: {user.Username}"
                );

                return RedirectToAction("AccessDenied", "Account");
            }

            if (currentUserId == user.Id)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            string action = user.IsActive ? "Activate User" : "Deactivate User";

            await _auditLogService.LogAsync(
                action,
                "AppUsers",
                user.Id,
                $"{action}: {user.Username}"
            );

            TempData["Success"] = user.IsActive ? "User activated." : "User deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}