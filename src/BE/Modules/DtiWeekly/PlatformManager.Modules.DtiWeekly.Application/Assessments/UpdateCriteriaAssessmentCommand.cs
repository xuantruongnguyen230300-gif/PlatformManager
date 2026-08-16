using FluentValidation;
using MediatR;
using PlatformManager.Modules.DtiWeekly.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Modules.DtiWeekly.Application.Criteria;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <summary>
/// PUT /api/criteria/{id}/assessment — sửa tay tại tab "Đánh giá theo tuần". CHỈ
/// ProgressPercent/Note được sửa qua đây (khớp đúng dashboard.html gốc — không có control
/// cho SelfScore/VerifiedScore/Status/OwnerId/Deadline, xem
/// spec/dashboard-dti-weekly/business-rules.md mục 5). Luôn ghi vào bản ghi "hôm nay" —
/// ViewYear/ViewPeriod/ViewPeriodValue chỉ để GUARD chặn sửa khi FE đang xem dữ liệu
/// lịch sử (xem spec/danh-muc-dti/business-rules.md mục 2.4), không ảnh hưởng ngày ghi.
/// </summary>
public sealed record UpdateCriteriaAssessmentCommand(
    Guid CriteriaId,
    decimal ProgressPercent,
    string? Note,
    int ViewYear,
    string ViewPeriod,
    string? ViewPeriodValue) : ICommand<CriteriaAssessmentDto>;

public sealed class UpdateCriteriaAssessmentValidator : AbstractValidator<UpdateCriteriaAssessmentCommand>
{
    public UpdateCriteriaAssessmentValidator()
    {
        RuleFor(x => x.CriteriaId).NotEmpty();
        RuleFor(x => x.ViewPeriod).NotEmpty().Must(p => p is "all" or "week" or "month");
        // Kẹp [0,100] ở entity (Math.Clamp) khớp hành vi JS gốc — validator không chặn cứng,
        // chỉ chặn giá trị rõ ràng vô nghĩa (NaN đã bị model binding chặn trước đó).
    }
}

public sealed class UpdateCriteriaAssessmentHandler(
    ICriteriaRepository criteriaRepo, IAssessmentUpsertService upsertService, IUnitOfWork uow, IDateTimeProvider clock)
    : BaseResponse, IRequestHandler<UpdateCriteriaAssessmentCommand, IApiResult<CriteriaAssessmentDto>>
{
    public async Task<IApiResult<CriteriaAssessmentDto>> Handle(UpdateCriteriaAssessmentCommand cmd, CancellationToken ct)
    {
        var criteria = await criteriaRepo.GetByIdAsync(cmd.CriteriaId, ct);
        if (criteria is null)
            return Fail<CriteriaAssessmentDto>(AssessmentErrors.CriteriaNotFound);

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        if (!PeriodRangeCalculator.IsLive(cmd.ViewYear, cmd.ViewPeriod, today))
            return Fail<CriteriaAssessmentDto>(AssessmentErrors.NotEditableOutsideLivePeriod);

        var record = await upsertService.UpsertTodayAsync(cmd.CriteriaId, entity =>
        {
            entity.UpdateProgress(cmd.ProgressPercent);
            entity.UpdateNote(cmd.Note);
        }, ct);

        await uow.SaveChangesAsync(ct);

        return Ok(new CriteriaAssessmentDto(
            record.Id, record.CriteriaId, record.ProgressPercent, record.SelfScore, record.VerifiedScore,
            record.Status, record.OwnerId, record.Deadline, record.Note,
            record.DateCreate.HasValue ? DateOnly.FromDateTime(record.DateCreate.Value.UtcDateTime) : today));
    }
}
