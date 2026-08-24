# 7. Auth/Identity phía FE — cookie session

## Đã CHỐT (2026-08-15)

Dùng cookie session của ASP.NET Core Identity (đồng bộ với
`doc/huong_dan/quy-uoc/be-api-controller.md` §Auth/Permission) — **không** tự
lưu JWT bearer trong `localStorage`/biến JS.

## Cấu hình `HttpClient` bắt buộc gửi cookie

Set `withCredentials: true` **tại mỗi request** qua interceptor riêng
(không dựa vào cấu hình toàn cục dễ quên khi thêm `HttpClient` provider
mới):

```ts
// core/interceptors/with-credentials.interceptor.ts
export const withCredentialsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req.clone({ withCredentials: true }));
```

```ts
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([withCredentialsInterceptor, httpErrorInterceptor])),
  ],
};
```

`withCredentialsInterceptor` đăng ký **trước** `httpErrorInterceptor` trong
mảng `withInterceptors([...])` — thứ tự interceptor Angular chạy đúng theo
thứ tự khai báo.

## `ICurrentUser` — context, không phải HTTP

```ts
// core/services/current-user.service.ts
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly user = signal<ICurrentUser | null>(null);
  readonly isAuthenticated = computed(() => this.user() !== null);

  async load(): Promise<void> {
    // GET /api/auth/me — 401 nếu chưa đăng nhập, KHÔNG throw ra ngoài (catchError trả null)
  }
}
```

- `CurrentUserService` **không** biết chi tiết envelope HTTP — 1 service
  riêng trong `core/services/auth.service.ts` gọi API, mapper trả về
  `ICurrentUser`.
- Load 1 lần lúc app khởi động (`provideAppInitializer`), không load lại
  mỗi lần đổi route.

## Guard

Guard đặt **trong route của feature cần bảo vệ**, không cấu hình rời rạc ở
`app.routes.ts`. Toàn bộ quy ước — `authGuard` kèm `returnUrl`,
`mustChangePasswordGuard`, guard theo role, và **thứ tự** của ba guard đó —
ở [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md).

> ⚠️ **Bản trước ở đây chép sẵn một `authGuard` redirect về `/login`.** Route
> thật là **`/dang-nhap`** (`doc/Design/Frontend/PlatformManager/UiInventory.md`),
> và bản chép đó thiếu cả `returnUrl` lẫn `mustChangePasswordGuard` — thiếu cái
> sau là **lỗ hổng**: người bị buộc đổi mật khẩu vẫn vào được toàn bộ app. Đã xoá
> 2026-08-23 để không tồn tại hai bản guard nói khác nhau.

## Login/logout — đã chốt, không còn phải hỏi

Cả hai là **API JSON thật**, không phải trang Razor Pages của Identity:
`POST /api/auth/login` và `POST /api/auth/logout` (`doc/contracts/auth.md`).
FE tự dựng form đăng nhập tại `platform/login`.

Bằng chứng Identity **không** chiếm quyền điều hướng: `GET /api/auth/me` khi
chưa đăng nhập trả **401 JSON sạch**, không phải 302 redirect sang
`/Account/Login` — đã verify thật 2026-08-16, xem `doc/contracts/auth.md`.

## CORS phía BE — điều kiện bắt buộc để cookie hoạt động

`AllowCredentials()` phải bật kèm origin cụ thể (không `AllowAnyOrigin()`)
— nếu thiếu, browser âm thầm **không gửi** cookie dù `withCredentials: true`
đã set đúng phía FE, và lỗi trông giống "chưa đăng nhập" dù đã login thật.
Đây là lỗi khó debug nhất của cấu hình cookie — kiểm tra CORS **trước** khi
nghi ngờ code FE khi gặp "luôn 401 dù đã login".

---

## CSRF phía FE — nửa còn lại của phòng thủ 2 lớp

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `doc/huong_dan/wiki-core/be/02-identity-auth.md` (mục "CSRF — lỗ hổng đặc
> thù của cookie auth") vừa chốt phòng thủ 2 lớp — `SameSite` + custom header
> đọc qua `IAntiforgery` — nhưng toàn bộ `wiki-core/fe/` trước bản này
> **không có một dòng nào** về việc FE đọc/gửi header đó. Thiếu nửa này thì
> lớp 2 phía BE vô nghĩa: BE đòi `X-XSRF-TOKEN`, FE không biết lấy giá trị đó
> từ đâu, mọi request ghi (`POST`/`PUT`/`PATCH`/`DELETE`) thành 403 hàng loạt.

### Angular `HttpClient` có sẵn cơ chế đúng — không tự viết interceptor

`withXsrfConfiguration()` làm đúng thứ cần: tự đọc 1 cookie, tự gắn giá trị
đó vào header trên mọi request `POST`/`PUT`/`PATCH`/`DELETE` cùng-origin —
đúng pattern "double submit cookie" mà `IAntiforgery` kiểu SPA phía BE đang
dùng.

```ts
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withInterceptors([withCredentialsInterceptor, httpErrorInterceptor]),
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',      // PHẢI khớp AntiforgeryOptions.Cookie.Name phía BE
        headerName: 'X-XSRF-TOKEN',    // khớp options.HeaderName ở AddAntiforgery
      }),
    ),
  ],
};
```

- **Tên cookie phải khớp tường minh 2 phía.** Mặc định Angular đọc cookie
  tên `XSRF-TOKEN`, nhưng `IAntiforgery.GetAndStoreTokens` mặc định của
  ASP.NET Core đặt tên cookie khác (`.AspNetCore.Antiforgery.<hash>`). Nếu BE
  không cấu hình `AntiforgeryOptions.Cookie.Name = "XSRF-TOKEN"` tường minh,
  `withXsrfConfiguration()` phía FE đọc đúng cơ chế nhưng sai tên cookie, ra
  `null`, và mọi request ghi vẫn thiếu header — lỗi trông giống "FE quên cấu
  hình" trong khi thật ra là 2 phía đặt 2 tên khác nhau.
- **`withXsrfConfiguration()` là option của `provideHttpClient`, KHÔNG phải
  interceptor tự viết** như `withCredentialsInterceptor` — đặt **cạnh**
  `withInterceptors([...])`, không phải bên trong mảng đó.

### Cookie CSRF KHÔNG được `HttpOnly` — khác cookie session, có chủ đích

Dễ nhầm lẫn nhất: cookie session (`PlatformManager.Auth`) **bắt buộc**
`HttpOnly = true` (JS không đọc được — chặn XSS đánh cắp cookie, đã chốt ở
đầu file này). Cookie CSRF thì **ngược lại, bắt buộc `HttpOnly = false`** —
`withXsrfConfiguration()` đọc giá trị token qua `document.cookie`; nếu
`HttpOnly = true` thì JS không đọc được và cơ chế chết ngay từ bước đầu. Đây
không phải lỗ hổng: giá trị trong cookie CSRF không phải bí mật cần giấu JS
(nó chỉ có tác dụng khi đi kèm cookie session thật, và cookie session mới là
thứ cần giấu) — thiết kế "double submit cookie" dựa đúng vào việc JS **đọc
được** cookie này để gắn lại vào header.

## 401 bất ngờ giữa phiên — cookie bị revoke trong lúc đang dùng

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: file
> này và `doc/huong_dan/quy-uoc/fe-routing-guard.md` chỉ xử lý 401 tại **thời
> điểm điều hướng** (`authGuard` đọc `isAuthenticated()` — giá trị nạp 1 lần
> lúc app khởi động). Không có chỗ nào xử lý 401 xảy ra **giữa phiên**, khi
> người dùng đang đứng yên trên 1 trang và cookie bị vô hiệu ở giữa chừng —
> đúng kịch bản `SecurityStampValidator` ở
> `doc/huong_dan/wiki-core/be/02-identity-auth.md` mô tả (khoá tài khoản, gỡ
> role, đổi mật khẩu ở phiên khác — có hiệu lực trong ≤30 phút, không phải
> ngay lúc điều hướng tiếp theo).

Không xử lý thì hậu quả cụ thể: người dùng đang điền form, bấm lưu, request
nhận 401 — `httpErrorInterceptor`
(`doc/huong_dan/wiki-core/fe/02-http-envelope.md`) bắt được lỗi nhưng chỉ
biết hiện toast theo `fallbackMessageForStatus(401)` — sai bản chất (đây
không phải thiếu quyền, là **hết phiên**) và không dẫn người dùng tới việc
cần làm (đăng nhập lại).

```ts
// core/interceptors/session-expired.interceptor.ts
export const sessionExpiredInterceptor: HttpInterceptorFn = (req, next) => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const wasAuthenticated = currentUser.isAuthenticated();  // đọc TRƯỚC khi request chạy

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && wasAuthenticated) {
        currentUser.clear();   // reset về null — KHÔNG gọi lại /auth/me (chính API vừa 401)
        router.navigate(['/dang-nhap'], { queryParams: { returnUrl: router.url } });
        return EMPTY;          // chặn tại đây — KHÔNG cho rơi xuống httpErrorInterceptor
      }
      return throwError(() => err);
    }),
  );
};
```

```ts
// app.config.ts — đăng ký SAU httpErrorInterceptor trong mảng
provideHttpClient(withInterceptors([
  withCredentialsInterceptor,
  httpErrorInterceptor,
  sessionExpiredInterceptor,   // gần backend nhất trong mảng ⇒ thấy response TRƯỚC lúc unwind
])),
```

- **`wasAuthenticated` đọc TRƯỚC request, không phải trong `catchError`** —
  phân biệt đúng 2 tình huống cùng trả 401: lần gọi `GET /api/auth/me` lúc
  app khởi động khi **chưa** đăng nhập (bình thường, không phải hết phiên —
  `wasAuthenticated` = `false`) và request giữa phiên của người **đã** đăng
  nhập rồi bị revoke (`wasAuthenticated` = `true`). Thiếu điều kiện này, mọi
  401 kể cả lần load đầu tiên cũng bị đá sang `/dang-nhap`.
- **Thứ tự trong mảng interceptor quyết định ai "thấy" lỗi trước.** Angular
  chạy interceptor theo thứ tự khai báo lúc request đi ra, nhưng theo thứ tự
  **ngược lại** lúc response/lỗi đi vào — interceptor đăng ký **sau cùng**
  trong mảng là interceptor gần backend nhất, và nó thấy lỗi **đầu tiên**
  trên đường quay lại. `sessionExpiredInterceptor` phải đứng sau
  `httpErrorInterceptor` để `catchError` của nó chạy trước, trả `EMPTY` chặn
  lỗi lại — nếu không, `httpErrorInterceptor` đã hiện toast sai nghĩa trước
  khi `sessionExpiredInterceptor` kịp làm gì.
- `returnUrl` dùng lại đúng cơ chế đã có ở `authGuard`
  (`doc/huong_dan/quy-uoc/fe-routing-guard.md` §3) — người dùng đăng nhập lại
  xong quay đúng về trang đang làm dở, không phải luôn về `/dashboard`.

## FOUC lúc khởi động — tránh flash màn login trước khi biết chắc

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> "`ICurrentUser` — context, không phải HTTP" ở trên chỉ nói **khi nào** load
> (`provideAppInitializer`, 1 lần lúc khởi động), không nói **UI hiện gì**
> trong lúc chờ — khoảng trống thật, vì cookie `HttpOnly` khiến FE **không
> có cách nào biết trạng thái đăng nhập** trước khi `GET /api/auth/me` trả
> về; luôn có một khoảng chờ round-trip mạng, dù ngắn.

`provideAppInitializer` chặn Angular bootstrap (root component chưa render)
cho tới khi promise/observable nó chờ hoàn tất — nghĩa là **không xảy ra**
kịch bản kinh điển "render sẵn màn cần đăng nhập rồi giật về `/dang-nhap`"
(flash-of-unauthenticated-content đúng nghĩa). Nhưng hệ quả khác vẫn còn:
trong lúc chờ, Angular **chưa render gì cả** — nếu `index.html` không có gì
khác, người dùng thấy **màn trắng** không phản hồi, trông giống app treo hơn
là đang tải, đặc biệt rõ trên mạng chậm.

```html
<!-- index.html -->
<body>
  <app-root>
    <div class="app-boot-loading" aria-label="Đang tải…">
      <div class="spinner"></div>
    </div>
  </app-root>
</body>
```

Nội dung đặt **lồng bên trong** `<app-root>...</app-root>` trong chính
`index.html` (file tĩnh, không phải template Angular) hiển thị ngay lập tức
— trước cả khi bundle Angular tải xong, huống chi trước khi
`provideAppInitializer` chạy xong. Angular tự thay thế nội dung đó bằng root
component thật **đúng 1 lần**, khi component đó render lần đầu — tức chỉ sau
khi `CurrentUserService.load()` đã có kết quả. Không cần logic Angular nào
để làm việc này (không signal, không `@if`) — đây là hành vi mặc định của
custom element khi có nội dung lồng bên trong, và nó biến "màn trắng không
rõ trạng thái" thành "đang tải" — đúng thông điệp cho đúng lúc.
