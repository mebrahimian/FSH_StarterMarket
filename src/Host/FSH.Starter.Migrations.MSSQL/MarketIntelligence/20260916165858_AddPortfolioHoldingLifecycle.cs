using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddPortfolioHoldingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HoldingAssetId",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PortfolioHoldingAssets",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FSortName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UnlistedCompanyId = table.Column<int>(type: "int", nullable: true),
                    ListedCompanyId = table.Column<int>(type: "int", nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioHoldingAssets", x => x.Id);
                    table.CheckConstraint("CK_PortfolioHoldingAssets_CompanyId", "[UnlistedCompanyId] IS NOT NULL OR [ListedCompanyId] IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "InvestmentPortfolioHoldingPeriods",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentSymbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HoldingAssetId = table.Column<int>(type: "int", nullable: false),
                    EntryDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExitDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentPortfolioHoldingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentPortfolioHoldingPeriods_PortfolioHoldingAssets_HoldingAssetId",
                        column: x => x.HoldingAssetId,
                        principalSchema: "marketintelligence",
                        principalTable: "PortfolioHoldingAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCompanyAliases_HoldingAssetId",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                column: "HoldingAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_HoldingAssetId",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                column: "HoldingAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                columns: new[] { "ParentSymbol", "HoldingAssetId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_HoldingAssetId_EntryDate",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                columns: new[] { "ParentSymbol", "HoldingAssetId", "EntryDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolioHoldingPeriods_ParentSymbol_IsActive",
                schema: "marketintelligence",
                table: "InvestmentPortfolioHoldingPeriods",
                columns: new[] { "ParentSymbol", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioHoldingAssets_FSortName",
                schema: "marketintelligence",
                table: "PortfolioHoldingAssets",
                column: "FSortName");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioHoldingAssets_ListedCompanyId",
                schema: "marketintelligence",
                table: "PortfolioHoldingAssets",
                column: "ListedCompanyId",
                unique: true,
                filter: "[ListedCompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioHoldingAssets_Symbol",
                schema: "marketintelligence",
                table: "PortfolioHoldingAssets",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioHoldingAssets_UnlistedCompanyId",
                schema: "marketintelligence",
                table: "PortfolioHoldingAssets",
                column: "UnlistedCompanyId",
                unique: true,
                filter: "[UnlistedCompanyId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioCompanyAliases_PortfolioHoldingAssets_HoldingAssetId",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases",
                column: "HoldingAssetId",
                principalSchema: "marketintelligence",
                principalTable: "PortfolioHoldingAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioCompanyAliases_PortfolioHoldingAssets_HoldingAssetId",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioCompanyAliases_HoldingAssetId",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases");

            migrationBuilder.DropColumn(
                name: "HoldingAssetId",
                schema: "marketintelligence",
                table: "PortfolioCompanyAliases");

            migrationBuilder.DropTable(
                name: "InvestmentPortfolioHoldingPeriods",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "PortfolioHoldingAssets",
                schema: "marketintelligence");
        }
    }
}
