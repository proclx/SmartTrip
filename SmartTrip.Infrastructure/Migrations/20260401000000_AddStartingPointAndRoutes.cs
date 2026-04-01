using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartingPointAndRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RouteBack",
                table: "Trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteToDestination",
                table: "Trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartingPoint",
                table: "Trips",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RouteBack",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RouteToDestination",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "StartingPoint",
                table: "Trips");
        }
    }
}