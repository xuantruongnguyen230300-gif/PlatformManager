import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route, Routes } from '@angular/router';
import { TranslateService, TranslationObject, provideTranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { APP_I18N } from './app.config';
import { routes } from './app.routes';

/**
 * Bảng dịch của DỰ ÁN NÀY (`src/FE/public/i18n/*.json`) đối chiếu với `APP_I18N` khai ở
 * `app.config.ts`. Đặt cạnh `app.config.spec.ts` chứ không trong `core/i18n/` vì thứ nó kiểm là
 * DỮ LIỆU của sản phẩm, không phải cơ chế của nền tảng — `core/` là tầng đáy, không được biết dự
 * án có bao nhiêu ngôn ngữ.
 *
 * Ba thứ được canh ở đây, và cả ba đều KHÔNG lộ ra lúc biên dịch:
 *
 *  1. **Khoá VIẾT HOA phải LỒNG hai cấp.** ngx-translate coi dấu CHẤM là ký tự PHÂN CẤP, nên
 *     `AUTH.INVALID_CREDENTIALS` tra theo đường `AUTH` → `INVALID_CREDENTIALS`. Viết phẳng thành
 *     một khoá chứa dấu chấm thì tra KHÔNG TRÚNG — và thư viện KHÔNG BÁO LỖI, nó trả về chính
 *     chuỗi khoá, nên màn hình hiện `AUTH.INVALID_CREDENTIALS` giữa giao diện. Build xanh, mọi
 *     test khác xanh. Xem doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch §1.
 *  2. **Parity `vi` ↔ `en`.** Một khoá chỉ có ở một file nghĩa là người dùng ngôn ngữ kia đọc
 *     câu của ngôn ngữ khác (qua đường fallback) giữa giao diện của mình.
 *  3. **Tham số `{{...}}` khớp nhau giữa hai file.** Sót một cặp ngoặc ở bản `en` thì câu đó mất
 *     giá trị BE gửi sang, và nó chỉ lộ ra khi có người gặp đúng lỗi đó ở đúng ngôn ngữ đó.
 *
 * Mỗi phép kiểm đi kèm một **ca đối chứng** chứng minh nó ĐỎ được — một gate chỉ chạy trên dữ
 * liệu đã đúng thì không phân biệt được "đã kiểm" với "không kiểm gì cả".
 */

/**
 * Đọc thẳng file thật mà trình duyệt sẽ tải lúc chạy — `public/` được `angular.json` khai là
 * assets cho cả target `build` lẫn `test`, nên đường dẫn ở đây CHÍNH LÀ đường dẫn khi deploy.
 * Đọc qua `import` một object TS sẽ kiểm một bản sao, không kiểm thứ được phục vụ.
 */
async function loadBundle(code: string): Promise<TranslationObject> {
  const url = `/i18n/${code}.json`;
  const response = await fetch(url);
  expect(response.ok)
    .withContext(`không đọc được ${url} — file bảng dịch phải nằm ở src/FE/public/i18n/`)
    .toBeTrue();
  return (await response.json()) as TranslationObject;
}

/** Trải phẳng cây khoá thành `a.b.c`. Chỉ đi vào object thuần — mảng và chuỗi là LÁ. */
function flatten(node: unknown, prefix = ''): string[] {
  if (node === null || typeof node !== 'object' || Array.isArray(node)) return [prefix];
  return Object.entries(node as Record<string, unknown>).flatMap(([key, value]) =>
    flatten(value, prefix ? `${prefix}.${key}` : key),
  );
}

/**
 * Tên khoá chứa dấu chấm = khoá viết PHẲNG, tức bẫy số 1. `flatten` không phân biệt được
 * `{"A":{"B":1}}` với `{"A.B":1}` sau khi đã trải, nên phải soi chính TÊN của từng cấp.
 */
function dottedKeyNames(node: unknown, path: readonly string[] = []): string[] {
  if (node === null || typeof node !== 'object' || Array.isArray(node)) return [];
  return Object.entries(node as Record<string, unknown>).flatMap(([key, value]) => {
    const here = key.includes('.') ? [[...path, key].join(' → ')] : [];
    return here.concat(dottedKeyNames(value, [...path, key]));
  });
}

/** Tập tên tham số `{{Ten}}` xuất hiện trong một câu. */
function paramsOf(text: string): string[] {
  return [...text.matchAll(/\{\{\s*([A-Za-z0-9_]+)\s*\}\}/g)].map((match) => match[1]).sort();
}

/** Mọi cặp `[khoá phẳng, câu]` trong một bảng dịch. */
function leafStrings(node: unknown, prefix = ''): [string, string][] {
  if (typeof node === 'string') return [[prefix, node]];
  if (node === null || typeof node !== 'object' || Array.isArray(node)) return [];
  return Object.entries(node as Record<string, unknown>).flatMap(([key, value]) =>
    leafStrings(value, prefix ? `${prefix}.${key}` : key),
  );
}

describe('public/i18n — bảng dịch của dự án', () => {
  const bundles = new Map<string, TranslationObject>();

  beforeAll(async () => {
    for (const language of APP_I18N.languages) {
      bundles.set(language.code, await loadBundle(language.code));
    }
  });

  it('🛑 mọi ngôn ngữ khai ở APP_I18N đều có file bảng dịch', () => {
    // Danh sách lấy từ `APP_I18N` chứ không viết cứng ở đây, nên đây là một ca đỏ THẬT: thêm `ja`
    // vào `APP_I18N.languages` mà quên `public/i18n/ja.json` → `loadBundle` đỏ ngay ở `beforeAll`.
    expect(bundles.size).toBe(APP_I18N.languages.length);
  });

  it('🛑 không khoá nào viết PHẲNG với dấu chấm trong tên (bẫy ngx-translate)', () => {
    for (const [code, bundle] of bundles) {
      expect(dottedKeyNames(bundle))
        .withContext(
          `${code}.json — dấu chấm là ký tự PHÂN CẤP của ngx-translate. Khoá có dấu chấm trong ` +
            `TÊN sẽ không bao giờ tra trúng, và thư viện trả về chính chuỗi khoá thay vì báo lỗi.`,
        )
        .toEqual([]);
    }
  });

  it('ca đối chứng — phép kiểm trên PHÁT HIỆN được khoá viết phẳng', () => {
    // Không có `it` này thì `it` phía trên vẫn xanh kể cả khi `dottedKeyNames` bị viết hỏng thành
    // `() => []`. Một gate không đỏ được là một gate không kiểm gì cả.
    expect(dottedKeyNames({ AUTH: { 'INVALID.CREDENTIALS': 'x' } })).toEqual([
      'AUTH → INVALID.CREDENTIALS',
    ]);
    expect(dottedKeyNames({ 'AUTH.INVALID_CREDENTIALS': 'x' })).toEqual([
      'AUTH.INVALID_CREDENTIALS',
    ]);
  });

  it('🛑 mọi ngôn ngữ có ĐÚNG cùng một tập khoá', () => {
    const [reference, ...others] = [...bundles.entries()];
    const referenceKeys = flatten(reference[1]).sort();

    for (const [code, bundle] of others) {
      const keys = flatten(bundle).sort();
      expect(keys.filter((key) => !referenceKeys.includes(key)))
        .withContext(`${code}.json có khoá mà ${reference[0]}.json không có`)
        .toEqual([]);
      expect(referenceKeys.filter((key) => !keys.includes(key)))
        .withContext(
          `${code}.json THIẾU khoá so với ${reference[0]}.json — người dùng ngôn ngữ này sẽ đọc ` +
            `câu của ngôn ngữ khác`,
        )
        .toEqual([]);
    }
  });

  it('ca đối chứng — phép so tập khoá PHÁT HIỆN được khoá thiếu', () => {
    expect(flatten({ a: { b: 'x', c: 'y' } })).toEqual(['a.b', 'a.c']);
    expect(flatten({ a: { b: 'x' } })).not.toEqual(['a.b', 'a.c']);
  });

  it('🛑 mỗi câu có cùng tập tham số {{...}} ở mọi ngôn ngữ', () => {
    const [reference, ...others] = [...bundles.entries()];
    const referenceParams = new Map(
      leafStrings(reference[1]).map(([key, text]) => [key, paramsOf(text)]),
    );

    for (const [code, bundle] of others) {
      for (const [key, text] of leafStrings(bundle)) {
        expect(paramsOf(text))
          .withContext(
            `${code}.json → ${key}: tham số khác với ${reference[0]}.json — câu này sẽ mất giá ` +
              `trị BE gửi kèm`,
          )
          .toEqual(referenceParams.get(key) ?? []);
      }
    }
  });

  it('ca đối chứng — phép kiểm tham số PHÁT HIỆN được ngoặc gõ sót', () => {
    expect(paramsOf("Tên đăng nhập '{{UserName}}' đã tồn tại.")).toEqual(['UserName']);
    expect(paramsOf("The user name '{UserName}' already exists.")).toEqual([]);
  });

  /**
   * `title` của mỗi route nay là KHOÁ DỊCH, không phải câu (`login.routeTitle`…). Khoá gõ sai hoặc
   * khoá quên thêm vào bảng dịch KHÔNG làm hỏng gì có thể thấy được: `PageTitleStrategy` tra trượt,
   * ngx-translate trả lại chính chuỗi khoá, và tab hiện `login.routeTitle` cạnh tên sản phẩm —
   * build xanh, lint xanh, `app.routes.spec.ts` cũng xanh vì nó chỉ so khoá với khoá.
   *
   * Đây là phép kiểm nối HAI nguồn đó lại: bảng route THẬT (nạp `loadChildren`) đối chiếu bảng dịch
   * THẬT. Nó cũng phủ luôn route thứ 7 thêm vào ngày mai.
   */
  it('🛑 mọi khoá `title` của route đều có mặt trong CẢ HAI bảng dịch', async () => {
    const titleKeys = await routeTitleKeys();

    expect(titleKeys.length).toBeGreaterThan(0);
    for (const [code, bundle] of bundles) {
      const keys = flatten(bundle);
      for (const key of titleKeys) {
        expect(keys)
          .withContext(
            `${code}.json thiếu "${key}" — tab và topbar sẽ hiện chính chuỗi khoá thay vì tiêu đề`,
          )
          .toContain(key);
      }
    }
  });

  it('ca đối chứng — danh sách khoá title đọc từ bảng route thật, không viết tay', async () => {
    // Không có `it` này thì phép kiểm trên vẫn xanh nếu `routeTitleKeys()` trả mảng rỗng vì một lý
    // do nào đó (đổi hình dạng route, `loadChildren` ném lỗi bị nuốt) — xanh vì không kiểm gì cả.
    const titleKeys = await routeTitleKeys();

    expect(titleKeys).toContain('login.routeTitle');
    expect(titleKeys.every((key) => key.endsWith('.routeTitle'))).toBeTrue();
  });
});

/**
 * Mọi giá trị `title` khai ở route lá của app, lấy bằng cách nạp THẬT `loadChildren` trong
 * `app.routes.ts` — cùng cách `app.routes.spec.ts` làm, và cùng lý do: danh sách viết tay sẽ mục
 * ruỗng, còn bảng route thì không.
 *
 * Chỉ nhận `title` dạng CHUỖI. Angular cho phép `title` là `ResolveFn<string>`; nếu có ngày một
 * route dùng dạng đó thì nó không phải khoá dịch tĩnh và không kiểm được ở đây — bỏ qua đúng chỗ,
 * thay vì báo lỗi giả.
 */
async function routeTitleKeys(): Promise<string[]> {
  const keys: string[] = [];

  const walk = (config: Routes): void => {
    for (const route of config) {
      if (route.children?.length) {
        walk(route.children);
        continue;
      }
      const title: Route['title'] = route.title;
      if (typeof title === 'string') keys.push(title);
    }
  };

  for (const top of routes) {
    if (!top.loadChildren) continue;
    walk(await (top.loadChildren as () => Promise<Routes>)());
  }

  return keys;
}

describe('public/i18n — tra khoá qua chính TranslateService (bẫy dấu chấm, đường thật)', () => {
  let translate: TranslateService;

  beforeEach(() => {
    // Không cấp loader: `TranslateNoOpLoader` mặc định + `setTranslation()` thủ công cho phép nạp
    // đúng nội dung muốn kiểm — tất định, không phụ thuộc thứ tự khởi động của app.
    // App chạy zoneless (`app.config.ts`) — `TestBed` phải khai theo, nếu không Angular ném
    // `NG0908 In this configuration Angular requires Zone.js`.
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    translate = TestBed.inject(TranslateService);
  });

  it('🛑 AUTH.INVALID_CREDENTIALS tra ra CÂU, không phải chuỗi khoá', async () => {
    translate.setTranslation('vi', await loadBundle('vi'));
    await firstValueFrom(translate.use('vi'));

    const text = translate.instant('AUTH.INVALID_CREDENTIALS');
    expect(text)
      .withContext(
        'tra không trúng thì ngx-translate trả về chính chuỗi khoá — đó là dạng hỏng hiện ' +
          '"AUTH.INVALID_CREDENTIALS" giữa giao diện mà không lỗi, không cảnh báo',
      )
      .not.toBe('AUTH.INVALID_CREDENTIALS');
    expect(text).toBe('Tên đăng nhập hoặc mật khẩu không đúng.');
  });

  it('bảng dịch TOÀN khoá phẳng vẫn tra được — hình dạng THẬT của bẫy, đo 2026-09-05', async () => {
    // ⚠️ Đây là chỗ hành vi thật KHÁC mô tả phổ biến của bẫy (kể cả mô tả trong
    // doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch §1, khối ⚠️). `getValue` của
    // @ngx-translate/core v18 gom dần từng đoạn khoá và thử LUÔN cả chuỗi có dấu chấm khi đoạn
    // lồng không khớp, nên một bảng dịch viết phẳng HOÀN TOÀN vẫn tra trúng.
    //
    // Ghi `it` này lại chứ không xoá: người sau đọc doc rồi thử tay sẽ thấy nó "chạy được" và
    // kết luận cả phép kiểm phẳng/lồng là thừa. Bẫy THẬT nằm ở `it` ngay dưới.
    translate.setTranslation('vi', {
      'AUTH.INVALID_CREDENTIALS': 'Tên đăng nhập hoặc mật khẩu không đúng.',
    });
    await firstValueFrom(translate.use('vi'));

    expect(translate.instant('AUTH.INVALID_CREDENTIALS')).toBe(
      'Tên đăng nhập hoặc mật khẩu không đúng.',
    );
  });

  it('🛑 ca đối chứng — khoá phẳng BỐC HƠI ngay khi có nhánh lồng cùng tên gốc', async () => {
    // Đây mới là dạng hỏng sẽ xảy ra thật ở repo này, và nó tệ hơn dạng "phẳng thì không tra
    // được": bảng dịch hôm nay LỒNG (`AUTH` là một nhánh). Ai đó thêm một mã mới bằng cách gõ
    // một dòng phẳng `"AUTH.SOMETHING": "..."` ở cấp gốc — `getValue` đi vào nhánh `AUTH` trước,
    // không thấy `SOMETHING` bên trong, rồi DỪNG: nó không quay ra thử chuỗi phẳng nữa.
    //
    // Kết quả: khoá mới trả về chính chuỗi khoá giữa giao diện, trong khi mọi khoá cũ vẫn đúng.
    // Không lỗi biên dịch, không test nào khác đỏ. Phép kiểm "không tên khoá nào chứa dấu chấm"
    // ở describe phía trên chặn đúng ca này.
    translate.setTranslation('vi', {
      AUTH: { LOCKED_OUT: 'Tài khoản đã bị khoá.' },
      'AUTH.INVALID_CREDENTIALS': 'Tên đăng nhập hoặc mật khẩu không đúng.',
    });
    await firstValueFrom(translate.use('vi'));

    expect(translate.instant('AUTH.LOCKED_OUT')).toBe('Tài khoản đã bị khoá.');
    expect(translate.instant('AUTH.INVALID_CREDENTIALS')).toBe('AUTH.INVALID_CREDENTIALS');
  });

  it('tham số {{UserName}} được thay bằng giá trị BE gửi kèm (messageParams)', async () => {
    translate.setTranslation('vi', await loadBundle('vi'));
    await firstValueFrom(translate.use('vi'));

    // `messageParams` của envelope là từ điển khoá-TÊN (doc/huong_dan/wiki-core/be/
    // 16-i18n-va-ma-loi.md §10.2) — khớp thẳng cú pháp `{{Ten}}` của ngx-translate, không phải
    // quy đổi gì. Đó chính là lý do BE chọn khoá tên thay vì `{0}`/`{1}`.
    expect(translate.instant('USER.DUPLICATE_USERNAME', { UserName: 'nguyen.van.a' })).toBe(
      "Tên đăng nhập 'nguyen.van.a' đã tồn tại.",
    );
  });

  it('🛑 ba câu "…thất bại" KHÔNG còn chỗ giữ {{Reasons}} — khớp BE sau quyết định 2026-09-05', async () => {
    // BE đã bỏ chỗ giữ ở cả ba khuôn thông điệp (`AuthErrors.ChangePasswordFailed`,
    // `UserErrors.CreateFailed`, `UserErrors.UpdateFailed`) — từng mã Identity nay đi ra
    // `fieldErrors` kèm đúng ô nhập nó nói tới, thay vì nối bằng dấu chấm phẩy vào giữa câu
    // (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2).
    //
    // Bảng dịch FE giữ `{{Reasons}}` sau khi BE bỏ tham số KHÔNG gây lỗi nào — ngx-translate thay
    // chỗ giữ không có giá trị bằng chuỗi rỗng, nên người dùng đọc "Đổi mật khẩu thất bại: " với
    // một dấu hai chấm cụt. Không lỗi biên dịch, không test nào khác đỏ. Đó là lý do có `it` này.
    translate.setTranslation('vi', await loadBundle('vi'));
    await firstValueFrom(translate.use('vi'));

    for (const key of ['AUTH.CHANGE_PASSWORD_FAILED', 'USER.CREATE_FAILED', 'USER.UPDATE_FAILED']) {
      expect(translate.instant(key) as string)
        .withContext(`${key} không được còn chỗ giữ nào — BE không gửi tham số cho mã này nữa`)
        .not.toContain('{{');
    }
  });
});
