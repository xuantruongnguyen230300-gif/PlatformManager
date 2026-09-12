// ===== Wire (DTO) — camelCase, xem doc/contracts/dashboard.md CONTRACT DB-3 =====
//
// DB-3 (`GET /api/dashboard/periods`) nuôi ô lọc kỳ của HAI màn: `modules/dashboard/` và
// `modules/danh-muc-dti/`. Gate G8 cấm hai module nghiệp vụ import chéo nhau, nên hợp đồng này
// nằm ở `shared/` — đúng lối thoát mà chính thông điệp của G8 chỉ ra
// (`src/FE/eslint.config.js`), và đúng nơi cả DB-3 lẫn hai `ui-spec.md` chỉ định.
//
// File KIỂU nằm ở `shared/models/`, còn mapper và service ở `shared/services/` — cùng khuôn mà
// bốn feature đang chạy đã tách (`platform/*/models/` ⇄ `platform/*/services/`), và đúng cây thư
// mục ở `fe-architecture.md` §"Cấu trúc một feature". Chuyển về đây 2026-09-10 (finding #1 của
// `core-reviewer`); trước đó nó nằm nhầm trong `services/`.
//
// ✅ CHỐT 2026-09-10 (người dùng, sau audit `core-reviewer`): `shared/services/` **ĐƯỢC** gọi HTTP
// cho service dữ liệu dùng chung, có điều kiện — `doc/huong_dan/quy-uoc/fe-architecture.md`
// §"Bên trong shared/". Trước ngày đó bảng phân loại ở file kia chỉ có HAI ô và cả hai đều đẩy
// `DtiPeriodService` về `core/`, tức nhét một endpoint nghiệp vụ DTI vào tầng đáy CoreBase.
//
// 🛑 Kèm theo chốt đó là một MÓN NỢ đã đăng ký, không phải một tờ giấy phép: `shared/` đi theo
// CoreBase, nên endpoint này và bảng trạng thái ở `dti-criteria-status.model.ts` hiện **đi nhờ**
// sang sản phẩm thứ hai. Nợ ghi ở `doc/kien-truc-core-module.md` § "Chỗ mang, đo được", kèm phép
// thử #11 dò đúng loại nợ này. Hoãn tái cấu trúc tới khi có module DTI thứ ba — đừng thêm file
// nghiệp vụ DTI vào `shared/` chỉ vì đã có hai file ở đây.

/** Một mốc kỳ trong `weeksInYear` / `monthsInYear` của DB-3. */
export interface IPeriodOptionDto {
  /** `"2026-W33"` (tuần ISO) | `"2026-08"` (tháng dương lịch). */
  value: string;
  /** Mốc ĐẦU kỳ, khuôn `YYYY-MM-DD` (ngày thuần, không giờ, không offset). */
  date: string;
  /**
   * `number?` — `Program.cs` bật `DefaultIgnoreCondition = WhenWritingNull`, nên kỳ chưa có số
   * liệu về tới đây là **khoá vắng mặt** (`undefined`), không phải `null`. Khai cả `?:` lẫn
   * `| null` vì hai hình dạng đó cùng nghĩa "chưa tính được"; mapper quy về một.
   */
  overallProgress?: number | null;
}

export interface IPeriodOptionsDto {
  years: number[];
  weeksInYear: IPeriodOptionDto[];
  monthsInYear: IPeriodOptionDto[];
}

// ===== Model app — PascalCase + prefix I =====

/** Đơn vị của một kỳ. `"all"` KHÔNG nằm ở đây: nó là một lựa chọn của ô lọc, không phải một kỳ. */
export type PeriodUnit = 'week' | 'month';

/**
 * Một kỳ đã lưu, đủ dữ liệu để dựng mọi nhãn kỳ mà `spec/dashboard-dti/business-rules.md` §6.2
 * khai — **không** cần trường mới nào từ BE (ghi chú cuối DB-3).
 *
 * `EndDate` do FE tính, và đó là phép tính DUY NHẤT về kỳ mà FE được phép làm: tuần ISO kết thúc
 * sau đúng 6 ngày, tháng kết thúc ở ngày cuối tháng chứa `date`. Quy một MÃ kỳ (`"2026-W33"`) ra
 * khoảng ngày thì **không** được tự làm — đó là lịch ISO, và
 * `spec/danh-muc-dti/business-rules.md` §5.1 giữ nó ở BE.
 */
export interface IPeriodOption {
  /** Giá trị truyền thẳng vào `period` của DM-2 và vào URL. */
  readonly Value: string;
  readonly Unit: PeriodUnit;
  /** Số tuần ISO hoặc số tháng — tách ra từ `Value`, dùng cho nhãn `Tuần {n}` / `Tháng {n}`. */
  readonly Ordinal: number;
  /** Năm nằm trong `Value`. Với tuần ISO đây là **năm ISO**, có thể lệch năm dương lịch của `StartDate`. */
  readonly Year: number;
  readonly StartDate: Date;
  readonly EndDate: Date;
  /** `null` = kỳ chưa có số liệu để tính bình quân. KHÁC `0`. */
  readonly OverallProgress: number | null;
}

export interface IPeriodOptions {
  /** Mọi năm có dữ liệu, LUÔN kèm năm hiện tại — BE bảo đảm, FE không tự chèn. */
  readonly Years: number[];
  readonly WeeksInYear: IPeriodOption[];
  readonly MonthsInYear: IPeriodOption[];
}
