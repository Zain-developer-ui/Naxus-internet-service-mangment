using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NEXUS.Data.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentPreferredVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredVendorId",
                table: "EquipmentProducts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentProducts_PreferredVendorId",
                table: "EquipmentProducts",
                column: "PreferredVendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentProducts_Vendors_PreferredVendorId",
                table: "EquipmentProducts",
                column: "PreferredVendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentProducts_Vendors_PreferredVendorId",
                table: "EquipmentProducts");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentProducts_PreferredVendorId",
                table: "EquipmentProducts");

            migrationBuilder.DropColumn(
                name: "PreferredVendorId",
                table: "EquipmentProducts");
        }
    }
}
