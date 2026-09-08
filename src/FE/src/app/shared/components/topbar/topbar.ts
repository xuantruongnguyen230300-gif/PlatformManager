import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { SidebarStateService } from '../../services/sidebar-state.service';
import { CurrentUserService } from '../../../core/auth/current-user.service';
import { AuthService } from '../../../core/auth/auth.service';
import { CORE_ROUTES } from '../../../core/config/core-routes';

/**
 * Topbar cross-cutting — thuộc ngoại lệ "app-shell" (xem `Sidebar`), inject
 * `SidebarStateService` để mở drawer mobile qua nút hamburger. Tiêu đề trang (`title`) do `App`
 * lấy từ `PageTitleStrategy` (`title` khai ở cấp `Route` của từng feature route) rồi truyền
 * xuống qua `input()` — Topbar không tự biết route nào đang mở.
 *
 * F3: thêm khối user-info + đăng xuất — cùng thuộc ngoại lệ app-shell (đăng xuất là hành vi hạ
 * tầng toàn app, không phải nghiệp vụ riêng 1 feature).
 */
@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Topbar {
  readonly title = input.required<string>();
  protected readonly state = inject(SidebarStateService);
  protected readonly currentUser = inject(CurrentUserService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  private readonly routes = inject(CORE_ROUTES);

  /**
   * Đường dẫn màn đăng nhập đến từ `CORE_ROUTES`, KHÔNG khai cứng ở đây.
   *
   * `shared/` cũng thuộc phần tái dùng cho dự án khác nên chịu cùng luật với `core/`:
   * component giữ HÀNH VI (đăng xuất xong thì về màn đăng nhập), app cung cấp ĐƯỜNG DẪN.
   * Chốt 2026-09-02 — xem `doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ, dự án cung
   * cấp DỮ LIỆU".
   *
   * Chỗ này từng khai cứng `/dang-nhap` và LỌT QUA lượt tách đầu tiên vì phép kiểm chỉ
   * quét `core/`. Ghi lại để lần sau nhớ quét cả `shared/`.
   */
  onLogout(): void {
    this.authService.logout().subscribe(() => this.router.navigateByUrl(this.routes.signIn));
  }
}
