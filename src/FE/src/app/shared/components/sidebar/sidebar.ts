import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, map } from 'rxjs';
import { MenuService } from '../../../core/menu/menu.service';
import { SidebarStateService } from '../../services/sidebar-state.service';
import { IMenuItem } from '../../../core/menu/menu-item.model';
import { CORE_BRANDING } from '../../../core/config/core-branding';

/** BE trả thẳng class PrimeIcons thật (`pi-th-large`, `pi-folder`...) trong field `icon` — xem
 * doc/contracts/meta-menu.md §Icon. KHÔNG còn map qua bảng khoá trừu tượng (đã bỏ
 * `menu-icon.util.ts`) — chỉ fallback an toàn khi BE trả `null`. */
const FALLBACK_ICON_CLASS = 'pi-circle';

/**
 * Sidebar điều hướng toàn app — thuộc ngoại lệ "app-shell" đã ghi nhận
 * (doc/huong_dan/wiki-core/fe/05-component-library.md), được phép inject `MenuService`/
 * `SidebarStateService` dù nằm trong `shared/components/`. Menu tải ĐỘNG qua `GET /api/meta/menu`
 * — KHÔNG hard-code danh sách item (khác code cũ đã xoá). Hành vi/breakpoint theo
 * doc/Design/Frontend/PlatformManager/Components/Sidebar.md (§Anatomy, §Variants, §States).
 * Sửa 2026-09-08: trước trỏ một ui-spec sidebar-menu nay đã bị xoá.
 */
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
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

  /**
   * Tên sản phẩm cho ô thương hiệu ở đầu sidebar — đến từ `CORE_BRANDING`, không viết cứng trong
   * template. `shared/` chịu cùng luật với `core/`: component giữ CHỖ ĐẶT và cách hiển thị (ô
   * vuông chữ tắt + dòng chữ đầy đủ, dòng chữ ẩn khi thu gọn), app cấp CHUỖI. Chốt 2026-09-02 —
   * `doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU".
   *
   * Đúng chỗ mà lượt tách `CORE_ROUTES` từng bỏ sót vì chỉ quét `core/`; lần này quét cả template
   * của `shared/`, nơi dữ liệu dự án dễ nấp nhất — nó không phải là code.
   */
  protected readonly branding = inject(CORE_BRANDING);

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
    // `error` handler giữ lại làm lưới cuối, KHÔNG phải đường lùi chính: từ 2026-09-08
    // `MenuService.getMenu()` tự toast một lần rồi trả `[]` nên nhánh này thực tế không còn được
    // gọi (xem khối §"Đường lùi" ở `core/menu/menu.service.ts`). Giữ vì nó rẻ và vì một lỗi đồng
    // bộ ném ra trước khi vào pipe vẫn sẽ rơi vào đây — bỏ đi thì thành unhandled rejection.
    this.menuService.getMenu().subscribe({
      next: (items) => this.onMenuLoaded(items),
      error: () => this.onMenuLoaded([]),
    });
  }

  private onMenuLoaded(items: IMenuItem[]): void {
    this.items.set(items);
    // Accordion mặc định MỞ: mọi nhóm có con đều bung sẵn ở lần tải đầu, người dùng thu lại
    // thủ công nếu muốn.
    //
    // 🛑 Quy tắc này hiện KHÔNG có nguồn tài liệu nào chống lưng. Nó từng trỏ mục 2.5 của một
    // ui-spec sidebar-menu nay đã bị xoá, và
    // doc/Design/Frontend/PlatformManager/Components/Sidebar.md — nguồn giao diện duy nhất theo
    // .claude/CLAUDE.md §7 — chỉ mô tả TRẠNG THÁI mở/đóng trông ra sao (class `.open`, chevron
    // xoay, `display:none` khi đóng), không nói nhóm mặc định mở hay đóng. Đối chiếu 2026-09-08.
    // Đừng đổi hành vi này theo cảm tính: hỏi người dùng rồi ghi chốt vào Sidebar.md trước.
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
