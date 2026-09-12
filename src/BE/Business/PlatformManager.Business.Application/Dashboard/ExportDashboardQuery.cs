using System.Globalization;
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
/// <b>DB-4</b> — <c>GET /api/dashboard/export</c>: tải thẳng file <c>.xlsx</c>.
///
/// <para><b>Query params đúng bộ của DB-1, thêm không gì cả</b> — đó là toàn bộ cách luật "export
/// dùng CHUNG object bộ lọc với endpoint danh sách" được thi hành
/// (doc/huong_dan/wiki-core/be/15-import-export.md §4). Hai đường lọc song song SẼ lệch nhau, và
/// triệu chứng thuộc loại tệ nhất: file tải về không khớp thứ đang hiện trên màn hình, người dùng
/// mất niềm tin vào cả hai, còn lập trình viên không có cách nào biết bên nào đúng.</para>
/// </summary>
public sealed record ExportDashboardQuery(
    string? Mode = null,
    DateOnly? Date = null,
    int? Year = null,
    string? Search = null,
    Guid? GroupId = null,
    string? Status = null)
    : IQuery<DashboardExportFile>;

/// <inheritdoc cref="GetDashboardValidator"/>
public sealed class ExportDashboardValidator : AbstractValidator<ExportDashboardQuery>
{
    public ExportDashboardValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1, 9999)
            .When(x => x.Year.HasValue)
            .WithMessage("Năm phải nằm trong khoảng 1..9999.");
    }
}

public sealed class ExportDashboardHandler(
    IDashboardRepository repository,
    IDashboardExportWriter writer,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : BaseResponse, IRequestHandler<ExportDashboardQuery, IApiResult<DashboardExportFile>>
{
    /// <summary>MIME chuẩn của OOXML spreadsheet — hợp đồng khai đích danh chuỗi này.</summary>
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<IApiResult<DashboardExportFile>> Handle(ExportDashboardQuery query, CancellationToken ct)
    {
        if (!DashboardPeriods.TryParseMode(query.Mode, out var mode))
            return Fail<DashboardExportFile>(DashboardErrors.ModeInvalid, ("Mode", query.Mode));

        // `year` là một mode HỢP LỆ của DB-1 nhưng endpoint này không phục vụ nó — nên mã riêng,
        // không dùng lại MODE_INVALID. Bố cục file đã duyệt có khối nhận dạng kỳ với Từ ngày/Đến
        // ngày của MỘT kỳ (Q14); "cả năm" không ánh xạ được vào khuôn đó mà không thiết kế lại file.
        //
        // ⚠️ Q37 ("chỉ nhập theo tuần") KHÔNG thu hẹp endpoint này: mode=month vẫn xuất TRỌN tháng.
        // Xuất một tháng là phép TỔNG HỢP trên dữ liệu đã có, không tạo bản ghi nào — đây là chỗ dễ
        // "dọn nhầm" nhất khi đọc Q37 vội.
        if (mode == DashboardMode.Year)
            return Fail<DashboardExportFile>(DashboardErrors.ExportModeUnsupported, ("Mode", query.Mode));

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        PeriodRange period;

        if (mode == DashboardMode.Week)
        {
            if (!DashboardPeriods.TryResolveWeek(query.Date, query.Year, today, out period, out var isoYear))
            {
                return Fail<DashboardExportFile>(
                    DashboardErrors.PeriodYearMismatch,
                    ("Year", query.Year), ("Date", query.Date), ("IsoYear", isoYear));
            }
        }
        else
        {
            period = DashboardPeriods.ResolveMonth(query.Date, query.Year, today);
        }

        string? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!AssessmentStatuses.TryResolve(query.Status, out var resolved))
                return Fail<DashboardExportFile>(DashboardErrors.StatusInvalid, ("Status", query.Status));

            status = resolved;
        }

        var spec = CriteriaFilterSpec.Create(query.Search, query.GroupId, status, period);
        var rows = await repository.GetExportRowsAsync(spec, ct);

        var groupName = query.GroupId is { } groupId
            ? await repository.FindGroupLabelAsync(groupId, ct)
            : null;

        var exportedBy = currentUser.UserId is { } userId
            ? await repository.FindUserFullNameAsync(userId, ct)
            : null;

        var layout = BuildLayout(mode, period, rows.Count, clock.UtcNow, exportedBy, query, groupName, status);

        return Ok(new DashboardExportFile(layout.FileName, XlsxContentType, writer.Write(layout, rows)));
    }

    /// <summary>
    /// Dựng khối nhận dạng kỳ (§4.2). Mọi chuỗi ở đây là NỘI DUNG nghiệp vụ; bộ ghi Excel chỉ đặt
    /// chúng vào ô.
    /// </summary>
    private static DashboardExportLayout BuildLayout(
        DashboardMode mode,
        PeriodRange period,
        int criteriaCount,
        DateTimeOffset exportedAt,
        string? exportedBy,
        ExportDashboardQuery query,
        string? groupLabel,
        string? status)
    {
        var isWeek = mode == DashboardMode.Week;
        var weekNumber = ISOWeek.GetWeekOfYear(period.Start.ToDateTime(TimeOnly.MinValue));

        // Tên sheet + tên file dùng dấu GẠCH NGANG thay cho '/': Excel cấm / \ ? * [ ] trong tên
        // sheet, và một tên file mang '/' thì không phải tên file.
        var sheetName = isWeek
            ? $"Tuần {weekNumber}-{period.Year}"
            : $"Tháng {period.Start.Month}-{period.Year}";

        // Tên file CHỈ ASCII, không dấu, không khoảng trắng — trình duyệt và mọi hệ tệp mở được mà
        // không cần giải mã. Đó cũng là lý do hợp đồng đòi `filename` ASCII đứng CẠNH `filename*`.
        var fileName = isWeek
            ? $"bao-cao-dti_Tuan-{weekNumber}-{period.Year}.xlsx"
            : $"bao-cao-dti_Thang-{period.Start.Month}-{period.Year}.xlsx";

        var (scopeLabel, scopeValue) = isWeek
            ? ("Thuộc tháng", $"Tháng {period.Start.Month}/{period.Year}")
            : ("Gồm các tuần", DescribeWeeksOf(period));

        return new DashboardExportLayout(
            sheetName,
            fileName,
            PeriodLabels.Full(period),
            period.Start,
            period.End,
            scopeLabel,
            scopeValue,
            period.Year,
            criteriaCount,
            exportedAt,
            string.IsNullOrWhiteSpace(exportedBy) ? "—" : exportedBy,
            DescribeFilter(query, groupLabel, status, criteriaCount));
    }

    /// <summary>
    /// Dòng 5 của file THÁNG: liệt kê <b>mọi tuần ISO GIAO với tháng</b>, không phải mọi tuần nằm
    /// gọn trong tháng.
    ///
    /// <para>File mẫu tháng 8/2026 liệt <c>Tuần 31 (27/07 – 02/08)</c> — bắt đầu từ tháng 7 — và
    /// <c>Tuần 36 (31/08 – 06/09)</c> — kết thúc sang tháng 9. Đây là hệ quả trực tiếp của việc tuần
    /// ISO không nằm gọn trong tháng. Lấy "tuần nằm gọn trong tháng" sẽ bỏ sót dữ liệu của hai tuần
    /// đầu–cuối.</para>
    /// </summary>
    private static string DescribeWeeksOf(PeriodRange month)
    {
        var weeks = new List<string>();

        // Bước theo TUẦN từ tuần chứa ngày đầu tháng tới tuần chứa ngày cuối tháng — dùng lịch ISO
        // thật qua PeriodRange, không tự tính bằng "ngày / 7".
        var cursor = PeriodRange.IsoWeekOf(month.Start);
        var last = PeriodRange.IsoWeekOf(month.End);

        while (cursor.Start <= last.Start)
        {
            var number = ISOWeek.GetWeekOfYear(cursor.Start.ToDateTime(TimeOnly.MinValue));

            weeks.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"Tuần {number} ({cursor.Start:dd/MM}{PeriodLabels.RangeSeparator}{cursor.End:dd/MM})"));

            cursor = PeriodRange.IsoWeekOf(cursor.Start.AddDays(7));
        }

        // Dấu phân cách giữa các mục dùng CHUNG ký tự với dòng "Bộ lọc đang áp" — một file, một quy
        // ước phân cách.
        return string.Join(" · ", weeks);
    }

    /// <summary>
    /// Dòng 10 <c>Bộ lọc đang áp</c>. Không lọc gì thì VẪN ghi một dòng nói rõ điều đó — dòng vắng
    /// mặt không phân biệt được với "quên ghi".
    ///
    /// <para>Nhóm ở đây ghi dạng <c>Code. Name</c> (Q42) chứ không phải tên trần: đây là MÔ TẢ cho
    /// người đọc, không phải dữ liệu nạp lại. Cột <c>Nhóm</c> trong bảng dữ liệu thì ngược lại —
    /// tên trần, để round-trip không gãy (Q55).</para>
    /// </summary>
    private static string DescribeFilter(
        ExportDashboardQuery query, string? groupLabel, string? status, int criteriaCount)
    {
        var parts = new List<string>(3);

        if (!string.IsNullOrWhiteSpace(query.Search))
            parts.Add($"Tìm kiếm: \"{query.Search.Trim()}\"");

        if (groupLabel is not null)
            parts.Add($"Nhóm: {groupLabel}");

        if (status is not null)
            parts.Add($"Trạng thái: {status}");

        return parts.Count == 0
            ? $"Không lọc — đủ {criteriaCount} chỉ tiêu"
            : string.Join(" · ", parts);
    }
}
