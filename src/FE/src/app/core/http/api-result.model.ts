// Envelope response DUY NHẤT cho mọi endpoint BE — xem
// doc/huong_dan/wiki-core/fe/02-http-envelope.md và src/BE/.claude/rules/api-controller.md
// §Envelope response. Khớp 1:1 IApiResult<T> phía C# — KHÔNG tự đặt tên field khác.
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
  | 'SystemError';

export interface IApiResult<T> {
  data: T | null;
  message: string | null;
  status: ApiResultStatus;
  code: ApiErrorCode;
  businessCode: string | null;
  traceId: string | null;
  retryable: boolean | null;
  /** Key = PascalCase khớp property C# đã serialize — xem ghi chú casing ở trên. */
  fields: Record<string, string[]> | null;
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
 * CHỈ coi `null`/`undefined` là "thiếu" — `false`, `0`, `''` là giá trị hợp lệ (BE trả
 * `data: false` cho logout/lock/unlock). KHÔNG rút gọn thành `if (!res.data)`.
 */
export function unwrapData<T>(res: IApiResult<T>): T {
  if (res.data === null || res.data === undefined) {
    throw new Error(`Envelope thiếu \`data\` (traceId: ${res.traceId ?? 'không có'})`);
  }
  return res.data;
}
