import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import { IPermissionMatrix, IPermissionMatrixDto, IPermissionRow, ISavePermissionMatrixRequestDto } from '../models/phan-quyen.model';
import { mapPermissionMatrixDtoToModel } from './phan-quyen.mapper';

/** Gọi API Phân quyền — xem doc/contracts/permissions.md. Gate BE: CHỈ `SuperAdmin`. */
@Injectable({ providedIn: 'root' })
export class PhanQuyenService {
  private readonly http = inject(HttpClient);

  getMatrix(): Observable<IPermissionMatrix> {
    return this.http
      .get<IApiResult<IPermissionMatrixDto>>('/admin/permissions')
      .pipe(map((res) => mapPermissionMatrixDtoToModel(unwrapData(res))));
  }

  /**
   * PUT ghi đè TOÀN BỘ `SysMenuRole` — `rows` truyền vào PHẢI là đủ toàn bộ danh sách hiện có
   * (không chỉ dòng vừa đổi), xem cảnh báo ở doc/contracts/permissions.md §Rủi ro.
   */
  saveMatrix(rows: IPermissionRow[]): Observable<void> {
    const body: ISavePermissionMatrixRequestDto = {
      entries: rows.map((r) => ({ sysMenuId: r.SysMenuId, roles: r.AssignedRoles })),
    };
    return this.http.put<IApiResult<boolean>>('/admin/permissions', body).pipe(map(() => undefined));
  }
}
