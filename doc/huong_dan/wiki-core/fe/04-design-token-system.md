---
kind: luat
scope: core
verified: 2026-09-06
---

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
// core/theme/core-preset.ts — CƠ CHẾ ở core/, bảng màu do app truyền vào
import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

export interface ICorePalette {
  readonly brand: string;
  readonly card: string;
  readonly text: string;
  // ...các trường còn lại, soi gương :root trong styles.scss — đếm bằng lệnh ở dưới
}

export function createCorePreset(palette: ICorePalette) {
  return definePreset(Aura, {
    semantic: {
      primary: { 500: palette.brand },  // map vào --brand đã có trong styles.scss
      colorScheme: {
        light: {
          surface: { 0: palette.card },
          text: { color: palette.text },
        },
      },
    },
  });
}
```

```ts
// app.config.ts — app khai bảng màu của CHÍNH NÓ rồi bơm vào
export const APP_PALETTE: ICorePalette = {
  brand: '#0f5bd7', card: '#ffffff', text: '#152033', // ...đủ MỌI trường của ICorePalette
};

providePrimeNG({ theme: { preset: createCorePreset(APP_PALETTE) } })
```

Token hiện có (`--brand`, `--card`, `--text`, `--line`...) **không đổi tên,
không mất** — chỉ thêm 1 lớp map để PrimeNG đọc đúng token, không để 2 hệ
màu song song tồn tại. Việc này làm 1 lần ở
[trien-khai/02-f1-design-token.md](trien-khai/02-f1-design-token.md),
không lặp lại cho mỗi component.

## Mọi nơi khai màu phải khớp nhau

```
doc/Design/Frontend/PlatformManager/Tokens/*.md, tokens.json   ← NGUỒN
                    ↓ code đuổi theo
src/FE/src/styles.scss  :root { --bg, --card, --fs-*, --sp-*, ... }
src/FE/src/app/app.config.ts  APP_PALETTE { brand, card, ... }   ← đừng quên chỗ này
src/FE/src/index.html  <meta name="theme-color">                 ← chỉ --brand, xem dưới
```

### Nơi thứ ba: `index.html` — ít người nhớ nhất, và người dùng nhìn thấy rõ nhất

`src/FE/src/index.html:8` khai `<meta name="theme-color" content="#0f5bd7">` — **chép đúng giá trị
`--brand`**. Trình duyệt mobile lấy hex này tô thanh địa chỉ / thanh trạng thái. Đổi màu thương
hiệu mà bỏ sót nó thì khung ứng dụng đổi màu còn viền hệ điều hành bao quanh vẫn giữ màu cũ — sai
lệch nằm **bên ngoài** vùng Angular render, nên không `grep` nào trong `src/app/` chạm tới, và
không ảnh chụp component nào bắt được.

Để hex thô ở đây là **đúng chỗ**, không phải nợ kỹ thuật: `index.html` thuộc **host**, không thuộc
CoreBase (ranh giới ở [../../../kien-truc-core-module.md](../../../kien-truc-core-module.md)
§"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU"), nên nó được phép mang dữ liệu thương hiệu — y như
`APP_PALETTE` trong `app.config.ts`. Vấn đề không phải hex nằm sai chỗ, mà là **nó chưa từng được
ghi ở đâu**. Và một file HTML tĩnh thì không có cách nào đọc `var(--brand)`: `<meta>` được đọc
trước khi CSS áp dụng, nên đây là chỗ bắt buộc phải chép tay.

Phép thử — ba nơi phải cùng một hex, đếm bằng lệnh chứ đừng chép giá trị vào bảng nào khác
(§6 `.claude/CLAUDE.md`):

```sh
grep -h -- '--brand:' src/FE/src/styles.scss
grep -h "brand:" src/FE/src/app/app.config.ts
grep -h 'name="theme-color"' src/FE/src/index.html
# PASS: cả ba dòng in ra cùng MỘT mã hex.
```

> **Chú thích trong `src/` vẫn đang khai thiếu — ghi nhận 2026-09-03, chưa sửa.**
> Khối chú thích “CHIỀU CẬP NHẬT” đầu `src/FE/src/styles.scss` (`grep -n 'CHIỀU CẬP NHẬT' src/FE/src/styles.scss`) và `src/FE/src/app/app.config.ts:61` (dòng *"Chiều cập nhật: `doc/Design/.../Tokens/*` → `styles.scss` **và** hằng số này"*) đều liệt kê **hai** nơi và không
> nhắc `index.html`. Vẫn đúng, đối chiếu lại 2026-09-06.
>
> 🔄 LẬT 2026-09-06: trích dẫn cũ ở đây là `app.config.ts:55`. Dòng 55 nói về `NG0203` và lý do
> phải truyền bảng màu bằng tham số thay vì `inject()` — **không liên quan** tới việc liệt kê
> hai nơi. Gate `check-docs.sh` mục 6 chỉ kiểm số dòng có nằm trong file; trỏ đúng file nhưng
> sai dòng thì nó vẫn xanh, còn người đi kiểm thì mở ra không thấy gì. Đây là mục chủ của chủ đề token (§5 `.claude/CLAUDE.md`) nên chỗ ghi đúng là ở
> đây; hai chú thích kia cần trỏ về mục này ở lượt chạm `src/FE` kế tiếp. Không sửa ở lượt này để
> tránh sửa `src/` ngoài phạm vi — nhưng ghi ra để nó không chìm.

## Chiều — ĐÃ CHỐT LẠI 2026-08-27, XÁC NHẬN LẠI 2026-08-29: **`doc/Design/` là nguồn, code đuổi theo**

> **Đảo so với bản trước.** Bản trước ghi *"code là nguồn sự thật, tài liệu
> mirror theo"*. Chiều đó **không dùng được nữa** vì một lý do đã đo được, không
> phải sở thích: giá trị đúng đang nằm ở tài liệu, còn code thì chưa bắt kịp —
> mirror ngược sẽ **ghi đè giá trị hỏng lên tài liệu** và xoá mất một bản vá
> tương phản đã tính toán cẩn thận.

> **Xác nhận lại 2026-08-29 — chiều GIỮ NGUYÊN, nhưng lý do đã đổi.** Người dùng
> chốt lại: `doc/Design/` là **nguồn**, code đuổi theo, còn
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html` chỉ là **bản tham
> khảo trực quan** — nơi *xem* một thay đổi trông ra sao, không phải nơi ghi nó.
>
> Tiền đề của lần chốt 27/08 (*"giá trị đúng nằm ở tài liệu, code chưa bắt kịp"*)
> **đã hết hiệu lực**: cả `styles.scss` lẫn bảng màu phía TypeScript vừa được
> viết lại, nên code không còn đi sau. Kiểm bằng lệnh chứ đừng tin câu này:
> `git log -1 --date=short -- src/FE/src/styles.scss` và
> `git status --porcelain src/FE/src/styles.scss src/FE/src/app/app.config.ts`.
>
> Chiều vẫn giữ, nay vì lý do khác và bền hơn: **tài liệu là nơi đội CHỐT thiết
> kế, không phải nơi chép lại code.** Nếu mirror ngược, `Tokens/*` chỉ còn là bản
> sao của `styles.scss` — và một quyết định vừa chốt sẽ bị xoá lặng lẽ ở lần
> trích xuất kế tiếp, không có gì báo lỗi.
>
> Hệ quả thi hành: một thay đổi xem thử ở prototype **chỉ có hiệu lực khi đã ghi
> vào** `Tokens/*.md` + `tokens.json` + `DESIGN.md`. Việc trích xuất từ code
> (`/design-inventory-ui`, `/design-extract-tokens`) vẫn chạy — nó **ghi nhận
> hiện trạng**, không phải quyết định giá trị mới.

Khi đổi theme/token: sửa `Tokens/*` + `tokens.json` + `DESIGN.md` trước, rồi
đưa vào code. Sửa cả **hai** nơi phía code — `styles.scss` và hằng số
`APP_PALETTE` trong `app.config.ts` (chỗ này khai lại **đủ bộ** màu cho ramp PrimeNG,
`core/theme/core-preset.ts` chỉ nhận vào rồi dẫn xuất; đổi bảng màu mà bỏ qua nó
thì CSS và component library render hai màu khác nhau, không có gì báo lỗi).

Số trường là thứ **đếm được bằng lệnh**, đừng chép ra tài liệu
([`../../../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §6) —
**tiêu chí PASS: hai lệnh in ra CÙNG một số**:

```bash
sed -n '/export interface ICorePalette/,/^}/p' src/FE/src/app/core/theme/core-preset.ts   | grep -cE '^\s+readonly '                      # số trường interface khai
grep -cE "^\s+[a-zA-Z]+: '#" src/FE/src/app/app.config.ts   # số trường APP_PALETTE điền
```

> 🔄 **LẬT 2026-09-08 — ba chỗ trong file này ghi "10 màu", `ICorePalette` không còn 10 trường.**
> `onPrimary` được thêm 2026-09-03 (ghi ngay trong JSDoc của chính trường đó), nhưng ba câu chép
> tay con số thì không ai sửa cùng lượt — đúng khuôn §6 sinh ra để chặn. Nay thay bằng lệnh; con
> số sẽ tự đúng ở lần thêm trường tiếp theo.

> Từ 2026-09-02, các hằng số màu **không còn nằm trong `core/`**: chúng là dữ liệu
> thương hiệu của dự án, `core/theme/core-preset.ts` chỉ giữ cơ chế dẫn xuất thang
> màu (`mix`/`ramp`) và nhận bảng màu qua tham số. Xem
> [../../../kien-truc-core-module.md](../../../kien-truc-core-module.md)
> §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU".

### ✅ Code đã bắt kịp — 2 token contrast (đối chiếu 2026-08-28, xác nhận lại 2026-09-06)

| Token | Giá trị | Trước | Đo lại sau khi sửa |
|---|---|---|---|
| `--warn` | `#965e08` | `#a8690a` (`3.88:1`) | **`4.66:1`** trên `--warn-bg` |
| `--bad` | `#a02b2b` | `#b83232` (`3.89:1`) | **`4.79:1`** trên nền hover nút danger · `5.69:1` trên `--bad-bg` |

Cả hai nay **đạt ngưỡng AA `4.5:1`** cho chữ badge 10px — mức đã chốt bắt buộc ở
[15-accessibility.md](15-accessibility.md) §1. Sửa ở **hai** nơi: `styles.scss`
và bảng màu phía TypeScript (nay là `APP_PALETTE` trong `app.config.ts`).

Xác nhận lại 2026-09-06, cả hai nơi vẫn khớp — đừng chép giá trị đi chỗ khác, đọc bằng lệnh:

```bash
grep -n -- '--warn:\|--bad:' src/FE/src/styles.scss
grep -n "warn:\|bad:" src/FE/src/app/app.config.ts
```

🔄 LẬT 2026-09-06 — hai chỗ sai trong đoạn này:

- Câu cũ neo vào **`DESIGN.md:397`** cho lời khẳng định *"bản vá đã vào cả hai file"*. Dòng 397
  của file đó là một khoá token (`text-emphasis-good`), **không** có câu nào như vậy. Trích dẫn
  bịa số dòng — đúng loại lỗi `check-docs.sh` mục 6 sinh ra để bắt, và nó lọt vì số dòng vẫn
  nằm trong file.
- Đợt sửa 2 token này (2026-08-28) **đã bị một đợt rộng hơn thay thế**: `doc/Design/.../DESIGN.md`
  §Colors ghi lần tính lại **2026-08-29** động tới tám giá trị bề mặt/đường viền. Bảng ở trên
  vẫn đúng về `--warn`/`--bad`, nhưng đừng đọc nó như bản kê đầy đủ của lần cân contrast gần
  nhất — nguồn cho việc đó là `Tokens/colors.md` § Contrast.

> Ghi chú đọc `Tokens/colors.md`: cột `*(not shipped)*` là cột **dark mode** và
> xuất hiện ở **mọi** dòng — nó **không** có nghĩa "giá trị này chưa vào code".
> Bản trước của mục này đọc nhầm đúng chỗ đó.

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

## Dark mode — hiện trạng

**Chưa có** — prototype gốc không có toggle theme/`prefers-color-scheme`
(xem `DESIGN.md` §Colors: "No dark mode exists"). Và nó đang được **tắt tường minh**, không chỉ
là "chưa làm": `providePrimeNG({ theme: { options: { darkModeSelector: false } } })` trong
`src/FE/src/app/app.config.ts` chặn hẳn auto dark-mode-selector của PrimeNG — thiếu dòng đó,
PrimeNG tự đổi màu theo OS trong khi `styles.scss` thì không, và giao diện lệch làm đôi trên
máy đang để dark mode (bổ sung 2026-09-06). Không tự thêm dark theme
khi chưa có yêu cầu — nếu cần sau này, thêm set `dark` trong `tokens.json`
(đã có cấu trúc W3C DTCG sẵn `global`/`light`/`dark`, chỉ đang để trống
`dark`) trước, rồi mới đổi code.

## Icon

Prototype hiện **không có hệ icon** (`Icons.md`: "none found", mọi cue trực
quan là text/màu/mũi tên Unicode `↑`/`↓`). Nếu component mới cần icon thật,
đây là quyết định mới — chọn 1 bộ (vd Angular Material Icons, hoặc SVG
sprite riêng) và ghi vào `Icons.md`, không lặng lẽ trộn nhiều nguồn icon.

### Dark mode — kiến trúc đã sẵn sàng, cơ chế switch thì chưa

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

## Token breakpoint/responsive — nay đã có cơ chế, không còn chỉ là kỷ luật

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `Tokens/spacing.md` xác nhận 3 breakpoint (`980px`/`560px`/`981px`) được
> dùng nhất quán qua toàn bộ SCSS hiện có — không phải mỗi nơi tự bịa số
> khác nhau. Nhưng nhất quán đó tới từ **kỷ luật của người viết**, không
> phải một cơ chế chặn được sai lệch — khác hẳn màu/spacing, nơi
> `var(--card)` thật sự **không biên dịch được** nếu gõ sai tên token.
>
> 🔄 **LẬT 2026-09-10 — vế sau đã hết đúng.** Cơ chế đã thi công: mọi `@media` theo bề rộng nay
> đi qua mixin của `src/FE/src/app/core/theme/_breakpoints.scss`, và gõ sai tên biến/mixin là
> **lỗi biên dịch Sass** (`Undefined variable`), tức đã có chốt chặn thật sự đúng như đoạn trên
> nói là còn thiếu. Vế đầu (`tokens.json` ↔ partial vẫn phải khớp tay) thì **giữ nguyên** — xem
> gạch đầu dòng cuối mục này.

Lý do gốc: `tokens.json` khai `breakpoint.tablet = 980px` như 1 token DTCG
hợp lệ (đúng định dạng, dùng được cho tài liệu/Figma) — nhưng **CSS
`@media` không đọc được CSS custom property**. `@media (max-width:
var(--breakpoint-tablet))` không phải CSS hợp lệ (giới hạn của đặc tả CSS,
không phải lỗi cấu hình) — nên khác với `--card`/`--brand`, token breakpoint
không có đường nào tự chặn 1 file SCSS mới lỡ gõ `979px` thay vì `980px`.

Cách chuẩn ngành xử lý đúng giới hạn này — biến SCSS + mixin, mất đi lúc
build nhưng là chỗ **duy nhất** thật sự enforce được số breakpoint;
`tokens.json` vẫn giữ vai trò tài liệu/Figma song song, không thay thế:

> ✅ **CÓ THẬT — thi công 2026-09-10 (đối chiếu 2026-09-10).** File chủ của breakpoint là
> `src/FE/src/app/core/theme/_breakpoints.scss`; ba biến khai ở
> `_breakpoints.scss:25` (`$tablet`), `:28` (`$mobile`), `:37` (`$desktop`).
> **Không còn `@media` theo bề rộng nào gõ tay ngoài file đó** — kiểm bằng lệnh, đừng chép danh
> sách file vào đây ([`../../../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §6):
>
> ```bash
> find src/FE/src -name '_breakpoints.scss'                              # PASS: đúng 1 kết quả
> grep -rn -E '@media *\((max|min)-width' src/FE/src --include='*.scss' \
>   | grep -v '_breakpoints.scss'                                        # PASS: không in dòng nào
> ```
>
> `@media print` và `@media (prefers-reduced-motion: reduce)` **cố ý ở ngoài** cơ chế này: chúng
> không mang giá trị bề rộng nào nên không có gì để token hoá.

Hình dạng THẬT đang chạy (đọc từ đĩa nếu nghi ngờ) — nạp bằng `@use` có namespace, nên tên gọi
là `bp.tablet` chứ không phải `bp-tablet` như bản phác trước đây của mục này:

```scss
// src/FE/src/app/core/theme/_breakpoints.scss
$tablet: 980px;            // khớp tokens.json breakpoint.tablet — sửa cả 2 khi đổi
$mobile: 560px;            // khớp tokens.json breakpoint.mobile
$desktop: $tablet + 1px;   // DẪN XUẤT, không gõ tay — xem lý do ngay dưới

@mixin tablet  { @media (max-width: $tablet)  { @content; } }
@mixin mobile  { @media (max-width: $mobile)  { @content; } }
@mixin desktop { @media (min-width: $desktop) { @content; } }
```

```scss
// dùng trong component thay vì @media (max-width: 980px) gõ tay lặp lại
@use '../../../core/theme/breakpoints' as bp;   // đường dẫn tương đối, KHÔNG cần includePaths

.layout { grid-template-columns: 1.15fr 0.85fr; }
@include bp.tablet { .layout { grid-template-columns: 1fr; } }
```

**`$desktop` dẫn xuất chứ không khai `981px` bằng tay** — đây là cạnh trên liền kề của `$tablet`,
nên viết rời hai số là để ngỏ một lỗi câm: đổi `$tablet` sang `1024px` mà quên số kia thì dải
`981px–1024px` không rơi vào khối nào. Đã kiểm bằng canary 2026-09-10: đặt `$tablet: 1024px` thì
CSS xuất ra `min-width:1025px` và không còn `min-width:981px`.

> 🛑 **Đừng "sửa" mixin thành CSS custom property.** `@media (max-width: var(--bp-mobile))` là cú
> pháp hợp lệ nên không có gì báo lỗi, nhưng nó **không bao giờ khớp** — media query được đánh giá
> trước khi custom property phân giải, và cả khối responsive biến mất im lặng. Lý do đầy đủ ghi
> ngay đầu `_breakpoints.scss`.

- **2 nguồn (`tokens.json` + `_breakpoints.scss`) phải khớp tay** — chấp
  nhận được ở quy mô này, đúng tinh thần mục "Mọi nơi khai màu phải khớp nhau"
  ở trên, chỉ khác: không có `grep`/`var()` nào ép được ở đây, người sửa
  phải tự nhớ sửa cả hai. Ghi comment tại chỗ để lần sau không ai chỉ sửa 1
  bên.
- Nếu sau này có logic TypeScript cần biết breakpoint (vd `isMobile()` qua
  `window.matchMedia`) → đọc lại đúng con số trong `_breakpoints.scss` bằng
  comment trỏ chéo, không phịa ra số thứ ba.

## Contrast/WCAG — đã sửa tay 1 lần, chưa có cơ chế chặn lần sau

> Đây là **phần a11y thuộc chủ đề token** — file chủ giữ nguyên ở đây. Điểm vào
> chung cho a11y (mức chuẩn phải đạt, checklist màn hình mới, các mục a11y không
> thuộc chủ đề nào): [15-accessibility.md](15-accessibility.md).

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

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG (đối chiếu 2026-09-06).** `scripts/` hiện chỉ có `fe-gate.sh`
> và ba file `.sql`; script dưới đây **chưa được viết**. Nghĩa là câu "chưa có cơ chế chặn lần
> sau" ở tiêu đề mục vẫn đúng nguyên. Kiểm bằng `ls scripts/`.

```js
// scripts/check-token-contrast.mjs  (chưa tồn tại — đích đến)
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
  đổi giá trị token phải bắt đầu lại từ `Tokens/*.md` + `tokens.json`, rồi mới vào code
  (`styles.scss` **và** `APP_PALETTE`), rồi export lại — không sửa thẳng trong Figma rồi coi
  là xong.

  🔄 LẬT 2026-09-06: dòng này trước đây viết *"phải bắt đầu lại từ `styles.scss`"* và tự dẫn
  chiếu là "đúng chiều đã chốt". **Ngược hẳn** §Chiều ngay trên cùng file — chiều đã chốt
  2026-08-27 và xác nhận lại 2026-08-29 là `doc/Design/` **là nguồn**, code đuổi theo. Đây là
  tàn dư của chiều cũ (*"code là nguồn, tài liệu mirror theo"*) sót lại trong đúng file đã đảo
  nó, tức một file tự mâu thuẫn — ai đọc mục này trước sẽ làm ngược ai đọc §Chiều trước.
- Vì không có cơ chế máy phát hiện lệch, đây là chỗ **duy nhất** trong toàn
  bộ luồng token mà tính đúng phụ thuộc hoàn toàn vào người, không phải
  lệnh `grep`/script — ghi nhận tường minh để không ai tưởng nhầm nó đã có
  gate như phần màu/breakpoint ở trên.
