using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Models;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>GET /api/users — grid màn "Quản trị người dùng".</summary>
public sealed record GetUsersListQuery(int Page = 1, int PageSize = 20, string? SearchText = null)
    : IQuery<PagedList<UserDto>>;

public sealed class GetUsersListHandler(IUserAdminService userAdminService)
    : BaseResponse, IRequestHandler<GetUsersListQuery, IApiResult<PagedList<UserDto>>>
{
    public async Task<IApiResult<PagedList<UserDto>>> Handle(GetUsersListQuery query, CancellationToken ct)
    {
        var result = await userAdminService.GetListAsync(
            Math.Max(query.Page, 1), query.PageSize <= 0 ? 20 : query.PageSize, query.SearchText, ct);

        return Ok(result);
    }
}
