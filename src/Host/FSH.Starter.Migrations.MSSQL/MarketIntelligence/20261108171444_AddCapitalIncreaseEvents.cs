using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddCapitalIncreaseEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapitalIncreaseEvents",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    OldShares = table.Column<long>(type: "bigint", nullable: false),
                    NewShares = table.Column<long>(type: "bigint", nullable: false),
                    AdjustmentEffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CodalDisclosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssemblyDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RegistrationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PreviousCapital = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    NewCapital = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    CashIncoming = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    RetainedEarnings = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    Reserves = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    SharePremium = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    RevaluationSurplus = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapitalIncreaseEvents", x => x.Id);
                    table.CheckConstraint("CK_CapitalIncreaseEvents_Shares", "[OldShares] > 0 AND [NewShares] > [OldShares]");
                    table.ForeignKey(
                        name: "FK_CapitalIncreaseEvents_TsetmcInstruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "dbo",
                        principalTable: "TsetmcInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CapitalIncreaseEvents_CodalDisclosureId",
                schema: "marketintelligence",
                table: "CapitalIncreaseEvents",
                column: "CodalDisclosureId",
                unique: true,
                filter: "[CodalDisclosureId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_CapitalIncreaseEvents_Instrument_Shares_Date",
                schema: "marketintelligence",
                table: "CapitalIncreaseEvents",
                columns: new[] { "InstrumentId", "OldShares", "NewShares", "AdjustmentEffectiveDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapitalIncreaseEvents",
                schema: "marketintelligence");
        }
    }
}
