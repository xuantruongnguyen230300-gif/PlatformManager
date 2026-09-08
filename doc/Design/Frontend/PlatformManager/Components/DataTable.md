---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "current"
updated: "2026-09-06"
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
  - "Prototypes/index.html"
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
> [headerTemplate] [bodyTemplate] [emptyTemplate?]      (pageChange)
> ```
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
> The frontmatter `sources` also listed `Prototype/index.html` and `Prototype/GHI-CHU-CAN-SUA.md`.
> Neither path exists: the prototype is at `Prototypes/index.html` in this folder, and the
> `GHI-CHU-CAN-SUA.md` file does not exist anywhere in the repo.

One variant here has **no shipped instance**: frozen edge columns, added to the contract on 2026-09-05 by decision Q30 (§ Frozen edge columns). Every other line in this file describes running code.

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

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG. No shipped instance.** The one grid that ships freezes
nothing. This variant exists because decision **Q30** pins two columns on the DTI
catalogue grid, and it is written here — as a variant of the component — rather than
as a note inside that one screen.

**What it is.** PrimeNG's `pFrozenColumn` directive pins a column against the
horizontal scroll of `[scrollable]="true"`. Two pins, one per edge:

| Edge | Which column | Binding, on both the `<th>` and the `<td>` |
| --- | --- | --- |
| Left | the row's identifier column | `pFrozenColumn` |
| Right | the row's action column | `pFrozenColumn` + `alignFrozen="right"` |

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

- It needs an opaque background, or the scrolled cells show through. That background
  comes from the PrimeNG preset, **not** from the zebra and hover rules in
  `Table.md` — so a frozen column can lose the even-row tint the rest of its row
  keeps.
- It costs width permanently. Two frozen edges on a phone leave very little
  scrollport between them.
- It is the third PrimeNG-owned surface on this component, after the paginator and
  the loading mask (§ Normalize on redesign).

**It cannot be read off the approved prototype.** [`../Prototypes/index.html`](../Prototypes/index.html)
has no PrimeNG at all, so it renders an ordinary scrolling table. The pin is a decision to be
built, not a rendering to be copied.

> 🔄 **SỬA 2026-09-06.** This paragraph cited `Prototype/index.html` and
> `Prototype/GHI-CHU-CAN-SUA.md` § 3.5. Neither path exists — the prototype lives at
> `Prototypes/index.html` inside this folder, and no file named `GHI-CHU-CAN-SUA.md` exists
> anywhere in the repo. `check-docs.sh` §4 missed both because it only resolves paths written
> with a `src/` or `doc/` prefix.

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
| Frozen edge columns 📐 | `pFrozenColumn` on the first column; `pFrozenColumn` + `alignFrozen="right"` on the last | Pinned against the horizontal scroll; requires `[scrollable]="true"`; the pinned cell's background comes from the preset, not from the zebra rule | A grid whose column `min-width`s sum past the viewport. **No shipped instance** — specified for the DTI catalogue grid by decision Q30, 2026-09-05 (§ Frozen edge columns) |

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
7. **A frozen cell is painted by PrimeNG, not by `Table.md`.** The hairline, the zebra tint and the row hover are global rules on `th, td`; a pinned cell additionally takes an opaque background from the preset so the scrolled content cannot show through it. Nothing in this design system owns that background, so the two edges of a row can drift out of step with its middle on a PrimeNG upgrade — the same blind spot as item 3, on the two columns a user looks at most. Recorded with the 2026-09-05 frozen-column variant; no shipped instance yet, so it is untested rather than broken.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **Only one of two grids was framed — resolved 2026-08-29** by subtraction: the unframed grid belonged to the business module removed the same day. The one grid that ships is wrapped in `.tablewrap` and inherits the `colors.border-strong` frame.
2. **A hand-computed pixel height — resolved 2026-08-29.** A grid used to measure itself against `window.innerHeight` on every `resize` with no debounce, and another hardcoded `480px`. Height now comes from `dimension.grid-h`, or from the page's flex chain on a `.page-fill` layout.
3. **Two default page sizes for two grids with the same paginator options — resolved 2026-08-29** with the second grid's removal. Only page size 10 ships.
4. **Scroll ownership was undocumented — recorded 2026-08-29.** The interaction between `.tablewrap.scroll` and `p-table`'s own container is now stated in the template, in `styles.scss`, and in both this spec and `Table.md`, because it fails in a way nothing reports: the header simply stops sticking.
