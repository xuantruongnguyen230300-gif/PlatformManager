import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../../core/auth/auth.service';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { ApiErrorMessageService } from '../../../../core/i18n/api-error-message.service';
import { LanguageService } from '../../../../core/i18n/language.service';
import { IApiResult, IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { AuthCard } from '../../../../shared/components/auth-card/auth-card';
import { LanguageSwitcher } from '../../../../shared/components/language-switcher/language-switcher';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';

/**
 * Khoá dịch NHÃN của từng ô nhập, tra theo khoá `fieldErrors` mà BE trả về (chính là tên property
 * C# của `LoginCommand`, giữ PascalCase — `GlobalExceptionHandler.NormalizeField`).
 *
 * Vì sao màn hình phải giữ bảng này chứ không để `core/` làm: tham số `PropertyName` mà
 * FluentValidation gửi kèm là tên property đã tách chữ (`UserName` → `"User Name"`) — một chuỗi
 * tiếng Anh sinh ra từ code C#, không dịch được. Ghép thẳng vào câu tiếng Việt sẽ ra *"Vui lòng
 * nhập User Name."*. Nhãn đúng của ô đó là thứ MÀN HÌNH sở hữu (`shared.field.userName`,
 * `login.field.password` — chính hai khoá đang gắn trên hai `<label>`), nên nó được truyền vào làm
 * giá trị ghi đè. Việc TRA MÃ vẫn nằm nguyên ở `ApiErrorMessageService`; ở đây chỉ có nhãn.
 *
 * Thiếu một khoá trong bảng này KHÔNG làm hỏng gì: `ApiErrorMessageService.fieldMessage` khi đó
 * dùng `PropertyName` của BE, tức câu vẫn đọc được, chỉ lẫn một cụm tiếng Anh.
 */
const FIELD_LABEL_KEYS: Readonly<Record<string, string>> = {
  UserName: 'shared.field.userName',
  Password: 'login.field.password',
};

/** Lỗi từ server đang hiển thị — giữ nguyên envelope + mã HTTP để dịch LẠI khi đổi ngôn ngữ. */
interface IServerFailure {
  readonly result: IApiResult<unknown> | null;
  readonly status: number;
}

/**
 * SMART — route `/dang-nhap` (public). Khớp màn "Đăng nhập" ở
 * `doc/Design/Frontend/PlatformManager/Screens/05-auth.md`.
 *
 * Ô đầu tiên là TÊN ĐĂNG NHẬP, không phải email (chốt 2026-08-31, §Normalize on redesign mục 6):
 * BE dùng `UserName` tự do kiểu `SuperAdmin`/`nguyen.van.a`, nên `type="text"` (không phải
 * `type="email"` — trình duyệt sẽ chặn nhầm bằng native validation) và nhãn/gợi ý/icon đều nói
 * "tên đăng nhập". Payload gửi đi KHÔNG đổi: field vẫn là `userName`.
 */
@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [AuthCard, LanguageSwitcher, TranslatePipe],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly currentUser = inject(CurrentUserService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /**
   * `<h1>` của card đăng nhập là TÊN SẢN PHẨM, không phải tiêu đề màn hình
   * (doc/Design/Frontend/PlatformManager/Components/AuthCard.md §Variants) — nên nó là dữ liệu
   * của dự án và phải đi qua `CORE_BRANDING`, y như ô vuông `.brand-mark` ngay trên nó.
   *
   * Vì sao chỗ này quan trọng hơn vẻ ngoài của nó: `platform/` NẰM TRONG CoreBase
   * (doc/kien-truc-core-module.md §"platform/ — các màn hình Core"), nên chuỗi khai cứng ở đây
   * đi theo nền tảng sang sản phẩm thứ hai. Hậu quả thấy được ngay ở màn hình đầu tiên người
   * dùng gặp: ô vuông đổ tên MỚI từ token, còn dòng chữ ngay bên dưới vẫn là tên CŨ. Lượt tách
   * 2026-09-02 bỏ sót vì phép kiểm khi đó chỉ quét `core/` và `shared/`.
   */
  protected readonly branding = inject(CORE_BRANDING);

  /**
   * Hai đích điều hướng sau khi đăng nhập thành công — màn đổi mật khẩu bắt buộc và màn mặc
   * định — đến từ `CORE_ROUTES`, KHÔNG khai cứng ở đây.
   *
   * Cùng lý do với `branding` ngay trên: `platform/` NẰM TRONG CoreBase
   * (doc/kien-truc-core-module.md §"platform/ — các màn hình Core"), nên hai chuỗi tiếng Việt
   * `/trang-chu` và `/doi-mat-khau` đi theo nền tảng sang sản phẩm thứ hai — nơi bảng route có
   * thể là `/home` và `/change-password`. Hậu quả khi đó KHÔNG phải lỗi biên dịch mà là: đăng
   * nhập xong rơi vào route không tồn tại, `**` nuốt đi và người dùng thấy mình quay về trang
   * chủ mà không hiểu vì sao — đúng dạng lỗi im lặng mà `core/config/core-routes.ts` sinh ra để
   * chặn.
   *
   * Lượt tách 2026-09-02 bỏ sót đúng ba dòng này (hai ở file này, một ở màn đổi mật khẩu) vì
   * phép kiểm khi đó chỉ quét `core/` rồi `shared/`; `platform/` là tầng thứ ba, và nó cũng là
   * CoreBase.
   */
  private readonly routes = inject(CORE_ROUTES);

  protected readonly userName = signal('');
  protected readonly password = signal('');
  protected readonly showPassword = signal(false);
  /**
   * "Ghi nhớ đăng nhập" — MẶC ĐỊNH `false` (chốt 2026-08-31 mục 4). Giá trị này đi thẳng vào body
   * `POST /api/auth/login` để BE quyết định tuổi thọ cookie: `false` ⇒ cookie phiên (chết khi
   * đóng trình duyệt), `true` ⇒ cookie 14 ngày trượt. Mặc định `true` là chọn hộ người dùng bên
   * kém an toàn trên máy dùng chung.
   */
  protected readonly rememberMe = signal(false);
  protected readonly submitting = signal(false);

  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);
  private readonly errorMessages = inject(ApiErrorMessageService);

  /** Lỗi do CHÍNH màn hình kiểm (chưa gửi request nào) — giữ KHOÁ, không giữ câu. */
  private readonly clientErrorKey = signal<string | null>(null);
  /** Lỗi từ server — giữ ENVELOPE, không giữ câu. */
  private readonly serverFailure = signal<IServerFailure | null>(null);

  /**
   * Câu lỗi hiển thị trong khối `.login-error`.
   *
   * 🛑 Là `computed` chứ không phải `signal<string>` CÓ CHỦ ĐÍCH, và đây là chỗ cả màn hình dễ làm
   * sai nhất: nếu lưu sẵn câu đã dịch thì người dùng bấm đổi ngôn ngữ **trong lúc** khối lỗi đang
   * hiện sẽ thấy một câu tiếng Việt nằm giữa giao diện tiếng Anh — và không có gì báo, vì mọi thứ
   * khác trên màn hình đã đổi đúng. Giữ NGUỒN (khoá hoặc envelope) rồi dịch lại mỗi lần đọc thì
   * câu đó đổi cùng phần còn lại.
   *
   * Dòng `this.language.current()` đầu hàm là một phụ thuộc khai TƯỜNG MINH.
   *
   * ⚠️ Sửa 2026-09-06 — câu ở đây trước ghi *"`instant()` không phải signal nên nó KHÔNG tự tạo
   * phụ thuộc; không đọc signal ngôn ngữ ở đây thì `computed` này không bao giờ tính lại"*. Đo
   * lại thì SAI: `instant()` → `getParsedResult` → `getTextToInterpolate`, và hàm đó gọi
   * `getCurrentLang()` khi không truyền `lang` tường minh — mà `currentLang` của ngx-translate v18
   * là `signal(...)`. Nên phụ thuộc VẪN được tạo dù không có dòng này; gỡ nó ra thì test vẫn xanh
   * (đã canary).
   *
   * Vì sao vẫn GIỮ dòng đó: nó biến một phụ thuộc vào **chi tiết nội tại của thư viện** thành một
   * phụ thuộc khai ra trên mặt code. Nếu ngx-translate đổi cách đọc ngôn ngữ ở một bản sau, chỗ
   * này không hỏng theo. Giữ vì lý do đó, KHÔNG phải vì lý do cũ.
   * (doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 2b)
   */
  protected readonly errorMessage = computed<string | null>(() => {
    this.language.current();

    const clientKey = this.clientErrorKey();
    if (clientKey) return this.translate.instant(clientKey) as string;

    const failure = this.serverFailure();
    if (!failure) return null;

    // Lỗi validate 400: câu ở mức envelope chỉ là "Dữ liệu không hợp lệ." — vô ích với người đang
    // gõ form. Câu của TỪNG ô nói đúng chuyện gì thiếu, nên nó thắng khi có mặt.
    //
    // Ghép bằng dấu cách vào MỘT khối: màn đăng nhập cố ý KHÔNG có chỗ báo lỗi theo từng ô
    // (doc/Design/Frontend/PlatformManager/Screens/05-auth.md §States, ca `field-validation` —
    // "one message slot for both concerns"). Đây là chỗ duy nhất có để nói.
    const fieldLines = this.fieldMessages(failure.result);
    if (fieldLines.length > 0) return fieldLines.join(' ');

    return this.errorMessages.messageFor(failure.result, failure.status);
  });

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

  onRememberMeChange(event: Event): void {
    this.rememberMe.set((event.target as HTMLInputElement).checked);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  onSubmit(): void {
    const userName = this.userName().trim();
    const password = this.password();
    if (!userName || !password) {
      this.clearError();
      this.clientErrorKey.set('login.error.missingCredentials');
      return;
    }

    this.clearError();
    this.submitting.set(true);
    this.authService.login(userName, password, this.rememberMe()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.redirectAfterAuth();
      },
      error: (err: IHttpErrorWithApiResult & { status?: number }) => {
        this.submitting.set(false);
        // KHÔNG lưu câu đã dịch — xem ghi chú ở `errorMessage`. `status` để `ApiErrorMessageService`
        // chọn đúng câu dự phòng khi response không mang envelope (mạng hỏng, proxy chặn).
        this.serverFailure.set({ result: err.apiResult ?? null, status: err.status ?? 0 });
      },
    });
  }

  private clearError(): void {
    this.clientErrorKey.set(null);
    this.serverFailure.set(null);
  }

  /**
   * Câu lỗi của từng ô, đã dịch. Đọc `fieldErrors` (mã + câu) chứ KHÔNG đọc `fields` (chuỗi trần):
   * chỉ `fieldErrors` mang `code` và `messageParams`, tức chỉ nó dịch được. `fields` vẫn còn trên
   * dây cho các form khác cho tới bước 11 của
   * doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §7.
   */
  private fieldMessages(result: IApiResult<unknown> | null): string[] {
    const fieldErrors = result?.fieldErrors;
    if (!fieldErrors) return [];

    return Object.entries(fieldErrors).flatMap(([field, errors]) =>
      errors.map((error) => this.errorMessages.fieldMessage(error, this.labelOverrideFor(field))),
    );
  }

  private labelOverrideFor(field: string): Record<string, string> | undefined {
    const labelKey = FIELD_LABEL_KEYS[field];
    return labelKey ? { PropertyName: this.translate.instant(labelKey) as string } : undefined;
  }

  private redirectAfterAuth(): void {
    if (this.currentUser.mustChangePassword()) {
      this.router.navigateByUrl(this.routes.changePassword);
      return;
    }
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/') ? returnUrl : this.routes.home);
  }
}
