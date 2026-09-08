import { Injectable, Signal, computed, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { CORE_BRANDING } from '../config/core-branding';

/** Dấu ngăn giữa tiêu đề trang và tên ứng dụng — `<title>` của tab, KHÔNG dùng cho topbar. */
const TITLE_SEPARATOR = ' · ';

/**
 * `TitleStrategy` tuỳ biến — MỘT nguồn tiêu đề, HAI nơi tiêu thụ.
 *
 * ## Vì sao tồn tại (đừng gỡ về `data: { title }`)
 *
 * Trước thay đổi này, tiêu đề trang khai ở `data: { title: '...' }` và chỉ có `App` đọc để đổ vào
 * topbar. Hệ quả: `<title>` của tab **không đổi theo trang** — luôn đứng nguyên chuỗi tĩnh viết
 * trong `src/index.html`, ở mọi route.
 *
 * Đó là **WCAG 2.4.2 Page Titled, mức A** đang trượt. Mức chuẩn đã CHỐT cho hệ thống này là
 * **WCAG 2.2 AA bắt buộc** (`doc/huong_dan/wiki-core/fe/15-accessibility.md` §1 — hệ thống phục
 * vụ khu vực công, a11y là ràng buộc nghiệm thu chứ không phải lựa chọn chất lượng), mà AA bao
 * gồm toàn bộ tiêu chí mức A. Ba nhóm người dùng chịu thiệt trực tiếp:
 *
 * - Người dùng screen reader: tiêu đề tab là thứ ĐẦU TIÊN được đọc khi chuyển trang/chuyển tab —
 *   nếu mọi trang đọc lên cùng một câu thì không cách nào biết mình đang ở đâu.
 * - Người mở nhiều tab: mọi tab hiện cùng một nhãn, không phân biệt được.
 * - Lịch sử duyệt web và bookmark: mọi mục lưu lại cùng một tên.
 *
 * Không lỗi nào trong ba lỗi đó làm build/lint/test đỏ — nên nó đã trôi được lâu. Nay có test
 * canh: `page-title.strategy.spec.ts` (hành vi) + `app.routes.spec.ts` (mọi route phải khai
 * `title` ở cấp `Route`, không phải trong `data`).
 *
 * ## Vì sao KHÔNG dùng `DefaultTitleStrategy` của Angular
 *
 * `DefaultTitleStrategy` set đúng `<title>` nhưng không để lại chỗ nào cho topbar lấy tiêu đề, mà
 * topbar là thứ người dùng NHÌN THẤY. Chuyển sang `title` cấp `Route` mà chỉ dùng default là
 * topbar mất tiêu đề: `title:` được router resolve vào `snapshot.data` dưới một **symbol key**
 * nội bộ, KHÔNG phải chuỗi `'title'` — nên `data['title']` cũ thành `undefined` và topbar rơi về
 * chuỗi dự phòng ở mọi trang. Lớp này giữ cả hai đầu ra từ một nguồn duy nhất.
 *
 * Đặt ở `core/` vì đây là hạ tầng singleton toàn app, không thuộc feature nào
 * (`doc/huong_dan/quy-uoc/fe-architecture.md` §Tầng app).
 *
 * Đăng ký ở `app.config.ts` bằng `{ provide: TitleStrategy, useExisting: PageTitleStrategy }` —
 * `useExisting` (không phải `useClass`) để router và `App` dùng **chung một** thể hiện; `useClass`
 * sẽ tạo thể hiện thứ hai và topbar bám vào cái không bao giờ được router gọi.
 */
@Injectable({ providedIn: 'root' })
export class PageTitleStrategy extends TitleStrategy {
  private readonly documentTitle = inject(Title);

  /**
   * Tên sản phẩm đến từ `CORE_BRANDING`, KHÔNG khai cứng ở đây — `core/` chỉ biết CÁCH ghép hậu
   * tố, chuỗi là của app (chốt 2026-09-02, xem `doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ,
   * dự án cung cấp DỮ LIỆU").
   *
   * Ghi chú thứ tự khai đã HẾT HIỆU LỰC (2026-09-06): bản trước bắt field này phải đứng trước
   * `current = signal(this.branding.name)`, vì khởi tạo field chạy theo thứ tự viết. `current` nay
   * là `pageTitle`, một `computed` — thân hàm chỉ chạy lúc có người ĐỌC, tức sau khi mọi field đã
   * khởi tạo xong, nên không còn ràng buộc thứ tự nào ở đây.
   */
  private readonly branding = inject(CORE_BRANDING);

  /**
   * 🛑 Inject `TranslateService` chứ KHÔNG `LanguageService`, dù `LanguageService.current` mới là
   * "nơi duy nhất biết đang ở ngôn ngữ nào".
   *
   * `LanguageService` đòi token `CORE_I18N` — dữ liệu do `app.config.ts` bơm vào. Lớp này thì được
   * mọi spec dùng `provideRouter` kéo theo, kể cả những spec không quan tâm gì tới i18n, nên buộc
   * chúng khai thêm một token của app là biến một lỗi cấu hình xa lạ (`NG0201`) thành cái giá của
   * việc dịch tiêu đề. `currentLang` của ngx-translate v18 vốn đã là `Signal`, đủ để làm phụ thuộc
   * — xem `translateKey()` bên dưới.
   */
  private readonly translate = inject(TranslateService);

  /**
   * KHOÁ DỊCH của route đang mở (`login.routeTitle`…), `null` khi route không khai `title`.
   *
   * Giữ KHOÁ chứ không giữ câu đã dịch — cùng luật với khối lỗi ở `platform/login/pages/login`:
   * lưu sẵn câu nghĩa là đổi ngôn ngữ xong, tiêu đề tab và tiêu đề topbar là hai chỗ duy nhất trên
   * màn hình còn nguyên ngôn ngữ cũ, và không có gì báo vì cả hai vẫn là chữ đọc được.
   */
  private readonly routeTitleKey = signal<string | null>(null);

  /**
   * Tiêu đề trang cho topbar của app shell (`app.html`) — chỉ phần riêng của trang, KHÔNG kèm hậu
   * tố tên ứng dụng: topbar đã nằm trong ứng dụng rồi, lặp lại tên là thừa. Hậu tố chỉ có ý nghĩa
   * ở `<title>` của tab, nơi tiêu đề bị tách khỏi ngữ cảnh.
   */
  readonly pageTitle: Signal<string> = computed(() => {
    const key = this.routeTitleKey();
    return key ? this.translateKey(key) : this.branding.name;
  });

  constructor() {
    super();
    // Đổi NGÔN NGỮ không sinh ra lần điều hướng nào, nên `updateTitle()` không chạy lại — mà
    // `<title>` của tab là thứ ghi vào DOM ngoài Angular, không có template nào tự vẽ lại hộ.
    // `pageTitle` (topbar) tự đúng nhờ là `computed`; riêng thẻ `<title>` phải được đẩy lại ở đây.
    effect(() => this.applyDocumentTitle());
  }

  /**
   * Router gọi hàm này sau mỗi lần điều hướng thành công (trước khi phát `NavigationEnd`).
   * `buildTitle()` của lớp cha đi từ gốc xuống lá theo outlet chính và lấy `title` của route sâu
   * nhất có khai — nên route con lazy-load (`path: ''` trong `<feature>.routes.ts`) là nơi khai
   * tiêu đề, đúng quy ước `doc/huong_dan/quy-uoc/fe-routing-guard.md` §2.
   *
   * Ghi `<title>` NGAY tại đây (không phó thác cho `effect` ở constructor) vì `effect` chạy lệch
   * một nhịp: nó được lên lịch chứ không chạy đồng bộ, nên tab sẽ mang tiêu đề của trang TRƯỚC cho
   * tới nhịp change detection kế tiếp. `effect` chỉ phụ trách nhánh đổi ngôn ngữ.
   */
  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.routeTitleKey.set(this.buildTitle(snapshot) ?? null);
    this.applyDocumentTitle();
  }

  private applyDocumentTitle(): void {
    const appName = this.branding.name;
    const key = this.routeTitleKey();
    const routeTitle = key ? this.translateKey(key) : null;
    this.documentTitle.setTitle(routeTitle ? `${routeTitle}${TITLE_SEPARATOR}${appName}` : appName);
  }

  /**
   * Tra một khoá ra câu, có ĐỌC signal ngôn ngữ để phụ thuộc là tường minh tại chỗ đọc.
   *
   * ⚠️ Dòng `this.translate.currentLang()` KHÔNG phải thứ làm cho reactivity chạy được, dù nhìn
   * rất giống. ĐO 2026-09-06 bằng canary (gỡ nó ra rồi chạy `ng test` → vẫn xanh): `instant()` của
   * @ngx-translate/core v18 tự đọc cả `currentLang` lẫn kho bản dịch bên trong, nên phụ thuộc đã
   * được đăng ký sẵn. Giữ lại vì đó là một chi tiết BÊN TRONG thư viện, không phải hợp đồng công
   * khai — xem doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 2.
   *
   * Thứ THẬT SỰ làm cho tiêu đề đổi theo ngôn ngữ là việc lớp này giữ KHOÁ chứ không giữ câu; canary
   * cho nhánh đó (lưu sẵn câu đã dịch trong `updateTitle`) làm ĐỎ đúng một test —
   * `page-title.strategy.spec.ts`, ca "đổi ngôn ngữ thì CẢ <title> lẫn tiêu đề topbar đổi theo".
   */
  private translateKey(key: string): string {
    this.translate.currentLang();
    return this.translate.instant(key) as string;
  }
}
