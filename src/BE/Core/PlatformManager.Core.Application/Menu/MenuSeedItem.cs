namespace PlatformManager.Core.Application.Menu;

/// <summary>
/// Định nghĩa MỘT mục menu do <b>host</b> khai qua <see cref="ICoreMenuSeedSource"/> — dữ liệu
/// thuần, không hành vi. Core nhận danh sách này rồi lo phần CƠ CHẾ (upsert theo
/// <paramref name="Code"/>, hồi sinh dòng đã xoá mềm, gán <c>SysMenuRole</c>); Core KHÔNG biết
/// và không được biết mục nào tồn tại.
/// </summary>
/// <param name="Code">
/// Khoá ổn định — <b>danh tính nghiệp vụ</b> của mục menu. Seeder tìm dòng cũ theo mã này, nên
/// đổi <c>Code</c> nghĩa là tạo một mục MỚI chứ không phải đổi tên mục cũ. Phải duy nhất trong
/// toàn danh sách (seeder kiểm và ném nếu trùng).
///
/// <para>⚠️ <b>Seeder là "chèn-hoặc-hồi-sinh", KHÔNG phải "upsert" đầy đủ</b> (làm rõ 2026-09-02
/// sau core-review): gặp dòng ĐANG SỐNG cùng <c>Code</c> thì nó trả về nguyên trạng, <b>không</b>
/// cập nhật <c>Name</c>/<c>Route</c>/<c>Icon</c>/<c>ParentCode</c>/<c>DisplayOrder</c>. Đổi mấy
/// giá trị đó ở host rồi chạy lại <c>--seed</c> sẽ <b>không có tác dụng</b> trên DB đã seed.</para>
///
/// <para>Đây là hành vi CÓ CHỦ ĐÍCH: nó bảo vệ chỉnh sửa thủ công trên DB thật khỏi bị một lần
/// chạy seed ghi đè. Muốn đổi thật thì sửa thẳng trong DB, hoặc xoá mềm rồi seed lại — nhánh hồi
/// sinh <b>có</b> ghi lại đủ 5 giá trị.</para>
/// </param>
/// <param name="Name">Nhãn hiển thị.</param>
/// <param name="Route">
/// Route phía FE. <c>null</c> cho mục cha (chỉ toggle expand/collapse, không điều hướng) — xem
/// doc/contracts/meta-menu.md.
/// </param>
/// <param name="Icon">
/// Class CSS icon, trả THẲNG cho FE chứ không qua bảng khoá trừu tượng nào. <c>null</c> ⇒ FE tự
/// fallback. Bộ icon nào là quyết định của host, không phải của Core.
/// </param>
/// <param name="ParentCode">
/// <b>Mã</b> của mục cha, KHÔNG phải <c>Id</c>. Host không thể khai <c>Id</c>: với database mới,
/// <c>Id</c> chỉ sinh ra lúc seeder tạo entity; với database đã seed lần trước, <c>Id</c> là giá
/// trị đang nằm sẵn trong bảng. Seeder tự ánh xạ <c>ParentCode → Id</c> sau khi upsert cha.
/// <c>null</c> = mục gốc.
/// </param>
/// <param name="DisplayOrder">Thứ tự hiển thị trong cùng một cấp.</param>
/// <param name="Roles">
/// Tên các role được thấy mục này (đối chiếu <c>AspNetRoles.Name</c>). <b>Rỗng = mở cho MỌI user
/// đã đăng nhập</b> — đó là ý nghĩa của "không có dòng nào trong <c>SysMenuRole</c>", KHÔNG phải
/// "không ai thấy". Không có giá trị mặc định là chủ đích: viết <c>[]</c> tường minh thì lựa chọn
/// mở-cho-mọi-người luôn là một quyết định, không phải một chỗ quên điền.
/// </param>
public sealed record MenuSeedItem(
    string Code,
    string Name,
    string? Route,
    string? Icon,
    string? ParentCode,
    int DisplayOrder,
    IReadOnlyCollection<string> Roles);
