import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { IApiResult } from '../http/api-result.model';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';
import { ICurrentUser, ICurrentUserDto, mapCurrentUserDtoToModel } from './current-user.model';

/**
 * Context người dùng hiện tại — signal thường (CHƯA cần `signalStore`, đúng ngưỡng đã chốt vì
 * chỉ 1 nơi giữ state này). KHÔNG tự biết chi tiết HTTP/envelope ngoài việc gọi `GET /api/auth/me`
 * — `AuthService` (core/auth/auth.service.ts) mới là nơi login/logout/change-password thật, rồi
 * gọi `setUser()`/`clear()` ở đây để đồng bộ. Xem doc/huong_dan/wiki-core/fe/07-auth-identity.md.
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly http = inject(HttpClient);

  private readonly user = signal<ICurrentUser | null>(null);
  private readonly loaded = signal(false);

  readonly currentUser = this.user.asReadonly();
  readonly isLoaded = this.loaded.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly mustChangePassword = computed(() => this.user()?.MustChangePassword ?? false);
  readonly fullName = computed(() => this.user()?.FullName ?? '');

  hasRole(role: string): boolean {
    return this.user()?.Roles.includes(role) ?? false;
  }

  /**
   * `GET /api/auth/me` — 401 (chưa đăng nhập) là tình huống BÌNH THƯỜNG lúc app khởi động, KHÔNG
   * throw ra ngoài: `catchError` chuyển thành `null`. `SKIP_ERROR_TOAST` chặn `httpErrorInterceptor`
   * hiện toast "chưa đăng nhập" gây phiền cho khách vãng lai chưa login.
   */
  load(): Observable<ICurrentUser | null> {
    return this.http
      .get<IApiResult<ICurrentUserDto>>('/auth/me', { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
      .pipe(
        map((res) => mapCurrentUserDtoToModel(res.data as ICurrentUserDto)),
        tap((user) => {
          this.user.set(user);
          this.loaded.set(true);
        }),
        catchError(() => {
          this.user.set(null);
          this.loaded.set(true);
          return of(null);
        }),
      );
  }

  setUser(user: ICurrentUser | null): void {
    this.user.set(user);
  }

  clear(): void {
    this.user.set(null);
  }

  /** Cập nhật cục bộ sau `POST /api/auth/change-password` thành công — tránh gọi lại `/auth/me`. */
  markPasswordChanged(): void {
    this.user.update((u) => (u ? { ...u, MustChangePassword: false } : u));
  }
}
