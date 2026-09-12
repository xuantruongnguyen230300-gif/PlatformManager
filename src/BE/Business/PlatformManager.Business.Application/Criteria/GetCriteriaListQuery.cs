using FluentValidation;
using MediatR;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Permissions;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// <b>DM-2</b> — <c>GET /api/criteria</c>, lưới Danh mục DTI, phân trang server-side.
///
/// <para>Đây là "object bộ lọc" DÙNG CHUNG của cụm: mọi endpoint đọc theo danh sách bind đúng bộ
/// này (doc/huong_dan/wiki-core/be/15-import-export.md §4).</para>
/// </summary>
/// <param name="Search">Khớp <c>Code</c> HOẶC <c>Name</c> — không phân biệt hoa/thường và KHÔNG phân biệt dấu (Q47 + Q59).</param>
/// <param name="Year">Mặc định = năm hiện tại.</param>
/// <param name="Period"><c>"all"</c> (mặc định) · <c>"YYYY-Www"</c> · <c>"YYYY-MM"</c>.</param>
public sealed record GetCriteriaListQuery(
    string? Search = null,
    Guid? GroupId = null,
    string? Status = null,
    int? Year = null,
    string? Period = null,
    int Page = 1,
    int PageSize = GetCriteriaListValidator.DefaultPageSize)
    : IQuery<CriteriaGridDto>;

/// <summary>
/// Chỉ kiểm những thứ KHÔNG cần DB và KHÔNG có mã nghiệp vụ riêng — <c>page</c>/<c>pageSize</c>
/// ra <c>400 ValidationError</c> + <c>fields</c> (khoá PascalCase), đúng bảng mã lỗi của DM-2.
///
/// <para><b><c>status</c> và <c>period</c> CỐ Ý không ở đây:</b> hợp đồng đòi chúng mang
/// <c>businessCode</c> riêng (<c>CRITERIA.STATUS_INVALID</c> /
/// <c>CRITERIA.ASSESSMENT_PERIOD_INVALID</c>) để FE bind được câu chữ, mà đường validator thì ra
/// <c>ValidationError</c> không mang mã nghiệp vụ nào. Vì vậy hai tham số đó do handler kiểm và
/// trả <c>Fail</c>.</para>
/// </summary>
public sealed class GetCriteriaListValidator : AbstractValidator<GetCriteriaListQuery>
{
    /// <summary>
    /// Mặc định 10 (Q19) — CHỐT ở CẢ HAI phía FE và BE. Lưới <c>Quản trị người dùng</c> của Core
    /// có mặc định FE là 10 nhưng mặc định BE là 20, và chênh lệch đó chưa bao giờ lộ vì FE luôn
    /// gửi <c>pageSize</c> tường minh. Đặt 10 ở đây để không dựng lại đúng cái bẫy ấy cho màn
    /// thứ hai.
    /// </summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Trần 200 — cùng con số với <c>GetUsersListValidator.MaxPageSize</c>. Không có trần thì
    /// <c>?pageSize=1000000</c> đi thẳng xuống <c>Take(1000000)</c>, một request kéo trọn bảng.
    /// </summary>
    public const int MaxPageSize = 200;

    public GetCriteriaListValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải từ 1 trở lên.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"Số dòng mỗi trang phải từ 1 đến {MaxPageSize}.");

        // Miền của `year` phải chặn ở ĐÂY, không phải ở handler. `year` đi thẳng vào phép dựng
        // khoảng ngày của kỳ (`new DateOnly(year, 1, 1)`), và giá trị ngoài 1..9999 làm chỗ đó ném
        // ArgumentOutOfRangeException — tức một tham số sai của client thành lỗi 500 thay vì 400.
        // Thuộc họ "sai KIỂU/miền do binder", nên ra ValidationError + fields.Year chứ không phải
        // một mã của catalog DTI (doc/contracts/danh-muc-dti.md §"Mã lỗi của DM-2").
        RuleFor(x => x.Year)
            .InclusiveBetween(1, 9999)
            .When(x => x.Year.HasValue)
            .WithMessage("Năm phải nằm trong khoảng 1..9999.");
    }
}

public sealed class GetCriteriaListHandler(
    ICriteriaGridRepository repository,
    IDtiWriteAccess writeAccess,
    IDateTimeProvider clock)
    : BaseResponse, IRequestHandler<GetCriteriaListQuery, IApiResult<CriteriaGridDto>>
{
    public async Task<IApiResult<CriteriaGridDto>> Handle(GetCriteriaListQuery query, CancellationToken ct)
    {
        // MỘT mốc thời gian cho cả request: kỳ mặc định, isCurrentYear, và currentPeriod của Q72
        // đều dẫn xuất từ đây. Đọc đồng hồ hai lần là mở cửa cho ca request rơi đúng nửa đêm và
        // hai giá trị nói về hai ngày khác nhau.
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var year = query.Year ?? today.Year;

        if (!PeriodParser.TryParse(query.Period, year, out var period))
            return Fail<CriteriaGridDto>(CriteriaErrors.AssessmentPeriodInvalid, ("Period", query.Period));

        string? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            // Giá trị lạ ⇒ 400, KHÔNG âm thầm bỏ lọc: trả 200 với bộ lọc bị lờ đi là ca hỏng
            // người dùng không có cách nào phát hiện (§"Mã lỗi của DM-2").
            if (!AssessmentStatuses.TryResolve(query.Status, out var resolved))
                return Fail<CriteriaGridDto>(CriteriaErrors.StatusInvalid, ("Status", query.Status));

            status = resolved;
        }

        var spec = CriteriaFilterSpec.Create(query.Search, query.GroupId, status, period);
        var page = await repository.GetPageAsync(spec, query.Page, query.PageSize, ct);

        // Khối quyền tính MỖI REQUEST — đó là lý do nó không có cửa sổ lệch như một payload
        // cached-at-bootstrap (xem IDtiWriteAccess).
        var access = CriteriaGridDto.Evaluate(await writeAccess.CanWriteAsync(ct), period, today);

        return Ok(new CriteriaGridDto
        {
            Items = page.Items,
            TotalCount = page.TotalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            CanWrite = access.CanWrite,
            IsEditable = access.IsEditable,
            EditBlockedBy = access.EditBlockedBy,
            IsCurrentYear = access.IsCurrentYear,
            CurrentPeriod = access.CurrentPeriod,
            CurrentPeriodLabel = access.CurrentPeriodLabel,
        });
    }
}
