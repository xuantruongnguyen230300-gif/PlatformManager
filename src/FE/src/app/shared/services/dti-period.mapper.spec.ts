import { mapPeriodOptionDto, mapPeriodOptionsDto, parsePlainDate, toPlainDate } from './dti-period.mapper';
import { IPeriodOptionsDto } from '../models/dti-period.model';

/**
 * Ba hình dạng dữ liệu mà mapper này phải phân biệt được, và cả ba đều KHÔNG tự lộ ra lúc chạy:
 *
 * 1. `overallProgress` **vắng mặt** (BE bật `WhenWritingNull`) ≠ `overallProgress: 0`;
 * 2. ngày thuần `YYYY-MM-DD` phải đọc theo lịch ĐỊA PHƯƠNG — `new Date('2026-08-10')` là UTC;
 * 3. mốc cuối kỳ do FE tính, và tuần ≠ tháng.
 */
describe('dti-period.mapper', () => {
  it('parsePlainDate đọc ngày thuần theo lịch địa phương, KHÔNG lệch sang UTC', () => {
    const parsed = parsePlainDate('2026-08-10');

    expect(parsed.getFullYear()).toBe(2026);
    // Nếu ai đó đổi sang `new Date('2026-08-10')` thì ở múi giờ âm giá trị này thành 9.
    expect(parsed.getDate()).withContext('ngày bị lệch ⇒ đang quy về UTC ở đâu đó').toBe(10);
    expect(parsed.getMonth()).toBe(7);
  });

  it('toPlainDate là phép ngược của parsePlainDate — giá trị này đi thẳng vào tham số `date` của DB-1', () => {
    expect(toPlainDate(parsePlainDate('2026-01-05'))).toBe('2026-01-05');
    expect(toPlainDate(new Date(2026, 11, 31))).toBe('2026-12-31');
  });

  it('tuần: EndDate = StartDate + 6 ngày, Ordinal và Year đọc lại từ mã kỳ', () => {
    const week = mapPeriodOptionDto({ value: '2026-W33', date: '2026-08-10', overallProgress: 82.1 }, 'week');

    expect(week.Unit).toBe('week');
    expect(week.Ordinal).toBe(33);
    expect(week.Year).toBe(2026);
    expect(toPlainDate(week.EndDate)).toBe('2026-08-16');
    expect(week.OverallProgress).toBe(82.1);
  });

  it('tháng: EndDate là ngày CUỐI tháng, kể cả tháng 30 ngày và tháng 2 năm nhuận', () => {
    expect(toPlainDate(mapPeriodOptionDto({ value: '2026-08', date: '2026-08-01' }, 'month').EndDate)).toBe(
      '2026-08-31',
    );
    expect(toPlainDate(mapPeriodOptionDto({ value: '2026-04', date: '2026-04-01' }, 'month').EndDate)).toBe(
      '2026-04-30',
    );
    expect(toPlainDate(mapPeriodOptionDto({ value: '2024-02', date: '2024-02-01' }, 'month').EndDate)).toBe(
      '2024-02-29',
    );
  });

  it('🛑 `overallProgress` VẮNG MẶT → null, KHÔNG phải 0 — hai thứ này nói hai điều ngược nhau', () => {
    const absent = mapPeriodOptionDto({ value: '2026-W34', date: '2026-08-17' }, 'week');
    const zero = mapPeriodOptionDto({ value: '2026-W35', date: '2026-08-24', overallProgress: 0 }, 'week');

    expect(absent.OverallProgress).toBeNull();
    expect(zero.OverallProgress).toBe(0);
  });

  it('mã kỳ sai khuôn không được biến thành một con số trông hợp lý', () => {
    const broken = mapPeriodOptionDto({ value: 'khong-phai-ma-ky', date: '2026-08-10' }, 'week');

    expect(broken.Ordinal).toBe(0);
    expect(broken.Year).toBe(0);
  });

  it('năm chưa có dữ liệu: hai mảng rỗng vẫn map được, và đó KHÔNG phải lỗi (DB-3 § Mã lỗi)', () => {
    const dto: IPeriodOptionsDto = { years: [2026], weeksInYear: [], monthsInYear: [] };
    const model = mapPeriodOptionsDto(dto);

    expect(model.Years).toEqual([2026]);
    expect(model.WeeksInYear).toEqual([]);
    expect(model.MonthsInYear).toEqual([]);
  });
});
