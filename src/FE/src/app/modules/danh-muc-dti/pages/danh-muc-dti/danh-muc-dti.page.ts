import { isPlatformBrowser } from '@angular/common';
import {
  Component,
  ElementRef,
  PLATFORM_ID,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { RouterLink } from '@angular/router';

import { NotificationService } from '../../../../core/services/notification.service';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { Topbar } from '../../../../shared/components/topbar/topbar';
import { ConfirmDialog } from '../../components/confirm-dialog/confirm-dialog';
import { CriteriaFormDialog } from '../../components/criteria-form-dialog/criteria-form-dialog';
import {
  CriteriaGridTable,
  ICellEditConfirmEvent,
  ICellEditStartEvent
} from '../../components/criteria-grid-table/criteria-grid-table';
import { CsvImportDialog } from '../../components/csv-import-dialog/csv-import-dialog';
import { ImportResultDialog } from '../../components/import-result-dialog/import-result-dialog';
import { GRID_DEFAULT_PAGE_SIZE, GRID_PAGE_SIZE_OPTIONS } from '../../data/danh-muc-dti.constants';
import {
  ICriteria,
  ICriteriaFormValue,
  ICriteriaGridResult,
  ICriteriaGroup,
  IEditingCell,
  IImportResult,
  PERIOD_ALL,
  PeriodValue
} from '../../models/danh-muc-dti.model';
import { DanhMucDtiService } from '../../services/danh-muc-dti.service';

interface ICriteriaDialogState {
  editing: ICriteria | null;
}

interface IConfirmDeleteState {
  criteriaId: string;
  message: string;
}

interface IPeriodListOption {
  Value: string;
  Label: string;
  Date: string;
}

const EMPTY_GRID_RESULT: ICriteriaGridResult = { IsEditable: false, Items: [], Page: 1, PageSize: 20, TotalCount: 0, TotalPages: 0 };

/**
 * Page SMART "Danh mục & Đánh giá theo tuần" — orchestrate filter Năm/Kỳ/tìm
 * kiếm/nhóm, phân trang, trạng thái editable/read-only (`IsEditable` từ
 * server), điều phối 4 dialog con + lưới. Toàn bộ gọi API qua
 * `DanhMucDtiService` — component con không bao giờ inject service trực tiếp.
 * `Period` khớp `doc/contracts/danh-muc-dti.md`: "all" | "YYYY-Www" (tuần ISO)
 * | "YYYY-MM" (tháng) — không còn khái niệm 1 ngày cụ thể.
 */
@Component({
  selector: 'app-danh-muc-dti-page',
  standalone: true,
  imports: [
    RouterLink,
    Topbar,
    Pagination,
    CriteriaGridTable,
    CriteriaFormDialog,
    ConfirmDialog,
    CsvImportDialog,
    ImportResultDialog
  ],
  templateUrl: './danh-muc-dti.page.html',
  styleUrl: './danh-muc-dti.page.scss'
})
export class DanhMucDtiPage {
  private readonly service = inject(DanhMucDtiService);
  private readonly notification = inject(NotificationService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly pageSizeOptions = [...GRID_PAGE_SIZE_OPTIONS];
  readonly periodAll = PERIOD_ALL;

  readonly groups = signal<ICriteriaGroup[]>([]);
  readonly years = signal<number[]>([]);
  readonly periodOptions = signal<IPeriodListOption[]>([]);

  readonly year = signal(new Date().getFullYear());
  readonly period = signal<PeriodValue>(PERIOD_ALL);
  readonly searchText = signal('');
  readonly groupFilter = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(GRID_DEFAULT_PAGE_SIZE);

  readonly gridResult = signal<ICriteriaGridResult>(EMPTY_GRID_RESULT);
  readonly loading = signal(false);
  readonly editingCell = signal<IEditingCell | null>(null);

  readonly criteriaDialogState = signal<ICriteriaDialogState | null>(null);
  readonly confirmDeleteState = signal<IConfirmDeleteState | null>(null);
  readonly csvDialogOpen = signal(false);
  readonly importLoading = signal(false);
  readonly importResult = signal<IImportResult | null>(null);
  readonly importResultDialogOpen = signal(false);

  readonly rows = computed(() => this.gridResult().Items);
  readonly totalCount = computed(() => this.gridResult().TotalCount);
  readonly isEditable = computed(() => this.gridResult().IsEditable);
  readonly rangeLabel = computed(() => {
    const total = this.totalCount();
    const count = this.rows().length;
    if (count === 0) return `0/${total}`;
    const start = (this.page() - 1) * this.pageSize() + 1;
    return `${start}–${start + count - 1}/${total}`;
  });

  private readonly gridCard = viewChild<ElementRef<HTMLElement>>('gridCard');
  readonly cardHeightPx = signal<number | null>(null);

  private searchDebounceHandle: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.service.getGroups().subscribe((groups) => this.groups.set(groups));
    this.loadPeriodOptions(this.year());
    this.loadGrid();

    afterNextRender(() => {
      this.updateGridCardHeight();
      if (isPlatformBrowser(this.platformId)) {
        window.addEventListener('resize', () => this.updateGridCardHeight());
        window.addEventListener('orientationchange', () => this.updateGridCardHeight());
      }
    });
  }

  // ===== Filters =====

  onSearchInput(value: string): void {
    clearTimeout(this.searchDebounceHandle);
    this.searchDebounceHandle = setTimeout(() => {
      this.searchText.set(value);
      this.page.set(1);
      this.loadGrid();
    }, 300);
  }

  onGroupFilterChange(value: string): void {
    this.groupFilter.set(value);
    this.page.set(1);
    this.loadGrid();
  }

  onYearChange(value: string): void {
    const year = Number(value);
    this.year.set(year);
    this.period.set(PERIOD_ALL);
    this.page.set(1);
    this.loadPeriodOptions(year);
    this.loadGrid();
  }

  onPeriodChange(value: string): void {
    this.period.set(value === '' ? PERIOD_ALL : value);
    this.page.set(1);
    this.loadGrid();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.loadGrid();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.loadGrid();
  }

  // ===== Edit-per-cell =====

  onCellEditStart(event: ICellEditStartEvent): void {
    this.editingCell.set({ CriteriaId: event.CriteriaId, Field: event.Field });
  }

  onCellEditCancel(): void {
    this.editingCell.set(null);
  }

  onCellEditConfirm(event: ICellEditConfirmEvent): void {
    const patch: { ProgressPercent?: number; Note?: string } = {};
    if (event.Field === 'progress') {
      const parsed = Number(event.Value);
      patch.ProgressPercent = Number.isFinite(parsed) ? Math.max(0, Math.min(100, parsed)) : 0;
    } else {
      patch.Note = event.Value;
    }

    this.service
      .patchAssessment(event.CriteriaId, patch, { Year: this.year(), Period: this.period() })
      .subscribe({
        next: (res) => {
          this.gridResult.update((gr) => ({
            ...gr,
            Items: gr.Items.map((row) =>
              row.CriteriaId === event.CriteriaId
                ? { ...row, ProgressPercent: res.ProgressPercent, Note: res.Note }
                : row
            )
          }));
          this.editingCell.set(null);
          this.notification.success('Đã lưu.');
          this.loadPeriodOptions(this.year());
        }
      });
  }

  // ===== CRUD Criteria =====

  openCreateDialog(): void {
    this.criteriaDialogState.set({ editing: null });
  }

  openEditDialog(criteriaId: string): void {
    const row = this.rows().find((r) => r.CriteriaId === criteriaId);
    if (!row) return;
    this.criteriaDialogState.set({
      editing: {
        Id: row.CriteriaId,
        Code: row.Code,
        Name: row.Name,
        GroupId: row.GroupId,
        GroupName: row.GroupName,
        MaxScore: row.MaxScore
      }
    });
  }

  onCriteriaDialogClosed(): void {
    this.criteriaDialogState.set(null);
  }

  onCriteriaSubmit(value: ICriteriaFormValue): void {
    const state = this.criteriaDialogState();
    if (!state) return;

    const request$ = state.editing
      ? this.service.updateCriteria(state.editing.Id, value)
      : this.service.createCriteria(value);

    request$.subscribe({
      next: () => {
        this.criteriaDialogState.set(null);
        this.notification.success(state.editing ? 'Đã cập nhật chỉ tiêu.' : 'Đã thêm chỉ tiêu.');
        this.loadGrid();
      }
    });
  }

  openDeleteConfirm(criteriaId: string): void {
    const row = this.rows().find((r) => r.CriteriaId === criteriaId);
    if (!row) return;
    this.confirmDeleteState.set({
      criteriaId,
      message: `Xoá chỉ tiêu ${row.Code}? Nếu chỉ tiêu đã có dữ liệu đánh giá, hệ thống sẽ ẩn khỏi danh mục (soft-delete) thay vì xoá hẳn.`
    });
  }

  onDeleteCancel(): void {
    this.confirmDeleteState.set(null);
  }

  onDeleteConfirmed(): void {
    const state = this.confirmDeleteState();
    if (!state) return;

    this.service.deleteCriteria(state.criteriaId).subscribe({
      next: (res) => {
        this.confirmDeleteState.set(null);
        this.notification.success(res.HardDeleted ? 'Đã xoá chỉ tiêu.' : 'Đã ẩn chỉ tiêu (đã có lịch sử đánh giá).');
        this.loadGrid();
      }
    });
  }

  // ===== Import CSV =====

  openCsvDialog(): void {
    this.csvDialogOpen.set(true);
  }

  onCsvDialogClosed(): void {
    this.csvDialogOpen.set(false);
  }

  onCsvImport(file: File): void {
    this.importLoading.set(true);
    this.service.importCsv(file).subscribe({
      next: (result) => {
        this.importLoading.set(false);
        this.csvDialogOpen.set(false);
        this.importResult.set(result);
        this.importResultDialogOpen.set(true);
        this.page.set(1);
        this.loadPeriodOptions(this.year());
        this.loadGrid();
      },
      error: () => this.importLoading.set(false)
    });
  }

  onImportResultClosed(): void {
    this.importResultDialogOpen.set(false);
  }

  // ===== Data loading =====

  private loadGrid(): void {
    this.loading.set(true);
    this.service
      .getGrid({
        Year: this.year(),
        Period: this.period(),
        SearchText: this.searchText(),
        GroupId: this.groupFilter(),
        Page: this.page(),
        PageSize: this.pageSize()
      })
      .subscribe({
        next: (result) => {
          this.gridResult.set(result);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  private loadPeriodOptions(year: number): void {
    this.service.getPeriodOptions(year).subscribe((options) => {
      this.years.set(options.Years);
      const weekOptions: IPeriodListOption[] = options.WeeksInYear.map((w) => ({
        Value: w.Value,
        Label: `Tuần ${this.weekNumberOf(w.Value)} · ${this.formatDateVN(w.Date)}`,
        Date: w.Date
      }));
      const monthOptions: IPeriodListOption[] = options.MonthsInYear.map((m) => ({
        Value: m.Value,
        Label: `Tháng ${this.monthNumberOf(m.Value)}/${year}`,
        Date: m.Date
      }));
      this.periodOptions.set(
        [...weekOptions, ...monthOptions].sort((a, b) => b.Date.localeCompare(a.Date))
      );
    });
  }

  private updateGridCardHeight(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const el = this.gridCard()?.nativeElement;
    if (!el) return;
    const top = el.getBoundingClientRect().top;
    this.cardHeightPx.set(Math.max(280, window.innerHeight - top - 16));
  }

  formatDateVN(value: string): string {
    const [y, m, d] = value.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }

  private weekNumberOf(value: string): string {
    const match = /W(\d+)/.exec(value);
    return match ? match[1] : value;
  }

  private monthNumberOf(value: string): string {
    const parts = value.split('-');
    return parts[1] ? String(Number(parts[1])) : value;
  }
}
