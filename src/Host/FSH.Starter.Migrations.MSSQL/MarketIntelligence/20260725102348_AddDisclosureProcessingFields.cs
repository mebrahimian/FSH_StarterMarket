using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddDisclosureProcessingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Ct",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Ft",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Let",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Rt",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "SalesParseStatus",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SalesParsedAt",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ct",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.DropColumn(
                name: "Ft",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.DropColumn(
                name: "Let",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.DropColumn(
                name: "Rt",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.DropColumn(
                name: "SalesParseStatus",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.DropColumn(
                name: "SalesParsedAt",
                schema: "marketintelligence",
                table: "Disclosures");
        }
    }
}
