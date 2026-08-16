import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, PLATFORM_ID, effect, inject, input, output, viewChild } from '@angular/core';
import { ICsvImportResult } from '../../models/danh-muc-dti.model';

/** Hiện kết quả `POST /api/import/csv` — tổng dòng/thành công/lỗi + danh sách lỗi từng dòng. */
@Component({
  selector: 'app-import-result-dialog',
  standalone: true,
  templateUrl: './import-result-dialog.html',
  styleUrl: './import-result-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportResultDialog {
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input.required<boolean>();
  readonly result = input<ICsvImportResult | null>(null);
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
