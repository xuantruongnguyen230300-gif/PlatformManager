import { isPlatformBrowser } from '@angular/common';
import { Component, ElementRef, PLATFORM_ID, effect, inject, input, output, viewChild } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { NotificationService } from '../../../../core/services/notification.service';

/**
 * Dialog "Xuất báo cáo" — dumb, hiển thị `title`/`contentHtml` do
 * `GET /api/dashboard/report` trả sẵn (server tự tính, mirror đúng
 * `generateReport()` gốc — xem `doc/contracts/dashboard.md` CONTRACT DB-2).
 * `contentHtml` chỉ chứa thẻ `<b>`/`<br>` từ server tin cậy nội bộ — sanitize
 * qua `DomSanitizer` trước khi bind `[innerHTML]`. "Sao chép"/"In" bọc
 * `isPlatformBrowser` theo `ui-conventions.md`.
 */
@Component({
  selector: 'app-report-dialog',
  standalone: true,
  templateUrl: './report-dialog.html',
  styleUrl: './report-dialog.scss'
})
export class ReportDialog {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly notification = inject(NotificationService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly open = input.required<boolean>();
  readonly title = input('Báo cáo tiến độ DTI');
  readonly contentHtml = input('');

  readonly closed = output<void>();

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('dialogRef');
  private readonly reportBox = viewChild<ElementRef<HTMLElement>>('reportBox');

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const dialog = this.dialogEl()?.nativeElement;
      if (!dialog) return;
      if (isOpen && !dialog.open) dialog.showModal();
      if (!isOpen && dialog.open) dialog.close();
    });
  }

  get safeContent(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(this.contentHtml());
  }

  onNativeClose(): void {
    this.closed.emit();
  }

  copyReport(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const text = this.reportBox()?.nativeElement.innerText ?? '';
    navigator.clipboard
      .writeText(text)
      .then(() => this.notification.success('Đã sao chép báo cáo.'))
      .catch(() => this.notification.error('Không thể sao chép — trình duyệt chặn quyền clipboard.'));
  }

  printReport(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    window.print();
  }
}
