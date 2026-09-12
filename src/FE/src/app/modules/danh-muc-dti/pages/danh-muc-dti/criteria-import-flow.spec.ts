import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../../app.config';
import { provideCoreI18n } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { IApiResult } from '../../../../core/http/api-result.model';
import { provideCoreRoutes } from '../../../../core/config/core-routes';
import { httpErrorInterceptor } from '../../../../core/interceptors/http-error.interceptor';
import { ToastService } from '../../../../core/toast/toast.service';
import { CriteriaImportFlow } from './criteria-import-flow';

const POLL_MS = 1500;

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-import-flow',
    retryable: null,
    fields: null,
  };
}

function csv(): File {
  return new File(['x'], 'dti.csv', { type: 'text/csv' });
}

/**
 * Đo thời gian bằng `jasmine.clock()` chứ không `fakeAsync()`: dự án chạy **zoneless**
 * (`angular.json` khai `"polyfills": []`), nên `fakeAsync` không có zone để bắt timer. Cùng lựa
 * chọn và cùng lý do với `core/interceptors/http-error.interceptor.spec.ts`.
 */
describe('CriteriaImportFlow (CONTRACT DM-7 — job nền + poll)', () => {
  let flow: CriteriaImportFlow;
  let httpMock: HttpTestingController;
  let toast: ToastService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        // Interceptor THẬT, không phải một mock: `err.apiResult` — thứ `errorOutcome` đọc để tra
        // bản dịch của `businessCode` — do chính nó gắn vào (`http-error.interceptor.ts:267`).
        // Bỏ nó ra thì test xanh với một đường lùi mà người dùng thật không bao giờ đi.
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        // `httpErrorInterceptor` inject `Router` + `CORE_ROUTES` (nó điều hướng về màn đăng nhập ở
        // ca 401). Thiếu hai thứ này thì `inject()` ném ngay trong thân interceptor, request KHÔNG
        // bao giờ rời client, và mọi phép kiểm ở đây đỏ ở một chỗ chẳng liên quan.
        provideRouter([{ path: '**', children: [] }]),
        provideCoreRoutes({ signIn: '/dang-nhap', changePassword: '/doi-mat-khau', home: '/trang-chu' }),
        provideTranslateService(),
        provideCoreI18n(APP_I18N),
        CriteriaImportFlow,
      ],
    });
    await useTranslationsInTest();
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
    flow = TestBed.inject(CriteriaImportFlow);
    jasmine.clock().install();
  });

  afterEach(() => {
    jasmine.clock().uninstall();
    httpMock
      .match((req) => APP_I18N.resources.some((prefix) => req.url.startsWith(prefix)))
      .forEach((req) => req.flush({}));
    httpMock.verify();
  });

  function start(): void {
    flow.open();
    flow.submit(csv(), { Period: '2026-W33' }, imported);
    httpMock.expectOne('/import').flush(ok({ jobId: 'job-1' }));
  }

  let importedCount = 0;
  const imported = (): void => {
    importedCount += 1;
  };

  beforeEach(() => (importedCount = 0));

  it('Pending → Running → Succeeded: V11 đóng, V12 mở, `busy` tắt', () => {
    start();
    expect(flow.busy()).withContext('đã gửi, đang chờ').toBeTrue();

    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Pending' }));
    expect(flow.resultOpen()).withContext('chưa xong thì chưa có kết quả nào').toBeFalse();

    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Running' }));
    expect(flow.dialogOpen()).withContext('đang chạy thì V11 KHÔNG đóng').toBeTrue();

    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(
      ok({
        status: 'Succeeded',
        result: { totalRows: 62, successCount: 59, errorCount: 3, criteriaCreatedCount: 2, errors: [] },
      }),
    );

    expect(flow.busy()).toBeFalse();
    expect(flow.dialogOpen()).toBeFalse();
    expect(flow.resultOpen()).toBeTrue();
    expect(flow.result()?.SuccessCount).toBe(59);
  });

  it('🛑 vòng poll DỪNG sau `Succeeded` — `takeWhile(..., true)` chứ không poll mãi', () => {
    start();
    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(
      ok({
        status: 'Succeeded',
        result: { totalRows: 1, successCount: 1, errorCount: 0, criteriaCreatedCount: 0, errors: [] },
      }),
    );

    jasmine.clock().tick(POLL_MS * 4);
    httpMock.expectNone('/import/job-1');
  });

  it('đóng V12 ⇒ TẢI LẠI lưới (import đổi cả số dòng lẫn phân trang)', () => {
    start();
    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(
      ok({
        status: 'Succeeded',
        result: { totalRows: 62, successCount: 62, errorCount: 0, criteriaCreatedCount: 62, errors: [] },
      }),
    );

    flow.closeResult();
    expect(importedCount).toBe(1);
    expect(flow.result()).toBeNull();
  });

  it('🛑 `Failed` (hạ tầng) ⇒ TOAST, KHÔNG mở V12 — `errorMessage` của job là dev-facing', () => {
    const spy = spyOn(toast, 'error');
    start();
    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Failed', errorMessage: 'NullReferenceException at row 3' }));

    expect(flow.resultOpen()).toBeFalse();
    expect(flow.dialogOpen()).toBeFalse();
    expect(spy).toHaveBeenCalled();
    expect(spy.calls.mostRecent().args[0])
      .withContext('câu cho người dùng, KHÔNG phải `errorMessage` của job')
      .not.toContain('NullReferenceException');
  });

  it('🛑 `Failed` kèm `errorCode` ⇒ toast nói ĐÚNG chuyện, không phải câu chung (Q75)', () => {
    const spy = spyOn(toast, 'error');
    start();
    jasmine.clock().tick(POLL_MS);
    httpMock
      .expectOne('/import/job-1')
      .flush(ok({ status: 'Failed', errorCode: 'IMPORT.FILE_TOO_MANY_ROWS', errorMessage: 'dev-facing' }));

    const text = spy.calls.mostRecent().args[0];
    // Câu phải nói rõ KHÔNG dòng nào được nhập — mơ hồ ở đây khiến người dùng tin file đã nạp
    // được một phần rồi đi tìm xem phần nào.
    expect(text).toContain('quá nhiều dòng');
    expect(text).toContain('Không dòng nào được nhập');
    expect(text).not.toContain('dev-facing');
  });

  it('🛑 `Succeeded` mà thiếu `result` ⇒ coi như hỏng, KHÔNG mở V12 với bốn số 0', () => {
    const spy = spyOn(toast, 'error');
    start();
    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Succeeded' }));

    expect(flow.resultOpen()).toBeFalse();
    expect(spy).toHaveBeenCalled();
  });

  it('lỗi BƯỚC 1 (400) ⇒ câu của `businessCode` hiện trong V11, V11 vẫn mở', () => {
    flow.open();
    flow.submit(csv(), { Period: '2026-W33' }, imported);
    httpMock.expectOne('/import').flush(
      { message: 'x', businessCode: 'IMPORT.FORMAT_UNSUPPORTED', status: 'BUSINESS_ERROR' },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(flow.busy()).toBeFalse();
    expect(flow.dialogOpen()).toBeTrue();
    expect(flow.errorMessage()).toContain('không phải CSV hoặc Excel');
  });

  it('🛑 `404 IMPORT.JOB_NOT_FOUND` DỪNG poll — nếu không thì tab mở qua đêm poll vô hạn', () => {
    start();
    jasmine.clock().tick(POLL_MS);
    httpMock
      .expectOne('/import/job-1')
      .flush(
        { message: 'x', businessCode: 'IMPORT.JOB_NOT_FOUND', status: 'BUSINESS_ERROR' },
        { status: 404, statusText: 'Not Found' },
      );

    expect(flow.errorMessage()).toContain('Hãy nạp lại file');
    jasmine.clock().tick(POLL_MS * 4);
    httpMock.expectNone('/import/job-1');
  });

  it('🛑 kỳ đang chọn là THÁNG ⇒ không gửi request nào (Q37)', () => {
    flow.open();
    flow.submit(csv(), null, imported);

    httpMock.expectNone('/import');
    expect(flow.busy()).toBeFalse();
  });

  it('🛑 lượt nạp thứ hai HUỶ vòng poll của lượt trước', () => {
    start();
    jasmine.clock().tick(POLL_MS);
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Running' }));

    // Đường đi thật: đóng V11 giữa chừng rồi mở lại (job vẫn chạy ở server, `open()` dựng lại
    // trạng thái hộp thoại). Trong lúc `busy`, nút `Nhập dữ liệu` `disabled` nên không có đường
    // nào gửi lượt thứ hai từ chính hộp thoại đang chờ.
    flow.close();
    flow.open();
    flow.submit(csv(), { Period: '2026-W34' }, imported);
    httpMock.expectOne('/import').flush(ok({ jobId: 'job-2' }));

    jasmine.clock().tick(POLL_MS);
    httpMock.expectNone('/import/job-1');
    httpMock.expectOne('/import/job-2').flush(ok({ status: 'Running' }));
  });
});
