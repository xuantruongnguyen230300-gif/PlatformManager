using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Common;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Bộ lọc ĐÃ CHUẨN HOÁ của lưới chỉ tiêu — dùng CHUNG bởi DM-2, bảng chi tiết của DB-1, và (vòng
/// 2) endpoint export DB-4.
///
/// <para><b>Vì sao một kiểu chung thay vì mỗi handler tự nhận tham số</b> (Q68): nghiệm thu
/// <i>"số dòng file xuất = số phần tử <c>data.table</c>"</i> là thứ DUY NHẤT cưỡng chế hai đường
/// còn lọc giống nhau. Hai bộ lọc viết rời nhau nghĩa là sửa một bên không làm bên kia đỏ. Gói bộ
/// lọc vào một kiểu, và dựng <c>IQueryable</c> đã lọc ở MỘT chỗ (<c>Business.Persistence</c>) —
/// hai handler chỉ khác phép <c>Select</c>.</para>
/// </summary>
/// <param name="SearchNormalized">
/// Từ khoá đã qua <see cref="SearchTextNormalizer"/> — so với cột <c>NameNormalized</c> theo kiểu
/// CHỨA CHUỖI (Q59). <c>null</c> = không lọc theo từ khoá.
/// </param>
/// <param name="SearchCode">
/// Từ khoá NGUYÊN VĂN đã cắt khoảng trắng — dùng cho nhánh khớp mã theo ĐOẠN (Q59):
/// <c>Code = q</c> HOẶC <c>Code</c> bắt đầu bằng <c>q + "."</c>. Gõ <c>4.2</c> ra <c>4.2</c> và
/// mọi <c>4.2.x</c>, KHÔNG ra <c>4.20</c>–<c>4.29</c>. So ORDINAL — mã chỉ gồm chữ số và dấu
/// chấm, không có gì để chuẩn hoá.
/// </param>
/// <param name="Status">Đã quy về đúng 1 trong 4 giá trị chuẩn, hoặc <c>null</c> = không lọc.</param>
/// <param name="Period">Kỳ đang xem — quyết định khoảng ngày chọn bản ghi đánh giá đại diện (§5.2).</param>
public sealed record CriteriaFilterSpec(
    string? SearchNormalized,
    string? SearchCode,
    Guid? GroupId,
    string? Status,
    PeriodRange Period)
{
    /// <summary>
    /// Dựng bộ lọc từ tham số thô. <paramref name="status"/> phải ĐÃ được quy chuẩn ở handler —
    /// giá trị lạ là <c>400</c>, không phải "bỏ lọc" (doc/contracts/danh-muc-dti.md
    /// §"Mã lỗi của DM-2").
    /// </summary>
    public static CriteriaFilterSpec Create(string? search, Guid? groupId, string? status, PeriodRange period)
    {
        // Khoảng trắng thuần được coi như "không lọc", giống SearchText của lưới người dùng Core —
        // không phải một giá trị sai.
        var trimmed = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        return new CriteriaFilterSpec(
            trimmed is null ? null : SearchTextNormalizer.Normalize(trimmed),
            trimmed,
            groupId,
            string.IsNullOrWhiteSpace(status) ? null : status,
            period);
    }
}
