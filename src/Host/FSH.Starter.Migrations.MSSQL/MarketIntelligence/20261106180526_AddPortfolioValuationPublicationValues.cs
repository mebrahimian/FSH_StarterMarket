using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddPortfolioValuationPublicationValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PortfolioValueAtPublication",
                schema: "marketintelligence",
                table: "PortfolioValuationSnapshots",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SharePriceAtPublication",
                schema: "marketintelligence",
                table: "PortfolioValuationSnapshots",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortfolioValueAtPublication",
                schema: "marketintelligence",
                table: "PortfolioValuationSnapshots");

            migrationBuilder.DropColumn(
                name: "SharePriceAtPublication",
                schema: "marketintelligence",
                table: "PortfolioValuationSnapshots");
        }
    }
}
