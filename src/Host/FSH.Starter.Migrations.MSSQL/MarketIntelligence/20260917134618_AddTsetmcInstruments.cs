using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddTsetmcInstruments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TsetmcInstruments",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Isin = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    YVal = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TsetmcInstruments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TsetmcInstruments_InsCode",
                schema: "dbo",
                table: "TsetmcInstruments",
                column: "InsCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TsetmcInstruments_Isin",
                schema: "dbo",
                table: "TsetmcInstruments",
                column: "Isin");

            migrationBuilder.CreateIndex(
                name: "IX_TsetmcInstruments_Symbol",
                schema: "dbo",
                table: "TsetmcInstruments",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TsetmcInstruments",
                schema: "dbo");
        }
    }
}
