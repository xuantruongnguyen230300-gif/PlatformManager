import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { LanguageService } from '../../../../core/i18n/language.service';
import { Toolbar } from '../../../../shared/components/toolbar/toolbar';
import { DELTA_EPSILON } from '../../../../shared/components/delta-indicator/delta-indicator';
import { IDashboardTableRow } from '../../models/dashboard.model';
import { KpiTile, KpiTone } from '../../components/kpi-tile/kpi-tile';
import { ProgressBar } from '../../components/progress-bar/progress-bar';
import { TrendChart, ITrendPoint } from '../../components/trend-chart/trend-chart';
import { HistoryRow } from '../../components/history-row/history-row';
import { CriteriaDetailTable } from '../../components/criteria-detail-table/criteria-detail-table';
import { DashboardExportFlow } from './dashboard-export-flow';
import { DashboardFeed } from './dashboard-feed';
import { DashboardFilters } from './dashboard-filters';
import { DashboardLabels } from './dashboard-labels';


/**
 * So mã chỉ tiêu theo THỨ TỰ TỰ NHIÊN: `4.2 < 4.10 < 4.22.11`.
 *
 * So chuỗi thuần cho ra `4.10 < 4.2`, và mã DTI có tới ba cấp nên lỗi đó xuất hiện ngay ở nhóm 4.
 */
function compareCode(left: string, right: string): number {
  const a = left.split('.');
  const b = right.split('.');
  for (let i = 0; i < Math.max(a.length, b.length); i++) {
    const x = Number(a[i] ?? 0);
    const y = Number(b[i] ?? 0);
    if (x !== y) return x - y;
  }
  return 0;
}

/**
 * SMART — màn Dashboard DTI.
 *
 * ## VÒNG 2 (2026-09-11) — nút `Xuất báo cáo` đã nối (DB-4, Q68)
 *
 * Tải **thẳng** file, không dialog, không xem trước (Q13), và **tôn trọng bộ lọc đang áp** (Q23)
 * vì nó gửi đúng bộ tham số của DB-1.
 *
 * 🛑 `mode=year` **không xuất được** (`400 DASHBOARD.EXPORT_MODE_UNSUPPORTED`): bố cục file đã
 * duyệt có khối nhận dạng kỳ với `Từ ngày`/`Đến ngày` của **một** kỳ, và "cả năm" không ánh xạ
 * được vào khuôn đó. Nút vì vậy `disabled` kèm `title` nói lối ra **TRƯỚC** khi người dùng bấm —
 * đây là ca hay gặp nhất (bấm Xuất khi đang xem `Tất cả`), nên để họ bấm rồi mới báo lỗi là đẩy
 * một quyết định đã biết trước xuống thành một thất bại.
 *
 * ## Ca Q32 — nhận dạng bằng `kpi`, KHÔNG bằng `table` (chốt Q69, 2026-09-10)
 *
 * ```
 * ca Q32  ⟺  kpi.totalCriteria > 0  VÀ  kpi.overallProgress vắng mặt
 * ```
 *
 * 🛑 Proxy cũ đọc `table.length > 0`, và nó **sai theo đúng luật 1 của DB-1**: `search`/`groupId`/
 * `status` chỉ áp cho `table`, còn `kpi` luôn tính trên toàn bộ chỉ tiêu của kỳ. Lọc một nhóm
 * không có dòng nào ⇒ `table` rỗng trong khi kỳ vẫn đủ chỉ tiêu ⇒ dải băng biến mất **đúng lúc nó
 * cần hiện**. Nghiệm thu của Q69 chính là ca đó.
 *
 * ## Đang tải — làm mờ vùng số liệu + MỘT vòng quay (Q34 + T10)
 *
 * V2/V3/V4/V5 mờ đi (`opacity: .5` + `aria-busy`), V1 và V8 **không** mờ: V1 là control vừa khởi
 * động lượt tải nên không được biến mất khỏi tay người dùng, còn V8 ăn theo nguồn thứ hai.
 * **Một** vòng quay cho cả cụm, không phải mỗi vùng một cái.
 *
 * *(Lệch có chủ đích so với `spec/dashboard-dti/ui-spec.md` §5.2, đã ghi nhận: bảng ở đó xếp V5
 * vào "mặt nạ loading sẵn có của `p-table`", nhưng T4 chốt V5 là bảng THUẦN — không có `p-table`
 * nào để mượn mặt nạ. Nên V5 đi cùng ba vùng kia.)*
 */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    RouterLink,
    Toolbar,
    KpiTile,
    ProgressBar,
    TrendChart,
    HistoryRow,
    CriteriaDetailTable,
    ProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
  providers: [DashboardFilters, DashboardFeed, DashboardLabels, DashboardExportFlow],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);

  protected readonly filters = inject(DashboardFilters);
  protected readonly feed = inject(DashboardFeed);
  protected readonly labels = inject(DashboardLabels);
  protected readonly exportFlow = inject(DashboardExportFlow);
  protected readonly localeId = this.language.localeId;

  /** Năm DƯƠNG LỊCH hiện tại — mặc định của ô `Năm`. Xem ghi chú cùng tên ở màn Danh mục. */
  // ===== Số liệu =====

  protected readonly aggregate = this.feed.data;
  protected readonly kpi = computed(() => this.aggregate()?.Kpi ?? null);

  /** `—` ở đây là *"không tính được"*; `0` là *"tính được, kết quả bằng không"*. Đừng gộp. */
  protected readonly overallProgressText = computed<string | null>(() => this.labels.percent(this.kpi()?.OverallProgress));

  protected readonly deltaText = computed<string | null>(() => {
    const delta = this.kpi()?.Delta;
    if (delta === null || delta === undefined || !Number.isFinite(delta)) return null;
    this.language.current();
    const formatted = new Intl.NumberFormat(this.localeId(), {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
      signDisplay: 'exceptZero',
    }).format(delta);
    // Dùng lại ĐÚNG ba khoá của `DeltaIndicator` — mũi tên và đơn vị `đ.%` đã nằm trong câu dịch,
    // nên không ghép chuỗi ở đây và không đẻ ra một bộ chuỗi thứ hai cho cùng một đại lượng.
    const key =
      delta > DELTA_EPSILON
        ? 'shared.deltaIndicator.pointUp'
        : delta < -DELTA_EPSILON
          ? 'shared.deltaIndicator.pointDown'
          : 'shared.deltaIndicator.pointFlat';
    return this.translate.instant(key, { Value: formatted }) as string;
  });

  protected readonly deltaTone = computed<KpiTone>(() => {
    const delta = this.kpi()?.Delta;
    if (delta === null || delta === undefined || !Number.isFinite(delta)) return 'default';
    if (delta > DELTA_EPSILON) return 'good';
    if (delta < -DELTA_EPSILON) return 'bad';
    return 'default';
  });

  /**
   * Ba ô ĐẾM. `0` ở đây là kết quả THẬT — ngay sau import cả `up` lẫn `flat` đều `0` vì chỉ tiêu
   * thiếu dữ liệu ở một trong hai kỳ thì không vào phép đếm nào (business-rules §1.4).
   *
   * 🛑 Đừng "vá" cho ô 4 hiện `62` theo trực giác *"chưa ai nhập thì cả 62 chỉ tiêu đều không
   * tăng"*: `flat` nghĩa là *"đã đo hai lần và không đổi"*, không phải *"chưa đo lần nào"*.
   */
  protected readonly upText = computed<string | null>(() => this.labels.count(this.kpi()?.Up));
  protected readonly flatText = computed<string | null>(() => this.labels.count(this.kpi()?.Flat));

  protected readonly doneText = computed<string | null>(() => {
    const kpi = this.kpi();
    if (!kpi) return null;
    return this.translate.instant('dashboard.kpi.doneValue', {
      Done: `${kpi.Done}`,
      Total: `${kpi.TotalCriteria}`,
    }) as string;
  });

  /**
   * Q52 — nhãn ô 1 và ô 2 đổi theo CHẾ ĐỘ kỳ. Ô 3…5 giữ nhãn ở mọi chế độ.
   *
   * Trước Q52 hai ô này ghim chữ *"tuần"*, nên ở chế độ Tháng màn hình gọi số của một tháng là
   * "tuần này".
   */
  protected readonly kpiLabelSuffix = computed(() => this.filters.mode());

  protected readonly trendPoints = computed<readonly ITrendPoint[]>(() =>
    (this.aggregate()?.Trend ?? []).map((point) => ({ Label: point.PeriodLabel, Value: point.Value })),
  );

  /** Câu tóm tắt cho trình đọc màn hình — dựng từ CHÍNH dữ liệu đã truyền vào biểu đồ. */
  protected readonly trendSummary = computed<string>(() => {
    const points = this.trendPoints().filter((point) => point.Value !== null);
    if (points.length === 0) return '';
    this.language.current();
    return this.translate.instant('dashboard.chart.summary', {
      From: points[0].Label,
      To: points[points.length - 1].Label,
    }) as string;
  });

  /** Bảng chi tiết ĐÃ sắp — Q22 để lại đúng hai lựa chọn, cả hai xếp trên trường đã có. */
  protected readonly sortedTable = computed<readonly IDashboardTableRow[]>(() => {
    const rows = [...(this.aggregate()?.Table ?? [])];
    if (this.filters.sortBy() === 'diff') {
      // "Lớn nhất" nói về ĐỘ LỆCH, không nói về dấu: xếp giảm dần theo giá trị CÓ DẤU sẽ đẩy đúng
      // những dòng bị thẩm định trừ điểm — ca đáng chú ý nhất sau Q25 — xuống cuối danh sách.
      return rows.sort((a, b) => Math.abs(b.Diff ?? 0) - Math.abs(a.Diff ?? 0));
    }
    return rows.sort((a, b) => compareCode(a.Code, b.Code));
  });

  // ===== `Xuất báo cáo` (DB-4) =====

  /**
   * 🛑 `mode=year` KHÔNG xuất được — nút tắt, và tắt **trước** khi bấm.
   *
   * Cũng tắt khi đang có một lượt tải bay (bấm hai lần = tính lại báo cáo hai lần ở server) và khi
   * chưa có số liệu nào (`kpi === null`: đang tải lượt đầu, hoặc lượt tải hỏng) — xuất một kỳ mà
   * màn hình còn chưa đọc được là xuất một thứ người dùng chưa nhìn thấy.
   */
  protected readonly exportEnabled = computed(
    () => this.filters.mode() !== 'year' && !this.exportFlow.busy() && this.kpi() !== null,
  );

  /** Số dòng bảng chi tiết đang hiện / tổng số chỉ tiêu của kỳ — nuôi `title` khi ĐANG lọc. */
  private readonly exportCounts = computed(() => ({
    Shown: `${this.sortedTable().length}`,
    Total: `${this.kpi()?.TotalCriteria ?? 0}`,
  }));

  /**
   * `title` của nút — ba biến thể, và biến thể thứ ba là lý do mục này tồn tại.
   *
   * | Trạng thái | Câu nói gì |
   * | --- | --- |
   * | `mode=year` | **vì sao không bấm được, và làm gì để bấm được** |
   * | đang lọc | file chỉ chứa các chỉ tiêu đang lọc, kèm số đếm — Q23 |
   * | không lọc | file của kỳ đang xem, gọi tên kỳ đó |
   *
   * Biến thể "đang lọc" là ràng buộc của thiết kế, không phải trang trí: nút nằm ở thanh **kỳ**
   * trong khi nó xuất theo bộ lọc của **bảng** bên dưới — hai bề mặt lọc khác phạm vi, và không có
   * gì khác trên màn nói ra điều đó.
   */
  protected readonly exportTitle = computed<string>(() => {
    this.language.current();
    if (this.filters.mode() === 'year') {
      return this.translate.instant('dashboard.action.exportYearUnsupported') as string;
    }
    if (this.filters.activeFilterCount() > 0 || this.filters.searchText()) {
      return this.translate.instant('dashboard.action.exportFilteredTitle', this.exportCounts()) as string;
    }
    return this.translate.instant('dashboard.action.exportTitle', { Period: this.labels.exportPeriodLabel() }) as string;
  });

  // ===== Ba trạng thái rỗng =====

  protected readonly hasError = computed(() => this.feed.loadError() !== null);

  /** `totalCriteria = 0` ⇒ chưa có kỳ nào / chưa có dữ liệu trong năm đang chọn (§5.3 hàng 1). */
  protected readonly showFirstRunNotice = computed(
    () => !this.hasError() && this.kpi() !== null && this.kpi()!.TotalCriteria === 0,
  );

  /** Ca Q32 — hai vế đều đọc từ `kpi`, xem JSDoc của class. */
  protected readonly showAwaitingProgressNotice = computed(() => {
    const kpi = this.kpi();
    if (this.hasError() || kpi === null) return false;
    return kpi.TotalCriteria > 0 && kpi.OverallProgress === null;
  });

  /** §5.3 hàng 1: chưa có kỳ nào trong năm ⇒ V2…V5 và V8 ẩn, chỉ còn dải băng và chân trang. */
  protected readonly showDataRegions = computed(() => !this.hasError() && !this.showFirstRunNotice());

  // ===== V8 — lịch sử các kỳ đã lưu =====

  /**
   * Hàng lịch sử, **mới nhất trước**, kèm delta so với kỳ liền TRƯỚC nó theo thời gian.
   *
   * Không có endpoint riêng (DB-3 ghi rõ): tái dùng danh sách kỳ và tự tính chênh lệch ở FE. Kỳ cũ
   * nhất mang `IsFirst` để hàng đó in `Kỳ đầu` thay vì một delta bịa bằng 0.
   *
   * Kỳ nào **chưa có** `OverallProgress` thì delta là `null` — không có gì để trừ. Đó là ca thường
   * gặp ngay sau import, không phải ca biên.
   */
  protected readonly historyRows = computed(() => {
    const options = this.filters.periodOptions();
    if (!options) return [];
    const source = this.filters.mode() === 'month' ? options.MonthsInYear : options.WeeksInYear;
    const ordered = [...source].sort((a, b) => a.StartDate.getTime() - b.StartDate.getTime());

    return ordered
      .map((option, index) => {
        const previous = index > 0 ? ordered[index - 1].OverallProgress : null;
        const current = option.OverallProgress;
        return {
          Option: option,
          Delta: previous === null || current === null ? null : current - previous,
          IsFirst: index === 0,
        };
      })
      .reverse();
  });

  /**
   * Gửi **đúng** bộ tham số của lượt tải hiện tại — export tôn trọng bộ lọc đang áp (Q23), nên nó
   * không được dựng một bộ tham số thứ hai. Một bộ thứ hai là một chỗ để file tải về không khớp
   * bảng người dùng đang nhìn.
   */
  protected onExport(): void {
    this.exportFlow.run(this.filters.requestParams());
  }

  protected reload(): void {
    this.feed.load(this.filters.requestParams());
  }


  /**
   * Một `effect` duy nhất nối bộ lọc với lượt tải.
   *
   * 🛑 Q62: URL bẩn ⇒ viết lại URL và **KHÔNG** gọi API ở nhịp này. Lần điều hướng đó làm
   * `queryParamMap` phát lại, effect chạy lần hai, và lúc đó `isClean()` đã đúng — nên phép làm
   * sạch xảy ra **trước** lần gọi DB-1 đầu tiên.
   */
  constructor() {
    effect(() => {
      if (!this.filters.isClean()) {
        this.filters.rewriteCleanUrl();
        return;
      }
      this.feed.load(this.filters.requestParams());
    });
  }
}
