using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;

namespace PlatformManager.Modules.DtiWeekly.Application.Criteria;

/// <summary>POST /api/criteria — tab "Chỉ tiêu", xem spec/danh-muc-dti/business-rules.md mục 1.1.</summary>
public sealed record CreateCriteriaCommand(string Code, string Name, Guid GroupId, decimal MaxScore)
    : ICommand<Guid>;

public sealed class CreateCriteriaValidator : AbstractValidator<CreateCriteriaCommand>
{
    public CreateCriteriaValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.MaxScore).GreaterThan(0);
    }
}

public sealed class CreateCriteriaHandler(
    ICriteriaRepository repo, ICriteriaGroupRepository groupRepo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<CreateCriteriaCommand, IApiResult<Guid>>
{
    public async Task<IApiResult<Guid>> Handle(CreateCriteriaCommand cmd, CancellationToken ct)
    {
        if (await repo.CodeExistsAsync(cmd.Code, excludeId: null, ct))
            return Fail<Guid>(CriteriaErrors.DuplicateCode, cmd.Code);

        if (await groupRepo.GetByIdAsync(cmd.GroupId, ct) is null)
            return Fail<Guid>(CriteriaErrors.GroupNotFound);

        var entity = Domain.Entities.Criteria.Create(cmd.Code, cmd.Name, cmd.GroupId, cmd.MaxScore);
        await repo.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);

        return Ok(entity.Id);
    }
}
