# 2. Xác thực (Identity) khi hệ thống có nhiều Process riêng biệt

## Câu hỏi: 2-3 Process riêng biệt thì Identity + JWT còn hợp lý không?

**Còn hợp lý — với 1 điều chỉnh quan trọng: không để MỖI process tự host riêng 1 bộ ASP.NET Core Identity đầy đủ.**

Nếu mỗi Process đều tự có `IdentityDbContext`/`UserManager`/`SignInManager`/endpoint `/login` riêng → 3 bản sao dữ liệu user, 3 nơi hash password, dễ lệch (đổi password ở Process A không ai biết Process B chưa cập nhật). Đây là lỗi kiến trúc thật, không phải lý thuyết.

## Mô hình đúng — 1 nơi phát hành token, còn lại chỉ xác thực token

Đây chính xác là cách VNR.Successor làm (đã xác nhận qua `architecture.md`): trong 6 Process của họ, chỉ **`VNR.Process.Identity`** (port 5004) sở hữu `UserAccessDbContext` kế thừa `IdentityDbContext` thật + host IdentityServer4 (`/connect/*`). 5 Process còn lại (`MasterData`, `HumanResource`, `Platform`, `Notification`...) **không đụng gì tới Identity package** — chỉ cấu hình `AddAuthentication().AddJwtBearer(...)` trỏ vào public signing key của Process Identity, xác thực token hoàn toàn stateless (không cần gọi DB, không cần gọi Process khác) mỗi request.

```
┌─────────────────────┐        ┌──────────────────────┐   ┌──────────────────────┐
│ Process A (Identity) │        │ Process B (nghiệp vụ)│   │ Process C (nghiệp vụ)│
│ - IdentityDbContext  │        │ - JwtBearer validator │   │ - JwtBearer validator │
│ - /login /refresh    │──JWT──▶│   (chỉ cần public key)│   │   (chỉ cần public key)│
│ - Phát hành JWT (RS256)│      │ - KHÔNG có Identity DB│   │ - KHÔNG có Identity DB│
└──────────────────────┘        └──────────────────────┘   └──────────────────────┘
```

Với `RS256` (khoá bất đối xứng): Process phát hành giữ private key ký token, các Process còn lại chỉ cần public key (qua JWKS endpoint hoặc file cấu hình) để verify — không cần gọi ngược lại Process Identity mỗi request, không tạo phụ thuộc runtime giữa các Process.

## Vậy có cần IdentityServer4/OpenIddict/Duende (authorization server thật) không?

**Không cần, nếu 2-3 Process đó đều là backend của chính bạn** (không phải app/bên thứ 3 độc lập). Chỉ cần **1 process phát hành JWT** bằng chính `SignInManager` của ASP.NET Core Identity, ký RS256, các process khác validate bearer token bình thường — đủ dùng, nhẹ, không cần thêm hạ tầng.

**Nên nâng cấp lên 1 authorization server thật (khuyến nghị: OpenIddict — mã nguồn mở, tích hợp thẳng lên ASP.NET Core Identity sẵn có, không mất phí như Duende) khi:**

| Tình huống | Vì sao cần |
|---|---|
| Có app/mobile của bên thứ 3 cần đăng nhập vào hệ thống bạn | Cần chuẩn OAuth2 (authorization code + PKCE), không tự chế được an toàn |
| Cần trang "cấp quyền" (consent) — app X xin quyền đọc dữ liệu Y | Đây đúng là bài toán OAuth2 scope, JWT tay không có khái niệm này |
| Số lượng client app tăng nhanh, cần thu hồi/theo dõi token tập trung | Authorization server có sẵn token introspection/revocation chuẩn |
| Cần SSO với hệ thống ngoài (AD/LDAP/SAML) | Cần 1 lớp trung gian dịch giao thức |

**Điểm quan trọng: 2 hướng không loại trừ nhau.** Bắt đầu bằng Identity + JWT đơn giản (1 process phát hành), khi thật sự cần OAuth2 chuẩn thì **lắp OpenIddict lên trên chính `UserManager`/`IdentityDbContext` đang có** — không phải viết lại từ đầu, vì OpenIddict được thiết kế để chạy cùng ASP.NET Core Identity chứ không thay thế nó.

## Áp dụng vào PlatformManager

Hiện tại 1 process duy nhất, chưa cần bàn — nhưng nếu sau này tách backend PlatformManager thành ≥2 process (ví dụ 1 process API chính + 1 worker xử lý import nền), áp dụng đúng mô hình trên: process API chính giữ Identity thật, worker chỉ cần validate JWT nếu có gọi API nội bộ (nhiều khả năng worker chạy nền không cần xác thực người dùng, chỉ cần service account riêng).
