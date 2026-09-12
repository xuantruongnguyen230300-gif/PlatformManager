---
kind: luat
scope: core
status: "import + export built 2026-09-11; seam ITabularWriter hoãn (Q9)"
verified: 2026-09-11
---

# 15. Import / Export engine — định dạng, bộ lọc, hình dạng endpoint

> **Nửa còn lại của [14-file-storage.md](14-file-storage.md).** File đó trả lời
> *"file nằm ở đâu trên đĩa, ai dọn, khi nào"*. File này trả lời *"đọc/ghi định
> dạng nào, ai quyết định lọc gì, endpoint hình dạng ra sao"*.
>
> Không lặp lại chuyện đường dẫn/retention ở đây. Job nền: xem
> [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md)
> §"Command chạy lâu → job nền". Seam `IBackgroundJobScheduler` đã có thật ở
> `Core.Application` — xem [01-core-components.md](01-core-components.md) #17.

## Trạng thái — hai nửa, hai nhãn khác nhau (đối chiếu 2026-09-09)

Đóng một nhãn chung cho cả file này là sai theo **cả hai chiều**: nó vừa giấu mất
một nửa đã chạy thật, vừa hứa một nửa còn chưa có dòng nào.

| Nửa | Nhãn | Neo |
| --- | --- | --- |
| **Import** — seam + 3 reader + bộ chọn | ✅ **CÓ THẬT**, thi công 2026-09-09 | `src/BE/Core/PlatformManager.Core.Application/Import/IImportFileReader.cs:17` · `.../Import/IImportFileReaderSelector.cs` · `src/BE/Core/PlatformManager.Core.Infrastructure/Import/{CsvImportFileReader,ExcelImportFileReader,ImportFileReaderSelector}.cs` |
| **Import** — nơi gọi (endpoint/handler/bảng job) | ✅ **CÓ THẬT**, thi công 2026-09-11 | `src/BE/Business/PlatformManager.Business.Api/Controllers/ImportController.cs:56` (hai route) · handler bước 1 `src/BE/Business/PlatformManager.Business.Application/Import/StartImportCommand.cs:73` · thân job `src/BE/Business/PlatformManager.Business.Application/Import/ImportJobRunner.cs:34` |
| **Export** — nơi ghi file | ✅ **CÓ THẬT**, thi công 2026-09-11 | DB-4: `src/BE/Business/PlatformManager.Business.Infrastructure/Export/DashboardExcelExportWriter.cs` — NPOI trực tiếp, đúng Q9 |
| **Export** — seam `ITabularWriter` của Core | 📐 **CHƯA THI CÔNG** | **hoãn có chủ đích** (Q9) — xem §7. Không phải bỏ sót |

Kiểm bằng lệnh, đừng tin bảng (`.claude/CLAUDE.md` §6):

```bash
grep -rn "interface IImportFileReader\b" src/BE --include=*.cs | grep -v /obj/   # PASS: 1 dòng
grep -rn "ITabularWriter" src/BE --include=*.cs | grep -v /obj/                  # PASS: rỗng
grep -rn "ReadRowsAsync" src/BE --include=*.cs | grep -v /obj/                   # PASS: khác rỗng (nơi gọi đã có)
```

> 🔄 **LẬT 2026-09-11 (nửa "nơi gọi" và nửa Export).** Hai hàng đó mang nhãn 📐 cho tới hết
> 2026-09-10; đường import của DM-7 và đường xuất của DB-4 đều thi công ngày 2026-09-11 và đã chạy
> thật trên `platformmanager_dev`. Seam `ITabularWriter` thì **vẫn** hoãn, đúng Q9 — xem §7.
>
> 🔄 **LẬT 2026-09-09.** Bản trước ghi *"📐 ĐÍCH ĐẾN, CHƯA THI CÔNG — Core hiện chưa có
> dòng nào"*, cùng `status: "import/export not built"` + `verified: khong-ap-dung` ở
> frontmatter. Nửa import đã hết đúng. Đáng chú ý là **`verified: khong-ap-dung` không phải
> giá trị sai lúc nó được đặt** — khi đó không có gì để đối chiếu nên nó trung thực; nó chỉ
> thành sai đúng vào ngày code xuất hiện. Đó là lý do khoá đó phải được xem lại mỗi lượt
> thi công, chứ không phải đặt một lần rồi quên.

Các quyết định trong file này chốt 2026-08-29 (định dạng, ranh giới) và 2026-09-09
(trần dung lượng Q12b, hoãn `ITabularWriter` Q9).

## 1. Ranh giới Core ↔ Module — bộ lọc KHÔNG bao giờ đi vào Core

Đây là quyết định quan trọng nhất của file này, vì làm sai nó là làm hỏng ranh
giới Core/Business ở [`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md).

| Core giữ | Module giữ |
| --- | --- |
| Đọc file → dòng dữ liệu trung tính (`IImportFileReader`) | Cột nào bắt buộc, validate gì, upsert vào entity nào |
| Ghi dòng + mô tả cột → file (`ITabularWriter`) | Query nào, **lọc theo gì**, cột nào xuất ra, format từng ô |
| Lưu file, chạy job nền (`IBackgroundJobScheduler`), trả link | **Bảng trạng thái job** (`ImportJobs`) + entity + endpoint poll — xem ghi chú ngay dưới |
| Trần số dòng (**Q75** — §2), trần **dung lượng file**, tắt phân trang, dọn file hết hạn | Permission-key của chính nghiệp vụ đó · ý nghĩa nghiệp vụ của bộ lọc |

> ### 🔄 SỬA 2026-09-09 (Q11) — `ImportJobs` là bảng NGHIỆP VỤ, không phải bảng Core
>
> Bản trước xếp *"theo dõi trạng thái"* vào cột Core giữ. Sai, và nó nói ngược lại hai file
> đã chốt điều đó trước: [`../../../cau-truc-database.md`](../../../cau-truc-database.md)
> — *"Định nghĩa `business."ImportJobs"` **không** còn ở đây — nó là bảng nghiệp vụ"* — và
> [`../../../cau-truc-database-dti.md`](../../../cau-truc-database-dti.md), nơi khai bảng đó;
> bộ cột đầy đủ ở `spec/danh-muc-dti/business-rules.md` §1.5.
>
> Chốt: bảng ở schema **`business`**, entity ở **`Business.Domain`**, EF Configuration ở
> `Business.Persistence`, endpoint poll ở `Business.Api`. Core **không** khai entity nào cho
> việc theo dõi job — nó chỉ cung cấp **cơ chế chạy** (`IBackgroundJobScheduler`) và cơ chế
> lưu file. Để bảng đó ở Core nghĩa là mang một khái niệm import của DTI sang mọi dự án dựng
> trên nền tảng này, đúng thứ §1 này sinh ra để chặn.

**Vì sao "lọc theo Tuần/Tháng" thuộc Module, không thuộc Core.** Kỳ báo cáo là
khái niệm nghiệp vụ DTI. Nếu Core biết "tuần ISO thứ 35 của năm 2026 nghĩa là gì
với bảng đánh giá", Core đã lấn nghiệp vụ — và sản phẩm thứ hai dựng trên nền
tảng này sẽ mang theo một khái niệm nó không dùng.

Handler của module chạy query **của chính nó** với đúng bộ lọc đó, rồi đưa Core
một chuỗi dòng. Core chỉ thấy dòng và cột, không thấy `WHERE`.

> ⚠️ **Phân biệt hai thứ dễ lẫn.** *"Tuần ISO thứ 35 của 2026 là từ ngày nào tới
> ngày nào"* là logic **thời gian thuần**, không dính nghiệp vụ — thứ đó đặt ở
> Core được. Còn *"lọc bản ghi đánh giá theo cột `AssessmentDate` trong khoảng
> đó"* là nghiệp vụ, thuộc module. Đưa cái đầu lên Core thì được; đưa cái sau lên
> là hỏng ranh giới.

## 2. Import — CSV + XLSX + XLS

Chốt 2026-08-29: hỗ trợ **cả ba**. `.xls` giữ lại vì người dùng khu vực công thật
sự còn file cũ, không phải vì "cho đủ bộ".

Seam giữ **hình dạng** đã dùng ở module cũ (dictionary theo tên cột), nhưng đổi
**kiểu giá trị của ô** — lý do ngay dưới khối:

```csharp
public interface IImportFileReader
{
    bool CanRead(ReadOnlySpan<byte> header, string fileName);
    IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadAsync(
        Stream stream, string fileName, CancellationToken ct);
}
```

> ### 🔄 SỬA 2026-09-09 (Phần C) — `string?` → `ImportCellValue`, và vì sao chọn vế đó
>
> Bản trước của khối này in `IReadOnlyDictionary<string, string?>`, trong khi **§2c của
> chính mục này** đòi *"giữ kiểu ngày thật qua seam thay vì chuyển thành chuỗi"*. Hai câu
> không cùng đúng được — file tự mâu thuẫn, và code buộc phải chọn một vế.
>
> **Chọn theo §2c là chọn đúng, vì `string?` CHÍNH LÀ nguyên nhân của lỗi §2c.** Một ô
> ngày trong `.xlsx` mang kiểu ngày thật; ép nó qua một seam chỉ chở được `string?` buộc
> reader phải định dạng (mất giờ/phút) và buộc bên xử lý parse ngược (dính locale máy
> chạy). Sửa "lỗi §2c" mà giữ nguyên chữ ký `string?` là bịt triệu chứng ở hai đầu của
> đúng cái ống gây ra nó.
>
> `ImportCellValue`
> (`src/BE/Core/PlatformManager.Core.Application/Import/ImportCellValue.cs:21`) chở **cả
> hai**: `Value` giữ kiểu gốc (`DateTime`/`double`/`bool`/`string`), `Text` là chuỗi
> **invariant round-trip** để file CSV — thứ không có kiểu — vẫn dùng được đúng một đường
> như trước. Nghĩa là nó không bắt bên xử lý gánh thêm gì; nó chỉ thôi vứt thông tin đi.

Runner chọn reader bằng cách hỏi `CanRead` từng cái trong
`IEnumerable<IImportFileReader>` đã đăng ký — **không `switch` theo enum định
dạng**. Thêm định dạng thứ tư về sau chỉ là thêm một implementation; Core không
sửa dòng nào. Việc chọn đó nay đi qua `IImportFileReaderSelector`
(`src/BE/Core/PlatformManager.Core.Application/Import/IImportFileReaderSelector.cs`), để
không nơi gọi nào phải tự đọc magic byte lấy.

Thư viện: **CsvHelper** cho CSV, **NPOI** cho cả `.xls` (HSSF) lẫn `.xlsx`
(XSSF). NPOI là lựa chọn bắt buộc nếu muốn `.xls` — ClosedXML và EPPlus đều chỉ
đọc được `.xlsx`, và EPPlus từ v5 còn đổi sang giấy phép thương mại.

⚠️ **NPOI và CsvHelper chỉ được reference ở `Core.Infrastructure`, KHÔNG kéo vào
`Core.Application`** — seam ở Application phải sạch thư viện, đúng luật tầng ở
[`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md).

> ✅ **Nay cưỡng chế BẰNG MÁY (thêm 2026-09-09, finding F3).** Hai tiền tố `"NPOI"` và
> `"CsvHelper"` đã vào `ForbiddenAssemblyPrefixes` của
> `src/BE/Tests/PlatformManager.ArchTests/LayerDependencyTests.cs:48-49`.
>
> Trước đó `PlatformManager.Core.Infrastructure.csproj:46` **tuyên bố** *"LayerDependencyTests
> cưỡng chế"* trong khi mảng chỉ có `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`,
> `Hangfire` — tức máy không cưỡng chế gì. Hôm đó `Core.Application` vẫn sạch nên chưa có vi
> phạm nào; đó chính là lúc rẻ nhất để đóng, và đúng khuôn bài học 2026-09-08 ở
> `.claude/CLAUDE.md` §8 (một luật tự nhận được cưỡng chế bằng máy mà máy không chặn).
>
> Canary 2026-09-09: thêm `PackageReference CsvHelper` **kèm một lời gọi thật** vào
> `Core.Application` ⇒ test đỏ, nêu đích danh `CsvHelper`; gỡ ⇒ xanh lại. ⚠️ Chịu chung giới
> hạn đã đo của luật này: `GetReferencedAssemblies()` chỉ thấy assembly **có code chạm tới**,
> nên nó bắt "đã dùng", không bắt "csproj còn `PackageReference` thừa".

### Trần dung lượng file upload — **10 MB**, chốt Q12b · ✅ đã thi công (2026-09-09)

Mặc định + kiểm hợp lệ: `src/BE/Core/PlatformManager.Core.Infrastructure/Import/ImportOptions.cs:30`,
đăng ký `ValidateOnStart()` ở `.../DependencyInjection.cs:205`. Trần được **bộ chọn** áp
(`ImportFileReaderSelector.cs:21`), tức trước khi bất kỳ reader nào chạm nội dung — đúng yêu
cầu "từ chối trước khi đọc một byte nào" ngay dưới.

Vượt trần ⇒ từ chối **trước khi** đọc một byte nội dung nào, trả mã lỗi riêng của nghiệp vụ
gọi (với DTI là `IMPORT.FILE_TOO_LARGE`, xem
[`../../../contracts/danh-muc-dti.md`](../../../contracts/danh-muc-dti.md) DM-7).

Con số lấy từ tham chiếu thật: file BA gửi có **62 dòng ≈ 19 KB**, tức 10 MB rộng hơn khoảng
**hai bậc độ lớn** so với ca dùng thật — đủ chỗ cho file `.xlsx` mang định dạng nặng, ảnh nhúng
hoặc vài chục nghìn dòng, mà vẫn chặn được ca một người kéo nhầm file video vào ô upload.

Trần này là **cấu hình**, cùng khuôn với ngưỡng job nền ở §3 — không phải hằng số rải trong
code. Nó là trần **Core** (thuộc hàng "trần số dòng, trần dung lượng" ở bảng §1), nhưng **mã
lỗi** trả ra thì thuộc catalog của nghiệp vụ gọi.

⚠️ Trần của ứng dụng **không thay thế** trần của tầng phục vụ: Kestrel và reverse proxy đều có
giới hạn thân request riêng, và nếu chúng thấp hơn thì người dùng nhận lỗi hạ tầng cụt lủn
trước khi tới được `ErrorDescriptor` này. Đặt hai chỗ khớp nhau khi triển khai —
[`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md).

### Trần SỐ DÒNG — **Q75, chốt 2026-09-11** · mặc định **20.000 dòng** · ✅ đã thi công (2026-09-11)

Neo: mặc định + kiểm hợp lệ ở
`src/BE/Core/PlatformManager.Core.Infrastructure/Import/ImportOptions.cs:50`; bộ chọn đưa con số ra ở
`src/BE/Core/PlatformManager.Core.Infrastructure/Import/ImportFileReaderSelector.cs:22`; trần đi ra qua
`src/BE/Core/PlatformManager.Core.Application/Import/IImportFileReaderSelector.cs:40`; và **đường đọc
đã áp trần** — thứ thật sự cưỡng chế nó — ở
`src/BE/Core/PlatformManager.Core.Application/Import/ImportFileReaderSelectionExtensions.cs`.

Hàng *"trần số dòng"* đã nằm trong bảng §1 từ đầu như một trách nhiệm của Core, nhưng **không có
con số và không có chỗ cài** — nên trên thực tế nó không tồn tại, và trần dung lượng là guard duy
nhất. Q75 đóng lỗ đó.

**Cách đo ra con số, thay vì chọn một số tròn:**

| Ràng buộc | Giá trị | Suy ra từ |
| --- | ---: | --- |
| Ca dùng thật hôm nay | 62 dòng | file BA gửi — 62 dòng ≈ 19 KB |
| Kích thước một dòng của định dạng này | ≈ 314 byte | 19 KB ÷ 62 dòng, đo trên chính file đó |
| Số dòng mà trần DUNG LƯỢNG 10 MB còn cho lọt | ≈ **33.000** | 10 MB ÷ 314 byte |
| Ca lớn nhất còn hợp lý | ≈ 12.000 | 62 chỉ tiêu × ~200 đơn vị, nếu về sau có người gộp nhiều đơn vị vào một file |

Trần phải nằm **giữa hai con số cuối**: dưới ~33.000 thì nó mới thật sự bịt được lỗ (cao hơn thì
trần dung lượng luôn chạm trước và trần số dòng không bao giờ chạy), và trên ~12.000 thì nó không
chặn nhầm một file hợp lệ. **20.000** nằm giữa — dư ~320 lần so với ca dùng thật, vẫn bắt trước
trần dung lượng.

> ⚠️ **Vì sao trần số dòng KHÔNG thừa dù đã có trần dung lượng:** một file 200 KB gồm toàn dòng
> ngắn vẫn có thể có nửa triệu dòng. Hai trần chặn hai thứ khác nhau — một cái chặn *byte đọc từ
> đĩa*, một cái chặn *đối tượng dựng trong bộ nhớ*. Đây chính là câu cảnh báo đã có sẵn ở mục trần
> dung lượng; Q75 là chỗ nó được thi hành.

**Ranh giới, giữ đúng khuôn của trần dung lượng:** Core giữ **con số** (`Import:MaxRows`,
`ImportOptions`) **và cấp một ĐƯỜNG ĐỌC đã áp trần**
(`ImportFileReaderSelectionExtensions.ReadRowsAsync`); **mã lỗi** thì thuộc catalog của nghiệp vụ
gọi — Core không được biết mã lỗi của một nghiệp vụ nào. Với DTI đó là
`IMPORT.FILE_TOO_MANY_ROWS` ([`../../../contracts/danh-muc-dti.md`](../../../contracts/danh-muc-dti.md) DM-7).

> 🛑 **Core cấp ĐƯỜNG ĐỌC, không cấp một con số rời — sửa 2026-09-11.** Bản đầu chỉ thêm `MaxRows`
> vào `ImportFileReaderSelection` và để việc áp trần cho từng bên gọi tự giác. Hai trần khi đó
> **không cùng khuôn dù đoạn văn này nói là cùng**: trần dung lượng ép bằng CẤU TRÚC (vượt trần ⇒
> `Reader = null` ⇒ quên kiểm là nổ ngay, ồn ào), còn trần dòng chỉ là một `int` đi kèm — bên gọi
> **thứ hai** bỏ qua thì biên dịch sạch, test xanh, **trần biến mất im lặng**. Một trần khuyến nghị
> không phải là trần.
>
> Nay bên gọi phải đi qua `selection.ReadRowsAsync(...)`; vượt trần thì Core ném
> `ImportRowLimitExceededException` mang đúng con số, bên gọi bắt rồi ánh xạ sang `ErrorDescriptor`
> của mình — **đúng y khuôn `ImportFileRejection` → catalog nghiệp vụ**. Cưỡng chế thêm bằng máy:
> `ImportSeamUsageTests` cấm mọi mã sản phẩm gọi thẳng `Reader.ReadAsync`.

⚠️ **Core KHÔNG tự cắt dòng thừa.** Nạp một file bị cắt cụt mà không ai biết còn tệ hơn từ chối
nó: người dùng thấy "thành công" và một kỳ thiếu dữ liệu. Bên gọi đọc `MaxRows` rồi TỪ CHỐI cả
lượt.

### Ba lỗi phải sửa khi bê code cũ lên — ✅ cả ba ĐÃ SỬA (đối chiếu 2026-09-09)

> **Giữ nguyên mô tả ba lỗi, không rút gọn thành "đã sửa".** Mỗi lỗi ở đây là một cái bẫy
> của thư viện (NPOI trả chuỗi công thức, phần mở rộng không phải bằng chứng về nội dung,
> `dd/MM/yyyy` dính locale) — nó sẽ quay lại nguyên si ở định dạng thứ tư, ở writer export,
> hoặc ở dự án thứ hai dùng CoreBase. Xoá mô tả đi thì lượt sau chỉ còn cách phát hiện lại
> bằng dữ liệu sai trong DB.
>
> | Lỗi | Sửa ở |
> | --- | --- |
> | **a.** Nhận diện định dạng bằng phần mở rộng | `src/BE/Core/PlatformManager.Core.Application/Import/IImportFileReader.cs:33` — `CanRead` nhận `header` **đứng trước** `fileName`, và việc lấy header thuộc `IImportFileReaderSelector` |
> | **b.** Ô công thức trả chuỗi công thức | `src/BE/Core/PlatformManager.Core.Infrastructure/Import/ExcelImportFileReader.cs:95` — đọc `CachedFormulaResultType` thay vì `cell.ToString()` |
> | **c.** Ngày ép về chuỗi rồi parse ngược | `src/BE/Core/PlatformManager.Core.Application/Import/ImportCellValue.cs:21` — xem khối 🔄 ở trên |

Code cũ chạy được, nhưng mang ba khiếm khuyết đã xác định. Lấy lại để tham khảo:

```bash
# Liệt kê code import cũ (đã xoá 2026-08-29) — commit cuối còn giữ chúng là d20995d
git ls-tree -r d20995d --name-only | grep -i '/Import/'

# Lấy nội dung một file — đối chiếu kiểm 2026-08-29, chạy được
git show d20995d:<đường-dẫn-in-ra-ở-trên>
```

**a. Nhận diện định dạng bằng phần mở rộng file.** `CanRead` cũ chỉ so
`Path.GetExtension()`, và bản Excel còn chọn `HSSFWorkbook` hay `XSSFWorkbook`
cũng theo đuôi file — docstring của nó ghi rõ là **cố ý** không dùng magic byte.
Hệ quả: người dùng đổi tên `bao-cao.xlsx` thành `.xls` sẽ nhận một exception khó
hiểu thay vì thông báo tử tế. Sâu hơn: phần mở rộng do người dùng đặt, không phải
bằng chứng về nội dung.

Sửa: đọc vài byte đầu.

| Định dạng | Magic byte |
| --- | --- |
| `.xlsx` (OOXML = zip) | `50 4B 03 04` |
| `.xls` (OLE2 compound) | `D0 CF 11 E0 A1 B1 1A E1` |
| CSV | không có chữ ký — nhánh mặc định |

**b. Ô công thức nhập vào SAI — lỗi im lặng, không exception.** Nhánh
`CellType.Formula` cũ trả `cell.ToString()`, mà NPOI (giống Apache POI) trả về
**chuỗi công thức**, không phải kết quả. Ô `=B2*100` nhập vào DB thành chữ
`"B2*100"`. Sửa: đọc `cell.CachedFormulaResultType` rồi lấy giá trị theo đúng
kiểu đó, hoặc dùng `IFormulaEvaluator`.

Đây đúng dạng lỗi đắt nhất: không crash, không log, chỉ có dữ liệu rác nằm im
trong DB tới khi ai đó phát hiện con số vô lý.

**c. Ngày bị ép về chuỗi `dd/MM/yyyy` rồi parse ngược.** Vừa mất thông tin vừa
dính locale. Nên giữ kiểu ngày thật qua seam thay vì chuyển thành chuỗi.

### d. Dòng header là dòng VẬT LÝ đầu tiên — và dòng trống đứng trước nó phải NÉM

> **Lỗi thứ tư, tìm ra 2026-09-09 (không có trong ba lỗi bê từ code cũ) — đã sửa cùng ngày.**

Hai reader đều lấy **dòng vật lý đầu tiên** làm header (`sheet.GetRow(sheet.FirstRowNum)` với
Excel, dòng 1 với CSV). Docstring trước đó lại viết *"dòng đầu tiên **có dữ liệu** = header"* —
hai câu khác nhau ở đúng ca hay gặp nhất: **người dùng chèn một dòng tiêu đề ở đầu rồi xoá nội
dung, để lại một dòng tồn tại vật lý nhưng toàn ô rỗng.**

Hậu quả của bản cũ là hỏng **im lặng**, và mỗi reader im lặng một kiểu:

| Reader | Bản cũ làm gì | Người dùng thấy gì |
| --- | --- | --- |
| Excel | `yield break` khi không tên cột nào đọc được | "File không có dòng nào" |
| CSV | đi tiếp, mỗi dòng dữ liệu thành dictionary **rỗng** | Đúng số bản ghi, nhưng mọi phép tra cột đều trượt |

**Chốt: giữ nguyên "dòng vật lý đầu tiên", sửa docstring cho khớp, và NÉM thay vì trả 0 dòng.**

**Vì sao không chọn "bỏ qua dòng trống ở đầu rồi lấy dòng sau làm header"** — lối đó khớp
docstring cũ và nghe khoan dung hơn, nhưng nó phá một hợp đồng đang có: `IImportFileReader.ReadAsync`
quy định *"reader KHÔNG bỏ dòng nào, kể cả dòng rỗng"* vì **bên gọi đánh số dòng theo thứ tự
yield** (dòng dữ liệu thứ n = dòng n+1 của file — xem `spec/danh-muc-dti/business-rules.md`). Bỏ
dòng ở đầu file làm phép tính đó lệch đúng bằng số dòng đã bỏ, và mọi thông báo lỗi cấp dòng sau
đó trỏ nhầm. Tức là nó **đổi một lỗi im lặng lấy một lỗi im lặng khác** — còn tệ hơn, vì lỗi mới
nằm trong thông báo gửi cho người dùng.

Ném thì đúng phân loại sẵn có: *"lỗi của CẢ file, khác hẳn lỗi của một dòng"*
(`IImportFileReader.cs:46`). Thông điệp nêu cả cách sửa — *"xoá hẳn dòng trống đó (xoá cả dòng,
không chỉ xoá nội dung)"* — vì đúng thao tác "xoá nội dung mà không xoá dòng" là thứ sinh ra ca này.

Neo: `src/BE/Core/PlatformManager.Core.Infrastructure/Import/ExcelImportFileReader.cs:65` ·
`src/BE/Core/PlatformManager.Core.Infrastructure/Import/CsvImportFileReader.cs:78`. Một test cho
**mỗi** reader (`BlankRowBeforeHeader_Throws` / `BlankLineBeforeHeader_Throws`).

⚠️ Ca **file thật sự rỗng** (không sheet nào / không byte nào) vẫn là **0 dòng, KHÔNG ném** — đó
không phải file hỏng. Bốn test cũ canh điều đó, đừng gộp hai ca lại.

## 3. Export — hai đường, chọn theo ngưỡng số dòng

Chốt 2026-08-29. [14-file-storage.md §5](14-file-storage.md) mô tả **đường job
nền**; nó đúng cho file nặng nhưng là đường duy nhất, và đó là thiếu sót: xuất 62
dòng danh mục mà phải tạo job → poll → gọi link tải → rồi sinh một file trên đĩa
cần retention, là ba round-trip và một file rác để giao vài KB.

| | Điều kiện | Hình dạng |
| --- | --- | --- |
| **Đồng bộ** | dưới ngưỡng | `GET …/export?…` trả thẳng `FileStreamResult`. Không chạm đĩa, không job, không cần dọn |
| **Job nền** | trên ngưỡng | `200` + `jobId` trong envelope → poll trạng thái → link tải. Đúng khuôn §5 của file kia |

> **Sửa 2026-09-05 — ô "Job nền" trước đây ghi `202`.** Không có đường nào trong hệ sinh ra
> 202: [`ApiControllerBase.cs:37`](../../../../src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs)
> map mọi thành công về 200, và
> [`ErrorCode.cs:11`](../../../../src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorCode.cs)
> không có member nào mang giá trị đó. "Đã nhận, chưa xử lý xong" thể hiện bằng **`jobId` cần
> poll tiếp**, không bằng status code. Lý lẽ đầy đủ + đoạn mẫu controller đã sửa:
> [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §"Command chạy lâu → job nền".

Ngưỡng là **cấu hình**, không phải hằng số nằm rải trong code — bắt đầu ở mức
vài nghìn dòng rồi chỉnh theo đo đạc thật.

**Định dạng export: XLSX + CSV. KHÔNG làm `.xls`.** Bất đối xứng với import là cố
ý: `.xls` chặn cứng ở 65.536 dòng (`.xlsx` cho 1.048.576), và mọi Excel từ 2007
trở đi đều mở được `.xlsx`. Nhận `.xls` vào thì có lý do thật; xuất `.xls` ra thì
không có ai cần.

Với file lớn, dùng `SXSSFWorkbook` của NPOI (ghi luồng, xả xuống đĩa tạm) thay
cho `XSSFWorkbook` (giữ toàn bộ workbook trong RAM).

### Seam

> 🛑 **HOÃN — Q9, 2026-09-09. Chữ ký dưới đây là ĐỀ XUẤT, KHÔNG dựng ở lượt này.** Lý do và
> điều kiện dựng: §7 cuối file. Đọc §7 **trước** khi định thi công khối này.

```csharp
// Core.Application
public sealed record ExportColumn<TRow>(string Header, Func<TRow, object?> Value, string? Format = null);

public interface IExportDefinition<TRow>
{
    IReadOnlyList<ExportColumn<TRow>> Columns { get; }
    IAsyncEnumerable<TRow> FetchAsync(CancellationToken ct);
}

public interface ITabularWriter   // impl ở Core.Infrastructure: CSV + XLSX
{
    Task WriteAsync<TRow>(IExportDefinition<TRow> definition, ExportFormat format,
                          Stream target, CancellationToken ct);
}
```

Module chỉ cài `IExportDefinition<T>`. `FetchAsync` **chính là** query có bộ lọc
nghiệp vụ — Core gọi nó và không biết bên trong có gì.

## 4. Luật quan trọng nhất — export dùng CHUNG object bộ lọc với endpoint danh sách

```
GET /api/<tài-nguyên>?period=2026-W35&groupId=…&q=…                    ← lưới, CÓ phân trang
GET /api/<tài-nguyên>/export?period=2026-W35&groupId=…&q=…&format=xlsx ← CÙNG filter, BỎ phân trang
```

Cùng **một** record filter bind cho cả hai endpoint. Không sao chép điều kiện
lọc sang một đường riêng cho export.

**Vì sao đây là luật chứ không phải gợi ý.** Hai đường lọc song song *sẽ* lệch
nhau — chỉ cần một lần thêm bộ lọc mới mà quên sửa chỗ kia. Và triệu chứng thuộc
loại tệ nhất: file tải về không khớp thứ đang hiện trên màn hình, người dùng mất
niềm tin vào cả hai, còn lập trình viên không có cách nào biết bên nào đúng.
Không test nào bắt được, vì cả hai endpoint đều "chạy đúng" theo định nghĩa
riêng của chúng.

Khoá lại bằng máy:

> Cùng một bộ lọc → **số dòng dữ liệu trong file export = `totalCount` của
> endpoint danh sách**. Một integration test cho mỗi màn có export.

**Bỏ phân trang thì phải có trần.** Không có trần, một người bấm "Tất cả" trên
nhiều năm dữ liệu là đủ làm nghẽn server. Vượt trần thì **tự động rơi sang đường
job nền** (§3), không phải báo lỗi cụt.

## 5. CSV + tiếng Việt — hai cái bẫy lộ ra ngay ngày đầu

Cả hai đều không phải lỗi code, mà là hành vi của Excel — nên không test đơn vị
nào bắt được, chỉ người dùng báo.

**a. Thiếu BOM UTF-8 → Excel trên Windows hiện tiếng Việt thành ký tự rác.**
Excel không tự đoán UTF-8; không có BOM nó đọc theo codepage hệ thống. File
export CSV **phải** ghi BOM. (Chiều ngược lại — đọc — đã đúng: reader cũ bật
`detectEncodingFromByteOrderMarks`.)

**b. Locale `vi-VN` dùng `;` làm dấu phân cách danh sách** → file phân cách bằng
dấu phẩy mở ra dồn hết vào một cột. Hai cách xử lý, chọn một và ghi lại lý do:
thêm dòng `sep=,` ở đầu file, hoặc xuất thẳng bằng `;`.

Người dùng gặp một trong hai sẽ báo là *"file lỗi"*, không báo là *"sai mã hoá"*
— nên nếu không biết trước, thời gian tìm nguyên nhân dài hơn hẳn công sức sửa.

## 6. Áp dụng vào PlatformManager

| Việc | Khi nào | Ghi chú |
| --- | --- | --- |
| ~~`IImportFileReader` + 3 reader (CSV/XLSX/XLS) ở Core~~ | ✅ **Xong** (đối chiếu 2026-09-09) | `.../Application/Import/IImportFileReader.cs:17`; hiện thực ở `Core.Infrastructure/Import/`: `CsvImportFileReader.cs:19`, `ExcelImportFileReader.cs:20` (lớp cơ sở) → `XlsxImportFileReader.cs:9` + `XlsImportFileReader.cs:14`, bộ chọn `ImportFileReaderSelector.cs:7`. Cả 3 lỗi §2 đã sửa |
| ~~Nhận diện định dạng bằng magic byte (§2a)~~ | ✅ **Xong** (đối chiếu 2026-09-09) | Rẻ, và là chỗ duy nhất chặn được file đổi đuôi |
| ~~Nơi gọi seam import (endpoint + handler + bảng job)~~ | ✅ **Xong** (2026-09-11) | DM-7 — `ImportController` + `StartImportCommand` + `ImportJobRunner` + bảng `business."ImportJobs"` |
| ~~Trần SỐ DÒNG (Q75)~~ | ✅ **Xong** (2026-09-11) | `Import:MaxRows` mặc định 20.000, cưỡng chế bằng `ReadRowsAsync` — xem §2 |
| `ITabularWriter` + `IExportDefinition<T>` ở Core | **KHÔNG phải lượt này** — Q9, 2026-09-09 | Chờ người tiêu thụ thứ hai; DB-4 dùng NPOI thẳng ở `Business.Infrastructure`. Xem §7 |
| ~~Đường export đồng bộ (§3)~~ | ✅ **Xong** (2026-09-11) | DB-4 — `File(...)` của ASP.NET, không qua seam Core nào |
| Đường export job nền + `ExportJob` + retention | khi có màn thật vượt ngưỡng | Hạ tầng đã sẵn (`IBackgroundJobScheduler`), không phải dựng mới |
| Test "số dòng export = `totalCount`" (§4) | 🚧 **viết rồi, CHƯA CHẠY** (2026-09-11) | `src/BE/Tests/PlatformManager.Business.IntegrationTests/Dashboard/ExportMatchesTableTests.cs` — cần Docker, máy thi công không có. Đã đối chiếu TAY trên 5 bộ lọc: `doc/contracts/dashboard.md` DB-4 §Nghiệm thu |
| BOM + dấu phân cách CSV (§5) | cùng lúc với writer CSV | Sửa sau khi có người dùng thật thì đã mất niềm tin rồi |

Permission-key cho các endpoint này khai ở `ResourceKeys` (`Core.Application`) —
hiện chỉ còn `import.manage`, xem
[`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md)
§"Phân quyền theo hành động".

---

## 7. 📐 HOÃN — `ITabularWriter` không dựng ở lượt này (Q9, chốt 2026-09-09)

Quyết định người dùng 2026-09-09: **đường export `.xlsx` đầu tiên (DB-4) viết NPOI trực tiếp
trong `Business.Infrastructure`.** Seam `ITabularWriter` + `IExportDefinition<T>` ở §3 vẫn là
đích đến, nhưng chỉ dựng **khi có người tiêu thụ thứ hai**.

**Lý do không phải "để sau cho nhanh" — chữ ký ở §3 không diễn đạt được file phải xuất.**
`ITabularWriter` nhận một `IExportDefinition<TRow>` gồm **danh sách cột** và **luồng dòng**,
tức nó mô tả đúng một hình dạng: bảng phẳng bắt đầu từ ô đầu tiên. Bố cục DB-4 đã duyệt thì
không phải hình dạng đó — nó có một **khối nhận dạng kỳ nhiều dòng** phía trên header, một
dòng `TỔNG CỘNG` phía dưới dữ liệu, nền màu cho hàng header và **tên sheet đổi theo kỳ**. Đặc
tả từng ô: `spec/dashboard-dti/business-rules.md` §Export; phần thuộc hợp đồng đường dây:
[`../../../contracts/dashboard.md`](../../../contracts/dashboard.md) DB-4.

Ép bố cục đó qua seam hiện tại chỉ có hai lối, cả hai đều tệ hơn việc chưa dựng seam:

| Lối | Hỏng ở đâu |
| --- | --- |
| Nhét khối nhận dạng kỳ thành "mấy dòng dữ liệu đặc biệt" ở đầu `FetchAsync` | Dòng tiêu đề đi chung luồng với dòng dữ liệu ⇒ nghiệm thu *"số dòng file = `totalCount`"* (§4) hết đo được đúng thứ nó sinh ra để đo |
| Phình `IExportDefinition<T>` thêm header/footer/màu/tên sheet cho vừa **một** ca dùng | Đó là thiết kế seam theo một consumer duy nhất — hình dạng seam khoá cứng theo màn Dashboard trước khi biết màn thứ hai cần gì |

**Điều kiện dựng seam** — có **người tiêu thụ thứ hai** thật (một màn export khác, hoặc đường
`format=csv` quay lại). Lúc đó hai ca dùng thật mới nói ra được phần nào là chung, phần nào là
bố cục riêng của từng báo cáo. Trước đó, một abstraction rút ra từ một ví dụ là phỏng đoán.

**Ràng buộc vẫn còn hiệu lực dù chưa có seam** — đây là phần dễ mất nhất khi hoãn:

- NPOI **chỉ** được reference ở tầng `*.Infrastructure` (§2). Với DB-4 là
  `Business.Infrastructure`. Không kéo NPOI vào `Business.Application`, không kéo vào Core.
- Luật §4 (export dùng **chung** object bộ lọc với endpoint danh sách) **không** phụ thuộc
  seam — nó là luật về bộ lọc, giữ nguyên. Kèm cả test nghiệm thu số dòng.
- `SXSSFWorkbook` cho file lớn, `XSSFWorkbook` cho file nhỏ — vẫn như §3.
- Hai cái bẫy CSV ở §5 chỉ phải xử lý khi có đường CSV; DB-4 hiện chỉ `.xlsx`.

**Khi dựng seam, việc đầu tiên là chuyển DB-4 sang dùng nó** — để lượt đó có ngay một consumer
đã chạy thật đối chứng, thay vì hai đường ghi Excel song song tồn tại lâu dài.
