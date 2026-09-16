using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class DeleteIsic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Isic",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Isic",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);
        }
    }
}
