using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddUserSelectedPortfolioValuationSymbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSelectedPortfolioValuationSymbols",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SelectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSelectedPortfolioValuationSymbols", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSelectedPortfolioValuationSymbols_TenantId_UserId_CompanyId",
                schema: "marketintelligence",
                table: "UserSelectedPortfolioValuationSymbols",
                columns: new[] { "TenantId", "UserId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSelectedPortfolioValuationSymbols_TenantId_UserId_SelectedAtUtc",
                schema: "marketintelligence",
                table: "UserSelectedPortfolioValuationSymbols",
                columns: new[] { "TenantId", "UserId", "SelectedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSelectedPortfolioValuationSymbols",
                schema: "marketintelligence");
        }
    }
}
