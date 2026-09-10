---
kind: luat
scope: core
verified: 2026-09-06
---

# 17. Phục vụ & triển khai FE — từ `dist/` tới trình duyệt người dùng

> Chặng đường từ **artifact build xong** tới **trang chạy trên máy người dùng**.
> Trước 2026-08-27 chặng này **không có một dòng nào** trong toàn bộ `doc/` —
> hai file khác đã giả định nó tồn tại: [14-security.md](14-security.md) §2 nói
> *"header do BE phát"*, và [01-core-components.md](01-core-components.md) #17
> nói *"file thật trên server"*. Không file nào định nghĩa **server đó là gì**.
>
> Không thuộc file này: `browserslist`, nhịp nâng cấp —
> [16-nen-tang-va-nang-cap.md](16-nen-tang-va-nang-cap.md). Cây thư mục nguồn —
> [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md).

## 1. Hiện trạng — 🚧 GẦN NHƯ CHƯA CÓ GÌ (đối chiếu lại 2026-09-06)

```bash
grep -c "UseStaticFiles\|MapFallbackToFile\|UseSpa" src/BE/PlatformManager.Api/Program.cs
find . -maxdepth 3 \( -name "Dockerfile*" -o -name "nginx*.conf" -o -name "web.config" \) -not -path "*/node_modules/*"
grep -c "baseHref\|outputPath" src/FE/angular.json
```

Kết quả đối chiếu **2026-09-06**:

| Câu hỏi | Trả lời hôm nay |
|---|---|
| BE có phục vụ file tĩnh của FE không? | **Không** — `Program.cs` không có middleware nào. Đúng theo mô hình B đã chốt ở §2 |
| Có file hạ tầng nào không? | ✅ **Có `nginx.conf` ở gốc repo** (mới) — xem ghi chú lật ngay dưới. Vẫn **không** Dockerfile, không `web.config` |
| `angular.json` khai `baseHref`/`outputPath`? | **Không** — dùng mặc định, output vào `dist/platform-manager/` |

> 🔄 LẬT 2026-09-06 — dòng *"Có file hạ tầng nào không? **Không** — không Dockerfile, không
> nginx.conf, không web.config"* đã hết đúng. **`nginx.conf` tồn tại ở gốc repo.** Nó phủ đúng
> ba việc mà §6.1 xếp là "sẽ thành": ép HTTPS + HSTS, ba security header
> (`X-Content-Type-Options` / `X-Frame-Options` / `Referrer-Policy`), và chặn `/health` từ bên
> ngoài — kèm `proxy_set_header X-Forwarded-Proto` / `X-Forwarded-For` cho nửa còn lại của §6.3.
>
> ⚠️ **Đó là file MẪU, không phải cấu hình đang chạy:** `server_name` là `api.example.com`,
> đường dẫn chứng chỉ là chỗ giữ chỗ. Nó thuộc nhóm việc §6.5 *"Domain thật — chưa có"*. Có file
> để review như code là bước tiến thật; nhưng có file mẫu **không** đồng nghĩa hạ tầng đã dựng.

Một dòng thứ tư tìm ra ngày 2026-08-30 vẫn giữ nguyên, xem §6.1: `appsettings.json` không khai
khoá `Cors` nào, nên ở Production allowlist CORS là **rỗng**.

Trên máy dev việc này không lộ ra: `ng serve` tự phục vụ và tự lo SPA fallback.
Nó chỉ lộ ra ở lần triển khai thật đầu tiên. Bản trước của dòng này nói mốc đó
đến **sớm hơn thế** vì quyết định i18n cần định tuyến theo ngôn ngữ; chốt
2026-09-03 gỡ đúng lý do ấy (§3), nên mốc lộ ra lùi về chỗ cũ — lần deploy thật
đầu tiên, không sớm hơn.

## 2. Chọn mô hình phục vụ — quyết định phải đứng đầu

| | **A — BE phục vụ luôn** | **B — web server riêng** |
|---|---|---|
| Cách làm | `UseStaticFiles` + `MapFallbackToFile` trong `Program.cs` | nginx/IIS đứng trước, proxy `/api` về BE |
| Origin | **Cùng origin** — FE và API chung host | Khác origin, hoặc cùng origin qua proxy |
| CORS | **Không cần** | Cần, trừ khi proxy |
| CSRF/cookie | Đơn giản nhất — cookie same-site tự nhiên | Phải cấu hình đúng, xem [07-auth-identity.md](07-auth-identity.md) |
| Số thành phần vận hành | 1 | 2 |
| Đặt security header (§5) | Trong `Program.cs` | Trong cấu hình web server |

**Khuyến nghị: A**, cho quy mô hiện tại. Hệ thống đang chạy **1 process**
(cùng lý do đã chọn in-memory cache thay Redis ở
[`../be/11-performance-caching.md`](../be/11-performance-caching.md)); thêm một
web server nữa là thêm một thứ phải cấu hình đúng, mà lợi ích chỉ xuất hiện khi
có nhiều instance hoặc nhiều app dùng chung một cổng vào.

Quan trọng hơn: mô hình A làm **cùng origin**, nên toàn bộ phần CORS + cookie
`SameSite` mà [07-auth-identity.md](07-auth-identity.md) đang phải cẩn thận trở
thành không cần thiết. Đó là **bớt một lớp cấu hình sai được**, không chỉ là
bớt một tiến trình.

Đổi sang B khi: có ≥2 instance cần load balancer, hoặc cần đặt CDN trước FE.

### Chốt 2026-08-30 — **mô hình B**, FE khác origin

Khuyến nghị ở trên là **A**; quyết định của product owner là **B**. Giữ nguyên cả
hai đoạn để người đọc sau biết đây là lựa chọn có cân nhắc, không phải doc quên
cập nhật.

| Câu hỏi | Chốt |
|---|---|
| Ai đứng trước Kestrel? | **nginx**, chạy **cùng máy** — nói chuyện qua loopback |
| Trước nginx còn gì? | Chưa xác định. Xử lý ở nginx, **không** đổi cấu hình app — xem §6 |
| nginx phục vụ luôn `dist/` của FE? | **Không** — chỉ proxy `/api`. FE ở origin khác |
| CORS | **Bắt buộc** |
| API mở ra Internet công cộng? | **Có** |

Hệ quả trực tiếp: đoạn *"mô hình A làm cùng origin nên toàn bộ phần CORS + cookie
`SameSite` trở thành không cần thiết"* ở ngay trên **không áp dụng cho dự án này**.
Cookie phiên và cookie CSRF đều phải đi cross-site, nên mọi điều kiện ở
[07-auth-identity.md](07-auth-identity.md) là bắt buộc đúng, không phải tuỳ chọn.

Lựa chọn B cũng dời ba việc ra khỏi `Program.cs`: ép HTTPS, HSTS và security
header nay do nginx phát (§5), còn app nhận thêm một việc mới mà mô hình A không
có — đọc `X-Forwarded-*`. Chi tiết ở §6.

## 3. Định tuyến theo ngôn ngữ — KHÔNG CÒN, sau khi lật chốt i18n 2026-09-03

> **Mục này từng là mục nặng nhất của file, và nay gần như trống — có chủ đích.**
> Toàn bộ nó tồn tại **chỉ vì** chốt 2026-08-27 dịch chuỗi lúc *build*: mỗi ngôn
> ngữ một bundle riêng, nên ngôn ngữ buộc phải nằm trên URL, nên tầng phục vụ
> phải biết về ngôn ngữ. Chốt 2026-09-03 lật hướng sang dịch lúc **chạy** —
> người dùng đổi ngôn ngữ ngay trong app, mỗi người tự chọn, không tải lại
> trang. Khi ngôn ngữ rời khỏi URL thì **không còn gì để định tuyến**.
>
> Giữ lại vết này thay vì xoá sạch mục, vì hai file khác từng được viết dựa trên
> nó và người đọc cần biết vì sao chúng nhắc tới một mục nay đã rỗng.
>
> 📖 Thư viện, cú pháp đánh dấu chuỗi, kế hoạch bật: đọc
> [08-i18n.md](08-i18n.md) — file chủ, không lặp lại ở đây.

| Việc mục này từng bắt buộc | Sau chốt 2026-09-03 |
|---|---|
| `baseHref` riêng từng locale trong `angular.json` | **Bỏ** — một bundle, `baseHref` giữ mặc định |
| SPA fallback riêng cho `/vi/**` và `/en/**` | **Bỏ.** Vẫn cần **một** SPA fallback chung — nhưng đó là việc của mọi SPA, đã nằm ở §7 dòng "SPA fallback + phục vụ tĩnh", không phải việc i18n kéo theo |
| Chuyển hướng gốc `/` theo ngôn ngữ mặc định hoặc `Accept-Language` | **Bỏ** — `/` lại là đường vào duy nhất |
| Nút đổi ngôn ngữ phải tự giữ nguyên đường dẫn khi nhảy sang tiền tố locale khác | **Bỏ** — không rời trang thì không có đường dẫn nào để giữ |
| N ngôn ngữ = N bản build, N đường triển khai | **1 build, 1 đường.** Đây là phần tiết kiệm thật và nó lớn dần: dự án dùng lại CoreBase thêm ngôn ngữ thứ ba không đụng một dòng nào ở tầng phục vụ |

### 3.1 Thứ hướng runtime LẤY ĐI — `<html lang>` không còn tự đúng — ✅ ĐÃ THI CÔNG (đối chiếu 2026-09-06)

Hướng build-time cho không một thứ: mỗi bundle mang sẵn `<html lang>` của mình,
nên **WCAG 3.1.1 Language of Page** (mức A, bắt buộc theo
[15-accessibility.md](15-accessibility.md) §1) tự đạt mà không ai phải làm gì.
Hướng runtime **không** cho thứ đó. Đây là cái giá phải trả, ghi ra đây thay vì
để nó lộ ra lúc kiểm a11y.

| | Trước 2026-09-05 | Có thật hôm nay (đối chiếu 2026-09-06) |
|---|---|---|
| Giá trị `lang` của trang | Hằng số `vi` viết thẳng ở [`src/FE/src/index.html:2`](../../../../src/FE/src/index.html), không bao giờ đổi | Vẫn là `vi` **lúc tải trang** (đúng — đó là ngôn ngữ mặc định), rồi đổi theo lựa chọn của người dùng |
| Code chạm tới `documentElement.lang` | Không có dòng nào | ✅ **Đúng một chỗ ghi**, đi kèm nơi đổi ngôn ngữ: `src/FE/src/app/core/i18n/language.service.ts:100` |

🔄 LẬT 2026-09-06: bảng trên trước đây là bảng *"có thật hôm nay → sẽ thành"* của một việc chưa
làm, và ô bên trái khẳng định `grep -rn "documentElement" src/FE/src` **in rỗng**. Lệnh đó nay
in ra kết quả — chạy lại để tự kiểm:

```bash
grep -rn "documentElement.lang" src/FE/src --include=*.ts | grep -v spec
# PASS: đúng 1 dòng GHI (language.service.ts), phần còn lại là chú thích
```

Một điều bản trước không lường và đáng ghi: `<html lang>` phải được đặt **trước khi màn hình đầu
tiên được vẽ**, nên việc này nằm trong `provideCoreI18n()` chạy lúc khởi động, không đợi tới lần
người dùng bấm đổi ngôn ngữ đầu tiên.

Vì sao nó nguy hiểm hơn vẻ ngoài: quên bước này **không làm hỏng gì nhìn thấy
được**. Giao diện hiện đúng tiếng Anh, mọi phép thử thủ công qua hết, chỉ có
trình đọc màn hình đọc tiếng Anh bằng giọng tiếng Việt và trình duyệt gợi ý
dịch sai chiều. Đó cũng là lý do nó phải nằm trong bảng nghiệm thu chứ không
nằm trong lời dặn.

Nghiệm thu — nút đổi ngôn ngữ đã có (`src/FE/src/app/shared/components/language-switcher/`),
chạy tay:

| # | Phép thử | PASS |
|---|---|---|
| 1 | Đổi sang tiếng Anh rồi đọc `document.documentElement.lang` | `en` |
| 2 | Đổi ngược về tiếng Việt | `vi` |
| 3 | F5 sau khi đã chọn tiếng Anh | Vẫn `en` — chứng minh lựa chọn được nhớ, không chỉ đặt một lần lúc bấm |

### 3.2 Bộ lọc trên URL — vẫn là quy tắc đúng, nhưng KHÔNG còn do i18n ép

Bản trước của mục này nói bộ lọc phải nằm trên URL *vì* đổi ngôn ngữ sẽ reload
và state trong `signal()` sẽ mất sạch. Lý do đó **đã biến mất** cùng với reload.

Quy tắc thì vẫn còn, chỉ là nó đứng bằng chân của chính nó: F5, chia sẻ link,
nút Back — ba thứ đó vẫn xoá state trong bộ nhớ. Xem
[`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §8.
Điều **thay đổi** là mức khẩn: nó không còn là điều kiện tiên quyết để bật `en`,
nên đừng để nó chặn việc bật đa ngôn ngữ nữa (§7 đã sửa thứ tự theo).

## 4. Cache — hai loại file, hai chính sách ngược nhau

Angular gắn hash nội dung vào tên file bundle. Điều đó cho phép một chính sách
rất mạnh, nhưng chỉ khi tách đúng hai loại:

| Loại | Cache | Vì sao |
|---|---|---|
| Bundle có hash (`main-A1B2C3.js`) | **Lâu, bất biến** | Nội dung đổi thì tên đổi — không bao giờ cần làm mới |
| `index.html` | **Không cache** | Đây là file trỏ tới bundle nào; cache nó là người dùng kẹt ở bản cũ |

Đảo ngược cặp này là lỗi triển khai kinh điển: deploy xong, một phần người dùng
vẫn chạy bản cũ và **không có cách nào bảo họ thoát ra** ngoài việc hướng dẫn
xoá cache thủ công.

### 🚧 Quyết định người dùng 2026-08-31 — người đang mở tab lúc deploy

Chính sách cache ở trên lo cho người **mở trang sau khi** deploy xong. Còn người
**đang mở tab lúc deploy** thì gặp một chuỗi khác, và nó chắc chắn xảy ra với mọi
người dùng ở mọi lần deploy:

| Bước | Chuyện gì |
|---|---|
| 1 | App dùng route **tải lười**, tên file mang mã băm. Đếm bằng lệnh: `grep -rhoE "load(Children\|Component)" src/FE/src/app --include=*.routes.ts \| wc -l` |
| 2 | Deploy bản mới ⇒ mã băm đổi ⇒ tên file cũ không còn trên đĩa |
| 3 | Người đang mở tab bấm sang một màn **chưa tải** ⇒ trình duyệt xin file cũ |
| 4 | nginx không thấy file ⇒ SPA fallback trả `index.html` với **HTTP 200** |
| 5 | Trình duyệt cố chạy HTML như JavaScript ⇒ `SyntaxError: Unexpected token '<'` |
| 6 | Không có `ErrorHandler` toàn cục ⇒ màn hình chết, không một chữ giải thích |

Bước 4 là chỗ làm nó khó chẩn đoán: mã trả về là **200**, nên mọi công cụ giám sát
đều thấy bình thường. Đây cũng là lý do không thể sửa bằng cấu hình nginx — trả 404
thay vì `index.html` sẽ phá SPA fallback mà chính §2 cần.

**Chốt: `ErrorHandler` toàn cục nhận diện lỗi tải chunk, hiện thông báo kèm nút tải lại
— KHÔNG tự tải lại.**

Tự tải lại nghe mượt hơn nhưng **vứt mất dữ liệu đang nhập**, đúng thứ quy ước
[`09-forms-validation.md`](09-forms-validation.md) §"Form dirty + điều hướng đi" tồn tại
để bảo vệ. Hai quyết định cùng ngày mà ngược nhau thì quyết định yếu hơn sẽ thắng trong
lúc thi công — nên ghi rõ ở đây rằng đây là lựa chọn có cân nhắc, không phải thiếu sót.

#### Có thật hôm nay → sẽ thành

| | Có thật hôm nay (đối chiếu 2026-09-06) |
|---|---|
| `ErrorHandler` toàn cục | ✅ đăng ký ở `src/FE/src/app/app.config.ts:142` bằng **`useExisting`**; nhận diện ca chunk tại `src/FE/src/app/core/errors/chunk-load-error.ts`; cờ một chiều `newVersionAvailable` ở `src/FE/src/app/core/errors/global-error.handler.ts:26` |
| Người dùng thấy gì khi trúng ca này | ✅ Dải *"đã có phiên bản mới"* + nút tải lại, render ở `src/FE/src/app/app.html:9`, mang `role="alert"` và `no-print` |

🔄 LẬT 2026-09-06, hai chỗ:

- Trích dẫn cũ `app.config.ts:33` trỏ vào dòng khai `APP_CORE_ROUTES` — không liên quan.
  `ErrorHandler` đăng ký ở `:142`.
- Cột *"Có thật hôm nay"* còn ghi *"Màn hình chết, không thông báo"* trong khi cột "Sẽ thành"
  mô tả dải thông báo — **dải đó đã dựng xong**. Bảng hai cột kiểu này chỉ đúng trong đúng ngày
  viết ra; nay gộp về một cột hiện trạng.

🛑 **`useExisting` chứ không `useClass` — và đây là ràng buộc, không phải sở thích.** `App` inject
THẲNG `GlobalErrorHandler` để đọc signal `newVersionAvailable()`. `useClass` tạo thể hiện thứ hai,
Angular ném lỗi vào thể hiện này còn `App` nhìn thể hiện kia ⇒ **dải thông báo không bao giờ
hiện**, build xanh, không test nào đỏ. Đúng ca mà cả mục này sinh ra để chặn.

#### Nghiệm thu

Tái hiện được mà không cần deploy thật: build, chạy, mở app, **đổi tên một file chunk
trong thư mục đã build**, rồi bấm sang màn dùng chunk đó.

| # | Phép thử | PASS |
|---|---|---|
| 1 | Bấm sang màn có chunk đã bị đổi tên | Hiện thông báo có nút tải lại, **không** phải màn hình trắng |
| 2 | Bấm nút tải lại | App chạy lại bình thường |
| 3 | Gây một lỗi JavaScript khác bất kỳ | **Không** hiện thông báo "có phiên bản mới" — chứng minh nhận diện đúng ca, không bắt bừa mọi lỗi |

## 5. Security header — điểm phát nằm ở đây

[14-security.md](14-security.md) §2 định nghĩa **nội dung** CSP. File này định
nghĩa **chỗ đặt**: theo mô hình A thì header do `Program.cs` phát cho response
tĩnh; theo mô hình B thì do web server phát.

Không lặp lại danh sách header ở đây — mở 14 §2.

## 6. Cấu hình production cho mô hình B — 🚧 ĐÃ CHỐT — ĐANG THI CÔNG

Mục này ghi **quyết định triển khai**, không chép lại **luật**. Luật đã có file chủ:

> 📖 `UseForwardedHeaders` + `KnownProxies`, HSTS, security header, rate limit phân
> vùng theo IP: đọc [`../be/09-security-beyond-auth.md`](../be/09-security-beyond-auth.md)

### 6.1 Có thật hôm nay → sẽ thành

Đối chiếu source 2026-08-30. Kiểm lại bằng lệnh, đừng tin số viết trong văn xuôi:

```bash
grep -c "UseForwardedHeaders\|UseHsts\|UseHttpsRedirection" src/BE/PlatformManager.Api/Program.cs
grep -c "Cors" src/BE/PlatformManager.Api/appsettings.json
git ls-files src/BE/PlatformManager.Api/appsettings*.json   # PASS: CHỈ thấy appsettings.json
ls src/BE/PlatformManager.Api/appsettings*.json             # máy đã cấu hình: thêm .Development.json
find . -maxdepth 3 -name "nginx*.conf" -not -path "*/node_modules/*"
```

| Việc | Có thật hôm nay | Sẽ thành |
|---|---|---|
| Đọc `X-Forwarded-Proto` / `X-Forwarded-For` | ✅ **Đã có** (đối chiếu 2026-09-08) — `ForwardedHeadersOptions` dựng ở [`src/BE/PlatformManager.Api/Program.cs:522`](../../../../src/BE/PlatformManager.Api/Program.cs) (`XForwardedProto \| XForwardedFor`, `ForwardLimit = 1`), `KnownProxies` khai tường minh loopback v4+v6 ở `Program.cs:531`–`:532`, `app.UseForwardedHeaders(...)` ở `Program.cs:533` (neo đo lại 2026-09-10). 🔄 **SỬA 2026-09-08:** ô này trước ghi *"chỉ là comment ở `Program.cs:127`, không có lời gọi nào"* — dòng 127, khi đo 2026-09-08, là chú thích về module nghiệp vụ, không liên quan | `UseForwardedHeaders` đặt **đầu pipeline**, trước `app.UseExceptionHandler()` ở [`Program.cs:535`](../../../../src/BE/PlatformManager.Api/Program.cs); `KnownProxies` = loopback v4 + v6; `ForwardLimit` = 1 |
| Allowlist CORS cho domain thật | Không nguồn nào **trong repo** đặt `Cors:AllowedOrigins`: [`src/BE/PlatformManager.Api/appsettings.json`](../../../../src/BE/PlatformManager.Api/appsettings.json) không có khoá `Cors`, và không có `appsettings.Production.json`. Hình dạng + ràng buộc của allowlist kiểm ở [`src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs:48`](../../../../src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs) (`[Required]` ở `:48`, `[MinLength(1)]` ở `:50` — số cũ `:38` lệch 10 dòng từ 2026-09-08, khi chú thích lớp được viết lại); chỗ bind và fail-fast ở [`src/BE/PlatformManager.Api/Program.cs:260`](../../../../src/BE/PlatformManager.Api/Program.cs) + [`Program.cs:270`](../../../../src/BE/PlatformManager.Api/Program.cs) — `ValidateOnStart()` **chỉ** gắn ở Production. Giá trị dev là **cấu hình cục bộ từng máy**, xem ghi chú dưới bảng | Origin thật nạp từ biến môi trường / secret store — **không** commit vào git ([`../be/09-security-beyond-auth.md`](../be/09-security-beyond-auth.md) §"Quản lý secret") |
| Ép HTTPS + HSTS | Không có `UseHttpsRedirection`, không có `UseHsts` | Đặt ở **nginx**, không ở `Program.cs` — theo §5, mô hình B thì web server phát |
| Security header (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) | Không có | **nginx** — cùng lý do |
| File cấu hình nginx trong repo | ✅ **`nginx.conf` ở gốc repo** (đối chiếu 2026-09-06) — đã phủ HTTPS+HSTS, ba security header, chặn `/health`, và hai `proxy_set_header X-Forwarded-*` | Thay `server_name` + đường dẫn chứng chỉ bằng domain thật (§6.5) |
| 🚧 `/health/live`, `/health/ready`, `/health` (thêm 2026-08-31) | Mở công khai, không xác thực | nginx **từ chối từ bên ngoài**, chỉ cho mạng nội bộ gọi |

> **`appsettings.Development.json` là cấu hình cục bộ, KHÔNG phải nguồn kiểm được — sửa
> 2026-09-08.** Dòng "Allowlist CORS" ở trên trước đây neo vào **dòng 12** của
> `appsettings.Development.json` (nằm cạnh `appsettings.json` trong thư mục
> `PlatformManager.Api`). Neo đó hỏng theo **hai** cách cùng lúc, và cách thứ hai là cách
> đắt:
>
> 1. Bản trên máy dev (đối chiếu 2026-09-08) ngắn hơn 12 dòng — không có dòng đó để trích;
>    khoá `Cors` nằm gần đầu file.
> 2. Quan trọng hơn: [`src/BE/.gitignore:20`](../../../../src/BE/.gitignore) (`appsettings.*.json`)
>    **loại file này khỏi repo**. Ai clone về sẽ không có nó, nên mọi khẳng định neo vào đó
>    là khẳng định họ **không kiểm lại được** — trong khi cổng cũ vẫn cho qua, vì file tình
>    cờ có trên máy người viết. Kiểm nhanh: `git ls-files <đường-dẫn>` không in gì.
>
> Vì vậy: điều gì thuộc **luật** thì neo vào nguồn nằm trong cây source
> (`CorsPolicyOptions.cs`, `Program.cs`, và ArchTest
> [`src/BE/Tests/PlatformManager.ArchTests/OptionsValidateOnStartTests.cs`](../../../../src/BE/Tests/PlatformManager.ArchTests/OptionsValidateOnStartTests.cs)
> canh việc `[Required]` phải đi kèm `ValidateOnStart()`); điều gì thuộc **máy của bạn** thì
> nói rõ là vậy. Cụ thể ở đây: **máy mới phải tự tạo `appsettings.Development.json`** với
> chuỗi kết nối Postgres và origin dev của riêng mình — không có nó thì allowlist rỗng và
> FE không gọi được API nào. Đừng ghi credential thật vào bất kỳ file `doc/` nào.

### 6.2 Vì sao hai dòng đầu là sự cố chắc chắn, không phải rủi ro

Hai dòng đầu bảng trên **không phải** loại "có thể hỏng". Chúng hỏng ngay lần
deploy đầu, và hỏng theo kiểu khó chẩn đoán nhất — health check vẫn xanh:

- **Thiếu `UseForwardedHeaders`.** nginx cắt TLS rồi chuyển HTTP thuần vào Kestrel.
  Cookie CSRF khai `SecurePolicy = Always` ([`Program.cs:488`](../../../../src/BE/PlatformManager.Api/Program.cs))
  nên app từ chối phát nó trên kết nối nó **tưởng** là không bảo mật. `GET /api/antiforgery/token`
  trả 500, FE không bao giờ có token, không request ghi nào đi qua.
- **Allowlist CORS rỗng ở Production.** `WithOrigins()` với mảng rỗng chặn **mọi**
  origin. FE nằm ở origin khác nên không một lời gọi API nào tới được app.

Cả hai đều **không** chạm tới `/health/live` và `/health/ready` — hai endpoint đó
không cần cookie, không qua CORS. Deploy sẽ báo thành công.

### 6.3 Chia việc — ranh giới phải rõ, vì sai ở đây làm rate limit vô hiệu

Nguyên tắc: **app chỉ tin nginx trên loopback; nginx chịu trách nhiệm tìm ra IP
thật của người dùng.** Chia như vậy thì mai kia thêm CDN chỉ phải sửa nginx, không
phải sửa và deploy lại app.

nginx đặt hai header:

```nginx
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
```

App khai proxy tin cậy **tường minh** ở mức hẹp nhất có thể — loopback. Request từ
Internet không bao giờ đến từ `127.0.0.1`, nên không ai giả mạo được `X-Forwarded-For`
để tự chọn phân vùng rate limit. Đây là chỗ mà [`../be/09-security-beyond-auth.md`](../be/09-security-beyond-auth.md)
cảnh báo *"bật mà không khai `KnownProxies` thì tệ hơn không bật"*.

**Không** tăng `ForwardLimit` ở app để xử lý CDN. Nếu sau này phát hiện có CDN đứng
trước (dấu hiệu: `$remote_addr` trong log nginx là một IP lạ lặp lại), cách đúng là
thêm module `real_ip` vào nginx với dải IP của nhà cung cấp — vẫn giữ app ở
`ForwardLimit = 1`.

### 6.3b Health check — nội bộ, không công bố (chốt 2026-08-31)

Ba endpoint health không yêu cầu xác thực và trả về đúng một chuỗi trạng thái, nên
**rò rỉ rất ít** — người gọi biết app còn sống và database có kết nối được hay không.

Cách xử lý chuẩn không phải thêm xác thực vào chúng: health check là để load balancer
và công cụ giám sát gọi từ **mạng nội bộ**, và bắt chúng đăng nhập sẽ làm hỏng đúng
việc chúng sinh ra để làm. Chặn ở nginx, giữ code nguyên vẹn.

Không rate-limit chúng ở app — quy ước đó đã có và giữ nguyên
([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §"Rate limiting"):
giám sát gọi thường xuyên, chặn ở đó chỉ sinh báo động giả.

### 6.4 Nghiệm thu — ba phép thử, chạy sau khi deploy

Không phép thử nào trong ba phép này chạy được ở máy dev, vì máy dev không có proxy.

| # | Phép thử | PASS |
|---|---|---|
| 1 | `GET /api/antiforgery/token` | **200**, không phải 500 |
| 2 | Đăng nhập từ FE ở origin thật | Vào được — chứng minh CORS + cookie cross-site cùng đúng |
| 3 | `POST /api/auth/login` sai mật khẩu 6 lần liên tiếp | Lần thứ 6 trả **429** |

Phép thử 3 là phép thử quan trọng nhất và dễ bỏ qua nhất: nó phân biệt *"vá đúng"*
với *"vá xong nhưng đã vô hiệu hoá rate limit"*. Hạn mức `login` là 5 lượt/phút
phân vùng theo IP ([`Program.cs:332`](../../../../src/BE/PlatformManager.Api/Program.cs));
nếu `KnownProxies` khai sai thì mọi người dùng gộp về một phân vùng, hoặc kẻ tấn công
tự chọn phân vùng bằng header giả — cả hai trường hợp phép thử 3 đều **không** trả 429.

### 6.5 Việc chưa chốt

| Câu hỏi | Cần gì để chốt |
|---|---|
| Domain thật của FE và của API | Chưa có — quyết định lúc dựng hạ tầng |
| Có CDN/load balancer trước nginx không | Đọc `$remote_addr` trong log nginx sau lần deploy đầu (§6.3) |

## 7. Áp dụng vào PlatformManager

| Việc | Thứ tự | Vì sao thứ tự đó |
|---|---|---|
| ~~Chốt mô hình A hay B (§2)~~ | — | **Xong 2026-08-30: mô hình B.** Việc này phải đứng đầu vì mọi việc dưới đây có hình dạng khác nhau tuỳ mô hình |
| `UseForwardedHeaders` (§6.1) | 1 | Thiếu nó thì mọi request ghi trả 500 ngay lần deploy đầu, trong khi health check vẫn xanh (§6.2) |
| Allowlist CORS cho domain thật (§6.1) | 2 | Cùng lý do — hôm nay allowlist Production đang rỗng, không lời gọi API nào tới được app |
| ~~Ép HTTPS + HSTS + security header, **trong nginx** (§6.1)~~ | ✅ có trong `nginx.conf` (2026-09-06) | Mô hình B chuyển ba việc này ra khỏi `Program.cs` (§5). Còn lại: thay domain mẫu bằng domain thật |
| SPA fallback + phục vụ tĩnh | 3 | Không có nó thì F5 ở route con là 404. **Mô hình B: việc của nginx**, không phải `Program.cs`. `nginx.conf` hiện chỉ proxy `/api` — **chưa** có khối phục vụ `dist/` của FE |
| ~~Cập nhật `<html lang>` khi đổi ngôn ngữ (§3.1)~~ | ✅ xong (2026-09-06) | `language.service.ts:100` |
| Cache header (§4) | 4 | Sai ở đây chỉ lộ ra ở lần deploy **thứ hai** |
| CSP (§5 → 14 §2) | 5 | Sau khi đường phục vụ đã ổn định. `nginx.conf` đã có ba header khác nhưng **chưa** có `Content-Security-Policy` |

Bốn việc đầu **không phụ thuộc i18n** — chúng đang thiếu từ trước, và sẽ chặn
lần triển khai thật đầu tiên dù có đa ngôn ngữ hay không.

> **Đổi thứ tự 2026-08-30.** Bản trước xếp *"SPA fallback + phục vụ tĩnh"* ở vị
> trí 2 và không có dòng nào cho `UseForwardedHeaders` hay CORS — vì lúc đó bảng
> được viết theo giả định mô hình A. Chốt B làm hai thứ vắng mặt đó thành hai sự
> cố chắc chắn (§6.2), còn SPA fallback thì đổi chủ sang nginx.

> **Rút bảng 2026-09-03.** Hai dòng biến mất khỏi bảng này, cả hai vì chốt i18n
> ngày 2026-09-03 (§3): *"`baseHref` + fallback theo locale"* mất hẳn lý do tồn
> tại, còn *"đưa bộ lọc lên URL"* thì rời khỏi đường tới hạn của việc triển khai
> — nó vẫn là quy tắc đúng, chỉ không còn là điều kiện tiên quyết để bật `en`
> (§3.2), nên nó thuộc [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §8
> chứ không thuộc bảng này. Vào chỗ chúng là một việc **mới**: `<html lang>`,
> thứ hướng build-time cho không mà hướng runtime bắt phải tự làm.
