import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { SidebarStateService } from '../../services/sidebar-state.service';

/**
 * Sidebar shell (menu điều hướng trái) — port nguyên hành vi collapse (desktop)
 * + drawer (mobile <980px) từ `doc/Prototype/dashboard.html`/`danh-muc-dti.html`.
 * Dùng chung > 1 feature (Dashboard, Danh mục > DTI) nên thuộc `shared/`, không
 * phải của riêng 1 module. Chỉ giữ UI state cục bộ (submenu mở) + state chia sẻ
 * qua `SidebarStateService` (collapsed/drawer) — không inject data service nào,
 * không có logic nghiệp vụ.
 */
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss'
})
export class Sidebar {
  private readonly sidebarState = inject(SidebarStateService);

  readonly collapsed = this.sidebarState.collapsed;
  readonly drawerOpen = this.sidebarState.drawerOpen;
  readonly danhMucOpen = signal(true);

  toggleCollapse(): void {
    this.sidebarState.toggleCollapse();
  }

  closeDrawer(): void {
    this.sidebarState.closeDrawer();
  }

  toggleDanhMuc(): void {
    this.danhMucOpen.update((v) => !v);
  }

  onNavItemClick(): void {
    this.closeDrawer();
  }
}
