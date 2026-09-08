import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import {
  IPermissionMatrix,
  IPermissionMatrixDto,
  IPermissionRow,
  IResourcePermissionMatrix,
  IResourcePermissionMatrixDto,
  IResourcePermissionRow,
  ISavePermissionMatrixRequestDto,
  ISaveResourcePermissionMatrixRequestDto,
} from '../models/phan-quyen.model';
import { mapPermissionMatrixDtoToModel, mapResourcePermissionMatrixDtoToModel } from './phan-quyen.mapper';

/**
 * Gọi API Phân quyền — xem doc/contracts/permissions.md. Gate BE: CHỈ `SuperAdmin`.
 *
 * HAI ma trận, HAI cặp endpoint, KHÔNG gộp: PERM-1 (`/admin/permissions`) ghi `SysMenuRole` —
 * menu nào role nào nhìn thấy, vắng mặt = mở cho mọi user đã đăng nhập. PERM-2
 * (`/admin/permissions/resources`) ghi `RolePermission` — quyền gọi API, vắng mặt = TỪ CHỐI.
 * Ngữ nghĩa mặc định ngược nhau nên trộn payload là ghi nhầm bảng.
 */
@Injectable({ providedIn: 'root' })
export class PhanQuyenService {
  private readonly http = inject(HttpClient);

  // ===== PERM-1 — ma trận menu =====

  getMatrix(): Observable<IPermissionMatrix> {
    return this.http
      .get<IApiResult<IPermissionMatrixDto>>('/admin/permissions')
      .pipe(map((res) => mapPermissionMatrixDtoToModel(unwrapData(res))));
  }

  /**
   * PUT ghi đè TOÀN BỘ `SysMenuRole` — `rows` truyền vào PHẢI là đủ toàn bộ danh sách hiện có
   * (không chỉ dòng vừa đổi), xem cảnh báo ở doc/contracts/permissions.md §Rủi ro. Từ 2026-08-31
   * BE còn TỪ CHỐI payload thiếu mục (400) thay vì âm thầm xoá mục thiếu.
   *
   * `version` là token nhận từ `GET` gần nhất, gửi lại nguyên văn. BẮT BUỘC: BE so khớp Ordinal
   * với token nó tính lại từ DB, lệch hoặc thiếu ⇒ **409** và không ghi gì
   * (`PermissionErrors.VersionConflict`, businessCode `PERMISSION.VERSION_CONFLICT`). Nơi gọi
   * phải làm mới token sau MỖI lần lưu thành công — ghi xong là nội dung bảng đổi, nên token vừa
   * gửi đã cũ ngay lập tức và lần lưu thứ hai chắc chắn 409 nếu dùng lại nó.
   */
  saveMatrix(rows: IPermissionRow[], version: string): Observable<void> {
    const body: ISavePermissionMatrixRequestDto = {
      entries: rows.map((r) => ({ sysMenuId: r.SysMenuId, roles: r.AssignedRoles })),
      version,
    };
    return this.http.put<IApiResult<boolean>>('/admin/permissions', body).pipe(map(() => undefined));
  }

  // ===== PERM-2 — ma trận tài nguyên =====

  getResourceMatrix(): Observable<IResourcePermissionMatrix> {
    return this.http
      .get<IApiResult<IResourcePermissionMatrixDto>>('/admin/permissions/resources')
      .pipe(map((res) => mapResourcePermissionMatrixDtoToModel(unwrapData(res))));
  }

  /**
   * PUT ghi đè TOÀN BỘ `RolePermission` — cũng phải gửi ĐỦ mọi `rows`, thiếu dòng nào là thu hồi
   * sạch quyền của dòng đó (và với PERM-2 "không có dòng" nghĩa là TỪ CHỐI, nặng hơn PERM-1).
   *
   * Gửi NGUYÊN `AssignedRoles` mà BE trả về, KHÔNG tự thêm `SuperAdmin` dù UI hiển thị cột đó đã
   * tick sẵn: cái tick ấy là mô tả bypass sẵn có ở `RequirePermissionFilter`, không phải một dòng
   * dữ liệu — tự thêm vào payload sẽ ghi vào DB thứ người dùng không hề chọn.
   *
   * `version`: xem `saveMatrix()`. Token của PERM-2 tính trên bảng `RolePermissions`, KHÁC token
   * của PERM-1 — dùng nhầm token của tab kia thì 409 chắc chắn.
   */
  saveResourceMatrix(rows: IResourcePermissionRow[], version: string): Observable<void> {
    const body: ISaveResourcePermissionMatrixRequestDto = {
      entries: rows.map((r) => ({ resourceKey: r.ResourceKey, roles: r.AssignedRoles })),
      version,
    };
    return this.http
      .put<IApiResult<boolean>>('/admin/permissions/resources', body)
      .pipe(map(() => undefined));
  }
}
