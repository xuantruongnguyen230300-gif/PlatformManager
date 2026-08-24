using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformManager.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Optimistic concurrency cho CriteriaAssessment — xem doc/huong_dan/quy-uoc/be-entity-domain.md
    /// §RowVersion. `AddColumn&lt;uint&gt; name: "xmin"` TRÔNG như tạo cột mới nhưng Npgsql's
    /// migrations SQL generator NO-OP hoá thao tác này khi sinh SQL thật (không có
    /// `ALTER TABLE ... ADD COLUMN xmin` nào chạy — Postgres cấm tuyệt đối cột trùng tên cột hệ
    /// thống). Xác nhận trực tiếp từ tác giả Npgsql.EntityFrameworkCore.PostgreSQL (roji,
    /// npgsql/efcore.pg#3270, 2024): "A migration indeed gets generated for xmin, but it's
    /// ignored when generating the actual SQL... that would cause an error in PostgreSQL. Things
    /// are done this way because of how EF is designed." An toàn để giữ nguyên trong migration.
    /// </summary>
    public partial class AddCriteriaAssessmentRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "business",
                table: "CriteriaAssessments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "business",
                table: "CriteriaAssessments");
        }
    }
}
