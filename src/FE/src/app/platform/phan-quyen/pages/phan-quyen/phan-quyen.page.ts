import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { PhanQuyenService } from '../../services/phan-quyen.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { MenuService } from '../../../../core/menu/menu.service';
import { IPermissionRow } from '../../models/phan-quyen.model';
import { PermissionMatrix } from '../../components/permission-matrix/permission-matrix';

/**
 * SMART — route `/quan-tri/phan-quyen`. Gate `authGuard`+`superAdminGuard` (CHỈ SuperAdmin, xem
 * doc/contracts/permissions.md). Tick/bỏ tick chỉ đổi state cục bộ (`rows`), bấm "Lưu thay đổi"
 * mới gọi API — gửi ĐỦ toàn bộ `rows` hiện có (không chỉ dòng vừa đổi).
 */
@Component({
  selector: 'app-phan-quyen-page',
  standalone: true,
  imports: [PermissionMatrix],
  templateUrl: './phan-quyen.page.html',
  styleUrl: './phan-quyen.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhanQuyenPage {
  private readonly service = inject(PhanQuyenService);
  private readonly toast = inject(ToastService);
  private readonly menu = inject(MenuService);

  protected readonly roles = signal<string[]>([]);
  protected readonly rows = signal<IPermissionRow[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly dirty = signal(false);

  constructor() {
    this.service.getMatrix().subscribe({
      next: (matrix) => {
        this.roles.set(matrix.Roles);
        this.rows.set(matrix.Rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onToggle(event: { SysMenuId: string; Role: string }): void {
    this.rows.update((rows) =>
      rows.map((row) => {
        if (row.SysMenuId !== event.SysMenuId) return row;
        const has = row.AssignedRoles.includes(event.Role);
        return {
          ...row,
          AssignedRoles: has ? row.AssignedRoles.filter((r) => r !== event.Role) : [...row.AssignedRoles, event.Role],
        };
      }),
    );
    this.dirty.set(true);
  }

  onSave(): void {
    this.saving.set(true);
    this.service.saveMatrix(this.rows()).subscribe({
      next: () => {
        this.saving.set(false);
        this.dirty.set(false);
        this.toast.success('Đã lưu thay đổi phân quyền.');
        // `PUT` vừa đổi chính `SysMenuRole` mà sidebar render — ép tải lại ngay để nav rail phản
        // ánh đúng quyền mới, không bắt người quản trị F5 (Normalize on redesign #6, 04-phan-quyen.md).
        this.menu.refresh().subscribe();
      },
      error: () => this.saving.set(false),
    });
  }
}
