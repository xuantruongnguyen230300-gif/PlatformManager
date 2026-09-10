import { ApiFieldError } from './api-result.model';

/**
 * Gom `fieldErrors` của envelope lỗi về đúng khoá ô trên form.
 *
 * Ba việc, không việc nào bỏ được:
 *  1. **Bỏ hậu tố chỉ số.** `RuleForEach(x => x.Roles)` của FluentValidation phát `PropertyName`
 *     dạng `Roles[0]`, `Roles[1]` (BE giữ nguyên chuỗi này, xem `GlobalExceptionHandler`
 *     `NormalizeField` — chỉ cắt tiền tố `Request.`, không đụng chỉ số). Tra thẳng
 *     `fieldErrors['Roles']` sẽ trượt và lỗi biến mất im lặng: form từ chối mà không ô nào đỏ,
 *     không gì báo.
 *  2. **Dịch từng lỗi qua callback `translate`.** Phần tử của `fieldErrors` là `ApiFieldError`
 *     (mã lỗi + tham số), KHÔNG phải câu tiếng Việt dựng sẵn — nơi gọi truyền vào
 *     `ApiErrorMessageService.fieldMessage`. Hàm này cố ý KHÔNG tự inject service dịch: nó ở tầng
 *     đáy `core/http/`, và nhận callback giữ nó thuần (test được không cần TestBed) đồng thời để
 *     nơi gọi quyết định cách dịch.
 *  3. **Gộp nhiều thông điệp cho cùng một ô** thành một chuỗi — mỗi ô chỉ có một chỗ để hiện.
 *
 * ⚠️ **Tham số là `fieldErrors`, KHÔNG phải `fields`.** Envelope mang hai trường lỗi theo ô, cố ý
 * chạy song song cho tới bước 11 của `doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md` §7:
 * `fields` là chuỗi trần BE đã dựng sẵn, `fieldErrors` là mã + tham số. Chỉ trường thứ hai dịch
 * được. Đọc nhầm `result.fields` thì code vẫn chạy, câu vẫn hiện — chỉ mất sạch mã lỗi và mọi
 * bản dịch, tức hỏng IM LẶNG. Tên tham số ở đây đổi từ `fields` → `fieldErrors` ngày 2026-09-10
 * vì chính nó đang dạy sai tên trường (xem `api-result.model.ts` — `fields` dòng 77,
 * `fieldErrors` dòng 98).
 *
 * ## Vì sao ở `core/http/` chứ không ở feature
 *
 * Cho tới 2026-09-10 hàm này nằm chôn (không export) trong `platform/quan-tri-nguoi-dung/`
 * `components/user-form-dialog/user-form-dialog.ts` — file component của MỘT màn hình. Nó không
 * biết gì về màn đó: đầu vào là `fieldErrors` của envelope, thứ mà MỌI form gặp lỗi 400 đều nhận.
 * Đặt ở đây, cạnh `api-result.model.ts` nơi `ApiFieldError` được khai, vì đó là chỗ hợp đồng
 * envelope sống.
 *
 * Lý do nâng lên, không phải "cho gọn": người viết dialog thứ hai hoặc chép lại hàm, hoặc bỏ sót
 * mẹo (1) — và bỏ sót mẹo (1) hỏng IM LẶNG (xem lại điểm 1). Một bản sao thứ hai cũng là hai chỗ
 * phải sửa khi khuôn `PropertyName` của FluentValidation đổi.
 *
 * `core/` là tầng đáy (gate G9): file này chỉ được import trong chính `core/`, không bao giờ
 * import ngược lên `shared/` / `platform/` / `modules/`.
 *
 * Luật: doc/huong_dan/wiki-core/fe/09-forms-validation.md §"Bind lỗi từ `fieldErrors` vào form".
 */
export function groupServerFieldErrors(
  fieldErrors: Record<string, ApiFieldError[]> | null,
  translate: (error: ApiFieldError) => string,
): Record<string, string> {
  if (!fieldErrors) return {};
  const grouped: Record<string, string[]> = {};
  for (const [rawKey, errors] of Object.entries(fieldErrors)) {
    // `Roles[0]` / `Roles[1]` là lỗi của cùng MỘT nhóm ô — gộp về `Roles` để hiện một chỗ.
    const key = rawKey.replace(/\[\d+\]$/, '');
    grouped[key] = [...(grouped[key] ?? []), ...errors.map(translate)];
  }
  return Object.fromEntries(Object.entries(grouped).map(([key, messages]) => [key, messages.join(' ')]));
}
