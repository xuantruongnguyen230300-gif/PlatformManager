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

    if (!email) {
      this.localError.set('Email bắt buộc.');
      return;
    }
    if (!fullName) {
      this.localError.set('Họ tên bắt buộc.');
      return;
    }
    if (roles.length === 0) {
      this.localError.set('Chọn ít nhất 1 vai trò.');
      return;
    }

    const editing = this.editing();
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
    this.saved.emit({ IsEditing: true, Update: { Email: email, FullName: fullName, Roles: roles } });
  }
}
