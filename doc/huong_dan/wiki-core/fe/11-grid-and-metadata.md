---
kind: luat
scope: core
verified: 2026-09-06
---

# 11. Grid — thư viện, ngưỡng nâng cấp, và đồng bộ metadata với BE

## Quyết định — Đã CHỐT LẠI (2026-08-15): PrimeNG `p-table` ngay từ module tiếp theo

> Đảo ngược đề xuất trước ("chưa cần grid library, dùng `CdkTable`, đợi
> ngưỡng mới đổi"). Lý do: chiến lược "đợi ngưỡng" đúng cho *kiến trúc lõi*
> nhưng áp sai cho *lựa chọn thư viện UI* trong domain ERP/quản lý/chuyển
> đổi số — nhóm phần mềm này gần như chắc chắn cần Grid mạnh (sort/filter/
> group/export) khi số module tăng, không phải "có thể". Đợi tới khi thấy
> nỗi đau rồi mới đổi nghĩa là lúc đó đã có N màn hình hand-rolled phải
> migrate cùng lúc — đắt hơn nhiều so với chọn ngay từ đầu. Xem
> [../01-core-components.md](01-core-components.md) và nhận định đầy đủ đã
> trao đổi trực tiếp với người dùng.

```bash
npm install primeng @primeng/themes
```

### Hiện trạng (đối chiếu 2026-09-06) — mọi lưới đi qua `shared/components/data-grid/`

🔄 LẬT 2026-09-06: mục này trước đây nói về `CriteriaGridTable`
(`modules/danh-muc-dti/components/criteria-grid-table/`) và dặn "không bắt buộc migrate ngay".
Component đó **không còn tồn tại** — nó bị gỡ cùng module `danh-muc-dti` ngày 2026-08-29.
Module `danh-muc-dti` **đã dựng lại 2026-09-09** nhưng `CriteriaGridTable` thì không quay lại
(đối chiếu 2026-09-10: `find src/FE/src -iname "*criteria*"` không ra gì). Không còn lưới
hand-rolled nào để migrate.

Luật hiện hành:

- **`p-table` chỉ được import ở đúng MỘT chỗ**: `src/FE/src/app/shared/components/data-grid/`.
  Lưới của màn hình là **người dùng** của `DataGrid`, không dựng `p-table` rời.
- Lý do gói lại thay vì để mỗi màn tự dựng: chiều cao "cố định bằng màn hình, cuộn bên trong"
  đòi **ba** thao tác ở **ba** file rời nhau (xem §Chiều cao lưới ngay dưới) — thiếu một cái là
  hỏng im lặng, và nó đã hỏng thật trước 2026-09-06.
- Ví dụ người dùng đầu tiên:
  `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/`.

```bash
grep -rn "from 'primeng/table'" src/FE/src --include=*.ts | grep -v spec
# PASS: đúng 1 dòng, và dòng đó thuộc shared/components/data-grid/
```

> 🔄 **SỬA 2026-09-10 — lệnh cũ báo FAIL GIẢ.** Bản trước đo bằng `"primeng/table"` trần,
> nên nó đếm cả **chú thích** nhắc tên gói (`src/FE/src/app/core/i18n/core-i18n.ts:67`) chứ không
> chỉ `import` thật, và ra nhiều hơn 1 dòng dù ranh giới không hề bị phá. Ranh giới vẫn đúng như
> mô tả — chỉ phép đo sai. Neo `import` thật đang chạy:
> `src/FE/src/app/shared/components/data-grid/data-grid.ts:4` (đối chiếu 2026-09-10).
> Một tiêu chí PASS đỏ vì lý do không liên quan tới code là tiêu chí người ta tắt đi.

### Mẫu dùng `DataGrid` với server-side pagination

Màn hình khai **cột** bằng hai `ng-template` rồi truyền vào; `DataGrid` giữ **khung**
(phân trang, cuộn, chiều cao, câu rỗng mặc định):

```html
<ng-template #header><tr><th>Mã</th><th>Tên</th></tr></ng-template>
<ng-template #body let-row><tr><td>{{ row.Code }}</td><td>{{ row.Name }}</td></tr></ng-template>

<app-data-grid
  class="grid-host"
  [rows]="rows()"
  [loading]="loading()"
  [totalCount]="totalCount()"
  [page]="page()"
  [pageSize]="pageSize()"
  [headerTemplate]="header"
  [bodyTemplate]="body"
  (pageChange)="onPageChange($event)"
/>
```

Hai điều `DataGrid` đã lo hộ, **đừng** làm lại ở màn hình:

- **`[lazy]` + quy đổi trang.** PrimeNG phát `first` (0-based); API và mọi tầng gọi dùng trang
  **1-based**. Phép quy đổi lệch-một-đơn-vị đó nằm gọn trong `DataGrid.onLazyLoad`, một chỗ duy
  nhất. Server-side pagination vẫn là mặc định — xem
  [13-performance.md](13-performance.md) §5.
- **Truyền template bằng `TemplateRef`, KHÔNG `<ng-content>`.** `p-table` nhận
  `#header`/`#body`/`#emptymessage` bằng *content query*; chiếu chúng qua một lớp bọc là dựa
  vào chi tiết nội tại của Angular — chạy được hôm nay, hỏng khi nâng phiên bản mà **không có
  lỗi biên dịch**.

🛑 **Giới hạn có chủ đích:** `DataGrid` chỉ phát `page`/`pageSize`; `sortField`/`sortOrder`/`filters`
của `TableLazyLoadEvent` bị bỏ qua — lý do và việc phải làm khi màn đầu tiên cần sort/filter
server-side ghi tại chỗ, `src/FE/src/app/shared/components/data-grid/data-grid.ts:68` (đối chiếu
2026-09-10).

### Chiều cao lưới — ba thao tác, thiếu một là hỏng im lặng

"Lưới cao bằng màn hình, cuộn bên trong, phân trang luôn thấy" cần **cả ba** thứ sau. Hai
trong ba đã nằm sẵn trong `DataGrid`; phần còn lại là việc của màn hình:

| Ai làm | Việc |
|---|---|
| Trang | `host: { class: 'page-fill' }` trên component trang |
| Màn hình | đặt `class="grid-host"` lên thẻ `<app-data-grid>` |
| `DataGrid` (đã lo) | `[scrollable]="true"` + `scrollHeight="flex"` trên `p-table` |

Mỗi thao tác nhìn riêng đều **có vẻ đủ**, nên thiếu một cái không đỏ ở đâu cả — lưới chỉ trông
hơi khác. Đó đúng là cách nó đã hỏng thật trước 2026-09-06.

## Tính năng ERP-grade có sẵn, không cần tự viết thêm

`p-table` có sẵn (bật khi cần, không bật thừa): sort đa cột, filter theo
cột, `columnResize`, `reorderableColumns`, export CSV built-in (`exportCSV()`),
row group + subtotal (`rowGroupMode`). Đây chính là nhóm tính năng mà nếu
tự viết tay sẽ tốn nhiều tuần công — lý do cốt lõi của quyết định này.

## Khi nào cần thêm ag-Grid/DevExtreme/Kendo (hiếm, riêng lẻ)

Chỉ xét thêm 1 thư viện thứ 2 khi có **đúng 1** màn hình cần pivot/tree-data
nâng cao mà `p-table` không đáp ứng — không thay thế toàn bộ `p-table` bằng
lib khác, chỉ dùng cục bộ cho đúng màn hình đó (tránh 2 hệ Grid song song
không cần thiết).

## Metadata sync — khi menu/cột grid do BE điều khiển

Đối chiếu `doc/huong_dan/wiki-core/be/03-metadata-driven-design.md` §3.1 —
2 loại liên quan tới FE:

- **Loại C (menu)** — ✅ **ĐÃ THI CÔNG (đối chiếu 2026-09-06).** `MenuService`
  (`src/FE/src/app/core/menu/menu.service.ts`) gọi `GET /meta/menu`, dựng cây 1 cấp từ danh
  sách phẳng, và cache theo **phiên đăng nhập** (khoá gồm cả `Roles`, vì BE lọc `SysMenuRole`
  theo role — cùng user mà đổi role thì không được dùng lại bản cũ). `Sidebar` là nơi tiêu thụ.

  🔄 LẬT 2026-09-06: bản trước ghi *"**Chưa cần** ở quy mô 2 module hiện tại … **không** xây
  lớp tiêu thụ menu động trước khi BE thật sự phục vụ endpoint đó"*. Ngưỡng đó đã bị vượt qua
  và code đã về; câu cũ nay khuyên gỡ bỏ một thứ đang chạy.
- **Loại A (cột grid)** — data facet **sinh từ code** BE, DB/JSON chỉ
  override phần trình bày (tên cột, thứ tự, ẩn/hiện) — không phải toàn bộ
  cấu trúc cột. Ngưỡng theo `be/03`: "khi có ≥5-10 màn CRUD giống nhau" — 📐 **chưa chạm
  ngưỡng (đối chiếu 2026-09-06)**: đếm màn CRUD bằng `ls -d src/FE/src/app/platform/*/`, hiện
  còn xa mức đó và `IGridColumnMeta` chưa có file nào.

### Hợp đồng đã thiết kế trước — dùng khi chạm ngưỡng

Để BE/FE không phải đàm phán lại từ đầu khi ngưỡng tới, hợp đồng JSON cho cả
2 loại **chốt sẵn hình dạng** ở đây:

> 🔄 LẬT 2026-09-06 — **hợp đồng Loại C không còn là "thiết kế trước", nó đã thành code, và
> hình dạng thật KHÁC bản thiết kế.** Bản dưới đây giữ lại làm lịch sử; đừng viết code theo nó.
>
> | Bản thiết kế (dưới) | Thực tế `src/FE/src/app/core/menu/menu-item.model.ts` |
> |---|---|
> | `core/models/menu-item.model.ts` | `core/menu/menu-item.model.ts` |
> | `key` | `Id` (guid) + `Code` |
> | `order` | `DisplayOrder` |
> | `requiredPermission` | **không có** — BE đã lọc theo role trước khi trả, FE không lọc lại |
> | *(không có)* | `ParentId` + `Children` — menu là **cây 1 cấp**, không phải danh sách phẳng |
> | camelCase | model app **PascalCase** (`IMenuItem`), DTO camelCase (`IMenuItemDto`) — đúng ranh giới wire ở [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) |
>
> Một chi tiết đáng giữ lại vì nó đã gây bug thật: `IMenuItemDto` khai `parentId?: string | null`
> (**có** dấu `?`) vì BE bật `DefaultIgnoreCondition = WhenWritingNull` — field null là **key
> vắng mặt** trên dây, không phải `null`. Khai `field: T | null` là type nói sai sự thật; đã làm
> ma trận phân quyền render rỗng hoàn toàn một lần.

```ts
// 🗄️ LỊCH SỬ — bản thiết kế trước 2026-09-06, KHÔNG phải hình dạng đang chạy
export interface IMenuItem {
  key: string;
  label: string;
  icon: string | null;
  route: string;
  requiredPermission: string | null;
  order: number;
}
```

```ts
// core/models/grid-column-meta.model.ts — Loại A (chỉ override trình bày)
export interface IGridColumnMeta {
  field: string;          // khớp property C# của DTO — KHÔNG phải tên cột tự do
  label: string;
  order: number;
  visible: boolean;
  width: string | null;   // "120px" | null = auto
}
```

`IGridColumnMeta[]` map trực tiếp vào cấu hình cột của `p-table` (`field`→
`pSortableColumn`, `order`→ thứ tự `<th>`, `visible`→ `*ngIf`/`@if` ẩn cột,
`width`→ style cột) — không cần tầng chuyển đổi trung gian nào khác.

`GET /api/meta/menu` — ✅ **đã có thật cả hai phía** (đối chiếu 2026-09-06).
`GET /api/meta/grid/{gridKey}` — 📐 chưa, đúng ngưỡng Loại A ở trên.

🔄 LẬT 2026-09-06, hai câu đã sai:

- *"`MetadataService` (`core/services/`) gọi, cache trong `signal()`"* — **không có**
  `MetadataService` và không có thư mục `core/services/`. Việc đó do `MenuService`
  (`core/menu/`) làm. Cách cache thì đúng như đã thiết kế: `signal()`, không `signalStore()`,
  và có `invalidate()` gọi ở `login`/`logout`.
- *"**Việc KHÔNG làm bây giờ:** viết `MetadataService`/2 model trên thành code thật trước khi
  BE có endpoint"* — câu này đã hết hiệu lực cho **Loại C** (BE có endpoint, FE đã tiêu thụ).
  Nó vẫn còn nguyên hiệu lực cho **Loại A** (`IGridColumnMeta`): đừng viết trước.

**Cập nhật (2026-08-15):** bảng `SysMenu` (Loại C) đã có ERD + migration thật
— xem `doc/cau-truc-database.md` §4.1 §2 và
`doc/cau-truc-database.md` (nguồn cũ `doc/ERD/` đã xoá). Schema khớp đúng
`IMenuItem` ở trên, chỉ khác 1 điểm: cột DB tên `RequiredRole` (không phải
`RequiredPermission`) — phạm vi rút gọn có chủ đích vì role Identity cụ thể
mới chốt gần đây, xem lý do đầy đủ ở `doc/cau-truc-database.md` §4.1 §2.3. Khi implement
`GET /api/meta/menu` thật, map `RequiredRole` (DB) → `requiredPermission`
(FE model) giữ nguyên tên phía FE, không đổi hợp đồng đã thiết kế.

## Export dữ liệu grid — API export riêng cho dữ liệu lớn, không export những gì đã load

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> "Tính năng ERP-grade có sẵn" ở trên liệt kê `exportCSV()` built-in của
> `p-table` như 1 tính năng có sẵn — đúng, nhưng chưa cảnh báo giới hạn quan
> trọng nhất: `exportCSV()` chỉ export **`value` đang có trong bộ nhớ
> browser**. Với server-side pagination (mặc định của hệ thống, xem
> [13-performance.md](13-performance.md) §5), `value` chỉ chứa **đúng 1
> trang** (`pageSize` dòng) — bấm "Export" ở trang 1/50 chỉ ra file 20 dòng,
> không phải toàn bộ dataset lọc được, và phần lớn user không nhận ra cho
> tới khi mở file ra đếm dòng.

**Quy tắc: export "toàn bộ kết quả đang lọc" phải là 1 lời gọi API riêng,
không tái dùng `rows()` đang hiển thị trên grid:**

```ts
// grid đang [lazy] qua onLazyLoad — nút Export gọi API riêng, KHÔNG dùng rows()
onExportClick(): void {
  this.criteriaService.exportGrid(this.currentFilter()).subscribe(blob => {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `criteria-${todayIso()}.xlsx`;
    a.click();
    URL.revokeObjectURL(url);
  });
}
```

Gọi API export bằng đúng bộ **filter** đang áp trên grid (không phải
`Page`/`PageSize`), để "export" và "đang xem" luôn khớp nghĩa với user dù số
dòng trả về khác nhau. Phía BE trả file trực tiếp (không qua envelope
`IApiResult<T>` — cùng dạng ngoại lệ với response 429 của rate limit đã ghi ở
`doc/huong_dan/wiki-core/be/09-security-beyond-auth.md`), hoặc với dataset
thật sự lớn, trả job nền (mẫu "Command chạy lâu → job nền" ở
`doc/huong_dan/quy-uoc/be-cqrs-handler.md`) + endpoint tải file khi xong.

`exportCSV()` built-in **vẫn dùng được** cho đúng 1 trường hợp: dataset nhỏ
đã tải hết ở client, không `[lazy]` — như bảng 6 `CriteriaGroup` cố định đã
nêu ở [13-performance.md](13-performance.md) §5.

## Column resize/reorder — lưu preference theo user, không mất khi F5

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `columnResize`/`reorderableColumns` đã liệt kê ở trên là tính năng có sẵn,
> nhưng mặc định `p-table` chỉ giữ state đó **trong bộ nhớ component** — F5
> hoặc điều hướng rời trang là mất, user phải chỉnh lại từ đầu mỗi phiên.
> Với grid nhiều cột (10+), đây là kiểu khó chịu nhỏ nhưng lặp lại **mỗi
> ngày** — đáng lưu dù chi phí thêm vào thấp.

`localStorage` là đúng chỗ cho preference này — không phải state cần đồng bộ
server, mất thì user chỉnh lại chứ không hỏng dữ liệu nghiệp vụ nào:

```ts
// <feature>/components/<ten>-grid-table/<ten>-grid-table.ts
// (🔄 LẬT 2026-09-06: mẫu cũ ghi `modules/danh-muc-dti/...`, module đã gỡ 2026-08-29)
private readonly storageKey = 'grid-pref:criteria-list';   // tiền tố gridKey riêng, tránh đụng key khác

onColReorder(event: { columns: { field: string }[] }): void {
  this.savePref({ order: event.columns.map(c => c.field) });
}
onColResize(event: { element: HTMLElement; delta: number }): void {
  this.savePref({ [event.element.id]: event.element.offsetWidth });
}

private savePref(patch: Record<string, unknown>): void {
  const current = JSON.parse(localStorage.getItem(this.storageKey) ?? '{}');
  localStorage.setItem(this.storageKey, JSON.stringify({ ...current, ...patch }));
}
```

Đọc lại lúc khởi tạo component, áp vào cấu hình cột trước khi render lần đầu
để không bị "nhảy" layout sau khi mount. **Khoá key theo cả `gridKey` lẫn
user** nếu nhiều người dùng chung máy (`localStorage` không tự phân biệt
user đăng nhập) — xác nhận với nghiệp vụ trước khi giả định 1 user/máy.
Không đồng bộ preference này lên server trừ khi có yêu cầu thật ("mở trên
máy khác vẫn giữ layout cũ") — thêm bảng/endpoint cho việc này trước khi có
nỗi đau là đi ngược nguyên tắc chung của cả bộ tài liệu.

## Virtual scroll — ngưỡng chuyển từ phân trang server sang `[virtualScroll]`

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> [13-performance.md](13-performance.md) §3 đã nói `CdkVirtualScrollViewport`
> là lựa chọn "hiếm, vì server-side pagination đã là mặc định" — đúng,
> nhưng chưa ghi ngưỡng cụ thể cho riêng `p-table`, và `p-table` có cơ chế
> virtual scroll **riêng** của nó (không dùng chung với CDK) nên cách bật
> khác hẳn.

`p-table` hỗ trợ `[virtualScroll]="true"` kèm `[virtualScrollItemSize]` (số
px cố định mỗi dòng):

```html
<p-table [value]="rows()" [scrollable]="true" scrollHeight="400px"
         [virtualScroll]="true" [virtualScrollItemSize]="46">
  <ng-template #body let-row>
    <tr style="height: 46px">...</tr>
  </ng-template>
</p-table>
```

**Ngưỡng cân nhắc: >500 dòng đã tải hết ở client trong 1 lần.** Dưới ngưỡng
đó, phân trang server (đã có, đủ dùng — [13-performance.md](13-performance.md)
§5) rẻ hơn về độ phức tạp: không giữ cả tập dữ liệu ở client, không phải
tính lại chiều cao dòng khi nội dung động (`[virtualScrollItemSize]` cố định
px — sai lệch với chiều cao dòng thật do nội dung dài ngắn khác nhau sẽ làm
cuộn giật). Virtual scroll chỉ đáng bật khi **bản chất dữ liệu không hợp
phân trang** — vd dropdown lookup nhiều trăm mục cần cuộn mượt trong 1 lần
mở (đã nêu ở [13-performance.md](13-performance.md) §3), hoặc màn hình cần
xem/so sánh nhiều dòng cùng lúc mà phân trang cắt ngang thao tác đó (chưa
gặp ở PlatformManager hiện tại). **Không** bật virtual scroll "cho chắc" khi
phân trang server đã hoạt động tốt — 2 cơ chế giải quyết cùng vấn đề, chọn 1.

## Ba trạng thái của lưới — ✅ CÓ THẬT (thi công xong 2026-08-31)

Một lưới có **ba** trạng thái khác hẳn nhau về ý nghĩa, và người dùng phải phân biệt được:

| Trạng thái | Nghĩa với người dùng | Việc tiếp theo của họ |
|---|---|---|
| Đang tải | Chờ chút | Không làm gì |
| Không có kết quả | Bộ lọc không khớp ai | Nới bộ lọc |
| Tải hỏng | Hệ thống trục trặc | Thử lại |

🔄 LẬT 2026-09-06: ngay dưới tiêu đề "✅ CÓ THẬT" này, bản trước còn để lại câu *"Hôm nay lưới
chỉ phân biệt được **một** — `[loading]` của `p-table`. Không có `emptyMessage`, không có nhánh
lỗi."* Đó là mô tả **trước** đợt 2026-08-31, đọc như hiện trạng và mâu thuẫn thẳng với chính
tiêu đề mục. Đã đổi sang thì quá khứ.

### Ca nguy hiểm ĐÃ SỬA: tải hỏng mà bảng vẫn hiện dữ liệu cũ

Trước 2026-08-31, nhánh lỗi chỉ tắt cờ `loading`, **không xoá `rows`**. Nên khi người dùng đổi bộ lọc
sang "Đang khoá" mà request hỏng, bảng vẫn hiện danh sách của bộ lọc **trước** — trông
y như đó là kết quả của bộ lọc mới.

Interceptor có bắn toast, nhưng toast biến mất sau vài giây còn bảng thì ở lại. Người
đến sau hoặc người vừa rời mắt khỏi màn hình chỉ thấy một bảng dữ liệu trông hoàn toàn
bình thường và **sai**.

### Chốt

**Tải hỏng ⇒ xoá bảng, hiện khối lỗi kèm nút thử lại.**

Đánh đổi đã chấp nhận: người dùng mất danh sách đang xem, hơi giật. Đổi lại nguyên tắc
được giữ — **không hiển thị dữ liệu mà ta không biết có còn đúng hay không.** Hai phương
án kia (giữ dữ liệu cũ kèm dải cảnh báo; hoặc khôi phục bộ lọc về giá trị cũ) đều đã cân
nhắc và loại: cái đầu phụ thuộc vào việc người dùng đọc dải cảnh báo, cái sau làm bộ lọc
tự nhảy ngược khiến người dùng tưởng mình bấm nhầm.

### Đã thi công

Đối chiếu lại 2026-09-06: nhánh lỗi **xoá** `rows`/`totalCount` và bật `loadError`;
template thay bảng bằng khối `.notice.bad` mang `role="alert"` kèm nút "Thử lại" gọi lại
đúng bộ lọc đang chọn — `quan-tri-nguoi-dung.page.html:68` (`@if (loadError(); as messageKey)`)
và `:69` (`<div class="notice bad" role="alert">`). "Không có kết quả" vẫn là dòng
trong lòng bảng (`#emptymessage` của `DataGrid`) nên **khác hẳn** khối lỗi về mặt thị giác.

🔄 LẬT 2026-09-06 — trích dẫn cũ ở đây là `quan-tri-nguoi-dung.page.html:64`, và **dòng 64 là
một dòng COMMENT**, không phải câu lệnh dựng khối lỗi. Gate `check-docs.sh` mục 6 chỉ kiểm số
dòng có nằm trong file hay không nên nó qua được; ai mở ra kiểm thì thấy một lời chú thích và
không có bằng chứng nào. Trích dẫn phải trỏ vào **câu lệnh**.

### Nghiệm thu

| # | Phép thử | PASS |
|---|---|---|
| 1 | Tắt mạng rồi đổi bộ lọc | Bảng **trống**, hiện khối lỗi có nút thử lại |
| 2 | Bật mạng, bấm thử lại | Dữ liệu về đúng bộ lọc đang chọn |
| 3 | Lọc theo điều kiện chắc chắn không khớp ai | Hiện *"không có kết quả"*, **khác hẳn** khối lỗi ở phép thử 1 |

Phép thử 3 là phép thử phân biệt: nếu "trống vì lỗi" và "trống vì không khớp" trông
giống nhau thì việc này chưa xong.
