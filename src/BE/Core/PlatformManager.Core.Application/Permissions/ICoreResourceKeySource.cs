namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Một permission-key của dự án và nhãn hiển thị của nó.
///
/// <para><b><c>DisplayName</c> là chữ HIỂN THỊ, và đó là nợ đã biết.</b> Theo hướng đã chốt "FE sở
/// hữu câu chữ", trường này đáng lẽ là một MÃ để FE tra bảng dịch, không phải câu tiếng Việt dựng
/// sẵn ở BE (cùng lý lẽ với <c>businessCode</c>, xem
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4). Lượt tách 2026-09-03 CỐ Ý không đụng vào
/// đó: đổi kiểu giá trị là đổi hợp đồng <c>GET /api/admin/permissions/resources</c>
/// (doc/contracts/permissions.md CONTRACT PERM-2) và kéo theo FE — hai việc khác nhau thì làm
/// thành hai lượt. Điều lượt này ĐÃ đạt được là chuyển câu tiếng Việt đó ra khỏi Core, nên khi
/// i18n tới thì chỉ còn một chỗ ở host phải sửa.</para>
/// </summary>
/// <param name="Key">Khoá gắn vào <c>[RequirePermission]</c> và lưu ở cột <c>ResourceKey</c> của
/// bảng <c>RolePermissions</c>. So khớp ORDINAL ở mọi nơi — khác hoa/thường là hai khoá khác nhau.</param>
/// <param name="DisplayName">Nhãn cho màn hình phân quyền. KHÔNG lưu DB.</param>
public sealed record ResourceKeyDefinition(string Key, string DisplayName);

/// <summary>
/// Seam "danh mục permission-key của dự án" — <b>host cung cấp, Core tiêu thụ</b>. Cùng ranh giới
/// và cùng khuôn với <c>ICoreMenuSeedSource</c> / <c>ICoreBootstrapAccountSource</c>: <b>Core giữ
/// CƠ CHẾ, dự án cung cấp DỮ LIỆU</b> (doc/kien-truc-core-module.md).
///
/// <para><b>Vì sao tồn tại</b> (tách 2026-09-03): trước đó Core có lớp <c>ResourceKeys</c> mang
/// một danh sách ĐÓNG các key, một bảng nhãn tiếng Việt, và một docstring ra chỉ thị "module
/// nghiệp vụ mới khai key của mình Ở ĐÂY". Tức là Core chủ động bảo dự án ghi dữ liệu vào trong
/// Core — chiều phụ thuộc ngược hẳn với ranh giới đã chốt, và là thứ khiến CoreBase không cắm
/// được vào dự án thứ hai nếu không mổ lại. Ba đường dùng nó (ma trận GET, validator của PUT,
/// seeder) đều là CƠ CHẾ và ở lại Core; chỉ danh sách key + nhãn đi ra host.</para>
///
/// <para><b>Cái gì KHÔNG đi qua seam này</b> — phần Core giữ vì nó là cơ chế, không phải dữ liệu:
/// phép kiểm quyền deny-by-default và đường bỏ qua của SuperAdmin (<c>RequirePermissionFilter</c>);
/// luật "PUT là ghi đè toàn bộ nên payload phải phủ đủ danh mục"; việc seed cấp đủ danh mục cho
/// Admin + User; và tập vai (<c>Roles.All</c>) — vai là khái niệm của Core, không phải của dự án.</para>
///
/// <para><b>Phải đăng ký ở host; Core cố ý KHÔNG có hiện thực mặc định.</b> Cùng lý do đã ghi ở
/// <c>ICoreMenuSeedSource</c>: một bản mặc định trả danh sách rỗng sẽ biến "quên đăng ký" thành
/// một màn hình phân quyền trống rỗng và một bảng <c>RolePermissions</c> không dòng nào — mà
/// <c>RequirePermissionFilter</c> thì deny-by-default, nên hậu quả thật là mọi endpoint có
/// <c>[RequirePermission]</c> trả 403 cho tất cả trừ SuperAdmin, không kèm lỗi nào giải thích.
/// Thiếu đăng ký ⇒ DI không phân giải được ⇒ hỏng ngay và hỏng ồn ào.</para>
/// </summary>
public interface ICoreResourceKeySource
{
    /// <summary>
    /// Toàn bộ permission-key của dự án. Thứ tự khai được giữ nguyên ra tới response của
    /// <c>GET /api/admin/permissions/resources</c> — đó là thứ tự các dòng trên màn hình phân
    /// quyền, nên sắp xếp có chủ đích ở host chứ Core không sắp lại.
    ///
    /// <para>Đồng bộ có chủ đích — đây là bảng hằng số của một dự án, không phải thứ đọc từ I/O
    /// (cùng lý do đã ghi ở <c>ICoreMenuSeedSource.GetMenuItems</c>).</para>
    /// </summary>
    IReadOnlyCollection<ResourceKeyDefinition> GetResourceKeys();
}

/// <summary>
/// Phần dùng chung của ba đường tiêu thụ <see cref="ICoreResourceKeySource"/> (ma trận GET,
/// validator của PUT, seeder).
/// </summary>
public static class ResourceKeySourceExtensions
{
    /// <summary>
    /// Danh mục đã kiểm tính duy nhất, giữ NGUYÊN thứ tự host khai.
    ///
    /// <para><b>Vì sao ném thay vì lặng lẽ gộp trùng.</b> Bản Core cũ giữ nhãn trong một
    /// <c>Dictionary</c> khởi tạo tĩnh, nên một key khai hai lần là lỗi ngay lần dùng đầu — tính
    /// chất đó mất đi khi danh sách chuyển sang host, và mất im lặng: key trùng sẽ thành hai dòng
    /// giống hệt trên màn hình phân quyền, còn <c>PUT</c> thì bị chính validator chống trùng của
    /// nó từ chối vĩnh viễn vì payload buộc phải phủ đủ danh mục. Ném ở đây trả lại đúng mức ồn
    /// ào cũ.</para>
    /// </summary>
    public static IReadOnlyList<ResourceKeyDefinition> Catalog(this ICoreResourceKeySource source)
    {
        var definitions = source.GetResourceKeys().ToList();

        var duplicates = definitions
            .GroupBy(definition => definition.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        return duplicates.Count == 0
            ? definitions
            : throw new InvalidOperationException(
                $"Host khai trùng permission-key: {string.Join(", ", duplicates)}. Mỗi key chỉ được " +
                $"xuất hiện đúng một lần trong {nameof(ICoreResourceKeySource)}.{nameof(ICoreResourceKeySource.GetResourceKeys)}().");
    }

    /// <summary>Chỉ tập khoá, giữ nguyên thứ tự khai.</summary>
    public static IReadOnlyList<string> Keys(this ICoreResourceKeySource source) =>
        [.. source.Catalog().Select(definition => definition.Key)];
}
