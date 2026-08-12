import { Component, ElementRef, computed, effect, input, output, viewChild } from '@angular/core';

/**
 * Ô sửa inline dùng chung cho cột "Tiến độ %" và "Ghi chú" của lưới Danh mục
 * DTI — dumb component tái dùng, KHÔNG tự ghi vào đâu cả (theo
 * `spec/danh-muc-dti/ui-spec.md` mục 3: bấm ✓ chỉ phát `confirm`, page cha mới
 * gọi service; bấm ✗ chỉ phát `cancel`, KHÔNG đọc giá trị input — khôi phục
 * đơn giản bằng cách page render lại đúng `value` cũ).
 */
@Component({
  selector: 'app-editable-cell',
  standalone: true,
  templateUrl: './editable-cell.html',
  styleUrl: './editable-cell.scss'
})
export class EditableCell {
  readonly value = input<string | number | null>(null);
  readonly editing = input(false);
  readonly editable = input(false);
  readonly type = input<'number' | 'text'>('text');
  readonly suffix = input('');
  readonly emptyText = input('—');

  readonly editStart = output<void>();
  readonly confirm = output<string>();
  readonly cancel = output<void>();

  readonly displayValue = computed(() => {
    const v = this.value();
    if (v === null || v === undefined || v === '') return this.emptyText();
    return this.type() === 'number' ? `${v}${this.suffix()}` : String(v);
  });

  private readonly inputEl = viewChild<ElementRef<HTMLInputElement>>('cellInput');

  constructor() {
    // Mỗi khi chuyển sang trạng thái Sửa, focus + select toàn bộ text — đúng
    // hành vi gốc của prototype. `setTimeout(0)` (thay vì gọi thẳng) vì input
    // vừa được `@if` thêm vào DOM, cần đợi 1 vòng render trước khi query được
    // `viewChild`.
    effect(() => {
      if (this.editing()) {
        setTimeout(() => {
          const el = this.inputEl()?.nativeElement;
          el?.focus();
          el?.select();
        });
      }
    });
  }

  onStart(): void {
    if (this.editable()) this.editStart.emit();
  }

  onKeydown(event: KeyboardEvent, currentValue: string): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.confirm.emit(currentValue);
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.cancel.emit();
    }
  }
}
