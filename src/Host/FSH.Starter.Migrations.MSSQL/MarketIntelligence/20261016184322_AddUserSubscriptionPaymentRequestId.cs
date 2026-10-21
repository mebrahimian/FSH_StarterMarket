using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddUserSubscriptionPaymentRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentRequestId",
                schema: "marketintelligence",
                table: "UserSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaymentRequestId",
                schema: "marketintelligence",
                table: "UserSubscriptions",
                columns: new[] { "PaymentRequestId", "TenantId" },
                unique: true,
                filter: "[PaymentRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_PaymentRequestId",
                schema: "marketintelligence",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentRequestId",
                schema: "marketintelligence",
                table: "UserSubscriptions");
        }
    }
}
