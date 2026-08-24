using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformManager.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissionResourceKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ResourceKey_RoleId",
                schema: "core",
                table: "RolePermissions",
                columns: new[] { "ResourceKey", "RoleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_ResourceKey_RoleId",
                schema: "core",
                table: "RolePermissions");
        }
    }
}
