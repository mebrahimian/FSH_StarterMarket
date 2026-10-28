using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddCalculationWorkItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalculationWorkItems",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangeVersion = table.Column<long>(type: "bigint", nullable: false),
                    CompletedVersion = table.Column<long>(type: "bigint", nullable: false),
                    ClaimedVersion = table.Column<long>(type: "bigint", nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetryAfterUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LastSuccessAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationWorkItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationWorkItems_CompanyId_TaskType",
                schema: "marketintelligence",
                table: "CalculationWorkItems",
                columns: new[] { "CompanyId", "TaskType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculationWorkItems_RetryAfterUtc",
                schema: "marketintelligence",
                table: "CalculationWorkItems",
                column: "RetryAfterUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalculationWorkItems",
                schema: "marketintelligence");
        }
    }
}
