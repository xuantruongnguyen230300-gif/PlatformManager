import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, finalize, map, tap } from 'rxjs';
import { IApiResult, unwrapData } from '../http/api-result.model';
import { ICurrentUser, ICurrentUserDto, mapCurrentUserDtoToModel } from './current-user.model';
import { IChangePasswordRequestDto, ILoginRequestDto } from './auth.model';
import { CurrentUserService } from './current-user.service';
import { MenuService } from '../menu/menu.service';
import { CsrfService } from '../http/csrf.service';

/**
 * Gọi API auth thật (`POST /api/auth/login|logout|change-password`) — xem doc/contracts/auth.md.
 * `CurrentUserService` không tự gọi HTTP cho các endpoint này, chỉ nhận lại kết quả qua
 * `setUser()`/`clear()`/`markPasswordChanged()` để giữ đúng ranh giới "context vs HTTP" đã chốt.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUser = inject(CurrentUserService);
  private readonly menu = inject(MenuService);
  private readonly csrf = inject(CsrfService);

  login(userName: string, password: string): Observable<ICurrentUser> {
    const body: ILoginRequestDto = { userName, password };
    return this.http.post<IApiResult<ICurrentUserDto>>('/auth/login', body).pipe(
      map((res) => mapCurrentUserDtoToModel(unwrapData(res))),
      tap((user) => {
        // Xoá cache menu của phiên CŨ trước khi gán user mới — nếu gán trước, một `getMenu()` kẹt
        // giữa 2 bước này có thể đọc nhầm cache còn khớp `SessionKey` cũ (xem menu.service.ts B3).
        this.menu.invalidate();
        this.currentUser.setUser(user);
        // Mồi lại cookie CSRF NGAY sau khi đăng nhập: token lấy lúc anonymous gắn với danh tính
        // (ClaimsPrincipal) tại thời điểm phát hành — dùng lại sau khi đổi danh tính (login) sẽ bị
        // 403 "meant for a different claims-based user" (xem doc/contracts/auth.md §CSRF). Không
        // mồi lại thì luồng bắt buộc đổi mật khẩu lần đầu (`mustChangePassword` →
        // `POST /auth/change-password`, áp dụng cho gần như mọi tài khoản mới) sẽ bị chặn CSRF
        // ngay sau khi vừa đăng nhập thành công. `primeToken()` tự nuốt lỗi, không cần xử lý gì
        // thêm ở đây.
        this.csrf.primeToken().subscribe();
      }),
    );
  }

  logout(): Observable<void> {
    return this.http.post<IApiResult<boolean>>('/auth/logout', {}).pipe(
      map(() => undefined),
      // `finalize()` — không phải `tap()`: phiên client phải bị xoá kể cả khi request lỗi (500) hoặc
      // bị huỷ giữa chừng (unsubscribe), không chỉ khi request thành công. Regression cũ: dùng
      // `tap()` khiến logout hỏng để lại phiên còn nguyên trong khi người dùng tin là đã thoát.
      finalize(() => {
        this.menu.invalidate();
        this.currentUser.clear();
      }),
    );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    const body: IChangePasswordRequestDto = { currentPassword, newPassword };
    return this.http.post<IApiResult<boolean>>('/auth/change-password', body).pipe(map(() => undefined));
  }
}
