# SRMSS Integration Fixes

This package combines the authentication/dashboard branch with the route, route-stop, schedule, trip-status, customer route-search and favorite-route work.

## Main fixes applied

- Removed the duplicate navigation problem by keeping one shared role-aware sidebar in `Views/Shared/_Layout.cshtml`.
- Changed the dashboard view to content-only so it no longer renders a second sidebar.
- Added explicit `/Dashboard` attribute routing and mapped attribute-routed controllers in `Program.cs`.
- Replaced hardcoded sidebar user names and roles with values from the active session.
- Added role-specific navigation for SuperAdmin, Admin, User and Customer.
- Protected route, route-stop and schedule management for SuperAdmin/Admin only.
- Protected customer route discovery and favorites for Customer only.
- Made Trip Status available to SuperAdmin/Admin/User, with updates limited to SuperAdmin/Admin.
- Fixed customer favorite ownership so favorites are stored per logged-in customer session ID instead of a shared demo key.
- Added route, stop, schedule, trip-status and favorite-route actions to the audit log.
- Harmonized trip statuses across schedules and trip-status views: Scheduled, On Time, Departed, Delayed, Completed and Cancelled.
- Fixed nullable navigation warnings in trip-status search filters.
- Blocked route deletion while schedules are still assigned to that route.
- Added server-side role allowlists to user creation/editing and prevented self-deactivation/self-role changes.
- Added customer registration success feedback on the login page.
- Removed local development HTTPS redirection warning by applying HTTPS redirection only outside Development.
- Removed fake navigation placeholders and hardcoded Denuwan/Admin identity labels from the integrated UI.

## Static validation completed

- C# delimiter balance checked across all `.cs` files.
- Razor code-block brace balance checked across all `.cshtml` files.
- Internal tag-helper controller/action references checked against controller actions.
- POST actions checked for anti-forgery validation.
- Confirmed only one shared `RenderBody()` call.
- Confirmed only one sidebar/navigation structure remains in the Razor views.
- Confirmed no `href="#"`, `DEMO_CUSTOMER`, hardcoded Denuwan role labels, or `Home/Index` dashboard links remain.

## Local verification required

The sandbox used for this review does not have the .NET SDK installed, so run these commands on the Windows development machine before committing:

```powershell
cd "C:\Users\DELL\OneDrive - esoft.lk\Top Up\Advanced Software Engineering\SRMSS\src\SRMSS.Web"
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

Verify these routes:

- `/Dashboard`
- `/Users`
- `/AuditLogs`
- `/TransportRoutes`
- `/RouteStops`
- `/Schedules`
- `/TripStatus`
- `/CustomerRoutes`
- `/Account/Profile`
- `/Account/ChangePassword`

## Role test matrix

### SuperAdmin
- Dashboard
- Admin/user/customer management
- Routes and route stops
- Schedules
- Trip status view and update
- Audit logs
- Profile and password management

### Admin
- Dashboard
- User/customer management
- Routes and route stops
- Schedules
- Trip status view and update
- No audit-log access

### User
- Dashboard
- Trip-status view only
- No route/schedule CRUD
- No audit-log/user-management access

### Customer
- Dashboard
- Route search
- Route details and timetables
- Favorites isolated to the logged-in customer
- No internal management access
