using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Configurations;

public class CriteriaAssessmentConfiguration : IEntityTypeConfiguration<CriteriaAssessment>
{
    public void Configure(EntityTypeBuilder<CriteriaAssessment> builder)
    {
        builder.ToTable("CriteriaAssessments", schema: "business");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ProgressPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SelfScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.VerifiedScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.Note).HasColumnType("text");

        builder.Property(x => x.UserCreate).HasMaxLength(50);
        builder.Property(x => x.UserUpdate).HasMaxLength(50);

        builder.HasOne<Criteria>()
            .WithMany()
            .HasForeignKey(x => x.CriteriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index thường (không unique) để query nhanh theo CriteriaId + khoảng ngày — EF
        // Fluent API KHÔNG diễn đạt được unique filtered index trên CAST(DateCreate AS date),
        // phải vá tay vào file .sql sinh ra bởi `dotnet ef migrations script` (xem
        // doc/ke-hoach-xay-lai-corebase.md gotcha #5, doc/ERD/migrations/0003_corebase_v2.sql).
        //
        // ĐÃ XẢY RA LỖI THẬT (không phải giả định) — nếu quy tắc "cùng 1 ngày chỉ 1 record
        // CHƯA xoá mềm/CriteriaId" còn cần vá lại (model đổi → chạy lại `dotnet ef migrations
        // script`): TUYỆT ĐỐI KHÔNG viết thẳng CAST("DateCreate" AS date) trong biểu thức
        // index — Postgres từ chối với lỗi 42P17 "functions in index expression must be
        // marked IMMUTABLE" vì timestamptz→date phụ thuộc TimeZone session, không
        // deterministic. Phải bọc qua hàm SQL đã tạo sẵn CREATE OR REPLACE FUNCTION
        // criteria_assessment_date_utc(ts timestamptz) RETURNS date LANGUAGE sql IMMUTABLE
        // AS $$ SELECT (ts AT TIME ZONE 'UTC')::date; $$ — dùng
        // criteria_assessment_date_utc("DateCreate") thay vì CAST trực tiếp trong index
        // "UX_CriteriaAssessments_CriteriaId_DateCreate_Day" (xem 2 bản .sql đã vá:
        // doc/ERD/migrations/0003_corebase_v2.sql và
        // Core.Infrastructure/Persistence/Migrations/sql/0003_corebase_v2.sql).
        builder.HasIndex(x => new { x.CriteriaId, x.DateCreate })
            .HasDatabaseName("IX_CriteriaAssessments_CriteriaId_DateCreate");

        builder.HasQueryFilter(x => !x.IsDelete);
    }
}
