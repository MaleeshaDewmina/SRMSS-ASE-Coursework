using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Models;

namespace SRMSS.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<TransportRoute> TransportRoutes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<FuelLog> FuelLogs { get; set; }
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique username for login users
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Unique vehicle registration number
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.RegistrationNumber)
                .IsUnique();

            // Unique driver license number
            modelBuilder.Entity<Driver>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique();

            // TransportRoute to RouteStop relationship
            // One route can have many stops
            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.TransportRoute)
                .WithMany(r => r.RouteStops)
                .HasForeignKey(rs => rs.TransportRouteId)
                .OnDelete(DeleteBehavior.Cascade);

            // TransportRoute to Schedule relationship
            // One route can have many schedules
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.TransportRoute)
                .WithMany(r => r.Schedules)
                .HasForeignKey(s => s.TransportRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Driver to Schedule relationship
            // One driver can have many schedules
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Driver)
                .WithMany(d => d.Schedules)
                .HasForeignKey(s => s.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Vehicle to Schedule relationship
            // One vehicle can have many schedules
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Vehicle)
                .WithMany(v => v.Schedules)
                .HasForeignKey(s => s.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision settings
            modelBuilder.Entity<TransportRoute>()
                .Property(r => r.DistanceKm)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Vehicle>()
                .Property(v => v.Mileage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<FuelLog>()
                .Property(f => f.Litres)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<FuelLog>()
                .Property(f => f.Cost)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<FuelLog>()
                .Property(f => f.DistanceCoveredKm)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<FuelLog>()
                .Property(f => f.FuelEfficiencyKmPerLitre)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MaintenanceLog>()
                .Property(m => m.Cost)
                .HasColumnType("decimal(18,2)");
        }
    }
}