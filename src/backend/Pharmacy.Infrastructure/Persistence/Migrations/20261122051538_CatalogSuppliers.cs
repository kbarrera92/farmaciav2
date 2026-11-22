using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharmacy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Modern_Spanish_100_CI_AI"),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Code",
                keyValue: "catalog.manage",
                column: "Description",
                value: "Crear, editar y activar/desactivar categorías, proveedores y productos del catálogo.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Code",
                keyValue: "catalog.view",
                column: "Description",
                value: "Consultar las categorías, proveedores y productos del catálogo.");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreatedByUserId",
                table: "Suppliers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxId",
                table: "Suppliers",
                column: "TaxId",
                unique: true,
                filter: "[TaxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UpdatedByUserId",
                table: "Suppliers",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Code",
                keyValue: "catalog.manage",
                column: "Description",
                value: "Crear, editar y activar/desactivar categorías del catálogo de productos.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Code",
                keyValue: "catalog.view",
                column: "Description",
                value: "Consultar las categorías del catálogo de productos.");
        }
    }
}
