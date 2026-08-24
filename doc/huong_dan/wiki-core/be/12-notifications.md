# 12. Thông báo (Notification)

> **Trạng thái: 🚧 ĐANG THI CÔNG — xác minh lại 2026-08-24, khác bản rà
> 2026-08-23 bên dưới.** Working copy hiện tại (chưa commit) đã có
> `Core.Application/Notifications/`, `Core.Infrastructure/Notifications/`
> (`SmtpNotificationSender`, `SmtpOptions`) và trọn bộ
> `Modules/DtiWeekly/.../Application/Import/` (`StartImportCommand`,
> `ImportJobRunner`, `GetImportJobStatusQuery`...) + entity `ImportJob` +
> migration `20260818090451_AddRolePermissionAndImportJob` — nghĩa là §0 và
> bảng §4 dưới đây (viết khi seam **chưa tồn tại**) không còn mô tả đúng hiện
> trạng. Chưa đối chiếu lại chi tiết implementation mới với các nguyên tắc
> §3 (outbox, idempotency, template, opt-out) — **đọc hết trước khi sửa file
> này hoặc viết thêm code**, đừng coi bảng "0 kết quả" ở §4 là còn đúng.

## 0. Vì sao gỡ seam cũ thay vì giữ lại

Trước 2026-08-21 repo có sẵn 3 file: `INotificationSender`,
`SmtpNotificationSender`, `SmtpOptions`. Đối chiếu code lúc đó:

- `grep RecurringJob` toàn `src/BE` → **0 kết quả**.
- `INotificationSender` có **0 consumer** — không handler/job nào gọi `SendAsync`.

Tức đúng là "hạ tầng chết" mà chính `doc/huong_dan/quy-uoc/be-architecture.md` cấm dựng.
Nhưng lý do gỡ **không phải** vì nó thừa vài chục dòng code. Lý do thật:

```csharp
Task SendAsync(string to, string subject, string body, CancellationToken ct);
```

Interface này **có hình dạng của email**. `to` là địa chỉ, `subject` là tiêu
đề — hai khái niệm mà thông báo trong ứng dụng, Zalo ZNS hay SMS đều không
có. Nếu nhu cầu thật hoá ra là một trong ba kênh đó thì interface phải viết
lại, **không tái dùng được dòng nào**.

Đây là điểm dễ hiểu nhầm nhất về nguyên tắc "chỉ dựng hạ tầng khi có nhu cầu
thật": vấn đề không phải là lãng phí công sức, mà là **không biết use case
thì không thiết kế đúng được trừu tượng**. Giữ một trừu tượng sai lại nguy
hiểm hơn không có gì, vì người sau sẽ cố uốn nhu cầu thật cho vừa nó.

## 1. Ngưỡng kích hoạt

Bắt đầu làm khi **Modules có nghiệp vụ thật cần thông báo** — nghĩa là có
người dùng nghiệp vụ nói rõ "khi X xảy ra, tôi cần được báo qua Y".

Không bắt đầu vì: "sớm muộn cũng cần", "hệ khác đều có", hay "tiện tay làm
luôn". Ba lý do đó chính là thứ đã đẻ ra seam vừa gỡ.

## 2. Chọn kênh — quyết định đầu tiên và khó nhất

Chọn kênh **trước**, thiết kế interface **sau**. Ngược lại là lặp lại đúng sai
lầm cũ.

| Kênh | Chi phí | Hợp khi | Không hợp khi |
| --- | --- | --- | --- |
| **In-app** (chuông trong ứng dụng) | Gần như 0 (thêm bảng + endpoint) | Người dùng vào hệ thống hằng ngày; thông báo không gấp | Cần báo khi người dùng **không** mở app |
| **Email** | 0 nếu dùng SMTP nội bộ | Báo cáo, nhắc hạn, có nội dung dài; cần lưu vết | Cần gấp — người Việt ít đọc email công việc ngoài giờ |
| **Zalo ZNS** | Từ ~200 đ/tin, rẻ hơn SMS **>40%** | Bối cảnh doanh nghiệp Việt Nam; cần chắc chắn người nhận thấy | Không có/không muốn dùng số điện thoại; cần nội dung dài |
| **SMS** | Đắt nhất | Kênh dự phòng khi ZNS thất bại | Là kênh chính — gần như luôn có lựa chọn rẻ hơn |
| **Push (web/mobile)** | 0 nhưng tốn công dựng | Đã có app mobile | Chỉ có web nội bộ — không đáng |

**Bối cảnh Việt Nam đáng cân nhắc nghiêm túc:** Zalo có hơn 74 triệu người
dùng, và ZNS gửi được tới số điện thoại **kể cả khi người nhận chưa từng
tương tác với Zalo OA của tổ chức** — khác hẳn giới hạn thường thấy của các
kênh OTT. ZNS bắt buộc dùng **template được duyệt trước**, nên nếu chọn kênh
này thì phần "template hoá nội dung" ở §3.3 không còn là tuỳ chọn.

**Gợi ý thực dụng cho PlatformManager:** đây là hệ thống quản trị nội bộ,
người dùng đăng nhập thường xuyên. **In-app + email** nhiều khả năng đủ, và
rẻ hơn hẳn. Chỉ cân nhắc ZNS khi có yêu cầu nghiệp vụ thật sự cần "chắc chắn
người nhận thấy trong ngày".

## 3. Bốn thứ phải thiết kế trước khi viết dòng code đầu tiên

### 3.1 Outbox — tuyệt đối không gửi trong transaction nghiệp vụ

Đây là sai lầm phổ biến nhất, và nó hỏng theo **hai** chiều chứ không phải
một:

| Thứ tự | Chuyện xảy ra |
| --- | --- |
| Ghi DB xong → gửi thông báo lỗi | Dữ liệu đã đổi nhưng **không ai được báo** |
| Gửi thông báo xong → ghi DB lỗi (rollback) | **Đã báo một việc chưa từng xảy ra** — tệ hơn hẳn |

Không có cách nào làm cả hai nguyên tử trừ khi cả DB lẫn kênh gửi cùng hỗ trợ
distributed transaction — thứ vừa đắt vừa hiếm.

**Cách đúng:** trong cùng transaction nghiệp vụ, chỉ ghi **một dòng vào bảng
outbox**. Một tiến trình riêng đọc bảng đó rồi mới gửi thật. Nhờ vậy "có thay
đổi" và "có ý định thông báo" luôn cùng thành công hoặc cùng thất bại.

> `be/05-cross-module-consistency.md` đã trình bày Outbox cho integration
> event — **đọc file đó trước**, đừng dựng cơ chế outbox thứ hai song song.
> Thông báo chỉ là một loại consumer của chính cơ chế đó.

### 3.2 Idempotency & retry — job nền sẽ chạy lại, đó là thiết kế

Hangfire tự thử lại job thất bại. Không có khoá chống trùng thì một lần mạng
chập chờn là người dùng nhận **nhiều bản sao** của cùng một thông báo.

- Mỗi thông báo cần **khoá idempotency ổn định** (ví dụ
  `assessmentId + loại thông báo + kỳ`), lưu lại khi gửi thành công; gửi lại
  cùng khoá thì bỏ qua.
- Retry theo **backoff tăng dần**, có **giới hạn số lần**; hết số lần thì
  chuyển sang trạng thái thất bại cuối (dead-letter) để người vận hành xem —
  đừng thử lại vô hạn.
- Khoá phải sinh từ **dữ liệu nghiệp vụ**, không phải từ thời điểm chạy job —
  khoá theo thời gian thì lần chạy lại sinh khoá mới và mất tác dụng.

### 3.3 Template tách khỏi code

Nội dung thông báo là thứ **người không biết lập trình** sẽ muốn sửa. Nhúng
chuỗi vào code nghĩa là mỗi lần đổi câu chữ phải deploy lại.

Tách template ra, tham số hoá bằng biến. Với ZNS thì đây là **bắt buộc**
(template phải được Zalo duyệt trước khi dùng). Lưu ý phần i18n: xem
`fe/08-i18n.md` — chuỗi do BE sinh thì **BE chịu trách nhiệm dịch**, không
đẩy sang FE.

> Bổ sung 2026-08-24, đối chiếu thực hành chuẩn ngành: tách template khỏi
> code (vừa quyết ở trên) chưa tự động an toàn. Nếu template chèn thẳng dữ
> liệu người dùng nhập (ví dụ tên chỉ tiêu, ghi chú) vào HTML email mà không
> encode, đây là injection HTML — cùng bản chất với XSS phản chiếu, chỉ khác
> nơi hiển thị là hộp thư/thông báo thay vì trình duyệt trực tiếp. Người
> dùng nhập tên chỉ tiêu kiểu `<img src=x onerror=alert(1)>` hoặc chèn link
> giả mạo, template ráp chuỗi trực tiếp sẽ đưa nguyên văn vào email HTML gửi
> tới người khác.

**Luôn encode dữ liệu người dùng khi ráp vào template**, không tự tay nối
chuỗi:

```csharp
// SAI — ráp chuỗi trực tiếp, dữ liệu người dùng đi thẳng vào HTML
var body = $"<p>Chỉ tiêu <b>{criteriaName}</b> đã quá hạn.</p>";

// ĐÚNG — encode trước khi chèn, hoặc dùng template engine tự động escape
var body = $"<p>Chỉ tiêu <b>{HtmlEncoder.Default.Encode(criteriaName)}</b> đã quá hạn.</p>";
```

Nếu dùng template engine thật (Scriban, RazorLight — khuyến nghị hơn ráp
chuỗi tay khi số lượng template tăng), ưu tiên engine **auto-escape theo mặc
định** (Razor `@Model.CriteriaName` tự encode; Scriban cần bật tường minh) —
đừng chọn engine bắt tự nhớ encode ở từng chỗ chèn, vì kiểu lỗi này không lộ
ra khi test tay bằng dữ liệu sạch, chỉ lộ khi có dữ liệu thật độc hại.

Kênh **in-app** rủi ro cao hơn email: nếu FE (Angular) render nội dung thông
báo bằng `[innerHTML]` hoặc `bypassSecurityTrustHtml`, dữ liệu độc hại chạy
được JavaScript thật trong phiên người dùng đang đăng nhập — khác hẳn email
(phần lớn mail client không thực thi script trong HTML). Angular tự sanitize
theo mặc định khi bind `[innerHTML]`; **tuyệt đối không** dùng
`bypassSecurityTrustHtml` cho nội dung thông báo có nguồn gốc từ dữ liệu
người dùng nhập.

### 3.4 Tuỳ chọn người nhận + lưu vết

- **Opt-out:** phải có đường tắt nhận. Thông báo không tắt được sẽ bị người
  dùng lọc bỏ toàn bộ — kể cả cái quan trọng.
- **Gộp (digest):** nhắc hạn hằng ngày cho 50 chỉ tiêu nên là **một** thông
  báo tổng hợp, không phải 50 cái rời.
- **Lưu vết:** bảng lịch sử "đã gửi gì, cho ai, lúc nào, kết quả ra sao" —
  vừa để tra cứu khi người dùng nói "tôi không nhận được", vừa là nơi tự
  nhiên để đặt khoá idempotency ở §3.2.

> Bổ sung 2026-08-24, đối chiếu thực hành chuẩn ngành: digest ở trên gộp
> đúng **một** loại thông báo định kỳ có sẵn (nhắc hạn hằng ngày, một job,
> một lần quét). Nó không chặn được trường hợp **nhiều loại sự kiện khác
> nhau**, từ nhiều handler độc lập, cùng dội vào 1 người nhận trong thời
> gian ngắn — ví dụ user được gán vào 5 chỉ tiêu khác nhau trong 1 phút, mỗi
> lần gán là 1 command riêng, không đi qua job digest nào để gộp. Cần thêm
> một lớp chặn ở tầng gửi, độc lập với digest:

- Đặt **ngưỡng cứng theo (người nhận, kênh)** trong 1 cửa sổ thời gian — ví
  dụ tối đa **20 email/giờ/người** — dùng chính bảng lưu vết ở trên để đếm
  số đã gửi trong cửa sổ trước khi gửi tiếp:

```csharp
var sentInLastHour = await _db.NotificationHistory
    .CountAsync(n => n.RecipientId == recipientId
        && n.Channel == NotificationChannel.Email
        && n.SentAt > DateTime.UtcNow.AddHours(-1));

if (sentInLastHour >= 20)
{
    // Không gửi ngay — đẩy vào hàng chờ digest kỳ tiếp theo, không huỷ bỏ
    await _db.PendingDigestItems.AddAsync(new PendingDigestItem(recipientId, notification));
    return;
}
```

- Ngưỡng này là **lưới an toàn cho lỗi/tăng đột biến** (bug gọi lặp, import
  hàng loạt kích hoạt hàng trăm event), không phải cơ chế thay thế digest —
  vận hành bình thường không nên chạm ngưỡng; chạm thường xuyên là dấu hiệu
  cần thêm digest cho đúng loại sự kiện đó, không phải nâng ngưỡng lên.

### 3.5 Bounce — khác Opt-out, hệ thống phải tự phát hiện chứ không đợi người dùng báo

> Bổ sung 2026-08-24, đối chiếu thực hành gửi email chuẩn ngành: Opt-out ở
> trên xử lý trường hợp người dùng **chủ động** từ chối nhận. Bounce là
> trường hợp ngược lại — địa chỉ **không còn nhận được** (gõ sai, hộp thư đã
> đóng, domain hết tồn tại) nhưng không ai chủ động báo. Thiếu cơ chế này,
> hệ thống cứ gửi mãi vào một địa chỉ chết, tốn quota SMTP và (nếu dùng dịch
> vụ gửi email thứ 3) kéo tụt uy tín domain gửi (sender reputation) — ảnh
> hưởng lây sang cả việc gửi cho những địa chỉ còn sống.

Cùng một dạng suy biến với `AppUser.Email = null` đã cảnh báo ở §4 — chỉ
khác là ở đây địa chỉ **có giá trị nhưng không còn hợp lệ**, nên không lọc
được bằng kiểm tra `null` đơn giản, phải phát hiện dần từ kết quả gửi thật.

- **Hard bounce** (địa chỉ không tồn tại, domain sai) — dừng gửi **ngay từ
  lần đầu phát hiện**, không retry.
- **Soft bounce** (hộp thư đầy, server tạm thời từ chối) — cho phép thử lại
  vài lần (dùng lại backoff đã bàn ở §3.2), nhưng vẫn thất bại liên tục sau
  ngưỡng thì coi như hard bounce, chuyển sang danh sách chặn.

```csharp
public interface IBounceHandler
{
    Task MarkSuppressedAsync(string email, BounceReason reason, CancellationToken ct);
}

// Trước khi gửi bất kỳ thông báo nào — kiểm danh sách chặn trước, không đợi lỗi
if (await _suppressionList.IsSuppressedAsync(recipient.Email, ct))
{
    _logger.LogInformation("Bỏ qua gửi tới {Email} — đã trong danh sách bounce", recipient.Email);
    return;
}
```

Cách phát hiện bounce phụ thuộc kênh gửi:

- **SMTP tự host** (không qua dịch vụ ngoài) — bounce thường trả về
  **muộn**, qua 1 email lỗi gửi tới hộp `From`, không đồng bộ ngay lúc gọi
  hàm gửi. Tối thiểu, bắt `SmtpException` với mã lỗi 5xx (permanent failure)
  ngay tại điểm gửi để phát hiện được phần bounce tức thời.
- **Dịch vụ gửi email thứ 3** (SendGrid, AWS SES, Mailgun...) — có webhook
  bounce/complaint riêng, đáng tin cậy hơn hẳn tự parse SMTP response; nếu
  hệ thống chuyển sang dùng dịch vụ ngoài, đăng ký webhook đó thay vì tự dò.

## 4. Những mảnh dự kiến tái dùng — kiểm tồn tại TRƯỚC khi dựa vào

> ### ⚠️ Bảng rà 2026-08-23 đã lạc hậu — rà lại 2026-08-24
>
> Bảng 2026-08-23 (giữ bên dưới để biết lịch sử) kết luận cả 3 mảnh dưới đều
> **chưa có**. Rà lại trên working copy 2026-08-24 (chưa commit) cho kết quả
> khác hẳn — cả 3 mảnh coi là "chưa có" trước đây **giờ đã tồn tại thật**:
>
> | Mảnh | Kiểm 2026-08-23 (lạc hậu) | Kiểm 2026-08-24 |
> | --- | --- | --- |
> | Hangfire | `grep -r Hangfire src/BE` = 0 | Cấu hình đầy đủ trong `Program.cs` (xem bảng dưới) |
> | `StartImportCommand`, `ImportJobRunner`, `GetImportJobStatusQuery`, bảng `ImportJobs` | cả 4 = 0 | Cả 4 **tồn tại thật** trong `Modules/DtiWeekly/.../Application/Import/` + migration `20260818090451_AddRolePermissionAndImportJob` — chưa xác minh `ImportController` đã trả 202 hay còn đồng bộ |
> | `AppUser` | có thật | vẫn có thật |
>
> Vẫn giữ nguyên tắc: **kiểm sự tồn tại trước khi dựa vào** thay vì tin bảng
> dưới đây theo mặt chữ — bảng dưới viết khi các mảnh này chưa tồn tại, nên
> có thể đã lệch so với implementation thật hiện tại (interface, tên field...).

| Mảnh có sẵn | Ở đâu | Dùng làm gì |
| --- | --- | --- |
| **Hangfire đã cấu hình đầy đủ** | `PlatformManager.Api/Program.cs` (`UsePostgreSqlStorage`, `AddHangfireServer`, dashboard `/hangfire` khoá cứng `Roles.SuperAdmin`) | Chạy job gửi nền + job định kỳ. **Không cần dựng gì thêm.** |
| **Khuôn mẫu job nền hoàn chỉnh** | `Modules/DtiWeekly/…/Application/Import/` — `StartImportCommand`, `ImportJobRunner`, `GetImportJobStatusQuery`, `ImportErrors`, bảng `ImportJobs` | **Bắt chước nguyên mẫu này.** Nó đã giải xong: job tự mở scope DI riêng, không có `HttpContext`, trạng thái lưu vào bảng, controller trả 202. |
| **Nguồn người nhận** | `Core.Infrastructure/Identity/AppUser` | Email/tên người nhận |

> ⚠️ **`AppUser.Email` là NULLABLE, và có đường tạo user không hề có email.**
> `UserLookupService.ResolveOrCreateByFullNameAsync` tạo user từ tên trong file
> import — những tài khoản đó `Email = null`. Job gửi thông báo **phải tự bỏ
> qua** trường hợp này, không được ném lỗi: một job nhắc hạn đổ vỡ vì một user
> thiếu email là mất **toàn bộ** lượt nhắc của mọi người.

## 5. Ba ứng viên use case có sẵn trong codebase

| # | Use case | Độ khó | Tái dùng được gì |
| --- | --- | --- | --- |
| 1 | **Nhắc `Deadline` kỳ đánh giá DTI** — job định kỳ quét hạn sắp tới | Trung bình | Hangfire recurring job; cần thêm outbox + idempotency + digest (§3.2, §3.4) |
| 2 | **Báo khi tài khoản bị khoá** — gắn vào `LockUserCommand` | Dễ nhất | Đường ghi đã có sẵn; đúng ca cần outbox vì nằm trong transaction nghiệp vụ |
| 3 | **Báo khi job import chạy xong** | Dễ | Hạ tầng job + bảng trạng thái `ImportJobs` **đã có đủ**; chỉ thêm bước gửi ở cuối `ImportJobRunner` |

**Nếu cần chọn một để bắt đầu:** ứng viên **3**. Nó đã có sẵn gần hết hạ tầng,
người nhận luôn là người vừa bấm nút (nên chắc chắn có danh tính), và không
cần job định kỳ. Làm nó trước sẽ lộ ra mọi quyết định ở §2–§3 với chi phí
thấp nhất.

## 6. Đặt file ở đâu (theo layout v3)

Theo `doc/kien-truc-core-module.md` — v3 là đích đến đã chốt, đang thi công:

| Thành phần | Project |
| --- | --- |
| Interface gửi (`INotification…`) | `Core.Application/Notifications/` |
| Implementation kênh cụ thể (SMTP/ZNS…) | `Core.Infrastructure/Notifications/` |
| Entity outbox + lịch sử gửi | `Core.Domain` + EF config ở `Core.Persistence` |
| Job/handler **nghiệp vụ** kích hoạt thông báo | `Business.Application` / `Business.Infrastructure` |

Ranh giới quan trọng: **`Core.*` không được biết về nghiệp vụ.** Job "nhắc
`Deadline` của `CriteriaAssessment`" là **nghiệp vụ** → thuộc `Business.*`,
chỉ gọi xuống interface ở `Core.Application`. Đặt nhầm nó vào `Core` là vi
phạm luật đã có ArchTest canh.

## 7. Bẫy đã gặp thật — cấu hình giả vẫn qua được validation

`appsettings.json` từng chứa:

```json
"Smtp": { "Host": "localhost", "Port": 25, "FromAddress": "noreply@localhost" }
```

Giá trị giả này được đặt vào **cốt để `ValidateOnStart()` không chặn app khởi
động**. Hôm đó vô hại vì không ai gửi gì. Nhưng cái bẫy nằm ở chỗ: cấu hình
giả **vẫn qua được validation** — `[Required]` chỉ kiểm "có giá trị", không
kiểm "giá trị đúng". Ngày có người thêm consumer, app khởi động bình thường,
rồi thất bại **âm thầm** đúng lúc gửi thật.

Cách tránh khi dựng lại:

- **Đừng** đặt giá trị giả cho qua validation. Thà để app không khởi động
  được ở môi trường chưa cấu hình — đó chính là điều `ValidateOnStart()` sinh
  ra để làm.
- Nếu buộc phải có mặc định cho môi trường dev, dùng giá trị **không thể nhầm
  là thật** và ghi rõ trong tên (ví dụ một sink ghi ra file/log thay vì gửi
  đi), để nhìn là biết ngay chưa cấu hình.
- Thêm một health check cho kênh gửi (xem `be/07-observability.md`) — cấu
  hình sai lộ ra ở `/health` chứ không đợi tới lúc có người cần nhận thông
  báo.

### Cách làm cụ thể — sink log-only / MailHog, không chỉ dừng ở ý tưởng

> Bổ sung 2026-08-24, đối chiếu thực hành chuẩn ngành: gợi ý "một sink ghi
> ra file/log" ở trên đúng hướng nhưng mới dừng ở ý tưởng. Cụ thể hoá thành
> 2 lựa chọn thật, chọn theo môi trường — cả hai đều tránh lặp lại đúng bẫy
> vừa nêu (cấu hình giả trông như thật, chỉ vỡ lúc gửi thật):

- **Development (chạy tay trên máy dev):** trỏ SMTP tới
  [MailHog](https://github.com/mailhog/MailHog) (hoặc
  [Mailpit](https://github.com/axllent/mailpit) — bản kế thừa đang được duy
  trì tích cực hơn) chạy local qua Docker. Nó nhận mọi email gửi tới, có UI
  web xem nội dung, **không** gửi ra Internet thật — dù code chạy y hệt
  luồng production, không cần rẽ nhánh `if (isDev)` nào trong code gửi:

```yaml
# docker-compose.yml — chỉ cho dev
mailhog:
  image: mailhog/mailhog
  ports:
    - "1025:1025"   # SMTP giả — trỏ SmtpOptions.Host/Port vào đây khi Development
    - "8025:8025"   # UI xem email đã "gửi"
```

- **Integration test (chạy tự động, không có Docker/mạng):** đăng ký 1
  implementation khác hẳn của interface gửi, chỉ ghi vào danh sách in-memory
  để test assert, không chạm mạng — override đúng registration mà production
  dùng, qua `WebApplicationFactory` hoặc fixture DI riêng của test:

```csharp
// Test fixture — override đúng registration production dùng
services.AddSingleton<INotificationSender, FakeNotificationSender>();

public class FakeNotificationSender : INotificationSender
{
    public List<SentNotification> Sent { get; } = new();
    public Task SendAsync(SentNotification n, CancellationToken ct)
    {
        Sent.Add(n);   // Test assert trên danh sách này, không có gì rời khỏi process
        return Task.CompletedTask;
    }
}
```

MailHog/Mailpit là "giá trị không thể nhầm là thật" ở mức hạ tầng (không
cấu hình thật, luôn thấy ngay qua UI); fake sender ở integration test tránh
việc test vô tình gửi email thật ra ngoài khi có người quên đổi cấu hình.

## 8. Vẫn hoãn — chưa tới ngưỡng

- **Nhiều kênh cùng lúc + tự động chuyển kênh dự phòng** (ZNS lỗi → SMS). Chỉ
  làm khi đã chạy thật ≥2 kênh và đo được tỉ lệ thất bại.
- **Trung tâm tuỳ chọn thông báo cho người dùng tự cấu hình.** Bắt đầu bằng
  một cờ bật/tắt cho mỗi loại là đủ.
- **Message broker riêng** (RabbitMQ/Kafka). Hệ thống đang chạy 1 process,
  Hangfire + Postgres đã đủ — xem `be/11-performance-caching.md` §4.4 cho
  cùng lập luận đã áp dụng với cache.

---

## Nguồn tham khảo

- [Transactional outbox pattern — AWS Prescriptive Guidance](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/transactional-outbox.html)
- [Pattern: Transactional outbox — microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- [Implementing the Outbox Pattern — Milan Jovanović](https://milanjovanovic.tech/blog/implementing-the-outbox-pattern)
- [Zalo ZNS là gì? Hướng dẫn triển khai A-Z](https://interits.com/zalo-zns-la-gi-huong-dan-trien-khai-a-z/)
- [Bảng giá ZNS (Zalo Notification Service) mới nhất 2026](https://www.smsthuonghieu.com/gia-zns/)
- [Zalo ZNS Template — VietGuys](https://www.vietguys.biz/vi/service/ott/zalo-zns-template)
