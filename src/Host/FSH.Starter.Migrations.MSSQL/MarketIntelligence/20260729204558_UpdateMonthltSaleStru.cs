using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class UpdateMonthltSaleStru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MonthlySalesAddress",
                schema: "marketintelligence",
                table: "MonthlySales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonthlySalesFormula",
                schema: "marketintelligence",
                table: "MonthlySales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlySalesRowSequence",
                schema: "marketintelligence",
                table: "MonthlySales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "YearToDateSalesAddress",
                schema: "marketintelligence",
                table: "MonthlySales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YearToDateSalesFormula",
                schema: "marketintelligence",
                table: "MonthlySales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearToDateSalesRowSequence",
                schema: "marketintelligence",
                table: "MonthlySales",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlySalesAddress",
                schema: "marketintelligence",
                table: "MonthlySales");

            migrationBuilder.DropColumn(
                name: "MonthlySalesFormula",
                schema: "marketintelligence",
                table: "MonthlySales");

            migrationBuilder.DropColumn(
                name: "MonthlySalesRowSequence",
                schema: "marketintelligence",
                table: "MonthlySales");

            migrationBuilder.DropColumn(
                name: "YearToDateSalesAddress",
                schema: "marketintelligence",
                table: "MonthlySales");

            migrationBuilder.DropColumn(
                name: "YearToDateSalesFormula",
                schema: "marketintelligence",
                table: "MonthlySales");

            migrationBuilder.DropColumn(
                name: "YearToDateSalesRowSequence",
                schema: "marketintelligence",
                table: "MonthlySales");
        }
    }
}
