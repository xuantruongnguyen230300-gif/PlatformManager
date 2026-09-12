// Envelope response DUY NHẤT cho mọi endpoint BE — xem
// doc/huong_dan/wiki-core/fe/02-http-envelope.md và doc/huong_dan/quy-uoc/be-api-controller.md
// §"Envelope response". Khớp 1:1 IApiResult<T> phía C# — KHÔNG tự đặt tên field khác.
//
// Casing: envelope này serialize camelCase (đã CHỐT LẠI 2026-08-15, khác bản cũ PascalCase
// đã xoá). `fields` là NGOẠI LỆ duy nhất — key bên trong dictionary đó vẫn PascalCase vì khớp
// tên property C# (`FluentValidation` group theo `PropertyName`, không đi qua naming policy áp
// dụng cho tên thuộc tính object — chỉ áp dụng cho property CỦA object, không áp dụng cho KEY
// của Dictionary<string,...>). Đừng lowercase nhầm khi đọc `fields`.

export type ApiResultStatus = 'SUCCESS' | 'VALIDATION_ERROR' | 'BUSINESS_ERROR' | 'SYSTEM_ERROR';

export type ApiErrorCode =
  | 'Success'
  | 'ValidationError'
  | 'AuthenticationError'
  | 'AuthorizationError'
  | 'NotFound'
  | 'Conflict'
  | 'BusinessRuleError'
  // 429 — BE trả envelope này từ `RateLimiterOptions.OnRejected` (Program.cs), KHÔNG phải body
  // rỗng mặc định của middleware. Thiếu nhánh này thì mọi `switch` trên `code` bỏ sót 429 mà
  // TypeScript KHÔNG báo (union hẹp hơn payload thật = type nói dối về dữ liệu chạy thật).
  // Đi kèm `retryable: true` + header `Retry-After` — xem ghi chú `retryable` bên dưới.
  | 'TooManyRequests'
  // 405 — BE trả envelope này từ `ApiStatusCodeEnvelopeMiddleware` (thêm 2026-09-04) kèm
  // `businessCode: 'ROUTE.METHOD_NOT_ALLOWED'` và giữ nguyên header `Allow`. Cùng lý do với
  // 'TooManyRequests' ở trên: thiếu nhánh thì `switch` bỏ sót mà TypeScript KHÔNG báo.
  // Khác `NotFound` của handler: đây là "đường dẫn đúng, verb sai" — thường là bug của client.
  | 'MethodNotAllowed'
  | 'SystemError';

/**
 * Một lỗi validate của MỘT ô nhập, dạng **mã + câu** — phần tử của `fieldErrors`.
 *
 * `code` là `ValidationFailure.ErrorCode` của FluentValidation phía BE, tức TÊN VALIDATOR
 * (`NotEmptyValidator`, `InclusiveBetweenValidator`, `PredicateValidator`…), KHÔNG theo khuôn
 * `MIEN.MA_LOI` của `businessCode`. Hai hệ mã nằm ở hai trường khác nhau có chủ đích — xem
 * `src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiFieldError.cs`.
 *
 * BE dùng `'UnspecifiedValidator'` khi rule `Custom` không cấp mã nào, nên `code` LUÔN là chuỗi
 * không rỗng — không phải kiểm null ở phía này.
 */
export interface ApiFieldError {
  code: string;
  /**
   * ⚠️ **KHÔNG phải lúc nào cũng là một câu** — đừng hiển thị thẳng trường này.
   *
   * Nó là fallback **chỉ ở nơi nguồn vốn đã có sẵn một câu**, tức nhánh FluentValidation. Với mã
   * ASP.NET Core Identity, `IdentityFieldErrors.Build` đặt `message` **bằng chính `code`**
   * (`src/BE/Core/PlatformManager.Core.Application/Common/Results/IdentityFieldErrors.cs`) — đó là
   * **CHỦ ĐÍCH, không phải nợ chưa trả**: kênh có FE là kênh FE sở hữu câu chữ, BE không dựng bộ
   * chữ thứ hai (`doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md` §3). Lời hứa của trường này ở
   * phía BE đã được thu hẹp đúng như vậy ngày 2026-09-11, nên hai file nay nói cùng một điều.
   *
   * Hệ quả cứng: nơi tiêu thụ DUY NHẤT được phép là `ApiErrorMessageService.fieldMessage`, và nó
   * kiểm `message !== code` trước khi dùng. Viết `error.message` thẳng vào template là đẩy chuỗi
   * `"CommonPassword"` ra màn hình — đã xảy ra thật, xem bậc 2 ở hàm đó.
   */
  message: string;
  /**
   * Tham số của câu mà `code` trỏ tới (vd `{ MinLength: '12' }` cho `MinimumLengthValidator`) —
   * **cùng tên, cùng kiểu, cùng ý nghĩa** với `messageParams` ở gốc envelope. Đó là MỘT cơ chế
   * đặt ở hai chỗ, không phải hai cơ chế: `messageParams` luôn nằm cạnh **mã mà nó tham số hoá**,
   * nên nơi tiêu thụ viết đúng một hàm ráp câu rồi gọi ở cả hai chỗ. Vì sao không gom hết về gốc
   * envelope: một lần submit hỏng nhiều ô thì hai ô cùng fail một validator sẽ ghi đè khoá của
   * nhau — câu của ô này hiện con số của ô kia, hỏng IM LẶNG. Hình dạng đầy đủ ở
   * doc/huong_dan/quy-uoc/be-api-controller.md §`messageParams`.
   *
   * ✅ **ĐANG DÙNG từ 2026-09-05** — `ApiErrorMessageService.fieldMessage`
   * (`core/i18n/api-error-message.service.ts`) tra `code` ra khoá dịch rồi ráp từ điển này vào câu.
   * Nơi tiêu thụ đầu tiên là màn đăng nhập.
   */
  messageParams?: Record<string, string> | null;
}

export interface IApiResult<T> {
  data: T | null;
  message: string | null;
  status: ApiResultStatus;
  code: ApiErrorCode;
  businessCode: string | null;
  traceId: string | null;
  /**
   * BE đánh dấu lỗi "thử lại được" (hiện chắc chắn có với `TooManyRequests`, xem
   * `RateLimitErrors.cs`). Nơi gọi CÓ THỂ dùng cờ này để mời người dùng thử lại thay vì chỉ báo
   * lỗi; hiện `httpErrorInterceptor` mới chỉ hiện toast — chưa có toast kèm hành động.
   */
  retryable: boolean | null;
  /** Key = PascalCase khớp property C# đã serialize — xem ghi chú casing ở trên. */
  fields: Record<string, string[]> | null;
  /**
   * Cùng tập lỗi với `fields`, cùng bộ khoá, chỉ khác phần tử: mỗi lỗi nay mang **mã + câu** thay
   * vì chuỗi trần (BE thêm 2026-09-03 — xem `doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`
   * §4(b)).
   *
   * ✅ **ĐANG DÙNG từ 2026-09-05, ở ĐÚNG MỘT màn hình.** `platform/login` đọc trường này để dịch
   * lỗi của từng ô (`LoginPage.fieldMessages`) — nó là trường DUY NHẤT mang `code` và
   * `messageParams`, tức trường duy nhất dịch được. Mọi form còn lại vẫn đọc `fields`. Hai trường
   * cố ý chạy song song cho tới bước 11 của §7 (gỡ `fields` ở CẢ hai phía) — đổi shape một nhát
   * tạo ra khoảnh khắc FE cũ gặp BE mới, mà envelope là thứ mọi màn hình đi qua.
   *
   * **Casing bên trong lệch nhau, và đó là hợp đồng thật:** KHOÁ của dictionary giữ PascalCase
   * (`'PageSize'`, cùng quy ước với `fields`) nhưng property của phần tử là camelCase (`code`,
   * `message`) — vì chỉ khoá dictionary mới nằm ngoài naming policy. Đừng "sửa cho đều".
   *
   * Khai **optional** chứ không phải `| null` như `fields`: trường này vắng mặt trong mọi envelope
   * không phải nhánh 400, và ~13 object literal `IApiResult` trong spec FE đang dựng envelope đầy
   * đủ — bắt buộc chúng khai thêm một trường chưa ai đọc là chi phí không đổi lấy gì. Khi bước 11
   * chạy và FE thật sự đọc trường này, siết lại thành bắt buộc.
   */
  fieldErrors?: Record<string, ApiFieldError[]> | null;
  /**
   * Tham số của câu mà `businessCode` trỏ tới — vd `{ UserName: 'abc' }` đi kèm
   * `USER.DUPLICATE_USERNAME`. **Vắng mặt hoàn toàn** khi mã không có tham số (vắng chứ không
   * phải rỗng: envelope không phình thêm một khoá cho mọi lỗi, cùng cách `retryable`/`fields`
   * đang làm).
   *
   * **Khoá là TÊN tham số, không phải số thứ tự** (`{UserName}` chứ không `{0}`): trật tự từ mỗi
   * ngôn ngữ một khác, nên bảng dịch `en` phải được phép đảo tham số mà không cần biết BE truyền
   * theo thứ tự nào. Khoá giữ PascalCase — khoá của Dictionary nằm NGOÀI naming policy, cùng lý do
   * với `fields`/`fieldErrors` (xem ghi chú casing ở đầu file); đừng camelCase lại khi tra.
   *
   * **Giá trị LUÔN là chuỗi**, BE đã đổi sang chuỗi bằng văn hoá invariant. Cái giá đã biết và
   * chấp nhận: FE KHÔNG định dạng lại số/ngày theo locale được — ở quy mô này tham số hầu hết là
   * số nguyên nhỏ và tên riêng nên không đổi gì. Đây không phải sót, đừng sửa thành `unknown`
   * rồi định dạng lại: kiểu `object` mở sẵn một đường rò dữ liệu (doc BE §10.4).
   *
   * ✅ **ĐANG DÙNG từ 2026-09-05.** `ApiErrorMessageService.messageFor` ráp từ điển này vào câu lấy
   * từ BẢNG DỊCH theo `businessCode` — không thay hằng số của FE vào chỗ tham số (fe/08-i18n.md
   * §Khuôn khoá dịch §7). Khi `businessCode` chưa có bản dịch, đường lùi vẫn là `message` nguyên
   * văn của BE (câu đó BE đã tự ráp sẵn tham số vào rồi).
   *
   * Khai **optional** chứ không `| null` như `fields`: cùng lý do đã ghi ở `fieldErrors` — trường
   * này vắng mặt ở phần lớn envelope, bắt mọi object literal trong spec khai thêm một trường chưa
   * ai đọc là chi phí không đổi lấy gì.
   */
  messageParams?: Record<string, string> | null;
}

/**
 * `HttpErrorResponse` sau khi qua `httpErrorInterceptor` được gắn thêm `apiResult` (nếu server
 * trả đúng envelope) để nơi gọi (thường là form) tự đọc `fields`/`businessCode` khi cần xử lý
 * riêng — xem `core/interceptors/http-error.interceptor.ts`.
 */
export interface IHttpErrorWithApiResult {
  apiResult?: IApiResult<unknown> | null;
}

/**
 * Đọc `data` khỏi envelope, ném lỗi rõ ràng nếu thiếu — dùng ở MỌI service gọi HTTP thay cho
 * `res.data as T` / `res.data!` vốn tắt hẳn kiểm tra kiểu và biến `data` vắng mặt thành lỗi vô
 * nghĩa ("cannot read property of undefined") ở tận trong mapper.
 *
 * CHỈ coi `null`/`undefined` là "thiếu" — `false`, `0`, `''` là giá trị hợp lệ. KHÔNG rút gọn
 * thành `if (!res.data)`: envelope `IApiResult<boolean>`/`<number>` hợp lệ mang `data: false`
 * hoặc `data: 0`, và `!res.data` sẽ ném lỗi "thiếu data" cho một response hoàn toàn bình thường.
 *
 * Cập nhật 2026-08-29 — bản trước nêu ví dụ "BE trả `data: false` cho logout/lock/unlock", nay
 * SAI ở cả ba: `LogoutHandler` luôn `Ok(true)`; lock/unlock thất bại nay trả **422** kèm
 * `businessCode` `USER.LOCK_FAILED` / `USER.UNLOCK_FAILED` (xem
 * `src/BE/Core/PlatformManager.Core.Application/Users/UserErrors.cs`) chứ không còn 200 +
 * `data: false`. Luật vẫn giữ nguyên và vẫn cần: nó bảo vệ ở mức KIỂU, không phụ thuộc endpoint
 * nào hiện đang trả falsy — `api-result.model.spec.ts` khoá hành vi này bằng test.
 */
export function unwrapData<T>(res: IApiResult<T>): T {
  if (res.data === null || res.data === undefined) {
    throw new Error(`Envelope thiếu \`data\` (traceId: ${res.traceId ?? 'không có'})`);
  }
  return res.data;
}
