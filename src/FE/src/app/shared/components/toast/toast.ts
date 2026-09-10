import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { IToastMessage, ToastSeverity, ToastService } from '../../../core/toast/toast.service';

/**
 * Bảng icon theo mức độ — khai ở TẦNG HIỂN THỊ, không phải trong `IToastMessage`: chọn hình
 * minh hoạ là việc của giao diện, nơi gọi `toast.error(...)` không cần (và không nên) biết
 * PrimeIcons có class nào. Giá trị theo thiết kế đã duyệt
 * (doc/Design/Frontend/PlatformManager/Prototypes/index.html §"15 · Thông báo nổi").
 */
const SEVERITY_ICON: Record<ToastSeverity, string> = {
  success: 'pi-check',
  error: 'pi-times',
  warn: 'pi-exclamation-triangle',
  info: 'pi-info-circle',
};

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Toast {
  private readonly toastService = inject(ToastService);
  protected readonly toasts = this.toastService.toasts;

  protected iconClass(severity: ToastSeverity): string {
    return SEVERITY_ICON[severity];
  }

  dismiss(id: number): void {
    this.toastService.dismiss(id);
  }

  /**
   * Chạy hành động rồi đóng toast — theo thứ tự đó. Đóng trước thì `message` vẫn còn tham chiếu
   * hợp lệ (mảng bị thay chứ object không bị huỷ), nhưng chạy trước giữ đúng nhân quả: nếu `Run`
   * ném lỗi thì toast còn đó để bấm lại, thay vì biến mất cùng với cơ hội thử lại.
   */
  protected runAction(message: IToastMessage): void {
    message.Action?.Run();
    this.toastService.dismiss(message.Id);
  }
}
