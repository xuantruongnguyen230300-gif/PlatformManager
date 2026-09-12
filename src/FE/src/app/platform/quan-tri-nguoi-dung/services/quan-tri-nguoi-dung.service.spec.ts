import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable } from 'rxjs';
import { IApiResult } from '../../../core/http/api-result.model';
import { IUserDto } from '../models/quan-tri-nguoi-dung.model';
import { QuanTriNguoiDungService } from './quan-tri-nguoi-dung.service';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-quan-tri-nguoi-dung',
    retryable: null,
    fields: null,
  };
}

const USER_DTO: IUserDto = {
  id: 'u1',
  userName: 'nguyenvana',
  email: 'a@example.com',
  fullName: 'Nguyễn Văn A',
  roles: ['Admin'],
  isLocked: false,
  mustChangePassword: true,
  dateCreate: '2026-08-29T03:00:00Z',
};

/**
 * Service này mang MỘT quyết định đã trả giá thật: `envelopeSucceeded`. Bản BE trước 2026-08-29
 * trả `200` + `data: false` khi tầng ghi Identity hỏng, FE coi "không lỗi mạng ⇒ xong" rồi hiện
 * toast *"Đã khoá tài khoản."* cho một thao tác chưa khoá được gì — xem doc/contracts/users.md
 * §"Thao tác hỏng nay là LỖI". BE nay trả 422 nên đường đó không còn đi qua `next`, và chính vì
 * vậy nó KHÔNG còn được thứ gì canh: chốt chặn duy nhất cho FE chạy với một BE chưa cập nhật là
 * mấy test dưới đây.
 */
describe('QuanTriNguoiDungService', () => {
  let service: QuanTriNguoiDungService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(QuanTriNguoiDungService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ===== getList =====

  it('getList(): GET /users, map DTO camelCase sang model PascalCase kèm shape phân trang', () => {
    let items: string[] | undefined;
    let totalCount: number | undefined;
    service.getList({ Page: 2, PageSize: 20 }).subscribe((res) => {
      items = res.Items.map((u) => u.UserName);
      totalCount = res.TotalCount;
    });

    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush(ok({ items: [USER_DTO], page: 2, pageSize: 20, totalCount: 41 }));

    expect(items).toEqual(['nguyenvana']);
    expect(totalCount).toBe(41);
  });

  it('getList(): bỏ hẳn tham số lọc khi không truyền — KHÔNG gửi chuỗi rỗng', () => {
    service.getList({ Page: 1, PageSize: 10 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.params.has('searchText')).toBeFalse();
    expect(req.request.params.has('role')).toBeFalse();
    expect(req.request.params.has('isLocked')).toBeFalse();
    req.flush(ok({ items: [], page: 1, pageSize: 10, totalCount: 0 }));
  });

  /**
   * WIRE — `IsLocked: false` là *"chỉ tài khoản đang hoạt động"*, một BỘ LỌC THẬT, không phải
   * "không lọc". Rút gọn điều kiện thành `if (params.IsLocked)` là mất hẳn nhánh này mà
   * TypeScript không kêu một tiếng: màn hình lặng lẽ trả về cả tài khoản đã khoá.
   */
  it('WIRE: `IsLocked: false` vẫn được gửi lên (falsy KHÔNG phải là vắng mặt)', () => {
    service.getList({ Page: 1, PageSize: 10, IsLocked: false }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.params.get('isLocked'))
      .withContext('mất tham số này là hiện nhầm cả tài khoản đã khoá')
      .toBe('false');
    req.flush(ok({ items: [], page: 1, pageSize: 10, totalCount: 0 }));
  });

  it('getList(): gửi searchText/role/isLocked=true đúng tên tham số camelCase của BE', () => {
    service
      .getList({ Page: 1, PageSize: 10, SearchText: 'an', Role: 'Admin', IsLocked: true })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.params.get('searchText')).toBe('an');
    expect(req.request.params.get('role')).toBe('Admin');
    expect(req.request.params.get('isLocked')).toBe('true');
    req.flush(ok({ items: [], page: 1, pageSize: 10, totalCount: 0 }));
  });

  /**
   * ĐỔI 2026-09-08 — test này trước khoá hành vi NGƯỢC LẠI: *"envelope thiếu `data` trả danh sách
   * rỗng, giữ page/pageSize đã hỏi"*. Lưới rỗng là một câu trả lời hợp lệ của server, nên dựng nó
   * ra từ envelope hỏng khiến màn hình nói "Không có dữ liệu" cho một lượt tải thất bại — không
   * toast, không khối lỗi, không cách nào biết. Nay `getList` đi qua `unwrapData` nên hỏng là
   * hỏng, và trang rơi vào nhánh `loadError` sẵn có.
   */
  it('getList(): envelope thiếu `data` là LỖI, KHÔNG dựng lưới rỗng giả', () => {
    let err: Error | undefined;
    let emitted = false;
    service.getList({ Page: 3, PageSize: 25 }).subscribe({
      next: () => (emitted = true),
      error: (e: Error) => (err = e),
    });

    httpMock.expectOne((r) => r.url === '/users').flush(ok(null));

    expect(err).toBeDefined();
    expect(emitted).withContext('không được emit một lưới rỗng dựng sẵn').toBeFalse();
  });

  // ===== create =====

  it('create(): POST /users với body camelCase, trả id từ `data`', () => {
    let newId: string | undefined;
    service
      .create({
        UserName: 'nguyenvanb',
        Email: 'b@example.com',
        FullName: 'Nguyễn Văn B',
        TempPassword: 'MatKhauTam123!',
        Roles: ['User'],
      })
      .subscribe((id) => (newId = id));

    const req = httpMock.expectOne('/users');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      userName: 'nguyenvanb',
      email: 'b@example.com',
      fullName: 'Nguyễn Văn B',
      tempPassword: 'MatKhauTam123!',
      roles: ['User'],
    });
    req.flush(ok('u2'));

    expect(newId).toBe('u2');
  });

  it('create(): envelope thiếu `data` là LỖI, không trả id rỗng', () => {
    let err: Error | undefined;
    service
      .create({
        UserName: 'x',
        Email: 'x@example.com',
        FullName: 'X',
        TempPassword: 'MatKhauTam123!',
        Roles: [],
      })
      .subscribe({ error: (e: Error) => (err = e) });

    httpMock.expectOne('/users').flush(ok(null));

    expect(err).toBeDefined();
  });

  // ===== update — hình dạng body =====

  it('update(): PUT /users/:id với body camelCase (KHÔNG gửi kèm userName)', () => {
    service
      .update('u1', { Email: 'moi@example.com', FullName: 'Tên Mới', Roles: ['Admin'], Version: 'stamp-1' })
      .subscribe();

    const req = httpMock.expectOne('/users/u1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      email: 'moi@example.com',
      fullName: 'Tên Mới',
      roles: ['Admin'],
      version: 'stamp-1',
    });
    req.flush(ok(true));
  });

  /**
   * 🛑 Test này canh một lỗ hổng KHÔNG lộ ra ở đâu khác.
   *
   * BE chỉ kiểm tranh chấp ghi khi client THẬT SỰ gửi token (`UpdateUserHandler.Handle` —
   * `cmd.Version is not null`). Nên nếu FE thôi gửi `version`, mọi thứ vẫn xanh: build xanh, lint
   * xanh, `update()` vẫn trả `true`, người dùng vẫn thấy toast "Đã cập nhật". Thứ DUY NHẤT thay
   * đổi là hai admin sửa cùng một người lại ghi đè nhau im lặng — đúng trạng thái trước 2026-09-08.
   *
   * Vì vậy phải khoá riêng sự CÓ MẶT của khoá `version` trong body, chứ không dựa vào test hình
   * dạng ở trên (`toEqual` sẽ đỏ, nhưng người sửa dễ "dọn" nó bằng cách bỏ luôn dòng version).
   */
  it('update(): LUÔN gửi khoá `version` — kể cả khi không có token thì gửi null, không bỏ khoá', () => {
    service.update('u1', { Email: 'e@example.com', FullName: 'F', Roles: [], Version: null }).subscribe();

    const req = httpMock.expectOne('/users/u1');
    const body = req.request.body as Record<string, unknown>;
    expect('version' in body)
      .withContext(
        'Thiếu khoá `version` ⇒ BE bỏ qua kiểm tranh chấp ghi và quay lại ghi đè im lặng. ' +
          'Xem doc/contracts/users.md §"Quyết định người dùng 2026-08-30" quyết định 3.',
      )
      .toBeTrue();
    expect(body['version']).toBeNull();
    req.flush(ok(true));
  });

  /**
   * 409 `USER.VERSION_CONFLICT` rơi vào nhánh `error` như mọi lỗi khác — service KHÔNG nuốt nó
   * thành `false`. Phân biệt hai đường này là điều kiện để trang hiện được câu của BE thay vì câu
   * dự phòng "envelope không báo thành công" (xem `submitUpdate`).
   */
  it('update(): 409 VERSION_CONFLICT đi ra nhánh error, KHÔNG thành `false` ở nhánh next', () => {
    let succeeded: boolean | undefined;
    let status: number | undefined;
    service.update('u1', { Email: 'e@example.com', FullName: 'F', Roles: [], Version: 'cu' }).subscribe({
      next: (v) => (succeeded = v),
      error: (err: { status: number }) => (status = err.status),
    });

    httpMock.expectOne('/users/u1').flush(
      { ...ok(false), status: 'ERROR', businessCode: 'USER.VERSION_CONFLICT' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(succeeded).toBeUndefined();
    expect(status).toBe(409);
  });

  // ===== envelopeSucceeded — chốt chặn chống lời nói dối im lặng =====

  interface IThaoTac {
    ten: string;
    url: string;
    method: string;
    chay: (s: QuanTriNguoiDungService) => Observable<boolean>;
  }

  const THAO_TAC: IThaoTac[] = [
    {
      ten: 'update',
      url: '/users/u1',
      method: 'PUT',
      chay: (s) => s.update('u1', { Email: 'e@example.com', FullName: 'F', Roles: [], Version: 'v1' }),
    },
    { ten: 'lock', url: '/users/u1/lock', method: 'POST', chay: (s) => s.lock('u1') },
    { ten: 'unlock', url: '/users/u1/unlock', method: 'POST', chay: (s) => s.unlock('u1') },
  ];

  for (const tt of THAO_TAC) {
    it(`${tt.ten}(): envelope SUCCESS + data true ⇒ true`, () => {
      let succeeded: boolean | undefined;
      tt.chay(service).subscribe((v) => (succeeded = v));

      const req = httpMock.expectOne(tt.url);
      expect(req.request.method).toBe(tt.method);
      req.flush(ok(true));

      expect(succeeded).toBeTrue();
    });

    /**
     * ĐÂY là bug đã xảy ra thật: 200 + `data: false`. Nơi gọi đọc giá trị này để quyết định có
     * hiện toast thành công hay không — trả `true` ở đây là dựng lại nguyên lời nói dối cũ.
     */
    it(`${tt.ten}(): envelope SUCCESS nhưng data false ⇒ false, KHÔNG báo thành công`, () => {
      let succeeded: boolean | undefined;
      tt.chay(service).subscribe((v) => (succeeded = v));

      httpMock.expectOne(tt.url).flush(ok(false));

      expect(succeeded)
        .withContext('200 + data:false là thao tác HỎNG — xem doc/contracts/users.md')
        .toBeFalse();
    });

    it(`${tt.ten}(): status khác SUCCESS ⇒ false dù data true`, () => {
      let succeeded: boolean | undefined;
      tt.chay(service).subscribe((v) => (succeeded = v));

      httpMock.expectOne(tt.url).flush({
        ...ok(true),
        status: 'BUSINESS_ERROR',
        code: 'BusinessRuleError',
      });

      expect(succeeded).toBeFalse();
    });

    it(`${tt.ten}(): envelope thiếu data ⇒ false`, () => {
      let succeeded: boolean | undefined;
      tt.chay(service).subscribe((v) => (succeeded = v));

      httpMock.expectOne(tt.url).flush(ok(null));

      expect(succeeded).toBeFalse();
    });
  }

  it('lock/unlock gọi HAI endpoint khác nhau, không dùng lại đường dẫn của nhau', () => {
    service.lock('u1').subscribe();
    service.unlock('u2').subscribe();

    const lockReq = httpMock.expectOne('/users/u1/lock');
    const unlockReq = httpMock.expectOne('/users/u2/unlock');
    expect(lockReq.request.body).toEqual({});
    expect(unlockReq.request.body).toEqual({});

    lockReq.flush(ok(true));
    unlockReq.flush(ok(true));
  });
});
