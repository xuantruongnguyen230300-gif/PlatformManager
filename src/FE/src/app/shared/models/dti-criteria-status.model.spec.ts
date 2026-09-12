import { CRITERIA_STATUSES, criteriaStatusBadge } from './dti-criteria-status.model';

describe('dti-criteria-status.model — bảng ánh xạ DUY NHẤT (business-rules §2)', () => {
  it('đúng bốn giá trị, nguyên văn tiếng Việt — cùng bộ mà DM-2 nhận ở tham số `status`', () => {
    expect(CRITERIA_STATUSES).toEqual([
      'Chưa thực hiện',
      'Đang thực hiện',
      'Cần bổ sung minh chứng',
      'Hoàn thành',
    ]);
  });

  it('ánh xạ đủ bốn trạng thái sang bốn lớp badge', () => {
    expect(criteriaStatusBadge('Hoàn thành')).toBe('ok');
    expect(criteriaStatusBadge('Đang thực hiện')).toBe('warn');
    expect(criteriaStatusBadge('Cần bổ sung minh chứng')).toBe('bad');
    expect(criteriaStatusBadge('Chưa thực hiện')).toBe('neutral');
  });

  it('🛑 vắng mặt / rỗng ⇒ KHÔNG badge nào — ô hiện `—`, không phải một badge xám', () => {
    expect(criteriaStatusBadge(null)).toBeNull();
    expect(criteriaStatusBadge(undefined)).toBeNull();
    expect(criteriaStatusBadge('')).toBeNull();
  });

  it('trạng thái LẠ không được bịa ra một màu', () => {
    expect(criteriaStatusBadge('Đang xử lý')).toBeNull();
  });
});
