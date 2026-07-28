using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RidersHub.Migrations
{
    /// <inheritdoc />
    public partial class JobProposedFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProposedFee",
                table: "Jobs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedFee",
                table: "Jobs");
        }
    }
}
