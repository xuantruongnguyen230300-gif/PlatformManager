using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Core.UnitTests.Permissions;

/// <summary>
/// Danh mục permission-key dựng tay cho unit test.
///
/// <para><b>Vì sao có (2026-09-03):</b> danh mục key đã chuyển từ Core ra host qua
/// <see cref="ICoreResourceKeySource"/>, và project này CỐ Ý không tham chiếu
/// <c>PlatformManager.Api</c> (chỉ IntegrationTests mới được — xem csproj của cả hai). Nếu test
/// đơn vị mượn danh mục thật của host thì mọi lần dự án thêm/bớt key sẽ làm đỏ những test không
/// nói gì về danh mục, và tệ hơn: chúng sẽ ngừng chạy nếu danh mục thật có lúc rỗng.</para>
///
/// <para>Khoá dùng ở đây CỐ Ý không phải khoá thật của dự án — các test dưới đây khẳng định hành
/// vi của validator (trùng lặp, thiếu roles, khác hoa/thường), không khẳng định gì về dự án nào
/// khai key gì.</para>
/// </summary>
internal sealed class FakeResourceKeySource : ICoreResourceKeySource
{
    public const string Key = "unittest.manage";

    private static readonly ResourceKeyDefinition[] Definitions = [new(Key, "Khoá dựng cho unit test")];

    public IReadOnlyCollection<ResourceKeyDefinition> GetResourceKeys() => Definitions;
}
