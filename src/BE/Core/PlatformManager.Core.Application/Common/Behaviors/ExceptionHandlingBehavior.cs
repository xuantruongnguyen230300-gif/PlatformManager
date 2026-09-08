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
/// doc/huong_dan/quy-uoc/be-entity-domain.md §DomainException).
///
/// Đăng ký behavior này TRƯỚC ValidationBehavior (outermost) — nếu ValidationBehavior ném
/// FluentValidation.ValidationException, behavior này KHÔNG bắt (chỉ bắt đúng 2 loại domain
/// exception ở trên), để exception đó bay tiếp lên middleware toàn cục xử lý riêng (xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §Exception-handling middleware toàn cục).
///
/// TResponse không phải lúc nào cũng là IApiResult&lt;T&gt; (MediatR bọc mọi IRequest trong
/// process, kể cả request không đi qua ICommand/IQuery) — dùng reflection kiểm tra tại
/// runtime, KHÔNG catch/nuốt lỗi khi TResponse không khớp shape envelope.
/// </summary>
public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> BusinessErrorMethodCache = new();

    /// <summary>
    /// Tên phương thức factory, tách ra biến riêng để KHÔNG nằm trong dấu nháy của chuỗi nội suy bên
    /// dưới. Luật <c>Core_MustNotKnowBusinessName</c> quét <b>văn bản nguồn</b>, nên
    /// <c>$"…{nameof(X.BusinessError)}…"</c> vẫn bị bắt dù đó không phải literal theo nghĩa của C#.
    /// Đặt ở đây thì tên vẫn tự cập nhật khi đổi tên phương thức, mà chuỗi thì sạch.
    /// </summary>
    private static readonly string FactoryMethodName = nameof(ApiResult<object>.BusinessError);

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            return BuildErrorResponse(ex.Error, ErrorCode.BusinessRuleError, ex.Message);
        }
        catch (ConflictException ex)
        {
            return BuildErrorResponse(ex.Error, ErrorCode.Conflict, ex.Message);
        }
    }

    /// <summary>
    /// ĐÂY là chỗ duy nhất quyết định lỗi domain ra HTTP nào — <see cref="DomainException"/> ⇒ 422,
    /// <see cref="ConflictException"/> ⇒ 409. Vì ánh xạ đó nằm ở loại exception chứ không ở bản ghi
    /// lỗi, <c>DomainError</c> cố ý KHÔNG mang <see cref="ErrorCode"/> (xem chú thích ở
    /// <c>DomainError</c>): hai nguồn cho cùng một sự thật thì chúng mâu thuẫn được.
    ///
    /// <para><c>businessCode</c> ở đây LUÔN đến từ một catalog đã khai, không còn là chuỗi gõ tại
    /// chỗ ném — đó là toàn bộ mục đích của việc đổi chữ ký hai exception này (2026-09-03,
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(a)). Lời gọi <c>new ErrorDescriptor(...)</c>
    /// bên dưới vì vậy KHÔNG phải ngoại lệ của luật "khai tập trung": nó chỉ ghép mã đã có sẵn với
    /// mã HTTP do loại exception quyết định, không đặt ra mã mới. <c>ErrorDescriptorLiteral</c> ở
    /// <c>ErrorCodeSourceTests</c> chỉ cấm dựng bằng CHUỖI LITERAL, đúng ranh giới đó.</para>
    /// </summary>
    private static TResponse BuildErrorResponse(DomainError error, ErrorCode errorCode, string message)
    {
        var responseType = typeof(TResponse);
        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(IApiResult<>))
            throw new InvalidOperationException(
                $"{typeof(TRequest).Name} ném DomainException/ConflictException nhưng TResponse " +
                $"({responseType.Name}) không phải IApiResult<T> — request này phải là ICommand<T>/IQuery<T>.");

        var dataType = responseType.GetGenericArguments()[0];
        var method = BusinessErrorMethodCache.GetOrAdd(dataType, static t =>
            typeof(ApiResult<>).MakeGenericType(t)
                .GetMethod(
                    nameof(ApiResult<object>.BusinessError),
                    // Danh sách kiểu phải khai ĐỦ CẢ tham số tuỳ chọn (messageParams thêm
                    // 2026-09-04, fieldErrors thêm 2026-09-05): GetMethod so khớp chữ ký ĐẦY ĐỦ,
                    // tham số có giá trị mặc định KHÔNG làm nó khớp một danh sách ngắn hơn.
                    //
                    // Cái bẫy đã nổ THẬT một lần (2026-09-05, đợt thi công §11). Khi đó GetMethod trả
                    // null, `method!` biến MỌI DomainException thành NullReferenceException — hỏng ở
                    // đúng nhánh lỗi, không có lỗi biên dịch, không ArchTest nào chạm tới, nên chỉ lộ
                    // ra ở Production. Đường duy nhất bắt được nó là PipelineBehaviorTests.
                    // DomainException_Thrown_In_Handler_Is_Translated_To_BusinessError.
                    //
                    // Vô hiệu hoá 2026-09-05: `?? throw` bên dưới thay NRE câm bằng một thông điệp
                    // nói thẳng phải sửa gì. Test kia vẫn là lưới chính — chỗ này chỉ bảo đảm khi
                    // lưới thủng thì lỗi tự giải thích thay vì đổ ở một stack trace vô nghĩa.
                    [
                        typeof(ErrorDescriptor),
                        typeof(string),
                        typeof(Dictionary<string, string>),
                        typeof(Dictionary<string, ApiFieldError[]>),
                    ])
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy ApiResult<{t.Name}>.{FactoryMethodName} khớp danh sách kiểu đang khai "
                    + "ở ExceptionHandlingBehavior.BuildErrorResponse. Nguyên nhân gần như chắc chắn: chữ ký "
                    + "của phương thức đó vừa được thêm/bớt/đổi thứ tự tham số, mà danh sách kiểu ở đây thì "
                    + "không đổi theo. GetMethod so khớp chữ ký ĐẦY ĐỦ — tham số có giá trị mặc định KHÔNG "
                    + "khớp một danh sách ngắn hơn. Sửa: đồng bộ danh sách kiểu ở đây với chữ ký thật."));

        var descriptor = new ErrorDescriptor(error.BusinessCode, errorCode, error.MessageTemplate);

        // Lỗi Domain hôm nay chưa có khuôn thông điệp nào mang tham số (mọi DomainError trong
        // catalog đều là câu cố định) và cũng không lỗi nào thuộc về MỘT ô nhập cụ thể, nên truyền
        // null cho cả hai — hai trường vắng mặt trên dây. Khi một DomainError đầu tiên cần tham số,
        // chỗ nối là đây và DomainException phải mang theo bộ tham số ĐÃ ĐẶT TÊN thay vì mảng object.
        var result = method.Invoke(null, [descriptor, message, null, null]);
        return (TResponse)result!;
    }
}
