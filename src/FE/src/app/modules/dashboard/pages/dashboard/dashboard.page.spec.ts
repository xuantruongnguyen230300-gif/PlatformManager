import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../../app.config';
import { provideCoreI18n } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { IApiResult } from '../../../../core/http/api-result.model';
import { IDashboardAggregateDto, IDashboardKpiDto } from '../../models/dashboard.model';
import { DashboardPage } from './dashboard.page';

const CURRENT_YEAR = new Date().getFullYear();

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-dashboard-page',
    retryable: null,
    fields: null,
  };
}

/** Ngay sau import: hai trường suy từ `Tiến độ %` VẮNG MẶT, ba số đếm là `0` thật (T12). */
const KPI_AFTER_IMPORT = { up: 0, flat: 0, down: 0, done: 26, totalCriteria: 62 } as unknown as IDashboardKpiDto;

/** Kỳ đã có người nhập tiến độ. */
const KPI_POPULATED: IDashboardKpiDto = {
  overallProgress: 82.1,
  delta: 2.3,
  previousPeriodLabel: `Tuần 32/${CURRENT_YEAR} (03/08 – 09/08/${CURRENT_YEAR})`,
  up: 18,
  flat: 27,
  down: 3,
  done: 26,
  totalCriteria: 62,
};

function aggregate(patch: Partial<IDashboardAggregateDto> = {}): IDashboardAggregateDto {
  return {
    mode: 'week',
    periodLabel: `Tuần 33/${CURRENT_YEAR} (10/08 – 16/08/${CURRENT_YEAR})`,
    periodStart: `${CURRENT_YEAR}-08-10`,
    periodEnd: `${CURRENT_YEAR}-08-16`,
    kpi: KPI_POPULATED,
    groups: [{ groupId: 'g1', groupCode: '1', groupName: 'Hạ tầng và Nền tảng số', progress: 74.3 }],
    trend: [
      { period: `${CURRENT_YEAR}-W32`, periodLabel: '03/08 – 09/08' },
      { period: `${CURRENT_YEAR}-W33`, periodLabel: '10/08 – 16/08', value: 82.1 },
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
    ...patch,
  };
}

function drainI18nRequests(httpMock: HttpTestingController): void {
  httpMock
    .match((req) => APP_I18N.resources.some((prefix) => req.url.startsWith(prefix)))
    .forEach((req) => req.flush({}));
}

describe('DashboardPage — đường đọc + `Xuất báo cáo` (vòng 2)', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let httpMock: HttpTestingController;
  let router: Router;

  async function boot(queryParams: Record<string, string> = {}): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        provideTranslateService(),
        provideCoreI18n(APP_I18N),
      ],
    });
    await useTranslationsInTest();
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    await router.navigate([], { queryParams });
    fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
  }

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  function aggregateRequests(): TestRequest[] {
    return httpMock.match((req) => req.url === '/dashboard');
  }

  /** Xả DB-3 — nguồn thứ hai, nuôi ô chọn kỳ và vùng Lịch sử. */
  function flushPeriods(weeks: { value: string; date: string; overallProgress?: number }[] = []): void {
    httpMock
      .match((req) => req.url === '/dashboard/periods')
      .forEach((req) => req.flush(ok({ years: [CURRENT_YEAR], weeksInYear: weeks, monthsInYear: [] })));
  }

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
  }

  // ================================================================= mặc định
  describe('trạng thái mặc định (§5.1)', () => {
    it('gọi DB-1 với mode=week và year hiện tại; KHÔNG gửi `date` khi chưa chọn kỳ cụ thể', async () => {
      await boot();
      flushPeriods();

      const requests = aggregateRequests();
      expect(requests.length).toBe(1);
      expect(requests[0].request.params.get('mode')).toBe('week');
      expect(requests[0].request.params.get('year')).toBe(`${CURRENT_YEAR}`);
      expect(requests[0].request.params.has('date'))
        .withContext('bỏ trống `date` = kỳ hiện tại do server chọn')
        .toBeFalse();
      requests[0].flush(ok(aggregate()));
    });

    it('nhãn kỳ ở `.period-display` lấy NGUYÊN chuỗi BE dựng — FE không ghép lại (Q12)', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate()));
      fixture.detectChanges();

      expect(host().querySelector('.period-display')?.textContent?.trim()).toBe(
        `Tuần 33/${CURRENT_YEAR} (10/08 – 16/08/${CURRENT_YEAR})`,
      );
    });

    it('dải KPI có đúng 5 ô, nhãn ô 1/ô 2 theo chế độ TUẦN (Q52)', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate()));
      fixture.detectChanges();

      const labels = [...host().querySelectorAll('.kpis .label')].map((el) => el.textContent?.trim());
      expect(labels.length).toBe(5);
      expect(labels[0]).toBe('Tiến độ chung tuần này');
      expect(labels[1]).toBe('So với tuần trước');
    });

    it('🛑 chế độ THÁNG đổi nhãn ô 1 và ô 2 — không gọi số của một tháng là "tuần này" (Q52)', async () => {
      await boot({ mode: 'month' });
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate({ mode: 'month' })));
      fixture.detectChanges();

      const labels = [...host().querySelectorAll('.kpis .label')].map((el) => el.textContent?.trim());
      expect(labels[0]).toBe('Tiến độ chung tháng này');
      expect(labels[1]).toBe('So với tháng trước');
    });

    it('chế độ `Tất cả` (mode=year): nhãn ô 1/ô 2 theo NĂM, và có badge cảnh báo trên thanh kỳ', async () => {
      await boot({ mode: 'year' });
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate({ mode: 'year' })));
      fixture.detectChanges();

      const labels = [...host().querySelectorAll('.kpis .label')].map((el) => el.textContent?.trim());
      expect(labels[0]).toBe('Tiến độ chung năm nay');
      expect(host().querySelector('.toolbar .badge.warn')?.textContent?.trim()).toBe(`Tất cả · ${CURRENT_YEAR}`);
    });

    it('nút `Xuất báo cáo` render ở thanh chọn kỳ, và bấm được khi đã có số liệu (DB-4)', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate()));
      fixture.detectChanges();

      // Chỉ soi THANH CHỌN KỲ (`<div class="toolbar">`), không soi `<app-toolbar>` của bảng chi
      // tiết — component đó luôn tự dựng một `.toolbar-actions` cho ô sắp xếp.
      const button = host().querySelector('div.toolbar > .toolbar-actions button') as HTMLButtonElement;
      expect(button.textContent?.trim()).toBe('Xuất báo cáo');
      expect(button.disabled).toBeFalse();
    });
  });

  // ================================================================= DB-4
  describe('`Xuất báo cáo` (DB-4)', () => {
    function exportButton(): HTMLButtonElement {
      return host().querySelector('div.toolbar > .toolbar-actions button') as HTMLButtonElement;
    }

    async function bootReady(params: Record<string, string> = {}): Promise<void> {
      await boot(params);
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate()));
      fixture.detectChanges();
    }

    it('🛑 `mode=year` ⇒ nút TẮT, và `title` nói lối ra TRƯỚC khi bấm', async () => {
      await bootReady({ mode: 'year' });

      const button = exportButton();
      expect(button.disabled).toBeTrue();
      expect(button.getAttribute('title')).toContain('Chọn một tuần hoặc một tháng');
    });

    it('bấm ⇒ `GET /dashboard/export` mang ĐÚNG bộ tham số của lượt tải hiện tại (Q23)', async () => {
      await bootReady({ status: 'Hoàn thành' });
      exportButton().click();

      const req = httpMock.expectOne((r) => r.url === '/dashboard/export');
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('mode')).toBe('week');
      expect(req.request.params.get('status')).withContext('export tôn trọng bộ lọc đang áp').toBe('Hoàn thành');
      expect(req.request.responseType).withContext('thân response là BYTES, không phải JSON').toBe('blob');
      req.flush(new Blob(['x'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), {
        headers: { 'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' },
      });
    });

    it('ĐANG LỌC ⇒ `title` nói rõ file chỉ chứa chỉ tiêu đang lọc, kèm số đếm', async () => {
      await bootReady({ status: 'Hoàn thành' });
      expect(exportButton().getAttribute('title')).toContain('CHỈ các chỉ tiêu đang lọc');
    });

    it('🛑 200 mà `Content-Type` là JSON ⇒ coi là LỖI, KHÔNG lưu thành `.xlsx`', async () => {
      await bootReady();
      exportButton().click();

      // Nếu bỏ phép kiểm này, người dùng lưu được một `.xlsx` mở lên chỉ có một dòng JSON — và
      // triệu chứng lúc đó không trỏ về đâu cả.
      const req = httpMock.expectOne((r) => r.url === '/dashboard/export');
      const createUrl = spyOn(URL, 'createObjectURL');
      req.flush(new Blob(['{"message":"x"}'], { type: 'application/json' }), {
        headers: { 'Content-Type': 'application/json' },
      });
      fixture.detectChanges();

      expect(createUrl).not.toHaveBeenCalled();
    });

    it('🛑 bấm hai lần KHÔNG gửi hai request — server sẽ tính lại cả báo cáo', async () => {
      await bootReady();
      exportButton().click();
      fixture.detectChanges();
      exportButton().click();

      const req = httpMock.expectOne((r) => r.url === '/dashboard/export');
      req.flush(new Blob(['x'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), {
        headers: { 'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' },
      });
    });
  });

  // ================================================================= Q69
  describe('🛑 ca Q32 — nhận dạng bằng `kpi`, KHÔNG bằng `table` (Q69)', () => {
    it('đã có chỉ tiêu, chưa có `Tiến độ %` ⇒ dải băng thông tin, KHÔNG phải `.warn`/`.bad`', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate({ kpi: KPI_AFTER_IMPORT })));
      fixture.detectChanges();

      const notice = host().querySelector('.notice');
      expect(notice?.textContent).toContain('Đã có 62 chỉ tiêu');
      expect(notice?.classList).not.toContain('warn');
      expect(notice?.classList).not.toContain('bad');
      expect(notice?.querySelector('a')?.getAttribute('href')).toBe('/danh-muc/dti');
    });

    /**
     * 🛑 NGHIỆM THU của Q69, viết đúng ca mà proxy cũ (`table.length > 0`) làm hỏng: lọc một nhóm
     * không khớp dòng nào ⇒ `table` RỖNG trong khi `kpi` vẫn đủ 62 chỉ tiêu. Băng phải VẪN hiện.
     */
    it('🛑 `table` rỗng vì bộ lọc, `kpi.totalCriteria` vẫn > 0 ⇒ băng Q32 VẪN hiện', async () => {
      await boot({ groupId: '11111111-1111-1111-1111-111111111111' });
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate({ kpi: KPI_AFTER_IMPORT, table: [] })));
      fixture.detectChanges();

      expect(host().querySelector('.notice')?.textContent)
        .withContext('proxy cũ đọc `table.length` sẽ làm băng biến mất đúng lúc cần hiện')
        .toContain('Đã có 62 chỉ tiêu');
    });

    it('ca đối chứng — đã có `Tiến độ %` thì KHÔNG băng nào', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate()));
      fixture.detectChanges();

      expect(host().querySelector('.notice')).toBeNull();
    });

    it('🛑 năm ô KPI ngay sau import: ô 1 và 2 hiện `—`, ô 3 và 4 hiện `0`, ô 5 hiện số thật (T12)', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate({ kpi: KPI_AFTER_IMPORT })));
      fixture.detectChanges();

      const values = [...host().querySelectorAll('.kpis .value')].map((el) => el.textContent?.trim());
      expect(values[0]).withContext('`—` = không tính được').toBe('—');
      expect(values[1]).toBe('—');
      expect(values[2]).withContext('`0` = tính được, kết quả bằng không — KHÔNG phải 62').toBe('0');
      expect(values[3]).toBe('0');
      expect(values[4]).toBe('26/62');
    });

    it('nhóm chưa có `Tiến độ %`: thanh KHÔNG có `.fill` nào, cột số hiện `—` (RỖNG ≠ 0)', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(
        ok(
          aggregate({
            kpi: KPI_AFTER_IMPORT,
            groups: [{ groupId: 'g1', groupCode: '1', groupName: 'Hạ tầng và Nền tảng số' }],
          }),
        ),
      );
      fixture.detectChanges();

      expect(host().querySelector('app-progress-bar .fill')).toBeNull();
      expect(host().querySelector('app-progress-bar .num')?.textContent?.trim()).toBe('—');
    });
  });

  // ================================================================= rỗng + lỗi
  describe('hai trạng thái rỗng còn lại (§5.3, §5.4)', () => {
    it('chưa có kỳ nào trong năm (`totalCriteria = 0`) ⇒ băng đầu trang, V2…V5 và V8 ẩn', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(
        ok(
          aggregate({
            kpi: { up: 0, flat: 0, down: 0, done: 0, totalCriteria: 0 },
            groups: [],
            trend: [],
            table: [],
          }),
        ),
      );
      fixture.detectChanges();

      expect(host().querySelector('.notice')?.textContent).toContain('Chưa có dữ liệu DTI nào');
      expect(host().querySelector('.kpis')).toBeNull();
      expect(host().querySelector('.history-card')).toBeNull();
      // Chân trang VẪN còn — nó là lối ra thứ hai.
      expect(host().querySelector('.footer a')?.getAttribute('href')).toBe('/danh-muc/dti');
    });

    it('tải hỏng ⇒ băng `.bad` đầu trang, KHÔNG để trang trắng (đây là trang đích của mọi redirect)', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush({ message: 'boom' }, { status: 500, statusText: 'Server Error' });
      fixture.detectChanges();

      expect(host().querySelector('.notice.bad')).not.toBeNull();
      expect(host().querySelector('.kpis')).toBeNull();
    });
  });

  // ================================================================= Q62
  describe('🛑 Q62 — tham số lạ trên URL', () => {
    it('`mode` lạ về `week`, và KHÔNG gọi API với giá trị sai', async () => {
      await boot({ mode: 'quarter' });
      flushPeriods();

      expect(aggregateRequests().length).toBe(0);

      await settle();
      const requests = aggregateRequests();
      expect(requests.length).toBe(1);
      expect(requests[0].request.params.get('mode')).toBe('week');
      requests[0].flush(ok(aggregate()));
    });

    it('`status` ngoài bốn giá trị Q4 bị bỏ', async () => {
      await boot({ status: 'Đang xử lý' });
      flushPeriods();
      await settle();

      const requests = aggregateRequests();
      expect(requests[0].request.params.has('status')).toBeFalse();
      requests[0].flush(ok(aggregate()));
    });

    it('`period` không khớp ĐƠN VỊ của chế độ bị bỏ (tháng trong chế độ Tuần)', async () => {
      await boot({ period: `${CURRENT_YEAR}-08` });
      flushPeriods();
      await settle();

      const requests = aggregateRequests();
      expect(requests[0].request.params.has('date')).toBeFalse();
      expect(requests[0].request.params.get('year')).toBe(`${CURRENT_YEAR}`);
      requests[0].flush(ok(aggregate()));
    });
  });

  // ================================================================= kỳ cụ thể
  it('🛑 chọn một kỳ cụ thể ⇒ gửi `date` của mốc đầu kỳ và KHÔNG gửi `year` (tránh lớp lỗi Q61)', async () => {
    await boot({ period: `${CURRENT_YEAR}-W33` });

    // Lượt ĐẦU chạy khi danh sách kỳ chưa về, nên chưa tra được mốc đầu kỳ ⇒ gửi `year` và để
    // server chọn kỳ hiện tại. Xả nó trước khi DB-3 về, nếu không `switchMap` huỷ nó giữa chừng.
    const first = aggregateRequests();
    expect(first.length).toBe(1);
    expect(first[0].request.params.get('year')).toBe(`${CURRENT_YEAR}`);
    first[0].flush(ok(aggregate()));

    flushPeriods([{ value: `${CURRENT_YEAR}-W33`, date: `${CURRENT_YEAR}-08-10`, overallProgress: 82.1 }]);
    await settle();

    const second = aggregateRequests();
    expect(second.length).withContext('tra được mốc đầu kỳ rồi thì phải gọi lại đúng kỳ đó').toBe(1);
    expect(second[0].request.params.get('date')).toBe(`${CURRENT_YEAR}-08-10`);
    expect(second[0].request.params.has('year')).toBeFalse();
    second[0].flush(ok(aggregate()));
  });

  // ================================================================= bảng chi tiết
  describe('bảng chi tiết (V5)', () => {
    it('caption `{đang hiện}/{tổng}` mang `aria-live` — chỗ DUY NHẤT nói bảng đang hẹp hơn trang', async () => {
      await boot();
      flushPeriods();
      aggregateRequests()[0].flush(ok(aggregate()));
      fixture.detectChanges();

      const caption = host().querySelector('.criteria-table-card .title span[aria-live="polite"]');
      expect(caption?.textContent?.trim()).toBe('1/62 chỉ tiêu');
    });

    it('sắp xếp mặc định theo mã, THỨ TỰ TỰ NHIÊN — `4.2` trước `4.10`', async () => {
      await boot();
      flushPeriods();
      const base = aggregate().table[0];
      aggregateRequests()[0].flush(
        ok(
          aggregate({
            table: [
              { ...base, criteriaId: 'a', code: '4.10' },
              { ...base, criteriaId: 'b', code: '4.2' },
            ],
          }),
        ),
      );
      fixture.detectChanges();

      const codes = [...host().querySelectorAll('tbody tr td:first-child')].map((td) => td.textContent?.trim());
      expect(codes).toEqual(['4.2', '4.10']);
    });
  });

  // ================================================================= lịch sử
  it('V8 — mới nhất TRƯỚC, kỳ cũ nhất in `Kỳ đầu` thay vì một delta bịa bằng 0', async () => {
    await boot();
    flushPeriods([
      { value: `${CURRENT_YEAR}-W32`, date: `${CURRENT_YEAR}-08-03`, overallProgress: 79.8 },
      { value: `${CURRENT_YEAR}-W33`, date: `${CURRENT_YEAR}-08-10`, overallProgress: 82.1 },
    ]);
    aggregateRequests()[0].flush(ok(aggregate()));
    fixture.detectChanges();

    const rows = host().querySelectorAll('app-history-row');
    expect(rows.length).toBe(2);
    expect(rows[0].querySelector('b')?.textContent?.trim())
      .withContext('hàng đầu phải là kỳ MỚI NHẤT')
      .toBe(`10/08 – 16/08/${CURRENT_YEAR}`);
    expect(rows[1].textContent).toContain('Kỳ đầu');
    // Biến động tính trên danh sách xếp TĂNG dần rồi mới đảo — tính sau khi đảo là lật mọi dấu.
    expect(rows[0].querySelector('.delta')?.classList).toContain('up');
  });
});
