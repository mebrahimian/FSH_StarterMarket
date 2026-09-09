using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MarketIntelligence
{
    /// <inheritdoc />
    public partial class AddCodalIncrementalScheduleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodalIncrementalScheduleSettings",
                schema: "marketintelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartHour = table.Column<int>(type: "int", nullable: false),
                    MorningEndHour = table.Column<int>(type: "int", nullable: false),
                    EndHour = table.Column<int>(type: "int", nullable: false),
                    BusyPeriodEndDay = table.Column<int>(type: "int", nullable: false),
                    BusyMorningMinutes = table.Column<int>(type: "int", nullable: false),
                    BusyAfternoonMinutes = table.Column<int>(type: "int", nullable: false),
                    NormalMorningMinutes = table.Column<int>(type: "int", nullable: false),
                    NormalAfternoonMinutes = table.Column<int>(type: "int", nullable: false),
                    ThursdayBusyMorningMinutes = table.Column<int>(type: "int", nullable: false),
                    ThursdayBusyAfternoonMinutes = table.Column<int>(type: "int", nullable: false),
                    ThursdayNormalMorningMinutes = table.Column<int>(type: "int", nullable: false),
                    ThursdayNormalAfternoonMinutes = table.Column<int>(type: "int", nullable: false),
                    FridayMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodalIncrementalScheduleSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodalIncrementalScheduleSettings",
                schema: "marketintelligence");
        }
    }
}
