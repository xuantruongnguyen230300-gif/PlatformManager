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
import { TranslatePipe } from '@ngx-translate/core';

/** Đúng ba đuôi của Q6. Dùng cho `accept` — **không** dùng để quyết định file hợp lệ hay không. */
const ACCEPTED_EXTENSIONS = '.csv,.xlsx,.xls';

/**
 * V11 — hộp thoại `Import CSV/Excel` (DM-7 bước 1). DUMB: nhận `input()`, phát `output()`, không
 * gọi service, không giữ `jobId`, không biết vòng poll tồn tại.
 *
 * ## `accept` KHÔNG phải một phép kiểm
 *
 * 🛑 Server nhận diện định dạng bằng **magic byte**, không bằng đuôi tên file (luật §2a của
 * `doc/huong_dan/wiki-core/be/15-import-export.md`). Nên một file `.xlsx` đổi tên từ thứ khác vẫn
 * bị từ chối bằng `400 IMPORT.FORMAT_UNSUPPORTED` — và câu người dùng đọc nói theo hướng *"nội
 * dung file không phải CSV/Excel"*, không phải *"sai đuôi file"*. `accept` ở đây chỉ lọc hộp thoại
 * chọn file của hệ điều hành cho đỡ phiền; thêm một phép kiểm đuôi ở FE là dựng một lớp luật thứ
 * hai **không khớp** lớp thật, và nó sẽ chặn nhầm hoặc cho lọt nhầm.
 *
 * ## Dialog KHÔNG tự đóng khi bấm Nhập
 *
 * Import là thao tác chạy dài: bấm `Nhập dữ liệu` chỉ **bắt đầu** một job. Trong suốt
 * `Pending`/`Running` dialog **ở lại**, nút submit `disabled`, và tiến trình được thông báo qua
 * vùng `aria-live` — người dùng trình đọc màn hình không có cách nào khác để biết máy đang chạy
 * (a11y §4 mục 4). Đóng dialog ngay khi gửi xong sẽ báo "đã nhập" cho một việc chưa xảy ra.
 *
 * ## Kỳ đích hiện ngay trong dialog, và đó là chuỗi ĐÃ DỊCH do trang cấp
 *
 * Người dùng chọn kỳ ở **ô lọc** trước khi mở hộp thoại này; hộp thoại chỉ **nói lại** kỳ đó để
 * không ai nạp 62 dòng vào nhầm tuần (DM-7 — *"việc cho người dùng thấy kỳ đích trước khi bấm Nhập
 * thuộc ui-spec"*). Nhận câu đã dựng sẵn thay vì tự ghép: component dumb không được tra bảng dịch
 * cho một câu có tham số động, và nhãn kỳ thì do **BE** dựng (Q40/Q72).
 */
@Component({
  selector: 'app-import-dialog',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './import-dialog.html',
  styleUrl: './import-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportDialog {
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input.required<boolean>();

  /**
   * Job đang chạy (`Pending`/`Running`) **hoặc** request bước 1 đang bay — một cờ cho cả hai, vì
   * với người dùng đó là cùng một trạng thái: "đã bấm, đang chờ". Tách làm hai cờ chỉ để hiển thị
   * hai câu giống nhau.
   */
  readonly busy = input<boolean>(false);

  /** Câu lỗi **cấp request** (bước 1) — đã dịch. Lỗi từng dòng thuộc V12, không hiện ở đây. */
  readonly errorMessage = input<string | null>(null);

  /** Câu "đang nhập cho kỳ X", đã dịch — `''` thì không hiện dòng nào. */
  readonly targetPeriodText = input<string>('');

  readonly submitted = output<File>();
  readonly closed = output<void>();

  protected readonly accept = ACCEPTED_EXTENSIONS;
  protected readonly chosenFile = signal<File | null>(null);
  protected readonly chosenFileName = computed(() => this.chosenFile()?.name ?? '');

  /**
   * Nút `Nhập dữ liệu` tắt khi **chưa chọn file** hoặc **đang chạy**.
   *
   * Chưa chọn file mà vẫn gửi được thì server trả `400 IMPORT.FILE_MISSING` — một vòng đi-về chỉ
   * để nói lại điều màn hình đã biết. Câu của mã đó vẫn phải có trong bảng dịch: nó tới được từ
   * đường khác (người dùng chọn rồi bỏ chọn file ở một trình duyệt cũ, hoặc gọi lại request).
   */
  protected readonly canSubmit = computed(() => !this.busy() && this.chosenFile() !== null);

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');
  private readonly fileInputEl = viewChild.required<ElementRef<HTMLInputElement>>('fileEl');

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      const el = this.dialogEl().nativeElement;
      if (this.open() && !el.open) {
        // Mở lại là một lượt nạp MỚI: xoá file của lượt trước. Giữ lại sẽ cho người dùng bấm
        // `Nhập dữ liệu` ngay và nạp đúng file họ vừa nạp xong — thao tác khó hoàn tác nhất của
        // màn này, thực hiện bằng một cú bấm không chủ ý.
        this.chosenFile.set(null);
        this.fileInputEl().nativeElement.value = '';
        el.showModal();
      }
      if (!this.open() && el.open) el.close();
    });
  }

  protected onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.chosenFile.set(input.files?.[0] ?? null);
  }

  protected onSubmit(): void {
    // Chốt chặn thứ hai sau `[disabled]`: nút tắt vẫn có thể bị kích bằng Enter ở một số đường,
    // và `[disabled]` là giao diện chứ không phải bất biến của luồng.
    const file = this.chosenFile();
    if (!file || this.busy()) return;
    this.submitted.emit(file);
  }

  /**
   * Đóng bằng × / Huỷ / Escape.
   *
   * **Không** chặn đóng khi job đang chạy: job sống ở server, đóng hộp thoại không huỷ nó. Trang
   * cha quyết định vòng poll đi tiếp hay dừng — xem `criteria-import-flow.ts`.
   */
  protected onNativeClose(): void {
    this.closed.emit();
  }
}
