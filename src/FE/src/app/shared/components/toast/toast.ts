import { Component, inject } from '@angular/core';

import { NotificationService } from '../../../core/services/notification.service';

/**
 * Toast list dùng chung toàn app — render `NotificationService.items()`
 * (lỗi HTTP từ interceptor + thông báo thành công từ các page). Dumb ở chỗ chỉ
 * đọc signal có sẵn + phát hành động dismiss, không tự gọi API nào.
 */
@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.html',
  styleUrl: './toast.scss'
})
export class Toast {
  private readonly notification = inject(NotificationService);

  readonly items = this.notification.items;

  dismiss(id: number): void {
    this.notification.dismiss(id);
  }
}
