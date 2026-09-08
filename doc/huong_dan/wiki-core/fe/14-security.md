---
kind: luat
scope: core
verified: 2026-09-06
---

# 14. Bảo mật phía FE — render nội dung, CSP, secret, dependency

> **Nửa còn lại của [07-auth-identity.md](07-auth-identity.md).** File đó phủ
> *"ai đang đăng nhập và làm sao giữ phiên an toàn"* — cookie `HttpOnly`, CSRF,
> 401 giữa phiên. File này phủ **mọi thứ còn lại**: hiển thị nội dung không tin
> cậy, CSP, secret lọt vào bundle, dependency.
>
> Không lặp lại cookie/CSRF ở đây. Đối ứng phía BE:
> [`../be/09-security-beyond-auth.md`](../be/09-security-beyond-auth.md).

## 1. Render HTML không tin cậy — `bypassSecurityTrustHtml` KHÔNG phải "sanitize"

Đây là mục quan trọng nhất của file, vì tên hàm mà `DomSanitizer` của Angular
cung cấp gây hiểu nhầm theo đúng chiều nguy hiểm — nó nằm trong một service tên
là *Sanitizer*, nhưng phương thức `bypassSecurityTrust*` làm điều ngược lại.

| Hàm | Nó làm gì |
|---|---|
| Interpolation `{{ x }}` | Escape toàn bộ — an toàn mặc định |
| `[innerHTML]="x"` | **Angular tự sanitize** — gỡ `<script>`, `on*=`, `javascript:` |
| `bypassSecurityTrustHtml(x)` | **TẮT sanitize.** Chuỗi đi thẳng vào DOM nguyên vẹn |

`bypassSecurityTrust*` không làm sạch gì cả — nó là lời hứa của lập trình viên
rằng chuỗi này *đã* sạch. Gọi nó trên dữ liệu chưa kiểm là tự tay mở XSS.

**Quy tắc:**

1. Mặc định dùng interpolation. Cần HTML thật thì dùng `[innerHTML]` **trần** —
   để Angular sanitize.
2. `bypassSecurityTrust*` chỉ được dùng khi **cả hai** điều kiện đúng: nội dung
   do hệ thống mình sinh ra (không có đoạn nào người dùng nhập vào), **và** cần
   thẻ mà sanitizer của Angular gỡ mất.
3. Mỗi lần gọi `bypassSecurityTrust*` phải kèm chú thích trả lời được: *nội dung
   này do ai sinh, người dùng có chèn được gì vào không.* Không trả lời được thì
   không được dùng.
4. **Không bao giờ** viết chú thích gọi nó là "sanitize".

> **🗄️ Ca đã khép 2026-08-29 — cả hai file đã xoá cùng module DtiWeekly. Giữ lại
> vì bài học không phụ thuộc vào file.**
>
> Đối chiếu toàn tuyến 2026-08-27 kết luận: KHÔNG có lỗ hổng đang mở, nhưng lý do
> an toàn được ghi sai.
>
> `report-dialog.ts` đưa HTML do BE sinh qua `bypassSecurityTrustHtml`. Truy ngược
> tới nơi dựng chuỗi (`AggregationService`): **mọi trường dữ liệu người dùng đều đi
> qua `WebUtility.HtmlEncode`** — tiêu đề mục, mã tiêu chí, tên tiêu chí. Các lời
> gọi `sb.Append` không escape đều là chuỗi HTML cứng do lập trình viên viết. Số
> liệu format từ `decimal`, không phải chuỗi. **Đường XSS đóng.**
>
> Vấn đề nằm ở chú thích dòng 20 của `report-dialog.ts`, sai **hai** chỗ:
>
> 1. Viết *"Sanitize qua `DomSanitizer.bypassSecurityTrustHtml`"* — ngược với
>    thứ hàm đó làm.
> 2. Nói *"không có input người dùng lẫn vào chuỗi này"* — **sai**. `Code` và
>    `Name` chính là dữ liệu người dùng nhập qua form tiêu chí
>    (`CreateCriteriaCommand`, chỉ validate `NotEmpty`/`MaximumLength`, không
>    cấm ký tự HTML). Chúng an toàn vì **được escape**, không phải vì vắng mặt.
>
> **Vì sao lý do sai vẫn nguy hiểm dù kết quả đúng.** Chú thích đang nói lớp
> bảo vệ nằm ở chỗ nó không nằm. Người đọc nó sẽ kết luận `HtmlEncode` bên BE là
> thừa và có thể bỏ; người thêm một trường mới vào báo cáo sẽ không biết escape
> là thứ đang gánh toàn bộ an toàn. Không có test nào bắt được nếu một lần
> `HtmlEncode` bị bỏ sót — và lúc đó lỗ hổng mở ra âm thầm.
>
> **Bài học mang sang code mới:** mỗi lần gọi `bypassSecurityTrust*`, chú thích
> phải nói đúng **nguồn** an toàn — *"an toàn vì bên sinh chuỗi escape mọi trường
> người dùng"* — chứ không nói *"không có input người dùng"* khi thực ra có. Và
> kèm một test ở BE khẳng định chuỗi người dùng chứa `<script>` bị escape trong
> HTML trả về, để kỷ luật thành ràng buộc máy kiểm được.

Kiểm bằng lệnh — mỗi kết quả phải soi được vào quy tắc 2–3:

```bash
grep -rn "bypassSecurityTrust" src/FE/src
```

## 2. CSP — lớp phòng thủ thứ hai, chỉ có tác dụng khi §1 đã đúng

CSP không sửa được lỗi ở §1; nó giới hạn thiệt hại khi §1 bị bỏ qua. Vì vậy
**không** dùng CSP làm lý do để nới §1.

Header do BE phát (FE là SPA tĩnh, không tự đặt được header). Khởi điểm hợp lý
cho một SPA Angular không dùng CDN ngoài:

```
default-src 'self';
script-src 'self';
style-src 'self' 'unsafe-inline';
img-src 'self' data:;
connect-src 'self';
frame-ancestors 'none';
```

Hai lưu ý làm hỏng CSP trong thực tế nếu không biết trước:

- **`style-src 'unsafe-inline'` hiện là bắt buộc** — Angular và PrimeNG đều đặt
  style inline lúc chạy. Gỡ nó ra sẽ vỡ giao diện. Đây là đánh đổi đã biết, chấp
  nhận được vì rủi ro của inline style thấp hơn hẳn inline script.
- **`script-src` tuyệt đối không thêm `'unsafe-inline'`** — thêm vào là CSP mất
  gần hết tác dụng chống XSS, tức là xoá luôn lý do triển khai nó.

**Ngưỡng:** đặt CSP trước lần mở cho người dùng thật ngoài đội dev, cùng lúc với
các header bảo mật khác ở BE. Bật ở chế độ report-only trước một thời gian để
tìm chỗ vỡ, rồi mới cưỡng chế.

## 3. Secret không bao giờ nằm trong bundle FE

Mọi thứ trong `src/FE/` đều đi vào bundle mà **bất kỳ ai cũng tải về và đọc
được** — kể cả `environments/environment.ts`. Không có "biến môi trường bí mật"
ở FE; khái niệm đó không tồn tại trên trình duyệt.

| Được đặt ở FE | Không bao giờ |
|---|---|
| `apiBaseUrl`, cờ `production`, feature flag công khai | API key của dịch vụ bên thứ 3, connection string, chuỗi ký, mật khẩu |

✅ **Đối chiếu 2026-09-06:** `src/FE/src/environments/` chỉ chứa đúng `production` và
`apiBaseUrl` — không secret nào. Kiểm bằng `cat src/FE/src/environments/*.ts`.

Cần gọi dịch vụ bên thứ 3 có khoá → gọi **qua BE**, khoá nằm ở BE. Quy tắc đặt
secret phía server: [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §1.

Cấu trúc `environments/` vs `public/`:
[`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md)
§"Cây thư mục cấp `src/FE/`".

## 4. Dependency — bề mặt tấn công lớn nhất mà không ai viết dòng code nào

`node_modules` của một app Angular tầm trung có hàng trăm package bắc cầu. Rủi
ro không đến từ PrimeNG mà từ tầng sâu.

*(🔄 LẬT 2026-09-06: câu trên trước đây nêu "PrimeNG hay Chart.js". `chart.js` đã gỡ khỏi
`src/FE/package.json` ngày 2026-09-04 — xem [12-charting.md](12-charting.md).)*

Tối thiểu:

```bash
cd src/FE && npm audit --omit=dev
```

**Tiêu chí:** không còn lỗ `high`/`critical` ở dependency chạy runtime. Lỗ chỉ
nằm ở `devDependencies` không đi vào bundle — ghi lại lý do bỏ qua, đừng im lặng.

Chạy khi: thêm package mới, nâng major, và định kỳ. Nhịp nâng cấp và cách xử lý
khi bản vá đòi nâng major: [16-nen-tang-va-nang-cap.md](16-nen-tang-va-nang-cap.md).

## 5. Áp dụng vào PlatformManager

| Việc | Mức | Ghi chú |
|---|---|---|
| Áp đủ quy tắc 1–4 của §1 cho lần dùng `bypassSecurityTrust*` **tiếp theo** | khi có lần dùng đầu tiên | ✅ `grep -rn "bypassSecurityTrust" src/FE/src` vẫn ra **rỗng** (đối chiếu lại 2026-09-06) — lần dùng cũ đã xoá cùng module DtiWeekly |
| `npm audit` vào quy trình (§4) | **Ngay** | Một lệnh, không cần hạ tầng |
| CSP report-only (§2) | Trước khi có user thật ngoài đội dev | Cùng lúc với header bảo mật BE |
| CSP cưỡng chế (§2) | Sau khi report-only chạy đủ lâu để hết cảnh báo giả | Bật thẳng sẽ vỡ giao diện lúc đang chạy thật |

Nội dung không thuộc file này — a11y ở [15-accessibility.md](15-accessibility.md),
cookie/CSRF ở [07-auth-identity.md](07-auth-identity.md).
