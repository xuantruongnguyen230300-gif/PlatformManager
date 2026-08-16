import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter, map } from 'rxjs';
import { MenuService } from '../../services/menu.service';
import { SidebarStateService } from '../../services/sidebar-state.service';
import { IMenuItem } from '../../models/menu-item.model';

/** BE trả thẳng class PrimeIcons thật (`pi-th-large`, `pi-folder`...) trong field `icon` — xem
 * doc/contracts/meta-menu.md §Icon. KHÔNG còn map qua bảng khoá trừu tượng (đã bỏ
 * `menu-icon.util.ts`) — chỉ fallback an toàn khi BE trả `null`. */
const FALLBACK_ICON_CLASS = 'pi-circle';

/**
 * Sidebar điều hướng toàn app — thuộc ngoại lệ "app-shell" đã ghi nhận
 * (doc/huong_dan/wiki-core/fe/05-component-library.md), được phép inject `MenuService`/
 * `SidebarStateService` dù nằm trong `shared/components/`. Menu tải ĐỘNG qua `GET /api/meta/menu`
 * — KHÔNG hard-code danh sách item (khác code cũ đã xoá). Hành vi/breakpoint theo
 * spec/sidebar-menu/ui-spec.md.
 */
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class Sidebar {
  private readonly menuService = inject(MenuService);
  private readonly router = inject(Router);
  protected readonly state = inject(SidebarStateService);

  protected iconClass(icon: string | null): string {
    return icon ?? FALLBACK_ICON_CLASS;
  }

  protected readonly items = signal<IMenuItem[]>([]);
  protected readonly openGroups = signal<Record<string, boolean>>({});

  protected readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  constructor() {
    // `error` handler: BE meta/menu có thể chưa sẵn sàng (chưa scaffold Infrastructure lúc code F1
    // này) — tránh unhandled rejection, sidebar chỉ hiện rỗng thay vì crash. `httpErrorInterceptor`
    // đã lo phần toast báo lỗi chung.
    this.menuService.getMenu().subscribe({
      next: (items) => this.onMenuLoaded(items),
      error: () => this.onMenuLoaded([]),
    });
  }

  private onMenuLoaded(items: IMenuItem[]): void {
    this.items.set(items);
    // Accordion mặc định MỞ — spec/sidebar-menu/ui-spec.md mục 2.5.
    const open: Record<string, boolean> = {};
    for (const item of items) {
      if (item.Children.length > 0) open[item.Id] = true;
    }
    this.openGroups.set(open);
  }

  isGroupOpen(id: string): boolean {
    return this.openGroups()[id] ?? true;
  }

  toggleGroup(id: string): void {
    this.openGroups.update((groups) => ({ ...groups, [id]: !this.isGroupOpen(id) }));
  }

  isGroupActive(item: IMenuItem): boolean {
    const url = this.currentUrl();
    return item.Children.some((child) => !!child.Route && url.startsWith(child.Route));
  }

  onNavItemClick(): void {
    // Bấm 1 nav item LÁ đóng drawer mobile; nav item CHA (toggleGroup) không gọi hàm này — 2 hành
    // vi khác nhau, xem spec mục 3.
    this.state.closeDrawer();
  }

  onEscape(): void {
    this.state.closeDrawer();
  }
}
