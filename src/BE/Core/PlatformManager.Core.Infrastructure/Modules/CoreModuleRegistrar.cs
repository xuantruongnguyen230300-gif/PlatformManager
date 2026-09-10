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
    /// <c>null</c> — <b>không phải</b> vì Core thiếu project <c>*.Api</c>: <c>Core.Api</c> đã dựng
    /// 2026-09-09 (Q8). Lý do thật là nó CHƯA CHỨA CONTROLLER NÀO — hôm nay trong đó chỉ có
    /// <c>ApiControllerBase</c>, còn 4 controller Core vẫn nằm ở project host, nơi MVC vốn đã tự
    /// quét. Nộp một assembly không có controller làm <c>ApplicationPart</c> chẳng thêm được gì
    /// vào bảng route; nó chỉ làm seam trông như đã hoàn tất trong khi việc dời controller chưa làm.
    ///
    /// <para><b>Sửa 2026-09-09 — điều kiện kích hoạt.</b> Câu trước ở đây dặn <i>"khi Core.Api
    /// được tách ra thì đổi dòng này"</i>. Điều kiện đó ĐÃ XẢY RA mà làm theo lúc này lại sai, nên
    /// nó được thay bằng một điều kiện không đọc nhầm được: <b>trả về assembly <c>Core.Api</c>
    /// CÙNG LƯỢT dời 4 controller Core sang đó, không sớm hơn.</b> Đổi trước lượt đó thì host mất
    /// chỗ quét controller mà registrar chưa mang lại được cái nào.</para>
    ///
    /// <para>⚠️ Lượt đó còn phải giải một việc chưa có lời giải ở đây: registrar này sống trong
    /// <c>Core.Infrastructure</c>, mà <c>Infrastructure → Api</c> là cạnh ngược chiều phân lớp.
    /// Nên "đổi dòng này" KHÔNG phải một sửa đổi một dòng — nó kéo theo quyết định registrar (hoặc
    /// một kiểu mốc của <c>Core.Api</c>) đặt ở đâu. Ghi ra để lượt sau không tưởng là việc nhỏ.</para>
    ///
    /// <para>Nhánh <c>null</c> vì thế không phải nhánh giả định: nó chạy thật mỗi lần khởi
    /// động, nên hành vi "bỏ qua registrar không có controller" được kiểm chứng liên tục.</para>
    /// </summary>
    public Assembly? ApiAssembly => null;
}
