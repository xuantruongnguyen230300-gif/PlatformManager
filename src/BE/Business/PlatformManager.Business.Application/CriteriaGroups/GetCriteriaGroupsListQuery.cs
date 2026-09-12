using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.CriteriaGroups;

/// <summary>
/// <b>DM-1</b> — <c>GET /api/criteria-groups</c>. Không tham số: danh mục nhóm là danh mục ĐÓNG
/// (§6.3) và chỉ có 6 hàng (§1.6), nên không phân trang, không lọc.
/// </summary>
public sealed record GetCriteriaGroupsListQuery : IQuery<IReadOnlyList<CriteriaGroupDto>>;

public sealed class GetCriteriaGroupsListHandler(ICriteriaGroupRepository repository)
    : BaseResponse, IRequestHandler<GetCriteriaGroupsListQuery, IApiResult<IReadOnlyList<CriteriaGroupDto>>>
{
    public async Task<IApiResult<IReadOnlyList<CriteriaGroupDto>>> Handle(
        GetCriteriaGroupsListQuery query, CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));
}
