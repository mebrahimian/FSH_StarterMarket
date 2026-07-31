using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddprevytdtoMonthlyActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PreviousYearToDateAmount",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                type: "decimal(18,2)",
                nullable: true,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousYearToDateAmount",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries");
        }
    }
}
