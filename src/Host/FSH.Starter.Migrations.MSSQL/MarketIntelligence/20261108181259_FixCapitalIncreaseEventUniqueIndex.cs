using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class FixCapitalIncreaseEventUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CapitalIncreaseEvents_Instrument_Shares_Date",
                schema: "marketintelligence",
                table: "CapitalIncreaseEvents");

            migrationBuilder.CreateIndex(
                name: "UX_CapitalIncreaseEvents_Instrument_Shares",
                schema: "marketintelligence",
                table: "CapitalIncreaseEvents",
                columns: new[] { "InstrumentId", "OldShares", "NewShares" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CapitalIncreaseEvents_Instrument_Shares",
                schema: "marketintelligence",
                table: "CapitalIncreaseEvents");

            migrationBuilder.CreateIndex(
                name: "UX_CapitalIncreaseEvents_Instrument_Shares_Date",
                schema: "marketintelligence",
                table: "CapitalIncreaseEvents",
                columns: new[] { "InstrumentId", "OldShares", "NewShares", "AdjustmentEffectiveDate" },
                unique: true);
        }
    }
}
