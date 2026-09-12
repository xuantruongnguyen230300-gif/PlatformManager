using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Configurations;

/// <summary>
/// Bảng <c>business."CriteriaGroups"</c> — spec/danh-muc-dti/business-rules.md §1.1 và §1.4.
/// </summary>
public sealed class CriteriaGroupConfiguration : IEntityTypeConfiguration<CriteriaGroup>
{
    public void Configure(EntityTypeBuilder<CriteriaGroup> builder)
    {
        // Schema khai TƯỜNG MINH. Mặc định của model là "core" (HasDefaultSchema ở
        // PlatformManagerDbContext), nên QUÊN tham số thứ hai là bảng nghiệp vụ rơi thẳng vào
        // schema đi theo CoreBase — không lỗi biên dịch, không test đơn vị nào báo. Canh bằng
        // SchemaBoundaryTests. Lấy hằng số từ DbContext chứ không gõ chuỗi: test lấy kỳ vọng từ
        // cùng một nguồn với mã sản phẩm.
        builder.ToTable("CriteriaGroups", PlatformManagerDbContext.BusinessSchema);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();

        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // Unique PARTIAL: chỉ trong tập chưa xoá mềm (§1.4). Unique toàn bảng sẽ chặn việc tạo
        // lại đúng mã vừa xoá mềm — dòng đã xoá vẫn chiếm chỗ trong index — trong khi màn hình
        // không còn nhóm nào mang mã ấy, và không có đường đi vòng nào ngoài xoá cứng dòng trong DB.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_CriteriaGroups_Code_Active");
    }
}
