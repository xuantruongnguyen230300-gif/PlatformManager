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
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { CRITERIA_STATUSES } from '../../../../shared/models/dti-criteria-status.model';
import { ICriteriaGroup, ICriteriaRow, IOwnerOption } from '../../models/danh-muc-dti.model';

/** DM-3 — `maxlength 20` trên `Mã`, cưỡng chế cả ở form lẫn ở server. */
const CODE_MAX_LENGTH = 20;

/** Q58 — mỗi **đoạn** (phần giữa hai dấu chấm) tối đa 4 chữ số. */
const CODE_MAX_SEGMENT_DIGITS = 4;

/**
 * Q65 — chỉ chữ số và dấu chấm ngăn cách; không đoạn rỗng, không dấu chấm ở đầu/cuối.
 *
 * 🛑 **KHÔNG** giả định đúng hai cấp: dữ liệu thật có `4.22.11`, và một khuôn `N.N` sẽ từ chối
 * những mã hoàn toàn hợp lệ. Số **cấp** không bị giới hạn; thứ bị giới hạn là số **chữ số mỗi
 * đoạn** (Q58) và tổng độ dài (`maxlength`).
 */
const CODE_PATTERN = /^\d+(\.\d+)*$/;

/** Khoá của một ô trên form — dùng cho `fieldErrors`, `aria-describedby` và `[class.invalid]`. */
export type CriteriaFormField = 'Code' | 'Name' | 'GroupId' | 'MaxScore';

/** Những gì FORM biết. Kỳ đích (`Period`/`Year`) **không** ở đây — nó là trạng thái của MÀN HÌNH. */
export interface ICriteriaFormValue {
  readonly Code: string;
  readonly Name: string;
  readonly GroupId: string;
  readonly MaxScore: number;
  readonly SelfScore: number | null;
  readonly VerifiedScore: number | null;
  readonly Status: string | null;
  readonly OwnerId: string | null;
  readonly Deadline: string | null;
  readonly Note: string | null;
}

/**
 * V9 — dialog `Thêm chỉ tiêu` / `Sửa chỉ tiêu`. **10 trường, 2 nhóm, MỘT nút Lưu, MỘT request.**
 *
 * | Nhóm | Trường | Bắt buộc |
 * | --- | --- | --- |
 * | 1 — bản thân chỉ tiêu | `Mã` · `Tên chỉ tiêu` · `Nhóm` · `Điểm tối đa` | **có**, cả bốn |
 * | 2 — đánh giá theo kỳ (Q9) | `Tự đánh giá` · `Thẩm định` · `Trạng thái` · `Phụ trách` · `Hạn xử lý` · `Minh chứng/Ghi chú` | không |
 *
 * `Chênh lệch` **không có mặt** — trường TÍNH, chỉ hiển thị ở lưới (Q25). `Tiến độ %` cũng không:
 * nó sửa inline (Q9). Thêm một trong hai vào đây là dựng nguồn nhập liệu thứ hai cho một con số
 * đã có chủ.
 *
 * ## Kỳ đích KHÔNG phải một ô của form
 *
 * Nó đến từ ô lọc `Kỳ trong năm` và được trang ghép vào payload. Dialog chỉ **nói lại** kỳ đó
 * (`targetPeriodText`) — §5.5 mục 1: *"kỳ đích phải nhìn thấy được ngay tại chỗ đang gõ"*, và ở
 * chế độ `Tất cả` thì kỳ đích là **tuần hiện tại**, không phải kỳ của dòng đang mở (mục 2).
 *
 * ## `assessment` VẮNG MẶT ≠ `assessment` rỗng
 *
 * Hai ca khác nghĩa hẳn nhau — *"không đụng tới dữ liệu đánh giá"* và *"xoá trắng dữ liệu đánh
 * giá"* — nên form phát ra `ICriteriaFormValue` **phẳng** và để trang quyết dựng `Assessment` hay
 * không. Dựng sẵn một object rỗng ở đây là chọn hộ vế thứ hai cho mọi lần lưu.
 */
@Component({
  selector: 'app-criteria-form-dialog',
  standalone: true,
  imports: [AutofocusDirective, TranslatePipe],
  templateUrl: './criteria-form-dialog.html',
  styleUrl: './criteria-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CriteriaFormDialog {
  private readonly platformId = inject(PLATFORM_ID);

  /** Chỉ dịch KHOÁ lỗi kiểm-tại-chỗ. `TranslateService` nằm trong `UI_INFRA_SERVICES` của G4. */
  private readonly translate = inject(TranslateService);

  readonly open = input.required<boolean>();

  /** Dòng đang sửa; `null` = thêm mới. Quyết cả tiêu đề lẫn giá trị khởi tạo của 10 ô. */
  readonly editing = input<ICriteriaRow | null>(null);

  readonly groups = input<readonly ICriteriaGroup[]>([]);
  readonly owners = input<readonly IOwnerOption[]>([]);

  /** Tải danh sách người dùng HỎNG (thường là `403`, xem `CriteriaOwnerService`) ⇒ ô `disabled`. */
  readonly ownersFailed = input<boolean>(false);

  /** Danh sách người dùng đã bị cắt theo trần ⇒ hiện lối tìm kiếm thay vì im lặng thiếu người. */
  readonly ownersTruncated = input<boolean>(false);

  /** Câu đã dịch: "đang nhập cho kỳ X". `''` ⇒ không hiện dòng nào. */
  readonly targetPeriodText = input<string>('');

  readonly serverError = input<string | null>(null);
  readonly saving = input<boolean>(false);

  readonly saved = output<ICriteriaFormValue>();
  readonly closed = output<void>();
  readonly ownerSearch = output<string>();

  protected readonly statuses = CRITERIA_STATUSES;
  protected readonly codeMaxLength = CODE_MAX_LENGTH;

  protected readonly titleKey = computed(() =>
    this.editing() ? 'danh-muc-dti.dialog.editTitle' : 'danh-muc-dti.dialog.createTitle',
  );

  protected readonly codeField = signal('');
  protected readonly nameField = signal('');
  protected readonly groupField = signal('');
  protected readonly maxScoreField = signal('');
  protected readonly selfScoreField = signal('');
  protected readonly verifiedScoreField = signal('');
  protected readonly statusField = signal('');
  protected readonly ownerField = signal('');
  protected readonly deadlineField = signal('');
  protected readonly noteField = signal('');

  /** Lỗi form tự kiểm — GIÁ TRỊ LÀ KHOÁ DỊCH, không phải câu. Xem `fieldErrors`. */
  private readonly localFieldErrors = signal<Partial<Record<CriteriaFormField, string>>>({});

  /**
   * Câu lỗi dưới từng ô.
   *
   * Lỗi server **không** được bind theo ô ở màn này: catalog DTI trả `businessCode` cấp request
   * (`CRITERIA.DUPLICATE_CODE`, `CRITERIA.GROUP_NOT_FOUND`…) chứ không trả `fieldErrors` theo ô,
   * nên chúng hiện ở `.form-error` chung phía dưới. Bind một mã cấp request vào một ô là đoán —
   * và đoán sai thì câu lỗi nằm dưới ô không liên quan.
   */
  protected readonly fieldErrors = computed<Record<string, string>>(() => {
    this.translate.currentLang();
    return Object.fromEntries(
      Object.entries(this.localFieldErrors()).map(([field, key]) => [
        field,
        this.translate.instant(key as string) as string,
      ]),
    );
  });

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      const el = this.dialogEl().nativeElement;
      if (this.open() && !el.open) {
        this.resetFrom(this.editing());
        el.showModal();
      }
      if (!this.open() && el.open) el.close();
    });
  }

  /**
   * Khởi tạo 10 ô.
   *
   * Số ra ô nhập là số **THÔ** (`${value}`), không qua `Intl.NumberFormat`: một ô `input` đã định
   * dạng theo locale (`7,04`) sẽ được `Number()` đọc thành `NaN` lúc gửi. Định dạng theo locale là
   * việc của chỗ **hiển thị**, không phải chỗ **nhập**.
   */
  private resetFrom(row: ICriteriaRow | null): void {
    this.localFieldErrors.set({});
    this.codeField.set(row?.Code ?? '');
    this.nameField.set(row?.Name ?? '');
    this.groupField.set(row?.GroupId ?? '');
    this.maxScoreField.set(row ? `${row.MaxScore}` : '');
    this.selfScoreField.set(row?.SelfScore === null || row === null ? '' : `${row.SelfScore}`);
    this.verifiedScoreField.set(row?.VerifiedScore === null || row === null ? '' : `${row.VerifiedScore}`);
    this.statusField.set(row?.Status ?? '');
    this.ownerField.set(row?.OwnerId ?? '');
    this.deadlineField.set(row?.Deadline ?? '');
    this.noteField.set(row?.Note ?? '');
  }

  protected onNativeClose(): void {
    this.closed.emit();
  }

  protected onInput(field: 'code' | 'name' | 'maxScore' | 'selfScore' | 'verifiedScore' | 'note', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLTextAreaElement).value;
    if (field === 'code') this.codeField.set(value);
    else if (field === 'name') this.nameField.set(value);
    else if (field === 'maxScore') this.maxScoreField.set(value);
    else if (field === 'selfScore') this.selfScoreField.set(value);
    else if (field === 'verifiedScore') this.verifiedScoreField.set(value);
    else this.noteField.set(value);
  }

  protected onSelect(field: 'group' | 'status' | 'owner', event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (field === 'group') this.groupField.set(value);
    else if (field === 'status') this.statusField.set(value);
    else this.ownerField.set(value);
  }

  protected onDeadlineInput(event: Event): void {
    this.deadlineField.set((event.target as HTMLInputElement).value);
  }

  protected onOwnerSearch(event: Event): void {
    this.ownerSearch.emit((event.target as HTMLInputElement).value);
  }

  /** `{code}. {name}` — Q42. Ghép qua khoá dịch, không nối chuỗi trong template. */
  protected groupParams(group: ICriteriaGroup): { Code: string; Name: string } {
    return { Code: group.Code, Name: group.Name };
  }

  protected errorId(field: CriteriaFormField): string {
    return `cf${field}Error`;
  }

  protected onSubmit(): void {
    // Chốt chặn thứ hai sau `[disabled]`: nút tắt vẫn kích được bằng Enter ở một số đường.
    if (this.saving()) return;

    const code = this.codeField().trim();
    const name = this.nameField().trim();
    const groupId = this.groupField();
    const maxScore = Number(this.maxScoreField());

    // Gom HẾT lỗi rồi mới dừng, không `return` ở lỗi đầu tiên: mỗi ô có chỗ hiện lỗi riêng nên
    // báo một lỗi mỗi lần bấm là bắt người dùng sửa - bấm - sửa nhiều vòng không cần thiết.
    const errors: Partial<Record<CriteriaFormField, string>> = {};
    if (!code) errors.Code = 'danh-muc-dti.error.codeRequired';
    else if (code.length > CODE_MAX_LENGTH) errors.Code = 'danh-muc-dti.error.codeTooLong';
    else if (!CODE_PATTERN.test(code)) errors.Code = 'danh-muc-dti.error.codeFormatInvalid';
    else if (code.split('.').some((segment) => segment.length > CODE_MAX_SEGMENT_DIGITS)) {
      errors.Code = 'danh-muc-dti.error.codeSegmentTooLong';
    }

    if (!name) errors.Name = 'danh-muc-dti.error.nameRequired';
    if (!groupId) errors.GroupId = 'danh-muc-dti.error.groupRequired';
    if (!Number.isFinite(maxScore) || maxScore <= 0) errors.MaxScore = 'danh-muc-dti.error.maxScoreInvalid';

    this.localFieldErrors.set(errors);
    if (Object.keys(errors).length > 0) return;

    this.saved.emit({
      Code: code,
      Name: name,
      GroupId: groupId,
      MaxScore: maxScore,
      SelfScore: this.numberOrNull(this.selfScoreField()),
      VerifiedScore: this.numberOrNull(this.verifiedScoreField()),
      // Ô trống của `Trạng thái` / `Phụ trách` mang giá trị RỖNG và nghĩa là *không có* — quy về
      // `null`, KHÔNG gửi chuỗi rỗng: `""` không thuộc 4 giá trị Q4 và sẽ nhận
      // `400 CRITERIA.STATUS_INVALID`, còn `ownerId: ""` không phải một GUID.
      Status: this.statusField() || null,
      OwnerId: this.ownerField() || null,
      Deadline: this.deadlineField() || null,
      Note: this.noteField().trim() || null,
    });
  }

  /**
   * Ô số trống ⇒ `null`, **không** ⇒ `0`.
   *
   * Hai thứ đó khác nhau ở mọi chỗ tiêu thụ: `0` điểm là "đã chấm, được 0", còn vắng mặt là "chưa
   * ai chấm" — và `diff` chỉ được tính khi **cả hai** điểm có mặt (DM-2).
   */
  private numberOrNull(raw: string): number | null {
    const text = raw.trim();
    if (text === '') return null;
    const value = Number(text);
    return Number.isFinite(value) ? value : null;
  }
}
