import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../../core/auth/auth.service';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { ApiFieldError, IApiResult, IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { ApiErrorMessageService } from '../../../../core/i18n/api-error-message.service';
import { AuthCard } from '../../../../shared/components/auth-card/auth-card';
import { CORE_ROUTES } from '../../../../core/config/core-routes';

/**
 * Bản sao PHÍA FE của chính sách độ dài mật khẩu, phục vụ DUY NHẤT việc kiểm-trước-khi-gửi —
 * chặn một vòng mạng chắc chắn hỏng, KHÔNG phải để dựng câu người dùng đọc.
 *
 * Nguồn thật là BE, nơi CƯỠNG CHẾ chính sách (`options.Password.RequiredLength` trong
 * `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs`); vì sao là 12 chứ
 * không phải 15 thì đọc doc/huong_dan/wiki-core/be/09-security-beyond-auth.md §"Ghi chú về
 * ngưỡng" — đó là file chủ của con số, đừng chốt lại ở đây.
 *
 * ⚠️ Giá trị này KHÔNG được ghép vào câu báo lỗi. Bản chép tay thì sẽ trôi, và nó ĐÃ trôi thật:
 * FE giữ 8 trong khi BE cưỡng chế 12 (tìm ra 2026-09-04) ⇒ người dùng gõ 9 ký tự được FE cho qua
 * rồi BE từ chối. Luật đã chốt: **câu người dùng đọc dựng từ tham số BE gửi** (`messageParams`
 * của envelope), không từ hằng số này — doc/huong_dan/wiki-core/fe/08-i18n.md §7 và
 * .../be/16-i18n-va-ma-loi.md §10.6.
 *
 * Nợ CÒN LẠI, đã ghi ở be/16-i18n-va-ma-loi.md §10.7 — **đừng tự đóng ở đây**: hai bản sao vẫn
 * tồn tại song song nên vẫn trôi được; sửa số ở đây chỉ gỡ hậu quả nặng (câu sai số), không gỡ
 * việc chép. Đóng dứt điểm cần BE công bố chính sách qua endpoint, và đó là quyết định CỐ Ý
 * hoãn — không phải chỗ sót để người sau "dọn nốt".
 */
const MIN_PASSWORD_LENGTH = 12;

/**
 * Khoá của một ô trên form. `CurrentPassword`/`NewPassword` viết PascalCase khớp **tên property
 * C#** của `ChangePasswordCommand` — đó chính là key trong `fields` của envelope lỗi
 * (doc/huong_dan/quy-uoc/fe-api-client.md §Envelope: `fields` PascalCase, phần còn lại camelCase).
 * `ConfirmPassword` chỉ tồn tại phía FE (BE không nhận ô này) nên không bao giờ có lỗi server.
 */
export type ChangePasswordField = 'CurrentPassword' | 'NewPassword' | 'ConfirmPassword';



/**
 * SMART — route `/doi-mat-khau`. Màn hình MỚI, không có trong prototype (carve-out fidelity —
 * xem doc/Design/CLAUDE.md) — dựng từ brief, tái dùng 100% token/khung `AuthCard` của màn đăng
 * nhập cho đồng bộ hình ảnh. 2 tình huống dùng chung 1 màn: (1) bắt buộc đổi mật khẩu lần đăng
 * nhập đầu (`currentUser.mustChangePassword()===true`, `mustChangePasswordGuard` gắn trên mọi
 * route KHÁC tự ép về đây — chính route này KHÔNG gắn guard đó, xem doi-mat-khau.routes.ts),
 * (2) user chủ
 * động vào đổi mật khẩu sau này (endpoint `change-password` không giới hạn chỉ dùng 1 lần).
 *
 * **Lỗi hiện dưới đúng ô** (2026-08-29): `fields` của envelope bind thẳng vào từng ô thay vì gộp
 * chung vào một câu ở đầu form — nếu không, một lỗi 400 nói rõ "mật khẩu mới quá ngắn" sẽ hiện ra
 * thành "Dữ liệu không hợp lệ." và người dùng phải tự đoán ô nào sai.
 */
@Component({
  selector: 'app-doi-mat-khau-page',
  standalone: true,
  imports: [AuthCard, TranslatePipe],
  templateUrl: './doi-mat-khau.page.html',
  styleUrl: './doi-mat-khau.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoiMatKhauPage {
  private readonly authService = inject(AuthService);
  private readonly currentUser = inject(CurrentUserService);
  private readonly router = inject(Router);
  /**
   * 🛑 `TranslateService`, KHÔNG phải `LanguageService` — dù `LanguageService.current` mới là "nơi
   * duy nhất biết đang ở ngôn ngữ nào" và màn đăng nhập bên cạnh đang dùng nó.
   *
   * `LanguageService` đòi token `CORE_I18N` (dữ liệu do `app.config.ts` bơm vào), mà spec của màn
   * này không cấp — inject nó ở đây làm 6 test đỏ với `NG0201`, đúng cái bẫy đã ghi ở
   * `UserGridTable.localeId`. `currentLang` của ngx-translate v18 vốn đã là `Signal` và là thứ
   * `LanguageService.current` bọc lại, nên đọc thẳng vừa đủ cho việc tạo phụ thuộc.
   */
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ApiErrorMessageService);

  /**
   * Đích "đổi xong thì đi đâu" đến từ `CORE_ROUTES`, KHÔNG khai cứng `/trang-chu`.
   *
   * `platform/` NẰM TRONG CoreBase (doc/kien-truc-core-module.md §"platform/ — các màn hình
   * Core"), nên chuỗi tiếng Việt ở đây đi theo nền tảng sang sản phẩm thứ hai — nơi màn mặc
   * định có thể là `/home`. Sai ở đây không gây lỗi biên dịch: route không khớp rơi vào `**`,
   * người dùng vừa đổi mật khẩu xong bị ném về đâu đó mà không có thông báo nào.
   *
   * Đúng seam mà `mustChangePasswordGuard` đã dùng để ép người dùng TỚI màn này — đường vào và
   * đường ra phải đọc chung một nguồn, nếu không chúng lệch nhau được.
   */
  private readonly routes = inject(CORE_ROUTES);

  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly submitting = signal(false);

  /**
   * Lỗi KHÔNG gắn ô nào — `message` của envelope (vd `AUTH.CHANGE_PASSWORD_FAILED` 422), đã là CÂU
   * do BE soạn. `null` khi không có.
   */
  private readonly serverError = signal<string | null>(null);
  /**
   * `fieldErrors` nguyên văn của envelope lỗi gần nhất — key PascalCase, KHÔNG camelCase lại.
   *
   * Giữ nguyên văn (mã + câu) thay vì câu đã dịch, cùng lý do với `localFieldErrors`: người dùng
   * đổi ngôn ngữ trong lúc lỗi đang hiện thì câu phải đổi theo.
   *
   * <b>Đọc `fieldErrors` chứ KHÔNG `fields`</b> (đổi 2026-09-06): với lỗi nghiệp vụ — đúng nhánh
   * mà màn này sinh ra nhiều nhất (`PasswordTooShort`, `PasswordMismatch`…) — BE **để trống**
   * `fields` và chỉ điền `fieldErrors`, vì nội dung là MÃ chứ không phải câu hiện thẳng. Bản
   * trước đọc `fields` nên mọi lỗi Identity đều rơi vào rỗng: không ô nào được tô đỏ, người dùng
   * chỉ thấy câu chung ở đầu form. Xem doc/huong_dan/wiki-core/fe/02-http-envelope.md
   * §"Lỗi theo ô".
   */
  private readonly serverFieldErrors = signal<Record<string, ApiFieldError[]> | null>(null);
  /**
   * Lỗi do chính form kiểm tại chỗ, đã gắn khoá ô — GIÁ TRỊ LÀ KHOÁ DỊCH, không phải câu.
   *
   * Giữ khoá thay vì câu vì cùng lý do với khối lỗi ở màn đăng nhập: người dùng đổi ngôn ngữ trong
   * lúc câu lỗi đang hiện sẽ thấy một câu tiếng Việt giữa giao diện tiếng Anh, và không có gì báo
   * vì mọi thứ khác trên màn hình đã đổi đúng. Dịch lại mỗi lần đọc thì câu đó đổi cùng phần còn
   * lại (doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 2).
   */
  private readonly localFieldErrors = signal<Partial<Record<ChangePasswordField, string>>>({});

  /**
   * Lỗi kiểm tại chỗ ĐÈ lỗi server cho cùng một ô: nó mô tả đúng lần bấm này.
   *
   * Hai nguồn vào đây mang HAI loại giá trị — server trả CÂU (đã soạn sẵn, FE không có khoá cho
   * nó), form trả KHOÁ — nên khoá được dịch ngay tại đây, và cái ra khỏi `computed` này luôn là
   * câu. Nhờ vậy template chỉ có một dạng để hiển thị, không phải đoán giá trị nào cần `translate`.
   *
* Dòng `this.translate.currentLang()` không dùng tới giá trị — nó làm PHỤ THUỘC signal của
   * `computed` này trở nên tường minh tại chỗ đọc.
   *
   * ⚠️ Đừng viết rằng thiếu nó thì `computed` không bao giờ tính lại: ĐO 2026-09-06 bằng canary
   * (gỡ dòng tương đương ở `core/title/page-title.strategy.ts` → bộ test vẫn xanh) cho thấy
   * `instant()` của @ngx-translate/core v18 TỰ đọc cả `currentLang` lẫn kho bản dịch, nên phụ
   * thuộc đã được đăng ký sẵn. Giữ dòng này là để không buộc cả màn hình vào một chi tiết bên
   * trong thư viện — xem doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 2.
   */
  protected readonly fieldErrors = computed<Record<string, string>>(() => {
    this.translate.currentLang();
    const local = Object.fromEntries(
      Object.entries(this.localFieldErrors()).map(([field, key]) => [
        field,
        this.translate.instant(key as string) as string,
      ]),
    );
    return { ...this.resolveServerFieldErrors(), ...local };
  });

  /**
   * Dịch từng `ApiFieldError` của một ô rồi gộp lại — mỗi ô chỉ có một chỗ để hiện câu.
   *
   * Dịch Ở ĐÂY chứ không lúc nhận response: `fieldMessage` tra bảng dịch theo mã, nên gọi lại mỗi
   * lần `computed` chạy thì câu đổi theo ngôn ngữ. Giữ câu đã dịch trong signal sẽ đóng băng nó ở
   * ngôn ngữ lúc lỗi xảy ra.
   */
  private resolveServerFieldErrors(): Record<string, string> {
    const errors = this.serverFieldErrors();
    if (!errors) return {};
    return Object.fromEntries(
      Object.entries(errors).map(([field, list]) => [
        field,
        list.map((error) => this.errorMessages.fieldMessage(error)).join(' '),
      ]),
    );
  }

  /** Câu lỗi chung ở đầu form — CHỈ hiện khi không có lỗi ô nào, để không lặp lại cùng một thông
   * tin bằng câu mơ hồ hơn ngay bên trên câu cụ thể. */
  protected readonly errorMessage = computed(() =>
    Object.keys(this.fieldErrors()).length > 0 ? null : this.serverError(),
  );

  protected readonly isForced = computed(() => this.currentUser.mustChangePassword());
  /** KHOÁ dịch của phụ đề — template dịch bằng `| translate`, nên nó đổi theo ngôn ngữ. */
  protected readonly subtitleKey = computed(() =>
    this.isForced() ? 'doi-mat-khau.hint.forcedSubtitle' : 'doi-mat-khau.hint.subtitle',
  );

  /** Id thẻ chứa lỗi của một ô — dùng cho `aria-describedby`, phải khớp `[id]` trong template. */
  protected errorId(field: ChangePasswordField): string {
    return `${field}Error`;
  }

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

    // Gom HẾT lỗi rồi mới dừng: mỗi ô nay có chỗ hiện lỗi riêng, báo từng lỗi một là bắt người
    // dùng sửa - bấm - sửa nhiều vòng không cần thiết.
    // Giá trị là KHOÁ DỊCH, không phải câu — xem `localFieldErrors`.
    const errors: Partial<Record<ChangePasswordField, string>> = {};
    if (!current) errors.CurrentPassword = 'doi-mat-khau.error.currentPasswordRequired';
    if (!next) {
      errors.NewPassword = 'doi-mat-khau.error.newPasswordRequired';
    } else if (next.length < MIN_PASSWORD_LENGTH) {
      // Câu KHÔNG mang con số, có chủ đích: con số duy nhất được phép hiện cho người dùng là con
      // số BE gửi kèm mã lỗi. Nội suy `MIN_PASSWORD_LENGTH` vào đây là dựng câu từ một bản sao có
      // thể sai — đúng lỗi vừa sửa ở trên, và lần trước nó đã sai thật. Câu có số quay lại ở pha
      // bọc chuỗi, khi FE ráp `messageParams` vào bảng dịch (fe/08-i18n.md §7).
      errors.NewPassword = 'doi-mat-khau.error.newPasswordTooShort';
    }
    if (!confirm) {
      errors.ConfirmPassword = 'doi-mat-khau.error.confirmPasswordRequired';
    } else if (next && next !== confirm) {
      errors.ConfirmPassword = 'doi-mat-khau.error.confirmPasswordMismatch';
    }

    this.localFieldErrors.set(errors);
    if (Object.keys(errors).length > 0) return;

    this.serverError.set(null);
    this.serverFieldErrors.set(null);
    this.submitting.set(true);
    this.authService.changePassword(current, next).subscribe({
      next: () => {
        this.submitting.set(false);
        this.currentUser.markPasswordChanged();
        this.router.navigateByUrl(this.routes.home);
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.submitting.set(false);
        const result: IApiResult<unknown> | null | undefined = err.apiResult;
        this.serverFieldErrors.set(result?.fieldErrors ?? null);
        // Câu dự phòng phải dịch NGAY tại đây (không giữ khoá) vì `serverError` cũng mang câu của
        // BE — một trường, một loại giá trị. Đánh đổi đã biết: đổi ngôn ngữ trong lúc câu này đang
        // hiện thì nó đứng yên. Chấp nhận được vì nhánh này chỉ chạy khi envelope KHÔNG có
        // `message`, tức lỗi mạng/hạ tầng, và người dùng khi đó bấm Lưu lại chứ không đổi ngôn ngữ.
        this.serverError.set(
          result?.message ?? (this.translate.instant('doi-mat-khau.error.changeFailed') as string),
        );
      },
    });
  }
}
