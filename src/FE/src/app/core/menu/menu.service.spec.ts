import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
import { MenuService } from './menu.service';
import { CurrentUserService } from '../../core/auth/current-user.service';
import { IApiResult } from '../../core/http/api-result.model';
import { ICurrentUser } from '../../core/auth/current-user.model';
import { ToastService } from '../toast/toast.service';
import { useTranslationsInTest } from '../i18n/i18n.testing';
import { IMenuItem, IMenuItemDto } from './menu-item.model';

function dto(partial: Partial<IMenuItemDto> & Pick<IMenuItemDto, 'id' | 'code'>): IMenuItemDto {
  return {
    parentId: null,
    label: partial.code,
    icon: null,
    route: `/${partial.code}`,
    displayOrder: 0,
    ...partial,
  };
}

function ok(data: IMenuItemDto[]): IApiResult<IMenuItemDto[]> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-menu',
    retryable: null,
    fields: null,
  };
}

function user(id: string, roles: string[] = ['User']): ICurrentUser {
  return {
    Id: id,
    UserName: `user-${id}`,
    Email: null,
    FullName: `User ${id}`,
    Roles: roles,
    MustChangePassword: false,
  };
}

/**
 * Finding B3 (doc/huong_dan/wiki-core/be/11-performance-caching.md §6.3): `MenuService` phải cache
 * `GET /meta/menu`. Test quan trọng nhất KHÔNG phải "cache có hoạt động không" mà là "cache có
 * RÒ RỈ qua phiên đăng nhập khác không" — menu lọc theo role, dùng lại bản của user cũ là lộ
 * thông tin phân quyền.
 */
describe('MenuService', () => {
  let service: MenuService;
  let currentUser: CurrentUserService;
  let httpMock: HttpTestingController;
  let toast: ToastService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
      ],
    });
    service = TestBed.inject(MenuService);
    currentUser = TestBed.inject(CurrentUserService);
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
    // Bảng dịch THẬT, không stub: nhờ vậy assert lên câu tiếng Việt bên dưới cũng là phép kiểm
    // "khoá `shared.menu.loadFailed` có mặt trong `public/i18n/vi.json`" — gõ sai khoá thì
    // `translate.instant` trả về chính chuỗi khoá và test ĐỎ. Xem `core/i18n/i18n.testing.ts`.
    await useTranslationsInTest();
  });

  afterEach(() => httpMock.verify());

  function flushMenu(data: IMenuItemDto[]): void {
    httpMock.expectOne('/meta/menu').flush(ok(data));
  }

  function collect(): IMenuItem[][] {
    const received: IMenuItem[][] = [];
    service.getMenu().subscribe((items) => received.push(items));
    return received;
  }

  it('map DTO → model và dựng cây 1 cấp theo DisplayOrder', () => {
    currentUser.setUser(user('u1'));
    const received = collect();
    flushMenu([
      dto({ id: 'c2', code: 'con-2', parentId: 'p1', displayOrder: 2 }),
      dto({ id: 'p1', code: 'cha', route: null, displayOrder: 1 }),
      dto({ id: 'c1', code: 'con-1', parentId: 'p1', displayOrder: 1 }),
    ]);

    const roots = received[0];
    expect(roots.length).toBe(1);
    expect(roots[0].Id).toBe('p1');
    expect(roots[0].Route).toBeNull();
    expect(roots[0].Children.map((c) => c.Code)).toEqual(['con-1', 'con-2']);
    expect(roots[0].Children[0].ParentId).toBe('p1');
  });

  it('chỉ gọi API 1 lần cho nhiều lần getMenu() trong cùng phiên', () => {
    currentUser.setUser(user('u1'));
    collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);

    const again = collect();
    httpMock.expectNone('/meta/menu');
    expect(again[0].map((i) => i.Code)).toEqual(['dashboard']);
  });

  it('gộp nhiều người gọi ĐỒNG THỜI vào đúng 1 request (sidebar dựng lại khi request chưa về)', () => {
    currentUser.setUser(user('u1'));
    const first = collect();
    const second = collect();

    // `expectOne` tự fail nếu có 2 request cùng URL đang chờ.
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);

    expect(first[0].map((i) => i.Code)).toEqual(['dashboard']);
    expect(second[0].map((i) => i.Code)).toEqual(['dashboard']);
  });

  it('KHÔNG trả menu của user cũ sau khi đổi phiên đăng nhập — gọi API lại', () => {
    currentUser.setUser(user('userA', ['Admin']));
    collect();
    flushMenu([dto({ id: 'm1', code: 'quan-tri' })]);
    expect(service.menu().map((i) => i.Code)).toEqual(['quan-tri']);

    // logout: `AuthService` gọi `invalidate()` + `clear()`; ngay cả khi ai đó QUÊN `invalidate()`,
    // khoá phiên đổi là đủ để cache cũ không đọc được nữa.
    currentUser.clear();
    expect(service.menu()).toEqual([]);

    currentUser.setUser(user('userB', ['User']));
    expect(service.menu()).toEqual([]);

    const received = collect();
    flushMenu([dto({ id: 'm2', code: 'bao-cao' })]);
    expect(received[0].map((i) => i.Code)).toEqual(['bao-cao']);
    expect(service.menu().map((i) => i.Code)).toEqual(['bao-cao']);
  });

  it('coi đổi ROLE của cùng một user là đổi phiên — menu lọc theo role', () => {
    currentUser.setUser(user('u1', ['User']));
    collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);

    currentUser.setUser(user('u1', ['User', 'Admin']));
    expect(service.menu()).toEqual([]);

    collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' }), dto({ id: 'm2', code: 'quan-tri' })]);
    expect(service.menu().map((i) => i.Code)).toEqual(['dashboard', 'quan-tri']);
  });

  it('thứ tự role khác nhau KHÔNG bị coi là đổi phiên (tránh gọi API thừa)', () => {
    currentUser.setUser(user('u1', ['Admin', 'User']));
    collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);

    currentUser.setUser(user('u1', ['User', 'Admin']));
    collect();
    httpMock.expectNone('/meta/menu');
    expect(service.menu().map((i) => i.Code)).toEqual(['dashboard']);
  });

  it('invalidate() xoá cache — lần gọi kế tiếp bắn request mới', () => {
    currentUser.setUser(user('u1'));
    collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);

    service.invalidate();
    expect(service.menu()).toEqual([]);

    collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);
  });

  it('refresh() tải lại ngay và đẩy kết quả mới ra signal menu()', () => {
    currentUser.setUser(user('u1'));
    collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);

    service.refresh().subscribe();
    flushMenu([dto({ id: 'm1', code: 'dashboard' }), dto({ id: 'm2', code: 'quan-tri' })]);
    expect(service.menu().map((i) => i.Code)).toEqual(['dashboard', 'quan-tri']);
  });

  /**
   * Lỗi HTTP: service KHÔNG toast — `httpErrorInterceptor` đã làm việc đó ở tầng trên (nó không
   * có mặt trong `TestBed` này, đúng ranh giới đơn vị đang kiểm). Đây là vế "hiện ra ĐÚNG MỘT
   * LẦN": bỏ điều kiện `instanceof HttpErrorResponse` trong service thì assert toast rỗng dưới
   * đây ĐỎ.
   */
  it('lỗi HTTP → [] và KHÔNG toast lần hai (interceptor đã toast), lần gọi sau vẫn thử lại thật', () => {
    currentUser.setUser(user('u1'));
    let errored = false;
    const received: IMenuItem[][] = [];
    service.getMenu().subscribe({ next: (items) => received.push(items), error: () => (errored = true) });
    httpMock.expectOne('/meta/menu').flush(null, { status: 500, statusText: 'Server Error' });
    expect(errored).toBeFalse();
    expect(received[0]).toEqual([]);
    expect(toast.toasts()).toEqual([]);
    expect(service.menu()).toEqual([]);

    const retried = collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);
    expect(retried[0].map((i) => i.Code)).toEqual(['dashboard']);
  });

  it('response phiên CŨ về MUỘN không ghi đè cache phiên mới (race, audit 2026-08-18 finding #2)', () => {
    // Phiên A phát request rồi user đổi phiên trước khi response về.
    currentUser.setUser(user('userA', ['Admin']));
    service.getMenu().subscribe({ next: () => undefined, error: () => undefined });

    currentUser.setUser(user('userB', ['User']));
    service.getMenu().subscribe({ next: () => undefined, error: () => undefined });

    const [reqA, reqB] = httpMock.match('/meta/menu');
    expect(reqA).toBeDefined();
    expect(reqB).toBeDefined();

    // Phiên B về TRƯỚC, ghi cache; phiên A về SAU (muộn).
    reqB.flush(ok([dto({ id: 'mB', code: 'menu-cua-B' })]));
    reqA.flush(ok([dto({ id: 'mA', code: 'menu-cua-A' })]));

    expect(service.menu().map((i) => i.Code)).toEqual(['menu-cua-B']);

    // Và cache của B vẫn còn nguyên giá trị — không bị response muộn xoá/thay, không sinh request
    // thừa ở lần đọc kế tiếp.
    const received = collect();
    httpMock.expectNone('/meta/menu');
    expect(received[0].map((i) => i.Code)).toEqual(['menu-cua-B']);
  });

  /**
   * Đổi 2026-09-08, cùng lượt bỏ toán tử `??` kèm mảng rỗng dựng sẵn khỏi `menu.service.ts`.
   * Test cũ tên là *"data null (envelope rỗng) →
   * menu rỗng, không ném lỗi"* và khoá đúng hành vi nay đã bỏ: nó coi envelope thiếu `data` là
   * một câu trả lời **hợp lệ**, không phân biệt được với một tài khoản không được cấp menu nào.
   *
   * Hành vi mới: envelope thiếu `data` LÀ LỖI (`unwrapData` ném), lỗi hiện ra đúng một lần bằng
   * toast, rồi service trả `[]` để sidebar vẫn dựng được. Ba assert dưới đây đo đủ ba vế đó —
   * bỏ vế toast thì test này lại xanh cho cả bản `?? []` cũ, tức không đo gì cả.
   */
  it('envelope thiếu `data` là LỖI → toast 1 lần, nơi gọi vẫn nhận [] (không lan lỗi)', () => {
    currentUser.setUser(user('u1'));
    const received: IMenuItem[][] = [];
    let errored = false;
    service.getMenu().subscribe({ next: (items) => received.push(items), error: () => (errored = true) });

    httpMock.expectOne('/meta/menu').flush({ ...ok([]), data: null });

    expect(errored).withContext('lỗi KHÔNG được lan tới nơi gọi').toBeFalse();
    expect(received[0]).withContext('nơi gọi vẫn nhận menu rỗng để dựng sidebar').toEqual([]);
    expect(toast.toasts().map((t) => t.Text))
      .withContext('lỗi phải HIỆN RA, đúng một lần')
      .toEqual(['Không tải được menu điều hướng. Tải lại trang để thử lại.']);
  });

  it('envelope hỏng KHÔNG được cache — lần gọi sau vẫn thử lại thật', () => {
    currentUser.setUser(user('u1'));
    service.getMenu().subscribe();
    httpMock.expectOne('/meta/menu').flush({ ...ok([]), data: null });
    expect(service.menu()).toEqual([]);

    const received = collect();
    flushMenu([dto({ id: 'm1', code: 'dashboard' })]);
    expect(received[0].map((i) => i.Code)).toEqual(['dashboard']);
  });
});
