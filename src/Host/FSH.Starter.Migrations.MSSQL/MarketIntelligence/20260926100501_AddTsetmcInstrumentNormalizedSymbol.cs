using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddTsetmcInstrumentNormalizedSymbol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedSymbol",
                schema: "dbo",
                table: "TsetmcInstruments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TsetmcInstruments_NormalizedSymbol",
                schema: "dbo",
                table: "TsetmcInstruments",
                column: "NormalizedSymbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TsetmcInstruments_NormalizedSymbol",
                schema: "dbo",
                table: "TsetmcInstruments");

            migrationBuilder.DropColumn(
                name: "NormalizedSymbol",
                schema: "dbo",
                table: "TsetmcInstruments");
        }
    }
}
