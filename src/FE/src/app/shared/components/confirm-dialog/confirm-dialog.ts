import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  PLATFORM_ID,
  computed,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/** Bốn mức độ nghiêm trọng, đúng 4 biến thể `.dialog-icon` có trong styles.scss §6. */
export type ConfirmSeverity = 'ask' | 'ok' | 'warn' | 'bad';

/**
 * Icon mặc định theo mức độ — lấy từ khối "14 · Hộp thoại hỏi" của prototype. Màu đi kèm nằm ở
 * `.dialog-icon.<severity>`, nên chỉ cần chọn hình; đổi hình ở đây là đổi cho toàn hệ thống.
 */
const SEVERITY_ICON: Record<ConfirmSeverity, string> = {
  ask: 'pi-question-circle',
  ok: 'pi-check-circle',
  warn: 'pi-exclamation-triangle',
  bad: 'pi-trash',
};

/** Đếm để sinh id duy nhất cho `aria-labelledby` — hai dialog cùng trang không được trùng id. */
let nextInstanceId = 0;

/**
 * Hộp thoại hỏi / xác nhận, dựng trên `<dialog>` gốc (đã chốt giữ native, không đổi sang
 * `p-dialog` — xem doc/huong_dan/wiki-core/fe/05-component-library.md §Phạm vi áp dụng).
 * Markup khớp `dialog.confirm-dialog` + `.dialog-head` ở `src/FE/src/styles.scss` §6 và §8.
 *
 * Vì sao dùng `<dialog>` gốc mà không tự dựng overlay: `showModal()` cho SẴN bẫy focus, lớp
 * backdrop, đóng bằng Escape và trả focus về phần tử đã mở dialog. Tự viết lại đủ 4 thứ đó là
 * việc lớn và dễ sót — nhưng đổi lại phải giữ đúng markup: bọc `<dialog>` bằng overlay tự chế sẽ
 * làm mất chính những hành vi này mà không có lỗi biên dịch nào báo.
 *
 * ĐÚNG 2 NÚT — Đồng ý / Huỷ (người dùng đã chốt bỏ phương án 3 nút). Nhãn đổi được qua input, và
 * nút xác nhận mang `.danger` khi hành động là xoá.
 *
 * Cách dùng ở trang cha: lấy tham chiếu bằng `viewChild(ConfirmDialog)` rồi gọi `open()`; đóng do
 * người dùng thì component tự lo, trang cha chỉ nghe `(confirmed)` / `(cancelled)`.
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialog {
  private readonly platformId = inject(PLATFORM_ID);

  readonly severity = input<ConfirmSeverity>('ask');
  readonly title = input.required<string>();
  readonly description = input<string>('');
  /*
   * Hai nhãn nút mặc định là chuỗi RỖNG, không phải câu tiếng Việt — cùng quyết định và cùng lý do
   * với `shared/components/toolbar/toolbar.ts`: trang cha truyền vào câu ĐÃ DỊCH
   * (`[confirmLabel]="'…' | translate"`), nên input này không được vừa mang câu vừa mang khoá.
   * Rỗng ⇒ template lùi về `shared.action.confirm` / `shared.action.cancel` bằng `| translate` ngay
   * tại chỗ hiển thị, nên nhãn mặc định đổi theo ngôn ngữ mà component dumb này không inject gì.
   */
  readonly confirmLabel = input<string>('');
  readonly cancelLabel = input<string>('');
  /** Hành động xoá / không hoàn tác được → nút xác nhận chuyển sang `.btn.danger` thay vì `.primary`. */
  readonly confirmDanger = input<boolean>(false);
  /** Khoá nút xác nhận trong lúc trang cha còn đang xử lý — chặn bấm hai lần. */
  readonly confirmDisabled = input<boolean>(false);
  /** Ghi đè icon khi mức độ mặc định không tả đúng việc (vd `bad` nhưng không phải xoá). */
  readonly icon = input<string>('');

  /**
   * Tên ở thể đã-rồi (`confirmed`/`cancelled`) theo đúng lối đặt tên output đang dùng trong repo
   * (`saved`/`closed` ở `user-form-dialog`) — và bắt buộc phải khác `cancel`, vì đó là tên một sự
   * kiện DOM có thật của chính `<dialog>`; trùng tên là mở đường cho nhầm lẫn giữa sự kiện của
   * component và sự kiện gốc của trình duyệt (lint `@angular-eslint/no-output-native` chặn thẳng).
   */
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');
  private readonly cancelButtonEl = viewChild.required<ElementRef<HTMLButtonElement>>('cancelEl');

  protected readonly titleId = `confirm-dialog-title-${nextInstanceId++}`;
  protected readonly descriptionId = `${this.titleId}-desc`;
  protected readonly iconClass = computed(() => this.icon() || SEVERITY_ICON[this.severity()]);

  /**
   * Lối thoát mà người dùng đã chọn, đọc lại trong `(close)`. Cần biến này vì `<dialog>` chỉ phát
   * MỘT sự kiện `close` chung cho mọi đường đóng — bấm Đồng ý, bấm Huỷ, bấm ×, hay gõ Escape đều
   * về cùng chỗ. Không ghi lại ý định trước khi đóng thì không phân biệt được "đồng ý" với "thoát".
   * `null` = trình duyệt tự đóng (Escape) → tính là huỷ, đúng kỳ vọng của người dùng.
   */
  private outcome: 'confirm' | 'cancel' | 'silent' | null = null;

  /** Mở dialog ở chế độ modal. Gọi khi đang mở là vô hại (native `showModal()` sẽ ném lỗi). */
  open(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const el = this.dialogEl().nativeElement;
    if (el.open) return;
    this.outcome = null;
    el.showModal();
    // `showModal()` tự focus phần tử focus được ĐẦU TIÊN — ở đây là nút ×. Với một câu hỏi có thể
    // phá dữ liệu, phím Enter theo phản xạ phải rơi vào lối thoát an toàn, nên kéo focus về nút
    // Huỷ. Làm bằng code thay vì thuộc tính `autofocus` (quy tắc a11y của lint chặn `autofocus`,
    // và ở đây ta chỉ cần nó đúng lúc dialog mở chứ không phải lúc trang tải).
    this.cancelButtonEl().nativeElement.focus();
  }

  /**
   * Đóng bằng CODE của trang cha — cố ý KHÔNG phát `cancelled`. Trang cha gọi hàm này là đã biết
   * mình đang đóng; phát thêm `cancelled` sẽ tạo vòng lặp với chính handler `(cancelled)` của nó.
   */
  close(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const el = this.dialogEl().nativeElement;
    if (!el.open) return;
    this.outcome = 'silent';
    el.close();
  }

  protected onConfirm(): void {
    this.outcome = 'confirm';
    this.dialogEl().nativeElement.close();
  }

  protected onCancel(): void {
    this.outcome = 'cancel';
    this.dialogEl().nativeElement.close();
  }

  protected onNativeClose(): void {
    const outcome = this.outcome;
    this.outcome = null;
    if (outcome === 'silent') return;
    if (outcome === 'confirm') {
      this.confirmed.emit();
      return;
    }
    this.cancelled.emit();
  }
}
