import { Injectable, signal } from '@angular/core';

export type NotificationKind = 'success' | 'error' | 'info';

export interface INotification {
  Id: number;
  Kind: NotificationKind;
  Message: string;
}

let nextId = 1;

/**
 * Toast/notification singleton toàn app (core/) — dùng bởi
 * `httpErrorInterceptor` để hiện lỗi HTTP, và bởi các page/service khác khi
 * cần báo thành công (vd "Đã lưu chỉ tiêu"). Render tại `shared/components/toast`.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _items = signal<INotification[]>([]);
  readonly items = this._items.asReadonly();

  success(message: string): void {
    this.push('success', message);
  }

  error(message: string): void {
    this.push('error', message);
  }

  info(message: string): void {
    this.push('info', message);
  }

  dismiss(id: number): void {
    this._items.update((list) => list.filter((n) => n.Id !== id));
  }

  private push(kind: NotificationKind, message: string): void {
    const item: INotification = { Id: nextId++, Kind: kind, Message: message };
    this._items.update((list) => [...list, item]);
    setTimeout(() => this.dismiss(item.Id), 5000);
  }
}
