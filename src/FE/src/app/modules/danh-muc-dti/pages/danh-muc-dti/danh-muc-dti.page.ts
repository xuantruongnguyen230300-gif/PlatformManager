import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { DanhMucDtiService } from '../../services/danh-muc-dti.service';
import { PeriodOptionsService } from '../../../../shared/services/period-options.service';
import { IPeriodOptions } from '../../../../shared/models/period-options.model';
import { ToastService } from '../../../../core/toast/toast.service';
import { IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import {
  ICriteria,
  ICriteriaGroup,
  ICriteriaListParams,
  ICriteriaRow,
  ICriteriaUpsertPayload,
  ICsvImportResult,
} from '../../models/danh-muc-dti.model';
import { CriteriaGridTable } from '../../components/criteria-grid-table/criteria-grid-table';
import { CriteriaFormDialog } from '../../components/criteria-form-dialog/criteria-form-dialog';
import { ConfirmDialog } from '../../components/confirm-dialog/confirm-dialog';
import { CsvImportDialog } from '../../components/csv-import-dialog/csv-import-dialog';
import { ImportResultDialog } from '../../components/import-result-dialog/import-result-dialog';

const CURRENT_YEAR = new Date().getFullYear();
const DEBOUNCE_MS = 300;
const DEFAULT_PAGE_SIZE = 20;
const LIVE_PERIOD = 'all';

/**
 * SMART — route target `danh-muc/dti`. Điều phối `criteria-grid-table` + 4 dialog, gọi
 * `DanhMucDtiService`. Bộ lọc năm/kỳ/nhóm/search phân trang SERVER-SIDE (`GET /api/criteria`,
 * xem doc/contracts/danh-muc-dti.md CONTRACT DM-2) — search debounce 300ms theo đúng chuẩn
 * doc/huong_dan/wiki-core/fe/13-performance.md §6.
 */
@Component({
  selector: 'app-danh-muc-dti-page',
  standalone: true,
  imports: [CriteriaGridTable, CriteriaFormDialog, ConfirmDialog, CsvImportDialog, ImportResultDialog],
  templateUrl: './danh-muc-dti.page.html',
  styleUrl: './danh-muc-dti.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DanhMucDtiPage {
  private readonly service = inject(DanhMucDtiService);
  private readonly periodOptionsService = inject(PeriodOptionsService);
  private readonly toast = inject(ToastService);

  // ===== Bộ lọc =====
  protected readonly selectedYear = signal(CURRENT_YEAR);
  protected readonly selectedPeriod = signal(LIVE_PERIOD);
  protected readonly selectedGroupId = signal('');
  protected readonly searchInput = signal('');
  private readonly searchSubject = new Subject<string>();
  private readonly debouncedSearch = toSignal(
    this.searchSubject.pipe(debounceTime(DEBOUNCE_MS), distinctUntilChanged()),
    { initialValue: '' },
  );

  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);

  protected readonly groups = signal<ICriteriaGroup[]>([]);
  protected readonly periodOptions = signal<IPeriodOptions | null>(null);

  protected readonly rows = signal<ICriteriaRow[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);

  /**
   * Chỉ dùng để ẩn/hiện control ghi (Import/+Thêm/banner) ở FE — BE vẫn là nguồn sự thật cuối
   * cùng qua `row.IsEditable` (đọc từng dòng) + 409 `CRITERIA_ASSESSMENT_READONLY_PERIOD` khi
   * upsert assessment lệch trạng thái Live, xem CONTRACT DM-6.
   */
  protected readonly isLive = computed(() => this.selectedYear() === CURRENT_YEAR && this.selectedPeriod() === LIVE_PERIOD);

  protected readonly formOpen = signal(false);
  protected readonly formEditing = signal<ICriteria | null>(null);
  protected readonly formServerError = signal<string | null>(null);

  protected readonly confirmOpen = signal(false);
  protected readonly confirmTarget = signal<ICriteriaRow | null>(null);
  protected readonly confirmMessage = signal('');

  protected readonly csvDialogOpen = signal(false);
  protected readonly csvImporting = signal(false);
  protected readonly importResultOpen = signal(false);
  protected readonly importResult = signal<ICsvImportResult | null>(null);

  private readonly requestParams = computed<ICriteriaListParams>(() => ({
    Year: this.selectedYear(),
    Period: this.selectedPeriod(),
    GroupId: this.selectedGroupId() || undefined,
    Search: this.debouncedSearch() || undefined,
    Page: this.page(),
    PageSize: this.pageSize(),
  }));

  constructor() {
    this.service.getGroups().subscribe({
      next: (groups) => this.groups.set(groups),
      error: () => this.groups.set([]),
    });

    effect(() => {
      const year = this.selectedYear();
      this.periodOptionsService.getPeriodOptions(year).subscribe({
        next: (options) => this.periodOptions.set(options),
        error: () => this.periodOptions.set(null),
      });
    });

    effect(() => {
      const params = this.requestParams();
      this.loading.set(true);
      this.service.getList(params).subscribe({
        next: (result) => {
          this.rows.set(result.Items);
          this.totalCount.set(result.TotalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    });
  }

  private reload(): void {
    // Kích lại effect gọi list bằng cách "chạm" vào 1 signal nó phụ thuộc — page không đổi thì
    // set lại đúng giá trị hiện tại vẫn an toàn (Angular chỉ re-run effect khi giá trị thực sự
    // khác, nhưng nguồn dữ liệu server đã đổi sau CUD nên cần gọi lại tay ở đây).
    this.service.getList(this.requestParams()).subscribe({
      next: (result) => {
        this.rows.set(result.Items);
        this.totalCount.set(result.TotalCount);
      },
      error: () => {
        // httpErrorInterceptor đã hiện toast — giữ nguyên rows cũ, không xoá dữ liệu đang hiển thị.
      },
    });
  }

  // ===== Bộ lọc =====
  onYearChange(year: number): void {
    this.selectedYear.set(year);
    this.selectedPeriod.set(LIVE_PERIOD);
    this.page.set(1);
  }

  onPeriodChange(value: string): void {
    this.selectedPeriod.set(value);
    this.page.set(1);
  }

  onGroupChange(value: string): void {
    this.selectedGroupId.set(value);
    this.page.set(1);
  }

  onSearchInput(value: string): void {
    this.searchInput.set(value);
    this.searchSubject.next(value);
    this.page.set(1);
  }

  // Adapter cho binding template (tránh `$any($event.target)` rải trong HTML) — chỉ đọc giá trị
  // DOM rồi gọi lại đúng method nghiệp vụ ở trên.
  onSearchInputEvent(event: Event): void {
    this.onSearchInput((event.target as HTMLInputElement).value);
  }

  onGroupSelectChange(event: Event): void {
    this.onGroupChange((event.target as HTMLSelectElement).value);
  }

  onYearSelectChange(event: Event): void {
    this.onYearChange(Number((event.target as HTMLSelectElement).value));
  }

  onPeriodSelectChange(event: Event): void {
    this.onPeriodChange((event.target as HTMLSelectElement).value);
  }

  onGridPageChange(event: { Page: number; PageSize: number }): void {
    if (event.PageSize !== this.pageSize()) {
      this.pageSize.set(event.PageSize);
    }
    this.page.set(event.Page);
  }

  // ===== CRUD chỉ tiêu =====
  openCreateForm(): void {
    this.formEditing.set(null);
    this.formServerError.set(null);
    this.formOpen.set(true);
  }

  openEditForm(row: ICriteriaRow): void {
    this.formEditing.set({
      Id: row.CriteriaId,
      Code: row.Code,
      Name: row.Name,
      GroupId: row.GroupId,
      GroupName: row.GroupName,
      MaxScore: row.MaxScore,
    });
    this.formServerError.set(null);
    this.formOpen.set(true);
  }

  onFormSaved(payload: ICriteriaUpsertPayload): void {
    const editing = this.formEditing();
    const request$ = editing ? this.service.update(editing.Id, payload) : this.service.create(payload);
    request$.subscribe({
      next: () => {
        this.formOpen.set(false);
        this.reload();
        this.toast.success(editing ? 'Đã cập nhật chỉ tiêu.' : 'Đã thêm chỉ tiêu.');
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.formServerError.set(err.apiResult?.message ?? 'Không lưu được chỉ tiêu — thử lại sau.');
      },
    });
  }

  onFormClosed(): void {
    this.formOpen.set(false);
  }

  onDeleteRow(row: ICriteriaRow): void {
    this.confirmTarget.set(row);
    const hasHistory = row.AssessmentId !== null;
    this.confirmMessage.set(
      hasHistory
        ? `Chỉ tiêu "${row.Code}" đã có dữ liệu đánh giá — sẽ ẩn khỏi danh mục (soft-delete), lịch sử vẫn được giữ nguyên. Tiếp tục?`
        : `Xoá hẳn chỉ tiêu "${row.Code}"? Chỉ tiêu này chưa có dữ liệu đánh giá nào nên sẽ bị xoá vĩnh viễn.`,
    );
    this.confirmOpen.set(true);
  }

  onConfirmDelete(): void {
    const row = this.confirmTarget();
    if (!row) return;
    this.service.delete(row.CriteriaId).subscribe({
      next: () => {
        this.confirmOpen.set(false);
        this.reload();
        this.toast.success('Đã xoá chỉ tiêu.');
      },
      error: () => this.confirmOpen.set(false),
    });
  }

  onConfirmClosed(): void {
    this.confirmOpen.set(false);
  }

  // ===== Inline-edit Tiến độ %/Ghi chú =====
  onEditProgress(event: { Row: ICriteriaRow; Value: number }): void {
    this.service
      .upsertAssessment(
        event.Row.CriteriaId,
        { ProgressPercent: event.Value, Note: event.Row.Note },
        this.selectedYear(),
        this.selectedPeriod(),
      )
      .subscribe({
        next: () => this.reload(),
        error: () => this.toast.error('Không lưu được Tiến độ % — thử lại sau.'),
      });
  }

  onEditNote(event: { Row: ICriteriaRow; Value: string }): void {
    this.service
      .upsertAssessment(
        event.Row.CriteriaId,
        { ProgressPercent: event.Row.ProgressPercent, Note: event.Value },
        this.selectedYear(),
        this.selectedPeriod(),
      )
      .subscribe({
        next: () => this.reload(),
        error: () => this.toast.error('Không lưu được Ghi chú — thử lại sau.'),
      });
  }

  // ===== Import CSV =====
  openCsvImportDialog(): void {
    this.csvDialogOpen.set(true);
  }

  onCsvImported(file: File): void {
    this.csvImporting.set(true);
    this.service.importCsv(file).subscribe({
      next: (result) => {
        this.csvImporting.set(false);
        this.csvDialogOpen.set(false);
        this.importResult.set(result);
        this.importResultOpen.set(true);
        this.reload();
      },
      error: () => {
        this.csvImporting.set(false);
      },
    });
  }

  onCsvDialogClosed(): void {
    this.csvDialogOpen.set(false);
  }

  onImportResultClosed(): void {
    this.importResultOpen.set(false);
  }
}
