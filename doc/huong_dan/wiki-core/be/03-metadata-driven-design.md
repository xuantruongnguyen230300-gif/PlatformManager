# 3. Metadata-driven design — áp dụng cho phần nào trong thực tế

## 3.0 Vấn đề gốc — 3 tầng phải khớp nhau

Mọi thiết kế metadata cho grid/form đều xoay quanh 3 tầng, và nguồn rủi ro luôn là **khoảng hở giữa 3 tầng này**, không phải bản thân việc dùng DB metadata:

```
Tầng 1: Bảng metadata (SysGrid)   →  "grid X có cột Email, kiểu string"
Tầng 2: Bảng dữ liệu vật lý       →  cột Email có thật sự tồn tại chưa?
Tầng 3: Entity/DTO C#             →  code có property Email, EF Core có map nó chưa?
```

Có kiểm soát thay đổi bằng migration script (review qua PR, chạy tuần tự dev→staging→prod) giải quyết được **ai/khi nào được đổi** — nhưng không tự động đảm bảo Tầng 1/2/3 luôn khớp nhau, vì migration chỉ là "SQL chạy được", không có liên kết compile-time nào với entity thật. Gõ sai tên cột trong script vẫn build xanh, chỉ vỡ lúc runtime (cột trả `null` âm thầm).

**Insight quan trọng nhất của toàn bộ phần 3 này**: có **3 loại nhu cầu khác nhau** thường bị gộp chung thành "metadata cho grid", mỗi loại cần **1 cơ chế khác nhau** — hiểu rõ 3 loại này thì tự khắc biết áp dụng cái gì ở đâu.

## 3.1 Ba loại nhu cầu, ba cơ chế

| Loại nhu cầu | Ví dụ | Cơ chế đúng | Có rủi ro lệch Tầng 2/3 không? |
|---|---|---|---|
| **(A) Đổi cách HIỂN THỊ 1 field đã tồn tại trong code** | Đổi nhãn cột "Tên KH" → "Họ tên khách hàng", đổi thứ tự cột, ẩn/hiện cột | Data facet (tên field/kiểu) **sinh từ code**, DB/JSON chỉ override phần trình bày — đúng mô hình VNR đã làm (`dynamicui-screen-flow.md`) | Không — vì data facet không tự do, luôn bám code thật |
| **(B) Thêm 1 field HOÀN TOÀN MỚI, code chưa từng biết, không muốn deploy lại** | User tự thêm "Số fax" vào form Customer qua UI | **Cột JSON/JSONB** (xem 3.2) — field mới là DATA, không phải ALTER TABLE | Không — vì JSON vốn không có schema cứng để mà lệch |
| **(C) Dữ liệu thuần, không liên quan gì tới schema bảng nghiệp vụ** | Menu, danh mục dùng chung, nội dung template email, i18n, gán quyền, feature flag | DB tự do 100% | Không áp dụng — không có Tầng 2/3 nào để lệch |

Ba dòng trong bảng trên chính là câu trả lời cho câu hỏi "sysgrid nên áp dụng cách nào" — **không phải chọn 1 trong 3, mà là nhận diện đúng yêu cầu đang ở loại nào** rồi áp cơ chế tương ứng.

## 3.2 Cơ chế cho Loại (B) — field mở rộng thật sự, chi tiết cho .NET 10

**Bước 1 — Thêm sẵn 1 cột JSON vào bảng nghiệp vụ ngay từ đầu thiết kế** (không phải thêm sau):

```csharp
public class Customer : BaseEntity
{
    public string Name { get; private set; } = default!;   // cố định, Loại (A)
    public string Code { get; private set; } = default!;   // cố định, Loại (A)

    public Dictionary<string, JsonElement>? ExtraFields { get; private set; }  // Loại (B)
}

// EF Core 7+ — map thẳng thành 1 cột jsonb thật trong Postgres
builder.OwnsOne(x => x.ExtraFields, b => b.ToJson());
```

**Bước 2 — Vẫn cần 1 bảng metadata mô tả "field mở rộng nào đang tồn tại"** — nhưng bảng này **an toàn tuyệt đối** để tự do sửa qua UI, vì nó không map tới cột vật lý nào cả:

```sql
CREATE TABLE "SysCustomFieldDef" (
    "Id" uuid PRIMARY KEY,
    "EntityName" varchar(100),   -- "Customer"
    "FieldKey" varchar(100),     -- "Fax" — khớp key trong ExtraFields, KHÔNG phải cột vật lý
    "DataType" varchar(20),      -- 'string'|'number'|'date'|'bool' — chỉ dùng validate input + chọn control UI
    "LabelKey" varchar(200),
    "Required" boolean,
    "CreatedBy" uuid,
    "CreatedAt" timestamptz
);
```

Vì sao bảng này an toàn dù chứa cả "DataType" (nghe giống định nghĩa cấu trúc)? Vì `DataType` ở đây **không ảnh hưởng schema vật lý** — nó chỉ dùng để (a) validate dữ liệu nhập ở tầng ứng dụng, (b) chọn control render (ô nhập text/số/ngày). Đổi `DataType` từ `string` sang `number` sau khi đã có dữ liệu cũ — vẫn là vấn đề cần xử lý (dữ liệu cũ không parse được thành số), nhưng đây là lỗi validate **mềm** (cảnh báo, có thể xử lý ở tầng ứng dụng), không phải crash cứng "cột không tồn tại" như Tầng 2/3 lệch nhau.

**Bước 3 — Query/lọc trên field JSON**: EF Core dịch được LINQ trực tiếp vào JSON path của Postgres, không cần viết SQL tay:

```csharp
var result = await db.Customers
    .Where(c => c.ExtraFields!["Fax"].GetString() == "0281234567")
    .ToListAsync();
// EF Core tự dịch sang: WHERE "ExtraFields"->>'Fax' = '0281234567'
```

**Bước 4 — Hiệu năng ở quy mô trung bình**: thêm GIN index nếu lọc/tìm kiếm trên field JSON thường xuyên:

```sql
CREATE INDEX ix_customer_extrafields ON "Customer" USING GIN ("ExtraFields");
```

**So với `dynamic`/`DataTable` thời .NET Framework 4.8**: cùng tinh thần "đọc theo tên lúc runtime", nhưng `System.Text.Json` (`JsonElement`/`JsonNode`) an toàn hơn — có thể ép kiểu tường minh (`TryGetProperty`), nhẹ hơn (`Utf8JsonReader`, source-generator), và tương thích Native AOT nếu sau này cần deploy nhẹ/khởi động nhanh (`dynamic`/reflection nặng thì không).

## 3.3 Tham chiếu thực tế — 3 hệ thống production làm "metadata thuần"

(kiến thức kiến trúc đã ổn định lâu năm, không phải kết quả tra cứu trực tuyến)

| Hệ thống | Cách kiểm soát |
|---|---|
| **Odoo** (`ir.model`/`ir.model.fields`) | Field có code thì tự đồng bộ vào bảng metadata lúc cài module (không gõ tay) — đúng Loại (A). Field custom thật sự do admin thêm (Loại B kiểu cũ, trước khi JSON phổ biến) → **hệ thống tự `ALTER TABLE` ngay lúc đó**, không chỉ ghi metadata suông. Quyền sửa giới hạn nhóm `group_system`. |
| **SAP Data Dictionary** | Metadata là nguồn **sinh ra** DDL (không phải mirror ngược) — đổi phải qua Transport Request (quy trình duyệt dev→test→prod), có authorization object riêng, audit đầy đủ. |
| **Salesforce** (Custom Object/Field) | Sửa trực tiếp production bị chặn theo thực hành chuẩn — phải qua Change Set/pipeline từ sandbox, cần quyền "Customize Application", mọi thay đổi vào Setup Audit Trail. |

## 3.4 Nguyên tắc quản trị chung (áp dụng cho cả Loại A và B)

1. Metadata không bao giờ đứng 1 mình — Loại (A) bám code qua sinh tự động, Loại (B) không đụng schema vật lý nên không có gì để lệch.
2. Bảng metadata có quyền riêng, tách khỏi quyền nghiệp vụ thường (vd chỉ role kỹ thuật/admin hệ thống).
3. Thay đổi đi qua draft → publish, không sửa thẳng bản đang chạy production.
4. Audit trail đầy đủ cho mọi thay đổi metadata.
5. Thay đổi Loại (A) hoặc field vật lý thật (không phải JSON) luôn qua migration script được review — không bao giờ sửa tay trực tiếp trên DB production.

## 3.5 Lộ trình mở rộng dần — đảm bảo dễ mở rộng về sau mà không over-engineering ngay từ đầu

Không cần xây cả 3 cơ chế cùng lúc. Thứ tự đầu tư hợp lý khi hệ thống lớn dần:

1. **Giai đoạn đầu** (như PlatformManager hiện tại): chỉ cần Loại (C) — danh mục/menu/config thuần DB. Không cần Loại (A)/(B).
2. **Khi có ≥5-10 màn CRUD lặp lại cấu trúc giống nhau**: đầu tư Loại (A) — nguồn cột từ code (reflection đơn giản là đủ, chưa cần cả bộ code-gen như VNR).
3. **Khi có yêu cầu thật "user tự thêm field không cần deploy"**: mới thêm cột JSON (Loại B) cho đúng entity đang cần — không thêm `ExtraFields` cho mọi entity ngay từ đầu nếu chưa ai cần.
4. **Chỉ khi hệ thống thật sự lớn** (nhiều team, nhiều module, nhiều màn hình dùng chung 1 engine): mới đáng đầu tư tool sinh metadata tự động từ code (như `VNR.Tools.UiConfigGen`) + bộ CI gate kiểm tra drift. Ở quy mô "trung bình, 1 server", 1 bài test đối chiếu đơn giản là đủ.

Thiết kế theo lộ trình này vẫn "dễ mở rộng về sau" đúng nghĩa: Loại (C) không cản gì Loại (A)/(B) thêm sau; cột JSON thêm được bất cứ lúc nào không cần đổi cấu trúc đã có; bảng `SysCustomFieldDef` độc lập, thêm được cho entity mới mà không đụng entity cũ.

## Áp dụng vào PlatformManager — cụ thể

| Vùng | Loại | Nên làm? |
|---|---|---|
| `CriteriaGroup` (6 nhóm) | (C) | ✅ Đã đúng — giữ nguyên |
| Ngưỡng badge (`>=99.999%`, `delta<=0.001`) | (C) | ✅ Nên đưa vào config/`SysConfig` |
| Menu sidebar | (C), nhưng chưa cần bảng riêng | ❌ Chưa cần — 2 màn hình, hard-code trong Angular route là đủ |
| Cột grid (Danh mục DTI, bảng 62 chỉ tiêu) | (A) | ❌ Chưa cần engine generic — 2 màn hình, schema ổn định |
| Field mở rộng kiểu "user tự thêm cột" | (B) | ❌ Không có nhu cầu này ở demo hiện tại — nhưng nếu có, đây chính xác là chỗ dùng cột JSON, không phải `ALTER TABLE` |

**Tóm lại cho PlatformManager**: giữ nguyên ở Loại (C) như hiện tại, chưa cần đầu tư Loại (A)/(B) — nhưng khi thiết kế **hệ thống mới** (mục tiêu thật sự của wiki này), nên tính trước cột JSON (Loại B) cho các entity có khả năng cần "field tự thêm" cao (vd Customer, Product trong ERP) ngay từ lúc thiết kế bảng đầu tiên — thêm 1 cột `jsonb` từ đầu rẻ hơn nhiều so với thêm sau khi đã có dữ liệu lớn.
