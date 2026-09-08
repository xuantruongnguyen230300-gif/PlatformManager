using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mọi <c>AbstractValidator&lt;T&gt;</c> trong code sản phẩm phải nằm trong một
/// assembly được <c>AddValidatorsFromAssembly(...)</c> quét — tức phải THẬT SỰ có mặt trong
/// <c>IServiceCollection</c> sau khi composition root chạy xong.
///
/// <para><b>Lỗi thật nó bắt:</b> viết validator ở một assembly không ai quét (ví dụ đặt nhầm vào
/// <c>PlatformManager.Api</c>, hoặc dựng tầng nghiệp vụ mới mà <c>RegisterServices</c> của
/// registrar quên gọi <c>AddValidatorsFromAssembly</c>). Khi đó <c>ValidationBehavior</c> phân giải ra MỘT TẬP RỖNG
/// validator cho request đó và cho qua thẳng — luật nghiệp vụ tưởng có mà không có. Không lỗi
/// biên dịch, không cảnh báo; unit test của chính validator vẫn xanh vì nó <c>new</c> validator
/// lên tay, không đi qua DI.</para>
///
/// <para><b>Cách kiểm — đối chiếu 2 nguồn ĐỘC LẬP:</b> tập kỳ vọng lấy bằng reflection trên
/// assembly sản phẩm (biết CÓ những validator nào); tập thực tế lấy từ
/// <c>ServiceCollection</c> dựng bằng đúng đường <c>Program.cs</c> đi — <c>AddModules()</c> trên
/// danh sách tầng của <c>HostModuleRegistrars</c> (biết những validator nào ĐƯỢC ĐĂNG KÝ). Nếu cả hai cùng lấy từ DI thì test sẽ chỉ hỏi DI "anh có
/// ai" rồi kiểm đúng những người đó — xanh vĩnh viễn, kể cả khi cả một assembly bị bỏ quên.</para>
///
/// <para>Không <c>BuildServiceProvider()</c>: chỉ đọc MÔ TẢ đăng ký, nên không khởi tạo gì và
/// không mở kết nối nào ⇒ vẫn thuộc ArchTests, không cần Docker.</para>
/// </summary>
public class ValidatorRegistrationTests
{
    [Fact(DisplayName = "Mọi AbstractValidator<T> đều nằm trong assembly được AddValidatorsFromAssembly quét")]
    public void EveryAbstractValidator_IsRegistered_InCompositionRoot()
    {
        var declared = DeclaredValidatorTypes();

        // Chặn "pass rỗng": reflection không tìm thấy validator nào thì mọi assert dưới vô nghĩa.
        Assert.True(declared.Count > 0,
            "Không tìm thấy AbstractValidator<T> nào trong assembly sản phẩm. Nếu repo thật sự chưa có " +
            "validator nào thì test này chưa có gì để đo (hãy xoá nó đi, đừng để nó xanh giả); nhiều khả " +
            "năng hơn là ProductAssemblies.All thiếu assembly hoặc một assembly không nạp được.");

        var registered = RegisteredValidatorTypes();

        // Chặn "pass rỗng" phía thực tế: nếu composition root không đăng ký nổi một validator nào,
        // nguyên nhân là composition root hỏng chứ không phải "không có gì để kiểm".
        Assert.True(registered.Count > 0,
            "Composition root KHÔNG đăng ký một validator nào ⇒ ValidationBehavior cho MỌI request đi qua " +
            "không kiểm gì. Nguyên nhân gần như chắc chắn: thiếu `services.AddValidatorsFromAssembly(assembly)` " +
            "trong Core/PlatformManager.Core.Application/DependencyInjection.cs (hoặc trong RegisterServices() " +
            "của registrar tầng vừa thêm).");

        var unregistered = declared
            .Where(type => !registered.Contains(type))
            .Select(type => $"{type.FullName} (assembly {type.Assembly.GetName().Name})")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(unregistered.Count == 0,
            "AbstractValidator TỒN TẠI nhưng KHÔNG được đăng ký vào DI ⇒ ValidationBehavior sẽ không " +
            "tìm thấy nó và cho request đi qua không kiểm gì: " + string.Join(", ", unregistered) +
            ". Hai nguyên nhân thường gặp: (1) validator đặt nhầm assembly — validator thuộc TẦNG " +
            "APPLICATION, không phải Api/Infrastructure, hãy chuyển nó về cạnh Command/Query của nó; " +
            "(2) tầng mới quên `services.AddValidatorsFromAssembly(assembly)` trong RegisterServices() — " +
            "xem Core/PlatformManager.Core.Application/DependencyInjection.cs làm mẫu. KHÔNG phải sửa gì " +
            "trong ArchTests: CompositionRoot.CoreServices() đọc thẳng HostModuleRegistrars, nên tầng nào " +
            "host nối vào thì test này tự thấy.");
    }

    /// <summary>
    /// Đối chứng — chứng minh phép đối chiếu KHÔNG tự động đúng. Assembly test này cũng có một
    /// <c>AbstractValidator</c> thật (<c>PipelineBehaviorTests.RequireNonEmptyValidator</c>), và
    /// nó KHÔNG nằm trong composition root sản phẩm. Nếu tập "đã đăng ký" vô tình bao trùm mọi
    /// thứ, ca này sẽ đỏ.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: validator ngoài composition root phải bị coi là CHƯA đăng ký")]
    public void ValidatorOutsideCompositionRoot_IsReported_AsUnregistered()
    {
        var registered = RegisteredValidatorTypes();
        Assert.True(registered.Count > 0,
            "Composition root KHÔNG đăng ký một validator nào — ca đối chứng này không chứng minh được gì " +
            "trên một tập rỗng (tập rỗng thì DoesNotContain luôn đúng). Sửa composition root trước.");

        var testOnlyValidator = typeof(PipelineBehaviorTests.RequireNonEmptyValidator);
        Assert.True(IsValidator(testOnlyValidator),
            "Bộ nhận diện validator không nhận ra một AbstractValidator<T> có thật ⇒ tập kỳ vọng của " +
            "test chính đang rỗng một cách âm thầm.");

        Assert.DoesNotContain(testOnlyValidator, registered);
    }

    // ── Hai nguồn ────────────────────────────────────────────────────────

    private static List<Type> DeclaredValidatorTypes() =>
        [.. ProductAssemblies.AllLoadableTypes()
            .Where(IsValidator)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];

    /// <summary>
    /// Lấy theo <c>ImplementationType</c> thay vì theo <c>ServiceType == IValidator&lt;T&gt;</c>:
    /// FluentValidation có thể đăng ký thêm/đổi hình dạng ServiceType giữa các phiên bản, còn
    /// "kiểu hiện thực là một AbstractValidator" thì không đổi.
    /// </summary>
    private static HashSet<Type> RegisteredValidatorTypes() =>
        [.. CompositionRoot.CoreServices()
            .Select(descriptor => descriptor.ImplementationType)
            .Where(type => type is not null && IsValidator(type))
            .Select(type => type!)];

    private static bool IsValidator(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            return false;

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                return true;
        }

        return false;
    }
}
