using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class RenameMonthlySalesToMonthlyActivitySummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "MonthlySales",
                schema: "marketintelligence",
                newName: "MonthlyActivitySummaries",
                newSchema: "marketintelligence");

            migrationBuilder.Sql(
                """
        EXEC sp_rename
            N'[marketintelligence].[PK_MonthlySales]',
            N'PK_MonthlyActivitySummaries',
            N'OBJECT';
        """);

            migrationBuilder.RenameIndex(
                name: "IX_MonthlySales_Symbol_PeriodEndDate",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "IX_MonthlyActivitySummaries_Symbol_PeriodEndDate");

            migrationBuilder.RenameColumn(
                name: "MonthlySalesAmount",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "PeriodAmount");

            migrationBuilder.RenameColumn(
                name: "MonthlySalesFormula",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "PeriodFormula");

            migrationBuilder.RenameColumn(
                name: "MonthlySalesAddress",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "PeriodAddress");

            migrationBuilder.RenameColumn(
                name: "MonthlySalesRowSequence",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "PeriodRowSequence");

            migrationBuilder.RenameColumn(
                name: "YearToDateSalesAmount",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateAmount");

            migrationBuilder.RenameColumn(
                name: "YearToDateSalesFormula",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateFormula");

            migrationBuilder.RenameColumn(
                name: "YearToDateSalesAddress",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateAddress");

            migrationBuilder.RenameColumn(
                name: "YearToDateSalesRowSequence",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateRowSequence");

            migrationBuilder.AddColumn<byte>(
                name: "Rt",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                type: "tinyint",
                nullable: true);

            migrationBuilder.Sql(
                """
        UPDATE summary
        SET summary.Rt = disclosure.Rt
        FROM [marketintelligence].[MonthlyActivitySummaries] AS summary
        INNER JOIN [marketintelligence].[Disclosures] AS disclosure
            ON disclosure.Id = summary.DisclosureId
        WHERE summary.Rt IS NULL;
        """);

            migrationBuilder.Sql(
                """
        UPDATE summary
        SET summary.Rt = disclosure.Rt
        FROM [marketintelligence].[MonthlyActivitySummaries] AS summary
        INNER JOIN [marketintelligence].[Disclosures] AS disclosure
            ON disclosure.TracingNo = summary.TracingNo
        WHERE summary.Rt IS NULL;
        """);

            migrationBuilder.Sql(
                """
        IF EXISTS
        (
            SELECT 1
            FROM [marketintelligence].[MonthlyActivitySummaries]
            WHERE Rt IS NULL
        )
        BEGIN
            THROW 51000,
                'Some MonthlyActivitySummaries rows could not be assigned an Rt value.',
                1;
        END;
        """);

            migrationBuilder.AlterColumn<byte>(
                name: "Rt",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rt",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries");

            migrationBuilder.RenameColumn(
                name: "PeriodAmount",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "MonthlySalesAmount");

            migrationBuilder.RenameColumn(
                name: "PeriodFormula",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "MonthlySalesFormula");

            migrationBuilder.RenameColumn(
                name: "PeriodAddress",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "MonthlySalesAddress");

            migrationBuilder.RenameColumn(
                name: "PeriodRowSequence",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "MonthlySalesRowSequence");

            migrationBuilder.RenameColumn(
                name: "YearToDateAmount",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateSalesAmount");

            migrationBuilder.RenameColumn(
                name: "YearToDateFormula",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateSalesFormula");

            migrationBuilder.RenameColumn(
                name: "YearToDateAddress",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateSalesAddress");

            migrationBuilder.RenameColumn(
                name: "YearToDateRowSequence",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "YearToDateSalesRowSequence");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlyActivitySummaries_Symbol_PeriodEndDate",
                schema: "marketintelligence",
                table: "MonthlyActivitySummaries",
                newName: "IX_MonthlySales_Symbol_PeriodEndDate");

            migrationBuilder.Sql(
                """
        EXEC sp_rename
            N'[marketintelligence].[PK_MonthlyActivitySummaries]',
            N'PK_MonthlySales',
            N'OBJECT';
        """);

            migrationBuilder.RenameTable(
                name: "MonthlyActivitySummaries",
                schema: "marketintelligence",
                newName: "MonthlySales",
                newSchema: "marketintelligence");
        }
    }
}
