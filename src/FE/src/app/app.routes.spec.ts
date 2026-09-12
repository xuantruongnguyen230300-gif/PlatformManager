import { CanActivateFn, CanDeactivateFn, Route, Routes } from '@angular/router';
import { routes } from './app.routes';
import { APP_CORE_ROUTES } from './app.config';
import { authGuard } from './core/auth/auth.guard';
import { mustChangePasswordGuard } from './core/auth/must-change-password.guard';
import { adminGuard, superAdminGuard } from './core/auth/role.guard';
import { IHasUnsavedChanges, unsavedChangesGuard } from './core/guards/unsaved-changes.guard';
import { LOGIN_ROUTES } from './platform/login/login.routes';
import { DOI_MAT_KHAU_ROUTES } from './platform/doi-mat-khau/doi-mat-khau.routes';
import { QUAN_TRI_NGUOI_DUNG_ROUTES } from './platform/quan-tri-nguoi-dung/quan-tri-nguoi-dung.routes';
import { PHAN_QUYEN_ROUTES } from './platform/phan-quyen/phan-quyen.routes';
import { DANH_MUC_DTI_ROUTES } from './modules/danh-muc-dti/danh-muc-dti.routes';
import { DASHBOARD_ROUTES } from './modules/dashboard/dashboard.routes';

// Tham số đặt tên `config` chứ không phải `routes`: `routes` nay là bảng mục lục thật của app,
// import ở đầu file — trùng tên sẽ che mất nó ngay trong file đang dùng cả hai.
function guardsOf(config: Routes): CanActivateFn[] {
  return (config[0].canActivate ?? []) as CanActivateFn[];
}

function deactivateGuardsOf(config: Routes): CanDeactivateFn<IHasUnsavedChanges>[] {
  return (config[0].canDeactivate ?? []) as CanDeactivateFn<IHasUnsavedChanges>[];
}

function dataOf(config: Routes): Route['data'] {
  return config[0].data;
}

function titleOf(config: Routes): Route['title'] {
  return config[0].title;
}

interface LeafRoute {
  /** Đường dẫn đầy đủ, dựng lại từ `app.routes.ts` + route con — dùng để báo lỗi cho ra tên. */
  readonly url: string;
  readonly route: Route;
  /**
   * Guard THỰC SỰ chạy khi vào route lá này = guard của mọi route tổ tiên + của chính nó, theo
   * đúng thứ tự router gọi. Phải cộng dồn chứ không đọc mỗi `route.canActivate` của lá: Angular
   * chạy `canActivate` của cha trước con, nên một guard khai ở cấp cha vẫn bảo vệ lá — đọc mỗi lá
   * sẽ báo động giả. Chiều ngược lại cũng thật: hôm nay guard nằm hết ở lá, nhưng cấu trúc route
   * đổi thì test này vẫn đo đúng thứ đang chạy.
   */
  readonly guards: readonly CanActivateFn[];
}

function collectLeaves(config: Routes, prefix: string, inherited: readonly CanActivateFn[], out: LeafRoute[]): void {
  for (const route of config) {
    const url = route.path ? `${prefix}/${route.path}` : prefix;
    const guards = [...inherited, ...((route.canActivate ?? []) as CanActivateFn[])];
    if (route.children?.length) {
      collectLeaves(route.children, url, guards, out);
      continue;
    }
    // Redirect thuần không render gì nên không cần tiêu đề.
    if (route.redirectTo !== undefined) continue;
    out.push({ url, route, guards });
  }
}

/**
 * Nạp THẬT mọi `loadChildren` khai trong `app.routes.ts` rồi trả về danh sách route lá.
 *
 * Đây là điểm khác cốt lõi so với danh sách viết tay `ALL_LEAF_ROUTES` bên dưới: hàm này lấy
 * nguồn từ chính bảng mục lục của app, nên **route thứ 7 thêm vào ngày mai cũng bị kiểm** mà
 * không ai phải nhớ cập nhật test. Test dựa trên danh sách viết tay xanh vì MÙ chứ không vì sạch
 * — repo này đã trả giá đúng khuôn đó (mẫu `\bDto\b` ở gate G6 không bao giờ khớp tên DTO thật).
 *
 * Rẻ: `loadChildren()` chỉ nạp module `*.routes.ts` (vài dòng khai báo + guard). Component vẫn
 * nằm sau `loadComponent`, không bị đụng tới, nên không kéo theo service/HTTP nào.
 */
async function loadLeafRoutes(): Promise<LeafRoute[]> {
  const leaves: LeafRoute[] = [];
  for (const top of routes) {
    if (!top.loadChildren) continue;
    const loaded = await (top.loadChildren as () => Promise<Routes>)();
    collectLeaves(loaded, `/${top.path}`, (top.canActivate ?? []) as CanActivateFn[], leaves);
  }
  return leaves;
}

/**
 * Hai danh sách MIỄN TRỪ dưới đây là toàn bộ phần "viết tay" còn lại của test guard — và chúng
 * được viết theo chiều AN TOÀN MẶC ĐỊNH: route lá nào KHÔNG có tên ở đây thì **bắt buộc** phải đủ
 * `authGuard` + `mustChangePasswordGuard`. Vì vậy route thứ 7 thêm vào ngày mai mà quên guard sẽ
 * ĐỎ, còn đổi tên đường dẫn của chính hai route này cũng ĐỎ (tên cũ biến mất khỏi danh sách lá) —
 * không có cách nào lọt qua bằng cách im lặng.
 *
 * Đây là điểm sửa cốt lõi so với bản trước: bản trước liệt kê tay 4 route ĐƯỢC KIỂM, nên mọi route
 * không có trong danh sách đều **không bị kiểm gì cả** — xanh vì mù, đúng khuôn mà chú thích của
 * `loadLeafRoutes()` ở trên cảnh báo.
 */

/** Route lá PUBLIC — nơi người dùng CHƯA đăng nhập đến, nên không đòi `authGuard`. */
const PUBLIC_LEAF_ROUTES: readonly string[] = ['/dang-nhap'];

/**
 * Route lá đã đăng nhập nhưng CẤM `mustChangePasswordGuard`: guard đó đẩy người dùng về đúng
 * `/doi-mat-khau`, gắn lên chính nó là vòng lặp redirect vô hạn (fe-routing-guard.md §4 điểm 2).
 */
const MUST_CHANGE_PASSWORD_EXEMPT: readonly string[] = ['/doi-mat-khau'];

/**
 * Mọi route lá của app, kèm tiêu đề trang mong đợi — nguồn cho các test `title` bên dưới.
 *
 * Giá trị là KHOÁ DỊCH, không phải câu: `PageTitleStrategy` tra nó qua bảng dịch rồi tra LẠI mỗi
 * lần đổi ngôn ngữ (doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch). Test dưới đây khoá
 * đúng hình dạng đó — quay về câu tiếng Việt viết thẳng sẽ ĐỎ ngay, chứ không trôi im lặng thành
 * "tiêu đề tab không đổi khi sang tiếng Anh".
 */
const ALL_LEAF_ROUTES: readonly { readonly name: string; readonly routes: Routes; readonly title: string }[] = [
  { name: '/dang-nhap', routes: LOGIN_ROUTES, title: 'login.routeTitle' },
  { name: '/doi-mat-khau', routes: DOI_MAT_KHAU_ROUTES, title: 'doi-mat-khau.routeTitle' },
  // `/trang-chu` nay là Dashboard DTI (Q3, hoán đổi 2026-09-11) — `platform/trang-chu/` đã xoá.
  { name: '/trang-chu', routes: DASHBOARD_ROUTES, title: 'dashboard.routeTitle' },
  { name: '/quan-tri/nguoi-dung', routes: QUAN_TRI_NGUOI_DUNG_ROUTES, title: 'quan-tri-nguoi-dung.routeTitle' },
  { name: '/quan-tri/phan-quyen', routes: PHAN_QUYEN_ROUTES, title: 'phan-quyen.routeTitle' },
  { name: '/danh-muc/dti', routes: DANH_MUC_DTI_ROUTES, title: 'danh-muc-dti.routeTitle' },
];

/**
 * Đây là chốt chặn BẰNG MÁY cho bảng route ở doc/huong_dan/quy-uoc/fe-routing-guard.md §1 + thứ
 * tự guard §6. Lý do phải có nó: hai lỗi dưới đây đều KHÔNG làm hỏng build, không làm hỏng lint,
 * và không màn hình nào báo lỗi khi chạy dev —
 *
 * 1. QUÊN `mustChangePasswordGuard` trên một route = LỖ HỔNG: người bị buộc đổi mật khẩu đã đăng
 *    nhập rồi nên `authGuard` cho họ qua, và họ vào được toàn bộ app (§4 điểm 1).
 * 2. GẮN `mustChangePasswordGuard` lên `/doi-mat-khau` = VÒNG LẶP REDIRECT VÔ HẠN (§4 điểm 2).
 */
describe('Bảng route — guard đúng và đủ (fe-routing-guard.md §1, §6)', () => {
  it('/dang-nhap là route PUBLIC — không guard nào', () => {
    expect(guardsOf(LOGIN_ROUTES)).toEqual([]);
  });

  it('🛑 /doi-mat-khau CHỈ authGuard — gắn mustChangePasswordGuard vào đây là vòng lặp vô hạn', () => {
    expect(guardsOf(DOI_MAT_KHAU_ROUTES)).toEqual([authGuard]);
    expect(guardsOf(DOI_MAT_KHAU_ROUTES)).not.toContain(mustChangePasswordGuard);
  });

  // 🛑 /trang-chu là ĐÍCH của mọi redirect "về chỗ an toàn" (role.guard khi thiếu quyền,
  // doi-mat-khau sau khi đổi xong, `**` khi URL lạ). Gắn thêm guard theo VAI TRÒ vào đây sẽ tạo
  // vòng lặp redirect vô hạn cho đúng nhóm người bị đá về đây — nên test khoá lại ĐÚNG 2 guard.
  it('/trang-chu: ĐÚNG authGuard → mustChangePasswordGuard, không guard vai trò nào', () => {
    expect(guardsOf(DASHBOARD_ROUTES)).toEqual([authGuard, mustChangePasswordGuard]);
    expect(guardsOf(DASHBOARD_ROUTES)).not.toContain(adminGuard);
    expect(guardsOf(DASHBOARD_ROUTES)).not.toContain(superAdminGuard);
  });

  it('/quan-tri/nguoi-dung: authGuard → mustChangePasswordGuard → adminGuard', () => {
    expect(guardsOf(QUAN_TRI_NGUOI_DUNG_ROUTES)).toEqual([authGuard, mustChangePasswordGuard, adminGuard]);
  });

  it('/quan-tri/phan-quyen: superAdminGuard (KHÔNG phải adminGuard — chống leo thang quyền)', () => {
    expect(guardsOf(PHAN_QUYEN_ROUTES)).toEqual([authGuard, mustChangePasswordGuard, superAdminGuard]);
    expect(guardsOf(PHAN_QUYEN_ROUTES)).not.toContain(adminGuard);
  });

  // 🛑 Chốt Q39 (spec/danh-muc-dti/ui-spec.md §2): màn Danh mục DTI KHÔNG có guard theo quyền.
  // Quyền GHI chỉ ẩn affordance ghi bên trong màn — gắn guard quyền lên route sẽ đá người chỉ-đọc
  // về trang chủ thay vì cho họ xem danh mục, và không có gì báo vì cả hai đều là "một trang mở
  // ra được".
  it('/danh-muc/dti: ĐÚNG authGuard → mustChangePasswordGuard, không guard vai trò nào (Q39)', () => {
    expect(guardsOf(DANH_MUC_DTI_ROUTES)).toEqual([authGuard, mustChangePasswordGuard]);
    expect(guardsOf(DANH_MUC_DTI_ROUTES)).not.toContain(adminGuard);
    expect(guardsOf(DANH_MUC_DTI_ROUTES)).not.toContain(superAdminGuard);
  });

  /**
   * 🛑 KHÔNG còn route thứ hai trỏ vào Dashboard.
   *
   * Vòng 1 khai nó ở `/tong-quan/dti` để kiểm thử mà không đụng vào bến an toàn của cả app; lượt
   * hoán đổi 2026-09-11 (Q3) gỡ hẳn đường đó. Test này khoá lại điều ấy vì hai URL cho một màn là
   * hai bookmark và hai mục lịch sử, và một trong hai sẽ trôi khỏi mọi phép kiểm mà không ai thấy.
   */
  it('🛑 `/tong-quan/dti` đã GỠ — Dashboard chỉ còn MỘT đường vào, là `/trang-chu`', () => {
    const paths = routes.map((route) => route.path);
    expect(paths).not.toContain('tong-quan/dti');
    expect(paths).toContain('trang-chu');
    expect(titleOf(DASHBOARD_ROUTES)).toBe('dashboard.routeTitle');
  });

  // Hai test dưới đây duyệt `app.routes.ts` THẬT (nạp `loadChildren`), KHÔNG dùng danh sách route
  // viết tay — chúng là phần canh route TƯƠNG LAI, song song với ba test `title` ở describe sau.
  // Sáu test ở trên chỉ canh sáu route đã biết và khoá cả THỨ TỰ guard; hai test này canh mọi lá,
  // kể cả lá chưa tồn tại lúc viết dòng này.

  it('MỌI route lá ngoài /dang-nhap đều có authGuard, và ngoài /doi-mat-khau đều có mustChangePasswordGuard — kể cả route thêm sau này', async () => {
    const leaves = await loadLeafRoutes();

    expect(leaves.length).toBeGreaterThan(0);
    for (const { url, guards } of leaves) {
      if (PUBLIC_LEAF_ROUTES.includes(url)) continue;

      expect(guards)
        .withContext(`${url} thiếu authGuard — người CHƯA đăng nhập gõ thẳng URL này là vào được`)
        .toContain(authGuard);

      if (MUST_CHANGE_PASSWORD_EXEMPT.includes(url)) continue;

      expect(guards)
        .withContext(
          `${url} thiếu mustChangePasswordGuard — người bị buộc đổi mật khẩu đã đăng nhập rồi nên ` +
            `authGuard cho qua, và họ vào được màn này (fe-routing-guard.md §4 điểm 1)`,
        )
        .toContain(mustChangePasswordGuard);
    }
  });

  it('🛑 route miễn trừ (/doi-mat-khau) vẫn còn trong bảng route và vẫn KHÔNG mang mustChangePasswordGuard', async () => {
    const leaves = await loadLeafRoutes();
    const exempt = leaves.filter((leaf) => MUST_CHANGE_PASSWORD_EXEMPT.includes(leaf.url));

    // Miễn trừ mà không còn route nào khớp = danh sách miễn trừ đã mục ruỗng (route bị đổi tên
    // đường dẫn). Bắt ở đây, vì nếu bỏ qua thì test trên vẫn xanh trong khi miễn trừ trỏ vào hư vô.
    expect(exempt.map((leaf) => leaf.url).sort())
      .withContext('danh sách MUST_CHANGE_PASSWORD_EXEMPT trỏ tới route không còn tồn tại')
      .toEqual([...MUST_CHANGE_PASSWORD_EXEMPT].sort());

    for (const { url, guards } of exempt) {
      expect(guards)
        .withContext(`${url} bị gắn mustChangePasswordGuard — VÒNG LẶP REDIRECT VÔ HẠN (§4 điểm 2)`)
        .not.toContain(mustChangePasswordGuard);
    }
  });

  /**
   * `canDeactivate` khai SAI chỗ là lỗi im lặng hoàn hảo: build xanh, lint xanh, màn hình chạy y
   * hệt — chỉ khác ở chỗ người dùng rời trang giữa chừng thì mất sạch thay đổi chưa lưu. Màn Phân
   * quyền lưu bằng GHI ĐÈ TOÀN BỘ nên đó là mất trắng, không phải mất một ô.
   */
  it('/quan-tri/phan-quyen khai canDeactivate: [unsavedChangesGuard] (fe/09-forms-validation.md)', () => {
    expect(deactivateGuardsOf(PHAN_QUYEN_ROUTES)).toEqual([unsavedChangesGuard]);
  });

  /**
   * `core/` không còn biết đường dẫn nào của dự án này (tách 2026-09-02, xem
   * core/config/core-routes.ts) — ba đường dẫn nó cần để chuyển hướng do `app.config.ts` bơm vào
   * qua `APP_CORE_ROUTES`. Cái giá của phép tách đó là một lỗi im lặng MỚI: đổi `path` ở
   * `app.routes.ts` mà quên đổi `APP_CORE_ROUTES` thì build xanh, lint xanh, và guard chuyển hướng
   * tới route không tồn tại → rơi vào `**` → về trang chủ. Người dùng thấy "bấm vào thì về trang
   * chủ", không ai lần ra nguyên nhân.
   *
   * Ba test dưới đây là chốt chặn cho đúng lỗi đó. Chúng đối chiếu với `app.routes.ts` THẬT (nạp
   * `loadChildren`), không với danh sách viết tay.
   */
  it('🛑 3 đường dẫn APP_CORE_ROUTES đều là route LÁ có thật trong app.routes.ts', async () => {
    const leafUrls = (await loadLeafRoutes()).map((leaf) => leaf.url);

    for (const [key, path] of Object.entries(APP_CORE_ROUTES)) {
      expect(leafUrls)
        .withContext(
          `APP_CORE_ROUTES.${key} = "${path}" không có trong bảng route — guard sẽ chuyển hướng ` +
            `tới hư vô rồi rơi vào '**' (app.config.ts ↔ app.routes.ts đã lệch nhau)`,
        )
        .toContain(path);
    }
  });

  it('APP_CORE_ROUTES.signIn là route PUBLIC — nếu không, người chưa đăng nhập bị đá vòng tròn', () => {
    // authGuard đá người chưa đăng nhập về `signIn`. Nếu chính route đó đòi authGuard thì mỗi lần
    // đá lại kích hoạt guard → vòng lặp redirect vô hạn.
    expect(PUBLIC_LEAF_ROUTES).toContain(APP_CORE_ROUTES.signIn);
  });

  it('APP_CORE_ROUTES.changePassword được miễn mustChangePasswordGuard (§4 điểm 2)', () => {
    // mustChangePasswordGuard đá về `changePassword`. Gắn chính guard đó lên route ấy là vòng lặp
    // vô hạn — miễn trừ và đích đến phải luôn là CÙNG một đường dẫn.
    expect(MUST_CHANGE_PASSWORD_EXEMPT).toContain(APP_CORE_ROUTES.changePassword);
  });

  it('đúng 2 màn auth khai noShell (§7)', () => {
    expect(dataOf(LOGIN_ROUTES)?.['noShell']).toBeTrue();
    expect(dataOf(DOI_MAT_KHAU_ROUTES)?.['noShell']).toBeTrue();
    expect(dataOf(DASHBOARD_ROUTES)?.['noShell']).toBeUndefined();
    expect(dataOf(PHAN_QUYEN_ROUTES)?.['noShell']).toBeUndefined();
  });
});

/**
 * Chốt chặn BẰNG MÁY cho §2 quy tắc 3 (*"mỗi route có `title`"*): tiêu đề khai ở **cấp `Route`**,
 * không phải trong `data`. Vì sao cần canh cả hai chiều:
 *
 * - THIẾU `title` → `<title>` của tab đứng nguyên chuỗi tĩnh ở `index.html` cho mọi trang, tức
 *   trượt **WCAG 2.4.2 Page Titled mức A** (bắt buộc theo
 *   doc/huong_dan/wiki-core/fe/15-accessibility.md §1), ĐỒNG THỜI topbar mất tiêu đề.
 * - Viết ngược về `data: { title }` → build xanh, lint xanh, không màn hình nào báo lỗi, nhưng
 *   `TitleStrategy` không thấy gì: `buildTitle()` chỉ đọc `title` cấp `Route`. Đây đúng là dạng
 *   trôi ngược mà test này sinh ra để chặn.
 */
describe('Bảng route — tiêu đề trang khai ở cấp Route (fe-routing-guard.md §2)', () => {
  for (const { name, routes: featureRoutes, title } of ALL_LEAF_ROUTES) {
    it(`${name} khai title = "${title}" ở cấp Route`, () => {
      expect(titleOf(featureRoutes)).toBe(title);
    });
  }

  // Ba test dưới đây duyệt `app.routes.ts` thật, KHÔNG dùng `ALL_LEAF_ROUTES` — chúng là phần
  // canh route TƯƠNG LAI. Sáu test ở trên chỉ canh sáu route đã biết; thiếu ba test này thì route
  // thứ 7 quên `title` sẽ xanh mọi gate, đúng lỗi vừa được sửa.

  it('MỌI route lá trong app.routes.ts đều khai title ở cấp Route — kể cả route thêm sau này', async () => {
    const leaves = await loadLeafRoutes();

    expect(leaves.length).toBeGreaterThan(0);
    for (const { url, route } of leaves) {
      expect(route.title)
        .withContext(`${url} thiếu title ở cấp Route — <title> tab sẽ đứng yên (WCAG 2.4.2 mức A)`)
        .toBeDefined();
      expect(route.title).withContext(`${url} khai title rỗng`).not.toBe('');
    }
  });

  it('🛑 KHÔNG route lá nào khai title trong data — TitleStrategy không đọc được chỗ đó', async () => {
    const leaves = await loadLeafRoutes();

    for (const { url, route } of leaves) {
      expect(route.data?.['title'])
        .withContext(`${url} còn khai title trong data — buildTitle() chỉ đọc title cấp Route`)
        .toBeUndefined();
    }
  });

  it('danh sách viết tay ở trên phủ ĐÚNG mọi route lá thật — thêm route mới phải cập nhật nó', async () => {
    const actual = (await loadLeafRoutes()).map((leaf) => leaf.url).sort();
    const whitelisted = ALL_LEAF_ROUTES.map((entry) => entry.name).sort();

    expect(actual).toEqual(whitelisted);
  });
});
