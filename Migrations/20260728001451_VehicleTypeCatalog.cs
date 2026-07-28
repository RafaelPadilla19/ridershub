using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RidersHub.Migrations
{
    /// <inheritdoc />
    public partial class VehicleTypeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "VehicleTypes",
                columns: new[] { "Id", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("9c1b6f1a-0001-4000-8000-000000000001"), true, "Moto", 0 },
                    { new Guid("9c1b6f1a-0001-4000-8000-000000000002"), true, "Carro", 1 },
                    { new Guid("9c1b6f1a-0001-4000-8000-000000000003"), true, "Bicicleta", 2 },
                    { new Guid("9c1b6f1a-0001-4000-8000-000000000004"), true, "A pie", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_Name",
                table: "VehicleTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleTypes");
        }
    }
}
