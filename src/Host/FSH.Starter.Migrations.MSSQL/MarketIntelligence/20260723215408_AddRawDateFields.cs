using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddRawDateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublishDateTimeRaw",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentDateTimeRaw",
                schema: "marketintelligence",
                table: "Disclosures",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishDateTimeRaw",
                schema: "marketintelligence",
                table: "Disclosures");

            migrationBuilder.DropColumn(
                name: "SentDateTimeRaw",
                schema: "marketintelligence",
                table: "Disclosures");
        }
    }
}
