import { ApiFieldError } from './api-result.model';

/**
 * Khoá mà BE dùng cho lỗi KHÔNG thuộc ô nhập nào — phải khớp `IdentityFieldErrors.RecordKey`
 * phía BE, ký tự `$` mở đầu là có chủ đích (không định danh C# nào bắt đầu bằng nó).
 */
export const RECORD_FIELD_KEY = '$record';

/**
 * Lỗi theo ô và lỗi mức bản ghi, **tách rời** — hình dạng trả về của {@link groupServerFieldErrors}.
 *
 * Trả một object hai nhánh thay vì một `Record<string, string>` phẳng là quyết định có chủ đích:
 * nó biến "quên hiển thị lỗi mức bản ghi" từ một lỗi IM LẶNG thành một lỗi BIÊN DỊCH. Nơi gọi
 * buộc phải nhìn thấy `record` khi rã object ra, thay vì nhận một khoá `$record` mà template của
 * nó không có chỗ nào bind tới.
 */
export interface IServerFieldErrorView {
  /**
   * Khoá = tên ô trên form (PascalCase, đã cắt hậu tố chỉ số). KHÔNG bao giờ chứa `$record`.
   *
   * 🛑 Tên là `byField`, **không phải `fields`** — và đó không phải chuyện thẩm mỹ. `fields` là tên
   * một TRƯỜNG KHÁC của envelope (chuỗi trần BE dựng sẵn, `api-result.model.ts` dòng 77), trường
   * đang trên đường bị gỡ ở bước 3 của
   * doc/huong_dan/wiki-core/fe/02-http-envelope.md §"Trình tự expand/contract". Đặt lại đúng cái
   * tên ấy lên chữ ký công khai của tầng đáy là dạy sai tên trường cho người viết form tiếp theo —
   * đúng lý do **tham số** của hàm này đã phải đổi `fields` → `fieldErrors` ngày 2026-09-10.
   *
   * Hệ quả đo được nếu đặt sai: lệnh nghiệm thu ở `02-http-envelope.md` §"Nghiệm thu bước 2"
   * (*"phải in rỗng"*) chuyển sang luôn in 2 dòng dương tính giả — tức một lệnh mà lượt sau người
   * ta bỏ qua.
   */
  readonly byField: Record<string, string>;
  /** Câu cho lỗi không thuộc ô nào, hoặc `null`. Nơi gọi hiện nó ở khối lỗi chung của form. */
  readonly record: string | null;
}

/**
 * Gom `fieldErrors` của envelope lỗi về đúng khoá ô trên form.
 *
 * Bốn việc, không việc nào bỏ được:
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
 *  4. **Tách riêng lỗi mức BẢN GHI** ({@link RECORD_FIELD_KEY}) ra khỏi lỗi theo ô — xem khối
 *     🛑 ngay dưới đây. Đây là việc thêm 2026-09-11.
 *
 * ⚠️ **Tham số là `fieldErrors`, KHÔNG phải `fields`.** Envelope mang hai trường lỗi theo ô, cố ý
 * chạy song song cho tới bước 11 của `doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md` §7:
 * `fields` là chuỗi trần BE đã dựng sẵn, `fieldErrors` là mã + tham số. Chỉ trường thứ hai dịch
 * được. Đọc nhầm sang trường `fields` thì code vẫn chạy, câu vẫn hiện — chỉ mất sạch mã lỗi và mọi
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
 * ## 🛑 `$record` — cái bẫy mà việc (4) sinh ra để bịt
 *
 * BE gom lỗi KHÔNG thuộc ô nhập nào về khoá `"$record"`
 * (`src/BE/Core/PlatformManager.Core.Application/Common/Results/IdentityFieldErrors.cs`,
 * `RecordKey`) — cả lỗi mức bản ghi (`ConcurrencyFailure`) lẫn **mọi mã Identity chưa có trong
 * allowlist ánh xạ ô** của nó. Khoá đó cố ý bắt đầu bằng `$` để không bao giờ đụng tên property C#.
 *
 * Trước 2026-09-11, hàm này đổ `$record` vào cùng một `Record<string, string>` với lỗi theo ô, và
 * hậu quả là một **lỗ đen kép** — đã xảy ra thật với mã `CommonPassword`:
 *
 *  · template không có ô nào tên `$record` nên câu đó **không hiện ở đâu cả**; và
 *  · khối lỗi chung ở cuối form thì lại ẨN ĐI, vì cả hai màn dùng cùng một luật *"chỉ hiện câu
 *    chung khi KHÔNG có lỗi ô nào"* — mà `$record` khi đó được đếm là "một lỗi ô".
 *
 * Kết quả người dùng thấy: form từ chối, một toast chung chung, và **không một chữ nào** nói vì sao.
 * Hai điều kiện đều cần thiết để hỏng; sửa một mình cái nào cũng không đủ.
 *
 * Luật: doc/huong_dan/wiki-core/fe/09-forms-validation.md §"Bind lỗi từ `fieldErrors` vào form".
 */
export function groupServerFieldErrors(
  fieldErrors: Record<string, ApiFieldError[]> | null,
  translate: (error: ApiFieldError) => string,
): IServerFieldErrorView {
  if (!fieldErrors) return { byField: {}, record: null };
  const grouped: Record<string, string[]> = {};
  for (const [rawKey, errors] of Object.entries(fieldErrors)) {
    // `Roles[0]` / `Roles[1]` là lỗi của cùng MỘT nhóm ô — gộp về `Roles` để hiện một chỗ.
    const key = rawKey.replace(/\[\d+\]$/, '');
    grouped[key] = [...(grouped[key] ?? []), ...errors.map(translate)];
  }

  const { [RECORD_FIELD_KEY]: recordMessages, ...perField } = grouped;
  return {
    byField: Object.fromEntries(Object.entries(perField).map(([key, messages]) => [key, messages.join(' ')])),
    record: recordMessages?.length ? recordMessages.join(' ') : null,
  };
}
