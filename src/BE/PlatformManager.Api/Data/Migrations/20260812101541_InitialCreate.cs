using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Criteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Criteria_CriteriaGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "CriteriaGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SelfScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    VerifiedScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriteriaAssessments_AppUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CriteriaAssessments_Criteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriteriaAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriteriaEvidences_CriteriaAssessments_CriteriaAssessmentId",
                        column: x => x.CriteriaAssessmentId,
                        principalTable: "CriteriaAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_Code",
                table: "Criteria",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_GroupId",
                table: "Criteria",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaAssessments_CriteriaId",
                table: "CriteriaAssessments",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaAssessments_OwnerId",
                table: "CriteriaAssessments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaEvidences_CriteriaAssessmentId",
                table: "CriteriaEvidences",
                column: "CriteriaAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaGroups_Code",
                table: "CriteriaGroups",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            // Unique (CriteriaId, CAST(CreatedAt AS date)) filtered WHERE IsDeleted=false — tối đa
            // 1 CriteriaAssessment/chỉ tiêu/ngày, xem doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm)".
            // EF Core fluent API (HasIndex) không diễn đạt được CAST trực tiếp -> raw SQL.
            //
            // LƯU Ý (đã tự nghiên cứu, xem cảnh báo trong CLAUDE.md): CAST(timestamptz AS date)
            // trực tiếp KHÔNG dùng được trong index expression vì hàm ép kiểu đó phụ thuộc
            // session TimeZone (Postgres coi là STABLE) -> lỗi "functions in index expression
            // must be marked IMMUTABLE" (đã verify thực tế khi chạy migration). Cách khắc phục
            // chuẩn của cộng đồng Postgres: bọc qua 1 SQL function nhỏ ép về UTC trước rồi mới
            // cast — vì 'UTC' là múi giờ CỐ ĐỊNH (không có quy tắc DST thay đổi theo thời gian
            // như các zone khác), quy đổi này thực sự ổn định/tất định nên có thể khai báo
            // IMMUTABLE an toàn. Ứng dụng luôn ghi CreatedAt dưới dạng UTC (DateTimeOffset.UtcNow)
            // nên hàm này phản ánh đúng "ngày" nghiệp vụ của record.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION assessment_date_utc(ts timestamptz)
                RETURNS date
                LANGUAGE sql
                IMMUTABLE
                AS $$ SELECT (ts AT TIME ZONE 'UTC')::date $$;
                """);
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_CriteriaAssessments_CriteriaId_CreatedDate"
                ON "CriteriaAssessments" ("CriteriaId", assessment_date_utc("CreatedAt"))
                WHERE "IsDeleted" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_CriteriaAssessments_CriteriaId_CreatedDate\";");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS assessment_date_utc(timestamptz);");

            migrationBuilder.DropTable(
                name: "CriteriaEvidences");

            migrationBuilder.DropTable(
                name: "CriteriaAssessments");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "Criteria");

            migrationBuilder.DropTable(
                name: "CriteriaGroups");
        }
    }
}
