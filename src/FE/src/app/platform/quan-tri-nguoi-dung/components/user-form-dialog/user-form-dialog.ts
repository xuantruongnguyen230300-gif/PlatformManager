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
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ApiFieldError } from '../../../../core/http/api-result.model';
import { groupServerFieldErrors } from '../../../../core/http/server-field-errors';
import { ApiErrorMessageService } from '../../../../core/i18n/api-error-message.service';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { ASSIGNABLE_ROLES, ICreateUserPayload, IUpdateUserPayload, IUser } from '../../models/quan-tri-nguoi-dung.model';

/**
 * Bản sao thứ HAI của cùng một chính sách độ dài mật khẩu — mật khẩu tạm đi thẳng vào
 * `UserManager.CreateAsync` (`src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs`)
 * nên nó chịu ĐÚNG `options.Password.RequiredLength` của Identity, không phải một ngưỡng riêng.
 * Cùng lý do và cùng ràng buộc với `MIN_PASSWORD_LENGTH` ở
 * `platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts` — đọc chú thích ở đó trước khi
 * đụng vào con số này; file chủ của con số là
 * doc/huong_dan/wiki-core/be/09-security-beyond-auth.md §"Ghi chú về ngưỡng".
 *
 * ⚠️ Chỉ dùng cho kiểm-trước-khi-gửi, KHÔNG ghép vào câu người dùng đọc (fe/08-i18n.md §7).
 * Bản này cũng đang trôi cùng kiểu: giữ 8 trong khi BE cưỡng chế 12 ⇒ tạo người dùng với mật khẩu
 * tạm 9 ký tự thì form cho qua rồi BE từ chối. Sửa 2026-09-04 cùng đợt với màn đổi mật khẩu.
 */
const MIN_TEMP_PASSWORD_LENGTH = 12;

/**
 * Phần cập nhật mà FORM biết: đúng những ô người dùng gõ. Cố ý KHÔNG phải cả `IUpdateUserPayload`.
 *
 * `Version` bị trừ ra bằng `Omit` chứ không bằng một interface viết tay song song: token chống ghi
 * đè là thứ của BẢN GHI, không phải của ô nhập nào — trang cha lấy nó từ `IUser.Version` của dòng
 * đang sửa (xem `submitUpdate`). Dùng `Omit` giữ hai kiểu dính vào nhau, nên thêm một trường vào
 * `IUpdateUserPayload` sau này là lỗi biên dịch ở đây chứ không trôi qua im lặng.
 */
export type IUserFormUpdate = Omit<IUpdateUserPayload, 'Version'>;

export interface IUserFormSaveEvent {
  IsEditing: boolean;
  Create?: ICreateUserPayload;
  Update?: IUserFormUpdate;
}

/**
 * Khoá của một ô trên form. CỐ Ý viết PascalCase khớp **tên property C#** (`CreateUserCommand`/
 * `UpdateUserCommand`) vì đó chính là key trong `fields` của envelope lỗi — xem
 * doc/huong_dan/quy-uoc/fe-api-client.md §Envelope ("`fields` dùng PascalCase, phần còn lại của
 * payload là camelCase"). Trùng khoá là điều kiện để bind lỗi server vào đúng ô mà không phải
 * duy trì một bảng ánh xạ thứ hai.
 */
export type UserFormField = 'UserName' | 'Email' | 'FullName' | 'TempPassword' | 'Roles';

/**
 * Dialog Thêm/Sửa người dùng (native `<dialog>`) — khớp `doc/contracts/users.md`: tạo mới cần
 * `TempPassword` (đủ mạnh, BE tự set `MustChangePassword=true`), sửa KHÔNG đổi được
 * `UserName`/mật khẩu qua đây (đúng contract PUT). `Roles` chỉ gồm `Admin`/`User` — xem
 * `ASSIGNABLE_ROLES` (không cấp `SuperAdmin` qua màn này, xem models).
 *
 * Dùng field SIGNAL (không đọc qua template reference lúc submit) vì `UserName`/`TempPassword`
 * chỉ tồn tại trong DOM ở chế độ tạo mới (`@if`) — template reference của Angular không sống sót
 * qua ranh giới `@if`, nên đọc trực tiếp qua signal an toàn hơn.
 *
 * **Lỗi hiện ngay dưới ô sai** (2026-08-29): trước đây mọi lỗi 400 của BE rơi vào đúng một câu
 * "Dữ liệu không hợp lệ." ở cuối form vì không nơi nào đọc `fields`, dù BE trả đủ. Nay lỗi có
 * khoá ô (`serverFieldErrors`, hoặc kiểm tra tại chỗ) hiện dưới đúng ô đó, kèm `aria-invalid` +
 * `aria-describedby` để trình đọc màn hình đọc được lý do — không thấy màu đỏ thì vẫn phải biết
 * ô nào sai (doc/huong_dan/wiki-core/fe/15-accessibility.md §1, WCAG 2.2 AA).
 */
@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [AutofocusDirective, TranslatePipe],
  templateUrl: './user-form-dialog.html',
  styleUrl: './user-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormDialog {
  private readonly platformId = inject(PLATFORM_ID);

  /**
   * Dùng để dịch KHOÁ lỗi tại chỗ trong `fieldErrors` — xem chú thích ở đó.
   *
   * LUẬT G4 cấm component `components/` inject **`HttpClient` hoặc service DATA**
   * (doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md) — `TranslateService` không thuộc nhóm nào
   * trong hai nhóm đó, và `TranslatePipe` mà component này vốn đã import cũng đang inject chính nó.
   *
   * Cổng G4 đã bật 2026-09-08 và **xanh với đúng hai dòng inject dưới đây**: cả `TranslateService`
   * lẫn `ApiErrorMessageService` nằm trong `UI_INFRA_SERVICES` của `scripts/fe-gate.sh` — miễn trừ
   * theo TÊN SERVICE (đã chứng minh không chạm `HttpClient`), KHÔNG phải miễn trừ cho file này.
   * Thêm một service DỮ LIỆU vào đây vẫn đỏ.
   * Cái KHÔNG được inject ở đây là `LanguageService`: nó đòi token `CORE_I18N` do `app.config.ts`
   * bơm vào, mà spec của các màn nghiệp vụ không cấp — xem ghi chú tại `UserGridTable.localeId`,
   * nơi lỗi đó đã xảy ra thật và làm đỏ 37 test.
   */
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ApiErrorMessageService);

  readonly open = input.required<boolean>();
  readonly editing = input<IUser | null>(null);
  /** Thông điệp lỗi KHÔNG gắn với ô nào (`message` của envelope) — hiện ở cuối form. */
  readonly serverError = input<string | null>(null);
  /**
   * `fieldErrors` nguyên văn của envelope — mã + câu, KHÔNG phải câu đã dịch. Key PascalCase khớp
   * property C#; trang cha truyền thẳng `err.apiResult?.fieldErrors`, KHÔNG tự camelCase lại và
   * cũng không tự gộp thành một câu.
   *
   * Đổi 2026-09-06 từ `fields` (`Record<string, string[]>`) sang `fieldErrors`: với lỗi nghiệp vụ
   * BE **để trống** `fields` và chỉ điền `fieldErrors`, nên bản trước bỏ lọt toàn bộ mã Identity.
   * Xem doc/huong_dan/wiki-core/fe/02-http-envelope.md §"Lỗi theo ô".
   */
  readonly serverFieldErrors = input<Record<string, ApiFieldError[]> | null>(null);
  /**
   * Request tạo/sửa ĐANG BAY. Nút Lưu tắt suốt thời gian đó và bật lại khi có kết quả —
   * fe/09-forms-validation.md §"Khoá form trong lúc đang gửi". Không có cờ này thì mạng chậm =
   * bấm hai lần = hai request; lần thứ hai bị BE từ chối vì trùng tên đăng nhập, và người dùng
   * nhận câu "tên đăng nhập đã tồn tại" ngay sau khi vừa tạo thành công chính tài khoản đó.
   */
  readonly saving = input<boolean>(false);

  readonly saved = output<IUserFormSaveEvent>();
  readonly closed = output<void>();

  protected readonly assignableRoles = ASSIGNABLE_ROLES;

  /**
   * Vai trò của tài khoản đang sửa mà KHÔNG có trong ô tick — hôm nay chỉ `SuperAdmin`.
   *
   * Tính bằng phép trừ với `ASSIGNABLE_ROLES` chứ **không** khai cứng chuỗi `'SuperAdmin'`: thêm
   * một vai trò hệ thống thứ hai sau này thì dòng thông báo tự đúng, không phải nhớ sửa ở đây.
   *
   * Vì sao cần (chốt 2026-09-06): ô tick chỉ hiện `Admin`/`User`, nên khi sửa một tài khoản
   * `SuperAdmin` thì cả hai ô đều trống và không có gì nói vai trò kia vẫn còn. BE chặn việc đổi
   * nó (`USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN`) — nên đây không phải lỗ hổng bảo mật, mà là
   * người sửa tưởng tài khoản không có vai trò nào, tích thêm rồi gặp 403 không hiểu vì sao.
   * Đặc tả: doc/Design/Frontend/PlatformManager/Components/FormRow.md §"Vai trò được giữ nguyên".
   */
  protected readonly preservedRoles = computed<string[]>(() => {
    const assignable = new Set<string>(ASSIGNABLE_ROLES);
    return (this.editing()?.Roles ?? []).filter((role) => !assignable.has(role));
  });
  protected readonly selectedRoles = signal<string[]>([]);

  protected readonly userNameField = signal('');
  protected readonly emailField = signal('');
  protected readonly fullNameField = signal('');
  protected readonly tempPasswordField = signal('');

  /**
   * Lỗi do chính form kiểm tại chỗ, đã gắn khoá ô — GIÁ TRỊ LÀ KHOÁ DỊCH, không phải câu. Trước đây
   * là một `localError` phẳng nên một lần bấm Lưu chỉ nói được MỘT lỗi và không biết nó thuộc ô nào.
   */
  private readonly localFieldErrors = signal<Partial<Record<UserFormField, string>>>({});

  /** KHOÁ dịch tiêu đề hộp thoại — template dịch bằng `| translate`. */
  protected readonly titleKey = computed(() =>
    this.editing() ? 'quan-tri-nguoi-dung.dialog.editTitle' : 'quan-tri-nguoi-dung.dialog.createTitle',
  );

  /**
   * Lỗi hiển thị dưới từng ô. Lỗi kiểm tại chỗ ĐÈ lỗi server cho cùng một ô: người dùng vừa sửa ô
   * đó xong bấm Lưu lại, thông điệp mới mô tả đúng lần bấm này hơn thông điệp của lần gọi trước.
   *
   * Hai nguồn mang HAI loại giá trị — server trả CÂU (BE đã soạn, FE không có khoá cho nó), form
   * trả KHOÁ — nên khoá được dịch ngay tại đây và thứ ra khỏi `computed` này luôn là câu. Template
   * vì vậy chỉ có một dạng để hiển thị, không phải đoán giá trị nào cần `| translate`; và bind
   * `[class.invalid]`/`aria-describedby` vẫn đọc đúng một chỗ.
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
    return {
      ...groupServerFieldErrors(this.serverFieldErrors(), (error) => this.errorMessages.fieldMessage(error)),
      ...local,
    };
  });

  /**
   * Câu lỗi chung ở cuối form — CHỈ hiện khi không có lỗi ô nào. BE trả kèm cả `message`
   * ("Dữ liệu không hợp lệ.") lẫn `fields`; hiện cả hai là lặp lại một thông tin bằng câu mơ hồ
   * hơn ngay bên dưới câu cụ thể.
   */
  protected readonly generalError = computed(() =>
    Object.keys(this.fieldErrors()).length > 0 ? null : this.serverError(),
  );

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
        this.localFieldErrors.set({});
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

  /** Id của thẻ chứa lỗi cho một ô — dùng cho `aria-describedby`, phải khớp `[id]` trong template. */
  protected errorId(field: UserFormField): string {
    return `uf${field}Error`;
  }

  onSubmit(): void {
    // Chốt chặn thứ hai, sau `[disabled]` của template: nút bị tắt vẫn có thể bị kích bằng phím
    // Enter ở một số đường, và `[disabled]` chỉ là giao diện chứ không phải bất biến của luồng.
    if (this.saving()) return;

    const email = this.emailField().trim();
    const fullName = this.fullNameField().trim();
    const roles = this.selectedRoles();
    const editing = this.editing();
    // Vai trò cuối cùng gửi lên BE — gồm cả role hệ thống giữ nguyên, nên "chưa tick ô nào" (Admin
    // sửa user SuperAdmin, bỏ hết tick Admin/User) vẫn là trạng thái HỢP LỆ, không phải lỗi rỗng.
    const finalRoles = editing ? [...this.preservedSystemRoles(), ...roles] : roles;

    // Gom HẾT lỗi rồi mới dừng, không `return` ở lỗi đầu tiên: nay mỗi ô có chỗ hiện lỗi riêng nên
    // báo một lỗi mỗi lần bấm là bắt người dùng sửa - bấm - sửa nhiều vòng không cần thiết.
    // Giá trị là KHOÁ DỊCH, không phải câu — xem `localFieldErrors`.
    const errors: Partial<Record<UserFormField, string>> = {};
    if (!email) errors.Email = 'quan-tri-nguoi-dung.error.emailRequired';
    if (!fullName) errors.FullName = 'quan-tri-nguoi-dung.error.fullNameRequired';
    if (finalRoles.length === 0) errors.Roles = 'quan-tri-nguoi-dung.error.rolesRequired';

    const userName = this.userNameField().trim();
    const tempPassword = this.tempPasswordField();
    if (!editing) {
      if (!userName) errors.UserName = 'quan-tri-nguoi-dung.error.userNameRequired';
      if (tempPassword.length < MIN_TEMP_PASSWORD_LENGTH) {
        // Không nội suy hằng số vào câu — con số hiện cho người dùng phải đến từ tham số BE gửi
        // (`messageParams`), xem chú thích tại chỗ khai hằng số ở đầu file.
        errors.TempPassword = 'quan-tri-nguoi-dung.error.tempPasswordTooShort';
      }
    }

    this.localFieldErrors.set(errors);
    if (Object.keys(errors).length > 0) return;

    if (!editing) {
      this.saved.emit({
        IsEditing: false,
        Create: { UserName: userName, Email: email, FullName: fullName, TempPassword: tempPassword, Roles: roles },
      });
      return;
    }

    this.saved.emit({
      IsEditing: true,
      Update: { Email: email, FullName: fullName, Roles: finalRoles },
    });
  }
}
