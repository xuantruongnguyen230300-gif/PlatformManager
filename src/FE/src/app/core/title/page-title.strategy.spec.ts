import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Routes, TitleStrategy, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PageTitleStrategy } from './page-title.strategy';
import { ICoreBranding, provideCoreBranding } from '../config/core-branding';
import { appConfig } from '../../app.config';

/**
 * Tên sản phẩm dùng trong test CỐ Ý khác tên thật của dự án: nếu ai đó khai cứng lại chuỗi
 * `PlatformManager` vào `PageTitleStrategy`, mọi `expect` dưới đây đỏ ngay. Dùng đúng tên thật
 * thì test vẫn xanh dù seam đã bị vô hiệu — phép kiểm khi đó chỉ đang tự soi gương.
 */
const TEST_BRANDING: ICoreBranding = { name: 'Ứng Dụng Thử', shortName: 'UT' };

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPage {}

/**
 * Route dựng theo ĐÚNG hình dạng thật của app: route cấp 1 không có component, tiêu đề nằm ở
 * route con `path: ''` — đó là hình mà `loadChildren` + `<feature>.routes.ts` sinh ra
 * (doc/huong_dan/quy-uoc/fe-routing-guard.md §2). Test bằng `children` thay vì `loadChildren`
 * để không phải nạp trang thật (kéo theo service + HTTP), nhưng cây snapshot mà `buildTitle()`
 * duyệt thì giống hệt.
 */
const TEST_ROUTES: Routes = [
  { path: 'dashboard', children: [{ path: '', component: BlankPage, title: 'test.dashboard' }] },
  { path: 'quan-tri/phan-quyen', children: [{ path: '', component: BlankPage, title: 'test.phanQuyen' }] },
  { path: 'khong-co-title', children: [{ path: '', component: BlankPage }] },
];

/**
 * Bảng dịch RIÊNG của test, không nạp `public/i18n/*.json`.
 *
 * Cùng chủ đích với `TEST_BRANDING` ngay trên: khoá và câu ở đây CỐ Ý không phải khoá thật của
 * app, nên nếu ai đó khai cứng lại một câu tiếng Việt vào `PageTitleStrategy` thì mọi `expect`
 * dưới đây đỏ. Dùng khoá thật thì test vẫn xanh dù cơ chế tra bảng dịch đã bị vô hiệu.
 *
 * Hai ngôn ngữ vì phép kiểm quan trọng nhất của describe này là *đổi ngôn ngữ thì `<title>` đổi
 * theo* — nó cần ít nhất hai bảng để so.
 */
const TEST_TRANSLATIONS = {
  vi: { test: { dashboard: 'Bảng điều khiển', phanQuyen: 'Phân quyền' } },
  en: { test: { dashboard: 'Dashboard', phanQuyen: 'Permissions' } },
};

/**
 * Test này canh tiêu chí **WCAG 2.4.2 Page Titled, mức A** — bắt buộc theo
 * doc/huong_dan/wiki-core/fe/15-accessibility.md §1 (WCAG 2.2 AA, mà AA bao trùm mức A).
 *
 * Vì sao phải canh bằng test: `<title>` đứng yên ở mọi trang KHÔNG làm build đỏ, KHÔNG làm lint
 * đỏ, và không nhìn thấy được khi thử app bằng mắt — người sáng mắt không đọc tiêu đề tab. Đó
 * đúng là lý do nó đã trượt suốt một thời gian dài trước khi có `PageTitleStrategy`.
 *
 * Hai đầu ra được kiểm riêng vì chúng KHÁC NHAU có chủ đích: `<title>` của tab kèm hậu tố tên
 * ứng dụng (tiêu đề bị tách khỏi ngữ cảnh, cần biết nó thuộc ứng dụng nào), còn tiêu đề topbar
 * thì không (đã ở trong ứng dụng rồi).
 */
describe('PageTitleStrategy — <title> của tab đổi theo route (WCAG 2.4.2)', () => {
  const originalDocumentTitle = document.title;
  let translate: TranslateService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter(TEST_ROUTES),
        // Tên sản phẩm là DỮ LIỆU do app bơm vào (core/config/core-branding.ts) — token cố ý
        // không có mặc định, thiếu dòng này là `NG0201` ngay lần điều hướng đầu.
        provideCoreBranding(TEST_BRANDING),
        provideTranslateService(),
        { provide: TitleStrategy, useExisting: PageTitleStrategy },
      ],
    });

    translate = TestBed.inject(TranslateService);
    for (const [code, table] of Object.entries(TEST_TRANSLATIONS)) {
      translate.setTranslation(code, table);
    }
    await firstValueFrom(translate.use('vi'));
  });

  afterEach(() => {
    document.title = originalDocumentTitle;
  });

  it('set <title> của tab theo route đang mở, kèm hậu tố tên ứng dụng', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/dashboard');
    expect(document.title).toBe(`Bảng điều khiển · ${TEST_BRANDING.name}`);
  });

  it('điều hướng sang route khác thì <title> ĐỔI THEO — không giữ lại tiêu đề cũ', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/dashboard');
    const afterFirstNavigation = document.title;

    await harness.navigateByUrl('/quan-tri/phan-quyen');

    expect(afterFirstNavigation).toBe(`Bảng điều khiển · ${TEST_BRANDING.name}`);
    expect(document.title).toBe(`Phân quyền · ${TEST_BRANDING.name}`);
    expect(document.title).not.toBe(afterFirstNavigation);
  });

  it('route không khai title → <title> lùi về tên ứng dụng, không giữ tiêu đề trang trước', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/dashboard');
    await harness.navigateByUrl('/khong-co-title');

    expect(document.title).toBe(TEST_BRANDING.name);
  });

  it('signal `pageTitle` (nguồn tiêu đề topbar) mang tiêu đề TRẦN, không hậu tố', async () => {
    const harness = await RouterTestingHarness.create();
    const strategy = TestBed.inject(PageTitleStrategy);

    await harness.navigateByUrl('/dashboard');
    expect(strategy.pageTitle()).toBe('Bảng điều khiển');

    await harness.navigateByUrl('/quan-tri/phan-quyen');
    expect(strategy.pageTitle()).toBe('Phân quyền');
  });

  it('route không khai title → topbar vẫn có chữ (lùi về tên ứng dụng), KHÔNG rỗng', async () => {
    const harness = await RouterTestingHarness.create();
    const strategy = TestBed.inject(PageTitleStrategy);

    await harness.navigateByUrl('/khong-co-title');

    expect(strategy.pageTitle()).toBe(TEST_BRANDING.name);
  });

  it('router dùng ĐÚNG thể hiện mà topbar đọc — `useExisting`, không phải bản sao', () => {
    expect(TestBed.inject(TitleStrategy)).toBe(TestBed.inject(PageTitleStrategy));
  });

  /**
   * 🛑 Đổi ngôn ngữ KHÔNG sinh ra lần điều hướng nào, nên `updateTitle()` không chạy lại. Nếu
   * `PageTitleStrategy` lưu sẵn câu đã dịch (thay vì lưu KHOÁ và tra lại), thì sau khi bấm sang
   * tiếng Anh, tiêu đề tab và tiêu đề topbar là HAI chỗ duy nhất trên màn hình còn nguyên tiếng
   * Việt — và không có gì báo, vì cả hai vẫn là chữ đọc được.
   *
   * `<title>` và `pageTitle` kiểm riêng vì chúng đi hai đường khác nhau: `pageTitle` là `computed`
   * (Angular tự tính lại), còn `<title>` là DOM ngoài Angular nên phải có `effect` đẩy lại.
   */
  it('🛑 đổi ngôn ngữ thì CẢ <title> lẫn tiêu đề topbar đổi theo — không cần điều hướng lại', async () => {
    const harness = await RouterTestingHarness.create();
    const strategy = TestBed.inject(PageTitleStrategy);

    await harness.navigateByUrl('/dashboard');
    expect(document.title).toBe(`Bảng điều khiển · ${TEST_BRANDING.name}`);
    expect(strategy.pageTitle()).toBe('Bảng điều khiển');

    await firstValueFrom(translate.use('en'));
    TestBed.tick();

    expect(strategy.pageTitle()).toBe('Dashboard');
    expect(document.title).toBe(`Dashboard · ${TEST_BRANDING.name}`);
  });

  it('ca đối chứng — hai bảng dịch của test THẬT SỰ khác nhau', () => {
    // Không có `it` này thì test trên vẫn xanh kể cả khi `use('en')` không đổi gì cả: hai câu bằng
    // nhau thì mọi `expect` đều đúng và phép kiểm không phân biệt được "đã dịch lại" với "không
    // làm gì".
    expect(TEST_TRANSLATIONS.vi.test.dashboard).not.toBe(TEST_TRANSLATIONS.en.test.dashboard);
  });
});

/**
 * Test trên KHÔNG canh được `app.config.ts`, và đây là chỗ dễ tự lừa nhất trong cả file.
 *
 * `beforeEach` của describe trên tự khai `{ provide: TitleStrategy, useExisting: PageTitleStrategy }`
 * trong `TestBed` của chính nó — nên `expect(inject(TitleStrategy)).toBe(inject(PageTitleStrategy))`
 * chỉ đang kiểm lại **dòng test vừa viết ở trên nó 58 dòng**, không hề chạm tới cấu hình thật của
 * app. Hệ quả đo được: đổi `app.config.ts` sang `useClass` thì topbar mất tiêu đề ở **mọi** trang
 * mà toàn bộ bộ test vẫn xanh.
 *
 * Describe này đọc thẳng `appConfig` đã export — nguồn cấu hình duy nhất mà `main.ts` truyền vào
 * `bootstrapApplication` — nên nó ĐỎ khi và chỉ khi app thật bị cấu hình sai.
 *
 * Vì sao kiểm hình dạng provider chứ không dựng cả `appConfig.providers` trong `TestBed`: bộ
 * providers thật kéo theo `provideAuthInit()`/`provideCsrfInit()`, tức HTTP lúc khởi tạo — dựng
 * lên trong test sẽ đổi một phép kiểm tất định thành một phép kiểm phụ thuộc mạng.
 */
describe('app.config.ts — TitleStrategy phải đăng ký bằng useExisting', () => {
  function titleStrategyProvider(): Record<string, unknown> | undefined {
    // KHÔNG dùng type predicate ở đây: `appConfig.providers` có kiểu phần tử
    // `Provider | EnvironmentProviders`, mà predicate bắt buộc thu hẹp về SUBTYPE của
    // kiểu phần tử — `Record<string, unknown>` không phải subtype nên TS2677.
    const found = appConfig.providers.find(
      (provider) =>
        typeof provider === 'object' &&
        provider !== null &&
        (provider as { provide?: unknown }).provide === TitleStrategy,
    );
    return found as Record<string, unknown> | undefined;
  }

  it('appConfig có khai provider cho TitleStrategy', () => {
    expect(titleStrategyProvider())
      .withContext('gỡ provider TitleStrategy = router quay về DefaultTitleStrategy, topbar mất tiêu đề')
      .toBeDefined();
  });

  it('🛑 dùng useExisting: PageTitleStrategy — `useClass` tạo thể hiện THỨ HAI mà router không bao giờ gọi', () => {
    const provider = titleStrategyProvider();

    expect(provider?.['useExisting'])
      .withContext('app.config.ts phải trỏ TitleStrategy vào chính thể hiện root của PageTitleStrategy')
      .toBe(PageTitleStrategy);
    expect(provider?.['useClass'])
      .withContext('`useClass` = router set <title> trên thể hiện A, topbar đọc signal của thể hiện B')
      .toBeUndefined();
  });
});
