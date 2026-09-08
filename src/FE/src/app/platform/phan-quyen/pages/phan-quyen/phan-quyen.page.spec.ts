import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Observable } from 'rxjs';
import { PhanQuyenPage, isVersionConflict } from './phan-quyen.page';
import { HttpErrorResponse } from '@angular/common/http';
import { IApiResult, IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-page',
    retryable: null,
    fields: null,
  };
}

/**
 * `version` trong hai fixture này KHÔNG phải chi tiết trang trí: BE so khớp chính xác token đó ở
 * mỗi `PUT` (`PermissionErrors.VersionConflict`), và nó phải đi vòng GET → state → PUT. Bỏ nó khỏi
 * fixture là dựng lại đúng khoảng mù cũ — trang không gửi `version`, mọi lần lưu 409, mà test vẫn
 * xanh vì `HttpTestingController` không kiểm tra thứ BE đòi.
 */
const MENU_MATRIX = {
  roles: ['SuperAdmin', 'Admin'],
  rows: [{ sysMenuId: 'm1', sysMenuCode: 'a', sysMenuName: 'Màn A', assignedRoles: ['SuperAdmin'] }],
  version: 'menu-v1',
};

const RESOURCE_MATRIX = {
  roles: ['SuperAdmin', 'Admin', 'User'],
  rows: [{ resourceKey: 'import.manage', resourceName: 'Import CSV/Excel', assignedRoles: ['Admin'] }],
  version: 'res-v1',
};

/** Envelope lỗi 409 BE trả khi có người vừa ghi trước — chép theo `PermissionErrors.VersionConflict`. */
const VERSION_CONFLICT_ENVELOPE: IApiResult<null> = {
  data: null,
  message:
    'Ma trận phân quyền đã được người khác thay đổi kể từ lúc bạn mở màn hình. ' +
    'Hãy tải lại trang rồi thực hiện lại thay đổi — thay đổi vừa rồi CHƯA được lưu.',
  status: 'BUSINESS_ERROR',
  code: 'Conflict',
  businessCode: 'PERMISSION.VERSION_CONFLICT',
  traceId: 'trace-conflict',
  retryable: false,
  fields: null,
};

/**
 * Chốt việc NỐI ma trận PERM-2 vào trang (trước 2026-08-29 component `ResourcePermissionMatrix`
 * tồn tại nhưng không route nào render nó — code chết). Test này đỏ ngay nếu ai đó gỡ tab thứ hai
 * hoặc đấu nhầm nó vào endpoint của PERM-1.
 */
describe('PhanQuyenPage — 2 ma trận độc lập', () => {
  let fixture: ComponentFixture<PhanQuyenPage>;
  let httpMock: HttpTestingController;

  function segmentButtons(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.seg-btn'));
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PhanQuyenPage);
    fixture.detectChanges();
    httpMock.expectOne('/admin/permissions').flush(ok(MENU_MATRIX));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('mở trang chỉ gọi PERM-1 — ma trận tài nguyên tải LƯỜI, chưa đụng tới', () => {
    httpMock.expectNone('/admin/permissions/resources');
    expect(segmentButtons().length).withContext('phải có đúng 2 chế độ xem').toBe(2);
    expect(fixture.nativeElement.querySelector('app-permission-matrix')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-resource-permission-matrix')).toBeNull();
  });

  it('chuyển sang tab "Theo tài nguyên": gọi GET /resources và render ma trận PERM-2', () => {
    segmentButtons()[1].click();
    fixture.detectChanges();

    httpMock.expectOne('/admin/permissions/resources').flush(ok(RESOURCE_MATRIX));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-resource-permission-matrix')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-permission-matrix')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Import CSV/Excel');
  });

  it('quay đi quay lại KHÔNG tải lại — chỉ đúng 1 lần GET /resources', () => {
    segmentButtons()[1].click();
    fixture.detectChanges();
    httpMock.expectOne('/admin/permissions/resources').flush(ok(RESOURCE_MATRIX));
    fixture.detectChanges();

    segmentButtons()[0].click();
    fixture.detectChanges();
    segmentButtons()[1].click();
    fixture.detectChanges();

    httpMock.expectNone('/admin/permissions/resources');
  });

  it('PUT trả 409 → hiện dải cảnh báo có lối đi tiếp, KHÔNG chỉ dựa vào toast của interceptor', () => {
    // Tick một ô để nút Lưu mở khoá (`!dirty()` giữ nó tắt khi chưa đổi gì).
    const cells = fixture.nativeElement.querySelectorAll(
      'app-permission-matrix tbody input[type="checkbox"]',
    ) as NodeListOf<HTMLInputElement>;
    cells[1].dispatchEvent(new Event('change'));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.btn.primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock
      .expectOne('/admin/permissions')
      .flush(VERSION_CONFLICT_ENVELOPE, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    // Dải cảnh báo phải Ở LẠI trên màn hình. Toast của interceptor biến mất sau vài giây và không
    // mang theo lối đi tiếp nào — người dùng sẽ không biết thay đổi của mình CHƯA được lưu.
    const banner = fixture.nativeElement.querySelector('.notice.warn, .notice.bad') as HTMLElement;
    expect(banner).withContext('409 phải để lại dải cảnh báo trên màn hình').toBeTruthy();
    expect(banner.textContent).toContain('tải lại');

    // KHÔNG gửi lại lần nữa bằng token cũ — gửi lại chắc chắn 409 tiếp.
    httpMock.expectNone('/admin/permissions');
  });

  it('bộ nhận diện 409 chấp nhận CẢ businessCode LẪN mã HTTP trần', () => {
    // Hai đường vào khác nhau, cùng một kết luận. Chỉ nhận businessCode thì mất ca proxy/gateway
    // trả 409 mà không kèm envelope; chỉ nhận status thì mất ca envelope tới qua mã khác.
    expect(
      isVersionConflict({
        status: 409,
        apiResult: undefined,
      } as unknown as HttpErrorResponse & IHttpErrorWithApiResult),
    ).toBeTrue();

    expect(
      isVersionConflict({
        status: 422,
        apiResult: VERSION_CONFLICT_ENVELOPE,
      } as unknown as HttpErrorResponse & IHttpErrorWithApiResult),
    ).toBeTrue();

    // Đối chứng: lỗi khác KHÔNG được nhận nhầm thành xung đột phiên bản.
    expect(
      isVersionConflict({
        status: 500,
        apiResult: undefined,
      } as unknown as HttpErrorResponse & IHttpErrorWithApiResult),
    ).toBeFalse();
  });

  it('lưu ở tab tài nguyên chỉ PUT /resources — KHÔNG đụng bảng SysMenuRole của PERM-1', () => {
    segmentButtons()[1].click();
    fixture.detectChanges();
    httpMock.expectOne('/admin/permissions/resources').flush(ok(RESOURCE_MATRIX));
    fixture.detectChanges();

    // Bỏ tick "Admin" (cột thứ 2 — cột SuperAdmin bị khoá theo break-glass).
    const cells = fixture.nativeElement.querySelectorAll(
      'app-resource-permission-matrix tbody input[type="checkbox"]',
    ) as NodeListOf<HTMLInputElement>;
    expect(cells[1].checked).toBeTrue();
    cells[1].dispatchEvent(new Event('change'));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.btn.primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    const req = httpMock.expectOne('/admin/permissions/resources');
    expect(req.request.method).toBe('PUT');
    // `version` đi kèm mọi PUT — thiếu nó là 409, xem `phan-quyen.model.ts`.
    expect(req.request.body).toEqual({
      entries: [{ resourceKey: 'import.manage', roles: [] }],
      version: 'res-v1',
    });
    req.flush(ok(true));
    fixture.detectChanges();

    // Lưu xong thì trang GET lại ĐÚNG endpoint đó để lấy `version` mới — token là hàm băm của dữ
    // liệu, nên sau khi ghi nó đã đổi và bản cũ trong tay không dùng được cho lần lưu kế tiếp.
    const refresh = httpMock.expectOne('/admin/permissions/resources');
    expect(refresh.request.method).toBe('GET');
    refresh.flush(ok(RESOURCE_MATRIX));

    // Không có PUT nào sang endpoint PERM-1, và cũng không gọi lại menu (RolePermission không
    // quyết định menu nào hiện ra).
    httpMock.expectNone('/admin/permissions');
    httpMock.expectNone('/meta/menu');
  });

  it('nút Lưu của tab tài nguyên khoá khi chưa đổi gì — `entries: []` là THU HỒI SẠCH, không phải no-op', () => {
    segmentButtons()[1].click();
    fixture.detectChanges();
    // BE trả ma trận RỖNG (chưa khai `ResourceKey` nào) — đúng ca nguy hiểm: bấm Lưu lúc này sẽ
    // gửi `entries: []` và xoá sạch mọi dòng `RolePermission` còn tồn trong DB.
    httpMock.expectOne('/admin/permissions/resources').flush(ok({ roles: ['SuperAdmin'], rows: [] }));
    fixture.detectChanges();

    const save = fixture.nativeElement.querySelector('.btn.primary') as HTMLButtonElement;
    expect(save.disabled).withContext('chưa tick gì thì không được lưu').toBeTrue();

    save.click();
    fixture.detectChanges();
    httpMock.expectNone('/admin/permissions/resources');
  });

  it('nói đúng sự thật: có ghi chú các quyền này chưa chặn/mở thao tác nào', () => {
    segmentButtons()[1].click();
    fixture.detectChanges();
    httpMock.expectOne('/admin/permissions/resources').flush(ok(RESOURCE_MATRIX));
    fixture.detectChanges();

    // BE hiện chỉ còn `ResourceKeys.All = [import.manage]` và KHÔNG endpoint nào mang
    // `[RequirePermission]`. Bỏ ghi chú này đi là để màn hình ngụ ý sai rằng nó đang điều khiển
    // quyền thật — gỡ test cùng lúc với việc gắn `[RequirePermission]` đầu tiên ở BE.
    const notice = fixture.nativeElement.querySelector('.notice') as HTMLElement | null;
    expect(notice).not.toBeNull();
    expect(notice?.textContent).toContain('chưa chặn');
  });
});

/**
 * FE — chặn mất thay đổi chưa lưu khi rời trang (fe/09-forms-validation.md §"Form dirty + điều
 * hướng đi", chốt 2026-08-31) và khoá nút Lưu của PERM-1 khi chưa có gì để lưu.
 *
 * Hai việc này cùng bảo vệ một chỗ: ma trận sống trong bộ nhớ cho tới lúc bấm Lưu, và cả hai
 * endpoint đều GHI ĐÈ TOÀN BỘ. Rời trang im lặng = mất trắng; bấm Lưu lúc bảng rỗng vì `GET` hỏng
 * = ghi một ma trận rỗng đè lên ma trận thật.
 */
describe('PhanQuyenPage — chặn rời trang khi còn thay đổi chưa lưu', () => {
  let fixture: ComponentFixture<PhanQuyenPage>;
  let page: PhanQuyenPage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PhanQuyenPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
    httpMock.expectOne('/admin/permissions').flush(ok(MENU_MATRIX));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function leaveDialog(): HTMLDialogElement {
    return fixture.nativeElement.querySelector('dialog.confirm-dialog') as HTMLDialogElement;
  }

  function saveButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('.title .btn.primary') as HTMLButtonElement;
  }

  /** `<dialog>.close()` phát `close` ở một task sau — phải nghe chính sự kiện đó, không chờ bằng
   * bộ đếm thời gian (xem confirm-dialog.spec.ts). */
  function closeEvent(): Promise<void> {
    return new Promise((resolve) => leaveDialog().addEventListener('close', () => resolve(), { once: true }));
  }

  function tickOneCell(): void {
    page.onToggle({ SysMenuId: 'm1', Role: 'Admin' });
    fixture.detectChanges();
  }

  /**
   * Phép thử số 4 của bảng nghiệm thu, quan trọng ngang ba phép kia: một guard hỏi cả khi không
   * có gì để mất sẽ bị bấm qua theo phản xạ, và lúc đó nó ngừng bảo vệ được bất cứ thứ gì.
   */
  it('KHÔNG sửa gì → đi THẲNG, không hỏi', () => {
    expect(page.canDeactivate()).toBeTrue();
    expect(leaveDialog().open).withContext('không có gì để mất thì không được hỏi').toBeFalse();
  });

  it('có thay đổi chưa lưu → hiện hộp thoại xác nhận, chưa cho đi ngay', () => {
    tickOneCell();

    let answered: boolean | null = null;
    const result = page.canDeactivate();
    expect(typeof result).withContext('phải trả Observable để chờ câu trả lời').not.toBe('boolean');
    (result as Observable<boolean>).subscribe((value) => (answered = value));
    fixture.detectChanges();

    expect(leaveDialog().open).toBeTrue();
    expect(answered).withContext('chưa trả lời thì guard chưa được quyết').toBeNull();
  });

  it('chọn "Ở lại" → guard trả false, và ô vừa tick CÒN NGUYÊN', async () => {
    tickOneCell();

    let answered: boolean | null = null;
    (page.canDeactivate() as Observable<boolean>).subscribe((value) => (answered = value));
    fixture.detectChanges();

    const closed = closeEvent();
    (leaveDialog().querySelector('.dialog-actions .btn:not(.danger)') as HTMLButtonElement).click();
    await closed;
    fixture.detectChanges();

    expect(answered).toBeFalse();
    // Ở lại mà mất thay đổi thì hộp thoại vô nghĩa — người dùng chọn "ở lại" chính vì muốn giữ nó.
    const checked = fixture.nativeElement.querySelectorAll(
      'app-permission-matrix tbody input[type="checkbox"]:checked',
    ) as NodeListOf<HTMLInputElement>;
    expect(checked.length).toBe(2);
  });

  it('chọn "Rời khỏi trang" → guard trả true', async () => {
    tickOneCell();

    let answered: boolean | null = null;
    (page.canDeactivate() as Observable<boolean>).subscribe((value) => (answered = value));
    fixture.detectChanges();

    const closed = closeEvent();
    (leaveDialog().querySelector('.dialog-actions .btn.danger') as HTMLButtonElement).click();
    await closed;
    fixture.detectChanges();

    expect(answered).toBeTrue();
  });

  it('lớp 2 — beforeunload chỉ chặn khi CÓ thay đổi chưa lưu (đóng tab/F5 Router không thấy)', () => {
    const clean = new Event('beforeunload', { cancelable: true });
    page.onBeforeUnload(clean as BeforeUnloadEvent);
    expect(clean.defaultPrevented).withContext('chưa sửa gì mà vẫn cảnh báo là làm phiền').toBeFalse();

    tickOneCell();
    const dirty = new Event('beforeunload', { cancelable: true });
    page.onBeforeUnload(dirty as BeforeUnloadEvent);
    expect(dirty.defaultPrevented).toBeTrue();
  });

  /**
   * PERM-1 trước 2026-08-31 chỉ khoá theo `saving() || loading()`, thiếu `!dirty()`. Chuỗi dẫn tới
   * xoá sạch ma trận: `GET` hỏng ⇒ `rows` rỗng ⇒ nút vẫn bật ⇒ bấm ⇒ `PUT` một mảng rỗng, mà
   * endpoint này GHI ĐÈ TOÀN BỘ.
   */
  it('nút Lưu của PERM-1 khoá khi chưa đổi gì, mở khi đã tick', () => {
    expect(saveButton().disabled).withContext('chưa tick gì thì không được lưu').toBeTrue();
    saveButton().click();
    fixture.detectChanges();
    httpMock.expectNone('/admin/permissions');

    tickOneCell();
    expect(saveButton().disabled).toBeFalse();
  });
});
