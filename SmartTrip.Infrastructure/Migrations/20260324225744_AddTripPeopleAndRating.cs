using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrip.Migrations
{
    /// <inheritdoc />
    public partial class AddTripPeopleAndRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeopleCount",
                table: "Trips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Trips",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeopleCount",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Trips");
        }
    }
}
