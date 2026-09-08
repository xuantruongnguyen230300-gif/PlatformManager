using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Menu;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>PUT /api/admin/permissions — ghi đè TOÀN BỘ SysMenuRole theo ma trận gửi lên.
/// Controller gate [Authorize(Roles="SuperAdmin")] — Admin thường KHÔNG được sửa (rủi ro leo
/// thang quyền, xem doc/ke-hoach-xay-lai-corebase.md).
///
/// <para><b><paramref name="Version"/> BẮT BUỘC từ 2026-08-31</b> — token nhận được ở <c>GET</c>,
/// gửi lại nguyên văn. Thiếu hoặc lệch ⇒ <b>409</b> và KHÔNG ghi gì (quyết định người dùng
/// 2026-08-30, doc/contracts/permissions.md). Khai <c>= null</c> ở đây là để thân request thiếu
/// khoá này DESERIALIZE ĐƯỢC rồi bị handler từ chối tử tế bằng 409 — không phải để nó thành tuỳ
/// chọn; không có default thì System.Text.Json ném và client nhận 400 với thông điệp của
/// serializer thay vì lỗi nghiệp vụ đúng nghĩa.</para></summary>
public sealed record UpdatePermissionMatrixCommand(
    IReadOnlyCollection<PermissionMatrixEntryDto> Entries, string? Version = null) : ICommand<bool>;

/// <summary>
/// <para><b>Rule cấp COLLECTION cho <c>sysMenuId</c> trùng (sửa 2026-08-29)</b> — cùng một lỗi,
/// cùng một lý do như <see cref="UpdateResourcePermissionMatrixValidator"/>: handler dựng
/// <c>Dictionary</c> bằng <c>ToDictionary(e => e.SysMenuId, …)</c>, và <c>ToDictionary</c> ném
/// <see cref="ArgumentException"/> khi gặp key trùng. Loại exception này không có nhánh xử lý
/// riêng nên client nhận <b>500</b>, trong khi doc/contracts/permissions.md §Lỗi hứa <b>400</b>.
/// Đọc docstring của <see cref="UpdateResourcePermissionMatrixValidator"/> để biết vì sao chặn ở
/// validator chứ không bọc try-catch quanh <c>ToDictionary</c>.</para>
///
/// <para>Ca trùng ở đây KHÔNG hiếm như nhìn qua: FE gửi lại toàn bộ <c>rows</c> của ma trận (bắt
/// buộc, vì đây là lệnh ghi đè toàn bộ), nên một lỗi ghép mảng — ví dụ nối kết quả của hai lần
/// tải, hoặc thêm dòng vừa sửa vào danh sách cũ thay vì thay thế — sinh ra đúng payload này.</para>
/// </summary>
public sealed class UpdatePermissionMatrixValidator : AbstractValidator<UpdatePermissionMatrixCommand>
{
    public UpdatePermissionMatrixValidator()
    {
        // Cascade.Stop: `entries` null (body `{}`) mà chạy tiếp xuống Must sẽ ném
        // NullReferenceException NGAY TRONG validator — vẫn 500, đúng thứ đang đi sửa.
        RuleFor(x => x.Entries)
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithMessage("Thiếu danh sách entries.")
            .Must(entries => DuplicateSysMenuIds(entries).Count == 0)
                .WithMessage(cmd =>
                    $"sysMenuId bị lặp trong entries: {string.Join(", ", DuplicateSysMenuIds(cmd.Entries))}. " +
                    "Mỗi sysMenuId chỉ được xuất hiện đúng một lần — đây là lệnh ghi đè toàn bộ, " +
                    "không phải danh sách thao tác cộng dồn.");

        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.SysMenuId).NotEmpty();

            // Roles = null (entry thiếu hẳn khoá `roles`) làm handler ném NullReferenceException ở
            // e.Roles.Distinct() → 500. Cùng lớp lỗi với key trùng, chặn cùng chỗ.
            entry.RuleFor(e => e.Roles)
                .NotNull()
                .WithMessage("Thiếu danh sách roles của entry — gửi mảng rỗng nếu muốn thu hồi sạch.");

            entry.RuleForEach(e => e.Roles).Must(r => Roles.All.Contains(r))
                .WithMessage("Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User.");
        });
    }

    /// <summary>Các <c>sysMenuId</c> xuất hiện từ 2 lần trở lên, sắp thứ tự ổn định để thông điệp
    /// lỗi không đổi giữa các lần chạy.</summary>
    private static List<Guid> DuplicateSysMenuIds(IReadOnlyCollection<PermissionMatrixEntryDto> entries)
        => [.. entries
            .GroupBy(e => e.SysMenuId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order()];
}

/// <summary>
/// Rule "payload phải PHỦ ĐỦ mọi <c>SysMenu</c>" — tách RIÊNG khỏi
/// <see cref="UpdatePermissionMatrixValidator"/> vì nó cần đọc DB
/// (<see cref="ISysMenuRepository"/>), trong khi validator kia phải giữ được constructor rỗng để
/// kiểm được bằng unit test thuần. <c>ValidationBehavior</c> chạy MỌI
/// <c>IValidator&lt;TRequest&gt;</c> đăng ký cho cùng một request, nên hai lớp cộng dồn chứ không
/// thay thế nhau.
///
/// <para><b>Vì sao rule này tồn tại</b> (quyết định người dùng 2026-08-30,
/// doc/contracts/permissions.md): đây là lệnh GHI ĐÈ TOÀN BỘ, nên trước đó sự VẮNG MẶT của một
/// <c>sysMenuId</c> mang nghĩa "thu hồi sạch mục đó". Một ngữ nghĩa mà sự vắng mặt có sức phá huỷ
/// là ngữ nghĩa không an toàn: mọi lỗi tải thiếu ở client đều trở thành lệnh xoá hợp lệ — và đã
/// xảy ra thật với payload <c>entries: []</c> xoá sạch bảng, làm sidebar của mọi user không phải
/// SuperAdmin thành trắng. Nay thiếu bất kỳ mục nào là <b>400</b>; muốn thu hồi sạch một mục thì
/// gửi mục đó với <c>roles: []</c>.</para>
///
/// <para><b>Chạy ở validator, KHÔNG ở handler</b> — có chủ đích: validator chạy TRƯỚC handler nên
/// request 400 không đụng một dòng nào của <c>SysMenuRoles</c>. Trả 400 sau khi bảng đã bị xoá là
/// vô nghĩa, và đó đúng là phép nghiệm thu số 1 của quyết định trên.</para>
/// </summary>
public sealed class UpdatePermissionMatrixCoverageValidator : AbstractValidator<UpdatePermissionMatrixCommand>
{
    public UpdatePermissionMatrixCoverageValidator(ISysMenuRepository menuRepo)
    {
        RuleFor(x => x.Entries).CustomAsync(async (entries, context, ct) =>
        {
            // Entries null đã có UpdatePermissionMatrixValidator báo — im lặng bỏ qua ở đây để
            // người dùng không nhận hai thông điệp nói cùng một chuyện.
            if (entries is null)
                return;

            var sentIds = entries.Select(e => e.SysMenuId).ToHashSet();
            var allMenus = await menuRepo.GetAllAsync(ct);

            // ---- Chiều THỪA: id không tồn tại (bổ sung 2026-08-31, quyết định người dùng) ----
            // Không có rule này thì id lạ đi thẳng tới handler và vỡ khoá ngoại
            // FK_SysMenuRoles_SysMenus_SysMenuId ⇒ client nhận 500. Đó là lỗi CỦA CLIENT bị báo
            // như lỗi hệ thống: người dùng tưởng hệ thống hỏng, và nó lọt vào log lỗi nghiêm
            // trọng cùng chỗ với sự cố thật.
            //
            // Kiểm chiều này TRƯỚC chiều thiếu: payload mang id lạ thường là payload ghép sai
            // nguồn, và "thiếu N mục" khi đó là triệu chứng phái sinh — báo nguyên nhân gốc
            // hữu ích hơn báo triệu chứng.
            var knownIds = allMenus.Select(menu => menu.Id).ToHashSet();
            var unknown = sentIds.Where(id => !knownIds.Contains(id)).Order().ToList();
            if (unknown.Count > 0)
            {
                context.AddFailure(
                    "Entries",
                    $"{unknown.Count} sysMenuId không tồn tại: {string.Join(", ", unknown)}. " +
                    "Chỉ gửi mục menu có thật — danh sách lấy từ GET /api/admin/permissions.");
                return;
            }

            // ---- Chiều THIẾU ----
            var missing = allMenus
                .Where(menu => !sentIds.Contains(menu.Id))
                .Select(menu => menu.Code)
                .Order(StringComparer.Ordinal)
                .ToList();

            if (missing.Count == 0)
                return;

            // Khoá "Entries" (không có chỉ số) — lỗi thuộc về TOÀN BỘ danh sách, không quy được
            // cho một phần tử cụ thể. Cùng quy ước với rule "sysMenuId bị lặp".
            context.AddFailure(
                "Entries",
                $"Thiếu {missing.Count} mục menu trong entries: {string.Join(", ", missing)}. " +
                "Đây là lệnh ghi đè toàn bộ nên payload phải liệt kê ĐỦ mọi mục menu — " +
                "gửi mục kèm roles rỗng nếu muốn thu hồi sạch quyền của mục đó.");
        });
    }
}

public sealed class UpdatePermissionMatrixHandler(ISysMenuRoleRepository menuRoleRepo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<UpdatePermissionMatrixCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UpdatePermissionMatrixCommand cmd, CancellationToken ct)
    {
        // Kiểm tranh chấp ghi TRƯỚC MỌI THỨ KHÁC: server tính lại token từ DB rồi so với token
        // client gửi lên. Lệch = có người đã lưu ma trận sau khi client tải nó về ⇒ 409 và không
        // ghi gì. So Ordinal, và cmd.Version null (client không gửi) cũng không bao giờ khớp —
        // đó là hành vi đã chốt, xem PermissionErrors.VersionConflict.
        var currentVersion = await menuRoleRepo.GetVersionAsync(ct);
        if (!string.Equals(cmd.Version, currentVersion, StringComparison.Ordinal))
            return Fail<bool>(PermissionErrors.VersionConflict);

        // Key trùng đã bị UpdatePermissionMatrixValidator loại TRƯỚC khi tới đây — không thêm lớp
        // phòng thủ thứ hai ở handler (xem docstring validator).
        var assignments = cmd.Entries.ToDictionary(
            e => e.SysMenuId, IReadOnlyCollection<string> (e) => e.Roles.Distinct().ToList());

        await menuRoleRepo.ReplaceAllAsync(assignments, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(true);
    }
}
