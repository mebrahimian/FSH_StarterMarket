using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddPortfolioReportHeaderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RegisteredCapital",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                type: "decimal(28,3)",
                precision: 28,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportCompanyName",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportSymbol",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnauthorizedCapital",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                type: "decimal(28,3)",
                precision: 28,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegisteredCapital",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata");

            migrationBuilder.DropColumn(
                name: "ReportCompanyName",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata");

            migrationBuilder.DropColumn(
                name: "ReportSymbol",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata");

            migrationBuilder.DropColumn(
                name: "UnauthorizedCapital",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata");

        }
    }
}
