import { Component, ElementRef, effect, input, output, signal, viewChild } from '@angular/core';

/**
 * Dialog chọn file CSV để Import — dumb, chỉ giữ file đã chọn cục bộ rồi phát
 * `imported` (File) khi bấm "Import"; page cha gọi
 * `DanhMucDtiService.importCsv()` và mở `ImportResultDialog` với kết quả.
 */
@Component({
  selector: 'app-csv-import-dialog',
  standalone: true,
  templateUrl: './csv-import-dialog.html',
  styleUrl: './csv-import-dialog.scss'
})
export class CsvImportDialog {
  readonly open = input.required<boolean>();
  readonly loading = input(false);

  readonly imported = output<File>();
  readonly closed = output<void>();

  readonly selectedFile = signal<File | null>(null);

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('dialogRef');

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const dialog = this.dialogEl()?.nativeElement;
      if (!dialog) return;
      if (isOpen) {
        this.selectedFile.set(null);
        if (!dialog.open) dialog.showModal();
      } else if (dialog.open) {
        dialog.close();
      }
    });
  }

  onNativeClose(): void {
    this.closed.emit();
  }

  onFileSelected(file: File | undefined): void {
    this.selectedFile.set(file ?? null);
  }

  onSubmit(): void {
    const file = this.selectedFile();
    if (file) this.imported.emit(file);
  }
}
