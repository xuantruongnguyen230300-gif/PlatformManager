import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { DataGrid, IDataGridPageChange } from '../../../../shared/components/data-grid/data-grid';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { DeltaIndicator } from '../../../../shared/components/delta-indicator/delta-indicator';
import { criteriaStatusBadge } from '../../../../shared/models/dti-criteria-status.model';
import { ICriteriaRow } from '../../models/danh-muc-dti.model';

/** `min-width` từng cột — giá trị đã duyệt ở `Screens/02-danh-muc-dti.md` § Layout Blueprint. */
const COLUMN_WIDTHS = {
  code: 70,
  name: 220,
  group: 120,
  period: 110,
  maxScore: 90,
  selfScore: 90,
  verifiedScore: 90,
  diff: 90,
  status: 150,
  owner: 110,
  deadline: 100,
  progress: 130,
  note: 220,
  actions: 120,
} as const;

/** Số cột **luôn** có mặt — tổng 14 trừ hai cột có điều kiện (`period`, `actions`). */
const ALWAYS_ON_COLUMNS = 12;

/** Hai ô sửa inline của Q9 — không có ô thứ ba, và đó là ràng buộc hợp đồng chứ không phải giới hạn tạm. */
export type InlineField = 'progress' | 'note';

/** Trần của `Tiến độ %`. FE kẹp TRƯỚC khi gửi; BE kẹp lại — hai lớp, cùng một miền (DM-6). */
const PROGRESS_MIN = 0;
const PROGRESS_MAX = 100;

/**
 * Một lần lưu inline — mang **đúng trường người dùng vừa sửa**, không hơn.
 *
 * 🛑 `?:` là NGỮ NGHĨA (Q74): `undefined` = *"không đụng tới"*, `null` = *"xoá trắng"*. BE phân
 * biệt hai thứ đó bằng `Assigned<T>`, nên "điền cho đủ" trường còn lại bằng giá trị đang hiện là
 * **ghi đè mù** thứ người khác vừa đổi.
 */
export interface IInlineSaveEvent {
  readonly CriteriaId: string;
  readonly ProgressPercent?: number | null;
  readonly Note?: string | null;
}

/**
 * Lưới 14 cột của màn Danh mục DTI — DUMB: nhận `input()`, phát `output()`, không tự gọi service.
 *
 * Khung (chiều cao cuộn trong, phân trang server-side, mặt nạ loading, ghim mép) thuộc
 * `<app-data-grid>`; file này chỉ khai **cột nào và mỗi ô vẽ ra sao** — đúng vai mà
 * `platform/quan-tri-nguoi-dung/components/user-grid-table/` đóng ở màn Core.
 *
 * ## Hai cột CÓ ĐIỀU KIỆN, và chúng đổi SỐ CỘT chứ không chỉ đổi hiển thị
 *
 * | Cột | Hiện khi | Nguồn luật |
 * | --- | --- | --- |
 * | `Kỳ của số liệu` (#4) | `Kỳ trong năm` = `Tất cả` | Q31, ui-spec §3.2.2 luật (a) |
 * | `Hành động` (#14) | `canWrite` của khối quyền cấp màn | Q39, ui-spec §5.6.1 ràng buộc 2 |
 *
 * `@if` bọc **cả `<th>` lẫn `<td>`** để số cột của header và body luôn khớp; `colspan` của dòng
 * rỗng tính theo số cột ĐANG render, không hardcode. Ẩn bằng `visibility: hidden` thì bảng vẫn
 * chừa chỗ — sai với lý do cột bị ẩn (một lưới đã phải cuộn ngang).
 *
 * ## Ghim hai mép — qua input `frozenColumns`, KHÔNG qua directive
 *
 * 🛑 Không đặt `pFrozenColumn`/`alignFrozen` ở đây: chúng là directive của `TableModule`, mà
 * `data-grid` là nơi DUY NHẤT được import `p-table` (ui-spec §3.2). Trang khai *cột nào bị ghim*,
 * component chịu trách nhiệm *ghim ra sao*.
 *
 * Mép phải chỉ có nghĩa khi cột `Hành động` còn tồn tại. Không có quyền ghi ⇒ cột biến mất ⇒ chỉ
 * còn **một** cột ghim, và đó là cấu hình hợp lệ của biến thể, không phải ca cần xử lý riêng.
 *
 * ## Sửa inline — đúng HAI ô, và ô thứ ba không tồn tại (Q9)
 *
 * `Tiến độ %` và `Minh chứng/Ghi chú`. Bốn trường đánh giá còn lại chỉ sửa được qua hộp thoại
 * (DM-4) — ràng buộc của **hợp đồng**, không phải giới hạn tạm thời.
 *
 * Bấm đúp mở editor; `Enter`/`Space` trên ô cũng mở (a11y §4 ràng buộc 2). `Enter` lưu, `Escape`
 * huỷ, rời ô **lưu** — xem `onEditorBlur` để biết vì sao chiều đó chứ không phải chiều ngược lại.
 *
 * `isEditable = false` ⇒ hai ô mất hẳn ngữ nghĩa control (không `.cell-editable`, không
 * `tabindex`, không `role`, không `title`), đúng hình dạng của ca không có quyền ghi (§5.6.1 ràng
 * buộc 3). Để lại `tabindex` trên một ô không mở được là đưa bàn phím vào một control câm.
 *
 * ## Component này KHÔNG gọi API
 *
 * Nó phát `inlineSave` / `editRow` / `deleteRow` rồi thôi — G4, và cũng là lý do một lần lưu hỏng
 * tự động **hoàn nguyên** ô: giá trị hiển thị luôn đến từ `rows()`, mà `rows()` chỉ đổi khi trang
 * thay dòng sau một response thành công.
 */
@Component({
  selector: 'app-criteria-grid-table',
  standalone: true,
  imports: [AutofocusDirective, DataGrid, DeltaIndicator, TranslatePipe],
  templateUrl: './criteria-grid-table.html',
  styleUrl: './criteria-grid-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CriteriaGridTable {
  readonly rows = input.required<ICriteriaRow[]>();
  readonly loading = input<boolean>(false);
  readonly totalCount = input<number>(0);
  readonly page = input<number>(1);
  readonly pageSize = input<number>(10);

  /** Q31 — chỉ `true` ở chế độ `Kỳ trong năm` = `Tất cả`. */
  readonly showPeriodColumn = input<boolean>(false);

  /** Q39 — `canWrite` của khối quyền CẤP MÀN. `false` ⇒ ẩn CẢ cột `Hành động`. */
  readonly canWrite = input<boolean>(false);

  /**
   * `isEditable` của khối quyền CẤP MÀN — bật/**tắt** affordance, không ẩn (§5.5.1).
   *
   * 🛑 KHÁC `canWrite`, và đừng hoán đổi: `canWrite = false` nghĩa *"không phải của bạn"* ⇒ ẩn
   * hẳn; `isEditable = false` nghĩa *"không phải lúc này"* ⇒ nút ở nguyên chỗ và xám đi. Hoán đổi
   * hai thứ này làm toolbar nhảy mỗi lần đổi ô lọc, và làm người có quyền tưởng mình vừa mất quyền.
   */
  readonly isEditable = input<boolean>(false);

  /**
   * Q38 + T14 — chuỗi ĐÃ ĐỊNH DẠNG cho ô `Kỳ của số liệu`, tra theo mã kỳ (`"2026-W33"`).
   *
   * Trang dựng bảng này từ `weeksInYear` của DB-3 (một phép TRA, không phải quy đổi lịch — quy mã
   * tuần ra khoảng ngày là việc của BE, `business-rules` §5.1). Tra trượt thì ô lùi về nhãn đầy đủ
   * BE trả kèm dòng, và đó vẫn là một câu đúng, chỉ dài hơn.
   */
  readonly periodRangeLabels = input<Readonly<Record<string, string>>>({});

  /** Ô rỗng dùng chung một dấu gạch — `shared.format.absentValue`. */
  readonly localeId = input<string>('vi');

  readonly pageChange = output<{ Page: number; PageSize: number }>();
  readonly inlineSave = output<IInlineSaveEvent>();
  readonly editRow = output<string>();
  readonly deleteRow = output<string>();

  /** Ô ĐANG mở editor — `null` là không ô nào. Đúng MỘT ô mỗi lúc, cố ý. */
  protected readonly editing = signal<{ CriteriaId: string; Field: InlineField } | null>(null);

  /** Giá trị đang gõ trong editor. Chưa lưu ⇒ KHÔNG chạm vào `rows()`. */
  protected readonly draft = signal('');

  /**
   * Escape đang huỷ — đọc trong `(blur)`.
   *
   * Cần cờ này vì `(blur)` cũng LƯU (xem `onBlur`): gõ Escape làm editor đóng, editor đóng làm
   * input mất focus, và `(blur)` sẽ lưu đúng thứ vừa bị huỷ. Không có cờ thì Escape không huỷ
   * được gì cả — một lỗi không đỏ ở đâu, chỉ sai lúc chạy.
   */
  private cancelling = false;

  protected readonly widths = COLUMN_WIDTHS;

  /** Số cột ĐANG render — `colspan` của dòng rỗng đọc từ đây, không viết số cứng. */
  protected readonly columnCount = computed<number>(
    () => ALWAYS_ON_COLUMNS + (this.showPeriodColumn() ? 1 : 0) + (this.canWrite() ? 1 : 0),
  );

  /**
   * Tổng `min-width` để lưới CUỘN NGANG thay vì bóp cột. Cộng từ chính bảng bề rộng ở trên nên nó
   * không thể lệch khi một cột đổi số — đây là lý do không viết một hằng số tổng.
   */
  protected readonly minWidth = computed<string>(() => {
    const base =
      COLUMN_WIDTHS.code +
      COLUMN_WIDTHS.name +
      COLUMN_WIDTHS.group +
      COLUMN_WIDTHS.maxScore +
      COLUMN_WIDTHS.selfScore +
      COLUMN_WIDTHS.verifiedScore +
      COLUMN_WIDTHS.diff +
      COLUMN_WIDTHS.status +
      COLUMN_WIDTHS.owner +
      COLUMN_WIDTHS.deadline +
      COLUMN_WIDTHS.progress +
      COLUMN_WIDTHS.note;
    const period = this.showPeriodColumn() ? COLUMN_WIDTHS.period : 0;
    const actions = this.canWrite() ? COLUMN_WIDTHS.actions : 0;
    return `${base + period + actions}px`;
  });

  /** Mép trái luôn ghim (`Mã`); mép phải chỉ ghim khi cột `Hành động` còn tồn tại. */
  protected readonly frozenColumns = computed(() => ({ left: true, right: this.canWrite() }));

  /** `{groupCode}. {groupName}` — Q42. Ghép qua khoá dịch, không nối chuỗi trong template. */
  protected groupParams(row: ICriteriaRow): { Code: string; Name: string } {
    return { Code: row.GroupCode, Name: row.GroupName };
  }

  protected periodText(row: ICriteriaRow): string | null {
    const key = row.AssessmentPeriod;
    if (!key) return null;
    return this.periodRangeLabels()[key] ?? row.AssessmentPeriodLabel;
  }

  protected badgeClass(row: ICriteriaRow): string | null {
    return criteriaStatusBadge(row.Status);
  }

  /**
   * Số theo locale đang chọn. Chữ số thập phân là **hình dạng đã duyệt**, không phải tuỳ chọn:
   * điểm và tiến độ hiện 2 chữ số như bản dựng (`7,04` · `70,40`).
   */
  protected num(value: number | null): string | null {
    if (value === null || !Number.isFinite(value)) return null;
    return new Intl.NumberFormat(this.localeId(), {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  }

  /** `dd/MM/yyyy` cho `Hạn xử lý`. Ngày thuần từ BE (`YYYY-MM-DD`) — đọc theo lịch địa phương. */
  protected deadlineText(row: ICriteriaRow): string | null {
    if (!row.Deadline) return null;
    const [year, month, day] = row.Deadline.split('-').map(Number);
    if (!year || !month || !day) return null;
    return new Intl.DateTimeFormat(this.localeId(), {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    }).format(new Date(year, month - 1, day));
  }

  protected onGridPageChange(event: IDataGridPageChange): void {
    this.pageChange.emit({ Page: event.page, PageSize: event.pageSize });
  }

  /**
   * 🛑 Kiểm `isEditable()` ở đây nữa, không chỉ ở chỗ MỞ editor.
   *
   * Ca thật: người dùng mở ô sửa, rồi đổi ô lọc sang một **tháng** — lưới nạp lại, `isEditable` về
   * `false`, nhưng `editing` vẫn giữ id cũ. Nếu chỉ kiểm lúc mở thì ô sửa **mở lại** trên một bảng
   * đã chỉ đọc, và thứ gõ vào đó sẽ đi tới một server chắc chắn từ chối
   * (`400 …ASSESSMENT_PERIOD_NOT_WEEKLY`).
   */
  protected isEditingCell(row: ICriteriaRow, field: InlineField): boolean {
    if (!this.isEditable()) return false;
    const editing = this.editing();
    return editing !== null && editing.CriteriaId === row.CriteriaId && editing.Field === field;
  }

  /**
   * Mở editor. Bấm đúp (chuột) và `Enter`/`Space` (bàn phím) cùng vào đây — a11y ràng buộc 2 của
   * `doc/huong_dan/wiki-core/fe/15-accessibility.md` §4: mọi thao tác làm được bằng chuột phải
   * làm được bằng bàn phím.
   */
  protected openEditor(row: ICriteriaRow, field: InlineField): void {
    if (!this.isEditable()) return;
    this.cancelling = false;
    this.draft.set(field === 'progress' ? this.progressDraftOf(row) : (row.Note ?? ''));
    this.editing.set({ CriteriaId: row.CriteriaId, Field: field });
  }

  /** `Enter`/`Space` mở ô sửa — chặn `Space` cuộn trang, và chặn `Enter` submit form bao ngoài. */
  protected onCellKeydown(event: KeyboardEvent, row: ICriteriaRow, field: InlineField): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    event.preventDefault();
    this.openEditor(row, field);
  }

  protected onDraftInput(event: Event): void {
    this.draft.set((event.target as HTMLInputElement | HTMLTextAreaElement).value);
  }

  protected onEditorKeydown(event: KeyboardEvent, row: ICriteriaRow, field: InlineField): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.cancelling = true;
      this.editing.set(null);
      return;
    }
    // `Enter` lưu. Ở `Minh chứng/Ghi chú` (textarea) thì `Shift+Enter` vẫn xuống dòng — ô này là
    // MỘT ô text nhiều dòng (Q5), nên cướp hẳn `Enter` sẽ chặn đúng thứ nó dùng để làm.
    if (event.key === 'Enter' && !(field === 'note' && event.shiftKey)) {
      event.preventDefault();
      this.commit(row, field);
    }
  }

  /**
   * Rời ô ⇒ **LƯU**, không huỷ.
   *
   * Quyết định của FE, không có trong spec — và chọn chiều này vì `ui-spec` §5.4 đã nêu nguyên
   * tắc: *"mất chữ vừa gõ là cách nhanh nhất khiến người ta ngừng tin màn hình"*. Huỷ khi rời ô
   * biến mọi cú bấm ra ngoài thành một lần mất dữ liệu im lặng. Đường huỷ **tường minh** vẫn có,
   * và chỉ có một: phím `Escape`.
   */
  protected onEditorBlur(row: ICriteriaRow, field: InlineField): void {
    if (this.cancelling) {
      this.cancelling = false;
      return;
    }
    this.commit(row, field);
  }

  /**
   * Phát **đúng một** trường — cái vừa sửa.
   *
   * 🔄 LẬT 2026-09-11 (Q74). Trước đây phát cả hai, lấy trường còn lại từ dòng trong bộ nhớ, vì
   * DM-6 khi đó là `PUT` ghi đè trọn gói và bỏ trống một trường sẽ **null-hoá** nó. Sau khi BE
   * dựng `Assigned<T>`, bỏ trống nghĩa là **giữ nguyên** — nên gửi kèm giá trị cũ không còn là
   * bảo vệ, nó thành ghi đè mù: người khác sửa ghi chú trong lúc ô `Tiến độ %` đang mở, và lần
   * lưu này đẩy ghi chú về giá trị đọc được lúc mở ô.
   */
  private commit(row: ICriteriaRow, field: InlineField): void {
    // 🛑 Không còn editor nào mở ⇒ KHÔNG lưu lần thứ hai.
    //
    // Ca thật đã bắt được bằng test: `Enter` lưu và đóng editor, Angular gỡ `<input>` khỏi DOM,
    // việc gỡ đó phát `blur`, và `onEditorBlur` sẽ gọi `commit` **lần nữa** cho cùng một ô. Lần
    // hai gửi đúng payload đó tới server một lần nữa — vô hại về dữ liệu, nhưng là một request
    // thừa mỗi lần sửa, và nó ăn mất `version` vừa đổi nên lần lưu kế tiếp có thể nhận `409`.
    if (this.editing() === null) return;

    this.editing.set(null);
    const raw = this.draft();

    if (field === 'note') {
      // Ô rỗng ⇒ `null` (xoá trắng có chủ đích), KHÔNG phải `undefined` (không đụng tới).
      const note = raw.trim() === '' ? null : raw;
      if (note === row.Note) return;
      this.inlineSave.emit({ CriteriaId: row.CriteriaId, Note: note });
      return;
    }

    const progress = this.clampProgress(raw);
    if (progress === row.ProgressPercent) return;
    this.inlineSave.emit({ CriteriaId: row.CriteriaId, ProgressPercent: progress });
  }

  /**
   * Kẹp `[0, 100]` **trước khi gửi** (DM-6).
   *
   * Ô trống ⇒ `null` (xoá số), không phải `0`: hai thứ đó khác nhau trên Dashboard — `0%` là "đã
   * đo, chưa tiến triển", còn vắng mặt là "chưa ai nhập". Chuỗi không phải số cũng về `null` chứ
   * không về `0`, cùng lý do.
   */
  private clampProgress(raw: string): number | null {
    const text = raw.trim();
    if (text === '') return null;
    const value = Number(text);
    if (!Number.isFinite(value)) return null;
    return Math.min(PROGRESS_MAX, Math.max(PROGRESS_MIN, value));
  }

  /** Giá trị khởi tạo của ô `Tiến độ %` — số THÔ, không định dạng locale: đây là ô NHẬP. */
  private progressDraftOf(row: ICriteriaRow): string {
    return row.ProgressPercent === null ? '' : `${row.ProgressPercent}`;
  }
}
