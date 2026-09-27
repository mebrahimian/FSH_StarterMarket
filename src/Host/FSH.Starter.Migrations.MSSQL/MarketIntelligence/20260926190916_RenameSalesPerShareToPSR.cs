using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class RenameSalesPerShareToPSR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
             name: "SalesPerShare",
             schema: "marketintelligence",
             table: "InstrumentValuationHistory",
             newName: "PSR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
        name: "PSR",
        schema: "marketintelligence",
        table: "InstrumentValuationHistory",
        newName: "SalesPerShare");
        }
    }
}
