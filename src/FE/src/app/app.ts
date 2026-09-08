import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, map } from 'rxjs';
import { Sidebar } from './shared/components/sidebar/sidebar';
import { Topbar } from './shared/components/topbar/topbar';
import { Toast } from './shared/components/toast/toast';
import { SidebarStateService } from './shared/services/sidebar-state.service';
import { PageTitleStrategy } from './core/title/page-title.strategy';
import { GlobalErrorHandler } from './core/errors/global-error.handler';

function leafRouteData(snapshot: ActivatedRouteSnapshot): Record<string, unknown> {
  let node = snapshot;
  while (node.firstChild) node = node.firstChild;
  return node.data;
}

/**
 * Root component = app shell (sidebar + topbar + `<router-outlet>` + toast). Route đánh dấu
 * `data: { noShell: true }` (`login`/`doi-mat-khau`, xem module tương ứng) render TRẦN
 * `<router-outlet>` — không sidebar/topbar, đúng doc/Design/Frontend/PlatformManager (không có
 * sidebar trước khi xác thực). Route còn lại (đã đăng nhập) hiển thị đủ shell như F0+F1.
 *
 * Hai thứ lấy từ route, hai nguồn khác nhau — KHÔNG gộp lại:
 * - **Tiêu đề topbar** đến từ `PageTitleStrategy` (`title` khai ở cấp `Route`). Đọc
 *   `data['title']` như bản trước sẽ luôn ra `undefined`: router resolve `title:` vào
 *   `snapshot.data` dưới một symbol key nội bộ, không phải chuỗi `'title'`.
 * - **Cờ `noShell`** vẫn đọc từ `data` của route lá, vì đó là cờ riêng của app này — router
 *   không có API cấp `Route` cho nó.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Sidebar, Topbar, Toast, TranslatePipe],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly router = inject(Router);
  protected readonly sidebarState = inject(SidebarStateService);

  private readonly routeEvents = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => leafRouteData(this.router.routerState.snapshot.root)),
    ),
    { initialValue: leafRouteData(this.router.routerState.snapshot.root) },
  );

  private readonly errorHandler = inject(GlobalErrorHandler);
  /**
   * Máy chủ đã đổi bản build dưới chân tab đang mở (xem `GlobalErrorHandler`). Dải mời tải lại
   * nằm ở đây — cấp app, NGOÀI `@if (showShell())` — vì ca này trúng cả màn đã đăng nhập lẫn màn
   * đăng nhập, và vì mọi route khác đã không còn tải được.
   */
  protected readonly newVersionAvailable = this.errorHandler.newVersionAvailable;

  protected readonly pageTitle = inject(PageTitleStrategy).pageTitle;
  protected readonly showShell = computed(() => this.routeEvents()['noShell'] !== true);

  /**
   * Skip link (WCAG 2.2 AA 2.4.1) — chuyển focus bằng chương trình thay vì để trình duyệt tự nhảy
   * theo `href="#main-content"`.
   *
   * `href` VẪN GIỮ, không thay bằng `<button>`: nó là thứ khiến phần tử được công bố là "liên kết
   * tới nội dung chính" và vẫn hoạt động cả khi handler này chưa gắn kịp. Nhưng để trình duyệt xử
   * lý thật thì URL bị thêm `#main-content` và Angular Router coi đó là một lượt điều hướng mới
   * (guard chạy lại, fragment dính lại trên thanh địa chỉ ở mọi trang) — không đáng cho một thao
   * tác thuần giao diện.
   */
  /** KHÔNG tự động gọi — người dùng phải tự bấm, xem `GlobalErrorHandler`. */
  protected reloadApp(): void {
    this.errorHandler.reloadApp();
  }

  protected focusMainContent(event: Event): void {
    const main = (event.target as HTMLElement).ownerDocument.getElementById('main-content');
    if (!main) return; // template đổi mà quên id — để trình duyệt xử lý theo href, còn hơn nuốt cú bấm
    event.preventDefault();
    main.focus();
    main.scrollIntoView();
  }
}
