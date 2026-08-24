import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { ASSIGNABLE_ROLES, ICreateUserPayload, IUpdateUserPayload, IUser } from '../../models/quan-tri-nguoi-dung.model';

const MIN_TEMP_PASSWORD_LENGTH = 8;

export interface IUserFormSaveEvent {
  IsEditing: boolean;
  Create?: ICreateUserPayload;
  Update?: IUpdateUserPayload;
}

/**
 * Dialog Thêm/Sửa người dùng (native `<dialog>`) — khớp `doc/contracts/users.md`: tạo mới cần
 * `TempPassword` (đủ mạnh, BE tự set `MustChangePassword=true`), sửa KHÔNG đổi được
 * `UserName`/mật khẩu qua đây (đúng contract PUT). `Roles` chỉ gồm `Admin`/`User` — xem
 * `ASSIGNABLE_ROLES` (không cấp `SuperAdmin` qua màn này, xem models).
 *
 * Dùng field SIGNAL (không đọc qua template reference lúc submit) vì `UserName`/`TempPassword`
 * chỉ tồn tại trong DOM ở chế độ tạo mới (`@if`) — template reference của Angular không sống sót
 * qua ranh giới `@if`, nên đọc trực tiếp qua signal an toàn hơn.
 */
@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [AutofocusDirective],
  templateUrl: './user-form-dialog.html',
  styleUrl: './user-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormDialog {
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input.required<boolean>();
  readonly editing = input<IUser | null>(null);
  readonly serverError = input<string | null>(null);

  readonly saved = output<IUserFormSaveEvent>();
  readonly closed = output<void>();

  protected readonly assignableRoles = ASSIGNABLE_ROLES;
  protected readonly localError = signal<string | null>(null);
  protected readonly selectedRoles = signal<string[]>([]);

  protected readonly userNameField = signal('');
  protected readonly emailField = signal('');
  protected readonly fullNameField = signal('');
  protected readonly tempPasswordField = signal('');

  protected readonly title = computed(() => (this.editing() ? 'Sửa người dùng' : 'Thêm người dùng'));
  protected readonly errorMessage = computed(() => this.localError() ?? this.serverError());

  /**
   * Role của user đích KHÔNG thuộc `ASSIGNABLE_ROLES` (vd `SuperAdmin`, hoặc chuỗi casing lạ do BE
   * trả) — form không có ô tick cho các role này nên phải tự giữ lại NGUYÊN VĂN, VÔ ĐIỀU KIỆN
   * (không phụ thuộc role người gọi). Thiếu bước này, `PUT /api/users/{id}` (ghi đè trọn gói
   * `Roles`) sẽ âm thầm gỡ role hệ thống của user đích — 403 nếu người gọi là Admin, hạ quyền âm
   * thầm nếu người gọi là SuperAdmin. Xem doc/contracts/users.md §"Luật cấp/gỡ role SuperAdmin".
   */
  private readonly preservedSystemRoles = computed(() =>
    (this.editing()?.Roles ?? []).filter((r) => !(this.assignableRoles as readonly string[]).includes(r)),
  );

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      const el = this.dialogEl().nativeElement;
      if (this.open() && !el.open) {
        const editing = this.editing();
        this.localError.set(null);
        this.userNameField.set(editing?.UserName ?? '');
        this.emailField.set(editing?.Email ?? '');
        this.fullNameField.set(editing?.FullName ?? '');
        this.tempPasswordField.set('');
        this.selectedRoles.set(editing?.Roles.filter((r) => (this.assignableRoles as readonly string[]).includes(r)) ?? []);
        el.showModal();
      }
      if (!this.open() && el.open) el.close();
    });
  }

  onNativeClose(): void {
    this.closed.emit();
  }

  onUserNameInput(event: Event): void {
    this.userNameField.set((event.target as HTMLInputElement).value);
  }

  onEmailInput(event: Event): void {
    this.emailField.set((event.target as HTMLInputElement).value);
  }

  onFullNameInput(event: Event): void {
    this.fullNameField.set((event.target as HTMLInputElement).value);
  }

  onTempPasswordInput(event: Event): void {
    this.tempPasswordField.set((event.target as HTMLInputElement).value);
  }

  isRoleSelected(role: string): boolean {
    return this.selectedRoles().includes(role);
  }

  toggleRole(role: string): void {
    this.selectedRoles.update((roles) => (roles.includes(role) ? roles.filter((r) => r !== role) : [...roles, role]));
  }

  onSubmit(): void {
    const email = this.emailField().trim();
    const fullName = this.fullNameField().trim();
    const roles = this.selectedRoles();
    const editing = this.editing();
    // Vai trò cuối cùng gửi lên BE — gồm cả role hệ thống giữ nguyên, nên "chưa tick ô nào" (Admin
    // sửa user SuperAdmin, bỏ hết tick Admin/User) vẫn là trạng thái HỢP LỆ, không phải lỗi rỗng.
    const finalRoles = editing ? [...this.preservedSystemRoles(), ...roles] : roles;

    if (!email) {
      this.localError.set('Email bắt buộc.');
      return;
    }
    if (!fullName) {
      this.localError.set('Họ tên bắt buộc.');
      return;
    }
    if (finalRoles.length === 0) {
      this.localError.set('Chọn ít nhất 1 vai trò.');
      return;
    }
    if (!editing) {
      const userName = this.userNameField().trim();
      const tempPassword = this.tempPasswordField();
      if (!userName) {
        this.localError.set('Tên đăng nhập bắt buộc.');
        return;
      }
      if (tempPassword.length < MIN_TEMP_PASSWORD_LENGTH) {
        this.localError.set(`Mật khẩu tạm phải có ít nhất ${MIN_TEMP_PASSWORD_LENGTH} ký tự.`);
        return;
      }
      this.localError.set(null);
      this.saved.emit({
        IsEditing: false,
        Create: { UserName: userName, Email: email, FullName: fullName, TempPassword: tempPassword, Roles: roles },
      });
      return;
    }

    this.localError.set(null);
    this.saved.emit({
      IsEditing: true,
      Update: { Email: email, FullName: fullName, Roles: finalRoles },
    });
  }
}
