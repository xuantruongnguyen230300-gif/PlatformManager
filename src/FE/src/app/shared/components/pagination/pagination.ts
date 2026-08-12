import { Component, computed, input, output } from '@angular/core';

/**
 * Phân trang dùng chung (Danh mục DTI + bảng 62 chỉ tiêu Dashboard) — dumb
 * component: chỉ nhận `page`/`pageSize`/`total` qua `input()`, phát
 * `pageChange`/`pageSizeChange` qua `output()`. Không tự cắt mảng dữ liệu —
 * page (smart) chịu trách nhiệm `computed()` slice theo page/pageSize hiện tại.
 *
 * Mỗi `<option>` của select "Dòng/trang" trong template có thêm
 * `[selected]="opt === pageSize()"` bên cạnh `[value]` trên `<select>` — với
 * `<option>` sinh động bằng `@for`, chỉ `[value]` trên `<select>` không đủ
 * (trình duyệt có thể fallback chọn option đầu tiên do thứ tự tạo DOM), xem
 * giải thích đầy đủ ở `period-toolbar.ts`.
 */
@Component({
  selector: 'app-pagination',
  standalone: true,
  templateUrl: './pagination.html',
  styleUrl: './pagination.scss'
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly total = input.required<number>();
  readonly pageSizeOptions = input<number[]>([10, 20, 50]);

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

  readonly rangeStart = computed(() => (this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.total()));

  onPageSizeChange(value: string): void {
    this.pageSizeChange.emit(Number(value));
  }

  goPrev(): void {
    if (this.page() > 1) this.pageChange.emit(this.page() - 1);
  }

  goNext(): void {
    if (this.page() < this.totalPages()) this.pageChange.emit(this.page() + 1);
  }
}
