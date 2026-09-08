import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, HostListener, computed, inject, signal, viewChild } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subject } from 'rxjs';
import { PhanQuyenService } from '../../services/phan-quyen.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { MenuService } from '../../../../core/menu/menu.service';
import { IHasUnsavedChanges } from '../../../../core/guards/unsaved-changes.guard';
import { IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { IPermissionRow, IResourcePermissionRow } from '../../models/phan-quyen.model';
import { PermissionMatrix } from '../../components/permission-matrix/permission-matrix';
import { ResourcePermissionMatrix } from '../../components/resource-permission-matrix/resource-permission-matrix';
import { ConfirmDialog } from '../../../../shared/components/confirm-dialog/confirm-dialog';

/** Hai ma trận độc lập trên cùng route — xem doc/contracts/permissions.md PERM-1 và PERM-2. */
export type PhanQuyenTab = 'menu' | 'resource';

/**
 * `businessCode` BE trả khi `version` gửi lên không khớp trạng thái DB — khai ở
 * `src/BE/Core/PlatformManager.Core.Application/Permissions/PermissionErrors.cs`. Khớp theo MÃ chứ
 * không theo câu `message`: mã là phần ổn định của hợp đồng, câu chữ thì sửa lúc nào cũng được.
 */
const VERSION_CONFLICT_CODE = 'PERMISSION.VERSION_CONFLICT';

/**
 * KHOÁ DỊCH của câu cho ca 409. Câu đó phải nói đủ BA điều mà một toast lỗi chung chung không nói:
 * chuyện gì đã xảy ra (người khác vừa lưu), thay đổi của mình ĐANG Ở ĐÂU (chưa lưu), và làm gì
 * tiếp (tải lại, kèm hệ quả là mất phần vừa tick). Thiếu vế cuối thì nút "Tải lại" bên cạnh trở
 * thành bẫy — ba vế đó nay nằm trong `public/i18n/<mã>.json`, không trong file này.
 */
const CONFLICT_TEXT_KEY = 'phan-quyen.error.versionConflict';

/**
 * Ca hiếm hơn nhưng cùng lối thoát: `GET` gần nhất hỏng nên không có token phiên bản nào để gửi.
 * Lưu lúc này là ghi đè lên một trạng thái mình chưa đọc được — đúng thứ `version` sinh ra để chặn.
 */
const NO_VERSION_TEXT_KEY = 'phan-quyen.error.noVersion';

/**
 * Lỗi vừa nhận có phải tranh chấp ghi không. Nhận theo `businessCode` là chính; `status === 409`
 * là lưới hứng cho ca envelope bị một lớp trước app (proxy/WAF) thay mất — với hai endpoint này
 * 409 chỉ có đúng một nguyên nhân, xem `PermissionErrors` (chỉ khai duy nhất `VersionConflict`).
 *
 * `export` để test khoá được CẢ HAI nhánh: `apiResult` do `httpErrorInterceptor` gắn vào lúc chạy
 * thật, nhưng TestBed của trang không cài interceptor nên qua `HttpTestingController` chỉ nhánh
 * `status` chạy — đúng khoảng mù đã để lọt chính lỗi này.
 */
export function isVersionConflict(err: HttpErrorResponse & IHttpErrorWithApiResult): boolean {
  return err.apiResult?.businessCode === VERSION_CONFLICT_CODE || err.status === 409;
}

/**
 * SMART — route `/quan-tri/phan-quyen`. Gate `authGuard`+`mustChangePasswordGuard`+
 * `superAdminGuard` (CHỈ SuperAdmin, xem doc/contracts/permissions.md).
 *
 * MỘT trang, HAI ma trận tách bạch hoàn toàn (state riêng, nút lưu riêng, endpoint riêng):
 * - Tab "Theo màn hình" — PERM-1, ghi `SysMenuRole`: role nào THẤY menu nào. Vắng mặt = mở cho
 *   mọi user đã đăng nhập.
 * - Tab "Theo tài nguyên" — PERM-2, ghi `RolePermission`: role nào GỌI được hành động nào. Vắng
 *   mặt = TỪ CHỐI.
 * Ngữ nghĩa mặc định ngược nhau, nên KHÔNG có nút "Lưu tất cả" và không dùng chung `dirty`: bấm
 * lưu ở tab này không bao giờ đụng tới bảng của tab kia.
 *
 * Tick/bỏ tick chỉ đổi state cục bộ; bấm "Lưu thay đổi" mới gọi API — và gửi ĐỦ toàn bộ `rows`
 * hiện có (không chỉ dòng vừa đổi) vì cả hai endpoint đều GHI ĐÈ TOÀN BỘ.
 */
@Component({
  selector: 'app-phan-quyen-page',
  standalone: true,
  imports: [PermissionMatrix, ResourcePermissionMatrix, ConfirmDialog, TranslatePipe],
  templateUrl: './phan-quyen.page.html',
  // Xem ghi chú cùng tên ở quan-tri-nguoi-dung.page.ts: `page-fill` đặt lên chính thẻ host để
  // ma trận cao bằng chỗ còn lại của màn hình và cuộn bên trong, thay vì co theo số dòng.
  host: { class: 'page-fill' },
  styleUrl: './phan-quyen.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhanQuyenPage implements IHasUnsavedChanges {
  private readonly service = inject(PhanQuyenService);
  private readonly toast = inject(ToastService);
  private readonly menu = inject(MenuService);
  private readonly translate = inject(TranslateService);

  protected readonly tab = signal<PhanQuyenTab>('menu');

  // ===== PERM-1 — ma trận menu =====
  protected readonly roles = signal<string[]>([]);
  protected readonly rows = signal<IPermissionRow[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly dirty = signal(false);
  /**
   * Token phiên bản của lần `GET` GẦN NHẤT — gửi lại nguyên văn ở `PUT`. `null` = chưa/không đọc
   * được, và khi đó KHÔNG được lưu (xem `onSave`). Không bao giờ tự sinh giá trị cho nó.
   */
  private readonly version = signal<string | null>(null);
  /**
   * KHOÁ DỊCH của cảnh báo "phải tải lại" đang hiện, `null` = không có. Xem `CONFLICT_TEXT_KEY`.
   *
   * Giữ KHOÁ chứ không giữ câu: dải cảnh báo này Ở LẠI trên màn hình cho tới khi người dùng tải
   * lại — đúng khoảng thời gian dài nhất để họ kịp bấm đổi ngôn ngữ. Template dịch bằng
   * `| translate` nên câu đổi cùng phần còn lại của giao diện.
   */
  protected readonly conflict = signal<string | null>(null);

  // ===== PERM-2 — ma trận tài nguyên =====
  protected readonly resourceRoles = signal<string[]>([]);
  protected readonly resourceRows = signal<IResourcePermissionRow[]>([]);
  protected readonly resourceLoading = signal(false);
  protected readonly resourceSaving = signal(false);
  protected readonly resourceDirty = signal(false);
  /** Token phiên bản RIÊNG của PERM-2 — xem `version`. Hai tab không dùng chung token. */
  private readonly resourceVersion = signal<string | null>(null);
  /** KHOÁ DỊCH — xem `conflict`. */
  protected readonly resourceConflict = signal<string | null>(null);
  /** Chặn gọi lại `GET` mỗi lần đổi tab — chỉ tải lần đầu tab được mở. */
  private resourceRequested = false;

  constructor() {
    this.loadMatrix();
  }

  /**
   * Tải (hoặc TẢI LẠI) ma trận menu. Dùng cho cả ba đường vào: mở trang, bấm "Tải lại" ở dải cảnh
   * báo xung đột, và làm mới sau mỗi lần lưu thành công.
   *
   * Đặt lại `dirty` về `false` là đúng chứ không phải tiện tay: hàm này thay TOÀN BỘ `rows` bằng
   * dữ liệu server, nên mọi tick chưa lưu đã biến mất — giữ `dirty = true` sau đó là nói dối về
   * thứ đang có trong bộ nhớ, và sẽ bật lại nút Lưu cho một ma trận y hệt server.
   */
  protected loadMatrix(): void {
    this.loading.set(true);
    this.service.getMatrix().subscribe({
      next: (matrix) => {
        this.roles.set(matrix.Roles);
        this.rows.set(matrix.Rows);
        this.version.set(matrix.Version);
        this.dirty.set(false);
        this.conflict.set(null);
        this.loading.set(false);
      },
      error: () => {
        // XOÁ token cũ, không giữ lại: nó mô tả một trạng thái ta không còn chắc là đúng. Gửi lại
        // token cũ sau một lần đọc hỏng là ghi đè lên thứ mình chưa đọc được — đúng kịch bản
        // `version` sinh ra để chặn. `httpErrorInterceptor` đã hiện toast nên không báo thêm.
        this.version.set(null);
        this.loading.set(false);
      },
    });
  }

  /**
   * Ma trận tài nguyên tải LƯỜI (lần đầu mở tab) chứ không nạp sẵn cùng trang: phần lớn lượt vào
   * màn này là để sửa menu, và `GET /resources` đọc bảng `RolePermissions` — bảng mới, có thể chưa
   * migrate ở môi trường cũ. Nạp sẵn sẽ bắn một toast lỗi cho người dùng chưa hề mở tab đó.
   */
  onSelectTab(tab: PhanQuyenTab): void {
    this.tab.set(tab);
    if (tab !== 'resource' || this.resourceRequested) return;

    this.resourceRequested = true;
    this.loadResourceMatrix();
  }

  /** Xem `loadMatrix()` — bản PERM-2, cùng ba đường vào và cùng lý do đặt lại `resourceDirty`. */
  protected loadResourceMatrix(): void {
    this.resourceRequested = true;
    this.resourceLoading.set(true);
    this.service.getResourceMatrix().subscribe({
      next: (matrix) => {
        this.resourceRoles.set(matrix.Roles);
        this.resourceRows.set(matrix.Rows);
        this.resourceVersion.set(matrix.Version);
        this.resourceDirty.set(false);
        this.resourceConflict.set(null);
        this.resourceLoading.set(false);
      },
      error: () => {
        // Cho phép thử lại: mở tab khác rồi quay lại sẽ gọi `GET` lần nữa. `httpErrorInterceptor`
        // đã hiện toast nên ở đây không báo thêm.
        this.resourceRequested = false;
        this.resourceVersion.set(null);
        this.resourceLoading.set(false);
      },
    });
  }

  // ===== Chặn mất thay đổi khi rời trang =====
  // Hai lớp, cần CẢ HAI (fe/09-forms-validation.md §"Form dirty + điều hướng đi"): `canDeactivate`
  // bắt điều hướng trong Router (bấm sidebar, Back của SPA), `beforeunload` bắt đóng tab/F5.
  // Không lớp nào thay được lớp kia.
  private readonly leaveConfirm = viewChild.required(ConfirmDialog);
  /** Câu trả lời của hộp thoại "rời trang?" đang chờ. `null` = không có câu hỏi nào đang mở. */
  private leaveDecision: Subject<boolean> | null = null;

  /** Gộp cả HAI tab: tab đang ẩn vẫn giữ thay đổi chưa lưu, rời trang là mất luôn phần đó. */
  protected readonly hasUnsavedChanges = computed(() => this.dirty() || this.resourceDirty());

  canDeactivate(): Observable<boolean> | boolean {
    // KHÔNG sửa gì thì đi THẲNG, không hỏi. Hỏi khi không có gì để mất là cách nhanh nhất dạy
    // người dùng bấm qua hộp thoại theo phản xạ — lúc đó nó hết bảo vệ được gì.
    if (!this.hasUnsavedChanges()) return true;
    this.settleLeave(false); // câu hỏi cũ còn treo (điều hướng bị huỷ giữa chừng) → đóng sổ trước
    const decision = new Subject<boolean>();
    this.leaveDecision = decision;
    this.leaveConfirm().open();
    return decision.asObservable();
  }

  protected onLeaveConfirmed(): void {
    this.settleLeave(true);
  }

  protected onLeaveCancelled(): void {
    this.settleLeave(false);
  }

  private settleLeave(allowed: boolean): void {
    const decision = this.leaveDecision;
    if (!decision) return;
    this.leaveDecision = null;
    decision.next(allowed);
    decision.complete();
  }

  /**
   * Đóng tab / F5 / đóng trình duyệt — Router không thấy những đường này nên `canDeactivate` ở
   * trên không chạy. Trình duyệt hiện đại (Chrome ≥ 51) LUÔN dùng câu chữ mặc định của nó và bỏ
   * qua mọi chuỗi gán vào `returnValue`, nên đừng cố ráp một câu tiếng Việt vào đây.
   */
  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (!this.hasUnsavedChanges()) return;
    event.preventDefault();
    event.returnValue = '';
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

  onResourceToggle(event: { ResourceKey: string; Role: string }): void {
    this.resourceRows.update((rows) =>
      rows.map((row) => {
        if (row.ResourceKey !== event.ResourceKey) return row;
        const has = row.AssignedRoles.includes(event.Role);
        return {
          ...row,
          AssignedRoles: has ? row.AssignedRoles.filter((r) => r !== event.Role) : [...row.AssignedRoles, event.Role],
        };
      }),
    );
    this.resourceDirty.set(true);
  }

  onSave(): void {
    const version = this.version();
    if (version === null) {
      // Không có token thì DỪNG HẲN, không "cứ gửi thử": BE sẽ trả 409 và người dùng nhận một câu
      // khó hiểu cho thứ FE đã biết trước. Nói thẳng ra và mời tải lại.
      this.conflict.set(NO_VERSION_TEXT_KEY);
      return;
    }

    this.conflict.set(null);
    this.saving.set(true);
    this.service.saveMatrix(this.rows(), version).subscribe({
      next: () => {
        this.saving.set(false);
        this.dirty.set(false);
        this.toast.success(this.translate.instant('phan-quyen.toast.savedMenu') as string);
        // `PUT` vừa đổi chính `SysMenuRole` mà sidebar render — ép tải lại ngay để nav rail phản
        // ánh đúng quyền mới, không bắt người quản trị F5 (Normalize on redesign #6, 04-phan-quyen.md).
        this.menu.refresh().subscribe();
        // Token vừa gửi ĐÃ CŨ ngay lúc ghi xong: `version` là hàm băm của chính dữ liệu, ghi xong
        // là dữ liệu đổi ⇒ băm đổi. Không làm mới thì lần lưu THỨ HAI trên cùng màn hình chắc chắn
        // 409. Cách duy nhất chắc chắn đúng là hỏi lại server — FE không tự tính lại token được.
        this.loadMatrix();
      },
      error: (err: HttpErrorResponse & IHttpErrorWithApiResult) => {
        this.saving.set(false);
        // 409 = có người vừa ghi trước. Dải cảnh báo ở lại trên màn hình kèm nút "Tải lại" — toast
        // của interceptor biến mất sau vài giây và không mang theo lối đi tiếp nào.
        if (isVersionConflict(err)) this.conflict.set(CONFLICT_TEXT_KEY);
      },
    });
  }

  /**
   * KHÔNG gọi `menu.refresh()`: `RolePermission` không quyết định menu nào hiện ra (đó là việc của
   * `SysMenuRole` ở PERM-1) nên tải lại menu ở đây chỉ tốn một request và tạo ấn tượng sai rằng
   * hai bảng liên quan tới nhau.
   */
  onResourceSave(): void {
    const version = this.resourceVersion();
    if (version === null) {
      this.resourceConflict.set(NO_VERSION_TEXT_KEY);
      return;
    }

    this.resourceConflict.set(null);
    this.resourceSaving.set(true);
    this.service.saveResourceMatrix(this.resourceRows(), version).subscribe({
      next: () => {
        this.resourceSaving.set(false);
        this.resourceDirty.set(false);
        this.toast.success(this.translate.instant('phan-quyen.toast.savedResource') as string);
        // Làm mới token — xem chú thích cùng chỗ ở `onSave()`.
        this.loadResourceMatrix();
      },
      error: (err: HttpErrorResponse & IHttpErrorWithApiResult) => {
        this.resourceSaving.set(false);
        if (isVersionConflict(err)) this.resourceConflict.set(CONFLICT_TEXT_KEY);
      },
    });
  }
}
