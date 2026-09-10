import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Params, Router, provideRouter } from '@angular/router';
import { QuanTriNguoiDungPage } from './quan-tri-nguoi-dung.page';
import { IApiResult } from '../../../../core/http/api-result.model';
import { IPagedResultDto } from '../../../../core/http/paged-result.model';
import { ToastService } from '../../../../core/toast/toast.service';
import { IUser, IUserDto } from '../../models/quan-tri-nguoi-dung.model';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { provideCoreI18n } from '../../../../core/i18n/core-i18n';
import { APP_CORE_ROUTES, APP_I18N } from '../../../../app.config';
import { provideCoreRoutes } from '../../../../core/config/core-routes';
import { httpErrorInterceptor } from '../../../../core/interceptors/http-error.interceptor';

const DEBOUNCE_MS = 300;

function pagedOk(page = 1, pageSize = 10, totalCount = 0): IApiResult<IPagedResultDto<IUserDto>> {
  return {
    data: { items: [], page, pageSize, totalCount },
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-users',
    retryable: null,
    fields: null,
  };
}

const A_USER: IUser = {
  Id: 'u1',
  UserName: 'u1',
  Email: null,
  FullName: 'U1',
  Roles: ['User'],
  IsLocked: false,
  MustChangePassword: false,
  DateCreate: '2026-08-18',
  Version: 'stamp-u1',
};

const A_LOCKED_USER: IUser = { ...A_USER, Id: 'u2', IsLocked: true };

const A_USER_DTO: IUserDto = {
  id: 'u1',
  userName: 'u1',
  email: null,
  fullName: 'Nguyễn Văn A',
  roles: ['User'],
  isLocked: false,
  mustChangePassword: false,
  dateCreate: '2026-08-18',
  version: 'stamp-u1',
};

/** Envelope thành công MANG dữ liệu — `pagedOk()` luôn trả danh sách rỗng nên không phân biệt
 * được "trống vì lỗi" với "trống vì không khớp". */
function pagedWith(items: IUserDto[]): IApiResult<IPagedResultDto<IUserDto>> {
  return { ...pagedOk(1, 10, items.length), data: { items, page: 1, pageSize: 10, totalCount: items.length } };
}


/**
 * Route `**` (không phải `[]`): trang ghi bộ lọc lên URL bằng `router.navigate`, mà navigate tới
 * một URL không khớp cấu hình nào sẽ hỏng ở `NavigationError` — query param không bao giờ tới nơi
 * và test sẽ báo lỗi ở chỗ chẳng liên quan gì tới thứ đang kiểm.
 */
async function configure(): Promise<void> {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([{ path: '**', children: [] }]),
      provideTranslateService(),
      // Page này inject `LanguageService` để truyền `localeId` xuống grid, mà `LanguageService`
      // đòi token `CORE_I18N` — thiếu nó là NG0201 và cả suite đỏ.
      provideCoreI18n(APP_I18N),
    ],
  });
  // Bảng dịch THẬT: gõ sai một khoá trong template thì chuỗi render ra chính khoá đó, và
  // mọi assert lên câu tiếng Việt dưới đây ĐỎ. Xem core/i18n/i18n.testing.ts.
  await useTranslationsInTest();
}

/**
 * Xả mọi request bảng dịch mà `LanguageService` phát ra lúc khởi tạo — MỘT request cho mỗi cặp
 * (nguồn × ngôn ngữ) khai ở `APP_I18N`.
 *
 * **Vì sao cần**: loader i18n khai `useHttpBackend: true` để né chuỗi interceptor lúc chạy
 * thật — nhưng `provideHttpClientTesting()` thay **cả** `HttpBackend`, nên trong test các request
 * `/i18n/vi.json`, `/i18n/en.json`… rơi vào mock và nằm đó. `httpMock.verify()` ở `afterEach`
 * đếm chúng là "open request" rồi báo đỏ ở một chỗ chẳng liên quan gì tới thứ đang kiểm — đúng
 * cách nó đã hỏng thật: 31 test đỏ với thông điệp `found 2: GET /i18n/vi.json, GET /i18n/en.json`.
 *
 * 🔄 SỬA 2026-09-09 — điều kiện khớp trước đây là chuỗi khai cứng `req.url.includes('/i18n/')`.
 * Ngày `APP_I18N.resources` có nguồn thứ hai (`/i18n-app/`, nhóm khoá dự án), chuỗi đó **không**
 * khớp — `'/i18n-app/vi.json'.includes('/i18n/')` là `false` — và **34 test đỏ ngay**, ở 5 file
 * chẳng liên quan gì tới i18n. Nay danh sách tiền tố đọc thẳng từ `APP_I18N`, nên nguồn thứ ba
 * thêm vào ngày mai không làm lại chuyện đó.
 *
 * Trả `{}` chứ không trả bảng dịch thật: `useTranslationsInTest()` đã nạp bảng thật bằng `fetch`
 * (không qua HttpClient) từ trước, nên nội dung ở đây không được dùng tới — việc duy nhất cần làm
 * là đóng request lại.
 */
function drainI18nRequests(httpMock: HttpTestingController): void {
  httpMock
    .match((req) => APP_I18N.resources.some((prefix) => req.url.startsWith(prefix)))
    .forEach((req) => req.flush({}));
}

/**
 * Bug đã sửa (đợt performance 2026-08-18): mỗi phím gõ bắn một request, và request đó mang chuỗi
 * TRƯỚC debounce. Test kiểm chứng CẢ 2 vế: đúng 1 request VÀ request đó mang đúng chuỗi vừa gõ.
 *
 * Dùng đồng hồ THẬT + `detectChanges()` thủ công thay cho `fakeAsync()/tick()`: dự án đã bỏ
 * `zone.js` (zoneless, fe/13-performance.md §1) nên `fakeAsync` không dùng được, còn
 * `jasmine.clock()` không chi phối được scheduler của RxJS trong bundle test (đã thử, debounce
 * không kích). Đổi lại mỗi test chờ thật ~350ms.
 */
describe('QuanTriNguoiDungPage — debounce tìm kiếm', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await configure();
    httpMock = TestBed.inject(HttpTestingController);
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;

    // Lần nạp đầu do effect() kích khi CD chạy lần đầu — flush để không còn request treo.
    fixture.detectChanges();
    const initial = listRequests();
    expect(initial.length).toBe(1);
    initial[0].flush(pagedOk());
  });

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  /** `match()` lấy RA khỏi hàng đợi các request đang chờ — gọi 2 lần không trả lại cùng 1 request. */
  function listRequests(): TestRequest[] {
    return httpMock.match((req) => req.method === 'GET' && req.url === '/users');
  }

  function type(value: string): void {
    page.onSearchValueChange(value);
  }

  /** Chờ thật `ms` (cho `debounceTime` kịp kích + điều hướng kịp hoàn tất) rồi chạy CD. */
  async function advance(ms: number): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, ms));
    fixture.detectChanges();
  }

  it('gõ liên tục 10 ký tự → CHỈ 1 request, và mang ĐÚNG chuỗi vừa gõ', async () => {
    const text = 'nguyenvana';
    for (let i = 1; i <= text.length; i++) {
      type(text.slice(0, i));
    }

    // Vế 1 — trong lúc gõ (chưa đủ 300ms kể từ phím cuối) không được có request nào.
    await advance(DEBOUNCE_MS - 150);
    expect(listRequests().length).toBe(0);

    await advance(250);
    const requests = listRequests();
    expect(requests.length).toBe(1);

    // Vế 2 — chuỗi gửi đi phải là chuỗi CUỐI CÙNG, không phải giá trị trước debounce.
    expect(requests[0].request.params.get('searchText')).toBe(text);
    expect(requests[0].request.params.get('page')).toBe('1');
    requests[0].flush(pagedOk());
  });

  it('gõ rồi xoá về đúng giá trị cũ → không bắn request thừa (distinctUntilChanged)', async () => {
    type('a');
    await advance(DEBOUNCE_MS + 100);
    const first = listRequests();
    expect(first.length).toBe(1);
    first[0].flush(pagedOk());

    type('ab');
    type('a');
    await advance(DEBOUNCE_MS + 100);
    expect(listRequests().length).toBe(0);
  });

  it('đổi trang vẫn nạp lại NGAY (không bị debounce nuốt mất)', async () => {
    page.onGridPageChange({ Page: 3, PageSize: 10 });
    await advance(0);

    const requests = listRequests();
    expect(requests.length).toBe(1);
    expect(requests[0].request.params.get('page')).toBe('3');
    requests[0].flush(pagedOk(3));
  });

  it('khoá tài khoản xong nạp lại với ĐÚNG bộ lọc hiện tại (đường reload ngoài effect)', async () => {
    page.onGridPageChange({ Page: 2, PageSize: 10 });
    await advance(0);
    listRequests()[0].flush(pagedOk(2));

    page.onToggleLock(A_USER);
    page.onLockConfirmed();
    httpMock.expectOne('/users/u1/lock').flush({ ...pagedOk(), data: true });
    await advance(0);

    const requests = listRequests();
    expect(requests.length).toBe(1);
    expect(requests[0].request.params.get('page')).toBe('2');
    requests[0].flush(pagedOk(2));
  });
});

/**
 * Bảng lọc của `<app-toolbar>`: 2 điều kiện Vai trò + Trạng thái, có bản nháp riêng và chỉ áp khi
 * bấm "Áp dụng".
 *
 * Ba điều dễ sai nhất được chốt ở đây:
 *   1. Chọn trong bảng lọc mà CHƯA bấm "Áp dụng" thì KHÔNG được gọi API — nếu không, mỗi lần đổi
 *      ý là một request và danh sách nhảy ngay dưới tay người đang chọn dở.
 *   2. "Tất cả trạng thái" (`''`) phải thành `undefined`, KHÔNG phải `false` — rút gọn thành
 *      boolean sẽ biến "tất cả" thành "chỉ đang hoạt động", một lỗi im lặng không ai thấy.
 *   3. Áp/gỡ lọc phải về trang 1 — trang 5 của kết quả cũ thường rỗng ở kết quả mới.
 *
 * `role`/`isLocked` nay lọc THẬT: CONTRACT USER-6 đã AGREED và BE hiện thực hoá 2026-08-29
 * (doc/contracts/users.md). Test này chốt phần FE — request mang đúng tham số.
 */
describe('QuanTriNguoiDungPage — bộ lọc Vai trò/Trạng thái', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await configure();
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    await router.navigate([]);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;

    fixture.detectChanges();
    listRequests()[0].flush(pagedOk());
  });

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  function listRequests(): TestRequest[] {
    return httpMock.match((req) => req.method === 'GET' && req.url === '/users');
  }

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
  }

  function pickRole(value: string): void {
    page.onRoleDraftChange({ target: { value } } as unknown as Event);
  }

  function pickStatus(value: string): void {
    page.onStatusDraftChange({ target: { value } } as unknown as Event);
  }

  it('chọn điều kiện nhưng CHƯA bấm "Áp dụng" → không gọi API lần nào', async () => {
    pickRole('Admin');
    pickStatus('locked');
    await settle();

    expect(listRequests().length).toBe(0);
  });

  it('bấm "Áp dụng" → 1 request mang role + isLocked, và về trang 1', async () => {
    page.onGridPageChange({ Page: 4, PageSize: 10 });
    await settle();
    listRequests()[0].flush(pagedOk(4));

    pickRole('Admin');
    pickStatus('locked');
    page.onApplyFilters();
    await settle();

    const requests = listRequests();
    expect(requests.length).toBe(1);
    expect(requests[0].request.params.get('role')).toBe('Admin');
    expect(requests[0].request.params.get('isLocked')).toBe('true');
    expect(requests[0].request.params.get('page')).toBe('1');
    requests[0].flush(pagedOk());
  });

  it('"Tất cả trạng thái" KHÔNG gửi isLocked=false mà bỏ hẳn tham số', async () => {
    pickRole('User');
    pickStatus('');
    page.onApplyFilters();
    await settle();

    const requests = listRequests();
    expect(requests[0].request.params.has('isLocked')).toBeFalse();
    expect(requests[0].request.params.get('role')).toBe('User');
    requests[0].flush(pagedOk());
  });

  it('"Xoá lọc" gỡ hết tham số lọc và ẩn chip', async () => {
    pickRole('Admin');
    pickStatus('active');
    page.onApplyFilters();
    await settle();
    listRequests()[0].flush(pagedOk());
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.filter-chip').length).toBe(2);

    page.onClearFilters();
    await settle();

    const requests = listRequests();
    expect(requests[0].request.params.has('role')).toBeFalse();
    expect(requests[0].request.params.has('isLocked')).toBeFalse();
    requests[0].flush(pagedOk());
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.filter-chip').length).toBe(0);
  });

  it('gỡ chip "Vai trò" chỉ bỏ điều kiện đó, điều kiện trạng thái giữ nguyên', async () => {
    pickRole('Admin');
    pickStatus('locked');
    page.onApplyFilters();
    await settle();
    listRequests()[0].flush(pagedOk());

    page.onRemoveFilter('role');
    await settle();

    const requests = listRequests();
    expect(requests[0].request.params.has('role')).toBeFalse();
    expect(requests[0].request.params.get('isLocked')).toBe('true');
    requests[0].flush(pagedOk());
    fixture.detectChanges();

    const chips = fixture.nativeElement.querySelectorAll('.filter-chip');
    expect(chips.length).toBe(1);
    expect((chips[0] as HTMLElement).textContent).toContain('Trạng thái: Đã khoá');
  });

  it('số trên `.filter-count` bằng số điều kiện ĐANG ÁP, không tính bản nháp', async () => {
    pickRole('Admin');
    await settle();
    expect(fixture.nativeElement.querySelector('.filter-count')).toBeNull();

    page.onApplyFilters();
    await settle();
    listRequests()[0].flush(pagedOk());
    fixture.detectChanges();

    expect((fixture.nativeElement.querySelector('.filter-count') as HTMLElement).textContent?.trim()).toBe('1');
  });

  it('mở lại bảng lọc sau khi gỡ chip → bản nháp khớp điều kiện đang áp, không giữ giá trị đã gỡ', async () => {
    pickRole('Admin');
    pickStatus('locked');
    page.onApplyFilters();
    await settle();
    listRequests()[0].flush(pagedOk());

    page.onRemoveFilter('role');
    await settle();
    listRequests()[0].flush(pagedOk());

    // Bản nháp phải theo URL: mở panel ra thấy "Tất cả vai trò", không phải "Admin" vừa gỡ.
    expect((fixture.nativeElement.querySelector('#userFilterRole') as HTMLSelectElement).value).toBe('');
    expect((fixture.nativeElement.querySelector('#userFilterStatus') as HTMLSelectElement).value).toBe('locked');
  });
});

/**
 * FE-13 — bộ lọc sống trên URL (doc/huong_dan/quy-uoc/fe-routing-guard.md §8). Ba thứ hỏng khi
 * chúng chỉ nằm trong `signal()`, và cả ba đều là chuyện hằng ngày: F5 mất bộ lọc, link gửi đi mở
 * ra danh sách khác, nút Back nhảy khỏi trang thay vì lùi một bước lọc.
 */
describe('QuanTriNguoiDungPage — bộ lọc trên URL', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;
  let router: Router;

  /** Dựng trang khi URL ĐÃ mang sẵn bộ lọc — đúng cảnh F5 hoặc mở link người khác gửi. */
  async function startAt(queryParams: Params): Promise<void> {
    await configure();
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    await router.navigate([], { queryParams });
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  }

  function listRequests(): TestRequest[] {
    return httpMock.match((req) => req.method === 'GET' && req.url === '/users');
  }

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
  }

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  it('URL có sẵn bộ lọc → request ĐẦU TIÊN đã mang đúng bộ lọc đó (F5 không mất lọc)', async () => {
    await startAt({ role: 'Admin', isLocked: 'true', page: '3', pageSize: '20', searchText: 'nguyen' });

    const requests = listRequests();
    expect(requests.length).toBe(1);
    const params = requests[0].request.params;
    expect(params.get('role')).toBe('Admin');
    expect(params.get('isLocked')).toBe('true');
    expect(params.get('page')).toBe('3');
    expect(params.get('pageSize')).toBe('20');
    expect(params.get('searchText')).toBe('nguyen');
    requests[0].flush(pagedOk(3, 20));

    // Ô tìm kiếm cũng phải hiện lại chuỗi trong URL, không để trống.
    expect(page.searchInput()).toBe('nguyen');
  });

  it('áp bộ lọc → URL mang bộ lọc đó (link gửi đi mở ra đúng danh sách đang xem)', async () => {
    await startAt({});
    listRequests()[0].flush(pagedOk());

    page.onRoleDraftChange({ target: { value: 'SuperAdmin' } } as unknown as Event);
    page.onStatusDraftChange({ target: { value: 'active' } } as unknown as Event);
    page.onApplyFilters();
    await settle();
    listRequests()[0].flush(pagedOk());

    expect(router.url).toContain('role=SuperAdmin');
    expect(router.url).toContain('isLocked=false');
  });

  it('giá trị mặc định KHÔNG chiếm chỗ trên URL (trang 1, pageSize 10)', async () => {
    await startAt({ page: '5' });
    listRequests()[0].flush(pagedOk(5));

    page.onGridPageChange({ Page: 1, PageSize: 10 });
    await settle();
    listRequests()[0].flush(pagedOk());

    expect(router.url).not.toContain('page=');
    expect(router.url).not.toContain('pageSize=');
  });

  it('query param rác (page=abc) rơi về mặc định thay vì gửi NaN lên server', async () => {
    await startAt({ page: 'abc', pageSize: '-3' });

    const requests = listRequests();
    expect(requests[0].request.params.get('page')).toBe('1');
    expect(requests[0].request.params.get('pageSize')).toBe('10');
    requests[0].flush(pagedOk());
  });

  it('URL đổi từ bên ngoài (nút Back) → danh sách nạp lại theo URL mới', async () => {
    await startAt({ role: 'Admin' });
    listRequests()[0].flush(pagedOk());

    await router.navigate([], { queryParams: { role: 'User' } });
    await settle();

    const requests = listRequests();
    expect(requests.length).toBe(1);
    expect(requests[0].request.params.get('role')).toBe('User');
    requests[0].flush(pagedOk());
  });
});

/**
 * FE-4 — khoá tài khoản phải hỏi lại, và câu hỏi phải nói ĐÚNG hệ quả.
 *
 * Nội dung không phải chuyện thẩm mỹ: doc/contracts/users.md §"Khoá KHÔNG có hiệu lực tức thì"
 * ghi rõ phiên đang chạy của người bị khoá còn sống tối đa ~30 phút (cookie session +
 * `SecurityStampValidator`). Nói "đã khoá xong" trơn tru là để quản trị viên tin đã chặn được
 * ngay, trong khi người kia vẫn đang thao tác.
 */
describe('QuanTriNguoiDungPage — xác nhận trước khi khoá', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;
  let toast: ToastService;

  beforeEach(async () => {
    await configure();
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
    httpMock.match((req) => req.url === '/users')[0].flush(pagedOk());
  });

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  function confirmDialogEl(): HTMLDialogElement {
    return fixture.nativeElement.querySelector('dialog.confirm-dialog') as HTMLDialogElement;
  }

  it('bấm Khoá KHÔNG gọi API ngay — mở hộp thoại hỏi lại trước', () => {
    page.onToggleLock(A_USER);
    fixture.detectChanges();

    httpMock.expectNone('/users/u1/lock');
    expect(confirmDialogEl().open).toBeTrue();
  });

  it('nội dung hộp thoại nói rõ độ trễ 30 phút, không hứa "đã đăng xuất ngay"', () => {
    page.onToggleLock(A_USER);
    fixture.detectChanges();

    const text = confirmDialogEl().textContent ?? '';
    expect(text).toContain('30 phút');
    expect(text).toContain('KHÔNG phải ngay lập tức');
    expect(text).toContain(A_USER.FullName);
  });

  it('Huỷ → không gọi API, không đổi gì', () => {
    page.onToggleLock(A_USER);
    fixture.detectChanges();
    page.onLockCancelled();

    httpMock.expectNone('/users/u1/lock');
  });

  it('Đồng ý → gọi lock, và toast cũng nói đúng độ trễ 30 phút', async () => {
    page.onToggleLock(A_USER);
    fixture.detectChanges();
    page.onLockConfirmed();

    httpMock.expectOne('/users/u1/lock').flush({ ...pagedOk(), data: true });
    await new Promise((resolve) => setTimeout(resolve, 0));
    httpMock.match((req) => req.url === '/users')[0].flush(pagedOk());

    const message = toast.toasts().at(-1)?.Text ?? '';
    expect(message).toContain('Đã khoá tài khoản');
    expect(message).toContain('30 phút');
  });

  it('MỞ KHOÁ không hỏi lại — đó là chiều khôi phục, chặn thêm một bước chỉ làm chậm sửa sai', () => {
    page.onToggleLock(A_LOCKED_USER);
    fixture.detectChanges();

    expect(confirmDialogEl().open).toBeFalse();
    httpMock.expectOne('/users/u2/unlock').flush({ ...pagedOk(), data: true });
    httpMock.match((req) => req.url === '/users')[0].flush(pagedOk());

    expect(toast.toasts().at(-1)?.Text).toBe('Đã mở khoá tài khoản.');
  });
});

/**
 * FE-3 + FE-6 — trang dùng `<app-toolbar>` dùng chung (không chép markup) và vùng đếm kết quả là
 * vùng SỐNG cho trình đọc màn hình.
 */
describe('QuanTriNguoiDungPage — thanh công cụ dùng chung & vùng đếm kết quả', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await configure();
    httpMock = TestBed.inject(HttpTestingController);
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    fixture.detectChanges();
    httpMock.match((req) => req.url === '/users')[0].flush(pagedOk(1, 10, 42));
    fixture.detectChanges();
  });

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('thanh công cụ là component dùng chung, và trang không dựng thêm `<details class="filter">` nào khác', () => {
    const toolbar = host().querySelector('app-toolbar');
    expect(toolbar).not.toBeNull();
    expect(toolbar?.classList).toContain('toolbar');

    // Đúng MỘT bảng lọc, và nó nằm bên trong component dùng chung — không còn bản chép của trang.
    const panels = host().querySelectorAll('details.filter');
    expect(panels.length).toBe(1);
    expect(toolbar?.contains(panels[0])).toBeTrue();
  });

  it('ô lọc và nút "+ Thêm người dùng" được project vào đúng khe của thanh công cụ', () => {
    expect(host().querySelector('.filter-panel #userFilterRole')).not.toBeNull();
    expect(host().querySelector('.filter-panel #userFilterStatus')).not.toBeNull();
    expect((host().querySelector('.toolbar-actions .btn.primary') as HTMLElement).textContent?.trim()).toBe(
      '+ Thêm người dùng',
    );
  });

  it('vùng đếm kết quả là aria-live — lọc xong người dùng bàn phím biết còn bao nhiêu dòng', () => {
    const counter = host().querySelector('.title .muted') as HTMLElement;
    expect(counter.getAttribute('aria-live')).toBe('polite');
    expect(counter.textContent?.trim()).toBe('42 người dùng');
  });
});

/**
 * Thao tác khoá/mở khoá HỎNG không được hiện toast thành công (doc/contracts/users.md §"Thao tác
 * hỏng nay là LỖI", finding BE-4 — 2026-08-29).
 *
 * Đường `lock` đổi con dấu bảo mật TRƯỚC khi đặt lockout, nên hỏng giữa chừng để lại trạng thái
 * nửa vời: nạn nhân bị chấm dứt phiên nhưng vẫn đăng nhập lại được. Một câu "Đã khoá tài khoản."
 * cho ca đó là lời nói dối mà không test nào khác bắt được — nó không gây lỗi, không đổi màu gì.
 */
describe('QuanTriNguoiDungPage — khoá/mở khoá thất bại', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;
  let toast: ToastService;

  beforeEach(async () => {
    await configure();
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
    httpMock.match((req) => req.url === '/users')[0].flush(pagedOk());
  });

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  it('422 USER.LOCK_FAILED → KHÔNG có toast thành công (interceptor lo phần hiện message)', () => {
    page.onToggleLock(A_USER);
    fixture.detectChanges();
    page.onLockConfirmed();

    httpMock.expectOne('/users/u1/lock').flush(
      {
        data: null,
        message: 'Khoá tài khoản không thành công.',
        status: 'BUSINESS_ERROR',
        code: 'BusinessRuleError',
        businessCode: 'USER.LOCK_FAILED',
        traceId: 'trace-lock',
        retryable: null,
        fields: null,
      },
      { status: 422, statusText: 'Unprocessable Content' },
    );

    expect(toast.toasts().some((message) => message.Severity === 'success')).toBeFalse();
  });

  it('200 kèm `data: false` (BE chưa cập nhật) vẫn KHÔNG được coi là đã khoá', () => {
    page.onToggleLock(A_USER);
    fixture.detectChanges();
    page.onLockConfirmed();

    // Đúng shape mà bản BE trước 2026-08-29 trả về khi tầng ghi Identity hỏng.
    httpMock.expectOne('/users/u1/lock').flush({ ...pagedOk(), data: false });
    httpMock.match((req) => req.url === '/users')[0].flush(pagedOk());

    const last = toast.toasts().at(-1);
    expect(last?.Severity).toBe('error');
    expect(last?.Text).toContain('Chưa khoá được tài khoản');
    expect(toast.toasts().some((message) => message.Severity === 'success')).toBeFalse();
  });

  it('mở khoá trả `data: false` → báo lỗi, không báo "Đã mở khoá"', () => {
    page.onToggleLock(A_LOCKED_USER);
    fixture.detectChanges();

    httpMock.expectOne('/users/u2/unlock').flush({ ...pagedOk(), data: false });
    httpMock.match((req) => req.url === '/users')[0].flush(pagedOk());

    expect(toast.toasts().at(-1)?.Severity).toBe('error');
  });
});

/**
 * FE — huỷ request cũ (fe/02-http-envelope.md, chốt 2026-08-31) và BA trạng thái của lưới
 * (fe/11-grid-and-metadata.md §"Ba trạng thái của lưới", chốt 2026-08-31).
 *
 * Hai lỗi được chốt ở đây đều KHÔNG gây lỗi, không đổi màu gì, và không test nào khác bắt được —
 * chúng chỉ khiến bảng hiển thị dữ liệu SAI trong khi trông hoàn toàn bình thường:
 *   1. kết quả của bộ lọc CŨ về sau đè lên kết quả của bộ lọc MỚI;
 *   2. tải hỏng nhưng bảng vẫn giữ dữ liệu của bộ lọc trước.
 */
describe('QuanTriNguoiDungPage — huỷ request cũ & ba trạng thái của lưới', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await configure();
    httpMock = TestBed.inject(HttpTestingController);
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
    listRequests()[0].flush(pagedWith([A_USER_DTO]));
    fixture.detectChanges();
  });

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  function listRequests(): TestRequest[] {
    return httpMock.match((req) => req.method === 'GET' && req.url === '/users');
  }

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function advance(ms: number): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, ms));
    fixture.detectChanges();
  }

  it('request cũ chưa về mà đã có request mới → request cũ BỊ HUỶ (switchMap)', async () => {
    page.onSearchValueChange('ngu');
    await advance(DEBOUNCE_MS + 100);
    const first = listRequests();
    expect(first.length).toBe(1);
    expect(first[0].request.params.get('searchText')).toBe('ngu');

    // Cố ý KHÔNG flush request đầu: đúng cảnh mạng giật một nhịp.
    page.onSearchValueChange('nguyen');
    await advance(DEBOUNCE_MS + 100);
    const second = listRequests();
    expect(second.length).toBe(1);
    expect(second[0].request.params.get('searchText')).toBe('nguyen');

    // Đây là toàn bộ điểm khác biệt so với `.subscribe()` trần: request cũ không còn đường về để
    // ghi đè bảng bằng kết quả của câu hỏi khác.
    expect(first[0].cancelled).withContext('request của chuỗi cũ phải bị huỷ').toBeTrue();

    second[0].flush(pagedWith([A_USER_DTO]));
  });

  it('tải hỏng → XOÁ BẢNG, hiện khối lỗi kèm nút thử lại', async () => {
    expect(host().querySelector('app-user-grid-table')).not.toBeNull();
    expect(host().textContent).toContain('Nguyễn Văn A');

    page.onRoleDraftChange({ target: { value: 'Admin' } } as unknown as Event);
    page.onApplyFilters();
    await advance(0);
    listRequests()[0].flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    // Giữ lại bảng cũ là ca nguy hiểm: nó trông y như kết quả của bộ lọc VỪA chọn.
    expect(host().querySelector('app-user-grid-table')).withContext('bảng phải biến mất').toBeNull();
    expect(host().textContent).not.toContain('Nguyễn Văn A');

    const notice = host().querySelector('.notice.bad') as HTMLElement;
    expect(notice).not.toBeNull();
    expect(notice.getAttribute('role')).toBe('alert');
    expect(notice.querySelector('.btn')?.textContent).toContain('Thử lại');
  });

  it('bấm "Thử lại" → gọi lại ĐÚNG bộ lọc đang chọn, dữ liệu về thì bảng trở lại', async () => {
    page.onRoleDraftChange({ target: { value: 'Admin' } } as unknown as Event);
    page.onApplyFilters();
    await advance(0);
    listRequests()[0].flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    (host().querySelector('.notice.bad .btn') as HTMLButtonElement).click();
    await advance(0);

    const retry = listRequests();
    expect(retry.length).toBe(1);
    expect(retry[0].request.params.get('role')).withContext('thử lại phải giữ bộ lọc đang chọn').toBe('Admin');
    retry[0].flush(pagedWith([A_USER_DTO]));
    fixture.detectChanges();

    expect(host().querySelector('.notice.bad')).toBeNull();
    expect(host().querySelector('app-user-grid-table')).not.toBeNull();
  });

  it('"không có kết quả" trông KHÁC HẲN khối lỗi — bảng vẫn còn, không có dải đỏ', async () => {
    page.onRoleDraftChange({ target: { value: 'Admin' } } as unknown as Event);
    page.onApplyFilters();
    await advance(0);
    listRequests()[0].flush(pagedWith([]));
    fixture.detectChanges();

    expect(host().querySelector('.notice.bad')).withContext('lọc không khớp ai KHÔNG phải lỗi hệ thống').toBeNull();
    expect(host().querySelector('app-user-grid-table')).not.toBeNull();
    expect(host().textContent).toContain('Không có người dùng nào khớp bộ lọc.');
  });
});

/**
 * Chống ghi đè khi hai admin sửa cùng một người — nối 2026-09-08, hoàn tất quyết định 3 của
 * doc/contracts/users.md §"Quyết định người dùng 2026-08-30".
 *
 * 🛑 Vì sao phải kiểm ở TẦNG TRANG chứ không chỉ ở service: lớp bảo vệ này gãy im lặng ở đúng
 * mắt xích giữa. `QuanTriNguoiDungService.update()` có thể gửi `version` hoàn hảo, nhưng nếu
 * trang truyền xuống `Version: null` (quên ghép từ `formEditing`, hoặc tra nhầm từ danh sách
 * hiện tại) thì BE bỏ qua bước kiểm — và KHÔNG có gì đỏ: không lỗi biên dịch, không lỗi lint,
 * `update()` vẫn trả `true`, người dùng vẫn thấy "Đã cập nhật". Test service ở
 * `services/quan-tri-nguoi-dung.service.spec.ts` không với tới được chỗ này.
 */
describe('QuanTriNguoiDungPage — token chống ghi đè đi tới tận request', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;

  /**
   * TestBed RIÊNG — có `httpErrorInterceptor` thật, khác mọi describe khác trong file.
   *
   * Bắt buộc, không phải cho chắc: trang đọc câu lỗi từ `err.apiResult`, mà **chỉ interceptor
   * gắn field đó** vào error khi rethrow. Thiếu nó thì `apiResult` là `undefined` và
   * `applyFormError` lùi về câu dự phòng của FE — test sẽ xanh/đỏ vì hạ tầng test, chứ không
   * vì hành vi 409. Đã dính đúng vậy lúc viết: assert nhận được *"Không cập nhật được người
   * dùng — thử lại sau."* thay vì câu của BE.
   *
   * `provideCoreRoutes` đi kèm vì interceptor inject `CORE_ROUTES` (đường dẫn màn đăng nhập cho
   * nhánh 401) — thiếu là NG0201 ngay request đầu.
   */
  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        provideTranslateService(),
        provideCoreI18n(APP_I18N),
        provideCoreRoutes(APP_CORE_ROUTES),
      ],
    });
    await useTranslationsInTest();

    httpMock = TestBed.inject(HttpTestingController);
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
    httpMock.match((req) => req.method === 'GET' && req.url === '/users')[0].flush(pagedWith([A_USER_DTO]));
    fixture.detectChanges();
  });

  afterEach(() => {
    drainI18nRequests(httpMock);
    httpMock.verify();
  });

  /** Mở form sửa trên `user` rồi bấm Lưu với đúng dữ liệu form phát ra (form KHÔNG biết Version). */
  function editAndSave(user: IUser): TestRequest {
    page.openEditForm(user);
    page.onFormSaved({ IsEditing: true, Update: { Email: 'moi@example.com', FullName: 'Tên Mới', Roles: ['User'] } });
    return httpMock.expectOne((req) => req.method === 'PUT' && req.url === `/users/${user.Id}`);
  }

  it('PUT mang đúng `version` của BẢN GHI đang mở form', () => {
    const req = editAndSave(A_USER);

    expect((req.request.body as Record<string, unknown>)['version'])
      .withContext('Version phải lấy từ `formEditing` — bản ghi mà người này ĐÃ NHÌN THẤY lúc bấm Sửa.')
      .toBe('stamp-u1');
    req.flush({ ...pagedOk(), data: true });
    httpMock.match((req2) => req2.method === 'GET' && req2.url === '/users').forEach((r) => r.flush(pagedWith([])));
  });

  /**
   * Bản ghi cũ do BE chưa cấp token: `Version: null` phải đi ra **nguyên vẹn** chứ không bị bỏ
   * khoá. Ca này khoá đúng ranh giới "null là giá trị hợp lệ", không phải "chưa điền".
   */
  it('bản ghi không có token ⇒ vẫn gửi khoá `version` với giá trị null', () => {
    const req = editAndSave({ ...A_USER, Version: null });

    const body = req.request.body as Record<string, unknown>;
    expect('version' in body).toBeTrue();
    expect(body['version']).toBeNull();
    req.flush({ ...pagedOk(), data: true });
    httpMock.match((req2) => req2.method === 'GET' && req2.url === '/users').forEach((r) => r.flush(pagedWith([])));
  });

  /**
   * 409 `USER.VERSION_CONFLICT` đi CHUNG đường với mọi lỗi form khác — hợp đồng chỉ chốt "lệch ⇒
   * 409", không mô tả hành vi màn hình nào thêm. Test khoá đúng hai điều đó: form KHÔNG đóng, và
   * câu người dùng đọc là `message` của BE chứ không phải câu dự phòng của FE.
   */
  it('409 VERSION_CONFLICT: form KHÔNG đóng, hiện đúng `message` của BE', () => {
    const req = editAndSave(A_USER);

    req.flush(
      {
        data: null,
        message: 'Người dùng này vừa được người khác cập nhật.',
        status: 'ERROR',
        code: 'Conflict',
        businessCode: 'USER.VERSION_CONFLICT',
        traceId: 'trace-users',
        retryable: null,
        fields: null,
      },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    // Đọc qua DOM chứ không qua signal: `formOpen`/`formServerError` là `protected`, và quan
    // trọng hơn — thứ cần khoá là điều NGƯỜI DÙNG THẤY, không phải giá trị nội bộ.
    const host = fixture.nativeElement as HTMLElement;
    const dialog = host.querySelector('dialog.form-dialog') as HTMLDialogElement;

    expect(dialog.open).withContext('đóng form là mất trắng thứ người dùng vừa gõ').toBeTrue();
    expect(dialog.querySelector('.form-error')?.textContent?.trim())
      .withContext('phải là câu của BE, không phải câu dự phòng "Cập nhật người dùng thất bại" của FE')
      .toBe('Người dùng này vừa được người khác cập nhật.');
  });
});
