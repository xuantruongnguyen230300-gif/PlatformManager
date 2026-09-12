using PlatformManager.Business.Domain.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Ánh xạ luật MÃ chỉ tiêu (§2) sang catalog lỗi của đường GHI TAY.
///
/// <para><b>Vì sao phải kiểm ở handler dù entity đã ép luật:</b> <c>Criteria.SetCode</c> ném
/// <c>DomainException</c>, mà <c>ExceptionHandlingBehavior</c> dịch mọi <c>DomainException</c>
/// thành <b>422</b>. Hợp đồng DM-3/DM-4 thì đòi <b>400</b> cho bốn ca mã sai — chúng là khuôn dữ
/// liệu sai, không phải vi phạm luật nghiệp vụ trên dữ liệu hợp khuôn. Kiểm trước ở handler là cách
/// duy nhất cho ra đúng mã HTTP mà không đụng vào lớp dịch exception dùng chung.</para>
///
/// <para>Entity VẪN giữ phép ném của nó — đó là lưới an toàn cho mọi đường ghi khác (seeder, test,
/// đường thứ tư về sau). Hai lớp cùng một luật, đọc từ CÙNG <see cref="CriteriaCode"/>, nên chúng
/// không lệch nhau được.</para>
///
/// <para><b>Thứ tự kiểm CÓ NGHĨA</b> — độ dài trước, khuôn sau, đoạn sau cùng. Đảo lại thì một mã
/// 25 chữ số nhận <c>CODE_FORMAT_INVALID</c> (vì <see cref="CriteriaCode.IsWellFormed"/> cũng chặn
/// độ dài) thay vì <c>CODE_TOO_LONG</c>, và câu hướng dẫn người dùng sẽ chỉ sai chỗ cần sửa. Cùng
/// thứ tự với đường import.</para>
/// </summary>
public static class CriteriaCodeRules
{
    /// <summary>
    /// <c>null</c> = mã hợp lệ. Ngược lại là cặp (descriptor, tham số câu) để handler trả
    /// <c>Fail</c>.
    /// </summary>
    public static (ErrorDescriptor Error, (string Name, object? Value)[] Args)? Validate(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (CriteriaErrors.AsValidation(CriteriaDomainErrors.CodeRequired), []);

        var trimmed = code.Trim();

        if (trimmed.Length > CriteriaCode.MaxLength)
            return (CriteriaErrors.CodeTooLong, [("MaxLength", CriteriaCode.MaxLength)]);

        if (!CriteriaCode.IsWellFormed(trimmed))
            return (CriteriaErrors.AsValidation(CriteriaDomainErrors.CodeFormatInvalid), []);

        if (CriteriaCode.HasSegmentTooLong(trimmed))
            return (
                CriteriaErrors.AsValidation(
                    CriteriaDomainErrors.CodeSegmentTooLong,
                    "Mỗi đoạn của mã chỉ tiêu tối đa {MaxSegmentDigits} chữ số."),
                [("MaxSegmentDigits", CriteriaCode.MaxSegmentDigits)]);

        return null;
    }
}
