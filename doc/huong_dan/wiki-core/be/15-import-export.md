---
kind: luat
scope: core
status: "import/export not built"
verified: khong-ap-dung
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

**Trạng thái: 📐 ĐÍCH ĐẾN, CHƯA THI CÔNG.** Toàn bộ code import của module
`DtiWeekly` đã xoá 2026-08-29 cùng module; Core hiện chưa có dòng nào. Các quyết
định dưới đây chốt ngày 2026-08-29, trước khi viết code.

## 1. Ranh giới Core ↔ Module — bộ lọc KHÔNG bao giờ đi vào Core

Đây là quyết định quan trọng nhất của file này, vì làm sai nó là làm hỏng ranh
giới Core/Business ở [`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md).

| Core giữ | Module giữ |
| --- | --- |
| Đọc file → dòng dữ liệu trung tính (`IImportFileReader`) | Cột nào bắt buộc, validate gì, upsert vào entity nào |
| Ghi dòng + mô tả cột → file (`ITabularWriter`) | Query nào, **lọc theo gì**, cột nào xuất ra, format từng ô |
| Lưu file, chạy job nền, theo dõi trạng thái, trả link | Permission-key của chính nghiệp vụ đó |
| Trần số dòng, tắt phân trang, dọn file hết hạn | Ý nghĩa nghiệp vụ của bộ lọc |

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

Seam giữ nguyên hình dạng đã dùng ở module cũ, vì nó vốn đã đúng:

```csharp
public interface IImportFileReader
{
    bool CanRead(ReadOnlySpan<byte> header, string fileName);
    IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ReadAsync(
        Stream stream, string fileName, CancellationToken ct);
}
```

Runner chọn reader bằng cách hỏi `CanRead` từng cái trong
`IEnumerable<IImportFileReader>` đã đăng ký — **không `switch` theo enum định
dạng**. Thêm định dạng thứ tư về sau chỉ là thêm một implementation; Core không
sửa dòng nào.

Thư viện: **CsvHelper** cho CSV, **NPOI** cho cả `.xls` (HSSF) lẫn `.xlsx`
(XSSF). NPOI là lựa chọn bắt buộc nếu muốn `.xls` — ClosedXML và EPPlus đều chỉ
đọc được `.xlsx`, và EPPlus từ v5 còn đổi sang giấy phép thương mại.

⚠️ **NPOI chỉ được reference ở `Core.Infrastructure`, KHÔNG kéo vào
`Core.Application`** — seam ở Application phải sạch thư viện, đúng luật tầng ở
[`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md).

### Ba lỗi phải sửa khi bê code cũ lên — đừng chép nguyên

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
> 202: [`ApiControllerBase.cs:25`](../../../../src/BE/PlatformManager.Api/Common/ApiControllerBase.cs)
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
| `IImportFileReader` + 3 reader (CSV/XLSX/XLS) ở Core | **ngay** | Bê từ code cũ, sửa đủ 3 lỗi §2 trước khi commit |
| Nhận diện định dạng bằng magic byte (§2a) | cùng lúc | Rẻ, và là chỗ duy nhất chặn được file đổi đuôi |
| `ITabularWriter` + `IExportDefinition<T>` ở Core | **ngay** | XLSX + CSV, không `.xls` |
| Đường export đồng bộ (§3) | **ngay** | Là ca phổ biến nhất, làm trước |
| Đường export job nền + `ExportJob` + retention | khi có màn thật vượt ngưỡng | Hạ tầng đã sẵn (`IBackgroundJobScheduler`), không phải dựng mới |
| Test "số dòng export = `totalCount`" (§4) | cùng lúc với màn export đầu tiên | Đây là thứ duy nhất giữ hai đường lọc không lệch |
| BOM + dấu phân cách CSV (§5) | cùng lúc với writer CSV | Sửa sau khi có người dùng thật thì đã mất niềm tin rồi |

Permission-key cho các endpoint này khai ở `ResourceKeys` (`Core.Application`) —
hiện chỉ còn `import.manage`, xem
[`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md)
§"Phân quyền theo hành động".
