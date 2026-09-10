import { Injectable, signal } from '@angular/core';

export type ToastSeverity = 'success' | 'error' | 'info' | 'warn';

export interface IToastMessage {
  Id: number;
  Severity: ToastSeverity;
  /**
   * Tiêu đề ngắn in đậm trên dòng đầu — KHÔNG bắt buộc. Thiết kế mới
   * (doc/Design/Frontend/PlatformManager/Prototypes/index.html §"15 · Thông báo nổi") tách toast
   * thành tiêu đề + mô tả để đọc lướt được; nhưng phần lớn nơi gọi hiện chỉ có đúng một câu
   * (vd `httpErrorInterceptor` chỉ nhận `message` từ envelope), nên không có tiêu đề vẫn phải
   * render đẹp — component chỉ hiện `.toast-title` khi field này có giá trị.
   */
  Title?: string;
  Text: string;
  /**
   * Nút hành động DUY NHẤT đi kèm toast — cũng KHÔNG bắt buộc, và cố ý không phải một mảng.
   *
   * Mở rộng tối thiểu cho đúng MỘT ca dùng: toast "không kết nối được" của
   * `core/interceptors/http-error.interceptor.ts` cần chỗ đặt nút "Thử lại". Một danh sách
   * action, action mang icon, action mang mức độ riêng… đều dựng được, nhưng hôm nay không có
   * nơi gọi thứ hai để nói hình dạng nào là đúng — và một API tổng quát chưa ai dùng là một API
   * sẽ sai lúc có người dùng thật. Thêm nơi gọi thứ hai rồi hẵng tổng quát hoá.
   */
  Action?: IToastAction;
}

/**
 * Nhãn đã DỊCH RỒI + việc phải làm khi bấm. `core/toast/` không tra bảng dịch (nó là hạ tầng
 * hiển thị, không sở hữu câu chữ) — nơi gọi truyền chuỗi đã dịch vào, đúng như `Text`/`Title`.
 */
export interface IToastAction {
  Label: string;
  Run: () => void;
}

/**
 * Toast tự biến mất sau ngần này — export vì nó KHÔNG chỉ là chi tiết nội bộ nữa: một toast mang
 * `Action` chỉ bấm được trong khoảng thời gian nó còn trên màn hình, nên nơi gọi phải biết cửa sổ
 * đó dài bao lâu để đóng việc lại cho đúng lúc (xem `http-error.interceptor.ts` §cửa sổ thử lại).
 * Hai hằng số ở hai file rồi lệch nhau là cách hỏng đã lường trước.
 */
export const TOAST_AUTO_DISMISS_MS = 5000;

/**
 * Notification/toast abstraction dùng chung toàn app — service hạ tầng UI singleton, sống ở
 * `core/` (KHÔNG phải `shared/`) đúng nguyên tắc tầng đáy — gate G9, xem
 * doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md. Component hiển thị
 * (`shared/components/toast/toast.ts`) import ngược từ đây — chiều `shared/` → `core/` được phép.
 *
 * `title` là THAM SỐ THỨ HAI dù khi hiển thị nó nằm TRÊN `text`: đặt nó thứ nhất sẽ đổi nghĩa
 * của mọi lời gọi `toast.error(message)` đang có mà trình biên dịch không hề báo — cùng kiểu
 * `string`, build vẫn xanh, chỉ sai lúc chạy. Tham số tuỳ chọn ở cuối là cách duy nhất thêm
 * tiêu đề mà không phải sửa đồng loạt nơi gọi.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly messages = signal<IToastMessage[]>([]);
  readonly toasts = this.messages.asReadonly();

  success(text: string, title?: string): void {
    this.push('success', text, title);
  }

  /**
   * `action` chỉ mở ở đây, KHÔNG mở cho `success`/`info`/`warn`: ca dùng duy nhất hôm nay là lỗi
   * mất kết nối. Mở cả bốn "cho đều" là dựng sẵn ba API không ai gọi.
   */
  error(text: string, title?: string, action?: IToastAction): void {
    this.push('error', text, title, action);
  }

  info(text: string, title?: string): void {
    this.push('info', text, title);
  }

  warn(text: string, title?: string): void {
    this.push('warn', text, title);
  }

  dismiss(id: number): void {
    this.messages.update((list) => list.filter((m) => m.Id !== id));
  }

  private push(severity: ToastSeverity, text: string, title?: string, action?: IToastAction): void {
    const id = this.nextId++;
    // `title` để nguyên `undefined` khi không truyền (không ép thành chuỗi rỗng) — template dùng
    // `@if (toast.Title)` nên hai giá trị cho cùng kết quả, nhưng giữ `undefined` thì so sánh
    // object trong test vẫn khớp với toast không tiêu đề.
    this.messages.update((list) => [
      ...list,
      { Id: id, Severity: severity, Title: title, Text: text, Action: action },
    ]);
    setTimeout(() => this.dismiss(id), TOAST_AUTO_DISMISS_MS);
  }
}
