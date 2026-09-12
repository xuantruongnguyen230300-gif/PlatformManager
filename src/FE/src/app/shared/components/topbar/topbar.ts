import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
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
 *
 * ## Vì sao `Đổi mật khẩu` nằm ở ĐÂY (2026-09-11)
 *
 * Trước lượt này, lối vào **tự nguyện** của màn đổi mật khẩu là một `<a class="btn">` trên trang
 * chủ cũ. Lượt hoán đổi `/trang-chu` (Q3) xoá `platform/trang-chu/`, và cùng với nó là lối vào duy
 * nhất đó — luồng **ép buộc** (`mustChangePasswordGuard`) vẫn chạy, nhưng người đã đổi mật khẩu
 * rồi mà muốn tự đổi lại thì không còn đường nào ngoài gõ thẳng URL. Không có mục menu nào thay
 * thế: menu do BE cấp và `AppMenuSeedSource` không seed dòng nào cho nó.
 *
 * Topbar là chỗ đúng vì nó đã là **vùng tài khoản** của app-shell: nó đã render trên mọi route có
 * shell, đã gác sau `isAuthenticated()`, và đã chứa hành động tài khoản kia (đăng xuất). Đặt vào
 * một module nghiệp vụ thì một màn Core sẽ phụ thuộc `modules/`; đặt vào Sidebar thì phải seed
 * thêm ở BE cho một thứ không phải chức năng nghiệp vụ.
 *
 * 🛑 Nút này **không** tự link tới chính nó: `/doi-mat-khau` khai `data.noShell = true` nên topbar
 * không render ở đó.
 */
@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
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

  /**
   * `protected` chứ không `private`: template đọc `routes.changePassword` cho `routerLink`.
   *
   * Hai đường dẫn (`signIn`, `changePassword`) đều đến từ `CORE_ROUTES`, KHÔNG khai cứng — xem
   * `onLogout()` ngay dưới để biết vì sao luật đó áp cho cả `shared/`, không chỉ `core/`.
   */
  protected readonly routes = inject(CORE_ROUTES);

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
