using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.Files
{
    /// <inheritdoc />
    public partial class InitialFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets");

            migrationBuilder.CreateIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets",
                columns: new[] { "StorageKey", "TenantId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets");

            migrationBuilder.CreateIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets",
                columns: new[] { "StorageKey", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
