import { ChangeDetectionStrategy, Component, ElementRef, computed, input, model, output, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Một điều kiện lọc ĐANG ÁP, bày thành thẻ gỡ được ngay trên thanh công cụ.
 * `Key` là định danh trả về cho trang cha khi người dùng bấm × — trang cha tự biết `Key` nào
 * ứng với ô lọc nào, component này cố tình không hiểu ngữ nghĩa của bộ lọc.
 */
export interface IToolbarChip {
  Key: string;
  Label: string;
}

/**
 * Thanh công cụ đầu danh sách: ô tìm kiếm + nút Lọc (bảng điều kiện thả xuống) + thẻ điều kiện
 * đang áp + nhóm nút hành động. Markup và class khớp `.toolbar`/`.filter*` ở
 * `src/FE/src/styles.scss` §6 (bản đối ứng của khối "11 · Thanh công cụ" trong
 * doc/Design/Frontend/PlatformManager/Prototypes/index.html).
 *
 * HAI ĐIỂM DỄ LÀM SAI, ghi lại để lần sau không phải dò:
 *
 * 1. `.toolbar` đặt trên CHÍNH HOST (`host: { class }`), không bọc thêm `<div>`. Nếu bọc thêm thì
 *    thẻ `<app-toolbar>` (mặc định `display: inline` vì trình duyệt không biết thẻ này) sẽ chen
 *    vào giữa `.card` và `.toolbar`, cắt đứt chuỗi flex mà `.page-fill .toolbar { flex: none }`
 *    đang dựa vào — hậu quả là thanh công cụ co lại theo lưới trên màn hình thấp.
 * 2. Nội dung bộ lọc và nhóm nút đi qua `<ng-content>`, mà style scoped của Angular KHÔNG xuyên
 *    qua nội dung được project. Vì vậy mọi class dùng bên trong (`.form-row`, `.btn`, `.input`…)
 *    bắt buộc là class TOÀN CỤC ở `styles.scss` — file `.scss` của component này cố ý để trống.
 *
 * 5 trạng thái bắt buộc (05-component-library.md) đến từ chính các class dùng chung: `.btn` và
 * `.icon-btn` đã có đủ default/hover/focus-visible/active/disabled, ô tìm kiếm dùng hợp đồng
 * `.input-icon input` (có `:focus-visible` và `:disabled`). Đó là lý do component này không được
 * phép tự khai lại chúng — khai lại là mất luôn 4 trạng thái kia.
 */
@Component({
  selector: 'app-toolbar',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './toolbar.html',
  styleUrl: './toolbar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // `no-print`: thanh công cụ chỉ có nghĩa khi tương tác được, bản in không cần nó
    // (doc/huong_dan/quy-uoc/fe-ui-conventions.md §"In ấn").
    class: 'toolbar no-print',
    '(document:click)': 'onDocumentClick($event)',
    // Escape nghe ở tầng document chứ không gắn lên <details>: khi panel đang mở, focus có thể
    // đang nằm ở một ô bên trong panel, ở nút Lọc, hoặc chưa ở đâu cả (vừa bấm chuột ra chỗ
    // trống). Gắn lên <details> chỉ bắt được nhánh giữa — và còn buộc phải cho một thẻ bọc
    // không tương tác nhận tabindex, thứ mà quy tắc a11y của lint chặn đúng lý do.
    '(document:keydown.escape)': 'closeFilterPanel()',
  },
})
export class Toolbar {
  /** Ẩn hẳn ô tìm kiếm cho thanh công cụ chỉ có nút hành động. */
  readonly showSearch = input<boolean>(true);

  /*
   * BỐN NHÃN DƯỚI ĐÂY MẶC ĐỊNH LÀ CHUỖI RỖNG, KHÔNG phải câu tiếng Việt — và đó là quyết định,
   * không phải chỗ quên điền.
   *
   * Trang cha truyền vào một câu ĐÃ DỊCH (`[searchPlaceholder]="'…' | translate"`), nên input này
   * KHÔNG được giữ khoá dịch: hai kiểu giá trị đi chung một đường sẽ hoặc phải đoán, hoặc phải cho
   * chuỗi của trang cha đi qua `| translate` lần nữa — thứ chỉ chạy được nhờ ngx-translate trả lại
   * chính chuỗi khoá khi tra trượt, tức đúng bẫy mà app-i18n.spec.ts sinh ra để chặn.
   *
   * Rỗng ⇒ template lùi về khoá `shared.toolbar.*` bằng `| translate` NGAY TẠI CHỖ HIỂN THỊ, nên
   * câu mặc định đổi theo ngôn ngữ mà component này không phải inject gì (LUẬT G4 — `components/`
   * là tầng dumb; luật ở doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md, cổng tự động cho nó
   * thì chưa hiện thực hoá).
   */
  readonly searchPlaceholder = input<string>('');
  /**
   * Ô tìm kiếm không có `<label>` nhìn thấy được (chỗ trên thanh công cụ quá hẹp), nên nhãn cho
   * trình đọc màn hình phải đi qua `aria-label` — mặc định chung chung, trang cha nên đặt lại
   * theo đúng thứ đang tìm ("Tìm chỉ tiêu", "Tìm người dùng").
   */
  readonly searchAriaLabel = input<string>('');
  /** Khoá ô tìm kiếm (vd đang tải trang đầu tiên) — chặn gõ thật, không chỉ đổi màu. */
  readonly searchDisabled = input<boolean>(false);

  /**
   * Two-way: trang cha `[(searchValue)]`. Component KHÔNG tự debounce — mỗi trang có ngưỡng chờ
   * khác nhau (gọi API ngay vs lọc tại chỗ), gói cứng ở đây là ép mọi trang theo một ngưỡng.
   */
  readonly searchValue = model<string>('');

  /**
   * Có bộ lọc hay không. Phải khai tường minh vì Angular không cho hỏi "có ai project vào
   * `[filter]` không" — không có cờ này thì thanh công cụ nào cũng mọc một nút Lọc rỗng.
   */
  readonly hasFilter = input<boolean>(false);
  /** Số điều kiện đang bật — hiện trên nút Lọc để biết danh sách đang bị lọc kể cả khi panel đóng. */
  readonly filterCount = input<number>(0);
  readonly filterClearLabel = input<string>('');
  readonly filterApplyLabel = input<string>('');

  readonly chips = input<readonly IToolbarChip[]>([]);

  readonly chipRemove = output<string>();
  readonly filterApply = output<void>();
  readonly filterClear = output<void>();

  private readonly filterEl = viewChild<ElementRef<HTMLDetailsElement>>('filterEl');

  /** Vạch ngăn chỉ có nghĩa khi có thứ gì đó ở BÊN TRÁI nó để ngăn cách. */
  protected readonly showSeparator = computed(() => this.showSearch() || this.hasFilter());

  protected onSearchInput(event: Event): void {
    this.searchValue.set((event.target as HTMLInputElement).value);
  }

  protected onChipRemove(key: string): void {
    this.chipRemove.emit(key);
  }

  /**
   * Đóng panel trước rồi mới phát sự kiện: trang cha thường phản ứng bằng cách gọi lại API và
   * render lại, để panel mở lơ lửng trên dữ liệu vừa đổi là trạng thái không ai muốn thấy.
   */
  protected onFilterApply(): void {
    this.closeFilterPanel();
    this.filterApply.emit();
  }

  protected onFilterClear(): void {
    this.closeFilterPanel();
    this.filterClear.emit();
  }

  /** `<details>` gốc không tự đóng khi bấm ra ngoài hay bấm Escape — đây là phần phải bù bằng tay. */
  closeFilterPanel(): void {
    const details = this.filterEl()?.nativeElement;
    if (details) details.open = false;
  }

  protected onDocumentClick(event: Event): void {
    const details = this.filterEl()?.nativeElement;
    if (!details?.open) return;
    // Bấm vào chính nút Lọc hay vào bên trong panel thì KHÔNG đóng — nếu không, cú bấm mở panel
    // cũng sẽ bị chính handler này đóng lại ngay (sự kiện nổi lên tới document sau khi
    // <details> đã tự mở).
    if (event.target instanceof Node && details.contains(event.target)) return;
    details.open = false;
  }
}
