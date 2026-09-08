import { DOCUMENT } from '@angular/common';
import { ErrorHandler, Injectable, inject, signal } from '@angular/core';
import { isChunkLoadError } from './chunk-load-error';

/**
 * `ErrorHandler` toàn cục — điểm cuối của mọi lỗi JavaScript không ai bắt trong app.
 *
 * Việc DUY NHẤT nó làm khác hành vi mặc định: nhận ra ca "bản build trên máy chủ đã đổi" và bật
 * một dải mời tải lại (`App` render nó, xem app.html). Mọi lỗi khác vẫn chỉ được ghi log như cũ —
 * xem `isChunkLoadError` để biết vì sao nhận diện phải hẹp.
 *
 * **KHÔNG tự động tải lại** (chốt 2026-08-31,
 * doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md §4). Tự tải lại nghe mượt hơn nhưng vứt
 * mất dữ liệu người dùng đang nhập — đúng thứ mà `unsavedChangesGuard`
 * (fe/09-forms-validation.md §"Form dirty + điều hướng đi") tồn tại để bảo vệ. Hai quyết định
 * cùng ngày mà ngược nhau thì quyết định yếu hơn sẽ thắng trong lúc thi công, nên ghi lại ở đây:
 * đây là lựa chọn có cân nhắc, không phải chỗ còn thiếu.
 */
@Injectable({ providedIn: 'root' })
export class GlobalErrorHandler implements ErrorHandler {
  private readonly document = inject(DOCUMENT);
  private readonly staleBuild = signal(false);

  /** `true` = máy chủ đang chạy một bản build khác với bản tab này tải về. Một chiều, không tắt
   * lại được: tab hiện tại không có cách nào tự lành, chỉ tải lại mới thoát ra được. */
  readonly newVersionAvailable = this.staleBuild.asReadonly();

  handleError(error: unknown): void {
    if (isChunkLoadError(error)) {
      this.staleBuild.set(true);
    }
    // Vẫn log NGUYÊN error kể cả ca đã nhận diện được: người trực sự cố cần thấy nó trong console
    // và trong công cụ giám sát, đúng như `ErrorHandler` mặc định của Angular vẫn làm.
    console.error(error);
  }

  /** Người dùng bấm nút — đường thoát duy nhất khỏi một tab đang giữ bản build đã biến mất. */
  reloadApp(): void {
    this.document.defaultView?.location.reload();
  }
}
