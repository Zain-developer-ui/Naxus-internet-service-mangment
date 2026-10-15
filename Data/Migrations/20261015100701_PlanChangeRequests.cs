using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NEXUS.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlanChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CurrentPlanId = table.Column<int>(type: "int", nullable: false),
                    RequestedPlanId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecidedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    DecidedById = table.Column<int>(type: "int", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanChangeRequests_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeRequests_Employees_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanChangeRequests_Plans_CurrentPlanId",
                        column: x => x.CurrentPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeRequests_Plans_RequestedPlanId",
                        column: x => x.RequestedPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeRequests_ConnectionId",
                table: "PlanChangeRequests",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeRequests_CurrentPlanId",
                table: "PlanChangeRequests",
                column: "CurrentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeRequests_CustomerId",
                table: "PlanChangeRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeRequests_DecidedById",
                table: "PlanChangeRequests",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeRequests_RequestedPlanId",
                table: "PlanChangeRequests",
                column: "RequestedPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeRequests_Status",
                table: "PlanChangeRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanChangeRequests");
        }
    }
}
