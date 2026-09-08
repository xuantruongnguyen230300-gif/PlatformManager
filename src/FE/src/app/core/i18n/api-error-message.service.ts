import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ApiFieldError, IApiResult } from '../http/api-result.model';

/**
 * Khoá dịch cho câu dự phòng theo mã HTTP — dùng khi response KHÔNG mang envelope hợp lệ (mạng
 * hỏng, CORS chặn, `ProblemDetails` từ model-binding, 429 do reverse proxy sinh ra trước khi tới
 * app). Đây là đường LÙI, không phải đường chính: đường chính là `businessCode` của envelope.
 *
 * Đặt ở `core/i18n/` chứ không ở interceptor vì `LoginPage` cũng cần đúng bộ câu này cho khối lỗi
 * inline — hai nơi hiển thị, MỘT bộ câu.
 */
const FALLBACK_KEYS: Readonly<Record<number, { title: string; text: string }>> = {
  0: { title: 'shared.httpError.offlineTitle', text: 'shared.httpError.offline' },
  401: { title: 'shared.httpError.unauthenticatedTitle', text: 'shared.httpError.unauthenticated' },
  403: { title: 'shared.httpError.forbiddenTitle', text: 'shared.httpError.forbidden' },
  404: { title: 'shared.httpError.notFoundTitle', text: 'shared.httpError.notFound' },
  429: { title: 'shared.httpError.tooManyTitle', text: 'shared.httpError.tooMany' },
};

/**
 * Nhánh chứa bản dịch của `fieldErrors[].code`.
 *
 * `code` ở đó là TÊN VALIDATOR của FluentValidation (`NotEmptyValidator`, `MinimumLengthValidator`…
 * — xem `ApiFieldError` trong `core/http/api-result.model.ts`), tức một mã do **BE sở hữu**, viết
 * PascalCase và KHÔNG có dấu chấm. Cho nó vào nhánh `VALIDATION` — nhánh vốn đã có `VALIDATION.FAILED`
 * — thay vì thả ở cấp gốc, vì hai lý do:
 *
 *  · cấp gốc của bảng dịch đang là nơi phân biệt HAI HỌ KHOÁ bằng kiểu chữ (VIẾT HOA = BE sở hữu,
 *    chữ thường = FE sở hữu — doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch). Một nhánh
 *    `NotEmptyValidator` PascalCase ở cấp gốc không thuộc họ nào và làm hỏng quy tắc "nhìn chữ
 *    HOA/thường là biết ai sở hữu";
 *  · gom lại một nhánh thì `grep '"VALIDATION"' -A` trả về trọn bộ câu lỗi validate.
 *
 * ⚠️ Đây là chỗ doc CHƯA chốt (khuôn khoá dịch chỉ nói về `businessCode`, không nói về
 * `fieldErrors[].code`) — xem báo cáo bàn giao, cần người dùng xác nhận.
 */
const FIELD_ERROR_KEY_PREFIX = 'VALIDATION.';

const UNEXPECTED_KEYS = {
  title: 'shared.httpError.unexpectedTitle',
  text: 'shared.httpError.unexpected',
} as const;

/**
 * NƠI DUY NHẤT của FE biến một lỗi HTTP thành câu người dùng đọc được.
 *
 * ## Vì sao là MỘT chỗ, và vì sao nó là service chứ không phải hàm trong interceptor
 *
 * doc/huong_dan/wiki-core/fe/08-i18n.md §"Chuỗi BE trả về" chốt *"chỗ đặt bản dịch: đúng một nơi —
 * interceptor lỗi… Không rải việc tra mã ra từng feature"*. Nhưng màn đăng nhập hiển thị lỗi ở
 * **hai** kênh độc lập (toast của interceptor + khối `.login-error` inline —
 * doc/Design/Frontend/PlatformManager/Screens/05-auth.md §States, ca `server-error`), nên nếu logic
 * tra mã nằm TRONG interceptor thì kênh thứ hai buộc phải chép lại nó. Tách ra service giữ đúng
 * tinh thần "một nơi": một lớp, hai nơi gọi — không phải hai bản logic.
 *
 * Cùng khuôn với ràng buộc phía BE (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.1):
 * *"FE viết đúng MỘT hàm ráp câu và gọi nó ở cả hai chỗ"* — hai chỗ ở đó là `messageParams` cạnh
 * `businessCode` và `messageParams` cạnh `code` trong từng `fieldErrors`. Cả hai đi qua
 * {@link translateCode} bên dưới.
 *
 * ## Thứ tự ưu tiên khi dựng câu — có chủ đích, đừng đảo
 *
 * 1. `businessCode` có bản dịch  → dùng bản dịch (đã ráp `messageParams`).
 * 2. `message` của envelope      → dùng nguyên văn của BE.
 * 3. câu dự phòng theo mã HTTP.
 *
 * Bước 2 là **trạng thái tạm được ghi rõ** ở doc/huong_dan/wiki-core/fe/08-i18n.md §"Ba lỗi tiên
 * quyết" ý 2: mã lỗi mới thêm ở BE mà quên thêm bản dịch ở FE thì người dùng bản tiếng Anh đọc
 * một câu tiếng Việt — xấu, nhưng vẫn nói đúng chuyện gì xảy ra. Bỏ bước 2 để "ép phải dịch" là
 * đổi một lỗi hiển thị lấy một lỗi mất thông tin.
 */
@Injectable({ providedIn: 'root' })
export class ApiErrorMessageService {
  private readonly translate = inject(TranslateService);

  /**
   * Tra một mã (`businessCode` dạng `AUTH.INVALID_CREDENTIALS`, hoặc `fieldErrors[].code` dạng
   * `NotEmptyValidator`) ra câu đã dịch, hoặc `null` khi bảng dịch không có mã đó.
   *
   * 🛑 Phép kiểm "có bản dịch không" phải là **so với chính chuỗi khoá**: ngx-translate KHÔNG ném
   * và KHÔNG trả `null` khi tra trượt — nó trả về đúng chuỗi khoá, nên `AUTH.INVALID_CREDENTIALS`
   * sẽ hiện giữa giao diện thay vì một câu (doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch
   * §1). Không có phép so này thì đường lùi về `message` của envelope không bao giờ chạy.
   *
   * Cũng phải kiểm `typeof === 'string'`: tra một khoá là NHÁNH (`'AUTH'`) trả về cả object con.
   * Ca đó xảy ra thật nếu BE lỡ trả `businessCode: 'AUTH'`, và ép nó vào chỗ chờ chuỗi sẽ cho ra
   * `[object Object]` trên màn hình.
   *
   * @param params `messageParams` của envelope — khoá là TÊN tham số (`{{UserName}}`), khớp thẳng
   *   cú pháp nội suy của ngx-translate nên không phải quy đổi gì. Đó chính là lý do BE chọn khoá
   *   tên thay vì `{0}`/`{1}` (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.2).
   */
  translateCode(code: string | null | undefined, params?: Record<string, string> | null): string | null {
    if (!code) return null;
    const text = this.translate.instant(code, params ?? undefined);
    return typeof text === 'string' && text !== code ? text : null;
  }

  /**
   * Câu chính để hiển thị cho một lỗi HTTP. `status` chỉ được dùng ở bước 3 (đường lùi).
   */
  messageFor(result: IApiResult<unknown> | null | undefined, status: number): string {
    return (
      this.translateCode(result?.businessCode, result?.messageParams) ??
      result?.message ??
      this.fallbackText(status)
    );
  }

  /**
   * Tiêu đề ngắn đi kèm — **chỉ** cho đường lùi, `undefined` khi envelope có câu thật.
   *
   * Giữ nguyên quyết định đã có ở `http-error.interceptor.ts`: khi envelope mang câu do BE soạn
   * cho đúng nghiệp vụ đang lỗi thì interceptor không biết gì hơn để đặt tên cho nó, và một tiêu
   * đề chung chung ("Lỗi") chỉ chiếm chỗ mà không thêm thông tin.
   */
  titleFor(result: IApiResult<unknown> | null | undefined, status: number): string | undefined {
    const hasSentence = this.translateCode(result?.businessCode, result?.messageParams) ?? result?.message;
    return hasSentence ? undefined : this.fallbackTitle(status);
  }

  /**
   * Dịch một phần tử `fieldErrors` — cùng cơ chế, chỉ khác chỗ đặt (§10.1 của doc BE).
   *
   * @param paramOverrides ghi đè lên `messageParams` của BE. Có mặt vì một tham số rất hay gặp,
   *   `PropertyName`, do FluentValidation sinh ra từ TÊN PROPERTY C# (`UserName` → `"User Name"`)
   *   — một chuỗi tiếng Anh, không dịch được, và đem ghép vào câu tiếng Việt sẽ ra *"Vui lòng nhập
   *   User Name."*. Màn hình biết nhãn của ô đó bằng ngôn ngữ đang chọn (`shared.field.userName`)
   *   nên nó truyền nhãn ấy vào đây. Đây KHÔNG phải "rải việc tra mã ra feature": mã vẫn tra ở
   *   lớp này, feature chỉ cấp NHÃN ô nhập — thứ vốn đã là của màn hình.
   */
  fieldMessage(fieldError: ApiFieldError, paramOverrides?: Record<string, string>): string {
    const params = { ...(fieldError.messageParams ?? {}), ...(paramOverrides ?? {}) };
    const key = `${FIELD_ERROR_KEY_PREFIX}${fieldError.code}`;
    // `translateCode` so kết quả với chuỗi KHOÁ, nên khi bảng dịch chưa có validator này thì nó
    // trả null và ta lùi về câu của BE — người dùng đọc một câu tiếng Việt thay vì
    // "VALIDATION.GreaterThanValidator".
    return this.translateCode(key, params) ?? fieldError.message;
  }

  private fallbackText(status: number): string {
    return this.translate.instant(FALLBACK_KEYS[status]?.text ?? UNEXPECTED_KEYS.text) as string;
  }

  private fallbackTitle(status: number): string {
    return this.translate.instant(FALLBACK_KEYS[status]?.title ?? UNEXPECTED_KEYS.title) as string;
  }
}
