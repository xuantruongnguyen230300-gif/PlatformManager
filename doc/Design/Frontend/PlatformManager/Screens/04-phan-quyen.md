---
project: "PlatformManager"
status: "draft"
updated: "2026-08-23"
flow: "Permissions"
screens: ["Phân quyền"]
source_routes: ["/quan-tri/phan-quyen"]
---

# Permissions (Phân quyền) — Screens

One lazy-loaded route (`/quan-tri/phan-quyen`) gated by `authGuard` + `superAdminGuard` — **only** a `SuperAdmin` reaches it, an `Admin` is redirected to `/dashboard` (`phan-quyen.routes.ts:5-11`, `super-admin.guard.ts:10-18`). The page is **one card holding one role × row checkbox matrix** — no tabs, no view switcher, no second panel: the whole template is 19 lines (`phan-quyen.page.html:1-19`). That matrix is menu visibility, contract PERM-1 in `doc/contracts/permissions.md`. **This screen has no prototype**: the deleted prototype contains no file for it, it was built directly in Angular, and the Angular app is its only source (see `../../../CLAUDE.md` § Fidelity Policy — the greenfield carve-out expired 2026-08-22).

> **Not on this screen: resource/action permissions.** `doc/contracts/permissions.md` also carries PERM-2 (`RolePermission`, `GET`/`PUT /api/admin/permissions/resources`), a *second* matrix intended to live beside this one. It is marked **`Status: DRAFT`** (`doc/contracts/permissions.md:59-64`) and nothing implementing it exists in `src/FE/`. It is a plan, recorded under § Normalize on redesign — do **not** compose it into a screen, a prompt pack or a generated image.

> **Shell:** app shell — `app-sidebar` + `app-topbar` + `<router-outlet>` + `app-toast` (`src/FE/src/app/app.html:1-14`). This **is** the shell described in `DESIGN.md` → Layout, which was brought up to the Angular app in the 2026-08-22 token refresh: it documents both shells (`DESIGN.md:418-420`) — the main one, `Sidebar` at `--sidebar-w` 220px / `--sidebar-w-collapsed` 60px with `.shell-content` offset to match, a sticky `Topbar` and a `main` capped at `--container-max-width`, which is the one this route renders — alongside the `noShell` auth shell, and all three breakpoints (980 / 560 / 981px).
> **Sources:** `src/FE/src/app/platform/phan-quyen/` (`pages/phan-quyen/*`, `components/permission-matrix/*`, `services/*`, `models/phan-quyen.model.ts`, `phan-quyen.routes.ts`), `src/FE/src/styles.scss`, `src/FE/src/app/app.html`, `src/FE/src/app/app.scss`, `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`, `src/FE/src/app/core/interceptors/http-error.interceptor.ts`, `doc/contracts/permissions.md`

---

## Phân quyền (`/quan-tri/phan-quyen`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:1-14`) — surrounds every routed screen, not part of this route's own template
  - `app-sidebar` — fixed left, width `--sidebar-w` (`--sidebar-w-collapsed` when collapsed), `z-index:35`; this screen's entry is the `SysMenu` row `Phân quyền` under group `Quản trị hệ thống` (`sidebar.scss:3-20`, `CoreSeeder.cs:84` for the row and `:82` for its parent group)
  - `.shell-content` — `margin-left:var(--sidebar-w)` (`app.scss:11-22`)
    - `app-topbar` — sticky, `z-index:20`, inner `.topin` max-width `--container-max-width`, padding `--sp-4` `--sp-5`; `<h1>` prints the route's `data.title` (`topbar.html:13`, `phan-quyen.routes.ts:9`)
    - `main` — max-width `--container-max-width`, padding `--sp-5`, holds `<router-outlet>` = everything below (`app.scss:24-30`)
  - `app-toast` — fixed bottom-right, offsets `--sp-5`, `z-index:60`, `aria-live="polite"` (`toast.html:1`, `toast.scss:1-10`)
- **One `Card`, unconditionally** (`Components/Card.md`) — the route template's root element and its only region; there is no `@if`, no tab bar and no second card anywhere in it (`phan-quyen.page.html:1,19`)
  - `.title` row (flex, space-between, gap `--sp-3`, margin-bottom `--sp-4` — `styles.scss:252-263`)
    - `<h2>` "Phân quyền màn hình" (`phan-quyen.page.html:3`; `h2` is `--fs-md`, `styles.scss:259-262`)
    - `Button` (primary) — "Lưu thay đổi" / "Đang lưu…", `[disabled]="saving() || loading()"` (`phan-quyen.page.html:4-6`)
  - `<p class="muted">` helper text — `--muted` at `--fs-xs` globally (`styles.scss:265-268`), plus this page's only own rule, `display:block; margin-bottom:var(--sp-4)` (`phan-quyen.page.scss:1-4`, `phan-quyen.page.html:8-11`)
  - `app-permission-matrix` → `Table` (`Components/Table.md`, variant **Permission matrix**) — hand-rolled `<table>`, deliberately not PrimeNG (see note below). Inputs: `[rows]`, `[roles]`, `[loading]="loading() || saving()"`; output `(permissionToggle)` (`phan-quyen.page.html:13-18`, `permission-matrix.ts:41-47`)
    - `.tablewrap` — border 1px `--border-strong`, radius `--radius-table`, `overflow:auto`, `max-height:560px` (raw literal, no token — `permission-matrix.scss:1-6`)
    - `<table>` — global treatment: `width:100%`, `border-collapse:collapse`, background `#fff`; **no `min-width`** (`styles.scss:343-347`)
    - `<thead>` — `th` "Màn hình" with inline `style="width:40%"`, then one `th.num` per role from `roles()`; every `th` is `position:sticky;top:0`, `z-index:4`, background `#f8fafc`, color `#536076`, font-size `--fs-xs` (`permission-matrix.html:4-9`, `styles.scss:358-368`)
    - `<tbody>` — one `<tr>` per row of `displayRows()`, ordered **parent first, its children immediately after** by `toDisplayOrder()` (`permission-matrix.ts:9-26`, `:49`)
      - name cell: `<td>`, or `<td class="indent">` (`padding-left:28px`, raw literal) prefixed by a `└` glyph in `.tree-branch` (color `--muted`, `margin-right:4px`) for any non-root row (`permission-matrix.html:14-19`, `permission-matrix.scss:8-15`)
      - one `td.num` per role (`text-align:right`, `font-variant-numeric:tabular-nums` — `styles.scss:378-381`) containing a native `<input type="checkbox">`, 16×16 raw px, `accent-color:var(--brand)`, `[disabled]="loading()"`, `aria-label` = `"<menu name> — <role>"` (`permission-matrix.html:20-30`, `permission-matrix.scss:17-22`)
      - `@empty` → one `<tr>` with a `td.muted` spanning `roles().length + 1` (`permission-matrix.html:32-36`)
    - Row chrome comes from the global table rules: 1px `--line` bottom border, zebra `tbody tr:nth-child(even)` background `#f8fafc`, `tbody tr:hover` background `--bg`, `vertical-align:top` (`styles.scss:349-376`)

#### Why the matrix is hand-rolled

`PermissionMatrix` is a dumb standalone component with `input.required` for `rows`/`roles`, an optional `loading` input and a single `permissionToggle` output; it owns no service call and no save button, both of which belong to the smart `PhanQuyenPage` (`permission-matrix.ts:28-53`, `phan-quyen.page.ts:20-28`). It needs a full checkbox grid, a data-driven column count (`roles()` decides how many columns exist), no paging and a `max-height` scroll box — none of which PrimeNG's `p-table` was earning its weight for. `Table.md` § Do/Don't and `DataTable.md` both record the same split: this screen has **no `p-table` under `platform/phan-quyen/`** at all.

<!-- Component gap — reviewed 2026-08-23. Every region of this screen composes from an
     indexed spec:
       Card    -> Components/Card.md        Button  -> Components/Button.md
       Sidebar -> Components/Sidebar.md     Topbar  -> Components/Topbar.md
       Toast   -> Components/Toast.md
       the matrix -> Components/Table.md, variant "Permission matrix"
       the 16px checkbox cell -> Components/Input.md, variant "Checkbox"
     The matrix trimmings — td.indent plus the `└` .tree-branch glyph — are covered inside
     Table.md's matrix variant row, not as a spec of their own.
     Still genuinely undocumented on this screen: the page-local `<p class="muted">` helper
     line (phan-quyen.page.scss:1-4) — one rule, three declarations, no states, no variants.
     Recorded, not invented into a spec.
     Deleted 2026-08-23: the note here used to route a tab-bar class to a `TabBar` spec,
     and a second matrix to a "Resource permission matrix" variant of Table.md. Neither
     the class nor the component has ever existed in src/FE/; both specs are gone. -->

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

No i18n layer exists — every string below is a hardcoded Vietnamese literal in a template, a TypeScript file, or the API payload, so the localization key column reads `— (hardcoded)` throughout.

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Topbar heading (route title) | `Phân quyền` | — (hardcoded) | `phan-quyen.routes.ts:9` (rendered by `topbar.html:13`) |
| Card heading | `Phân quyền màn hình` | — (hardcoded) | `phan-quyen.page.html:3` |
| Save button (idle / saving) | `Lưu thay đổi` / `Đang lưu…` | — (hardcoded) | `phan-quyen.page.html:5` |
| Helper text | `Tick chọn role được thấy màn hình tương ứng. Mục không tick role nào = mở cho mọi user đã đăng nhập.` | — (hardcoded) | `phan-quyen.page.html:9-10` |
| Column header 1 | `Màn hình` | — (hardcoded) | `permission-matrix.html:5` |
| Role column headers (dynamic) | `{{ role }}` — API-supplied; contract lists `SuperAdmin` / `Admin` / `User` | — (hardcoded, API value) | `permission-matrix.html:7`; `doc/contracts/permissions.md:14` |
| Row label (dynamic) | `{{ row.SysMenuName }}` — seeded values: `Dashboard`, `Danh mục`, `DTI`, `Quản trị hệ thống`, `Người dùng`, `Phân quyền` | — (hardcoded, DB value) | `permission-matrix.html:18`; `CoreSeeder.cs:79-84` |
| Child-row glyph | `└` | — (hardcoded) | `permission-matrix.html:16` |
| Checkbox accessible name (dynamic) | `<SysMenuName> — <role>` | — (hardcoded template) | `permission-matrix.html:27` |
| Empty/loading row | `Chưa có mục menu nào.` | — (hardcoded) | `permission-matrix.html:34` |
| Save-success toast | `Đã lưu thay đổi phân quyền.` | — (hardcoded) | `phan-quyen.page.ts:61` |
| Error toast (API envelope present) | server-supplied `message` from the API envelope | — (server value) | `http-error.interceptor.ts:44` |
| Error toast fallback — no connection | `Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.` | — (hardcoded) | `http-error.interceptor.ts:16` |
| Error toast fallback — 403 | `Bạn không có quyền thực hiện thao tác này.` | — (hardcoded) | `http-error.interceptor.ts:20` |
| Error toast fallback — other | `Đã có lỗi xảy ra. Vui lòng thử lại.` | — (hardcoded) | `http-error.interceptor.ts:24` |
| Toast dismiss accessible name (shell) | `Đóng thông báo` | — (hardcoded) | `toast.html:8` |

Copy notes, as shipped: the save button uses a real ellipsis character (`…`, U+2026) rather than three dots; the helper sentence states the **open-by-default** rule of PERM-1 in words — a row with no role ticked is visible to every signed-in user — which is the same rule the contract records (`doc/contracts/permissions.md:41-42`). No typos were found in this screen's strings.

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **loading (first paint):** `loading` starts `true` and the single `GET` fires in the constructor (`phan-quyen.page.ts:26,30-38`). While loading, `rows()` **and** `roles()` are still `[]`, so the matrix renders its `@empty` branch: a `<thead>` with only the `Màn hình` column and one body row reading `Chưa có mục menu nào.` at `colspan=1`. **There is no spinner, skeleton or progress text anywhere on this screen** — the loading state is visually identical to the empty state, distinguishable only by the save button, which is `[disabled]` while `loading()` is true (`phan-quyen.page.html:4`).
- **populated:** rows render parent-then-child. A root row is a plain `<td>`; every non-root row gets `td.indent` plus the `└` glyph. `Indent` is a boolean, not a depth counter, so a grandchild would render at the same single indent as a child — the seeded tree is one level deep so this is not visible today (`permission-matrix.ts:18-24`). Checkboxes reflect `AssignedRoles` per row (`permission-matrix.ts:51-53`); the `SuperAdmin` column is an ordinary clickable column with no lock, no tag and no note. A ticked box means "this role can see this menu"; **a row with no box ticked is open to every signed-in user**, which is what the helper text states.
- **dirty (unsaved edits):** ticking a box only mutates local signal state and sets `dirty()`; nothing is sent until the save button is pressed (`phan-quyen.page.ts:41-53`). **The flag is never read by any template** — there is no unsaved-changes badge, no button-enabled-only-when-dirty behaviour and no navigation guard, so navigating away or closing the browser discards edits silently.
- **saving:** the save button switches its label to `Đang lưu…` and goes `[disabled]`; the matrix receives `[loading]="loading() || saving()"`, which disables **every** checkbox for the duration (`phan-quyen.page.html:4-6,16`, `permission-matrix.html:25`). The `PUT` always sends the **complete** row set, not just the edited rows, as the contract's overwrite semantics require (`phan-quyen.service.ts:19-28`, `doc/contracts/permissions.md:51-55`).
- **save success:** `saving` clears, `dirty` clears, and a success toast appears bottom-right for 5 s (`phan-quyen.page.ts:58-62`, `toast.service.ts:11`).
- **error (the `GET` or the `PUT`):** the only feedback is the toast raised by `httpErrorInterceptor` — server `message` when the API envelope is present, otherwise a status-based fallback (`http-error.interceptor.ts:34-49`). On this screen the handlers do nothing beyond clearing their own loading/saving flag (`phan-quyen.page.ts:37,63`): a failed `GET` leaves the matrix in the empty-looking state described above with **no error text inside the card and no retry control**; a failed `PUT` leaves the local edits on screen with the dirty flag still set, and the user's only route forward is to press save again. A 401 on a *later* navigation is caught by `authGuard`, which redirects to `/dang-nhap` carrying a `returnUrl` (`auth.guard.ts:21`); the interceptor itself only toasts.
- **access denied:** a signed-in non-`SuperAdmin` who navigates here never sees the screen — `superAdminGuard` returns a `UrlTree` to `/dashboard` (`super-admin.guard.ts:14-17`). There is **no** 403 page, no explanatory message and no toast on that path; the redirect is silent.
- **validation:** none exists on this screen. Every input is a checkbox with two legal values, so there is no inline validation state, no error styling and no field-level message anywhere in the matrix.

### Responsive

<!-- Behavior per breakpoint. -->

- **No `@media` query exists in either of this screen's own stylesheets** — `phan-quyen.page.scss` (4 lines) and `permission-matrix.scss` (27 lines) contain zero media queries. Every breakpoint effect below is inherited from the shell (`app.scss`, `topbar.scss`, `sidebar.scss`) or from global rules in `styles.scss`.
- **≥981px (desktop default):** sidebar fixed at `--sidebar-w`, content offset by the same via `.shell-content{margin-left}`; `main` is centred at `--container-max-width` with `--sp-5` padding (`app.scss:11-30`). Collapsing the sidebar narrows the offset to `--sidebar-w-collapsed` with a 0.2 s transition — the matrix simply reflows wider, it has no layout of its own tied to that.
- **≤`breakpoint-tablet` (980px, `app.scss:32-36`):** `.shell-content` margin-left is forced to 0 and the sidebar becomes an off-canvas drawer opened by the topbar hamburger, which is `display:none` above this width (`sidebar.scss:236-276`, `topbar.scss:49-56`). The full viewport width goes to `main`, so the matrix gets *wider* here, not narrower.
- **≤`breakpoint-mobile` (560px, `app.scss:38-42`):** `main` padding drops to `10px` (raw literal, no token) and the topbar hides the user's name (`topbar.scss:39-43`). Nothing inside the card changes.
- **Wide role × row matrix on a narrow viewport — what the code actually does:** the table is `width:100%` with **no `min-width`** (`styles.scss:343-347`), so it is *not* pinned to a fixed width the way the prototype's criteria table was. Columns therefore compress with the container: the name column is held at an inline `width:40%` and the role columns share the rest. Horizontal scrolling is available — `.tablewrap{overflow:auto}` — but it is **content-driven, not width-driven**: it engages only once the table's intrinsic min-content width (longest untruncated menu name plus one column per role, since nothing sets `text-overflow`/`white-space` on body cells) exceeds the container. With the three shipped roles and the seeded labels there is normally no horizontal scroll even at 390px; the more roles the API returns, the sooner it starts. There is **no** column collapse, no card-per-row fallback and no per-viewport column hiding anywhere in this screen.
- **Vertical scrolling (all viewports):** `.tablewrap{max-height:560px}` (raw literal, `permission-matrix.scss:5`) makes the body scroll vertically once the rows exceed that height, while `th{position:sticky;top:0;z-index:4}` keeps the role headers pinned inside that scroll container (`styles.scss:358-368`).
- **Print (`@media print`):** the sidebar, topbar and toast stack disappear via `.no-print` (`styles.scss:100-108`, `sidebar.scss:338-343`, `topbar.scss:58-62`, `toast.html:1`); `.shell-content` margin is zeroed and `main` loses its max-width (`app.scss:44-52`). The card and the matrix do print — but `.tablewrap` keeps `overflow:auto` **and** `max-height:560px` in print, so any row past that height is clipped from the printout.

### Iconography

See `Icons.md` § Per-Action Map. **This screen's own template contains no icon at all** — every control is a text `Button` or a native checkbox, and the only glyph is the literal `└` text character marking a child row (`permission-matrix.html:16`), which is plain text in a `<span>`, not an icon element.

Icons visible while this screen is open all belong to the app shell, which loads **PrimeIcons v7** globally (`angular.json:39`). `Icons.md` was refreshed on 2026-08-22 and now covers every one of them by name in its § Per-Action Map — the prototype-era `library: "none"` declaration, and the gap this spec used to record because of it, are both gone.

**No PrimeNG inline-SVG icon reaches this screen.** That is worth stating explicitly, because the app's *other* three grids get a second, runtime-injected icon set from PrimeNG (paginator arrows and a loading spinner) that appears nowhere in `src/FE/`. This screen gets none of it: there is **no `p-table` anywhere under `platform/phan-quyen/`** — the matrix is a hand-rolled `<table>` element (`permission-matrix.html:2`) — so there is no paginator and no PrimeNG loading mask. The `[loading]` input at `phan-quyen.page.html:16` is the app's own signal input on that component (`permission-matrix.ts:43`); it renders no spinner and no overlay, and its only visible effect is `[disabled]` on every checkbox (`permission-matrix.html:25`) — see § States for how that reads to the user.

| Action | Icon | Placement |
| --- | --- | --- |
| Save the matrix | — (text `Button`, no icon) | `.title` row, right-aligned inside the card |
| Grant/revoke a role on a row | — (native `<input type="checkbox">`, `accent-color:var(--brand)`) | Every `td.num`, right-aligned |
| Sidebar entry for this screen | `pi pi-shield` (from `SysMenu.Icon`) | Shell sidebar, under the `pi pi-cog` group "Quản trị hệ thống". Both are seeded values, not FE constants: the icon-bearing menu rows are `CoreSeeder.cs:79-84`, with `pi-shield` on the "Phân quyền" row at `:84` and `pi-cog` on its parent group at `:82` — so either can change in the database without an FE deploy (`Icons.md` § Per-Action Map) |
| Open the navigation drawer (≤980px) | `pi pi-bars` | Shell topbar, left (`topbar.html:11`) |
| Sign out | `pi pi-sign-out` | Shell topbar, right (`topbar.html:19`) |
| Dismiss a toast | `pi pi-times` | Shell toast item, right (`toast.html:11`) |

### Screenshots

<!-- Refs into Assets/Screenshots/phan-quyen/ -->

**The desktop shot exists — captured 2026-08-22 from the live Angular app**, as `permission-matrix--desktop-1440.png`. That is the full target under `doc/Design/CLAUDE.md` § Rules (ONE desktop shot per screen, decided 2026-08-22). Every remaining row below is an **on-demand** state/viewport variant, *not* an outstanding debt: capture one when someone actually needs that case, and flip its status then. All of them need both servers running and a signed-in `SuperAdmin`, because the route is guarded and the matrix is API-driven.

Common prerequisites for all rows:

1. Start the API: `dotnet run --project src/BE/PlatformManager.Api` — the FE dev config expects it on `http://localhost:5027/api` (`src/FE/src/environments/environment.development.ts`).
2. Start the app: `npm start` in `src/FE/` → `http://localhost:4200` (Angular CLI default; no port override in `angular.json`).
3. Sign in at `http://localhost:4200/dang-nhap` with a **SuperAdmin** account — in Development `CoreSeeder` seeds one; credentials are deliberately not recorded in this spec. An `Admin` account will be redirected to `/dashboard` and cannot capture this screen.
4. Navigate to `http://localhost:4200/quan-tri/phan-quyen`.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png` | captured 2026-08-22 | Live app, `/quan-tri/phan-quyen`, full page. Shows the whole route: one card, the menu tree with parent/child indent, and the clickable `SuperAdmin` column. Environment as recorded in `UiInventory.md` § Screenshot Manifest (API on `:5027`, FE served with `npx ng serve --port 4201`). |
| `Assets/Screenshots/phan-quyen/permission-matrix--saving--desktop-1440.png` | on demand | Same session @ 1440×1000; in DevTools set network throttling to "Slow 3G" (or add a request-blocking delay on `PUT /api/admin/permissions`), toggle any checkbox, click "Lưu thay đổi" and capture while the button reads `Đang lưu…` and every checkbox is disabled. |
| `Assets/Screenshots/phan-quyen/permission-matrix--saved-toast--desktop-1440.png` | on demand | Toggle any checkbox, click "Lưu thay đổi", capture within 5 s of success so the `Đã lưu thay đổi phân quyền.` toast is still in the bottom-right stack (auto-dismiss is 5 s, `toast.service.ts:11`). |
| `Assets/Screenshots/phan-quyen/permission-matrix--loading-empty--desktop-1440.png` | on demand | In DevTools, block `GET /api/admin/permissions` (Network → Block request URL), then reload the route. Captures the shared loading/empty rendering: single-column header + `Chưa có mục menu nào.` + disabled save button, plus the interceptor's error toast. |
| `Assets/Screenshots/phan-quyen/permission-matrix--tablet-900.png` | on demand | Same as row 1 @ 900×1200 — below `breakpoint-tablet`, so the sidebar is an off-canvas drawer and the topbar shows the `pi pi-bars` hamburger. |
| `Assets/Screenshots/phan-quyen/permission-matrix--mobile-390.png` | on demand | Same as row 1 @ 390×900 — below `breakpoint-mobile`; verify whether the table compresses or starts scrolling horizontally with the role list the API actually returns, and record which occurred in the capture note. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

1. **Resource permissions are a plan, not a screen — and the design for them is not written.** `doc/contracts/permissions.md` PERM-2 (`RolePermission`, `GET`/`PUT /api/admin/permissions/resources`) describes a second role × row matrix, deny-by-default, with a `SuperAdmin` break-glass column, intended to sit beside the menu matrix on this route. It is **`Status: DRAFT`** (`doc/contracts/permissions.md:59-64`): the endpoint is not agreed, and no component, route or class implementing it exists in `src/FE/`. Consequences for design work: this route stays a single-matrix, tab-less card until BE settles PERM-2; a second matrix implies a view switcher, and the app's only switcher today is `SegmentedControl` on `/dashboard` (`Components/SegmentedControl.md`) — reuse it rather than inventing a `.btn`-based one. **Write the spec when the endpoint moves off `DRAFT`, from the shipped code, not from the contract.** Until then no screen spec, prompt pack or generated image may show it. *(Recorded 2026-08-23 after a `TabBar` component spec and a whole second tab were found here describing UI that had never been built.)*
2. **Loading is indistinguishable from empty and from a failed load.** All three render the same single-column table plus `Chưa có mục menu nào.`; only a disabled save button hints that a request is in flight, and a `GET` failure leaves no error text or retry control inside the card — the only signal is a toast that auto-dismisses after 5 s (`phan-quyen.page.ts:30-38`). Give the matrix a real loading state, a distinct empty state and an in-card error state with a retry action.
3. **`dirty()` is tracked but never read by any template** (`phan-quyen.page.ts:28,52,60`) — no unsaved-changes indicator, no dirty-gated save button, no `CanDeactivate` guard. Navigating away silently discards edits on a screen whose save is a full overwrite. Either wire the flag to the UI or remove it.
4. **A failed save leaves the user with no path but to press the same button again** — the edits stay on screen and dirty, but there is no "retry / discard / reload from server" affordance and no way to see which rows differ from the server (`phan-quyen.page.ts:63`).
5. **No confirmation before a full-overwrite save.** The `PUT` replaces the entire matrix, and un-ticking every role on a row silently *widens* access — the menu becomes visible to every signed-in user (`doc/contracts/permissions.md:41-42`). A destructive-looking gesture with an expanding consequence, and zero confirmation; a confirm step summarising the delta would be proportionate here.
6. **Saving does not refresh the sidebar.** The `PUT` changes exactly what the shell's `SysMenu` tree renders, but `onSave()` only clears its flags and toasts (`phan-quyen.page.ts:55-65`), so the nav rail keeps showing the pre-save visibility until a reload. The screen edits the menu it is displayed inside and does not tell it.
7. **The indent flag is a boolean, not a depth.** `toDisplayOrder()` marks every descendant with the same `Indent: true` (`permission-matrix.ts:18-24`); today's seeded menu is one level deep, but a grandchild menu would render visually identical to a child, misrepresenting the tree the matrix is supposed to show.
8. **Untokenized literals inside the matrix** — `max-height:560px`, checkbox `16px`/`16px`, `td.indent{padding-left:28px}`, `.tree-branch{margin-right:4px}`, `outline:2px`/`outline-offset:2px`, and the inline `style="width:40%"` on the first column (`permission-matrix.scss:1-27`, `permission-matrix.html:5`). None resolve to a token in `Tokens/`; they should be tokenized, or the existing scale extended.
9. **Printing clips the matrix.** `.tablewrap` keeps `overflow:auto` and `max-height:560px` under `@media print`, so rows past that height never reach paper. Reset the wrapper's height and overflow for print.
