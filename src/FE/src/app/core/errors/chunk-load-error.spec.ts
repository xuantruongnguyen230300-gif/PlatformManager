import { isChunkLoadError } from './chunk-load-error';

/**
 * Nhận diện phải ĐÚNG CẢ HAI CHIỀU. Bắt sót ⇒ màn hình chết không một chữ giải thích sau mỗi lần
 * deploy; bắt bừa ⇒ mọi bug JavaScript đều hiện "đã có phiên bản mới", và người dùng học được
 * rằng câu đó vô nghĩa đúng lúc nó nói thật (phép thử số 3 của bảng nghiệm thu,
 * doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md §4).
 */
describe('isChunkLoadError', () => {
  it('nhận ra câu của Chromium — import động nhận về `index.html` của SPA fallback', () => {
    const error = new TypeError(
      'Failed to fetch dynamically imported module: https://app.example.vn/chunk-BWN5TTLZ.js',
    );
    expect(isChunkLoadError(error)).toBeTrue();
  });

  it('nhận ra câu của Firefox và của Safari — cùng một sự việc, ba câu chữ khác nhau', () => {
    expect(isChunkLoadError(new TypeError('error loading dynamically imported module'))).toBeTrue();
    expect(isChunkLoadError(new TypeError('Importing a module script failed.'))).toBeTrue();
  });

  it('nhận ra `ChunkLoadError` và "Loading chunk … failed"', () => {
    const error = new Error('Loading chunk 42 failed. (missing: /chunk-42.js)');
    error.name = 'ChunkLoadError';
    expect(isChunkLoadError(error)).toBeTrue();
  });

  it("nhận ra `SyntaxError: Unexpected token '<'` — HTML bị chạy như JavaScript", () => {
    expect(isChunkLoadError(new SyntaxError("Unexpected token '<'"))).toBeTrue();
  });

  it('đi qua được lớp bọc `rejection`/`cause` — lỗi import động luôn tới qua promise bị từ chối', () => {
    const inner = new TypeError('Failed to fetch dynamically imported module: /chunk-1.js');
    expect(isChunkLoadError({ rejection: inner })).toBeTrue();
    expect(isChunkLoadError(new Error('Uncaught (in promise)', { cause: inner }))).toBeTrue();
  });

  it('🛑 KHÔNG bắt lỗi JavaScript thường — đây là vế dễ hỏng nhất của việc này', () => {
    expect(isChunkLoadError(new TypeError("Cannot read properties of undefined (reading 'Id')"))).toBeFalse();
    expect(isChunkLoadError(new ReferenceError('x is not defined'))).toBeFalse();
    expect(isChunkLoadError(new Error('Http failure response for /users: 500 Server Error'))).toBeFalse();
    expect(isChunkLoadError('Đã có lỗi xảy ra')).toBeFalse();
    expect(isChunkLoadError(null)).toBeFalse();
    expect(isChunkLoadError(undefined)).toBeFalse();
  });

  it("🛑 `Unexpected token '<'` KHÔNG phải SyntaxError thì không tính", () => {
    // Cùng câu chữ, khác bản chất: chỉ `SyntaxError` mới là "trình duyệt đang cố PHÂN TÍCH một
    // đoạn HTML như mã nguồn".
    expect(isChunkLoadError(new TypeError("Unexpected token '<'"))).toBeFalse();
  });
});
