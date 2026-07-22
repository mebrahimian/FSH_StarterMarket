using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class InitialMarketIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketintelligence");

            migrationBuilder.CreateTable(
                name: "Disclosures",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TracingNo = table.Column<long>(type: "bigint", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    LetterCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SentDateTime = table.Column<DateTime>(type: "datetime2", maxLength: 32, nullable: false),
                    PublishDateTime = table.Column<DateTime>(type: "datetime2", maxLength: 32, nullable: false),
                    HasHtml = table.Column<bool>(type: "bit", nullable: false),
                    IsEstimate = table.Column<bool>(type: "bit", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    HasExcel = table.Column<bool>(type: "bit", nullable: false),
                    HasPdf = table.Column<bool>(type: "bit", nullable: false),
                    HasXbrl = table.Column<bool>(type: "bit", nullable: false),
                    HasAttachment = table.Column<bool>(type: "bit", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ExcelUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    XbrlUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TedanUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disclosures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Disclosures_PublishDateTime",
                schema: "marketintelligence",
                table: "Disclosures",
                column: "PublishDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Disclosures_TracingNo",
                schema: "marketintelligence",
                table: "Disclosures",
                column: "TracingNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Disclosures",
                schema: "marketintelligence");
        }
    }
}
