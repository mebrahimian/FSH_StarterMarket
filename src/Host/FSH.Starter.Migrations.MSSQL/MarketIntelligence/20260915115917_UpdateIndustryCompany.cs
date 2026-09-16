using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class UpdateIndustryCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyIndustries_Industries_IndustryId",
                schema: "marketintelligence",
                table: "CompanyIndustries");

            migrationBuilder.RenameTable(
                name: "Industries",
                schema: "marketintelligence",
                newName: "Industries",
                newSchema: "dbo");

            migrationBuilder.CreateTable(
                name: "CodalCompanyImport",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IndustryId = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Isic = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IndustryGroupId = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ReportingType = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<int>(type: "int", nullable: true),
                    CompanyType = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodalCompanyImport", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodalCompanyImport_Symbol",
                schema: "dbo",
                table: "CodalCompanyImport",
                column: "Symbol");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyIndustries_Industries_IndustryId",
                schema: "marketintelligence",
                table: "CompanyIndustries",
                column: "IndustryId",
                principalSchema: "dbo",
                principalTable: "Industries",
                principalColumn: "IndustryId",
                onDelete: ReferentialAction.Restrict);
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyIndustries_Industries_IndustryId",
                schema: "marketintelligence",
                table: "CompanyIndustries");

            migrationBuilder.DropTable(
                name: "CodalCompanyImport",
                schema: "dbo");

            migrationBuilder.RenameTable(
                name: "Industries",
                schema: "dbo",
                newName: "Industries",
                newSchema: "marketintelligence");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyIndustries_Industries_IndustryId",
                schema: "marketintelligence",
                table: "CompanyIndustries",
                column: "IndustryId",
                principalSchema: "marketintelligence",
                principalTable: "Industries",
                principalColumn: "IndustryId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
