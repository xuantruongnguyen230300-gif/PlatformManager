# 7. Auth/Identity phía FE — cookie session

## Đã CHỐT (2026-08-15)

Dùng cookie session của ASP.NET Core Identity (đồng bộ với
`src/BE/.claude/rules/api-controller.md` §Auth/Permission) — **không** tự
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

```ts
export const authGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  return currentUser.isAuthenticated() || inject(Router).createUrlTree(['/login']);
};
```

Guard đặt **trong route của feature cần bảo vệ** (đúng quy ước
`architecture.md` §Routing), không cấu hình rời rạc ở `app.routes.ts`.

## Điều chưa chốt — hỏi trước khi implement

- Route `/login` render form thật hay redirect sang trang Identity mặc định
  (`/Account/Login` kiểu Razor Pages) — phụ thuộc cách `backend-expert`
  scaffold Identity, xác nhận trước khi dựng UI login.
- Logout: gọi API rồi điều hướng, hay điều hướng thẳng tới endpoint
  Identity logout — cùng phụ thuộc cách BE scaffold.

## CORS phía BE — điều kiện bắt buộc để cookie hoạt động

`AllowCredentials()` phải bật kèm origin cụ thể (không `AllowAnyOrigin()`)
— nếu thiếu, browser âm thầm **không gửi** cookie dù `withCredentials: true`
đã set đúng phía FE, và lỗi trông giống "chưa đăng nhập" dù đã login thật.
Đây là lỗi khó debug nhất của cấu hình cookie — kiểm tra CORS **trước** khi
nghi ngờ code FE khi gặp "luôn 401 dù đã login".
