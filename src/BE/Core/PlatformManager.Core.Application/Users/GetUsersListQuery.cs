using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Models;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>
/// GET /api/users — grid màn "Quản trị người dùng".
///
/// <paramref name="Role"/>/<paramref name="IsLocked"/> là bộ lọc của thanh công cụ (CONTRACT
/// USER-6, xem doc/contracts/users.md). Bỏ trống tham số nào thì KHÔNG áp điều kiện đó — hai
/// bộ lọc độc lập, gửi cả hai thì giao nhau (AND).
/// </summary>
public sealed record GetUsersListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchText = null,
    string? Role = null,
    bool? IsLocked = null)
    : IQuery<PagedList<UserDto>>;

/// <summary>
/// <c>Role</c> ngoài <see cref="Roles.All"/> bị chặn ở đây → ValidationError 400 kèm
/// <c>fields.Role</c> (key của <c>fields</c> là PascalCase đúng tên property C# — xem
/// GlobalExceptionHandler.NormalizeField), KHÔNG âm thầm trả danh sách rỗng hay bỏ qua bộ lọc.
///
/// Vì sao chọn báo lỗi (chốt 2026-08-29, câu hỏi để ngỏ của CONTRACT USER-6): cả "bỏ qua bộ lọc"
/// lẫn "trả rỗng" đều im lặng — bỏ qua thì một tham số sai chính tả trả về TOÀN BỘ danh sách,
/// còn trả rỗng thì người quản trị đọc được một câu trả lời SAI mà tưởng là đúng ("hệ thống
/// không có Admin nào") rồi hành động theo nó. Ngoài ra cùng repo này đã có đúng một luật cho
/// giá trị role không hợp lệ — <see cref="CreateUserValidator"/>/<c>UpdateUserValidator</c> đều
/// trả ValidationError 400 — nên xử lý khác đi ở endpoint list là drift trên cùng một tập giá trị.
///
/// Khoảng trắng thuần được coi như "không lọc" (giống <c>SearchText</c>), không phải giá trị sai.
///
/// <para><b>Trần <c>PageSize</c> = <see cref="MaxPageSize"/> (chốt 2026-08-29, finding BE-3).</b>
/// Trước bản này không có trần nào: handler chỉ vá <c>PageSize &lt;= 0</c> về 20 nên
/// <c>?pageSize=1000000</c> đi thẳng xuống <c>Take(1000000)</c> — một request kéo trọn bảng user
/// (kèm role của từng dòng), không cần đăng nhập nhiều lần cũng làm nghẽn được. Trần bắt buộc
/// theo doc/huong_dan/quy-uoc/be-performance.md §"Ngoại lệ chỉ hợp lệ khi comment nêu con số".</para>
///
/// <para><b>Vì sao 200:</b> ô chọn số dòng của FE cho tối đa 50
/// (<c>user-grid-table.html</c> <c>[rowsPerPageOptions]="[10, 20, 50]"</c>), nên 200 vẫn dư 4 lần
/// cho các đường gọi ngoài UI (script quản trị, kiểm tra thủ công) mà không ai phải xin nới, trong
/// khi chặn đứng ca kéo cả bảng. Con số này hết đúng nếu FE thêm tuỳ chọn &gt; 200 dòng/trang —
/// khi đó phải nới ở ĐÂY và sửa luôn card <c>doc/contracts/users.md</c>, không vá ở handler.</para>
///
/// <para><b>Vì sao ràng buộc nằm ở validator chứ không phải nhánh vá trong handler:</b> vá âm thầm
/// (<c>PageSize &lt;= 0 ? 20 : PageSize</c>) làm giá trị hợp lệ được quyết ở hai nơi, và người gọi
/// gửi sai không bao giờ biết mình gửi sai. Nay chỉ còn MỘT nơi, và sai thì trả 400 kèm
/// <c>fields.PageSize</c>/<c>fields.Page</c>.</para>
/// </summary>
public sealed class GetUsersListValidator : AbstractValidator<GetUsersListQuery>
{
    /// <summary>Trần trên của <c>pageSize</c> — xem phần chú thích của lớp để biết vì sao 200.</summary>
    public const int MaxPageSize = 200;

    public GetUsersListValidator()
    {
        // Page cũng phải kiểm ở đây: handler đã bỏ Math.Max(Page, 1) cùng lượt, mà page=0 thì
        // Skip((0-1)*pageSize) là offset ÂM — Npgsql ném lỗi, tức 500 thay vì 400.
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải từ 1 trở lên.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"Số dòng mỗi trang phải từ 1 đến {MaxPageSize}.");

        RuleFor(x => x.Role)
            .Must(r => Roles.All.Contains(r!))
            .When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Vai trò lọc không hợp lệ — chỉ nhận SuperAdmin/Admin/User.");
    }
}

public sealed class GetUsersListHandler(IUserAdminService userAdminService)
    : BaseResponse, IRequestHandler<GetUsersListQuery, IApiResult<PagedList<UserDto>>>
{
    public async Task<IApiResult<PagedList<UserDto>>> Handle(GetUsersListQuery query, CancellationToken ct)
    {
        // KHÔNG vá Page/PageSize ở đây nữa (finding BE-3): GetUsersListValidator chạy trước
        // handler qua ValidationBehavior và đã bảo đảm Page >= 1, 1 <= PageSize <= 200. Giữ thêm
        // một lớp vá ở đây nghĩa là hai nơi cùng quyết định giá trị hợp lệ — lớp nào đúng thì chỉ
        // đọc cả hai mới biết, và lớp vá nuốt mất giá trị sai thay vì báo cho người gọi.
        var result = await userAdminService.GetListAsync(
            query.Page,
            query.PageSize,
            new UserListFilter(query.SearchText, query.Role, query.IsLocked),
            ct);

        return Ok(result);
    }
}
