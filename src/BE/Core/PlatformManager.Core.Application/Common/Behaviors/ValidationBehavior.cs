using FluentValidation;
using MediatR;

namespace PlatformManager.Core.Application.Common.Behaviors;

/// <summary>
/// Chạy mọi FluentValidation validator đăng ký cho TRequest TRƯỚC khi vào handler.
/// Behavior NÉM ValidationException, không tự dựng response lỗi — dịch exception này
/// thành IApiResult là việc của middleware toàn cục (xem .claude/rules/api-controller.md
/// §Exception-handling middleware toàn cục). Đăng ký behavior này SAU
/// ExceptionHandlingBehavior trong pipeline (xem cqrs-handler.md).
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
