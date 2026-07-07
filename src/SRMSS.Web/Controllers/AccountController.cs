using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Models;
using SRMSS.Web.Services;
using SRMSS.Web.Utilities;
using SRMSS.Web.ViewModels;

namespace SRMSS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public AccountController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            string? role = HttpContext.Session.GetString(SessionKeys.Role);

            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            if (user == null)
            {
                await _auditLogService.LogAsync(
                    "Failed Login",
                    "AppUsers",
                    null,
                    $"Failed login attempt for username: {model.Username}"
                );

                ViewBag.Error = "Invalid username or password";
                return View(model);
            }

            bool passwordValid = PasswordHasher.VerifyPassword(model.Password, user.PasswordHash);

            if (!passwordValid)
            {
                await _auditLogService.LogAsync(
                    "Failed Login",
                    "AppUsers",
                    user.Id,
                    $"Invalid password attempt for username: {user.Username}"
                );

                ViewBag.Error = "Invalid username or password";
                return View(model);
            }

            HttpContext.Session.SetInt32(SessionKeys.UserId, user.Id);
            HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
            HttpContext.Session.SetString(SessionKeys.Username, user.Username);
            HttpContext.Session.SetString(SessionKeys.Role, user.Role);

            await _auditLogService.LogAsync(
                "Login",
                "AppUsers",
                user.Id,
                $"{user.FullName} logged in as {user.Role}"
            );

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Register()
        {
            string? role = HttpContext.Session.GetString(SessionKeys.Role);

            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterCustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool usernameExists = await _context.AppUsers
                .AnyAsync(u => u.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return View(model);
            }

            bool emailExists = await _context.AppUsers
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email is already registered");
                return View(model);
            }

            var customer = new AppUser
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                PasswordHash = PasswordHasher.HashPassword(model.Password),
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.AppUsers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Customer Registration",
                "AppUsers",
                customer.Id,
                $"New customer registered: {customer.FullName}"
            );

            TempData["Success"] = "Customer account created successfully. Please login.";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.AppUsers.FindAsync(userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.AppUsers.FindAsync(userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            bool usernameExists = await _context.AppUsers
                .AnyAsync(u => u.Username == model.Username && u.Id != user.Id);

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username is already taken");
            }

            bool emailExists = await _context.AppUsers
                .AnyAsync(u => u.Email == model.Email && u.Id != user.Id);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email is already registered");
            }

            if (!ModelState.IsValid)
            {
                model.Role = user.Role;
                return View(model);
            }

            user.FullName = model.FullName;
            user.Username = model.Username;
            user.Email = model.Email;

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
            HttpContext.Session.SetString(SessionKeys.Username, user.Username);

            await _auditLogService.LogAsync(
                "Update Profile",
                "AppUsers",
                user.Id,
                $"{user.Username} updated their profile details"
            );

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile", "Account");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.AppUsers.FindAsync(userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            bool currentPasswordValid = PasswordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash);

            if (!currentPasswordValid)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            if (model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError("NewPassword", "New password must be different from current password");
                return View(model);
            }

            user.PasswordHash = PasswordHasher.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Change Password",
                "AppUsers",
                user.Id,
                $"{user.Username} changed their account password"
            );

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Profile", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            string? username = HttpContext.Session.GetString(SessionKeys.Username);

            await _auditLogService.LogAsync(
                "Logout",
                "AppUsers",
                HttpContext.Session.GetInt32(SessionKeys.UserId),
                $"{username} logged out"
            );

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> AccessDenied()
        {
            string? username = HttpContext.Session.GetString(SessionKeys.Username);
            string? role = HttpContext.Session.GetString(SessionKeys.Role);

            await _auditLogService.LogAsync(
                "Access Denied",
                "System",
                null,
                $"Access denied for user {username} with role {role}"
            );

            return View();
        }
    }
}