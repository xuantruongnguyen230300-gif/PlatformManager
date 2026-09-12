import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { DeltaIndicator } from '../../../../shared/components/delta-indicator/delta-indicator';
import { criteriaStatusBadge } from '../../../../shared/models/dti-criteria-status.model';
import { IDashboardTableRow } from '../../models/dashboard.model';

/**
 * Bảng chi tiết 9 cột của Dashboard — CHỈ ĐỌC, DUMB.
 *
 * ## 🛑 Dùng `Table`, KHÔNG dùng `DataTable` — chốt T4
 *
 * Bảng thuần trong `.tablewrap.scroll`, cuộn dọc trong vùng cao cố định, **không paginator**,
 * không `[lazy]`. DB-1 trả trọn bộ chỉ tiêu của kỳ trong một lần nên không có gì để phân trang
 * phía server, và cơ chế của `p-table` không đóng góp gì ở đây.
 *
 * Lý do chọn như vậy là để **không sinh ra biến thể thứ hai** của `DataTable`:
 * `Components/DataTable.md` khai đúng một biến thể (`[lazy]` + luôn có paginator), và một lưới
 * đọc-only không phải lý do đủ để nới hợp đồng đó. Màn Danh mục là nơi duy nhất dùng `DataTable`.
 *
 * ## Sắp xếp và lọc KHÔNG ở đây
 *
 * Cả hai do trang cha lo: lọc đi qua `<app-toolbar>` và qua tham số của DB-1 (`search`/`groupId`/
 * `status` — chỉ áp cho `table`), sắp xếp làm ở FE trên `Table[]` đã tải (Q22 — hai lựa chọn còn
 * lại đều xếp theo trường đã có: `code` và `diff`). Component này nhận mảng ĐÃ sắp và vẽ.
 */
@Component({
  selector: 'app-criteria-detail-table',
  standalone: true,
  imports: [DeltaIndicator, TranslatePipe],
  templateUrl: './criteria-detail-table.html',
  styleUrl: './criteria-detail-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CriteriaDetailTable {
  readonly rows = input.required<readonly IDashboardTableRow[]>();
  readonly localeId = input<string>('vi');

  protected badgeClass(row: IDashboardTableRow): string | null {
    return criteriaStatusBadge(row.Status);
  }

  /** Hai chữ số thập phân, dấu phẩy theo locale — cùng khuôn với lưới màn Danh mục. */
  protected num(value: number | null): string | null {
    if (value === null || !Number.isFinite(value)) return null;
    return new Intl.NumberFormat(this.localeId(), {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  }
}
