using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddInvestmentPortfolioMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestmentPortfolioPositions",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCompanyId = table.Column<int>(type: "int", nullable: false),
                    ChildCompanyId = table.Column<int>(type: "int", nullable: true),
                    RawCompanyName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FSortName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PeriodEndDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsListed = table.Column<bool>(type: "bit", nullable: false),
                    RowSequence = table.Column<int>(type: "int", nullable: false),
                    Capital = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    NominalValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    BeginningQuantity = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    BeginningCost = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    BeginningMarketValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    ChangeQuantity = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    ChangeCost = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    ChangeMarketValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    OwnershipPercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    EndingQuantity = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    EndingCost = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    EndingMarketValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    EndingCostPerShare = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    EndingMarketPrice = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    IncreaseDecrease = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DisclosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TracingNo = table.Column<long>(type: "bigint", nullable: false),
                    PublishDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ParsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentPortfolioPositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioCompanyAliases",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AliasName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FSortName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioCompanyAliases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioPositions_ChildCompanyId",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions",
                column: "ChildCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioPositions_DisclosureId",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions",
                column: "DisclosureId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioPositions_FSortName",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions",
                column: "FSortName");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioPositions_ParentCompanyId_PeriodEndDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions",
                columns: new[] { "ParentCompanyId", "PeriodEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioPositions_TracingNo",
                schema: "marketintelligence",
                table: "InvestmentPortfolioPositions",
                column: "TracingNo");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCompanyAliases_CompanyId",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCompanyAliases_FSortName",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                column: "FSortName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentPortfolioPositions",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "PortfolioCompanyAliases",
                schema: "marketintelligence");
        }
    }
}
