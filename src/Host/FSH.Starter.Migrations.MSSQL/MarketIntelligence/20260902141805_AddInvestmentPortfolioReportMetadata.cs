using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddInvestmentPortfolioReportMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuditStatus",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "InvestmentPortfolioReportMetadata",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisclosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TracingNo = table.Column<long>(type: "bigint", nullable: false),
                    PeriodEndToDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    YearEndToDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Period = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SheetCode = table.Column<int>(type: "int", nullable: false),
                    MetaTableId = table.Column<int>(type: "int", nullable: false),
                    MetaTableCode = table.Column<int>(type: "int", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TitleEn = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SourceType = table.Column<int>(type: "tinyint", nullable: false),
                    AuditStatus = table.Column<int>(type: "tinyint", nullable: false),
                    ParsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentPortfolioReportMetadata", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioReportMetadata_DisclosureId",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                column: "DisclosureId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioReportMetadata_TracingNo",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                column: "TracingNo");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioReportMetadata_TracingNo_SheetCode_MetaTableId_MetaTableCode",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                columns: new[] { "TracingNo", "SheetCode", "MetaTableId", "MetaTableCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentPortfolioReportMetadata",
                schema: "marketintelligence");

            migrationBuilder.DropColumn(
                name: "AuditStatus",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions");
        }
    }
}
