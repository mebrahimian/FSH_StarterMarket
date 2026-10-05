using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddBackgroundJobStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundJobStatuses",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastFailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    LastProcessed = table.Column<int>(type: "int", nullable: true),
                    LastInserted = table.Column<int>(type: "int", nullable: true),
                    LastUpdated = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobStatuses_JobCode",
                schema: "marketintelligence",
                table: "BackgroundJobStatuses",
                column: "JobCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobStatuses",
                schema: "marketintelligence");
        }
    }
}
