using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Configurations;

/// <summary>
/// Bảng <c>business."ImportJobs"</c> — spec/danh-muc-dti/business-rules.md §1.5 và §1.4.
/// </summary>
public sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        // CHECK constraint khai ngay tại ToTable: TargetWeekEnd luôn là CHỦ NHẬT — lưới an toàn
        // thứ hai cho §1.5 (lưới thứ nhất là ImportJob.Create). Đây cũng là lý do cột dùng kiểu
        // date thay vì chuỗi "YYYY-Www": Postgres kiểm được kiểu, còn một cột chuỗi thì nhận cả
        // "2026-W99".
        builder.ToTable(
            "ImportJobs",
            PlatformManagerDbContext.BusinessSchema,
            table => table.HasCheckConstraint(
                "CK_ImportJobs_TargetWeekEnd_Sunday",
                "EXTRACT(ISODOW FROM \"TargetWeekEnd\") = 7"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(20).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ResultJson);
        builder.Property(x => x.ErrorMessage);

        // Dài 100: khuôn businessCode là "MIEN.MA_LOI" UPPER_SNAKE — mã dài nhất của cụm hôm nay
        // chưa tới 40 ký tự. Đặt trần thay vì để text tự do vì đây là một MÃ, không phải câu chữ.
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.TargetWeekEnd).HasColumnType("date").IsRequired();

        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // Phục vụ endpoint poll GET /api/import/{jobId} và việc quét job theo trạng thái — giữ
        // nguyên tên từ bảng cũ (§1.4).
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_ImportJobs_Status");

        // KHÔNG có FK nào — job độc lập với dữ liệu nó ghi ra (§1.5).
    }
}
