namespace SRMSS.Web.ViewModels
{
    public class ReportsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public int TotalRoutes { get; set; }
        public int ActiveRoutes { get; set; }
        public int TotalSchedules { get; set; }
        public int CompletedTrips { get; set; }
        public int DelayedTrips { get; set; }
        public int CancelledTrips { get; set; }
        public int TotalDrivers { get; set; }
        public int AvailableDrivers { get; set; }
        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public decimal FuelCost { get; set; }
        public decimal FuelLitres { get; set; }
        public decimal AverageEfficiency { get; set; }
        public decimal MaintenanceCost { get; set; }
        public int OpenFeedback { get; set; }
        public int PublishedAnnouncements { get; set; }

        public List<RoutePerformanceRow> RoutePerformance { get; set; } = new();
        public List<VehicleCostRow> VehicleCost { get; set; } = new();
        public List<StatusBreakdownRow> StatusBreakdown { get; set; } = new();
    }

    public class RoutePerformanceRow
    {
        public string RouteName { get; set; } = string.Empty;
        public int TotalTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int DelayedTrips { get; set; }
        public int CancelledTrips { get; set; }
    }

    public class VehicleCostRow
    {
        public string Vehicle { get; set; } = string.Empty;
        public decimal FuelCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal TotalCost => FuelCost + MaintenanceCost;
    }

    public class StatusBreakdownRow
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
