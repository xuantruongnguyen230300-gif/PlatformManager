import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IPermissionRow } from '../../models/phan-quyen.model';

interface IPermissionDisplayRow extends IPermissionRow {
  Indent: boolean;
}

/** Sắp cha trước, con ngay sau cha (dựa `ParentId`) — DTO không có `displayOrder`, giữ nguyên thứ tự BE trả trong từng nhóm. */
function toDisplayOrder(rows: IPermissionRow[]): IPermissionDisplayRow[] {
  const byParent = new Map<string | null, IPermissionRow[]>();
  for (const row of rows) {
    const list = byParent.get(row.ParentId) ?? [];
    list.push(row);
    byParent.set(row.ParentId, list);
  }

  const result: IPermissionDisplayRow[] = [];
  const visit = (parentId: string | null, indent: boolean): void => {
    for (const row of byParent.get(parentId) ?? []) {
      result.push({ ...row, Indent: indent });
      visit(row.SysMenuId, true);
    }
  };
  visit(null, false);
  return result;
}

/**
 * Ma trận checkbox Role × SysMenu — hàng = `rows` (dựng cây cha/con qua `ParentId`), cột =
 * `roles`. Dumb — không tự gọi service, chỉ phát `permissionToggle()`; "Lưu thay đổi" thuộc về
 * `PhanQuyenPage` (smart), xem doc/ke-hoach-xay-lai-corebase.md §"Màn hình mới: Phân quyền".
 */
@Component({
  selector: 'app-permission-matrix',
  standalone: true,
  templateUrl: './permission-matrix.html',
  styleUrl: './permission-matrix.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PermissionMatrix {
  readonly rows = input.required<IPermissionRow[]>();
  readonly roles = input.required<string[]>();
  readonly loading = input<boolean>(false);

  // Không đặt tên `toggle` — trùng tên native DOM event (`<details>`/`<dialog>`), vi phạm
  // `@angular-eslint/no-output-native`.
  readonly permissionToggle = output<{ SysMenuId: string; Role: string }>();

  protected readonly displayRows = computed(() => toDisplayOrder(this.rows()));

  isChecked(row: IPermissionRow, role: string): boolean {
    return row.AssignedRoles.includes(role);
  }
}
