import { IDashboardAggregateDto, IDashboardKpiDto } from '../models/dashboard.model';
import { mapDashboardAggregateDto, mapDashboardKpiDto, mapDashboardTrendPointDto } from './dashboard.mapper';

/** Ba số đếm luôn có giá trị; hai trường suy từ `Tiến độ %` thì vắng mặt ngay sau import. */
const KPI_AFTER_IMPORT = {
  up: 0,
  flat: 0,
  down: 0,
  done: 26,
  totalCriteria: 62,
} as unknown as IDashboardKpiDto;

const AGGREGATE: IDashboardAggregateDto = {
  mode: 'week',
  periodLabel: 'Tuần 33/2026 (10/08 – 16/08/2026)',
  periodStart: '2026-08-10',
  periodEnd: '2026-08-16',
  kpi: KPI_AFTER_IMPORT,
  groups: [{ groupId: 'g1', groupCode: '1', groupName: 'Hạ tầng và Nền tảng số' }],
  trend: [
    { period: '2026-W32', periodLabel: '03/08 – 09/08' },
    { period: '2026-W33', periodLabel: '10/08 – 16/08', value: 82.1 },
  ],
  table: [
    {
      criteriaId: 'c1',
      code: '1.4',
      name: 'Mức độ ứng dụng AI',
      groupId: 'g1',
      groupCode: '1',
      groupName: 'Hạ tầng và Nền tảng số',
      maxScore: 5,
      selfScore: 5,
      verifiedScore: 0,
      diff: -5,
      status: 'Cần bổ sung minh chứng',
    },
  ],
};

describe('dashboard.mapper', () => {
  /**
   * 🛑 Test quan trọng nhất của file: `—` và `0` là HAI điều khác nhau, và một `?? 0` lạc chỗ ở
   * đây làm dashboard tuyên bố "tiến độ toàn xã: 0%" ngay sau khi nạp một file có 26 chỉ tiêu đã
   * hoàn thành (business-rules §1.6 mục b).
   */
  it('🛑 KPI ngay sau import: hai trường suy từ `Tiến độ %` là NULL, ba số đếm là 0 THẬT', () => {
    const kpi = mapDashboardKpiDto(KPI_AFTER_IMPORT);

    expect(kpi.OverallProgress).withContext('vắng mặt ⇒ null, KHÔNG phải 0').toBeNull();
    expect(kpi.Delta).toBeNull();
    expect(kpi.PreviousPeriodLabel).toBeNull();
    expect(kpi.Up).withContext('số đếm — 0 là kết quả thật, không phải "chưa có"').toBe(0);
    expect(kpi.Flat).toBe(0);
    expect(kpi.Done).toBe(26);
    expect(kpi.TotalCriteria).toBe(62);
  });

  it('`overallProgress: 0` giữ nguyên 0 — đã đo và bằng không', () => {
    expect(mapDashboardKpiDto({ ...KPI_AFTER_IMPORT, overallProgress: 0 }).OverallProgress).toBe(0);
  });

  it('nhóm chưa có `Tiến độ %`: Progress = null ⇒ thanh RỖNG, không phải thanh 0%', () => {
    const model = mapDashboardAggregateDto(AGGREGATE);

    expect(model.Groups[0].Progress).toBeNull();
  });

  /**
   * Q44 — bỏ phần tử `null` cho gọn là bug im lặng: trên trục category của `chart.js` hai điểm kề
   * nhau sẽ được nối THẲNG qua đúng chỗ không có số liệu.
   */
  it('🛑 trend GIỮ NGUYÊN số phần tử; kỳ rỗng mang Value = null (Q44)', () => {
    const model = mapDashboardAggregateDto(AGGREGATE);

    expect(model.Trend.length).withContext('mapper không được lọc bớt kỳ nào').toBe(2);
    expect(model.Trend[0].Value).toBeNull();
    expect(model.Trend[1].Value).toBe(82.1);
  });

  it('nhãn trục X đọc từ `periodLabel` BE dựng sẵn — FE không quy mã tuần ra ngày (Q43)', () => {
    const point = mapDashboardTrendPointDto({ period: '2026-W28', periodLabel: '06/07 – 12/07', value: 70 });

    expect(point.PeriodLabel).toBe('06/07 – 12/07');
    expect(point.Period).toBe('2026-W28');
  });

  it('`diff` âm giữ NGUYÊN DẤU — ca "tự chấm 5, thẩm định bác trắng" là ca xấu nhất, không phải ca xanh', () => {
    const model = mapDashboardAggregateDto(AGGREGATE);

    expect(model.Table[0].Diff).toBe(-5);
  });

  it('dòng bảng thiếu `note` ⇒ null, không `undefined`', () => {
    const model = mapDashboardAggregateDto(AGGREGATE);

    expect(model.Table[0].Note).toBeNull();
  });

  it('kỳ chưa có dữ liệu nào: groups/table rỗng vẫn map được — đó KHÔNG phải lỗi (DB-1 §0)', () => {
    const model = mapDashboardAggregateDto({ ...AGGREGATE, groups: [], table: [] });

    expect(model.Groups).toEqual([]);
    expect(model.Table).toEqual([]);
    expect(model.PeriodLabel).toBe('Tuần 33/2026 (10/08 – 16/08/2026)');
  });
});
