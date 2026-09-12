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
import { ICriteriaGridDto, ICriteriaRowDto } from '../../models/danh-muc-dti.model';
import { DanhMucDtiPage } from './danh-muc-dti.page';

const CURRENT_YEAR = new Date().getFullYear();

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-dti-page',
    retryable: null,
    fields: null,
  };
}

const ROW: ICriteriaRowDto = {
  criteriaId: 'c1',
  code: '1.1',
  name: 'Tỷ lệ hồ sơ trực tuyến',
  groupId: '11111111-1111-1111-1111-111111111111',
  groupCode: '1',
  groupName: 'Hạ tầng và Nền tảng số',
  maxScore: 10,
  selfScore: 7.04,
  verifiedScore: 10,
  diff: 2.96,
  status: 'Đang thực hiện',
  version: 'v1',
  assessmentPeriod: `${CURRENT_YEAR}-W33`,
  assessmentPeriodLabel: `Tuần 33/${CURRENT_YEAR} (10/08 – 16/08/${CURRENT_YEAR})`,
};

function grid(patch: Partial<ICriteriaGridDto> = {}): ICriteriaGridDto {
  return {
    items: [ROW],
    page: 1,
    pageSize: 10,
    totalCount: 62,
    canWrite: true,
    isEditable: true,
    editBlockedBy: [],
    isCurrentYear: true,
    currentPeriod: `${CURRENT_YEAR}-W33`,
    currentPeriodLabel: `Tuần 33/${CURRENT_YEAR} (10/08 – 16/08/${CURRENT_YEAR})`,
    ...patch,
  };
}

/**
 * Bảng dịch của `LanguageService` đi qua `HttpBackend`, mà `provideHttpClientTesting()` thay CẢ
 * backend — nên các request bảng dịch rơi vào mock rồi nằm đó và `verify()` báo đỏ ở một
 * chỗ chẳng liên quan. Danh sách tiền tố đọc thẳng từ `APP_I18N`, không khai cứng.
 */
function drainI18nRequests(httpMock: HttpTestingController): void {
  httpMock
    .match((req) => APP_I18N.resources.some((prefix) => req.url.startsWith(prefix)))
    .forEach((req) => req.flush({}));
}

describe('DanhMucDtiPage — đường đọc + bốn luồng ghi (vòng 2)', () => {
  let fixture: ComponentFixture<DanhMucDtiPage>;
  let httpMock: HttpTestingController;
  let router: Router;

  async function boot(queryParams: Record<string, string> = {}): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        // Route `**` chứ không `[]`: trang ghi bộ lọc lên URL bằng `router.navigate`, và điều
        // hướng tới một URL không khớp cấu hình nào sẽ hỏng ở `NavigationError`.
        provideRouter([{ path: '**', children: [] }]),
        provideTranslateService(),
        provideCoreI18n(APP_I18N),
      ],
    });
    await useTranslationsInTest();
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    await router.navigate([], { queryParams });
    fixture = TestBed.createComponent(DanhMucDtiPage);
    fixture.detectChanges();
  }

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  function gridRequests(): TestRequest[] {
    return httpMock.match((req) => req.url === '/criteria');
  }

  /** Xả DM-1 và DB-3 — hai nguồn phụ, không phải thứ mỗi test đang kiểm. */
  function flushSideSources(): void {
    httpMock.match((req) => req.url === '/criteria-groups').forEach((req) => req.flush(ok([])));
    httpMock
      .match((req) => req.url === '/dashboard/periods')
      .forEach((req) => req.flush(ok({ years: [CURRENT_YEAR], weeksInYear: [], monthsInYear: [] })));
  }

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  /** Chờ thật một nhịp cho `router.navigate` hoàn tất rồi chạy CD. */
  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
  }

  // ================================================================= mặc định
  describe('trạng thái mặc định (§5.1)', () => {
    it('gọi DM-2 với year = năm hiện tại, period = all, pageSize = 10 (Q19)', async () => {
      await boot();
      flushSideSources();

      const requests = gridRequests();
      expect(requests.length).toBe(1);
      expect(requests[0].request.params.get('year')).toBe(`${CURRENT_YEAR}`);
      expect(requests[0].request.params.get('period')).toBe('all');
      expect(requests[0].request.params.get('pageSize')).toBe('10');
      requests[0].flush(ok(grid()));
    });

    it('cột `Kỳ của số liệu` CÓ render ở chế độ `Tất cả` (Q31 luật a)', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      const headers = [...host().querySelectorAll('thead th')].map((th) => th.textContent?.trim());
      expect(headers).toContain('Kỳ của số liệu');
    });

    it('chọn MỘT kỳ cụ thể ⇒ cột `Kỳ của số liệu` KHÔNG render, và số cột giảm đúng một', async () => {
      await boot({ period: `${CURRENT_YEAR}-W33` });
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      const headers = [...host().querySelectorAll('thead th')].map((th) => th.textContent?.trim());
      expect(headers).not.toContain('Kỳ của số liệu');
      expect(headers.length).toBe(13);
    });

    /**
     * 🛑 Bốn nhãn chip và hai nhãn option kỳ được tra bằng khoá DỰNG ĐỘNG
     * (`` `danh-muc-dti.filter.${key}` ``). Tra trượt thì ngx-translate **trả về chính chuỗi
     * khoá** — màn hình hiện `danh-muc-dti.filter.chipYear` giữa giao diện, không lỗi, không
     * cảnh báo, và G12 vẫn xanh vì template CÓ bọc `| translate`. Test này là chốt chặn duy nhất.
     */
    it('🛑 chip và option kỳ tra ra CÂU, không phải chuỗi khoá (khoá dựng động)', async () => {
      const groupId = '11111111-1111-1111-1111-111111111111';
      await boot({ groupId, status: 'Hoàn thành', year: '2025', period: '2025-W33' });
      httpMock
        .match((req) => req.url === '/criteria-groups')
        .forEach((req) => req.flush(ok([{ id: groupId, code: '1', name: 'Hạ tầng và Nền tảng số', displayOrder: 1 }])));
      httpMock.match((req) => req.url === '/dashboard/periods').forEach((req) =>
        req.flush(
          ok({
            years: [2025, CURRENT_YEAR],
            weeksInYear: [{ value: '2025-W33', date: '2025-08-11', overallProgress: 82.1 }],
            monthsInYear: [],
          }),
        ),
      );
      await settle();
      gridRequests().forEach((req) => req.flush(ok(grid())));
      fixture.detectChanges();

      const chips = [...host().querySelectorAll('.filter-chip')].map((el) => el.textContent?.trim() ?? '');
      expect(chips.length).withContext('bốn điều kiện đều KHÁC mặc định ⇒ bốn chip').toBe(4);
      expect(chips[0]).toContain('Nhóm: 1. Hạ tầng và Nền tảng số');
      expect(chips[1]).toContain('Trạng thái: Hoàn thành');
      expect(chips[2]).toContain('Năm: 2025');
      expect(chips[3]).toContain('Kỳ: Tuần 33: 11/08 – 17/08/2025');
      expect(chips.join(' ')).not.toContain('danh-muc-dti.');
      expect(host().querySelector('.filter-count')?.textContent?.trim()).toBe('4');
    });

    it('`.filter-count` KHÔNG render khi cả bốn ô lọc ở mặc định (T8)', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      expect(host().querySelector('.filter-count')).toBeNull();
      expect(host().querySelectorAll('.filter-chip').length).toBe(0);
    });
  });

  // ================================================================= Q62
  describe('🛑 Q62 — tham số lạ trên URL', () => {
    /**
     * Điều kiện nghiệm thu THẬT của Q62 nằm ở vế thứ hai: **không lần gọi API nào mang giá trị
     * sai**. Chỉ kiểm "URL đã được sửa" là bỏ sót đúng nửa quan trọng — server vẫn sẽ trả 400 nếu
     * request kia đã bay đi.
     */
    it('🛑 `status` ngoài bốn giá trị Q4: KHÔNG gọi API với giá trị sai, và URL được sửa lại', async () => {
      await boot({ status: 'Đang xử lý' });
      flushSideSources();

      expect(gridRequests().length).withContext('phải chờ URL sạch rồi mới gọi').toBe(0);

      await settle();
      expect(router.url).not.toContain('status');

      const requests = gridRequests();
      expect(requests.length).toBe(1);
      expect(requests[0].request.params.has('status')).toBeFalse();
      requests[0].flush(ok(grid()));
    });

    it('`period` sai khuôn về `all`', async () => {
      await boot({ period: 'tuan-33' });
      flushSideSources();
      await settle();

      const requests = gridRequests();
      expect(requests.length).toBe(1);
      expect(requests[0].request.params.get('period')).toBe('all');
      requests[0].flush(ok(grid()));
    });

    it('`year` không phải số nguyên về năm hiện tại', async () => {
      await boot({ year: 'nam-ngoai' });
      flushSideSources();
      await settle();

      const requests = gridRequests();
      expect(requests[0].request.params.get('year')).toBe(`${CURRENT_YEAR}`);
      requests[0].flush(ok(grid()));
    });

    it('`pageSize` ngoài {10, 20, 50} về 10; `page` < 1 về 1', async () => {
      await boot({ pageSize: '37', page: '0' });
      flushSideSources();
      await settle();

      const requests = gridRequests();
      expect(requests[0].request.params.get('pageSize')).toBe('10');
      expect(requests[0].request.params.get('page')).toBe('1');
      requests[0].flush(ok(grid()));
    });

    it('`groupId` không phải GUID bị bỏ', async () => {
      await boot({ groupId: 'nhom-1' });
      flushSideSources();
      await settle();

      const requests = gridRequests();
      expect(requests[0].request.params.has('groupId')).toBeFalse();
      requests[0].flush(ok(grid()));
    });

    it('ca đối chứng — tham số HỢP LỆ đi qua nguyên vẹn và gọi API NGAY, không chờ một vòng', async () => {
      await boot({ status: 'Hoàn thành', period: `${CURRENT_YEAR}-08`, pageSize: '20' });
      flushSideSources();

      const requests = gridRequests();
      expect(requests.length).withContext('URL đã sạch thì không được hoãn lời gọi').toBe(1);
      expect(requests[0].request.params.get('status')).toBe('Hoàn thành');
      expect(requests[0].request.params.get('period')).toBe(`${CURRENT_YEAR}-08`);
      expect(requests[0].request.params.get('pageSize')).toBe('20');
      requests[0].flush(ok(grid()));
    });
  });

  // ================================================================= quyền ghi
  describe('khối quyền CẤP MÀN (§7.1a)', () => {
    it('không có quyền ghi ⇒ ẨN hai nút toolbar và ẨN CẢ cột `Hành động` (Q39)', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(
        ok(grid({ canWrite: false, isEditable: false, editBlockedBy: ['NO_WRITE_PERMISSION'] })),
      );
      fixture.detectChanges();

      const headers = [...host().querySelectorAll('thead th')].map((th) => th.textContent?.trim());
      expect(headers).not.toContain('Hành động');
      expect(host().querySelector('.toolbar-actions .btn')).toBeNull();
    });

    /** Q51: mã `NO_WRITE_PERMISSION` KHÔNG sinh dòng băng nào — thiếu quyền không phải lỗi bộ lọc. */
    it('🛑 thiếu quyền ghi ⇒ KHÔNG có dải băng nào (Q51)', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(
        ok(grid({ canWrite: false, isEditable: false, editBlockedBy: ['NO_WRITE_PERMISSION'] })),
      );
      fixture.detectChanges();

      expect(host().querySelector('.notice')).toBeNull();
    });

    it('có quyền ghi + ghi được ⇒ CẢ HAI nút toolbar bấm được', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      const buttons = [...host().querySelectorAll('.toolbar-actions button')] as HTMLButtonElement[];
      expect(buttons.map((b) => b.textContent?.trim())).toEqual(['Import CSV/Excel', '+ Thêm chỉ tiêu']);
      expect(buttons.every((b) => b.disabled)).toBeFalse();
    });

    it('🛑 bộ lọc đưa bảng về chỉ đọc ⇒ `Import CSV/Excel` vẫn HIỆN nhưng `disabled` (§5.5.1)', async () => {
      await boot({ period: `${CURRENT_YEAR}-08` });
      flushSideSources();
      gridRequests()[0].flush(ok(grid({ isEditable: false, editBlockedBy: ['PERIOD_NOT_WEEKLY'] })));
      fixture.detectChanges();

      const importButton = host().querySelector('.toolbar-actions button') as HTMLButtonElement;
      expect(importButton.textContent?.trim()).toBe('Import CSV/Excel');
      expect(importButton.disabled).withContext('"không phải lúc này", không phải "không phải của bạn"').toBeTrue();
    });

    it('cột `Hành động` hiện khi có quyền, và hai nút trong đó bấm được', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      const rowButtons = [...host().querySelectorAll('tbody .row-actions button')] as HTMLButtonElement[];
      expect(rowButtons.map((b) => b.textContent?.trim())).toEqual(['Sửa', 'Xoá']);
      expect(rowButtons.every((b) => b.disabled)).toBeFalse();
    });

    it('🛑 KHÔNG có quyền ghi ⇒ hai ô sửa inline mất luôn ngữ nghĩa control (§5.6.1 ràng buộc 3)', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(
        ok(grid({ canWrite: false, isEditable: false, editBlockedBy: ['NO_WRITE_PERMISSION'] })),
      );
      fixture.detectChanges();

      // Để lại `tabindex` trên một ô không mở được là đưa bàn phím vào một control câm.
      expect(host().querySelector('tbody [role="button"]')).toBeNull();
      expect(host().querySelector('tbody [tabindex]')).toBeNull();
      expect(host().querySelector('tbody .cell-editable')).toBeNull();
    });

    it('🛑 bộ lọc đưa về chỉ đọc ⇒ cũng mất affordance inline, dù VẪN có quyền', async () => {
      await boot({ period: `${CURRENT_YEAR}-08` });
      flushSideSources();
      gridRequests()[0].flush(ok(grid({ isEditable: false, editBlockedBy: ['PERIOD_NOT_WEEKLY'] })));
      fixture.detectChanges();

      expect(host().querySelector('tbody .cell-editable')).toBeNull();
      // Nhưng cột `Hành động` VẪN render — `canWrite` quyết ẩn, `isEditable` chỉ quyết `disabled`.
      const rowButtons = [...host().querySelectorAll('tbody .row-actions button')] as HTMLButtonElement[];
      expect(rowButtons.length).toBe(2);
      expect(rowButtons.every((b) => b.disabled)).toBeTrue();
    });

    it('ghi được ⇒ hai ô sửa inline mang `.cell-editable` + `role="button"` + `tabindex="0"`', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      const cells = [...host().querySelectorAll('tbody .cell-editable')] as HTMLElement[];
      expect(cells.length).withContext('đúng HAI ô, không hơn — Q9').toBe(2);
      expect(cells.every((cell) => cell.getAttribute('role') === 'button')).toBeTrue();
      expect(cells.every((cell) => cell.getAttribute('tabindex') === '0')).toBeTrue();
    });
  });

  // ================================================================= dải băng V3
  describe('dải băng V3 — chỉ đọc vì bộ lọc (§5.5.1)', () => {
    it('`PERIOD_NOT_WEEKLY` + năm hiện tại ⇒ câu CÓ nêu lối `Tất cả` (Q60)', async () => {
      await boot({ period: `${CURRENT_YEAR}-08` });
      flushSideSources();
      gridRequests()[0].flush(ok(grid({ isEditable: false, editBlockedBy: ['PERIOD_NOT_WEEKLY'], isCurrentYear: true })));
      fixture.detectChanges();

      const text = host().querySelector('.notice')?.textContent ?? '';
      expect(text).toContain('chỉ đọc');
      expect(text).toContain('Tất cả (mới nhất trong năm)');
    });

    /**
     * 🛑 `isCurrentYear` đến từ DM-2 (Q66), KHÔNG suy từ đồng hồ máy khách. Ca này chứng minh điều
     * đó: URL vẫn mang năm hiện tại, nhưng server nói `isCurrentYear = false` và câu băng phải đổi
     * theo server.
     */
    it('🛑 `PERIOD_NOT_WEEKLY` + `isCurrentYear = false` ⇒ câu KHÔNG nêu lối `Tất cả`', async () => {
      await boot({ period: `${CURRENT_YEAR}-08` });
      flushSideSources();
      gridRequests()[0].flush(
        ok(grid({ isEditable: false, editBlockedBy: ['PERIOD_NOT_WEEKLY'], isCurrentYear: false })),
      );
      fixture.detectChanges();

      const text = host().querySelector('.notice')?.textContent ?? '';
      expect(text).toContain('Chọn một tuần cụ thể');
      expect(text).not.toContain('Tất cả (mới nhất trong năm)');
    });

    it('`PERIOD_OUT_OF_YEAR` ⇒ câu nêu CẢ hai lối thoát (T15 + Q41)', async () => {
      await boot({ year: '2025' });
      flushSideSources();
      gridRequests()[0].flush(
        ok(grid({ isEditable: false, editBlockedBy: ['PERIOD_OUT_OF_YEAR'], isCurrentYear: false })),
      );
      fixture.detectChanges();

      const text = host().querySelector('.notice')?.textContent ?? '';
      expect(text).toContain('2025');
      expect(text).toContain('Chọn một tuần cụ thể');
      expect(text).toContain('Năm đánh giá');
    });

    it('mã LẠ vẫn coi là bị chặn — hiện một câu chung, KHÔNG im lặng bỏ qua', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid({ isEditable: false, editBlockedBy: ['MOT_MA_MOI'] })));
      fixture.detectChanges();

      expect(host().querySelector('.notice')?.textContent).toContain('chỉ đọc');
    });

    it('🛑 đang xem ĐÚNG tuần mà lời ghi sẽ rơi vào ⇒ KHÔNG có băng nào', async () => {
      await boot({ period: `${CURRENT_YEAR}-W33` });
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      expect(host().querySelector('.notice')).toBeNull();
    });
  });

  // ============================================== dải băng V3 — nhắc kỳ đích (§5.5, Q72)
  describe('dải băng V3 — nhắc kỳ đích (§5.5)', () => {
    it('`Tất cả` ⇒ băng nói lời ghi rơi vào TUẦN HIỆN TẠI, dùng nhãn của SERVER', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      const text = host().querySelector('.notice')?.textContent ?? '';
      expect(text).toContain(`Tuần 33/${CURRENT_YEAR} (10/08 – 16/08/${CURRENT_YEAR})`);
      expect(text).toContain('tuần hiện tại');
    });

    it('một tuần KHÁC tuần hiện tại ⇒ băng nhắc rằng số liệu KHÔNG vào tuần này', async () => {
      await boot({ period: `${CURRENT_YEAR}-W31` });
      httpMock.match((req) => req.url === '/criteria-groups').forEach((req) => req.flush(ok([])));
      // Nhãn tuần đang chọn dựng từ `weeksInYear` của DB-3 — một phép TRA, không phải quy đổi lịch.
      // KHÔNG dùng `flushSideSources()`: nó trả `weeksInYear` RỖNG, nên không có nhãn nào để tra.
      httpMock
        .match((req) => req.url === '/dashboard/periods')
        .forEach((req) =>
          req.flush(
            ok({
              years: [CURRENT_YEAR],
              weeksInYear: [{ value: `${CURRENT_YEAR}-W31`, date: `${CURRENT_YEAR}-07-27` }],
              monthsInYear: [],
            }),
          ),
        );
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();

      const text = host().querySelector('.notice')?.textContent ?? '';
      expect(text).toContain('không phải tuần hiện tại');
    });

    it('🛑 chỉ đọc vì bộ lọc ĐÈ băng nhắc kỳ đích — tối đa MỘT băng', async () => {
      await boot({ period: `${CURRENT_YEAR}-08` });
      flushSideSources();
      gridRequests()[0].flush(ok(grid({ isEditable: false, editBlockedBy: ['PERIOD_NOT_WEEKLY'] })));
      fixture.detectChanges();

      const notices = host().querySelectorAll('.notice');
      expect(notices.length).toBe(1);
      expect(notices[0].textContent).toContain('chỉ đọc');
    });
  });

  // ================================================================= rỗng
  describe('trạng thái rỗng (§5.3)', () => {
    it('🛑 T9 — chưa import lần nào: `NoticeBanner` TRÊN lưới, và lưới VẪN render đủ header', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid({ items: [], totalCount: 0 })));
      fixture.detectChanges();

      const notice = host().querySelector('.notice');
      expect(notice?.textContent).toContain('Danh mục chưa có chỉ tiêu nào');
      expect(notice?.classList).withContext('biến thể mặc định — không phải lỗi, không phải cảnh báo').not.toContain('bad');
      expect(host().querySelectorAll('thead th').length)
        .withContext('header là thứ cho người dùng biết file import cần cột nào')
        .toBeGreaterThan(0);
    });

    it('T9 + thiếu quyền ghi ⇒ băng BỎ hai lối đi, chỉ còn câu thông báo', async () => {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(
        ok(grid({ items: [], totalCount: 0, canWrite: false, isEditable: false, editBlockedBy: ['NO_WRITE_PERMISSION'] })),
      );
      fixture.detectChanges();

      const text = host().querySelector('.notice')?.textContent ?? '';
      expect(text).toContain('Danh mục chưa có chỉ tiêu nào');
      expect(text).not.toContain('Import CSV/Excel');
    });

    it('🛑 lọc không khớp ⇒ thông điệp TRONG BẢNG, KHÔNG phải băng', async () => {
      // `period` = đúng tuần hiện tại để băng "nhắc kỳ đích" không hiện — phép kiểm ở đây nói về
      // ca RỖNG-VÌ-LỌC, và một băng hợp lệ của ca khác chen vào sẽ làm nó đo nhầm thứ.
      await boot({ status: 'Hoàn thành', period: `${CURRENT_YEAR}-W33` });
      flushSideSources();
      gridRequests()[0].flush(ok(grid({ items: [], totalCount: 0 })));
      fixture.detectChanges();

      expect(host().querySelector('.notice')).toBeNull();
      expect(host().querySelector('tbody td.muted')?.textContent?.trim()).toBe('Không có chỉ tiêu nào khớp bộ lọc.');
    });
  });

  // ================================================================= đường GHI
  describe('đường ghi (DM-3 … DM-7)', () => {
    async function bootWritable(): Promise<void> {
      await boot();
      flushSideSources();
      gridRequests()[0].flush(ok(grid()));
      fixture.detectChanges();
      // Hộp thoại Thêm/Sửa nạp danh sách `Phụ trách` ngay khi trang dựng (`startWith('')`).
      httpMock.match((req) => req.url === '/users').forEach((req) => req.flush(ok({ items: [], totalCount: 0 })));
    }

    it('bấm `Import CSV/Excel` ⇒ V11 mở, và nói KỲ ĐÍCH trước khi bấm Nhập', async () => {
      await bootWritable();
      (host().querySelectorAll('.toolbar-actions button')[0] as HTMLButtonElement).click();
      fixture.detectChanges();

      const dialog = host().querySelector('app-import-dialog dialog');
      expect(dialog?.hasAttribute('open')).toBeTrue();
      expect(dialog?.querySelector('.import-target')?.textContent)
        .withContext('62 dòng sắp rơi vào tuần nào — hệ thống KHÔNG đọc kỳ từ nội dung file')
        .toContain(`Tuần 33/${CURRENT_YEAR}`);
    });

    it('bấm `+ Thêm chỉ tiêu` ⇒ V9 mở ở chế độ THÊM, mọi ô trống', async () => {
      await bootWritable();
      (host().querySelectorAll('.toolbar-actions button')[1] as HTMLButtonElement).click();
      fixture.detectChanges();

      const dialog = host().querySelector('app-criteria-form-dialog dialog');
      expect(dialog?.querySelector('h2')?.textContent?.trim()).toBe('Thêm chỉ tiêu');
      expect((dialog?.querySelector('#cfCode') as HTMLInputElement).value).toBe('');
    });

    it('bấm `Sửa` ⇒ V9 mở ở chế độ SỬA, 10 ô nạp từ đúng dòng đó', async () => {
      await bootWritable();
      (host().querySelectorAll('tbody .row-actions button')[0] as HTMLButtonElement).click();
      fixture.detectChanges();

      const dialog = host().querySelector('app-criteria-form-dialog dialog');
      expect(dialog?.querySelector('h2')?.textContent?.trim()).toBe('Sửa chỉ tiêu');
      expect((dialog?.querySelector('#cfCode') as HTMLInputElement).value).toBe('1.1');
      expect((dialog?.querySelector('#cfSelfScore') as HTMLInputElement).value)
        .withContext('số THÔ, không định dạng locale — ô nhập, không phải ô hiển thị')
        .toBe('7.04');
    });

    it('🛑 lưu dialog SỬA ⇒ MỘT request `PUT` mang `assessment` LỒNG kèm kỳ đích', async () => {
      await bootWritable();
      (host().querySelectorAll('tbody .row-actions button')[0] as HTMLButtonElement).click();
      fixture.detectChanges();

      const dialog = host().querySelector('app-criteria-form-dialog dialog') as HTMLElement;
      (dialog.querySelectorAll('.dialog-actions button')[1] as HTMLButtonElement).click();

      const req = httpMock.expectOne('/criteria/c1');
      expect(req.request.method).toBe('PUT');
      const body = req.request.body as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual(['assessment', 'code', 'groupId', 'maxScore', 'name']);

      const assessment = body['assessment'] as Record<string, unknown>;
      expect(assessment['period']).withContext('nguyên giá trị ô lọc — Q40').toBe('all');
      expect(assessment['year']).withContext('bắt buộc khi period = "all" — T15').toBe(CURRENT_YEAR);
      req.flush(ok({ ...ROW, selfScore: 9 }));
    });

    it('🛑 bấm `Xoá` ⇒ hộp thoại hỏi nêu MÃ + TÊN, và chưa gọi API nào', async () => {
      await bootWritable();
      (host().querySelectorAll('tbody .row-actions button')[1] as HTMLButtonElement).click();
      fixture.detectChanges();

      const confirm = host().querySelector('app-confirm-dialog dialog');
      expect(confirm?.querySelector('.dialog-desc')?.textContent).toContain('1.1 — Tỷ lệ hồ sơ trực tuyến');
      httpMock.expectNone((req) => req.method === 'DELETE');
    });

    it('xác nhận xoá ⇒ `DELETE` rồi TẢI LẠI lưới (xoá đổi totalCount và phân trang)', async () => {
      await bootWritable();
      (host().querySelectorAll('tbody .row-actions button')[1] as HTMLButtonElement).click();
      fixture.detectChanges();

      const confirm = host().querySelector('app-confirm-dialog dialog') as HTMLElement;
      (confirm.querySelectorAll('.dialog-actions button')[1] as HTMLButtonElement).click();
      // `<dialog>` phát `close` ở một task sau, và `(confirmed)` treo trên `close` — không chờ
      // một nhịp thì phép kiểm chạy trước khi luồng xoá kịp gửi gì.
      await settle();

      httpMock.expectOne('/criteria/c1').flush(ok({ hardDeleted: true }));
      fixture.detectChanges();
      // `httpMock.match()` GỠ request khỏi danh sách đang mở, nên gọi `gridRequests()` hai lần thì
      // lần thứ hai luôn rỗng. Giữ một tham chiếu.
      const reload = gridRequests();
      expect(reload.length).withContext('lượt tải lại sau khi xoá').toBe(1);
      reload[0].flush(ok(grid({ items: [], totalCount: 0 })));
    });

    it('🛑 sửa inline ⇒ `PUT` mang ĐÚNG trường vừa sửa + `version` (Q74)', async () => {
      await bootWritable();
      const cell = host().querySelectorAll('tbody .cell-editable')[0] as HTMLElement;
      cell.dispatchEvent(new MouseEvent('dblclick'));
      fixture.detectChanges();

      const input = host().querySelector('tbody input.progressInput') as HTMLInputElement;
      input.value = '70';
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

      const req = httpMock.expectOne('/criteria/c1/assessment');
      expect(req.request.method).toBe('PUT');
      const body = req.request.body as Record<string, unknown>;
      expect(body['progressPercent']).toBe(70);
      // 🔄 Q74: BE phân biệt khoá VẮNG với khoá mang `null`. Chỉ sửa `Tiến độ %` thì `note` không
      // được ra dây — gửi kèm giá trị cũ là ghi đè mù thứ người khác vừa đổi.
      expect('note' in body).withContext('không đụng tới ⇒ KHÔNG có khoá').toBeFalse();
      expect(body['version']).toBe('v1');
      expect(body['period']).toBe('all');

      req.flush(ok({ ...ROW, progressPercent: 70 }));
      fixture.detectChanges();
      expect(gridRequests().length).withContext('thay dòng TẠI CHỖ, không refetch').toBe(0);
    });

    it('🛑 `Tiến độ %` bị KẸP về [0,100] trước khi gửi', async () => {
      await bootWritable();
      const cell = host().querySelectorAll('tbody .cell-editable')[0] as HTMLElement;
      cell.dispatchEvent(new MouseEvent('dblclick'));
      fixture.detectChanges();

      const input = host().querySelector('tbody input.progressInput') as HTMLInputElement;
      input.value = '250';
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

      const req = httpMock.expectOne('/criteria/c1/assessment');
      expect(req.request.body.progressPercent).toBe(100);
      req.flush(ok({ ...ROW, progressPercent: 100 }));
    });

    it('🛑 `Escape` HUỶ — không gửi request nào, dù đã gõ', async () => {
      await bootWritable();
      const cell = host().querySelectorAll('tbody .cell-editable')[0] as HTMLElement;
      cell.dispatchEvent(new MouseEvent('dblclick'));
      fixture.detectChanges();

      const input = host().querySelector('tbody input.progressInput') as HTMLInputElement;
      input.value = '70';
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      input.dispatchEvent(new Event('blur'));

      httpMock.expectNone('/criteria/c1/assessment');
    });

    it('`Enter`/`Space` trên ô cũng mở editor — bàn phím làm được mọi thứ chuột làm được', async () => {
      await bootWritable();
      const cell = host().querySelectorAll('tbody .cell-editable')[0] as HTMLElement;
      cell.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      fixture.detectChanges();

      expect(host().querySelector('tbody input.progressInput')).not.toBeNull();
    });
  });

  // ================================================================= lỗi
  it('tải hỏng ⇒ GỠ HẲN bảng rồi hiện khối lỗi `.bad` — không giữ dữ liệu của lượt trước', async () => {
    await boot();
    flushSideSources();
    gridRequests()[0].flush({ message: 'boom' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(host().querySelector('.notice.bad')).not.toBeNull();
    expect(host().querySelector('app-criteria-grid-table'))
      .withContext('giữ lại bảng của lượt trước là ca nguy hiểm nhất — nó trông như kết quả mới')
      .toBeNull();
    expect(host().querySelector('.notice.bad button')?.textContent?.trim()).toBe('Thử lại');
  });

  // ================================================================= tiêu đề
  it('số chỉ tiêu ở hàng tiêu đề mang `aria-live` — bằng chứng DUY NHẤT rằng bộ lọc đã chạy', async () => {
    await boot();
    flushSideSources();
    gridRequests()[0].flush(ok(grid()));
    fixture.detectChanges();

    const count = host().querySelector('.title span[aria-live="polite"]');
    expect(count?.textContent?.trim()).toBe('62 chỉ tiêu');
  });
});
