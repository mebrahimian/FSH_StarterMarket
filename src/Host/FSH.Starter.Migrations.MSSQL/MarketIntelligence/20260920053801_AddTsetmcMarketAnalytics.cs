using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddTsetmcMarketAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BuyIndividualCount",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BuyIndividualValue",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BuyIndividualVolume",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BuyInstitutionalCount",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BuyInstitutionalValue",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BuyInstitutionalVolume",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IndividualBuyerPower",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionalNetFlow",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RealMoneyFlow",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SellIndividualCount",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SellIndividualValue",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SellIndividualVolume",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SellInstitutionalCount",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SellInstitutionalValue",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SellInstitutionalVolume",
                schema: "marketintelligence",
                table: "DailyPrices",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstrumentShareChanges",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OldShares = table.Column<long>(type: "bigint", nullable: false),
                    NewShares = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentShareChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentShareChanges_TsetmcInstruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "dbo",
                        principalTable: "TsetmcInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentValuationHistory",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    ObservedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Eps = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    PE = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    SectorPE = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    SalesPerShare = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentValuationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentValuationHistory_TsetmcInstruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "dbo",
                        principalTable: "TsetmcInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentShareChanges_InstrumentId_EffectiveDate",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                columns: new[] { "InstrumentId", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentValuationHistory_InstrumentId_ObservedDate",
                schema: "marketintelligence",
                table: "InstrumentValuationHistory",
                columns: new[] { "InstrumentId", "ObservedDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstrumentShareChanges",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "InstrumentValuationHistory",
                schema: "marketintelligence");

            migrationBuilder.DropColumn(
                name: "BuyIndividualCount",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "BuyIndividualValue",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "BuyIndividualVolume",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "BuyInstitutionalCount",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "BuyInstitutionalValue",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "BuyInstitutionalVolume",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "IndividualBuyerPower",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "InstitutionalNetFlow",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "RealMoneyFlow",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "SellIndividualCount",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "SellIndividualValue",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "SellIndividualVolume",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "SellInstitutionalCount",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "SellInstitutionalValue",
                schema: "marketintelligence",
                table: "DailyPrices");

            migrationBuilder.DropColumn(
                name: "SellInstitutionalVolume",
                schema: "marketintelligence",
                table: "DailyPrices");
        }
    }
}
