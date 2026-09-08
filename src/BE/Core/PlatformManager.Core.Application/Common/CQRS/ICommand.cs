using MediatR;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Common.CQRS;

/// <summary>
/// ICommand&lt;TResult&gt; khai IRequest&lt;IApiResult&lt;TResult&gt;&gt; — envelope được ép ở tầng
/// type, handler không thể "quên" trả envelope. Xem doc/huong_dan/quy-uoc/be-cqrs-handler.md.
/// </summary>
public interface ICommand<TResult> : IRequest<IApiResult<TResult>>;
