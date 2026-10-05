using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddBenchmarkAssetSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalCode",
                schema: "marketintelligence",
                table: "BenchmarkAssets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "marketintelligence",
                table: "BenchmarkAssets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkAssets_Source_ExternalCode",
                schema: "marketintelligence",
                table: "BenchmarkAssets",
                columns: new[] { "Source", "ExternalCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BenchmarkAssets_Source_ExternalCode",
                schema: "marketintelligence",
                table: "BenchmarkAssets");

            migrationBuilder.DropColumn(
                name: "ExternalCode",
                schema: "marketintelligence",
                table: "BenchmarkAssets");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "marketintelligence",
                table: "BenchmarkAssets");
        }
    }
}
