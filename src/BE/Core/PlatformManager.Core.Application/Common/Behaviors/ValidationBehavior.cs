using FluentValidation;
using MediatR;

namespace PlatformManager.Core.Application.Common.Behaviors;

/// <summary>
/// Chạy mọi FluentValidation validator đăng ký cho TRequest TRƯỚC khi vào handler.
/// Behavior NÉM ValidationException, không tự dựng response lỗi — dịch exception này
/// thành IApiResult là việc của middleware toàn cục (xem doc/huong_dan/quy-uoc/be-api-controller.md
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

        // MỖI validator một ValidationContext RIÊNG — KHÔNG dùng chung một context.
        //
        // Vì sao (lỗi đã đo 2026-09-01, và nó im lặng cho tới khi có request thứ hai có 2 validator):
        // rule kiểu `Custom`/`CustomAsync` báo lỗi bằng `context.AddFailure(...)`, tức ghi thẳng vào
        // ValidationContext ĐANG DÙNG CHUNG. Khi đó `ValidationResult` mà MỌI validator trả về đều
        // chứa lỗi ấy, nên `SelectMany` bên dưới đếm nó N lần với N = số validator.
        //
        // Triệu chứng: người dùng nhận đúng một lỗi nhưng thấy thông điệp lặp 2 lần trong `fields`.
        // Trước 2026-08-31 không lệnh nào có quá 1 validator nên lỗi này chưa từng lộ ra — không
        // phải hồi quy mới, mà là bẫy đã nằm sẵn và vừa đủ điều kiện kích hoạt.
        //
        // `Task.WhenAll` giữ nguyên: nay mỗi validator ghi vào context của riêng nó nên chạy song
        // song không còn tranh chấp trạng thái.
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
