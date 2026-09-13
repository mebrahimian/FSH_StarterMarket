using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddDisclosureCompanyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disclosures_CompanyId",
                schema: "marketintelligence",
                table: "Disclosures",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Disclosures_CompanyId",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "marketintelligence",
                table: "Disclosures");
        }
    }
}
