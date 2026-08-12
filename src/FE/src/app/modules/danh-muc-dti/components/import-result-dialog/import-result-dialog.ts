import { Component, ElementRef, computed, effect, input, output, viewChild } from '@angular/core';

import { IImportResult } from '../../models/danh-muc-dti.model';

/**
 * Dialog tóm tắt kết quả Import CSV — dumb, chỉ hiển thị `result` do page cha
 * truyền vào sau khi gọi `DanhMucDtiService.importCsv()` thành công.
 */
@Component({
  selector: 'app-import-result-dialog',
  standalone: true,
  templateUrl: './import-result-dialog.html',
  styleUrl: './import-result-dialog.scss'
})
export class ImportResultDialog {
  readonly open = input.required<boolean>();
  readonly result = input<IImportResult | null>(null);

  readonly closed = output<void>();

  readonly errorRows = computed(() => this.result()?.Errors ?? []);

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
