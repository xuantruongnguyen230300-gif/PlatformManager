import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

/**
 * Dialog xác nhận chung (dùng cho Xoá chỉ tiêu) — dumb, không gọi API. Page
 * cha quyết định hành động thật khi nhận `confirmed`.
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss'
})
export class ConfirmDialog {
  readonly open = input.required<boolean>();
  readonly message = input('');
  readonly confirmLabel = input('Xác nhận');

  readonly confirmed = output<void>();
  readonly closed = output<void>();

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('dialogRef');

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const dialog = this.dialogEl()?.nativeElement;
      if (!dialog) return;
      if (isOpen && !dialog.open) dialog.showModal();
      if (!isOpen && dialog.open) dialog.close();
    });
  }

  onNativeClose(): void {
    this.closed.emit();
  }
}
