import { IWritePeriod } from '../models/danh-muc-dti.model';

/** `Kỳ trong năm` = `Tất cả` — phép chiếu "bản ghi mới nhất trong năm", KHÔNG phải một kỳ. */
export const PERIOD_ALL = 'all';

/** Khuôn tuần ISO ra dây. Tháng (`YYYY-MM`) cố ý KHÔNG có khuôn ở đây — xem `buildWritePeriod`. */
const WEEK_PERIOD = /^\d{4}-W\d{1,2}$/;

/**
 * CHỖ DUY NHẤT dựng cặp `{ Period, Year }` của một lời ghi — ba đường ghi (DM-4 dialog, DM-6 sửa
 * inline, DM-7 import) cùng gọi hàm này.
 *
 * 📖 Luật: `spec/danh-muc-dti/ui-spec.md` §7.4b, ràng buộc 1. Ba bản sao là ba cơ hội để **một**
 * đường quên `Year` khi `"all"` — đường đó nhận `400` validate trong khi hai đường kia chạy, và
 * không có gì làm đỏ lúc biên dịch.
 *
 * ## Hàm này KHÔNG quy đổi gì (Q40, 2026-09-10)
 *
 * `"all"` đi ra dây **nguyên văn**; server quy nó về tuần ISO chứa hôm nay. FE tự thay `"all"`
 * bằng một tuần nghĩa là FE phải có lịch ISO riêng — và hai bản cài lịch thì lệch nhau đúng vào
 * đêm Chủ nhật, hoặc trên một máy sai múi giờ. Kỳ thật sự được ghi thì FE đọc **sau** khi lưu, qua
 * `AssessmentPeriod` / `AssessmentPeriodLabel` của response.
 *
 * ## `null` = KHÔNG CÓ LỜI GHI NÀO ĐỂ GỬI
 *
 * Ô lọc đang chọn một **tháng** thì bảng chỉ đọc (Q37) — không control nào mở được, nên hàm này
 * lẽ ra không được gọi. Trả `null` thay vì "quy tháng về một tuần cho chắc": mọi cách neo một
 * tháng vào một tuần đều sai (tuần ISO không nằm gọn trong tháng), nên một `YYYY-MM` chạm tới đây
 * là dấu hiệu điều kiện chỉ-đọc đã bị bỏ sót ở đâu đó — phải lộ ra, không phải được vá ngầm.
 *
 * @param period nguyên giá trị ô `Kỳ trong năm`
 * @param year   nguyên giá trị ô `Năm đánh giá`
 */
export function buildWritePeriod(period: string, year: number): IWritePeriod | null {
  if (period === PERIOD_ALL) {
    // T15 — `"all"` tự nó không chứa năm nào. Thiếu `Year` thì lớp chặn thứ hai ở server không
    // tồn tại, chỉ còn FE ẩn nút; mà ẩn nút là trải nghiệm, không phải luật.
    return { Period: PERIOD_ALL, Year: year };
  }
  if (WEEK_PERIOD.test(period)) {
    // Năm đã nằm sẵn trong chuỗi tuần. Không gửi `Year` — gửi thì server bỏ qua, nhưng một trường
    // bị bỏ qua trên dây là một trường người đọc log phải đoán xem nó có ý nghĩa gì.
    return { Period: period };
  }
  return null;
}
