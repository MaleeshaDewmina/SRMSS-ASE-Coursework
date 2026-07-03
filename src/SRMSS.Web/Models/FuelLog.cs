namespace SRMSS.Web.Models
{
    public class FuelLog
    {
        public int Id { get; set; }

        public int VehicleId { get; set; }

        public DateTime FuelDate { get; set; }

        public decimal Litres { get; set; }

        public decimal Cost { get; set; }

        public decimal DistanceCoveredKm { get; set; }

        public decimal FuelEfficiencyKmPerLitre { get; set; }

        public int OdometerReading { get; set; }

        public Vehicle? Vehicle { get; set; }
    }
}
