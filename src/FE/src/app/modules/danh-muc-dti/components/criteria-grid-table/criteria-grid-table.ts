import { Component, input, output } from '@angular/core';

import { TableScroll } from '../../../../shared/components/table-scroll/table-scroll';
import { CellField, ICriteriaGridRow, IEditingCell } from '../../models/danh-muc-dti.model';
import { EditableCell } from '../editable-cell/editable-cell';

export interface ICellEditConfirmEvent {
  CriteriaId: string;
  Field: CellField;
  Value: string;
}

export interface ICellEditStartEvent {
  CriteriaId: string;
  Field: CellField;
}

/**
 * Lưới "Danh mục & Đánh giá theo tuần" — dumb component (12 cột theo
 * `spec/danh-muc-dti/ui-spec.md` mục 5), cột Mã sticky trái + cột Hành động
 * sticky phải (mục 6.8). Không inject service nào — chỉ nhận `rows` đã phân
 * trang sẵn từ page cha, phát sự kiện cho mọi thao tác ghi.
 */
@Component({
  selector: 'app-criteria-grid-table',
  standalone: true,
  imports: [TableScroll, EditableCell],
  templateUrl: './criteria-grid-table.html',
  styleUrl: './criteria-grid-table.scss',
  host: {
    class: 'grid-table-host'
  }
})
export class CriteriaGridTable {
  readonly rows = input.required<ICriteriaGridRow[]>();
  readonly editingCell = input<IEditingCell | null>(null);
  readonly isEditable = input(false);

  readonly cellEditStart = output<ICellEditStartEvent>();
  readonly cellEditConfirm = output<ICellEditConfirmEvent>();
  readonly cellEditCancel = output<void>();
  readonly edit = output<string>();
  readonly delete = output<string>();

  isEditingCell(row: ICriteriaGridRow, field: CellField): boolean {
    const cell = this.editingCell();
    return !!cell && cell.CriteriaId === row.CriteriaId && cell.Field === field;
  }

  onCellStart(criteriaId: string, field: CellField): void {
    this.cellEditStart.emit({ CriteriaId: criteriaId, Field: field });
  }

  onCellConfirm(criteriaId: string, field: CellField, value: string): void {
    this.cellEditConfirm.emit({ CriteriaId: criteriaId, Field: field, Value: value });
  }

  formatDate(value: string | null): string {
    if (!value) return '—';
    const [y, m, d] = value.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }

  formatScore(value: number | null): string {
    return value === null || value === undefined ? '—' : value.toLocaleString('vi-VN', { maximumFractionDigits: 2 });
  }
}
