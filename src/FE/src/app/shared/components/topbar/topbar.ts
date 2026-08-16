import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { SidebarStateService } from '../../services/sidebar-state.service';
import { CurrentUserService } from '../../../core/auth/current-user.service';
import { AuthService } from '../../../core/auth/auth.service';

/**
 * Topbar cross-cutting — thuộc ngoại lệ "app-shell" (xem `Sidebar`), inject
 * `SidebarStateService` để mở drawer mobile qua nút hamburger. Tiêu đề trang (`title`) do `App`
 * suy ra từ route data (`data: { title: '...' }` của từng feature route) rồi truyền xuống qua
 * `input()` — Topbar không tự biết route nào đang mở.
 *
 * F3: thêm khối user-info + đăng xuất — cùng thuộc ngoại lệ app-shell (đăng xuất là hành vi hạ
 * tầng toàn app, không phải nghiệp vụ riêng 1 feature).
 */
@Component({
  selector: 'app-topbar',
  standalone: true,
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

  onLogout(): void {
    this.authService.logout().subscribe(() => this.router.navigateByUrl('/dang-nhap'));
  }
}
