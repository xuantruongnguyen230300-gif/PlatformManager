import {
  EnvironmentProviders,
  InjectionToken,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateMultiHttpLoader } from '@ngx-translate/http-loader';
import { Translation } from 'primeng/api';
import { LanguageService } from './language.service';

/**
 * Một ngôn ngữ mà SẢN PHẨM này hỗ trợ. `core/` giữ CƠ CHẾ (nạp bảng dịch, đổi ngôn ngữ tại chỗ,
 * đặt `<html lang>`, nối nhãn PrimeNG); danh sách ngôn ngữ và mọi dữ liệu kèm theo là của DỰ ÁN.
 *
 * Vì sao phải tách: "Dự án nào cũng có 2 ngôn ngữ VN và EN, có dự án sẽ có thêm"
 * (doc/huong_dan/wiki-core/fe/08-i18n.md §Phạm vi) — số ngôn ngữ là THAM SỐ của dự án, không phải
 * hằng số của Core. Khai cứng `vi`/`en` trong `core/` nghĩa là sản phẩm thứ ba (thêm `ja` chẳng
 * hạn) phải mở nền tảng ra sửa, đúng thứ mà định nghĩa "CoreBase xong" loại trừ.
 *
 * Cùng khuôn với `CORE_ROUTES` / `CORE_BRANDING` (hai file bên cạnh) — KHÔNG phát minh khuôn thứ
 * tư, theo doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn CoreBase ý 3.
 */
export interface ICoreLanguage {
  /**
   * Mã ngôn ngữ ngắn (BCP-47). Ba nơi tiêu thụ, và chúng PHẢI là cùng một chuỗi:
   *  · tên file bảng dịch — `<resource><code>.json` (vd `/i18n/vi.json`);
   *  · giá trị `document.documentElement.lang` (WCAG 3.1.1 Language of Page, mức A — bắt buộc
   *    theo doc/huong_dan/wiki-core/fe/15-accessibility.md §1);
   *  · khoá ghi nhớ lựa chọn trong `localStorage`.
   */
  readonly code: string;

  /** Nhãn hiện trên nút đổi ngôn ngữ. Viết bằng CHÍNH ngôn ngữ đó ("Tiếng Việt", "English") — người
   * đang lạc trong giao diện họ không đọc được vẫn phải nhận ra dòng của mình. */
  readonly label: string;

  /**
   * Mã locale của Angular dùng cho `DatePipe`/`DecimalPipe`/`CurrencyPipe` — KHÁC `code` được
   * (`en` ↔ `en-US`). Truyền làm THAM SỐ CUỐI của pipe, xem `LanguageService.localeId`.
   */
  readonly localeId: string;

  /**
   * Dữ liệu locale của Angular (`import localeVi from '@angular/common/locales/vi'`), hoặc bỏ
   * trống với locale Angular đã gói sẵn (`en-US`).
   *
   * 🛑 Thiếu dòng này cho một locale KHÔNG gói sẵn thì Angular ném `NG0701 MISSING_LOCALE_DATA`
   * **lúc chạy** — không phải lỗi biên dịch, và nhánh `en-US` chạy hoàn hảo sẽ che mất nó cho tới
   * khi có người bấm sang ngôn ngữ kia (doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 2).
   */
  readonly localeData?: unknown;

  /**
   * Bảng nhãn PrimeNG của ngôn ngữ này — lấy từ gói `primelocale` (`primelocale/js/<mã>.js`),
   * KHÔNG gõ tay 90+ khoá. PrimeNG không ship file locale nào, mặc định cứng tiếng Anh.
   *
   * ⚠️ HẠN CHẾ ĐÃ BIẾT, là bẫy cho NGƯỜI THÊM COMPONENT SAU chứ không cho người bọc chuỗi:
   * `PrimeNG.translation` là **object thường**, không phải `WritableSignal` như `ripple`/`theme`/
   * `csp`/`pt`. Cập nhật đi qua một RxJS observer, và chỉ **7** bundle subscribe + `markForCheck`.
   * **15** bundle đọc `config.translation` mà KHÔNG subscribe: autocomplete, carousel,
   * cascadeselect, galleria, image, megamenu, menubar, message, multiselect, orderlist, paginator,
   * picklist, rating, toast, treetable. Dưới zoneless, `setTranslation()` lúc chạy **sẽ không**
   * làm 15 component đó vẽ lại.
   *
   * Hôm nay thiệt hại nhỏ vì FE chỉ import `primeng/config` + `primeng/table` (+ `primeng/api` cho
   * KIỂU dữ liệu ở đúng dòng dưới đây, không kéo component nào), và `table` CÓ subscribe. Kiểm lại
   * bằng lệnh trước khi thêm component mới:
   *
   *     grep -rho "from 'primeng/[a-z]*'" src/FE/src | sort -u
   *
   * Nổ ra ngay khi thêm `p-select` / `p-multiselect` / `p-toast`. Khi phải thêm: hoặc chấp nhận
   * nhãn PrimeNG không đổi cho tới lần điều hướng tiếp theo, hoặc ép vẽ lại thủ công, hoặc truyền
   * nhãn qua `pt`. Xem doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 1.
   */
  readonly primeTranslation: Translation;
}

/** Cấu hình i18n của một sản phẩm dựng trên nền tảng này. */
export interface ICoreI18n {
  /** Mọi ngôn ngữ chọn được. Phần tử đầu KHÔNG mặc nhiên là mặc định — khai rõ ở `defaultCode`. */
  readonly languages: readonly ICoreLanguage[];

  /**
   * Ngôn ngữ dùng khi người dùng chưa từng chọn, VÀ là ngôn ngữ dự phòng khi một khoá thiếu bản
   * dịch ở ngôn ngữ đang chọn. PHẢI là `code` của một phần tử trong `languages` — sai thì
   * `LanguageService` ném ngay lúc khởi động, xem `LanguageService.resolve`.
   */
  readonly defaultCode: string;

  /**
   * Danh sách TIỀN TỐ đường dẫn của các nguồn bảng dịch, nạp và **ghép** theo đúng thứ tự này
   * (nguồn sau ghi đè khoá trùng của nguồn trước). Mỗi nguồn cho ra `<tiền tố><mã>.json`.
   *
   * Đây là chỗ ranh giới Core ↔ dự án được tách bằng FILE chứ không bằng tiền tố khoá
   * (doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch §5): sản phẩm thứ hai giữ nguyên
   * nguồn Core rồi thêm nguồn của mình vào sau, không phải sửa file dịch của nền tảng.
   *
   * Sản phẩm này khai HAI nguồn kể từ 2026-09-09 — nhóm CoreBase rồi nhóm dự án, xem
   * `app.config.ts` (`APP_I18N.resources`). Danh sách và thứ tự là DỮ LIỆU của dự án: `core/`
   * không được biết tên nhóm nào, chỉ biết "nạp theo thứ tự, nguồn sau ghi đè".
   *
   * 🛑 Hệ quả cho TEST, chỗ dễ hỏng im lặng nhất: một spec nạp thiếu nguồn thì khoá của nhóm
   * thiếu tra không trúng và ngx-translate trả lại chính chuỗi khoá. `useTranslationsInTest()`
   * (`./i18n.testing.ts`) đọc `resources` từ chính token này, nên TestBed của màn nghiệp vụ phải
   * cấp `CORE_I18N` — không cấp thì helper lùi về nhóm CoreBase một mình.
   */
  readonly resources: readonly string[];
}

/**
 * CỐ Ý không có `factory` mặc định, cùng lý do với `CORE_ROUTES` và `CORE_BRANDING`: một mặc định
 * (vd "chỉ có tiếng Việt") sẽ biến "app quên khai" thành một giao diện chạy được nhưng KHÔNG đổi
 * được ngôn ngữ — sai im lặng, không gate nào bắt. Thiếu provider thì Angular ném `NG0201` ngay
 * lần dựng đầu tiên: ồn ào, đúng chỗ, sửa một dòng là xong.
 */
export const CORE_I18N = new InjectionToken<ICoreI18n>('CORE_I18N');

/**
 * Wire ở `app.config.ts`. Gói trọn bốn việc để app không phải nhớ thứ tự của chúng:
 *
 *  1. bơm `CORE_I18N` cho `LanguageService`;
 *  2. dựng `TranslateService` với ngôn ngữ dự phòng = `defaultCode`;
 *  3. nối loader HTTP nhiều nguồn (`resources`), suffix `.json`;
 *  4. chạy `LanguageService.init()` lúc khởi động — đăng ký locale data, khôi phục lựa chọn đã
 *     nhớ, đặt `<html lang>`, nạp xong bảng dịch RỒI mới cho app vẽ.
 *
 * Vì sao (4) phải là app initializer chứ không phải "gọi ở đâu đó trong `App`": `provideAppInitializer`
 * là chỗ DUY NHẤT Angular chờ Promise trước khi dựng component đầu tiên. Nạp muộn hơn thì màn hình
 * đầu tiên vẽ ra bằng chính chuỗi KHOÁ (`login.action.submit`) rồi mới nháy sang câu thật.
 *
 * 🛑 `useHttpBackend: true` là BẮT BUỘC ở repo này, không phải tuỳ chọn hiệu năng. Bảng dịch là
 * tài nguyên TĨNH của chính site (`/i18n/vi.json`), còn `apiBaseUrlInterceptor` viết lại MỌI URL
 * tương đối thành `environment.apiBaseUrl + url` và chỉ miễn trừ tiền tố `/assets` — repo này dùng
 * `public/` (ra thẳng gốc site) chứ không dùng `/assets`, nên không có tiền tố nào cứu. Đi qua
 * chuỗi interceptor thì request bay sang máy chủ API thành `…/api/i18n/vi.json` và 404. Đã quan
 * sát THẬT trong `ng test` trước khi thêm dòng này, không phải phòng xa.
 *
 * `HttpBackend` cũng đúng về ngữ nghĩa cho cả hai interceptor còn lại: bảng dịch không cần cookie
 * phiên (`withCredentialsInterceptor`), và một toast đỏ "Đã có lỗi xảy ra" bắn ra TRƯỚC khi app
 * kịp vẽ (`httpErrorInterceptor`) thì vô nghĩa — chưa có gì trên màn hình để hiện toast lên.
 *
 * 🛑 `failOnError: true` cũng là quyết định: mặc định của http-loader là nuốt lỗi 404 và trả về
 * object rỗng kèm một dòng `console.warn`. Nuốt lỗi ở đây nghĩa là một lần deploy quên copy
 * `public/i18n/` sẽ ra một giao diện hiện TOÀN khoá — chạy được, không lỗi, không ai biết. Cái giá
 * đã chấp nhận: initializer reject ⇒ app KHÔNG khởi động. Cùng họ với `NG0201` của ba seam
 * `CORE_*` — hỏng ồn ào, đúng chỗ, sửa một dòng là xong; và phép kiểm "file có tồn tại không" đã
 * nằm trong `ng test` (`app-i18n.spec.ts`) nên nó không lọt tới lúc deploy một cách yên lặng.
 */
export function provideCoreI18n(config: ICoreI18n): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: CORE_I18N, useValue: config },
    provideTranslateService({
      fallbackLang: config.defaultCode,
      loader: provideTranslateMultiHttpLoader({
        resources: config.resources.map((prefix) => ({ prefix, suffix: '.json' })),
        useHttpBackend: true,
        failOnError: true,
      }),
    }),
    provideAppInitializer(() => inject(LanguageService).init()),
  ]);
}
