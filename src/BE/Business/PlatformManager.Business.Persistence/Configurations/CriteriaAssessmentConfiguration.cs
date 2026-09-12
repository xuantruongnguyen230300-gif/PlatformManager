using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Configurations;

/// <summary>
/// Bảng <c>business."CriteriaAssessments"</c> — spec/danh-muc-dti/business-rules.md §1.3 và §1.4.
/// </summary>
public sealed class CriteriaAssessmentConfiguration : IEntityTypeConfiguration<CriteriaAssessment>
{
    public void Configure(EntityTypeBuilder<CriteriaAssessment> builder)
    {
        builder.ToTable("CriteriaAssessments", PlatformManagerDbContext.BusinessSchema);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Cột date THẬT (không phải phần ngày của một timestamp), nên EF Core index thẳng được —
        // KHÔNG cần hàm SQL IMMUTABLE viết tay như mô hình cũ, thứ tồn tại chỉ vì Postgres từ
        // chối CAST("CreatedAt" AS date) trong index (lỗi 42P17). Xem §1.3 b.
        builder.Property(x => x.AssessmentDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.Deadline).HasColumnType("date");

        builder.Property(x => x.SelfScore).HasPrecision(10, 2);
        builder.Property(x => x.VerifiedScore).HasPrecision(10, 2);
        builder.Property(x => x.Status).HasMaxLength(40);

        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // Optimistic concurrency: uint + IsRowVersion() ⇒ Npgsql bind thẳng vào cột hệ thống
        // xmin, KHÔNG tạo cột mới. byte[] + IsRowVersion() trên PostgreSQL tạo một cột bytea mà
        // KHÔNG AI CẬP NHẬT, nên WHERE "Version" = @original luôn khớp và check concurrency vô
        // hiệu HOÀN TOÀN, IM LẶNG. Luật: doc/huong_dan/quy-uoc/be-entity-domain.md §RowVersion.
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasOne<Criteria>()
            .WithMany()
            .HasForeignKey(x => x.CriteriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK XUYÊN SCHEMA, chiều duy nhất được phép: business -> core. Đây cũng chính là lý do
        // hai schema ở chung MỘT database — Postgres không khai được FK xuyên database
        // (doc/cau-truc-database.md §1.1). Không FK nào đi ngược core -> business.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // "1 đánh giá / 1 chỉ tiêu / 1 NGÀY" — NỀN MÓNG của toàn bộ mô hình kỳ. Mất nó, dữ liệu
        // trùng lọt vào IM LẶNG và mọi phép tổng hợp của Dashboard đếm hai lần.
        //
        // ⚠️ Ràng buộc này KHÔNG bảo đảm "1 bản ghi / 1 chỉ tiêu / 1 KỲ": một tuần có 7 ngày, nên
        // hai lời ghi vào cùng tuần ở hai ngày khác nhau vẫn tạo được hai dòng. DB không chặn nổi
        // vì period không phải một cột. Việc gộp về một bản ghi/kỳ do HANDLER làm (§5.3, vòng 2);
        // luật đọc §5.2 (lấy bản có AssessmentDate lớn nhất trong kỳ) là thứ giữ cho kết quả vẫn
        // xác định ngay cả khi có hai dòng lọt vào. Đừng ghi ở đâu rằng DB bảo đảm điều đó.
        // Index này phục vụ LUÔN hàng "Tra cứu theo kỳ | index (CriteriaId, AssessmentDate)" của
        // §1.4 — không khai thêm một index thứ hai trên cùng cặp cột. Lý do nó đủ: mọi đường đọc
        // đi qua global query filter soft-delete, tức mọi truy vấn đều mang "IsDeleted" = false,
        // nên Postgres dùng được index PARTIAL này. Một index không-unique trùng cột chỉ thêm chi
        // phí ghi mà không phục vụ truy vấn nào.
        builder.HasIndex(x => new { x.CriteriaId, x.AssessmentDate })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("UX_CriteriaAssessments_CriteriaId_AssessmentDate");

        // Truy vấn NÓNG của Dashboard lọc CHỈ theo khoảng ngày, không kèm CriteriaId
        // (DashboardRepository.GetAssessmentFactsAsync — nó nạp facts của cả một cửa sổ hai năm
        // rồi mới gộp trong bộ nhớ). Index ngay trên có AssessmentDate ở vị trí THỨ HAI, mà
        // doc/huong_dan/quy-uoc/tieu-chi-review.md §6 nói thẳng: index (A, B) mà query chỉ lọc
        // theo B thì VẪN LÀ THIẾU — Postgres phải quét toàn bộ index hoặc toàn bộ bảng.
        //
        // Đây KHÔNG mâu thuẫn với khối chú thích ngay trên (nó nói index unique phục vụ luôn
        // "tra cứu theo kỳ" của §1.4). Lập luận đó đúng cho đường DM-2 — đường đó luôn lọc kèm
        // CriteriaId — nhưng KHÔNG phủ đường Dashboard, thứ chỉ ra đời cùng lượt này.
        //
        // Partial cùng điều kiện với các index kia: mọi đường đọc đi qua global query filter
        // soft-delete nên truy vấn luôn mang "IsDeleted" = false.
        builder.HasIndex(x => x.AssessmentDate)
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_CriteriaAssessments_AssessmentDate");
    }
}
