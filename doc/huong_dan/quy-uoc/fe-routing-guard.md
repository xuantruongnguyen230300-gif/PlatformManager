# Routing & Guard — src/FE

Quy ước điều hướng và bảo vệ route cho Angular 20 standalone + zoneless.

> 📖 Ranh giới tầng và cấu trúc thư mục: [`fe-architecture.md`](fe-architecture.md) ·
> Gọi API và envelope: [`fe-api-client.md`](fe-api-client.md) ·
> Hợp đồng auth: [`../../contracts/auth.md`](../../contracts/auth.md)

> **Lịch sử:** trước 2026-08-23 toàn bộ `wiki-core/fe/` (70 KB) có **0 dòng** về
> `loadChildren`, **0 dòng** về guard theo role, **0 dòng** về `mustChangePassword`,
> và đúng 2 dòng nhắc tên tầng `platform/`. Bốn khoảng trống đó chặn ngay bước tạo
> file thứ hai khi dựng lại app, và cái thứ ba có hệ quả bảo mật (xem §4).

## 1. Bản đồ route — 6 route, không hơn

| Route | Tầng | Guard | Shell |
| --- | --- | --- | --- |
| `/dang-nhap` | `platform/login` | — | **không** (`noShell`) |
| `/doi-mat-khau` | `platform/doi-mat-khau` | `authGuard` | **không** (`noShell`) |
| `/dashboard` | `modules/dashboard` | `authGuard` → `mustChangePasswordGuard` | có |
| `/danh-muc/dti` | `modules/danh-muc-dti` | `authGuard` → `mustChangePasswordGuard` | có |
| `/quan-tri/nguoi-dung` | `platform/quan-tri-nguoi-dung` | + `adminGuard` | có |
| `/quan-tri/phan-quyen` | `platform/phan-quyen` | + `superAdminGuard` | có |

- **`/dashboard` là route mặc định** — `''` và `**` đều redirect về đó.
- **Route đặt tiếng Việt không dấu**, khớp `doc/Design/.../UiInventory.md`. Không
  dùng `/login`.
- **4 màn Core ở `platform/`**, 2 màn nghiệp vụ ở `modules/`. Phép thử khi thêm
  màn mới: *"màn này có ý nghĩa với MỌI sản phẩm dựng trên nền tảng, hay chỉ riêng
  domain nghiệp vụ hiện tại?"* — xem [`fe-architecture.md`](fe-architecture.md).

## 2. Cấu trúc file — mỗi feature một `*.routes.ts`

```ts
// app.routes.ts — CHỈ khai route cấp 1, không import component nào
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },

  { path: 'dang-nhap',
    loadChildren: () => import('./platform/login/login.routes').then(m => m.routes) },

  { path: 'doi-mat-khau',
    loadChildren: () => import('./platform/doi-mat-khau/doi-mat-khau.routes').then(m => m.routes) },

  { path: 'dashboard',
    loadChildren: () => import('./modules/dashboard/dashboard.routes').then(m => m.routes) },

  { path: 'quan-tri/phan-quyen',
    loadChildren: () => import('./platform/phan-quyen/phan-quyen.routes').then(m => m.routes) },

  { path: '**', redirectTo: 'dashboard' },
];
```

```ts
// platform/phan-quyen/phan-quyen.routes.ts — guard khai Ở ĐÂY, không ở app.routes.ts
export const routes: Routes = [{
  path: '',
  canActivate: [authGuard, mustChangePasswordGuard, superAdminGuard],
  title: 'Phân quyền',
  loadComponent: () => import('./pages/phan-quyen/phan-quyen.page').then(m => m.PhanQuyenPage),
}];
```

**Quy tắc cứng:**

1. `app.routes.ts` **chỉ** `loadChildren`, không `loadComponent`, không import
   component. Nó là bảng mục lục, không phải nơi khai chi tiết.
2. Guard khai trong `*.routes.ts` của **chính feature** — feature nào tự biết nó
   cần quyền gì. Đặt ở `app.routes.ts` là bắt nơi khác nhớ hộ.
3. Mỗi route có `title` — Angular tự set `<title>` trang.
4. Route auth khai `data: { noShell: true }` — xem §6.

## 3. `authGuard` — kèm `returnUrl`

```ts
export const authGuard: CanActivateFn = (_route, state) => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  return currentUser.isAuthenticated()
    ? true
    : router.createUrlTree(['/dang-nhap'], { queryParams: { returnUrl: state.url } });
};
```

Guard trả **`UrlTree`**, không gọi `router.navigate()` — trả `UrlTree` để Angular
huỷ điều hướng cũ rồi chuyển hướng trong **một** chu kỳ; gọi `navigate()` bên
trong guard tạo hai lần điều hướng chồng nhau.

Sau khi đăng nhập thành công, `login.page` đọc `returnUrl` từ query param và điều
hướng về đó; không có thì về `/dashboard`.

## 4. `mustChangePasswordGuard` — thứ dễ quên nhất, và là lỗ hổng nếu quên

`GET /api/auth/me` trả `mustChangePassword: boolean` trong `CurrentUserInfo`
(`doc/contracts/auth.md`). Người dùng do quản trị viên tạo mang giá trị `true`.

**Luật: khi `mustChangePassword === true`, MỌI route khác `/doi-mat-khau` đều bị
chặn.**

```ts
export const mustChangePasswordGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  return currentUser.mustChangePassword()
    ? router.createUrlTree(['/doi-mat-khau'])
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
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  return currentUser.hasAnyRole(...roles)
    ? true
    : router.createUrlTree(['/dashboard']);
};

export const adminGuard      = roleGuard('Admin', 'SuperAdmin');
export const superAdminGuard = roleGuard('SuperAdmin');
```

**Thiếu quyền → điều hướng về `/dashboard`, KHÔNG có trang 403.** Đây là hành vi
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
| Đã đăng nhập, buộc đổi mật khẩu | → `/doi-mat-khau` (không đá về dashboard rồi mới chặn) |
| Đã đăng nhập, đủ điều kiện, thiếu quyền | → `/dashboard` |

Đảo thứ tự `roleGuard` lên trước sẽ đá người **chưa đăng nhập** về `/dashboard`
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

## 8. Khi thêm màn hình mới — checklist

1. Chọn tầng: `platform/` (có ý nghĩa với mọi sản phẩm) hay `modules/` (riêng
   domain nghiệp vụ)?
2. Tạo `<feature>.routes.ts` trong thư mục feature, khai `loadComponent` + `title`.
3. Khai guard **trong file đó**, đúng thứ tự §6.
4. Thêm **một** dòng `loadChildren` vào `app.routes.ts`.
5. Cần hiện trong sidebar → thêm bản ghi `SysMenus` + `SysMenuRoles` phía BE
   (`doc/contracts/meta-menu.md`), **không** hardcode vào FE.
6. Route mới cần quyền riêng → contract BE phải có `[RequirePermission]` tương ứng;
   guard FE **không** thay thế được nó.
