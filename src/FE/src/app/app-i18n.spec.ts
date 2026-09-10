import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route, Routes } from '@angular/router';
import { TranslateService, TranslationObject, provideTranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { APP_I18N } from './app.config';
import { routes } from './app.routes';
import { CORE_I18N } from './core/i18n/core-i18n';
import { CORE_ONLY_RESOURCES, useTranslationsInTest } from './core/i18n/i18n.testing';

/**
 * Bảng dịch của DỰ ÁN NÀY — MỌI nguồn khai ở `APP_I18N.resources` (`src/FE/public/i18n/*.json`
 * và `src/FE/public/i18n-app/*.json`) — đối chiếu với `APP_I18N` khai ở
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
 *
 * `prefix` là một phần tử của `APP_I18N.resources` — mỗi nguồn cho ra `<tiền tố><mã>.json`. Danh
 * sách nguồn KHÔNG viết cứng trong file này: nguồn thứ ba thêm vào ngày mai cũng bị kiểm mà không
 * ai phải nhớ cập nhật spec.
 */
async function loadBundle(prefix: string, code: string): Promise<TranslationObject> {
  const url = `${prefix}${code}.json`;
  const response = await fetch(url);
  expect(response.ok)
    .withContext(`không đọc được ${url} — file bảng dịch phải nằm ở src/FE/public${prefix}`)
    .toBeTrue();
  return (await response.json()) as TranslationObject;
}

/**
 * Ghép mọi nguồn theo ĐÚNG thứ tự `APP_I18N.resources` — cùng phép ghép mà
 * `provideTranslateMultiHttpLoader` làm lúc chạy (nguồn sau ghi đè khoá trùng của nguồn trước).
 * Dùng cho phép kiểm hỏi *"người dùng thật sự tra ra cái gì"*; phép kiểm parity thì soi TỪNG nguồn
 * một, vì một khoá thiếu ở nguồn dự án mà được nguồn Core che lấp vẫn là một khoá thiếu.
 */
async function loadMergedBundle(code: string): Promise<TranslationObject> {
  let merged: TranslationObject = {};
  for (const prefix of APP_I18N.resources) {
    merged = mergeDeep(merged, await loadBundle(prefix, code));
  }
  return merged;
}

function mergeDeep(base: TranslationObject, extra: TranslationObject): TranslationObject {
  const out: TranslationObject = { ...base };
  for (const [key, value] of Object.entries(extra)) {
    const existing = out[key];
    out[key] =
      isPlainObject(existing) && isPlainObject(value) ? mergeDeep(existing, value) : value;
  }
  return out;
}

function isPlainObject(value: unknown): value is TranslationObject {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
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

/**
 * Khoá nào ĐƯỢC PHÉP khai trùng ở hai nguồn. Rỗng là đúng hôm nay — mỗi tên thêm vào đây là một
 * quyết định có chữ ký, xem phép kiểm ngay dưới.
 */
const DELIBERATE_OVERRIDES: readonly string[] = [];

describe('public/i18n — bảng dịch của dự án', () => {
  /** `<tiền tố>` → (`<mã ngôn ngữ>` → bảng dịch của RIÊNG nguồn đó). */
  const sources = new Map<string, Map<string, TranslationObject>>();
  /** `<mã ngôn ngữ>` → bảng dịch ĐÃ GHÉP mọi nguồn, tức thứ người dùng thật sự tra vào. */
  const bundles = new Map<string, TranslationObject>();

  beforeAll(async () => {
    for (const prefix of APP_I18N.resources) {
      const perCode = new Map<string, TranslationObject>();
      for (const language of APP_I18N.languages) {
        perCode.set(language.code, await loadBundle(prefix, language.code));
      }
      sources.set(prefix, perCode);
    }
    for (const language of APP_I18N.languages) {
      bundles.set(language.code, await loadMergedBundle(language.code));
    }
  });

  it('🛑 MỌI nguồn trong APP_I18N.resources đều có đủ file cho MỌI ngôn ngữ', () => {
    // Cả hai danh sách lấy từ `APP_I18N` chứ không viết cứng ở đây, nên đây là ca đỏ THẬT theo cả
    // hai chiều: thêm `ja` vào `languages` mà quên `<mọi tiền tố>ja.json`, HOẶC thêm một tiền tố
    // thứ ba mà quên file nào — `loadBundle` đỏ ngay ở `beforeAll`.
    expect(sources.size).toBe(APP_I18N.resources.length);
    for (const [prefix, perCode] of sources) {
      expect(perCode.size)
        .withContext(`nguồn ${prefix} thiếu file bảng dịch cho một ngôn ngữ đã khai`)
        .toBe(APP_I18N.languages.length);
    }
  });

  /**
   * Ranh giới Core ↔ dự án tách bằng FILE, không bằng tiền tố khoá
   * (doc/huong_dan/wiki-core/fe/08-i18n.md §Khoá nằm ở file nào). Cái giá của lựa chọn đó là một
   * lỗi im lặng MỚI: hai nguồn có thể khai TRÙNG một khoá, và nguồn sau lặng lẽ ghi đè nguồn trước.
   *
   * Cơ chế ghi đè là CÓ CHỦ ĐÍCH — nó là lối để sản phẩm thứ hai đổi một câu của nền tảng mà không
   * phải mở file dịch của nền tảng ra sửa. Nhưng ghi đè NGOÀI Ý MUỐN thì không gì báo: câu Core
   * đổi nghĩa ở đúng một sản phẩm, và người sửa file Core không hiểu vì sao thay đổi của mình biến
   * mất.
   */
  it('🛑 nguồn dự án KHÔNG âm thầm ghi đè khoá của Core', () => {
    for (const language of APP_I18N.languages) {
      const owner = new Map<string, string>();
      const clashes: string[] = [];

      for (const [prefix, perCode] of sources) {
        for (const key of flatten(perCode.get(language.code))) {
          const first = owner.get(key);
          if (first !== undefined && !DELIBERATE_OVERRIDES.includes(key)) {
            clashes.push(`${key} (${first} ⇄ ${prefix})`);
          }
          owner.set(key, first ?? prefix);
        }
      }

      expect(clashes)
        .withContext(
          `${language.code}: các khoá trên khai ở HAI nguồn — nguồn sau ghi đè nguồn trước. Cố ý ` +
            `thì thêm khoá vào DELIBERATE_OVERRIDES kèm lý do; không cố ý thì đổi tên khoá ở ` +
            `nguồn dự án.`,
        )
        .toEqual([]);
    }
  });

  it('ca đối chứng — phép kiểm trùng khoá PHÁT HIỆN được một khoá khai ở hai nguồn', () => {
    // Không có `it` này thì phép kiểm trên xanh kể cả khi vòng lặp không bao giờ chạy tới `fail`.
    const core = flatten({ shared: { action: { cancel: 'Huỷ' } } });
    const app = flatten({ shared: { action: { cancel: 'Bỏ' } } });

    expect(app.filter((key) => core.includes(key))).toEqual(['shared.action.cancel']);
  });

  it('🛑 không khoá nào viết PHẲNG với dấu chấm trong tên (bẫy ngx-translate)', () => {
    for (const [prefix, perCode] of sources) {
      for (const [code, bundle] of perCode) {
        expect(dottedKeyNames(bundle))
          .withContext(
            `${prefix}${code}.json — dấu chấm là ký tự PHÂN CẤP của ngx-translate. Khoá có dấu ` +
              `chấm trong TÊN sẽ không bao giờ tra trúng, và thư viện trả về chính chuỗi khoá ` +
              `thay vì báo lỗi.`,
          )
          .toEqual([]);
      }
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

  /**
   * Parity soi TỪNG NGUỒN, không soi bảng đã ghép. Ghép rồi mới so là phép kiểm MÙ đúng ở chỗ nguy
   * hiểm nhất: một khoá có ở `i18n/vi.json` mà thiếu ở `i18n-app/vi.json` vẫn hiện ra đủ trong bảng
   * ghép, nên bản ghép của hai ngôn ngữ có thể khớp nhau hoàn hảo trong khi từng file thì lệch.
   */
  it('🛑 trong TỪNG nguồn, mọi ngôn ngữ có ĐÚNG cùng một tập khoá', () => {
    for (const [prefix, perCode] of sources) {
      const [reference, ...others] = [...perCode.entries()];
      const referenceKeys = flatten(reference[1]).sort();

      for (const [code, bundle] of others) {
        const keys = flatten(bundle).sort();
        expect(keys.filter((key) => !referenceKeys.includes(key)))
          .withContext(`${prefix}${code}.json có khoá mà ${prefix}${reference[0]}.json không có`)
          .toEqual([]);
        expect(referenceKeys.filter((key) => !keys.includes(key)))
          .withContext(
            `${prefix}${code}.json THIẾU khoá so với ${prefix}${reference[0]}.json — người dùng ` +
              `ngôn ngữ này sẽ đọc câu của ngôn ngữ khác`,
          )
          .toEqual([]);
      }
    }
  });

  it('ca đối chứng — phép so tập khoá PHÁT HIỆN được khoá thiếu', () => {
    expect(flatten({ a: { b: 'x', c: 'y' } })).toEqual(['a.b', 'a.c']);
    expect(flatten({ a: { b: 'x' } })).not.toEqual(['a.b', 'a.c']);
  });

  it('🛑 mỗi câu có cùng tập tham số {{...}} ở mọi ngôn ngữ, trong TỪNG nguồn', () => {
    for (const [prefix, perCode] of sources) {
      const [reference, ...others] = [...perCode.entries()];
      const referenceParams = new Map(
        leafStrings(reference[1]).map(([key, text]) => [key, paramsOf(text)]),
      );

      for (const [code, bundle] of others) {
        for (const [key, text] of leafStrings(bundle)) {
          expect(paramsOf(text))
            .withContext(
              `${prefix}${code}.json → ${key}: tham số khác với ${prefix}${reference[0]}.json — ` +
                `câu này sẽ mất giá trị BE gửi kèm`,
            )
            .toEqual(referenceParams.get(key) ?? []);
        }
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
  it('🛑 mọi khoá `title` của route đều có mặt trong bảng dịch của MỌI ngôn ngữ', async () => {
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
    translate.setTranslation('vi', await loadMergedBundle('vi'));
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
    translate.setTranslation('vi', await loadMergedBundle('vi'));
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
    translate.setTranslation('vi', await loadMergedBundle('vi'));
    await firstValueFrom(translate.use('vi'));

    for (const key of ['AUTH.CHANGE_PASSWORD_FAILED', 'USER.CREATE_FAILED', 'USER.UPDATE_FAILED']) {
      expect(translate.instant(key) as string)
        .withContext(`${key} không được còn chỗ giữ nào — BE không gửi tham số cho mã này nữa`)
        .not.toContain('{{');
    }
  });
});

/**
 * `useTranslationsInTest()` — bậc thang phân giải NGUỒN bảng dịch, kiểm ở tầng dự án.
 *
 * Helper nằm ở `core/i18n/i18n.testing.ts` và cố ý KHÔNG biết tên nhóm khoá nào của dự án; nó lấy
 * danh sách nguồn từ `CORE_I18N` mà TestBed cấp. Vì vậy phép kiểm phải đứng ở đây — `src/app/` là
 * chỗ duy nhất được phép biết `APP_I18N`.
 *
 * 🛑 Vì sao phép kiểm này tồn tại: tới 2026-09-09 helper còn khai CỨNG `fetch('/i18n/<mã>.json')`,
 * tức chỉ nạp được nhóm CoreBase. Mọi spec màn NGHIỆP VỤ dùng nó rồi assert lên câu tiếng Việt sẽ
 * đỏ — và cách sửa dễ nhất, đã xảy ra thật, là chép một loader riêng vào từng spec module. Hai
 * helper cùng vai thì người sau dùng cái mù (`.claude/CLAUDE.md` §5). Cặp `it` dưới đây khoá cả
 * hai chiều: nạp đủ nguồn thì tra TRÚNG, và chỉ nhóm Core thì tra TRẬT.
 *
 * Khoá dùng để đo được TÍNH ra từ chính các file bảng dịch (khoá chỉ có ở nguồn CUỐI, không có ở
 * nguồn ĐẦU), không viết cứng — đổi tên khoá hay thêm nguồn thứ ba đều không phải sửa spec này.
 */
describe('useTranslationsInTest — nạp ĐỦ mọi nguồn của APP_I18N, không riêng nhóm Core', () => {
  /** Một khoá chỉ có ở nguồn dự án. Không có khoá nào như vậy = spec dưới vô nghĩa, nên nó ĐỎ. */
  let projectOnlyKey: string;

  beforeAll(async () => {
    const prefixes = APP_I18N.resources;
    expect(prefixes.length)
      .withContext('cần ít nhất HAI nguồn thì "khoá riêng của dự án" mới có nghĩa')
      .toBeGreaterThan(1);

    const coreKeys = flatten(await loadBundle(prefixes[0], APP_I18N.defaultCode));
    const appKeys = flatten(await loadBundle(prefixes[prefixes.length - 1], APP_I18N.defaultCode));
    const onlyInApp = appKeys.filter((key) => !coreKeys.includes(key));

    expect(onlyInApp.length)
      .withContext(`${prefixes[prefixes.length - 1]} không có khoá nào riêng — không đo được gì`)
      .toBeGreaterThan(0);
    projectOnlyKey = onlyInApp[0];
  });

  it('🛑 có CORE_I18N trong TestBed ⇒ khoá của nhóm DỰ ÁN tra ra CÂU', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });

    const translate = await useTranslationsInTest(APP_I18N.defaultCode);

    expect(translate.instant(projectOnlyKey) as string)
      .withContext(
        `helper nạp thiếu nguồn thì ngx-translate trả lại chính "${projectOnlyKey}" — spec màn ` +
          `nghiệp vụ sẽ xanh vì MÙ nếu nó chỉ assert "khác rỗng"`,
      )
      .not.toBe(projectOnlyKey);
  });

  it('🛑 ca đối chứng — KHÔNG có CORE_I18N ⇒ lùi về nhóm Core, đúng khoá đó tra TRẬT', async () => {
    // Không có `it` này thì `it` trên vẫn xanh kể cả khi helper nạp bừa mọi thứ vì một lý do khác.
    // Nó cũng khoá luôn nghĩa của `CORE_ONLY_RESOURCES`: nấc lùi là nhóm CoreBase MỘT MÌNH.
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });

    const translate = await useTranslationsInTest(APP_I18N.defaultCode);

    expect(CORE_ONLY_RESOURCES).toEqual([APP_I18N.resources[0]]);
    expect(translate.instant(projectOnlyKey) as string).toBe(projectOnlyKey);
  });

  it('options.resources được truyền thẳng thì thắng CORE_I18N — nấc 1 của bậc thang', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });

    const translate = await useTranslationsInTest(APP_I18N.defaultCode, {
      codes: [APP_I18N.defaultCode],
      resources: CORE_ONLY_RESOURCES,
    });

    expect(translate.instant(projectOnlyKey) as string).toBe(projectOnlyKey);
  });
});
