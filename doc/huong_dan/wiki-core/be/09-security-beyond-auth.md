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
- **Security response header** — checklist tối thiểu OWASP cho mọi response, chi phí gần bằng 0, hay bị quên vì không gây lỗi rõ ràng nếu thiếu:
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
