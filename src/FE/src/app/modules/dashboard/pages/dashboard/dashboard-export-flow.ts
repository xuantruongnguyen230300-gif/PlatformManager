import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { ApiErrorMessageService } from '../../../../core/i18n/api-error-message.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { IApiResult } from '../../../../core/http/api-result.model';
import { IDashboardParams, IExportedFile } from '../../models/dashboard.model';
import { DashboardService } from '../../services/dashboard.service';

/**
 * LUỒNG `Xuất báo cáo` (DB-4): gọi endpoint, kiểm rồi giao file cho trình duyệt lưu.
 *
 * ## Lỗi của endpoint này đi về dưới dạng `Blob`, không phải object — và đó là cái bẫy
 *
 * Request đặt `responseType: 'blob'`, nên khi server trả `400`/`403`/`500` thì `err.error` là một
 * **Blob chứa JSON**, không phải envelope đã parse. Hệ quả: `httpErrorInterceptor` đọc
 * `businessCode` ra `undefined` và bắn một toast **chung chung** ("Đã có lỗi xảy ra").
 *
 * Lớp này đọc lại Blob đó thành text, parse, và **chỉ** bắn thêm một toast khi tìm được một
 * `businessCode` **có bản dịch**. Nghĩa là:
 *
 * | Ca | Toast |
 * | --- | --- |
 * | lỗi hạ tầng (500, 0, …) — không `businessCode` | **một** toast chung của interceptor |
 * | `400 DASHBOARD.EXPORT_MODE_UNSUPPORTED` | toast chung **+** câu dẫn đường cụ thể |
 *
 * Ca thứ hai hai toast, và đó là đánh đổi có chủ đích: lựa chọn còn lại là đặt `SKIP_ERROR_TOAST`
 * để tắt toast của interceptor, nhưng cùng token đó **cũng tắt đường điều hướng về màn đăng nhập
 * khi 401** — mất lối thoát của một phiên đã chết để đổi lấy một dòng toast bớt đi là cuộc đổi
 * tồi. Ca thứ hai lẽ ra cũng không xảy ra: nút đã `disabled` ở `mode=year`.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — vòng đời trùng vòng đời trang.
 */
@Injectable()
export class DashboardExportFlow {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly service = inject(DashboardService);
  private readonly toast = inject(ToastService);
  private readonly errorMessages = inject(ApiErrorMessageService);

  /** Request đang bay ⇒ nút `disabled`, chặn bấm hai lần thành hai lần tải cùng một file. */
  readonly busy = signal(false);

  run(params: IDashboardParams): void {
    if (this.busy()) return;
    this.busy.set(true);

    this.service.exportReport(params).subscribe({
      next: (file) => {
        this.busy.set(false);
        this.save(file);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        void this.explain(err);
      },
    });
  }

  /**
   * Giao file cho trình duyệt.
   *
   * `URL.createObjectURL` + một `<a download>` tạm là cách duy nhất lưu một `Blob` đã nằm trong bộ
   * nhớ mà không gọi lại server — và gọi lại server cho một endpoint xuất báo cáo nghĩa là **tính
   * lại toàn bộ báo cáo** một lần nữa.
   *
   * 🛑 `revokeObjectURL` là BẮT BUỘC, không phải dọn dẹp cho gọn: mỗi object URL giữ nguyên Blob
   * trong bộ nhớ cho tới khi tab đóng. Một người xuất 20 báo cáo trong một phiên giữ lại cả 20
   * file, và triệu chứng ("trình duyệt chậm dần") không trỏ về đây.
   */
  private save(file: IExportedFile): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const url = URL.createObjectURL(file.Blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = file.FileName;
    // Không cần gắn vào DOM ở trình duyệt hiện đại, nhưng gắn rồi gỡ là đường chạy được ở mọi nơi
    // và không để lại gì.
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  /**
   * Đọc Blob lỗi → envelope → câu người dùng đọc. `async` vì `Blob.text()` là promise; nơi gọi
   * `void` nó đi, không có gì phải chờ.
   */
  private async explain(err: unknown): Promise<void> {
    if (!(err instanceof HttpErrorResponse) || !(err.error instanceof Blob)) return;

    try {
      const envelope = JSON.parse(await err.error.text()) as IApiResult<unknown>;
      const message = this.errorMessages.translateCode(envelope.businessCode, envelope.messageParams);
      // CHỈ toast khi có bản dịch thật: không có thì interceptor đã nói đủ, và một câu thứ hai
      // giống hệt câu thứ nhất chỉ làm người đọc tưởng có hai sự cố.
      if (message) this.toast.error(message);
    } catch {
      // Body không phải JSON (proxy trả HTML, response rỗng…) — interceptor đã bắn toast chung,
      // và ở đây không có gì thêm để nói.
    }
  }
}
