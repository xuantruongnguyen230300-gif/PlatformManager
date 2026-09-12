import {
  IDashboardAggregate,
  IDashboardAggregateDto,
  IDashboardGroup,
  IDashboardGroupDto,
  IDashboardKpi,
  IDashboardKpiDto,
  IDashboardTableRow,
  IDashboardTableRowDto,
  IDashboardTrendPoint,
  IDashboardTrendPointDto,
} from '../models/dashboard.model';

/**
 * Nơi DUY NHẤT casing đổi cho màn Dashboard — camelCase (dây) → PascalCase (app).
 *
 * ## `?? null`, không bao giờ `?? 0`
 *
 * Đây là ranh giới nơi *"chưa có dữ liệu"* dễ bị biến thành *"bằng không"* nhất, vì cả hai đều đi
 * tiếp thành một con số trên màn hình. `spec/dashboard-dti/business-rules.md` §1.6 dựng nguyên một
 * bảng cho ca này: ngay sau import, ô 1 và ô 2 hiện `—` trong khi ô 3 và ô 4 hiện `0` — và hai
 * cách hiển thị đó nói hai điều ngược nhau. `?? 0` ở dòng `overallProgress` là đủ để xoá sự phân
 * biệt đó khỏi cả sản phẩm.
 *
 * Chiều ngược lại cũng cấm: ba số ĐẾM (`up`/`flat`/`down`/`done`/`totalCriteria`) **không** được
 * `?? null`. Chúng luôn tính được, nên vắng mặt nghĩa là hợp đồng đã gãy — `?? 0` ở đó là bịa ra
 * một phép đếm chưa từng chạy. Để `Number(...)` đọc thẳng: hợp đồng gãy thì `NaN` hiện ra ngay,
 * ồn ào, ở đúng ô sai.
 */
export function mapDashboardKpiDto(dto: IDashboardKpiDto): IDashboardKpi {
  return {
    OverallProgress: dto.overallProgress ?? null,
    Delta: dto.delta ?? null,
    PreviousPeriodLabel: dto.previousPeriodLabel ?? null,
    Up: dto.up,
    Flat: dto.flat,
    Down: dto.down,
    Done: dto.done,
    TotalCriteria: dto.totalCriteria,
  };
}

export function mapDashboardGroupDto(dto: IDashboardGroupDto): IDashboardGroup {
  return {
    GroupId: dto.groupId,
    GroupCode: dto.groupCode,
    GroupName: dto.groupName,
    // RỖNG ≠ 0 (Q24): ngay sau import mọi nhóm đều rỗng, và đó là đường đi phổ biến nhất của ngày
    // đầu chạy thật. Một thanh `.fill` rộng 0 nói "đã đo, toàn xã đạt 0%".
    Progress: dto.progress ?? null,
  };
}

/**
 * Q44 — **giữ nguyên số phần tử**. Kỳ không có dữ liệu vẫn là một điểm trên trục, mang `Value`
 * bằng `null`.
 *
 * 🛑 Đừng "dọn" các phần tử `null` cho gọn. Trên trục category của `chart.js`, bỏ một điểm thì hai
 * điểm kề nhau được nối THẲNG — trục không có ô nào cho kỳ bị bỏ, nên không có khoảng đứt nào và
 * biểu đồ vẽ ra một đường liền mạch qua đúng chỗ không có số liệu.
 */
export function mapDashboardTrendPointDto(dto: IDashboardTrendPointDto): IDashboardTrendPoint {
  return {
    Period: dto.period,
    // Nhãn trục X do BE dựng (Q43) — FE **không** quy mã tuần ra khoảng ngày, đó là lịch ISO và
    // nó ở một chỗ duy nhất (business-rules §6.3).
    PeriodLabel: dto.periodLabel,
    Value: dto.value ?? null,
  };
}

export function mapDashboardTableRowDto(dto: IDashboardTableRowDto): IDashboardTableRow {
  return {
    CriteriaId: dto.criteriaId,
    Code: dto.code,
    Name: dto.name,
    GroupId: dto.groupId,
    GroupCode: dto.groupCode,
    GroupName: dto.groupName,
    MaxScore: dto.maxScore,
    SelfScore: dto.selfScore ?? null,
    VerifiedScore: dto.verifiedScore ?? null,
    // Dấu của `diff` LÀ THÔNG TIN (Q25): âm = bị thẩm định trừ điểm, và đó là ca đáng chú ý nhất.
    // Không lấy trị tuyệt đối, không đảo dấu ở tầng nào.
    Diff: dto.diff ?? null,
    Status: dto.status ?? null,
    Note: dto.note ?? null,
  };
}

export function mapDashboardAggregateDto(dto: IDashboardAggregateDto): IDashboardAggregate {
  return {
    Mode: dto.mode,
    PeriodLabel: dto.periodLabel,
    PeriodStart: dto.periodStart,
    PeriodEnd: dto.periodEnd,
    Kpi: mapDashboardKpiDto(dto.kpi),
    Groups: (dto.groups ?? []).map(mapDashboardGroupDto),
    Trend: (dto.trend ?? []).map(mapDashboardTrendPointDto),
    Table: (dto.table ?? []).map(mapDashboardTableRowDto),
  };
}
