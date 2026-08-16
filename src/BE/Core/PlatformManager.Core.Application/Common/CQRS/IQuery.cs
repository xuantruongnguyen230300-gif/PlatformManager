using MediatR;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Common.CQRS;

public interface IQuery<TResult> : IRequest<IApiResult<TResult>>;
