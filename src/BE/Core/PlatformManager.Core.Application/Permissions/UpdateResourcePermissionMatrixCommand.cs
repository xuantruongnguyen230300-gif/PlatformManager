using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>PUT /api/admin/permissions/resources — ghi đè TOÀN BỘ RolePermission theo ma trận
/// gửi lên (giống hệt UpdatePermissionMatrixCommand — PERM-1). Controller gate
/// [Authorize(Roles="SuperAdmin")]. Xem
/// doc/contracts/permissions.md CONTRACT PERM-2 §"Rủi ro rollout" trước khi migrate.</summary>
/// <param name="Version">Token phiên bản — BẮT BUỘC từ 2026-08-31, cùng cơ chế và cùng lý do
/// khai <c>= null</c> như <see cref="UpdatePermissionMatrixCommand"/>: thiếu hoặc lệch ⇒ 409,
/// không ghi gì.</param>
public sealed record UpdateResourcePermissionMatrixCommand(
    IReadOnlyCollection<ResourcePermissionEntryDto> Entries, string? Version = null) : ICommand<bool>;

/// <summary>
/// <para><b>Vì sao có rule cấp COLLECTION chứ không chỉ rule từng phần tử (sửa 2026-08-29):</b>
/// handler dựng <c>Dictionary</c> bằng <c>Entries.ToDictionary(e => e.ResourceKey, …)</c>.
/// <c>ToDictionary</c> ném <see cref="ArgumentException"/> khi gặp key trùng, mà
/// <c>ExceptionHandlingBehavior</c>/<c>GlobalExceptionHandler</c> KHÔNG có nhánh riêng cho loại
/// exception đó — nó rơi vào nhánh mặc định và client nhận <b>500 SYSTEM_ERROR</b>. Trong khi
/// doc/contracts/permissions.md §Lỗi hứa <b>400 VALIDATION_ERROR</b>. Đây là lỗi ĐẦU VÀO (payload
/// của người gọi sai), không phải lỗi hệ thống: chặn ở validator là đúng chỗ, và chỉ ở đây mới
/// trả kèm <c>fields</c> cho FE tô đỏ.</para>
///
/// <para><b>Vì sao KHÔNG bọc try-catch quanh <c>ToDictionary</c>:</b> làm vậy thì quy tắc "payload
/// hợp lệ" nằm ở hai nơi (validator + handler), và thông điệp lỗi phải dịch ngược từ text tiếng
/// Anh của BCL. Ngoài ra validator chạy TRƯỚC handler nên request sai không đụng vào
/// <c>RolePermissions</c> — với một lệnh "ghi đè toàn bộ" thì đó là khác biệt thật, không phải
/// khác biệt hình thức.</para>
///
/// <para><b>So khớp ORDINAL</b> (<see cref="StringComparer.Ordinal"/>) — đúng bằng comparer mặc
/// định mà <c>ToDictionary</c> dùng (<c>EqualityComparer&lt;string&gt;.Default</c>). Dùng
/// comparer lỏng hơn (bỏ qua hoa/thường) sẽ chặn cả những cặp mà <c>ToDictionary</c> vẫn nhận,
/// tức validator nghiêm hơn thứ nó bảo vệ; dùng chặt hơn thì lọt. Việc so khớp phải khớp ĐÚNG
/// hành vi hạ nguồn.</para>
/// </summary>
public sealed class UpdateResourcePermissionMatrixValidator : AbstractValidator<UpdateResourcePermissionMatrixCommand>
{
    /// <param name="resourceKeySource">Danh mục key HỢP LỆ do host khai (tách 2026-09-03, xem
    /// <see cref="ICoreResourceKeySource"/>). Đọc MỘT LẦN lúc dựng validator, không đọc lại trong
    /// từng rule: danh mục là bảng hằng số của dự án, và gọi lại ở mỗi phần tử sẽ chạy phép kiểm
    /// trùng của <see cref="ResourceKeySourceExtensions.Catalog"/> theo số entry gửi lên.</param>
    public UpdateResourcePermissionMatrixValidator(ICoreResourceKeySource resourceKeySource)
    {
        var allowedKeys = resourceKeySource.Keys().ToHashSet(StringComparer.Ordinal);

        // Cascade.Stop: nếu bỏ qua thì `entries` null (body `{}`) vẫn chạy tiếp xuống Must và ném
        // NullReferenceException NGAY TRONG validator — vẫn 500, đúng thứ đang đi sửa.
        RuleFor(x => x.Entries)
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithMessage("Thiếu danh sách entries.")
            .Must(entries => DuplicateResourceKeys(entries).Count == 0)
                .WithMessage(cmd =>
                    $"resourceKey bị lặp trong entries: {string.Join(", ", DuplicateResourceKeys(cmd.Entries))}. " +
                    "Mỗi resourceKey chỉ được xuất hiện đúng một lần — đây là lệnh ghi đè toàn bộ, " +
                    "không phải danh sách thao tác cộng dồn.");

        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.ResourceKey).Must(allowedKeys.Contains)
                .WithMessage("ResourceKey không hợp lệ — chỉ nhận các key host đã khai qua ICoreResourceKeySource.");

            // Cùng lớp lỗi với key trùng: entry thiếu hẳn `roles` cho Roles = null, và handler gọi
            // e.Roles.Distinct() → NullReferenceException → 500. Chặn tại đây để ra 400 kèm
            // fields["Entries[i].Roles"].
            entry.RuleFor(e => e.Roles)
                .NotNull()
                .WithMessage("Thiếu danh sách roles của entry — gửi mảng rỗng nếu muốn thu hồi sạch.");

            entry.RuleForEach(e => e.Roles).Must(r => Roles.All.Contains(r))
                .WithMessage("Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User.");
        });
    }

    /// <summary>Các <c>resourceKey</c> xuất hiện từ 2 lần trở lên, sắp xếp ổn định để thông điệp
    /// lỗi không đổi thứ tự giữa các lần chạy (thông điệp này là hợp đồng với FE).</summary>
    private static List<string> DuplicateResourceKeys(IReadOnlyCollection<ResourcePermissionEntryDto> entries)
        => [.. entries
            .GroupBy(e => e.ResourceKey, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)];
}

/// <summary>
/// Rule "payload phải PHỦ ĐỦ mọi key trong danh mục của host" (<see cref="ICoreResourceKeySource"/>) — đối xứng với
/// <c>UpdatePermissionMatrixCoverageValidator</c> của PERM-1; đọc docstring lớp đó cho lý do đầy
/// đủ (sự vắng mặt không được phép mang nghĩa phá huỷ; chặn ở validator để request 400 KHÔNG đụng
/// vào bảng).
///
/// <para>Rule này KHÔNG cần DB — danh mục key là bảng hằng số của dự án — nên về kỹ thuật nó gộp
/// được vào <see cref="UpdateResourcePermissionMatrixValidator"/>. Vẫn tách ra để hai endpoint
/// cùng hình dạng có cùng cấu trúc lớp: quy tắc "phủ đủ" là MỘT quyết định áp cho cả hai, sửa nó
/// thì sửa hai lớp cùng tên hậu tố, không phải một lớp ở đây và một lớp ở kia.</para>
/// </summary>
public sealed class UpdateResourcePermissionMatrixCoverageValidator
    : AbstractValidator<UpdateResourcePermissionMatrixCommand>
{
    /// <param name="resourceKeySource">Xem chú thích cùng chỗ ở
    /// <see cref="UpdateResourcePermissionMatrixValidator"/>.</param>
    public UpdateResourcePermissionMatrixCoverageValidator(ICoreResourceKeySource resourceKeySource)
    {
        var requiredKeys = resourceKeySource.Keys();

        RuleFor(x => x.Entries).Custom((entries, context) =>
        {
            // Entries null: đã có validator kia báo, không nói chồng lên.
            if (entries is null)
                return;

            var sentKeys = entries.Select(e => e.ResourceKey).ToHashSet(StringComparer.Ordinal);
            var missing = requiredKeys
                .Where(key => !sentKeys.Contains(key))
                .Order(StringComparer.Ordinal)
                .ToList();

            if (missing.Count == 0)
                return;

            context.AddFailure(
                "Entries",
                $"Thiếu {missing.Count} resourceKey trong entries: {string.Join(", ", missing)}. " +
                "Đây là lệnh ghi đè toàn bộ nên payload phải liệt kê ĐỦ mọi key host đã khai qua " +
                "ICoreResourceKeySource — gửi key kèm roles rỗng nếu muốn thu hồi sạch quyền của key đó.");
        });
    }
}

public sealed class UpdateResourcePermissionMatrixHandler(IRolePermissionRepository repo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<UpdateResourcePermissionMatrixCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UpdateResourcePermissionMatrixCommand cmd, CancellationToken ct)
    {
        // Kiểm tranh chấp ghi TRƯỚC khi chạm dữ liệu — xem chú thích cùng chỗ ở
        // UpdatePermissionMatrixHandler (PERM-1). Lệch ⇒ 409, không ghi gì.
        var currentVersion = await repo.GetVersionAsync(ct);
        if (!string.Equals(cmd.Version, currentVersion, StringComparison.Ordinal))
            return Fail<bool>(PermissionErrors.VersionConflict);

        // ToDictionary vẫn ném ArgumentException khi key trùng — nhưng UpdateResourcePermissionMatrixValidator
        // đã loại ca đó TRƯỚC khi tới đây (xem docstring của validator). Không thêm lớp phòng thủ
        // thứ hai ở đây: hai nơi cùng quyết định "payload hợp lệ" là đúng thứ đã sinh ra bug này.
        var assignments = cmd.Entries.ToDictionary(
            e => e.ResourceKey, IReadOnlyCollection<string> (e) => e.Roles.Distinct().ToList());

        await repo.ReplaceAllAsync(assignments, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(true);
    }
}
