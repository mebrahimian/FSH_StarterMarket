using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToPortfolioCompanyAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCompanyAliases_FSortName_IsListed",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                columns: new[] { "FSortName", "IsListed" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PortfolioCompanyAliases_FSortName_IsListed",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases");
        }
    }
}
