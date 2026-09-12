using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NSubstitute;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Business.Infrastructure.Export;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>DB-4</b> — "Xuất báo cáo".
///
/// <para>Bộ test này ĐỌC NGƯỢC file <c>.xlsx</c> vừa ghi thay vì chỉ kiểm các chuỗi trung gian. Bố
/// cục file là thứ dễ sai IM LẶNG nhất của cả card: sai chỉ số dòng header, quên dấu <c>—</c> ở ô
/// trống, hay đặt nhầm tên sheet đều build xanh và chỉ lộ ra khi có người mở file bằng Excel.</para>
/// </summary>
public class DashboardExportTests
{
    /// <summary>Thứ Tư 12/08/2026 — trong tuần ISO 33 (10/08 – 16/08).</summary>
    private static readonly DateOnly Today = new(2026, 8, 12);

    private static readonly Guid GroupId = Guid.CreateVersion7();

    private readonly IDashboardRepository _repository = Substitute.For<IDashboardRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public DashboardExportTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 8, 12, 14, 30, 0, TimeSpan.Zero));
        _currentUser.UserId.Returns(Guid.CreateVersion7());

        _repository.FindUserFullNameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("Nguyễn Văn A");
        _repository.GetExportRowsAsync(Arg.Any<CriteriaFilterSpec>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<DashboardExportRow>>(_ => [Row("1.1", 3m, 1.8m, 1.5m), Row("1.2", 4m, null, null)]);
    }

    private static DashboardExportRow Row(string code, decimal max, decimal? self, decimal? verified) =>
        new(code, $"Chỉ tiêu {code}", "Hạ tầng và Nền tảng số", max, self, verified,
            self is null || verified is null ? null : verified - self,
            self is null ? null : "Đang thực hiện",
            null, null, null, null);

    private ExportDashboardHandler CreateHandler() =>
        new(_repository, new DashboardExcelExportWriter(), _currentUser, _clock);

    private static ISheet Open(byte[] content)
    {
        using var stream = new MemoryStream(content);
        return new XSSFWorkbook(stream).GetSheetAt(0);
    }

    // ───────────────────────────── Nhánh lỗi ─────────────────────────────

    /// <summary>
    /// Q37 KHÔNG chạm endpoint này — nhưng <c>mode=year</c> thì có mã RIÊNG, không dùng lại
    /// <c>MODE_INVALID</c>: <c>year</c> là một mode hợp lệ của DB-1, chỉ là export không phục vụ nó.
    /// </summary>
    [Fact(DisplayName = "mode=year ⇒ 400 DASHBOARD.EXPORT_MODE_UNSUPPORTED, KHÔNG phải MODE_INVALID")]
    public async Task YearMode_IsRejectedWithItsOwnCode()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("year", Year: 2026), CancellationToken.None);

        Assert.Equal("DASHBOARD.EXPORT_MODE_UNSUPPORTED", result.BusinessCode);
        Assert.Equal(ErrorCode.ValidationError, result.Code);
    }

    [Fact(DisplayName = "mode lạ ⇒ DASHBOARD.MODE_INVALID (mã khác hẳn ca year)")]
    public async Task UnknownMode_IsModeInvalid()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("quarter"), CancellationToken.None);

        Assert.Equal("DASHBOARD.MODE_INVALID", result.BusinessCode);
    }

    [Fact(DisplayName = "status lạ ⇒ DASHBOARD.STATUS_INVALID, không âm thầm bỏ lọc")]
    public async Task UnknownStatus_IsRejected()
    {
        var result = await CreateHandler().Handle(
            new ExportDashboardQuery("week", Today, Status: "Đã xong"), CancellationToken.None);

        Assert.Equal("DASHBOARD.STATUS_INVALID", result.BusinessCode);
    }

    /// <summary>Q37 chỉ thu hẹp đường GHI — xuất một tháng là phép tổng hợp, vẫn phải chạy.</summary>
    [Fact(DisplayName = "mode=month VẪN xuất được — Q37 không thu hẹp export")]
    public async Task MonthMode_StillExports()
    {
        var result = await CreateHandler().Handle(
            new ExportDashboardQuery("month", Today), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Equal("bao-cao-dti_Thang-8-2026.xlsx", result.Data!.FileName);
    }

    // ───────────────────────────── Tên file, tên sheet ─────────────────────────────

    [Fact(DisplayName = "Tên file + tên sheet theo kỳ, chỉ ASCII, dùng gạch ngang thay '/'")]
    public async Task FileName_AndSheetName_FollowPeriod()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);

        Assert.Equal("bao-cao-dti_Tuan-33-2026.xlsx", result.Data!.FileName);
        Assert.Equal(ExportDashboardHandler.XlsxContentType, result.Data.ContentType);

        // Tên file phải mở được trên mọi hệ tệp mà không cần giải mã.
        Assert.All(result.Data.FileName, ch => Assert.True(ch < 128, $"Ký tự '{ch}' không phải ASCII."));

        Assert.Equal("Tuần 33-2026", Open(result.Data.Content).SheetName);
    }

    // ───────────────────────────── Bố cục ─────────────────────────────

    [Fact(DisplayName = "Header ở dòng 12, đủ 12 cột, đúng thứ tự §4.3")]
    public async Task Header_IsOnRow12_With12Columns()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var sheet = Open(result.Data!.Content);

        // Chỉ số 11 = dòng 12 theo cách Excel đánh số.
        var header = sheet.GetRow(11);

        Assert.Equal("Mã", header.GetCell(0).StringCellValue);
        Assert.Equal("Tiến độ %", header.GetCell(10).StringCellValue);
        Assert.Equal("Minh chứng/Ghi chú", header.GetCell(11).StringCellValue);

        // Cột 7 là ngoại lệ DUY NHẤT của quy tắc "trùng tên với file import" — tiêu đề ghi rõ chiều
        // tính (Q25), vì cột Chênh lệch của file gốc BA tính ngược dấu.
        Assert.Equal("Chênh lệch (Thẩm định − Tự đánh giá)", header.GetCell(6).StringCellValue);
    }

    /// <summary>
    /// 11 tiêu đề đầu phải trùng NGUYÊN VĂN tên cột của đường IMPORT — import khớp header theo TÊN,
    /// nên lệch một ký tự là phá round-trip <i>export → sửa → import</i> mà không có gì báo.
    /// </summary>
    [Fact(DisplayName = "10 tiêu đề (trừ cột Chênh lệch) khớp NGUYÊN VĂN hằng số của đường import")]
    public async Task Headers_MatchImportColumnNames()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var header = Open(result.Data!.Content).GetRow(11);

        Assert.Equal(Business.Application.Import.ImportColumns.Code, header.GetCell(0).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.Name, header.GetCell(1).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.Group, header.GetCell(2).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.MaxScore, header.GetCell(3).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.SelfScore, header.GetCell(4).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.VerifiedScore, header.GetCell(5).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.Status, header.GetCell(7).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.Owner, header.GetCell(8).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.Deadline, header.GetCell(9).StringCellValue);
        Assert.Equal(Business.Application.Import.ImportColumns.Note, header.GetCell(11).StringCellValue);
    }

    /// <summary>
    /// §4.3 — ô trống ghi <c>—</c>. Đường import bỏ qua <c>—</c> như bỏ qua ô trống; nếu bên xuất
    /// để rỗng hoặc bên nạp không bỏ qua thì round-trip ghi chữ <c>—</c> vào database.
    /// </summary>
    [Fact(DisplayName = "Ô trống ghi '—', và đường import đọc nó như ô TRỐNG (round-trip khép kín)")]
    public async Task EmptyCells_UseEmDash_AndImportTreatsItAsBlank()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var sheet = Open(result.Data!.Content);

        // Dòng dữ liệu thứ hai (chỉ số 13) là dòng "1.2" — không điểm, không trạng thái.
        var row = sheet.GetRow(13);
        var marker = row.GetCell(4).StringCellValue;

        Assert.Equal("—", marker);
        Assert.Equal("—", row.GetCell(7).StringCellValue);
        Assert.Equal("—", row.GetCell(11).StringCellValue);

        // Vòng khép kín: chính bộ đọc của đường nạp phải coi ký tự vừa ghi ra là ô TRỐNG.
        Assert.True(Business.Application.Import.ImportColumns.IsBlank(
            PlatformManager.Core.Application.Import.ImportCellValue.FromText(marker)));
    }

    [Fact(DisplayName = "Số ghi ra dạng SỐ, không phải chuỗi — người nhận còn SUM được")]
    public async Task Numbers_AreNumericCells()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var row = Open(result.Data!.Content).GetRow(12);

        Assert.Equal(CellType.Numeric, row.GetCell(3).CellType);
        Assert.Equal(3d, row.GetCell(3).NumericCellValue);
        Assert.Equal(1.8d, row.GetCell(4).NumericCellValue, 3);
    }

    [Fact(DisplayName = "Dòng TỔNG CỘNG: tổng 4 cột điểm, cột Tiến độ % KHÔNG có tổng")]
    public async Task TotalRow_SumsScoreColumnsOnly()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var sheet = Open(result.Data!.Content);

        // 2 dòng dữ liệu (12, 13) + 1 dòng trống (14) ⇒ tổng ở dòng 15 (chỉ số 15).
        var total = sheet.GetRow(15);

        Assert.Equal("TỔNG CỘNG", total.GetCell(0).StringCellValue);
        Assert.Equal(7d, total.GetCell(3).NumericCellValue, 3);      // 3 + 4
        Assert.Equal(1.8d, total.GetCell(4).NumericCellValue, 3);

        // Cộng 62 giá trị phần trăm lại cho ra một con số vô nghĩa (Σ% không phải %).
        Assert.Null(total.GetCell(10));
    }

    // ───────────────────────────── Khối nhận dạng kỳ ─────────────────────────────

    [Fact(DisplayName = "Dòng 5 file TUẦN ghi 'Thuộc tháng'; file THÁNG ghi 'Gồm các tuần'")]
    public async Task Row5_SwitchesLabelByMode()
    {
        var week = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var weekSheet = Open(week.Data!.Content);

        Assert.Equal("Thuộc tháng", weekSheet.GetRow(4).GetCell(0).StringCellValue);
        Assert.Equal("Tháng 8/2026", weekSheet.GetRow(4).GetCell(1).StringCellValue);

        var month = await CreateHandler().Handle(new ExportDashboardQuery("month", Today), CancellationToken.None);
        var monthSheet = Open(month.Data!.Content);

        Assert.Equal("Gồm các tuần", monthSheet.GetRow(4).GetCell(0).StringCellValue);

        // Liệt kê mọi tuần GIAO với tháng, không phải mọi tuần nằm gọn trong tháng: tuần 31 bắt đầu
        // từ 27/07 và tuần 36 kết thúc sang 06/09. Lấy "nằm gọn" sẽ bỏ sót dữ liệu hai tuần đầu-cuối.
        var weeks = monthSheet.GetRow(4).GetCell(1).StringCellValue;
        Assert.Contains("Tuần 31", weeks, StringComparison.Ordinal);
        Assert.Contains("Tuần 36", weeks, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Dòng 'Số chỉ tiêu' ghi số dòng THỰC XUẤT, không ghi cứng 62")]
    public async Task CriteriaCount_ReflectsFilteredRows()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);

        Assert.Equal("2", Open(result.Data!.Content).GetRow(6).GetCell(1).StringCellValue);
    }

    /// <summary>
    /// §4.6 — dòng <c>Bộ lọc đang áp</c> BẮT BUỘC kể cả khi không lọc, vì <b>file sống lâu hơn màn
    /// hình</b>: người nhận nó qua email không có cách nào biết nó được xuất lúc đang lọc gì.
    /// </summary>
    [Fact(DisplayName = "Dòng 10 'Bộ lọc đang áp' LUÔN có — không lọc thì nói rõ là không lọc")]
    public async Task FilterRow_IsAlwaysPresent()
    {
        var unfiltered = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var row = Open(unfiltered.Data!.Content).GetRow(9);

        Assert.Equal("Bộ lọc đang áp", row.GetCell(0).StringCellValue);
        Assert.Equal("Không lọc — đủ 2 chỉ tiêu", row.GetCell(1).StringCellValue);
    }

    /// <summary>
    /// Nhóm ở dòng mô tả dùng dạng <c>Code. Name</c> (Q42) — khác cột <c>Nhóm</c> trong bảng dữ
    /// liệu, nơi phải là tên trần (Q55) để import khớp được.
    /// </summary>
    [Fact(DisplayName = "Có lọc ⇒ dòng 10 liệt kê đủ; nhóm ghi dạng 'Code. Name'")]
    public async Task FilterRow_ListsAppliedFilters()
    {
        _repository.FindGroupLabelAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns("1. Hạ tầng và Nền tảng số");

        var result = await CreateHandler().Handle(
            new ExportDashboardQuery("week", Today, Search: "4.2", GroupId: GroupId, Status: "Hoàn thành"),
            CancellationToken.None);

        var summary = Open(result.Data!.Content).GetRow(9).GetCell(1).StringCellValue;

        Assert.Contains("Tìm kiếm: \"4.2\"", summary, StringComparison.Ordinal);
        Assert.Contains("Nhóm: 1. Hạ tầng và Nền tảng số", summary, StringComparison.Ordinal);
        Assert.Contains("Trạng thái: Hoàn thành", summary, StringComparison.Ordinal);

        // Cột `Nhóm` của bảng dữ liệu thì ngược lại — TÊN TRẦN, để round-trip không gãy.
        Assert.Equal("Hạ tầng và Nền tảng số", Open(result.Data.Content).GetRow(12).GetCell(2).StringCellValue);
    }

    [Fact(DisplayName = "Dòng 'Ngày xuất' + 'Người xuất' ghi đúng mốc và tên người đăng nhập")]
    public async Task IdentityBlock_CarriesExporter()
    {
        var result = await CreateHandler().Handle(new ExportDashboardQuery("week", Today), CancellationToken.None);
        var sheet = Open(result.Data!.Content);

        Assert.Equal("12/08/2026 14:30", sheet.GetRow(7).GetCell(1).StringCellValue);
        Assert.Equal("Nguyễn Văn A", sheet.GetRow(8).GetCell(1).StringCellValue);
    }

    /// <summary>
    /// Luật §4 của Core: export dùng CHUNG object bộ lọc với endpoint danh sách. Kiểm ở đây rằng
    /// handler thật sự CHUYỂN bộ lọc xuống repository thay vì dựng một bộ riêng.
    /// </summary>
    [Fact(DisplayName = "Bộ lọc được chuyển nguyên vẹn xuống projection export")]
    public async Task Filter_IsForwardedToRepository()
    {
        await CreateHandler().Handle(
            new ExportDashboardQuery("week", Today, Search: "4.2", GroupId: GroupId, Status: "Hoàn thành"),
            CancellationToken.None);

        await _repository.Received(1).GetExportRowsAsync(
            Arg.Is<CriteriaFilterSpec>(spec =>
                spec.SearchCode == "4.2"
                && spec.GroupId == GroupId
                && spec.Status == "Hoàn thành"
                && spec.Period.Value == "2026-W33"),
            Arg.Any<CancellationToken>());
    }
}
