using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Modules;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Core.Infrastructure.Modules;

/// <summary>
/// Core tự cắm vào nền tảng qua ĐÚNG seam mà tầng nghiệp vụ dùng — không có đường riêng cho
/// Core và đường khác cho tầng khác.
///
/// <para><b>Vì sao điều đó quan trọng hơn nó nghe:</b> nếu Core được host gọi thẳng
/// (<c>AddCoreModule</c>) còn tầng nghiệp vụ mới đi qua <see cref="IModuleRegistrar"/>, thì
/// đường registrar KHÔNG chạy lần nào cho tới khi tầng nghiệp vụ đầu tiên xuất hiện — và một
/// đường code không ai chạy là đường code không ai biết là đã hỏng. Cho Core đi chung đường
/// biến seam thành thứ được thực thi mỗi lần ứng dụng khởi động.</para>
/// </summary>
/// <param name="requireBootstrapOptions">
/// Chuyển thẳng xuống <see cref="DependencyInjection.AddCoreModule"/> — chỉ đường chạy lệnh
/// seed mới truyền <c>true</c> (lý do đầy đủ ghi tại đó). Tham số này ở HÀM DỰNG chứ không ở
/// <see cref="RegisterServices"/> vì hợp đồng <see cref="IModuleRegistrar"/> chỉ nhận
/// <c>IServiceCollection</c> + <c>IConfiguration</c>; nhét thêm cờ riêng của Core vào hợp đồng
/// chung sẽ bắt mọi tầng nghiệp vụ của mọi dự án mang theo một tham số không liên quan tới nó.
/// </param>
public sealed class CoreModuleRegistrar(bool requireBootstrapOptions = false) : IModuleRegistrar
{
    public string ModuleName => "Core";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddCoreModule(configuration, requireBootstrapOptions);

    /// <summary>
    /// Assembly CHÍNH NÓ (Core.Infrastructure) — nơi sống của AppUserConfiguration/
    /// AppRoleConfiguration/SysMenuConfiguration/SysMenuRoleConfiguration/
    /// RolePermissionConfiguration. Lấy qua <c>typeof</c> chứ không gõ chuỗi tên assembly: đổi
    /// tên project thì lỗi biên dịch, không phải lỗi lúc chạy.
    /// </summary>
    public Assembly PersistenceAssembly => typeof(PlatformManagerDbContext).Assembly;

    /// <summary>
    /// <c>null</c> — Core CHƯA có project <c>*.Api</c> riêng (xem bảng "có thật hôm nay → sẽ
    /// thành" ở doc/kien-truc-core-module.md): 4 controller Core hiện nằm trong project host, mà
    /// MVC vốn đã tự quét assembly của host nên không có gì để nạp thêm. Khi <c>Core.Api</c>
    /// được tách ra thì đổi dòng này thành assembly đó — KHÔNG phải sửa host.
    ///
    /// <para>Nhánh <c>null</c> vì thế không phải nhánh giả định: nó chạy thật mỗi lần khởi
    /// động, nên hành vi "bỏ qua registrar không có controller" được kiểm chứng liên tục.</para>
    /// </summary>
    public Assembly? ApiAssembly => null;
}
