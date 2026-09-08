using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformManager.Core.Application.Modules;

/// <summary>
/// Seam để một TẦNG tự cắm phần của mình vào nền tảng — Core khai hợp đồng, mỗi tầng tự hiện
/// thực, host gom mọi hiện thực rồi gọi một lượt. Hợp đồng CHỐT 2026-08-23, xem
/// doc/kien-truc-core-module.md §IModuleRegistrar.
///
/// <para><b>Vì sao interface này ở Core.Application chứ không ở nơi khác:</b> Core không được
/// phép tham chiếu tầng nghiệp vụ nào (ArchTest <c>Core_MustNotReference_AnyModulesAssembly</c>
/// + <c>CoreSource_MustNotContain_BusinessNameStringLiteral</c> cưỡng chế). Đảo phụ thuộc qua
/// một interface là cách DUY NHẤT để Core duyệt được mọi tầng mà không biết tên tầng nào. Dự án
/// thứ hai vì thế chỉ phải viết một hiện thực của interface này, KHÔNG phải mổ vào Core.</para>
///
/// <para><b>Ba đường mà một tầng cần, và đủ ba:</b> dịch vụ vào DI
/// (<see cref="RegisterServices"/>), cấu hình EF vào model (<see cref="PersistenceAssembly"/>),
/// controller vào MVC (<see cref="ApiAssembly"/>). Thiếu bất kỳ đường nào thì tầng nghiệp vụ
/// vẫn phải sửa vào trong Core để chạy được — đúng thứ seam này sinh ra để chặn.</para>
///
/// <para><b>Core CŨNG hiện thực interface này</b> (<c>CoreModuleRegistrar</c> ở
/// Core.Infrastructure). Cố ý: một đường code duy nhất cho mọi tầng thì đường đó được chạy mỗi
/// lần ứng dụng khởi động, nên nó không thể mục ruỗng trong im lặng. Một seam chỉ dành cho
/// "người khác" là seam không ai chạy cho tới lần đầu có người khác — và lần đó là lúc tệ nhất
/// để phát hiện nó hỏng.</para>
/// </summary>
public interface IModuleRegistrar
{
    /// <summary>
    /// Tên tầng, dùng cho thông điệp lỗi và cho luật "không hai tầng trùng tên". KHÔNG dùng để
    /// rẽ nhánh: Core không được biết tên tầng nghiệp vụ nào (xem
    /// <c>CoreMustNotKnowBusinessNameTests</c>), nên mọi thứ khác biệt giữa các tầng phải đi qua
    /// 3 thành viên còn lại của hợp đồng này.
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Đăng ký MỌI dịch vụ của tầng vào DI (MediatR/FluentValidation của assembly mình,
    /// repository, seam riêng...). Chạy lúc dựng <c>ServiceCollection</c>, KHÔNG được resolve
    /// dịch vụ nào.
    /// </summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Assembly chứa <c>IEntityTypeConfiguration&lt;T&gt;</c> của tầng, để
    /// <c>PlatformManagerDbContext.OnModelCreating</c> quét. Bắt buộc có (không nullable): mọi
    /// tầng đều có ít nhất một entity, và một tầng "quên" khai sẽ mất bảng của mình khỏi model
    /// trong im lặng — kiểu hỏng mà finding F6 đã trả giá một lần.
    /// </summary>
    Assembly PersistenceAssembly { get; }

    /// <summary>
    /// Assembly chứa controller của tầng, để host nạp làm ApplicationPart.
    /// <c>null</c> khi tầng không có controller riêng — hôm nay Core rơi đúng vào ca đó
    /// (controller Core còn nằm trong project host, và MVC vốn đã tự quét assembly host).
    /// </summary>
    Assembly? ApiAssembly { get; }
}
