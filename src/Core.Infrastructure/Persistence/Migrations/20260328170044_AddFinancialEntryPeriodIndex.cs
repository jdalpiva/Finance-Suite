using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMEFinanceSuite.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialEntryPeriodIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_financial_entries_EntryType_OccurredOn",
                table: "financial_entries",
                columns: new[] { "EntryType", "OccurredOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_financial_entries_EntryType_OccurredOn",
                table: "financial_entries");
        }
    }
}
