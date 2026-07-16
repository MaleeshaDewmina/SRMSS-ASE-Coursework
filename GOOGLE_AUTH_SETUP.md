# SRMSS Google Sign-In Setup

The project includes real Google OAuth login and registration for **Customer** accounts.

For security, Google sign-in is intentionally blocked from authenticating privileged `SuperAdmin`, `Admin`, and `User` accounts. Staff continue to use the normal secure username and password login.

## 1. Create a Google OAuth web application

In Google Cloud Console:

1. Create or select a project.
2. Configure the OAuth consent screen.
3. Create an OAuth 2.0 Client ID with application type **Web application**.
4. Add this authorized redirect URI for local development:

```text
http://localhost:5008/signin-google
```

When running with the HTTPS launch profile, also add:

```text
https://localhost:7165/signin-google
```

## 2. Store credentials safely with .NET user secrets

From `src/SRMSS.Web` run:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

Do not commit real OAuth secrets to GitHub.

## 3. Run the application

```powershell
dotnet restore
dotnet build
dotnet run
```

Open:

```text
http://localhost:5008/Account/Login
```

Choose **Continue with Google**.

## Behaviour

- Existing customer email found: signs in to that customer account.
- New Google email: automatically creates a new `Customer` account and signs in.
- Inactive customer: login is blocked.
- Existing privileged staff email: Google login is blocked to prevent role escalation.
- Google login and Google registration are recorded in `AuditLogs`.
