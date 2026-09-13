using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddMonthlyActivitySummaryCompanyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyActivitySummaries_CompanyId_PeriodEndDate",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                columns: new[] { "CompanyId", "PeriodEndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MonthlyActivitySummaries_CompanyId_PeriodEndDate",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries");
        }
    }
}
