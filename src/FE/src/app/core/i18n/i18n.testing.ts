import { TestBed } from '@angular/core/testing';
import { TranslateService, TranslationObject } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { CORE_I18N } from './core-i18n';

/**
 * Tiện ích DÙNG TRONG TEST — nạp bảng dịch THẬT vào `TestBed`.
 *
 * ## Vì sao nạp file thật chứ không stub một object nhỏ
 *
 * Một stub kiểu `{ login: { action: { submit: 'Sign in' } } }` làm test xanh kể cả khi khoá đó
 * **không có** trong bảng dịch được phục vụ — tức là kiểm một bản sao, không kiểm thứ trình duyệt
 * nhận được. Nạp file thật biến mọi spec đang dùng nó thành một phép kiểm phụ: gõ sai một khoá
 * trong template thì chuỗi render ra chính là khoá, và assert lên câu tiếng Việt sẽ ĐỎ.
 *
 * `public/` được `angular.json` khai là assets cho **cả** target `build` lẫn `test`, nên đường dẫn
 * bảng dịch ở đây chính là đường dẫn lúc deploy.
 *
 * ## Nguồn bảng dịch lấy từ đâu — đọc trước khi viết spec cho màn NGHIỆP VỤ
 *
 * Bảng dịch **không** còn là một thư mục: `ICoreI18n.resources` là một DANH SÁCH tiền tố, nạp và
 * ghép theo thứ tự (nguồn sau ghi đè khoá trùng của nguồn trước). Repo này có hai nguồn — nhóm
 * CoreBase và nhóm dự án — xem `core-i18n.ts` (`ICoreI18n.resources`) và `app.config.ts`
 * (`APP_I18N.resources`).
 *
 * Helper này KHÔNG được biết tên nhóm khoá nào của dự án (nó thuộc `core/`, đi theo CoreBase sang
 * sản phẩm thứ hai). Nó phân giải danh sách nguồn theo đúng ba nấc dưới đây, dừng ở nấc đầu tiên
 * có giá trị:
 *
 *  1. `options.resources` — spec truyền thẳng;
 *  2. `CORE_I18N` nếu TestBed có cấp — tức **đúng danh sách nguồn mà app thật dùng**;
 *  3. `CORE_ONLY_RESOURCES` — chỉ nhóm CoreBase.
 *
 * 🛑 **Spec của màn nghiệp vụ PHẢI đi nấc 1 hoặc nấc 2.** Rơi xuống nấc 3 thì khoá của dự án tra
 * không trúng, ngx-translate trả về chính chuỗi khoá, và spec đỏ với thông điệp dạng
 * `Expected 'danh-muc-dti.title' to be 'Danh mục DTI'`. Sửa bằng đúng MỘT dòng trong `providers`:
 * `{ provide: CORE_I18N, useValue: APP_I18N }`.
 *
 * 🛑 **Đừng viết bản thứ hai của hàm này trong spec của module.** Hai helper cùng vai thì người
 * không biết sẽ dùng cái cũ và test lại mù — đúng thứ `.claude/CLAUDE.md` §5 cấm. Bản chỉ nạp một
 * nguồn đã tồn tại thật ở hai spec module (`dashboard`, `danh-muc-dti`) và đã được gỡ 2026-09-09.
 *
 * ## Vì sao file này KHÔNG có đuôi `.spec.ts`
 *
 * Nó không chứa `describe`/`it` nào — Karma nạp nó qua `import` của các spec, không nạp như một
 * suite. Nó cũng không bao giờ vào bundle sản phẩm: `main.ts` không có đường nào tới đây.
 */

/**
 * Nguồn bảng dịch mà **CoreBase tự sở hữu** — nấc 3 của bảng phân giải trên.
 *
 * Đây là hằng số duy nhất trong `core/` được phép nêu một đường dẫn bảng dịch, và nó nêu đường dẫn
 * của CHÍNH Core (`public/i18n/`) chứ không phải của dự án. Một sản phẩm khác dựng trên nền tảng
 * này bê nguyên thư mục đó đi, nên giá trị vẫn đúng; nhóm khoá riêng của sản phẩm thì đi qua
 * `CORE_I18N`, không đi qua đây.
 */
export const CORE_ONLY_RESOURCES: readonly string[] = ['/i18n/'];

/** Khoá cache là URL đầy đủ, không phải mã ngôn ngữ: hai nguồn khác nhau có cùng mã `vi`. */
const bundleCache = new Map<string, Promise<TranslationObject>>();

/**
 * Nạp (và nhớ) MỘT file bảng dịch thật. Cache ở tầng module để N spec không `fetch` lại N lần.
 *
 * `prefix` là một phần tử của `ICoreI18n.resources` (`'/i18n/'`, `'/i18n-app/'`, …) — cố ý KHÔNG
 * có giá trị mặc định: một mặc định ở đây là đúng cái khai cứng vừa gỡ đi.
 */
export function loadTranslationBundle(prefix: string, code: string): Promise<TranslationObject> {
  const url = `${prefix}${code}.json`;
  const cached = bundleCache.get(url);
  if (cached) return cached;

  const pending = fetch(url).then(async (response) => {
    if (!response.ok) {
      throw new Error(
        `Không đọc được ${url} — bảng dịch phải nằm ở src/FE/public${prefix} và mọi tiền tố khai ` +
          `trong ICoreI18n.resources phải có đủ file cho MỌI ngôn ngữ. Đây cũng là lỗi mà người ` +
          `dùng sẽ gặp lúc chạy nếu bản deploy quên copy thư mục đó.`,
      );
    }
    return (await response.json()) as TranslationObject;
  });

  bundleCache.set(url, pending);
  return pending;
}

/**
 * Nạp bảng dịch thật vào `TranslateService` của `TestBed` hiện tại rồi chọn ngôn ngữ `active`.
 *
 * Gọi SAU `TestBed.configureTestingModule({ providers: [..., provideTranslateService()] })` và
 * TRƯỚC `TestBed.createComponent(...)` — component dựng trước khi bảng dịch về sẽ render ra chính
 * chuỗi khoá, đúng cách hỏng mà `provideAppInitializer` ở `provideCoreI18n` sinh ra để chặn.
 *
 * @param active ngôn ngữ chọn sau khi nạp xong.
 * @param options `codes` — các ngôn ngữ cần nạp; `resources` — danh sách tiền tố nguồn, bỏ trống
 *   thì lấy từ `CORE_I18N` của TestBed, không có nữa thì lùi về `CORE_ONLY_RESOURCES`.
 */
export async function useTranslationsInTest(
  active = 'vi',
  options: { readonly codes?: readonly string[]; readonly resources?: readonly string[] } = {},
): Promise<TranslateService> {
  const translate = TestBed.inject(TranslateService);
  const codes = options.codes ?? ['vi', 'en'];
  const resources =
    options.resources ?? TestBed.inject(CORE_I18N, null)?.resources ?? CORE_ONLY_RESOURCES;

  for (const code of codes) {
    for (const prefix of resources) {
      // `shouldMerge = true`: nguồn sau GHÉP vào nguồn trước thay vì thay thế — đúng hành vi
      // `provideTranslateMultiHttpLoader` của app thật, và là chỗ quy tắc "nguồn sau ghi đè khoá
      // trùng" được tái hiện trong test.
      translate.setTranslation(code, await loadTranslationBundle(prefix, code), true);
    }
  }
  translate.addLangs([...codes]);
  await firstValueFrom(translate.use(active));

  return translate;
}
