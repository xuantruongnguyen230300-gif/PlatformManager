using System.Collections.Concurrent;
using System.Reflection;
using MediatR;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Application.Common.Behaviors;

/// <summary>
/// Bắt DomainException/ConflictException ném ra từ handler (thường là từ entity factory/
/// mutation method) và dịch thành IApiResult lỗi ĐÚNG NGAY TẠI Application layer — không
/// để 2 exception này lọt thẳng ra Api layer thành lỗi 500 chung chung (xem
/// .claude/rules/entity-domain.md §DomainException).
///
/// Đăng ký behavior này TRƯỚC ValidationBehavior (outermost) — nếu ValidationBehavior ném
/// FluentValidation.ValidationException, behavior này KHÔNG bắt (chỉ bắt đúng 2 loại domain
/// exception ở trên), để exception đó bay tiếp lên middleware toàn cục xử lý riêng (xem
/// .claude/rules/api-controller.md §Exception-handling middleware toàn cục).
///
/// TResponse không phải lúc nào cũng là IApiResult&lt;T&gt; (MediatR bọc mọi IRequest trong
/// process, kể cả request không đi qua ICommand/IQuery) — dùng reflection kiểm tra tại
/// runtime, KHÔNG catch/nuốt lỗi khi TResponse không khớp shape envelope.
/// </summary>
public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, MethodInfo?> BusinessErrorMethodCache = new();

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            return BuildErrorResponse(ex.Code, ErrorCode.BusinessRuleError, ex.Message);
        }
        catch (ConflictException ex)
        {
            return BuildErrorResponse(ex.Code, ErrorCode.Conflict, ex.Message);
        }
    }

    private static TResponse BuildErrorResponse(string businessCode, ErrorCode errorCode, string message)
    {
        var responseType = typeof(TResponse);
        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(IApiResult<>))
            throw new InvalidOperationException(
                $"{typeof(TRequest).Name} ném DomainException/ConflictException nhưng TResponse " +
                $"({responseType.Name}) không phải IApiResult<T> — request này phải là ICommand<T>/IQuery<T>.");

        var dataType = responseType.GetGenericArguments()[0];
        var method = BusinessErrorMethodCache.GetOrAdd(dataType, static t =>
            typeof(ApiResult<>).MakeGenericType(t)
                .GetMethod(nameof(ApiResult<object>.BusinessError), [typeof(ErrorDescriptor), typeof(string)]));

        var descriptor = new ErrorDescriptor(businessCode, errorCode, message);
        var result = method!.Invoke(null, [descriptor, message]);
        return (TResponse)result!;
    }
}
