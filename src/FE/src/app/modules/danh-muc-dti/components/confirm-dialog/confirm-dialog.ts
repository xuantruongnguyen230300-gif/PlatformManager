import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, PLATFORM_ID, effect, inject, input, output, viewChild } from '@angular/core';

/**
 * Dialog xác nhận (native `<dialog>`, nhỏ gọn) — dùng cho xoá chỉ tiêu. Chỉ 1 feature dùng ở lượt
 * này nên đặt tại `danh-muc-dti/components/` (không phải `shared/`, xem
 * src/FE/.claude/docs/architecture.md — component dùng chung phải tái dùng >1 feature).
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialog {
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input.required<boolean>();
  readonly title = input<string>('Xác nhận');
  readonly message = input<string>('');
  readonly confirmLabel = input<string>('Xác nhận');

  readonly confirmed = output<void>();
  readonly closed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      const el = this.dialogEl().nativeElement;
      if (this.open() && !el.open) el.showModal();
      if (!this.open() && el.open) el.close();
    });
  }

  onNativeClose(): void {
    this.closed.emit();
  }
}
