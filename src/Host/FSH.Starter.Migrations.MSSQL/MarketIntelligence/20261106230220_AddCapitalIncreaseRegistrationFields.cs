using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddCapitalIncreaseRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AssemblyDate",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashIncoming",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CodalDisclosureId",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NewCapital",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousCapital",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RegistrationDate",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Reserves",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RetainedEarnings",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RevaluationSurplus",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SharePremium",
                schema: "marketintelligence",
                table: "InstrumentShareChanges",
                type: "decimal(28,6)",
                precision: 28,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssemblyDate",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "CashIncoming",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "CodalDisclosureId",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "NewCapital",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "PreviousCapital",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "Reserves",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "RetainedEarnings",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "RevaluationSurplus",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");

            migrationBuilder.DropColumn(
                name: "SharePremium",
                schema: "marketintelligence",
                table: "InstrumentShareChanges");
        }
    }
}
