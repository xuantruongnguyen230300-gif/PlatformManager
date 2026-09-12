using FluentValidation;
using MediatR;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Dashboard;

/// <summary>
/// <b>DB-1</b> — <c>GET /api/dashboard</c>. Tổng hợp theo Tuần / Tháng / "Tất cả trong năm".
///
/// <para><b>Ba luật về PHẠM VI TÍNH, đây là chỗ dễ làm sai nhất</b> (doc/contracts/dashboard.md
/// DB-1):</para>
/// <list type="number">
///   <item>Bộ lọc <c>search</c>/<c>groupId</c>/<c>status</c> CHỈ áp cho <c>table</c>.
///   <c>kpi</c>, <c>groups</c>, <c>trend</c> LUÔN tính trên TOÀN BỘ chỉ tiêu của kỳ — lọc một
///   nhóm mà ô "Tiến độ chung" đổi theo thì người đọc tưởng tiến độ toàn xã thay đổi.</item>
///   <item><c>trend</c> trả ĐỦ các kỳ của phạm vi; kỳ không có dữ liệu mang <c>value</c> vắng
///   mặt nhưng PHẦN TỬ vẫn còn (Q44).</item>
///   <item>Công thức nằm ở <see cref="DashboardCalculator"/> — BE tính, FE chỉ hiển thị.</item>
/// </list>
/// </summary>
public sealed record GetDashboardQuery(
    string? Mode = null,
    DateOnly? Date = null,
    int? Year = null,
    string? Search = null,
    Guid? GroupId = null,
    string? Status = null)
    : IQuery<DashboardAggregateDto>;

/// <summary>
/// Chỉ chặn MIỀN của <c>year</c> — mọi thứ khác của DB-1 có mã nghiệp vụ riêng nên do handler
/// kiểm (xem <see cref="DashboardErrors"/>).
///
/// <para>Cần thiết vì <c>year</c> đi thẳng vào <c>ISOWeek.GetWeeksInYear</c> và
/// <c>new DateOnly(year, …)</c> — giá trị ngoài <c>1..9999</c> làm chúng ném
/// <see cref="ArgumentOutOfRangeException"/>, tức một tham số sai của client thành lỗi 500 thay
/// vì 400. Thuộc họ "sai kiểu/miền do binder" nên ra <c>ValidationError</c> + <c>fields.Year</c>,
/// KHÔNG phải một mã của catalog Dashboard.</para>
/// </summary>
public sealed class GetDashboardValidator : AbstractValidator<GetDashboardQuery>
{
    public GetDashboardValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1, 9999)
            .When(x => x.Year.HasValue)
            .WithMessage("Năm phải nằm trong khoảng 1..9999.");
    }
}

public sealed class GetDashboardHandler(IDashboardRepository repository, IDateTimeProvider clock)
    : BaseResponse, IRequestHandler<GetDashboardQuery, IApiResult<DashboardAggregateDto>>
{
    public async Task<IApiResult<DashboardAggregateDto>> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        // mode là tham số BẮT BUỘC; vắng mặt cũng ra MODE_INVALID — cả hai ca đều kết thúc bằng
        // "gửi lại một mode hợp lệ", nên không cần một mã ..._REQUIRED riêng.
        if (!DashboardPeriods.TryParseMode(query.Mode, out var mode))
            return Fail<DashboardAggregateDto>(DashboardErrors.ModeInvalid, ("Mode", query.Mode));

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        PeriodRange period;

        switch (mode)
        {
            case DashboardMode.Week:
                if (!DashboardPeriods.TryResolveWeek(query.Date, query.Year, today, out period, out var isoYear))
                {
                    return Fail<DashboardAggregateDto>(
                        DashboardErrors.PeriodYearMismatch,
                        ("Year", query.Year),
                        ("Date", query.Date),
                        ("IsoYear", isoYear));
                }

                break;

            case DashboardMode.Month:
                period = DashboardPeriods.ResolveMonth(query.Date, query.Year, today);
                break;

            default:
                period = DashboardPeriods.ResolveYear(query.Year, today);
                break;
        }

        string? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!AssessmentStatuses.TryResolve(query.Status, out var resolved))
                return Fail<DashboardAggregateDto>(DashboardErrors.StatusInvalid, ("Status", query.Status));

            status = resolved;
        }

        // Cửa sổ dữ liệu: từ đầu năm TRƯỚC năm đang lọc tới hết kỳ đang xem hoặc hết năm — vế
        // "năm trước" tồn tại cho ô "So với kỳ trước" khi kỳ hiện tại là kỳ ĐẦU của năm. Đọc
        // rộng hơn cần thiết một chút rẻ hơn nhiều so với một truy vấn thứ hai cho mỗi lần lùi kỳ.
        var windowStart = new DateOnly(period.Year - 1, 1, 1);
        var windowEnd = period.End > new DateOnly(period.Year, 12, 31)
            ? period.End
            : new DateOnly(period.Year, 12, 31);

        var criteria = await repository.GetCriteriaAsync(ct);
        var facts = await repository.GetAssessmentFactsAsync(windowStart, windowEnd, ct);

        var current = DashboardCalculator.RepresentativeIn(facts, period.Start, period.End);
        var previous = DashboardPeriods.PreviousWithData(mode, period, facts, windowStart);

        var overall = DashboardCalculator.ProgressOfPeriod(criteria, facts, period);
        decimal? delta = null;
        string? previousLabel = null;
        Dictionary<Guid, AssessmentFact>? previousRepresentative = null;

        if (previous is { } previousPeriod)
        {
            previousRepresentative = DashboardCalculator.RepresentativeIn(facts, previousPeriod.Start, previousPeriod.End);
            previousLabel = PeriodLabels.Full(previousPeriod);

            var previousOverall = DashboardCalculator.ProgressOfPeriod(criteria, facts, previousPeriod);

            // Không có tiến độ chung ở MỘT trong hai kỳ thì không có hiệu số — delta vắng mặt,
            // FE hiện dấu gạch chứ không hiện 0 (§1.3).
            if (overall is { } now && previousOverall is { } before)
                delta = now - before;
        }

        var (up, flat, down) = DashboardCalculator.CountMovement(criteria, current, previousRepresentative);

        var kpi = new DashboardKpiDto(
            overall,
            delta,
            previousLabel,
            up,
            flat,
            down,
            DashboardCalculator.CountDone(current),
            criteria.Count);

        var groups = criteria
            .GroupBy(item => (item.GroupId, item.GroupCode, item.GroupName, item.GroupDisplayOrder))
            .OrderBy(group => group.Key.GroupDisplayOrder)
            .Select(group => new DashboardGroupProgressDto(
                group.Key.GroupId,
                group.Key.GroupCode,
                group.Key.GroupName,
                DashboardCalculator.ProgressOfPeriod([.. group], facts, period)))
            .ToList();

        var trend = DashboardPeriods.TrendWindow(mode, period, today)
            .Select(point => new DashboardTrendPointDto(
                point.Value,
                PeriodLabels.TrendAxis(point),
                Clamp(DashboardCalculator.ProgressOfPeriod(criteria, facts, point))))
            .ToList();

        var spec = CriteriaFilterSpec.Create(query.Search, query.GroupId, status, period);
        var table = await repository.GetTableAsync(spec, ct);

        return Ok(new DashboardAggregateDto(
            DashboardPeriods.ToWireValue(mode),
            PeriodLabels.Full(period),
            period.Start,
            period.End,
            kpi,
            groups,
            trend,
            table));
    }

    /// <summary>
    /// Kẹp giá trị của <c>trend</c> về <c>[0, 100]</c> (§1.5) — chỉ áp cho kỳ CÓ giá trị;
    /// <c>null</c> đi nguyên, KHÔNG kẹp thành 0.
    /// </summary>
    private static decimal? Clamp(decimal? value) =>
        value is null ? null : Math.Clamp(value.Value, 0m, 100m);
}
