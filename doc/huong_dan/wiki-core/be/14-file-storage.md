---
kind: luat
scope: core
verified: 2026-09-09
---

# 14. Lưu file trên đĩa — upload, export, file mẫu

> Đây là **file chủ** cho thành phần core **#14 File storage abstraction**
> ([01-core-components.md](01-core-components.md)). Mọi quy tắc về file nằm trên
> đĩa hoặc object storage ở đây, không rải sang nơi khác.
>
> **KHÔNG thuộc file này:** vòng đời *dòng dữ liệu trong DB* (soft-delete,
> archival, hard-delete) — đó là [10-data-retention.md](10-data-retention.md).
> Hai chủ đề nghe giống nhau ở chữ "dọn dẹp" nhưng cơ chế khác hẳn: dòng DB xoá
> bằng SQL **trong** transaction, file trên đĩa xoá bằng lời gọi filesystem
> **ngoài** mọi transaction. Sự lệch pha đó là bài toán riêng của file — xem §6.
>
> **Cũng không thuộc file này:** quy tắc *cái gì được commit vào git* — đó là
> [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md).

## 1. Hai loại file — luật đặt chỗ, quyết định TRƯỚC khi tạo thư mục

Sai lầm phổ biến là dựng một cây `Resources/` gom chung `Templates/`, `i18n/`,
`Uploads/`, `Exports/`. Bốn thứ đó có **vòng đời hoàn toàn khác nhau**, và gộp
chúng lại là cách chắc chắn nhất để file người dùng upload lọt vào git history.

| | **Tài sản của source** | **Dữ liệu runtime** |
|---|---|---|
| Ví dụ | file mẫu import, ảnh/logo, file bản dịch | file người dùng upload, file export sinh ra |
| Ai tạo | lập trình viên, lúc viết code | người dùng/hệ thống, lúc chạy |
| Vào git? | **Có** — versioned cùng code | **KHÔNG BAO GIỜ** |
| Đặt ở đâu | trong cây source của project sở hữu nó | ngoài cây source, đường dẫn **lấy từ cấu hình** |
| Mất thì sao | `git checkout` là có lại | mất vĩnh viễn, không khôi phục được |

Phép thử một dòng: **"xoá thư mục này rồi build lại — có lại được không?"**
Có → tài sản của source. Không → dữ liệu runtime, phải nằm ngoài repo.

## 2. Hiện trạng — ✅ CÓ THẬT: seam lưu file đã thi công (đối chiếu source 2026-09-09)

**Seam đã có, và đường dẫn đã đến từ cấu hình.** Kiểm bằng lệnh, đừng tin bảng
(§6 của `.claude/CLAUDE.md`):

```bash
grep -rn "interface IFileStorage" src/BE --include=*.cs | grep -v /obj/
grep -rn "class LocalFileStorage\|class StorageOptions" src/BE --include=*.cs | grep -v /obj/
grep -rn "File.WriteAll\|new FileStream\|Directory.CreateDirectory" src/BE --include=*.cs | grep -v /obj/ | grep -v Tests
```

PASS hôm nay: hai lệnh đầu mỗi lệnh ra đúng **một** khai báo; lệnh thứ ba ra
**đúng những dòng nằm trong `LocalFileStorage`** (`LocalFileStorage.cs:45`, `:49`,
`:67`) cộng **một dòng chú thích XML** nhắc tên `Directory.CreateDirectory`
(`src/BE/Core/PlatformManager.Core.Infrastructure/Storage/StorageOptions.cs:42`) —
không phải một đường ghi file. Một dòng `new FileStream` xuất hiện
**ngoài** `LocalFileStorage` là vi phạm — mọi đường ghi file của người dùng phải
đi qua seam.

> 🔄 **SỬA 2026-09-10.** Đoạn trên trước ghi lệnh thứ ba còn ra *"một dòng ghi log
> Serilog"* và neo vào một dòng của `Program.cs`. Đo lại: **`Program.cs` không khớp mẫu
> grep đó dòng nào** — dòng được trích là `.WriteTo.File(` của Serilog, một lời gọi sink,
> không phải `File.WriteAll`. Neo trỏ đúng file nhưng khẳng định quanh nó sai — đúng loại
> lỗi `check-docs.sh` mục 7 không bắt được.

| Có thật hôm nay (2026-09-09) | Sẽ thành |
|---|---|
| ✅ `IFileStorage` ở `Core.Application` (`src/BE/Core/PlatformManager.Core.Application/Storage/IFileStorage.cs:36`) — dùng chung cho mọi feature cần file, không mỗi nơi một seam | giữ nguyên |
| ✅ Hiện thực `LocalFileStorage` ở `Core.Infrastructure` (`src/BE/Core/PlatformManager.Core.Infrastructure/Storage/LocalFileStorage.cs:16`), khoá lưu trữ đúng layout `<khu>/<feature>/<id><ext>` khai ở `:101` | thêm hiện thực object storage khi có nhu cầu thật — seam không phải đổi |
| ✅ Đường dẫn đọc từ `Storage:RootPath`, bind qua `StorageOptions` (`src/BE/Core/PlatformManager.Core.Infrastructure/Storage/StorageOptions.cs:16`) + fail-fast `ValidateOnStart()` (`src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:219`) — xem §3 | giữ nguyên |
| ⚠️ Seam đã có nhưng **chưa nơi gọi nào** — `grep -rn "IFileStorage" src/BE/Core src/BE/PlatformManager.Api --include=*.cs \| grep -v /obj/` hôm nay chỉ ra dòng khai báo, dòng hiện thực và dòng đăng ký DI. Hai khu `uploads/`/`exports/` mới chỉ tồn tại trong `AreaFolder` (`LocalFileStorage.cs:87`) | handler import đầu tiên gọi `SaveAsync(FileStorageArea.Upload, …)`; đó cũng là lúc §6 (dọn file) thôi là nợ lý thuyết |
| 📐 Chưa có cơ chế dọn file | Chính sách retention theo §6 |
| ✅ `App_Data/` **đã** bị `.gitignore` chặn (`src/BE/.gitignore:23`, cùng `logs/` ở dòng 24) | — việc này xong rồi; luật ở [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |

> **🔄 LẬT 2026-09-09 — code đã đuổi kịp doc, nhãn phải đổi theo.** Bản 2026-09-06 của mục
> này mang nhãn `📐 ĐÍCH ĐẾN, CHƯA THI CÔNG` và mở đầu bằng *"Không có seam nào trong
> `src/BE`"*. Cả hai đã hết đúng: `IFileStorage`, `LocalFileStorage`, `StorageOptions` +
> validator và lời đăng ký fail-fast đều là code chạy thật, có unit test riêng
> (`src/BE/Tests/PlatformManager.Core.UnitTests/Storage/LocalFileStorageTests.cs`,
> `.../Storage/StorageOptionsValidatorTests.cs`). Giữ nhãn cũ ở đây sẽ khiến lượt sau đi
> **xây lại** một seam đã có — đúng chi phí mà §4 của `.claude/CLAUDE.md` sinh ra để tránh,
> chỉ theo chiều ngược lại với chiều thường gặp.
>
> **Chỉ đổi nhãn cho thứ đo được.** Ba mục vẫn chưa xong và giữ nguyên trạng thái:
> export (§5), cơ chế dọn file (§6), và hiện thực storage ngoài đĩa cục bộ.
>
> 🔎 Lịch sử lượt lật trước (2026-09-06) giữ lại vì bài học còn dùng được: khi đó tiêu đề
> ghi `🚧 ĐÃ CHỐT, ĐANG THI CÔNG` + *"Seam đã có và đúng hướng"* trong khi bảng ngay dưới
> nói *"Không có seam nào"*; ngày đối chiếu (`2026-08-27`) còn sớm hơn sự kiện nó mô tả
> (`2026-08-29`). **Nguyên nhân cả hai lượt giống hệt nhau: bảng được sửa, câu mở đầu thì
> không.** Sửa mục này lần sau thì sửa cả hai cùng lúc.

> Thứ tự ưu tiên còn lại: **§6 (dọn file) đi CÙNG lượt có nơi gọi đầu tiên**, không sau đó.
> Hôm nay chưa file nào được ghi nên nợ này chưa tốn gì — nhưng từ dòng `SaveAsync` đầu tiên
> trở đi, mỗi ngày chạy là thêm file không ai xoá, và lúc đó việc dọn phải làm ngược trên một
> kho đã đầy. §5 (export) không có tính chất đó, làm sau vẫn rẻ.

## 3. Đường dẫn phải đến từ cấu hình — ✅ CÓ THẬT (đối chiếu 2026-09-09)

Một section cấu hình, một root duy nhất, mọi loại file runtime là thư mục con:

```
Storage:RootPath          # Development: bỏ trống -> mặc định {ContentRoot}/App_Data
                          # Production:  đường dẫn volume/mount, KHÔNG nằm trong thư mục app
  ├── uploads/<feature>/<jobId><ext>
  └── exports/<feature>/<jobId><ext>
```

```csharp
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Bỏ trống = {ContentRootPath}/App_Data. Production PHẢI trỏ ra ngoài thư mục app.</summary>
    public string? RootPath { get; init; }
}
```

Đăng ký fail-fast theo đúng luật đã có ở
[`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md)
§"Cấu hình — fail-fast validation" — không lặp lại cách làm ở đây.

**✅ Đã thi công đúng khuôn trên (đối chiếu 2026-09-09).** Khối `csharp` ngay trên là bản rút
gọn của `src/BE/Core/PlatformManager.Core.Infrastructure/Storage/StorageOptions.cs:16`; luật
"Production PHẢI khai, và phải là đường dẫn TUYỆT ĐỐI" nằm ở `StorageOptionsValidator`
(cùng file, `:49`) chứ không ở data annotation — vì nó phụ thuộc `IHostEnvironment`. Lời đăng
ký + `ValidateOnStart()` ở
`src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:219`, và
`ValidateOnStart()` là **không điều kiện** (lý do ghi tại chỗ, `:214`): gọi có điều kiện thì
cấu hình sai chỉ lộ ra ở môi trường đã bật kiểm, tức đúng môi trường ít ai chạy thử nhất.

**Vì sao không để giá trị mặc định cho production:** một đường dẫn mặc định
"chạy được" là đường dẫn không ai kiểm lại, và nó luôn trỏ vào trong thư mục
app — đúng chỗ bị xoá sạch mỗi lần deploy. Cùng một bài học đã trả giá ở
`BootstrapOptions`
(`src/BE/Core/PlatformManager.Core.Persistence/BootstrapOptions.cs`):
cấu hình giả đặt vào cho qua validation vẫn thất bại âm thầm đúng lúc cần dùng.

## 4. Vì sao `ContentRootPath` là bẫy — ba kịch bản hỏng

Đây là lý do §3 tồn tại, giữ lại vì nó vẫn là cái bẫy dễ rơi lại nhất. Ghi ở
thì hiện tại thì mục này đang mô tả **cách làm bị loại bỏ**, không phải code
hôm nay: từ 2026-09-09, `RootPath` bỏ trống chỉ còn hợp lệ **ngoài Production**
(`LocalFileStorage.cs:24`), còn ở Production `StorageOptionsValidator` chặn ngay
lúc khởi động. Ba tình huống dưới đây là thứ sẽ quay lại nếu ai đó nới luật đó —
và không tình huống nào báo lỗi lúc build:

1. **Container** — `ContentRootPath` nằm trong image layer. Redeploy = mất toàn
   bộ file đã nhận. Job import đang chờ sẽ đọc vào đường dẫn không còn tồn tại.
2. **Nhiều instance** — instance A ghi file, Hangfire dispatch job sang instance
   B, B mở `ImportJob.StoragePath` và không thấy gì. Lỗi này không tái hiện được
   trên máy dev 1 process.
3. **Thư mục app chỉ-đọc** — cấu hình siết chặt thường gắn app read-only;
   `Directory.CreateDirectory` ném exception ngay lần upload đầu tiên.

Cả ba biến mất khi `RootPath` trỏ vào volume/mount bên ngoài. Đó là lý do §3
đứng trước mọi việc khác trong file này.

## 5. Export — 📐 ĐÍCH ĐẾN, CHƯA THI CÔNG

> 📖 Định dạng file, ranh giới bộ lọc Core↔Module, hình dạng endpoint và luật
> "export dùng chung filter với endpoint danh sách": đọc
> [15-import-export.md](15-import-export.md). Mục này chỉ giữ phần **file nằm ở
> đâu và ai dọn**.

Hôm nay **không có tính năng xuất file nào**. Bản duy nhất từng tồn tại là nút
"Xuất báo cáo" của dashboard — nó trả **chuỗi HTML** qua envelope rồi FE dựng
dialog, không sinh file, không chạm đĩa — và đã xoá cùng module 2026-08-29.

Ghi lại vì nó là **một lựa chọn hợp lệ, không phải thiếu sót**: báo cáo chỉ để
xem ngay thì trả HTML rẻ hơn hẳn sinh file. Chỉ khi người dùng cần **giữ lại**
bản báo cáo mới cần tới seam file bên dưới.

Khi có yêu cầu xuất file thật, áp đúng khuôn của Import, không phát minh đường
mới:

- File export là **dữ liệu runtime** (§1) → cùng `RootPath`, khác thư mục con.
- Sinh file nặng thì đi qua job nền + poll trạng thái, đúng pattern ở
  [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md)
  §"Command chạy lâu → job nền". Không giữ request mở để chờ render.
- Trả **link tải** trong envelope, không nhồi bytes vào `data`.
- Export có thời hạn — xem §6.

> ⚠️ **Bốn gạch đầu dòng trên chỉ áp cho đường JOB NỀN.** Chốt 2026-08-29: export
> có **hai** đường, chọn theo ngưỡng số dòng — dưới ngưỡng thì stream thẳng trong
> response, **không chạm đĩa, không sinh job, không cần retention**, nên §6 không
> áp cho nó. Lý do và ngưỡng: [15-import-export.md](15-import-export.md) §3.
>
> Bản trước của mục này chỉ mô tả đường job nền như thể đó là đường duy nhất —
> nghĩa là xuất 62 dòng danh mục cũng phải tạo job, poll, tải link, rồi để lại
> một file cần dọn.

## 6. Dọn file — vấn đề riêng, không phải bản sao của retention DB

[10-data-retention.md](10-data-retention.md) giải quyết dòng dữ liệu trong DB.
File trên đĩa cần chính sách **riêng**, vì ba lý do không áp dụng cho dòng DB:

1. **Không có transaction.** Xoá dòng `ImportJob` thành công rồi xoá file thất
   bại → file mồ côi. Ngược lại → `StoragePath` trỏ vào hư không. Phải chọn
   trước chiều nào được phép hỏng, và chiều đúng là **giữ file lâu hơn dòng DB**
   (file mồ côi tốn đĩa; đường dẫn chết làm vỡ chức năng).
2. **Soft-delete không áp được.** Không có cột `IsDeleted` trên filesystem — file
   hoặc còn hoặc mất.
3. **Không đi qua backup của DB.** Khôi phục DB về mốc cũ không mang file quay
   lại. Backup file là việc tách riêng.

Chính sách tối thiểu khi bật §3:

| Loại | Giữ bao lâu | Lý do |
|---|---|---|
| File upload đã import xong | tới khi dòng `ImportJob` bị dọn | còn cần để tra khi kết quả bị nghi ngờ |
| File upload của job lỗi/treo | dài hơn job thành công | đây đúng là thứ người ta cần khi đi tìm nguyên nhân |
| File export | ngắn — sinh lại được từ dữ liệu | giữ lâu là tự tạo bản sao dữ liệu lệch dần |

Dọn bằng job Hangfire định kỳ, không xoá đồng bộ trong request.

### 📐 Con số cụ thể — CHỐT 2026-09-10, CHƯA THI CÔNG

Bảng trên khai **hình dạng** chính sách (chiều nào được phép hỏng, loại nào giữ lâu hơn
loại nào) nhưng không có con số, nên không ai thi công được mà không tự nghĩ ra một mốc.
Người dùng chốt ba mốc sau:

| Loại | Giữ | Vì sao mốc này |
| --- | --- | --- |
| Dòng `ImportJob` **thành công** + file upload của nó | **7 ngày** | Đủ dài để tra khi kết quả một lần nạp bị nghi ngờ — nghi ngờ kiểu đó nảy sinh trong tuần làm việc, không phải sau một quý |
| Dòng `ImportJob` **lỗi/treo** + file upload của nó | **30 ngày** | Đây đúng là thứ người ta đi tìm khi dò nguyên nhân, và việc dò thường bắt đầu muộn. Dài hơn job thành công — đúng chiều bảng trên đã chốt |
| File **export** | **24 giờ** | Sinh lại được từ dữ liệu bất cứ lúc nào. Giữ lâu là tự tạo một bản sao lệch dần khỏi nguồn |

**Trạng thái: chưa có dòng code nào** (đối chiếu 2026-09-10). `IFileStorage` hiện chưa có
nơi gọi nào trong mã sản phẩm ngoài đăng ký DI, nên chưa file nào được ghi ra để mà dọn.

**Thi công cùng lượt dựng đường import (DM-7), không sớm hơn** — và đó là quyết định chứ
không phải trì hoãn: một job dọn viết trước khi có file để dọn thì không nghiệm thu được
bằng luồng thật, chỉ bằng dữ liệu tự dựng. Nghiệm thu đúng là: nạp một file, đợi qua mốc,
kiểm file đã biến mất **và** dòng `ImportJob` tương ứng cũng vậy — hai vế phải cùng đúng,
vì §6 ý 1 ở trên chọn sẵn chiều được phép hỏng là **file sống lâu hơn dòng DB**.

## 7. File mẫu import — tài sản của source, KHÔNG phải storage runtime

Đây là mục dễ đặt nhầm chỗ nhất. File mẫu `.csv`/`.xlsx` cho người dùng tải về
**không** đi qua `IFileStorage`: nó không do ai upload, không có vòng đời, và
phải đổi cùng lúc với code đọc cột (`ImportColumnNames`) — nghĩa là nó phải nằm
trong git, cạnh code đó, chứ không nằm trên volume ngoài.

Đặt trong cây source của project sở hữu tính năng Import, phục vụ qua static
file hoặc embedded resource. Hôm nay **chưa có file mẫu nào**, và
`src/BE/PlatformManager.Api/` chưa có thư mục `wwwroot`.

## 8. Áp dụng vào PlatformManager

Mức ưu tiên theo đúng nguyên tắc Nhóm A/B ở
[01-core-components.md](01-core-components.md) — chỉ làm khi có nỗi đau thật,
không làm trước "phòng khi cần":

| Việc | Khi nào | Vì sao không sớm hơn/muộn hơn |
|---|---|---|
| ~~`App_Data/` vào `.gitignore`~~ | ✅ **Xong** (đối chiếu 2026-09-06, `src/BE/.gitignore:23`) | Rẻ nhất, và hậu quả không lùi được: file lọt vào git history không xoá sạch bằng một commit |
| ~~Đường dẫn qua `Storage:RootPath` (§3)~~ | ✅ **Xong** (đối chiếu 2026-09-09, `src/BE/Core/PlatformManager.Core.Infrastructure/Storage/StorageOptions.cs:16`) | Ba kịch bản §4 đều không lộ ra trên máy dev |
| ~~Dựng `IFileStorage` ở `Core.Application` (§5)~~ | ✅ **Xong** (đối chiếu 2026-09-09, `src/BE/Core/PlatformManager.Core.Application/Storage/IFileStorage.cs:36`) | Ngưỡng cũ là "khi có tính năng **thứ hai** cần file" (Rule of Three). Nay có ≥2 màn import/export đã lên kế hoạch và **không còn seam cũ để bám theo**, nên dựng thẳng ở Core thay vì dựng trong module rồi nâng lên |
| Chính sách dọn file (§6) | 📐 **còn nợ** — cùng lượt có nơi gọi `SaveAsync` đầu tiên | Cần biết file nằm ở đâu trước khi bàn chuyện dọn nó. §3 xong rồi nên điều kiện tiên quyết đã đủ; thứ còn thiếu là một file thật để dọn |
| S3/Blob thay local | khi chạy ≥2 instance thật | Volume dùng chung giải quyết được đa số; đổi hạ tầng lưu trữ chỉ để "cho chuẩn" là đổi chi phí vận hành lấy không gì |

✅ **Bẫy đường dẫn cũ đã tránh được (đối chiếu 2026-09-09).** Bản `IImportFileStorage`
cũ mang chú thích trỏ `.claude/rules/cqrs-handler.md`, một đường dẫn **đã chuyển** về
[`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md). `IFileStorage`
mới không chép lại nó — kiểm bằng lệnh, không bằng trí nhớ:

```bash
grep -rn "\.claude/rules" src/BE --include=*.cs | grep -v /obj/
```

PASS = **rỗng**. Đây chính là mục 11 của `.claude/check-docs.sh` áp ở phạm vi hẹp hơn:
chú thích trong `src/` trỏ `doc/` cũng phải tồn tại.
