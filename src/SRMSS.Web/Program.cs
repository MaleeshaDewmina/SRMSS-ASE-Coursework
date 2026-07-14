using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Models;
using SRMSS.Web.Services;
using SRMSS.Web.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogService>();


// =========================================================
// GOOGLE AUTHENTICATION CONFIGURATION
// =========================================================

string? googleClientId =
    builder.Configuration["Authentication:Google:ClientId"];

string? googleClientSecret =
    builder.Configuration["Authentication:Google:ClientSecret"];

bool googleConfigured =
    !string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret);


var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.Cookie.Name = "SRMSS.ExternalAuth";

            options.Cookie.HttpOnly = true;

            options.Cookie.SameSite =
                SameSiteMode.Lax;

            options.ExpireTimeSpan =
                TimeSpan.FromMinutes(10);

            options.SlidingExpiration = false;
        }
    );


if (googleConfigured)
{
    authenticationBuilder.AddGoogle(
        GoogleDefaults.AuthenticationScheme,
        options =>
        {
            options.ClientId = googleClientId!;
            options.ClientSecret = googleClientSecret!;

            options.CallbackPath = "/signin-google";

            options.SaveTokens = false;

            options.BackchannelTimeout =
                TimeSpan.FromSeconds(120);


            // =================================================
            // CUSTOM IPV4 GOOGLE BACKCHANNEL
            //
            // Your network tests showed that Google works over
            // IPv4 while .NET authentication requests were
            // experiencing timeout problems.
            // =================================================

            var socketsHandler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(30),

                PooledConnectionLifetime =
                    TimeSpan.FromMinutes(5),

                PooledConnectionIdleTimeout =
                    TimeSpan.FromMinutes(2),

                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate |
                    DecompressionMethods.Brotli,

                ConnectCallback = async (
                    context,
                    cancellationToken) =>
                {
                    IPAddress[] addresses =
                        await Dns.GetHostAddressesAsync(
                            context.DnsEndPoint.Host,
                            cancellationToken
                        );

                    IPAddress? ipv4Address =
                        addresses.FirstOrDefault(
                            address =>
                                address.AddressFamily ==
                                AddressFamily.InterNetwork
                        );


                    if (ipv4Address == null)
                    {
                        throw new HttpRequestException(
                            $"No IPv4 address was found for " +
                            $"{context.DnsEndPoint.Host}."
                        );
                    }


                    var socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp
                    );


                    try
                    {
                        await socket.ConnectAsync(
                            new IPEndPoint(
                                ipv4Address,
                                context.DnsEndPoint.Port
                            ),
                            cancellationToken
                        );

                        return new NetworkStream(
                            socket,
                            ownsSocket: true
                        );
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };


            options.Backchannel = new HttpClient(
                socketsHandler,
                disposeHandler: true
            )
            {
                Timeout = TimeSpan.FromSeconds(120)
            };


            // =================================================
            // HANDLE GOOGLE AUTHENTICATION FAILURES
            //
            // Prevents the ugly ASP.NET exception page.
            // =================================================

            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();

                string failureMessage =
                    context.Failure?.GetBaseException().Message
                    ??
                    context.Failure?.Message
                    ??
                    "Google authentication failed.";

                string redirectUrl =
                    "/Account/Login?googleError=" +
                    Uri.EscapeDataString(failureMessage);

                context.Response.Redirect(redirectUrl);

                return Task.CompletedTask;
            };
        }
    );
}


// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// DATABASE MIGRATIONS AND DEMO ACCOUNT SEEDING
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    context.Database.Migrate();


    if (!context.AppUsers.Any(
        user => user.Username == "superadmin"))
    {
        context.AppUsers.Add(
            new AppUser
            {
                FullName = "System Super Admin",

                Username = "superadmin",

                Email = "superadmin@srmss.local",

                PasswordHash =
                    PasswordHasher.HashPassword(
                        "admin123"
                    ),

                Role = "SuperAdmin",

                IsActive = true,

                CreatedAt = DateTime.Now
            }
        );
    }


    if (!context.AppUsers.Any(
        user => user.Username == "admin"))
    {
        context.AppUsers.Add(
            new AppUser
            {
                FullName = "Depot Admin",

                Username = "admin",

                Email = "admin@srmss.local",

                PasswordHash =
                    PasswordHasher.HashPassword(
                        "admin123"
                    ),

                Role = "Admin",

                IsActive = true,

                CreatedAt = DateTime.Now
            }
        );
    }


    if (!context.AppUsers.Any(
        user => user.Username == "user"))
    {
        context.AppUsers.Add(
            new AppUser
            {
                FullName = "Depot User",

                Username = "user",

                Email = "user@srmss.local",

                PasswordHash =
                    PasswordHasher.HashPassword(
                        "admin123"
                    ),

                Role = "User",

                IsActive = true,

                CreatedAt = DateTime.Now
            }
        );
    }


    if (!context.AppUsers.Any(
        user => user.Username == "customer"))
    {
        context.AppUsers.Add(
            new AppUser
            {
                FullName = "Demo Customer",

                Username = "customer",

                Email = "customer@srmss.local",

                PasswordHash =
                    PasswordHasher.HashPassword(
                        "admin123"
                    ),

                Role = "Customer",

                IsActive = true,

                CreatedAt = DateTime.Now
            }
        );
    }


    await context.SaveChangesAsync();
}


// =========================================================
// HTTP PIPELINE
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();

    app.UseHttpsRedirection();
}


app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();


// =========================================================
// INTEGRATED MODULE ROUTES
// =========================================================

app.MapControllerRoute(
    name: "transport-routes",
    pattern: "TransportRoutes/{action=Index}/{id?}",
    defaults: new
    {
        controller = "TransportRoutes"
    }
);


app.MapControllerRoute(
    name: "route-stops",
    pattern: "RouteStops/{action=Index}/{id?}",
    defaults: new
    {
        controller = "RouteStops"
    }
);


app.MapControllerRoute(
    name: "schedules",
    pattern: "Schedules/{action=Index}/{id?}",
    defaults: new
    {
        controller = "Schedules"
    }
);


app.MapControllerRoute(
    name: "trip-status",
    pattern: "TripStatus/{action=Index}/{id?}",
    defaults: new
    {
        controller = "TripStatus"
    }
);


app.MapControllerRoute(
    name: "customer-routes",
    pattern: "CustomerRoutes/{action=Index}/{id?}",
    defaults: new
    {
        controller = "CustomerRoutes"
    }
);


// =========================================================
// DEFAULT MVC ROUTE
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Login}/{id?}"
);


// Required for attribute routes such as:
// /Dashboard
// /Users
// /AuditLogs

app.MapControllers();


app.Run();