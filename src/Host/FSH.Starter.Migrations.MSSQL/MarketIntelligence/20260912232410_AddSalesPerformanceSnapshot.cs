using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddSalesPerformanceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesPerformanceSnapshots",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodEndDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PreviousYearCurrentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MonthlyYoYGrowthPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RollingThreeMonthAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PreviousYearRollingThreeMonthAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ThreeMonthYoYGrowthPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RollingSixMonthAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PreviousYearRollingSixMonthAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SixMonthYoYGrowthPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    YearToDateAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PreviousYearToDateAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    YearToDateYoYGrowthPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ThreeMonthAverage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SixMonthAverage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TwelveMonthAverage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TwentyFourMonthAverage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SixtyMonthAverage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsThreeMonthAveragePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsSixMonthAveragePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsTwelveMonthAveragePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsTwentyFourMonthAveragePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsSixtyMonthAveragePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ThreeMonthHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ThreeMonthHighPeriod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SixMonthHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SixMonthHighPeriod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TwelveMonthHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TwelveMonthHighPeriod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TwentyFourMonthHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TwentyFourMonthHighPeriod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SixtyMonthHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SixtyMonthHighPeriod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CurrentVsThreeMonthHighPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsSixMonthHighPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsTwelveMonthHighPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsTwentyFourMonthHighPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentVsSixtyMonthHighPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AvailableHistoryMonths = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPerformanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesPerformanceSnapshots_CompanyId_PeriodEndDate",
                schema: "marketintelligence",
                table: "SalesPerformanceSnapshots",
                columns: new[] { "CompanyId", "PeriodEndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesPerformanceSnapshots_PeriodEndDate",
                schema: "marketintelligence",
                table: "SalesPerformanceSnapshots",
                column: "PeriodEndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesPerformanceSnapshots",
                schema: "marketintelligence");
        }
    }
}
