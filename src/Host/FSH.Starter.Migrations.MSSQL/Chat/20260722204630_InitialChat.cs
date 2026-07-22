using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.Chat
{
    /// <inheritdoc />
    public partial class InitialChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                schema: "chat",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ChannelId", "IsPinned" },
                filter: "[IsPinned] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                schema: "chat",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ChannelId", "IsPinned" },
                filter: "\"IsPinned\" = true");
        }
    }
}
