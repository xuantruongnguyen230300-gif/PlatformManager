# F3 — Auth (chờ BE scaffold Identity thật)

> **Định nghĩa hoàn thành:** đăng nhập qua form thật (hoặc redirect, tuỳ BE
> scaffold) → `CurrentUserService.isAuthenticated()` = `true`, route có
> `authGuard` chặn đúng khi chưa đăng nhập, logout xoá session và guard
> chặn lại route cũ.

## Phụ thuộc BE — không tự đoán

F3 **chờ** `backend-expert` scaffold xong `AppUser : IdentityUser<Guid>` +
endpoint `/api/auth/login`, `/api/auth/logout`, `/api/auth/me` thật (theo
`src/BE/.claude/rules/api-controller.md` §Auth/Permission). Không tự dựng
UI login trước khi có endpoint thật để gọi — dễ đoán sai shape request/
response rồi phải viết lại.

**API Contract Card** cho 3 endpoint trên nên được lập trước
(`doc/contracts/auth.md`, theo mẫu trong `.claude/agents/frontend-expert.md`
§Bàn giao) — đây đúng là chỗ cơ chế "teammate song song" giữa
`backend-expert`/`frontend-expert` phát huy giá trị nhất trong toàn bộ core
FE, vì cả 2 phía cùng phụ thuộc nhau chặt tại đúng điểm này.

## Thứ tự viết (sau khi có contract AGREED)

```
1. withCredentialsInterceptor (xem ../07-auth-identity.md)     15 phút
        │
        ▼
2. AuthService (login/logout/me) + mapper response → ICurrentUser   nửa ngày
        │
        ▼
3. CurrentUserService (signal state, load lúc app init)        nửa ngày
        │
        ▼
4. authGuard + gắn vào route cần bảo vệ                         1 giờ
        │
        ▼
5. Trang/form login (shape theo Contract Card đã AGREED)         1 ngày
```

## Kiểm chứng

- [ ] Gọi API cần auth mà chưa login → 401, FE điều hướng `/login` (không
      phải màn trắng/lỗi console)
- [ ] Login thành công → gọi lại API cần auth → 200, không cần reload trang
- [ ] Logout → gọi API cần auth → quay lại 401/redirect, đúng ngay lập tức
      (không phải sau khi F5 hoặc mở tab mới)
- [ ] CORS phía BE đã bật `AllowCredentials()` — xác nhận bằng DevTools
      Network tab thấy cookie thật sự được gửi kèm request
