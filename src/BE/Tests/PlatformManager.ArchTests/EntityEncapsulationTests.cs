using System.Reflection;
using PlatformManager.Core.Domain.Common;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Chỉ 6 field kỹ thuật của BaseEntity được phép lộ setter public (`Id` là `init`, 5 field
/// audit là `set`) — mọi field nghiệp vụ của entity con phải private set + mutate qua method
/// có tên nghiệp vụ (factory method pattern, xem
/// doc/huong_dan/quy-uoc/be-entity-domain.md). Hiện chỉ quét Core.Domain — thêm module nghiệp vụ
/// mới thì thêm assembly Domain của nó vào <see cref="DomainAssemblies"/>. AppUser/AppRole (Identity)
/// KHÔNG kế thừa BaseEntity nên không bị test này ràng buộc — đúng chủ đích thiết kế.
/// </summary>
public class EntityEncapsulationTests
{
    private static readonly HashSet<string> AllowedPublicSetterNames =
        ["Id", "CreatedBy", "UpdatedBy", "CreatedAt", "UpdatedAt", "IsDeleted"];

    /// <summary>
    /// 🛑 <b>DANH SÁCH LIỆT KÊ TAY — thiếu một assembly domain thì luật dưới XANH MÀ KHÔNG ĐO GÌ</b>
    /// cho đúng những entity nó bỏ sót (không có assert nào bắt được, vì entity vắng mặt cũng vắng
    /// khỏi tập được kiểm — cùng khuôn hỏng đã đo ở finding F7).
    ///
    /// <para><b>Khi dựng <c>PlatformManager.Business.Domain</c> (bước 3): thêm assembly của nó vào
    /// đây</b> — đó mới là nơi CÓ entity nghiệp vụ thật, tức nơi luật "field nghiệp vụ phải
    /// <c>private set</c>, mutate qua method có tên nghiệp vụ" thực sự có việc để làm. Hôm nay
    /// danh sách chỉ có Core.Domain nên luật gần như chỉ chạy không tải.</para>
    ///
    /// <para><c>Core.Api</c> (2026-09-09) KHÔNG thuộc danh sách này — nó không chứa entity nào.</para>
    /// </summary>
    private static readonly Assembly[] DomainAssemblies =
    [
        typeof(BaseEntity).Assembly,
    ];

    [Fact]
    public void BaseEntity_Descendants_MustNotHave_PublicSetter_ForBusinessProperties()
    {
        var violations = new List<string>();

        foreach (var domainAssembly in DomainAssemblies)
        {
            var entityTypes = domainAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseEntity).IsAssignableFrom(t));

            foreach (var type in entityTypes)
            {
                var declaredProperties = type.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var prop in declaredProperties)
                {
                    if (AllowedPublicSetterNames.Contains(prop.Name))
                        continue;

                    if (prop.SetMethod is { IsPublic: true })
                        violations.Add($"{type.Name}.{prop.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Entity nghiệp vụ có public setter ngoài 6 field kỹ thuật cho phép: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// <c>Id</c> nằm trong <see cref="AllowedPublicSetterNames"/> nên test trên MIỄN TRỪ nó hoàn
    /// toàn — nghĩa là không có gì canh việc nó phải là <c>init</c> chứ không phải <c>set</c>.
    /// Đổi <c>BaseEntity.Id</c> về <c>{ get; set; }</c> thì cả bộ test vẫn xanh (thử 2026-08-28,
    /// finding F7), trong khi chính <c>init</c> là bất biến mà quyết định "BaseEntity sở hữu Id,
    /// sinh một lần lúc khởi tạo" dựa vào.
    ///
    /// <para>Reflection không có khái niệm "init-only": C# mã hoá nó bằng modreq
    /// <c>IsExternalInit</c> trên kiểu trả về của setter. Đó là lý do phải kiểm qua
    /// <c>GetRequiredCustomModifiers()</c> thay vì một cờ nào đó của <c>SetMethod</c>.</para>
    ///
    /// <para>Canary (đã chạy 2026-08-28): tạm đổi <c>Id</c> sang <c>{ get; set; }</c> → test này
    /// đỏ; khôi phục <c>init</c> → xanh.</para>
    /// </summary>
    [Fact(DisplayName = "BaseEntity.Id phải là init-only, không phải set")]
    public void BaseEntityId_MustBe_InitOnly()
    {
        var idProperty = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
        Assert.NotNull(idProperty);

        var setter = idProperty!.SetMethod;
        Assert.NotNull(setter);

        Assert.Contains(
            typeof(System.Runtime.CompilerServices.IsExternalInit),
            setter!.ReturnParameter.GetRequiredCustomModifiers());
    }
}
