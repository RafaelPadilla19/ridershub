using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RidersHub.Migrations
{
    /// <inheritdoc />
    public partial class JobCustomerContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Jobs");
        }
    }
}
