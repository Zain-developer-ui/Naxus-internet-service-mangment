using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NEXUS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Order_PreferredSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredSlot",
                table: "ConnectionOrders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredSlot",
                table: "ConnectionOrders");
        }
    }
}
