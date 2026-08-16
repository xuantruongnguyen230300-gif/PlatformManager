import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

export type ReportCopyResult = 'success' | 'error';

/**
 * Dialog (native `<dialog>`) — báo cáo nhanh từ BE (`ContentHtml` đã render sẵn, xem
 * doc/contracts/dashboard.md CONTRACT DB-2). Sanitize qua `DomSanitizer.bypassSecurityTrustHtml`
 * NGAY TẠI ĐÂY (BE tự tính HTML, FE chỉ hiển thị, tin tưởng nguồn — không có input người dùng
 * lẫn vào chuỗi này) theo đúng yêu cầu gốc.
 *
 * Dumb component (`modules/dashboard/components/`) — KHÔNG inject `ToastService` (ngoại lệ
 * "app-shell" chỉ áp dụng sidebar/topbar/toast, xem
 * doc/huong_dan/wiki-core/fe/05-component-library.md §Vị trí trong cây thư mục). Kết quả copy
 * phát qua `copyResult` để `DashboardPage` (smart) tự quyết định cách báo cho user.
 */
@Component({
  selector: 'app-report-dialog',
  standalone: true,
  templateUrl: './report-dialog.html',
  styleUrl: './report-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportDialog {
  private readonly sanitizer = inject(DomSanitizer);
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input.required<boolean>();
  readonly title = input<string>('Báo cáo tiến độ DTI');
  readonly contentHtml = input<string>('');
  readonly closed = output<void>();
  readonly copyResult = output<ReportCopyResult>();

  protected readonly safeContent = computed<SafeHtml>(() => this.sanitizer.bypassSecurityTrustHtml(this.contentHtml()));

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');
  private readonly reportBoxEl = viewChild.required<ElementRef<HTMLDivElement>>('reportBoxEl');

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

  onCopy(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const text = this.reportBoxEl().nativeElement.innerText;
    navigator.clipboard
      .writeText(text)
      .then(() => this.copyResult.emit('success'))
      .catch(() => this.copyResult.emit('error'));
  }

  onPrint(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    window.print();
  }
}
