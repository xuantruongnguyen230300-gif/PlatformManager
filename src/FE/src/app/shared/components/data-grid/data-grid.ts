import { ChangeDetectionStrategy, Component, TemplateRef, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';

/** Trang mới + số dòng mỗi trang, đã quy về 1-based cho tầng gọi. */
export interface IDataGridPageChange {
  page: number;
  pageSize: number;
}

/**
 * Hai mép được ghim khi lưới cuộn ngang.
 *
 * Hợp đồng: `doc/Design/Frontend/PlatformManager/Components/DataTable.md`
 * § Frozen edge columns, khối 🆕 CHỐT 2026-09-09.
 *
 * ## Vì sao là hai cờ, không phải danh sách cột
 *
 * Khối chốt để ngỏ KIỂU của input, kèm lý do: *"Designing a column API from a single example is
 * the mistake the 2026-09-06 block refused to make; fix the signature when the DTI grid is built,
 * against a real column list."* Danh sách cột thật đã có
 * (`doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` § Layout Blueprint) và nó ghim
 * ĐÚNG hai cột: cột `Mã` ở mép trái, cột `Hành động` ở mép phải. Lưới đó còn đổi giữa 13 và 14 cột
 * tuỳ chế độ kỳ, trong khi *"the two frozen edges are the same columns either way"* — nên một API
 * theo CHỈ SỐ cột sẽ buộc màn hình tự tính lại chỉ số mỗi lần số cột đổi, đúng loại phép tính lệch
 * một đơn vị mà `onLazyLoad` bên dưới tồn tại để dập.
 *
 * Hai cờ mô tả thẳng cái hợp đồng nói: mép trái = cột ĐẦU, mép phải = cột CUỐI. Cần ghim nhiều
 * hơn một cột mỗi mép thì đổi kiểu ở đây, khi có màn hình thật đòi nó.
 */
export interface IDataGridFrozenColumns {
  /** Ghim cột ĐẦU vào mép trái — cột định danh dòng. */
  readonly left?: boolean;
  /** Ghim cột CUỐI vào mép phải — cột hành động (`alignFrozen="right"` của PrimeNG). */
  readonly right?: boolean;
}

/**
 * Lưới bản ghi dùng chung — sở hữu KHUNG, không sở hữu CỘT.
 *
 * Ranh giới và lý do đầy đủ:
 * `doc/Design/Frontend/PlatformManager/Components/DataTable.md` § CHỐT 2026-09-06.
 *
 * ## Vì sao component này tồn tại
 *
 * Chiều cao "cố định bằng màn hình, cuộn bên trong" đòi **ba** thao tác rời nhau ở ba file:
 * trang phải mang class `page-fill`, lưới phải mang `grid-host`, và `p-table` phải dùng
 * `scrollHeight="flex"`. Mỗi thao tác nhìn riêng đều có vẻ đủ, nên thiếu một cái là hỏng im
 * lặng — đúng cách nó đã hỏng thật trước 2026-09-06. Gói cả ba vào đây thì không còn ba chỗ
 * để quên: màn hình chỉ cần đặt `page-fill` lên trang và `grid-host` lên thẻ này.
 *
 * ## Vì sao template truyền bằng `TemplateRef` chứ không `<ng-content>`
 *
 * `p-table` nhận `#header`/`#body`/`#emptymessage` bằng **content query**
 * (`predicate: ["header"]`). Chiếu chúng qua một lớp bọc bằng `<ng-content>` là dựa vào chi
 * tiết nội tại của Angular về ngữ cảnh khai báo — chạy được hôm nay, nhưng khi nâng phiên bản
 * thì hỏng mà không có lỗi biên dịch. Nhận `TemplateRef` rồi tự `ngTemplateOutlet` làm hợp
 * đồng hiện rõ trên chữ ký và không phụ thuộc thứ tự chiếu.
 *
 * ## Dumb — không inject service dữ liệu
 *
 * Nằm trong `shared/components/`, nên LUẬT G4 cấm inject `HttpClient`/service dữ liệu. Mọi thứ
 * đi vào bằng `input()`, đi ra bằng `output()`. Luật ở
 * doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md; cổng TỰ ĐỘNG cho G4 xếp lịch "Sau F4" và
 * chưa hiện thực hoá, nên không có gì bắt lỗi hộ khi ai đó lách.
 *
 * Component NÀY không nhận `localeId` — nó không định dạng ngày. Chỗ áp dụng đúng ranh giới đó
 * là `platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts`: ở đó
 * `localeId` là `input` chứ không `inject(LanguageService)`, và JSDoc tại chỗ kể lại lỗi NG0201
 * đã làm 37 test đỏ. (Sửa 2026-09-08: chú thích cũ gán input đó cho `data-grid`, sai chỗ.)
 */
@Component({
  selector: 'app-data-grid',
  standalone: true,
  imports: [NgTemplateOutlet, TableModule, TranslatePipe],
  templateUrl: './data-grid.html',
  styleUrl: './data-grid.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataGrid {
  /**
   * `unknown[]` chứ KHÔNG `readonly unknown[]`: `p-table` khai `[value]` là mảng khả biến, và
   * `readonly` không gán được vào đó (TS4104). Đây là ràng buộc của thư viện, không phải lựa
   * chọn — đừng "siết" lại thành readonly, nó sẽ đỏ ở template chứ không đỏ ở đây.
   */
  readonly rows = input.required<unknown[]>();
  readonly loading = input(false);
  readonly totalCount = input(0);

  /** Trang hiện tại, **1-based** — cùng quy ước với API, khác `first` 0-based của PrimeNG. */
  readonly page = input(1);
  readonly pageSize = input(10);
  readonly rowsPerPageOptions = input<number[]>([10, 20, 50]);

  /**
   * Khoá định danh dòng. PrimeNG dùng nó để giữ trạng thái theo dòng; để trống thì mọi dòng bị
   * coi là mới sau mỗi lần nạp.
   */
  readonly dataKey = input<string | undefined>(undefined);

  /**
   * Tổng `min-width` của các cột, truyền qua biến CSS. Để trống thì bảng co theo khung — chỉ
   * đúng khi số cột ít; nhiều cột mà không khai thì các cột bị bóp lại thay vì sinh cuộn ngang.
   */
  readonly minWidth = input<string | undefined>(undefined);

  /** `<tr>` của phần tiêu đề. */
  readonly headerTemplate = input.required<TemplateRef<unknown>>();
  /** `<tr>` của một dòng; nhận dòng dữ liệu qua `$implicit`. */
  readonly bodyTemplate = input.required<TemplateRef<{ $implicit: unknown }>>();
  /** Thay câu rỗng mặc định. Để trống thì dùng `shared.grid.empty`. */
  readonly emptyTemplate = input<TemplateRef<unknown> | undefined>(undefined);

  /**
   * Ghim cột mép trái/mép phải chống lại cuộn ngang. Để trống = không ghim gì (hành vi của mọi
   * lưới đang chạy hôm nay — lưới Người dùng có 5 cột vừa khung và **không được** bắt đầu ghim).
   *
   * Ghim đòi `[scrollable]="true"`; template dưới đây khai cứng `true` nên điều kiện đó luôn đủ.
   *
   * ## 🛑 Vì sao KHÔNG dùng directive `pFrozenColumn` — lệch có chủ đích so với hợp đồng
   *
   * `DataTable.md` § Frozen edge columns mô tả cơ chế là *"the component applies `pFrozenColumn`
   * … internally"*. Cơ chế đó KHÔNG hiện thực hoá được dưới hợp đồng `TemplateRef` mà chính file
   * đó chốt ngày 2026-09-06: `<th>`/`<td>` nằm trong template do MÀN HÌNH khai, nên Angular gắn
   * directive theo `imports` của màn hình chứ không theo `imports` của component này. Đưa
   * `pFrozenColumn` vào đây không chạm được tới ô nào, mà viết ở màn hình thì kéo `TableModule`
   * ra khỏi `data-grid` — đúng ranh giới mà khối chốt sinh ra để giữ.
   *
   * Phần hợp đồng có thể quan sát được thì giữ nguyên và đó mới là phần quan trọng: màn hình xin
   * ghim bằng `[frozenColumns]`, **không bao giờ** viết directive. Cơ chế bên trong là việc của
   * component, đúng như khối chốt tự nói: *"The screen says which columns are pinned and to which
   * edge; the component says how a pin is expressed."*
   *
   * Cách hiện thực thật: `data-grid.scss` đặt `position: sticky` cho ô đầu/ô cuối. Nó làm đúng
   * việc `FrozenColumn` làm (đặt `position: sticky` + `left`/`right`), nhưng không phải đo bề rộng
   * anh em bằng JavaScript sau mỗi lần vẽ vì mỗi mép chỉ có MỘT cột nên độ lệch luôn là 0.
   */
  readonly frozenColumns = input<IDataGridFrozenColumns | undefined>(undefined);

  readonly pageChange = output<IDataGridPageChange>();

  /**
   * PrimeNG phát `first` (0-based) + `rows`; tầng gọi và API đều làm việc với trang 1-based.
   * Quy đổi Ở ĐÂY một lần thay vì để mỗi màn hình tự nhớ — đây đúng loại phép tính lệch một đơn
   * vị mà chép đi chép lại sẽ sai ở đâu đó.
   */
  protected onLazyLoad(event: TableLazyLoadEvent): void {
    const size = event.rows ?? this.pageSize();
    const first = event.first ?? 0;
    this.pageChange.emit({ page: Math.floor(first / size) + 1, pageSize: size });
  }

  protected get firstIndex(): number {
    return (this.page() - 1) * this.pageSize();
  }
}
