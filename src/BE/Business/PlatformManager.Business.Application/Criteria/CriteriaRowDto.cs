namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Một dòng của lưới Danh mục DTI — doc/contracts/danh-muc-dti.md DM-2.
///
/// <para><b>Thuần dữ liệu: KHÔNG trường quyền nào ở đây.</b> <c>isEditable</c> đã RỜI khỏi cấp
/// dòng (2026-09-06) — cả ba điều kiện của nó thuộc về REQUEST (quyền của người gọi · đơn vị kỳ ·
/// năm đang lọc) nên giá trị giống hệt ở mọi dòng, và một bản sao mỗi dòng vừa tạo chỗ cho chúng
/// lệch nhau vừa vô dụng ở ca lưới rỗng. Khối quyền ở cấp màn:
/// <see cref="CriteriaGridDto"/>.</para>
///
/// <para><b>Trường <c>null</c> VẮNG MẶT khỏi JSON</b>, không ra dây thành <c>"x": null</c>
/// (<c>JsonIgnoreCondition.WhenWritingNull</c>, khai ở <c>Program.cs</c> cho cả đường MVC lẫn
/// <c>Http.Json</c>). Đa số trường của một dòng là nullable, nên đây là chỗ luật đó cắn mạnh
/// nhất: một chỉ tiêu chưa có đánh giá trong kỳ KHÔNG có khoá <c>selfScore</c> nào cả.</para>
/// </summary>
/// <param name="Diff">
/// Trường TÍNH, không lưu, không nhận từ client: <c>verifiedScore − selfScore</c> (Q2 + Q25 —
/// đảo chiều 2026-09-05). Một trong hai vế vắng ⇒ <c>diff</c> vắng. Dấu MANG NGHĨA nghiệp vụ
/// (dương = thẩm định chấm cao hơn tự chấm) nên KHÔNG lấy trị tuyệt đối, KHÔNG đảo dấu ở tầng
/// hiển thị. FE tính lại là tạo nguồn sự thật thứ hai.
/// </param>
/// <param name="AssessmentPeriod">
/// Kỳ của CHÍNH dòng này (Q31) — <c>"YYYY-Www"</c>, tuần ISO chứa <paramref name="AssessmentDate"/>.
/// LUÔN là một tuần, không bao giờ là tháng: sau Q37 không đường nào ghi dữ liệu vào kỳ tháng.
/// Vắng mặt khi dòng chưa có bản ghi đánh giá trong phạm vi đang xem.
/// </param>
/// <param name="AssessmentPeriodLabel">
/// Nhãn kỳ BE DỰNG SẴN (<c>Tuần 33/2026 (10/08 – 16/08/2026)</c>). FE không tự ghép — ghép được
/// thì FE phải có lịch ISO riêng (spec/dashboard-dti/business-rules.md §6.3). API luôn trả cả
/// hai trường ở mọi chế độ; cột hẹp trên lưới chọn hiển thị phần nào là việc của màn hình (Q38).
/// </param>
/// <param name="Version">
/// Token optimistic concurrency của bản ghi đánh giá (cột hệ thống <c>xmin</c>, dạng chuỗi).
/// Chưa có bản ghi ⇒ vắng mặt. Vòng 1 chỉ TRẢ nó; đường ghi đọc lại nó ở vòng 2.
/// </param>
public sealed record CriteriaRowDto(
    Guid CriteriaId,
    string Code,
    string Name,
    Guid GroupId,
    string GroupCode,
    string GroupName,
    decimal MaxScore,
    Guid? AssessmentId,
    DateOnly? AssessmentDate,
    int? ProgressPercent,
    decimal? SelfScore,
    decimal? VerifiedScore,
    decimal? Diff,
    string? Status,
    Guid? OwnerId,
    string? OwnerName,
    DateOnly? Deadline,
    string? Note,
    string? Version,
    string? AssessmentPeriod,
    string? AssessmentPeriodLabel);
