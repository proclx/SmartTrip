using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripIsFavorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Trips_TripId1",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_TripId1",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "TripId1",
                table: "Photos");

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Trips",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Trips");

            migrationBuilder.AddColumn<int>(
                name: "TripId1",
                table: "Photos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_TripId1",
                table: "Photos",
                column: "TripId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Trips_TripId1",
                table: "Photos",
                column: "TripId1",
                principalTable: "Trips",
                principalColumn: "Id");
        }
    }
}
