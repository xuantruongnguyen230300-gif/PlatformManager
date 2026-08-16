import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { Sidebar } from './shared/components/sidebar/sidebar';
import { Topbar } from './shared/components/topbar/topbar';
import { Toast } from './shared/components/toast/toast';
import { SidebarStateService } from './shared/services/sidebar-state.service';

const DEFAULT_TITLE = 'PlatformManager';

function leafRouteData(snapshot: ActivatedRouteSnapshot): Record<string, unknown> {
  let node = snapshot;
  while (node.firstChild) node = node.firstChild;
  return node.data;
}

/**
 * Root component = app shell (sidebar + topbar + `<router-outlet>` + toast). Route đánh dấu
 * `data: { noShell: true }` (`login`/`doi-mat-khau`, xem module tương ứng) render TRẦN
 * `<router-outlet>` — không sidebar/topbar, đúng `doc/Prototype/login.html` (không có sidebar
 * trước khi xác thực). Route còn lại (đã đăng nhập) hiển thị đủ shell như F0+F1.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Sidebar, Topbar, Toast],
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

  protected readonly pageTitle = computed(() => (this.routeEvents()['title'] as string | undefined) ?? DEFAULT_TITLE);
  protected readonly showShell = computed(() => this.routeEvents()['noShell'] !== true);
}
