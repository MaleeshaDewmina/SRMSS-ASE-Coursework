using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRMSS.Web.Migrations
{
    /// <inheritdoc />
    public partial class ImproveRouteStopScheduleModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "TransportRoutes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Schedules",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutesFromStart",
                table: "RouteStops",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "TransportRoutes");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutesFromStart",
                table: "RouteStops");
        }
    }
}
