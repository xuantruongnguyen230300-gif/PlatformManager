import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { AuthCard } from '../../../../shared/components/auth-card/auth-card';

const DEFAULT_REDIRECT = '/dashboard';

/**
 * SMART — route `/dang-nhap` (public). Khớp `doc/Prototype/login.html` — trường "Email" hiển thị
 * theo prototype nhưng gửi lên BE làm `userName` (BE dùng `UserName` tự do kiểu "SuperAdmin",
 * không phải email thật — xem doc/ke-hoach-xay-lai-corebase.md §"Email"), nên dùng `type="text"`
 * thay vì `type="email"` để trình duyệt không tự chặn native validation sai với username không
 * đúng cú pháp email.
 */
@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [AuthCard],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly currentUser = inject(CurrentUserService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly userName = signal('');
  protected readonly password = signal('');
  protected readonly showPassword = signal(false);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    // Đã đăng nhập sẵn (vd bấm Back sau khi login) mà lỡ vào lại /dang-nhap → điều hướng đi luôn,
    // không hiện lại form đăng nhập cho user đã có phiên.
    if (this.currentUser.isAuthenticated()) {
      this.redirectAfterAuth();
    }
  }

  onUserNameInput(event: Event): void {
    this.userName.set((event.target as HTMLInputElement).value);
  }

  onPasswordInput(event: Event): void {
    this.password.set((event.target as HTMLInputElement).value);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  onSubmit(): void {
    const userName = this.userName().trim();
    const password = this.password();
    if (!userName || !password) {
      this.errorMessage.set('Vui lòng nhập đầy đủ tài khoản và mật khẩu.');
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);
    this.authService.login(userName, password).subscribe({
      next: () => {
        this.submitting.set(false);
        this.redirectAfterAuth();
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.submitting.set(false);
        this.errorMessage.set(err.apiResult?.message ?? 'Đăng nhập thất bại — thử lại sau.');
      },
    });
  }

  private redirectAfterAuth(): void {
    if (this.currentUser.mustChangePassword()) {
      this.router.navigateByUrl('/doi-mat-khau');
      return;
    }
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/') ? returnUrl : DEFAULT_REDIRECT);
  }
}
