using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAboutToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Trips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "About",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "About",
                table: "AspNetUsers");
        }
    }
}
