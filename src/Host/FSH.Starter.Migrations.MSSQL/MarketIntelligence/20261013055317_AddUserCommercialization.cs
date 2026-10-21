using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddUserCommercialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPaymentRequests",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PlanKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ReceiptFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPaymentRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPlanLimits",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxSelectedSymbols = table.Column<int>(type: "int", nullable: false),
                    MaxSymbolsPerIndustry = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlanLimits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSelectedSymbols",
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
                    table.PrimaryKey("PK_UserSelectedSymbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PlanKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPaymentRequests_ReceiptFileId",
                schema: "marketintelligence",
                table: "UserPaymentRequests",
                columns: new[] { "ReceiptFileId", "TenantId" },
                unique: true,
                filter: "[ReceiptFileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPaymentRequests_TenantId_UserId_CreatedAtUtc",
                schema: "marketintelligence",
                table: "UserPaymentRequests",
                columns: new[] { "TenantId", "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPlanLimits_PlanKey",
                schema: "marketintelligence",
                table: "UserPlanLimits",
                column: "PlanKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSelectedSymbols_TenantId_UserId_CompanyId",
                schema: "marketintelligence",
                table: "UserSelectedSymbols",
                columns: new[] { "TenantId", "UserId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSelectedSymbols_TenantId_UserId_SelectedAtUtc",
                schema: "marketintelligence",
                table: "UserSelectedSymbols",
                columns: new[] { "TenantId", "UserId", "SelectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_TenantId_UserId_StartUtc",
                schema: "marketintelligence",
                table: "UserSubscriptions",
                columns: new[] { "TenantId", "UserId", "StartUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPaymentRequests",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "UserPlanLimits",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "UserSelectedSymbols",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "UserSubscriptions",
                schema: "marketintelligence");
        }
    }
}
