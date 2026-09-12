using FluentValidation;
using MediatR;
using PlatformManager.Business.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Dashboard;

/// <summary>
/// Một tuỳ chọn kỳ trong ô chọn kỳ — doc/contracts/dashboard.md DB-3.
/// </summary>
/// <param name="Value">
/// Chính là giá trị truyền vào <c>period</c> của DM-2 và vào <c>date</c>/<c>mode</c> của DB-1 —
/// <c>"2026-W33"</c> · <c>"2026-08"</c>. Đây là lý do FE không bao giờ phải tự dựng chuỗi kỳ.
/// </param>
/// <param name="Date">
/// Mốc ĐẦU kỳ (thứ Hai của tuần ISO, hoặc ngày 1 của tháng). Đủ để FE dựng nhãn
/// <c>Tuần 33 · 10/08 – 16/08 · 82,1%</c> mà KHÔNG cần trường mới: tuần ISO kết thúc sau đúng
/// 6 ngày.
/// </param>
public sealed record PeriodOptionDto(string Value, DateOnly Date, decimal? OverallProgress);

/// <summary>Payload của DB-3.</summary>
/// <param name="Years">Mọi năm CÓ dữ liệu, LUÔN kèm năm hiện tại dù chưa có dữ liệu.</param>
public sealed record PeriodOptionsDto(
    IReadOnlyList<int> Years,
    IReadOnlyList<PeriodOptionDto> WeeksInYear,
    IReadOnlyList<PeriodOptionDto> MonthsInYear);

/// <summary>
/// <b>DB-3</b> (= <b>DM-8</b>, dùng CHUNG route cho cả hai màn) —
/// <c>GET /api/dashboard/periods</c>.
///
/// <para><b>KHÔNG có mã lỗi nghiệp vụ nào</b>, và đó là câu trả lời chứ không phải mục còn
/// thiếu. Đặc biệt: một năm KHÔNG có dữ liệu trả <c>200</c> với hai mảng RỖNG, không phải
/// 404/400 — endpoint này nuôi ô lọc <c>Năm</c> của HAI màn, và báo lỗi cho một năm chưa nhập số
/// liệu sẽ khoá cứng ô đó ngay lần đầu người dùng mở một năm mới, đúng lúc chưa thể có dữ liệu.</para>
/// </summary>
public sealed record GetPeriodOptionsQuery(int? Year = null) : IQuery<PeriodOptionsDto>;

/// <summary>Cùng lý do với <see cref="GetDashboardValidator"/> — <c>year</c> dựng khoảng ngày.</summary>
public sealed class GetPeriodOptionsValidator : AbstractValidator<GetPeriodOptionsQuery>
{
    public GetPeriodOptionsValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1, 9999)
            .When(x => x.Year.HasValue)
            .WithMessage("Năm phải nằm trong khoảng 1..9999.");
    }
}

public sealed class GetPeriodOptionsHandler(IDashboardRepository repository, IDateTimeProvider clock)
    : BaseResponse, IRequestHandler<GetPeriodOptionsQuery, IApiResult<PeriodOptionsDto>>
{
    public async Task<IApiResult<PeriodOptionsDto>> Handle(GetPeriodOptionsQuery query, CancellationToken ct)
    {
        var currentYear = clock.UtcNow.Year;
        var year = query.Year ?? currentYear;

        var yearsWithData = await repository.GetYearsWithDataAsync(ct);
        var years = yearsWithData.Append(currentYear).Distinct().Order().ToList();

        var criteria = await repository.GetCriteriaAsync(ct);
        var facts = await repository.GetAssessmentFactsAsync(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), ct);

        // Tuần: gom theo tuần ISO chứa AssessmentDate, rồi GIỮ LẠI tuần thuộc đúng NĂM ISO đang
        // hỏi. Cần bước lọc đó vì tuần ISO không nằm gọn trong một năm — ngày 31/12 có thể thuộc
        // tuần 1 của năm sau, và tuần đó là tuỳ chọn của năm sau chứ không phải năm này.
        var weeks = facts
            .Select(fact => PeriodRange.IsoWeekOf(fact.AssessmentDate))
            .Where(week => week.Year == year)
            .DistinctBy(week => week.Value)
            .OrderBy(week => week.Start)
            .Select(week => new PeriodOptionDto(
                week.Value,
                week.Start,
                DashboardCalculator.ProgressOfPeriod(criteria, facts, week)))
            .ToList();

        var months = facts
            .Select(fact => PeriodRange.CalendarMonth(fact.AssessmentDate.Year, fact.AssessmentDate.Month))
            .Where(month => month.Year == year)
            .DistinctBy(month => month.Value)
            .OrderBy(month => month.Start)
            .Select(month => new PeriodOptionDto(
                month.Value,
                month.Start,
                DashboardCalculator.ProgressOfPeriod(criteria, facts, month)))
            .ToList();

        return Ok(new PeriodOptionsDto(years, weeks, months));
    }
}
