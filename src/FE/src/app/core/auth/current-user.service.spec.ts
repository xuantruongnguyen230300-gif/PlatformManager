import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { CurrentUserService } from './current-user.service';
import { IApiResult } from '../http/api-result.model';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';
import { ICurrentUserDto } from './current-user.model';

function ok(data: ICurrentUserDto): IApiResult<ICurrentUserDto> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-me',
    retryable: null,
    fields: null,
  };
}

const DTO: ICurrentUserDto = {
  id: 'u1',
  userName: 'sa',
  email: null,
  fullName: 'Quản trị viên',
  roles: ['SuperAdmin', 'Admin'],
  mustChangePassword: false,
};

describe('CurrentUserService', () => {
  let service: CurrentUserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CurrentUserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('load(): GET /auth/me, map DTO camelCase sang model PascalCase', () => {
    service.load().subscribe();

    const req = httpMock.expectOne('/auth/me');
    expect(req.request.method).toBe('GET');
    req.flush(ok(DTO));

    expect(service.currentUser()).toEqual({
      Id: 'u1',
      UserName: 'sa',
      Email: null,
      FullName: 'Quản trị viên',
      Roles: ['SuperAdmin', 'Admin'],
      MustChangePassword: false,
    });
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.isLoaded()).toBeTrue();
  });

  it('load(): 401 là tình huống BÌNH THƯỜNG lúc khởi động — trả null, KHÔNG throw', () => {
    // `APP_INITIALIZER` gọi hàm này trước mọi thứ; nếu 401 vỡ ra ngoài thì app không khởi động
    // được cho khách vãng lai chưa đăng nhập, và họ cũng không tới được màn login.
    let emitted: unknown = 'chưa phát';
    let errored = false;
    service.load().subscribe({ next: (v) => (emitted = v), error: () => (errored = true) });

    httpMock.expectOne('/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(errored).withContext('401 không được vỡ ra ngoài').toBeFalse();
    expect(emitted).toBeNull();
    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
    // `isLoaded` vẫn phải bật: "đã hỏi xong, câu trả lời là chưa đăng nhập" khác hẳn "chưa hỏi".
    // Guard dựa vào cờ này để biết được phép quyết định hay còn phải chờ.
    expect(service.isLoaded()).toBeTrue();
  });

  it('load(): đánh dấu bỏ qua toast lỗi — 401 lúc khởi động không được làm phiền người dùng', () => {
    service.load().subscribe();
    const req = httpMock.expectOne('/auth/me');
    // Đọc thẳng token thay vì đếm số key: cờ này là thứ giữ cho khách vãng lai chưa đăng nhập
    // không bị ném toast "Bạn cần đăng nhập" ngay khi mở app.
    expect(req.request.context.get(SKIP_ERROR_TOAST))
      .withContext('phải gắn SKIP_ERROR_TOAST')
      .toBeTrue();
    req.flush(ok(DTO));
  });

  it('hasAnyRole(): đúng khi giữ ÍT NHẤT MỘT role trong danh sách', () => {
    service.load().subscribe();
    httpMock.expectOne('/auth/me').flush(ok(DTO));

    expect(service.hasAnyRole('SuperAdmin')).toBeTrue();
    expect(service.hasAnyRole('User', 'Admin')).withContext('chỉ cần khớp 1').toBeTrue();
    expect(service.hasAnyRole('User')).toBeFalse();
    expect(service.hasAnyRole()).withContext('không truyền role nào = không khớp gì').toBeFalse();
  });

  it('hasAnyRole(): chưa đăng nhập thì luôn false, không ném lỗi', () => {
    expect(service.hasAnyRole('SuperAdmin')).toBeFalse();
  });

  it('markPasswordChanged(): chỉ hạ cờ MustChangePassword, giữ nguyên phần còn lại', () => {
    service.load().subscribe();
    httpMock.expectOne('/auth/me').flush(ok({ ...DTO, mustChangePassword: true }));
    expect(service.mustChangePassword()).toBeTrue();

    service.markPasswordChanged();

    expect(service.mustChangePassword()).toBeFalse();
    expect(service.currentUser()?.Roles).toEqual(['SuperAdmin', 'Admin']);
    expect(service.fullName()).toBe('Quản trị viên');
  });

  it('markPasswordChanged() khi chưa có user thì không tạo ra user rỗng', () => {
    service.markPasswordChanged();
    expect(service.currentUser()).toBeNull();
  });

  it('clear(): đăng xuất xoá user nhưng KHÔNG đặt lại isLoaded', () => {
    service.load().subscribe();
    httpMock.expectOne('/auth/me').flush(ok(DTO));

    service.clear();

    expect(service.currentUser()).toBeNull();
    expect(service.fullName()).toBe('');
    // Hạ `isLoaded` ở đây sẽ khiến guard tưởng app đang khởi động lại và treo màn hình.
    expect(service.isLoaded()).toBeTrue();
  });
});
