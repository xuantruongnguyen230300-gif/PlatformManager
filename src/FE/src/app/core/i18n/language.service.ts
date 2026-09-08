import { DOCUMENT, isPlatformBrowser, registerLocaleData } from '@angular/common';
import { Injectable, PLATFORM_ID, Signal, computed, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { PrimeNG } from 'primeng/config';
import { firstValueFrom } from 'rxjs';
import { CORE_I18N, ICoreLanguage } from './core-i18n';

/**
 * Khoá `localStorage` giữ ngôn ngữ người dùng đã chọn.
 *
 * 🛑 Tên khoá TRUNG TÍNH (`core.*`) có chủ đích, cùng luật với `core.sidebar.collapsed.v1` ở
 * `shared/services/sidebar-state.service.ts`: tầng này là CoreBase dùng lại cho sản phẩm khác
 * (doc/kien-truc-core-module.md), nên tên sản phẩm KHÔNG được nằm trong khoá.
 *
 * Vì sao phải nhớ chứ không chỉ đặt lúc bấm: nghiệm thu số 3 của
 * doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md §3.1 là "F5 sau khi đã chọn tiếng Anh →
 * vẫn `en`". Không nhớ thì mỗi lần tải lại người dùng bị đưa về ngôn ngữ mặc định, và lựa chọn
 * "của mỗi người" trở thành lựa chọn của một phiên.
 *
 * Hậu tố `.v1`: đổi Ý NGHĨA của giá trị (vd sang `vi-VN` đầy đủ) thì tăng lên `.v2` thay vì đọc
 * nhầm dữ liệu cũ theo luật mới.
 */
export const LANGUAGE_STORAGE_KEY = 'core.language.v1';

/**
 * Nơi DUY NHẤT trong app biết "đang ở ngôn ngữ nào" và là nơi DUY NHẤT đổi nó.
 *
 * Ba việc mà chốt runtime (doc/huong_dan/wiki-core/fe/08-i18n.md, lật 2026-09-03) tự nhận về tay
 * mình — bản build-time trước đây được CLI làm hộ:
 *
 *  1. `registerLocaleData()` cho mọi ngôn ngữ khai ở `CORE_I18N` (§Cạm bẫy 2);
 *  2. `document.documentElement.lang` (§Cạm bẫy 5, WCAG 3.1.1 mức A);
 *  3. nhãn component PrimeNG (§Cạm bẫy 1).
 *
 * Cả ba đều KHÔNG lộ ra lúc biên dịch, và cả ba đều không làm hỏng thứ gì nhìn thấy được khi
 * quên — đó là lý do chúng nằm chung một chỗ có test, thay vì rải ra nơi gọi.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly config = inject(CORE_I18N);
  private readonly translate = inject(TranslateService);
  private readonly primeng = inject(PrimeNG);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);

  /** Danh sách để dựng nút đổi ngôn ngữ. Chỉ đọc — thêm/bớt ngôn ngữ là việc của `app.config.ts`. */
  readonly languages: readonly ICoreLanguage[] = this.config.languages;

  /**
   * Mã ngôn ngữ đang dùng, dạng signal.
   *
   * Đọc thẳng `translate.currentLang` (ngx-translate v18 phơi nó ra là `Signal`) thay vì nuôi một
   * signal thứ hai rồi đồng bộ hai chiều — hai nguồn cho một sự thật là hai nguồn sẽ lệch nhau.
   */
  readonly current: Signal<string> = computed(
    () => this.translate.currentLang() ?? this.config.defaultCode,
  );

  /**
   * Mã locale cho `DatePipe` / `DecimalPipe` / `CurrencyPipe`, dạng signal.
   *
   * 🛑 PHẢI truyền làm THAM SỐ CUỐI của pipe, KHÔNG đăng ký qua `LOCALE_ID`:
   *
   *     {{ createdAt | date: 'dd/MM/yyyy' : undefined : lang.localeId() }}
   *     {{ total | number: '1.0-2' : lang.localeId() }}
   *
   * `LOCALE_ID` là `InjectionToken<string>` cấp bằng `useValue`, và ba pipe trên CHỤP giá trị đó
   * trong constructor — đổi nó lúc chạy không làm gì cả, mà cũng không báo gì. Truyền qua tham số
   * thì pipe (vốn `pure`) tự tính lại khi tham số đổi; và dưới zoneless, đọc signal trong template
   * CHÍNH LÀ trigger change detection. Xem doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 2.
   */
  readonly localeId: Signal<string> = computed(() => this.resolve(this.current()).localeId);

  /**
   * Chạy đúng 1 lần lúc khởi động (`provideCoreI18n` → `provideAppInitializer`).
   *
   * `registerLocaleData` gọi cho TẤT CẢ ngôn ngữ ngay từ đầu, không chờ tới lúc người dùng bấm
   * đổi: đăng ký muộn nghĩa là lần vẽ đầu tiên sau khi đổi có thể chạy trước lúc dữ liệu locale
   * về, và `NG0701` khi đó nổ ở giữa một thao tác của người dùng thay vì lúc khởi động.
   */
  init(): Promise<void> {
    for (const language of this.config.languages) {
      if (language.localeData !== undefined) registerLocaleData(language.localeData);
    }
    this.translate.addLangs(this.config.languages.map((language) => language.code));
    return this.use(this.readStoredCode() ?? this.config.defaultCode);
  }

  /**
   * Đổi ngôn ngữ TẠI CHỖ — không tải lại trang, không đổi URL, không mất state đang có trên màn
   * hình (doc/huong_dan/wiki-core/fe/08-i18n.md §Cách đổi ngôn ngữ).
   *
   * Thứ tự CÓ CHỦ ĐÍCH: ba việc đồng bộ (thẻ `lang`, nhãn PrimeNG, ghi nhớ lựa chọn) làm TRƯỚC,
   * rồi mới `await` việc nạp bảng dịch qua mạng. Nạp hỏng hoặc chậm thì thẻ `lang` vẫn đúng —
   * ngược lại, đặt nó sau `await` nghĩa là một lần mạng lỗi để lại trang mang `lang` của ngôn ngữ
   * người dùng vừa rời bỏ, và không có gì trên màn hình cho thấy điều đó.
   */
  async use(code: string): Promise<void> {
    const language = this.resolve(code);
    this.document.documentElement.lang = language.code;
    this.primeng.setTranslation(language.primeTranslation);
    this.writeStoredCode(language.code);
    await firstValueFrom(this.translate.use(language.code));
  }

  /**
   * Tra một mã về khai báo ngôn ngữ, có đường lùi về `defaultCode` — dùng cho giá trị KHÔNG tin
   * được (chuỗi cũ còn trong `localStorage` sau khi dự án bỏ bớt một ngôn ngữ).
   *
   * Nhưng `defaultCode` sai thì KHÔNG có đường lùi nào nữa: ném ngay, kèm tên token, vì đó là lỗi
   * khai báo ở `app.config.ts` chứ không phải dữ liệu bẩn — im lặng chọn đại phần tử đầu tiên sẽ
   * cho một app chạy được ở ngôn ngữ không ai định chọn.
   */
  private resolve(code: string): ICoreLanguage {
    const found = this.config.languages.find((language) => language.code === code);
    if (found) return found;

    const fallback = this.config.languages.find(
      (language) => language.code === this.config.defaultCode,
    );
    if (fallback) return fallback;

    throw new Error(
      `CORE_I18N.defaultCode = '${this.config.defaultCode}' không khớp ngôn ngữ nào trong ` +
        `CORE_I18N.languages. Sửa ở app.config.ts.`,
    );
  }

  /** `try/catch` vì `localStorage` ném khi bị chặn (chế độ riêng tư, cookie bị khoá theo site) —
   * một lựa chọn ngôn ngữ không nhớ được là phiền, không phải lý do để app không khởi động nổi. */
  private readStoredCode(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    try {
      return localStorage.getItem(LANGUAGE_STORAGE_KEY);
    } catch {
      return null;
    }
  }

  private writeStoredCode(code: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(LANGUAGE_STORAGE_KEY, code);
    } catch {
      /* xem readStoredCode */
    }
  }
}
