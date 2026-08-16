# F2 — Dọn nợ kỹ thuật hiện có

> **Định nghĩa hoàn thành:** grep hex color literal (`#[0-9a-fA-F]{3,6}`)
> trong `src/app/**/*.scss` chỉ còn khớp ở đúng những nơi **định nghĩa**
> token (`styles.scss` `:root`), không còn ở nơi **dùng**; có ít nhất 2 file
> test mới (mapper rủi ro cao nhất + interceptor) chạy xanh trong CI.

## 9 chỗ hardcode hex cần gỡ (theo audit 2026-08-14)

| File | Giá trị hex | Token thay thế |
|---|---|---|
| `criteria-grid-table.scss` (nhiều dòng) | `#fff`, `#f8fafc` | `var(--card)`, cân nhắc token mới `--surface-alt` |
| `editable-cell.scss` | `#fff`, `#cad4e1`, `#bfe3d2`, `#e7f7f0`, `#f3caca`, `#fdecec` | `var(--card)`, `var(--line)` hoặc token mới cho cặp good/bad background |
| `pagination.scss` | `#fff` | `var(--card)` |
| `sidebar.scss` | `#fff` | `var(--card)` |
| `period-toolbar.scss` | `#fff` | `var(--card)` |
| `report-dialog.scss` | `#f8fafc`, `#cbd6e5` | token mới `--surface-alt`/`--line-strong` |
| `group-progress-list.scss` | `#edf1f6` | token mới `--surface-track` (đã có tên tương ứng trong `DESIGN.md` frontmatter — `surface-track: "#edf1f6"`, chỉ chưa đưa vào `styles.scss`) |

Với các màu **chưa có token** (`#f8fafc`, `#cad4e1`, `#bfe3d2`/`#e7f7f0`,
`#f3caca`/`#fdecec`, `#cbd6e5`) — thêm vào `:root` của `styles.scss` trước
(đặt tên theo `DESIGN.md` frontmatter đã có sẵn: `surface-table-header`,
`border-input`, `surface-badge-success`, `surface-badge-danger`,
`border-report-dashed`), rồi mới thay thế từng chỗ dùng — **không** để
component tự định nghĩa token cục bộ trùng ý nghĩa với token global.

## 2 file test ưu tiên

1. `modules/danh-muc-dti/services/danh-muc-dti.service.spec.ts` — mapper
   rủi ro cao nhất theo audit trước (5 mapper, 18 field/row) — dùng
   `provideHttpClientTesting`, khẳng định mapper giữ đủ field và service
   unwrap đúng `IApiResult<T>.data`.
2. `core/interceptors/http-error.interceptor.spec.ts` — đã mô tả mẫu ở
   [../06-testing-strategy.md](../06-testing-strategy.md), chính là bài
   kiểm chứng cho F0.

## Kiểm chứng

- [ ] `grep -rn "#[0-9a-fA-F]\{3,6\}" src/FE/src/app --include=*.scss` chỉ
      còn kết quả ở nơi **định nghĩa** token mới (nếu có thêm), không còn ở
      component dùng màu trần
- [ ] 2 file test §trên tồn tại, chạy `ng test` xanh
- [ ] Không token mới nào được thêm mà chưa đối chiếu tên với `DESIGN.md`
      frontmatter (tránh 2 tên khác nhau cho cùng 1 màu)
