import { Injectable, signal } from '@angular/core';

/**
 * State UI của sidebar shell (collapse desktop + drawer mobile) — singleton
 * dùng chung giữa `shared/components/sidebar` (chính nó) và
 * `shared/components/topbar` (nút hamburger mở drawer trên mobile). Thuộc
 * `shared/` vì là UI cross-cutting, không phải state nghiệp vụ của 1 feature.
 */
@Injectable({ providedIn: 'root' })
export class SidebarStateService {
  readonly collapsed = signal(false);
  readonly drawerOpen = signal(false);

  toggleCollapse(): void {
    this.collapsed.update((v) => !v);
  }

  openDrawer(): void {
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }
}
