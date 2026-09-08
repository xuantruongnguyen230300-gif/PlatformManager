using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// Đọc danh mục permission-key ĐÚNG NHƯ host đang đăng ký, qua DI của host thật.
///
/// <para><b>Vì sao không gọi thẳng <c>AppResourceKeySource</c></b> (thêm 2026-09-03 khi danh mục
/// chuyển từ Core ra host, xem <see cref="ICoreResourceKeySource"/>): dựng hiện thực bằng
/// <c>new</c> ở test sẽ vẫn xanh kể cả khi <c>Program.cs</c> quên dòng đăng ký — tức là bỏ sót
/// đúng cách hỏng mà seam này sinh ra để làm ồn ào. Đi qua <c>IServiceProvider</c> thì "host có
/// đăng ký không" nằm trong phạm vi được kiểm.</para>
///
/// <para>Không hằng số hoá danh sách ở đây: dự án thêm key thì mọi test dùng helper này tự phủ,
/// và không có bản chép thứ hai để lệch.</para>
/// </summary>
internal static class HostResourceKeys
{
    public static IReadOnlyList<ResourceKeyDefinition> CatalogFrom(IServiceProvider services) =>
        services.GetRequiredService<ICoreResourceKeySource>().Catalog();

    public static IReadOnlyList<string> AllFrom(IServiceProvider services) =>
        services.GetRequiredService<ICoreResourceKeySource>().Keys();
}
