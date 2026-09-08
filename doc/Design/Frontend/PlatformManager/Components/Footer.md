---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Footer"
sources:  # 2 nguồn dưới modules/dashboard/ đã xoá 2026-08-29 cùng module DtiWeekly
  - "src/FE/src/styles.scss"
---

# Footer

> 🗄️ **Chỗ nào dưới đây nhắc `/dashboard` hoặc `/danh-muc/dti` là LỊCH SỬ, không phải hiện trạng.**
> Module nghiệp vụ `DtiWeekly` — cả cây `src/FE/src/app/modules/` — đã xoá 2026-08-29 để xây lại; danh
> sách route sống hôm nay đọc thẳng ở `src/FE/src/app/app.routes.ts`. Spec này vẫn là `kind: luat` vì
> component còn dùng thật trên màn Core, nên **không** hạ cả file xuống `lich-su` — chỉ những chỗ nhắc
> màn đã xoá mới bị hạ cấp, và chúng đều mang dấu 🗄️ hoặc được viết ở thì quá khứ. Nội dung đầy đủ
> của hai màn đó nằm ở file chủ lịch sử [`../Screens/01-dashboard.md`](../Screens/01-dashboard.md) và
> [`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md) (§5 — một chủ đề một file chủ); code
> tra ở commit `98a5d96`.
>
> ⚠️ **Chưa đối chiếu lại toàn file sau lần viết lại `styles.scss` ngày 2026-08-29** — đúng như
> `verified: chua-doi-chieu` ở frontmatter. Trích dẫn dạng `styles.scss § <selector>` chỉ neo theo tên
> selector chứ không theo số dòng; giá trị thật lấy bằng lệnh, đừng tin số chép trong bảng:
> `grep -n '^\.footer' src/FE/src/styles.scss`.

**Description:** `.footer` — a single closing footnote line: one muted sentence with an inline brand-coloured `routerLink`. It is **page content**, not app chrome — nó do template của route render bên trong `main`, không phải do `app.html`.

> 🗄️ **Call site duy nhất đã biến mất — spec này hiện tả một rule KHÔNG AI DÙNG.** Instance duy nhất
> là dòng ở cuối dashboard (trỏ sang danh mục DTI), đã xoá 2026-08-29 cùng module `DtiWeekly`. Phần
> CSS thì **vẫn ship**: `styles.scss` § `.footer` (+ `.footer a`, `.footer a:hover`). Kiểm bằng lệnh,
> đừng tin câu này: `grep -rn 'footer' src/FE/src/app` (0 kết quả) so với
> `grep -n '^\.footer' src/FE/src/styles.scss` (có kết quả). Nên spec giữ `kind: luat` cho phần
> **hình thức** của rule, còn mọi câu nói *nó xuất hiện ở đâu* đều là lịch sử. Việc cần quyết:
> xây lại màn có chân trang, hay xoá luôn rule khỏi `styles.scss` — xem § Normalize #7.

> **Declared once, in `src/FE/src/styles.scss` § `.footer`** — alongside the other global primitives (`.card`, `.btn`, `.title`, `.badge`). Đó vẫn là khai báo duy nhất. (🗄️ Bản cũ nói thêm rằng `dashboard.page.scss` giữ một comment ở chỗ bản sao đã xoá — file đó cũng đã xoá 2026-08-29 cùng module.)
>
> 🗄️ **History worth keeping (fixed 2026-08-22; cả hai file dưới đây đều đã xoá 2026-08-29).** The rule used to exist in full, property for property, in **both** `styles.scss` and `dashboard.page.scss` (at what were then lines 28-42; that range now holds the explanatory comment). Angular's emulated encapsulation rewrites the page-scoped copy to `.footer[_ngcontent-…]`, raising it to specificity (0,2,0) against the global copy's (0,1,0) — so the page-scoped copy was the one that painted and the global copy was inert. Because the two were byte-identical, nothing looked wrong: the duplication was undetectable until someone edited one copy and watched the change do nothing. The page-scoped copy was deleted and the global one kept, because `.footer` sits in the global primitive layer and this spec documents it as a component.

## Anatomy

A bare `<div class="footer">` containing a text sentence with one inline `<a>`. No wrapper, no icon, no separator, no columns.

- **Container** — `font-size: --fs-xs` (`typography.footer`, 11px / 400), `color: colors.muted`, `padding: 12px 4px`. That is the entire box: **no background, no border, no top rule, no radius, no shadow, no margin**. It separates from the last card by its own padding alone.
- **Link** — nested `a` rule: `color: colors.brand`, `font-weight: 700`, `text-decoration: none`; `&:hover { text-decoration: underline }`. It is an Angular `routerLink`, not an `href`, so navigation stays client-side.
- **Placement** — last element of the routed page template; it sits inside `main` (capped at `dimension.container-max-width`, padded `spacing.sp-5`) and scrolls with the page. It is **not** part of the app shell: `app.html` renders `Sidebar` → `Topbar` → `main` → `Toast` and contains no footer, so no route gets one for free. 🗄️ Vị trí cụ thể của instance cũ — sau history card, trước report `<dialog>` — thuộc dashboard đã xoá 2026-08-29.

**The same anchor treatment ships a third time.** `.notice a` (`styles.scss` § `.notice a`) declares the identical four properties — `colors.brand`, `font-weight: 700`, `text-decoration: none`, hover underline. "Brand link inside a muted block" is therefore written out three times across two files with no shared primitive.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Page footnote | `footer` | `typography.footer`, `colors.muted`, padding `12px 4px`; no surface, border or rule | 🗄️ Hiện **không có call site**. Instance duy nhất là dòng cuối dashboard, đã xoá 2026-08-29 |
| Inline link | `footer > a` | `colors.brand`, `font-weight: 700`, no underline at rest | The one navigational target inside the sentence |

There are **no** other variants: no bordered, sticky, fixed, dark, multi-column or app-shell footer exists, and no route renders one. The container also carries no `no-print` class, so it **does** print. (🗄️ Mốc so sánh cũ là period toolbar của dashboard, đã xoá 2026-08-29 cùng chính class `.no-print`.)

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

The container is non-interactive; the link is the only focusable, hoverable part. Every row below was checked against both copies of the rule and against the five `:focus-visible` blocks in `styles.scss`.

| State | Treatment |
| --- | --- |
| default | Container: `typography.footer` (11px / 400), `colors.muted`, padding `12px 4px`, transparent. Link: `colors.brand`, `font-weight: 700`, `text-decoration: none` |
| hover | Link: `text-decoration: underline`; colour and weight unchanged, no transition declared. **Container: no rule** — `.footer:hover` is authored in neither copy, and the block is not clickable |
| focus-visible | **No authored rule — the browser default ring applies.** `.footer` declares no focus rule, and `styles.scss` has **no** global `a` or `a:focus-visible` rule at all, so this link would be the app's only focusable element without the house `outline: 2px solid colors.brand` ring. 🗄️ **Danh sách đối chứng đã cũ:** bản 2026-08-22 liệt kê năm khối `:focus-visible` thuộc `.btn`, `.action-btn`, rule field của `.filters`/`.weekbar` và hai rule auth — `.action-btn`, `.filters`, `.weekbar` đều đã xoá 2026-08-29. Đếm lại bằng lệnh: `grep -n ':focus-visible' src/FE/src/styles.scss`. See § Normalize #5 |
| active / selected | **Not applicable.** No `:active` rule and no `:visited` rule is authored, so the pressed and visited appearances are the browser's defaults. There is no selected concept either: the footer never reflects the current route. 🗄️ Đích của `routerLink` trong instance cũ là `/danh-muc/dti`, route đã xoá 2026-08-29 |
| disabled | **Not applicable — an anchor cannot be disabled.** No `.footer` rule declares `opacity`, `pointer-events` or a `:disabled` selector, so there is no "unavailable" appearance to record. 🗄️ Bản cũ nói thêm khối được render vô điều kiện (không `@if`, không `[hidden]`, không guard) — quan sát đó thuộc template dashboard đã xoá 2026-08-29 |

## Tokens Used
- `colors.muted` (container text), `colors.brand` (link)
- `typography.footer` (11px / 400) — the container; `Tokens/typography.md` records it as `.footer{font-size:var(--fs-xs)}`
- `spacing.sp-1` — the `4px` horizontal padding, typed as a **literal** rather than `var(--sp-1)`
- **No token behind** the `12px` vertical padding (off the `--sp-*` scale; nearest step `--sp-5` is 14px) or the link's `font-weight: 700` (weights are not tokenised — `Tokens/typography.md`)
- No radius, border, background, shadow or motion token — the element paints and animates none
- Icons: none

## Reference markup

🗄️ **Mẫu dưới đây đã xoá cùng `DtiWeekly` (2026-08-29)** — giữ làm đầu vào thiết kế, không phải
mã đang chạy. Khối `scss` ngay sau nó thì **vẫn sống**.

```html
<!-- dashboard.page.html — the whole instance, đã xoá 2026-08-29 -->
<div class="footer">
  Xem toàn bộ danh mục &amp; nhập/cập nhật dữ liệu tại
  <a routerLink="/danh-muc/dti">Danh mục &gt; DTI</a>.
</div>
```

```scss
/* styles.scss § .footer — khai báo duy nhất, viết PHẮNG (không lồng), đối chiếu 2026-09-04 */
.footer { font-size: var(--fs-xs); color: var(--muted); padding: 12px 4px; }
.footer a { color: var(--brand); font-weight: 700; text-decoration: none; }
.footer a:hover { text-decoration: underline; }
```

🗄️ Verbatim copy của instance đã xoá: `Xem toàn bộ danh mục & nhập/cập nhật dữ liệu tại Danh mục > DTI.` — the template wrote `&amp;` and `&gt;` as HTML entities, and `Danh mục > DTI` was the link text. It was a Vietnamese literal in the template — which is why it could not ship again unchanged: the app now translates through `@ngx-translate/core` v18 (`src/FE/public/i18n/{vi,en}.json`), and `scripts/fe-gate.sh` § G12 fails any `.html` carrying Vietnamese diacritics. Rebuilding this footer means allocating a key for the sentence and a second one for the link text, with English siblings. (Corrected 2026-09-08; the previous sentence read *"there is no i18n layer"*.)

Sources: `src/FE/src/styles.scss` § `.footer` (the single declaration, with `.footer a` / `.footer a:hover`), § `.notice a` (the **second** copy of the same anchor treatment — see § Normalize #2), § `.no-print` (**not** applied here), § `:root` (`--fs-xs`, `--muted`, `--brand`), `src/FE/src/app/app.html:1-14` (the shell — no footer anywhere in it).

> 🗄️ **Nguồn đã chết, tách khỏi dòng `Sources:` ngày 2026-09-08.** Ba neo dưới đây từng nằm trên dòng `Sources:` ở trên; cả ba trỏ vào `src/FE/src/app/modules/dashboard/pages/dashboard/`, **đã xoá** 2026-08-29 cùng module `DtiWeekly`. Tra ở commit `98a5d96`, nơi `dashboard.page.scss` dài 48 dòng, `dashboard.page.html` dài 66 dòng và `dashboard.page.ts` dài 172 dòng.
>
> | Neo cũ | Nó thật sự là gì (đối chiếu tại `98a5d96`, 2026-09-08) |
> | --- | --- |
> | `dashboard.page.html` dòng `55-58` | ✅ Đúng chính xác — bốn dòng `<div class="footer">…</div>`, đúng là call site markup duy nhất |
> | `dashboard.page.ts` dòng `2,44` | ✅ Đúng chính xác — dòng 2 là `import { RouterLink } from '@angular/router'`, dòng 44 là `RouterLink,` trong mảng `imports` |
> | `dashboard.page.scss` dòng `28-33` | ⚠️ **Mô tả sai.** Neo mở đúng chỗ, nhưng chú thích đi kèm gọi nó là *"a comment at the deleted duplicate's site, not a rule"* — thứ nằm ở đó **chính là bản chép `.footer`**, một rule lồng nhau trải dài dòng `28-42`, không phải comment và không dừng ở 33. Đây là bản sao mà § Normalize #1 nói tới; mô tả cũ làm nó nhẹ đi thành "chỉ là một dòng ghi chú" |
>
> Nói cách khác: hai neo đúng hoàn toàn, một neo trỏ đúng chỗ nhưng **mô tả sai thứ nằm ở đó**. Không neo nào là neo bịa. Loại thứ ba là loại đắt nhất và cũng là loại không cổng nào chạm tới được — `check-docs.sh` kiểm được số dòng có nằm trong file không, nó không đọc được câu văn bên cạnh nói gì về nội dung ở đó.
>
> Vì sao phải rời khỏi dòng `Sources:`: một mệnh đề *"… đã xoá 2026-08-29 …"* ở cuối dòng khiến cổng miễn trừ **toàn bộ** neo trên dòng đó, sống lẫn chết. Cổng đã bịt lỗ này. Dòng `Sources:` là danh sách bằng chứng — thứ trên đó phải mở được hôm nay.

## Do / Don't

- ✅ Keep it as page content at the end of the routed template, after the last card — that is where it **shipped**, and `main`'s padding plus the footer's own `12px` top padding are all the separation it gets. (🗄️ Hiện không route nào render nó; đây là luật cho lần đặt lại.)
- ✅ Use `routerLink` for in-app destinations so navigation stays client-side; the deleted instance did.
- ✅ Reserve it for a closing footnote — one sentence pointing somewhere else. It has no structural or legal role and carries no branding.
- ❌ Don't re-declare `.footer` in a component stylesheet “to be safe”: Angular's emulated encapsulation raises the page-scoped copy's specificity above the global one, so the global copy goes inert and edits to it silently do nothing — đúng cái bẫy đã mắt một lần sửa 2026-08-22 để gỡ (§ Normalize #1). Dùng lại class toàn cục.
- ❌ Don't promote it into the app shell. `app.html` has no footer, so adding one changes **every** route at once — including the auth routes, which render with no shell at all. Đếm route bằng `src/FE/src/app/app.routes.ts`, đừng tin số chép (§6).
- ❌ Don't give it a top border or a background to "separate" it; the app separates blocks with `Card` shadows, and this element is deliberately chrome-free.

## Normalize on redesign
1. ~~**`.footer` is declared twice, identically**~~ — **FIXED 2026-08-22**, và cửa sồ tái phát đã đóng hẳn 2026-08-29 khi `dashboard.page.scss` bị xoá. `styles.scss` § `.footer` là khai báo duy nhất. Also logged in `UiInventory.md` § Normalize #5, `Tokens/spacing.md` § Normalize #3 and `Screens/01-dashboard.md` § Normalize.
2. **The anchor treatment is declared twice** — here and as `.notice a` (`styles.scss` § `.notice a`), same colour, weight and hover-underline. It was three times until the duplicate `.footer` block went (item 1); one shared link primitive would replace both and give the app a single link style.
3. **Padding `12px 4px` is half off-scale.** `12px` has no step on `--sp-*` (nearest `--sp-5` is 14px); `4px` *is* `--sp-1` but is typed as a literal. Both are free to fix — the second with zero visual change.
4. **`font-weight: 700` is a literal.** There is no `--fw-*` scale anywhere in the app (`Tokens/typography.md` § Normalize #3), so this link's weight cannot be changed systemically.
5. **The link has no house focus ring.** Every other focusable control authors `outline: 2px solid var(--brand)`; this one falls through to the user-agent default, so keyboard focus looks different here from everywhere else in the product.
6. **At rest the link is signalled by colour and weight only**, at `typography.footer`'s 11px, with the underline appearing on hover. Below the smallest type step, `colors.brand` on `colors.bg` is a thin affordance — the same objection applies to `.notice a`.
7. 🗄️ **Rule còn, call site thì không — phải chốt (mở 2026-09-04).** `.footer` vẫn nằm trong `styles.scss` nhưng không template nào dùng kể từ 2026-08-29. Hai lối đi, chọn một: (a) màn xây lại có chân trang — giữ rule, spec này thành luật trở lại; (b) không — xoá rule khỏi `styles.scss` và hạ cả file này xuống `kind: lich-su`. Để nguyên trạng thái lơ lửng là cách CSS chết tích lại.
