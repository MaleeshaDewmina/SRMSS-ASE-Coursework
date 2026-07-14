using SRMSS.Web.Models;

namespace SRMSS.Web.ViewModels
{
    public class DashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public int TotalAdmins { get; set; }
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveAccounts { get; set; }
        public int InactiveAccounts { get; set; }

        public int TotalAuditLogs { get; set; }
        public int TotalRoutes { get; set; }
        public int TotalSchedules { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalVehicles { get; set; }

        public int ActiveTrips { get; set; }
        public int DelayedTrips { get; set; }
        public int CompletedTrips { get; set; }

        public int AvailableRoutes { get; set; }
        public int AvailableSchedules { get; set; }
        public int FavoriteRoutes { get; set; }

        public List<AuditLog> RecentAuditLogs { get; set; } = new();
    }
}
