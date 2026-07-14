using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Route")]
        public int TransportRouteId { get; set; }

        [Required]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Required]
        [Display(Name = "Driver")]
        public int DriverId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Schedule Date")]
        public DateTime ScheduleDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Departure Time")]
        public TimeSpan DepartureTime { get; set; }

        [Required]
        [Display(Name = "Arrival Time")]
        public TimeSpan ArrivalTime { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Trip Status")]
        public string Status { get; set; } = "Scheduled";

        [MaxLength(250)]
        public string? Notes { get; set; }

        public TransportRoute? TransportRoute { get; set; }

        public Vehicle? Vehicle { get; set; }

        public Driver? Driver { get; set; }
    }
}