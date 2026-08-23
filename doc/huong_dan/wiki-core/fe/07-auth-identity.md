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
