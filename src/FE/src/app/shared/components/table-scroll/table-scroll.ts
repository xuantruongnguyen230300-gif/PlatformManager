import { Component, input } from '@angular/core';

/**
 * Wrapper cuộn (ngang luôn bật, dọc tuỳ chọn) cho bảng dữ liệu — dùng chung
 * giữa lưới "Danh mục & Đánh giá theo tuần" và bảng "62 chỉ tiêu DTI" của
 * Dashboard. Dumb component: chiếu `<table>` qua `ng-content`, không giữ
 * state/data.
 *
 * **Lưu ý kiến trúc**: sticky cột trái/phải (`position:sticky` trên
 * `th:first-child`/`td:last-child`...) phải khai báo trong SCSS của chính
 * component chứa `<table>` (vd `criteria-grid-table.scss`) — Angular view
 * encapsulation không cho phép style từ đây xuyên qua nội dung `ng-content`
 * chiếu vào, nên wrapper này chỉ chịu trách nhiệm cơ chế cuộn (overflow),
 * không định nghĩa CSS sticky cho `th`/`td`.
 *
 * `fillHeight`: khi `true`, wrapper tự `flex:1;min-height:0;overflow-y:auto` —
 * dùng cho card cha đã là `display:flex;flex-direction:column` với chiều cao
 * cố định tính động (xem `danh-muc-dti.page.ts`). Khi `false` (mặc định,
 * Dashboard), chỉ cuộn ngang — cuộn dọc xảy ra ở cấp trang như prototype gốc.
 */
@Component({
  selector: 'app-table-scroll',
  standalone: true,
  templateUrl: './table-scroll.html',
  styleUrl: './table-scroll.scss',
  host: {
    '[class.fill-height-host]': 'fillHeight()'
  }
})
export class TableScroll {
  readonly fillHeight = input(false);
}
