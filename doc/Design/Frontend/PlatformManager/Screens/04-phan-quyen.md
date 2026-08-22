---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
flow: "Permissions"
screens: ["Phân quyền"]
source_routes: ["/quan-tri/phan-quyen"]
---

# Permissions (Phân quyền) — Screens

One lazy-loaded route (`/quan-tri/phan-quyen`) gated by `authGuard` + `superAdminGuard` — **only** a `SuperAdmin` reaches it, an `Admin` is redirected to `/dashboard` (`phan-quyen.routes.ts:5-12`, `super-admin.guard.ts:10-18`). The page hosts **two independent role × row checkbox matrices** behind two tab buttons, each with its own signal state, its own API pair and its own save button (`phan-quyen.page.ts:35-47`). The two matrices look alike and behave differently on purpose — conflating them is the failure mode this spec exists to prevent, so the difference is documented as a shipped, deliberate fact in Layout Blueprint / Copy / States, not as a redesign item. **This screen has no prototype**: `doc/Prototype/` contains no file for it, it was built directly in Angular, and the Angular app is its only source (see `../../../CLAUDE.md` § Fidelity Policy — the greenfield carve-out expired 2026-08-22). The API contract behind both matrices is `doc/contracts/permissions.md` (PERM-1 = `SysMenuRole`, PERM-2 = `RolePermission`).

> **Shell:** app shell — `app-sidebar` + `app-topbar` + `<router-outlet>` + `app-toast` (`src/FE/src/app/app.html:1-14`). This **is** the shell described in `DESIGN.md` → Layout, which was brought up to the Angular app in the 2026-08-22 token refresh: it documents both shells (`DESIGN.md:418-422`) — the main one, `Sidebar` at `--sidebar-w` 220px / `--sidebar-w-collapsed` 60px with `.shell-content` offset to match, a sticky `Topbar` and a `main` capped at `--container-max-width`, which is the one this route renders — alongside the `noShell` auth shell, and all three breakpoints (980 / 560 / 981px).
> **Sources:** `src/FE/src/app/platform/phan-quyen/` (`pages/phan-quyen/*`, `components/permission-matrix/*`, `components/resource-permission-matrix/*`, `services/*`, `models/phan-quyen.model.ts`, `phan-quyen.routes.ts`), `src/FE/src/styles.scss`, `src/FE/src/app/app.html`, `src/FE/src/app/app.scss`, `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`, `src/FE/src/app/core/interceptors/http-error.interceptor.ts`, `doc/contracts/permissions.md`

---

## Phân quyền (`/quan-tri/phan-quyen`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:1-14`) — surrounds every routed screen, not part of this route's own template
  - `app-sidebar` — fixed left, width `--sidebar-w` (`--sidebar-w-collapsed` when collapsed), `z-index:35`; this screen's entry is the `SysMenu` row `Phân quyền` under group `Quản trị hệ thống` (`sidebar.scss:3-20`, `CoreSeeder.cs:126` for the row and `:124` for its parent group)
  - `.shell-content` — `margin-left:var(--sidebar-w)` (`app.scss:11-22`)
    - `app-topbar` — sticky, `z-index:20`, inner `.topin` max-width `--container-max-width`, padding `--sp-4` `--sp-5`; `<h1>` prints the route's `data.title` (`topbar.html:13`, `app.ts:44`, `phan-quyen.routes.ts:9`)
    - `main` — max-width `--container-max-width`, padding `--sp-5`, holds `<router-outlet>` = everything below (`app.scss:24-30`)
  - `app-toast` — fixed bottom-right, offsets `--sp-5`, `z-index:60`, `aria-live="polite"` (`toast.html:1`, `toast.scss:1-10`)
- **`TabBar`** (`.tabs-bar.no-print`, `Components/TabBar.md`; flex, gap `--sp-3`, margin-bottom `--sp-4` — `phan-quyen.page.html:1-8`, `phan-quyen.page.scss:6-10`)
  - `Button` — "Phân quyền màn hình"; carries `.primary` only while it is the active tab (`[class.primary]="activeTab() === 'menu'"`)
  - `Button` — "Quyền theo tài nguyên"; same `.primary`-when-active binding
  - **Not** a tab widget: no `role="tablist"`/`role="tab"`/`aria-selected` is set; these are two ordinary `Button`s whose only "selected" cue is the primary variant. It is also **not** `SegmentedControl` — that spec documents the separate `.segmented`/`.seg-btn` switcher on `/dashboard`; both ship, deliberately
- **Exactly one `Card` is in the DOM at a time** — the inactive tab's card is removed by `@if`, not hidden (`phan-quyen.page.html:10,32`)

- **Tab A — `Card` "Phân quyền màn hình"** (PERM-1, menu visibility — `phan-quyen.page.html:11-29`)
  - `.title` row (flex, space-between, gap `--sp-3`, margin-bottom `--sp-4` — `styles.scss:272-283`)
    - `<h2>` "Phân quyền màn hình"
    - `Button` (primary) — "Lưu thay đổi" / "Đang lưu…", `[disabled]="saving() || loading()"`
  - `<p class="muted">` helper text, `display:block`, margin-bottom `--sp-4` (`phan-quyen.page.scss:1-4`)
  - `app-permission-matrix` → `Table` (`Components/Table.md`, variant **Menu permission matrix**) — hand-rolled `<table>`, deliberately not PrimeNG (see note below)
    - `.tablewrap` — border 1px `--border-strong`, radius `--radius-table`, `overflow:auto`, `max-height:560px` (raw literal, no token — `permission-matrix.scss:1-6`)
    - `<table>` — global treatment: `width:100%`, `border-collapse:collapse`, background `--card`; **no `min-width`** (`styles.scss:363-367`)
    - `<thead>` — `th` "Màn hình" with inline `style="width:40%"`, then one `th.num` per role from `roles()`; every `th` is `position:sticky;top:0`, `z-index:4`, background `--surface-table-header`, color `--text-table-header`, font-size `--fs-xs` (`styles.scss:378-388`)
    - `<tbody>` — one `<tr>` per row of `displayRows()`, ordered **parent first, its children immediately after** by `toDisplayOrder()` (`permission-matrix.ts:9-26`)
      - name cell: `<td>`, or `<td class="indent">` (`padding-left:28px`, raw literal) prefixed by a `└` glyph in `.tree-branch` (color `--muted`, `margin-right:4px`) for any non-root row (`permission-matrix.scss:8-15`)
      - one `td.num` per role (`text-align:right`, `font-variant-numeric:tabular-nums` — `styles.scss:398-401`) containing a native `<input type="checkbox">`, 16×16 raw px, `accent-color:var(--brand)`, `[disabled]="loading()"`, `aria-label` = `"<menu name> — <role>"`
      - `@empty` → one `<tr>` with a `td.muted` spanning `roles().length + 1`
    - Row chrome comes from the global table rules: 1px `--line` bottom border, zebra `tbody tr:nth-child(even)` background `--surface-table-header`, `tbody tr:hover` background `--bg`, `vertical-align:top` (`styles.scss:369-396`)

- **Tab B — `Card` "Quyền theo tài nguyên"** (PERM-2, action permissions — `phan-quyen.page.html:33-52`)
  - `.title` row: `<h2>` "Quyền theo tài nguyên" + `Button` (primary) "Lưu thay đổi" / "Đang lưu…", `[disabled]="resourceSaving() || resourceLoading()"`
  - `<p class="muted">` helper text — explicitly contrasts itself with tab A (deny-by-default vs. open-to-all)
  - `app-resource-permission-matrix` → `Table` (`Components/Table.md`, variant **Resource permission matrix**) — the second hand-rolled `<table>`
    - `.tablewrap` — identical treatment to tab A (`resource-permission-matrix.scss:1-6`)
    - `<thead>` — `th` "Tài nguyên" with inline `style="width:40%"`, then one `th.num` per role; the `SuperAdmin` header cell additionally renders `<span class="always-tag muted">` on its own line (`display:block`, `margin-top:2px`, `font-weight:400`, `white-space:nowrap` — `resource-permission-matrix.scss:27-34`)
    - `<tbody>` — one `<tr>` per row of `rows()`, a **flat list** in the order the API returned it; no tree, no indent, no `└` glyph (`resource-permission-matrix.html:17-31`)
      - name cell: plain `<td>` with the resource display name
      - one `td.num` per role containing a native `<input type="checkbox">`, same 16×16 / `accent-color:var(--brand)` treatment; `aria-label` = `"<resource name> — <role>"`, extended to `"… — luôn có quyền, không thay đổi được"` on the `SuperAdmin` column (`resource-permission-matrix.ts:63-66`)
      - `@empty` → one `<tr>` with a `td.muted` spanning `roles().length + 1`
    - `<p class="muted always-note">` under the table — rendered **only** when `SuperAdmin` is present in `roles()`; `margin-top:--sp-3`, `line-height:1.5` (`resource-permission-matrix.html:41-48`, `resource-permission-matrix.scss:37-40`)

#### The two matrices are deliberately different

| | **PERM-1** — menu visibility | **PERM-2** — action permissions |
| --- | --- | --- |
| Angular component | `app-permission-matrix` | `app-resource-permission-matrix` |
| Card / tab | "Phân quyền màn hình" | "Quyền theo tài nguyên" |
| Row shape | `SysMenu` tree — parent row then its children, one indent level (`toDisplayOrder()`, `permission-matrix.ts:9-26`) | flat resource keys, API order preserved (`resource-permission-matrix.html:17`) |
| Row identity | `SysMenuId` (GUID) | `ResourceKey` (`criteria.manage`, `criteria-groups.manage`, `import.manage` — `ResourceKeys.cs:12-14`) |
| `SuperAdmin` column | **normal, clickable** — checked state comes from `AssignedRoles` only | **checked + `disabled`** for every row, whether or not the API assigned it (`resource-permission-matrix.ts:53-61`) |
| Explanatory note under the table | none | `.always-note` paragraph + `luôn có toàn quyền` tag in the column header |
| API pair | `GET`/`PUT /admin/permissions` | `GET`/`PUT /admin/permissions/resources` |
| Backend semantics of "no role ticked" | menu is **open to every signed-in user** | action is **denied to everyone** (deny-by-default) |

**Why PERM-2's `SuperAdmin` column is disabled — as shipped, on purpose.** `RequirePermissionFilter` bypasses `[RequirePermission]` for `Roles.SuperAdmin` (break-glass, `doc/contracts/permissions.md:122-133`). Un-ticking that column and saving would return success while revoking nothing, so the administrator would believe a permission was withdrawn when it was not. Locking the cell **tells the truth about the permission state**; it is not a security boundary (the FE never is) and the `PUT` payload still ships the API's own `AssignedRoles` untouched, so the contract is unchanged (`resource-permission-matrix.ts:17-26`). PERM-1 writes `SysMenuRole`, which has no such bypass, so un-ticking `SuperAdmin` there **does** take effect and the cell stays clickable. Both directions are locked by unit tests that fail if either is copied onto the other: `permission-matrix.spec.ts:41-52` asserts the PERM-1 cell is *not* disabled, `resource-permission-matrix.spec.ts:38-47` asserts every PERM-2 `SuperAdmin` cell *is* checked and disabled.

<!-- Component gap — CLOSED 2026-08-22 by the COMPONENTS.md re-documentation pass (12 -> 27 specs).
     The previous note here said only `Card` and `Button` existed, that `Table` documented the
     prototype's fixed 9-column DTI table, and that the checkbox cell / tab bar / shell had no
     entry. All four claims are now stale. As shipped, every region of this screen composes from
     an indexed spec:
       Card -> Components/Card.md              Button   -> Components/Button.md
       .tabs-bar -> Components/TabBar.md       Sidebar  -> Components/Sidebar.md
       Topbar -> Components/Topbar.md          Toast    -> Components/Toast.md
       both matrices -> Components/Table.md (variants "Menu permission matrix" and
         "Resource permission matrix"); Table.md was rewritten against src/FE/ and no longer
         describes the prototype table
       the 16px checkbox cell -> Components/Input.md, variant "Checkbox"
     The matrix trimmings — td.indent + the `└` .tree-branch glyph, and .always-tag /
     .always-note — are covered inside Table.md's two matrix variant rows, not as specs of
     their own.
     Still genuinely undocumented on this screen: the page-local `<p class="muted">` helper
     line (phan-quyen.page.scss:1-4) — one rule, three declarations, no states, no variants.
     Recorded, not invented into a spec. -->

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

No i18n layer exists — every string below is a hardcoded Vietnamese literal in a template, a TypeScript file, or the API payload, so the localization key column reads `— (hardcoded)` throughout.

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Topbar heading (route title) | `Phân quyền` | — (hardcoded) | `phan-quyen.routes.ts:9` (rendered by `topbar.html:13`) |
| Tab button 1 | `Phân quyền màn hình` | — (hardcoded) | `phan-quyen.page.html:3` |
| Tab button 2 | `Quyền theo tài nguyên` | — (hardcoded) | `phan-quyen.page.html:6` |
| Tab A card heading | `Phân quyền màn hình` | — (hardcoded) | `phan-quyen.page.html:13` |
| Tab A save button (idle / saving) | `Lưu thay đổi` / `Đang lưu…` | — (hardcoded) | `phan-quyen.page.html:15` |
| Tab A helper text | `Tick chọn role được thấy màn hình tương ứng. Mục không tick role nào = mở cho mọi user đã đăng nhập.` | — (hardcoded) | `phan-quyen.page.html:19-20` |
| Tab A column header 1 | `Màn hình` | — (hardcoded) | `permission-matrix.html:5` |
| Tab A role column headers (dynamic) | `{{ role }}` — API-supplied; contract lists `SuperAdmin` / `Admin` / `User` | — (hardcoded, API value) | `permission-matrix.html:7`; `doc/contracts/permissions.md:16` |
| Tab A row label (dynamic) | `{{ row.SysMenuName }}` — seeded values: `Dashboard`, `Danh mục`, `DTI`, `Quản trị hệ thống`, `Người dùng`, `Phân quyền` | — (hardcoded, DB value) | `permission-matrix.html:18`; `CoreSeeder.cs:121-126` |
| Tab A child-row glyph | `└` | — (hardcoded) | `permission-matrix.html:16` |
| Tab A checkbox accessible name (dynamic) | `<SysMenuName> — <role>` | — (hardcoded template) | `permission-matrix.html:27` |
| Tab A empty/loading row | `Chưa có mục menu nào.` | — (hardcoded) | `permission-matrix.html:34` |
| Tab B card heading | `Quyền theo tài nguyên` | — (hardcoded) | `phan-quyen.page.html:35` |
| Tab B save button (idle / saving) | `Lưu thay đổi` / `Đang lưu…` | — (hardcoded) | `phan-quyen.page.html:37` |
| Tab B helper text | `Tick chọn role được phép thực hiện hành động ứng với tài nguyên. Khác với "Phân quyền màn hình" ở trên: tài nguyên chưa gán role nào sẽ bị TỪ CHỐI hoàn toàn (deny mặc định), không phải mở cho mọi người.` | — (hardcoded) | `phan-quyen.page.html:41-43` |
| Tab B column header 1 | `Tài nguyên` | — (hardcoded) | `resource-permission-matrix.html:5` |
| Tab B role column headers (dynamic) | `{{ role }}` — same API-supplied list as tab A | — (hardcoded, API value) | `resource-permission-matrix.html:8` |
| Tab B `SuperAdmin` header tag | `luôn có toàn quyền` | — (hardcoded) | `resource-permission-matrix.html:10` |
| Tab B row label (dynamic) | `{{ row.ResourceName }}` — the three shipped values: `Quản lý chỉ tiêu`, `Quản lý nhóm chỉ tiêu`, `Import CSV/Excel` | — (hardcoded, API value) | `resource-permission-matrix.html:19`; `ResourceKeys.cs:21-26` |
| Tab B checkbox accessible name (dynamic) | `<ResourceName> — <role>` | — (hardcoded template) | `resource-permission-matrix.html:27`, `resource-permission-matrix.ts:64` |
| Tab B `SuperAdmin` checkbox accessible name (dynamic) | `<ResourceName> — <role> — luôn có quyền, không thay đổi được` | — (hardcoded template) | `resource-permission-matrix.ts:65` |
| Tab B empty/loading row | `Chưa có tài nguyên nào.` | — (hardcoded) | `resource-permission-matrix.html:34` |
| Tab B break-glass note (rendered only when `SuperAdmin` is in `roles()`; `SuperAdmin` appears twice, both in `<strong>`) | `Cột SuperAdmin luôn được tick và không sửa được ở đây: role này mặc định có mọi quyền với mọi tài nguyên, nên bỏ tick cũng không thu hồi được gì. Muốn một người không còn toàn quyền, vào "Quản trị hệ thống → Người dùng" và gỡ role SuperAdmin khỏi tài khoản của họ.` | — (hardcoded) | `resource-permission-matrix.html:43-46` (role name from `ALWAYS_ALLOWED_ROLE`, `resource-permission-matrix.ts:15`) |
| Tab A save-success toast | `Đã lưu thay đổi phân quyền.` | — (hardcoded) | `phan-quyen.page.ts:97` |
| Tab B save-success toast | `Đã lưu thay đổi quyền theo tài nguyên.` | — (hardcoded) | `phan-quyen.page.ts:129` |
| Error toast (API envelope present) | server-supplied `message` from the API envelope | — (server value) | `http-error.interceptor.ts:82` |
| Error toast fallback — no connection | `Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.` | — (hardcoded) | `http-error.interceptor.ts:21` |
| Error toast fallback — 403 | `Bạn không có quyền thực hiện thao tác này.` | — (hardcoded) | `http-error.interceptor.ts:27` |
| Error toast fallback — other | `Đã có lỗi xảy ra. Vui lòng thử lại.` | — (hardcoded) | `http-error.interceptor.ts:34` |
| Toast dismiss accessible name (shell) | `Đóng thông báo` | — (hardcoded) | `toast.html:8` |

Copy notes, as shipped: both save buttons use a real ellipsis character (`…`, U+2026) rather than three dots; the `.always-note` sentence quotes the navigation path `"Quản trị hệ thống → Người dùng"` and that wording matches the seeded `SysMenu` labels exactly (`CoreSeeder.cs:124-125` — the group `Quản trị hệ thống` and its child `Người dùng`); tab B's helper text shouts `TỪ CHỐI` in caps, tab A's does not — the asymmetry is intentional emphasis on deny-by-default. No typos were found in this screen's strings.

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **loading (first paint, both tabs at once):** `loading` and `resourceLoading` both start `true` and **both** `GET`s fire in the constructor — tab B is fetched even though tab A is showing, a decision recorded in-code as deliberate rather than an oversight (`phan-quyen.page.ts:38,45,50-70`). While loading, `rows()` **and** `roles()` are still `[]`, so the matrix renders its `@empty` branch: a `<thead>` with only the `Màn hình` / `Tài nguyên` column and one body row reading `Chưa có mục menu nào.` / `Chưa có tài nguyên nào.` at `colspan=1`. **There is no spinner, skeleton or progress text anywhere on this screen** — the loading state is visually identical to the empty state, distinguishable only by the save button, which is `[disabled]` while `loading()` is true (`phan-quyen.page.html:14,36`). Because both matrices load up front, switching tabs later never produces a loading state.
- **populated — tab A (PERM-1):** rows render parent-then-child. A root row is a plain `<td>`; every non-root row gets `td.indent` plus the `└` glyph. `Indent` is a boolean, not a depth counter, so a grandchild would render at the same single indent as a child — the seeded tree is one level deep so this is not visible today (`permission-matrix.ts:18-24`). Checkboxes reflect `AssignedRoles` per row; the `SuperAdmin` column is an ordinary clickable column. A ticked box means "this role can see this menu"; **a row with no box ticked is open to every signed-in user**, which is what the helper text states.
- **populated — tab B (PERM-2):** rows render flat, in API order. Checkboxes reflect `AssignedRoles` except in the `SuperAdmin` column; **a row with no box ticked is denied to everyone**, the inverse of tab A.
- **disabled cell — PERM-2 `SuperAdmin` column (the deliberate one):** `isChecked()` returns `true` for that column unconditionally and `isDisabled()` returns `true` for it unconditionally, so every cell renders ticked and locked even for rows where the API sent an `AssignedRoles` list that omits `SuperAdmin` — a legitimate payload, since a `SuperAdmin` needs no `RolePermission` row at all (`resource-permission-matrix.ts:53-61`, `doc/contracts/permissions.md:128`). Visual treatment: `opacity:.6` and `cursor:not-allowed` (`resource-permission-matrix.scss:21-24`) — still legibly ticked, but not inviting a click. The column header carries `luôn có toàn quyền` and the `.always-note` paragraph explains the consequence and points at the real remedy (remove the role in "Quản trị hệ thống → Người dùng"). If the API ever returns a `roles` list without `SuperAdmin`, **nothing is locked and the note is not rendered** — both are driven by `roles()`, not assumed (`resource-permission-matrix.ts:45-47`, test at `resource-permission-matrix.spec.ts:70-76`). The equivalent column in tab A carries none of this: no lock, no tag, no note (`permission-matrix.spec.ts:54-57`).
- **dirty (unsaved edits):** ticking a box only mutates local signal state and sets `dirty()` / `resourceDirty()`; nothing is sent until the save button is pressed (`phan-quyen.page.ts:77-89,109-121`). **Neither flag is read by any template** — there is no unsaved-changes badge, no button-enabled-only-when-dirty behaviour, and no navigation guard, so switching tabs, navigating away or closing the browser discards edits silently.
- **saving:** the pressed tab's save button switches its label to `Đang lưu…` and goes `[disabled]`; the matrix receives `[loading]="loading() || saving()"`, which disables **every** checkbox in that matrix for the duration (`phan-quyen.page.html:14-27,36-50`; PERM-2 additionally keeps its `SuperAdmin` column locked via `isDisabled()`). The other tab's state is untouched. The `PUT` always sends the **complete** row set, not just the edited rows, as the contract's overwrite semantics require (`phan-quyen.service.ts:32-37,49-54`, `doc/contracts/permissions.md:51-55`).
- **save success:** `saving` clears, `dirty` clears, and a success toast appears bottom-right for 5 s (`toast.service.ts:11,48`). Tab A additionally calls `menu.refresh()` so the sidebar reflects the new menu visibility without a page reload; a failure of that refresh is swallowed on purpose so a successful save is never reported as a failure (`phan-quyen.page.ts:98-103`). Tab B does not touch the menu.
- **error (either `GET` or either `PUT`):** the only feedback is the toast raised by `httpErrorInterceptor` — server `message` when the API envelope is present, otherwise a status-based fallback (`http-error.interceptor.ts:73-84`). On this screen the handlers do nothing beyond clearing their own loading/saving flag (`phan-quyen.page.ts:60,69,105,131`): a failed `GET` leaves the matrix in the empty-looking state described above with **no error text inside the card and no retry control**; a failed `PUT` leaves the local edits on screen with the dirty flag still set, and the user's only route forward is to press save again. A 401 mid-session is handled globally — the interceptor clears the user context and redirects to `/dang-nhap` with a `returnUrl` (`http-error.interceptor.ts:52-58`).
- **access denied:** a signed-in non-`SuperAdmin` who navigates here never sees the screen — `superAdminGuard` returns a `UrlTree` to `/dashboard` (`super-admin.guard.ts:14-18`). There is **no** 403 page, no explanatory message and no toast on that path; the redirect is silent.
- **validation:** none exists on this screen. Every input is a checkbox with two legal values, so there is no inline validation state, no error styling and no field-level message anywhere in either matrix.

### Responsive

<!-- Behavior per breakpoint. -->

- **No `@media` query exists in any of this screen's own SCSS** — `phan-quyen.page.scss`, `permission-matrix.scss` and `resource-permission-matrix.scss` contain zero media queries. Every breakpoint effect below is inherited from the shell (`app.scss`, `topbar.scss`, `sidebar.scss`) or from global rules in `styles.scss`.
- **≥981px (desktop default):** sidebar fixed at `--sidebar-w`, content offset by the same via `.shell-content{margin-left}`; `main` is centred at `--container-max-width` with `--sp-5` padding (`app.scss:11-30`). Collapsing the sidebar narrows the offset to `--sidebar-w-collapsed` with a 0.2 s transition — the matrix simply reflows wider, it has no layout of its own tied to that.
- **≤`breakpoint-tablet` (980px, `app.scss:32-36`):** `.shell-content` margin-left is forced to 0 and the sidebar becomes an off-canvas drawer opened by the topbar hamburger, which is `display:none` above this width (`sidebar.scss:236-276`, `topbar.scss:49-56`). The full viewport width goes to `main`, so the matrix gets *wider* here, not narrower.
- **≤`breakpoint-mobile` (560px, `app.scss:38-42`):** `main` padding drops to `10px` (raw literal, no token) and the topbar hides the user's name (`topbar.scss:39-43`). Nothing inside the card changes.
- **Wide role × row matrix on a narrow viewport — what the code actually does:** the table is `width:100%` with **no `min-width`** (`styles.scss:363-367`), so it is *not* pinned to a fixed width the way the prototype's criteria table was. Columns therefore compress with the container: the name column is held at an inline `width:40%` and the role columns share the rest. Horizontal scrolling is available — `.tablewrap{overflow:auto}` — but it is **content-driven, not width-driven**: it engages only once the table's intrinsic min-content width (longest untruncated menu/resource name plus one column per role, since nothing sets `text-overflow`/`white-space` on body cells) exceeds the container. With the three shipped roles and the seeded labels there is normally no horizontal scroll even at 390px; the more roles the API returns, the sooner it starts. There is **no** column collapse, no card-per-row fallback and no per-viewport column hiding anywhere in this screen.
- **Vertical scrolling (all viewports):** `.tablewrap{max-height:560px}` (raw literal, both matrices) makes the body scroll vertically once the rows exceed that height, while `th{position:sticky;top:0;z-index:4}` keeps the role headers pinned inside that scroll container (`styles.scss:378-388`). The `.always-note` paragraph sits **outside** `.tablewrap`, so it never scrolls away.
- **Print (`@media print`):** the tab bar is `.no-print` so both tab buttons disappear (`phan-quyen.page.html:1`, `styles.scss:115-119`), as do the sidebar, topbar and toast stack (`sidebar.scss:338-343`, `topbar.scss:58-62`, `toast.html:1`); `.shell-content` margin is zeroed and `main` loses its max-width (`app.scss:44-52`). The card and the active matrix do print — but `.tablewrap` keeps `overflow:auto` **and** `max-height:560px` in print, so any row past that height is clipped from the printout, and the printed page carries no indication of which tab produced it beyond its `<h2>`.

### Iconography

See `Icons.md` § Per-Action Map. **This screen's own template contains no icon at all** — every control is a text `Button` or a native checkbox, and the only glyph is the literal `└` text character marking a child row (`permission-matrix.html:16`), which is plain text in a `<span>`, not an icon element.

Icons visible while this screen is open all belong to the app shell, which loads **PrimeIcons v7** globally (`angular.json:39`). `Icons.md` was refreshed on 2026-08-22 and now covers every one of them by name in its § Per-Action Map — the prototype-era `library: "none"` declaration, and the gap this spec used to record because of it, are both gone.

**No PrimeNG inline-SVG icon reaches this screen.** That is worth stating explicitly, because the app's *other* three grids get a second, runtime-injected icon set from PrimeNG (paginator arrows and a loading spinner) that appears nowhere in `src/FE/`. This screen gets none of it: there is **no `p-table` anywhere under `platform/phan-quyen/`** — both matrices are hand-rolled `<table>` elements (`permission-matrix.html:2`, `resource-permission-matrix.html:2`) — so there is no paginator and no PrimeNG loading mask. The `[loading]` inputs at `phan-quyen.page.html:26,49` are the app's own signal inputs on those two components (`permission-matrix.ts:43`, `resource-permission-matrix.ts:37`); they render no spinner and no overlay, and their only visible effect is `[disabled]` on every checkbox (`permission-matrix.html:25`, `resource-permission-matrix.ts:60`) — see § States for how that reads to the user.

| Action | Icon | Placement |
| --- | --- | --- |
| Switch to "Phân quyền màn hình" / "Quyền theo tài nguyên" | — (text `Button`, no icon) | Tab bar above the card |
| Save either matrix | — (text `Button`, no icon) | `.title` row, right-aligned inside the card |
| Grant/revoke a role on a row | — (native `<input type="checkbox">`, `accent-color:var(--brand)`) | Every `td.num`, right-aligned |
| Sidebar entry for this screen | `pi pi-shield` (from `SysMenu.Icon`) | Shell sidebar, under the `pi pi-cog` group "Quản trị hệ thống". Both are seeded values, not FE constants: the icon-bearing menu rows are `CoreSeeder.cs:121-126`, with `pi-shield` on the "Phân quyền" row at `:126` and `pi-cog` on its parent group at `:124` — so either can change in the database without an FE deploy (`Icons.md` § Per-Action Map) |
| Open the navigation drawer (≤980px) | `pi pi-bars` | Shell topbar, left (`topbar.html:11`) |
| Sign out | `pi pi-sign-out` | Shell topbar, right (`topbar.html:19`) |
| Dismiss a toast | `pi pi-times` | Shell toast item, right (`toast.html:11`) |

### Screenshots

<!-- Refs into Assets/Screenshots/phan-quyen/ -->

**✅ The desktop shot exists — captured 2026-08-22 from the live Angular app**, as `permission-matrix--desktop-1440.png` (the PERM-1 tab). That is the full target under `doc/Design/CLAUDE.md` § Rules (ONE desktop shot per screen, decided 2026-08-22). Every remaining row below is an **on-demand** state/tab variant, *not* an outstanding debt: capture one when someone actually needs that case, and flip its status then. All of them need both servers running and a signed-in `SuperAdmin`, because the route is guarded and both matrices are API-driven.

Common prerequisites for all rows:

1. Start the API: `dotnet run --project src/BE/PlatformManager.Api` — the FE dev config expects it on `http://localhost:5027/api` (`src/FE/src/environments/environment.development.ts`).
2. Start the app: `npm start` in `src/FE/` → `http://localhost:4200` (Angular CLI default; no port override in `angular.json`).
3. Sign in at `http://localhost:4200/dang-nhap` with a **SuperAdmin** account — in Development `CoreSeeder` seeds one; credentials are deliberately not recorded in this spec. An `Admin` account will be redirected to `/dashboard` and cannot capture this screen.
4. Navigate to `http://localhost:4200/quan-tri/phan-quyen`.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png` | captured 2026-08-22 | Live app, `/quan-tri/phan-quyen`, full page, tab "Phân quyền màn hình" (PERM-1, the default on load). Shows the PERM-1 tree with parent/child indent and the **enabled/clickable** `SuperAdmin` column — contrast with PERM-2, where that column is checked-and-disabled. Environment as recorded in `UiInventory.md` § Screenshot Manifest (API on `:5027`, FE served with `npx ng serve --port 4201`). |
| `Assets/Screenshots/phan-quyen/resource-matrix--desktop-1440.png` | on demand | Same session @ 1440×1000, click tab "Quyền theo tài nguyên". Must include the `luôn có toàn quyền` header tag, the greyed `SuperAdmin` column and the `.always-note` paragraph below the table. The one on-demand row that shows a genuinely different layout rather than a state of the captured one. |
| `Assets/Screenshots/phan-quyen/resource-matrix--saving--desktop-1440.png` | on demand | Same as above; in DevTools set network throttling to "Slow 3G" (or add a request-blocking delay on `PUT /api/admin/permissions/resources`), toggle any non-`SuperAdmin` checkbox, click "Lưu thay đổi" and capture while the button reads `Đang lưu…` and every checkbox is disabled. |
| `Assets/Screenshots/phan-quyen/permission-matrix--saved-toast--desktop-1440.png` | on demand | Tab A, toggle any checkbox, click "Lưu thay đổi", capture within 5 s of success so the `Đã lưu thay đổi phân quyền.` toast is still in the bottom-right stack (auto-dismiss is 5 s, `toast.service.ts:11`). |
| `Assets/Screenshots/phan-quyen/permission-matrix--loading-empty--desktop-1440.png` | on demand | In DevTools, block `GET /api/admin/permissions` (Network → Block request URL), then reload the route. Captures the shared loading/empty rendering: single-column header + `Chưa có mục menu nào.` + disabled save button, plus the interceptor's error toast. |
| `Assets/Screenshots/phan-quyen/permission-matrix--tablet-900.png` | on demand | Same as row 1 @ 900×1200 — below `breakpoint-tablet`, so the sidebar is an off-canvas drawer and the topbar shows the `pi pi-bars` hamburger. |
| `Assets/Screenshots/phan-quyen/resource-matrix--mobile-390.png` | on demand | Same as row 2 @ 390×900 — below `breakpoint-mobile`; verify whether the table compresses or starts scrolling horizontally with the role list the API actually returns, and record which occurred in the capture note. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

The `SuperAdmin` column difference between the two matrices is **not** listed here — it is shipped, deliberate and test-locked in both directions (see Layout Blueprint). The items below are genuine quirks.

1. **Loading is indistinguishable from empty and from a failed load.** All three render the same single-column table plus `Chưa có mục menu nào.` / `Chưa có tài nguyên nào.`; only a disabled save button hints that a request is in flight, and a `GET` failure leaves no error text or retry control inside the card — the only signal is a toast that auto-dismisses after 5 s (`phan-quyen.page.ts:54-70`). Give the matrix a real loading state, a distinct empty state and an in-card error state with a retry action.
2. **`dirty()` / `resourceDirty()` are tracked but never read by any template** (`phan-quyen.page.ts:40,47,88,120`) — no unsaved-changes indicator, no dirty-gated save button, no `CanDeactivate` guard. Switching tabs or navigating away silently discards edits on a screen whose save is a full overwrite. Either wire the flags to the UI or remove them.
3. **A failed save leaves the user with no path but to press the same button again** — the edits stay on screen and dirty, but there is no "retry / discard / reload from server" affordance and no way to see which rows differ from the server (`phan-quyen.page.ts:105,131`).
4. **No confirmation before a full-overwrite save.** Both `PUT`s replace the entire matrix, and un-ticking every role on a PERM-1 row silently *widens* access (the menu becomes visible to all signed-in users) while the same gesture on a PERM-2 row *removes* access from everyone. Two opposite consequences, one identical gesture, zero confirmation — a confirm step summarising the delta would be proportionate here.
5. **The tab bar is not an accessible tab pattern** — two plain buttons with no `role="tablist"`/`role="tab"`/`aria-selected`/`aria-controls`, so the only "which tab am I on" cue for assistive tech is the visual `Button` primary variant (`phan-quyen.page.html:1-8`). Also, the active tab is not reflected in the URL, so a tab-B view cannot be linked or restored by reload.
6. **The indent flag is a boolean, not a depth.** `toDisplayOrder()` marks every descendant with the same `Indent: true` (`permission-matrix.ts:18-24`); today's seeded menu is one level deep, but a grandchild menu would render visually identical to a child, misrepresenting the tree the matrix is supposed to show.
7. **Untokenized literals inside both matrices** — `max-height:560px`, checkbox `16px`/`16px`, `td.indent{padding-left:28px}`, `.tree-branch{margin-right:4px}`, `.always-tag{margin-top:2px}`, `outline:2px`/`outline-offset:2px`, `opacity:.6`, and the inline `style="width:40%"` on both first columns. None resolve to a token in `Tokens/`; they should be tokenized (or the existing scale extended) rather than repeated across the two component stylesheets.
8. **Printing clips the matrix.** `.tablewrap` keeps `overflow:auto` and `max-height:560px` under `@media print`, so rows past that height never reach paper, and since `.tabs-bar` is `.no-print` the printout does not say which of the two matrices it shows. Reset the wrapper's height/overflow for print and print the tab name.
