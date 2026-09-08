using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mỗi <c>IPipelineBehavior&lt;,&gt;</c> trong code sản phẩm phải được đăng ký ĐÚNG
/// MỘT LẦN trong composition root.
///
/// <para><b>Hai lỗi thật nó bắt, hai chiều ngược nhau:</b></para>
/// <list type="number">
///   <item><b>Đăng ký HAI lần.</b> MediatR gọi behavior theo đúng số lần đăng ký, nên mọi request
///   chạy qua behavior đó hai lượt. Với <c>ValidationBehavior</c>, thông điệp lỗi trả về client
///   nhân đôi; với <c>ExceptionHandlingBehavior</c>, mỗi exception ghi 2 dòng log cùng traceId
///   nên đọc log tưởng có 2 sự cố. Cùng họ với lỗi <c>ValidationContext</c> dùng chung vừa vá:
///   không hỏng ngay, chỉ làm mọi thứ sai gấp đôi. Kịch bản gây ra nó rất dễ xảy ra và không có
///   gì cản: <c>AddCoreApplication()</c> đã đăng ký 2 behavior open-generic dùng chung cho TOÀN
///   hệ thống, nên một tầng mới "đăng ký behavior cho phần mình" trong <c>RegisterServices()</c> là đăng ký
///   lần thứ hai — xem chú thích ở Core.Application/DependencyInjection.cs.</item>
///   <item><b>Viết behavior rồi quên đăng ký.</b> Nó nằm im trong assembly, không lỗi biên dịch,
///   và không bao giờ nằm trên đường đi của request nào.</item>
/// </list>
///
/// <para><b>Quan hệ với <see cref="PipelineBehaviorTests"/>:</b> file đó kiểm HÀNH VI của 2
/// behavior bằng một mini-pipeline TỰ DỰNG (nó tự <c>AddTransient</c> 2 dòng, chép tay thứ tự từ
/// <c>DependencyInjection.cs</c>). Vì tự dựng nên nó KHÔNG thể phát hiện composition root thật
/// đăng ký thiếu, thừa, hay sai thứ tự — chép tay không phải là đo. File này đo composition root
/// THẬT.</para>
/// </summary>
public class PipelineBehaviorRegistrationTests
{
    [Fact(DisplayName = "Mọi IPipelineBehavior<,> được đăng ký ĐÚNG MỘT LẦN trong composition root")]
    public void EveryPipelineBehavior_IsRegistered_ExactlyOnce()
    {
        var declared = DeclaredBehaviorTypes();

        // Chặn "pass rỗng" ở nguồn kỳ vọng.
        Assert.True(declared.Count > 0,
            "Không tìm thấy IPipelineBehavior nào trong assembly sản phẩm ⇒ test này không đo gì. " +
            "Nhiều khả năng ProductAssemblies.All thiếu assembly hoặc một assembly không nạp được.");

        var registrationCounts = BehaviorRegistrationCounts();

        // Chặn "pass rỗng" phía thực tế: composition root không đăng ký behavior nào thì nguyên
        // nhân là nó hỏng, không phải "không có gì để kiểm".
        Assert.True(registrationCounts.Count > 0,
            "Composition root KHÔNG đăng ký một IPipelineBehavior nào ⇒ mọi request đi thẳng vào handler, " +
            "không qua validation lẫn dịch DomainException. Xem AddCoreApplication() ở " +
            "Core/PlatformManager.Core.Application/DependencyInjection.cs.");

        var missing = declared
            .Where(type => !registrationCounts.ContainsKey(type))
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            "IPipelineBehavior TỒN TẠI nhưng KHÔNG được đăng ký ⇒ không request nào chạy qua nó: " +
            string.Join(", ", missing) + ". Cách sửa: thêm `services.AddTransient(typeof(IPipelineBehavior<,>), " +
            "typeof(TênBehavior<,>));` vào Core/PlatformManager.Core.Application/DependencyInjection.cs — " +
            "ĐÚNG MỘT chỗ đó, vì behavior là open-generic nên nó áp cho mọi request của mọi module. " +
            "Thứ tự đăng ký = thứ tự bọc (khai trước nằm ngoài cùng). Nếu behavior này CỐ Ý chưa dùng " +
            "thì xoá nó đi.");

        var duplicated = registrationCounts
            .Where(pair => pair.Value > 1)
            .Select(pair => $"{pair.Key.FullName} ×{pair.Value}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(duplicated.Count == 0,
            "IPipelineBehavior đăng ký NHIỀU HƠN MỘT LẦN ⇒ MediatR chạy nó đúng số lần đó cho MỌI " +
            "request: " + string.Join(", ", duplicated) + ". Hậu quả im lặng: thông điệp lỗi validation " +
            "nhân đôi, mỗi exception ghi 2 dòng log cùng traceId. Cách sửa: 2 behavior dùng chung chỉ " +
            "được đăng ký tại Core/PlatformManager.Core.Application/DependencyInjection.cs — RegisterServices() " +
            "của registrar tầng nghiệp vụ KHÔNG đăng ký lại.");
    }

    /// <summary>
    /// Đối chứng — chứng minh phép đếm THẬT SỰ đếm, chứ không phải luôn trả 1 (nếu nó luôn trả 1
    /// thì nhánh "đăng ký hai lần" của test chính xanh vĩnh viễn, và đó đúng là lỗi nguy hiểm hơn
    /// trong hai lỗi mà file này canh).
    ///
    /// <para>Dựng trên <c>ServiceCollection</c> RỖNG, không đụng composition root thật — nhờ đó
    /// ca đối chứng này giữ nguyên ý nghĩa kể cả khi composition root đang hỏng, và không đỏ lây
    /// theo test chính.</para>
    /// </summary>
    [Fact(DisplayName = "Đối chứng: phép đếm phân biệt được 1 lần và 2 lần đăng ký")]
    public void Counter_Distinguishes_Single_From_DoubleRegistration()
    {
        var declared = DeclaredBehaviorTypes();
        Assert.True(declared.Count > 0,
            "Không tìm thấy IPipelineBehavior nào trong assembly sản phẩm — không có gì để làm mẫu đếm. " +
            "Nhiều khả năng ProductAssemblies.All thiếu assembly hoặc một assembly không nạp được.");

        var probe = declared[0];

        var once = new ServiceCollection();
        once.AddTransient(typeof(IPipelineBehavior<,>), probe);
        Assert.Equal(1, CountBehaviorRegistrations(once)[probe]);

        var twice = new ServiceCollection();
        twice.AddTransient(typeof(IPipelineBehavior<,>), probe);
        twice.AddTransient(typeof(IPipelineBehavior<,>), probe);
        Assert.Equal(2, CountBehaviorRegistrations(twice)[probe]);
    }

    // ── Hai nguồn ────────────────────────────────────────────────────────

    /// <summary>Nguồn kỳ vọng: REFLECTION, độc lập với DI.</summary>
    private static List<Type> DeclaredBehaviorTypes() =>
        [.. ProductAssemblies.AllLoadableTypes()
            .Where(type => type.IsClass
                        && !type.IsAbstract
                        && type.GetInterfaces().Any(@interface =>
                            @interface.IsGenericType
                            && @interface.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];

    private static Dictionary<Type, int> BehaviorRegistrationCounts() =>
        CountBehaviorRegistrations(CompositionRoot.CoreServices());

    /// <summary>
    /// Đếm theo <c>ImplementationType</c> của các đăng ký có <c>ServiceType</c> là
    /// <c>IPipelineBehavior&lt;,&gt;</c> — đúng hình dạng mà MediatR phân giải lúc dựng pipeline.
    /// </summary>
    private static Dictionary<Type, int> CountBehaviorRegistrations(IServiceCollection services) =>
        services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>)
                              && descriptor.ImplementationType is not null)
            .GroupBy(descriptor => descriptor.ImplementationType!)
            .ToDictionary(group => group.Key, group => group.Count());
}
