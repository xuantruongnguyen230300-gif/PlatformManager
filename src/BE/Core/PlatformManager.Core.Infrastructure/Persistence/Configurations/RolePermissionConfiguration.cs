using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence.Configurations;

/// <summary>PK là <c>Id</c> (BaseEntity) + unique index lọc theo <c>IsDeleted = false</c> cho cặp
/// (RoleId, ResourceKey) — cùng lý do và cùng khuôn với SysMenuRoleConfiguration, đổi
/// 2026-08-31.</summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.Property(x => x.ResourceKey).HasMaxLength(100);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // Xem docstring của SysMenuRoleConfiguration cho lý do đầy đủ của filter này.
        builder.HasIndex(x => new { x.RoleId, x.ResourceKey })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_RolePermissions_RoleId_ResourceKey_Active");

        // Q2 (doc/huong_dan/quy-uoc/be-performance.md) — PK ghép dẫn đầu bằng "RoleId" nên KHÔNG seek được
        // cho predicate nóng nhất của hệ thống: RequirePermissionFilter lọc trước hết theo
        // ResourceKey (= key của endpoint) rồi mới ghép RoleId. Index này dẫn đầu ĐÚNG cột được
        // lọc, và phủ luôn cột join → cho phép index-only scan, không phải chạm heap.
        // Lưu ý khi đọc EXPLAIN: ở cỡ dữ liệu hiện tại (vài dòng — đếm bằng lệnh; danh mục key do
        // host khai, xem ICoreResourceKeySource) Postgres vẫn chọn Seq Scan vì
        // bảng chỉ có 1 page — đó là lựa chọn ĐÚNG của planner, không phải index vô dụng. Index
        // có giá trị khi số (role × resource key) tăng lên; chi phí giữ nó gần bằng 0.
        //
        // CỐ Ý KHÔNG thêm filter "IsDeleted = false" cho index tra cứu này (khác index unique ở
        // trên, nơi filter là BẮT BUỘC về mặt ngữ nghĩa). Index tra cứu chỉ cần phủ đúng cột được
        // lọc; thêm filter vào đây bắt Postgres phải CHỨNG MINH được predicate của query
        // (`NOT "IsDeleted"` do global query filter sinh ra) suy ra được predicate của index
        // (`"IsDeleted" = false`) — hai biểu thức khác dạng, và nếu planner không chứng minh được
        // thì index thành vô dụng trong im lặng. Đánh đổi: index chứa cả dòng đã xoá mềm, tức lớn
        // hơn — chấp nhận được với bảng cỡ này.
        builder.HasIndex(x => new { x.ResourceKey, x.RoleId })
            .HasDatabaseName("IX_RolePermissions_ResourceKey_RoleId");

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
