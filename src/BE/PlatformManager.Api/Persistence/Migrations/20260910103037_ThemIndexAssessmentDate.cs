using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformManager.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemIndexAssessmentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CriteriaAssessments_AssessmentDate",
                schema: "business",
                table: "CriteriaAssessments",
                column: "AssessmentDate",
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CriteriaAssessments_AssessmentDate",
                schema: "business",
                table: "CriteriaAssessments");
        }
    }
}
