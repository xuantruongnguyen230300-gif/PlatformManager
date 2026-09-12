using NSubstitute;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using Xunit;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>DM-3 · DM-4 · DM-5 · DM-6</b> — bốn đường ghi TAY.
///
/// <para>Trọng tâm là những bất biến mà ba đường ghi PHẢI trả lời giống nhau (§5.3): kỳ đích,
/// copy-forward, và phân biệt "trường không gửi" với "trường gửi <c>null</c>" (Q74). Một đường mọc
/// nhánh riêng ở bất kỳ điểm nào trong ba điểm đó là lỗi không lộ ra cho tới khi có người so hai
/// màn hình.</para>
/// </summary>
public class CriteriaWriteHandlerTests
{
    /// <summary>Thứ Tư 12/08/2026 — trong tuần ISO 33 (10/08 – 16/08).</summary>
    private static readonly DateOnly Today = new(2026, 8, 12);

    private readonly FakeCriteriaWriteRepository _repository = new();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private readonly CriteriaGroup _group = CriteriaGroup.Create("1", "Hạ tầng và Nền tảng số", 1);

    public CriteriaWriteHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        _repository.Groups.Add(_group);
    }

    private AssessmentWriter Writer() => new(_repository, _clock);

    private CreateCriteriaHandler CreateHandler() => new(_repository, Writer(), _unitOfWork);

    private UpdateCriteriaHandler UpdateHandler() =>
        new(_repository, Writer(), new DtiPeriodContext(_clock), _unitOfWork);

    private UpdateCriteriaAssessmentHandler InlineHandler() => new(_repository, Writer(), _unitOfWork);

    private DeleteCriteriaHandler DeleteHandler() => new(_repository, _unitOfWork);

    private CriteriaEntity Seed(string code = "1.1", decimal maxScore = 20m)
    {
        var criteria = CriteriaEntity.Create(code, "Chỉ tiêu", _group.Id, maxScore);
        _repository.Criteria.Add(criteria);
        return criteria;
    }

    // ───────────────────────────── DM-3 — tạo ─────────────────────────────

    [Fact(DisplayName = "DM-3 — tạo chỉ tiêu, trả CriteriaDto kèm tên nhóm")]
    public async Task Create_ReturnsCriteriaDto()
    {
        var result = await CreateHandler().Handle(
            new CreateCriteriaCommand("4.22.11", "Chỉ tiêu mới", _group.Id, 30m), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Equal("4.22.11", result.Data!.Code);
        Assert.Equal(_group.Name, result.Data.GroupName);
        Assert.Single(_repository.Criteria);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "DM-3 — bốn ca mã sai cho ra bốn mã lỗi RỜI NHAU, tất cả 400")]
    [InlineData(null, "CRITERIA.CODE_REQUIRED")]
    [InlineData("1111.1111.1111.1111.1111.1", "CRITERIA.CODE_TOO_LONG")]
    [InlineData("1.a", "CRITERIA.CODE_FORMAT_INVALID")]
    [InlineData("1.99999", "CRITERIA.CODE_SEGMENT_TOO_LONG")]
    public async Task Create_MalformedCode_Is400WithDistinctCode(string? code, string expected)
    {
        var result = await CreateHandler().Handle(
            new CreateCriteriaCommand(code, "Chỉ tiêu", _group.Id, 30m), CancellationToken.None);

        Assert.Equal(expected, result.BusinessCode);

        // 400, KHÔNG phải 422: đây là khuôn dữ liệu sai, không phải vi phạm luật nghiệp vụ trên dữ
        // liệu hợp khuôn. Mã domain cùng businessCode thì đi ra 422 — xem CriteriaErrors.AsValidation.
        Assert.Equal(ErrorCode.ValidationError, result.Code);
        Assert.Empty(_repository.Criteria);
    }

    [Fact(DisplayName = "DM-3 — trùng mã ⇒ 409 CRITERIA.DUPLICATE_CODE")]
    public async Task Create_DuplicateCode_IsConflict()
    {
        Seed("1.1");

        var result = await CreateHandler().Handle(
            new CreateCriteriaCommand("1.1", "Chỉ tiêu", _group.Id, 30m), CancellationToken.None);

        Assert.Equal("CRITERIA.DUPLICATE_CODE", result.BusinessCode);
        Assert.Equal(ErrorCode.Conflict, result.Code);
    }

    /// <summary>
    /// Unique index là partial theo <c>IsDeleted = false</c> (§1.4), nên xoá mềm một mã rồi tạo lại
    /// đúng mã đó PHẢI thành công. Kiểm bằng phép kiểm của handler, không phải bằng DB.
    /// </summary>
    [Fact(DisplayName = "DM-3 — mã ĐÃ XOÁ MỀM thì tạo lại ĐƯỢC")]
    public async Task Create_ReusesSoftDeletedCode()
    {
        var removed = Seed("1.1");
        removed.IsDeleted = true;

        var result = await CreateHandler().Handle(
            new CreateCriteriaCommand("1.1", "Chỉ tiêu", _group.Id, 30m), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
    }

    [Fact(DisplayName = "DM-3 — nhóm không tồn tại ⇒ 422 (FK trong payload, KHÔNG phải 404)")]
    public async Task Create_UnknownGroup_Is422()
    {
        var result = await CreateHandler().Handle(
            new CreateCriteriaCommand("9.9", "Chỉ tiêu", Guid.CreateVersion7(), 30m), CancellationToken.None);

        Assert.Equal("CRITERIA.GROUP_NOT_FOUND", result.BusinessCode);
        Assert.Equal(ErrorCode.BusinessRuleError, result.Code);
    }

    [Fact(DisplayName = "DM-3 — kèm assessment ⇒ ghi luôn bản ghi đánh giá của kỳ đích")]
    public async Task Create_WithAssessment_WritesFirstAssessment()
    {
        var result = await CreateHandler().Handle(
            new CreateCriteriaCommand("9.9", "Chỉ tiêu", _group.Id, 30m, new AssessmentPayload("2026-W33")
            {
                SelfScore = Assigned<decimal?>.Set(12m),
            }),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var assessment = Assert.Single(_repository.Assessments);
        Assert.Equal(12m, assessment.SelfScore);
        Assert.Equal(Today, assessment.AssessmentDate);   // hôm nay NẰM TRONG tuần 33
    }

    // ───────────────────────────── DM-4 — dialog ─────────────────────────────

    [Fact(DisplayName = "DM-4 — giữ nguyên mã mà đổi tên KHÔNG bị coi là trùng mã")]
    public async Task Update_SameCode_IsNotDuplicate()
    {
        var criteria = Seed("1.1");

        var result = await UpdateHandler().Handle(
            new UpdateCriteriaCommand(criteria.Id, "1.1", "Tên mới", _group.Id, 20m), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Equal("Tên mới", criteria.Name);
    }

    /// <summary>
    /// Q74 ở cấp OBJECT: <c>assessment</c> vắng mặt = không đụng tới dữ liệu đánh giá, khác hẳn
    /// "có mặt với mọi trường <c>null</c>" (xoá trắng).
    /// </summary>
    [Fact(DisplayName = "DM-4 — assessment VẮNG MẶT ⇒ không tạo/không sửa bản ghi đánh giá nào")]
    public async Task Update_WithoutAssessment_DoesNotTouchAssessments()
    {
        var criteria = Seed("1.1");

        await UpdateHandler().Handle(
            new UpdateCriteriaCommand(criteria.Id, "1.1", "Tên mới", _group.Id, 20m), CancellationToken.None);

        Assert.Empty(_repository.Assessments);
    }

    /// <summary>
    /// <b>Q74 ở cấp TRƯỜNG</b> — hệ quả bắt buộc mà coordinator nêu đích danh: DM-4 phải phân biệt
    /// "trường không gửi" với "trường gửi giá trị rỗng", y như đường import phân biệt "cột vắng khỏi
    /// file" với "ô trống". Hai đường ghi trả lời khác nhau cho cùng một câu hỏi là đúng thứ §5.3
    /// sinh ra để chặn.
    /// </summary>
    [Fact(DisplayName = "DM-4 (Q74) — trường KHÔNG gửi ⇒ copy-forward; trường gửi null ⇒ XOÁ TRẮNG")]
    public async Task Update_UnsentField_CopiesForward_SentNullClears()
    {
        var criteria = Seed("1.1");

        var prior = CriteriaAssessment.Create(criteria.Id, new DateOnly(2026, 8, 3));
        prior.SetScores(7m, 6m);
        prior.SetStatus(AssessmentStatuses.InProgress);
        prior.SetNote("ghi chú tuần 32");
        _repository.Assessments.Add(prior);

        var result = await UpdateHandler().Handle(
            new UpdateCriteriaCommand(criteria.Id, "1.1", "Chỉ tiêu", _group.Id, 20m, new AssessmentPayload("2026-W33")
            {
                SelfScore = Assigned<decimal?>.Set(9m),     // gửi giá trị
                Note = Assigned<string?>.Set(null),         // gửi null ⇒ xoá trắng
                // Status KHÔNG gửi ⇒ copy-forward
            }),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var written = _repository.Assessments.Single(a => a.AssessmentDate == Today);
        Assert.Equal(9m, written.SelfScore);
        Assert.Null(written.Note);
        Assert.Equal(AssessmentStatuses.InProgress, written.Status);
        Assert.Equal(6m, written.VerifiedScore);   // cũng không gửi ⇒ copy-forward
    }

    [Fact(DisplayName = "DM-4 — điểm vượt trần ⇒ 422, và KHÔNG ghi gì")]
    public async Task Update_ScoreExceedsMax_Is422()
    {
        var criteria = Seed("1.1", maxScore: 10m);

        var result = await UpdateHandler().Handle(
            new UpdateCriteriaCommand(criteria.Id, "1.1", "Chỉ tiêu", _group.Id, 10m, new AssessmentPayload("2026-W33")
            {
                SelfScore = Assigned<decimal?>.Set(12m),
            }),
            CancellationToken.None);

        Assert.Equal("CRITERIA.ASSESSMENT_SELF_SCORE_EXCEEDS_MAX", result.BusinessCode);
        Assert.Equal(ErrorCode.BusinessRuleError, result.Code);
        Assert.Empty(_repository.Assessments);
    }

    /// <summary>
    /// Dialog cho sửa <c>maxScore</c> và điểm trong CÙNG một lượt. Nếu điểm được so với trần CŨ thì
    /// một lời ghi nâng cả hai lên sẽ bị từ chối, và người dùng không có cách nào thoát ra ngoài
    /// việc lưu hai lần.
    /// </summary>
    [Fact(DisplayName = "DM-4 — nâng maxScore VÀ điểm trong cùng lượt ⇒ so với trần MỚI")]
    public async Task Update_RaisingMaxScoreAndScoreTogether_UsesNewMax()
    {
        var criteria = Seed("1.1", maxScore: 10m);

        var result = await UpdateHandler().Handle(
            new UpdateCriteriaCommand(criteria.Id, "1.1", "Chỉ tiêu", _group.Id, 30m, new AssessmentPayload("2026-W33")
            {
                SelfScore = Assigned<decimal?>.Set(12m),
            }),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Equal(12m, _repository.Assessments.Single().SelfScore);
    }

    [Fact(DisplayName = "DM-4 — trạng thái ngoài 4 giá trị ⇒ 400 CRITERIA.STATUS_INVALID")]
    public async Task Update_InvalidStatus_Is400()
    {
        var criteria = Seed("1.1");

        var result = await UpdateHandler().Handle(
            new UpdateCriteriaCommand(criteria.Id, "1.1", "Chỉ tiêu", _group.Id, 20m, new AssessmentPayload("2026-W33")
            {
                Status = Assigned<string?>.Set("Đã xong"),
            }),
            CancellationToken.None);

        Assert.Equal("CRITERIA.STATUS_INVALID", result.BusinessCode);
        Assert.Equal(ErrorCode.ValidationError, result.Code);
    }

    [Fact(DisplayName = "DM-4 — ownerId không tồn tại ⇒ 422 CRITERIA.OWNER_NOT_FOUND")]
    public async Task Update_UnknownOwner_Is422()
    {
        var criteria = Seed("1.1");

        var result = await UpdateHandler().Handle(
            new UpdateCriteriaCommand(criteria.Id, "1.1", "Chỉ tiêu", _group.Id, 20m, new AssessmentPayload("2026-W33")
            {
                OwnerId = Assigned<Guid?>.Set(Guid.CreateVersion7()),
            }),
            CancellationToken.None);

        Assert.Equal("CRITERIA.OWNER_NOT_FOUND", result.BusinessCode);
        Assert.Equal(ErrorCode.BusinessRuleError, result.Code);
    }

    // ───────────────────────────── DM-6 — sửa inline ─────────────────────────────

    /// <summary>
    /// 🔴 <b>Ca FE nêu đích danh:</b> FE chốt <i>rời ô sửa inline thì LƯU</i>, nên mỗi lần rời một ô
    /// là một <c>PUT</c>. Bất biến phải giữ: nhiều lời ghi vào CÙNG một kỳ chồng lên nhau trên MỘT
    /// bản ghi (§5.3 bước 2 mục 1), không sinh bản thứ hai — nếu không, luật đọc §5.2 sẽ âm thầm
    /// chọn một trong mấy bản và Dashboard đếm sai.
    /// </summary>
    [Fact(DisplayName = "DM-6 — BA lời ghi liên tiếp vào cùng kỳ ⇒ ĐÚNG MỘT bản ghi")]
    public async Task Inline_RepeatedWritesToSamePeriod_KeepSingleAssessment()
    {
        var criteria = Seed("1.1");
        var handler = InlineHandler();

        foreach (var progress in new[] { 10, 40, 75 })
        {
            var result = await handler.Handle(
                new UpdateCriteriaAssessmentCommand(criteria.Id, "2026-W33", ProgressPercent: progress, Note: "ghi chú"),
                CancellationToken.None);

            Assert.Equal(ErrorCode.Success, result.Code);
        }

        var assessment = Assert.Single(_repository.Assessments);
        Assert.Equal(75, assessment.ProgressPercent);
        Assert.Equal(Today, assessment.AssessmentDate);
    }

    [Fact(DisplayName = "DM-6 — ngữ nghĩa PUT: bỏ trống note ⇒ null-hoá, không giữ giá trị cũ")]
    public async Task Inline_OmittedNote_IsCleared()
    {
        var criteria = Seed("1.1");
        var handler = InlineHandler();

        await handler.Handle(
            new UpdateCriteriaAssessmentCommand(criteria.Id, "2026-W33", ProgressPercent: 10, Note: "có ghi chú"),
            CancellationToken.None);

        await handler.Handle(
            new UpdateCriteriaAssessmentCommand(criteria.Id, "2026-W33", ProgressPercent: 20, Note: null),
            CancellationToken.None);

        Assert.Null(_repository.Assessments.Single().Note);
    }

    /// <summary>§3.2 — ngoài miền 0..100 thì KẸP, KHÔNG báo lỗi. Đây là lý do
    /// <c>CRITERIA.PROGRESS_PERCENT_INVALID</c> không tồn tại trong catalog.</summary>
    [Fact(DisplayName = "DM-6 — progressPercent ngoài miền bị KẸP, không báo lỗi")]
    public async Task Inline_OutOfRangeProgress_IsClamped()
    {
        var criteria = Seed("1.1");

        var result = await InlineHandler().Handle(
            new UpdateCriteriaAssessmentCommand(criteria.Id, "2026-W33", ProgressPercent: 250),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Equal(100, _repository.Assessments.Single().ProgressPercent);
    }

    [Theory(DisplayName = "DM-6 — bốn ca hỏng của kỳ đích cho ra bốn mã CRITERIA.ASSESSMENT_PERIOD_* rời nhau")]
    [InlineData(null, null, "CRITERIA.ASSESSMENT_PERIOD_REQUIRED")]
    [InlineData("2026-W99", null, "CRITERIA.ASSESSMENT_PERIOD_INVALID")]
    [InlineData("2026-08", null, "CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY")]
    [InlineData("all", 2025, "CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR")]
    public async Task Inline_PeriodFailures_MapToDistinctCodes(string? period, int? year, string expected)
    {
        var criteria = Seed("1.1");

        var result = await InlineHandler().Handle(
            new UpdateCriteriaAssessmentCommand(criteria.Id, period, year, ProgressPercent: 10),
            CancellationToken.None);

        Assert.Equal(expected, result.BusinessCode);
        Assert.Empty(_repository.Assessments);
    }

    /// <summary>Ca ÂM của T15 (nghiệm thu mục 14): năm cũ + TUẦN CỤ THỂ vẫn ghi được (Q20/Q41).</summary>
    [Fact(DisplayName = "DM-6 — year cũ + tuần cụ thể ⇒ GHI ĐƯỢC, neo vào Chủ nhật tuần đó")]
    public async Task Inline_PastYearWithSpecificWeek_Writes()
    {
        var criteria = Seed("1.1");

        var result = await InlineHandler().Handle(
            new UpdateCriteriaAssessmentCommand(criteria.Id, "2025-W33", 2025, ProgressPercent: 50),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var assessment = Assert.Single(_repository.Assessments);
        Assert.Equal(2025, assessment.AssessmentDate.Year);
        Assert.Equal(DayOfWeek.Sunday, assessment.AssessmentDate.DayOfWeek);
    }

    [Fact(DisplayName = "DM-6 — version lệch ⇒ 409 CRITERIA.ASSESSMENT_CONFLICT, KHÔNG ghi gì")]
    public async Task Inline_StaleVersion_IsConflict()
    {
        var criteria = Seed("1.1");
        var existing = CriteriaAssessment.Create(criteria.Id, Today);
        existing.SetProgressPercent(10);
        _repository.Assessments.Add(existing);

        var result = await InlineHandler().Handle(
            new UpdateCriteriaAssessmentCommand(criteria.Id, "2026-W33", ProgressPercent: 90, Version: "999999"),
            CancellationToken.None);

        Assert.Equal("CRITERIA.ASSESSMENT_CONFLICT", result.BusinessCode);
        Assert.Equal(ErrorCode.Conflict, result.Code);
        Assert.Equal(10, existing.ProgressPercent);
    }

    /// <summary>
    /// Kỳ đích CHƯA có bản ghi thì không có gì để ghi đè, nên không có xung đột nào để phát hiện —
    /// trả 409 ở đó là từ chối một lời ghi hoàn toàn an toàn.
    /// </summary>
    [Fact(DisplayName = "DM-6 — gửi version khi kỳ đích chưa có bản ghi ⇒ vẫn ghi được")]
    public async Task Inline_VersionOnEmptyPeriod_StillWrites()
    {
        var criteria = Seed("1.1");

        var result = await InlineHandler().Handle(
            new UpdateCriteriaAssessmentCommand(criteria.Id, "2026-W33", ProgressPercent: 30, Version: "12345"),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
    }

    [Fact(DisplayName = "DM-6 — chỉ tiêu không tồn tại ⇒ 404")]
    public async Task Inline_UnknownCriteria_Is404()
    {
        var result = await InlineHandler().Handle(
            new UpdateCriteriaAssessmentCommand(Guid.CreateVersion7(), "2026-W33"), CancellationToken.None);

        Assert.Equal("CRITERIA.NOT_FOUND", result.BusinessCode);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    // ───────────────────────────── DM-5 — xoá ─────────────────────────────

    [Fact(DisplayName = "DM-5 — chưa TỪNG có đánh giá ⇒ xoá CỨNG, hardDeleted = true")]
    public async Task Delete_WithoutHistory_IsHardDelete()
    {
        var criteria = Seed("1.1");

        var result = await DeleteHandler().Handle(new DeleteCriteriaCommand(criteria.Id), CancellationToken.None);

        Assert.True(result.Data!.HardDeleted);
        Assert.Contains(criteria, _repository.Removed);
        Assert.Empty(_repository.Criteria);
    }

    /// <summary>
    /// §5.5 — câu hỏi là "đã TỪNG có bản ghi nào chưa", MỌI NĂM. Bản ghi của một năm khác hẳn kỳ
    /// đang xem vẫn phải làm lượt xoá thành xoá MỀM, nếu không lịch sử năm đó biến mất cùng nó.
    /// </summary>
    [Fact(DisplayName = "DM-5 — có lịch sử ở NĂM KHÁC ⇒ xoá MỀM, hardDeleted = false, lịch sử còn nguyên")]
    public async Task Delete_WithHistoryInAnotherYear_IsSoftDelete()
    {
        var criteria = Seed("1.1");
        _repository.Assessments.Add(CriteriaAssessment.Create(criteria.Id, new DateOnly(2024, 3, 10)));

        var result = await DeleteHandler().Handle(new DeleteCriteriaCommand(criteria.Id), CancellationToken.None);

        Assert.False(result.Data!.HardDeleted);
        Assert.Empty(_repository.Removed);
        Assert.True(criteria.IsDeleted);
        Assert.Single(_repository.Assessments);
    }

    [Fact(DisplayName = "DM-5 — chỉ tiêu không tồn tại ⇒ 404")]
    public async Task Delete_UnknownCriteria_Is404()
    {
        var result = await DeleteHandler().Handle(
            new DeleteCriteriaCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal("CRITERIA.NOT_FOUND", result.BusinessCode);
    }
}
