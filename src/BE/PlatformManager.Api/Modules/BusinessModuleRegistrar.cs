using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Business.Api.Controllers;
using PlatformManager.Business.Application;
using PlatformManager.Business.Infrastructure;
using PlatformManager.Business.Persistence;
using PlatformManager.Core.Application.Modules;

namespace PlatformManager.Api.Modules;

/// <summary>
/// Tầng NGHIỆP VỤ tự cắm vào nền tảng qua đúng seam <see cref="IModuleRegistrar"/> mà Core dùng.
///
/// <para>🛑 <b>Vì sao lớp này nằm ở HOST chứ không ở <c>Business.Infrastructure</c></b> — đây là
/// một quyết định phải giải trình, vì <c>CoreModuleRegistrar</c> thì nằm trong
/// <c>Core.Infrastructure</c>:</para>
///
/// <list type="number">
///   <item>Registrar phải khai <see cref="ApiAssembly"/>, tức phải THẤY <c>Business.Api</c>. Cạnh
///   <c>*.Infrastructure → *.Api</c> đi NGƯỢC chiều phân lớp và không có trong đồ thị phụ thuộc
///   đã khai ở doc/kien-truc-core-module.md §"Nguyên tắc phụ thuộc bắt buộc".</item>
///   <item>Nó cũng phải khai <see cref="PersistenceAssembly"/>, tức phải thấy
///   <c>Business.Persistence</c> — một cạnh nữa cũng không có trong đồ thị đó.</item>
///   <item>Host là project DUY NHẤT được phép thấy mọi tầng, và nó vốn đã phải reference cả năm
///   project <c>Business.*</c> để gọi <c>AddApplicationPart</c>. Đặt registrar ở đây không mở
///   thêm cạnh nào cả.</item>
/// </list>
///
/// <para><c>CoreModuleRegistrar</c> KHÔNG gặp vấn đề (2) vì cạnh
/// <c>Core.Infrastructure → Core.Persistence</c> đã được khai tường minh, và không gặp vấn đề (1)
/// vì <c>ApiAssembly</c> của nó là <c>null</c> — 4 controller Core còn nằm ở host. Chính docstring
/// của nó ghi nhận rằng lượt dời controller Core sang <c>Core.Api</c> sẽ vấp đúng câu hỏi
/// <i>"registrar đặt ở đâu"</i> mà lớp này vừa trả lời.</para>
///
/// <para><b>Lời hứa của seam vẫn nguyên vẹn</b>: <c>Core.*</c> không sửa một dòng nào, và dự án
/// thứ hai chỉ cần viết một lớp như lớp này trong host của nó.</para>
/// </summary>
public sealed class BusinessModuleRegistrar : IModuleRegistrar
{
    public string ModuleName => "Business";

    /// <summary>
    /// Ba lời gọi, đúng ba project có gì để đăng ký. <c>Business.Domain</c> không có DI nào
    /// (entity thuần), <c>Business.Api</c> vào bằng đường <see cref="ApiAssembly"/> chứ không
    /// bằng đường này.
    /// </summary>
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddBusinessApplication();
        services.AddBusinessPersistence();
        services.AddBusinessInfrastructure(configuration);
    }

    /// <summary>
    /// Assembly chứa <c>IEntityTypeConfiguration&lt;T&gt;</c> của 4 entity nghiệp vụ. Lấy qua
    /// <c>typeof</c> chứ không gõ chuỗi tên assembly: đổi tên/chuyển project thì kết quả tự đi
    /// theo kiểu, không thành lỗi lúc chạy. Thiếu đường này thì bảng nghiệp vụ vắng khỏi model EF
    /// và migration sinh ra sẽ THIẾU chúng — im lặng.
    /// </summary>
    public Assembly PersistenceAssembly => typeof(BusinessSeeder).Assembly;

    /// <summary>
    /// Assembly chứa controller nghiệp vụ, nạp làm <c>ApplicationPart</c>. Đây là tầng ĐẦU TIÊN
    /// của dự án đi qua nhánh khác <c>null</c> — trước lượt này nhánh đó mới chỉ chạy trên một
    /// registrar giả trong test.
    /// </summary>
    public Assembly? ApiAssembly => typeof(CriteriaController).Assembly;
}
