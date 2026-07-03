using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        public int TransportRouteId { get; set; }

        public int VehicleId { get; set; }

        public int DriverId { get; set; }

        public DateTime ScheduleDate { get; set; }

        public TimeSpan DepartureTime { get; set; }

        public TimeSpan ArrivalTime { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public TransportRoute? TransportRoute { get; set; }

        public Vehicle? Vehicle { get; set; }

        public Driver? Driver { get; set; }
    }
}
