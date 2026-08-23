---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
flow: "User Administration"
screens: ["User Administration"]
source_routes: ["/quan-tri/nguoi-dung"]
---

# User Administration — Screens

User Administration is the platform's account-management screen: an administrator lists every user, searches by name or email, creates a user with a temporary password, edits an existing user's email/full name/roles, and locks or unlocks an account. It is a single lazy-loaded route (`/quan-tri/nguoi-dung`) gated by `authGuard` + `adminGuard` (`Admin` **or** `SuperAdmin`), rendered inside the app shell (sidebar + topbar + toast). The add/edit form is an in-page native `<dialog>` modal on this same route, not a separate route. This screen carries three deliberate, security-visible UI decisions — self-lock is blocked in the UI, `SuperAdmin` is omitted from the role picker, and roles the form cannot assign are preserved on save — all documented below as shipped behaviour. The live source is the Angular 20 app in `src/FE/`; the deleted prototype is design-intent reference only and the shipped app has diverged from it (see § Normalize on redesign).

> **Shell:** app shell (sidebar + topbar + toast) — `src/FE/src/app/app.html`, `src/FE/src/app/app.scss`. **`DESIGN.md` → Layout covers this shell correctly** as of the 2026-08-22 token refresh: it documents both of the app's shells (`DESIGN.md:418-422`) — the main one, `Sidebar` at `--sidebar-w` 220px / `--sidebar-w-collapsed` 60px with `.shell-content` offset to match, a sticky `Topbar` and a `main` capped at `--container-max-width`, which is the one this route renders — alongside the `noShell` auth shell, and all three breakpoints (980 / 560 / 981px).
> **Sources:** `src/FE/src/app/platform/quan-tri-nguoi-dung/` (pages, components, services, models, routes), `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`, `src/FE/src/app/app.html`, `src/FE/src/styles.scss`, `doc/contracts/users.md`

---

## User Administration (`/quan-tri/nguoi-dung`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:1-14`) — the route sets no `noShell` flag, so the full shell renders
  - **`Sidebar`** (`.sidebar`, `Components/Sidebar.md`; fixed left, width `--sidebar-w` 220px / `--sidebar-w-collapsed` 60px when collapsed, `z-index:35`, background `--card`, right border `--line`) — `sidebar.html:1`, `sidebar.scss:3-20`
    - Brand row (`.sidebar-brand`): `.brand-mark` "PM" square (26px, `--brand` fill, `--on-primary` text) + `.brand-text` "PlatformManager" + `.sidebar-toggle` ghost icon button — `sidebar.html:2-14`
    - Nav list (`.sidebar-nav`) — menu items come from the server-driven menu service, not from this screen; the item pointing here is highlighted via `routerLinkActive="active"` (`sidebar.html:38-47`)
  - **Shell content** (`.shell-content`, `margin-left:` `--sidebar-w`, or `--sidebar-w-collapsed` when the sidebar is collapsed) — `app.scss:11-22`
    - **`Topbar`** (`.topbar`, `Components/Topbar.md`; sticky top, `z-index:20`, translucent white background with `backdrop-filter: blur(10px)`, bottom border `--line`; inner `.topin` max-width `--container-max-width` 1600px, padding `--sp-4` `--sp-5`, gap `--sp-3`) — `topbar.html:1-24`, `topbar.scss:1-17`
      - `Button` (`.btn.sidebar-hamburger`, `pi-bars` icon-only) — hidden ≥981px, shown ≤`breakpoint-tablet`
      - `.logo h1` — page title text from route data (`quan-tri-nguoi-dung.routes.ts:10`)
      - `.topbar-user` (margin-left auto): current user's full name text + `Button` (`.btn`, `pi-sign-out` icon + "Đăng xuất")
    - **`main`** (max-width `--container-max-width` 1600px, centered, padding `--sp-5`) — `app.scss:24-30`
      - `Card` (`.card`, the whole screen body is one card: background `--card`, border `--line`, `--shadow`, radius `--radius-lg`, padding `--sp-5`) — `quan-tri-nguoi-dung.page.html:1`
        - Title row (`.title`, the global card-title row documented in `Components/Card.md` § Anatomy — no spec of its own; flex, space-between, gap `--sp-3`, margin-bottom `--sp-4`)
          - `h2` "Danh sách người dùng" (font-size `--fs-md`)
          - `Button` (primary) — "+ Thêm người dùng", opens the form `Dialog` in create mode
        - `FilterBar` (`.filters`, `Components/FilterBar.md`; flex, wrap, gap `--sp-3`, margin `--sp-4` 0) — this screen has no `.filters-actions` cluster
          - `.search` wrapper (also `Components/FilterBar.md`, which owns `.search`; `flex:1`, `min-width:220px` literal, `position:relative`) containing a `pi-search` PrimeIcon absolutely positioned at `left:10px` in `--muted`, plus `Input` (filter-with-search-adornment variant, `Components/Input.md`: border `--border-strong`, radius `--radius-sm`, padding `--sp-2` `--sp-3`, font `--fs-sm`, `padding-left:32px` to clear the icon) — `quan-tri-nguoi-dung.page.html:7-12`, `quan-tri-nguoi-dung.page.scss:1-19`
        - `app-user-grid-table` (`quan-tri-nguoi-dung.page.html:14-24`)
          - `Table` (`Components/Table.md`) — the `.tablewrap` shell plus the global `th`/`td`/zebra rules that paint the cells (here `.tablewrap` is re-declared locally: border `--border-strong`, radius `--radius-table`, `overflow:hidden`; that is the **PrimeNG-grid contract**, one of the two `.tablewrap` contracts Table.md records — the `overflow:auto; max-height:560px` form belongs to the two permission matrices). Inside it sits a PrimeNG `p-table` (`primeng@^20.2.0`) with `[lazy]="true"`, `[paginator]="true"`, `[rowsPerPageOptions]="[10,20,50]"`, `dataKey="Id"` — that grid mechanism, its paginator and its loading mask are `DataTable` (`Components/DataTable.md`) (`user-grid-table.html:1-80`, `user-grid-table.scss:1-5`)
            - Header row — 5 `<th>` with **inline** `style="min-width:…"` attributes: Người dùng 220px · Vai trò 140px · Trạng thái 120px · Ngày tạo 110px · Hành động 100px + `text-align:right`. `<th>` styling is inherited from global `th` (sticky top, background `--surface-table-header`, color `--text-table-header`, font `--fs-xs`/700) — `user-grid-table.html:14-22`, `styles.scss:378-388`
            - Body row (one per user; global `td` padding `--sp-2` `--sp-3`, font `--fs-sm`, bottom border `--line`; even rows tinted `--surface-table-header`, hover tint `--bg`)
              - Cell 1 — `Avatar` (`Components/Avatar.md`, which documents the whole identity cell): `.user-cell` flex with gap `--sp-3` holding the `.avatar` initials circle (30px literal, `border-radius:50%`, `--brand` fill, `--on-primary` text, weight 800, font `--fs-xs`) + `.user-name` (weight 700) over `.user-email` (`--muted`, `--fs-xs`, falls back to `—` when null)
              - Cell 2 — zero or more `RoleTag` chips (`.role-tag`, `Components/RoleTag.md`; border `--line`, radius `6px` literal, padding `2px 8px` literal, font `--fs-xs`, background `--surface-table-header`, `margin-right:4px`), one per role string returned by the API. Deliberately **not** a `Badge` — a role name is an identifier, not a status, so it carries no semantic colour
              - Cell 3 — `Badge` (`Components/Badge.md`, variants "Account locked" / "Account active"): `.badge.locked` (`--bad-bg` / `--bad`) or `.badge.active` (`--good-bg` / `--good`); base `.badge` supplies radius `--radius-pill`, padding `3px 6px`, font-size `10px`, weight 750. These two classes are declared **only** in `user-grid-table.scss:49-57`, not globally, unlike `.bdone`/`.bwork`/`.bstall` (`styles.scss:302-323`)
              - Cell 4 — creation date, `.muted` (`--muted`, `--fs-xs`), rendered by `formatDateVn()`; **`—` when the value is absent or unparseable** (`user-grid-table.ts:7-12`)
              - Cell 5 `.row-actions` (the flex box `Components/ActionButton.md` § Anatomy describes; gap `6px` literal, `justify-content:flex-end`): two `ActionButton`s (`.action-btn`, `Components/ActionButton.md`; transparent background, radius `--radius-sm`, padding `4px --sp-2`, colour `--muted`) — "Sửa" (`pi-pencil`) and the lock toggle (`pi-lock` / `pi-lock-open`, gains `.danger` → `--bad` while the account is unlocked). **The lock button is `[disabled]` on the signed-in user's own unlocked row** (`user-grid-table.html:53-68`, `user-grid-table.ts:63-66`, `styles.scss:211-251`)
            - Empty-message row — one `<td colspan="5" class="muted">` (`user-grid-table.html:74-78`)
            - PrimeNG paginator, bottom position (component default): first/prev/page-links/next/last buttons plus a rows-per-page dropdown (10 / 20 / 50). No `currentPageReportTemplate` is set, so no "showing X of Y" text renders — page numbers and the dropdown are the only visible paginator content (`user-grid-table.html:6-11`)
      - `Toast` stack (`.toast-stack`, `Components/Toast.md`; `position:fixed`, right/bottom `--sp-5`, `z-index:60`, max-width `min(360px, 90vw)`, `aria-live="polite"`, auto-dismiss after 5000 ms) — success/error feedback for every mutation on this screen (`toast.html:1-15`, `toast.scss:1-10`, `toast.service.ts:11`)
- `Dialog` (`dialog.form-dialog`, native `<dialog>` opened with `showModal()`; width `min(560px, 92vw)`, padding `--sp-5`, radius `--radius-dialog`, backdrop `overlay-backdrop`) — the Add/Edit user form, a sibling of the card on the same route (`quan-tri-nguoi-dung.page.html:27-33`, `user-form-dialog.html:1-71`, `styles.scss:464-482`)
  - Title row (`.title`): `h2` — "Thêm người dùng" or "Sửa người dùng" depending on mode — + `Button` (`.btn`) "Đóng"
  - `FormRow` × 3–5 (`.form-row`, `Components/FormRow.md`; column flex, gap `--sp-2`, margin-bottom `--sp-4`; labels `--fs-sm`/700). Its fields are the **third of four input tiers** — border `--line` (the faint tier, not `--border-strong`), radius `--radius-sm`, padding `--sp-2` `--sp-3`, `width:100%` — recorded as the "Form row (dialog)" variant in `Components/Input.md`
    - "Tên đăng nhập" + `Input` — **create mode only**, auto-focused and text-selected on open via `appAutofocus`
    - "Email" + `Input` (`type="email"`)
    - "Họ tên" + `Input`
    - "Mật khẩu tạm" + `Input` — **create mode only**, `type="text"` (the value is deliberately visible, not masked)
    - "Vai trò" + `.role-checkboxes` (part of `Components/FormRow.md`; flex, gap `--sp-4`, `role="group"`, `aria-labelledby`) — one native checkbox per entry of `ASSIGNABLE_ROLES`, which is **`['Admin', 'User']` only: `SuperAdmin` is deliberately omitted from the picker** (`quan-tri-nguoi-dung.model.ts:74-82`)
      - `.role-preserved` note (part of `Components/FormRow.md`, the `.role-checkboxes` sibling paragraph; `--fs-sm`, `--muted`) — rendered only in edit mode when the target user holds roles outside `ASSIGNABLE_ROLES`; those roles are re-sent unchanged on save rather than being stripped (`user-form-dialog.html:53-60`, `user-form-dialog.ts:77-79`, `:142`)
    - Each required label carries a `.required` asterisk in `--bad`
  - `.form-error` (part of `Components/FormRow.md`; global, `--bad`, `--fs-sm`) — rendered only when a local validation message or a server error message exists. There is **no per-field error slot** anywhere in the app
  - `.dialog-actions` (part of `Components/Dialog.md` § Anatomy — not global, re-declared per dialog; flex, `justify-content:flex-end`, gap `8px` literal, margin-top `8px` literal — this copy is one of the three drifted `margin-top` values): `Button` (`.btn`) "Huỷ" · `Button` (primary) "Lưu"

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `PlatformManager` | — (hardcoded) | `src/FE/src/index.html:5` — static, never updated per route |
| Topbar page title (route data) | `Người dùng hệ thống` | — (hardcoded) | `quan-tri-nguoi-dung.routes.ts:10` |
| Topbar logout button | `Đăng xuất` | — (hardcoded) | `topbar.html:18-20` |
| Topbar logout button title attr | `Đăng xuất` | — (hardcoded) | `topbar.html:18` |
| Topbar hamburger aria-label | `Mở menu điều hướng` | — (hardcoded) | `topbar.html:7` |
| Sidebar brand mark / text | `PM` / `PlatformManager` | — (hardcoded) | `sidebar.html:3-4` |
| Card heading | `Danh sách người dùng` | — (hardcoded) | `quan-tri-nguoi-dung.page.html:3` |
| Create button | `+ Thêm người dùng` | — (hardcoded) | `quan-tri-nguoi-dung.page.html:4` |
| Search placeholder | `Tìm theo tên hoặc email...` | — (hardcoded, three ASCII dots, not `…`) | `quan-tri-nguoi-dung.page.html:10` |
| Table column headers | `Người dùng` / `Vai trò` / `Trạng thái` / `Ngày tạo` / `Hành động` | — (hardcoded) | `user-grid-table.html:16-20` |
| Missing email fallback | `—` (em dash) | — (hardcoded) | `user-grid-table.html:31` |
| Missing/unparseable date fallback | `—` (em dash) | — (hardcoded) | `user-grid-table.ts:8,11` |
| Role chip text | *(role name verbatim from the API, e.g.* `Admin` *,* `User` *,* `SuperAdmin` *)* | — (server value, never re-cased by the FE) | `user-grid-table.html:36-38` |
| Status badge — locked | `● Đã khoá` (leading U+25CF glyph) | — (hardcoded) | `user-grid-table.html:42` |
| Status badge — active | `● Đang hoạt động` (leading U+25CF glyph) | — (hardcoded) | `user-grid-table.html:44` |
| Edit action title attr | `Sửa` | — (hardcoded) | `user-grid-table.html:50` |
| Lock action title attr — self-lock blocked | `Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất` | — (hardcoded) | `user-grid-table.html:60` |
| Lock action title attr — target locked | `Mở khoá tài khoản` | — (hardcoded) | `user-grid-table.html:62` |
| Lock action title attr — target unlocked | `Khoá tài khoản` | — (hardcoded) | `user-grid-table.html:63` |
| Grid empty message | `Không có người dùng nào khớp bộ lọc.` | — (hardcoded) | `user-grid-table.html:76` |
| Dialog title — create | `Thêm người dùng` | — (hardcoded) | `user-form-dialog.ts:86` |
| Dialog title — edit | `Sửa người dùng` | — (hardcoded) | `user-form-dialog.ts:86` |
| Dialog close button | `Đóng` | — (hardcoded) | `user-form-dialog.html:4` |
| Field label (create only) | `Tên đăng nhập` + `*` | — (hardcoded) | `user-form-dialog.html:9` |
| Field placeholder (create only) | `vd nguyen.van.a` | — (hardcoded) | `user-form-dialog.html:13` |
| Field label | `Email` + `*` | — (hardcoded) | `user-form-dialog.html:21` |
| Field placeholder | `ten@congty.vn` | — (hardcoded) | `user-form-dialog.html:22` |
| Field label | `Họ tên` + `*` | — (hardcoded) | `user-form-dialog.html:26` |
| Field placeholder | `Nguyễn Văn A` | — (hardcoded) | `user-form-dialog.html:27` |
| Field label (create only) | `Mật khẩu tạm` + `*` | — (hardcoded) | `user-form-dialog.html:32` |
| Field placeholder (create only) | `vd TempPass@123` | — (hardcoded) | `user-form-dialog.html:36` |
| Field label | `Vai trò` + `*` | — (hardcoded) | `user-form-dialog.html:44` |
| Role checkbox labels | `Admin` / `User` | — (hardcoded via `ASSIGNABLE_ROLES`; `SuperAdmin` deliberately absent) | `quan-tri-nguoi-dung.model.ts:82`, rendered `user-form-dialog.html:46-51` |
| Preserved-roles note | `Vai trò hệ thống: <roles, comma-separated> — giữ nguyên, không thay đổi khi lưu.` | — (hardcoded) | `user-form-dialog.html:56-59` |
| Dialog cancel button | `Huỷ` | — (hardcoded) | `user-form-dialog.html:68` |
| Dialog submit button | `Lưu` | — (hardcoded) | `user-form-dialog.html:69` |
| Validation — email empty | `Email bắt buộc.` | — (hardcoded) | `user-form-dialog.ts:145` |
| Validation — full name empty | `Họ tên bắt buộc.` | — (hardcoded) | `user-form-dialog.ts:149` |
| Validation — no role selected | `Chọn ít nhất 1 vai trò.` | — (hardcoded) | `user-form-dialog.ts:153` |
| Validation — username empty (create) | `Tên đăng nhập bắt buộc.` | — (hardcoded) | `user-form-dialog.ts:162` |
| Validation — temp password too short (create) | `Mật khẩu tạm phải có ít nhất 8 ký tự.` | — (hardcoded, JS template using `MIN_TEMP_PASSWORD_LENGTH = 8`) | `user-form-dialog.ts:166`, constant `:18` |
| Toast — create success | `Đã thêm người dùng.` | — (hardcoded) | `quan-tri-nguoi-dung.page.ts:142` |
| Dialog error — create failed (fallback when the API sends no message) | `Không tạo được người dùng — thử lại sau.` | — (hardcoded) | `quan-tri-nguoi-dung.page.ts:145` |
| Toast — update success | `Đã cập nhật người dùng.` | — (hardcoded) | `quan-tri-nguoi-dung.page.ts:155` |
| Dialog error — update failed (fallback when the API sends no message) | `Không cập nhật được người dùng — thử lại sau.` | — (hardcoded) | `quan-tri-nguoi-dung.page.ts:158` |
| Toast — unlock success | `Đã mở khoá tài khoản.` | — (hardcoded) | `quan-tri-nguoi-dung.page.ts:168` |
| Toast — lock success | `Đã khoá tài khoản.` | — (hardcoded) | `quan-tri-nguoi-dung.page.ts:168` |
| Toast — HTTP error, no envelope, offline | `Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.` | — (hardcoded) | `http-error.interceptor.ts:21` |
| Toast — HTTP error, no envelope, 401 | `Bạn cần đăng nhập để tiếp tục.` | — (hardcoded) | `http-error.interceptor.ts:23` |
| Toast — HTTP error, no envelope, 403 | `Bạn không có quyền thực hiện thao tác này.` | — (hardcoded) | `http-error.interceptor.ts:25` |
| Toast — HTTP error, no envelope, 404 | `Không tìm thấy dữ liệu yêu cầu.` | — (hardcoded) | `http-error.interceptor.ts:27` |
| Toast — HTTP error, no envelope, 429 | `Bạn thao tác quá nhanh. Vui lòng chờ một lát rồi thử lại.` | — (hardcoded) | `http-error.interceptor.ts:32` |
| Toast — HTTP error, no envelope, other | `Đã có lỗi xảy ra. Vui lòng thử lại.` | — (hardcoded) | `http-error.interceptor.ts:34` |
| Toast close aria-label | `Đóng thông báo` | — (hardcoded) | `toast.html:8` |
| Server lock errors surfaced verbatim as toasts | `Bạn không thể tự khoá tài khoản của chính mình. Nếu muốn kết thúc phiên làm việc, hãy đăng xuất.` / `Chỉ SuperAdmin mới được khoá tài khoản có vai trò SuperAdmin.` | — (server `message`, shown as-is) | `doc/contracts/users.md:68-69`, rendered via `http-error.interceptor.ts:82` |

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **loading (initial and every refetch):** `loading` is set `true` before each request and cleared in both the success and error callbacks (`quan-tri-nguoi-dung.page.ts:75-85`). `[loading]` on `p-table` renders PrimeNG's `.p-datatable-mask.p-overlay-mask` overlay with a spinning inline SVG spinner over the grid. The paginator, the search field and the "+ Thêm người dùng" button stay interactive underneath — nothing else on the screen is blocked. On first load `rows` is empty **and** `loading` is true, and PrimeNG only renders the empty-message row when `isEmpty() && !loading`, so the empty text never flashes before data arrives.
- **populated (default):** one row per user on the current page. `Badge` shows `● Đang hoạt động` or `● Đã khoá`; role chips render one per role string, in the order the API returned them, with no client-side re-casing; the date column shows a `vi-VN`-locale date (`toLocaleDateString('vi-VN')`). Pagination is **server-side** (`[lazy]="true"`, `[totalRecords]="totalCount()"`), page size 10 by default with 20/50 selectable.
- **empty (no users match the current filter):** `p-table` renders the `#emptymessage` template — a single `<td colspan="5" class="muted">` reading `Không có người dùng nào khớp bộ lọc.` The paginator still renders. There is no separate "no users at all" state — the same message covers both an empty database and an over-narrow search.
- **error (list fetch fails):** `fetchList`'s error callback only clears `loading` (`quan-tri-nguoi-dung.page.ts:83`) — **there is no screen-level error state**: no error banner, no retry button, and `rows`/`totalCount` keep their previous values, so the grid silently keeps showing stale data (or stays empty on first load). The only user-visible feedback is the toast raised by `httpErrorInterceptor`. A 401 mid-session additionally clears the user context and redirects to `/dang-nhap` with a `returnUrl` (`http-error.interceptor.ts:52-58`).
- **row action disabled — self-lock blocked:** on the signed-in user's own row, while that account is unlocked, the lock button is `[disabled]` and its `title` reads `Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất`. Visually it takes the global `.action-btn:disabled` treatment — `opacity:0.45`, `cursor:not-allowed`, and hover suppressed back to transparent/`--muted` (`styles.scss:242-250`). This is deliberate: the server rejects self-lock with `USER.SELF_LOCK_FORBIDDEN` (403), so clicking is guaranteed to fail and the UI blocks the click instead of letting the user earn an error (`user-grid-table.ts:50-66`, `doc/contracts/users.md:69`). Exactly one action is blocked — **unlock is never blocked** (the server deliberately allows it), and **locking someone else is never blocked**, including a `SuperAdmin`, because that is a server-side authorization rule the FE does not duplicate. When `currentUserId` is `null` (identity not yet known) no row is blocked.
- **dialog open — create:** opened by "+ Thêm người dùng"; `editing` is `null`, so the title reads `Thêm người dùng`, the "Tên đăng nhập" and "Mật khẩu tạm" rows render, all fields start empty, no role checkbox is ticked, and `appAutofocus` focuses and selects the username field. The `.role-preserved` note is absent.
- **dialog open — edit:** opened by a row's "Sửa" action; the title reads `Sửa người dùng`, the username and temp-password rows are **not rendered** (the API's `PUT` cannot change either), Email/Họ tên are pre-filled from the row, and role checkboxes are pre-ticked from the target's roles intersected with `ASSIGNABLE_ROLES` (`user-form-dialog.ts:98-102`). Any role outside that list — today only `SuperAdmin` — has no checkbox but is listed read-only in the `.role-preserved` note and is re-sent unchanged in the save payload (`user-form-dialog.ts:142`), so editing a `SuperAdmin` from this screen never silently strips the role.
- **validation (client-side, dialog):** checked in order on submit — email, full name, at least one role; then, in create mode only, username and a temp password of at least 8 characters. The first failure sets a single message rendered in the `.form-error` block below the role row and **returns without emitting** — there is no per-field error text, no red field border, and no field is marked invalid. The message clears when the next submit passes or when the dialog is reopened.
- **saving (dialog submit in flight):** **no busy state exists** — neither "Lưu" nor "Huỷ" is disabled while the create/update request is in flight, no spinner is shown, and nothing prevents a second click from firing a duplicate request (`quan-tri-nguoi-dung.page.ts:137-161`). See § Normalize on redesign.
- **save success:** the dialog closes (`formOpen` → `false`, which drives the native `close()`), the list refetches with the current filter/page unchanged, and a success toast appears bottom-right for 5 s.
- **save error:** the dialog **stays open** and `formServerError` renders in the `.form-error` block — the server's own `message` when the response carries an API envelope, otherwise the hardcoded fallback. Because the interceptor also toasts, a failed save shows the message **twice**: once inside the dialog and once as a toast.
- **lock/unlock in flight and after:** no optimistic update and no per-row busy state — the row is unchanged until the refetch lands, then the badge and the lock icon flip together and a success toast appears. On failure the page's error callback does nothing (`quan-tri-nguoi-dung.page.ts:170-172`) because the interceptor has already toasted the server's message.
- **search:** typing sets the input signal immediately (the field is never laggy), pushes the value through a 300 ms debounce with `distinctUntilChanged`, and resets to page 1. The request fires from a single `effect()` watching the params — so a burst of keystrokes produces exactly one request, carrying the string actually typed.
- **access denied:** not a visual state on this screen — `adminGuard` redirects a non-`Admin`/non-`SuperAdmin` user to `/dashboard` before anything renders (`admin.guard.ts:10-18`).

### Responsive

<!-- Behavior per breakpoint. -->

- **The screen's own SCSS declares zero media queries.** `quan-tri-nguoi-dung.page.scss`, `user-grid-table.scss` and `user-form-dialog.scss` contain no `@media` block at all — every responsive change below comes from the shell (`app.scss`, `topbar.scss`, `sidebar.scss`) or from fluid `min()`/`flex` widths.
- **≥981px (desktop default):** sidebar fixed at `--sidebar-w` (or `--sidebar-w-collapsed`), `.shell-content` offset by the same amount, `main` capped at `--container-max-width` 1600px with `--sp-5` padding. The topbar hamburger is `display:none` (`topbar.scss:45-47`). When the sidebar is collapsed, submenus become hover/focus flyouts (`sidebar.scss:289-336`).
- **≤`breakpoint-tablet` (980px):** `.shell-content { margin-left: 0 !important }` — the sidebar leaves the flow and becomes an off-canvas drawer (`transform: translateX(-100%)`, width `min(85vw, 300px)`, revealed by `.drawer-open` over a `.sidebar-backdrop`), and the topbar hamburger appears (`app.scss:32-36`, `sidebar.scss:236-276`, `topbar.scss:49-56`). The card, filters row and grid are unchanged — they simply reflow into the full width.
- **≤`breakpoint-mobile` (560px):** `main` padding drops to `10px`; the topbar's user-name text is hidden, leaving only the "Đăng xuất" button; the sidebar drawer widens to `min(90vw, 300px)` and its nav items grow to `10px` padding / `40px` min-height for touch (`app.scss:38-42`, `topbar.scss:39-43`, `sidebar.scss:278-287`). The search field keeps its `min-width:220px` and simply fills the row.
- **Grid (all viewports):** the app sets neither `responsiveLayout` nor `breakpoint` on `p-table`, so **no column-stacking treatment is enabled** — all 5 columns render at every viewport. The five inline `min-width` values total 690px plus cell padding, so on narrow screens the grid scrolls horizontally inside PrimeNG's own `.p-datatable-table-container` (which carries an inline `overflow:auto`). The surrounding `.tablewrap` is `overflow:hidden`, so it clips to the rounded corners rather than scrolling itself.
- **Dialog (all viewports):** fluid by design, no breakpoint — `width: min(560px, 92vw)` (`styles.scss:475-477`). The role checkbox row is a plain flex row with `--sp-4` gap and does not wrap to a column on small screens.
- **Toast (all viewports):** `max-width: min(360px, 90vw)`, pinned `--sp-5` from the right and bottom edges at every size (`toast.scss:1-10`).
- **Print (`@media print`):** `.shell-content` loses its sidebar offset, `main` loses its max-width, and the sidebar, backdrop, topbar and every `.no-print`-tagged element (including the toast stack) are hidden (`app.scss:44-52`, `sidebar.scss:338-343`, `topbar.scss:58-62`, `styles.scss:115-119`). The screen has no print-specific layout of its own; the grid prints as-is.

### Iconography

This screen uses a real icon library — **PrimeIcons v7**, loaded globally via `angular.json` → `styles` (`node_modules/primeicons/primeicons.css`) and rendered as `<i class="pi pi-…">` elements. `Icons.md` was refreshed on 2026-08-22 and records this correctly — the prototype-era `library: "none"` claim this spec used to repeat is gone. The map below is this screen's own, sourced directly from the live templates.

Two non-PrimeIcons sources also appear inside the grid. The first is **PrimeNG's own inline `<svg>` icon set**, injected at runtime with **no source line anywhere in `src/FE/`** — a grep for `pi-` misses it entirely, because these exist by switching a component on rather than by writing an icon. Two of the five reach this screen: the four paginator arrows, from `[paginator]="true"` (`user-grid-table.html:6`), and the loading spinner, from `[loading]` — bound by the page and forwarded into the grid (`quan-tri-nguoi-dung.page.html:16` → `user-grid-table.html:4`), so the two binding sites produce one spinner, not two. PrimeNG's sort and filter icons do **not** render here: no template uses `pSortableColumn`, `[sortField]` or `[filters]`. Full enumeration, plus the missing `aria-hidden` and the English paginator labels, is in `Icons.md` § Per-Action Map (rows marked *PrimeNG SVG*) and § Normalize on redesign #7-9. The second is the status badge's literal `●` (U+25CF) text glyph, which is not an icon element at all (`Icons.md` § Legacy Exceptions).

| Action | Icon | Placement |
| --- | --- | --- |
| Search users | `pi pi-search` (PrimeIcons, `--muted`, `font-size:13px`) | Absolutely positioned inside the search field, `left:10px`, vertically centred (`quan-tri-nguoi-dung.page.html:9`, `quan-tri-nguoi-dung.page.scss:11-18`) |
| Create user | — (text button `+ Thêm người dùng`, the `+` is a literal character) | Card title row, right side (`quan-tri-nguoi-dung.page.html:4`) |
| Edit user | `pi pi-pencil` (PrimeIcons, `--muted`) | Row actions cell, first `.action-btn` (`user-grid-table.html:50-52`) |
| Lock account | `pi pi-lock` (PrimeIcons, `--bad` via `.action-btn.danger`) | Row actions cell, second `.action-btn`, shown while the account is unlocked (`user-grid-table.html:67`) |
| Unlock account | `pi pi-lock-open` (PrimeIcons, `--muted` — the `.danger` class is not applied when locked) | Same button, shown while the account is locked (`user-grid-table.html:56,67`) |
| Account status | `●` (U+25CF literal glyph, inherits the badge's `--good` / `--bad` text colour) | Inline prefix inside the status `Badge` text (`user-grid-table.html:42,44`) |
| Paginate (first/prev/next/last) | PrimeNG inline `<svg>`, **not** PrimeIcons — `data-p-icon="angle-double-left"`, `"angle-left"`, `"angle-right"`, `"angle-double-right"` | Paginator below the grid; injected by `p-table` because `[paginator]="true"` (`user-grid-table.html:6-11`), no `src/FE/` source line |
| Grid loading | PrimeNG inline `<svg>` spinner, **not** PrimeIcons — `data-p-icon="spinner"`, spinning | Centred in the `.p-datatable-mask` overlay while `[loading]` is true (`quan-tri-nguoi-dung.page.html:16` → `user-grid-table.html:4`), no `src/FE/` source line |
| Close dialog / cancel / save | — (text buttons `Đóng`, `Huỷ`, `Lưu`) | Dialog title row and `.dialog-actions` (`user-form-dialog.html:4,68-69`) |
| Open nav drawer (shell) | `pi pi-bars` (PrimeIcons) | Topbar left, ≤980px only (`topbar.html:11`) |
| Log out (shell) | `pi pi-sign-out` (PrimeIcons) | Topbar right, inline before the "Đăng xuất" label (`topbar.html:19`) |
| Dismiss toast (shell) | `pi pi-times` (PrimeIcons, `--muted`) | Right edge of each toast item (`toast.html:11`) |

### Screenshots

<!-- Refs into Assets/Screenshots/quan-tri-nguoi-dung/ (flow stem, matching the existing Assets/Screenshots/dashboard/ convention) -->

**✅ The desktop shot exists — captured 2026-08-22 from the live Angular app.** That is the full target under `doc/Design/CLAUDE.md` § Rules (ONE desktop shot per screen, decided 2026-08-22). Every remaining row below is an **on-demand** state/viewport variant, *not* an outstanding debt: capture one when someone actually needs that case, and flip its status then. All of them need both servers running and an authenticated `Admin` (or `SuperAdmin`) session; the instructions are reproducible as written.

**Common prerequisites for every row:** (1) start the API — `dotnet run --project src/BE/PlatformManager.Api` — and confirm it listens on `http://localhost:5027` (the dev API base URL in `src/FE/src/environments/environment.development.ts`); (2) start the app — `npm start` in `src/FE/` — and open `http://localhost:4200`; (3) sign in at `/dang-nhap` as a user holding `Admin` or `SuperAdmin`, otherwise `adminGuard` redirects to `/dashboard`; (4) navigate to `http://localhost:4200/quan-tri/nguoi-dung`.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png` | captured 2026-08-22 | Live app, `/quan-tri/nguoi-dung`, full page, sidebar expanded. Captured against a freshly seeded database, so the grid holds the **single bootstrap account** rather than a full page — and it therefore also documents the **self-lock guard**: that row's lock button renders `disabled` with `title="Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất"`. Environment as recorded in `UiInventory.md` § Screenshot Manifest (API on `:5027`, FE served with `npx ng serve --port 4201`). |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--populated--desktop-1440.png` | on demand | @ 1440×1000 with at least 12 seeded users, so the paginator shows more than one page and both status badges appear. The most useful next shot, since the captured one holds a single row. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--tablet-900.png` | on demand | Same populated state @ 900×1000 — exercises `@media (max-width:980px)`: sidebar off-canvas, hamburger visible, card full width. Capture with the drawer **closed**. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--mobile-390.png` | on demand | Same populated state @ 390×900 — exercises `@media (max-width:560px)`: `main` padding 10px, topbar user-name hidden. Scroll the grid fully left before capturing so the "Người dùng" column is visible. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--empty--desktop-1440.png` | on demand | @ 1440×1000, type a string that matches no user (e.g. `zzzz`) into the search field, wait past the 300 ms debounce and the request, then capture the `Không có người dùng nào khớp bộ lọc.` row. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--loading--desktop-1440.png` | on demand | @ 1440×1000, throttle the network in DevTools (e.g. "Slow 3G"), trigger a refetch by changing the page, and capture while the `.p-datatable-mask` spinner overlay is visible. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--self-lock-disabled--desktop-1440.png` | on demand | Only the **hover** half of this state is missing — the disabled (`opacity:.45`) lock button is already visible in the captured shot above. @ 1440×1000, hover the signed-in admin's own lock button and capture with the `Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất` tooltip showing. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-form-dialog--create--desktop-1440.png` | on demand | @ 1440×1000, click "+ Thêm người dùng" and capture the empty dialog with backdrop — all 5 rows visible, username field focused. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-form-dialog--edit--desktop-1440.png` | on demand | @ 1440×1000, click the "Sửa" action on a plain `User`/`Admin` row and capture the pre-filled dialog — note the username and temp-password rows are absent. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-form-dialog--edit-preserved-roles--desktop-1440.png` | on demand | @ 1440×1000, click "Sửa" on a user holding `SuperAdmin` and capture the dialog showing the `Vai trò hệ thống: SuperAdmin — giữ nguyên, không thay đổi khi lưu.` note under the (Admin/User-only) checkbox row. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-form-dialog--validation-error--desktop-1440.png` | on demand | @ 1440×1000, open the create dialog, clear every field and click "Lưu" — capture the single `Email bắt buộc.` message in the `.form-error` block. |
| `Assets/Screenshots/quan-tri-nguoi-dung/user-list--toast-success--desktop-1440.png` | on demand | @ 1440×1000, unlock any locked account and capture within 5 s (the auto-dismiss window) so the `Đã mở khoá tài khoản.` toast is visible bottom-right. |

<!-- The captured desktop row is already recorded in UiInventory.md → Screenshot Manifest. On-demand rows stay here, next to the layout they belong to, and are only added to the manifest if one is actually captured. -->

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

- **No busy state on the dialog's save path** — "Lưu" and "Huỷ" stay enabled while a create/update request is in flight (`quan-tri-nguoi-dung.page.ts:137-161`), so a double-click can fire two `POST /users` requests and create two accounts. Disable both buttons (or show an inline spinner) between submit and response.
- **A failed save shows its message twice** — once in the dialog's `.form-error` block and once as an interceptor toast. Pick one channel for form-scoped errors: either mark form requests with `SKIP_ERROR_TOAST` and keep the inline message, or drop the inline block and rely on the toast.
- **No error state for a failed list fetch** — the error callback only clears `loading` (`quan-tri-nguoi-dung.page.ts:83`), leaving the previous rows on screen with no indication they are stale and no retry affordance. Add an inline error/retry row inside the grid area.
- **No confirmation before locking an account** — a single click on the lock button locks another user immediately, and locking terminates that user's session within 30 minutes (`doc/contracts/users.md:164`). Add a confirm step for lock (unlock can stay one-click; it is non-destructive).
- **Validation is one message at a time, detached from the fields** — the form checks five rules in sequence and renders only the first failure in a shared block at the bottom (`user-form-dialog.ts:137-179`); no field is marked invalid and there is no per-field message. Move to per-field validation state so a user fixing three empty fields does not have to submit three times.
- **The temporary password is rendered in a `type="text"` field** (`user-form-dialog.html:35`) — readable by anyone looking at the admin's screen. It is plausible this was deliberate (the admin must read the value back to the new user), but it should be an explicit choice: either a masked field with a reveal toggle (the pattern already shipped in `.field-input .toggle-visibility`, `styles.scss:548-568`), or a generate-and-copy control.
- **Untokenized literal values inside this screen's SCSS** — `.avatar` `30px`, `.role-tag` `border-radius:6px` / `padding:2px 8px`, `.row-actions` `gap:6px` (equal to `--sp-2`), `.dialog-actions` `gap:8px` / `margin-top:8px` (equal to `--sp-3`), `.search` `min-width:220px` / `padding-left:32px`, and the five `<th>` inline `style="min-width:…"` attributes. Each is a hardcoded value alongside an existing token set; route them through `--sp-*` / `--radius-*` (and add the missing sizing tokens) so the grid's column geometry is themeable rather than baked into the template.
- **`.tablewrap` means something different here than in `Components/Table.md`** — the documented component is `overflow:auto` with a `--line` border, while this screen re-declares it as `overflow:hidden` with a `--border-strong` border (`user-grid-table.scss:1-5`), delegating scrolling to PrimeNG's inner container. Converge on one `.tablewrap` contract, or rename the local one so the two are not confused.
- **The document title never changes** — `<title>PlatformManager</title>` is static (`src/FE/src/index.html:5`) even though the route already carries `title: 'Người dùng hệ thống'` and the shell renders it in the topbar. Wire the route title into `Title`/`TitleStrategy` so tabs and browser history are distinguishable.
- **Divergences from the deleted design-intent prototype** (recorded here only because the decision is still open — the prototype itself is gone): the prototype's three sortable column headers (`Người dùng ▾` / `Vai trò ▾` / `Ngày tạo ▾`, lines 246-249) ship **without sorting**, and its hand-rolled `‹ / ›` pager (lines 264-266) was replaced by the PrimeNG paginator with a rows-per-page dropdown. Decide deliberately whether column sorting is still wanted — if it is, it needs a server-side `sortBy`/`sortDir` contract, which `doc/contracts/users.md` does not currently define.
