using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NEXUS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdvanceAppliedBackfill : Migration
    {
        /**
         * AdvanceAdjustedOn used to be the flag that said the advance had been
         * taken in full. Those rows must start out already spent, otherwise the
         * new remaining-advance rule would credit the same money a second time.
         */
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE ConnectionOrders SET AdvanceApplied = QuotedAmount " +
                "WHERE AdvanceAdjustedOn IS NOT NULL AND AdvanceApplied = 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE ConnectionOrders SET AdvanceApplied = 0 " +
                "WHERE AdvanceAdjustedOn IS NOT NULL;");
        }
    }
}
