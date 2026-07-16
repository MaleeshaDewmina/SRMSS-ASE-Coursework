# SRMSS Enterprise Fleet and Communication Integration

This package integrates Dinethi's Driver and Vehicle work into the existing SRMSS.Web project and upgrades it into enterprise-level modules that match the current SRMSS dashboard, route, schedule and customer journey UI style.

## Added modules

- Driver Management
- Vehicle Management
- Fuel Log Management
- Maintenance Log Management
- Operational Reports
- Customer Feedback
- Announcements

## Important notes

- The standalone Dinethi project was not copied directly.
- The code was converted into the existing `SRMSS.Web` project structure.
- Existing authentication, role authorization, layout, session handling, audit logs, routes, schedules and customer route modules were preserved.
- A new EF migration was added: `AddEnterpriseFleetCommunicationModules`.

## Recommended test command

```powershell
cd "src/SRMSS.Web"
dotnet clean
dotnet build
dotnet run
```

## Recommended test flow

Test as SuperAdmin/Admin:

- Drivers CRUD
- Vehicles CRUD
- Fuel Logs CRUD
- Maintenance Logs CRUD
- Reports page
- Announcements CRUD
- Customer Feedback response workflow

Test as User:

- View Drivers and Vehicles
- Add/Edit Fuel Logs
- Add/Edit Maintenance Logs
- View Reports
- View and respond to Customer Feedback
- View Announcements
- Ensure admin-only create/edit/delete for Drivers and Vehicles is blocked

Test as Customer:

- View Announcements
- Submit Feedback
- View Feedback response
- Customer Route Search and Favourite Routes

## Commit suggestion

After successful build and testing:

```powershell
git status
git add .
git commit -m "Integrate enterprise fleet reports feedback and announcements modules"
git push origin integration-srmss
git status
```
