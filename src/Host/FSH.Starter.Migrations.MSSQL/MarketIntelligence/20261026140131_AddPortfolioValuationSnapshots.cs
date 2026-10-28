using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddPortfolioValuationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioValuationSnapshots",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PeriodEndDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentDisclosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentRegisteredCapital = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CurrentListedValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CurrentUnlistedValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CurrentPortfolioValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CurrentValuePerShare = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CurrentSharePrice = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CurrentPriceTradeDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrentPriceToPortfolioValuePercent = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CurrentListedPositionCount = table.Column<int>(type: "int", nullable: false),
                    CurrentPricedPositionCount = table.Column<int>(type: "int", nullable: false),
                    CurrentValuationAsOfDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrentCalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PreviousDisclosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousPeriodEndDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousRegisteredCapital = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    PreviousListedValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    PreviousUnlistedValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    PreviousPortfolioValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    PreviousValuePerShare = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    PreviousSharePrice = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    PreviousPriceTradeDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousPriceToPortfolioValuePercent = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    PreviousListedPositionCount = table.Column<int>(type: "int", nullable: true),
                    PreviousPricedPositionCount = table.Column<int>(type: "int", nullable: true),
                    PreviousValuationAsOfDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PreviousCalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FrozenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioValuationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioValuationSnapshots_CompanyId_PeriodEndDate",
                schema: "marketintelligence",
                table: "PortfolioValuationSnapshots",
                columns: new[] { "CompanyId", "PeriodEndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioValuationSnapshots_PeriodEndDate",
                schema: "marketintelligence",
                table: "PortfolioValuationSnapshots",
                column: "PeriodEndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioValuationSnapshots",
                schema: "marketintelligence");
        }
    }
}
