import { Component, ElementRef, effect, input, output, signal, viewChild } from '@angular/core';

import { ICriteria, ICriteriaFormValue, ICriteriaGroup } from '../../models/danh-muc-dti.model';

/**
 * Dialog "Thêm chỉ tiêu"/"Sửa" — dumb component, chỉ validate field cục bộ
 * (`spec/danh-muc-dti/business-rules.md` mục 1.1/1.2) rồi phát `submit`; page
 * cha mới gọi `DanhMucDtiService.createCriteria`/`updateCriteria`. Lỗi
 * business từ server (vd trùng Code) hiện qua toast dùng chung
 * (`core/interceptors/http-error.interceptor.ts`), không round-trip lại vào
 * dialog này.
 */
@Component({
  selector: 'app-criteria-form-dialog',
  standalone: true,
  templateUrl: './criteria-form-dialog.html',
  styleUrl: './criteria-form-dialog.scss'
})
export class CriteriaFormDialog {
  readonly open = input.required<boolean>();
  readonly groups = input.required<ICriteriaGroup[]>();
  readonly editing = input<ICriteria | null>(null);

  readonly submit = output<ICriteriaFormValue>();
  readonly closed = output<void>();

  readonly code = signal('');
  readonly name = signal('');
  readonly groupId = signal('');
  readonly maxScore = signal<number | null>(null);
  readonly error = signal<string | null>(null);

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('dialogRef');

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const dialog = this.dialogEl()?.nativeElement;
      if (!dialog) return;

      if (isOpen) {
        const editing = this.editing();
        this.code.set(editing?.Code ?? '');
        this.name.set(editing?.Name ?? '');
        this.groupId.set(editing?.GroupId ?? this.groups()[0]?.Id ?? '');
        this.maxScore.set(editing?.MaxScore ?? null);
        this.error.set(null);
        if (!dialog.open) dialog.showModal();
      } else if (dialog.open) {
        dialog.close();
      }
    });
  }

  get title(): string {
    return this.editing() ? 'Sửa chỉ tiêu' : 'Thêm chỉ tiêu';
  }

  onNativeClose(): void {
    this.closed.emit();
  }

  onCancel(): void {
    this.closed.emit();
  }

  onMaxScoreInput(value: string): void {
    this.maxScore.set(value === '' ? null : Number(value));
  }

  onSubmit(): void {
    const code = this.code().trim();
    const name = this.name().trim();
    const groupId = this.groupId();
    const maxScore = this.maxScore();

    if (!code || code.length > 20) {
      this.error.set('Mã bắt buộc, tối đa 20 ký tự.');
      return;
    }
    if (!name) {
      this.error.set('Tên chỉ tiêu bắt buộc.');
      return;
    }
    if (!groupId) {
      this.error.set('Vui lòng chọn nhóm.');
      return;
    }
    if (!maxScore || maxScore <= 0) {
      this.error.set('Điểm tối đa phải lớn hơn 0.');
      return;
    }

    this.error.set(null);
    this.submit.emit({ Code: code, Name: name, GroupId: groupId, MaxScore: maxScore });
  }
}
