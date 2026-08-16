import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, PLATFORM_ID, effect, inject, input, output, signal, viewChild } from '@angular/core';

/** Dialog chọn file CSV trước khi gọi `POST /api/import/csv` — xem CONTRACT DM-7. */
@Component({
  selector: 'app-csv-import-dialog',
  standalone: true,
  templateUrl: './csv-import-dialog.html',
  styleUrl: './csv-import-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CsvImportDialog {
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input.required<boolean>();
  readonly importing = input<boolean>(false);

  readonly imported = output<File>();
  readonly closed = output<void>();

  protected readonly selectedFile = signal<File | null>(null);

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      const el = this.dialogEl().nativeElement;
      if (this.open() && !el.open) {
        this.selectedFile.set(null);
        el.showModal();
      }
      if (!this.open() && el.open) el.close();
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  onNativeClose(): void {
    this.closed.emit();
  }

  onSubmit(): void {
    const file = this.selectedFile();
    if (file) this.imported.emit(file);
  }
}
