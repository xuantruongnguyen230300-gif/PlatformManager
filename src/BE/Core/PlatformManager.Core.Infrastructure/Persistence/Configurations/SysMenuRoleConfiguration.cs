using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence.Configurations;

/// <summary>
/// PK là <c>Id</c> (BaseEntity), KHÔNG còn là cặp ghép (SysMenuId, RoleId) — đổi 2026-08-31 cùng
/// lượt cho entity kế thừa BaseEntity + <c>ReplaceAllAsync</c> chuyển sang xoá mềm. Cặp ghép làm
/// khoá thì dòng đã xoá mềm vẫn chiếm chỗ, nên lần lưu thứ hai của CÙNG một ô sẽ 23505; tính duy
/// nhất nay do unique index LỌC theo <c>IsDeleted = false</c> giữ (khuôn của
/// <c>IX_SysMenus_Code</c>, xem SysMenuConfiguration).
/// </summary>
public class SysMenuRoleConfiguration : IEntityTypeConfiguration<SysMenuRole>
{
    public void Configure(EntityTypeBuilder<SysMenuRole> builder)
    {
        builder.ToTable("SysMenuRoles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // Unique FILTERED — chỉ trong tập chưa xoá mềm. THIẾU filter này thì việc chèn lại một cặp
        // vừa bị xoá mềm sẽ trùng khoá, tức mọi lần lưu ma trận thứ hai đều hỏng. Đây là cái bẫy
        // đã dính một lần trên SysMenu — xem doc/huong_dan/wiki-core/be/13-core-data-migration.md
        // mục 1, và phép nghiệm thu số 3 ở doc/huong_dan/quy-uoc/be-entity-domain.md
        // §"Quyết định người dùng 2026-08-31".
        builder.HasIndex(x => new { x.SysMenuId, x.RoleId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_SysMenuRoles_SysMenuId_RoleId_Active");

        builder.HasOne<SysMenu>()
            .WithMany()
            .HasForeignKey(x => x.SysMenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
