# 5. Thư viện component dùng chung

## Phạm vi áp dụng — PrimeNG vs hand-rolled (Đã CHỐT 2026-08-15)

Sau khi đối chiếu thực tế thị trường ERP/chuyển đổi số (xem
[04-design-token-system.md](04-design-token-system.md) §Thư viện component),
PrimeNG là mặc định cho thành phần **tương tác phức tạp**. 8 component đơn
giản đã build (bảng dưới) **không bắt buộc migrate ngay** — chi phí viết lại
không tương xứng lợi ích khi chúng đã chạy đúng và khớp thiết kế 1:1.

| Component | Quyết định | Vì sao |
|---|---|---|
| Table/Grid (`danh-muc-dti`) | **PrimeNG `p-table`** — mọi grid mới; grid cũ giữ tới khi cần mở rộng lớn | Xem [11-grid-and-metadata.md](11-grid-and-metadata.md) — đây là thành phần rủi ro "chay" thật nhất |
| Chart | **PrimeNG `p-chart`** | Xem [12-charting.md](12-charting.md) |
| Dropdown/Select có tìm kiếm, multiselect, date-range picker, autocomplete | **PrimeNG** (`p-select`, `p-multiselect`, `p-datepicker`...) khi lần đầu cần — **không** tự viết tay | Đây đúng nhóm input phức tạp mà tự viết tốn công + dễ thiếu a11y (xem cảnh báo 5 trạng thái bên dưới) |
| Dialog | Giữ nguyên `<dialog>` gốc hiện có — chỉ đổi sang `p-dialog` khi cần animation/nested dialog thật sự | Dialog gốc đã đơn giản, đủ dùng, không có nỗi đau rõ ràng để đổi ngay |
| Button, Card, Badge, ProgressBar, NoticeBanner, DeltaIndicator, HistoryRow, KpiTile, Input (text/number cơ bản) | **Giữ nguyên hand-rolled** — không migrate | Đã build đúng, khớp `Components/*.md` 1:1, đơn giản, không có tính năng ẩn khó tái tạo — PrimeNG không mang lại lợi ích tương xứng chi phí đổi |

**Nguyên tắc chung khi phân vân:** component càng nhiều trạng thái tương
tác/logic ẩn (sort, filter, keyboard nav phức tạp, a11y nhiều quy tắc) →
càng nên dùng PrimeNG. Component càng đơn giản (chỉ hiển thị + 1-2 style
biến thể) → hand-rolled vẫn ổn, không đổi chỉ vì "cho đồng bộ".

## Nguồn — không phát minh thêm

`doc/Design/Frontend/PlatformManager/COMPONENTS.md` đã liệt kê đủ 12
component thật trích từ prototype (Button, Card, KpiTile, Badge,
ProgressBar, Table, Dialog, Fab, Input, NoticeBanner, DeltaIndicator,
HistoryRow) — implement Angular component **đúng theo spec đó**, không tự
vẽ lại từ đầu. Mỗi component Angular tương ứng 1 file spec trong
`Components/*.md` — đọc trước khi code, không đoán anatomy từ tên.

## 5 trạng thái bắt buộc — khoảng trống lớn nhất ở phần hand-rolled

> Component PrimeNG (Table/Chart/input phức tạp, xem §Phạm vi áp dụng ở
> trên) đã có sẵn `:hover`/`:focus-visible`/`:disabled`/a11y chuẩn — mục
> này chỉ áp dụng cho 8 component **giữ hand-rolled**.

`COMPONENTS.md` tự ghi nhận: prototype gốc **không có** `:hover`/`:focus`/
`:disabled` custom cho gần như mọi component (chỉ có đúng 1 rule
`.btn:active`). Khi chuyển sang Angular, đây là chỗ **phải làm tốt hơn bản
gốc**, không phải "port y nguyên" — Fidelity Policy của `doc/Design/` áp
dụng cho việc *tài liệu hoá* app hiện tại, không có nghĩa là code Angular
mới được phép thiếu accessibility.

Mỗi component dùng chung khi viết bằng Angular phải định nghĩa tường minh:

| Trạng thái | Yêu cầu tối thiểu |
|---|---|
| `default` | Đúng theo spec `Components/*.md` |
| `:hover` | Đổi thị giác rõ ràng (background/border/shadow) — không để trống |
| `:focus-visible` | Outline/ring rõ, đủ tương phản — **bắt buộc cho keyboard nav**, khác `:focus` (không phạt chuột click) |
| `:active` | Giữ hiệu ứng đã có (`translateY(1px)` cho `.btn`) nếu phù hợp |
| `:disabled`/`[disabled]` | `opacity` giảm + `cursor: not-allowed` + **thật sự chặn tương tác** (Angular `[disabled]` binding, không chỉ đổi style) |

## Composition — mở rộng spec, không tạo biến thể ngầm

Cần 1 biến thể chưa có trong `Components/*.md` (vd `Button` size nhỏ hơn) →
sửa spec trước (thêm vào `Components/Button.md`, báo cáo), rồi mới code —
đúng nguyên tắc "Extend a spec... instead of inventing new ones" đã ghi ở
`doc/Design/CLAUDE.md`.

## Vị trí trong cây thư mục

```
shared/components/<name>/
├── <name>.ts          # standalone, input()/output(), không inject service data
├── <name>.html
└── <name>.scss        # dùng token, không hex trần (xem 04-design-token-system.md)
```

Ngoại lệ đã ghi nhận (audit trước): component "app-shell" (`sidebar`,
`topbar`, `toast`) được phép inject service hạ tầng UI singleton
(`SidebarStateService`, `NotificationService`) dù nằm trong `components/` —
đây là ngoại lệ tường minh cho lớp vỏ app, **không** áp dụng cho component
hiển thị dữ liệu nghiệp vụ.

## Test trực quan

Trước khi coi 1 component "xong": kiểm tra thật 5 trạng thái qua
`chrome-devtools-mcp` hoặc thao tác tay (tab qua bằng bàn phím để thấy
`:focus-visible`, set `[disabled]="true"` để thấy trạng thái khoá) — không
chỉ đọc code rồi coi là đủ.
