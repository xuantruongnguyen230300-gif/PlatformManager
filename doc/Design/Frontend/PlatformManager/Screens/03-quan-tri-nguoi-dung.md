---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "current"
updated: "2026-09-10"
flow: "User Administration"
screens: ["User Administration"]
source_routes: ["/quan-tri/nguoi-dung"]
---

# User Administration — Screens

User Administration is the platform's account-management screen: an administrator lists every user, searches by name or email, narrows the list by role and status, creates a user with a temporary password, edits an existing user's email / full name / roles, and locks or unlocks an account. It is a single lazy-loaded route (`/quan-tri/nguoi-dung`) gated by `authGuard` → `mustChangePasswordGuard` → `adminGuard` (`Admin` **or** `SuperAdmin`), rendered inside the app shell. The add/edit form is an in-page native `<dialog>` on this same route, not a separate route.

The screen carries four deliberate, security-visible UI decisions, all documented below as shipped behaviour: self-lock is blocked in the UI, `SuperAdmin` is omitted from the role picker but preserved on save, **locking asks first and says how long it actually takes**, and unlocking does not ask at all.

**Rebuilt 2026-08-29.** Five things changed on this screen in one pass and the spec is written against the result, not against what came before: the hand-rolled filter row became `<app-toolbar>`; the two row actions gained distinct colours; a confirmation dialog now stands in front of Lock; the filter state moved onto the URL; and the role chips moved from `.role-tag` to `.badge.outline`. Each is called out where it lands.

> ### 🔄 SỬA 2026-09-06 — second refresh, after the i18n and shared-grid work
>
> Four shipped changes landed after the 2026-08-29 rebuild and left parts of this file describing
> a screen that no longer exists. Each is corrected in place; this is the index.
>
> | This file said | As shipped (verified 2026-09-06) |
> | --- | --- |
> | *"There is **no i18n layer**"*, and ~50 Copy rows marked `—` | every string resolves through a key; § Copy is rewritten |
> | *"**there is no screen-level error state**: no banner, no retry"* | a `.notice.bad` banner **replaces the grid**, with a `Thử lại` button — § States, *error* |
> | `p-table` lives in `user-grid-table.html`, `scrollHeight="var(--grid-h)"` | it lives in `shared/components/data-grid/`, `scrollHeight="flex"` — `Components/DataTable.md` |
> | dates formatted `vi-VN`, temp password minimum quoted as 8 | locale is injected per language; the length is no longer named in the sentence |
>
> The middle two are the dangerous kind: nothing in `check-docs.sh` can see them, because every
> cited file still exists and every cited line number is still inside its file.

> **Shell:** app shell (sidebar + topbar + toast) — `src/FE/src/app/app.html`, `src/FE/src/app/app.scss`. `DESIGN.md` → Layout documents both of the app's shells; this route renders the main one — `Sidebar` at `spacing.sidebar-w` / `spacing.sidebar-w-collapsed` with `.shell-content` offset to match, a sticky `Topbar`, and a `main` capped at `spacing.container-max-width`.
> **Sources:** `src/FE/src/app/platform/quan-tri-nguoi-dung/` (pages, components, services, models, routes), `src/FE/src/app/shared/components/{toolbar,confirm-dialog,sidebar,topbar,toast}/`, `src/FE/src/app/app.html`, `src/FE/src/styles.scss`, `doc/contracts/users.md`

---

## User Administration (`/quan-tri/nguoi-dung`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:18-41`) — the route sets no `noShell` flag, so the full shell renders
  - **`Sidebar`** (`Components/Sidebar.md`; fixed left, `spacing.sidebar-w` / `spacing.sidebar-w-collapsed`, `z-index:35`, fill `colors.card`, right border `colors.line`, carrying `id="sidebar"` and `no-print`) — `sidebar.html:4-9`
  - **Shell content** (`.shell-content`, `margin-left` matching the sidebar width) — `app.scss:37-48`
    - **`Topbar`** (`Components/Topbar.md`; sticky, `z-index:20`, translucent white with `backdrop-filter: blur(10px)`, bottom border `colors.line`; inner `.topin` capped at `spacing.container-max-width`, padding `spacing.sp-4` `spacing.sp-5`, gap `spacing.sp-3`) — `topbar.html:1-25`
      - `Button` (`.btn.sidebar-hamburger`, `pi-bars`, `aria-controls="sidebar"`) — hidden ≥981px
      - `.logo h1` — the route title, delivered by `PageTitleStrategy` rather than read from `data` (`page-title.strategy.ts:70-74`)
      - `.topbar-user`: the signed-in user's full name + `Button` (`pi-sign-out` + "Đăng xuất")
    - **`main`** (capped at `spacing.container-max-width`, centred, padding `spacing.sp-5`) — `app.scss:50-71`
      - `Card` (`.card`, one card holds the whole screen body: fill `colors.card`, border `colors.line`, `colors.shadow`, `rounded.lg`, padding `spacing.sp-5`) — `quan-tri-nguoi-dung.page.html:1`
        - Title row (`.title`, the global card-title row documented in `Components/Card.md` § Anatomy; flex, space-between, gap `spacing.sp-3`, margin-bottom `spacing.sp-4`) — `quan-tri-nguoi-dung.page.html:2-13`
          - `h2` "Danh sách người dùng" (`typography.h2-title`)
          - `<span class="muted" aria-live="polite">` — the row count. **The create button is no longer here**; the title row now holds only the section name and its count. The live region is load-bearing rather than decorative: after a filter runs, the shrinking row count is the only evidence the filter did anything for a keyboard or screen-reader user, who cannot see the table get shorter. `polite`, not `assertive` — it confirms a result, it does not interrupt
        - **`Toolbar`** (`<app-toolbar>`, `Components/Toolbar.md`; `.toolbar` sits on the **host element itself** via `host: { class }`, not on a wrapper div — a wrapper would break the flex chain the layout depends on. Fill `colors.bg`, border `colors.line`, `rounded.md`, padding `spacing.sp-3` `spacing.sp-4`, gap `spacing.sp-3`, `flex-wrap`, margin-bottom `spacing.sp-5`. Carries `no-print`) — `quan-tri-nguoi-dung.page.html:18-54`, `toolbar.html:1-52`
          - `.input-icon.search` — **fixed `spacing.toolbar-search-width` (260px), `flex:none`**. Not `flex:1`: a 900px search field does not help anyone type faster, it just pushes everything else apart. Holds a `pi-search` glyph and an `Input` (`Components/Input.md`, the `.input-icon` contract shared with the auth screens; inside a toolbar it takes a tighter left inset so its height matches the button beside it). `type="search"`, with WebKit's native clear button suppressed because it duplicates the removable chips
          - `.filter` — a native `<details>`, so open/close needs no JavaScript
            - `summary.btn` — `pi-filter` + "Lọc", plus `.filter-count` (a `spacing.filter-count` pill in `colors.brand` / `colors.on-primary`, `typography.filter-count`) when at least one condition is applied. While `[open]`, the summary inverts to a filled `colors.brand` button and the count pill inverts with it
            - `.filter-panel` — absolute, `spacing.filter-panel-width` (320px), offset `spacing.filter-panel-offset` below the trigger, `z-index:50`, fill `colors.card`, border `colors.line`, `rounded.md`, its own deeper shadow, padding `spacing.sp-5`. Content is projected through `<ng-content select="[filter]">`, so every class inside it must be global
              - `FormRow` ×2 (`Components/FormRow.md`): "Vai trò" → `<select>` with `Tất cả vai trò` / `SuperAdmin` / `Admin` / `User`; "Trạng thái" → `<select>` with `Tất cả trạng thái` / `Đang hoạt động` / `Đã khoá`. Both option lists are written **statically in the template on purpose**: a `[value]` binding is applied before an `@for` block has built its `<option>`s, so a dynamic list drops the selected value on first render
              - `.filter-foot` — top border `colors.line`, space-between: `Button` "Xoá lọc" · `Button` (primary) "Áp dụng"
          - `.toolbar-sep` — a 1px `colors.line` rule, `align-self:stretch`, rendered only when something sits to its left
          - `.filter-chips` — one `.filter-chip` per applied condition (fill `colors.tonal-bg`, border `colors.line`, ink `colors.tonal-ink`, `rounded.pill`, `typography.filter-chip`), each ending in an `IconButton` (`pi-times`) whose hover deliberately goes brand-coloured rather than the shared grey, because it sits on a tonal fill
          - `.toolbar-actions` (`margin-left:auto`, `flex:none`, `:empty` collapsed): `Button` (primary) "+ Thêm người dùng" — projected through `<ng-content select="[actions]">`
        - **List-load failure notice** (`.notice.bad`, `role="alert"`) — `quan-tri-nguoi-dung.page.html:68-76`. **New since this spec was last written.** When the list fetch fails the grid is *replaced*, not merely emptied: a `NoticeBanner` in the `bad` variant with a `pi-exclamation-triangle`, the sentence, and a `Button` carrying `pi-refresh` + `Thử lại`. The toolbar stays so the user can change the filter or retry in place. See States — **error**
        - `app-user-grid-table` (in the `@else` branch, so it is absent while the notice shows) — `quan-tri-nguoi-dung.page.html:77-91`. Carries `class="grid-host"`, the second half of the height contract described in `Components/DataTable.md` § Anatomy
          - `Table` (`Components/Table.md`) — a plain global `.tablewrap` (border `colors.border-strong`, `rounded.table`, `overflow:hidden`) **without** `.scroll`. That is deliberate and the source says why: `p-table` wraps itself in `.p-datatable-table-container` with its own `overflow`, and renders the paginator *outside* it. Adding `.scroll` here would (a) take the sticky `th` away from its real scrollport, so column headers drift away while scrolling, and (b) trap the paginator inside the scroll region. Scrolling is handed to PrimeNG instead — `[scrollable]="true"` with **`scrollHeight="flex"`**, so the grid fills the flex chain rather than capping at a constant (`data-grid.html:1-6,17-19`)
          - `DataTable` (`Components/DataTable.md`) — **now the shared `<app-data-grid>`**, not a `p-table` written into this feature. It owns the frame (`[lazy]="true"`, `[lazyLoadOnInit]="false"`, `[paginator]="true"`, `[rowsPerPageOptions]` defaulting to `[10,20,50]`, `[first]` derived from the page number); this screen owns only the three `<ng-template>`s and passes them as `TemplateRef` inputs, plus `dataKey="Id"` (`user-grid-table.html:8-20`). The page-change event is renamed but **not recomputed** — `DataGrid` emits camelCase `{ page, pageSize }` and `user-grid-table.ts:67-69` maps it to the PascalCase shape the page expects
            - Header row — 5 `<th>` with **inline** `style="min-width:…"`: Người dùng 220px · Vai trò 140px · Trạng thái 120px · Ngày tạo 110px · Hành động 100px + `text-align:right`. Painting comes from the global `th` (sticky, fill `colors.surface-table-header`, ink `--th-ink`, `typography.table-header`) — `user-grid-table.html:22-32`
            - Body row (global `td`: padding `spacing.sp-2` `spacing.sp-3`, `typography.table-cell`, bottom border `colors.line`; even rows tinted `colors.surface-table-header`, hover tint `colors.bg`)
              - Cell 1 — `Avatar` (`Components/Avatar.md`, which documents the whole identity cell): `.user-cell` flex holding the initials disc, then `.user-name` over `.user-email` (falling back to `—`) — `user-grid-table.html:36-44`
              - Cell 2 — `.role-cell` (flex, wrap, gap `spacing.sp-2` — **spacing only**) holding one `Badge` per role in the **identity variant** `.badge.outline` (`Components/Badge.md`). Deliberately not a status variant: a role name is an identifier, not a state, so it carries a border instead of a semantic fill — `user-grid-table.html:45-56`
              - Cell 3 — `Badge` in a **status** variant: `.badge.bad` `● Đã khoá` or `.badge.ok` `● Đang hoạt động`. Both classes are global; neither is redeclared here — `user-grid-table.html:57-63`
              - Cell 4 — creation date via Angular's **`DatePipe` with an injected `localeId`**, `.muted` (`user-grid-table.html:64`). The locale is an `input()` fed from the page, **not** an `inject(LanguageService)`: gate **G4** forbids a dumb component from injecting services, and the first attempt to do it anyway turned 37 specs red (`user-grid-table.ts:44-59`). The old `formatDateVn()` helper is gone, and with it the hardcoded `'vi-VN'` that kept dates Vietnamese in an English interface
              - Cell 5 — `.row-actions` (flex, gap `spacing.sp-2`, right-aligned) holding **two `IconButton`s in two different colours** (`Components/IconButton.md`) — `user-grid-table.html:65-88`
                - "Sửa" — `.icon-btn.primary` (`pi-pencil`, ink `colors.brand`, hover fill `colors.tonal-bg`)
                - Lock toggle — `.icon-btn` with `[class.danger]="!row.IsLocked"` / `[class.primary]="row.IsLocked"`: red `pi-lock` to lock, brand `pi-lock-open` to unlock. **Disabled on the signed-in user's own unlocked row.** Both stay ghost buttons — a long list where every action cell has a filled button is a list nobody can read
            - Empty-message row — one `<td colspan="5" class="muted">`, passed to `DataGrid` as `[emptyTemplate]` so it overrides the component's own `shared.grid.empty` default (`user-grid-table.html:93-97`)
            - PrimeNG paginator, below the grid: first / prev / page links / next / last plus a rows-per-page dropdown. No `currentPageReportTemplate` is set, so no "showing X of Y" text renders
      - `Toast` stack (`Components/Toast.md`; fixed bottom-right, `z-index:60`, `aria-live="polite"`, auto-dismiss at 5 s) — feedback for every mutation on this screen
- `Dialog` (`dialog.form-dialog`, `Components/Dialog.md`; native `<dialog>` opened with `showModal()`, `spacing.dialog-width-form`, padding `spacing.sp-5`, `rounded.dialog`) — the Add/Edit form, a sibling of the card on the same route (`quan-tri-nguoi-dung.page.html:69-76`, `user-form-dialog.html:1-136`)
  - Title row (`.title`): `h2` "Thêm người dùng" / "Sửa người dùng" + an `IconButton` (`pi-times`, `aria-label="Đóng"`). **Deliberately `.title`, not `.dialog-head`** — the circled severity icon of `.dialog-head` is the contract of a *question* dialog, and a warning glyph above an add-user form would say the wrong thing about what is going to happen (`user-form-dialog.html:2-12`)
  - `.form-grid` (two columns, collapsing to one at `spacing.breakpoint-mobile`) ×2, holding `FormRow`s (`Components/FormRow.md`). Each input is an `Input` in the `.w-md` width variant (`Components/Input.md`) — width signals how much text is expected; a 520px box for a username says the wrong thing
    - **Create mode only**: "Tên đăng nhập" (auto-focused and selected via `appAutofocus`) + "Mật khẩu tạm" (`type="text"` — the value is deliberately readable)
    - **Both modes**: "Email" (`type="email"`) + "Họ tên"
  - Roles `FormRow`: a `.form-row-label` span + `.role-checkboxes` (flex, gap `spacing.sp-4`) with `role="group"` and `aria-labelledby`, holding one `Check` (`Components/Check.md`) per `ASSIGNABLE_ROLES` entry — **`['Admin', 'User']` only; `SuperAdmin` is deliberately absent from the picker** (`quan-tri-nguoi-dung.model.ts:84-92`)
  - `.form-error` under any invalid field, and one at the end of the form for messages that belong to no field. Each required label carries a `.required` asterisk in `colors.bad`
  - `.dialog-actions` (the **global** rule, flex, right-aligned, gap `spacing.sp-3`, margin-top `spacing.sp-5` — the local copy that used bare `8px` was removed on 2026-08-29): `Button` "Huỷ" · `Button` (primary) "Lưu"
- **`ConfirmDialog`** (`<app-confirm-dialog>`, `Components/ConfirmDialog.md`; `dialog.confirm-dialog`, `spacing.dialog-width-confirm`) — **new on this screen 2026-08-29**, a sibling of the card, opened only for Lock (`quan-tri-nguoi-dung.page.html:81-89`, `confirm-dialog.html:1-39`)
  - `.dialog-head`: `.dialog-icon.warn` (a `spacing.dialog-icon` disc, `colors.warn-bg` on `colors.warn`, `pi-exclamation-triangle`) + `.dialog-title` + `.dialog-desc` + an `IconButton` close. **`warn`, not `bad`**: this is not a delete, it is reversible, and the trash-can glyph of `bad` would overstate it
  - `.dialog-actions`: "Huỷ" (focused on open — the safe exit goes first) · "Khoá tài khoản" carrying `.btn.danger`, because the action does cut off a person's access even though it can be undone

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

**An i18n layer exists (verified 2026-09-06).** Every user-visible string on this screen now
resolves through `@ngx-translate/core` from `src/FE/public/i18n/{vi,en}.json`. The "Localization
key" column below carries the **real key** for each row; the Vietnamese column is what `vi.json`
renders today.

> ### 🔄 SỬA 2026-09-06 — this section previously claimed the opposite
>
> It read: *"There is **no i18n layer**; every string below is a hardcoded Vietnamese literal in a
> template or a TypeScript file, so the 'Localization key' column reads `—` throughout. That is a
> fact about the app, not an omission in this table."* Every part of that is now false, and the
> table under it listed **fifty-odd** rows as `—`. `check-docs.sh` could not catch it: the cited
> files all still exist and all still have the cited line numbers, so nothing was mechanically
> broken — only the sentences were wrong. This is the failure mode `.claude/CLAUDE.md` §8 names.
>
> Three key-shape rules are worth knowing before reading the table:
>
> | Family | Looks like | Owner |
> | --- | --- | --- |
> | screen copy | `quan-tri-nguoi-dung.grid.user` | the FE, written in `vi.json`/`en.json` |
> | promoted copy | `shared.field.role`, `shared.action.cancel` | the FE, but used by 2+ screens |
> | error code | `USER.SELF_LOCK_FORBIDDEN` | the **BE** picks the code, the FE picks the words |
>
> Dumb components under `components/` cannot inject `TranslateService` (gate **G4**), so they
> return **keys** and let the template's `| translate` pipe resolve them — see
> `user-grid-table.ts:84-87` for the pattern and the reason.

| Element | Verbatim copy (vi) | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `Người dùng hệ thống · PlatformManager` | **`quan-tri-nguoi-dung.routeTitle`** + injected app name | `quan-tri-nguoi-dung.routes.ts` holds the **key**; `PageTitleStrategy` resolves it and re-resolves on language change (`page-title.strategy.ts:83,91-92`), joining with `TITLE_SEPARATOR` (`:8`). The static `<title>PlatformManager</title>` in `src/FE/src/index.html:5` is only the pre-bootstrap value |
| Topbar page title | `Người dùng hệ thống` | **`quan-tri-nguoi-dung.routeTitle`** | same signal, no separator and no app name |
| Topbar logout button | `Đăng xuất` | **`shared.topbar.logout`** | `topbar.html` |
| Topbar hamburger aria-label | `Mở menu điều hướng` | **`shared.topbar.openMenu`** | `topbar.html` |
| Sidebar brand mark / text | `PM` / `PlatformManager` | — (injected, `CORE_BRANDING`) | `sidebar.html`. Product identity, not copy — it must **not** be translated |
| Card heading | `Danh sách người dùng` | **`quan-tri-nguoi-dung.title`** | `quan-tri-nguoi-dung.page.html:3` |
| Row count (live region) | `{{count}} người dùng` | **`quan-tri-nguoi-dung.grid.totalCount`** | `quan-tri-nguoi-dung.page.html:13`. An interpolated parameter, not string concatenation — word order differs per language |
| Search placeholder | `Tìm theo tên hoặc email...` | **`quan-tri-nguoi-dung.filter.searchPlaceholder`** | `quan-tri-nguoi-dung.page.html:23` (three ASCII dots, not the ellipsis character) |
| Search aria-label | `Tìm theo tên hoặc email` | **`quan-tri-nguoi-dung.filter.searchAriaLabel`** | `quan-tri-nguoi-dung.page.html:24` (no trailing dots) |
| Filter trigger | `Lọc` | **`shared.toolbar.filter`** | `Toolbar` default, not overridden |
| Filter clear / apply buttons | `Xoá lọc` / `Áp dụng` | **`shared.toolbar.clearFilter`** / **`shared.toolbar.applyFilter`** | `Toolbar` defaults |
| Filter label — role | `Vai trò` | **`shared.field.role`** | `quan-tri-nguoi-dung.page.html:33`. Promoted: the grid header and the dialog use the same label |
| Filter options — role | `Tất cả vai trò` / `SuperAdmin` / `Admin` / `User` | **`quan-tri-nguoi-dung.filter.allRoles`** plus three **untranslated** role names | `quan-tri-nguoi-dung.page.html:39-42`. Role names are server identifiers and are never re-cased or translated |
| Filter label — status | `Trạng thái` | **`quan-tri-nguoi-dung.grid.status`** | `quan-tri-nguoi-dung.page.html:47` |
| Filter options — status | `Tất cả trạng thái` / `Đang hoạt động` / `Đã khoá` | **`…filter.allStatuses`** / **`…grid.active`** / **`…grid.locked`** | `quan-tri-nguoi-dung.page.html:49-51`. The last two are the **same keys** the status badge uses — one word, one place |
| Applied-filter chip — role | `Vai trò: <role>` | **`quan-tri-nguoi-dung.filter.chipRole`** (`{{value}}`) | `quan-tri-nguoi-dung.page.ts:167` |
| Applied-filter chip — status | `Trạng thái: Đã khoá` / `Trạng thái: Đang hoạt động` | **`quan-tri-nguoi-dung.filter.chipStatus`** (`{{value}}`), value from `…grid.locked` / `…grid.active` | `quan-tri-nguoi-dung.page.ts:172-178`. Two nested lookups, so the inner word follows the language too |
| Chip remove aria-label | `Bỏ lọc <chip label>` | **`shared.toolbar.removeFilter`** (`{{label}}`) | `toolbar.html` |
| Create button | `+ Thêm người dùng` | **`quan-tri-nguoi-dung.action.create`** | `quan-tri-nguoi-dung.page.html:55-57`. The `+` is a **literal in the template**, outside the key — see Normalize |
| List load failed (notice) | `Không tải được danh sách người dùng. Kiểm tra kết nối rồi thử lại.` | **`quan-tri-nguoi-dung.error.loadFailed`** | key held in `LOAD_ERROR_KEY` (`quan-tri-nguoi-dung.page.ts:31`), set at `:325`, translated in the template (`quan-tri-nguoi-dung.page.html:71`) |
| Retry button | `Thử lại` | **`quan-tri-nguoi-dung.action.retry`** | `quan-tri-nguoi-dung.page.html:74` |
| Table column headers | `Người dùng` · `Vai trò` · `Trạng thái` · `Ngày tạo` · `Hành động` | **`…grid.user`** · **`shared.field.role`** · **`…grid.status`** · **`…grid.dateCreate`** · **`…grid.actions`** | `user-grid-table.html:24-30` |
| Missing email fallback | `—` (em dash) | — (punctuation, not copy) | `user-grid-table.html:41` |
| Creation date | locale-formatted | — (formatted, not translated) | `user-grid-table.html:64` — `DatePipe` with `localeId()` passed **in**. Before 2026-09-05 this was a hardcoded `'vi-VN'`, so dates stayed Vietnamese in an English interface (`user-grid-table.ts:44-59`) |
| Role chip text | *role name verbatim from the API* | — (server value, never re-cased) | `user-grid-table.html:53` |
| Status badge — locked / active | `● Đã khoá` / `● Đang hoạt động` | **`quan-tri-nguoi-dung.grid.locked`** / **`…grid.active`** | `user-grid-table.html:59,61`. The leading U+25CF is a template literal outside the key |
| Edit action title attr | `Sửa` | **`quan-tri-nguoi-dung.action.edit`** | `user-grid-table.html:72` |
| Lock action title — self-lock blocked | `Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất` | **`quan-tri-nguoi-dung.grid.selfLockTitle`** | key chosen at `user-grid-table.ts:85`, translated at `user-grid-table.html:83` |
| Lock action title — target locked / unlocked | `Mở khoá tài khoản` / `Khoá tài khoản` | **`…grid.unlockTitle`** / **`…grid.lockTitle`** | `user-grid-table.ts:86` |
| Grid empty message | `Không có người dùng nào khớp bộ lọc.` | **`quan-tri-nguoi-dung.grid.empty`** | `user-grid-table.html:95`. Overrides `DataGrid`'s own `shared.grid.empty` default |
| **Lock confirm — title** | `Khoá tài khoản này?` | **`quan-tri-nguoi-dung.dialog.lockTitle`** | `quan-tri-nguoi-dung.page.html:109` |
| **Lock confirm — description** | `“<Họ tên>” sẽ không đăng nhập lại được kể từ bây giờ. Phiên đang đăng nhập của người dùng này bị chấm dứt trong vòng 30 phút, KHÔNG phải ngay lập tức. Mở khoá lại được bất cứ lúc nào.` | **`…dialog.lockDescriptionNamed`** (`{{name}}`), or **`…dialog.lockDescription`** when no user is pending | `quan-tri-nguoi-dung.page.ts:234-241`. **Two whole sentences, not one sentence plus a spliced name** — a language that puts the name elsewhere needs that freedom |
| Lock confirm — confirm / cancel | `Khoá tài khoản` / `Huỷ` | **`…dialog.lockConfirm`** / **`shared.action.cancel`** | `quan-tri-nguoi-dung.page.html:111`; cancel is the `ConfirmDialog` default |
| Lock confirm — close aria-label | `Đóng` | **`shared.action.close`** | `confirm-dialog.html` |
| Dialog title — create / edit | `Thêm người dùng` / `Sửa người dùng` | **`…dialog.createTitle`** / **`…dialog.editTitle`** | `user-form-dialog.ts`. Note `dialog.createTitle` and `action.create` are **separate keys carrying the same Vietnamese words** — a heading and a button label must stay free to diverge in another language |
| Field label (create only) | `Tên đăng nhập` + `*` | **`shared.field.userName`** | promoted — the sign-in screen uses the same key |
| Field placeholder (create only) | `vd nguyen.van.a` | **`quan-tri-nguoi-dung.field.userNamePlaceholder`** | `user-form-dialog.html`. **Not** shared with `login.field.userNamePlaceholder` despite identical text — see Normalize |
| Field label / placeholder (create only) | `Mật khẩu tạm` / `vd TempPass@123` | **`…field.tempPassword`** / **`…field.tempPasswordPlaceholder`** | `user-form-dialog.html` |
| Field label / placeholder | `Email` / `ten@congty.vn` | **`shared.field.email`** / **`…field.emailPlaceholder`** | `user-form-dialog.html` |
| Field label / placeholder | `Họ tên` / `Nguyễn Văn A` | **`…field.fullName`** / **`…field.fullNamePlaceholder`** | `user-form-dialog.html` |
| Field label | `Vai trò` + `*` | **`shared.field.role`** | `user-form-dialog.html` |
| Role checkbox labels | `Admin` / `User` | — (from `ASSIGNABLE_ROLES`, untranslated identifiers) | `quan-tri-nguoi-dung.model.ts:92`. `SuperAdmin` deliberately absent |
| Dialog cancel / submit | `Huỷ` / `Lưu` | **`shared.action.cancel`** / **`quan-tri-nguoi-dung.action.save`** | `user-form-dialog.html` |
| Validation — email empty | `Email bắt buộc.` | **`quan-tri-nguoi-dung.error.emailRequired`** | `user-form-dialog.ts` |
| Validation — full name empty | `Họ tên bắt buộc.` | **`…error.fullNameRequired`** | `user-form-dialog.ts` |
| Validation — no role selected | `Chọn ít nhất 1 vai trò.` | **`…error.rolesRequired`** | `user-form-dialog.ts` |
| Validation — username empty (create) | `Tên đăng nhập bắt buộc.` | **`…error.userNameRequired`** | `user-form-dialog.ts` |
| Validation — temp password too short (create) | `Mật khẩu tạm chưa đủ dài.` — **no number in the sentence** | **`…error.tempPasswordTooShort`** | `user-form-dialog.ts`. Same rule as the change-password screen: the only number a user may be shown is the one the BE sent |
| Toast — create success | `Đã thêm người dùng.` | **`quan-tri-nguoi-dung.toast.created`** | `quan-tri-nguoi-dung.page.ts:461` |
| Dialog error — create failed (fallback) | `Không tạo được người dùng — thử lại sau.` | **`…error.createFailed`** | `quan-tri-nguoi-dung.page.ts:465`, applied at `:451` |
| Toast — update success | `Đã cập nhật người dùng.` | **`…toast.updated`** | `quan-tri-nguoi-dung.page.ts:484` |
| Dialog error — update failed (fallback) | `Không cập nhật được người dùng — thử lại sau.` | **`…error.updateFailed`** | `quan-tri-nguoi-dung.page.ts:488` |
| Dialog error — update returned "not succeeded" | `Chưa lưu được thay đổi — mở lại danh sách để kiểm tra trước khi coi là đã sửa.` | **`…error.updateNotSaved`** | `quan-tri-nguoi-dung.page.ts:478` |
| Toast — lock success | `Đã khoá tài khoản. Phiên đang đăng nhập của người dùng này sẽ bị chấm dứt trong vòng 30 phút.` | **`…toast.locked`** | `quan-tri-nguoi-dung.page.ts:523` |
| Toast — lock reported not-succeeded | `Chưa khoá được tài khoản. Kiểm tra lại trạng thái tài khoản trước khi coi là đã khoá.` | **`…toast.lockFailed`** | `quan-tri-nguoi-dung.page.ts:525` |
| Toast — unlock success / not-succeeded | `Đã mở khoá tài khoản.` / `Chưa mở khoá được tài khoản. Kiểm tra lại trạng thái tài khoản rồi thử lại.` | **`…toast.unlocked`** / **`…toast.unlockFailed`** | `quan-tri-nguoi-dung.page.ts:543,545` |
| Toast — HTTP fallbacks (offline / 401 / 403 / 404 / 429 / other) | see `Screens/05-auth.md` § Copy | **`shared.httpError.*`** | `api-error-message.service.ts:13-19,40-43` — one table, every screen |
| Toast — session ended (401 mid-session) | `Phiên đăng nhập đã kết thúc` / `Vui lòng đăng nhập lại để tiếp tục.` | **`shared.httpError.endedTitle`** / **`…endedText`** | `http-error.interceptor.ts:25-26`. Severity **warn**, and it now redirects — new 2026-09-05 |
| Toast close aria-label | `Đóng thông báo` | **`shared.toast.dismiss`** | `toast.html:16` |
| Server-side lock/role errors | e.g. `Bạn không thể tự khoá tài khoản của chính mình…` | **`USER.SELF_LOCK_FORBIDDEN`** and siblings (the `USER.*` branch of `vi.json`) | The BE sends the **code**; the FE picks the words through `ApiErrorMessageService` (`http-error.interceptor.ts:133`). The envelope's `message` is only the fallback for a code with no translation — `doc/contracts/users.md` § LẬT 2026-09-03 |

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **URL is the source of truth for the filter (new 2026-08-29).** `searchText`, `role`, `isLocked`, `page` and `pageSize` are query parameters read straight off `queryParamMap`; nothing keeps a second copy in a signal. Writes go through one `patchQueryParams()` using `queryParamsHandling: 'merge'` and `replaceUrl: true`, and a value at its default is written as `null` so it disappears from the address bar rather than sitting there as noise. The consequences are all user-visible: F5 keeps the filter, a pasted link opens the same list, and Back steps through filter changes instead of leaving the page. `replaceUrl` is what stops a dozen filter tweaks from burying the previous page in history (`quan-tri-nguoi-dung.page.ts:65-79`, `:214-221`).
  - Four things stay in signals **on purpose**, and each is a case where the URL is the wrong home: the two filter drafts (`roleDraft`/`statusDraft` — chosen but not yet applied), the string currently being typed, the loaded rows, and the dialog flags.
  - `isLocked` is a tri-state, not a boolean: absent = all, `true` = locked only, `false` = active only. It is never collapsed to a plain boolean, because that would silently turn "Tất cả trạng thái" into "only active" (`quan-tri-nguoi-dung.page.ts:149-160`, `:275-279`).
- **loading (initial and every refetch):** `loading` is set true before each request and cleared in both callbacks, and is passed down through `<app-user-grid-table>` → `<app-data-grid>` → `p-table`. `[loading]` renders PrimeNG's `.p-datatable-mask` overlay with an inline SVG spinner over the grid. The toolbar and the create button stay interactive underneath. On first load `rows` is empty **and** `loading` is true, and PrimeNG only shows the empty-message row when `isEmpty() && !loading`, so the empty text never flashes before data arrives.
- **populated (default):** one row per user on the current page. The status column shows `● Đang hoạt động` or `● Đã khoá`; the roles column shows one `.badge.outline` chip per role string in the order the API returned them, with no client-side re-casing; the date column shows a date formatted in **the language currently selected**, not always `vi-VN` (`user-grid-table.html:64`, locale passed in as an `input`). Paging is server-side, 10 per page by default with 20/50 selectable.
- **filter applied:** the `.filter-count` pill on the Lọc button shows how many conditions are active — that indicator is what keeps "the list is filtered" visible after the panel closes, which is the classic failure of this toolbar pattern. One removable chip renders per condition. Removing one chip leaves the other in place (the merge semantics of `patchQueryParams`), and both "Áp dụng" and any chip removal reset `page` to its default, because page 5 of the old result set is usually empty in the new one.
- **filter panel open / close:** `<details>` handles the toggle itself; the two behaviours it does *not* give for free are added by `Toolbar` — closing on an outside click and closing on Escape, both listened for at document level. The panel closes **before** the apply/clear event is emitted, so the parent's refetch never happens under a panel left hanging over changed data. The parent never touches the panel's open state; that belongs to the component.
- **empty (nothing matches):** `p-table` renders the caller-supplied `[emptyTemplate]` — a single `<td colspan="5" class="muted">` reading `quan-tri-nguoi-dung.grid.empty`. Without that template `DataGrid` would fall back to its own `shared.grid.empty` (`Không có dữ liệu.`), which is true but says nothing about the filter. The paginator still renders. There is no separate "no users at all" state — the same sentence covers an empty database and an over-narrow filter.
- **error (list fetch fails) — rewritten, and the old text described the opposite behaviour:** the grid is **replaced** by a `.notice.bad` banner carrying `role="alert"`, the sentence `quan-tri-nguoi-dung.error.loadFailed`, and a `Thử lại` button wired to `reload()` (`quan-tri-nguoi-dung.page.html:68-76`, `:353`). The toolbar stays above it so the user can change the filter instead of retrying the same failing query. **The table must disappear rather than merely empty**: leaving it up puts the *previous* filter's rows on screen as if they answered the filter just chosen. `loadError` holds a translation **key**, not a sentence (`quan-tri-nguoi-dung.page.ts:31,325`), so the banner follows a language switch. A 401 mid-session now redirects on its own (`http-error.interceptor.ts:100-121`); `authGuard` still covers the navigation case (`auth.guard.ts:26`).

  > 🔄 **SỬA 2026-09-06.** The previous text read *"the error callback only clears `loading` — **there is no screen-level error state**: no banner, no retry, and `rows`/`totalCount` keep their previous values, so the grid silently keeps showing stale data."* All three claims are now false. The three-shape rule this implements — loading overlay / empty row / **banner instead of the grid** — is stated in `doc/huong_dan/wiki-core/fe/11-grid-and-metadata.md` and quoted in-source at `quan-tri-nguoi-dung.page.html:60-67`.
- **row action — self-lock blocked:** on the signed-in user's own row, while that account is unlocked, the lock button is `[disabled]` and its `title` reads `Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất`. It takes the global `.icon-btn:disabled` treatment (`opacity:.45`, `cursor:not-allowed`, hover suppressed). The server rejects self-lock with `USER.SELF_LOCK_FORBIDDEN` (403), so the click is guaranteed to fail and the UI blocks it rather than making the user earn an error. Exactly one action is blocked — **unlock is never blocked**, and **locking someone else is never blocked**, including a `SuperAdmin`, because that is a server-side authorization rule the FE does not duplicate. When `currentUserId` is `null` (identity not yet known) no row is blocked (`user-grid-table.ts:57-65`).
- **lock — confirmation step (new 2026-08-29):** clicking the red lock button does **not** lock. It stores the row in `pendingLock` and opens the `ConfirmDialog`, whose description names the account and states the real delay: the session ends **within 30 minutes, not immediately**. That number is the shipped policy, not a hedge (`doc/contracts/users.md:215-226`) — telling an administrator the person is out *now* is exactly the kind of confidently wrong reassurance that gets acted on. Cancel, Escape and the × all clear `pendingLock` and do nothing else.
- **unlock — no confirmation, deliberately:** the asymmetry is the point. Lock is the direction with a consequence the clicker cannot fully predict; unlock is the recovery direction, and a confirmation step there only slows down fixing a mistake (`quan-tri-nguoi-dung.page.ts:358-370`).
- **lock/unlock result:** no optimistic update and no per-row busy state — the row is unchanged until the refetch lands, then the badge and the lock icon flip together. A success toast appears only when the envelope actually reports success; the "not succeeded" branch raises an **error** toast telling the administrator to re-check the account rather than claiming the job is done. On an HTTP error the callback does nothing, because the interceptor has already shown the server's `message`.
- **dialog open — create:** opened from the toolbar's `+ Thêm người dùng`; `editing` is `null`, so the title reads `quan-tri-nguoi-dung.dialog.createTitle`, the "Tên đăng nhập" and "Mật khẩu tạm" row renders, all fields start empty, no role is ticked, and `appAutofocus` focuses and selects the username field.
- **dialog open — edit:** opened from a row's `Sửa`; the title reads `quan-tri-nguoi-dung.dialog.editTitle`, the username and temp-password row is **not rendered** (the API's `PUT` cannot change either), Email/Họ tên are pre-filled, and role checkboxes are pre-ticked from the target's roles intersected with `ASSIGNABLE_ROLES`. Any role outside that list — today only `SuperAdmin` — has no checkbox but is re-sent unchanged in the save payload, unconditionally and regardless of the caller's own role, so editing a `SuperAdmin` from this screen never silently strips it. That also makes "no box ticked" a **valid** state in edit mode rather than an empty-selection error (`user-form-dialog.ts:136-138`, `:200`).
- **validation (client-side, dialog):** every rule is evaluated and **all** failures are reported at once — email, full name, at least one role, then username and a minimum-length temp password in create mode. The length threshold is **not quoted in the sentence** (`quan-tri-nguoi-dung.error.tempPasswordTooShort` reads `Mật khẩu tạm chưa đủ dài.`), for the same reason as the change-password screen: a hand-copied constant drifts below the server's real policy and then the client waves through a password the server refuses. Each message renders under its own field. The form does not stop at the first failure any more: now that each field has somewhere to put a message, reporting one per click would make a user with three empty fields submit three times (`user-form-dialog.ts:202-219`).
- **server validation errors (400):** the envelope's `fields` is passed through **verbatim** — not re-cased, not flattened by the page — and bound under the matching input. Two things happen on the way: FluentValidation's per-item suffixes (`Roles[0]`, `Roles[1]`) are stripped so the key matches the form's `Roles`, and several messages for one field are joined into one string, because a field has exactly one place to show one. Field keys are PascalCase on purpose, matching the C# property names, so no second mapping table has to be maintained. Each invalid input carries `.invalid` (red border), `aria-invalid` and `aria-describedby` pointing at its message — all three, since a red border alone is not a signal for everyone; and all three are `null` when valid, because `aria-describedby` pointing at a non-existent element is a real defect, not harmless noise.
- **the general error line:** the form-level `.form-error` renders **only when no field error exists**. The backend sends both `message` ("Dữ liệu không hợp lệ.") and `fields`; showing both would repeat the same information in vaguer words directly under the specific one.
- **saving (dialog submit in flight):** **no busy state exists** — neither "Lưu" nor "Huỷ" is disabled while the request is in flight, no spinner shows, and nothing stops a second click from firing a duplicate request. See § Normalize on redesign. (`ConfirmDialog` does expose a `confirmDisabled` input for exactly this, and this screen does not bind it.)
- **save success:** the dialog closes, the list refetches with the current filter and page unchanged, and a success toast appears for 5 s.
- **save error:** the dialog **stays open** and the message renders inside it — under the offending field when the envelope named one, otherwise in the form-level line. Because the interceptor also toasts, a failed save shows its message **twice**.
- **search:** typing sets the input signal immediately (the field is never laggy), then a 300 ms debounce with `distinctUntilChanged` writes the value to the URL and resets `page`. The request fires from a single `effect()` watching the resolved parameters, so a burst of keystrokes produces exactly one request carrying the string actually typed. The debounce lives in the page, not in `Toolbar` — each screen has its own threshold — and it suppresses redundant history entries as well as redundant requests. External URL changes (Back/Forward, a pasted link) pull the input box back into step; changes the page itself pushed do not, since the user may have typed more while the navigation ran (`quan-tri-nguoi-dung.page.ts:88-94`, `:170-183`).
- **access denied:** not a visual state on this screen — `adminGuard` redirects a user who is neither `Admin` nor `SuperAdmin` to `/trang-chu` before anything renders. There is no 403 page (`role.guard.ts:18-28`).

### Responsive

<!-- Behavior per breakpoint. -->

- **This screen's own SCSS declares zero media queries** — in fact the page has **no stylesheet at all** any more: `quan-tri-nguoi-dung.page.ts` carries no `styleUrl`, because everything the page needs is a global class. `user-grid-table.scss` and `user-form-dialog.scss` contain no `@media` block. Every responsive change below comes from the shell, from `styles.scss` § 6, or from fluid `min()`/`flex` widths.
- **≥981px (desktop default):** sidebar fixed at `spacing.sidebar-w` (or collapsed), `.shell-content` offset to match, `main` capped at `spacing.container-max-width` with `spacing.sp-5` padding. The topbar hamburger is `display:none`. With the sidebar collapsed, submenus become hover/focus flyouts.
- **≤`spacing.breakpoint-tablet` (980px):** `.shell-content { margin-left: 0 !important }` — the sidebar leaves the flow and becomes an off-canvas drawer over a backdrop, and the hamburger appears. The card, toolbar and grid are unchanged; they reflow into the full width.
- **≤`spacing.breakpoint-mobile` (560px):** `main` padding drops to `10px`; the topbar's user-name text is hidden, leaving only "Đăng xuất"; the sidebar drawer widens and its nav items grow for touch. The **toolbar restacks**: the search field goes to `width:100%`, every direct child stretches — **except** the Lọc button and the chip row, which are pinned `flex:none` because they are not input boxes and stretching them looks broken; `.toolbar-actions` loses its `margin-left:auto` and takes the full width; `.toolbar-sep` is hidden; the filter panel narrows to `min(320px, 86vw)`. The form dialog's `.form-grid` collapses to one column.
- **Grid (all viewports):** neither `responsiveLayout` nor `breakpoint` is set on `p-table` (`data-grid.html:7-21`), so **no column-stacking treatment is enabled** — all 5 columns render at every width. The five inline `min-width` values total 690px plus cell padding, so on narrow screens the grid scrolls horizontally inside PrimeNG's own container. Vertically the scroll region is `--grid-h` (`calc(100dvh - 280px)`), a screen-relative height rather than one of the three hardcoded pixel heights the app's tables used to each pick for themselves.
- **Dialogs (all viewports):** fluid by design, no breakpoint — `spacing.dialog-width-form` for the form, `spacing.dialog-width-confirm` for the lock confirmation. The role checkbox row is a plain flex row and does not wrap to a column on small screens.
- **Toast (all viewports):** `spacing.toast-stack-max-width`, pinned `spacing.sp-5` from the right and bottom edges at every size.
- **Print:** the sidebar, backdrop, topbar, toolbar and toast stack all carry `no-print` and are hidden by the single global `@media print` rule in `styles.scss`; a second `@media print` block there resets `.shell-content`'s `margin-left` and drops the `main` max-width, which is layout compensation the class cannot express. Per-component print blocks were removed on 2026-08-29. The screen has no print layout of its own; the grid prints as-is.

### Iconography

This screen uses a real icon library — **PrimeIcons v7**, loaded globally via `angular.json` → `styles` and rendered as `<i class="pi pi-…">`. The map below is this screen's own, read from the live templates on 2026-08-29.

Two non-PrimeIcons sources also appear. The first is **PrimeNG's own inline `<svg>` set**, injected at runtime with **no source line anywhere in `src/FE/`** — a grep for `pi-` misses it entirely, because these exist by switching a component on rather than by writing an icon. Two of them reach this screen: the four paginator arrows (from `[paginator]="true"`) and the loading spinner (from `[loading]`). PrimeNG's sort and filter icons do **not** render here — no template uses `pSortableColumn`, `[sortField]` or `[filters]`. Full enumeration, plus the missing `aria-hidden` and the English paginator labels, is in `Icons.md`. The second is the status badge's literal `●` (U+25CF), which is not an icon element at all (`Icons.md` § Legacy Exceptions).

| Action | Icon | Placement |
| --- | --- | --- |
| Search users | `pi pi-search` (`colors.muted`, 13px in a toolbar) | Absolutely positioned inside `.input-icon.search`, `pointer-events:none` (`toolbar.html`) |
| Open the filter panel | `pi pi-filter` | Inside `summary.btn`, before the `shared.toolbar.filter` label (`toolbar.html`) |
| Remove one filter condition | `pi pi-times` | Inside the `.icon-btn` at the end of each `.filter-chip` (`toolbar.html`) |
| Create user | — (text button `+ Thêm người dùng`; the `+` is a literal character **outside** the translation key) | Toolbar action group, far right (`quan-tri-nguoi-dung.page.html:55-57`) |
| Retry a failed list load | `pi pi-refresh` | Inside the `Button` in the `.notice.bad` banner (`quan-tri-nguoi-dung.page.html:73`) |
| List-load failure severity | `pi pi-exclamation-triangle` | Leading glyph of the `.notice.bad` banner (`quan-tri-nguoi-dung.page.html:70`) |
| Edit user | `pi pi-pencil` in `colors.brand` via `.icon-btn.primary` | Row actions cell, first button (`user-grid-table.html:69-76`) |
| Lock account | `pi pi-lock` in `colors.bad` via `.icon-btn.danger` | Row actions cell, second button, while the account is unlocked (`user-grid-table.html:86`) |
| Unlock account | `pi pi-lock-open` in `colors.brand` via `.icon-btn.primary` | Same button, while the account is locked (`user-grid-table.html:77-87`) |
| Account status | `●` (U+25CF literal glyph, inheriting the badge's `colors.good` / `colors.bad` ink) | Inline prefix inside the status `Badge` text (`user-grid-table.html:59,61`) |
| Lock confirmation severity | `pi pi-exclamation-triangle` on a `colors.warn-bg` disc | `.dialog-icon.warn` in the confirm dialog's head; chosen by `severity="warn"` (`confirm-dialog.ts:21-26`) |
| Close a dialog | `pi pi-times` | The `.icon-btn` in the form dialog's title row and in the confirm dialog's head (`user-form-dialog.html:10`, `confirm-dialog.html:19`) |
| Cancel / save / confirm | — (text buttons `Huỷ`, `Lưu`, `Khoá tài khoản`, `Xoá lọc`, `Áp dụng`) | `.dialog-actions` and `.filter-foot` |
| Paginate (first/prev/next/last) | PrimeNG inline `<svg>`, **not** PrimeIcons — `data-p-icon="angle-double-left"`, `"angle-left"`, `"angle-right"`, `"angle-double-right"` | Paginator below the grid; injected because `[paginator]="true"` (`user-grid-table.html:14`), no `src/FE/` source line |
| Grid loading | PrimeNG inline `<svg>` spinner, `data-p-icon="spinner"` | Centred in the `.p-datatable-mask` overlay while `[loading]` is true (`data-grid.html:9`), no `src/FE/` source line |
| Open nav drawer (shell) | `pi pi-bars` | Topbar left, ≤980px only (`topbar.html:12`) |
| Log out (shell) | `pi pi-sign-out` | Topbar right, before the "Đăng xuất" label (`topbar.html:20`) |
| Toast severity (shell) | `pi-check` / `pi-times` / `pi-exclamation-triangle` / `pi-info-circle` on a coloured disc | Leading disc of each toast item, `aria-hidden` (`toast.ts:11-16`) |
| Dismiss toast (shell) | `pi pi-times` | Right edge of each toast item (`toast.html:19`) |

### Screenshots

<!-- Refs into Assets/Screenshots/quan-tri-nguoi-dung/ -->

> ⚠️ **The one captured shot is out of date.** `user-list--desktop-1440.png` was captured 2026-08-22, before the 2026-08-29 rebuild. It shows the old filter row, the old single-colour row actions and the old `.role-tag` chips, and it predates the palette change — so it no longer matches anything in the blueprint above. **Recapturing it is the highest-value screenshot job on this screen**, and it is the only row below that is a real debt rather than an on-demand extra.

Every remaining row is an **on-demand** state/viewport variant under `doc/Design/CLAUDE.md` § Rules (one desktop shot per screen). Capture one when someone actually needs that case and flip its status then; do not treat the list as a backlog.

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`doc/Design/CLAUDE.md`](../../../CLAUDE.md) § Rules

**Common prerequisites for every row:** with both servers up, (1) sign in at `/dang-nhap` as a user holding `Admin` or `SuperAdmin`, otherwise `adminGuard` redirects to `/trang-chu`; (2) go to `/quan-tri/nguoi-dung`. Never record credentials here.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png` | **stale — recapture** (captured 2026-08-22, pre-rebuild) | Live app @ 1440×1000, full page, sidebar expanded, no filter applied. The existing file also documents the self-lock guard, since a freshly seeded database holds only the bootstrap account and that row's lock button renders disabled with its explanatory `title`. Reproduce that condition when recapturing so nothing is lost. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--populated--desktop-1440.png` | on demand | @ 1440×1000 with at least 12 seeded users, so the paginator shows more than one page and both status badges appear. The most useful second shot, since the primary one holds a single row. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--filter-panel-open--desktop-1440.png` | on demand | @ 1440×1000, click "Lọc" and capture the open panel with both selects and the `Xoá lọc` / `Áp dụng` foot. The single most changed region of the screen. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--filtered--desktop-1440.png` | on demand | @ 1440×1000, apply both conditions, close the panel, and capture with the `2` count pill on the Lọc button and both removable chips visible. Confirm the address bar shows `?role=…&isLocked=…`. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--lock-confirm--desktop-1440.png` | on demand | @ 1440×1000, click the red lock button on a user **other than** the signed-in account and capture the confirm dialog with the full 30-minute sentence readable. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--tablet-900.png` | on demand | Populated state @ 900×1000 — exercises `max-width:980px`: sidebar off-canvas, hamburger visible, card full width. Capture with the drawer **closed**. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--mobile-390.png` | on demand | Populated state @ 390×900 — exercises `max-width:560px`: the toolbar restack, full-width search, `main` padding 10px, topbar user-name hidden. Scroll the grid fully left first so the "Người dùng" column is visible. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--empty--desktop-1440.png` | on demand | @ 1440×1000, type a string matching no user (e.g. `zzzz`), wait past the 300 ms debounce and the request, then capture the `Không có người dùng nào khớp bộ lọc.` row. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--loading--desktop-1440.png` | on demand | @ 1440×1000, throttle the network in DevTools, change the page, and capture while the `.p-datatable-mask` spinner is visible. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-form-dialog--create--desktop-1440.png` | on demand | @ 1440×1000, click "+ Thêm người dùng" and capture the empty dialog with backdrop — both `.form-grid` rows visible, username field focused. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-form-dialog--edit--desktop-1440.png` | on demand | @ 1440×1000, click "Sửa" on a plain `User`/`Admin` row and capture the pre-filled dialog — note the username and temp-password row is absent. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-form-dialog--field-errors--desktop-1440.png` | on demand | @ 1440×1000, open the create dialog, clear every field and click "Lưu" — capture **all** messages at once, each under its own field with its red border. This is what replaced the old one-message-at-a-time block. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--toast-success--desktop-1440.png` | on demand | @ 1440×1000, unlock a locked account and capture within 5 s so the `Đã mở khoá tài khoản.` toast is visible bottom-right, with its green severity disc. |

<!-- The captured desktop row is recorded in UiInventory.md → Screenshot Manifest; its stale status needs flipping there too. On-demand rows stay here, next to the layout they belong to, and are added to the manifest only if one is actually captured. -->

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md. -->

- **No busy state on the dialog's save path** — "Lưu" and "Huỷ" stay enabled while a create/update request is in flight, so a double-click can fire two `POST /users` and create two accounts. The confirm dialog beside it already accepts a `confirmDisabled` input for exactly this, and this screen does not bind it; the form dialog has no equivalent at all.
- **A failed save shows its message twice** — once in the dialog and once as an interceptor toast. Pick one channel for form-scoped errors: mark form requests with `SKIP_ERROR_TOAST` and keep the inline messages, or drop the inline block and rely on the toast. The context token already exists.
- ~~**No error state for a failed list fetch**~~ — ✅ **resolved 2026-09-06**, see § Resolved item 11. The remainder of this bullet is kept for its reasoning: leaving the previous rows on screen with no indication they are stale and no retry. Add an inline error/retry row inside the grid area. `NoticeBanner` (`.notice.bad`) is the shipped component for this and is unused on this screen.
- **The lock confirmation states a 30-minute delay that nothing on screen verifies afterwards.** The dialog and the success toast both promise the session ends within 30 minutes; there is no way for the administrator to confirm it happened. That is honest about the mechanism and still leaves them holding an unverifiable promise — worth a follow-up affordance rather than more wording.
- **The temporary password renders in a `type="text"` field**, readable by anyone looking at the admin's screen. It is plausibly deliberate (the admin has to read it back to the new user), but it should be an explicit choice: either a masked field with a reveal toggle — the pattern already shipped as `.input-icon` + `.icon-btn` on the login screen — or a generate-and-copy control.
- **Five inline `style="min-width:…"` attributes on the `<th>`s** are the last hardcoded geometry on this screen. Everything else moved onto tokens in the 2026-08-29 pass; the column widths are still baked into the template where no theme can reach them, and their total (690px) is what decides when the grid starts scrolling sideways.
- **The grid has no sorting.** Nothing in the app offers it and `doc/contracts/users.md` defines no `sortBy`/`sortDir`, so this is a genuinely open product decision rather than an omission: on a list that pages server-side, client-side sorting would sort one page and look broken. If sorting is wanted it needs the contract first.
- **The role filter's option list is hardcoded in the template** — `SuperAdmin` / `Admin` / `User` written out by hand, deliberately, because a `[value]` binding is applied before an `@for` block builds its options. The workaround is sound; the cost is that a new role added on the server never appears in this filter and nothing will report it.
- **`.role-cell` and `.row-actions` are the last two screen-local classes**, and both do only spacing. Once a shared "chip row" and "action cell" utility exist, `user-grid-table.scss` collapses to the avatar and the two text lines.

### Resolved in the 2026-08-29 rebuild

<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. Same convention as the component specs. -->

1. **No confirmation before locking — resolved 2026-08-29.** A single click used to lock another user immediately. A `ConfirmDialog` now stands in front of it, and its wording states the real ≤30-minute session delay rather than implying the person is out instantly. Unlock stays one-click, deliberately.
2. **Filter state was invisible to the URL — resolved 2026-08-29.** Search, role, status, page and page size are query parameters now, so F5 keeps the filter, a shared link opens the same list, and Back steps through filter changes. Previously all five lived only in signals.
3. **One error message at a time, detached from the fields — resolved 2026-08-29.** Both the local checks and the server's `fields` now render under the offending input with `aria-invalid` and `aria-describedby`; every failure is reported in one pass. Before this, a 400 that named three fields collapsed into the single sentence "Dữ liệu không hợp lệ."
4. **Two row actions in one colour — resolved 2026-08-29.** Edit/unlock is brand-coloured and lock is red, both still ghost buttons so a long list does not fill with coloured blocks.
5. **A hand-copied toolbar that had drifted — resolved 2026-08-29.** The screen's own `.toolbar` markup had diverged from the shared design (an `×` character instead of `pi-times`, a different aria-label) and was missing two behaviours `<details>` does not provide: closing on an outside click and on Escape. `<app-toolbar>` replaced it, and the page's stylesheet became empty and was deleted.
6. **A second chip primitive in the roles column — resolved 2026-08-29.** `.role-tag` re-declared radius and padding in bare px next to a `.badge` of the same size. The column now uses `.badge.outline`, and a test pins it there.
7. **A local `.tablewrap` and a local badge pair — resolved 2026-08-29.** `user-grid-table.scss` used to redeclare the table frame verbatim and define `.badge.active` / `.badge.locked` as a second name for `.badge.ok` / `.badge.bad`. Both are gone; the file now holds only what is genuinely local to this grid.
8. **A local `.dialog-actions` with off-scale values — resolved 2026-08-29.** The form dialog carried its own copy using bare `8px`, one of three drifted `margin-top` values across the app's dialogs. It now uses the global rule.
9. **The document title never changed — resolved 2026-08-29.** `PageTitleStrategy` composes `<title>` from the route title plus the app name, so this route's tab now reads `Người dùng hệ thống · PlatformManager` instead of the static `PlatformManager` every route used to share. That was WCAG 2.4.2 (level A) failing silently, and it now has tests behind it.
10. **Every table picked its own scroll height — resolved 2026-08-29, then superseded 2026-09-06.** Three hardcoded values (480 / 520 / 560px) were replaced by `--grid-h`, computed from the viewport, so the grid neither wastes space on a large screen nor pushes its paginator below the fold on a small one.

11. **No error state for a failed list fetch — resolved 2026-09-06.** A failed load now replaces the grid with a `.notice.bad` banner carrying `role="alert"` and a `Thử lại` button (`quan-tri-nguoi-dung.page.html:68-76`). The grid is **removed**, not emptied, so the previous filter's rows cannot be mistaken for an answer to the current one.
12. **The grid frame was feature-local — resolved 2026-09-06.** `p-table`, `.tablewrap`, the paginator wiring and the zero-based→1-based page conversion moved to `shared/components/data-grid/`. `user-grid-table` now supplies only columns and cell rendering, as three `TemplateRef` inputs. See `Components/DataTable.md` § CHỐT 2026-09-06.
13. **The grid did not fill the screen — resolved 2026-09-06.** With two rows it used to be ~250px tall with a large void below. Height now comes from the `page-fill` ⇄ `grid-host` flex chain plus `scrollHeight="flex"`; `--grid-h` survives for plain `.tablewrap.scroll` tables only.
14. **Dates were formatted `vi-VN` regardless of language — resolved 2026-09-05.** `DatePipe` now takes the active `localeId`, passed in as an `input()` because a dumb component may not inject `LanguageService` (gate G4).
15. **Every string was a Vietnamese literal — resolved 2026-09-05/06.** The whole screen, including the route title, the confirm-dialog description and every toast, now resolves through translation keys. Dumb components return keys and let the template pipe resolve them.

## Cần chốt

<!-- Decisions this file must NOT make on its own. Opened 2026-09-06. -->

1. ~~**The prototype's copy of this screen still draws the pre-2026-08-29 filter chip.**~~
   **Fixed 2026-09-06.** `Prototypes/index.html` § `#screen-nguoi-dung` now renders the remove
   control the way the shipped `<app-toolbar>` does — `aria-label="Bỏ lọc Vai trò: User"` on a
   `pi pi-times` icon button, no `title`, no bare `×` — which is what decision **T2** confirmed
   as the contract. Verify:

   ```bash
   # Chỉ tính vùng màn này; hai lần còn lại trong file thuộc vùng DTI, xem ghi chú dưới
   grep -c 'Gỡ điều kiện vai trò' doc/Design/Frontend/PlatformManager/Prototypes/index.html   # PASS = 0
   ```

   > **Vì sao chỉ sửa vùng màn này.** Cùng chuỗi đó còn 2 lần nữa trong file, ở vùng
   > `#screen-dti`. Vùng đó **không** được vá lẻ: toàn bộ hai màn DTI trong prototype gốc là
   > bản trước đợt thiết kế lại 2026-09-04→06, nên chúng bị thay **cả khối** chứ không phải
   > sửa từng chi tiết. Thiết kế DTI đã duyệt nằm ở `Screens/01-dashboard.md`,
   > `Screens/02-danh-muc-dti.md` và bản dựng để duyệt ở
   > `doc/Design/Frontend/PlatformManager/Prototypes/index.html` (trỏ lại 2026-09-10 — trước đó
   > câu này trỏ một bản nằm NGOÀI repo). Vá chip trong một màn đã bị thay là làm cho
   > nó **trông như** đã cập nhật.

   *(🔄 SỬA 2026-09-06, hai lần. Lần đầu: khối lệnh này viết sai tên thư mục ở dạng số ít —
   thư mục thật là `Prototypes/`; dạng số ít trỏ vào khu prototype cấp trên đã xoá 2026-08-23
   và không được trích dẫn lại (`.claude/CLAUDE.md` §7). Lệnh sai tên trả "No such file" thay
   vì một con số, nên phép kiểm này **chưa từng chạy** suốt thời gian đó. Lần hai: tiêu chí
   `grep -c 'Gỡ điều kiện' … # PASS = 0` là **sai** kể từ khi chỉ vùng màn này được sửa —
   chạy thật ra `2`, vì vùng `#screen-dti` cố ý giữ nguyên. Đã thu hẹp thành
   `'Gỡ điều kiện vai trò'` ở khối lệnh phía trên, và con số đó **đã chạy thật, ra `0`**.)*

   Why it matters rather than being cosmetic: this prototype is the reference a
   generator or a new developer reads, so the stale region would reintroduce exactly
   the markup § Resolved item 5 removed from the code — and `<app-toolbar>` cannot
   produce it, so the result would not compile into the shared component at all.

   **Not fixed here on purpose.** `Prototypes/index.html` is held by the coordinator, and this
   spec may not edit it (brief constraint, 2026-09-06). Raised so the next re-sync pass
   covers the Core screens and not only the DTI ones.
