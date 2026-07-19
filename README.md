# SRMSS - Smart Route Management and Scheduling System

<div align="center">

**A web-based public transport depot management system built with ASP.NET Core MVC, Entity Framework Core and SQL Server.**

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge&logo=dotnet)
![C Sharp](https://img.shields.io/badge/C%23-.NET%208-239120?style=for-the-badge&logo=csharp)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=for-the-badge&logo=microsoftsqlserver)
![Bootstrap](https://img.shields.io/badge/Bootstrap-UI-7952B3?style=for-the-badge&logo=bootstrap)

</div>

---

## Overview

**SRMSS** is a Smart Route Management and Scheduling System developed for public transport depot operations.

The system supports route planning, route stop management, schedule management, driver and vehicle assignment, trip status tracking, customer route discovery, favorite routes, fleet management and reporting.

This project was developed for **Advanced Software Engineering Coursework 01**.


---

## Technology Stack

| Area | Technology |
|---|---|
| Frontend | HTML, CSS, Bootstrap, JavaScript |
| Backend | ASP.NET Core MVC |
| Language | C# |
| Database | SQL Server / LocalDB |
| ORM | Entity Framework Core |
| Maps | Leaflet, OpenStreetMap, Google Maps Links |
| Version Control | Git and GitHub |

---

## System Architecture

SRMSS follows a layered MVC architecture.


<img width="420" height="524" alt="image" src="https://github.com/user-attachments/assets/a55cadc5-469a-49a6-a000-6fa9b59c2b52" />


---

## Main Features

### Route Management

- Add, edit, delete and search transport routes
- Filter routes by service type and status
- View route details
- View online map preview
- Open route in Google Maps

### Route Stops

- Add, edit and delete route stops
- Maintain stops in correct order
- Validate duplicate stop order
- Display complete route flow

### Schedule Management

- Create, edit, delete and search schedules
- Assign route, driver and vehicle
- Validate departure and arrival time
- Detect driver and vehicle schedule conflicts

### Trip Status Board

- View all trips in one operational board
- Update trip status:
  - Scheduled
  - Departed
  - Delayed
  - Completed
  - Cancelled

### Customer Route Discovery

- Search routes by From and To
- Search using intermediate stops
- View route details, stops and timetable
- View trip status
- View route map preview
- Save and remove favorite routes

### Fleet and Reports

- Driver management
- Vehicle management
- Fuel logs
- Maintenance logs
- Reports and analytics
- Customer feedback
- Announcements

---

## Team Responsibilities

| Member | Main Responsibility |
|---|---|
| Maleesha | Authentication, dashboards, user management and audit logs |
| Denuawan | Routes, stops, schedules, conflict detection, trip status and customer route discovery |
| Dinethi | Drivers, vehicles, fuel logs, maintenance, reports, feedback and announcements |

---

## Project Structure

```text
SRMSS-ASE-Coursework
│
├── docs
│   └── screenshots
│
├── src
│   └── SRMSS.Web
│       ├── Controllers
│       ├── Data
│       ├── Migrations
│       ├── Models
│       ├── Views
│       ├── wwwroot
│       ├── appsettings.json
│       └── Program.cs
│
├── reports
├── presentation
├── SRMSS.sln
└── README.md
```

---

## Database Entities

Main database tables include:

- AppUsers
- TransportRoutes
- RouteStops
- Schedules
- Drivers
- Vehicles
- FavoriteRoutes
- FuelLogs
- MaintenanceLogs
- CustomerFeedbacks
- Announcements
- AuditLogs

---

## Setup Instructions

### 1. Clone the Repository

```powershell
git clone https://github.com/MaleeshaDewmina/SRMSS-ASE-Coursework.git
cd SRMSS-ASE-Coursework
```

### 2. Restore Packages

```powershell
dotnet restore
```

### 3. Configure Database

Open:

```text
src/SRMSS.Web/appsettings.json
```

For LocalDB:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SRMSS_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

For SQL Server Express:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=SRMSS_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Apply Database Migrations

```powershell
dotnet ef database update --project src/SRMSS.Web/SRMSS.Web.csproj --startup-project src/SRMSS.Web/SRMSS.Web.csproj
```

### 5. Run the Application

```powershell
dotnet run --project src/SRMSS.Web/SRMSS.Web.csproj
```

Open:

```text
http://localhost:5008
```
---

## Example Test Route

```text
Route Name: Mathugama to Kandy Express
Start Point: Mathugama
End Point: Kandy
Distance: 150 km
Duration: 270 minutes
Service Type: Express
Status: Active
```

Route flow:

```text
Mathugama → Kalutara → Horana → Avissawella → Kegalle → Peradeniya → Kandy
```

---

## Screenshots

### Login

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/cb890cf9-78b8-49c3-80f7-07cab80e41f6" />


### Dashboard

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/d474ee54-d353-4c47-89cf-4662fbaa7bc0" />


### Route Management

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/e67b3f71-5dc7-479b-ad44-6350d26766fb" />


### Route Stops Management

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/9fabddb0-3386-4f75-b98e-57dac836a281" />


### Schedule Management

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/5eb23a24-c447-4df1-83f6-54ba6d5b2236" />


### Trip Status Board

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/e01e343f-f37f-4cfc-91a4-3480d0ca6bc4" />


### Driver Management

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/fff89f1d-7f86-41e3-a97c-57750a4da7c2" />


### Vehicle Management

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/41dcbb4e-1400-48d8-be22-ddd68907e139" />


### Reports and Analytics

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/4ee8c42a-10bb-4e10-9db2-6f62345159d4" />

---

## Branches

```text
main
├── maleesha-auth-dashboard
├── denuawan-routes-schedules
└── dinethi-drivers-vehicles-reports
```

---

## Academic Purpose

This project was developed for academic coursework. It demonstrates MVC architecture, database design, Entity Framework Core migrations, team-based Git workflow, implementation, testing and documentation.

---

<div align="center">

**SRMSS - Smart Route Management and Scheduling System**

Built with ASP.NET Core MVC, Entity Framework Core and SQL Server.

</div>
