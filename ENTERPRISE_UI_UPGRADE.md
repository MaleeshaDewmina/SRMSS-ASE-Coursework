# SRMSS Enterprise UI and Google Authentication Upgrade

## Included in this build

- Modern split-screen login page with transport artwork and generated SRMSS logo.
- Modern customer registration page using the same enterprise visual language.
- Real Google OAuth login and automatic customer registration flow.
- Google sign-in restricted to Customer accounts to prevent staff role escalation.
- Google login and Google registration audit log events.
- Generated SRMSS logo assets added to login, register, favicon and application sidebar.
- Replaced dashboard letter badges with contextual SVG icons.
- Replaced sidebar letter badges with contextual SVG icons.
- Improved topbar notification, theme, logout and mobile menu controls with SVG icons.
- Existing animated dashboard charts, count-up cards and activity timeline retained.
- Added explicit conventional routes for Routes, Route Stops, Schedules, Trip Status and Customer Routes.
- Added Google OAuth setup guide in `GOOGLE_AUTH_SETUP.md`.

## Main changed files

- `src/SRMSS.Web/Program.cs`
- `src/SRMSS.Web/SRMSS.Web.csproj`
- `src/SRMSS.Web/Controllers/AccountController.cs`
- `src/SRMSS.Web/Views/Account/Login.cshtml`
- `src/SRMSS.Web/Views/Account/Register.cshtml`
- `src/SRMSS.Web/Views/Dashboard/Index.cshtml`
- `src/SRMSS.Web/Views/Shared/_Layout.cshtml`
- `src/SRMSS.Web/Views/Shared/_AppIcon.cshtml`
- `src/SRMSS.Web/wwwroot/css/site.css`
- `src/SRMSS.Web/appsettings.Development.json`
- `src/SRMSS.Web/wwwroot/images/*`

## Build requirement

The project now references:

```text
Microsoft.AspNetCore.Authentication.Google 8.0.0
```

Run:

```powershell
dotnet restore
dotnet build
dotnet run
```

Google sign-in remains gracefully unavailable until OAuth credentials are configured. The normal username/password flow continues to work without Google credentials.
