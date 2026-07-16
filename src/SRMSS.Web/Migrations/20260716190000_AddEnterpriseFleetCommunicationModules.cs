using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRMSS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseFleetCommunicationModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Drivers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Drivers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "Drivers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedDepot",
                table: "Drivers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Central Depot");

            migrationBuilder.AddColumn<string>(
                name: "ShiftType",
                table: "Drivers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Day");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "Drivers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "Drivers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Drivers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Drivers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Drivers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Drivers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Vehicles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Vehicles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "Vehicles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Diesel");

            migrationBuilder.AddColumn<string>(
                name: "ChassisNumber",
                table: "Vehicles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InsuranceExpiryDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevenueLicenseExpiryDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastServiceDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NextServiceKm",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedDepot",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Central Depot");

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceStatus",
                table: "Vehicles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Good");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Vehicles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelStation",
                table: "FuelLogs",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "FuelLogs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FuelLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FuelLogs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<int>(
                name: "OdometerReading",
                table: "MaintenanceLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedBy",
                table: "MaintenanceLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MaintenanceLogs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MaintenanceLogs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PublishDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: true),
                    TransportRouteId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Response = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerFeedbacks_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerFeedbacks_TransportRoutes_TransportRouteId",
                        column: x => x.TransportRouteId,
                        principalTable: "TransportRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Audience",
                table: "Announcements",
                column: "Audience");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Status",
                table: "Announcements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_AppUserId",
                table: "CustomerFeedbacks",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_CustomerKey",
                table: "CustomerFeedbacks",
                column: "CustomerKey");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_Status",
                table: "CustomerFeedbacks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_TransportRouteId",
                table: "CustomerFeedbacks",
                column: "TransportRouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Announcements");
            migrationBuilder.DropTable(name: "CustomerFeedbacks");

            migrationBuilder.DropColumn(name: "Email", table: "Drivers");
            migrationBuilder.DropColumn(name: "Address", table: "Drivers");
            migrationBuilder.DropColumn(name: "EmployeeNumber", table: "Drivers");
            migrationBuilder.DropColumn(name: "AssignedDepot", table: "Drivers");
            migrationBuilder.DropColumn(name: "ShiftType", table: "Drivers");
            migrationBuilder.DropColumn(name: "EmergencyContactName", table: "Drivers");
            migrationBuilder.DropColumn(name: "EmergencyContactPhone", table: "Drivers");
            migrationBuilder.DropColumn(name: "HireDate", table: "Drivers");
            migrationBuilder.DropColumn(name: "IsActive", table: "Drivers");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Drivers");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Drivers");

            migrationBuilder.DropColumn(name: "Model", table: "Vehicles");
            migrationBuilder.DropColumn(name: "Manufacturer", table: "Vehicles");
            migrationBuilder.DropColumn(name: "FuelType", table: "Vehicles");
            migrationBuilder.DropColumn(name: "ChassisNumber", table: "Vehicles");
            migrationBuilder.DropColumn(name: "InsuranceExpiryDate", table: "Vehicles");
            migrationBuilder.DropColumn(name: "RevenueLicenseExpiryDate", table: "Vehicles");
            migrationBuilder.DropColumn(name: "LastServiceDate", table: "Vehicles");
            migrationBuilder.DropColumn(name: "NextServiceKm", table: "Vehicles");
            migrationBuilder.DropColumn(name: "AssignedDepot", table: "Vehicles");
            migrationBuilder.DropColumn(name: "MaintenanceStatus", table: "Vehicles");
            migrationBuilder.DropColumn(name: "IsActive", table: "Vehicles");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Vehicles");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Vehicles");

            migrationBuilder.DropColumn(name: "FuelStation", table: "FuelLogs");
            migrationBuilder.DropColumn(name: "ReceiptNumber", table: "FuelLogs");
            migrationBuilder.DropColumn(name: "Notes", table: "FuelLogs");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "FuelLogs");

            migrationBuilder.DropColumn(name: "OdometerReading", table: "MaintenanceLogs");
            migrationBuilder.DropColumn(name: "PerformedBy", table: "MaintenanceLogs");
            migrationBuilder.DropColumn(name: "Status", table: "MaintenanceLogs");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "MaintenanceLogs");
        }
    }
}
