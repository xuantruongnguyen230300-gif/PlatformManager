# F2 — Auth + routing/guard

> **Định nghĩa hoàn thành:** đăng nhập qua form thật tại `/dang-nhap` →
> `CurrentUserService.isAuthenticated()` = `true`; route có guard chặn đúng khi
> chưa đăng nhập **và** điều hướng kèm `returnUrl`; user mang
> `mustChangePassword: true` bị ép sang `/doi-mat-khau` và **không** vào được
> route nào khác; logout xoá session và guard chặn lại đúng route cũ ngay lập
> tức (không phải sau khi F5 trang).

## Hợp đồng đã chốt — không còn phải hỏi

Bản trước của file này ghi *"chờ `backend-expert` scaffold Identity, không tự
đoán shape request/response"*. Nay **[`../../../../contracts/auth.md`](../../../../contracts/auth.md)
đã chốt và đã verify thật 2026-08-16**: cookie session (không JWT), ba endpoint
`POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`, và
`CurrentUserInfo` có `mustChangePassword`.

Vì shape đã cố định, FE **dựng được ngay** trên mock theo đúng contract, không
phải đợi. Nhưng **đóng** F2 thì cần endpoint thật chạy — tức BE phải đi tới
phần auth trong lộ trình của nó
([`../../be/trien-khai/00-lo-trinh-tong-the.md`](../../be/trien-khai/00-lo-trinh-tong-the.md)).
Dựng trên mock rồi đổi sang thật là một dòng đổi base URL; đợi BE xong mới bắt
đầu là mất trắng thời gian đó.

## Thứ tự viết

```
1. withCredentialsInterceptor (nếu chưa làm ở F0)               15 phút
        │
        ▼
2. AuthService — login/logout/me + mapper → ICurrentUser        nửa ngày
        │
        ▼
3. CurrentUserService — signal state, nạp 1 lần lúc app init    nửa ngày
   (provideAppInitializer, KHÔNG nạp lại mỗi lần đổi route)
        │
        ▼
4. 3 guard + app.routes.ts + *.routes.ts từng feature            2 giờ
        │
        ▼
5. Màn /dang-nhap và /doi-mat-khau (noShell)                     1–2 ngày
```

> 📖 **Toàn bộ quy ước routing và guard** — `authGuard` kèm `returnUrl`,
> `mustChangePasswordGuard`, `roleGuard` factory, **thứ tự** ba guard, cờ
> `noShell`: [`../../../quy-uoc/fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md).
> Đó là nguồn duy nhất; không viết lại guard theo trí nhớ.

## Hai chỗ hỏng im lặng — kiểm riêng

**`mustChangePasswordGuard` bị bỏ quên.** `authGuard` chỉ hỏi *"đã đăng nhập
chưa"*, mà người bị buộc đổi mật khẩu **đã** đăng nhập — nên thiếu guard này
thì họ vào được toàn bộ app. Không có lỗi biên dịch nào báo, không có test mặc
định nào bắt. Phải kiểm bằng tay bằng đúng một tài khoản `mustChangePassword`.

**CORS thiếu `AllowCredentials()` phía BE.** Browser sẽ **không gửi** cookie dù
`withCredentials: true` đã đặt đúng phía FE, và triệu chứng trông y hệt "chưa
đăng nhập" dù vừa login thành công. Gặp "luôn 401 dù đã login" thì kiểm CORS
**trước**, đừng nghi code FE — xem [`../07-auth-identity.md`](../07-auth-identity.md)
§CORS phía BE.

## Kiểm chứng

- [ ] Gọi API cần auth khi chưa login → điều hướng `/dang-nhap?returnUrl=…`
      (không phải màn trắng hay lỗi console)
- [ ] Login xong → quay đúng về `returnUrl`, không phải luôn về `/dashboard`
- [ ] Tài khoản `mustChangePassword: true`: gõ thẳng URL bất kỳ đều bị đưa về
      `/doi-mat-khau`; riêng `/doi-mat-khau` **không** lặp vô hạn
- [ ] Đổi mật khẩu xong đi thẳng vào app — **không** bắt đăng nhập lại
- [ ] Logout → gọi lại API cần auth → chặn ngay, không cần F5 hay mở tab mới
- [ ] DevTools → Network: cookie thật sự được gửi kèm request
