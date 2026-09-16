using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddIndustryCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Isic",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Industries",
                schema: "marketintelligence",
                columns: table => new
                {
                    IndustryId = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    IndustryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Industries", x => x.IndustryId);
                });

            migrationBuilder.CreateTable(
                name: "CompanyIndustries",
                schema: "marketintelligence",
                columns: table => new
                {
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IndustryId = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    IndustryGroupId = table.Column<string>(type: "char(3)", maxLength: 3, nullable: true),
                    Isic = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyIndustries", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_CompanyIndustries_Industries_IndustryId",
                        column: x => x.IndustryId,
                        principalSchema: "marketintelligence",
                        principalTable: "Industries",
                        principalColumn: "IndustryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyIndustries_IndustryId",
                schema: "marketintelligence",
                table: "CompanyIndustries",
                column: "IndustryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyIndustries",
                schema: "marketintelligence");

            migrationBuilder.DropTable(
                name: "Industries",
                schema: "marketintelligence");

            migrationBuilder.DropColumn(
                name: "Isic",
                schema: "marketintelligence",
                table: "InvestmentPortfolioReportMetadata");
        }
    }
}
