using MediatR;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Criteria;

/// <summary>DELETE /api/criteria/{id} — hard-delete nếu Criteria chưa từng có
/// CriteriaAssessment (kể cả đã xoá mềm), ngược lại chỉ soft-delete. Xem
/// spec/danh-muc-dti/business-rules.md mục 1.3 — quyết định TỰ ĐỘNG, không phải lựa chọn
/// của người gọi.</summary>
public sealed record DeleteCriteriaCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteCriteriaHandler(
    ICriteriaRepository repo, ICriteriaAssessmentRepository assessmentRepo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<DeleteCriteriaCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(DeleteCriteriaCommand cmd, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null)
            return Fail<bool>(CriteriaErrors.NotFound);

        var hasHistory = await assessmentRepo.AnyEverForCriteriaAsync(cmd.Id, ct);
        if (hasHistory)
            entity.IsDelete = true; // soft-delete — IsDelete là field kỹ thuật public set (BaseEntity)
        else
            repo.Remove(entity); // hard-delete — an toàn vì không có dữ liệu lịch sử phụ thuộc

        await uow.SaveChangesAsync(ct);
        return Ok(true);
    }
}
