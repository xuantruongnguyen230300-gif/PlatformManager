---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "current"
updated: "2026-09-09"
component: "DataTable"
sources:
  - "src/FE/src/app/shared/components/data-grid/data-grid.html"
  - "src/FE/src/app/shared/components/data-grid/data-grid.ts"
  - "src/FE/src/app/shared/components/data-grid/data-grid.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss"
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/core/theme/core-preset.ts"
  - "src/FE/src/app/app.config.ts"
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# DataTable

> ## 🆕 CHỐT 2026-09-06 — `shared/components/data-grid`, lưới bản ghi dùng chung
>
> **Quyết định người dùng.** Trước ngày này chỉ có `user-grid-table` nằm trong
> `platform/quan-tri-nguoi-dung/components/` — riêng của một feature, nên màn danh sách thứ hai
> sẽ phải dựng lại từ đầu và **dựng lại cả những chỗ dễ sai**.
>
> ### Ranh giới: component dùng chung sở hữu KHUNG, màn hình sở hữu CỘT
>
> | Thuộc `DataGrid` (dùng chung) | Thuộc từng màn hình |
> |---|---|
> | Chiều cao cố định + cuộn bên trong (`page-fill` ⇄ `grid-host` ⇄ `scrollHeight="flex"`) | Cột nào, thứ tự, `min-width` |
> | Khung `.tablewrap`, phân trang, `rowsPerPageOptions`, mặt nạ loading | Cách vẽ từng ô (avatar, tag vai trò, badge trạng thái, nút hành động) |
> | Nối `onLazyLoad` → `pageChange` theo trang 1-based | Câu cho trạng thái rỗng |
>
> **Vì sao KHÔNG gộp luôn định nghĩa cột:** hôm nay repo có **đúng một** lưới bản ghi. Thiết kế
> một API cột từ một ví dụ là đoán — và cái giá của đoán sai không phải là sửa component, mà là
> màn thứ hai phải uốn theo một API sai rồi không ai dám sửa nữa. Khi có màn danh sách thứ 2–3,
> những khuôn ô lặp lại thật (badge, ô avatar, cụm nút hành động) mới được thăng lên `shared/`,
> lúc đó **có bằng chứng** thay vì có linh cảm.
>
> **Vì sao phần KHUNG thì ngược lại — chia sẻ ngay là đúng:** nó không phải phỏng đoán, nó là
> bất biến đã được phát biểu. Và nó đã hỏng thật: lỗi "lưới không cao bằng màn hình"
> (§ SỬA 2026-09-06) tồn tại vì chiều cao đòi **ba** thao tác rời nhau ở ba file, mỗi thao tác
> nhìn riêng đều có vẻ đủ. Gói vào một component thì không còn ba chỗ để quên.
>
> ### API — template truyền bằng `TemplateRef`, KHÔNG bằng `<ng-content>`
>
> `p-table` nhận `#header`/`#body`/`#emptymessage` qua **content query**
> (`primeng-table.mjs`: `predicate: ["header"]`). Chiếu chúng qua một lớp bọc bằng `<ng-content>`
> là dựa vào chi tiết nội tại của Angular về ngữ cảnh khai báo — chạy được, nhưng hỏng âm thầm khi
> nâng phiên bản. `DataGrid` vì vậy nhận `TemplateRef` làm **input** rồi tự `ngTemplateOutlet`:
> hợp đồng hiện rõ trên chữ ký, không phụ thuộc thứ tự chiếu.
>
> ```
> [rows] [loading] [totalCount] [page] [pageSize] [rowsPerPageOptions] [dataKey] [minWidth]
> [headerTemplate] [bodyTemplate] [emptyTemplate?] [frozenColumns?]      (pageChange)
> ```
>
> Mọi dòng trên là API **đang chạy**. `[frozenColumns]` được chốt **và** thi công cùng
> ngày **2026-09-09** (`data-grid.ts:139`) — xem § Frozen edge columns.
>
> Màn hình khai `<ng-template #hdr>` / `<ng-template #row let-row>` ngay cạnh thẻ gọi và truyền
> vào. `user-grid-table` giữ nguyên vai trò cũ — nay nó là **người dùng** của `DataGrid`, chỉ còn
> phần riêng của màn Người dùng.

> ## 🔄 SỬA 2026-09-06 — lưới phải CAO BẰNG MÀN HÌNH, không co theo số dòng
>
> **Triệu chứng:** màn "Người dùng" có 2 bản ghi thì lưới chỉ cao ~250px, để lại một mảng
> trống lớn phía dưới. Yêu cầu là lưới cao cố định bằng chỗ còn lại của màn hình và **cuộn
> bên trong** khi nhiều dòng.
>
> **Hai nguyên nhân, cả hai đều là "đã dựng nhưng chưa nối":**
>
> | # | Thứ đã có | Thứ còn thiếu |
> |---|---|---|
> | 1 | Chuỗi flex `.page-fill` → `.card` → `.grid-host` khai đầy đủ ở `src/FE/src/styles.scss`, và `<main>` đã dọn sẵn ở `app.scss` | **Không trang nào đặt class `page-fill`.** `grep -rn "page-fill" src/FE/src/app --include=*.html` trả về rỗng — cơ chế là mã chết |
> | 2 | `[scrollable]="true"` | `scrollHeight="var(--grid-h)"` chỉ đặt **max-height**. Ít dòng ⇒ lưới co lại. Muốn lấp đầy phải dùng `scrollHeight="flex"` — PrimeNG đổi sang chế độ `100%` (`primeng-table.mjs:3100-3102`) |
>
> **Vì sao chỉ thêm `page-fill` là chưa đủ:** chuỗi flex cho *khung ngoài* chiều cao đúng,
> nhưng `max-height` của PrimeNG vẫn để bảng co theo nội dung. Phải sửa **cả hai** — đây là
> lý do lỗi sống sót: mỗi nửa nhìn riêng đều "đã làm rồi".
>
> **Token `--grid-h` KHÔNG bị bỏ.** Nó vẫn là chiều cao cho trang *không* dùng `page-fill`
> và cho bảng thuần `.tablewrap.scroll` (xem `Table.md`). Trong chuỗi `page-fill`, chiều cao
> do flex quyết định — đúng lập luận đã ghi sẵn ở `styles.scss`: một hằng số `calc(100dvh - N)`
> luôn sai ở đâu đó vì `N` khác nhau theo từng trang.

**Description:** The PrimeNG `p-table` server-side data grid as shipped — `[lazy]` paging with a 10/20/50 rows-per-page selector, a built-in loading mask, a custom empty message, and **PrimeNG-owned scrolling** that fills its flex parent (`scrollHeight="flex"`). It now lives in **one** place, `shared/components/data-grid/`, and has **one caller**, `user-grid-table`. It is distinct from `Table.md`, which owns the global cell, header and wrapper rules that paint it.

> 🔄 **SỬA 2026-09-06.** Two things below were written when `p-table` still sat inside
> `user-grid-table` and the scrollport was sized by a token:
>
> | Was | Is |
> | --- | --- |
> | `p-table` and its `.tablewrap` live in `user-grid-table.html` | they live in `data-grid.html`; `user-grid-table.html` holds only `<app-data-grid>` plus three `<ng-template>`s |
> | `scrollHeight="var(--grid-h)"` — height from `dimension.grid-h` | `scrollHeight="flex"` (`data-grid.html:19`) — height from the `page-fill` ⇄ `grid-host` flex chain. `dimension.grid-h` is **no longer used by this component**; it survives for plain `.tablewrap.scroll` (see `Table.md`) |
> | Column header and empty-message strings are literals | they are translation keys — `quan-tri-nguoi-dung.grid.*`, `shared.field.role`, `shared.grid.empty` |
>
> The frontmatter `sources` also listed two paths under `Prototype/`.
> `git check-ignore -v Prototype/index.html Prototype/GHI-CHU-CAN-SUA.md` prints the rule that
> excludes both: the root `.gitignore` keeps that whole folder out, so they sit on the author's
> machine and in no clone. **Corrected 2026-09-10** — this note used to say *"neither path
> exists"*, which made it read as a typo instead of the cite-something-nobody-else-can-open
> defect it actually was. The prototype anyone can open is
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html`, merged and anonymised 2026-09-10.

One variant here has **no shipped call site**: frozen edge columns, added to the contract on 2026-09-05 by decision Q30 and built into the component on **2026-09-09** (§ Frozen edge columns). Its support is running code; no grid in the app switches it on yet. Every other line in this file describes running code.

> **Citation policy.** Values cite the source **file plus selector or binding name**, not a line number — the templates were rewritten on 2026-08-29.

## Anatomy

Two files, one boundary. **`data-grid.html`** owns the frame: `<div class="tablewrap">` → `<p-table>` with three named templates and no custom PrimeNG CSS anywhere in the app. **The caller** owns what goes in those templates and passes them as `TemplateRef` inputs.

- **`<ng-template #header>`** — one `<tr>` of `<th>`s, each with an inline `style="min-width:…"`. Declared by the caller (`user-grid-table.html:22-32`), rendered by `data-grid.html:22-24`. Painted by the global `th` rule (`Table.md`): fill `colors.surface-table-header`, ink `colors.th-ink`, `typography.table-header`, sticky at `top: 0`, `z-index: 4`.
- **`<ng-template #body let-row>`** — one `<tr>` per record, `$implicit`-bound (`user-grid-table.html:34-91` → `data-grid.html:26-28`). Cells take the global `th, td` rule: bottom border 1px `colors.line`, padding `spacing.cell-padding`, `typography.table-cell`, `vertical-align: top`.
- **`<ng-template #emptymessage>`** — the caller may pass `emptyTemplate`; **if it does not**, `DataGrid` renders its own single `<tr>` with a `.grid-empty.muted` cell reading `shared.grid.empty` (`data-grid.html:30-38`). The user grid does pass one, so it can `colspan="5"` and use its own sentence.

Row rhythm is global too: even rows are tinted `colors.surface-table-header`, hovered rows `colors.bg`.

**Height, and why it needs three cooperating parts.** The page host takes `page-fill`, the `<app-data-grid>` tag takes `grid-host`, and `p-table` takes `scrollHeight="flex"`. `styles.scss` gives `.page-fill .grid-host` its `flex: 1 1 auto; min-height: 0`; `data-grid.scss:13-21` continues that chain **inside** the component down through `.tablewrap` and `p-table`. `min-height: 0` at every link is load-bearing — without it a flex item refuses to shrink below its content, the grid pushes the page taller instead of scrolling, and the paginator falls below the fold. The `:host(.grid-host)` guard means a grid used on a page that does **not** opt into `page-fill` keeps the old content-height behaviour.

### Scrolling — the trap, written down

The wrapper is **`.tablewrap` without `.scroll`**, and the grid scrolls itself:

```html
[scrollable]="true" scrollHeight="flex"
```

(`data-grid.html:17,19`)

This is deliberate, not an oversight, and the template says so inline. `p-table` wraps its own table in `.p-datatable-table-container`, which carries its own `overflow`, and it renders the paginator **outside** that container. Putting `.scroll` on `.tablewrap` therefore does two things at once:

1. the `position: sticky` `thead` loses its real scrollport, so **column headers scroll away** with the rows; and
2. the paginator is trapped **inside** the scrolling region, so a user must scroll the whole table to reach the page controls.

Handing the scroll to PrimeNG fixes both, and the height comes from the flex chain rather than any constant. `.scroll` remains correct for a plain `<table>` — see `Table.md`.

**Paging** is entirely server-side. `[lazy]="true"` suppresses PrimeNG's client-side slicing; `[first]` is derived as `(page() - 1) * pageSize()` (`data-grid.ts:96-98`) and `[totalRecords]` comes from the API. `onLazyLoad` converts PrimeNG's zero-based `first`/`rows` back into a 1-based page and re-emits it as `pageChange` (`data-grid.ts:90-94`) — the off-by-one conversion now exists in exactly one place instead of once per grid. Neither `DataGrid` nor `user-grid-table` calls a service; both are dumb, and gate **G4** forbids `shared/components/` from injecting `HttpClient`.

**Column width.** `minWidth` is an optional input written into `--data-grid-min-width` (`data-grid.html:6`, `data-grid.scss:25-27`). Omitted, the table shrinks to its container — correct only for few columns; a many-column grid that omits it gets squeezed columns instead of horizontal scroll. The shipped user grid **does not pass it** (`user-grid-table.html:8-20`): five columns fit.

**Theming** comes from the preset built by `createCorePreset(APP_PALETTE)`, which derives Aura's ramps from the app's own token values. That preset — not any stylesheet in the app — colours the paginator, the loading mask and the scroll shadows.

### Frozen edge columns — a contract extension made on 2026-09-05

✅ **CÓ THẬT — built 2026-09-09.** The input, the CSS that implements it and its tests
all ship (`data-grid.ts:139`, `data-grid.html:10-11`, `data-grid.scss:53-101`,
`data-grid.spec.ts:78-192`). **No shipped call site yet:** the one grid running today
freezes nothing, and a test fails the build if that changes silently
(`data-grid.spec.ts:110-119`). This variant exists because decision **Q30** pins two
columns on the DTI catalogue grid, and it is written here — as a variant of the
component — rather than as a note inside that one screen.

**What it is.** Two pins, one per edge, held against the horizontal scroll of
`[scrollable]="true"` — a precondition always met here, because the template hardcodes
that binding (`data-grid.html:24`). The input puts one class per edge on `.tablewrap`
(`data-grid.html:10-11`), and the pin itself is plain `position: sticky`:

| Edge | Which column | How this component expresses the pin |
| --- | --- | --- |
| Left | the row's identifier column — the **first** cell | `.tablewrap.frozen-left` → `position: sticky` + `left: 0` on `th:first-child` / `td:first-child` (`data-grid.scss:53-66`) |
| Right | the row's action column — the **last** cell | `.tablewrap.frozen-right` → `position: sticky` + `right: 0` on `th:last-child` / `td:last-child` (`data-grid.scss:68-79`) |

Both selectors carry `:not(:only-child)`, so the single-cell “no data” row is never
pinned — pinning it would glue the empty sentence to one edge while the rest of the row
scrolled past (`data-grid.scss:48-49`, asserted at `data-grid.spec.ts:169-178`).

> ### 🆕 CHỐT 2026-09-09 — a screen asks for the pin through a `frozenColumns` input, never by using the directive itself
>
> ✅ **CÓ THẬT — decided and built the same day** (`data-grid.ts:139`). The observable
> contract below is what shipped. The **mechanism** this block first named did not — see
> § SỬA 2026-09-09 immediately after it.
>
> **The problem it settles.** `pFrozenColumn` belongs to PrimeNG's `TableModule`.
> Writing it in a caller's `<ng-template #hdr>` / `#body` means that caller has to
> import `TableModule` — and `../COMPONENTS.md` § Index records `data-grid` as the
> **one and only** place `p-table` is imported (chốt 2026-09-06). One frozen grid
> would reopen that boundary for every screen built after it, and the boundary is the
> whole reason the shared grid exists.
>
> **The decision.** The boundary holds. `DataGrid` grows an input instead:
>
> ```
> [frozenColumns]
> ```
>
> The screen says **which** columns are pinned and to which edge; the component says
> **how** a pin is expressed. That is the same split this file's § CHỐT 2026-09-06
> already draws — the frame belongs to `DataGrid`, the columns belong to the screen —
> so the pin is stated with the column set and applied with the rest of the `p-table`
> vocabulary.
>
> **The type — settled 2026-09-09 against a real column list** (`data-grid.ts:32-37`):
>
> ```
> IDataGridFrozenColumns { readonly left?: boolean; readonly right?: boolean; }
> ```
>
> `left` pins the **first** column, `right` pins the **last**. This block deliberately
> left the type open — *“fix the signature when the DTI grid is built, against a real
> column list”* — and that list now exists: `Mã` pinned left, `Hành động` pinned right
> ([`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md) § Layout Blueprint).
>
> **Why an index-based API was rejected.** That same screen renders **13 columns in
> period mode and 14 in `Tất cả` mode**, while its own rule says *“the two frozen edges
> are the same columns either way”*. Column indices would therefore have to be recomputed
> by the screen every time the column count changed — the same class of off-by-one
> arithmetic that `onLazyLoad` exists to perform once instead of once per caller. Two edge
> flags say what the contract already says: first cell, last cell. Pinning more than one
> column per edge would need a different type; change it then, against the screen that
> asks for it.
>
> Call sites therefore write `<app-data-grid [frozenColumns]="…">` and **never** the
> directive. First call site: [`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md)
> § Layout Blueprint.

> ### 🔄 SỬA 2026-09-09 — the pin is `position: sticky`, not `pFrozenColumn`
>
> The block above, and the § What it is table before it, first said *“the component
> applies `pFrozenColumn`, and `alignFrozen="right"` on the last column, internally”*.
> **That mechanism is not implementable under this component's own § CHỐT 2026-09-06
> contract.** The two decisions were made four days apart, and nobody noticed they
> collided until the code was written.
>
> **Why it cannot work.** `pFrozenColumn` is an attribute directive declared in PrimeNG's
> `TableModule` and marked `isStandalone: false` (`primeng-table.mjs`, class
> `FrozenColumn`). Angular matches a directive against a template using the `imports` of
> the component that **declares** that template, not of the component that **renders** it.
> Here the `<th>`/`<td>` are declared by the *screen* and handed over as `TemplateRef`s —
> which is exactly the § CHỐT 2026-09-06 contract. So the directive written inside
> `data-grid` reaches no cell at all, while writing it at the screen pulls `TableModule`
> back into a feature folder — the one thing the block above exists to prevent.
>
> **What ships instead.** `position: sticky` with `left: 0` / `right: 0`, declared in
> `data-grid.scss:53-79`. That is what `FrozenColumn` itself does — it too only sets
> `position: sticky` plus a `left`/`right` offset — minus the part this component does not
> need: re-measuring sibling widths in JavaScript after every render. With exactly one
> column per edge the offset is always `0`.
>
> **`::ng-deep` is required, for the same reason.** The pinned cells carry the *calling*
> component's style scope, not this one's, so an ordinary scoped selector cannot reach
> them. Every block is anchored under `.tablewrap.frozen-*` — an element this template
> draws itself — so the selector cannot leak past a grid that asked to be pinned
> (`data-grid.scss:37-52`).
>
> **The lesson worth keeping.** A component that receives its cells as `TemplateRef`
> cannot apply **any** attribute-matched directive to those cells. Every future “the
> shared grid should apply X to a column” request meets this same wall, and the answer
> has the same shape: express it in CSS the component owns, or move the decision to
> whoever declares the template.

**Why this is a component contract and not a screen-local patch.** The trigger is
structural. A grid whose column `min-width`s sum past the viewport scrolls sideways,
and the moment it does, the two things a user needs at all times — *which row am I
on* and *what can I do to it* — are the first two to leave the screen. Any future
grid wide enough to scroll meets the same problem and must get the same answer, so
the rule belongs to the component. Settling it inside one screen spec would let the
second wide grid decide it again, differently — the duplication `.claude/CLAUDE.md`
§5 exists to prevent.

**When to use it.** Only on a grid that actually scrolls horizontally. The shipped
user grid has five columns that fit its container, so it freezes nothing and must not
start.

**What it costs — stated up front, because none of it is visible until it ships.** A
frozen cell is lifted out of the normal flow and painted on its own surface:

- It needs an opaque background, or the scrolled cells show through. The shipped
  implementation paints that background **from this app's own tokens**, not from the
  PrimeNG preset: `colors.card` by default, `colors.surface-table-header` on even rows,
  `colors.bg` on hover — the same three row surfaces `src/FE/src/styles.scss` declares at
  § `tbody tr:nth-child(even)` and § `tbody tr:hover` (`data-grid.scss:90-101`; opacity
  asserted at `data-grid.spec.ts:159-167`). A pinned cell therefore keeps the tint the
  rest of its row keeps, and the cost moved elsewhere — § Normalize item 7.
  🔄 **SỬA 2026-09-09.** This bullet used to read *“that background comes from the
  PrimeNG preset … so a frozen column can lose the even-row tint the rest of its row
  keeps”*. That followed from the `pFrozenColumn` mechanism, which was never built
  (§ SỬA 2026-09-09 above).
- It costs width permanently. Two frozen edges on a phone leave very little
  scrollport between them.
- It is the third PrimeNG-owned surface on this component, after the paginator and
  the loading mask (§ Normalize on redesign).

**It cannot be read off the approved prototype.** [`../Prototypes/index.html`](../Prototypes/index.html)
has no PrimeNG at all, so it renders an ordinary scrolling table. The pin is a decision to be
built, not a rendering to be copied.

> 🔄 **SỬA 2026-09-06, sửa lại 2026-09-10.** This paragraph used to cite two paths under
> `Prototype/`. Run `git check-ignore -v Prototype/index.html Prototype/GHI-CHU-CAN-SUA.md` to
> see why neither belongs in a citation: the root `.gitignore` excludes that folder, so both
> exist on the author's machine and in no clone. The 2026-09-06 revision called that *"neither
> path exists"* — they did exist, just nowhere a second reader could look, which is the entire
> defect. `check-docs.sh` §4 missed both because it only resolves paths written with a `src/` or
> `doc/` prefix. The prototype anyone can open is
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html`.

Call site: [`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md) § Layout Blueprint.

## Variants

| Variant | Classes / bindings | Key values | When to use |
| --- | --- | --- | --- |
| Wrapped scrolling grid | `.tablewrap > p-table[scrollable]` | Frame: border 1px `colors.border-strong`, radius `rounded.table`, `overflow: hidden`. Scrollport: `scrollHeight="flex"` | The shipped user grid — 5 columns, default page size 10, `dataKey="Id"`, `minWidth` **not** passed |
| Numeric column | `<th class="num">` / `<td class="num">` | Right-aligned, tabular figures | Any figure column. **No shipped call site in this grid** — the user list has no numeric column |
| Loading | `[loading]="loading()"` | PrimeNG's overlay mask and spinner, coloured by the preset; not styled or overridden by the app | Any fetch, including page changes and post-mutation refetches |
| Empty — caller-supplied | `[emptyTemplate]` | One `.muted` cell spanning all columns | Filter matched nothing — `quan-tri-nguoi-dung.grid.empty` = `Không có người dùng nào khớp bộ lọc.` (`user-grid-table.html:93-97`) |
| Empty — component default | none passed | `.grid-empty.muted`, `padding: spacing.sp-6 spacing.sp-4`, centred; colour inherited from the global `.muted`, **not** redeclared | Any grid that has nothing screen-specific to say — `shared.grid.empty` = `Không có dữ liệu.` (`data-grid.html:34-36`, `data-grid.scss:32-35`) |
| Paginator | `[paginator]="true" [rowsPerPageOptions]="[10, 20, 50]"` | Rendered by PrimeNG below the scrollport; **not** marked `no-print` | Always |
| Frozen edge columns ✅ | `[frozenColumns]="{ left: true, right: true }"` on `<app-data-grid>` — type `IDataGridFrozenColumns` (chốt **and** built 2026-09-09; `data-grid.ts:32-37,139`) | The component puts `.frozen-left` / `.frozen-right` on `.tablewrap` (`data-grid.html:10-11`); each pinned cell takes `position: sticky` + `left`/`right: 0` and keeps the row's own fill, zebra tint and hover (`data-grid.scss:53-101`). **Not** `pFrozenColumn` — § SỬA 2026-09-09. Requires `[scrollable]="true"`, which the template hardcodes | A grid whose column `min-width`s sum past the viewport. **No shipped call site** — specified for the DTI catalogue grid by decision Q30, 2026-09-05 (§ Frozen edge columns) |

The shipped row composes three documented components: `Avatar` + name/email block in the identity cell, `Badge` twice — once as `.badge.outline` per role string in the "Vai trò" cell, once as `.badge.ok` / `.badge.bad` for account status — and a pair of ghost `IconButton`s in the actions cell. The roles chip was `RoleTag` until **2026-08-29**, when that class was retired into `Badge.md`'s `.outline` variant (`COMPONENTS.md` § Retired); `user-grid-table.spec.ts` pins the column to `.badge.outline` and fails the build if it drifts back.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary unit: the body row (`tbody tr`).

| State | Treatment |
| --- | --- |
| default | Bottom hairline `colors.line` per cell, padding `spacing.cell-padding`, `typography.table-cell`; even rows tinted `colors.surface-table-header`; header pinned at `top: 0` on `colors.surface-table-header` |
| hover | `background: colors.bg` on the whole row — a global rule, so it applies here and to the hand-rolled matrices alike |
| focus | **Not applicable at row level** — rows carry no `tabindex` and no `tr:focus-visible` rule is authored. Focus lands on the controls inside a row (the two `.icon-btn`s, which draw `outline: 2px solid colors.brand`, `outline-offset: 1px`) and on the paginator buttons, which take their ring from the preset |
| active | **Not applicable — row selection is not enabled.** The instance sets no `selectionMode` and no `[(selection)]`; `dataKey` is present for row identity only, so PrimeNG's selection styles never activate |
| disabled | **Not applicable at grid level.** In-flight state is the loading mask. A single row action can be disabled — the lock button binds `[disabled]="isSelfLock(row)"` so nobody can lock their own account from the grid — and that button's appearance comes from `.icon-btn:disabled` |

## Tokens Used
- `colors.card` (table fill), `colors.line` (cell rules), `colors.surface-table-header` (header fill and zebra), `colors.th-ink`, `colors.bg` (row hover), `colors.border-strong` (`.tablewrap` frame), `colors.muted` (empty message)
- `rounded.table`
- `spacing.cell-padding`
- `spacing.sp-6` / `spacing.sp-4` (default empty-message padding)
- **No height token.** `dimension.grid-h` is **not** used by this component any more — `scrollHeight="flex"` hands sizing to the `page-fill` ⇄ `grid-host` flex chain. The token remains live for plain `.tablewrap.scroll` (`Table.md`)
- `dimension.user-grid-min-width` — recorded as the sum of the five inline `min-width`s; **not** bound anywhere, because the user grid does not pass `minWidth`
- `typography.table-header`, `typography.table-cell`
- Layer: `z-index: 4` on the sticky `th` (literal, no token)
- Indirect: every PrimeNG-rendered part — paginator, loading mask, scroll shadows — resolves through the preset built by `createCorePreset(APP_PALETTE)`, whose base values mirror the `:root` block

## Reference markup

**Inside the shared component** (`data-grid.html`) — the frame nobody re-declares:

```html
<div class="tablewrap" [style.--data-grid-min-width]="minWidth()">
  <p-table
    [value]="rows()" [loading]="loading()" [lazy]="true" [lazyLoadOnInit]="false"
    [paginator]="true" [rows]="pageSize()" [first]="firstIndex"
    [totalRecords]="totalCount()" [rowsPerPageOptions]="rowsPerPageOptions()"
    [scrollable]="true" [dataKey]="dataKey()" scrollHeight="flex"
    (onLazyLoad)="onLazyLoad($event)">
    <ng-template #header><ng-container *ngTemplateOutlet="headerTemplate()" /></ng-template>
    <ng-template #body let-row>
      <ng-container *ngTemplateOutlet="bodyTemplate(); context: { $implicit: row }" />
    </ng-template>
    <ng-template #emptymessage> … emptyTemplate() or the shared.grid.empty default … </ng-template>
  </p-table>
</div>
```

**At the call site** (`user-grid-table.html`) — only what is specific to this screen:

```html
<app-data-grid
  class="grid-host"
  [rows]="rows()" [loading]="loading()" [totalCount]="totalCount()"
  [page]="page()" [pageSize]="pageSize()" dataKey="Id"
  [headerTemplate]="hdr" [bodyTemplate]="row" [emptyTemplate]="empty"
  (pageChange)="onGridPageChange($event)" />

<ng-template #hdr>
  <tr>
    <th style="min-width:220px">{{ 'quan-tri-nguoi-dung.grid.user' | translate }}</th>
    <th style="min-width:140px">{{ 'shared.field.role' | translate }}</th>
    <th style="min-width:120px">{{ 'quan-tri-nguoi-dung.grid.status' | translate }}</th>
    <th style="min-width:110px">{{ 'quan-tri-nguoi-dung.grid.dateCreate' | translate }}</th>
    <th style="min-width:100px; text-align:right">{{ 'quan-tri-nguoi-dung.grid.actions' | translate }}</th>
  </tr>
</ng-template>
```

```ts
/** Server-side pagination — PrimeNG's zero-based event converted back to a 1-based page. */
protected onLazyLoad(event: TableLazyLoadEvent): void {
  const size = event.rows ?? this.pageSize();
  const first = event.first ?? 0;
  this.pageChange.emit({ page: Math.floor(first / size) + 1, pageSize: size });
}
```

Column headers as rendered in Vietnamese: `Người dùng` · `Vai trò` · `Trạng thái` · `Ngày tạo` · `Hành động` — keys `quan-tri-nguoi-dung.grid.user` · `shared.field.role` · `quan-tri-nguoi-dung.grid.status` · `quan-tri-nguoi-dung.grid.dateCreate` · `quan-tri-nguoi-dung.grid.actions`. Empty message: `quan-tri-nguoi-dung.grid.empty` = `Không có người dùng nào khớp bộ lọc.`; the component's own default is `shared.grid.empty` = `Không có dữ liệu.`

> 🔄 **SỬA 2026-09-06.** The old snippet showed the whole `p-table` living in
> `user-grid-table.html` with literal Vietnamese `<th>` text, and its `onLazyLoad` emitted
> `{ Page, PageSize }` (PascalCase). The shipped event interface is `IDataGridPageChange`
> with **camelCase** `{ page, pageSize }` (`data-grid.ts:7-10`) — copying the old snippet
> would produce a payload the component never emits.

Sources: `src/FE/src/app/shared/components/data-grid/` (`data-grid.html` — including the inline comment recording the `.scroll` trap — `data-grid.ts`, `data-grid.scss`), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts` (`onLazyLoad`, `isSelfLock`, `lockButtonTitle`), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss`, `src/FE/src/styles.scss` (§ 2 `.tablewrap`; § 7 the global cell, zebra and hover rules), `src/FE/src/app/core/theme/core-preset.ts`, `src/FE/src/app/app.config.ts` (theme wiring plus the `APP_PALETTE` values it feeds in)

## Do / Don't

- ✅ Keep the grid dumb. It takes `rows` / `loading` / `totalCount` / `page` / `pageSize` as inputs and emits `pageChange` plus row-action outputs; the smart page owns every API call and refetch.
- ✅ Let PrimeNG scroll itself with `[scrollable]="true" scrollHeight="flex"`, and leave `.tablewrap` **without** `.scroll`. The alternative unpins the header and buries the paginator.
- ✅ Pair `page-fill` on the page host with `grid-host` on the `<app-data-grid>` tag. Either one alone gives a grid that quietly reverts to content height.
- ❌ Don't re-declare `p-table` in a feature folder. `DataGrid` owns the frame; a screen owns columns and cell rendering, and passes them as `TemplateRef`s.
- ❌ Don't project the templates with `<ng-content>`. `p-table` matches `#header`/`#body` by content query, so a wrapper works today and breaks silently on upgrade.
- ✅ Keep `[lazy]="true"` paired with `[totalRecords]` and a derived `[first]`. Dropping `lazy` makes PrimeNG slice the page you already fetched.
- ✅ Declare column widths as inline `min-width` on each `<th>`. That is the shipped mechanism — there is no global table `min-width` and no column-config object.
- ✅ Let the preset colour PrimeNG's own chrome. Every value in it derives from `:root`, so overriding it in a stylesheet creates a second source of truth.
- ✅ Freeze **only** the two edge columns, and only on a grid that genuinely scrolls sideways. A pin on a grid that already fits costs width and buys nothing.
- ❌ Don't write `pFrozenColumn` at a call site — ask for the pin through `[frozenColumns]` (chốt 2026-09-09). The directive drags `TableModule` into a feature folder, which breaks the one-importer rule this component exists to hold — and written inside `data-grid` it reaches nothing at all, which is why the pin ships as `position: sticky` instead (§ SỬA 2026-09-09).
- ❌ Don't reach for a frozen column to make an over-wide grid acceptable. Q30 pins the edges of a fourteen-column grid; the column count itself stays on that screen's § Normalize list as a problem in its own right.
- ❌ Don't use `p-table` for a permission matrix — those are hand-rolled `<table>` on purpose (`Table.md`): a full checkbox grid, a data-driven column count and no paging.
- ❌ Don't add row selection styling; the grid enables no selection, so it would never appear.
- ❌ Don't disable a grid to express read-only — hide the row's controls instead, or disable the individual action as the self-lock guard does.

## Normalize on redesign
1. **The paginator is not marked `no-print`** while the toolbar above it is, so a printed grid carries page controls.
2. **Column min-widths are inline styles** split across five `<th>` elements — the reason `Tokens/spacing.md` has to record the grid width as a *sum*. A column config would make the total explicit.
3. **PrimeNG chrome is invisible to this design system.** Paginator spacing, mask opacity and scroll shadows are Aura defaults derived from the preset; none is recorded as a token, so a PrimeNG upgrade can move them silently.
4. **The caller's `#empty` hardcodes `colspan="5"`** (`user-grid-table.html:95`), which will silently desynchronise if a column is added. The hand-rolled matrices compute theirs (`Table.md`). The component's own default sidesteps this by not spanning at all.
5. **`.num` has no call site in the one shipped grid**, so the tabular-figure path is unexercised in the running app.
6. **The height contract spans two stylesheets and a class name, and nothing checks it.** `page-fill` on the page, `grid-host` on the tag, then `data-grid.scss:13-21` continuing the chain. Forget the class on the tag and the grid silently reverts to content height — the exact failure of § SỬA 2026-09-06, now with two of three parts inside the component instead of three of three outside it. A test asserting the rendered height, or a lint rule pairing `page-fill` with `grid-host`, would close it. *(Replaces the old item 6, which described `scrollHeight="var(--grid-h)"` — that binding no longer exists.)*
8. **`minWidth` is optional and silently wrong when omitted.** A grid with many columns that forgets it gets squeezed columns rather than horizontal scroll, and nothing reports it. The value is also still a hand-summed string, which is item 2 in a new place.
7. **A frozen cell's background restates `styles.scss` § 7 — the PrimeNG form of this risk closed 2026-09-09, a narrower one replaced it.** The original entry read: *“Nothing in this design system owns that background, so the two edges of a row can drift out of step with its middle on a PrimeNG upgrade.”* That risk is **gone**, and why it is gone matters more than the fact: the pin shipped as `position: sticky` in `data-grid.scss`, not as `pFrozenColumn` (§ SỬA 2026-09-09), so the app — not the preset — owns the pinned cell's fill, and a PrimeNG upgrade can no longer move it. What replaces it is smaller and closer to home: three rules restate the row surfaces `src/FE/src/styles.scss` declares once at § `tbody tr:nth-child(even)` and § `tbody tr:hover`, using the same tokens rather than new values (`data-grid.scss:90-101`). Change a row background there and forget it here, and a row's two edges differ from its middle — the same visible symptom, now triggered by a token change instead of a library upgrade, and now inside code this repo owns. The SCSS carries that warning at the point of copy (`data-grid.scss:81-89`). Closing it properly means one owner for the row surfaces that a pinned cell can reference instead of copy; still open.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **Only one of two grids was framed — resolved 2026-08-29** by subtraction: the unframed grid belonged to the business module removed the same day. The one grid that ships is wrapped in `.tablewrap` and inherits the `colors.border-strong` frame.
2. **A hand-computed pixel height — resolved 2026-08-29.** A grid used to measure itself against `window.innerHeight` on every `resize` with no debounce, and another hardcoded `480px`. Height now comes from `dimension.grid-h`, or from the page's flex chain on a `.page-fill` layout.
3. **Two default page sizes for two grids with the same paginator options — resolved 2026-08-29** with the second grid's removal. Only page size 10 ships.
4. **Scroll ownership was undocumented — recorded 2026-08-29.** The interaction between `.tablewrap.scroll` and `p-table`'s own container is now stated in the template, in `styles.scss`, and in both this spec and `Table.md`, because it fails in a way nothing reports: the header simply stops sticking.
