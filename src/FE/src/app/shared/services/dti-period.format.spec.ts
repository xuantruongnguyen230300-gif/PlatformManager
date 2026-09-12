import { mapPeriodOptionDto } from './dti-period.mapper';
import { formatDayMonth, formatDayMonthYear, periodLabelParams, shortRangeParams } from './dti-period.format';

const WEEK = mapPeriodOptionDto({ value: '2025-W33', date: '2025-08-11', overallProgress: 82.1 }, 'week');
const MONTH = mapPeriodOptionDto({ value: '2026-08', date: '2026-08-01' }, 'month');

describe('dti-period.format', () => {
  /**
   * 🛑 Phép kiểm quan trọng nhất của file, và nó bắt một lỗi ĐÃ TÁI PHÁT.
   *
   * Bộ khung ngày-tháng-không-năm có dấu ngăn RIÊNG trong dữ liệu ICU: `vi` cho `11-08` trong khi
   * khung đầy đủ cho `17/08/2025`. Hai nửa của một khoảng ngày viết bằng hai ký hiệu khác nhau là
   * lỗi không ai đọc ra được, và nó không làm đỏ thứ gì.
   */
  it('🛑 hai mốc của cùng một khoảng ngày dùng CHUNG một dấu ngăn (vi)', () => {
    const from = formatDayMonth(WEEK.StartDate, 'vi');
    const to = formatDayMonthYear(WEEK.EndDate, 'vi');

    expect(from).withContext('`11-08` ⇒ đang lấy dấu ngăn của khung ngày-tháng').toBe('11/08');
    expect(to).toBe('17/08/2025');
  });

  it('mốc đầu KHÔNG có năm, mốc cuối CÓ — năm viết đúng một lần, ở mốc cuối (§6.1 mục 3)', () => {
    const params = periodLabelParams(WEEK, 'vi');

    expect(params.From).toBe('11/08');
    expect(params.To).toBe('17/08/2025');
    expect(params.ToShort).withContext('option chọn kỳ lược năm — §6.2b').toBe('17/08');
    expect(params.Ordinal).toBe('33');
    expect(params.Year).toBe('2025');
  });

  it('tháng: mốc cuối là ngày cuối tháng', () => {
    const params = periodLabelParams(MONTH, 'vi');

    expect(params.From).toBe('01/08');
    expect(params.To).toBe('31/08/2026');
  });

  it('ô `Kỳ của số liệu` lược CẢ số tuần lẫn năm — chỉ còn khoảng ngày (Q38 + T14)', () => {
    expect(shortRangeParams(WEEK, 'vi')).toEqual({ From: '11/08', To: '17/08' });
  });

  it('locale khác vẫn ra một dấu ngăn nhất quán giữa hai mốc', () => {
    const from = formatDayMonth(WEEK.StartDate, 'en-US');
    const to = formatDayMonthYear(WEEK.EndDate, 'en-US');
    const separator = (text: string): string => text.replace(/\d/g, '').charAt(0);

    expect(separator(from)).toBe(separator(to));
  });
});
