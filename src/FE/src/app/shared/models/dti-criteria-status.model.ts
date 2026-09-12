/**
 * Bốn giá trị `Trạng thái` của một chỉ tiêu DTI, và ánh xạ sang lớp `.badge`.
 *
 * ## Vì sao file này ở `shared/` chứ không ở một trong hai module
 *
 * `spec/dashboard-dti/business-rules.md` §2 gọi bảng dưới đây là **"bảng ánh xạ DUY NHẤT"** và
 * chốt nó áp cho **cả hai** màn (Q10). Gate G8 cấm `modules/dashboard/` và `modules/danh-muc-dti/`
 * import chéo nhau, nên "duy nhất" chỉ có một chỗ đứng: `shared/`. Đó cũng đúng lối thoát mà
 * thông điệp của chính G8 chỉ ra (`src/FE/eslint.config.js`).
 *
 * Căng thẳng "`shared/` đi theo CoreBase, mà đây là nghiệp vụ DTI" đã được ghi nhận và báo lên
 * điều phối cùng lượt — xem đầu `shared/models/dti-period.model.ts`. Hai copy của một bảng ánh
 * xạ thì chắc chắn lệch nhau, và lệch ở đây nghĩa là **cùng một chỉ tiêu mang hai màu ở hai màn**.
 *
 * ## Ranh giới: đây là HIỂN THỊ, không phải suy luận
 *
 * 🛑 Trường `badge` do BE tính **đã bị bỏ** khỏi hợp đồng. Bộ bốn nhãn runtime cũ
 * (`Hoàn thành`/`Không tăng`/`Đang thực hiện`/`Chưa có dữ liệu`) không còn tồn tại ở tầng nào —
 * thấy tên `badge` trong code cũ thì đó là di sản, đừng port lại.
 *
 * Hệ quả phải chấp nhận: **màu ở đây phản ánh ý chí người nhập liệu, không phản ánh số liệu.** Một
 * chỉ tiêu đủ điểm vẫn mang badge `.bad` nếu người dùng chọn `Cần bổ sung minh chứng`, và dữ liệu
 * thật có đúng những dòng như vậy. Đó là hành vi đúng.
 */

/** Đúng bốn giá trị, chuỗi tiếng Việt NGUYÊN VĂN (Q4) — cùng bộ mà DM-2 nhận ở tham số `status`. */
export const CRITERIA_STATUSES = [
  'Chưa thực hiện',
  'Đang thực hiện',
  'Cần bổ sung minh chứng',
  'Hoàn thành',
] as const;

export type CriteriaStatus = (typeof CRITERIA_STATUSES)[number];

/** Bốn lớp màu của `.badge` ở `src/FE/src/styles.scss` §5. */
export type CriteriaStatusBadge = 'ok' | 'warn' | 'bad' | 'neutral';

const BADGE_BY_STATUS: Readonly<Record<string, CriteriaStatusBadge>> = {
  'Hoàn thành': 'ok',
  'Đang thực hiện': 'warn',
  'Cần bổ sung minh chứng': 'bad',
  'Chưa thực hiện': 'neutral',
};

/**
 * `null` khi **không render badge nào** — kỳ chưa có đánh giá thì ô hiện `—`, không hiện một badge
 * xám trông như một trạng thái thật.
 *
 * Giá trị lạ (BE thêm trạng thái thứ năm mà FE chưa biết) cũng trả `null`: bịa một màu cho nó là
 * nói với người dùng một điều mình không biết. Chuỗi vẫn hiện ra dưới dạng chữ thường ở nơi gọi.
 */
export function criteriaStatusBadge(status: string | null | undefined): CriteriaStatusBadge | null {
  if (!status) return null;
  return BADGE_BY_STATUS[status] ?? null;
}
