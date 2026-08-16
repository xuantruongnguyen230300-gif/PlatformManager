using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common.Behaviors;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Domain.Common;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Test kiến trúc quan trọng nhất của P2 (xem
/// doc/huong_dan/wiki-core/be/trien-khai/03-p2-platform-application.md §9) — không phải
/// ArchTest reflection thuần, mà chạy THẬT qua MediatR với 2 command/handler giả lập
/// (pattern "PingCommand") để verify hành vi 2 pipeline behavior, KHÔNG chỉ verify chúng tồn tại.
/// </summary>
public class PipelineBehaviorTests
{
    public sealed record ThrowDomainExceptionCommand : ICommand<string>;

    public sealed class ThrowDomainExceptionHandler : IRequestHandler<ThrowDomainExceptionCommand, IApiResult<string>>
    {
        public Task<IApiResult<string>> Handle(ThrowDomainExceptionCommand request, CancellationToken ct)
            => throw new DomainException("TEST.DOMAIN_ERROR", "Lỗi domain giả lập cho test.");
    }

    public sealed record RequireNonEmptyCommand(string Value) : ICommand<string>;

    public sealed class RequireNonEmptyValidator : AbstractValidator<RequireNonEmptyCommand>
    {
        public RequireNonEmptyValidator() => RuleFor(x => x.Value).NotEmpty();
    }

    public sealed class RequireNonEmptyHandler : IRequestHandler<RequireNonEmptyCommand, IApiResult<string>>
    {
        public Task<IApiResult<string>> Handle(RequireNonEmptyCommand request, CancellationToken ct)
            => Task.FromResult<IApiResult<string>>(ApiResult<string>.Success(request.Value));
    }

    private static IMediator BuildMediator()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PipelineBehaviorTests).Assembly));
        services.AddValidatorsFromAssembly(typeof(PipelineBehaviorTests).Assembly);

        // Thứ tự đăng ký PHẢI khớp Application/DependencyInjection.cs thật — ExceptionHandling
        // trước (outermost), Validation sau.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task DomainException_Thrown_In_Handler_Is_Translated_To_BusinessError()
    {
        var mediator = BuildMediator();

        var result = await mediator.Send(new ThrowDomainExceptionCommand());

        Assert.Equal(ApiResultStatus.BUSINESS_ERROR, result.Status);
        Assert.Equal(ErrorCode.BusinessRuleError, result.Code);
        Assert.Equal("TEST.DOMAIN_ERROR", result.BusinessCode);
    }

    [Fact]
    public async Task ValidationException_Is_NOT_Swallowed_By_ExceptionHandlingBehavior()
    {
        var mediator = BuildMediator();

        // ExceptionHandlingBehavior chỉ bắt DomainException/ConflictException — phải để
        // FluentValidation.ValidationException bay tiếp ra ngoài mediator.Send() (Api layer's
        // GlobalExceptionHandler mới là nơi dịch nó thành 400+Fields, không test được ở đây vì
        // đó là middleware ASP.NET Core, ngoài phạm vi ArchTests thuần Application).
        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(new RequireNonEmptyCommand("")));
    }

    [Fact]
    public async Task Valid_Command_Passes_Through_Both_Behaviors_Unchanged()
    {
        var mediator = BuildMediator();

        var result = await mediator.Send(new RequireNonEmptyCommand("ok"));

        Assert.Equal(ApiResultStatus.SUCCESS, result.Status);
        Assert.Equal("ok", result.Data);
    }
}
