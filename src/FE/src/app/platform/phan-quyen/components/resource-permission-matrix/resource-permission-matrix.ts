import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
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
  imports: [TranslatePipe],
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

  /**
   * KHOÁ DỊCH cho `aria-label` của ô tick — template dịch bằng `| translate` và tự truyền tham số
   * (`resource`, `role`), xem `resource-permission-matrix.html`.
   *
   * Trả khoá chứ không trả câu vì component này nằm trong `components/`, tức DUMB: LUẬT G4 cấm
   * inject service dữ liệu ở đây (doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md — luật đã
   * chốt, nhưng cổng TỰ ĐỘNG cho nó xếp lịch "Sau F4" và hôm nay CHƯA có, nên tuân thủ là việc
   * của người viết code chứ không có máy canh).
   *
   * 🛑 HAI khoá TRỌN CÂU thay vì một câu gốc nối thêm một đuôi. Đây là chuỗi CHỈ trình đọc màn
   * hình đọc, nên nó là chỗ dễ để lọt tiếng Việt nhất trong cả màn: không ai nhìn thấy nó sai khi
   * thử app bằng mắt ở bản tiếng Anh.
   */
  cellAriaLabelKey(role: string): string {
    return this.isAlwaysAllowed(role)
      ? 'phan-quyen.grid.cellAriaLabelAlwaysAllowed'
      : 'phan-quyen.grid.cellAriaLabel';
  }
}
