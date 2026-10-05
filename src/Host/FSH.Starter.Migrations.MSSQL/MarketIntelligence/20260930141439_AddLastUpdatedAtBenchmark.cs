using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddLastUpdatedAtBenchmark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "marketintelligence",
                table: "BenchmarkDailyPrices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdatedAt",
                schema: "marketintelligence",
                table: "BenchmarkDailyPrices",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "marketintelligence",
                table: "BenchmarkDailyPrices");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                schema: "marketintelligence",
                table: "BenchmarkDailyPrices");
        }
    }
}
