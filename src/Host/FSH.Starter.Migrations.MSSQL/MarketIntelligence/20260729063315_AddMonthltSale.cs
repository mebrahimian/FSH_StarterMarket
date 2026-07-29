using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddMonthltSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlySales",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodEndDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    YearEndDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MonthlySalesAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    YearToDateSalesAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DisclosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TracingNo = table.Column<long>(type: "bigint", nullable: true),
                    ParsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlySales", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySales_Symbol_PeriodEndDate",
                schema: "marketintelligence",
                table: "MonthlySales",
                columns: new[] { "Symbol", "PeriodEndDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlySales",
                schema: "marketintelligence");
        }
    }
}
