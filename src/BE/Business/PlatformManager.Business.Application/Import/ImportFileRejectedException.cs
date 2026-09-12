using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// Lỗi của <b>CẢ FILE</b> mà nguyên nhân là NGHIỆP VỤ, không phải hạ tầng — file thiếu cột bắt
/// buộc, file vượt trần số dòng.
///
/// <para><b>Vì sao cần một loại ngoại lệ riêng thay vì ném <see cref="InvalidDataException"/>:</b>
/// hai nhóm lỗi cả-file kết thúc ở cùng một chỗ (job <c>Failed</c>) nhưng khác nhau ở thứ quan
/// trọng nhất với người dùng — nhóm này có <c>businessCode</c> để FE dịch thành câu đọc được, nhóm
/// kia (job crash, file hỏng ở mức byte) thì không. Không phân biệt được ở tầng ngoại lệ thì
/// <c>MarkFailed</c> không có gì để ghi vào <c>ErrorCode</c>.</para>
///
/// <para>⚠️ <b>KHÔNG dùng cho lỗi một DÒNG.</b> Ném ở đây làm cả lượt nạp thành <c>Failed</c>, và
/// theo <b>Q64</b> thì không dòng nào được ghi — một ô trống sẽ cuộn ngược cả file. Lỗi dòng đi
/// vào <c>result.errors[]</c> và job vẫn <c>Succeeded</c>.</para>
/// </summary>
public sealed class ImportFileRejectedException : Exception
{
    public ImportFileRejectedException(ErrorDescriptor descriptor, params (string Name, object? Value)[] args)
        : base(BuildMessage(descriptor, args))
    {
        BusinessCode = descriptor.BusinessCode;

        var messageParams = new Dictionary<string, string>(args.Length, StringComparer.Ordinal);
        foreach (var (name, value) in args)
            messageParams[name] = MessageParamPolicy.Stringify(value);

        MessageParams = messageParams;
    }

    /// <summary>Mã đi ra <c>errorCode</c> của DM-7 bước 2 — FE dịch như mọi <c>businessCode</c>.</summary>
    public string BusinessCode { get; }

    /// <summary>
    /// Tham số ĐẶT TÊN của câu. Hôm nay chúng chỉ đi vào câu fallback dev-facing, chưa ra dây riêng
    /// — hợp đồng bước 2 mới có <c>errorCode</c> chứ chưa có <c>errorMessageParams</c>. Giữ chúng
    /// ở đây để lượt mở rộng hợp đồng sau không phải đi thu thập lại.
    /// </summary>
    public IReadOnlyDictionary<string, string> MessageParams { get; }

    /// <summary>
    /// Ráp câu fallback ngay tại chỗ ném, bằng chính chuỗi thay thế đơn giản theo TÊN — không dùng
    /// <c>string.Format</c>, vì khuôn của repo này là chỗ giữ ĐẶT TÊN (<c>{MaxRows}</c>) chứ không
    /// phải <c>{0}</c>.
    /// </summary>
    private static string BuildMessage(ErrorDescriptor descriptor, (string Name, object? Value)[] args)
    {
        var message = descriptor.MessageTemplate;

        foreach (var (name, value) in args)
            message = message.Replace($"{{{name}}}", MessageParamPolicy.Stringify(value), StringComparison.Ordinal);

        return message;
    }
}
