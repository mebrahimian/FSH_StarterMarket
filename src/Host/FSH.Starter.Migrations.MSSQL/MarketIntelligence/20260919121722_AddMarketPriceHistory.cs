using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddMarketPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkAssets",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyPrices",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    TradeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FirstPrice = table.Column<long>(type: "bigint", nullable: false),
                    LowPrice = table.Column<long>(type: "bigint", nullable: false),
                    HighPrice = table.Column<long>(type: "bigint", nullable: false),
                    ClosingPrice = table.Column<long>(type: "bigint", nullable: false),
                    LastPrice = table.Column<long>(type: "bigint", nullable: false),
                    YesterdayPrice = table.Column<long>(type: "bigint", nullable: false),
                    TradeCount = table.Column<long>(type: "bigint", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPrices_TsetmcInstruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "dbo",
                        principalTable: "TsetmcInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkDailyPrices",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BenchmarkAssetId = table.Column<int>(type: "int", nullable: false),
                    TradeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: false),
                    LowPrice = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: false),
                    HighPrice = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: false),
                    ClosePrice = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkDailyPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkDailyPrices_BenchmarkAssets_BenchmarkAssetId",
                        column: x => x.BenchmarkAssetId,
                        principalSchema: "marketintelligence",
                        principalTable: "BenchmarkAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkAssets_Code",
                schema: "marketintelligence",
                table: "BenchmarkAssets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkDailyPrices_BenchmarkAssetId_TradeDate",
                schema: "marketintelligence",
                table: "BenchmarkDailyPrices",
                columns: new[] { "BenchmarkAssetId", "TradeDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPrices_InstrumentId_TradeDate",
                schema: "marketintelligence",
                table: "DailyPrices",
                columns: new[] { "InstrumentId", "TradeDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkDailyPrices",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "DailyPrices",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "BenchmarkAssets",
                schema: "marketintelligence");
        }
    }
}
