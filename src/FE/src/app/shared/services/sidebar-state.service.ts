import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Khoá `localStorage` giữ trạng thái thu gọn sidebar.
 *
 * 🛑 Tên khoá TRUNG TÍNH có chủ đích. Bản trước có dạng `<ten_san_pham>_sidebar_collapsed_v1`,
 * tức tên sản phẩm nằm ngay trong một tầng (`shared/`) vốn là CoreBase dùng lại cho sản phẩm
 * khác (doc/kien-truc-core-module.md). Nó sống sót qua lượt tách 2026-09-02 nhờ viết snake_case
 * chữ thường: phép kiểm hôm đó tìm tên sản phẩm viết liền kiểu Pascal, nên khoá này không khớp
 * mẫu nào. Phép kiểm nay bắt cả hai cách viết — xem lệnh grep ở doc/kien-truc-core-module.md.
 *
 * Hệ quả đã biết của việc đổi khoá (2026-09-03): người dùng đang có sidebar thu gọn sẽ thấy nó
 * MỞ lại đúng một lần, vì giá trị cũ nằm dưới khoá cũ và không ai đọc nữa. Đây là hành vi đã
 * lường trước, KHÔNG phải lỗi — không viết code di trú cho một cờ boolean mà người dùng bấm lại
 * mất một giây. Khoá cũ tự biến mất khi trình duyệt dọn storage.
 *
 * Hậu tố `.v1` giữ nguyên vai trò cũ: đổi Ý NGHĨA của giá trị (vd sang enum 3 trạng thái) thì
 * tăng lên `.v2` thay vì đọc nhầm dữ liệu cũ theo luật mới.
 */
const COLLAPSED_STORAGE_KEY = 'core.sidebar.collapsed.v1';

/**
 * State hạ tầng UI singleton cho shell (sidebar + topbar) — ngoại lệ "app-shell" đã ghi nhận
 * (doc/huong_dan/wiki-core/fe/05-component-library.md), cho phép cả `Sidebar` lẫn `Topbar`
 * (2 component anh em, không cha-con) cùng đọc/ghi qua đúng 1 service thay vì nhét state vào
 * `App` rồi truyền input()/output() qua nhiều tầng. Xem hành vi ở
 * doc/Design/Frontend/PlatformManager/Components/Sidebar.md §Variants — dòng `Collapsed rail`
 * (thu gọn trên desktop, kèm khoá localStorage `core.sidebar.collapsed.v1`) và hai dòng
 * `Off-canvas drawer` (tablet ≤980px, mobile ≤560px).
 * Sửa 2026-09-08: trước trỏ §2.2/§3 của một ui-spec sidebar-menu nay đã bị xoá.
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
