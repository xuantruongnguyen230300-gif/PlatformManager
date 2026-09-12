using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Business.Domain.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Configurations;

/// <summary>
/// Bảng <c>business."Criteria"</c> — spec/danh-muc-dti/business-rules.md §1.2 và §1.4.
/// </summary>
public sealed class CriteriaConfiguration : IEntityTypeConfiguration<Criteria>
{
    public void Configure(EntityTypeBuilder<Criteria> builder)
    {
        builder.ToTable("Criteria", PlatformManagerDbContext.BusinessSchema);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).HasMaxLength(CriteriaCode.MaxLength).IsRequired();
        builder.Property(x => x.Name).IsRequired();

        // Không giới hạn độ dài: §1.2 khai Name là "bắt buộc, không giới hạn cứng độ dài", nên
        // NameNormalized — bản dẫn xuất 1-1 của nó — cũng không giới hạn được. Đặt trần cho cột
        // dẫn xuất mà không đặt cho cột gốc là dựng một chỗ cắt cụt im lặng.
        builder.Property(x => x.NameNormalized).IsRequired();

        // COLLATE "C" — so sánh theo BYTE. Bắt buộc: collation theo ngôn ngữ có thể bỏ qua dấu
        // câu khi so, mà khoá này dựa vào đúng vị trí của dấu chấm và các số 0 đệm để cho ra thứ
        // tự tự nhiên (Q53). Không có nó thì "0004.0002" và "00040002" có thể so bằng nhau.
        builder.Property(x => x.CodeSortKey)
            .HasMaxLength(CriteriaCode.SortKeyMaxLength)
            .UseCollation("C")
            .IsRequired();

        builder.Property(x => x.MaxScore).HasPrecision(10, 2).IsRequired();

        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // Cùng module, khác aggregate ⇒ hard FK + Restrict (be-entity-domain.md §FK cross-module).
        builder.HasOne<CriteriaGroup>()
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Criteria_Code_Active");

        // Sắp mã tự nhiên (Q53). Phụ theo Code để thứ tự XÁC ĐỊNH khi hai mã cho cùng khoá —
        // xảy ra khi chúng chỉ khác số 0 đầu đoạn ("4.02" và "4.2"); §2 không cấm ca đó, và phân
        // trang thì cần một thứ tự không đổi giữa hai lần gọi.
        builder.HasIndex(x => new { x.CodeSortKey, x.Code })
            .HasDatabaseName("IX_Criteria_CodeSortKey");

        // ⚠️ CỐ Ý KHÔNG có index cho NameNormalized (Q59 gỡ nó ngày 2026-09-10, §1.4). Khớp
        // CHUỖI CON (LIKE '%x%') không dùng được btree, còn index trigram thì cần extension mà
        // Q47 đã loại. Danh mục chỉ vài chục chỉ tiêu nên quét tuần tự là đủ. Đừng "thêm lại cho
        // chắc" — nó sẽ không phục vụ truy vấn nào.
    }
}
