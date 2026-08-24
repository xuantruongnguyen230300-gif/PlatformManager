import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IResourcePermissionRow } from '../../models/phan-quyen.model';

/**
 * Role được BE miễn kiểm quyền ở tầng `[RequirePermission]` ("break-glass", từ 2026-08-19 —
 * `RequirePermissionFilter` + doc/contracts/permissions.md dòng 122-133). Hệ quả cho MÀN NÀY: bỏ
 * tick cột đó rồi bấm lưu sẽ báo thành công nhưng KHÔNG thu hồi được gì, nên ô phải khoá lại thay
 * vì để người quản trị tin nhầm là đã thu quyền.
 *
 * ⚠️ CHỈ đúng cho ma trận PERM-2 (tài nguyên). Ma trận PERM-1 (`PermissionMatrix`, menu) ghi
 * `SysMenuRole` — cơ chế khác hẳn, KHÔNG có bypass nào, bỏ tick ở đó VẪN có tác dụng thật. Đừng
 * "gom chung cho nhất quán": copy hằng này sang PermissionMatrix là tạo ra đúng loại lỗi "UI nói
 * sai về quyền" theo chiều ngược lại.
 */
export const ALWAYS_ALLOWED_ROLE = 'SuperAdmin';

/**
 * Ma trận checkbox Role × resource-key — DANH SÁCH PHẲNG (khác `PermissionMatrix`, không có khái
 * niệm cha/con qua `ParentId`), xem doc/contracts/permissions.md CONTRACT PERM-2. Dumb — không tự
 * gọi service, chỉ phát `permissionToggle()`; "Lưu thay đổi" thuộc về `PhanQuyenPage` (smart).
 *
 * Cột `ALWAYS_ALLOWED_ROLE` hiển thị đã tick + disabled: đây là việc NÓI ĐÚNG trạng thái quyền cho
 * người quản trị, KHÔNG phải một lớp chặn — FE không bao giờ là ranh giới bảo mật, việc chặn nằm ở
 * BE. Payload gửi lên vẫn giữ nguyên `AssignedRoles` mà BE trả về (không tự thêm/bớt role) nên
 * contract PERM-2 không đổi.
 */
@Component({
  selector: 'app-resource-permission-matrix',
  standalone: true,
  templateUrl: './resource-permission-matrix.html',
  styleUrl: './resource-permission-matrix.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourcePermissionMatrix {
  readonly rows = input.required<IResourcePermissionRow[]>();
  readonly roles = input.required<string[]>();
  readonly loading = input<boolean>(false);

  // Không đặt tên `toggle` — trùng tên native DOM event, vi phạm `@angular-eslint/no-output-native`.
  readonly permissionToggle = output<{ ResourceKey: string; Role: string }>();

  protected readonly alwaysAllowedRole = ALWAYS_ALLOWED_ROLE;

  /** Chỉ hiện chú thích khi cột đó thật sự có trên màn — BE quyết định danh sách `roles`. */
  protected readonly hasAlwaysAllowedRole = computed(() =>
    this.roles().includes(ALWAYS_ALLOWED_ROLE),
  );

  isAlwaysAllowed(role: string): boolean {
    return role === ALWAYS_ALLOWED_ROLE;
  }

  isChecked(row: IResourcePermissionRow, role: string): boolean {
    // Tick cứng cho role break-glass: nó có quyền trên thực tế dù bảng `RolePermissions` có dòng
    // tương ứng hay không (`SuperAdmin` không cần dòng nào — permissions.md dòng 128).
    return this.isAlwaysAllowed(role) || row.AssignedRoles.includes(role);
  }

  isDisabled(role: string): boolean {
    return this.loading() || this.isAlwaysAllowed(role);
  }

  cellAriaLabel(row: IResourcePermissionRow, role: string): string {
    const base = `${row.ResourceName} — ${role}`;
    return this.isAlwaysAllowed(role) ? `${base} — luôn có quyền, không thay đổi được` : base;
  }
}
