import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { QuanTriNguoiDungPage } from './quan-tri-nguoi-dung.page';
import { IApiResult } from '../../../../core/http/api-result.model';
import { IPagedResultDto } from '../../../../core/http/paged-result.model';
import { IUser, IUserDto } from '../../models/quan-tri-nguoi-dung.model';

const DEBOUNCE_MS = 300;

function pagedOk(page = 1, pageSize = 10): IApiResult<IPagedResultDto<IUserDto>> {
  return {
    data: { items: [], page, pageSize, totalCount: 0 },
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
};

/**
 * Bug đã sửa (đợt performance 2026-08-18): `onSearchInputEvent` gọi thẳng `loadList()` mỗi phím
 * gõ, mà `requestParams()` lại đọc `debouncedSearch()` → vừa bắn 1 request/phím (sai
 * doc/huong_dan/wiki-core/fe/13-performance.md §6) vừa GỬI SAI chuỗi tìm kiếm (giá trị trước
 * debounce). Test kiểm chứng CẢ 2 vế: đúng 1 request VÀ request đó mang đúng chuỗi vừa gõ.
 *
 * Dùng đồng hồ THẬT + `detectChanges()` thủ công thay cho `fakeAsync()/tick()`: dự án đã bỏ
 * `zone.js` (zoneless, fe/13-performance.md §1) nên `fakeAsync` không dùng được, còn
 * `jasmine.clock()` không chi phối được scheduler của RxJS trong bundle test (đã thử, debounce
 * không kích). Đổi lại mỗi test chờ thật ~350ms — chấp nhận được cho 4 test.
 */
describe('QuanTriNguoiDungPage — debounce tìm kiếm', () => {
  let fixture: ComponentFixture<QuanTriNguoiDungPage>;
  let page: QuanTriNguoiDungPage;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(QuanTriNguoiDungPage);
    page = fixture.componentInstance;

    // Lần nạp đầu do effect() kích khi CD chạy lần đầu — flush để không còn request treo.
    fixture.detectChanges();
    const initial = listRequests();
    expect(initial.length).toBe(1);
    initial[0].flush(pagedOk());
  });

  afterEach(() => httpMock.verify());

  /** `match()` lấy RA khỏi hàng đợi các request đang chờ — gọi 2 lần không trả lại cùng 1 request. */
  function listRequests(): TestRequest[] {
    return httpMock.match((req) => req.method === 'GET' && req.url === '/users');
  }

  function type(value: string): void {
    page.onSearchInputEvent({ target: { value } } as unknown as Event);
  }

  /** Chờ thật `ms` (cho `debounceTime` kịp kích) rồi chạy CD để effect gọi API. */
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

    // Vế 2 — chuỗi gửi đi phải là chuỗi CUỐI CÙNG, không phải giá trị trước debounce ('' hoặc
    // 'nguyenvan'). Đây đúng là phần bug cũ không bao giờ đúng.
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
    httpMock.expectOne('/users/u1/lock').flush({ ...pagedOk(), data: true });
    await advance(0);

    const requests = listRequests();
    expect(requests.length).toBe(1);
    expect(requests[0].request.params.get('page')).toBe('2');
    requests[0].flush(pagedOk(2));
  });
});
