import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { IApiResult } from '../http/api-result.model';
import { ICurrentUser, ICurrentUserDto, mapCurrentUserDtoToModel } from './current-user.model';
import { IChangePasswordRequestDto, ILoginRequestDto } from './auth.model';
import { CurrentUserService } from './current-user.service';

/**
 * Gọi API auth thật (`POST /api/auth/login|logout|change-password`) — xem doc/contracts/auth.md.
 * `CurrentUserService` không tự gọi HTTP cho các endpoint này, chỉ nhận lại kết quả qua
 * `setUser()`/`clear()`/`markPasswordChanged()` để giữ đúng ranh giới "context vs HTTP" đã chốt.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUser = inject(CurrentUserService);

  login(userName: string, password: string): Observable<ICurrentUser> {
    const body: ILoginRequestDto = { userName, password };
    return this.http.post<IApiResult<ICurrentUserDto>>('/auth/login', body).pipe(
      map((res) => mapCurrentUserDtoToModel(res.data as ICurrentUserDto)),
      tap((user) => this.currentUser.setUser(user)),
    );
  }

  logout(): Observable<void> {
    return this.http.post<IApiResult<boolean>>('/auth/logout', {}).pipe(
      map(() => undefined),
      tap(() => this.currentUser.clear()),
    );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    const body: IChangePasswordRequestDto = { currentPassword, newPassword };
    return this.http.post<IApiResult<boolean>>('/auth/change-password', body).pipe(map(() => undefined));
  }
}
