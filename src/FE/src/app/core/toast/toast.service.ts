import { Injectable, signal } from '@angular/core';

export type ToastSeverity = 'success' | 'error' | 'info' | 'warn';

export interface IToastMessage {
  Id: number;
  Severity: ToastSeverity;
  Text: string;
}

const AUTO_DISMISS_MS = 5000;

/**
 * Notification/toast abstraction dùng chung toàn app — service hạ tầng UI singleton, sống ở
 * `core/` (KHÔNG phải `shared/`) đúng nguyên tắc tầng đáy — gate G9, xem
 * doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md. Component hiển thị
 * (`shared/components/toast/toast.ts`) import ngược từ đây — chiều `shared/` → `core/` được phép.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly messages = signal<IToastMessage[]>([]);
  readonly toasts = this.messages.asReadonly();

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  info(text: string): void {
    this.push('info', text);
  }

  warn(text: string): void {
    this.push('warn', text);
  }

  dismiss(id: number): void {
    this.messages.update((list) => list.filter((m) => m.Id !== id));
  }

  private push(severity: ToastSeverity, text: string): void {
    const id = this.nextId++;
    this.messages.update((list) => [...list, { Id: id, Severity: severity, Text: text }]);
    setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
  }
}
