import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const COLLAPSED_STORAGE_KEY = 'platform_manager_sidebar_collapsed_v1';

/**
 * State hạ tầng UI singleton cho shell (sidebar + topbar) — ngoại lệ "app-shell" đã ghi nhận
 * (doc/huong_dan/wiki-core/fe/05-component-library.md), cho phép cả `Sidebar` lẫn `Topbar`
 * (2 component anh em, không cha-con) cùng đọc/ghi qua đúng 1 service thay vì nhét state vào
 * `App` rồi truyền input()/output() qua nhiều tầng. Xem hành vi gốc ở
 * spec/sidebar-menu/ui-spec.md §2.2 (collapse desktop) và §3 (drawer mobile).
 */
@Injectable({ providedIn: 'root' })
export class SidebarStateService {
  private readonly platformId = inject(PLATFORM_ID);

  readonly collapsed = signal(this.readCollapsedFromStorage());
  readonly mobileOpen = signal(false);

  toggleCollapse(): void {
    this.collapsed.update((v) => {
      const next = !v;
      this.writeCollapsedToStorage(next);
      return next;
    });
  }

  openDrawer(): void {
    this.mobileOpen.set(true);
  }

  closeDrawer(): void {
    this.mobileOpen.set(false);
  }

  toggleDrawer(): void {
    this.mobileOpen.update((v) => !v);
  }

  private readCollapsedFromStorage(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    try {
      return localStorage.getItem(COLLAPSED_STORAGE_KEY) === '1';
    } catch {
      return false;
    }
  }

  private writeCollapsedToStorage(value: boolean): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(COLLAPSED_STORAGE_KEY, value ? '1' : '0');
    } catch {
      // localStorage không khả dụng (vd private mode chặn) — bỏ qua, chỉ mất persist qua session.
    }
  }
}
