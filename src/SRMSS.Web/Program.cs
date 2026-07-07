using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Models;
using SRMSS.Web.Utilities;
using SRMSS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    context.Database.Migrate();

    if (!context.AppUsers.Any(u => u.Username == "superadmin"))
    {
        context.AppUsers.Add(new AppUser
        {
            FullName = "System Super Admin",
            Username = "superadmin",
            Email = "superadmin@srmss.local",
            PasswordHash = PasswordHasher.HashPassword("admin123"),
            Role = "SuperAdmin",
            IsActive = true,
            CreatedAt = DateTime.Now
        });

        context.SaveChanges();
    }

    if (!context.AppUsers.Any(u => u.Username == "admin"))
    {
        context.AppUsers.Add(new AppUser
        {
            FullName = "Depot Admin",
            Username = "admin",
            Email = "admin@srmss.local",
            PasswordHash = PasswordHasher.HashPassword("admin123"),
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.Now
        });

        context.SaveChanges();
    }

    if (!context.AppUsers.Any(u => u.Username == "user"))
    {
        context.AppUsers.Add(new AppUser
        {
            FullName = "Depot User",
            Username = "user",
            Email = "user@srmss.local",
            PasswordHash = PasswordHasher.HashPassword("admin123"),
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.Now
        });

        context.SaveChanges();
    }

    if (!context.AppUsers.Any(u => u.Username == "customer"))
    {
        context.AppUsers.Add(new AppUser
        {
            FullName = "Demo Customer",
            Username = "customer",
            Email = "customer@srmss.local",
            PasswordHash = PasswordHasher.HashPassword("admin123"),
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.Now
        });

        context.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();