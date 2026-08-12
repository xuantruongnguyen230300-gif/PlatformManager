import { Component, inject, input } from '@angular/core';

import { SidebarStateService } from '../../services/sidebar-state.service';

/**
 * Topbar dùng chung cho mọi trang (Dashboard, Danh mục > DTI) — chỉ hiện
 * hamburger (mobile, mở drawer sidebar) + tiêu đề/phụ đề trang. Dumb component:
 * nhận `title`/`subtitle` qua `input()`, không có action nào khác trong topbar
 * theo đúng 2 prototype (toolbar hành động nằm trong `main`, không phải topbar).
 */
@Component({
  selector: 'app-topbar',
  standalone: true,
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss'
})
export class Topbar {
  private readonly sidebarState = inject(SidebarStateService);

  readonly title = input.required<string>();
  readonly subtitle = input<string>('');

  openDrawer(): void {
    this.sidebarState.openDrawer();
  }
}
