import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { AuthCard } from '../../../../shared/components/auth-card/auth-card';

const MIN_PASSWORD_LENGTH = 8;

/**
 * SMART — route `/doi-mat-khau`. Màn hình MỚI, không có trong prototype (carve-out fidelity —
 * xem doc/Design/CLAUDE.md) — dựng từ brief, tái dùng 100% token/khung `AuthCard` của màn đăng
 * nhập cho đồng bộ hình ảnh. 2 tình huống dùng chung 1 màn: (1) bắt buộc đổi mật khẩu lần đăng
 * nhập đầu (`currentUser.mustChangePassword()===true`, `authGuard` tự ép về đây), (2) user chủ
 * động vào đổi mật khẩu sau này (endpoint `change-password` không giới hạn chỉ dùng 1 lần).
 */
@Component({
  selector: 'app-doi-mat-khau-page',
  standalone: true,
  imports: [AuthCard],
  templateUrl: './doi-mat-khau.page.html',
  styleUrl: './doi-mat-khau.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoiMatKhauPage {
  private readonly authService = inject(AuthService);
  private readonly currentUser = inject(CurrentUserService);
  private readonly router = inject(Router);

  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly isForced = computed(() => this.currentUser.mustChangePassword());
  protected readonly subtitle = computed(() =>
    this.isForced()
      ? 'Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.'
      : 'Đổi mật khẩu tài khoản của bạn.',
  );

  onCurrentPasswordInput(event: Event): void {
    this.currentPassword.set((event.target as HTMLInputElement).value);
  }

  onNewPasswordInput(event: Event): void {
    this.newPassword.set((event.target as HTMLInputElement).value);
  }

  onConfirmPasswordInput(event: Event): void {
    this.confirmPassword.set((event.target as HTMLInputElement).value);
  }

  onSubmit(): void {
    const current = this.currentPassword();
    const next = this.newPassword();
    const confirm = this.confirmPassword();

    if (!current || !next || !confirm) {
      this.errorMessage.set('Vui lòng nhập đầy đủ các trường.');
      return;
    }
    if (next.length < MIN_PASSWORD_LENGTH) {
      this.errorMessage.set(`Mật khẩu mới phải có ít nhất ${MIN_PASSWORD_LENGTH} ký tự.`);
      return;
    }
    if (next !== confirm) {
      this.errorMessage.set('Xác nhận mật khẩu mới không khớp.');
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);
    this.authService.changePassword(current, next).subscribe({
      next: () => {
        this.submitting.set(false);
        this.currentUser.markPasswordChanged();
        this.router.navigateByUrl('/dashboard');
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.submitting.set(false);
        this.errorMessage.set(err.apiResult?.message ?? 'Đổi mật khẩu thất bại — thử lại sau.');
      },
    });
  }
}
