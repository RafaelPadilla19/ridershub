using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RidersHub.Migrations
{
    /// <inheritdoc />
    public partial class JobPickupCoords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PickupLat",
                table: "Jobs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLng",
                table: "Jobs",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupLat",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PickupLng",
                table: "Jobs");
        }
    }
}
