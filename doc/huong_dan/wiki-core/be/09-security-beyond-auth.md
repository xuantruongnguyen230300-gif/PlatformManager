---
kind: luat
scope: core
verified: 2026-09-06
---

# 9. Bảo mật ngoài phạm vi Auth

Đã bàn Auth/Identity kỹ ở [02-identity-auth.md](02-identity-auth.md) — còn vài điểm khác hay bị bỏ sót khi mới thiết kế:

- **Rate limiting** (giới hạn số request/IP hoặc /user) — chặn brute-force đăng nhập, chặn 1 client gọi API quá tải làm chậm cả hệ thống cho người khác.
  - ⚠️ **Cái bẫy đã dính thật (PlatformManager, sửa 2026-08-21):** trong ASP.NET Core, overload `options.AddFixedWindowLimiter("login", opt => …)` (và các overload `Add*Limiter(policyName, opt)` tương tự) **KHÔNG phân vùng theo ai cả** — nó tạo đúng **một** bộ đếm dùng chung cho toàn ứng dụng. Không tồn tại "partition key mặc định là remote IP". Hậu quả: hạn mức "5 lượt đăng nhập/phút" trở thành 5 lượt/phút **cộng dồn mọi người dùng**, và bất kỳ ai — không cần tài khoản — cũng khoá được đăng nhập của cả tổ chức bằng 5 request/phút. Cấu hình vẫn "trông đúng", không có lỗi biên dịch, không có test đỏ.
  - Muốn phân vùng thật thì phải tự khai: `options.AddPolicy(name, httpContext => RateLimitPartition.GetFixedWindowLimiter(partitionKey: …, factory: …))`. Xem cách làm cụ thể + xử lý `RemoteIpAddress == null` ở [`doc/huong_dan/quy-uoc/be-api-controller.md`](../../../../doc/huong_dan/quy-uoc/be-api-controller.md) §"Rate limiting".
  - ⚠️ **Phân vùng theo IP mất tác dụng khi chạy sau reverse proxy/load balancer** — `RemoteIpAddress` khi đó là IP của proxy, cả hệ thống lại về một phân vùng duy nhất, nhưng lần này khó phát hiện hơn nhiều. Cần `UseForwardedHeaders` với `KnownProxies`/`KnownNetworks` khai **tường minh**; bật `ForwardedHeaders` mà không khai `KnownProxies` thì tệ hơn không bật (ai cũng giả mạo được `X-Forwarded-For` để tự chọn phân vùng).
  - Rate limit chặn ở tầng middleware, **trước** controller ⇒ response 429 mặc định **không đi qua envelope `IApiResult`** (body rỗng). Nếu hệ thống cam kết "mọi response cùng một envelope" thì đây là ngoại lệ phải ghi rõ trong contract cho FE, hoặc phải tự bọc lại qua `RateLimiterOptions.OnRejected`.
- **Khoá tài khoản theo username (`Identity Lockout`) — bổ sung cho rate limiting theo IP ở trên, KHÔNG thay thế.** Rate limiting theo IP chặn được 1 client spam từ 1 nguồn, nhưng không chặn được tấn công brute-force **phân tán** — hàng trăm IP khác nhau, mỗi IP thử vài lần/phút (dưới ngưỡng rate limit), cùng nhắm vào 1 username cụ thể. Cơ chế `Lockout` sẵn có của ASP.NET Core Identity khoá theo **danh tính tài khoản**, không phải nguồn request, nên chặn được đúng kịch bản rate limiting bỏ lọt:
  ```csharp
  services.Configure<IdentityOptions>(options =>
  {
      options.Lockout.MaxFailedAccessAttempts = 5;
      options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
      options.Lockout.AllowedForNewUsers = true;   // bật ngay từ user đầu tiên, không phải sau N ngày
  });
  ```
  Đăng nhập phải gọi `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)` (không phải `false`) để cơ chế này thực sự đếm lần sai — xem cơ chế `LockoutEnd` đã bàn ở [02-identity-auth.md](02-identity-auth.md) §"Vòng đời phiên đăng nhập" (khoá tài khoản không đá được người **đang** online, nhưng chặn được **lần đăng nhập tiếp theo**, kể cả từ IP mới).
- **Security response header** — checklist tối thiểu OWASP cho mọi response, chi phí gần bằng 0, hay bị quên vì không gây lỗi rõ ràng nếu thiếu.

  > ✅ **Đã thi công 2026-08-31, nhưng KHÔNG ở đây** — đối chiếu 2026-09-02:
  > `grep -c "X-Content-Type-Options" src/BE/PlatformManager.Api/Program.cs` → **0**, và đó là
  > **đúng**. Mô hình triển khai đã chốt là **B** (nginx đứng trước), nên theo
  > [`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md) §5 *"mô hình B thì web
  > server phát"*. Ba header + HSTS nằm ở `nginx.conf` gốc repo.
  >
  > Ghi rõ vì một lượt rà 2026-09-02 đã báo đây là khoảng lệch doc↔code — nó **không phải** lệch,
  > chỉ là người rà quét `src/` mà thứ cần tìm không nằm trong `src/`. Mẫu code C# dưới đây giữ lại
  > cho ca **mô hình A** (app tự phục vụ file tĩnh), đừng chép vào `Program.cs` của dự án này.

  ```csharp
  app.Use(async (ctx, next) =>
  {
      ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");      // chặn browser tự đoán MIME type
      ctx.Response.Headers.Append("X-Frame-Options", "DENY");                // chặn nhúng iframe (clickjacking)
      ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
      await next();
  });
  builder.Services.AddHsts(o => o.MaxAge = TimeSpan.FromDays(365));   // bắt buộc HTTPS ở trình duyệt sau lần ghé đầu
  app.UseHsts();
  ```
  API JSON thuần (không render HTML nào cho người dùng cuối) thì rủi ro XSS/clickjacking thấp hơn app có UI server-render, nhưng vẫn nên bật vì chi phí gần như 0 — không cần Content-Security-Policy phức tạp cho tới khi có endpoint trả HTML thật.
- **Idempotency-Key cho API ghi dữ liệu qua HTTP — khác Outbox, đừng nhầm lẫn.** Outbox/idempotency đã bàn ở [05-cross-module-consistency.md](05-cross-module-consistency.md) và [12-notifications.md](12-notifications.md) chỉ giải quyết cho **job nền** (Hangfire tự retry). Endpoint ghi gọi trực tiếp qua HTTP (vd `PUT /api/admin/permissions`) không có cơ chế tương đương: client mất mạng ngay sau khi request đã tới server nhưng trước khi nhận response, client retry theo phản xạ (hoặc code tự động retry) → request thứ 2 chạy lại **toàn bộ logic ghi** dù lần đầu đã thành công. Với thao tác không tự nhiên idempotent (vd tăng số đếm, gửi thông báo), hậu quả là ghi trùng.
  - Chỉ cần cho endpoint **không tự nhiên idempotent** — `PUT` ghi đè toàn bộ (đã idempotent tự nhiên: gọi lại N lần cho cùng kết quả) không cần; `POST` tạo mới hoặc thao tác có hiệu ứng phụ (gửi email, trừ số lượng) thì cần.
  - Cách làm rẻ nhất: client tự sinh 1 `Guid` cho mỗi thao tác logic, gửi qua header `Idempotency-Key`; server lưu key đó (bảng nhỏ hoặc cache) kèm kết quả lần đầu — thấy key trùng thì trả lại kết quả cũ, không chạy lại logic.
- **Quản lý secret** (connection string, API key bên thứ 3) — không commit vào git dạng plaintext; môi trường production nên dùng cơ chế secret manager thật (Azure Key Vault, AWS Secrets Manager, hoặc tối thiểu biến môi trường không nằm trong git).
- **Input validation cho đường raw SQL/Dapper** (nếu dùng cho phần "field mở rộng"/`sysgrid` ở [03-metadata-driven-design.md](03-metadata-driven-design.md)) — EF Core tự parameterize query nên chống SQL injection mặc định; nhưng bất kỳ chỗ nào tự ráp chuỗi SQL tay (kể cả cho tính năng "linh hoạt" như lọc động) đều phải parameterize thủ công, không nối chuỗi trực tiếp giá trị người dùng nhập vào.


## Ngoại lệ đã có thật: `SuperAdmin` KHÔNG bị khoá — và mục trên từng nói ngược lại

> ### ⚠️ Sửa 2026-08-30 — code trích dẫn chính mục này để làm điều mục này cấm
>
> Dòng 19 ở trên viết: gọi `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`
> **(không phải `false`)**.
>
> Code thật gọi `lockoutOnFailure: !isSuperAdmin`
> (`src/BE/Core/PlatformManager.Core.Infrastructure/Identity/IdentityService.cs:44`, đối chiếu
> 2026-09-06 — trích dẫn cũ `:26` nay rơi vào **thân một khối chú thích**) — tức là
> `false` cho `SuperAdmin`. Và chú thích ngay trên dòng đó **trích dẫn chính mục này**
> làm căn cứ, trong khi trước 2026-08-30 mục này không chứa một chữ `SuperAdmin` nào.
>
> Kiểm lại bằng lệnh thay vì tin đoạn văn:
> `grep -c SuperAdmin doc/huong_dan/wiki-core/be/09-security-beyond-auth.md`
>
> Đây là lý do ngoại lệ đó tồn tại **10 ngày mà không ai rà tới**: người đọc code thấy
> có trích dẫn doc thì tin là đã được duyệt, người đọc doc thì thấy luật tuyệt đối và
> không có lý do đi kiểm code. Trích dẫn sai nguy hiểm hơn không trích dẫn.

**Ngoại lệ là đúng, giữ nguyên** (quyết định người dùng 2026-08-20, xác nhận lại
2026-08-30). Lý do: `lockoutOnFailure: true` cho phép **bất kỳ ai, kể cả chưa đăng nhập**,
khoá một tài khoản bất kỳ 15 phút bằng 5 lần đoán sai. Với tài khoản break-glass thì đó là
một đường tự-DoS quản trị viên cuối cùng ra khỏi hệ thống.

Cần đọc kèm hai dữ kiện làm đánh đổi này khác với vẻ ngoài của nó:

| Dữ kiện | Nguồn |
|---|---|
| `Admin` **mở khoá được** `SuperAdmin` — có test chốt | [`../../../contracts/users.md`](../../../contracts/users.md) §`POST /api/users/{id}/unlock` |
| Khoá chỉ kéo dài 15 phút rồi tự mở | `options.Lockout.DefaultLockoutTimeSpan` |

Nghĩa là kịch bản "khoá vĩnh viễn" cần **cả hai** tài khoản cùng bị khoá, và kể cả khi đó
nó tự lành. Ngoại lệ vẫn được giữ, nhưng đừng biện minh cho nó bằng một mối nguy lớn hơn
mối nguy thật.

## Liệt kê tài khoản qua đường đăng nhập — ✅ Quyết định người dùng 2026-08-30, ĐÃ VÁ

> 🔄 **LẬT 2026-09-06.** Mục này còn mang nhãn `🚧` cho một việc **đã thi công**. Nhánh
> "không tìm thấy tài khoản" nay gọi `BurnPasswordHashingTime(password)` trước khi trả lỗi:
> `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/IdentityService.cs:30`.
> Trích dẫn cũ `IdentityService.cs:14` trỏ vào dòng `/// </remarks>` — một dòng chú thích,
> không phải đường đi của code.

Hai đường rò rỉ, và đường nguy hiểm hơn không phải đường dễ thấy.

### Đường thời gian phản hồi — vá, không bàn

Hình dạng lỗ hổng **trước khi vá** (giữ lại để thấy vá cái gì):

```
Tên không tồn tại   → FindByNameAsync trả null → RETURN NGAY
Tên có thật, sai mk → chạy đủ PBKDF2 hàng trăm nghìn vòng
```

Chênh lệch đo được ổn định, và nó lộ ra **kể cả khi hai nhánh trả cùng một thông điệp lỗi**.
Đây là đường liệt kê được **hàng loạt**: quét một danh sách tên, đọc thời gian phản hồi, ra
danh sách tài khoản có thật mà không cần đoán đúng mật khẩu lần nào.

**Chốt: khi không tìm thấy tài khoản, vẫn chạy một phép băm giả rồi mới trả lỗi**, để hai
nhánh tốn thời gian tương đương. Cách xử lý chuẩn, có trong tài liệu của ASP.NET Core Identity,
không có mặt trái nào.

> ✅ **Đã thi công, đối chiếu 2026-09-06:**
> `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/IdentityService.cs:30`
> (`BurnPasswordHashingTime(password);` trong nhánh `user is null`).

### Đường thông điệp lỗi — giữ nguyên, có chủ đích

`AUTH.LOCKED_OUT` mang câu chữ khác `AUTH.INVALID_CREDENTIALS`, nên nó xác nhận tài khoản
đó có thật.

**Chốt: GIỮ hai thông điệp tách biệt** (người dùng quyết 2026-08-30). Lý do:

| | Gộp về một thông điệp | Giữ tách biệt *(chọn)* |
|---|---|---|
| Người dùng thật bị khoá | Không hiểu vì sao, thử lại liên tục — mà **mỗi lần thử lại gia hạn thêm 15 phút** | Biết phải chờ hoặc gọi ai |
| Kẻ tấn công thu được | Không gì | Xác nhận **một** tài khoản, sau khi đã đoán sai 5 lần cho riêng nó |

Điểm quyết định: sau khi vá đường thời gian, đường thông điệp **không còn liệt kê hàng loạt
được nữa** — nó tốn 5 lần đoán sai cho *mỗi* tài khoản và khoá luôn tài khoản đó lại. Đổi một
rò rỉ đắt đỏ như vậy lấy việc người dùng thật tự khoá mình lâu hơn là đổi lỗ.

Thứ tự ở đây quan trọng: **giữ được thông điệp riêng là NHỜ đã vá đường thời gian.** Nếu vì
lý do nào đó không vá được đường thời gian, quyết định này phải xét lại.

### Nghiệm thu

| # | Phép thử | PASS |
|---|---|---|
| 1 | Đo thời gian `POST /api/auth/login` với tên **không tồn tại** và tên **có thật + sai mật khẩu**, mỗi bên 50 lần | Trung vị hai bên chênh nhau **dưới 10%** |
| 2 | Đăng nhập sai 5 lần rồi lần 6 | Nhận `AUTH.LOCKED_OUT`, không phải `AUTH.INVALID_CREDENTIALS` |

Phép thử 1 phải đo **trung vị**, không phải trung bình — một lần chậm bất thường do GC hay
đọc đĩa sẽ kéo lệch trung bình và làm phép đo vô nghĩa.

## Chính sách mật khẩu — ✅ Quyết định người dùng 2026-08-30, ĐÃ THI CÔNG

> 🔄 **LẬT 2026-09-06.** Tiêu đề này mang nhãn `🚧` trong khi §"Có thật hôm nay → sẽ thành"
> ngay bên dưới **đã ghi ✅ ĐÃ XONG từ 2026-09-04**. Hai nhãn ngược nhau trong cùng một mục;
> nhãn đúng là nhãn ở dưới, đã đo lại 2026-09-06 (`RequiredLength = 12` ở
> `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:121`,
> `AddTop10000PasswordValidator` ở `:151`).

Trước ngày này, chính sách mật khẩu **không có một dòng nào trong `doc/`**. Nó chỉ tồn tại
trong code, kèm chú thích tự khai phạm vi áp dụng:

```bash
grep -n "ĐƠN GIẢN HOÁ" src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs
```

Chú thích đó ghi `demo/nội bộ`. Quyết định triển khai **công khai ra Internet**
([`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md) §2) làm nó thành sai
— không phải lỗi người viết, mà là một tiền đề đã thay đổi.

### Vì sao ba thứ cộng lại mới thành vấn đề

| Riêng lẻ | Cộng lại |
|---|---|
| Tên đăng nhập `SuperAdmin` cố định | Kẻ tấn công **biết sẵn một nửa bài toán** |
| Mật khẩu tối thiểu 6 ký tự, không đòi thành phần nào | Không gian ~3×10⁸ — dò vét được |
| `SuperAdmin` miễn khoá tài khoản | **Không giới hạn số lần thử** |

Ba quyết định đều có lý do tốt và đều được ghi lại. Không quyết định nào sai một mình.

### Chốt

| # | Quyết định | Vì sao |
|---|---|---|
| 1 | Độ dài tối thiểu **12**, **không** bắt buộc hoa/số/ký tự đặc biệt | NIST SP 800-63B: độ dài quan trọng hơn độ phức tạp. Bắt buộc thành phần chỉ đẻ ra `Matkhau1!` — dễ đoán hơn một cụm 12 chữ thường. 12 ký tự đưa không gian lên ~9×10¹⁶, ngoài tầm dò vét |
| 2 | **Từ chối mật khẩu nằm trong danh sách phổ biến/rò rỉ** ngay lúc đặt | Đây là thứ NIST khuyến nghị **thay cho** luật thành phần, và nó đóng đường tấn công thật: không ai dò vét 12 ký tự, người ta thử 10.000 mật khẩu phổ biến |
| 3 | Thêm **hàng rào rate limit thứ hai, phân vùng theo TÊN ĐĂNG NHẬP**, chồng lên hàng rào theo IP đang có | Chặn đúng kịch bản hàng rào theo IP bỏ lọt: tấn công **phân tán** từ hàng nghìn IP cùng nhắm một tài khoản. Xem §"Vì sao không dùng trễ luỹ tiến" bên dưới |
| 4 | **Giữ tên `SuperAdmin`** | Người dùng chốt 2026-08-31. Hợp lý sau quyết định 1: tên biết sẵn chỉ tiết kiệm nửa bài toán, mà nửa còn lại vừa thành bất khả thi |

#### Vì sao không dùng trễ luỹ tiến — phương án bị loại 2026-08-31

Bản đầu của mục này (viết 2026-08-30) chốt **trễ luỹ tiến**: mỗi lần sai làm lần thử sau chậm
hơn. Đã **loại**, vì nó có một lỗi thiết kế: chờ nghĩa là **giữ luồng xử lý**, nên chính biện
pháp phòng thủ trở thành đường làm cạn tài nguyên máy chủ — kẻ tấn công gửi hàng nghìn lần
đăng nhập sai để chiếm hết luồng, không cần đoán trúng gì cả.

Hàng rào theo tên đăng nhập đạt cùng mục tiêu mà không giữ luồng, và dùng cơ chế `RateLimiter`
đã có sẵn thay vì tự viết bộ đếm.

**Chi tiết thi công không hiển nhiên:** hàm chọn phân vùng của `RateLimiter` chạy **đồng bộ**
và chạy **trước** khi ASP.NET đọc thân request, trong khi tên đăng nhập nằm trong thân JSON.
Nên cần một middleware nhỏ đặt **trước** `UseRateLimiter`: bật đệm thân request, đọc tên đăng
nhập, cất vào `HttpContext.Items`, **trả con trỏ thân request về 0**. Quên bước cuối thì handler
đọc được thân rỗng. Đây không phải "chỉ thêm cấu hình" — nói rõ để người thi công không bất ngờ.

**Đánh đổi đã chấp nhận:** hàng rào này đưa lại một phần rủi ro tự-DoS mà việc miễn khoá đang
tránh — kẻ tấn công đốt hết hạn mức theo tên của `SuperAdmin`. Khác biệt so với khoá tài khoản:
cửa sổ trượt trả lại lượt **liên tục**, nên quản trị viên thật vẫn vào được, chỉ chậm hơn; còn
khoá thì từ chối sạch trong 15 phút.

#### Ghi chú về ngưỡng 12 ký tự — lệch chuẩn có chủ đích

[NIST SP 800-63B Rev 4](https://pages.nist.gov/800-63-4/sp800-63b.html) yêu cầu tối thiểu **15**
ký tự cho hệ thống **chỉ dùng mật khẩu** (8 ký tự chỉ áp khi có xác thực đa yếu tố). Hệ thống này
không có xác thực đa yếu tố, nên đúng chữ tiêu chuẩn là 15.

Người dùng chốt **12** ngày 2026-08-31, sau khi được cho biết dữ kiện trên. Đây là **lệch chuẩn
có cân nhắc**, không phải không biết: 12 ký tự đã đưa dò vét ra ngoài tầm, và khác biệt 12→15
là biên an toàn trước phần cứng tương lai chứ không phải ranh giới an toàn hôm nay. Ghi lại để
lần sau ai rà soát không mở lại cuộc thảo luận này từ đầu.

### Đã chốt → đã thành (không còn việc tồn đọng)

> **✅ ĐÃ XONG — đối chiếu lại 2026-09-04, đo lại 2026-09-06.** Bảng dưới đây mô tả trạng thái **trước** khi thi
> công; ba dòng đầu nay đã đóng. Giữ bảng thay vì xoá vì cột "sẽ thành" là nơi ghi *vì sao*
> chọn từng giá trị — đó vẫn là tri thức sống.
>
> Lệch được phát hiện khi `fe/08-i18n.md` §7 trỏ tới đây gọi đây là **file chủ của con số 12**:
> bảng còn ghi `6` cho một việc đã làm xong. Đây là lệch **chiều an toàn** (doc khai ít hơn
> thực tế) nhưng vẫn là tuyên bố hiện trạng sai — §4 không có ngoại lệ cho chiều an toàn.

| | Trước | Hôm nay (đo được 2026-09-04) |
|---|---|---|
| `RequiredLength` | `6` | **`12`** — `grep -n 'RequiredLength' src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs` |
| Luật thành phần | Tất cả `false` | **vẫn `false`, có chủ đích** — `RequireDigit`/`RequireNonAlphanumeric`/`RequireUppercase`/`RequireLowercase`, lý do ở mục dưới |
| Mật khẩu phổ biến | Không kiểm | **Đã kiểm** — `grep -n AddTop10000PasswordValidator src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs`. Gói `CommonPasswordsValidator` (Andrew Lock, MIT), danh sách nhúng sẵn trong assembly, chạy **hoàn toàn offline** |
| Tài khoản miễn khoá | Không có gì thay thế | ✅ **Hàng rào rate limit theo tên đăng nhập** (2026-09-01) — *không* phải trễ luỹ tiến, phương án đó đã bị loại, xem mục trên. Số đo thật (đối chiếu 2026-09-06): SlidingWindow **10 lượt / 5 phút / 5 đoạn**, `src/BE/PlatformManager.Api/Program.cs:306`–`:308`; middleware đọc tên đăng nhập ở `src/BE/PlatformManager.Api/Common/LoginUserNameRateLimitMiddleware.cs` |
| `BootstrapOptions` `MinLength` | `6`, chú thích *"khớp Identity Password.RequiredLength"* | `12` — **phải sửa cùng lượt**, nếu không hai nơi lệch nhau |

Dòng cuối là bẫy dễ quên nhất: `BootstrapOptions` tự khai nó khớp với `RequiredLength`, nên
đổi một nơi mà quên nơi kia thì câu chú thích đó thành lời nói dối, và mật khẩu bootstrap 6 ký
tự vẫn qua được validation rồi bị chính Identity từ chối lúc tạo tài khoản.

### Nghiệm thu

| # | Phép thử | PASS |
|---|---|---|
| 1 | Đổi mật khẩu thành 11 ký tự | Bị từ chối |
| 2 | Đổi mật khẩu thành 12 ký tự thường, không số không hoa | **Chấp nhận** — chứng minh luật thành phần vẫn tắt có chủ đích |
| 3 | Đổi mật khẩu thành một mật khẩu phổ biến dài ≥12 | Bị từ chối |
| 4 | Đăng nhập sai `SuperAdmin` 11 lần trong 5 phút, **mỗi lần một IP khác** | Tài khoản **không bị khoá**, nhưng lần thứ 11 trả **429** — hàng rào theo tên đăng nhập chặn được tấn công phân tán mà hàng rào theo IP bỏ lọt |
| 5 | Chờ qua cửa sổ rồi đăng nhập đúng | Vào được — hàng rào tự trả lượt, không cần ai mở khoá |

Phép thử 2 và 5 quan trọng ngang phép thử 1: chúng canh cho việc siết chặt không lỡ tay biến
thành thứ chặn nhầm người dùng thật.

