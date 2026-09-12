import { IPeriodOption, IPeriodOptionDto, IPeriodOptions, IPeriodOptionsDto, PeriodUnit } from '../models/dti-period.model';

/**
 * Đọc một ngày thuần `YYYY-MM-DD` thành `Date` ở múi giờ ĐỊA PHƯƠNG.
 *
 * 🛑 KHÔNG dùng `new Date('2026-08-10')`: chuỗi chỉ-ngày được ECMAScript quy định là **UTC**, nên
 * ở mọi múi giờ âm nó lùi sang ngày 09/08 — một ô "Kỳ của số liệu" lệch đúng một ngày, hiển thị
 * tự tin và không lỗi ở đâu cả. Việt Nam là UTC+7 nên lỗi này KHÔNG lộ ra ở đây; nó chỉ lộ ra khi
 * có người mở app ở múi giờ khác, tức đúng lúc không ai đang nhìn.
 */
export function parsePlainDate(raw: string): Date {
  const [year, month, day] = raw.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/**
 * Ngược lại: `Date` → `YYYY-MM-DD` theo lịch ĐỊA PHƯƠNG.
 *
 * `toISOString()` quy về UTC nên cũng lệch một ngày ở nửa vòng trái đất — và giá trị này đi thẳng
 * vào tham số `date` của DB-1, tức lệch ngày ở đây nghĩa là xem nhầm kỳ.
 */
export function toPlainDate(value: Date): string {
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');
  return `${value.getFullYear()}-${month}-${day}`;
}

/** Số ngày của một tuần ISO — hằng số, không phải phép đoán: thứ Hai → Chủ nhật. */
const DAYS_IN_ISO_WEEK = 7;

function endOfWeek(start: Date): Date {
  const end = new Date(start);
  end.setDate(end.getDate() + DAYS_IN_ISO_WEEK - 1);
  return end;
}

/** Ngày 0 của tháng SAU chính là ngày cuối tháng này — không phải bảng 28/30/31 viết tay. */
function endOfMonth(start: Date): Date {
  return new Date(start.getFullYear(), start.getMonth() + 1, 0);
}

/**
 * Tách `Year` + `Ordinal` khỏi mã kỳ. Đây **không** phải quy đổi lịch: nó chỉ đọc lại hai con số
 * mà chính BE đã ghi vào chuỗi. Mã lạ ⇒ `Ordinal = 0`, để nơi gọi thấy ngay là dữ liệu sai chứ
 * không nhận một số trông hợp lý.
 */
function readPeriodKey(value: string, unit: PeriodUnit): { Year: number; Ordinal: number } {
  const separator = unit === 'week' ? '-W' : '-';
  const cut = value.lastIndexOf(separator);
  if (cut < 0) return { Year: 0, Ordinal: 0 };
  const year = Number(value.slice(0, cut));
  const ordinal = Number(value.slice(cut + separator.length));
  return {
    Year: Number.isFinite(year) ? year : 0,
    Ordinal: Number.isFinite(ordinal) ? ordinal : 0,
  };
}

export function mapPeriodOptionDto(dto: IPeriodOptionDto, unit: PeriodUnit): IPeriodOption {
  const start = parsePlainDate(dto.date);
  const { Year, Ordinal } = readPeriodKey(dto.value, unit);
  return {
    Value: dto.value,
    Unit: unit,
    Ordinal,
    Year,
    StartDate: start,
    EndDate: unit === 'week' ? endOfWeek(start) : endOfMonth(start),
    // `?? null` cho MỌI trường nullable phía BE — key vắng mặt về tới đây là `undefined`, và một
    // model khai `number | null` mang `undefined` không sai ở đâu cho tới lúc có người viết
    // `=== null`, rồi nhánh đó im lặng không bao giờ chạy.
    OverallProgress: dto.overallProgress ?? null,
  };
}

export function mapPeriodOptionsDto(dto: IPeriodOptionsDto): IPeriodOptions {
  return {
    // `?? []` ở BA chỗ: mảng RỖNG là câu trả lời hợp lệ của DB-3 ("năm chưa có dữ liệu"), nhưng
    // một mảng rỗng phía C# vẫn được serialize thành `[]`. Ba dòng này chống ca BE cũ/khác trả
    // thiếu khoá — `.map` trên `undefined` ném lỗi ngay trong mapper, nơi thông điệp vô nghĩa nhất.
    Years: dto.years ?? [],
    WeeksInYear: (dto.weeksInYear ?? []).map((item) => mapPeriodOptionDto(item, 'week')),
    MonthsInYear: (dto.monthsInYear ?? []).map((item) => mapPeriodOptionDto(item, 'month')),
  };
}
