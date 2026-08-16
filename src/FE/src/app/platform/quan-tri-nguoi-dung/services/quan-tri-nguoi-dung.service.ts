import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult } from '../../../core/http/api-result.model';
import {
  ICreateUserPayload,
  IUpdateUserPayload,
  IUserListParams,
  IUserPagedList,
  IUserPagedListDto,
} from '../models/quan-tri-nguoi-dung.model';
import { mapUserPagedListDtoToModel } from './quan-tri-nguoi-dung.mapper';

/** Gọi API Quản trị người dùng — xem doc/contracts/users.md. Gate BE: `SuperAdmin,Admin`. */
@Injectable({ providedIn: 'root' })
export class QuanTriNguoiDungService {
  private readonly http = inject(HttpClient);

  getList(params: IUserListParams): Observable<IUserPagedList> {
    let httpParams = new HttpParams().set('page', params.Page).set('pageSize', params.PageSize);
    if (params.SearchText) httpParams = httpParams.set('searchText', params.SearchText);
    return this.http
      .get<IApiResult<IUserPagedListDto>>('/users', { params: httpParams })
      .pipe(
        map((res) =>
          mapUserPagedListDtoToModel(
            res.data ?? { items: [], total: 0, page: params.Page, pageSize: params.PageSize },
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
    return this.http.post<IApiResult<string>>('/users', body).pipe(map((res) => res.data as string));
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
