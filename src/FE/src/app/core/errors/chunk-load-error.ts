/**
 * Nhận diện lỗi "trình duyệt xin một file bundle không còn tồn tại trên máy chủ" — chuỗi sự kiện
 * xảy ra với MỌI người đang mở tab ở MỌI lần deploy, xem
 * doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md §4:
 *
 *   route tải lười → deploy → mã băm đổi → tên file cũ biến mất → nginx trả `index.html` với
 *   HTTP **200** → trình duyệt cố chạy HTML như JavaScript.
 *
 * Mã trả về 200 là lý do ca này khó chẩn đoán: mọi công cụ giám sát đều thấy bình thường.
 *
 * Nhận diện HẸP CÓ CHỦ ĐÍCH. Mỗi trình duyệt phát một câu khác nhau cho cùng một sự việc, nên
 * phải liệt kê cả bốn; nhưng bắt rộng hơn thế (vd mọi `TypeError` có chữ "fetch") sẽ hiện thông
 * báo "đã có phiên bản mới" cho những lỗi chẳng liên quan gì tới deploy — và một thông báo sai sẽ
 * dạy người dùng bỏ qua nó đúng lúc nó nói thật. Phép thử số 3 trong bảng nghiệm thu của quy ước
 * chốt đúng điều này: gây một lỗi JavaScript bất kỳ khác thì KHÔNG được hiện thông báo.
 */
const CHUNK_LOAD_ERROR_PATTERNS: readonly RegExp[] = [
  // Chromium: import động trả về file MIME `text/html` (chính là `index.html` của SPA fallback).
  /failed to fetch dynamically imported module/i,
  // Firefox.
  /error loading dynamically imported module/i,
  // Safari.
  /importing a module script failed/i,
  // Bundle không phải module + webpack-style chunk loader (`ChunkLoadError`, "Loading chunk N failed").
  /loading (css )?chunk [^\s]+ failed/i,
  /chunkloaderror/i,
];

/**
 * `SyntaxError: Unexpected token '<'` — dấu vân tay kinh điển của "trình duyệt vừa nhận HTML ở
 * chỗ đáng lẽ là JavaScript", đúng câu mà 17-phuc-vu-va-trien-khai.md §4 mô tả. Tách riêng khỏi
 * danh sách trên vì nó CHỈ được tính khi lỗi thực sự là `SyntaxError`.
 *
 * Ca dễ nhầm nhất — API trả HTML (nginx 502) rồi `JSON.parse` ném đúng câu này — KHÔNG tới được
 * đây: `HttpClient` bắt lỗi phân tích JSON và biến nó thành `HttpErrorResponse`, đi qua
 * `httpErrorInterceptor` chứ không bao giờ nổi lên `ErrorHandler` dưới dạng `SyntaxError` trần.
 */
const HTML_INSTEAD_OF_SCRIPT = /unexpected token '?</i;

function messageOf(error: unknown): string {
  if (typeof error === 'string') return error;
  if (error instanceof Error) return `${error.name}: ${error.message}`;
  if (error && typeof error === 'object' && 'message' in error) return String((error as { message: unknown }).message);
  return '';
}

/** `true` = lỗi này là "bản build trên máy chủ đã đổi", KHÔNG phải bug của ứng dụng. */
export function isChunkLoadError(error: unknown): boolean {
  // Angular gói lỗi gốc vào `rejection` (promise) hoặc `cause` (Error chuỗi) — bỏ qua lớp bọc thì
  // đúng ca này lọt lưới, vì lỗi import động luôn tới qua một promise bị từ chối.
  const wrapped =
    error && typeof error === 'object'
      ? ((error as { rejection?: unknown }).rejection ?? (error as { cause?: unknown }).cause)
      : undefined;
  if (wrapped !== undefined && wrapped !== null && isChunkLoadError(wrapped)) return true;

  const message = messageOf(error);
  if (!message) return false;
  if (CHUNK_LOAD_ERROR_PATTERNS.some((pattern) => pattern.test(message))) return true;

  const name = error instanceof Error ? error.name : '';
  return name === 'SyntaxError' && HTML_INSTEAD_OF_SCRIPT.test(message);
}
