# F1 — Đồng bộ hệ thống Design

> **Định nghĩa hoàn thành:** `npx --yes --package=@google/design.md designmd
> lint doc/Design/Frontend/PlatformManager/DESIGN.md` chạy 0 lỗi trên token
> mới nhất; `danh-muc-dti.html` có đủ 7 mục bắt buộc trong `Screens/`; cả 12
> component trong `COMPONENTS.md` có dòng mô tả `:hover`/`:focus-visible`/
> `:disabled` không còn ghi "not styled"; PrimeNG cài đặt + theme khớp token,
> kiểm chứng bằng 1 component mẫu render đúng màu.

## 4 việc độc lập — làm song song được

### 1. Re-run token extraction

```
/design-extract-tokens PlatformManager
```

Chạy sau khi xác nhận `styles.scss` là nguồn đúng (đã là — theo chính
comment trong file). Kết quả: `Tokens/typography.md`, `Tokens/spacing.md`,
`tokens.json` cập nhật theo bộ "compact" thật, không còn mô tả bản cũ.

### 2. Chạy đủ pipeline cho `danh-muc-dti.html`

```
/design-inventory-ui PlatformManager        # thêm route/view danh-muc-dti vào UiInventory.md
/design-document-components PlatformManager # bổ sung component mới nếu có (khác 12 cái đã có)
/design-create-screens PlatformManager danh-muc-dti
```

Module `danh-muc-dti` đã build thật trong Angular — đối chiếu ngược: nếu
Angular component đã lệch token (9 chỗ hex, xem F2), **đặc tả viết theo
`danh-muc-dti.html` (prototype)**, không viết theo code Angular đã lệch —
giữ đúng nguyên tắc "live source" là prototype cho tới khi F2 dọn xong.

### 3. Định nghĩa 5 trạng thái cho 12 component

Với từng file `Components/*.md`, thêm dòng thật cho `:hover`/
`:focus-visible`/`:disabled` (không còn "not styled — browser UA default")
— giá trị lấy từ token đã có (`--line`, `--brand`, `--bg`...), không phát
minh màu mới. Vd `Button` hover: `border-color: var(--brand)`,
`focus-visible`: `outline: 2px solid var(--brand); outline-offset: 2px`.

Sau khi đặc tả xong → áp vào SCSS Angular thật của từng component trong
`shared/components/`.

### 4. Cài đặt + theme PrimeNG khớp token

```bash
npm install primeng @primeng/themes chart.js
```

Tạo `PlatformManagerPreset` map token hiện có vào hệ theming PrimeNG (chi
tiết + code mẫu ở [../04-design-token-system.md](../04-design-token-system.md)
§Theme PrimeNG khớp token hiện có), đăng ký qua `providePrimeNG()` trong
`app.config.ts`. Kiểm tra bằng cách dựng 1 `p-table`/`p-button` mẫu, so màu
nền/viền/chữ với đúng token gốc (`--card`, `--brand`, `--line`) — lệch màu
dù chỉ vài chỗ nghĩa là Preset map sai.

## Kiểm chứng

- [ ] `designmd lint` 0 lỗi
- [ ] `Screens/02-danh-muc-dti.md` tồn tại, đủ 7 mục như `Screens/01-dashboard.md`
- [ ] Grep `not styled` trong `COMPONENTS.md`/`Components/*.md` → 0 kết quả
- [ ] Tab bằng bàn phím qua ít nhất 1 màn hình thật, thấy rõ `:focus-visible`
      trên mọi control tương tác được
- [ ] `PlatformManagerPreset` đăng ký qua `providePrimeNG()`, 1 component
      PrimeNG mẫu (`p-button`/`p-table`) render đúng màu token, không dùng
      theme mặc định (`Aura` gốc chưa map)
