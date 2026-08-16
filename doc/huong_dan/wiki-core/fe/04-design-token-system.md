# 4. Design-token bridge — nguồn màu/spacing/radius duy nhất

## Thư viện component — Đã CHỐT LẠI (2026-08-15): PrimeNG

> Đảo ngược quyết định trước ("không dùng UI-kit nào cho style"). Lý do đảo
> ngược: PlatformManager thuộc nhóm phần mềm quản lý/báo cáo/chuyển đổi số —
> nhóm gần như chắc chắn cần Grid/Chart/input phức tạp (multiselect,
> autocomplete, date-range) mở rộng dần theo thời gian, đúng thực tế phổ
> biến của thị trường ERP Việt Nam (DevExtreme/Kendo UI/PrimeNG). Tự viết
> tay (`@angular/cdk` + SCSS thuần) cho nhóm thành phần này tạo rủi ro mở
> rộng thật: càng nhiều module, càng nhiều biến thể không đồng nhất, chi phí
> migrate sau này cao hơn nhiều so với chọn thư viện từ module đầu. Xem phân
> tích đầy đủ ở [11-grid-and-metadata.md](11-grid-and-metadata.md).

**PrimeNG** (MIT, miễn phí hoàn toàn — không có tầng ẩn trả phí như Kendo
UI/DevExtreme) là thư viện đã chốt cho Grid, Chart, và input phức tạp. Xem
[05-component-library.md](05-component-library.md) §Phạm vi áp dụng để biết
thành phần nào giữ nguyên hand-rolled, thành phần nào chuyển sang PrimeNG.

### Theme PrimeNG khớp token hiện có — không lấy giao diện mặc định

PrimeNG dùng hệ theming CSS-variable riêng (kiểu "Preset") — **không** dùng
theme mặc định (`Aura`/`Lara`...) mà tự định nghĩa 1 Preset map thẳng vào
token đã có, để giao diện vẫn đúng bản đã duyệt trong `doc/Design/`:

```ts
// core/theme/platform-manager-preset.ts
import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

export const PlatformManagerPreset = definePreset(Aura, {
  semantic: {
    primary: { 500: '{brand}' },        // map vào --brand đã có trong styles.scss
    colorScheme: {
      light: {
        surface: { 0: '{card}' },
        text: { color: '{text}' },
      },
    },
  },
});
```

```ts
// app.config.ts
providePrimeNG({ theme: { preset: PlatformManagerPreset } })
```

Token hiện có (`--brand`, `--card`, `--text`, `--line`...) **không đổi tên,
không mất** — chỉ thêm 1 lớp map để PrimeNG đọc đúng token, không để 2 hệ
màu song song tồn tại. Việc này làm 1 lần ở
[trien-khai/02-f1-dong-bo-design.md](trien-khai/02-f1-dong-bo-design.md),
không lặp lại cho mỗi component.

## 2 nguồn phải luôn khớp nhau

```
doc/Design/Frontend/PlatformManager/Tokens/*.md, tokens.json   ← tài liệu
                    ↕ phải khớp
src/FE/src/styles.scss  :root { --bg, --card, --fs-*, --sp-*, ... }  ← code thật chạy
```

Quy tắc giống hệt BE ("1 luật = 1 nguồn"): **code là nguồn sự thật**, tài
liệu mirror theo, không ngược lại (xem `doc/Design/CLAUDE.md` §Core
Principle 4 — "Live source first"). Khi đổi theme/token: sửa `styles.scss`
trước, rồi chạy `/design-extract-tokens` để đồng bộ lại `Tokens/*.md`.

## Trạng thái hiện tại — lệch đã biết

`styles.scss` đã có bộ token "mật độ hiển thị compact" (`--fs-xs..--fs-lg`,
`--sp-1..--sp-5`, `--radius-*`, `--sidebar-w`) mới hơn 2 file
`Tokens/typography.md`/`spacing.md` — 2 file đó mô tả bản trước khi
prototype chuyển sang compact. Đây là nợ tài liệu, không phải nợ code —
xem lộ trình xử lý ở
[trien-khai/02-f1-dong-bo-design.md](trien-khai/02-f1-dong-bo-design.md).

## Quy tắc dùng token trong component

```scss
// ✅ ĐÚNG
.card { background: var(--card); border-radius: var(--radius-lg); }

// ❌ SAI — hex trần dù token --card đã tồn tại
.card { background: #fff; }
```

- **Không hardcode hex/px** khi token tương ứng đã tồn tại — vi phạm này
  đang có ở 9 chỗ trong codebase hiện tại (xem
  [trien-khai/03-f2-don-no-ky-thuat.md](trien-khai/03-f2-don-no-ky-thuat.md)),
  không nhân rộng thêm.
- Cần giá trị **chưa có token** → báo cáo, đề xuất token mới (đặt tên theo
  quy ước `--{nhóm}-{biến-thể}`, vd `--surface-alt`, `--good-bg`) — **không**
  tự thêm ngầm rồi quên báo, đúng yêu cầu đã có sẵn trong `ui-conventions.md`.
- Token global (dùng ≥3 nơi) đặt ở `styles.scss` `:root`. Token/style chỉ 1
  component dùng đặt trong chính SCSS của component đó (`styleUrl`), không
  đẩy lên global cho "tiện" — global phình to là dấu hiệu thiếu kỷ luật, khó
  biết token nào còn được dùng.

## Dark mode

**Chưa có** — prototype gốc không có toggle theme/`prefers-color-scheme`
(xem `DESIGN.md` §Colors: "No dark mode exists"). Không tự thêm dark theme
khi chưa có yêu cầu — nếu cần sau này, thêm set `dark` trong `tokens.json`
(đã có cấu trúc W3C DTCG sẵn `global`/`light`/`dark`, chỉ đang để trống
`dark`) trước, rồi mới đổi code.

## Icon

Prototype hiện **không có hệ icon** (`Icons.md`: "none found", mọi cue trực
quan là text/màu/mũi tên Unicode `↑`/`↓`). Nếu component mới cần icon thật,
đây là quyết định mới — chọn 1 bộ (vd Angular Material Icons, hoặc SVG
sprite riêng) và ghi vào `Icons.md`, không lặng lẽ trộn nhiều nguồn icon.
