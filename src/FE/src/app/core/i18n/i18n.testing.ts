import { TestBed } from '@angular/core/testing';
import { TranslateService, TranslationObject } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

/**
 * Tiện ích DÙNG TRONG TEST — nạp bảng dịch THẬT vào `TestBed`.
 *
 * ## Vì sao nạp file thật chứ không stub một object nhỏ
 *
 * Một stub kiểu `{ login: { action: { submit: 'Sign in' } } }` làm test xanh kể cả khi khoá đó
 * **không có** trong `public/i18n/*.json` — tức là kiểm một bản sao, không kiểm thứ trình duyệt
 * được phục vụ. Nạp file thật biến mọi spec đang dùng nó thành một phép kiểm phụ: gõ sai một khoá
 * trong template thì chuỗi render ra chính là khoá, và assert lên câu tiếng Việt sẽ ĐỎ.
 *
 * `public/` được `angular.json` khai là assets cho **cả** target `build` lẫn `test`, nên
 * `/i18n/vi.json` ở đây chính là đường dẫn lúc deploy.
 *
 * ## Vì sao file này KHÔNG có đuôi `.spec.ts`
 *
 * Nó không chứa `describe`/`it` nào — Karma nạp nó qua `import` của các spec, không nạp như một
 * suite. Nó cũng không bao giờ vào bundle sản phẩm: `main.ts` không có đường nào tới đây.
 */
const bundleCache = new Map<string, Promise<TranslationObject>>();

/** Nạp (và nhớ) một bảng dịch thật. Cache ở tầng module để N spec không `fetch` lại N lần. */
export function loadTranslationBundle(code: string): Promise<TranslationObject> {
  const cached = bundleCache.get(code);
  if (cached) return cached;

  const pending = fetch(`/i18n/${code}.json`).then(async (response) => {
    if (!response.ok) {
      throw new Error(
        `Không đọc được /i18n/${code}.json — bảng dịch phải nằm ở src/FE/public/i18n/. ` +
          `Đây cũng là lỗi mà người dùng sẽ gặp lúc chạy nếu bản deploy quên copy thư mục đó.`,
      );
    }
    return (await response.json()) as TranslationObject;
  });

  bundleCache.set(code, pending);
  return pending;
}

/**
 * Nạp bảng dịch thật vào `TranslateService` của `TestBed` hiện tại rồi chọn ngôn ngữ `active`.
 *
 * Gọi SAU `TestBed.configureTestingModule({ providers: [..., provideTranslateService()] })` và
 * TRƯỚC `TestBed.createComponent(...)` — component dựng trước khi bảng dịch về sẽ render ra chính
 * chuỗi khoá, đúng cách hỏng mà `provideAppInitializer` ở `provideCoreI18n` sinh ra để chặn.
 */
export async function useTranslationsInTest(
  active = 'vi',
  codes: readonly string[] = ['vi', 'en'],
): Promise<TranslateService> {
  const translate = TestBed.inject(TranslateService);

  for (const code of codes) {
    translate.setTranslation(code, await loadTranslationBundle(code));
  }
  translate.addLangs([...codes]);
  await firstValueFrom(translate.use(active));

  return translate;
}
