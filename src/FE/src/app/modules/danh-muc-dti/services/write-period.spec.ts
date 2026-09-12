import { buildWritePeriod } from './write-period';

/**
 * Hàm này nuôi **cả ba** đường ghi (DM-4 · DM-6 · DM-7), nên một lỗi ở đây hỏng cả ba cùng lúc —
 * và hỏng theo kiểu `400` từ server chứ không phải lỗi biên dịch. Đó là lý do nó có spec riêng
 * dù chỉ dài vài dòng.
 */
describe('buildWritePeriod — CHỖ DUY NHẤT dựng cặp { period, year } của một lời ghi', () => {
  it('một TUẦN ⇒ gửi chính nó, KHÔNG kèm `Year` (năm đã nằm trong chuỗi tuần)', () => {
    expect(buildWritePeriod('2026-W33', 2026)).toEqual({ Period: '2026-W33' });
  });

  it('tuần của NĂM CŨ vẫn ghi được — T15 không lật Q20 (Q41)', () => {
    expect(buildWritePeriod('2025-W33', 2025)).toEqual({ Period: '2025-W33' });
  });

  it('🛑 `"all"` đi NGUYÊN VĂN ra dây kèm `Year` — FE không quy về tuần hiện tại (Q40)', () => {
    expect(buildWritePeriod('all', 2026)).toEqual({ Period: 'all', Year: 2026 });
  });

  it('🛑 `"all"` + năm cũ VẪN gửi `year` — lớp chặn T15 nằm ở SERVER, không ở đây', () => {
    // Thiếu `year` thì server không có cách nào biết người dùng đang nhìn năm nào, và lớp chặn
    // thứ hai không tồn tại — chỉ còn FE ẩn nút, mà ẩn nút là trải nghiệm chứ không phải luật.
    expect(buildWritePeriod('all', 2025)).toEqual({ Period: 'all', Year: 2025 });
  });

  it('🛑 kỳ THÁNG ⇒ `null` — không quy về một tuần nào cho "chắc" (Q37)', () => {
    expect(buildWritePeriod('2026-08', 2026)).toBeNull();
  });

  it('chuỗi lạ ⇒ `null`, không đoán', () => {
    expect(buildWritePeriod('tuần 33', 2026)).toBeNull();
    expect(buildWritePeriod('', 2026)).toBeNull();
  });
});
