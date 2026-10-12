using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddCodalProcessingLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodalProcessingLogs",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Level = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FromDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ToDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    TracingNo = table.Column<long>(type: "bigint", nullable: true),
                    ErrorType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodalProcessingLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodalProcessingLogs_JobId",
                schema: "marketintelligence",
                table: "CodalProcessingLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CodalProcessingLogs_Operation_OccurredAt",
                schema: "marketintelligence",
                table: "CodalProcessingLogs",
                columns: new[] { "Operation", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CodalProcessingLogs_Status_Level",
                schema: "marketintelligence",
                table: "CodalProcessingLogs",
                columns: new[] { "Status", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_CodalProcessingLogs_TracingNo",
                schema: "marketintelligence",
                table: "CodalProcessingLogs",
                column: "TracingNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodalProcessingLogs",
                schema: "marketintelligence");
        }
    }
}
