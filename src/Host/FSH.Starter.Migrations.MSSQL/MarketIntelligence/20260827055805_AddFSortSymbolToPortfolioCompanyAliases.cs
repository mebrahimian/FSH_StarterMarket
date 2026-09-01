using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddFSortSymbolToPortfolioCompanyAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FSortSymbol",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCompanyAliases_FSortSymbol",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                column: "FSortSymbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PortfolioCompanyAliases_FSortSymbol",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases");

            migrationBuilder.DropColumn(
                name: "FSortSymbol",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases");
        }
    }
}
