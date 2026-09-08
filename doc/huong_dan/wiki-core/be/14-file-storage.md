---
kind: luat
scope: core
verified: 2026-09-06
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

## 2. Hiện trạng — 📐 ĐÍCH ĐẾN, CHƯA THI CÔNG (đối chiếu source 2026-09-06)

**Không còn seam nào trong `src/BE`.** Toàn bộ code lưu file từng tồn tại đi cùng module
DtiWeekly, gỡ 2026-08-29. Kiểm bằng lệnh, đừng tin bảng (§6 của `.claude/CLAUDE.md`):

```bash
grep -rn "IImportFileStorage\|IFileStorage" src/BE --include=*.cs | grep -v /obj/
grep -rn "File.WriteAll\|new FileStream\|Directory.CreateDirectory" src/BE --include=*.cs | grep -v /obj/ | grep -v Tests
```

PASS hôm nay: lệnh đầu **rỗng**; lệnh sau ra đúng **một** dòng và nó là đường ghi log
Serilog (`src/BE/PlatformManager.Api/Program.cs:73`), không phải storage của người dùng.

| Có thật hôm nay | Sẽ thành |
|---|---|
| **Không có seam nào** — `IImportFileStorage` của module DtiWeekly đã xoá cùng module 2026-08-29 | `IFileStorage` dùng chung, sống ở `Core.Application` — import, export và mọi feature cần file dùng lại, không mỗi nơi một seam |
| Không có code lưu file nào | Đường dẫn đọc từ cấu hình `Storage:RootPath`, bind qua `StorageOptions` fail-fast — xem §3 |
| Không có nhánh upload lẫn export | Upload + export, cùng một seam — xem §5 |
| Không có cơ chế dọn file | Chính sách retention theo §6 |
| ✅ `App_Data/` **đã** bị `.gitignore` chặn (`src/BE/.gitignore:23`, cùng `logs/` ở dòng 24) | — việc này xong rồi; luật ở [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |

> **🔄 LẬT 2026-09-06.** Bản trước sai ba chỗ, cùng một nguyên nhân: tiêu đề và câu mở
> không được sửa khi bảng bên dưới đã cập nhật.
> 1. Nhãn `🚧 ĐÃ CHỐT, ĐANG THI CÔNG` + câu *"Seam đã có và đúng hướng"* mâu thuẫn thẳng
>    với dòng đầu của chính cái bảng nó giới thiệu (*"Không có seam nào"*). Không có gì
>    đang thi công — đúng nhãn là `📐 ĐÍCH ĐẾN`.
> 2. Ngày đối chiếu ghi `2026-08-27`, trong khi nội dung bảng nói về việc xảy ra
>    **2026-08-29**. Ngày đối chiếu không thể sớm hơn sự kiện nó mô tả.
> 3. *"`App_Data/` chưa được `.gitignore` chặn"* — sai: `src/BE/.gitignore:23` đã có
>    `App_Data/`. Thư mục `src/BE/PlatformManager.Api/App_Data/imports/` còn file `.csv`
>    sót lại trên đĩa, nhưng `git ls-files` trên nó ra rỗng, tức chưa file nào bị theo dõi.

> Thứ tự ưu tiên: **§3 (đường dẫn qua cấu hình) trước**, vì nó là thứ duy nhất
> không lùi được — mỗi feature mới ghép cứng thêm một đường dẫn là một chỗ phải
> sửa lại sau. Việc gộp seam (§5) rẻ hơn nhiều nếu làm sau.

## 3. Đường dẫn phải đến từ cấu hình — 📐 ĐÍCH ĐẾN, CHƯA THI CÔNG

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

**Vì sao không để giá trị mặc định cho production:** một đường dẫn mặc định
"chạy được" là đường dẫn không ai kiểm lại, và nó luôn trỏ vào trong thư mục
app — đúng chỗ bị xoá sạch mỗi lần deploy. Cùng một bài học đã trả giá ở
`BootstrapOptions`
(`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/BootstrapOptions.cs`):
cấu hình giả đặt vào cho qua validation vẫn thất bại âm thầm đúng lúc cần dùng.

## 4. Vì sao `ContentRootPath` là bẫy — ba kịch bản hỏng

Cách làm hiện tại chạy đúng trên máy dev và hỏng ở cả ba tình huống triển khai
thật, không tình huống nào báo lỗi lúc build:

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
| Đường dẫn qua `Storage:RootPath` (§3) | **trước lần deploy thật đầu tiên** | Ba kịch bản §4 đều không lộ ra trên máy dev |
| Dựng `IFileStorage` ở `Core.Application` (§5) | **ngay** — ngưỡng đã đạt 2026-08-29 | Ngưỡng cũ là "khi có tính năng **thứ hai** cần file" (Rule of Three). Nay có ≥2 màn import/export đã lên kế hoạch và **không còn seam cũ để bám theo**, nên dựng thẳng ở Core thay vì dựng trong module rồi nâng lên |
| Chính sách dọn file (§6) | cùng lúc bật §3 | Cần biết file nằm ở đâu trước khi bàn chuyện dọn nó |
| S3/Blob thay local | khi chạy ≥2 instance thật | Volume dùng chung giải quyết được đa số; đổi hạ tầng lưu trữ chỉ để "cho chuẩn" là đổi chi phí vận hành lấy không gì |

Ghi chú cho người implement: bản `IImportFileStorage` cũ có chú thích trỏ tới
`.claude/rules/cqrs-handler.md` — đường dẫn đó **đã chuyển** về
[`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md). Đừng
chép lại đường dẫn cũ vào `IFileStorage` mới.
