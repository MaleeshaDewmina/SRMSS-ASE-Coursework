using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
        private readonly IConfiguration _configuration;

        public AccountController(
            ApplicationDbContext context,
            AuditLogService auditLogService,
            IConfiguration configuration)
        {
            _context = context;
            _auditLogService = auditLogService;
            _configuration = configuration;
        }


        // =========================================================
        // LOGIN
        // =========================================================

        [HttpGet]
        public IActionResult Login(string? googleError = null)
        {
            string? role =
                HttpContext.Session.GetString(SessionKeys.Role);

            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            PrepareExternalAuthView();


            // Show TempData errors
            if (
                TempData["Error"] is string tempError &&
                !string.IsNullOrWhiteSpace(tempError)
            )
            {
                ViewBag.Error = tempError;
            }


            // Handle Google authentication failures
            if (!string.IsNullOrWhiteSpace(googleError))
            {
                string normalizedError =
                    googleError.ToLowerInvariant();


                if (
                    normalizedError.Contains("timeout") ||
                    normalizedError.Contains("timed out") ||
                    normalizedError.Contains("task was canceled") ||
                    normalizedError.Contains("task was cancelled")
                )
                {
                    ViewBag.Error =
                        "Google sign-in timed out while connecting to Google's authentication service. Please try again.";
                }
                else if (
                    normalizedError.Contains("invalid_client") ||
                    normalizedError.Contains("client secret")
                )
                {
                    ViewBag.Error =
                        "Google authentication configuration is invalid. Please verify the OAuth Client ID and Client Secret.";
                }
                else if (
                    normalizedError.Contains("access_denied") ||
                    normalizedError.Contains("access denied")
                )
                {
                    ViewBag.Error =
                        "Google sign-in was cancelled or access was denied.";
                }
                else if (
                    normalizedError.Contains("correlation")
                )
                {
                    ViewBag.Error =
                        "The Google sign-in session expired. Please try again.";
                }
                else
                {
                    ViewBag.Error =
                        "Google sign-in could not be completed. Please try again.";
                }
            }


            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            PrepareExternalAuthView();

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            string cleanUsername =
                model.Username.Trim();


            var user =
                await _context.AppUsers
                    .FirstOrDefaultAsync(
                        user =>
                            user.Username == cleanUsername &&
                            user.IsActive
                    );


            if (user == null)
            {
                await _auditLogService.LogAsync(
                    "Failed Login",
                    "AppUsers",
                    null,
                    $"Failed login attempt for username: {cleanUsername}"
                );

                ViewBag.Error =
                    "Invalid username or password";

                return View(model);
            }


            bool passwordValid =
                PasswordHasher.VerifyPassword(
                    model.Password,
                    user.PasswordHash
                );


            if (!passwordValid)
            {
                await _auditLogService.LogAsync(
                    "Failed Login",
                    "AppUsers",
                    user.Id,
                    $"Invalid password attempt for username: {user.Username}"
                );

                ViewBag.Error =
                    "Invalid username or password";

                return View(model);
            }


            SetSession(user);


            await _auditLogService.LogAsync(
                "Login",
                "AppUsers",
                user.Id,
                $"{user.FullName} logged in as {user.Role}"
            );


            return RedirectToAction(
                "Index",
                "Dashboard"
            );
        }


        // =========================================================
        // CUSTOMER REGISTRATION
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            string? role =
                HttpContext.Session.GetString(SessionKeys.Role);

            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }


            PrepareExternalAuthView();


            if (
                TempData["Error"] is string error &&
                !string.IsNullOrWhiteSpace(error)
            )
            {
                ViewBag.Error = error;
            }


            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterCustomerViewModel model)
        {
            PrepareExternalAuthView();


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            string cleanFullName =
                model.FullName.Trim();

            string cleanUsername =
                model.Username.Trim();

            string cleanEmail =
                model.Email.Trim().ToLowerInvariant();


            bool usernameExists =
                await _context.AppUsers
                    .AnyAsync(
                        user =>
                            user.Username == cleanUsername
                    );


            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Username is already taken"
                );

                return View(model);
            }


            bool emailExists =
                await _context.AppUsers
                    .AnyAsync(
                        user =>
                            user.Email == cleanEmail
                    );


            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is already registered"
                );

                return View(model);
            }


            var customer = new AppUser
            {
                FullName = cleanFullName,

                Username = cleanUsername,

                Email = cleanEmail,

                PasswordHash =
                    PasswordHasher.HashPassword(
                        model.Password
                    ),

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


            TempData["Success"] =
                "Customer account created successfully. Please sign in.";


            return RedirectToAction(
                nameof(Login)
            );
        }


        // =========================================================
        // GOOGLE LOGIN / REGISTRATION
        // =========================================================

        [HttpGet]
        public IActionResult GoogleLogin(
            string? returnUrl = null,
            string mode = "login")
        {
            if (!IsGoogleConfigured())
            {
                TempData["Error"] =
                    "Google sign-in is not configured yet. Please configure the OAuth Client ID and Client Secret.";

                return RedirectToAction(
                    mode.Equals(
                        "register",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? nameof(Register)
                        : nameof(Login)
                );
            }


            string callbackUrl =
                Url.Action(
                    nameof(GoogleCallback),
                    "Account",
                    new
                    {
                        returnUrl
                    },
                    Request.Scheme
                )
                ?? "/Account/GoogleCallback";


            var properties =
                new AuthenticationProperties
                {
                    RedirectUri = callbackUrl
                };


            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme
            );
        }


        [HttpGet]
        public async Task<IActionResult> GoogleCallback(
            string? returnUrl = null)
        {
            if (!IsGoogleConfigured())
            {
                TempData["Error"] =
                    "Google OAuth is not configured yet.";

                return RedirectToAction(
                    nameof(Login)
                );
            }


            var authenticationResult =
                await HttpContext.AuthenticateAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );


            if (
                !authenticationResult.Succeeded ||
                authenticationResult.Principal == null
            )
            {
                TempData["Error"] =
                    "Google authentication was not completed. Please try again.";

                return RedirectToAction(
                    nameof(Login)
                );
            }


            ClaimsPrincipal principal =
                authenticationResult.Principal;


            string? email =
                principal.FindFirstValue(
                    ClaimTypes.Email
                )
                ??
                principal.FindFirstValue(
                    "email"
                );


            string? fullName =
                principal.FindFirstValue(
                    ClaimTypes.Name
                )
                ??
                principal.FindFirstValue(
                    "name"
                );


            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );


                TempData["Error"] =
                    "Your Google account did not provide an email address.";


                return RedirectToAction(
                    nameof(Login)
                );
            }


            email =
                email.Trim().ToLowerInvariant();


            var user =
                await _context.AppUsers
                    .FirstOrDefaultAsync(
                        appUser =>
                            appUser.Email == email
                    );


            bool newCustomerCreated = false;


            // Google sign-in is limited to Customer accounts.
            // Admin, SuperAdmin and User accounts must use
            // username and password authentication.
            if (
                user != null &&
                !string.Equals(
                    user.Role,
                    "Customer",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );


                await _auditLogService.LogAsync(
                    "Blocked Google Login",
                    "AppUsers",
                    user.Id,
                    $"Google sign-in blocked for privileged account {user.Username}"
                );


                TempData["Error"] =
                    "Google sign-in is available only for customer accounts. Staff must use the secure username and password login.";


                return RedirectToAction(
                    nameof(Login)
                );
            }


            // Automatically register a new Customer account
            // when the Google email does not already exist.
            if (user == null)
            {
                string generatedUsername =
                    await BuildUniqueUsernameAsync(
                        email
                    );


                user = new AppUser
                {
                    FullName =
                        string.IsNullOrWhiteSpace(fullName)
                            ? generatedUsername
                            : fullName.Trim(),

                    Username =
                        generatedUsername,

                    Email =
                        email,

                    // Google-created users do not know this password.
                    // A secure random password hash is generated.
                    PasswordHash =
                        PasswordHasher.HashPassword(
                            Guid.NewGuid().ToString("N")
                            +
                            Guid.NewGuid().ToString("N")
                        ),

                    Role =
                        "Customer",

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.Now
                };


                _context.AppUsers.Add(user);

                await _context.SaveChangesAsync();


                newCustomerCreated = true;


                await _auditLogService.LogAsync(
                    "Google Registration",
                    "AppUsers",
                    user.Id,
                    $"Customer account created with Google: {user.Email}"
                );
            }


            if (!user.IsActive)
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );


                TempData["Error"] =
                    "This customer account is inactive. Please contact an administrator.";


                return RedirectToAction(
                    nameof(Login)
                );
            }


            SetSession(user);


            await _auditLogService.LogAsync(
                "Google Login",
                "AppUsers",
                user.Id,
                $"{user.FullName} signed in with Google"
            );


            // External cookie is no longer needed because
            // SRMSS now uses its own session.
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );


            if (newCustomerCreated)
            {
                TempData["Success"] =
                    "Your SRMSS customer account was created with Google successfully.";
            }


            if (
                !string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl)
            )
            {
                return LocalRedirect(returnUrl);
            }


            return RedirectToAction(
                "Index",
                "Dashboard"
            );
        }


        // =========================================================
        // PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    SessionKeys.UserId
                );


            if (userId == null)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            var user =
                await _context.AppUsers
                    .FindAsync(userId.Value);


            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    nameof(Login)
                );
            }


            var model =
                new ProfileViewModel
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
        public async Task<IActionResult> Profile(
            ProfileViewModel model)
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    SessionKeys.UserId
                );


            if (userId == null)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            var user =
                await _context.AppUsers
                    .FindAsync(userId.Value);


            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    nameof(Login)
                );
            }


            string cleanFullName =
                model.FullName.Trim();

            string cleanUsername =
                model.Username.Trim();

            string cleanEmail =
                model.Email.Trim().ToLowerInvariant();


            bool usernameExists =
                await _context.AppUsers
                    .AnyAsync(
                        otherUser =>
                            otherUser.Username == cleanUsername &&
                            otherUser.Id != user.Id
                    );


            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Username is already taken"
                );
            }


            bool emailExists =
                await _context.AppUsers
                    .AnyAsync(
                        otherUser =>
                            otherUser.Email == cleanEmail &&
                            otherUser.Id != user.Id
                    );


            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is already registered"
                );
            }


            if (!ModelState.IsValid)
            {
                model.Role = user.Role;

                return View(model);
            }


            user.FullName =
                cleanFullName;

            user.Username =
                cleanUsername;

            user.Email =
                cleanEmail;


            await _context.SaveChangesAsync();


            HttpContext.Session.SetString(
                SessionKeys.FullName,
                user.FullName
            );

            HttpContext.Session.SetString(
                SessionKeys.Username,
                user.Username
            );


            await _auditLogService.LogAsync(
                "Update Profile",
                "AppUsers",
                user.Id,
                $"{user.Username} updated their profile details"
            );


            TempData["Success"] =
                "Profile updated successfully.";


            return RedirectToAction(
                nameof(Profile)
            );
        }


        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    SessionKeys.UserId
                );


            if (userId == null)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            return View(
                new ChangePasswordViewModel()
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    SessionKeys.UserId
                );


            if (userId == null)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _context.AppUsers
                    .FindAsync(userId.Value);


            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    nameof(Login)
                );
            }


            bool currentPasswordValid =
                PasswordHasher.VerifyPassword(
                    model.CurrentPassword,
                    user.PasswordHash
                );


            if (!currentPasswordValid)
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Current password is incorrect"
                );

                return View(model);
            }


            if (
                model.CurrentPassword ==
                model.NewPassword
            )
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "New password must be different from current password"
                );

                return View(model);
            }


            user.PasswordHash =
                PasswordHasher.HashPassword(
                    model.NewPassword
                );


            await _context.SaveChangesAsync();


            await _auditLogService.LogAsync(
                "Change Password",
                "AppUsers",
                user.Id,
                $"{user.Username} changed their account password"
            );


            TempData["Success"] =
                "Password changed successfully.";


            return RedirectToAction(
                nameof(Profile)
            );
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            string? username =
                HttpContext.Session.GetString(
                    SessionKeys.Username
                );


            int? userId =
                HttpContext.Session.GetInt32(
                    SessionKeys.UserId
                );


            if (
                userId.HasValue ||
                !string.IsNullOrWhiteSpace(username)
            )
            {
                await _auditLogService.LogAsync(
                    "Logout",
                    "AppUsers",
                    userId,
                    $"{username ?? "Unknown user"} logged out"
                );
            }


            HttpContext.Session.Clear();


            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );


            return RedirectToAction(
                nameof(Login)
            );
        }


        // =========================================================
        // ACCESS DENIED
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AccessDenied()
        {
            string? username =
                HttpContext.Session.GetString(
                    SessionKeys.Username
                );

            string? role =
                HttpContext.Session.GetString(
                    SessionKeys.Role
                );


            await _auditLogService.LogAsync(
                "Access Denied",
                "System",
                null,
                $"Access denied for user {username ?? "Unknown"} with role {role ?? "Unknown"}"
            );


            return View();
        }


        // =========================================================
        // PRIVATE HELPERS
        // =========================================================

        private bool IsGoogleConfigured()
        {
            string? clientId =
                _configuration[
                    "Authentication:Google:ClientId"
                ];

            string? clientSecret =
                _configuration[
                    "Authentication:Google:ClientSecret"
                ];


            return
                !string.IsNullOrWhiteSpace(clientId)
                &&
                !string.IsNullOrWhiteSpace(clientSecret);
        }


        private void PrepareExternalAuthView()
        {
            ViewBag.GoogleEnabled =
                IsGoogleConfigured();
        }


        private void SetSession(
            AppUser user)
        {
            HttpContext.Session.SetInt32(
                SessionKeys.UserId,
                user.Id
            );

            HttpContext.Session.SetString(
                SessionKeys.FullName,
                user.FullName
            );

            HttpContext.Session.SetString(
                SessionKeys.Username,
                user.Username
            );

            HttpContext.Session.SetString(
                SessionKeys.Role,
                user.Role
            );
        }


        private async Task<string> BuildUniqueUsernameAsync(
            string email)
        {
            string emailPrefix =
                email
                    .Split('@')[0]
                    .ToLowerInvariant();


            string safePrefix =
                new string(
                    emailPrefix
                        .Where(
                            character =>
                                char.IsLetterOrDigit(character)
                                ||
                                character == '.'
                                ||
                                character == '_'
                        )
                        .ToArray()
                );


            if (string.IsNullOrWhiteSpace(safePrefix))
            {
                safePrefix = "customer";
            }


            if (safePrefix.Length > 40)
            {
                safePrefix =
                    safePrefix[..40];
            }


            string candidate =
                safePrefix;

            int suffix = 1;


            while (
                await _context.AppUsers
                    .AnyAsync(
                        user =>
                            user.Username == candidate
                    )
            )
            {
                string suffixText =
                    suffix.ToString();


                int maxPrefixLength =
                    50 - suffixText.Length;


                string trimmedPrefix =
                    safePrefix.Length > maxPrefixLength
                        ? safePrefix[..maxPrefixLength]
                        : safePrefix;


                candidate =
                    trimmedPrefix + suffixText;


                suffix++;
            }


            return candidate;
        }
    }
}