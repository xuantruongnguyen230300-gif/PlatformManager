import { Directive, ElementRef, afterNextRender, inject } from '@angular/core';

/**
 * Auto-focus (+ select toàn bộ nếu là input) ngay khi phần tử được render — dùng cho ô inline-edit
 * (`criteria-grid-table`) và field đầu tiên của dialog form. `afterNextRender` tự động chỉ chạy ở
 * browser (không chạy khi SSR render trên server) — không cần tự bọc `isPlatformBrowser`.
 */
@Directive({
  selector: '[appAutofocus]',
  standalone: true,
})
export class AutofocusDirective {
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  constructor() {
    afterNextRender(() => {
      const el = this.elementRef.nativeElement;
      el.focus();
      if (el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement) {
        el.select();
      }
    });
  }
}
