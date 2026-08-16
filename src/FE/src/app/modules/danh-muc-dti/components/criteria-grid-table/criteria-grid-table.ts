import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  PLATFORM_ID,
  afterNextRender,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { ICriteriaRow } from '../../models/danh-muc-dti.model';

type EditField = 'progress' | 'note';
interface IEditingCell {
  CriteriaId: string;
  Field: EditField;
}

function formatPercent(v: number | null): string {
  return v === null ? '—' : `${v.toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}

function formatScore(v: number | null): string {
  return v === null ? '—' : v.toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatDateVn(iso: string | null): string {
  if (!iso) return '—';
  const [y, m, d] = iso.split('-');
  return `${d}/${m}/${y}`;
}

/**
 * Grid chỉnh sửa được (đọc CONTRACT DM-2, doc/contracts/danh-muc-dti.md) — `p-table` scrollable
 * với cột Mã ghim trái + cột Hành động ghim phải (`pFrozenColumn`), inline-edit cột Tiến độ %/Ghi
 * chú bằng DOUBLE-CLICK (Enter=lưu/Escape=huỷ, auto-focus qua `appAutofocus`) — quyết định UX
 * MỚI cho bản Angular (khác prototype gốc dùng single-click), theo đúng yêu cầu gốc của task.
 * Chỉ sửa được khi `row.IsEditable` (BE tính theo "đang xem Live" — năm hiện tại + period=all).
 *
 * Component KHÔNG tự gọi service — mọi thay đổi phát qua output() để `DanhMucDtiPage` (smart)
 * điều phối gọi API rồi refetch, giữ đúng ranh giới dumb/smart.
 */
@Component({
  selector: 'app-criteria-grid-table',
  standalone: true,
  imports: [TableModule, AutofocusDirective],
  templateUrl: './criteria-grid-table.html',
  styleUrl: './criteria-grid-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CriteriaGridTable {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly hostRef = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<ICriteriaRow[]>();
  readonly loading = input<boolean>(false);
  readonly totalCount = input<number>(0);
  readonly page = input<number>(1);
  readonly pageSize = input<number>(20);

  readonly editProgress = output<{ Row: ICriteriaRow; Value: number }>();
  readonly editNote = output<{ Row: ICriteriaRow; Value: string }>();
  readonly editRow = output<ICriteriaRow>();
  readonly deleteRow = output<ICriteriaRow>();
  readonly pageChange = output<{ Page: number; PageSize: number }>();

  protected readonly editingCell = signal<IEditingCell | null>(null);
  protected readonly gridHeight = signal(480);

  protected readonly formatPercent = formatPercent;
  protected readonly formatScore = formatScore;
  protected readonly formatDateVn = formatDateVn;

  constructor() {
    afterNextRender(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      this.recomputeHeight();
      const onResize = () => this.recomputeHeight();
      window.addEventListener('resize', onResize);
      this.destroyRef.onDestroy(() => window.removeEventListener('resize', onResize));
    });
  }

  private recomputeHeight(): void {
    const top = this.hostRef.nativeElement.getBoundingClientRect().top;
    const available = window.innerHeight - top - 160; // chừa chỗ pagination + padding dưới card
    this.gridHeight.set(Math.max(320, Math.round(available)));
  }

  isEditing(criteriaId: string, field: EditField): boolean {
    const cell = this.editingCell();
    return !!cell && cell.CriteriaId === criteriaId && cell.Field === field;
  }

  startEdit(row: ICriteriaRow, field: EditField): void {
    if (!row.IsEditable) return;
    this.editingCell.set({ CriteriaId: row.CriteriaId, Field: field });
  }

  cancelEdit(): void {
    this.editingCell.set(null);
  }

  confirmEdit(row: ICriteriaRow, field: EditField, rawValue: string): void {
    const cell = this.editingCell();
    if (!cell || cell.CriteriaId !== row.CriteriaId || cell.Field !== field) return;
    if (field === 'progress') {
      const value = Math.max(0, Math.min(100, Number(rawValue) || 0));
      this.editProgress.emit({ Row: row, Value: value });
    } else {
      this.editNote.emit({ Row: row, Value: rawValue });
    }
    this.editingCell.set(null);
  }

  onCellKeydown(event: KeyboardEvent, row: ICriteriaRow, field: EditField, input: HTMLInputElement): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.confirmEdit(row, field, input.value);
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.cancelEdit();
    }
  }

  /** Server-side pagination — xem doc/huong_dan/wiki-core/fe/11-grid-and-metadata.md §Mẫu dùng p-table. */
  onLazyLoad(event: TableLazyLoadEvent): void {
    const rows = event.rows ?? this.pageSize();
    const first = event.first ?? 0;
    const page = Math.floor(first / rows) + 1;
    this.pageChange.emit({ Page: page, PageSize: rows });
  }
}
