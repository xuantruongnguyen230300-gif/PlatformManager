using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformManager.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemCotErrorCodeChoImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                schema: "business",
                table: "ImportJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorCode",
                schema: "business",
                table: "ImportJobs");
        }
    }
}
