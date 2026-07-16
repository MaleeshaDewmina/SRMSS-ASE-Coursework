using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRMSS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TransportRouteId = table.Column<int>(type: "int", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteRoutes_TransportRoutes_TransportRouteId",
                        column: x => x.TransportRouteId,
                        principalTable: "TransportRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteRoutes_TransportRouteId",
                table: "FavoriteRoutes",
                column: "TransportRouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteRoutes");
        }
    }
}
