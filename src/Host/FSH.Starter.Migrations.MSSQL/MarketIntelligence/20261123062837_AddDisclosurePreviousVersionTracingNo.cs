using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddDisclosurePreviousVersionTracingNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods");

            migrationBuilder.DropIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_IsActive",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods");

            migrationBuilder.DropColumn(
                name: "ExitDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods");

            migrationBuilder.RenameColumn(
                name: "EntryDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                newName: "ChangeDate");

            migrationBuilder.RenameIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId_EntryDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                newName: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId_ChangeDate");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentQuantity",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "PreviousVersionTracingNo",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentQuantity",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods");

            migrationBuilder.DropColumn(
                name: "PreviousVersionTracingNo",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.RenameColumn(
                name: "ChangeDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                newName: "EntryDate");

            migrationBuilder.RenameIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId_ChangeDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                newName: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId_EntryDate");

            migrationBuilder.AddColumn<string>(
                name: "ExitDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                columns: new[] { "ParentSymbol", "HoldingAssetId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_IsActive",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                columns: new[] { "ParentSymbol", "IsActive" });
        }
    }
}
