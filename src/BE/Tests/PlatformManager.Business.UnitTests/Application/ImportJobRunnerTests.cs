using System.Text.Json;
using NSubstitute;
using PlatformManager.Business.Application.Import;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Application.Storage;
using PlatformManager.Core.Application.Users;
using Xunit;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>DM-7 — luật từng dòng (§6.3) và luật ghi (§5.3) của lượt nạp file.</b>
///
/// <para>Không cần DB và không cần Docker: bốn seam của runner (kho file, bộ chọn reader, hai
/// repository, tra người dùng) đều là interface. Phần CẦN Postgres thật — EF dịch LINQ sang SQL,
/// ràng buộc unique <c>(CriteriaId, AssessmentDate)</c>, <c>CHECK ISODOW = 7</c> — thuộc
/// integration test, xem doc/huong_dan/wiki-core/be/04-testing-strategy.md.</para>
/// </summary>
public class ImportJobRunnerTests
{
    /// <summary>Thứ Tư 12/08/2026 — trong tuần ISO 33 (10/08 – 16/08).</summary>
    private static readonly DateOnly Today = new(2026, 8, 12);

    private static readonly DateOnly Week33Sunday = new(2026, 8, 16);

    private static readonly Guid GroupId = Guid.CreateVersion7();

    private const string GroupName = "Hạ tầng và Nền tảng số";

    private readonly FakeImportJobRepository _jobs = new();
    private readonly FakeCriteriaImportRepository _data = new();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IUserLookupService _users = Substitute.For<IUserLookupService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ImportJobRunnerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        _storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4])));
        _data.Groups.Add(new CriteriaGroupRef(GroupId, GroupName));
    }

    // ───────────────────────────── Ca thành công ─────────────────────────────

    [Fact(DisplayName = "Mã chưa có ⇒ TẠO chỉ tiêu mới và đếm vào criteriaCreatedCount")]
    public async Task UnknownCode_CreatesCriteria()
    {
        var file = FullFile().Row("4.22.11", "Chỉ tiêu mới", GroupName, 30m, 12.5m, 10m, "2.5", "Đang thực hiện", null, null, "ghi chú");

        var result = await RunAsync(file);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, result.CriteriaCreatedCount);
        Assert.Empty(result.Errors);

        var created = Assert.Single(_data.AddedCriteria);
        Assert.Equal("4.22.11", created.Code);
        Assert.Equal("Chỉ tiêu mới", created.Name);
        Assert.Equal(GroupId, created.GroupId);
        Assert.Equal(30m, created.MaxScore);

        var assessment = Assert.Single(_data.AddedAssessments);
        Assert.Equal(12.5m, assessment.SelfScore);
        Assert.Equal(10m, assessment.VerifiedScore);
        Assert.Equal(AssessmentStatuses.InProgress, assessment.Status);
        Assert.Equal("ghi chú", assessment.Note);
    }

    /// <summary>
    /// §6.3: mã đã có ⇒ CHỈ cập nhật đánh giá. Import là đường nạp SỐ LIỆU, không phải đường sửa
    /// danh mục — đổi tên hàng loạt bằng một file Excel là thứ không ai xem lại được.
    /// </summary>
    [Fact(DisplayName = "Mã đã có ⇒ KHÔNG đổi Name/MaxScore/GroupId của chỉ tiêu")]
    public async Task KnownCode_DoesNotTouchCriteriaDefinition()
    {
        var existing = SeedCriteria("1.1", "Tên gốc", 20m);
        var otherGroup = new CriteriaGroupRef(Guid.CreateVersion7(), "Nhân lực số");
        _data.Groups.Add(otherGroup);

        var file = FullFile().Row("1.1", "Tên MỚI trong file", otherGroup.Name, 999m, 5m, null, null, null, null, null, null);

        var result = await RunAsync(file);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.CriteriaCreatedCount);
        Assert.Equal("Tên gốc", existing.Name);
        Assert.Equal(20m, existing.MaxScore);
        Assert.Equal(GroupId, existing.GroupId);
    }

    /// <summary>
    /// Q24 + §5.3: import KHÔNG gửi <c>Tiến độ %</c>. Không có bản ghi kỳ trước ⇒ ô đó TRỐNG —
    /// không phải 0, không suy từ điểm. Đây là lý do Dashboard trống ngay sau lần nạp đầu tiên, và
    /// đó là trạng thái ĐÚNG.
    /// </summary>
    [Fact(DisplayName = "Q24 — ProgressPercent để TRỐNG sau khi nạp (không có kỳ trước)")]
    public async Task Import_LeavesProgressPercentEmpty()
    {
        var file = FullFile().Row("1.1", "Chỉ tiêu", GroupName, 20m, 18m, 17m, null, null, null, null, null);

        await RunAsync(file);

        Assert.Null(Assert.Single(_data.AddedAssessments).ProgressPercent);
    }

    /// <summary>
    /// §5.3 — copy-forward áp cho CẢ đường import, không có luật riêng. Thiếu nó thì một lượt nạp
    /// chỉ có cột điểm sẽ xoá trắng <c>Tiến độ %</c> mà người dùng vừa gõ tay tuần trước.
    /// </summary>
    [Fact(DisplayName = "Copy-forward — bản ghi mới thừa hưởng Tiến độ % của kỳ TRƯỚC")]
    public async Task NewAssessment_CopiesForward_ProgressPercentFromPriorPeriod()
    {
        var criteria = SeedCriteria("1.1", "Chỉ tiêu", 20m);
        SeedAssessment(criteria, new DateOnly(2026, 8, 3), progressPercent: 70, note: "ghi chú tuần 32");

        var file = FullFile().Row("1.1", null, GroupName, null, 18m, 17m, null, null, null, null, "ghi chú tuần 33");

        await RunAsync(file);

        var written = Assert.Single(_data.AddedAssessments);
        Assert.Equal(70, written.ProgressPercent);          // copy-forward
        Assert.Equal("ghi chú tuần 33", written.Note);      // file ghi đè
        Assert.Equal(18m, written.SelfScore);
    }

    [Fact(DisplayName = "Kỳ đích ĐÃ CÓ bản ghi ⇒ cập nhật tại chỗ, AssessmentDate GIỮ NGUYÊN")]
    public async Task ExistingAssessmentInTargetWeek_IsUpdatedInPlace()
    {
        var criteria = SeedCriteria("1.1", "Chỉ tiêu", 20m);
        var existing = SeedAssessment(criteria, new DateOnly(2026, 8, 10), progressPercent: 40);

        var file = FullFile().Row("1.1", null, GroupName, null, 9m, 8m, null, null, null, null, null);

        await RunAsync(file);

        Assert.Empty(_data.AddedAssessments);
        Assert.Equal(new DateOnly(2026, 8, 10), existing.AssessmentDate);
        Assert.Equal(9m, existing.SelfScore);
        Assert.Equal(40, existing.ProgressPercent);   // không gửi ⇒ giữ nguyên
    }

    /// <summary>
    /// Luật neo ngày §5.3 cho ca nhập bù: tuần đích đã qua ⇒ neo vào CHỦ NHẬT của tuần đó, không
    /// phải "hôm nay". Sai chỗ này thì cột <c>Kỳ của số liệu</c> báo một tuần khác tuần đã chọn.
    /// </summary>
    [Fact(DisplayName = "Nạp bù cho tuần ĐÃ QUA ⇒ AssessmentDate = Chủ nhật của tuần đích")]
    public async Task PastTargetWeek_AnchorsToSunday()
    {
        SeedCriteria("1.1", "Chỉ tiêu", 20m);

        var file = FullFile().Row("1.1", null, GroupName, null, 5m, null, null, null, null, null, null);

        // Tuần 30/2026 = 20/07 – 26/07.
        await RunAsync(file, targetWeekEnd: new DateOnly(2026, 7, 26));

        Assert.Equal(new DateOnly(2026, 7, 26), Assert.Single(_data.AddedAssessments).AssessmentDate);
    }

    // ───────────────────────────── Ô trống, dấu gạch, cột bỏ qua ─────────────────────────────

    /// <summary>
    /// File XUẤT ghi <c>—</c> vào ô trống (<c>spec/dashboard-dti/business-rules.md</c> §4.3). Nếu
    /// đường nạp không bỏ qua nó thì vòng <i>export → sửa → import</i> ghi chính chữ <c>—</c> vào
    /// database — hỏng im lặng, và chỉ lộ ra khi ai đó nhìn một ô ghi chú.
    /// </summary>
    [Fact(DisplayName = "Ô ghi \"—\" của file xuất được đọc như ô TRỐNG (round-trip)")]
    public async Task EmDashMarker_IsTreatedAsBlank()
    {
        var file = FullFile().Row("1.1", "Chỉ tiêu", GroupName, 20m, "—", "—", "—", "—", "—", "—", "—");

        var result = await RunAsync(file);

        Assert.Equal(0, result.ErrorCount);

        var written = Assert.Single(_data.AddedAssessments);
        Assert.Null(written.SelfScore);
        Assert.Null(written.VerifiedScore);
        Assert.Null(written.Status);
        Assert.Null(written.Deadline);
        Assert.Null(written.Note);
    }

    /// <summary>
    /// §6.2 cột 7: <c>Chênh lệch</c> ĐỌC VÀO RỒI VỨT. Cột đó trong file gốc của BA tính theo chiều
    /// CŨ nên ngược dấu ở 27/62 dòng — một đường nạp có đối chiếu sẽ từ chối gần nửa file gốc và
    /// đổ lỗi cho dữ liệu của BA.
    /// </summary>
    [Fact(DisplayName = "Cột Chênh lệch NGƯỢC DẤU vẫn không sinh lỗi nào")]
    public async Task DiffColumn_IsIgnored_EvenWhenContradictory()
    {
        // Hệ thống tính diff = 10 − 18 = −8; file ghi +8 (chiều cũ). Không được báo lỗi.
        var file = FullFile().Row("1.1", "Chỉ tiêu", GroupName, 20m, 18m, 10m, 8m, null, null, null, null);

        var result = await RunAsync(file);

        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, result.SuccessCount);
    }

    [Fact(DisplayName = "File KHÔNG có cột Chênh lệch vẫn nạp bình thường")]
    public async Task MissingDiffColumn_IsNotRequired()
    {
        var file = new ImportFileBuilder(
                ImportColumns.Code, ImportColumns.Name, ImportColumns.Group, ImportColumns.MaxScore,
                ImportColumns.SelfScore)
            .Row("1.1", "Chỉ tiêu", GroupName, 20m, 9m);

        var result = await RunAsync(file);

        Assert.Equal(1, result.SuccessCount);
    }

    /// <summary>
    /// <b>Cột VẮNG KHỎI FILE ≠ ô TRỐNG.</b> File "số liệu tuần này" chỉ có cột điểm là ca dùng
    /// thật; nếu bốn cột vắng mặt bị hiểu là "gửi null" thì mỗi lần nạp như vậy sẽ XOÁ TRẮNG
    /// <c>Trạng thái</c> / <c>Phụ trách</c> / <c>Hạn xử lý</c> / <c>Ghi chú</c> của kỳ đó — không
    /// lỗi nào báo.
    /// </summary>
    [Fact(DisplayName = "Cột VẮNG khỏi file ⇒ copy-forward, KHÔNG xoá trắng")]
    public async Task ColumnAbsentFromFile_IsCopiedForward_NotCleared()
    {
        var criteria = SeedCriteria("1.1", "Chỉ tiêu", 20m);
        var prior = SeedAssessment(criteria, new DateOnly(2026, 8, 3), progressPercent: 65, note: "ghi chú cũ");
        prior.SetStatus(AssessmentStatuses.NeedsEvidence);
        prior.SetDeadline(new DateOnly(2026, 12, 31));

        // Chỉ 6 cột — KHÔNG có Trạng thái / Phụ trách / Hạn xử lý / Minh chứng.
        var file = new ImportFileBuilder(
                ImportColumns.Code, ImportColumns.Name, ImportColumns.Group, ImportColumns.MaxScore,
                ImportColumns.SelfScore, ImportColumns.VerifiedScore)
            .Row("1.1", null, GroupName, null, 9m, 8m);

        var result = await RunAsync(file);

        Assert.Equal(1, result.SuccessCount);

        var written = Assert.Single(_data.AddedAssessments);
        Assert.Equal(9m, written.SelfScore);
        Assert.Equal(AssessmentStatuses.NeedsEvidence, written.Status);
        Assert.Equal("ghi chú cũ", written.Note);
        Assert.Equal(new DateOnly(2026, 12, 31), written.Deadline);
        Assert.Equal(65, written.ProgressPercent);
    }

    /// <summary>
    /// Vế còn lại của cùng một luật: cột CÓ MẶT nhưng ô trống là "xoá trắng có chủ đích" — đúng
    /// thứ vòng <i>export → sửa → import</i> cần, vì file xuất luôn có đủ 12 cột.
    /// </summary>
    [Fact(DisplayName = "Cột CÓ MẶT nhưng ô trống ⇒ XOÁ TRẮNG, không copy-forward")]
    public async Task BlankCellInPresentColumn_Clears()
    {
        var criteria = SeedCriteria("1.1", "Chỉ tiêu", 20m);
        var prior = SeedAssessment(criteria, new DateOnly(2026, 8, 3), note: "ghi chú cũ");
        prior.SetStatus(AssessmentStatuses.NeedsEvidence);

        var file = FullFile().Row("1.1", null, GroupName, null, 9m, null, null, null, null, null, null);

        await RunAsync(file);

        var written = Assert.Single(_data.AddedAssessments);
        Assert.Null(written.Status);
        Assert.Null(written.Note);
    }

    // ───────────────────────────── Lỗi từng dòng ─────────────────────────────

    [Fact(DisplayName = "Nhóm lạ ⇒ LỖI DÒNG, không tự tạo nhóm thứ 7")]
    public async Task UnknownGroup_IsRowError()
    {
        var file = FullFile().Row("1.1", "Chỉ tiêu", "Nhóm không có thật", 20m, null, null, null, null, null, null, null);

        var result = await RunAsync(file);

        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Empty(_data.AddedCriteria);

        var error = Assert.Single(result.Errors);
        Assert.Equal(2, error.RowNumber);                        // dòng 1 = header
        Assert.Equal("IMPORT.ROW_GROUP_NOT_FOUND", error.Code);
        Assert.Equal("1.1", error.MessageParams!["Code"]);
        Assert.Equal("Nhóm không có thật", error.MessageParams["GroupName"]);
    }

    /// <summary>
    /// §6.3: <c>Phụ trách</c> là THAM CHIẾU MỀM. Không khớp ai ⇒ để trống và dòng vẫn nạp — đối xử
    /// khác hẳn <c>Nhóm</c>, và đó là chủ đích.
    /// </summary>
    [Fact(DisplayName = "Phụ trách không khớp ai ⇒ KHÔNG lỗi, OwnerId để trống")]
    public async Task UnknownOwner_IsNotAnError()
    {
        _users.ResolveByFullNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var file = FullFile().Row("1.1", "Chỉ tiêu", GroupName, 20m, null, null, null, null, "Người Không Tồn Tại", null, null);

        var result = await RunAsync(file);

        Assert.Empty(result.Errors);
        Assert.Equal(1, result.SuccessCount);
        Assert.Null(Assert.Single(_data.AddedAssessments).OwnerId);
    }

    [Fact(DisplayName = "Phụ trách khớp đúng một người ⇒ gán OwnerId")]
    public async Task KnownOwner_IsAssigned()
    {
        var ownerId = Guid.CreateVersion7();
        _users.ResolveByFullNameAsync("Nguyễn Văn A", Arg.Any<CancellationToken>()).Returns(ownerId);

        var file = FullFile().Row("1.1", "Chỉ tiêu", GroupName, 20m, null, null, null, null, "Nguyễn Văn A", null, null);

        await RunAsync(file);

        Assert.Equal(ownerId, Assert.Single(_data.AddedAssessments).OwnerId);
    }

    /// <summary>
    /// Hợp đồng nêu đích danh: một dòng sai CẢ HAI cột điểm thì báo HAI phần tử <c>errors[]</c>
    /// cùng <c>rowNumber</c>, không phải một. Gộp làm một mã thì FE chỉ dựng được câu mơ hồ và
    /// người dùng mở file ra không biết nhìn cột nào.
    /// </summary>
    [Fact(DisplayName = "Sai cả hai cột điểm ⇒ HAI lỗi cùng rowNumber, HAI mã rời")]
    public async Task BothScoresExceedMax_ProduceTwoErrors()
    {
        var file = FullFile().Row("1.1", "Chỉ tiêu", GroupName, 10m, 12m, 11m, null, null, null, null, null);

        var result = await RunAsync(file);

        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(1, result.ErrorCount);        // vẫn là MỘT dòng hỏng
        Assert.All(result.Errors, error => Assert.Equal(2, error.RowNumber));

        Assert.Contains(result.Errors, e => e.Code == "IMPORT.ROW_SELF_SCORE_EXCEEDS_MAX");
        Assert.Contains(result.Errors, e => e.Code == "IMPORT.ROW_VERIFIED_SCORE_EXCEEDS_MAX");
    }

    [Fact(DisplayName = "Điểm so với MaxScore của chỉ tiêu ĐÃ LƯU, không phải của file")]
    public async Task ScoreIsComparedAgainstStoredMaxScore()
    {
        SeedCriteria("1.1", "Chỉ tiêu", 10m);

        // File khai điểm tối đa 100 — bị bỏ qua vì chỉ tiêu đã tồn tại; 12 vẫn vượt trần 10.
        var file = FullFile().Row("1.1", null, GroupName, 100m, 12m, null, null, null, null, null, null);

        var result = await RunAsync(file);

        Assert.Equal("IMPORT.ROW_SELF_SCORE_EXCEEDS_MAX", Assert.Single(result.Errors).Code);
    }

    [Fact(DisplayName = "Trạng thái ngoài 4 giá trị ⇒ lỗi dòng; chữ hoa/thường thì KHỚP")]
    public async Task Status_IsValidatedCaseInsensitively()
    {
        var file = FullFile()
            .Row("1.1", "A", GroupName, 20m, null, null, null, "đang thực hiện", null, null, null)
            .Row("1.2", "B", GroupName, 20m, null, null, null, "Đã xong", null, null, null);

        var result = await RunAsync(file);

        Assert.Equal(1, result.SuccessCount);

        var error = Assert.Single(result.Errors);
        Assert.Equal("IMPORT.ROW_STATUS_INVALID", error.Code);
        Assert.Equal(3, error.RowNumber);
        Assert.Equal("Đã xong", error.MessageParams!["Status"]);

        Assert.Equal(AssessmentStatuses.InProgress, Assert.Single(_data.AddedAssessments).Status);
    }

    [Fact(DisplayName = "Mã rỗng ⇒ ROW_CODE_MISSING, và CỐ Ý không mang tham số Code")]
    public async Task MissingCode_HasNoCodeParameter()
    {
        var file = FullFile().Row(null, "Chỉ tiêu", GroupName, 20m);

        var result = await RunAsync(file);

        var error = Assert.Single(result.Errors);
        Assert.Equal("IMPORT.ROW_CODE_MISSING", error.Code);
        Assert.Null(error.MessageParams);
    }

    [Fact(DisplayName = "Mã trùng trong cùng file ⇒ báo ở dòng THỨ HAI, kèm FirstRowNumber")]
    public async Task DuplicateCode_IsReportedOnSecondRow()
    {
        var file = FullFile()
            .Row("1.1", "Chỉ tiêu", GroupName, 20m, 5m)
            .Row("1.1", "Chỉ tiêu", GroupName, 20m, 6m);

        var result = await RunAsync(file);

        Assert.Equal(1, result.SuccessCount);

        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.RowNumber);
        Assert.Equal("IMPORT.ROW_CODE_DUPLICATED_IN_FILE", error.Code);
        Assert.Equal("2", error.MessageParams!["FirstRowNumber"]);
    }

    [Theory(DisplayName = "Mã sai khuôn/quá dài/đoạn quá dài ⇒ đúng MỘT mã, không lẫn nhau")]
    [InlineData("1.a", "IMPORT.ROW_CODE_FORMAT_INVALID")]
    [InlineData("1..2", "IMPORT.ROW_CODE_FORMAT_INVALID")]
    [InlineData(".1", "IMPORT.ROW_CODE_FORMAT_INVALID")]
    [InlineData("1.99999", "IMPORT.ROW_CODE_SEGMENT_TOO_LONG")]
    [InlineData("1111.1111.1111.1111.1111.1", "IMPORT.ROW_CODE_TOO_LONG")]
    public async Task MalformedCode_MapsToExactlyOneCode(string code, string expected)
    {
        var file = FullFile().Row(code, "Chỉ tiêu", GroupName, 20m);

        var result = await RunAsync(file);

        Assert.Equal(expected, Assert.Single(result.Errors).Code);
    }

    [Fact(DisplayName = "Chỉ tiêu MỚI thiếu tên / điểm tối đa ⇒ lỗi dòng, không tạo gì")]
    public async Task NewCriteria_WithoutNameOrMaxScore_IsRowError()
    {
        var file = FullFile().Row("9.9", null, GroupName, null, 5m);

        var result = await RunAsync(file);

        Assert.Empty(_data.AddedCriteria);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Code == "IMPORT.ROW_NAME_MISSING");
        Assert.Contains(result.Errors, e => e.Code == "IMPORT.ROW_MAX_SCORE_INVALID");
    }

    // ───────────────────────────── Lỗi cả file ─────────────────────────────

    /// <summary>
    /// <b>Q64</b> — job <c>Failed</c> ⇒ KHÔNG dòng nào được ghi. Kiểm cả hai nửa: thay đổi bị VỨT
    /// (<c>DiscardTrackedChanges</c>) và trạng thái job nói đúng sự thật.
    /// </summary>
    [Fact(DisplayName = "Q64 — file hỏng ⇒ job Failed, vứt mọi thay đổi chưa lưu, KHÔNG ném ra ngoài")]
    public async Task BrokenFile_FailsJob_AndDiscardsChanges()
    {
        var job = CreateJob(Week33Sunday);
        _jobs.Seed(job);

        var runner = CreateRunner(new ThrowingImportFileReader("file hỏng"));

        // KHÔNG ném: ném lại chỉ làm Hangfire nạp lại một file hỏng thêm chín lần nữa.
        await runner.RunAsync(job.Id, CancellationToken.None);

        Assert.Equal(ImportJobStatuses.Failed, job.Status);
        Assert.Contains("file hỏng", job.ErrorMessage);
        Assert.Null(job.ResultJson);
        Assert.Empty(_data.AddedAssessments);

        _unitOfWork.Received(1).DiscardTrackedChanges();
    }

    [Fact(DisplayName = "Thiếu cột bắt buộc (Mã / Nhóm) ⇒ lỗi CẢ FILE, không phải 62 lỗi dòng")]
    public async Task MissingRequiredHeader_FailsWholeFile()
    {
        var file = new ImportFileBuilder(ImportColumns.Name, ImportColumns.MaxScore)
            .Row("Chỉ tiêu", 20m);

        var job = CreateJob(Week33Sunday);
        _jobs.Seed(job);

        await CreateRunner(new FakeImportFileReader(file.Rows)).RunAsync(job.Id, CancellationToken.None);

        Assert.Equal(ImportJobStatuses.Failed, job.Status);
        Assert.Contains(ImportColumns.Code, job.ErrorMessage);
        Assert.Contains(ImportColumns.Group, job.ErrorMessage);
    }

    [Fact(DisplayName = "Job không còn trong bảng ⇒ thoát êm, không ném")]
    public async Task MissingJob_IsNoOp()
    {
        await CreateRunner(new FakeImportFileReader([])).RunAsync(Guid.CreateVersion7(), CancellationToken.None);

        Assert.Null(_jobs.Job);
    }

    /// <summary>
    /// Header khớp KHÔNG phân biệt hoa/thường nhưng CÓ phân biệt dấu (§6.2) — bỏ dấu ở đây sẽ làm
    /// <c>Thẩm định</c> và <c>Tham dinh</c> cùng khớp, hai chuỗi người dùng gõ với ý định khác nhau.
    /// </summary>
    [Fact(DisplayName = "Header khác hoa/thường và thừa khoảng trắng vẫn khớp")]
    public async Task Header_MatchesCaseInsensitively()
    {
        var file = new ImportFileBuilder(" MÃ ", "chỉ tiêu", "NHÓM", "Điểm Tối Đa", "tự đánh giá")
            .Row("1.1", "Chỉ tiêu", GroupName, 20m, 7m);

        var result = await RunAsync(file);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(7m, Assert.Single(_data.AddedAssessments).SelfScore);
    }

    // ───────────────────────────── Hạ tầng của bộ test ─────────────────────────────

    /// <summary>Bộ 11 cột đúng thứ tự §6.2 — dùng cho phần lớn ca kiểm.</summary>
    private static ImportFileBuilder FullFile() => new(
        ImportColumns.Code,
        ImportColumns.Name,
        ImportColumns.Group,
        ImportColumns.MaxScore,
        ImportColumns.SelfScore,
        ImportColumns.VerifiedScore,
        ImportColumns.Diff,
        ImportColumns.Status,
        ImportColumns.Owner,
        ImportColumns.Deadline,
        ImportColumns.Note);

    private async Task<ImportResultDto> RunAsync(ImportFileBuilder file, DateOnly? targetWeekEnd = null)
    {
        var job = CreateJob(targetWeekEnd ?? Week33Sunday);
        _jobs.Seed(job);

        await CreateRunner(new FakeImportFileReader(file.Rows)).RunAsync(job.Id, CancellationToken.None);

        Assert.Equal(ImportJobStatuses.Succeeded, job.Status);

        return JsonSerializer.Deserialize<ImportResultDto>(
            job.ResultJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private ImportJobRunner CreateRunner(IImportFileReader reader) => new(
        _jobs, _data, _storage, new FakeImportFileReaderSelector(reader), _users, _unitOfWork, _clock);

    private static ImportJob CreateJob(DateOnly targetWeekEnd) =>
        ImportJob.Create("dti.csv", "CSV", "uploads/danh-muc-dti/file.csv", targetWeekEnd);

    private CriteriaEntity SeedCriteria(string code, string name, decimal maxScore)
    {
        var criteria = CriteriaEntity.Create(code, name, GroupId, maxScore);
        _data.Criteria.Add(criteria);
        return criteria;
    }

    private CriteriaAssessment SeedAssessment(
        CriteriaEntity criteria, DateOnly date, int? progressPercent = null, string? note = null)
    {
        var assessment = CriteriaAssessment.Create(criteria.Id, date);
        assessment.SetProgressPercent(progressPercent);
        assessment.SetNote(note);
        _data.Assessments.Add(assessment);
        return assessment;
    }
}
