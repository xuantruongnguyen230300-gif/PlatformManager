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

### Hiện trạng — không bắt buộc migrate ngay `CriteriaGridTable` đã có

`CriteriaGridTable` (`modules/danh-muc-dti/components/criteria-grid-table/`)
hiện chạy tốt, khớp thiết kế — **không** bắt buộc viết lại ngay lập tức chỉ
để đổi thư viện (rủi ro regression không đáng, tính năng inline-edit từng ô
hiện tại còn tinh vi hơn `p-table` mặc định hỗ trợ). Quy tắc áp dụng:

- **Module/grid mới từ giờ**: dùng `p-table` ngay, không hand-roll.
- **`CriteriaGridTable` đã có**: giữ nguyên tới khi cần sửa/mở rộng tính
  năng đáng kể (thêm sort đa cột, thêm export...) — lúc đó migrate luôn
  sang `p-table` thay vì mở rộng tiếp code tay.

### Mẫu dùng `p-table` với server-side pagination

```html
<p-table
  [value]="rows()"
  [lazy]="true"
  [paginator]="true"
  [rows]="pageSize()"
  [totalRecords]="totalCount()"
  (onLazyLoad)="onLazyLoad($event)"
>
  <ng-template #header>
    <tr><th pSortableColumn="code">Mã</th><th>Chỉ tiêu</th>...</tr>
  </ng-template>
  <ng-template #body let-row>
    <tr><td>{{ row.code }}</td>...</tr>
  </ng-template>
</p-table>
```

`[lazy]="true"` + `(onLazyLoad)` giữ nguyên đúng pattern server-side
pagination đã có (`GetGrid` nhận `Page`/`PageSize`, xem
[13-performance.md](13-performance.md) §5) — PrimeNG không ép phải tải hết
dữ liệu về client.

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

- **Loại C (menu)** — dữ liệu thuần, DB tự do 100%. **Chưa cần** ở quy mô 2
  module hiện tại (`be/03` §Áp dụng: "menu sidebar... chưa cần bảng riêng —
  2 màn hình, hard-code trong Angular route là đủ") — cùng ngưỡng đó áp
  dụng cho FE: **không** xây lớp tiêu thụ menu động trước khi BE thật sự
  phục vụ endpoint đó.
- **Loại A (cột grid)** — data facet **sinh từ code** BE, DB/JSON chỉ
  override phần trình bày (tên cột, thứ tự, ẩn/hiện) — không phải toàn bộ
  cấu trúc cột. Ngưỡng theo `be/03`: "khi có ≥5-10 màn CRUD giống nhau" —
  PlatformManager hiện có 2, **chưa chạm ngưỡng**.

### Hợp đồng đã thiết kế trước — dùng khi chạm ngưỡng

Để BE/FE không phải đàm phán lại từ đầu khi ngưỡng tới, hợp đồng JSON cho cả
2 loại **chốt sẵn hình dạng** ở đây:

```ts
// core/models/menu-item.model.ts — Loại C
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

`GET /api/meta/menu`, `GET /api/meta/grid/{gridKey}` — 2 endpoint riêng (BE
tự thêm khi cần, xem `be/03-metadata-driven-design.md` §3.2 nếu quyết định
dùng cột JSON thay vì bảng riêng). `MetadataService` (`core/services/`) gọi,
cache trong `signal()` (không cần `signalStore()` — state đơn giản, đọc
nhiều/ghi gần như không có), invalidate bằng cách gọi lại khi user thao tác
đổi cấu hình (không cần polling).

**Việc KHÔNG làm bây giờ:** viết `MetadataService`/2 model trên thành code
thật trước khi BE có endpoint — đây là hợp đồng **thiết kế trước**, không
phải thứ cần implement ngay ở F0–F3.

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
// modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.ts
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
