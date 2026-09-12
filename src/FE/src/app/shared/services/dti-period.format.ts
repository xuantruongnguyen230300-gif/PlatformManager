import { IPeriodOption } from '../models/dti-period.model';

/**
 * Hai khuôn ngày dùng để ráp MỌI nhãn kỳ của sản phẩm.
 *
 * File chủ của các khuôn nhãn là `spec/dashboard-dti/business-rules.md` §6.2. File này **không**
 * định nghĩa khuôn nào: nó chỉ cấp
 * hai mảnh (`dd/MM`, `dd/MM/yyyy`) để nơi gọi cắm vào một CÂU ĐÃ DỊCH. Dấu gạch ` – ` và trật tự
 * các thành phần nằm trong bảng dịch, không nối bằng `+` ở TypeScript — trật tự từ mỗi ngôn ngữ
 * một khác.
 *
 * Vì sao ở `shared/`: cả `modules/dashboard/` lẫn `modules/danh-muc-dti/` đều dựng nhãn kỳ, và G8
 * cấm hai module nghiệp vụ import chéo nhau. Căng thẳng "shared đi theo CoreBase" đã ghi nhận ở
 * đầu `dti-period.model.ts`.
 */

/** `16/08/2026`. */
export function formatDayMonthYear(value: Date, localeId: string): string {
  return new Intl.DateTimeFormat(localeId, { day: '2-digit', month: '2-digit', year: 'numeric' }).format(value);
}

/**
 * `10/08` — ngày và tháng, KHÔNG năm.
 *
 * 🛑 KHÔNG dùng `Intl.DateTimeFormat(locale, { day, month })`. Bộ khung ngày-tháng-không-năm có
 * **dấu ngăn riêng** trong dữ liệu ICU: ở locale `vi` nó ra `11-08`, trong khi khung đầy đủ ra
 * `17/08/2025`. Hai nửa của cùng một khoảng ngày khi đó viết bằng hai ký hiệu khác nhau —
 * `11-08 – 17/08/2025` — và không dòng chữ nào giải thích được.
 *
 * Cách chắc chắn, và là cách `modules/dashboard/components/history-row/history-row.ts` đã dùng từ
 * 2026-09-09 sau khi dính đúng lỗi này: định dạng ĐẦY ĐỦ rồi CẮT phần năm ra, nên hai mốc dùng
 * chung đúng một dấu ngăn theo định nghĩa, ở mọi locale.
 *
 * *(Lỗi tái phát ở chính file này ngày 2026-09-10, và bị `danh-muc-dti.page.spec.ts` bắt: chip
 * `Kỳ` hiện `Tuần 33: 11-08 – 17/08/2025`.)*
 */
export function formatDayMonth(value: Date, localeId: string): string {
  const parts = new Intl.DateTimeFormat(localeId, { day: '2-digit', month: '2-digit', year: 'numeric' })
    .formatToParts(value)
    .filter((part) => part.type !== 'year');

  // Bỏ dấu ngăn bị mồ côi ở CẢ HAI đầu: thứ tự ngày/tháng/năm đổi theo locale — `vi` để năm ở
  // cuối, có locale để năm ở đầu.
  while (parts.length > 0 && parts[0].type === 'literal') parts.shift();
  while (parts.length > 0 && parts[parts.length - 1].type === 'literal') parts.pop();

  return parts.map((part) => part.value).join('');
}

/**
 * Tham số của một câu nhãn kỳ đã dịch. Tên khoá `PascalCase` theo quy ước `messageParams` của
 * envelope — một khuôn duy nhất cho mọi chỗ ráp tham số vào câu.
 */
export interface IPeriodLabelParams {
  readonly Ordinal: string;
  readonly Year: string;
  /** `10/08` — mốc đầu kỳ, KHÔNG năm (§6.1 mục 3: năm viết đúng một lần, ở mốc cuối). */
  readonly From: string;
  /** `16/08/2026` — mốc cuối kỳ CÓ năm. Dùng khi nhãn đứng một mình. */
  readonly To: string;
  /**
   * `16/08` — mốc cuối kỳ KHÔNG năm.
   *
   * Dùng cho option trong ô chọn kỳ của Dashboard (`Tuần 33 · 10/08 – 16/08 · 82,1%`), nơi §6.2b
   * cho phép lược **năm** vì ô chọn **năm** nằm ngay bên trái. Đây là lược bỏ ĐÃ ĐĂNG KÝ, không
   * phải một khuôn thứ năm tự chế.
   */
  readonly ToShort: string;
}

/**
 * Bộ tham số đầy đủ của một kỳ — nơi gọi chọn khoá dịch nào cần mảnh nào.
 *
 * `To` **có năm**, `From` **không** — luật §6.1 mục 3: năm viết đúng một lần và đặt ở mốc cuối.
 */
export function periodLabelParams(option: IPeriodOption, localeId: string): IPeriodLabelParams {
  return {
    Ordinal: `${option.Ordinal}`,
    Year: `${option.Year}`,
    From: formatDayMonth(option.StartDate, localeId),
    To: formatDayMonthYear(option.EndDate, localeId),
    ToShort: formatDayMonth(option.EndDate, localeId),
  };
}

/**
 * Khoảng ngày KHÔNG năm — riêng cho ô `Kỳ của số liệu` của lưới Danh mục (Q38 + T14).
 *
 * Ngoại lệ có chủ đích của Q12, đã đăng ký ở `business-rules` §6.2b: cột rộng 110px, và sau Q37
 * thì mọi dòng đều là tuần nên chữ "Tuần" lặp 62 lần mà không thêm thông tin nào. Năm bỏ được vì
 * mọi dòng đều thuộc năm đang lọc, và năm đó hiện thường trực trên toolbar.
 */
export function shortRangeParams(option: IPeriodOption, localeId: string): { From: string; To: string } {
  return {
    From: formatDayMonth(option.StartDate, localeId),
    To: formatDayMonth(option.EndDate, localeId),
  };
}
