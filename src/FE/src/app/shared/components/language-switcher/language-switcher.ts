import { ChangeDetectionStrategy, Component, ErrorHandler, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../../core/i18n/language.service';

/**
 * DUMB — nút đổi ngôn ngữ. Không giữ state, không gọi API: nó đọc danh sách ngôn ngữ và ngôn ngữ
 * đang chọn từ `LanguageService` (`core/i18n/`) rồi gọi đúng một hàm để đổi.
 *
 * ## Vì sao đổi ngay tại chỗ, không tải lại trang
 *
 * doc/huong_dan/wiki-core/fe/08-i18n.md §"Cách đổi ngôn ngữ" (chốt 2026-09-03): *"Bấm nó, giao
 * diện đổi ngay trong phiên: không tải lại trang, không đổi URL, không mất state đang có trên màn
 * hình"*. Nghiệm thu số 2 của mục §Nghiệm thu trong file đó kiểm đúng điều này — nên component
 * này **không được** `location.reload()`, không được `router.navigate`, và không được đụng URL.
 * Việc đổi ngôn ngữ do `LanguageService.use()` làm trọn: bảng dịch, `<html lang>`, nhãn PrimeNG,
 * và ghi nhớ lựa chọn.
 *
 * ## Vì sao nhãn ngôn ngữ KHÔNG đi qua bảng dịch
 *
 * "Tiếng Việt" và "English" đến từ `ICoreLanguage.label` (`app.config.ts`), viết bằng **chính**
 * ngôn ngữ đó. Dịch chúng là làm hỏng đúng công dụng của nút: người đang lạc trong một giao diện
 * họ không đọc được vẫn phải nhận ra dòng của mình. Đây cũng là lý do mỗi nút mang `lang` riêng —
 * WCAG 3.1.2 *Language of Parts* (mức AA, ràng buộc nghiệm thu theo
 * doc/huong_dan/wiki-core/fe/15-accessibility.md §1): một cụm chữ khác ngôn ngữ với trang phải
 * được đánh dấu, nếu không trình đọc màn hình đọc "English" bằng bộ âm tiếng Việt.
 *
 * ## Vì sao là nút chứ không phải `<select>`
 *
 * `p-select` của PrimeNG đọc `config.translation` mà **không** subscribe, nên dưới zoneless nó
 * không vẽ lại khi `setTranslation()` chạy (doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 1, danh
 * sách 15 bundle). Dùng đúng thứ vừa đổi ngôn ngữ làm nơi đổi ngôn ngữ là cách chắc chắn nhất gặp
 * bẫy đó. `<button>` thuần không có vấn đề này, và ở hai lựa chọn thì nó cũng ít thao tác hơn.
 *
 * ⚠️ Thành phần này **chưa có spec ở `doc/Design/`**. Nó chỉ dùng token đã có trong
 * `src/styles.scss` (`--muted`, `--brand`, `--sp-*`, `--fs-sm`, `--radius-pill`) và không phát
 * minh giá trị mới — nhưng bố cục/chỗ đặt thì vẫn cần `doc/Design/Frontend/PlatformManager/`
 * chốt lại, xem báo cáo bàn giao.
 */
@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './language-switcher.html',
  styleUrl: './language-switcher.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSwitcher {
  private readonly language = inject(LanguageService);
  private readonly errorHandler = inject(ErrorHandler);

  protected readonly languages = this.language.languages;
  protected readonly current = this.language.current;

  /**
   * `void` chứ không trả Promise: `LanguageService.use()` là async (nạp bảng dịch qua mạng), nhưng
   * template không có gì để chờ — bảng dịch về tới đâu thì signal đổi tới đó và mọi pipe tự tính
   * lại.
   *
   * 🛑 `.catch()` là BẮT BUỘC, không phải cẩn thận thừa. Loader khai `failOnError: true`
   * (`core/i18n/core-i18n.ts`) nên một lần `/i18n/en.json` hỏng sẽ làm promise này **reject**; bỏ
   * trống thì nó thành *unhandled rejection* — thứ Angular KHÔNG đưa tới `ErrorHandler` và cũng
   * không hiện gì trên màn hình, nên người dùng bấm nút và không có chuyện gì xảy ra, mãi mãi.
   * Chuyển tay sang `ErrorHandler` để nó đi đúng đường của mọi lỗi khác trong app
   * (`core/errors/global-error.handler.ts`).
   */
  protected select(code: string): void {
    if (code === this.current()) return;
    this.language.use(code).catch((error: unknown) => this.errorHandler.handleError(error));
  }
}
