import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { PhanQuyenService } from './phan-quyen.service';
import { IApiResult } from '../../../core/http/api-result.model';
import { IPermissionRow, IResourcePermissionRow } from '../models/phan-quyen.model';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-phan-quyen',
    retryable: null,
    fields: null,
  };
}

/**
 * Hai cặp endpoint KHÁC NHAU trên cùng một service — test này tồn tại chủ yếu để chốt rằng chúng
 * không bị trộn vào nhau: PERM-1 ghi `SysMenuRole` (vắng mặt = MỞ cho mọi user đã đăng nhập),
 * PERM-2 ghi `RolePermission` (vắng mặt = TỪ CHỐI). Gửi nhầm payload sang endpoint kia là đổi
 * quyền của cả hệ thống theo chiều không ai mong muốn, và không có kiểu dữ liệu nào chặn được vì
 * hai request đều là `{ entries: [...] }`.
 */
describe('PhanQuyenService', () => {
  let service: PhanQuyenService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PhanQuyenService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ===== PERM-1 =====

  it('getMatrix(): GET /admin/permissions, trả model đã map (kèm `version`)', () => {
    let result: string[] | undefined;
    let version: string | undefined;
    service.getMatrix().subscribe((m) => {
      result = m.Rows.map((r) => r.SysMenuName);
      version = m.Version;
    });

    const req = httpMock.expectOne('/admin/permissions');
    expect(req.request.method).toBe('GET');
    req.flush(
      ok({
        roles: ['SuperAdmin'],
        rows: [{ sysMenuId: 'm1', sysMenuCode: 'a', sysMenuName: 'Màn A', assignedRoles: [] }],
        version: 'v-menu-1',
      }),
    );

    expect(result).toEqual(['Màn A']);
    expect(version).toBe('v-menu-1');
  });

  it('saveMatrix(): PUT /admin/permissions với body PHẲNG `{ entries, version }`', () => {
    const rows: IPermissionRow[] = [
      { SysMenuId: 'm1', SysMenuCode: 'a', SysMenuName: 'Màn A', ParentId: null, AssignedRoles: ['Admin'] },
      { SysMenuId: 'm2', SysMenuCode: 'b', SysMenuName: 'Màn B', ParentId: 'm1', AssignedRoles: [] },
    ];
    service.saveMatrix(rows, 'v-menu-1').subscribe();

    const req = httpMock.expectOne('/admin/permissions');
    expect(req.request.method).toBe('PUT');
    // GHI ĐÈ TOÀN BỘ: phải gửi ĐỦ mọi hàng, kể cả hàng không tick role nào — thiếu hàng nào nay là
    // 400 ở BE (rule "phủ đủ", permissions.md §Quyết định người dùng 2026-08-30).
    expect(req.request.body).toEqual({
      entries: [
        { sysMenuId: 'm1', roles: ['Admin'] },
        { sysMenuId: 'm2', roles: [] },
      ],
      version: 'v-menu-1',
    });
    req.flush(ok(true));
  });

  /**
   * TEST WIRE — chốt riêng sự CÓ MẶT của `version` trong body `PUT`, tách khỏi test hình dạng ở
   * trên vì đây là thứ đã lọt: FE không gửi khoá này thì BE so `null` với một chuỗi hex 64 ký tự
   * (hàm băm luôn trả chuỗi, kể cả cho ma trận rỗng) ⇒ MỌI lần lưu 409, màn hình không lưu được gì.
   * Build vẫn xanh vì `JSON.stringify` chỉ lặng lẽ bỏ khoá vắng mặt.
   */
  it('WIRE: body PUT PERM-1 luôn có khoá `version` mang đúng token đã truyền', () => {
    service.saveMatrix([], 'abc123').subscribe();

    // `expectOne` GỠ request khỏi hàng đợi — giữ lại `TestRequest` để `flush`, đừng gọi lần hai.
    const req = httpMock.expectOne('/admin/permissions');
    const body = req.request.body as Record<string, unknown>;
    expect(Object.keys(body)).withContext('thiếu khoá `version` là 409 ở mọi lần lưu').toContain('version');
    expect(body['version']).toBe('abc123');

    req.flush(ok(true));
  });

  // ===== PERM-2 =====

  it('getResourceMatrix(): GET /admin/permissions/resources (KHÔNG dùng lại đường dẫn PERM-1)', () => {
    let result: IResourcePermissionRow[] | undefined;
    service.getResourceMatrix().subscribe((m) => (result = m.Rows));

    const req = httpMock.expectOne('/admin/permissions/resources');
    expect(req.request.method).toBe('GET');
    req.flush(
      ok({
        roles: ['SuperAdmin', 'Admin', 'User'],
        rows: [{ resourceKey: 'import.manage', resourceName: 'Import CSV/Excel', assignedRoles: ['Admin'] }],
        version: 'v-res-1',
      }),
    );

    expect(result).toEqual([
      { ResourceKey: 'import.manage', ResourceName: 'Import CSV/Excel', AssignedRoles: ['Admin'] },
    ]);
  });

  it('saveResourceMatrix(): PUT /admin/permissions/resources với `{ entries, version }`', () => {
    const rows: IResourcePermissionRow[] = [
      { ResourceKey: 'import.manage', ResourceName: 'Import CSV/Excel', AssignedRoles: ['Admin', 'User'] },
    ];
    service.saveResourceMatrix(rows, 'v-res-1').subscribe();

    const req = httpMock.expectOne('/admin/permissions/resources');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      entries: [{ resourceKey: 'import.manage', roles: ['Admin', 'User'] }],
      version: 'v-res-1',
    });
    req.flush(ok(true));
  });

  /** Xem test WIRE của PERM-1 — cùng một lỗ hổng, hai endpoint, nên phải có hai test. */
  it('WIRE: body PUT PERM-2 luôn có khoá `version` mang đúng token đã truyền', () => {
    service.saveResourceMatrix([], 'def456').subscribe();

    const req = httpMock.expectOne('/admin/permissions/resources');
    const body = req.request.body as Record<string, unknown>;
    expect(Object.keys(body)).withContext('thiếu khoá `version` là 409 ở mọi lần lưu').toContain('version');
    expect(body['version']).toBe('def456');

    req.flush(ok(true));
  });

  /**
   * Hai token ĐỘC LẬP (băm trên hai bảng khác nhau) — test này chốt rằng service không "gom cho
   * gọn" một token dùng chung, thứ sẽ khiến mọi lần lưu ở tab thứ hai nhận 409.
   */
  it('token của PERM-2 đi đúng endpoint PERM-2, KHÔNG lẫn sang PERM-1', () => {
    service.saveMatrix([], 'token-menu').subscribe();
    service.saveResourceMatrix([], 'token-resource').subscribe();

    const menuReq = httpMock.expectOne('/admin/permissions');
    const resourceReq = httpMock.expectOne('/admin/permissions/resources');
    expect((menuReq.request.body as Record<string, unknown>)['version']).toBe('token-menu');
    expect((resourceReq.request.body as Record<string, unknown>)['version']).toBe('token-resource');

    menuReq.flush(ok(true));
    resourceReq.flush(ok(true));
  });

  it('saveResourceMatrix() KHÔNG tự thêm SuperAdmin vào payload dù UI hiển thị cột đó đã tick', () => {
    // Cột `SuperAdmin` tick sẵn + khoá ở `ResourcePermissionMatrix` chỉ MÔ TẢ bypass sẵn có của
    // `RequirePermissionFilter`, không phải một dòng dữ liệu. Tự thêm vào body là ghi vào bảng
    // `RolePermissions` thứ người quản trị chưa hề chọn — và contract PERM-2 nói rõ FE gửi nguyên
    // `assignedRoles` do BE trả về.
    service
      .saveResourceMatrix(
        [{ ResourceKey: 'import.manage', ResourceName: 'Import CSV/Excel', AssignedRoles: [] }],
        'v-res-1',
      )
      .subscribe();

    const req = httpMock.expectOne('/admin/permissions/resources');
    expect(req.request.body).toEqual({
      entries: [{ resourceKey: 'import.manage', roles: [] }],
      version: 'v-res-1',
    });
    req.flush(ok(true));
  });
});
