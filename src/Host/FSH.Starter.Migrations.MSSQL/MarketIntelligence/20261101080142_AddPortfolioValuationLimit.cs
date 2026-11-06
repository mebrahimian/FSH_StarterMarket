using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddPortfolioValuationLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPortfolioValuationSymbols",
                schema: "marketintelligence",
                table: "UserPlanLimits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
        UPDATE marketintelligence.UserPlanLimits
        SET MaxPortfolioValuationSymbols =
            CASE PlanKey
                WHEN 'free' THEN 3
                WHEN 'economic' THEN 3
                WHEN 'professional' THEN 5
                WHEN 'premium' THEN -1
                ELSE 0
            END;
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPortfolioValuationSymbols",
                schema: "marketintelligence",
                table: "UserPlanLimits");
        }
    }
}
