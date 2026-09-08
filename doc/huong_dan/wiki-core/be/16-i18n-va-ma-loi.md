---
kind: luat
scope: core
verified: 2026-09-05
---

# 16. i18n phía BE — BE sở hữu **MÃ**, không sở hữu **CÂU CHỮ**

> **File chủ của chủ đề "i18n phía BE"** (tạo 2026-09-03). Phía FE có file chủ
> riêng: [`../fe/08-i18n.md`](../fe/08-i18n.md). Hai file, hai phía, không chép
> nội dung của nhau.

## 0. Vì sao tạo file mới — giải trình theo `.claude/CLAUDE.md` §5

Trước hôm nay chủ đề này **không có file chủ**. Nó nằm rải ở ba chỗ, và mỗi chỗ
đều có lý do chính đáng để không phải là chủ:

| Chỗ đang giữ mảnh | Nó thật sự là chủ của cái gì | Vì sao không phải chủ của i18n BE |
| --- | --- | --- |
| `be/01-core-components.md` §Áp dụng #18 | Danh sách "core gồm những gì, còn thiếu mảng nào" | Là **bảng điểm danh**. Mỗi mục ở đó trỏ ra một file chủ; #18 lại tự giữ nội dung — đó chính là cái lệch. |
| `be/12-notifications.md` §3.3 | Kênh thông báo ra ngoài (email/ZNS/in-app) | Giữ đúng **một câu** *"chuỗi do BE sinh thì BE chịu trách nhiệm dịch"*. Câu đó đúng cho **kênh không có FE**, nhưng viết ở đó thì bị đọc thành luật chung. |
| `fe/08-i18n.md` §"Chuỗi BE trả về" | Cơ chế dịch **phía FE** | Đang mô tả việc **BE** phải làm, từ file của FE. |

Ba lựa chọn đã cân nhắc, và vì sao chọn cái thứ ba:

1. **Mở rộng `be/01-core-components.md` #18.** Bỏ, vì làm hỏng đúng thứ file đó
   sinh ra để làm: một trang trả lời *"core còn thiếu mảng nào"* trong một màn
   hình. #18 mà phình ra 200 dòng thì 17 mục còn lại bị đẩy khỏi tầm mắt.
2. **Mở rộng `quy-uoc/be-cqrs-handler.md` §ErrorDescriptor.** Bỏ, vì phạm vi
   lệch: §ErrorDescriptor là **khuôn khai báo mã lỗi**. Ranh giới sở hữu câu chữ
   giữa BE và FE, kênh email, những thứ **cố ý không làm** — không cái nào là
   quy ước viết handler.
3. **File chủ mới (chọn).** Chủ đề đủ lớn để có ranh giới rõ, và nó **cắt ngang**
   cả bốn file trên — thứ cắt ngang mà nhét vào một trong các file bị cắt thì
   ba file kia sẽ tự mọc bản sao.

**Đánh đổi thật, không giấu:** `wiki-core/be/` thêm một file nữa (thư mục này đã
dài), và người đọc phải nhảy giữa file này ↔ `be-cqrs-handler.md` ↔
`be-api-controller.md` cho một luồng lỗi duy nhất. Đổi lại: mỗi luật vẫn nằm ở
đúng một chỗ, không có bản sao để lệch nhau — chi phí của việc để bản sao đã trả
thật với recipe `RowVersion` (xem `.claude/CLAUDE.md` §3 lý do 2).

### Ranh giới sở hữu giữa các file — tra bảng này trước khi viết thêm

| Chủ đề | File chủ |
| --- | --- |
| Khuôn mã `MIEN.MA_LOI`, khai `ErrorDescriptor` tập trung | [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §ErrorDescriptor |
| Hình dạng envelope, field nào có mặt, `ErrorCode` → HTTP | [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §Envelope response |
| Cơ chế dịch, thư viện, bảng dịch phía FE | [`../fe/08-i18n.md`](../fe/08-i18n.md) |
| Kênh không có FE: template, escape, idempotency | [`12-notifications.md`](12-notifications.md) §3.3 |
| **Cơ chế** tham số trong thông điệp: nguồn giá trị, allowlist, ai sở hữu hằng số | **File này** §10 |
| Ranh giới sở hữu câu chữ BE ↔ FE, và mọi thứ còn lại | **File này** |

> ### ⚠️ Trùng lặp đã phát hiện 2026-09-03 — cần rút gọn một bên
>
> Đợt ghi tài liệu 2026-09-03 chạy **hai khu song song** (FE và BE), và cả hai
> cùng ghi ba lỗi tiên quyết phía BE. Kết quả:
> [`../fe/08-i18n.md`](../fe/08-i18n.md) hiện có §"Ba lỗi tiên quyết",
> §"KHÔNG LÀM ở BE" và bảng thứ tự thi công — **cùng nội dung** với §4, §5, §7
> của file này. Đó đúng là khuôn `.claude/CLAUDE.md` §5 cấm: hai bản sao rồi sẽ
> lệch nhau, và bản bị bỏ quên sẽ là bản ai đó đọc.
>
> **Phân xử:** ba lỗi đó là **luật của BE**, neo vào file `.cs` của BE, nghiệm
> thu bằng lệnh chạy trên `src/BE` — nên **file này giữ nội dung**, và phần
> tương ứng bên `fe/08-i18n.md` nên rút còn một dòng trỏ đường. Việc rút gọn đó
> thuộc khu FE, **chưa làm** tại thời điểm ghi dòng này.

## 1. Quyết định người dùng 2026-09-03 — "hướng A" **giữ tên, đổi nghĩa**

`be/01-core-components.md` §Áp dụng #18 chốt ngày 2026-08-27 rằng đi **hướng A —
FE dịch theo mã**. Tên hướng giữ nguyên; **cơ chế bên dưới nó bị lật**:

| | Chốt 2026-08-27 (hết hiệu lực) | Chốt 2026-09-03 (đang áp dụng) |
| --- | --- | --- |
| Đổi ngôn ngữ | Bấm nút → **tải lại trang** ở URL khác (`/vi/…` ↔ `/en/…`) | **Ngay trong app**, không tải lại, mỗi người tự chọn |
| Số bundle | 2 bundle, dịch lúc build | **1 bundle**, dịch lúc chạy |
| Ai giữ bản dịch | FE, bằng `$localize` | FE, bằng bảng dịch runtime — xem `fe/08-i18n.md` |
| BE phải làm gì | *"Không có"* | **Có** — xem §4 dưới đây |

Câu *"BE không phải dựng hạ tầng dịch nào"* ở chốt cũ vẫn **đúng** (BE vẫn không
có `.resx`, không `IStringLocalizer` — §5). Câu *"BE giữ nguyên, không phải làm
gì"* thì **sai** và đã bị lật: dưới hướng runtime, mọi câu chữ BE đẩy ra kênh có
FE đều trở thành chuỗi **không dịch được**, và ba lỗi ở §4 là ba đường rò đang mở.

### Vì sao lật — lý do nằm ở giới hạn thiết kế, không phải ở cấu hình

Đây là quyết định của người dùng. Bằng chứng kỹ thuật dưới đây không nói phương
án cũ tồi; nó nói yêu cầu mới **loại** phương án cũ:

- Tài liệu API của Angular nói thẳng về `$localize`: *"$localize messages are
  only processed once … does not provide dynamic language changing without
  refreshing the browser"*. Đây là **hạn chế thiết kế**, không phải thiếu cấu
  hình — nên yêu cầu *"đổi ngay trong app"* tự nó loại `@angular/localize`.
- Không có đường chờ: `angular.dev/roadmap` không có mục i18n nào; issue
  `angular/angular#38953` xin i18n runtime bị đóng **NOT PLANNED**, `#56318`
  đóng vì trùng. Chờ Angular ra API runtime là **hy vọng**, không phải kế hoạch.

Chi tiết chọn thư viện, so sánh các ứng viên và cạm bẫy phía client thuộc
[`../fe/08-i18n.md`](../fe/08-i18n.md) — **không lặp lại ở đây**.

### Cửa một chiều — vì sao chốt lúc này rẻ nhất

Message ID của `$localize` là **hash của chính câu nguồn**, không phải khoá ổn
định. Bỏ nhánh đó về sau nghĩa là **tự đặt khoá lại cho từng chuỗi**; công cụ
chuyển đổi duy nhất trên npm (`ngx-translate-migrate` 0.0.1, 2019) chỉ chạy
chiều ngược. Chiều runtime ↔ runtime thì rẻ.

Kiểm hiện trạng (chạy từ gốc repo) — hôm nay cả hai in `0`, nghĩa là **chi phí
chọn hôm nay bằng 0**, và cửa sổ này chỉ mở một lần:

```bash
grep -rl '\$localize' src/FE/src | wc -l
grep -rl 'i18n=' src/FE/src --include=*.html | wc -l
```

## 2. Core giữ CƠ CHẾ + KHOÁ, dự án cấp BẢNG DỊCH

Mọi dự án đều có **hai** ngôn ngữ (VN, EN), có dự án sẽ thêm. Suy ra ranh giới:

- **Core** giữ **cơ chế dịch** và **tập khoá của Core** (mã lỗi Core, nhãn menu
  Core, chuỗi khung sườn). Core **không** mang bản dịch riêng của dự án 1.
- **Dự án** cấp **bảng dịch** của mình qua seam, đúng khuôn đã dựng ba lần trong
  repo này: `ICoreMenuSeedSource`, `ICoreBootstrapAccountSource` (BE) và
  `CORE_BRANDING` / `CORE_ROUTES` (FE). Đây không phải khuôn mới phải phát minh —
  nó đã có hình dạng, có test, và có bản cài ở tầng `Api` / `app`.

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Hình dạng cụ thể của seam bảng dịch chưa chốt
và **không** chốt ở pha tài liệu này; nó thuộc bước 6 của §7.

## 3. Ai sở hữu câu chữ — bảng phân xử, tra trước khi viết bất kỳ chuỗi nào ở BE

| Kênh / loại chuỗi | Ai sở hữu câu chữ | BE trả ra cái gì |
| --- | --- | --- |
| API có FE tiêu thụ (toàn bộ `/api/**` hôm nay) | **FE** | **Mã** + **tham số có cấu trúc**. Câu tiếng Việt trong `message` tụt xuống vai trò *dev-facing + fallback* |
| Kênh **không có FE**: email, PDF, SMS/ZNS | **BE** | Chuỗi đã dựng xong — xem [`12-notifications.md`](12-notifications.md) §3.3 |
| Log, audit, thông điệp cho người vận hành | **Không ai** — chỉ một ngôn ngữ | Giữ nguyên |

### Chỗ song ngữ là **SAI**, không phải "khó"

Log và audit dịch được là log **không tra cứu được**: người vận hành `grep` một
câu và bỏ sót nửa số dòng vì nửa kia sinh ra trong phiên ngôn ngữ khác. Cùng lý
do, **mã lỗi không bao giờ được dịch** — nó là định danh, không phải câu.

**Nêu đích danh để người sau không dọn nhầm:** ba lời gọi `e.Description` trong
`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs` (đối chiếu
2026-09-05: `:218`, `:247`, `:259` — tìm lại bằng
`grep -n 'e.Description' src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs`)
là **log cho người vận hành**, **KHÔNG** phải điểm rò ra người
dùng. **Đừng đụng.** Bốn điểm rò thật liệt ở §4(c).

### Tham số có cấu trúc — vì sao không được ghép sẵn vào câu

BE ghép sẵn *"Mã chỉ tiêu 'ABC' đã tồn tại."* thì FE không tách lại được `ABC`
ra khỏi câu. Dưới hướng runtime, phần BE phải trả là **mã + tham số rời**, để FE
ráp vào câu của ngôn ngữ đang chọn. **Cơ chế** đưa tham số ra — nguồn giá trị,
allowlist khoá, ai sở hữu hằng số chính sách — chốt ở §10 của chính file này.
**Hình dạng** trường thì thuộc §Envelope response của
[`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md).

## 4. Ba lỗi tiên quyết — ✅ CẢ BA ĐÃ SỬA (đối chiếu 2026-09-03)

Ba lỗi này **không do i18n sinh ra**; i18n chỉ làm chúng thành đắt. Cả ba đều đã
đối chiếu source **2026-09-03**.

**Cả ba đã sửa xong trong ngày 2026-09-03** — (a) ở lượt đầu, (b) và (c) ở lượt
sau. Phần mô tả lỗi trong mỗi mục **giữ nguyên** vì nó là **lý do** của thiết kế
hiện tại; đừng đọc nó thành hiện trạng — hiện trạng nằm ở cột phải của bảng
*"có thật hôm nay → sẽ thành"* trong từng mục.

### (a) Hai hệ mã lỗi dùng chung một field, gate mù đúng một nửa — ✅ ĐÃ SỬA 2026-09-03

> **Trạng thái: ✅ CÓ THẬT (đối chiếu 2026-09-03).** Toàn bộ bảng *"có thật hôm
> nay → sẽ thành"* bên dưới đã đổ về cột phải. Phần mô tả lỗi giữ nguyên vì nó
> là **lý do** của thiết kế hiện tại; đừng đọc nó thành hiện trạng.
>
> **Một điểm THI CÔNG KHÁC với bản chốt, ghi rõ để không ai "sửa lại cho đúng
> doc":** bản chốt viết *"`DomainException` mang `ErrorDescriptor` thật"*. Không
> làm được đúng chữ đó — `ErrorDescriptor` thuộc `Core.Application`
> (`quy-uoc/be-architecture.md` liệt nó trong nội dung project đó), còn
> `Core.Domain` bị cấm phụ thuộc bất cứ thứ gì, luật này **cưỡng chế bằng máy** ở
> `src/BE/Tests/PlatformManager.ArchTests/LayerDependencyTests.cs:47`. Kéo
> `ErrorDescriptor` xuống Domain thì phải kéo theo `ErrorCode` — mà `ErrorCode`
> theo thiết kế *"giá trị enum CHÍNH LÀ mã HTTP"*, tức nhét HTTP vào Domain.
>
> Cái đã làm giữ **đúng mục đích** của bản chốt (compiler chặn mã sai khuôn ngay
> tại chỗ ném) mà không phá sơ đồ tầng: một bản ghi của riêng Domain,
> `src/BE/Core/PlatformManager.Core.Domain/Common/DomainError.cs:36`, mang
> `BusinessCode` + `MessageTemplate`. Nó **cố ý không mang `ErrorCode`**: mã HTTP
> đã do LOẠI EXCEPTION quyết định (`DomainException` ⇒ 422, `ConflictException` ⇒
> 409) tại `ExceptionHandlingBehavior`; để cả hai nơi cùng khai là tạo hai nguồn
> cho một sự thật, và chúng mâu thuẫn được mà vẫn biên dịch.

`ExceptionHandlingBehavior` dựng `ErrorDescriptor` **inline** từ `DomainException.Code` —
nghĩa là chuỗi lập trình viên gõ trong `throw` đi thẳng ra field `businessCode` của
envelope, **không đi qua catalog nào**.

> **Neo cố ý KHÔNG có số dòng** (sửa 2026-09-03). Đoạn trên tả **hiện trạng trước bản vá**,
> và bản vá đã xoá đúng thứ nó trỏ tới: `DomainException.Code` **không còn tồn tại** —
> `grep -rn 'public string Code' src/BE/Core/PlatformManager.Core.Domain/Common/` ra rỗng.
> Chỗ dựng descriptor hôm nay đọc `error.BusinessCode`, trong hàm
> `ExceptionHandlingBehavior.BuildErrorResponse`.
>
> Bản trước neo `…ExceptionHandlingBehavior.cs:59`. Dòng đó vẫn **nằm trong** file nên
> `check-docs.sh` mục 6 cho qua — nó chỉ kiểm số dòng có vượt độ dài file không, **không**
> kiểm dòng đó có nói đúng thứ được mô tả. Một neo trỏ vào giữa khối XML doc của bản vá,
> chống lưng cho câu mô tả cái lỗi mà bản vá đã bịt, là loại sai gate không bắt được. Vì
> vậy: **mô tả hiện trạng CŨ thì neo bằng mô tả, không neo bằng số dòng.**

Hệ quả: repo đang có **hai** hệ mã trong cùng một field. Hệ khai trong
`{Entity}Errors.cs` theo khuôn `MIEN.MA_LOI`, và hệ gõ tay trong `throw` **không
có dấu chấm**.

Gate hiện có không thấy: `ErrorCatalogTests` chỉ đọc field
`public static readonly` bằng reflection, nên descriptor dựng bằng
`new ErrorDescriptor(...)` trong thân code thì lọt. XML doc đầu file
`ErrorCatalogTests.cs` khi đó **tự thừa nhận** đúng lỗ này. Trớ trêu hơn: cùng file, ca
đối chứng `FormatPattern_Rejects_TheKnownWrongShapes` liệt `"USER_NOT_FOUND"` (thiếu dấu
chấm) là khuôn **phải từ chối** — đúng khuôn mà 7 điểm ném đang dùng thật.

> **Hai neo trong đoạn trên đã gỡ số dòng (2026-09-03), vì cả hai đã trôi.**
> `ErrorCatalogTests.cs:39` từng trỏ tới lời tự thú về cái lỗ; dòng 39 hôm nay là XML doc
> **mô tả bản vá** (*"Quét CẢ HAI kiểu bản ghi lỗi… mở rộng 2026-09-03"*) — nghĩa ngược hẳn
> với câu nó chống lưng. `ErrorCatalogTests.cs:152` từng trỏ tới chuỗi `"USER_NOT_FOUND"`;
> dòng đó nay là `/// </summary>`, chuỗi thật lùi xuống `:159`.
>
> Neo bằng **tên ca kiểm** thay vì số dòng: tên đổi thì compiler biết, số dòng đổi thì
> không ai biết. Vị trí hôm nay đọc bằng lệnh:
> `grep -n 'USER_NOT_FOUND' src/BE/Tests/PlatformManager.ArchTests/ErrorCatalogTests.cs`.

| Trước 2026-09-03 | ✅ CÓ THẬT hôm nay (đối chiếu 2026-09-03) |
| --- | --- |
| `DomainException` nhận `(string code, string message)`, `Code` là string tự do | Chỉ nhận `DomainError` + tham số: `src/BE/Core/PlatformManager.Core.Domain/Common/DomainException.cs:18`. `ConflictException` đổi theo, `src/BE/Core/PlatformManager.Core.Domain/Common/ConflictException.cs:15` — nó đẩy ra **cùng** field `businessCode`, để nguyên chữ ký chuỗi là chừa lại đúng cái lỗ vừa bịt |
| 6 mã domain không dấu chấm, 7 điểm ném | 6 mã trong 3 catalog: `src/BE/Core/PlatformManager.Core.Domain/Entities/SysMenuErrors.cs:12`, `:21`; `src/BE/Core/PlatformManager.Core.Domain/Entities/SysMenuRoleErrors.cs:13`, `:16`; `src/BE/Core/PlatformManager.Core.Domain/Entities/RolePermissionErrors.cs:8`, `:11`. Khuôn mới: `SYS_MENU.CODE_REQUIRED`, … |
| `SYS_MENU_NAME_REQUIRED` khai **trùng** ở `SysMenu.cs:27` và `SysMenu.cs:53` | Một khai báo (`SysMenuErrors.cs:21`), hai chỗ dùng: `src/BE/Core/PlatformManager.Core.Domain/Entities/SysMenu.cs:27` và `:53` |
| Không gate nào chặn dựng `DomainException` bằng string literal | 2 luật quét **mã nguồn** dùng lại `RepoSourceTree`: `src/BE/Tests/PlatformManager.ArchTests/ErrorCodeSourceTests.cs:60` (T1) và `:80` (T2), kèm 2 ca đối chứng `:104`, `:126` |
| `ErrorCatalogTests` chỉ soi `ErrorDescriptor` nên mù với mã domain | Soi **cả hai** kiểu bản ghi chung một rổ (khuôn + trùng), và có luật chặn rổ khuyết một kiểu: `src/BE/Tests/PlatformManager.ArchTests/ErrorCatalogTests.cs:224` |

**Nghiệm thu** — cả hai lệnh **PASS 2026-09-03** (không in gì); trước đó cả hai
đều có output:

```bash
grep -rho 'new DomainException("[A-Z0-9_.]*"' src/BE --include=*.cs \
     --exclude-dir=obj --exclude-dir=bin --exclude-dir=Tests | grep -v '\.'
grep -rho 'new DomainException("[A-Z0-9_.]*"' src/BE --include=*.cs \
     --exclude-dir=obj --exclude-dir=bin --exclude-dir=Tests | sort | uniq -d
```

> *(🔄 SỬA 2026-09-08: hai lệnh trên trước đây liệt kê `src/BE/Core src/BE/Modules`. Thư mục
> `src/BE/Modules/` đã xoá 2026-09-08 nên `grep` in "No such file or directory" ra **stderr**
> ở mỗi lần chạy, và — quan trọng hơn — phạm vi quét bị **đóng cứng vào tên thư mục cũ**: tầng
> nghiệp vụ dựng lại ở `src/BE/Business/` sẽ nằm ngoài tầm cả hai lệnh mà không có dấu hiệu
> nào. Quét cả `src/BE` rồi trừ `Tests`/`obj`/`bin` tự phủ mọi tầng ra đời sau. Vẫn PASS —
> không lệnh nào in gì, đối chiếu 2026-09-08.)*

**Vì sao đây là điều kiện tiên quyết cứng, không phải việc dọn dẹp để sau:** khi
i18n vào, `businessCode` **trở thành khoá dịch** — cùng một chuỗi nằm đồng thời
trong bảng dịch VI, bảng dịch EN, và mã FE tra nó. Đổi mã lúc đó là đổi một khoá
đã phát tán ra ba nơi. Đổi lúc này là sửa 7 dòng — và đó là lý do việc này chạy
trước, không phải sau.

**Nợ để ngỏ, ghi ra vì nó không tự lộ:** phép quét nguồn miễn trừ catalog bằng
**tên file** (`*Errors.cs`), không bằng cấu trúc — đặt tên file như vậy rồi viết
gì trong đó cũng qua được T2. Và phạm vi quét là `RepoSourceTree.ProductSourceFiles()`
(hôm nay Core + Api); khi tầng `Business.*` ra đời, phải thêm vào **chính hàm đó**,
đừng chép danh sách thư mục thứ hai.

**Nợ thứ hai, ghi nhận 2026-09-03 — nhãn hiển thị của permission-key.**
`ResourceKeyDefinition.DisplayName` là **câu tiếng Việt dựng sẵn ở BE**, trả thẳng ra
`GET /api/admin/permissions/resources`. Giá trị thật của dự án này khai ở
`src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs:53`.

Vì sao nó thuộc i18n chứ không chỉ là chuyện đặt chuỗi ở đâu: khi bảng dịch ra đời, nhãn
này **phải** trở thành một mã để FE tra, đúng như `businessCode`. Để nguyên thì lần bật
i18n sẽ dịch được mọi thứ **trừ** cột tên trên màn phân quyền — và hỏng **im lặng**: màn
hình vẫn hiện chữ, chỉ là sai ngôn ngữ, nên không ai báo lỗi.

Đây là **một nửa** của một nợ lớn hơn đã đóng. Nửa kia — danh mục key đóng nằm trong Core —
đã tách 2026-09-03 sang seam `ICoreResourceKeySource`, và chính lượt tách đó đưa câu tiếng
Việt này ra khỏi Core về đúng **một** chỗ ở host, nên khi i18n tới chỉ còn một điểm phải
sửa. Bối cảnh đầy đủ kèm phép đo ở
[`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md)
§"`ResourceKeys` — đã tách 2026-09-03". **Không** chép lại ở đây.

Lượt tách **cố ý** không đụng vào nửa i18n, và lý do ghi ngay tại
`src/BE/Core/PlatformManager.Core.Application/Permissions/ICoreResourceKeySource.cs:6`: đổi
kiểu của trường này là đổi hợp đồng `GET /api/admin/permissions/resources`
([`../../../contracts/permissions.md`](../../../contracts/permissions.md) CONTRACT PERM-2)
và kéo theo FE. Hai việc khác nhau thì làm thành hai lượt — gộp vào là cách một lượt tách
ranh giới biến thành một lượt sửa hợp đồng API mà không ai định trước.

Khuôn mã và luật *"mã lỗi domain cũng phải theo khuôn này"* thuộc
[`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md)
§ErrorDescriptor — **không** chép lại ở đây.

### (b) Lỗi validation không mang mã — ✅ ĐÃ SỬA 2026-09-03

> **Trạng thái: ✅ CÓ THẬT (đối chiếu 2026-09-03).** Bảng *"có thật hôm nay → sẽ
> thành"* bên dưới đã đổ về cột phải.
>
> **Một điểm thi công khác bản chốt, ghi rõ để không ai "sửa lại cho khớp doc":**
> bản chốt đo bằng `grep -c 'BusinessCode' GlobalExceptionHandler.cs` và coi
> khác `0` là PASS. Lệnh đó hôm nay in **`0`** dù việc đã xong — vì mã **không**
> được gán inline trong handler. Nó đi qua
> `ApiResult<T>.ValidationError(ValidationErrors.Failed, …)`, tức đúng chỗ dựng
> envelope 400 duy nhất. Giữ đúng chữ của bản chốt thì phải gán mã ở hai nơi
> hoặc bỏ qua factory — chọn cách đó là dựng lại đúng "hai bản dựng cho một loại
> response" mà bản sửa này vừa gộp về một. Nghiệm thu đã thay bằng phép đo đúng
> thứ cần đo (bên dưới).

Đo trước khi sửa, và phép đo đã lật ngược chỗ cần sửa:

- `ApiResult<T>.ValidationError(...)` có **0 nơi gọi**. Sửa nó không đổi hành vi của một
  request nào.
- Đường **thật** là nhánh `ValidationException` trong `GlobalExceptionHandler`, dựng
  envelope **bằng tay**; file đó khi ấy có **0 dòng** nhắc `BusinessCode`.

> **Số dòng gỡ khỏi hai gạch đầu dòng trên (2026-09-03) — cả hai neo đã trôi vì chính bản
> vá này.** `ApiResult.cs:31` từng là chỗ khai `ValidationError`; sau khi bản vá thêm XML
> doc giải thích *vì sao* hàm này từng là code chết, dòng 31 thành dòng trống và khai báo
> lùi xuống `:47`. `GlobalExceptionHandler.cs:23` từng là nhánh `ValidationException`; dòng
> 23 nay là dấu `{` của `WithTrace`, nhánh thật ở `:34` và thân xử lý ở `BuildValidationResult`
> (`:72`).
>
> Đây là hình dạng lặp lại đủ để thành luật: **bản vá thêm chú thích giải thích lý do, và
> chính chú thích đó đẩy trôi mọi neo bên dưới nó trong cùng file.** Càng giải thích kỹ,
> neo càng trôi xa. Vì vậy đoạn mô tả *hiện trạng trước khi vá* neo bằng **tên hàm**, không
> bằng số dòng; đoạn mô tả *hiện trạng sau khi vá* thì neo `file:dòng` được, vì nó sẽ được
> kiểm lại cùng lúc với lần sửa kế tiếp.

**Vì sao nó ép kiến trúc chứ không chỉ là thiếu một field:** không có mã thì FE
**không có gì để tra**, buộc phải hiển thị thẳng chữ BE trả về. Nghĩa là *"BE sở
hữu câu chữ"* trở thành mặc định **do thiếu mã**, chứ không do ai quyết định như
vậy. Một quyết định kiến trúc lớn đang được đưa ra bởi một field còn thiếu.

| Trước 2026-09-03 | ✅ CÓ THẬT hôm nay (đối chiếu 2026-09-03) |
| --- | --- |
| Nhánh `ValidationException` dựng envelope **bằng tay**, `Message` cứng tiếng Việt + `Fields` | Nhánh đó gọi một chỗ dựng duy nhất: `GlobalExceptionHandler.BuildValidationResult` |
| Không mã nào trên nhánh 400 | `businessCode` = `VALIDATION.FAILED`, khai trong catalog `ValidationErrors.Failed` |
| Lỗi từng field chỉ là chuỗi trần | Thêm trường `fieldErrors`: `src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs:41`, phần tử là record `ApiFieldError` (`Code` + `Message`) |
| — | Mã từng field lấy **khoá sẵn có của FluentValidation** (`ValidationFailure.ErrorCode` = tên validator), không đặt khoá mới: hàm `GlobalExceptionHandler.ToFieldError` |
| — | Rule `Custom`/`CustomAsync` để `ErrorCode` null (3 chỗ có thật) ⇒ mã dự phòng `ApiFieldError.UnspecifiedCode`, **không** để null ra tới client |
| `ApiResult.ValidationError` — code chết, 0 nơi gọi | Có nơi gọi thật và là chỗ dựng DUY NHẤT: `ApiResult<T>.ValidationError` |
| Không test nào chốt shape của nhánh 400 | `src/BE/Tests/PlatformManager.Core.IntegrationTests/Common/ValidationEnvelopeShapeTests.cs:31` (5 ca, có ca đối chứng) + 1 ca trên dây thật `src/BE/Tests/PlatformManager.Core.IntegrationTests/Users/UserListQueryValidationSeamTests.cs:159` |

**Di trú SONG SONG, không đổi shape một nhát:** thêm trường mới **cạnh** `fields`,
để FE chuyển sang đọc trường mới, rồi mới gỡ `fields`. Đổi shape một nhát nghĩa
là có một khoảnh khắc FE cũ gặp BE mới — và envelope là thứ **mọi** màn hình đi
qua. Bước gỡ là bước 11 của §7, **không được bỏ**.

`fields` **vẫn còn nguyên** và vẫn là thứ FE đang đọc. FE mới chỉ **nhận** trường
mới ở mức kiểu (`src/FE/src/app/core/http/api-result.model.ts:77`), chưa đọc.

Hình dạng field mới thuộc §Envelope response của
[`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md).

**Nghiệm thu** — lệnh 1 phải in **2** (nhánh 400 gọi đúng catalog, và trường mới
có mặt trong hợp đồng envelope); lệnh 2 là **ca đối chứng** cho việc `fields`
chưa bị gỡ sớm, phải in khác `0`:

```bash
grep -c 'ValidationErrors.Failed\|FieldErrors' src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs
grep -c 'Fields' src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs
```

> **Vì sao không giữ lệnh `grep -c 'BusinessCode' GlobalExceptionHandler.cs` của
> bản chốt:** nó đo **cách viết**, không đo **kết quả**. Sau bản sửa nó in `0`
> trong khi mã vẫn ra tới envelope đúng như yêu cầu — một nghiệm thu báo đỏ cho
> việc đã xong sẽ bị người sau "sửa cho xanh" bằng cách gán mã inline lần thứ
> hai, tức phá đúng thứ vừa gộp lại. Phép đo thật của mục này là hai test nêu ở
> bảng trên; hai lệnh `grep` chỉ là lưới chặn hồi quy rẻ tiền.

### (c) Tiếng Anh đang rò ra người dùng — **4 điểm**, không phải 2 — ✅ ĐÃ SỬA 2026-09-03

> **Trạng thái: ✅ CÓ THẬT (đối chiếu 2026-09-03).** Cả 4 điểm đã đổi sang
> `IdentityError.Code`: `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/IdentityService.cs:101`
> và `:113`; `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs:167`
> và `:175` (đối chiếu lại 2026-09-05: cả 4 vẫn đúng dòng). Ba dòng log trong `CoreSeeder.cs`
> **giữ nguyên**, đúng như §3 yêu cầu.
> Không dựng `IdentityErrorDescriber`, không override gì.

`IdentityResult.Errors[].Description` là chuỗi của ASP.NET Core Identity, và nó
**chỉ có tiếng Anh**: `dotnet/aspnetcore` có 97 file `.resx` nhưng **đúng 0 file
`.xlf`** — nghĩa là không có bản dịch nào để bật. Bốn chỗ đang lấy chuỗi đó đẩy
thẳng ra envelope:

| Điểm rò (mô tả hiện trạng **trước** bản vá — neo bằng mô tả, không bằng số dòng) | Đường ra |
| --- | --- |
| 2 lời gọi trong `IdentityService` (đổi mật khẩu, và cập nhật sau khi đổi) | `ChangePasswordResult` → envelope |
| 2 lời gọi trong `UserAdminService` (tạo user, và gán vai trò) | `CreateUserOutcome` → envelope |

**Cách sửa rẻ nhất — rẻ hơn mọi phương án đã cân nhắc:** `IdentityError.Code`
**vốn đã** là mã ổn định (`PasswordTooShort`, `DuplicateUserName`, …). Đổi 4 dòng
`e.Description` → `e.Code`. **KHÔNG** dựng `IdentityErrorDescriber`, **KHÔNG**
override 22 phương thức của nó — đó là dựng bộ chữ thứ hai ở BE cho một kênh mà
FE đã sở hữu câu chữ (§3).

| Trước 2026-09-03 | ✅ CÓ THẬT hôm nay (đối chiếu 2026-09-03) |
| --- | --- |
| 4 dòng `Errors.Select(e => e.Description)` trong thư mục `Identity/` | 4 dòng `Errors.Select(e => e.Code)` |
| 3 dòng `e.Description` trong `CoreSeeder.cs` | **Giữ nguyên** — là log (§3) |

**Nghiệm thu** — lệnh 1 PASS khi **không in gì** (đã PASS 2026-09-03; trước đó in
4 dòng); lệnh 2 là **ca đối chứng**, PASS khi **vẫn in 3 dòng**:

```bash
grep -rn 'Select(e => e.Description)' src/BE/Core/PlatformManager.Core.Infrastructure/Identity | grep -v '//'
grep -rn 'Select(e => e.Description)' src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs
```

Không có ca đối chứng thì một lần "dọn cho sạch" sẽ nuốt luôn 3 dòng log, và mất
mát đó **không lộ ra** cho tới lần đầu có sự cố seed cần đọc log.

> **Vì sao lệnh dài hơn bản chốt (`grep -rn 'e.Description' <thư mục>`):** lệnh cũ
> đếm **cả chú thích**. Bản sửa để lại chú thích giải trình *vì sao không dùng
> `Description`* ngay tại chỗ sửa, và một câu văn nhắc tên thuộc tính đó đủ làm
> nghiệm thu báo đỏ trong khi code hoàn toàn đúng. Đây là cùng một cái bẫy với
> lệnh đếm ở §5.1, và nó **không** giả định: nó đã kích hoạt thật trong lượt thi
> công này. Hai thay đổi: khớp trọn cụm `Select(e => e.Description)` thay vì mảnh
> `e.Description`, và loại dòng có `//`.

## 5. **KHÔNG LÀM** — và vì sao, để người sau không "dọn nốt"

### 5.1 KHÔNG bật `RequestLocalization` / `.resx` / `IStringLocalizer` ở BE

Đo 2026-09-05: `CultureInfo` xuất hiện **hai lần** trong cả `src/BE`, và **cả hai** đều là
`InvariantCulture` — `src/BE/PlatformManager.Api/Program.cs:364` (giá trị `Retry-After`) và
`MessageParamPolicy.Stringify` (§10.4). Lần thứ hai là **do §10 thi công**; nó không phá điều
mục này bảo vệ, và nghiệm thu đã sửa để đo đúng thứ đó — xem hộp giải trình bên dưới.
*(Bản trước ghi "đúng một lần … `Program.cs:337`"; số dòng đó cũng đã trôi từ trước đợt này.)*

Bật `SupportedCultures` sẽ đổi `CurrentCulture` của request. Với `vi-VN`, dấu
thập phân là **dấu phẩy** — nghĩa là mọi phép `Parse` / `ToString` không truyền
culture tường minh đổi hành vi. Đây là hỏng ở **tầng dữ liệu**, đổi lấy một thứ
ta không cần: dưới hướng A, FE sở hữu câu chữ nên BE **không cần** chữ tiếng
Việt cho kênh có FE.

BE hôm nay **sạch culture**. Giữ sạch. Nghiệm thu — lệnh 1 PASS khi **rỗng**; lệnh 2
PASS khi chỉ in `CultureInfo.InvariantCulture`:

```bash
grep -rn 'UseRequestLocalization\|SupportedCultures\|IStringLocalizer\|CurrentCulture\|LanguageManager' src/BE --include=*.cs
grep -rho 'CultureInfo\.[A-Za-z]*' src/BE --include=*.cs | sort -u
```

> **Sửa 2026-09-05 — nghiệm thu cũ đo SAI THỨ, và nó đã báo đỏ cho một bản sửa đúng.**
> Bản trước đếm `grep -rho 'CultureInfo' src/BE --include=*.cs | wc -l` và chốt PASS khi
> in `1`. Phép đếm đó coi **mọi** lần nhắc `CultureInfo` là nguy hiểm như nhau, trong khi
> thứ mục này thật sự cấm là **đổi văn hoá theo request**. §10.4 ý 2 lại **bắt buộc** đổi
> giá trị tham số sang chuỗi bằng văn hoá invariant tường minh — nên khi §10 thi công
> (2026-09-05), số đếm lên `2` dù BE vẫn sạch đúng nghĩa: cả hai lần xuất hiện đều là
> `CultureInfo.InvariantCulture`, tức khẳng định *"không phụ thuộc văn hoá tiến trình"*.
>
> Hai mục cùng file mà một mục báo đỏ cho việc mục kia bắt buộc là mâu thuẫn trong chính
> luật, và cách nó được "sửa" sẽ là bỏ phần invariant đi cho gate xanh. Nghiệm thu mới đo
> đúng hai điều mục này quan tâm: **không có** hạ tầng bản địa hoá nào được bật, và mọi
> lần dùng `CultureInfo` đều là bản invariant. Nó cũng thoát cái bẫy `grep` đếm cả **chú
> thích** đã ghi trong XML doc của hàm `DomainException.Render`
> (`src/BE/Core/PlatformManager.Core.Domain/Common/DomainException.cs`) — neo bằng **tên hàm**
> chứ không số dòng, đúng lý do §4(b) đã ghi.

Nếu một ngày có client thứ hai không phải FE này, quay lại mục này **với bằng
chứng** — đừng bật trước.

### 5.2 KHÔNG bật `ValidatorOptions.Global.LanguageManager` tiếng Việt

FluentValidation 12.0.0 có sẵn `VietnameseLanguage`, bật lên chỉ mất một dòng.
Vẫn không bật: đó là dựng **bộ chữ thứ hai ở BE** cho đúng cái kênh mà FE đã sở
hữu câu chữ. Một dòng hôm nay, hai nguồn câu chữ mãi mãi — đúng khuôn
`.claude/CLAUDE.md` §5 cấm.

## 6. `INotificationSender` — đổi chữ ký lúc còn **0 consumer**

`INotificationSender.SendAsync` (`src/BE/Core/PlatformManager.Core.Application/Notifications/INotificationSender.cs:9`)
trước 2026-09-03 nhận `(string to, string subject, string body, CancellationToken ct)` — tức là
**chuỗi đã dựng xong**. Không có tham số locale, và `AppUser` **không có cột
ngôn ngữ** (đối chiếu
`src/BE/Core/PlatformManager.Core.Infrastructure/Identity/AppUser.cs`
2026-09-03). Kênh email là kênh **không có FE**, nên BE sở hữu câu chữ (§3) — mà
để dựng đúng câu, BE cần **cả hai** thứ đang thiếu: biết khoá nào, và biết ngôn
ngữ nào.

✅ **ĐÃ ĐỔI CHỮ KÝ 2026-09-03** — 🚧 **còn một dòng chưa làm**, xem cột phải.

| Trước 2026-09-03 | Hôm nay (đối chiếu 2026-09-03) |
| --- | --- |
| `SendAsync(string to, string subject, string body, ct)` | ✅ Nhận **một record request**: `src/BE/Core/PlatformManager.Core.Application/Notifications/INotificationSender.cs:20`, record ở `src/BE/Core/PlatformManager.Core.Application/Notifications/NotificationRequest.cs:48` (`To` + `TemplateKey` + `Parameters` + `Locale`) |
| Không có ai biến khoá thành câu | ✅ Seam mới, **host cấp, Core tiêu thụ**: `src/BE/Core/PlatformManager.Core.Application/Notifications/INotificationTemplateRenderer.cs:27`. `src/BE/Core/PlatformManager.Core.Infrastructure/Notifications/SmtpNotificationSender.cs:30` dựng câu qua seam đó rồi mới gửi |
| `AppUser` không có cột ngôn ngữ | 🚧 **VẪN CHƯA CÓ** — đây là đổi lược đồ DB, không nằm trong "sửa 2 file", nên **chưa làm** và cần người dùng chốt. Xem ghi chú ngay dưới bảng |
| **0 consumer** — `src/BE/PlatformManager.Api/Program.cs:177` ghi rõ seam này cố ý chưa đăng ký | ✅ Vẫn 0 consumer, nên việc đổi không phá ai. Ghi chú "bật khi nào" ở `src/BE/PlatformManager.Api/Program.cs:190` nay liệt **hai** dòng đăng ký bắt buộc, không phải một. *(Hai neo sửa 2026-09-05: bản trước ghi `:162` và `:175`, cả hai đã trôi.)* |

**Mốc "miễn phí" này chỉ có hôm nay.** Đổi chữ ký một interface 0 consumer là
sửa 2 file. Sau consumer đầu tiên, nó là việc di trú. Bước này **độc lập** với
ba lỗi ở §4 và chạy song song được.

### Vì sao phải có `INotificationTemplateRenderer`, và vì sao Core không có bản mặc định

Bản chốt chỉ nói *"nhận record thay vì chuỗi đã dựng"*. Thi hành đúng câu đó thì
lộ ra một chỗ trống bản chốt không nêu: sau khi người gửi thư nhận **khoá**, nó
**không còn câu để gửi**. Câu phải sinh ra ở đâu đó, và không chỗ nào trong ba
chỗ sẵn có nhận được: nơi gọi vừa được giải phóng khỏi việc đó, `SmtpNotificationSender`
biết SMTP chứ không biết mẫu thư của dự án nào, còn Core thì không được giữ câu
chữ riêng của dự án 1 (§2). Bỏ seam này thì bước đổi chữ ký chỉ **dời** vấn đề.

Core **cố ý không có hiện thực mặc định** — cùng khuôn `ICoreMenuSeedSource` /
`ICoreBootstrapAccountSource`. Một bản mặc định trả chuỗi rỗng biến "quên đăng
ký" thành **email gửi đi thật với nội dung vô nghĩa**: hỏng im lặng, ở đúng kênh
không có màn hình nào để ai đó nhìn thấy. Thiếu đăng ký ⇒ DI không phân giải
được ⇒ hỏng lúc khởi động.

### 🚧 Câu hỏi để ngỏ, **cần người dùng chốt**: lấy `Locale` ở đâu

`Locale` khai **bắt buộc, không mặc định** — có chủ đích: một mặc định ngầm sẽ
tái lập đúng vấn đề vừa sửa (mọi nơi gọi bỏ qua tham số, hệ thống lại gửi một
ngôn ngữ cho mọi người mà không ai từng quyết định như vậy).

Nhưng hôm nay **không có nguồn nào** để lấy giá trị đó. Ba lựa chọn, chưa chọn:

| Lựa chọn | Cái giá |
| --- | --- |
| Thêm cột ngôn ngữ vào `AppUser` | Đổi lược đồ DB trên bảng đã có dữ liệu thật ⇒ phải được duyệt trước |
| Lấy từ phiên đang thao tác | Sai với thư gửi cho người **khác** người đang bấm nút |
| Chốt một hằng số ở tầng host | Đúng hôm nay (một ngôn ngữ), sai ngay khi có người nhận nói tiếng khác |

Việc này **không chặn** bước đổi chữ ký (0 consumer), nhưng nó chặn **consumer
đầu tiên** — và nó phải được trả lời ở đúng dòng gọi đầu tiên, không trôi qua im
lặng. Ghi chú tương ứng nằm tại `src/BE/PlatformManager.Api/Program.cs:190`.

## 7. Thứ tự thi công — pha sửa code theo bảng này

Thứ tự có lý do, không phải danh sách việc. Bước BE ghi chi tiết ở file này;
bước FE chỉ ghi một dòng, chi tiết thuộc [`../fe/08-i18n.md`](../fe/08-i18n.md).

| # | Việc | Phía | Vì sao xếp ở đây |
| ---: | --- | --- | --- |
| 0 | Ghi tài liệu (chính là đợt 2026-09-03 này) | — | Không chạm `src/` |
| 1 | ✅ **XONG 2026-09-03** — Lỗi (a): `DomainException` mang `DomainError`, 6 mã vào catalog | BE | Tiên quyết **cứng**: mã sẽ thành khoá dịch |
| 2 | ✅ **XONG 2026-09-03** — ArchTest quét mã nguồn chặn tái phát, kèm ca đối chứng | BE | Sửa xong mà không chặn thì mã tự do quay lại ngay |
| 3 | ✅ **XONG 2026-09-03** — Lỗi (b): mã cho lỗi validation, di trú **song song** | BE | Mở đường cho FE tra mã thay vì hiển thị chữ BE |
| 4 | ✅ **XONG 2026-09-03** — Lỗi (c): 4 điểm rò `Description` → `Code` | BE | Rẻ, độc lập, bịt đường rò tiếng Anh |
| 5 | ✅ **XONG 2026-09-03** phần chữ ký — `INotificationSender` nhận record (§6). 🚧 Còn nguồn `Locale` chưa chốt | BE | Độc lập, song song được — nhưng chỉ miễn phí khi còn 0 consumer |
| 5b | ✅ **XONG 2026-09-05** — cơ chế tham số `messageParams` cho cả hai ca (§10) | BE | **Không đánh số lại bảng này**: số bước đang được trích trong XML doc của `IApiResult.cs`/`ApiFieldError.cs`, đổi số là làm sai một loạt chú thích mà không có gì báo. Vị trí đúng của nó là **trước bước 7**, đúng như §10 đã khai |
| 6 | Hạ tầng FE: thư viện dịch, đăng ký locale, nối thư viện component | FE | — |
| 7 | **Đi bộ xuyên tường**: đúng **một** màn (`dang-nhap`) chạy tiếng Anh đầu-cuối | FE + BE | Bước có **giá trị thông tin cao nhất**: 1 form, ~8 chuỗi, 2 mã lỗi BE — đủ chạm cả hai phía mà chưa tốn gì |
| 8 | Gate so tập `businessCode` của BE ↔ tập khoá bảng dịch FE, và parity khoá vi/en | BE + FE | Đặt **trước** khi bọc hàng loạt: không có gate thì chuỗi mới lọt vào **nhanh hơn** tốc độ bọc |
| 9 | Nhãn menu: tra `Code` của item → khoá dịch, fallback nhãn cũ | FE | **Không đổi DB, không breaking change**: `src/BE/Core/PlatformManager.Core.Application/Menu/MenuItemDto.cs:12` đã trả `Code`, `src/FE/src/app/core/menu/menu-item.model.ts:28` đã nhận |
| 10 | Bọc chuỗi **theo tầng chia sẻ** (`shared/` + `core/` → `.ts` của `platform/` → `.html` theo màn) | FE | Chia theo màn hình ngay từ đầu là cách chắc chắn nhất sinh ra hàng chục khoá "Huỷ" trùng nhau |
| 11 | **DỌN**: gỡ trường cũ khỏi envelope, sửa nơi đọc nó | BE + FE | Bước duy nhất **không được bỏ** — bỏ nó là để lại vĩnh viễn hai đường đọc lỗi |
| 12 | Phát hành 2 ngôn ngữ: **một** bản build, đổi tại chỗ | FE | Không `/vi/` `/en/`, không `baseHref` theo locale |

> **Bước 7 không phải bước làm cho có.** Bọc hàng loạt trước khi có một màn chạy
> đầu-cuối nghĩa là mọi giả định về hình dạng khoá, chỗ đặt bảng dịch, và cách FE
> tra mã lỗi BE đều **chưa được kiểm** — sai một cái là sửa lại toàn bộ số chuỗi
> đã bọc.

## 8. Nợ tường minh — ghi ra, không giấu

**Nợ dưới đây đã CHÍNH THỨC phát sinh 2026-09-03** — §4(c) đã thi công, nên đây
không còn là nợ dự kiến mà là nợ đang mang.

**Mất tham số `RequiredLength` của `PasswordTooShort`.** Đổi `e.Description` →
`e.Code` (§4c) giữ được mã nhưng **vứt đi con số** trong câu tiếng Anh gốc
(*"Passwords must be at least N characters"*). FE vì vậy phải hiển thị **câu
chung** không có con số, cho tới khi có đường trả tham số có cấu trúc (§3).

Đây là **nợ đã biết và đã chấp nhận**, không phải sót. Phương án tránh nợ là
dựng `IdentityErrorDescriber` với 22 override — đắt hơn nhiều lần giá trị của
một con số trong một câu, và mở lại đúng cái cửa §5.2 vừa đóng.

🚧 **Đường trả con số đó về đã chạy 2026-09-05 (§10), nhưng nợ NÀY vẫn CHƯA đóng.**
Ghi rõ vì đây đúng là chỗ dễ tưởng nhầm là xong: `messageParams` mở được **đường ống**, còn
con số `RequiredLength` của Identity thì **vẫn không đi qua nó**. Lý do đo được: lỗi Identity
tới `ChangePasswordCommand` dưới dạng danh sách **mã** rồi bị nối bằng dấu chấm phẩy thành
một chuỗi, và chuỗi đó đi ra tham số `{Reasons}` — tham số của nó là *danh sách mã*, không
phải *con số trong câu*.

Đóng nợ này cần đúng thứ §10.7 **nợ 2** đang treo: BE trả **danh sách có cấu trúc** thay cho
một chuỗi đã nối, để mỗi mã Identity mang bộ tham số riêng. ~~Việc đó đổi kiểu dữ liệu của một
trường ⇒ đổi hợp đồng ⇒ **thuộc thẩm quyền người dùng**, không phải hệ quả kỹ thuật của §10.~~

> **🚧 Cập nhật 2026-09-05 — rào cản đã đổi, nợ thì CHƯA đóng.** Người dùng chốt đường
> `fieldErrors` (§11): mỗi mã Identity nay có một `ApiFieldError` riêng, mà `ApiFieldError`
> **đã** mang `MessageParams` từ §10.1. Nghĩa là rào cản "phải đổi hợp đồng" biến mất, phần
> còn lại thuần kỹ thuật: đọc `IdentityOptions.Password.RequiredLength` rồi gắn vào tham số
> của đúng mã `PasswordTooShort`.
>
> **Nhưng đó vẫn là việc CHƯA làm và CHƯA chốt.** BE hôm nay chỉ lấy `.Code` (§4c), nên con số
> **vẫn không đi qua** kể cả sau khi §11 thi công xong. Đây đúng chỗ dễ gạch nhầm khỏi danh
> sách nợ vì "đường đã thông".

## 9. Hợp đồng bị lật cùng đợt này

[`../../../contracts/users.md`](../../../contracts/users.md) chốt 2026-08-19 rằng
*"FE hiển thị thẳng `message` là đủ, không cần map lại theo `businessCode`"*, và
ghi chú 2026-08-29 nói *"Cột `message` ở bảng trên là **nguồn**"*. Hướng A dưới
cơ chế runtime **đảo vai**: `businessCode` là hợp đồng, `message` tụt xuống
dev-facing + fallback. Việc lật ghi tại chính card đó — xem mục
*"Lật 2026-09-03"* trong `doc/contracts/users.md`.

## 10. Cơ chế tham số trong thông điệp — ✅ CÓ THẬT (chốt 2026-09-04, thi công 2026-09-05)

> **Trạng thái: ✅ CÓ THẬT (đối chiếu 2026-09-05).** Cả 5 phép nghiệm thu ở §10.8 đã
> **xanh**, chạy thật trên repo chứ không suy luận. Phần văn xuôi giải thích **lý do**
> của từng lựa chọn giữ nguyên — đừng đọc nó thành "chưa làm"; hiện trạng nằm ở bảng
> §10.8 và ở cột phải của bảng *"có thật hôm nay → sẽ thành"* trong `be-api-controller.md`.
>
> **Một điểm THI CÔNG KHÁC chữ của bản chốt, ghi rõ để không ai "sửa lại cho đúng doc":**
> §10.4 ý 2 bắt buộc đổi giá trị sang chuỗi bằng văn hoá **invariant tường minh**, mà
> nghiệm thu cũ của §5.1 lại đếm mọi lần nhắc `CultureInfo` và chốt PASS khi in `1`. Hai
> mục cùng file mâu thuẫn nhau: làm đúng §10.4 thì §5.1 đỏ. Đã sửa **nghiệm thu của §5.1**
> (không sửa §10.4) — lý do đầy đủ ở chính §5.1. Đường Domain **cố ý không đụng**: hôm nay
> không `DomainError` nào có khuôn thông điệp mang tham số, chi tiết ở §10.9.

### Vì sao mục này phải tồn tại — chỗ trống nó bịt

Ba mục đã viết trước đó cùng dựa vào một thứ chưa ai định nghĩa:

| Mục | Câu nó đã hứa | Thứ nó giả định là có |
| --- | --- | --- |
| §3 | *"BE trả **mã** + **tham số có cấu trúc**"* | một đường để tham số đi ra |
| §4(b) | lỗi từng field mang mã để FE tra | tham số của từng lỗi field |
| §8 | nợ *"mất `RequiredLength` của `PasswordTooShort`"* | một chỗ để trả con số đó về |

Cả ba trỏ sang §Envelope response của
[`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) — nhưng ở đó
**chưa có trường nào** mang tham số. Nghĩa là ba lời hứa đang treo vào một chỗ trống.
§10 chốt cơ chế; **hình dạng** của trường thì vẫn khai ở file chủ, không khai ở đây (lý
do ở §10.2).

**Vị trí trong §7:** cơ chế này phải xong **trước bước 7** ("đi bộ xuyên tường"). Bước 7
chạy một màn đầu-cuối để kiểm mọi giả định về khoá và cách FE tra mã; chạy nó khi tham
số chưa có đường ra nghĩa là kiểm một hệ thiếu đúng phần khó nhất, rồi phát hiện lại từ
đầu ở bước 10. Nó **không** chèn thêm số thứ tự vào bảng §7: số bước ở bảng đó đang được
trích dẫn trong XML doc của source (`IApiResult.cs`, `ApiFieldError.cs`), đánh số lại là
làm sai một loạt chú thích mà không có gì báo.

### 10.1 MỘT cơ chế cho CẢ HAI ca — không phải hai cơ chế

Hai ca cần tham số, và chúng đến từ hai nguồn khác hẳn nhau:

| Ca | Nguồn tham số | Ví dụ có thật |
| --- | --- | --- |
| 1 — lỗi validate (400) | FluentValidation sinh ra khi rule fail | độ dài tối thiểu của mật khẩu mới |
| 2 — lỗi nghiệp vụ (409/422) | nơi ném lỗi tự truyền | tên đăng nhập đã tồn tại (`src/BE/Core/PlatformManager.Core.Application/Users/UserErrors.cs:11`) |

Cám dỗ ở đây là dựng **hai** đường: một đường "tham số validate" và một đường "tham số
nghiệp vụ". Không làm vậy, vì cái giá không nằm ở lúc viết mà ở lúc đọc: FE sẽ phải hỏi
*"lỗi này là loại nào"* trước khi biết đọc tham số ở đâu, và mọi chỗ tra bảng dịch sẽ có
hai nhánh. Hai nhánh rồi sẽ lệch — đúng khuôn `.claude/CLAUDE.md` §5 cấm.

**Chốt: một tên, một kiểu, một ý nghĩa** — `messageParams`, một từ điển khoá-giá trị,
**vắng mặt hoàn toàn khi không có tham số**. Vắng mặt chứ không phải rỗng: envelope
không phình thêm một khoá cho mọi lỗi, đúng cách `retryable` và `fields` đang làm hôm
nay (`src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs:20`).

#### Một điểm THI CÔNG KHÁC chữ của bản chốt — ghi rõ để không ai "sửa lại cho đúng doc"

Bản chốt viết *"MỘT trường tuỳ chọn trong envelope, cạnh `businessCode`"*. Đúng chữ đó
phục vụ được ca 2, nhưng **không** phục vụ được ca 1, và đây là lý do đo được:

Một lần submit hỏng nhiều field cùng lúc thì mỗi field có **bộ tham số riêng**. Form đổi
mật khẩu có `NewPassword` và `ConfirmPassword`; màn tạo người dùng có `UserName` và
`Email` với hai giới hạn độ dài khác nhau. Nhét tất cả vào **một** từ điển ở gốc envelope
thì hai field cùng fail một validator sẽ ghi đè khoá của nhau — và hỏng **im lặng**: FE
vẫn dựng được câu, chỉ là câu của field này mang con số của field kia.

Vì vậy **cơ chế** giữ đúng một cái, còn **chỗ đặt** thì đi theo đúng cái mã mà nó tham
số hoá — luật một dòng, không cần cân nhắc ở từng ca:

> `messageParams` nằm cạnh mã mà nó giải thích. Cạnh `businessCode` ở gốc envelope (ca
> 2), và cạnh `code` trong từng phần tử `fieldErrors` (ca 1, record `ApiFieldError`).

Đây **không** phải hai cơ chế: cùng một tên trường, cùng một kiểu, cùng một cách đọc.
FE viết đúng **một** hàm ráp câu và gọi nó ở cả hai chỗ.

### 10.2 Hình dạng trường — khai ở file chủ, KHÔNG khai ở đây

Tên và kiểu, đủ để đọc mục này mà không phải nhảy file:

| | |
| --- | --- |
| Tên trên dây | `messageParams` (camelCase, như mọi property của envelope) |
| Kiểu | từ điển `string` → `string`, khoá là **tên** tham số |
| Khi nào có mặt | **chỉ** khi mã đi kèm có tham số; không thì trường vắng hẳn |

**Định nghĩa đầy đủ** — khai trong `IApiResult<T>`, ví dụ JSON, và luật "vắng mặt khi
không có tham số" — nằm ở §Envelope response của
[`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md), là file chủ
của chủ đề "hình dạng envelope" theo đúng bảng ranh giới ở đầu file này. Chép nó xuống
đây là dựng bản sao thứ hai của một hợp đồng — và bản sao envelope trong repo này đã
lệch thật một lần, đo được, xem giải trình §5 ở chính file chủ đó.

**Vì sao khoá là chuỗi chứ không phải số thứ tự (`{0}`, `{1}`):** khoá số buộc FE giữ
**đúng thứ tự tham số của câu tiếng Việt**. Trật tự từ mỗi ngôn ngữ một khác, nên câu
tiếng Anh cần tham số thứ hai đứng trước tham số thứ nhất là chuyện thường — với khoá
số, dịch đúng đòi hỏi người dịch phải biết thứ tự BE truyền, thứ không có ở đâu trong
bảng dịch. Khoá tên thì bảng dịch tự đủ nghĩa. Đây cũng là khoá mà FluentValidation vốn
đã dùng (§10.3), nên chọn khoá tên là **không** phải quy đổi gì ở ca 1.

**Vì sao giá trị là chuỗi chứ không phải kiểu JSON nguyên bản:** xem §10.4 — lý do là an
toàn, không phải tiện.

### 10.3 Ca 1 — FluentValidation **đã có sẵn** thứ ta cần, chỉ chuyển tiếp

Không dựng cơ chế nào cho ca này. `FluentValidation.Results.ValidationFailure` đã mang
sẵn một từ điển tham số đã đặt tên: `FormattedMessagePlaceholderValues`, sinh ra bởi
`MessageFormatter` — chính thứ thư viện dùng để ráp câu lỗi mặc định.

Đã kiểm **nhị phân** (2026-09-04), không phải đọc tài liệu — bản đang pin là
`FluentValidation 12.0.0`
(`src/BE/Core/PlatformManager.Core.Application/PlatformManager.Core.Application.csproj:10`):

```bash
D="$HOME/.nuget/packages/fluentvalidation/12.0.0/lib/net8.0/FluentValidation.dll"
grep -ac 'FormattedMessagePlaceholderValues' "$D"   # 1 = co
grep -ac 'MessageFormatter'                  "$D"   # 1 = co
```

Các khoá độ dài (`MinLength`, `MaxLength`, `TotalLength`) là **user string UTF-16** trong
dll nên `grep` thường không thấy — kiểm bằng cách so cả hai bảng mã, đừng kết luận
"không có" từ một lệnh `grep` trần:

```bash
python -c "d=open(__import__('os').path.expanduser('~/.nuget/packages/fluentvalidation/12.0.0/lib/net8.0/FluentValidation.dll'),'rb').read(); print([(s, s.encode('utf-8') in d, s.encode('utf-16-le') in d) for s in ['MinLength','MaxLength','TotalLength','ComparisonValue','PropertyValue']])"
```

Chỗ nối đúng một hàm: `GlobalExceptionHandler.ToFieldError` — trước bản vá nó lấy `ErrorCode`
và `ErrorMessage` từ `ValidationFailure`; nó lấy thêm từ điển tham số từ **cùng đối
tượng đó**. Không thêm nguồn dữ liệu, không thêm lượt duyệt, không đụng validator nào.

> **Vì sao đây là điểm quyết định của cả §10:** nếu tham số validate phải đặt tay ở từng
> rule thì cơ chế này chết ngay từ rule thứ mười — không ai bọc nổi, và một hệ bọc dở
> dang còn tệ hơn không bọc (câu có tham số và câu không, lẫn lộn trong cùng một form).
> Thư viện đã cấp sẵn cho **mọi** rule dựng sẵn là thứ làm cho ca 1 trở nên miễn phí.

### 10.4 ⚠️ Ràng buộc BẮT BUỘC — allowlist khoá, KHÔNG chuyển tiếp cả từ điển

`FormattedMessagePlaceholderValues` **luôn** chứa khoá `PropertyValue`, và giá trị của nó
là **chính giá trị người dùng vừa gõ** — thư viện nạp khoá này cho mọi failure, không
phụ thuộc câu lỗi có dùng tới nó hay không. Kiểm nhị phân 2026-09-04: `PropertyValue` và
`AppendPropertyValue` đều có mặt trong dll (lệnh ở §10.3).

Hệ quả nếu chuyển tiếp cả từ điển: rule độ dài của **mật khẩu mới** fail ⇒ mật khẩu vừa
gõ đi ra HTTP response, vào log của trình duyệt, vào mọi telemetry FE. Đây không phải rủi
ro lý thuyết — màn đổi mật khẩu là màn có thật
(`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:143`, đối chiếu
2026-09-05) và đang kiểm đúng ràng buộc độ dài đó.

**Luật, cưỡng chế bằng test:**

1. Chỉ những khoá nằm trong **allowlist** mới được ra ngoài. `PropertyValue` **không bao
   giờ** nằm trong allowlist.
2. Giá trị đổi sang chuỗi bằng văn hoá **invariant** — BE hôm nay sạch culture và phải
   giữ sạch (§5.1). Đây cũng là lý do trường mang giá trị **chuỗi** chứ không phải kiểu
   JSON nguyên bản: từ điển gốc mang `object`, chuyển tiếp thô nghĩa là bất kỳ thứ gì rơi
   vào đó sẽ được serialize nguyên trạng — một đường rò dữ liệu mở sẵn, không đoán trước
   được nó sẽ mang gì.
3. Cái giá đã biết và chấp nhận: FE **không** định dạng lại số/ngày theo locale được, vì
   nhận về đã là chuỗi. Ở quy mô này tham số hầu hết là số nguyên nhỏ (độ dài tối thiểu)
   và tên riêng — định dạng theo locale không đổi gì. Ghi vào §10.7 để không ai tưởng là
   sót.

### 10.5 Ca 2 — 5 descriptor có chỗ giữ `{0}`, truyền tham số tường minh khi dựng

Năm chỗ khai có chỗ giữ theo số trong khuôn thông điệp (đếm lại bằng lệnh ở §10.8, đừng
tin con số này khi code đã đổi): 1 trong `AuthErrors` và 4 trong `UserErrors`. ✅ Cả năm đã
đổi sang chỗ giữ đặt tên ngày 2026-09-05 — xem §10.9.

Điều đáng chú ý: **tham số đã được truyền vào rồi**. `Fail<T>(ErrorDescriptor, params
object[] args)` (`BaseResponse.Fail`, chữ ký TRƯỚC bản vá 2026-09-05)
nhận đủ các giá trị, `string.Format` ráp chúng vào câu tiếng Việt, rồi **vứt bản rời
đi** — chỉ câu đã ráp ra tới envelope. Việc phải làm không phải là "thu thập tham số" mà
là **thôi vứt chúng đi**.

Kèm theo đó, khuôn thông điệp đổi từ chỗ giữ theo số sang chỗ giữ **đặt tên**
(`{UserName}`), vì lý do ở §10.2. Câu tiếng Việt trong `ErrorDescriptor`
(`src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorDescriptor.cs:8`) vẫn
được ráp như cũ để giữ vai trò *dev-facing + fallback* (§3) — ráp từ **chính** từ điển
sắp gửi đi, không phải từ một danh sách tham số thứ hai. Một nguồn, hai đầu ra: câu
fallback và `messageParams` không lệch nhau được.

`MessageFormatter` của FluentValidation làm đúng việc ráp theo khoá tên và là API công
khai, còn `Core.Application` thì **đã** tham chiếu thư viện này rồi (csproj dẫn ở §10.3)
— nên ca 2 cũng không thêm phụ thuộc và không thêm bộ ráp câu tự viết. Đây là điều biến
"một cơ chế" từ khẩu hiệu thành thứ kiểm được: cùng một lớp ráp câu, cùng một khuôn khoá,
cho cả hai ca.

### 10.6 Hằng số chính sách do BE sở hữu — và bug chứng minh điều đó

Đây không phải lựa chọn thẩm mỹ. Bug đã tìm ra 2026-09-04, đo được, đang có thật:

| Nơi khai | Đo 2026-09-04 | Đo lại 2026-09-05 |
| --- | --- | --- |
| `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:121` | `RequiredLength = 12` | `12` (không đổi) |
| `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:29` | `MIN_PASSWORD_LENGTH = 8` | `12` — hai số đã khớp |

Một hằng số chính sách **chép ở hai nơi, và đã trôi**. Hệ quả cho người dùng gõ mật khẩu
9 ký tự: FE cho qua, BE từ chối — và câu từ chối là câu tiếng Anh của Identity đã bị đổi
sang mã ở §4(c), nên người dùng nhận về một thông báo không nói được con số nào.

> **Hai số đã được nắn về bằng nhau (đo 2026-09-05), nhưng đó KHÔNG phải lý do gỡ mục này.**
> Hai bản sao vẫn còn nên chúng trôi lại được bất cứ lúc nào; thứ mục này chốt là **ai quyết
> định câu người dùng đọc**, và câu trả lời đó không đổi theo việc hôm nay hai số đang khớp.
> Đây đúng là thời điểm dễ kết luận "hết bug rồi, bỏ luật đi" nhất.

Suy ra luật, và nó áp cho **mọi** hằng số chính sách chứ không riêng độ dài mật khẩu:

> **Câu người dùng đọc phải dựng TỪ CON SỐ BE GỬI, không từ hằng số của FE.**
> FE ráp `messageParams` vào bảng dịch; nó **không** được thay con số của mình vào.

Vì sao chiều sở hữu là BE chứ không phải FE: BE là nơi **cưỡng chế** chính sách. Con số
của FE có sai thì chỉ làm phiền người dùng; con số của BE sai thì hệ thống nhận sai mật
khẩu. Đặt quyền sở hữu ở nơi cưỡng chế nghĩa là bản sao còn lại không bao giờ quyết định
điều gì quan trọng — kể cả khi nó vẫn còn trôi.

### 10.7 Nợ còn lại — nợ 2 **ĐÃ CHỐT 2026-09-05** (§11), nợ 1 vẫn treo

> **Đọc mục này với cái mốc đó trong đầu.** Nợ 2 dưới đây được viết ngày 2026-09-04 khi chưa
> ai quyết; người dùng đã chốt ngày 2026-09-05 và toàn bộ quyết định + lý do + nghiệm thu nằm
> ở **[§11](#11-hai-quyết-định-người-dùng-2026-09-05--mã-identity-đi-vào-fielderrors-updateasync-trả-outcome)**.
> Phần văn xuôi của nợ 2 giữ nguyên vì nó là **chẩn đoán**, vẫn đúng; chỉ có câu kết luận
> *"CHƯA làm, và cố ý không tự quyết ở đây"* là đã hết hạn — và một câu trong đó hoá ra **sai**,
> ghi rõ ngay tại chỗ. Nợ 1 thì **không** bị hai quyết định 2026-09-05 chạm tới, vẫn treo
> nguyên.

**Nợ 1 — rủi ro trôi CHƯA đóng hết.** Hằng số phía FE vẫn cần cho việc kiểm trước khi
gửi (báo lỗi ngay lúc gõ, không đợi round-trip), nên hai con số vẫn tồn tại song song và
vẫn trôi được. Cái §10.6 đóng là **hệ quả nặng nhất** (câu người dùng đọc mang con số
sai), không phải bản thân việc chép. Trôi tiếp thì triệu chứng còn lại là: form cho qua
một mật khẩu mà BE sẽ từ chối — khó chịu, nhưng lộ ra ngay và không sai lệch thông tin.

Đóng dứt điểm cần BE **công bố chính sách qua một endpoint** để FE nạp lúc khởi động.
**CỐ Ý KHÔNG làm bây giờ:** thêm một endpoint, một hợp đồng, một chỗ cache và một đường
hỏng lúc khởi động — đắt hơn giá trị ở quy mô một chính sách và một màn hình. Quay lại
mục này khi có **hằng số chính sách thứ ba** chép sang FE; lúc đó phép tính đổi chiều.

**Nợ 2 — có chuỗi mang một DANH SÁCH, không phải một giá trị.** Đếm bằng lệnh, đừng chép
số vào đây (§6 `.claude/CLAUDE.md`):

```bash
grep -rn 'thất bại: {' src/BE/Core --include='*Errors.cs'
```

> **Sửa 2026-09-05 cùng lượt thi công:** lệnh cũ tìm `'thất bại: {0}'` và nay in **rỗng** —
> không phải vì nợ đã đóng mà vì chỗ giữ theo số đã đổi thành chỗ giữ đặt tên (`{Reasons}`).
> Một lệnh liệt kê nợ mà im lặng sau khi cú pháp đổi là cách nợ biến mất khỏi tầm mắt trong
> khi vẫn còn nguyên.

Mỗi dòng nó in ra là một descriptor có khuôn *"…thất bại: <tham số>"*. **Không phải dòng
nào cũng nhận danh sách** — đọc chỗ gọi để biết: hai chỗ nhét vào đó một **danh sách mã lỗi
Identity nối bằng dấu chấm phẩy**
(`src/BE/Core/PlatformManager.Core.Application/Users/CreateUserCommand.cs:51`,
`src/BE/Core/PlatformManager.Core.Application/Auth/ChangePasswordCommand.cs:35`), chỗ còn
lại nhận một giá trị đơn.

> **Sửa 2026-09-04.** Bản trước ghi *"Bốn trong năm descriptor"* — số chép từ lời giao việc
> chứ không đo, và **sai**: lệnh trên in ra ba dòng. Câu *"chỗ gọi nhét vào đó một danh sách"*
> cũng khái quát quá tay, chỉ đúng cho hai trong ba. Đây đúng lý do §6 tồn tại.

Đặt tên cho tham số đó **không** làm nó dịch được: một danh sách nhét vào giữa câu là thứ
vỡ khi đổi ngôn ngữ, vì trật tự từ và cách nối danh sách mỗi ngôn ngữ một khác. Hướng
đúng: BE trả **danh sách có cấu trúc**, FE dịch từng mã rồi tự dựng câu theo quy tắc của
ngôn ngữ đang chọn.

~~**CHƯA làm, và cố ý không tự quyết ở đây:** nó đổi kiểu dữ liệu của một trường (một giá
trị ⇒ một danh sách), tức đổi hợp đồng — thuộc thẩm quyền người dùng, không phải hệ quả
kỹ thuật của §10.~~ Ghi thành nợ để nó không biến mất sau khi §10 thi công xong và trông
như đã trọn vẹn.

> **🚧 ĐÃ CHỐT 2026-09-05 — và câu gạch ngang ở trên hoá ra SAI, giữ lại để thấy vì sao.**
> Người dùng chốt: từng mã Identity đi vào **`fieldErrors`**, câu bỏ tham số. Đường đó
> **không đổi hợp đồng** — `fieldErrors` đã có trên envelope và FE đã khai nhận nó, nên danh
> sách đi vào một trường **vốn đã là danh sách**. Câu cũ đúng cho *một* phương án (biến
> `messageParams.Reasons` từ chuỗi thành mảng) nhưng đã được viết ra như thuộc tính của *vấn
> đề* — và một nợ bị dán nhãn "đổi hợp đồng" thì không ai buồn tìm đường rẻ hơn.
>
> Toàn bộ quyết định, lý do, ràng buộc thi công và nghiệm thu: **§11**. ✅ Code **đã về
> 2026-09-05**, 11/11 phép nghiệm thu §11.4 xanh — nợ 2 **đóng ở phía BE**; phần FE đọc
> `fieldErrors` **cũng đã về 2026-09-06** (câu cũ ở đây nói "thì chưa" — sai từ hôm đó, sửa
> 2026-09-08). Neo và bài học: khối 🔄 mở đầu §11.

### 10.8 Nghiệm thu — ✅ cả 5 lệnh XANH (chạy 2026-09-05)

Chạy từ gốc repo. Cột "trước pha code" đo 2026-09-04; cột "sau" chạy thật 2026-09-05.

| # | Đo cái gì | Trước | PASS khi | Sau ✅ |
| ---: | --- | --- | --- | --- |
| 1 | Trường có trong hợp đồng envelope | `0` | `>= 1` | `2` |
| 2 | Ca 1 nối vào từ điển sẵn có của FluentValidation | rỗng | in ra dòng ở `GlobalExceptionHandler.cs` | có dòng gọi `MessageParamPolicy.FromValidationPlaceholders` |
| 3 | Allowlist có test chặn `PropertyValue` | rỗng | in ra ít nhất 1 dòng trong `src/BE/Tests` | có, ở cả unit lẫn integration |
| 4 | Ca 2 dùng chỗ giữ **đặt tên** | rỗng | in ra các khoá dạng `{UserName}` | **2 dòng** (đo 2026-09-05): `{UserName}` `{Email}` — `{Reasons}`×3 đã gỡ ở §11 |
| 5 | Câu người dùng đọc không còn dựng từ hằng số FE | in 1 dòng | **rỗng** | rỗng |

> **Lệnh 5 XANH mà khu BE không sửa file nào của FE** — ghi rõ để không ai kết luận sai về
> nguyên nhân. Nó xanh vì phía FE đã bỏ việc dựng câu từ hằng số cục bộ trước đó; công của
> lượt BE này là làm cho con số **có đường đi ra** (§10.6), không phải gỡ dòng đó.

```bash
grep -c 'MessageParams' src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs
grep -rn 'FormattedMessagePlaceholderValues' src/BE --include=*.cs
grep -rn 'PropertyValue' src/BE/Tests --include=*.cs
grep -rnE '^[^/]*[{][A-Za-z][A-Za-z0-9]*[}]' src/BE/Core --include=*Errors.cs
grep -n 'MIN_PASSWORD_LENGTH' src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts | grep 'errors\.'
```

> **Lệnh 4 cố ý loại dòng bắt đầu bằng `///`, và cái bẫy này đã kích hoạt thật lúc soạn
> mục này (2026-09-04):** một lệnh `grep` trần tìm chỗ giữ đặt tên sẽ bắt luôn `{T}` trong
> XML doc (`<see cref="ApiResult{T}"/>` ở `RateLimitErrors.cs`, `ValidationErrors.cs`) và
> `{Entity}` trong `SysMenuErrors.cs` — tức **báo xanh khi chưa làm gì cả**. Một nghiệm thu
> xanh sẵn từ trước là nghiệm thu tệ hơn không có: nó vừa không đo gì, vừa làm người sau
> tin là đã đo.

**Ca đối chứng cho nợ 1** — hai lệnh dưới đây in hai con số phải **bằng nhau**. Ngày
2026-09-04 chúng là `12` và `8`, tức đang trôi; ✅ đo lại 2026-09-05 **cả hai đều in `12`**.
Đây là phép đo của §10.6, và nó **vẫn cần chạy sau khi §10 xong**: §10 chỉ gỡ hậu quả nặng
nhất của việc trôi, không gỡ việc trôi — hai con số vẫn ở hai nơi và vẫn trôi tiếp được.

```bash
grep -n 'RequiredLength' src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs
grep -n 'MIN_PASSWORD_LENGTH = ' src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts
```

**Đếm descriptor còn chỗ giữ theo số** (`.claude/CLAUDE.md` §6) — trước pha code in `5`,
✅ sau pha code phải in **`0`**: chỗ giữ theo số là thứ §10.2 loại bỏ, nên mọi lần nó quay
lại đều là hồi quy, kể cả ở một catalog mới toanh.

```bash
grep -rnE '[{]0[}]' src/BE/Core --include=*Errors.cs | wc -l
```

### 10.9 Đã thi công gì — neo để đối chiếu, 2026-09-05

Bảng này thay cho việc đọc lại diff. Mỗi dòng neo bằng **tên** (file, lớp, hàm) chứ không số
dòng, đúng bài học §4(b): bản vá nào cũng kèm chú thích, và chú thích đẩy trôi mọi neo dưới nó.

| Việc | Neo |
| --- | --- |
| Trường mới trên envelope + trên từng `fieldErrors` | `IApiResult<T>.MessageParams`, `ApiResult<T>.MessageParams`, `ApiFieldError.MessageParams` |
| Allowlist + đổi chuỗi invariant, **một** chỗ duy nhất | `src/BE/Core/PlatformManager.Core.Application/Common/Results/MessageParamPolicy.cs` |
| Ca 1 — chuyển tiếp từ điển của FluentValidation, đã lọc | `GlobalExceptionHandler.ToFieldError` |
| Ca 2 — một nguồn, hai đầu ra (câu fallback + tham số rời) | `BaseResponse.Fail`, dùng `MessageFormatter` của FluentValidation |
| 2 khuôn thông điệp còn chỗ giữ đặt tên (đo 2026-09-05) | `UserErrors` (`{UserName}`, `{Email}`). Ba khuôn `{Reasons}` đã gỡ ở §11 — mã Identity nay đi qua `fieldErrors`, không nối vào câu |
| Test canh bảo mật (`PropertyValue` không bao giờ ra ngoài) | `MessageParamPolicyTests` (unit) + `ValidationEnvelopeShapeTests` (shape trên dây) |
| Test canh ca 2 đi qua handler thật | `BusinessErrorMessageParamsTests` |

**Ba thứ CỐ Ý không làm trong lượt này** — ghi ra để không ai đọc §10 thành "đã trọn vẹn":

1. **Đường Domain giữ nguyên chỗ giữ theo số.** `DomainError` hôm nay không có khuôn thông
   điệp nào mang tham số, nên đổi chữ ký `DomainException` là đổi cho 0 nơi dùng — không ca
   nào kiểm được là đúng. Chỗ nối khi cần: `ExceptionHandlingBehavior.BuildErrorResponse`,
   và chú thích tại cả hai đầu đã nói lại điều này.
2. **`fields` chưa gỡ** — vẫn là bước 11 của §7, và phải làm cùng phía client.
3. **Nợ danh sách nối chuỗi (§10.7 nợ 2) — ✅ đã đóng ở phía BE ngày 2026-09-05**, trong một
   lượt riêng sau §10. Mã Identity vào `fieldErrors`, và đường đó **không** đổi hợp đồng như câu
   này từng nói. Quyết định + thi công + nghiệm thu: §11. *(Câu gốc của mục này — "chưa đụng, còn
   đỏ" — đúng tại thời điểm §10 xong; giữ lại chiều lịch sử đó thay vì viết lại như thể §10 đã
   làm luôn phần việc của §11.)*

> **Vì sao nghiệm thu ở đây là `grep` chứ không phải test:** `grep` là lưới chặn hồi quy
> rẻ tiền, chạy được ngay cả khi solution không build. Phép đo **thật** của mục này là
> hai thứ máy kiểm nội dung được: một test khoá shape envelope có `messageParams` khi và
> chỉ khi có tham số, và một test khẳng định `PropertyValue` không bao giờ ra tới
> response (§10.4). Cả hai thuộc pha code — `.claude/CLAUDE.md` §8 nhắc rằng gate `grep`
> xanh **không** có nghĩa là nội dung đúng.

## 11. Hai quyết định người dùng 2026-09-05 — mã Identity đi vào `fieldErrors`, `UpdateAsync` trả outcome

> **Trạng thái: ✅ CÓ THẬT — pha BE đã thi công 2026-09-05.** Người dùng chốt cùng ngày sau khi
> được trình bày bằng chứng đo được; code về ngay trong ngày. Cả 11 phép nghiệm thu ở §11.4 đã
> chạy lại và XANH (bảng dưới ghi cả cột "trước" để đối chiếu), `dotnet test` xanh với Unit 88 ·
> Arch 57 · Integration 112.
>
> 🔄 **LẬT 2026-09-08 — cảnh báo cũ ở đây đã hết đúng, và nó sai theo chiều hiếm gặp.**
> Nguyên văn: *"Xong pha BE ≠ người dùng thấy đổi. FE hôm nay còn bind lỗi từ `fields`; phần tô
> đỏ đúng ô và dịch từng mã thuộc pha FE, **chưa làm**"*. Đúng ngày viết (2026-09-05), **sai từ
> 2026-09-06**: pha FE đã về, và **cả ba** nơi tiêu thụ nay đọc `fieldErrors` —
> `src/FE/src/app/platform/login/pages/login/login.page.ts:219` · `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:241`
> (lý do đổi ghi ngay tại `:109`) · `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts:141`
> (nơi tiêu thụ ở `:211`).
>
> Giữ nguyên văn câu cũ thay vì xoá, vì đây là ví dụ của dạng sai **ngược** với dạng mà
> [`.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §4 canh: doc tự nhận mình **còn nợ** trong
> khi nợ đã trả. Không gate nào bắt được nó (không có nhãn "xong" nào để đòi ngày), và cái giá là
> người đọc sau đi làm lại một việc đã xong — hoặc tệ hơn, hoãn một việc khác vì tin rằng tiền đề
> kia còn đúng. §11.7 đã hoãn đúng như vậy một lần.
>
> Hai quyết định này đóng **nợ 2 của §10.7** và sửa tận gốc chỗ sinh ra nó. Chúng **không**
> chạm nợ 1 (hằng số chính sách chép hai nơi) — xem §10.7.

### 11.1 Vấn đề đã sửa — trạng thái TRƯỚC ngày 2026-09-05 (lịch sử)

> 🔄 **Sửa 2026-09-08 — mục này mô tả thứ ĐÃ SỬA, không phải hiện trạng.** Tiêu đề cũ là
> *"Hiện trạng đo được — thứ người dùng đang đọc hôm nay"* và cả hai bảng dưới viết ở thì
> hiện tại, trong khi code đã đổi ngay trong ngày 2026-09-05 theo đúng §11.2. Đây chính là
> khuôn sai `.claude/CLAUDE.md` §4 mô tả, chỉ ngược chiều: không phải nhãn "đã xong" cho việc
> chưa làm, mà là nhãn "đang hỏng" cho việc đã sửa — và nó cũng không gây lỗi biên dịch nào.
>
> Ba neo `file:dòng` trong bảng cũng đã trôi và **đã sửa cùng lượt**; neo cũ
> `AuthErrors.cs:17` mỉa mai thay lại trỏ đúng vào dòng doc-comment viết *"Câu KHÔNG mang
> tham số — đổi 2026-09-05"*, tức bằng chứng nó viện dẫn nói ngược lại chính nó.

Trước 2026-09-05, ba descriptor mang chỗ giữ `{Reasons}`, và ba chỗ gọi nhét vào đó một thứ
không dịch được:

| Descriptor | Khuôn thông điệp (đo 2026-09-05, TRƯỚC khi sửa) | Chỗ gọi nhét gì vào `{Reasons}` |
| --- | --- | --- |
| `AuthErrors.ChangePasswordFailed` | `"Đổi mật khẩu thất bại: {Reasons}"` | `string.Join("; ", result.Errors)` — danh sách **mã Identity** |
| `UserErrors.CreateFailed` | `"Tạo người dùng thất bại: {Reasons}"` | `string.Join("; ", outcome.Errors)` — danh sách **mã Identity** |
| `UserErrors.UpdateFailed` | `"Cập nhật người dùng thất bại: {Reasons}"` | chuỗi tiếng Việt **bịa tại chỗ gọi**: `"không lưu được thay đổi"` |

Câu người dùng đọc **khi đó**:

| Đường | Câu ra TRƯỚC 2026-09-05 | Hỏng ở đâu |
| --- | --- | --- |
| Đổi mật khẩu | `Đổi mật khẩu thất bại: PasswordTooShort; PasswordRequiresDigit` | Định danh tiếng Anh nối bằng dấu chấm phẩy, nhét giữa câu tiếng Việt. Không dịch được, và **không ô nhập nào được tô đỏ** |
| Tạo người dùng | `Tạo người dùng thất bại: DuplicateUserName` | như trên |
| Sửa người dùng | `Cập nhật người dùng thất bại: không lưu được thay đổi` | **Nói hai lần cùng một điều.** Mã lỗi Identity thật đã bị vứt trước khi tới được handler — §11.3 |

#### Hiện trạng sau khi sửa (đối chiếu 2026-09-08)

Không descriptor nào trong ba cái trên còn chứa ký tự `{`; từng mã Identity đi ra
`fieldErrors` qua `IdentityFieldErrors.Build(...)`:

| Descriptor | Khuôn thông điệp hôm nay | Chỗ gọi |
| --- | --- | --- |
| `AuthErrors.ChangePasswordFailed` (`src/BE/Core/PlatformManager.Core.Application/Auth/AuthErrors.cs:28-29`) | `"Đổi mật khẩu thất bại."` | `src/BE/Core/PlatformManager.Core.Application/Auth/ChangePasswordCommand.cs:48` |
| `UserErrors.CreateFailed` (`src/BE/Core/PlatformManager.Core.Application/Users/UserErrors.cs:23-24`) | `"Tạo người dùng thất bại."` | `src/BE/Core/PlatformManager.Core.Application/Users/CreateUserCommand.cs:55` |
| `UserErrors.UpdateFailed` (`src/BE/Core/PlatformManager.Core.Application/Users/UserErrors.cs:40-41`) | `"Cập nhật người dùng thất bại."` | `src/BE/Core/PlatformManager.Core.Application/Users/UpdateUserCommand.cs:86` |

Đừng chép ba dòng trên thành số đếm — kiểm bằng lệnh (`.claude/CLAUDE.md` §6), PASS = **không
dòng nào in ra**:

```bash
grep -h -A1 -E '(ChangePasswordFailed|CreateFailed|UpdateFailed) = new\(' \
  src/BE/Core/PlatformManager.Core.Application/Auth/AuthErrors.cs \
  src/BE/Core/PlatformManager.Core.Application/Users/UserErrors.cs | grep '{'
```

*(Lệnh cố ý **chỉ** soi ba descriptor này. `UserErrors.DuplicateUserName`/`DuplicateEmail`
vẫn mang tham số `{UserName}`/`{Email}` — có chủ đích, vì chúng đi kèm `messageParams` theo
§10, khác hẳn ca nối danh sách mã Identity vào giữa câu.)*

### 11.2 Quyết định 1 — mã Identity vào `fieldErrors`, KHÔNG nhét vào giữa câu

**Chốt:** câu bỏ tham số (`"Đổi mật khẩu thất bại."`, `"Tạo người dùng thất bại."`,
`"Cập nhật người dùng thất bại."`); **từng** mã Identity đi ra một phần tử của `fieldErrors`,
với `code` là **chính mã đó** (`PasswordTooShort`, `DuplicateUserName`, `ConcurrencyFailure`…).

#### Vì sao `fieldErrors` là đúng NGỮ NGHĨA, không phải mẹo tránh đổi hợp đồng

Đây là chỗ dễ đọc nhầm nhất của quyết định này, nên nói thẳng:

1. **`PasswordTooShort` VỐN LÀ lỗi của một ô nhập.** Nó không phải lỗi của "yêu cầu" nói
   chung — nó nói rằng **giá trị người dùng vừa gõ vào ô mật khẩu** không đạt. Đặt nó vào
   `fieldErrors` là mô tả đúng bản chất của nó. Nếu `fieldErrors` chưa tồn tại thì việc phải
   làm vẫn là dựng ra nó, chứ không phải nối chuỗi vào câu.
2. **Hệ quả kéo theo là thứ chỉ có ở chỗ đúng ngữ nghĩa:** FE tô đỏ đúng ô, và dịch **từng**
   mã. Một mẹo kỹ thuật không cho ra hai thứ đó — nó chỉ chuyển chuỗi sang một trường khác.
3. **Nối danh sách vào giữa câu là thứ vỡ khi đổi ngôn ngữ**, vì trật tự từ và cách nối danh
   sách mỗi ngôn ngữ một khác. §10.7 nợ 2 đã ghi điều này từ 2026-09-04; quyết định hôm nay
   là câu trả lời cho nó.

**Rẻ hơn mọi đường khác, và đó là hệ quả chứ không phải lý do:**
`ApiResult<T>.FieldErrors` **đã tồn tại**
(`src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiResult.cs:18`) và FE **đã
nhận** (trường `fieldErrors` khai trong `src/FE/src/app/core/http/api-result.model.ts`).
Không thêm trường, không đổi hình dạng envelope, không đụng hợp đồng công khai.

> **Sửa một câu của §10.7 (viết 2026-09-04).** Mục đó khẳng định đóng nợ 2 *"đổi kiểu dữ liệu
> của một trường (một giá trị ⇒ một danh sách), tức đổi hợp đồng"*. Câu đó đúng cho **một**
> đường đi — biến `messageParams.Reasons` từ chuỗi thành mảng. Đường được chọn thì không:
> danh sách đi vào một trường **đã là danh sách sẵn**, đã có trên dây, đã được FE khai. Bài
> học: "phải đổi hợp đồng" là kết luận về **một phương án**, đừng chép nó thành thuộc tính
> của **vấn đề**.

#### Ràng buộc bắt buộc khi thi công — tên field, và ba cái bẫy đã đo

Tên field gắn mã vào là **quyết định của người thi công**, nhưng phải thoả cả ba:

| # | Ràng buộc | Vì sao |
| --- | --- | --- |
| 1 | Khớp **tên property C#** của command, viết PascalCase | Đó chính là khoá FE bind lỗi lên form — quy ước ở `NormalizeField` trong `src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs`, và FE đã khai đúng bộ khoá đó: `ChangePasswordField` trong `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts`, `UserFormField` trong `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts` |
| 2 | Cùng **một quy tắc chọn tên** cho cả ba đường | Ba đường cùng một cơ chế mà ba kiểu khoá thì bảng dịch phải học ba lần, đổi lấy không gì |
| 3 | Có **một fallback được ghi ra** cho mã không thuộc ô nhập nào | Xem bẫy 2 dưới đây |

**Bẫy 1 — field theo MÃ, không theo ENDPOINT.** Đây là bẫy dễ mắc nhất: "đường đổi mật khẩu
⇒ tất cả về `NewPassword`" là **sai**. `userManager.ChangePasswordAsync` trả `PasswordMismatch`
khi **mật khẩu hiện tại** nhập sai — quy nó về `NewPassword` là tô đỏ đúng cái ô người dùng gõ
đúng. Ánh xạ phải theo mã: `PasswordMismatch` → `CurrentPassword`; `PasswordTooShort` /
`PasswordRequiresDigit` / … → `NewPassword` (là `TempPassword` ở đường tạo người dùng).

> Đây cũng chính là câu hỏi để ngỏ mà bản đặc tả giao diện đã nêu từ trước:
> [`../../../Design/Frontend/PlatformManager/Screens/05-auth.md`](../../../Design/Frontend/PlatformManager/Screens/05-auth.md)
> ghi rằng `AUTH.CHANGE_PASSWORD_FAILED` phủ **cả hai** ca ("sai mật khẩu hiện tại" và "mật
> khẩu mới quá yếu") nên FE không biết tô ô nào, và đề nghị *"tách mã ở phía server, hoặc
> luôn điền `fields`"*. Quyết định 1 trả lời bằng vế thứ hai, ở dạng có mã: mã Identity đi
> kèm tên ô.

**Bẫy 2 — có mã KHÔNG thuộc ô nhập nào.** Đường sửa người dùng trả `ConcurrencyFailure` — nó
nói về **bản ghi**, không về một ô.

> **✅ Fallback đã chọn khi thi công 2026-09-05: khoá `"$record"`**, khai thành hằng số
> `IdentityFieldErrors.RecordKey`
> (`src/BE/Core/PlatformManager.Core.Application/Common/Results/IdentityFieldErrors.cs`).
>
> **Chọn "ra dây kèm khoá riêng" chứ không phải "bỏ đi":** `ConcurrencyFailure` là thông tin
> **chẩn đoán**. Vứt nó là quay lại đúng hiện trạng mà quyết định 2 (§11.3) tồn tại để sửa —
> cập nhật hỏng mà không ai biết vì sao.
>
> **Vì sao ký tự `$`, không phải một từ tiếng Anh:** `$` không mở đầu được một định danh C#, nên
> khoá này **không thể** trùng tên property nào do `NormalizeField` sinh ra. Chọn `"Record"` hay
> `"General"` thì ngày có ai đặt một property tên như vậy, lỗi mức bản ghi sẽ ghi đè lỗi của một
> ô thật — hỏng im lặng. Phía FE, union tên ô là kiểu literal đóng nên khoá lạ đơn giản không
> bind vào ô nào; người dùng vẫn đọc câu ở `message`. Test canh:
> `IdentityFieldErrorsTests.RecordKey_CannotCollide_WithAnyCSharpPropertyName`.

**✅ Tên field đã chọn cho hai form** (ràng buộc 1 + 2), khai cạnh nhau ở đúng một chỗ —
`IdentityFormFields.ChangePassword` và `IdentityFormFields.UserForm`:

| Mã Identity nói về | Màn đổi mật khẩu | Màn thêm/sửa người dùng |
| --- | --- | --- |
| mật khẩu vừa ĐẶT (`PasswordTooShort`…) | `NewPassword` | `TempPassword` |
| mật khẩu HIỆN TẠI (`PasswordMismatch`) | `CurrentPassword` | *(không có ô)* → `$record` |
| `InvalidUserName` / `DuplicateUserName` | *(không có ô)* → `$record` | `UserName` |
| `InvalidEmail` / `DuplicateEmail` | *(không có ô)* → `$record` | `Email` |
| mã về role (`UserAlreadyInRole`…) | *(không có ô)* → `$record` | `Roles` |
| còn lại (`ConcurrencyFailure`, `DefaultError`…) | `$record` | `$record` |

Bảng mã → ô là **ALLOWLIST** (cùng lý do với `MessageParamPolicy`): mã Identity chưa biết rơi về
`$record` thay vì bị gắn bừa vào một ô. Mất chỗ tô đỏ còn chấp nhận được; tô **sai** ô thì không.

**Bẫy 3 — hôm nay có một CÂU TIẾNG VIỆT đang nằm trong danh sách `Errors`.**
`IdentityService.ChangePasswordAsync`, nhánh không tìm thấy người dùng, trả
`new ChangePasswordResult(false, ["Không tìm thấy người dùng."])` — một câu, không phải mã.
Hôm nay nó chỉ làm câu ghép đọc lạ; sau quyết định 1 nó trở thành `fieldErrors[].code` =
`"Không tìm thấy người dùng."`, tức một **khoá bảng dịch** là một câu tiếng Việt. Phải đổi
nhánh đó sang một mã (hoặc trả thẳng một `ErrorDescriptor` "không tìm thấy") **trong cùng
lượt**, nếu không quyết định này tự tạo ra đúng loại rác nó dọn.

> **✅ Đã sửa 2026-09-05, theo vế thứ hai — và bằng CÙNG một khuôn với ràng buộc của §11.3.**
> `ChangePasswordResult` nhận thêm cờ `NotFound` (y như `UpdateUserOutcome.NotFound`), nhánh đó
> trả `NotFound: true` với danh sách mã **rỗng**, và handler trả thẳng `UserErrors.NotFound`
> (404) thay vì `AUTH.CHANGE_PASSWORD_FAILED`.
>
> **Hai cái được, ngoài việc dọn rác:** (a) danh sách `Errors` nay là danh sách **mã thuần** ở
> mọi nhánh — bất biến đó phát biểu được thành một câu, nên nó kiểm được; (b) "tài khoản không
> còn" ra **404** đúng nghĩa thay vì 422 "đổi mật khẩu thất bại", tức nói đúng chuyện đã xảy ra.
>
> ⚠️ **Đây là bổ sung mã lỗi cho một route** (`POST /api/auth/change-password` nay trả được
> `USER.NOT_FOUND`) — additive, không đổi hình dạng envelope, nhưng vẫn phải ghi vào card:
> [`../../../contracts/auth.md`](../../../contracts/auth.md). Test canh:
> `IdentityFieldErrorsTests.ChangePassword_WhenUserVanished_DoesNotSmuggleASentenceIntoTheCodeList`.

#### Điều này KHÔNG tạo họ khoá mới cho FE

`fieldErrors[].code` từ trước tới nay đã mang mã dạng PascalCase không có dấu chấm — đó là
`ValidationFailure.ErrorCode` của FluentValidation (`NotEmptyValidator`, `EmailValidator`…),
lý do đầy đủ ở docstring của
`src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiFieldError.cs`. Mã Identity
có **đúng hình dạng đó** và vào **đúng trường đó**, nên FE không nhận thêm khuôn khoá nào
mới. Đây là lý do quyết định này rẻ ở cả hai phía, không riêng phía BE.

### 11.3 Quyết định 2 — `UpdateAsync` trả outcome có mã lỗi, không trả `bool` trần

**Chốt:** `IUserAdminService.UpdateAsync` đổi kiểu trả về thành một **outcome mang danh sách
mã lỗi**, đúng khuôn `CreateAsync` ngay cạnh nó đang dùng (`CreateUserOutcome`, khai ở
`src/BE/Core/PlatformManager.Core.Application/Users/IUserAdminService.cs:7`). Năm chỗ
`return false` trong `UpdateCoreAsync` trả **mã thật**. Bỏ `{Reasons}` khỏi
`UserErrors.UpdateFailed`. Rồi đi cùng đường `fieldErrors` của quyết định 1.

#### Vì sao sửa tận gốc thay vì chỉ bỏ `{Reasons}`

`UserAdminService.UpdateCoreAsync` thoát bằng `return false` ở **năm** chỗ — mã lỗi Identity
bị vứt **trước khi** tới được handler:

| Chỗ thoát trong `UpdateCoreAsync` | Vứt đi thứ gì |
| --- | --- |
| `FindByIdAsync` trả null | (không phải lỗi Identity — xem ràng buộc dưới) |
| `UpdateSecurityStampAsync` thất bại | `IdentityResult.Errors` |
| `userManager.UpdateAsync(user)` thất bại | `IdentityResult.Errors` — nơi `ConcurrencyFailure` sinh ra |
| `RemoveFromRolesAsync` thất bại | `IdentityResult.Errors` |
| `AddToRolesAsync` thất bại | `IdentityResult.Errors` |

Hai lý do, độc lập nhau:

1. **`bool` trần là chỗ lạc loài.** `CreateAsync` ngay cạnh đã trả outcome. Hai hàm cùng lớp,
   cùng loại việc, cùng loại thất bại, hai kiểu trả về khác nhau — bản thân điều đó đã đủ để
   chỗ gọi phải bịa, và nó **đã bịa thật**:
   `src/BE/Core/PlatformManager.Core.Application/Users/UpdateUserCommand.cs:69` tự viết ra câu
   `"không lưu được thay đổi"` vì không còn gì khác để nói.
2. **Vứt thông tin chẩn đoán ở năm chỗ nghĩa là khi cập nhật hỏng thật thì KHÔNG AI biết vì
   sao** — kể cả người vận hành đọc log. `ConcurrencyFailure` và "gán role hỏng" ra cùng một
   `false`, rồi ra cùng một câu.

**Ràng buộc:** nhánh `FindByIdAsync` trả null **không** phải lỗi Identity — nó là ca người
dùng bị xoá xen giữa lần đọc của handler và lần ghi. Outcome phải cho handler phân biệt được
ca này với "Identity từ chối", nếu không thì một bản ghi vừa bị xoá sẽ ra `USER.UPDATE_FAILED`
kèm danh sách mã **rỗng** — đúng lại chỗ trống mà quyết định này sinh ra để lấp.

**Ràng buộc đặt tên:** record outcome mới đặt cạnh `CreateUserOutcome` trong
`IUserAdminService.cs`, và **tên kết thúc bằng `Outcome`** — nghiệm thu ở §11.4 neo vào hậu
tố đó chứ không vào một cái tên đoán trước.

> **Đây là thay đổi NỘI BỘ của BE.** `IUserAdminService` là interface của `Core.Application`,
> không phải hợp đồng công khai với FE. Envelope không đổi.

**Cố ý KHÔNG làm trong lượt này:** `LockAsync` / `UnlockAsync` cũng trả `bool` trần và cũng
vứt `IdentityResult.Errors` theo đúng khuôn đó. Người dùng chốt phạm vi là `UpdateAsync`.
Ghi ra để lần sau không ai tưởng đã quét sạch — và đó là ứng viên kế tiếp hiển nhiên nhất.

### 11.4 Nghiệm thu — ✅ cả 11 lệnh XANH (chạy lại 2026-09-05, sau pha code)

Chạy từ gốc repo. Cột "trước" đo 2026-09-05 lúc chưa có code; cột "sau" chạy thật cùng ngày.

> **Luật viết lệnh ở mục này, rút từ ca đã trả giá 2026-09-05:** lệnh cũ của §10.7 là
> `grep` tìm đúng cụm `thất bại: {0}`, và nó **im lặng** ngay khi chỗ giữ đổi từ `{0}` sang
> `{Reasons}` — nợ biến mất khỏi tầm mắt trong khi vẫn còn nguyên. Nên: **lệnh phải LUÔN in ra
> thứ gì đó**, và điều kiện PASS đọc trên thứ nó in. Lệnh nào PASS-khi-rỗng thì phải đi kèm
> một lệnh luôn-in cùng phạm vi, và **rỗng ở lệnh luôn-in là BÁO ĐỘNG, không phải PASS**.

#### Quyết định 1

| # | Đo cái gì | Trước (ĐỎ) | PASS khi | Sau ✅ |
| ---: | --- | --- | --- | --- |
| 1 | Ba khuôn thông điệp không còn chỗ giữ | in 3 dòng, cả 3 chứa `{Reasons}` | vẫn in **3 dòng**, **không dòng nào** chứa `{`. In ít hơn 3 ⇒ descriptor bị xoá/đổi tên ⇒ đọc lại, **không** phải PASS | 3 dòng, sạch chỗ giữ |
| 2 | `BusinessError` mang được `fieldErrors` | in ra thân factory, **không** có `FieldErrors` | thân factory in ra **có** `FieldErrors` | có `FieldErrors = fieldErrors` |
| 3 | Ba chỗ gọi không còn nối chuỗi, không còn câu bịa | in mọi lời gọi `Fail<` của 3 file | **không dòng nào** chứa `string.Join` hoặc `"Reasons"` | 12 dòng, sạch cả hai |
| 4 | Có test canh mã Identity ra tới `fieldErrors` | in **rỗng** | in **≥ 1 dòng** | 9 dòng (unit + integration) |
| 5 | Bẫy 3 — không còn câu tiếng Việt trong danh sách `Errors` | in 3 dòng, **1 dòng** là câu tiếng Việt | vẫn in **3 dòng**, và **không dòng nào** chứa chữ tiếng Việt: hai dòng lấy từ `.Errors.Select(e => e.Code)`, dòng còn lại là nhánh `NotFound: true` với danh sách **rỗng** | 3 dòng, không dòng nào mang câu |

```bash
grep -rn 'CHANGE_PASSWORD_FAILED\|USER.CREATE_FAILED\|USER.UPDATE_FAILED' src/BE/Core --include=*Errors.cs
grep -n -A 12 'static ApiResult<T> BusinessError' src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiResult.cs
grep -rn 'Fail<' src/BE/Core/PlatformManager.Core.Application/Auth/ChangePasswordCommand.cs src/BE/Core/PlatformManager.Core.Application/Users/CreateUserCommand.cs src/BE/Core/PlatformManager.Core.Application/Users/UpdateUserCommand.cs
grep -rn 'PasswordTooShort' src/BE/Tests --include=*.cs
grep -n 'ChangePasswordResult(false' src/BE/Core/PlatformManager.Core.Infrastructure/Identity/IdentityService.cs
```

Lệnh 3 là **cặp luôn-in** của lệnh 6 dưới đây: nó in *mọi* lời gọi `Fail<` ở ba file, nên một
câu bịa được **viết lại** thay vì gỡ đi vẫn lộ ra ở đây, dù lệnh 6 đã im.

#### Quyết định 2

| # | Đo cái gì | Trước (ĐỎ) | PASS khi | Sau ✅ |
| ---: | --- | --- | --- | --- |
| 6 | Câu bịa ở chỗ gọi đã biến mất | in 1 dòng (`UpdateUserCommand`) | in **rỗng** — đọc kèm lệnh 3 | rỗng |
| 7 | Chữ ký `UpdateAsync` không còn `bool` | in 1 dòng, có `Task<bool>` | dòng khai `UpdateAsync` **không** chứa `Task<bool>` | `Task<UpdateUserOutcome>` |
| 8 | Toàn bộ điểm thoát của đường cập nhật | in 6 dòng: 5 × `return false`, 1 × `return true` | in **≥ 1 dòng**, và **không dòng nào** là `return false;` / `return true;` trần. In rỗng ⇒ khối đã đổi tên ⇒ **BÁO ĐỘNG** | 6 dòng: 4 × `Rejected(...)`, 1 × `UserNotFound()`, 1 × `Success()` |
| 9 | Có hai outcome cạnh nhau trong interface | in các dòng của `CreateUserOutcome` | in **thêm** dòng cho đường cập nhật (bất kể tên record là gì, miễn kết thúc bằng `Outcome`) | thêm `UpdateUserOutcome` |

> **Lệnh 6 đã sửa cùng lượt thi công — nó từng đếm cả CHÚ THÍCH.** Bản vá gỡ câu bịa cũng để lại
> chú thích *giải thích vì sao gỡ*, và chú thích đó tất nhiên nhắc lại nguyên văn chuỗi bị gỡ.
> Lệnh cũ vì vậy báo đỏ cho đúng bản sửa mà nó sinh ra để nghiệm thu — và cách nó sẽ được "sửa"
> là xoá lời giải thích đi cho gate xanh, tức xoá đúng thứ đáng giữ. Nay lệnh lọc bỏ dòng bắt đầu
> bằng `//`/`///`. Cùng một cái bẫy đã ghi ở §5.1 (`grep` đếm cả chú thích) và §10.8 (lệnh 4 phải
> loại `///`) — lần thứ ba, nên coi nó là **khuôn**, không phải sự cố lẻ.
>
> Điều này KHÔNG nới luật: lệnh 3 (cặp luôn-in) vẫn in mọi lời gọi `Fail<` của cả ba file, nên
> một câu bịa **viết lại trong code** vẫn lộ ra ở đó.

```bash
grep -rn 'không lưu được thay đổi' src/BE/Core --include=*.cs | grep -vE ':[[:space:]]*//'
grep -n 'UpdateAsync' src/BE/Core/PlatformManager.Core.Application/Users/IUserAdminService.cs
awk '/UpdateCoreAsync\(Guid/,/LockAsync\(Guid/' src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs | grep -nE '^[[:space:]]*return '
grep -n 'Outcome' src/BE/Core/PlatformManager.Core.Application/Users/IUserAdminService.cs
```

#### Ca đối chứng — thứ KHÔNG được đổi theo

| # | Đo cái gì | PASS khi |
| ---: | --- | --- |
| 10 | Đường tạo/đổi mật khẩu vẫn giữ **mã**, không quay về `Description` (§4c) | in ra các dòng `Select(e => e.Code)` trong thư mục `Identity/`, **không** dòng nào là `e.Description` |
| 11 | `LockAsync`/`UnlockAsync` **vẫn** trả `bool` — ngoài phạm vi lần này | in ra hai chữ ký còn `Task<bool>`. Nếu chúng cũng đổi thì phạm vi đã bị nới mà **không ai chốt** |

```bash
grep -rn 'Errors.Select(e =>' src/BE/Core/PlatformManager.Core.Infrastructure/Identity
grep -n 'LockAsync\|UnlockAsync' src/BE/Core/PlatformManager.Core.Application/Users/IUserAdminService.cs
```

> **✅ Phép đối chứng đã CHẠY THẬT 2026-09-05 — test có ĐỎ khi hồi quy quay lại.** Một bộ test
> chống hồi quy chưa bao giờ thấy màu đỏ thì không ai biết nó đo được gì. Cách kiểm: đưa lại đúng
> khuôn cũ (`"Đổi mật khẩu thất bại: {Reasons}"` + `Fail(..., ("Reasons", string.Join("; ", …)))`),
> chạy `dotnet test`, rồi khôi phục. Kết quả: **2 test unit + 2 test integration ĐỎ**, ArchTests
> vẫn xanh. Hai tầng cùng bắt được là có chủ đích — tầng unit bắt câu ghép, tầng integration bắt
> cả shape trên dây; và ArchTests xanh xác nhận đúng điều đã cảnh báo ở §11.4: **không luật kiến
> trúc nào chặn được lỗi này**, nên nếu gỡ hai file test kia đi thì đường quay lại là đường trống.

**Phép đo THẬT vẫn là test, không phải `grep`** ([`.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §8):
một test khẳng định mã Identity ra tới `fieldErrors` với đúng tên ô, và một test khẳng định
câu `message` **không** còn chứa mã nào. `grep` chỉ là lưới chặn hồi quy rẻ tiền, chạy được cả
khi solution không build.

### 11.5 Trước → sau (đã thi công 2026-09-05)

| | Trước pha code | ✅ Sau pha code (đối chiếu 2026-09-05) |
| --- | --- | --- |
| Câu người dùng đọc khi đổi mật khẩu hỏng | `Đổi mật khẩu thất bại: PasswordTooShort; PasswordRequiresDigit` | `Đổi mật khẩu thất bại.` + `fieldErrors` mang từng mã, gắn đúng ô |
| Ô nhập **BE chỉ ra** | không ô nào | đúng ô mà mã đó nói tới (`CurrentPassword` / `NewPassword` / `TempPassword`…) — **FE đã đọc từ 2026-09-06**, xem §11.6 |
| `{Reasons}` trong catalog lỗi | 3 descriptor | **0** |
| Mã lỗi Identity của đường **sửa** người dùng | bị vứt ở 5 chỗ trong `UpdateCoreAsync` | đi qua outcome tới handler, rồi ra `fieldErrors` |
| Câu ở `UpdateUserCommand` | bịa tại chỗ gọi: `"không lưu được thay đổi"` | không còn — mã thật thay chỗ |
| `IUserAdminService.UpdateAsync` | `Task<bool>` | `Task<UpdateUserOutcome>`, cùng khuôn `CreateAsync` |
| Nhánh "không tìm thấy" của cả 2 đường ghi | lẫn vào cùng một `false` / một câu tiếng Việt trong danh sách mã | cờ `NotFound` riêng ⇒ `USER.NOT_FOUND` (404) |
| `LockAsync` / `UnlockAsync` | `Task<bool>`, vứt `Errors` | **giữ nguyên** — ngoài phạm vi, cố ý |

**Neo để đối chiếu** (neo bằng TÊN, không số dòng — bài học §4(b)):

| Việc | Neo |
| --- | --- |
| Bảng mã Identity → ô nhập + khoá `$record` + 2 preset tên field | `src/BE/Core/PlatformManager.Core.Application/Common/Results/IdentityFieldErrors.cs` |
| Envelope lỗi nghiệp vụ mang được `fieldErrors` | `ApiResult<T>.BusinessError` (tham số thứ 4), nạp chồng `BaseResponse.Fail` nhận `fieldErrors` |
| Danh sách kiểu của lời gọi reflection phải khai đủ tham số mới | `ExceptionHandlingBehavior.BuildErrorResponse` |
| Bẫy reflection nay **tự tố cáo** thay vì NRE câm | `ExceptionHandlingBehavior.FactoryMethodName` + `?? throw` trong `BuildErrorResponse` |
| Outcome mới + 3 factory cưỡng chế bất biến | `UpdateUserOutcome` trong `IUserAdminService.cs` |
| 5 điểm thoát trả mã thật | `UserAdminService.UpdateCoreAsync` |
| Cờ `NotFound` thay câu tiếng Việt | `ChangePasswordResult`, `IdentityService.ChangePasswordAsync` |
| Test khoá cả 2 quyết định (bẫy 1/2/3 + bất biến outcome) | `IdentityFieldErrorsTests` (unit) |
| Test khoá shape **trên dây** | `IdentityCodeFieldErrorsTests` (integration, HTTP thật) |

#### Bẫy reflection — đã vô hiệu hoá 2026-09-05

Bẫy này **đã nổ thật** trong chính đợt §11: thêm tham số `fieldErrors` vào
`ApiResult<T>.BusinessError` làm `GetMethod` trả `null`, và `method!` biến **mọi**
`DomainException` thành `NullReferenceException` — không lỗi biên dịch, không ArchTest nào
chạm tới, chỉ lộ ra ở nhánh lỗi lúc chạy. Đường duy nhất bắt được là
`PipelineBehaviorTests.DomainException_Thrown_In_Handler_Is_Translated_To_BusinessError`.

Hai lớp phòng nay chồng lên nhau:

| Lớp | Làm gì |
|---|---|
| Test đường thuận (`PipelineBehaviorTests`) | Lưới chính — bắt bẫy ngay lúc chạy test |
| `?? throw` tại chỗ phân giải | Lưới dự phòng — khi lưới chính thủng, lỗi **tự nói phải sửa gì** thay vì đổ ở một stack trace vô nghĩa |

Cache đổi từ `ConcurrentDictionary<Type, MethodInfo?>` sang `MethodInfo` (không còn giữ
`null`), và dấu `!` đã biến mất khỏi lời gọi `Invoke`.

**Một ràng buộc không hiển nhiên:** tên phương thức phải để ở hằng số `FactoryMethodName`
**ngoài** chuỗi, không nội suy `nameof(...)` vào giữa thông điệp. Luật
`Core_MustNotKnowBusinessName` quét **văn bản nguồn**, nên `$"…{nameof(X.BusinessError)}…"`
vẫn bị bắt dù theo nghĩa C# đó không phải string literal. Đã dính thật khi thi công.

### 11.6 Thứ KHÔNG đi kèm — đọc trước khi kết luận "xong là hết"

1. **~~FE hôm nay bind lỗi từ `fields`, không từ `fieldErrors`~~ — HẾT ĐÚNG từ 2026-09-06,
   viết lại 2026-09-08.** Nguyên văn cũ: *"Trường `fieldErrors` đã có trên dây và đã được khai
   phía FE, nhưng còn ở trạng thái chỉ nhận, chưa dùng
   (`src/FE/src/app/core/http/api-result.model.ts`) … pha BE xong thì người dùng chưa thấy gì
   đổi"*. Câu đó đúng ngày viết (2026-09-05); hôm sau pha FE về và cả ba nơi tiêu thụ đã đổi:
   `src/FE/src/app/platform/login/pages/login/login.page.ts:219` ·
   `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:241` (chú thích lý do ở `:109`) ·
   `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts:141` (tiêu thụ ở `:211`).

   Để lại ở thì quá khứ vì mục này sinh ra để chống *tuyên bố thắng lợi sớm* — mà ca thật lại
   ngược: nó kể một khoản nợ **đã trả**, suốt hai ngày, và không gate nào đỏ. Một mục "thứ chưa
   làm" cũng phải được đối chiếu lại, đúng như mục "thứ đã làm".
2. **Nợ `RequiredLength` của §8 vẫn CHƯA đóng.** Quyết định 1 gỡ rào cản hợp đồng: mỗi mã
   Identity nay có một `ApiFieldError` riêng, mà `ApiFieldError` thì đã có `MessageParams`
   (§10.1). Nhưng BE vẫn chỉ lấy `.Code`, nên **con số vẫn không đi qua**: còn phải đọc
   `IdentityOptions.Password.RequiredLength` rồi gắn vào tham số của đúng mã `PasswordTooShort`.
   Chưa làm, chưa chốt.
3. **Nợ 1 của §10.7 (hằng số chính sách chép hai nơi) không bị chạm.** Hai bản sao vẫn còn và
   vẫn trôi được.

### 11.7 Doc bị lật cùng đợt này

Ba chỗ khác mô tả hiện trạng cũ. Chúng được dán nhãn 🚧 ở pha tài liệu (2026-09-05, trước code),
rồi **đổi sang ✅ khi code về cùng ngày** — hai lượt, không phải một, đúng thứ tự "doc chốt trước,
code sau" mà repo này dùng:

| File | Mô tả cũ | Trạng thái sau khi code về |
| --- | --- | --- |
| [`../../../contracts/users.md`](../../../contracts/users.md) | `USER.CREATE_FAILED`/`USER.UPDATE_FAILED` kèm `messageParams` `{"Reasons":"…"}`; *"kèm lỗi chi tiết từ Identity"*; `UpdateAsync` trả `bool` | ✅ đã viết lại + thêm ví dụ `fieldErrors` thật, và ghi rõ mã đổi nghĩa (`USER.NOT_FOUND` cho ca xoá xen giữa) |
| [`../../../contracts/auth.md`](../../../contracts/auth.md) | `AUTH.CHANGE_PASSWORD_FAILED` *"kèm message chi tiết từ Identity"* | ✅ bảng mã lỗi + ví dụ envelope; thêm `USER.NOT_FOUND` (404) vào route này |
| [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §`ErrorDescriptor` | dùng `{Reasons}` làm ví dụ mẫu cho chỗ giữ đặt tên | ✅ đổi thành lệnh đếm, thay cho câu "còn 3 descriptor" |

**~~Chưa chạm, cố ý~~ — lý do hoãn đã hết hiệu lực, gỡ 2026-09-08.**
`doc/Design/Frontend/PlatformManager/Screens/05-auth.md` nêu câu hỏi để ngỏ mà quyết định 1 vừa
trả lời (§11.2). Lý do hoãn ghi ở đây là: *"Nó là đặc tả giao diện, và việc FE đọc `fieldErrors`
là pha FE — cập nhật nó lúc màn hình chưa đổi sẽ là mô tả một giao diện chưa ai xây."* Tiền đề
đó **sai từ 2026-09-06** — màn hình đã đổi thật (ba neo ở khối 🔄 mở đầu §11).

Việc này nay **đến hạn**, và **không thuộc file này**: `doc/Design/` là khu có luật riêng
([`doc/Design/CLAUDE.md`](../../../Design/CLAUDE.md)) và người phụ trách riêng — **`design-expert`**.
Phạm vi bàn giao: đóng câu hỏi để ngỏ ở §11.2 bằng hành vi THẬT của màn hình (mã Identity hiện
trên đúng ô), có neo `file:dòng` sang mã nguồn FE, chứ không bằng dự kiến.

Bài học đi kèm, vì nó lặp lại được: **một quyết định hoãn phải neo vào tiền đề của nó.** Đoạn
hoãn trên đúng lúc viết, nhưng tiền đề của nó hết đúng sau đúng một ngày, và không có gì trong
câu chữ buộc ai đó kiểm lại. Hoãn thì ghi luôn *"hết hoãn khi X"* — ở đây X là "FE đọc
`fieldErrors`", một điều kiện `grep` được.
