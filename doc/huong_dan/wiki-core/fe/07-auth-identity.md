---
kind: luat
scope: core
verified: 2026-09-06
---

# 7. Auth/Identity phía FE — cookie session

> **Phạm vi: ai đang đăng nhập và giữ phiên an toàn** — cookie, CSRF, 401 giữa
> phiên. Bảo mật FE ngoài phạm vi đó (render HTML không tin cậy, CSP, secret
> trong bundle, `npm audit`): [14-security.md](14-security.md).

## Đã CHỐT (2026-08-15)

Dùng cookie session của ASP.NET Core Identity (đồng bộ với
`doc/huong_dan/quy-uoc/be-api-controller.md` §Auth/Permission) — **không** tự
lưu JWT bearer trong `localStorage`/biến JS.

## Cấu hình `HttpClient` bắt buộc gửi cookie

Set `withCredentials: true` **tại mỗi request** qua interceptor riêng
(không dựa vào cấu hình toàn cục dễ quên khi thêm `HttpClient` provider
mới):

```ts
// core/interceptors/credentials.interceptor.ts
export const withCredentialsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req.clone({ withCredentials: true }));
```

```ts
// app.config.ts — thứ tự THẬT, ba interceptor
provideHttpClient(
  withInterceptors([apiBaseUrlInterceptor, withCredentialsInterceptor, httpErrorInterceptor]),
  withXsrfConfiguration({}),
),
```

`withCredentialsInterceptor` đăng ký **trước** `httpErrorInterceptor` trong
mảng `withInterceptors([...])` — thứ tự interceptor Angular chạy đúng theo
thứ tự khai báo.

🔄 LẬT 2026-09-06, hai chỗ:

- Tên file là `credentials.interceptor.ts`, **không** `with-credentials.interceptor.ts`.
- Mảng thật có **ba** phần tử, mở đầu bằng `apiBaseUrlInterceptor` (gắn `environment.apiBaseUrl`
  vào URL tương đối). Bản trước bỏ sót nó, nên ai chép nguyên khối này sẽ dựng một
  `provideHttpClient` không có base URL và mọi request bay tới sai chỗ.

## `ICurrentUser` — context, không phải HTTP

```ts
// core/auth/current-user.service.ts
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly user = signal<ICurrentUser | null>(null);
  readonly isAuthenticated = computed(() => this.user() !== null);

  async load(): Promise<void> {
    // GET /api/auth/me — 401 nếu chưa đăng nhập, KHÔNG throw ra ngoài (catchError trả null)
  }
}
```

- `CurrentUserService` **không** biết chi tiết envelope HTTP ngoài `GET /auth/me` — service
  riêng `core/auth/auth.service.ts` lo login/logout/change-password rồi gọi
  `setUser()`/`clear()`/`markPasswordChanged()` để đồng bộ. (🔄 LẬT 2026-09-06: bản trước ghi
  hai file này nằm ở `core/services/` — **không có** thư mục đó; cả hai ở `core/auth/`.)
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

> 🚧 **Cập nhật 2026-08-31.** Đoạn dưới nói *"phòng thủ 2 lớp — `SameSite` + custom header"*.
> Trong triển khai đã chốt (FE khác origin) **lớp `SameSite` không tồn tại** — cookie buộc
> phải khai `SameSite=None`. Lớp thay thế là kiểm header `Origin` ở BE. Nội dung phía FE
> dưới đây **không đổi** (vẫn là lớp custom header), chỉ có tên của lớp còn lại là khác.
> 📖 Đọc [`../be/02-identity-auth.md`](../be/02-identity-auth.md) §"Quyết định người dùng 2026-08-31".

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
      withXsrfConfiguration({}),      // rỗng — xem ghi chú 2026-09-06 ngay dưới
    ),
  ],
};
```

> **Vì sao code thật truyền object RỖNG (đối chiếu 2026-09-06).** `src/FE/src/app/app.config.ts`
> gọi `withXsrfConfiguration({})`. Mặc định của Angular (`XSRF-TOKEN` / `X-XSRF-TOKEN`) đã khớp
> thẳng cấu hình BE, nên khai lại hai tên đó chỉ tạo thêm hai chỗ có thể lệch. Gọi hàm này tường
> minh **vẫn có ích** dù không truyền gì: nó là chỗ duy nhất trong file cấu hình nói ra rằng
> CSRF đang BẬT (`provideHttpClient` vốn bật sẵn kể cả khi không gọi). Đoạn dưới vẫn đúng và
> vẫn phải đọc — nếu **BE** đổi tên cookie/header thì object rỗng ngừng hoạt động, và lúc đó
> mới phải khai tường minh.

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

### Cookie CSRF phải được **mồi** trước request ghi đầu tiên — bổ sung 2026-09-06

`withXsrfConfiguration()` chỉ *đọc lại* một cookie đã tồn tại. Nếu chưa ai gọi endpoint phát
hành nó, cookie chưa có, header không được gắn, và **`POST /api/auth/login` — request ghi đầu
tiên của app — dính 403** trước khi người dùng kịp đăng nhập lần nào.

Hai chỗ mồi, cả hai đều cần (đối chiếu 2026-09-06):

| Khi nào | Ở đâu | Vì sao |
|---|---|---|
| Lúc app khởi động | `provideCsrfInit()` — `src/FE/src/app/core/http/csrf-init.provider.ts` | Cookie phải có **trước** request ghi đầu tiên, kể cả login |
| Ngay sau khi đăng nhập thành công | `AuthService.login()` gọi lại `csrf.primeToken()` | Token phát hành lúc **ẩn danh** gắn với danh tính tại thời điểm đó; dùng lại sau khi đổi danh tính ⇒ 403 *"meant for a different claims-based user"*. Không mồi lại thì luồng **buộc đổi mật khẩu lần đầu** (áp cho gần như mọi tài khoản mới) bị chặn ngay sau khi vừa login |

`CsrfService.primeToken()` **tự nuốt lỗi** — hỏng ở bước mồi không được phép chặn app khởi
động. Response của `GET /api/antiforgery/token` cũng là **ngoại lệ có chủ đích: không bọc
`IApiResult`** (xem `doc/contracts/auth.md`); giá trị `token` không ai tiêu thụ, mục đích duy
nhất của request là tác dụng phụ `Set-Cookie`.

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
nhận 401 và chỉ nhận được một toast lỗi chung — sai bản chất (đây
không phải thiếu quyền, là **hết phiên**) và không dẫn người dùng tới việc
cần làm (đăng nhập lại).

> ### ✅ ĐÃ XỬ LÝ, nhưng **KHÔNG** bằng interceptor riêng — đối chiếu 2026-09-06
>
> 🔄 LẬT 2026-09-06: mục này mô tả một file `core/interceptors/session-expired.interceptor.ts`
> **chưa bao giờ tồn tại**. Việc đó đã làm, và làm **bên trong** `httpErrorInterceptor`
> (`src/FE/src/app/core/interceptors/http-error.interceptor.ts`). Khác biệt không chỉ là chỗ
> đặt code — hình dạng giải pháp khác hẳn:
>
> | Mẫu dưới đây (chưa bao giờ dựng) | Bản đang chạy |
> |---|---|
> | Interceptor thứ 4, đặt **sau** `httpErrorInterceptor` để `catchError` chạy trước | Cùng một `catchError`, một nhánh `if` |
> | Phân biệt bằng `wasAuthenticated` đọc trước request | Phân biệt bằng **cờ `SKIP_ERROR_TOAST` trên `HttpContext`** của request probe — chính xác hơn: nó đánh dấu *"401 ở đây là bình thường"* ngay tại nơi gọi, không đoán từ trạng thái toàn cục |
> | Trả `EMPTY` để chặn lỗi | **Luôn rethrow** — nơi gọi vẫn cần biết request hỏng (tắt spinner, giữ dữ liệu form) |
> | *(không có)* | Bỏ qua khi **đang ở màn đăng nhập**: `POST /auth/login` trả 401 nghĩa là SAI MẬT KHẨU, không phải hết phiên. Điều hướng ở đó sẽ ghi đè `returnUrl` người dùng đang giữ |
> | *(không có)* | Cờ chống **nhiều 401 song song** — một trang bắn vài request cùng lúc; thiếu cờ thì 3 toast chồng nhau và 3 lần `navigate()` liên tiếp |
> | Toast lỗi (đỏ) | `toast.warn` — `doc/contracts/auth.md` §"Vòng đời phiên" nói rõ 401 giữa chừng là chuyện **bình thường**, toast đỏ làm người dùng tưởng app hỏng |
>
> Ba dòng cuối là những ca chỉ lộ ra lúc thi công. Giữ mẫu dưới lại vì **lập luận về thứ tự
> interceptor** vẫn đúng và vẫn đáng đọc; đừng dựng nó thành file thật.

```ts
// 📐 MẪU MINH HOẠ — file này KHÔNG tồn tại, xem bảng đối chiếu ngay trên
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
  xong quay đúng về trang đang làm dở, không phải luôn về màn mặc định.
  (🔄 LẬT 2026-09-06: bản trước ghi `/dashboard`; route đó đã gỡ 2026-08-29. Màn mặc định nay
  là `CORE_ROUTES.home` = `/trang-chu`, và `core/` **không** khai cứng đường dẫn nào — app bơm
  vào từ `app.config.ts`, xem `src/FE/src/app/core/config/core-routes.ts`.)

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

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG (đối chiếu 2026-09-06).** `src/FE/src/index.html` hiện để
> `<app-root></app-root>` **rỗng**, tức màn trắng trong lúc `provideAppInitializer` chờ. Khối
> dưới đây là việc phải làm.

```html
<!-- index.html  (chưa thi công — đích đến) -->
<body>
  <app-root>
    <div class="app-boot-loading" aria-label="Đang tải…">
      <div class="spinner"></div>
    </div>
  </app-root>
</body>
```

⚠️ Khi làm: câu `aria-label` này nằm **ngoài** Angular nên không đi qua bảng dịch
(`public/i18n/*.json`) và cổng **G12** cũng không quét `.html` ở `src/` gốc — nó chỉ quét
`src/app/**`. Đây là chuỗi tiếng Việt cứng duy nhất được phép, và phải cố ý chấp nhận nó:
`index.html` được đọc trước khi bất cứ thứ gì của Angular chạy.

Nội dung đặt **lồng bên trong** `<app-root>...</app-root>` trong chính
`index.html` (file tĩnh, không phải template Angular) hiển thị ngay lập tức
— trước cả khi bundle Angular tải xong, huống chi trước khi
`provideAppInitializer` chạy xong. Angular tự thay thế nội dung đó bằng root
component thật **đúng 1 lần**, khi component đó render lần đầu — tức chỉ sau
khi `CurrentUserService.load()` đã có kết quả. Không cần logic Angular nào
để làm việc này (không signal, không `@if`) — đây là hành vi mặc định của
custom element khi có nội dung lồng bên trong, và nó biến "màn trắng không
rõ trạng thái" thành "đang tải" — đúng thông điệp cho đúng lúc.
