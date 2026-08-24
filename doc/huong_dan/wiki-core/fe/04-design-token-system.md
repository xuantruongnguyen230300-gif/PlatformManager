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
[trien-khai/02-f1-design-token.md](trien-khai/02-f1-design-token.md),
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

## Ngoại lệ một lần — giai đoạn F1

Quy tắc trên chỉ áp dụng được **khi đã có code**. Ở F1 của `src/FE` viết mới
thì chưa có `styles.scss` nào để làm nguồn, nên chiều đi **ngược lại đúng một
lần**: `DESIGN.md` → `styles.scss`. Từ sau F1 mới quay về chiều thường ngày.

Vì sao không được lấy giá trị từ `src/FE` cũ — nợ tương phản WCAG đã sửa trong
`DESIGN.md` nhưng chưa bao giờ vào code:
[trien-khai/02-f1-design-token.md](trien-khai/02-f1-design-token.md).

## Quy tắc dùng token trong component

```scss
// ✅ ĐÚNG
.card { background: var(--card); border-radius: var(--radius-lg); }

// ❌ SAI — hex trần dù token --card đã tồn tại
.card { background: #fff; }
```

- **Không hardcode hex/px** khi token tương ứng đã tồn tại. Kiểm bằng lệnh
  grep chứ không bằng danh sách đếm tay — xem
  [trien-khai/02-f1-design-token.md](trien-khai/02-f1-design-token.md) §Kiểm chứng.
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

## Dark mode — kiến trúc đã sẵn sàng, cơ chế switch thì chưa

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> "Dark mode" ở trên đúng khi nói `tokens.json` đã có sẵn 3 set
> `global`/`light`/`dark` (`dark` cố ý để rỗng) — nhưng đó chỉ là nửa việc
> (giá trị màu). Nửa còn lại — **cơ chế chuyển theme lúc runtime** — chưa
> được bàn tới, và đây mới là phần hay phải viết lại nếu không tính trước,
> vì nó không nằm trong `tokens.json`, nó nằm ở cách chọn CSS selector.

Tin tốt: cách hệ thống đang dùng token — CSS custom property trên `:root`
(`--bg`, `--card`, `--text`...) thay vì hex trần — đã đúng **tiền đề bắt
buộc** để bật dark mode mà không phải viết lại component nào (component chỉ
biết `var(--card)`, không biết giá trị thật là gì). Cái thiếu là **selector**
quyết định giá trị nào được dùng lúc nào:

```scss
// styles.scss — mẫu 3 lớp chuẩn ngành, áp dụng khi có set `dark` thật trong tokens.json
:root {
  --bg: #eef2f8;   // light — giá trị mặc định, giữ nguyên vị trí hiện có
  --card: #ffffff;
}

// Lớp 1: tôn trọng OS khi user CHƯA chọn tay
@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) {
    --bg: #0f1420;
    --card: #171d2b;
  }
}

// Lớp 2: override tường minh khi user bấm toggle trong app (thắng cả OS)
:root[data-theme='dark'] {
  --bg: #0f1420;
  --card: #171d2b;
}
```

- **Vì sao không dùng một mình `prefers-color-scheme`:** nó không cho người
  dùng tự chọn dark khi OS đang light (hoặc ngược lại) — toggle trong app
  cần 1 điểm neo DOM (`[data-theme]`) để thắng được OS setting.
- **Tránh FOUC (nhấp nháy sai theme lúc load):** `data-theme` phải được set
  **trước** Angular bootstrap/first paint — 1 script inline nhỏ trong
  `index.html` đọc `localStorage` rồi set attribute lên `<html>` ngay,
  không đợi component nào chạy.
- **PrimeNG preset chỉ cần thêm 1 khoá, không viết lại.** Preset đã có ở
  đầu file này (`definePreset(Aura, { semantic: { colorScheme: { light:
  {...} } } })`) — thêm dark là thêm `colorScheme.dark` vào cùng object đó,
  không phải tạo preset thứ hai.

Việc cần làm khi bật thật (không làm bây giờ, chỉ ghi lại để không phải đoán
từ đầu): điền `tokens.json` → `dark`, thêm 2 lớp CSS trên, thêm 1
`ThemeService` (signal `'light' | 'dark' | 'system'`, ghi `localStorage`, set
attribute). Không bước nào đòi migrate lại component đã viết theo token.

## Token breakpoint/responsive — nhất quán bằng kỷ luật, chưa bằng cơ chế

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `Tokens/spacing.md` xác nhận 3 breakpoint (`980px`/`560px`/`981px`) được
> dùng nhất quán qua toàn bộ SCSS hiện có — không phải mỗi nơi tự bịa số
> khác nhau. Nhưng nhất quán đó tới từ **kỷ luật của người viết**, không
> phải một cơ chế chặn được sai lệch — khác hẳn màu/spacing, nơi
> `var(--card)` thật sự **không biên dịch được** nếu gõ sai tên token.

Lý do gốc: `tokens.json` khai `breakpoint.tablet = 980px` như 1 token DTCG
hợp lệ (đúng định dạng, dùng được cho tài liệu/Figma) — nhưng **CSS
`@media` không đọc được CSS custom property**. `@media (max-width:
var(--breakpoint-tablet))` không phải CSS hợp lệ (giới hạn của đặc tả CSS,
không phải lỗi cấu hình) — nên khác với `--card`/`--brand`, token breakpoint
không có đường nào tự chặn 1 file SCSS mới lỡ gõ `979px` thay vì `980px`.

Cách chuẩn ngành xử lý đúng giới hạn này — biến SCSS + mixin, mất đi lúc
build nhưng là chỗ **duy nhất** thật sự enforce được số breakpoint;
`tokens.json` vẫn giữ vai trò tài liệu/Figma song song, không thay thế:

```scss
// core/theme/_breakpoints.scss
$breakpoint-tablet: 980px;   // khớp tokens.json breakpoint.tablet — sửa cả 2 khi đổi
$breakpoint-mobile: 560px;   // khớp tokens.json breakpoint.mobile

@mixin bp-tablet { @media (max-width: $breakpoint-tablet) { @content; } }
@mixin bp-mobile { @media (max-width: $breakpoint-mobile) { @content; } }
```

```scss
// dùng trong component thay vì @media (max-width: 980px) gõ tay lặp lại
.layout { grid-template-columns: 1.15fr 0.85fr; }
@include bp-tablet { .layout { grid-template-columns: 1fr; } }
```

- **2 nguồn (`tokens.json` + `_breakpoints.scss`) phải khớp tay** — chấp
  nhận được ở quy mô này, đúng tinh thần mục "2 nguồn phải luôn khớp nhau"
  ở trên, chỉ khác: không có `grep`/`var()` nào ép được ở đây, người sửa
  phải tự nhớ sửa cả hai. Ghi comment tại chỗ để lần sau không ai chỉ sửa 1
  bên.
- Nếu sau này có logic TypeScript cần biết breakpoint (vd `isMobile()` qua
  `window.matchMedia`) → đọc lại đúng con số trong `_breakpoints.scss` bằng
  comment trỏ chéo, không phịa ra số thứ ba.

## Contrast/WCAG — đã sửa tay 1 lần, chưa có cơ chế chặn lần sau

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `tokens.json` (`light.color.warn`, `light.color.bad`) tự ghi lại 1 lần sửa
> contrast thật — `warn` hạ từ `#a8690a` xuống `#965e08` vì đo được
> `3.88:1` trên `warn-bg`, dưới ngưỡng AA `4.5:1`; `bad` tương tự từ
> `3.89:1`. Việc đó chứng minh contrast **có** được kiểm — nhưng bằng tay, 1
> lần, cho đúng 2 cặp màu đang có. Token màu **tiếp theo** ai đó thêm vào
> không có gì buộc phải qua lại bước đo đó.

Thực hành chuẩn ở quy mô 5-15 dev: 1 script nhỏ đọc thẳng `tokens.json`,
tính contrast ratio theo công thức WCAG (relative luminance), chạy trước khi
coi 1 token màu mới/sửa là hợp lệ — không cần Lighthouse/axe đầy đủ cho việc
này, công thức đủ ngắn để tự viết:

```js
// scripts/check-token-contrast.mjs
import { readFileSync } from 'node:fs';

const tokens = JSON.parse(readFileSync('doc/Design/Frontend/PlatformManager/Tokens/tokens.json', 'utf8'));

function luminance(hex) {
  const [r, g, b] = hex.match(/\w\w/g).map(c => {
    const v = parseInt(c, 16) / 255;
    return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}
function contrast(hex1, hex2) {
  const [l1, l2] = [luminance(hex1), luminance(hex2)].sort((a, b) => b - a);
  return (l1 + 0.05) / (l2 + 0.05);
}

// Cặp chữ/nền thật sự ghép cạnh nhau trong UI — mở rộng khi thêm cặp mới
const pairs = [['text', 'bg'], ['text', 'card'], ['warn', 'warn-bg'], ['bad', 'bad-bg'], ['good', 'good-bg']];

let failed = false;
for (const [fg, bg] of pairs) {
  const ratio = contrast(tokens.light.color[fg].$value, tokens.light.color[bg].$value);
  if (ratio < 4.5) { console.error(`FAIL ${fg}/${bg}: ${ratio.toFixed(2)}:1 (cần ≥4.5:1 AA)`); failed = true; }
}
process.exit(failed ? 1 : 0);
```

- Chạy khi thay đổi chạm `Tokens/tokens.json` phần `color` — không cần hạ
  tầng CI (repo hiện không có CI), chạy tay trước khi báo cáo token mới là
  hợp lệ, cùng tinh thần "kiểm bằng lệnh chứ không bằng danh sách đếm tay"
  đã áp dụng cho hex trần ở mục "Quy tắc dùng token trong component" trên.
- Chỉ kiểm được **cặp đã liệt kê tường minh** trong mảng `pairs` — không tự
  suy luận cặp nào ghép cạnh nhau trong UI thật; thêm cặp mới vào mảng khi
  thêm 1 tổ hợp chữ/nền mới.

## Token drift với Figma/Stitch — chưa có đường về, và cố ý chưa cần

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `doc/Design/CLAUDE.md` §Pipeline mô tả rõ chiều **code → docs → Figma**
> (bước Tokens rồi bước Figma Export, ghi log vào
> `doc/Design/Frontend/PlatformManager/Exports/ExportLog.md`) — nhưng đó là
> đường 1 chiều. Không có gì mô tả điều xảy ra nếu ai đó sửa trực tiếp 1
> màu trong file Figma đã export (áp lực deadline, designer chỉnh tay cho
> nhanh) — giá trị đó lệch khỏi `tokens.json` mà không ai biết cho tới lần
> đối chiếu tiếp theo, nếu có.

**Quyết định phù hợp quy mô: không dựng đồng bộ 2 chiều tự động** (Tokens
Studio GitHub sync 2 chiều + review PR cho thay đổi từ Figma) — chi phí vận
hành/hạ tầng đó chỉ đáng ở đội có designer chuyên trách sửa token thường
xuyên qua Figma. Ở quy mô 5-15 dev, kiểm soát đúng mức là **quy trình**,
không phải tool:

- File Figma export ra được coi là **bản xem, không phải nguồn** — mọi thay
  đổi giá trị token phải bắt đầu lại từ `styles.scss` (đúng chiều đã chốt ở
  mục "2 nguồn phải luôn khớp nhau" trên), rồi chạy `/design-extract-tokens`,
  rồi export lại — không sửa thẳng trong Figma rồi coi là xong.
- Vì không có cơ chế máy phát hiện lệch, đây là chỗ **duy nhất** trong toàn
  bộ luồng token mà tính đúng phụ thuộc hoàn toàn vào người, không phải
  lệnh `grep`/script — ghi nhận tường minh để không ai tưởng nhầm nó đã có
  gate như phần màu/breakpoint ở trên.
