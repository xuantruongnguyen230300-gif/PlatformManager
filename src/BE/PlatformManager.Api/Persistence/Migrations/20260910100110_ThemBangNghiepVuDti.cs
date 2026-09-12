using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformManager.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangNghiepVuDti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "business");

            migrationBuilder.CreateTable(
                name: "CriteriaGroups",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    Format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TargetWeekEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.CheckConstraint("CK_ImportJobs_TargetWeekEnd_Sunday", "EXTRACT(ISODOW FROM \"TargetWeekEnd\") = 7");
                });

            migrationBuilder.CreateTable(
                name: "Criteria",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameNormalized = table.Column<string>(type: "text", nullable: false),
                    CodeSortKey = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false, collation: "C"),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Criteria_CriteriaGroups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "business",
                        principalTable: "CriteriaGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaAssessments",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SelfScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    VerifiedScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriteriaAssessments_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "core",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CriteriaAssessments_Criteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalSchema: "business",
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_Code_Active",
                schema: "business",
                table: "Criteria",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_CodeSortKey",
                schema: "business",
                table: "Criteria",
                columns: new[] { "CodeSortKey", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_GroupId",
                schema: "business",
                table: "Criteria",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaAssessments_OwnerId",
                schema: "business",
                table: "CriteriaAssessments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "UX_CriteriaAssessments_CriteriaId_AssessmentDate",
                schema: "business",
                table: "CriteriaAssessments",
                columns: new[] { "CriteriaId", "AssessmentDate" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaGroups_Code_Active",
                schema: "business",
                table: "CriteriaGroups",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                schema: "business",
                table: "ImportJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriteriaAssessments",
                schema: "business");

            migrationBuilder.DropTable(
                name: "ImportJobs",
                schema: "business");

            migrationBuilder.DropTable(
                name: "Criteria",
                schema: "business");

            migrationBuilder.DropTable(
                name: "CriteriaGroups",
                schema: "business");
        }
    }
}
