import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { ICriteria, ICriteriaGroup, ICriteriaUpsertPayload } from '../../models/danh-muc-dti.model';

const MAX_CODE_LENGTH = 20;

/**
 * Dialog Thêm/Sửa chỉ tiêu (native `<dialog>`) — validate client trước (code≤20/tên bắt buộc/
 * nhóm bắt buộc/điểm>0, khớp yêu cầu gốc task), `serverError` hiển thị lỗi 409 (trùng mã)/422 từ
 * `DanhMucDtiPage` khi gọi API thất bại — page tự đọc `err.apiResult?.message`/`fields`, dialog
 * chỉ render chuỗi đã có sẵn (không tự parse envelope).
 */
@Component({
  selector: 'app-criteria-form-dialog',
  standalone: true,
  imports: [AutofocusDirective],
  templateUrl: './criteria-form-dialog.html',
  styleUrl: './criteria-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CriteriaFormDialog {
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input.required<boolean>();
  readonly editing = input<ICriteria | null>(null);
  readonly groups = input<ICriteriaGroup[]>([]);
  readonly serverError = input<string | null>(null);

  readonly saved = output<ICriteriaUpsertPayload>();
  readonly closed = output<void>();

  protected readonly localError = signal<string | null>(null);
  protected readonly title = computed(() => (this.editing() ? 'Sửa chỉ tiêu' : 'Thêm chỉ tiêu'));
  protected readonly errorMessage = computed(() => this.localError() ?? this.serverError());

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      const el = this.dialogEl().nativeElement;
      if (this.open() && !el.open) {
        this.localError.set(null);
        el.showModal();
      }
      if (!this.open() && el.open) el.close();
    });
  }

  onNativeClose(): void {
    this.closed.emit();
  }

  onSubmit(
    codeInput: HTMLInputElement,
    nameInput: HTMLTextAreaElement,
    groupSelect: HTMLSelectElement,
    maxScoreInput: HTMLInputElement,
  ): void {
    const code = codeInput.value.trim();
    const name = nameInput.value.trim();
    const groupId = groupSelect.value;
    const maxScore = Number(maxScoreInput.value);

    if (!code || code.length > MAX_CODE_LENGTH) {
      this.localError.set('Mã bắt buộc, tối đa 20 ký tự.');
      return;
    }
    if (!name) {
      this.localError.set('Tên chỉ tiêu bắt buộc.');
      return;
    }
    if (!groupId) {
      this.localError.set('Vui lòng chọn nhóm.');
      return;
    }
    if (!(maxScore > 0)) {
      this.localError.set('Điểm tối đa phải lớn hơn 0.');
      return;
    }

    this.localError.set(null);
    this.saved.emit({ Code: code, Name: name, GroupId: groupId, MaxScore: maxScore });
  }
}
