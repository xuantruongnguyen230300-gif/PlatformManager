---
kind: luat
scope: core
verified: 2026-09-10
---

# Routing & Guard — src/FE

Quy ước điều hướng và bảo vệ route cho Angular 20 standalone + zoneless.

> 📖 Ranh giới tầng và cấu trúc thư mục: [`fe-architecture.md`](fe-architecture.md) ·
> Gọi API và envelope: [`fe-api-client.md`](fe-api-client.md) ·
> Hợp đồng auth: [`../../contracts/auth.md`](../../contracts/auth.md)

> **Lịch sử:** trước 2026-08-23 toàn bộ `wiki-core/fe/` (70 KB) có **0 dòng** về
> `loadChildren`, **0 dòng** về guard theo role, **0 dòng** về `mustChangePassword`,
> và đúng 2 dòng nhắc tên tầng `platform/`. Bốn khoảng trống đó chặn ngay bước tạo
> file thứ hai khi dựng lại app, và cái thứ ba có hệ quả bảo mật (xem §4).

> ### Trạng thái đối chiếu `src/FE` — 2026-08-28
>
> | Doc (đích) | Code |
> | --- | --- |
> | `mustChangePasswordGuard` là guard riêng (§4) | ✅ đã tách — `core/auth/must-change-password.guard.ts` |
> | `roleGuard` factory (§5) | ✅ đã có — `core/auth/role.guard.ts`, export `adminGuard`/`superAdminGuard` |
> | `title` khai ở cấp `Route` (§2 quy tắc 3) | ✅ CÓ THẬT (đối chiếu lại 2026-09-06) — **mọi** route lá khai `title:` cấp `Route`, và giá trị là **khoá dịch**; `PageTitleStrategy` khai ở `src/FE/src/app/core/title/page-title.strategy.ts:49` |
> | Đường dẫn `core/` không khai cứng (§10) | ✅ CÓ THẬT (2026-09-02, đối chiếu lại 2026-09-06) — lệnh kiểm ở cuối file trả về 0 dòng |
>
> ⚠️ **Bẫy đã được khoá bằng máy, đừng gỡ.** `/doi-mat-khau` chỉ gắn `authGuard`;
> gắn thêm `mustChangePasswordGuard` vào đó là **vòng lặp redirect vô hạn** mà
> build/lint/test thường không bắt được. Ràng buộc này nay có test canh —
> `src/FE/src/app/app.routes.spec.ts` khẳng định đúng bảng route ở §1.
>
> ✅ **`title` đã chuyển xong (2026-08-28)** — và đúng như dự đoán, nó **không**
> phải đổi cơ học. `title:` được router resolve vào `snapshot.data` dưới một
> **symbol key** nội bộ, không phải chuỗi `'title'`; nên chỉ đổi các file
> `*.routes.ts` mà không làm gì thêm thì `data['title']` cũ thành `undefined` và
> **topbar mất tiêu đề ở mọi trang** trong khi build/lint vẫn xanh. Lời giải đã
> thi công: `PageTitleStrategy` đọc `title` một lần rồi cấp cho **hai** nơi —
> `<title>` của tab (`applyDocumentTitle()`, `page-title.strategy.ts:116`, kèm hậu tố lấy từ
> `CORE_BRANDING.name`) và signal `pageTitle` cho topbar (`page-title.strategy.ts:88`, tiêu đề
> trần). Đăng ký ở `src/FE/src/app/app.config.ts:135` bằng `useExisting`.
>
> 🔄 LẬT 2026-09-06 — ba trích dẫn trong khối này **trỏ vào dòng chú thích**, không phải câu
> lệnh: `page-title.strategy.ts:53` (đoạn giải thích `CORE_BRANDING`), `:70` (đoạn giải thích vì
> sao inject `TranslateService`), `app.config.ts:25` (đoạn nói về `APP_CORE_ROUTES`). Gate
> `check-docs.sh` mục 6 chỉ kiểm số dòng có nằm trong file nên cả ba lọt qua — nhưng ai mở ra
> kiểm thì không thấy bằng chứng nào. Hậu tố cũng **không còn** khai cứng là ` · PlatformManager`:
> tên sản phẩm đến từ `CORE_BRANDING`, `core/` chỉ biết cách ghép.
>
> Việc này đóng một tiêu chí a11y đang trượt, không phải dọn dẹp thẩm mỹ: trước
> đó `<title>` đứng nguyên chuỗi tĩnh ở `index.html` cho mọi route — trượt
> **WCAG 2.4.2 Page Titled, mức A**, trong khi mức đã chốt là WCAG 2.2 **AA bắt
> buộc** ([`../wiki-core/fe/15-accessibility.md`](../wiki-core/fe/15-accessibility.md) §1).
>
> Quy ước export: [`fe-architecture.md`](fe-architecture.md) §Cấu trúc một feature
> chốt **`export const <FEATURE>_ROUTES`**; ví dụ trong file này còn viết
> `export const routes` — sửa ví dụ ở đây theo nó khi đụng vào.

## 1. Bản đồ route

Đếm bằng lệnh, đừng tin số cứng: `grep -c "loadChildren" src/FE/src/app/app.routes.ts`.

| Route | Tầng | Guard | Shell |
| --- | --- | --- | --- |
| `/dang-nhap` | `platform/login` | — | **không** (`noShell`) |
| `/doi-mat-khau` | `platform/doi-mat-khau` | `authGuard` | **không** (`noShell`) |
| `/trang-chu` | `platform/trang-chu` | `authGuard` → `mustChangePasswordGuard` | có |
| `/quan-tri/nguoi-dung` | `platform/quan-tri-nguoi-dung` | + `adminGuard` | có |
| `/quan-tri/phan-quyen` | `platform/phan-quyen` | + `superAdminGuard` | có |
| `/danh-muc/dti` | `modules/danh-muc-dti` | `authGuard` → `mustChangePasswordGuard` | có |
| 🚧 `/tong-quan/dti` | `modules/dashboard` | `authGuard` → `mustChangePasswordGuard` | có |

🚧 **`/tong-quan/dti` là route TẠM và nó sẽ BIẾN MẤT** — chốt Q3: màn Dashboard DTI **chiếm**
`/trang-chu`, tức nó thay `platform/trang-chu/` chứ không đứng cạnh. Hoán đổi làm ở cuối vòng 2
(vòng 1 màn còn thiếu nút `Xuất báo cáo`); hoán đổi khi màn chưa xong là làm hỏng bến an toàn của
cả app. Điều kiện và bốn bước hoán đổi ghi tại chỗ trong
`src/FE/src/app/modules/dashboard/dashboard.routes.ts` — **hai dòng cuối bảng này gộp làm một khi
việc đó xong**, và `platform/trang-chu/` bị xoá (Q29).

- **`/trang-chu` là route mặc định** — `''` và `**` đều redirect về đó.
- 🛑 **Route mặc định KHÔNG được mang guard theo vai trò.** Nó là đích của mọi
  redirect "về chỗ an toàn" (`roleGuard` khi thiếu quyền, `/doi-mat-khau` sau khi
  đổi xong, `**` khi URL lạ) — gắn `adminGuard` vào đó là vòng lặp redirect vô hạn
  cho đúng nhóm người bị đá về. Khoá bằng máy ở `app.routes.spec.ts`.
- **Route đặt tiếng Việt không dấu**, khớp `doc/Design/.../UiInventory.md`. Không
  dùng `/login`.
- **`src/app/modules/` ĐÃ CÓ THẬT** (đối chiếu 2026-09-10) — hai màn nghiệp vụ DTI ở hai dòng cuối
  bảng trên. Phép thử khi thêm màn mới: *"màn này có ý nghĩa với MỌI sản phẩm dựng trên nền tảng,
  hay chỉ riêng domain nghiệp vụ hiện tại?"* — cái đầu vào `platform/`, cái sau vào `modules/`,
  xem [`fe-architecture.md`](fe-architecture.md). Thêm vào `modules/` thì **phải** thêm tên thư
  mục vào `BUSINESS_MODULES` ở `src/FE/eslint.config.js`, nếu không gate G8 là no-op cho chính
  module đó mà `ng lint` vẫn xanh.

  > 🔄 **LẬT 2026-09-10.** Gạch đầu dòng này từng khẳng định *"Hiện KHÔNG có màn nghiệp vụ nào —
  > `src/app/modules/` **không tồn tại**"*. Hết đúng từ 2026-09-09, và cái giá của nó không phải
  > một câu lỗi thời: bảng route ở trên khi đó liệt **5** route trong khi `app.routes.ts` có **7**,
  > mà `src/FE/src/app/app.routes.spec.ts` lại tự khai là *"chốt chặn BẰNG MÁY cho bảng route ở
  > §1"* — nghĩa là **máy và luật đếm khác nhau**, và bên đúng là máy. Một chốt chặn nói về một
  > bảng nó không đọc thì nó chỉ chặn được thứ nó tự viết ra.*

## 2. Cấu trúc file — mỗi feature một `*.routes.ts`

```ts
// app.routes.ts — CHỈ khai route cấp 1, không import component nào
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'trang-chu' },

  { path: 'dang-nhap',
    loadChildren: () => import('./platform/login/login.routes').then(m => m.LOGIN_ROUTES) },

  { path: 'doi-mat-khau',
    loadChildren: () => import('./platform/doi-mat-khau/doi-mat-khau.routes').then(m => m.DOI_MAT_KHAU_ROUTES) },

  { path: 'trang-chu',
    loadChildren: () => import('./platform/trang-chu/trang-chu.routes').then(m => m.TRANG_CHU_ROUTES) },

  { path: 'quan-tri/nguoi-dung',
    loadChildren: () => import('./platform/quan-tri-nguoi-dung/quan-tri-nguoi-dung.routes').then(m => m.QUAN_TRI_NGUOI_DUNG_ROUTES) },

  { path: 'quan-tri/phan-quyen',
    loadChildren: () => import('./platform/phan-quyen/phan-quyen.routes').then(m => m.PHAN_QUYEN_ROUTES) },

  // Màn NGHIỆP VỤ — `modules/`, không phải `platform/`.
  { path: 'danh-muc/dti',
    loadChildren: () => import('./modules/danh-muc-dti/danh-muc-dti.routes').then(m => m.DANH_MUC_DTI_ROUTES) },

  // 🚧 TẠM — biến mất khi Dashboard chiếm `/trang-chu` (Q3), xem ghi chú ở §1.
  { path: 'tong-quan/dti',
    loadChildren: () => import('./modules/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES) },

  { path: '**', redirectTo: 'trang-chu' },
];
```

*(🔄 LẬT 2026-09-06: khối trên trước đây **bỏ sót** `quan-tri/nguoi-dung` — đúng một route đang
chạy thật, và là route có bảng route ở §1 liệt kê. Đã thêm lại. 🔄 LẬT 2026-09-10: bỏ sót tiếp
**hai** route nghiệp vụ, đúng cùng một kiểu. Vẫn nên đối chiếu bằng lệnh ở đầu §1 thay vì tin khối
này — một khối mẫu chép tay đã sai hai lần thì lần thứ ba chỉ là vấn đề thời gian.)*

Tên biến `routes` ở `app.routes.ts` là **ngoại lệ có chủ đích** — nó là bảng mục
lục cấp app, không phải route của một feature. Route của feature theo
[`fe-architecture.md`](fe-architecture.md) §Cấu trúc một feature:
`export const <FEATURE>_ROUTES`.

```ts
// platform/phan-quyen/phan-quyen.routes.ts — guard khai Ở ĐÂY, không ở app.routes.ts
export const PHAN_QUYEN_ROUTES: Routes = [{
  path: '',
  canActivate: [authGuard, mustChangePasswordGuard, superAdminGuard],
  title: 'phan-quyen.routeTitle',   // KHOÁ DỊCH, không phải câu — xem quy tắc 3
  loadComponent: () => import('./pages/phan-quyen/phan-quyen.page').then(m => m.PhanQuyenPage),
}];
```

**Quy tắc cứng:**

1. `app.routes.ts` **chỉ** `loadChildren`, không `loadComponent`, không import
   component. Nó là bảng mục lục, không phải nơi khai chi tiết.
2. Guard khai trong `*.routes.ts` của **chính feature** — feature nào tự biết nó
   cần quyền gì. Đặt ở `app.routes.ts` là bắt nơi khác nhớ hộ.
3. Mỗi route có `title`, khai ở **cấp `Route`** (không phải trong `data`).
   `PageTitleStrategy` (`src/FE/src/app/core/title/page-title.strategy.ts`) đọc nó
   và set **hai** thứ: `<title>` của tab — tiêu chí **WCAG 2.4.2 Page Titled mức
   A**, bắt buộc theo [`../wiki-core/fe/15-accessibility.md`](../wiki-core/fe/15-accessibility.md) §1
   — và tiêu đề topbar của app shell. Khai nhầm vào `data: { title }` **không
   làm build/lint đỏ**, chỉ lặng lẽ mất cả hai; ràng buộc này có test canh ở
   `src/FE/src/app/app.routes.spec.ts:274` — test **duyệt `app.routes.ts` thật**
   (nạp từng `loadChildren`), nên route thêm sau này cũng bị kiểm mà không ai
   phải nhớ cập nhật test. (🔄 LẬT 2026-09-06: trích dẫn cũ là `:139`, một dòng **chú thích**
   thuộc test khác — test đang nói tới nằm ở `:274`.)

   > ### ✅ Nợ i18n đã TRẢ — `title` là **khoá dịch**, không phải câu (đối chiếu 2026-09-06)
   >
   > 🔄 LẬT 2026-09-06: khối này trước đây gắn nhãn 🚧 và nói *"các chuỗi `title` hiện **chưa**
   > đi qua cơ chế dịch nào … hôm nay `src/FE/package.json` chưa có thư viện dịch nào"*. Cả hai
   > vế đều không còn đúng.
   >
   > **Luật hiện hành: giá trị của `title` là KHOÁ (`<màn>.routeTitle`), không bao giờ là câu.**
   > Viết thẳng tiếng Việt vào đó thì tiêu đề tab và tiêu đề topbar là **hai chỗ duy nhất** trên
   > màn hình không đổi khi người dùng bấm sang English — và không có gì báo, vì cả hai vẫn là
   > chữ đọc được. Kiểm bằng lệnh:
   >
   > ```bash
   > grep -rn "title:" src/FE/src/app/*/*/*.routes.ts
   > # PASS: mọi giá trị có dạng '<màn>.routeTitle', không dòng nào là câu tiếng Việt
   > ```
   >
   > *(🔄 SỬA 2026-09-10: lệnh này trước đây neo cứng `platform/`, nên nó **không** soi hai màn
   > nghiệp vụ trong `modules/` — quét hẹp hơn luật thì nó xanh vì mù. Nay soi cả hai tầng.)*
   >
   > **Ca khó mà chốt 2026-09-03 sinh ra cũng đã xử lý.** Đổi ngôn ngữ **trong phiên** không sinh
   > lần điều hướng nào, nên `updateTitle()` không chạy lại, mà `<title>` thì ghi thẳng vào DOM
   > ngoài Angular — không template nào vẽ lại hộ. Lời giải nằm ở hai chỗ trong
   > `src/FE/src/app/core/title/page-title.strategy.ts`: lớp giữ **khoá** trong signal
   > `routeTitleKey` (`:81`) thay vì giữ câu đã dịch, và một `effect` ở constructor (`:98`) đẩy
   > lại thẻ `<title>` mỗi khi ngôn ngữ đổi. `pageTitle` của topbar tự đúng vì nó là `computed`.
4. Route auth khai `data: { noShell: true }` — xem §6. `noShell` **ở lại trong
   `data`** vì đó là cờ riêng của app, router không có API cấp `Route` cho nó;
   chỉ `title` chuyển lên.

## 3. `authGuard` — kèm `returnUrl`

```ts
export const authGuard: CanActivateFn = (_route, state) => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const coreRoutes = inject(CORE_ROUTES); // §10 — đường dẫn do app cung cấp

  return currentUser.isAuthenticated()
    ? true
    : router.createUrlTree([coreRoutes.signIn], { queryParams: { returnUrl: state.url } });
};
```

Guard trả **`UrlTree`**, không gọi `router.navigate()` — trả `UrlTree` để Angular
huỷ điều hướng cũ rồi chuyển hướng trong **một** chu kỳ; gọi `navigate()` bên
trong guard tạo hai lần điều hướng chồng nhau.

Sau khi đăng nhập thành công, `login.page` đọc `returnUrl` từ query param và điều
hướng về đó; không có thì về `/trang-chu`.

## 4. `mustChangePasswordGuard` — thứ dễ quên nhất, và là lỗ hổng nếu quên

`GET /api/auth/me` trả `mustChangePassword: boolean` trong `CurrentUserInfo`
(`doc/contracts/auth.md`). Người dùng do quản trị viên tạo mang giá trị `true`.

**Luật: khi `mustChangePassword === true`, MỌI route khác `/doi-mat-khau` đều bị
chặn.**

```ts
export const mustChangePasswordGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const coreRoutes = inject(CORE_ROUTES); // §10 — đường dẫn do app cung cấp

  return currentUser.mustChangePassword()
    ? router.createUrlTree([coreRoutes.changePassword])
    : true;
};
```

> ### ⚠️ Ba chỗ sai là hỏng
>
> **(1) Không gắn guard này = lỗ hổng.** `authGuard` chỉ hỏi *"đã đăng nhập chưa"*.
> Người bị buộc đổi mật khẩu **đã** đăng nhập, nên `authGuard` cho qua và họ vào
> được toàn bộ app — đúng thứ luật này sinh ra để chặn.
>
> **(2) `/doi-mat-khau` KHÔNG được gắn `mustChangePasswordGuard`** — gắn vào là
> vòng lặp redirect vô hạn. Route đó chỉ cần `authGuard`.
>
> **(3) Sau khi đổi mật khẩu thành công: KHÔNG bắt đăng nhập lại.** Cookie hiện
> tại vẫn hợp lệ (`doc/contracts/auth.md`). Luồng đúng: cập nhật
> `mustChangePassword` về `false` trong state (hoặc gọi lại `GET /api/auth/me`) →
> đi thẳng vào ứng dụng. Gọi `logout` rồi bắt đăng nhập lại là thêm một bước thừa
> mà người dùng không hiểu vì sao.

`CurrentUserService` vì vậy phải có đường cập nhật cờ này — `markPasswordChanged()`
hoặc `reload()`.

## 5. Guard theo role

```ts
// core/auth/role.guard.ts — factory, không viết tay từng guard
export const roleGuard = (...roles: string[]): CanActivateFn => () => {
  // `inject()` gọi TRONG guard trả về, không ở thân factory: `adminGuard`/`superAdminGuard` được
  // tạo lúc module nạp, gọi ở thân factory sẽ đóng băng đường dẫn của injector đầu tiên (§10).
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const coreRoutes = inject(CORE_ROUTES);

  return currentUser.hasAnyRole(...roles)
    ? true
    : router.createUrlTree([coreRoutes.home]);
};

export const adminGuard      = roleGuard('Admin', 'SuperAdmin');
export const superAdminGuard = roleGuard('SuperAdmin');
```

**Thiếu quyền → điều hướng về `/trang-chu`, KHÔNG có trang 403.** Đây là hành vi
đã chốt và đã ghi ở `doc/Design/.../Screens/04-phan-quyen.md` §States — *"a
signed-in non-`SuperAdmin` who navigates here never sees the screen… There is no
403 page"*.

⚠️ **`Admin` KHÔNG vào được `/quan-tri/phan-quyen`.** Chỉ `SuperAdmin` — đây là
biện pháp chống leo thang quyền qua UI, khớp `[Authorize(Roles = "SuperAdmin")]`
phía BE (`doc/contracts/permissions.md`). Đừng "sửa cho tiện" thành `adminGuard`.

> **Ẩn UI theo role KHÔNG thay cho kiểm quyền phía BE.** Sidebar chỉ hiện mục
> người dùng được phép (BE lọc qua `SysMenuRole`), nhưng ai gõ thẳng URL vẫn phải
> bị guard chặn — và BE vẫn phải trả 403. Ba lớp độc lập, không lớp nào thay được
> lớp nào.

## 6. Thứ tự guard — không tuỳ tiện

```
authGuard  →  mustChangePasswordGuard  →  roleGuard
```

Angular chạy `canActivate` **tuần tự theo thứ tự khai báo** và dừng ở cái đầu tiên
trả khác `true`. Thứ tự trên cho ra thông điệp đúng trong mọi trường hợp:

| Tình huống | Kết quả |
| --- | --- |
| Chưa đăng nhập | → `/dang-nhap?returnUrl=…` (không hỏi role của người chưa có danh tính) |
| Đã đăng nhập, buộc đổi mật khẩu | → `/doi-mat-khau` (không đá về trang chủ rồi mới chặn) |
| Đã đăng nhập, đủ điều kiện, thiếu quyền | → `/trang-chu` |

Đảo thứ tự `roleGuard` lên trước sẽ đá người **chưa đăng nhập** về `/trang-chu`
thay vì màn đăng nhập.

## 7. `noShell` — hai màn auth không có app shell

Hai route `/dang-nhap` và `/doi-mat-khau` khai `data: { noShell: true }`. `App`
đọc cờ đó và thay app shell (sidebar + topbar + toast) bằng một `<router-outlet>`
trần.

```ts
{ path: '', data: { noShell: true }, canActivate: [authGuard], ... }
```

Cờ đặt trên **route**, không phải trong component — component không nên biết nó
đang được bọc bởi cái gì.

## 8. State trên URL — bộ lọc thuộc về query param, không thuộc về `signal()`

> Bổ sung 2026-08-27, **đã thi công** ở màn hình đầu tiên (đối chiếu 2026-09-06). Khi viết,
> query param mới chỉ dùng cho `returnUrl` (§3) và bộ lọc sống trong signal — ba lỗi mô tả ngay
> dưới là hậu quả **thật** của trạng thái đó.

Ba thứ hỏng vì điều đó, đều là thứ người dùng gặp hằng ngày chứ không phải ca
biên: **F5 mất bộ lọc**; **không gửi được link "xem đúng cái tôi đang xem"**;
**nút Back của trình duyệt nhảy khỏi trang thay vì lùi một bước lọc**.

Quy tắc: state **mô tả người dùng đang xem gì** thì nằm trên URL.

| Lên URL | Ở lại trong signal |
| --- | --- |
| Bộ lọc, từ khoá tìm, kỳ/tuần/tháng, trang, sắp xếp | Dữ liệu đã tải về, cờ loading, dialog đang mở |
| Tab đang chọn (nếu chia sẻ link có ý nghĩa) | Bản nháp form chưa lưu |

- Đọc/ghi qua `ActivatedRoute.queryParams` + `router.navigate(..., { queryParams })`.
- Đổi bộ lọc dùng `replaceUrl: true` — mỗi lần gõ một chữ trong ô tìm kiếm không
  được đẻ ra một mục history mới, nếu không nút Back sẽ vô dụng.
- URL là **nguồn sự thật**: component đọc từ query param rồi mới gọi API, không
  giữ một bản sao state song song. Hai nguồn cho cùng một giá trị sẽ lệch nhau.
- Không đưa dữ liệu nhạy cảm lên URL — URL đi vào lịch sử trình duyệt và log server.

✅ **Đã áp dụng — đối chiếu 2026-09-06.** 🔄 LẬT 2026-09-06: dòng cũ ở đây ghi *"Quy ước này
**chưa được áp dụng trong `src/FE`**"*. Màn "Quản trị người dùng" đã theo đủ cả bốn gạch đầu
dòng trên: `searchText`/`role`/`isLocked`/`page`/`pageSize` đọc từ `queryParams` và ghi ngược
bằng `replaceUrl: true` + `queryParamsHandling: 'merge'`, **không** giữ bản sao trong `signal()`
(`src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.ts:344`).

Màn hình mới theo ngay từ đầu; màn hình nào chưa theo thì di trú khi có dịp chạm vào. Kiểm:

```bash
grep -rln "queryParams" src/FE/src/app/platform src/FE/src/app/modules --include=*.page.ts
```

*(🔄 SỬA 2026-09-10: lệnh này trước đây chỉ quét `platform/`. Cùng lỗi với lệnh ở §2 quy tắc 3 —
`modules/` ra đời 2026-09-09 và không lệnh nào chạm tới nó.)*

## 9. Khi thêm màn hình mới — checklist

1. Chọn tầng: `platform/` (có ý nghĩa với mọi sản phẩm) hay `modules/` (riêng
   domain nghiệp vụ)?
2. Tạo `<feature>.routes.ts` trong thư mục feature, khai `loadComponent` +
   `title` **ở cấp `Route`** (§2 quy tắc 3 — không đặt vào `data`).
3. Khai guard **trong file đó**, đúng thứ tự §6.
4. Thêm **một** dòng `loadChildren` vào `app.routes.ts`.
4b. Màn thuộc `modules/` → thêm tên thư mục vào `BUSINESS_MODULES` ở `src/FE/eslint.config.js`.
   Quên bước này thì gate **G8** là no-op cho chính module mới mà `ng lint` vẫn xanh — lý do đầy
   đủ ở [`fe-architecture.md`](fe-architecture.md) §"Thêm một module nghiệp vụ mới". Màn
   `platform/` bỏ qua bước này.
5. Cần hiện trong sidebar → thêm bản ghi `SysMenus` + `SysMenuRoles` phía BE
   (`doc/contracts/meta-menu.md`), **không** hardcode vào FE.
6. Route mới cần quyền riêng → contract BE phải có `[RequirePermission]` tương ứng;
   guard FE **không** thay thế được nó.

## 10. `CORE_ROUTES` — `core/` giữ LUẬT chuyển hướng, app cung cấp ĐƯỜNG DẪN

✅ **CÓ THẬT (đối chiếu 2026-09-02)** — `src/FE/src/app/core/config/core-routes.ts`,
wire ở `src/FE/src/app/app.config.ts` (`APP_CORE_ROUTES` + `provideCoreRoutes`).

Ba guard ở §3–§5 chuyển hướng về ba màn hình. Chúng biết **khi nào** và **đi đâu
về mặt ngữ nghĩa** (màn đăng nhập / màn đổi mật khẩu / màn mặc định) — nhưng
**không** biết chuỗi đường dẫn, vì chuỗi là thứ của riêng từng sản phẩm.

```ts
// core/config/core-routes.ts — hợp đồng, KHÔNG có giá trị mặc định
export interface ICoreRoutes {
  readonly signIn: string;
  readonly changePassword: string;
  readonly home: string;
}

export const CORE_ROUTES = new InjectionToken<ICoreRoutes>('CORE_ROUTES');

export function provideCoreRoutes(routes: ICoreRoutes): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_ROUTES, useValue: routes }]);
}
```

```ts
// app.config.ts — giá trị, thuộc về APP
export const APP_CORE_ROUTES: ICoreRoutes = {
  signIn: '/dang-nhap',
  changePassword: '/doi-mat-khau',
  home: '/trang-chu',
};

providers: [provideCoreRoutes(APP_CORE_ROUTES), /* … */];
```

**Vì sao tách (2026-09-02).** `core/` là CoreBase dùng lại cho sản phẩm khác
([`../../kien-truc-core-module.md`](../../kien-truc-core-module.md)). Trước đợt
này ba guard khai cứng ba đường dẫn tiếng Việt của riêng dự án này, nên dự án
thứ hai muốn dùng `/login`, `/change-password`, `/home` là **phải sửa vào trong
`core/`** — đúng thứ mà định nghĩa "CoreBase xong" loại trừ. Ràng buộc G9
(`eslint.config.js`) cấm `core/` import ngược lên `platform/`, nên hợp đồng phải
ở `core/`, còn giá trị thì app bơm vào.

> ### Người tiêu thụ thứ ba, và nó KHÔNG phải một guard (bổ sung 2026-09-11)
>
> Mục này — và cả bảng seam ở [`fe-architecture.md`](fe-architecture.md) §Seam — mô tả
> `CORE_ROUTES` là *"3 đường dẫn mà **guard** chuyển hướng tới"*. Nay có một người tiêu thụ khác
> loại: **một liên kết UI** trong `shared/` — nút `Đổi mật khẩu` trên Topbar đọc
> `CORE_ROUTES.changePassword` cho `routerLink` của nó
> (`src/FE/src/app/shared/components/topbar/topbar.html:26`).
>
> **Hợp đồng KHÔNG đổi** — vẫn đúng ba đường dẫn, vẫn khai theo ngữ nghĩa. Thứ đổi là *ai đọc*:
> từ "ba guard" thành "ba guard + một interceptor + một liên kết của app-shell". Ghi lại vì cái
> tên `changePassword` trong `core-routes.ts` từng được chú thích là *"Đích của
> `mustChangePasswordGuard`"* — đọc nguyên văn thì người sau sẽ tưởng thu hẹp nó về đúng luồng ép
> buộc là an toàn, và điều đó sẽ làm chết một liên kết trên mọi màn hình.
>
> 🛑 Và đây là chỗ **`shared/` chịu cùng luật với `core/`**: nó cũng đi theo nền tảng sang sản phẩm
> thứ hai. Chính `topbar.ts` đã một lần khai cứng `/dang-nhap` rồi lọt lưới vì phép kiểm khi đó
> chỉ quét `core/` — nay có test canh ở
> `src/FE/src/app/shared/components/topbar/topbar.spec.ts` § "đường dẫn đến từ CORE_ROUTES".

Cùng lý do, `httpErrorInterceptor` cũng đọc `CORE_ROUTES.signIn` thay cho hằng
`LOGIN_PATH` cũ — nó dùng đường dẫn ở **hai** chỗ: đích điều hướng khi phiên chết
(401) và phép nhận ra "đang ở màn đăng nhập" (CẠM BẪY 2).

> ### Ba quyết định thiết kế, mỗi cái chặn một lỗi im lặng
>
> **(1) Token KHÔNG có `factory` mặc định.** Quên `provideCoreRoutes()` thì
> Angular ném `NullInjectorError` ngay lần điều hướng đầu. Đặt một mặc định
> (vd `/login`) sẽ biến "quên khai" thành chuyển hướng êm ru tới route không tồn
> tại → rơi vào `**` → về trang chủ; không gate nào bắt được.
>
> **(2) `inject(CORE_ROUTES)` gọi TRONG guard, không ở thân factory `roleGuard`.**
> `adminGuard`/`superAdminGuard` được tạo một lần lúc module nạp — gọi `inject()`
> ở thân factory là đóng băng đường dẫn của injector đầu tiên (và ném NG0203 khi
> nạp module ngoài injection context).
>
> **(3) Ba giá trị app khai phải là route CÓ THẬT.** Đây là lỗi im lặng **mới**
> mà phép tách này sinh ra: đổi `path` ở `app.routes.ts` mà quên đổi
> `APP_CORE_ROUTES` thì build xanh, lint xanh, và guard đá người dùng vào hư vô.
> `src/FE/src/app/app.routes.spec.ts` khoá lại bằng máy — đối chiếu với bảng route
> thật (nạp `loadChildren`), gồm cả hai ràng buộc chống vòng lặp: `signIn` phải là
> route public (§3), `changePassword` phải nằm trong danh sách miễn trừ (§4 điểm 2).

**Kiểm bằng lệnh** — `core/` không được biết đường dẫn nào của dự án này:

```bash
grep -rn "dang-nhap\|doi-mat-khau\|trang-chu" src/FE/src/app/core --include=*.ts | grep -v spec
```

PASS = không in ra dòng nào. (File `*.spec.ts` được loại trừ có chủ đích: chúng
chép ba đường dẫn hiện tại để chứng minh **hành vi không đổi**, và chép chứ không
import từ `app.config.ts` vì `core/` là tầng đáy.)
