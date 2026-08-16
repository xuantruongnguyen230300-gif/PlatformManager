using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;

/// <summary>GET /api/criteria-groups — dropdown chọn nhóm cho tab "Chỉ tiêu".</summary>
public sealed record GetCriteriaGroupsListQuery : IQuery<List<CriteriaGroupDto>>;

public sealed class GetCriteriaGroupsListHandler(ICriteriaGroupRepository repo)
    : BaseResponse, IRequestHandler<GetCriteriaGroupsListQuery, IApiResult<List<CriteriaGroupDto>>>
{
    public async Task<IApiResult<List<CriteriaGroupDto>>> Handle(GetCriteriaGroupsListQuery request, CancellationToken ct)
    {
        var groups = await repo.GetAllAsync(ct);
        var dtos = groups
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new CriteriaGroupDto(g.Id, g.Code, g.Name, g.DisplayOrder))
            .ToList();

        return Ok(dtos);
    }
}
