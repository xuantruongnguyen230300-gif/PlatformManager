using Microsoft.Extensions.Hosting;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Core.Infrastructure.Permissions;

/// <summary>
/// Chạy <see cref="ResourceKeySourceExtensions.Catalog"/> <b>một lần lúc khởi động</b>, để host khai
/// danh mục permission-key sai thì tiến trình KHÔNG boot.
///
/// <para><b>Vấn đề nó sửa (2026-09-09).</b> Toàn bộ phép kiểm danh mục — key trùng, và ba ca sai của
/// <see cref="ResourceKeyDefinition.SeedRoles"/> (rỗng · tên vai lạ · khai <c>SuperAdmin</c>) — nằm
/// bên trong <c>Catalog()</c>, mà <c>Catalog()</c> chỉ được gọi khi có người DÙNG danh mục: ma trận
/// <c>GET</c>, validator của <c>PUT</c>, và seeder. Nghĩa là một host khai sai vẫn khởi động bình
/// thường, phục vụ mọi request bình thường, rồi ném <see cref="InvalidOperationException"/> ⇒ HTTP
/// 500 vào đúng lúc ai đó mở màn hình Phân quyền. Người vận hành nhận một lỗi 500 ở một màn hình,
/// còn nguyên nhân thì nằm ở một dòng cấu hình đã sai từ lúc deploy.</para>
///
/// <para><b>Nó chỏi với chính thứ Core đã tuyên bố.</b> Docstring của
/// <see cref="ICoreResourceKeySource"/> giải thích vì sao Core cố ý KHÔNG có hiện thực mặc định:
/// <i>"Thiếu đăng ký ⇒ DI không phân giải được ⇒ hỏng ngay và hỏng ồn ào"</i>. Đăng ký một seam
/// SAI thì đáng ra phải hỏng cùng kiểu — và cùng khuôn fail-fast mà
/// doc/huong_dan/quy-uoc/be-architecture.md áp cho mọi <c>IOptions&lt;T&gt;</c>
/// (<c>ValidateDataAnnotations().ValidateOnStart()</c>).</para>
///
/// <para><b>Vì sao là <see cref="IHostedService"/> chứ không phải một cơ chế riêng:</b>
/// <c>ValidateOnStart()</c> của <c>IOptions&lt;T&gt;</c> cũng chính là một hosted service. Dùng đúng
/// cơ chế đó cho danh mục key nghĩa là hai loại cấu hình sai hỏng theo cùng một cách, ở cùng một
/// thời điểm — không phải hai đường riêng mà người đọc phải nhớ cả hai.</para>
///
/// <para><b>Guard trong <c>Catalog()</c> GIỮ NGUYÊN làm lưới thứ hai.</b> Không gỡ đi sau khi có
/// lưới này: <c>Catalog()</c> còn được gọi trên những đường KHÔNG đi qua host — nổi bật là lệnh
/// <c>--seed</c> và các test dựng danh mục giả — và một hiện thực seam trả danh sách khác nhau giữa
/// hai lần gọi vẫn phải bị bắt.</para>
///
/// <para><b>Cố ý KHÔNG nhớ kết quả.</b> Lượt này chỉ sửa THỜI ĐIỂM kiểm, không đổi số lần dựng danh
/// mục ở đường phục vụ request. Nhớ kết quả là một thay đổi khác loại — nó khoá vòng đời của seam
/// thành "gọi đúng một lần", tức đổi hợp đồng chứ không tối ưu hợp đồng cũ — và luật của repo là
/// không thêm tầng nhớ nào khi chưa có số đo (doc/huong_dan/wiki-core/be/11-performance-caching.md
/// §Cache). Danh mục hôm nay là một mảng hằng số một phần tử ở host; chưa có gì để đo.</para>
/// </summary>
internal sealed class ResourceKeyCatalogStartupValidator(ICoreResourceKeySource resourceKeySource)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Catalog() ném với thông điệp đã nêu đích danh key/vai sai — không bọc thêm try/catch,
        // vì bọc lại chỉ làm thông điệp đó xa thêm một tầng.
        _ = resourceKeySource.Catalog();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
