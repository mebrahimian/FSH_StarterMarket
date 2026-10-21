using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddUserPaymentFinancialSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AppliedCredit",
                schema: "marketintelligence",
                table: "UserPaymentRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                schema: "marketintelligence",
                table: "UserPaymentRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingCredit",
                schema: "marketintelligence",
                table: "UserPaymentRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UpgradeFee",
                schema: "marketintelligence",
                table: "UserPaymentRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedCredit",
                schema: "marketintelligence",
                table: "UserPaymentRequests");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                schema: "marketintelligence",
                table: "UserPaymentRequests");

            migrationBuilder.DropColumn(
                name: "RemainingCredit",
                schema: "marketintelligence",
                table: "UserPaymentRequests");

            migrationBuilder.DropColumn(
                name: "UpgradeFee",
                schema: "marketintelligence",
                table: "UserPaymentRequests");
        }
    }
}
