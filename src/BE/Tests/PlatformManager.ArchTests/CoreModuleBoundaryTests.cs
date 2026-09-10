using System.Reflection;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// 2 ArchTest theo đúng doc/kien-truc-core-module.md §"2 ArchTest mới cần thêm" — bắt đúng 2
/// hướng vi phạm dễ xảy ra nhất khi thêm code/module mới: Core lỡ tay biết tới 1 Module cụ thể,
/// hoặc 1 Module lỡ tay reference thẳng Module khác thay vì nâng logic dùng chung lên
/// Core.Application.
///
/// <para><b>2026-08-29:</b> <see cref="BusinessTierAssemblies"/> đang RỖNG — module nghiệp vụ duy
/// nhất (DtiWeekly) đã bị xoá để xây lại. <c>Modules_MustNotReference_OtherModules</c> vì thế qua
/// một cách hiển nhiên, còn <c>Core_MustNotReference_AnyModulesAssembly</c> vẫn kiểm thật (nó quét
/// Core, không quét module). Giữ nguyên cả hai: đây là bảo hiểm MIỄN PHÍ cho tầng nghiệp vụ đầu
/// tiên được dựng lại — chỉ cần thêm assembly vào mảng dưới là test tự phủ.</para>
///
/// <para><b>Sửa 2026-09-09 — tiền tố tên tầng nghiệp vụ.</b> Cả 2 luật trước đó khớp
/// <c>PlatformManager.Modules.</c>, tên của mô hình N-module đã bị BỎ ở v3
/// (doc/kien-truc-core-module.md §"Vì sao KHÔNG dùng mô hình N-module"). Tên tầng đích nay là
/// <c>PlatformManager.Business.</c>, nên trước lần sửa này luật "Core không được reference tầng
/// nghiệp vụ" khớp một tiền tố KHÔNG BAO GIỜ XUẤT HIỆN — tức nó không cưỡng chế gì cả. Giữ lại
/// tên cũ trong <see cref="BusinessTierAssemblyPrefixes"/> làm dây bẫy: nó rẻ, và nếu mô hình
/// N-module quay lại thì luật vẫn canh ngay từ ngày đầu.</para>
/// </summary>
public class CoreModuleBoundaryTests
{
    /// <summary>Tiền tố tên assembly của tầng Core — dùng để lọc ra khỏi danh sách sản phẩm.</summary>
    private const string CoreTierAssemblyPrefix = "PlatformManager.Core.";

    /// <summary>
    /// Mọi assembly <c>PlatformManager.Core.*</c>, <b>DẪN XUẤT</b> từ
    /// <see cref="ProductAssemblies.All"/> — không liệt kê tay.
    ///
    /// <para><b>Sửa 2026-09-09.</b> Trước đó chỗ này là một danh sách liệt kê tay thứ bảy trong bộ
    /// ArchTests, trùng 4/5 mục với <see cref="ProductAssemblies.All"/> nhưng KHÔNG mang nhãn 🛑
    /// như 6 danh sách kia. Hai bản chép của cùng một sự thật chỉ lệch được theo một chiều: dựng
    /// project Core mới, nhớ khai ở <c>ProductAssemblies</c> (chỗ có cảnh báo) và quên chỗ này —
    /// khi đó luật "Core không được biết tầng nghiệp vụ" lặng lẽ bỏ qua đúng assembly mới nhất.
    /// Dẫn xuất làm ca đó không xảy ra được.</para>
    ///
    /// <para>Host <c>PlatformManager.Api</c> tự rơi ra vì tên nó không mang tiền tố trên — và đúng
    /// vậy: host là composition root, nó được phép thấy mọi tầng.</para>
    /// </summary>
    private static readonly Assembly[] CoreAssemblies =
        [.. ProductAssemblies.All.Where(assembly =>
            assembly.GetName().Name!.StartsWith(CoreTierAssemblyPrefix, StringComparison.Ordinal))];

    /// <summary>
    /// Tiền tố tên assembly của TẦNG NGHIỆP VỤ — thứ Core tuyệt đối không được reference.
    /// <c>PlatformManager.Modules.</c> là tên đã nghỉ hưu, giữ lại làm dây bẫy (xem docstring lớp).
    /// </summary>
    private static readonly string[] BusinessTierAssemblyPrefixes =
    [
        "PlatformManager.Business.",
        "PlatformManager.Modules.",
    ];

    /// <summary>
    /// 🛑 <b>DANH SÁCH LIỆT KÊ TAY — rỗng thì <c>Modules_MustNotReference_OtherModules</c> XANH MÀ
    /// KHÔNG ĐO GÌ.</b> Khoá là tiền tố assembly ĐẦY ĐỦ của tầng (ví dụ
    /// <c>"PlatformManager.Business."</c>), không phải tên rút gọn — luật so tiền tố thẳng trên
    /// tên assembly, nên khoá phải khớp đúng thứ nó so.
    ///
    /// <para><b>Khi dựng <c>PlatformManager.Business.*</c> (bước 3): thêm 5 assembly của nó vào
    /// đây.</b> Với mô hình 1 khối Business đã chốt (v3), luật chéo bên dưới sẽ vẫn không có gì
    /// để bắt — CHỦ ĐÍCH, vì chỉ có một tầng. Nó chỉ trở lại có hiệu lực nếu sau này tách thành
    /// nhiều tầng nghiệp vụ độc lập thật; lúc đó khai mỗi tầng một tiền tố riêng.</para>
    /// </summary>
    private static readonly (string TierAssemblyPrefix, Assembly Assembly)[] BusinessTierAssemblies = [];

    [Fact]
    public void Core_MustNotReference_AnyModulesAssembly()
    {
        // Chặn "xanh mà không đo gì": tập quét nay được DẪN XUẤT, nên nó có thể về rỗng vì một lý
        // do mới (đổi quy ước đặt tên project Core) mà không ai chạm vào file này.
        Assert.True(CoreAssemblies.Length > 0,
            $"Không có assembly nào mang tiền tố '{CoreTierAssemblyPrefix}' trong ProductAssemblies.All ⇒ " +
            "luật này xanh mà không đo gì.");

        var violations = new List<string>();

        foreach (var coreAssembly in CoreAssemblies)
        {
            var referencedModuleAssemblies = coreAssembly.GetReferencedAssemblies()
                .Where(a => BusinessTierAssemblyPrefixes.Any(
                    prefix => a.Name!.StartsWith(prefix, StringComparison.Ordinal)))
                .Select(a => a.Name!);

            violations.AddRange(referencedModuleAssemblies.Select(n => $"{coreAssembly.GetName().Name} -> {n}"));
        }

        Assert.True(violations.Count == 0, $"Core reference Modules (KHÔNG được phép): {string.Join(", ", violations)}");
    }

    [Fact]
    public void Modules_MustNotReference_OtherModules()
    {
        var violations = new List<string>();
        var tierPrefixes = BusinessTierAssemblies.Select(m => m.TierAssemblyPrefix).Distinct().ToList();

        foreach (var (tierPrefix, assembly) in BusinessTierAssemblies)
        {
            var referencedNames = assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

            foreach (var otherTierPrefix in tierPrefixes.Where(p => p != tierPrefix))
            {
                var crossReferences = referencedNames.Where(n => n.StartsWith(otherTierPrefix, StringComparison.Ordinal));
                violations.AddRange(crossReferences.Select(n => $"{assembly.GetName().Name} -> {n}"));
            }
        }

        Assert.True(violations.Count == 0, $"Module reference module khác (KHÔNG được phép): {string.Join(", ", violations)}");
    }
}
