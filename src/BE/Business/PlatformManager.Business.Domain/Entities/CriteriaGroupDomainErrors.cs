using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// Catalog lỗi BẤT BIẾN của <see cref="CriteriaGroup"/> — khuôn <c>{Entity}Errors.cs</c>, kiểu
/// <see cref="DomainError"/> (tầng Domain không được biết tới <c>ErrorCode</c>/HTTP, lý do đầy
/// đủ ở docstring của <c>DomainError</c>).
///
/// <para>Tên file có hậu tố <c>DomainErrors</c> chứ không <c>Errors</c> trơn để KHÔNG trùng tên
/// kiểu với <c>CriteriaGroupErrors</c>/<c>CriteriaErrors</c> ở tầng Application (nơi giữ
/// <c>ErrorDescriptor</c> của hợp đồng API). Hai kiểu cùng tên ở hai namespace vẫn biên dịch,
/// nhưng file nào <c>using</c> cả hai sẽ mơ hồ — và đó là file người ta viết vào vòng 2.</para>
/// </summary>
public static class CriteriaGroupDomainErrors
{
    public static readonly DomainError CodeRequired = new(
        "CRITERIA_GROUP.CODE_REQUIRED", "Mã nhóm chỉ tiêu không được để trống.");

    public static readonly DomainError NameRequired = new(
        "CRITERIA_GROUP.NAME_REQUIRED", "Tên nhóm chỉ tiêu không được để trống.");
}
