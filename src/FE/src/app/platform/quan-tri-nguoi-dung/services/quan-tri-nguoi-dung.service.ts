import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import { IPagedResult, IPagedResultDto, mapPagedResultDto } from '../../../core/http/paged-result.model';
import {
  ICreateUserPayload,
  IUpdateUserPayload,
  IUser,
  IUserDto,
  IUserListParams,
} from '../models/quan-tri-nguoi-dung.model';
import { mapUserDtoToModel } from './quan-tri-nguoi-dung.mapper';

/** Gọi API Quản trị người dùng — xem doc/contracts/users.md. Gate BE: `SuperAdmin,Admin`. */
@Injectable({ providedIn: 'root' })
export class QuanTriNguoiDungService {
  private readonly http = inject(HttpClient);

  getList(params: IUserListParams): Observable<IPagedResult<IUser>> {
    let httpParams = new HttpParams().set('page', params.Page).set('pageSize', params.PageSize);
    if (params.SearchText) httpParams = httpParams.set('searchText', params.SearchText);
    return this.http
      .get<IApiResult<IPagedResultDto<IUserDto>>>('/users', { params: httpParams })
      .pipe(
        map((res) =>
          mapPagedResultDto(
            res.data ?? { items: [], page: params.Page, pageSize: params.PageSize, totalCount: 0 },
            mapUserDtoToModel,
          ),
        ),
      );
  }

  create(payload: ICreateUserPayload): Observable<string> {
    const body = {
      userName: payload.UserName,
      email: payload.Email,
      fullName: payload.FullName,
      tempPassword: payload.TempPassword,
      roles: payload.Roles,
    };
    return this.http.post<IApiResult<string>>('/users', body).pipe(map((res) => unwrapData(res)));
  }

  update(id: string, payload: IUpdateUserPayload): Observable<void> {
    const body = { email: payload.Email, fullName: payload.FullName, roles: payload.Roles };
    return this.http.put<IApiResult<boolean>>(`/users/${id}`, body).pipe(map(() => undefined));
  }

  lock(id: string): Observable<void> {
    return this.http.post<IApiResult<boolean>>(`/users/${id}/lock`, {}).pipe(map(() => undefined));
  }

  unlock(id: string): Observable<void> {
    return this.http.post<IApiResult<boolean>>(`/users/${id}/unlock`, {}).pipe(map(() => undefined));
  }
}
