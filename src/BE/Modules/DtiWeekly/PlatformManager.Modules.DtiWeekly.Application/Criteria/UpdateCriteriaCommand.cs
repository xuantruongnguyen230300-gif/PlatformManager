using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;

namespace PlatformManager.Modules.DtiWeekly.Application.Criteria;

/// <summary>PUT /api/criteria/{id} — cho phép đổi cả Code (xem
/// spec/danh-muc-dti/business-rules.md mục 1.2).</summary>
public sealed record UpdateCriteriaCommand(Guid Id, string Code, string Name, Guid GroupId, decimal MaxScore)
    : ICommand<bool>;

public sealed class UpdateCriteriaValidator : AbstractValidator<UpdateCriteriaCommand>
{
    public UpdateCriteriaValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.MaxScore).GreaterThan(0);
    }
}

public sealed class UpdateCriteriaHandler(
    ICriteriaRepository repo, ICriteriaGroupRepository groupRepo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<UpdateCriteriaCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UpdateCriteriaCommand cmd, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null)
            return Fail<bool>(CriteriaErrors.NotFound);

        if (await repo.CodeExistsAsync(cmd.Code, excludeId: cmd.Id, ct))
            return Fail<bool>(CriteriaErrors.DuplicateCode, cmd.Code);

        if (await groupRepo.GetByIdAsync(cmd.GroupId, ct) is null)
            return Fail<bool>(CriteriaErrors.GroupNotFound);

        entity.UpdateDetails(cmd.Code, cmd.Name, cmd.GroupId, cmd.MaxScore);
        await uow.SaveChangesAsync(ct);

        return Ok(true);
    }
}
