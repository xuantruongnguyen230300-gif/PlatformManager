---
kind: lich-su
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "historical — deletion decided 2026-09-05 (Q29), code not yet removed"
updated: "2026-09-08"
flow: "Home"
screens: ["Trang chủ"]
source_routes: []          # /trang-chu now belongs to Screens/01-dashboard.md (Q18)
---

# Home (Trang chủ) — Screens

> # 🗄️ TÀI LIỆU LỊCH SỬ — 🚧 ĐÃ CHỐT XOÁ 2026-09-05, CODE CHƯA GỠ
>
> **Đây không còn là màn hình đích.** Quyết định **Q29** (2026-09-05): xoá hẳn
> `src/FE/src/app/platform/trang-chu/`. URL `/trang-chu` từ nay do **DTI Dashboard**
> chiếm chỗ (quyết định Q18) — nguồn sống là [`01-dashboard.md`](./01-dashboard.md).
>
> Giữ file theo `.claude/CLAUDE.md` §5: màn hình này **đã chạy thật**, và phần dưới là
> bản ghi trung thực của nó. Không xoá file, cũng không sửa nội dung cho khớp thiết kế
> mới — muốn biết `/trang-chu` hôm nay trông thế nào thì đọc file kia.
>
> **Code vẫn còn ở thời điểm dán banner này**, nên đây là `🚧 ĐÃ CHỐT — ĐANG THI CÔNG`
> chứ không phải mô tả cây thư mục hiện tại. Tự kiểm:
>
> ```bash
> ls src/FE/src/app/platform/trang-chu          # còn thư mục = việc xoá chưa chạy
> grep -n 'trang-chu' src/FE/src/app/app.routes.ts
> ```
>
> | Trong file này → | Thực tế sau khi Q29 thi hành |
> | --- | --- |
> | `/trang-chu` render lời chào + tóm tắt tài khoản | `/trang-chu` render **DTI Dashboard** — [`01-dashboard.md`](./01-dashboard.md) |
> | `NoticeBanner` *"Chưa có module nghiệp vụ nào được cài đặt…"* | Không còn — module DTI chính là thứ câu đó nói là chưa có |
> | Khối `.facts` (tên đăng nhập, email, vai trò) + nút đổi mật khẩu | **Không chuyển đi đâu cả** — xoá cùng màn hình. `/doi-mat-khau` chỉ còn vào được từ menu sidebar |
> | `.lead` và `.facts` là layout riêng của trang | Xoá cùng `trang-chu.page.scss` |
> | Mọi trích dẫn `trang-chu.page.{html,ts,scss}` bên dưới | Trỏ tới file **sẽ bị xoá**. Giữ nguyên số dòng: chúng mô tả bản code có thật hôm dán banner, neo lại theo cây thư mục sau khi xoá thì thành bịa |
>
> **Ba thứ KHÔNG đổi**, và đó là chủ ý của Q18: URL `/trang-chu`; hằng
> `APP_CORE_ROUTES.home` (`src/FE/src/app/core/config/core-routes.ts` § `home`); hàng
> menu seed. Đây là **thay ruột tại chỗ**, không phải đổi địa chỉ — nên `''`, `**` và
> nhánh dự phòng của `roleGuard` đều không phải sửa.
>
> ⚠️ **Hệ quả đã được chấp nhận khi chốt Q29: sản phẩm thứ hai dùng lại CoreBase sẽ
> KHÔNG có trang chủ.** Đây là màn landing duy nhất ở tầng Core, còn Dashboard thay nó
> là code **nghiệp vụ** và không đi theo CoreBase. Sản phẩm sau phải tự viết màn landing
> của mình.
>
> 🔄 **SỬA 2026-09-06 — cái bẫy của dòng "giữ nguyên số dòng" đã bật, sớm hơn dự kiến.**
> Câu đó viết cho tình huống thư mục **đã bị xoá**. Nhưng thư mục vẫn còn, và
> `trang-chu.page.html` đã được **sửa sau ngày dán banner** để bọc chuỗi qua i18n — nên mọi
> số dòng cũ vẫn "phân giải được", chỉ là trỏ ra nội dung khác. Đây đúng là ca mà
> `doc/Design/CLAUDE.md` § "Ngoại lệ: file mang banner TÀI LIỆU LỊCH SỬ" mô tả: file lịch sử
> mà nguồn nó trích **vẫn tồn tại** thì số dòng cũ nguy hiểm hơn số dòng chết, vì không cổng
> nào báo. § Copy bên dưới đã được đối chiếu lại và viết lại theo mã nguồn hôm nay; các mục
> khác giữ nguyên số dòng của bản 2026-09-05 và **không được tin** nếu không tự `grep` lại.
>
> Khi thư mục bị xoá thật, `bash .claude/check-docs.sh` §4 sẽ báo mọi trích dẫn
> `src/FE/src/app/platform/trang-chu/...` trong file này. Đó là **tín hiệu đúng**: xử lý
> bằng cách dán nhãn "đã xoá" cho từng dòng, không phải bằng cách gỡ bằng chứng đi cho
> cổng xanh.

`/trang-chu` is the app's landing route and its universal safe harbour: `''` and
the `**` wildcard both redirect here, `roleGuard` sends a user who lacks a role
here instead of showing a 403, and the change-password screen returns here after
a successful save. Because everything falls back to it, the route carries **no**
role guard at all — only `authGuard` + `mustChangePasswordGuard` — since a
guarded fallback would be a redirect loop. Today it does exactly three things:
greet the signed-in user, summarise their account, and point them at the
server-driven menu. There is no business module to link to — the only one
(`DtiWeekly`) was removed on 2026-08-29 to be rebuilt — and the screen says so
in a `NoticeBanner` rather than pretending the app is fuller than it is.

> ⚠️ **This screen has no approved prototype design.** Every other screen in this
> folder can be compared against `../Prototypes/index.html`; this one cannot —
> it was introduced on 2026-08-29, after that prototype was drawn, and there is
> no artboard for it. It was assembled entirely from controls that already
> existed in the shared library (`.notice`, `.card`, `.title`, `.btn`, `.badge`),
> which is the constraint recorded in its own source header
> (`src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.ts:13-15`).
> Treat the sections below as a record of what shipped, **not** as a design that
> passed a design review. When the first business module returns, this page is
> the intended place for its entry point — or it is replaced outright by a
> dashboard; that decision is open and is recorded under § Normalize on redesign.

> **Shell:** app shell — `Sidebar` + `Topbar` + `main` + `Toast` (`app.html:1-14`),
> rendered because the route does not set `data.noShell` (`trang-chu.routes.ts:11-18`).
> `DESIGN.md` → Layout describes this shell.
> **Sources:** `src/FE/src/app/platform/trang-chu/` (`trang-chu.routes.ts`,
> `pages/trang-chu/trang-chu.page.{html,ts,scss}`), `src/FE/src/app/app.html`,
> `src/FE/src/app/app.scss`, `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`,
> `src/FE/src/styles.scss`, `src/FE/src/app/core/auth/{current-user.service.ts,role.guard.ts,must-change-password.guard.ts}`,
> `doc/contracts/meta-menu.md`.
> **Token vocabulary:** token names are the live CSS custom properties in
> `src/FE/src/styles.scss` § `:root` minus the `--` prefix. Values quoted with no
> token name are literals in the shipped SCSS, recorded as as-shipped facts.

---

## Trang chủ (`/trang-chu`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:1-14`) — surrounds the route; not part of its own template
  - `Sidebar` (`sidebar.html:4-85`) — fixed left, `--sidebar-w` / `--sidebar-w-collapsed`, `z-index:35`, fill `card`, right border `line` (`sidebar.scss:3-20`). This screen's own entry is the seeded top-level `SysMenu` row `Trang chủ` with icon `pi-home` and **no role assignment**, so it is visible to every signed-in user (`AppMenuSeedSource.cs:44-45`)
  - `.shell-content` — `margin-left: var(--sidebar-w)` (or `--sidebar-w-collapsed`), flex column (`app.scss:11-22`)
    - `Topbar` (`topbar.html:1-24`) — sticky, `z-index:20`, `rgba(255,255,255,.95)` + `backdrop-filter: blur(10px)`, bottom border `line`; inner `.topin` capped at `--container-max-width`, padding `--sp-4` `--sp-5` (`topbar.scss:1-17`). The `<h1>` prints the route-level `title` relayed by `PageTitleStrategy`
    - `main` — max-width `--container-max-width`, `margin:auto`, padding `--sp-5`, flex column (`app.scss:24-37`). The route template's two regions are its direct children; **there is no page wrapper element and the page does not use `.page-fill`**
  - `Toast` (`toast.html:1-23`) — fixed bottom-right, offsets `--sp-5`, `z-index:60`, `aria-live="polite"`. Nothing on this screen raises one; it is present because `<app-toast />` sits outside the shell conditional
- **`NoticeBanner`** (`.notice`, default/info severity — `trang-chu.page.html:4-7`) — the first region. Flex row, `align-items:flex-start`, gap `--sp-3`, fill `tonal-bg`, 1px `line` border, 4px left border in `brand`, padding `--sp-3` `--sp-5`, radius `--radius-md`, `--fs-sm`, `margin-bottom: --sp-5` (`styles.scss` § `.notice`). Leading `pi pi-info-circle` glyph in `brand` (`styles.scss` § `.notice > .pi`) + one `<span>` of text. It carries **no** dismiss button and **no** `aria-live` — the message is static page content, not an event
- **`Card`** (`.card` — `trang-chu.page.html:9-42`) — the second region and the whole page body: fill `card`, 1px `line` border, `--shadow`, radius `--radius-lg`, padding `--sp-5` (`styles.scss` § `.card`)
  - `.title` row (flex, space-between, gap `--sp-3`, margin-bottom `--sp-4` — `styles.scss` § `.title`; the global card-title row documented under `Components/Card.md` § Anatomy)
    - `<h2>` — the greeting, `--fs-md` (`trang-chu.page.html:11`)
    - `Button` (default tonal variant) — **rendered as an `<a class="btn" routerLink="/doi-mat-khau">`, not a `<button>`**. `.btn` sets `text-decoration:none` precisely so this anchor-as-button does not render as a broken link (`styles.scss` § `.btn`). Leading `pi pi-key` glyph + label (`trang-chu.page.html:14`)
  - `<p class="lead">` — the intro paragraph. Page-local layout only: `margin:0`, colour `muted`, `line-height:1.6` (`trang-chu.page.scss:4-8`). It is deliberately **not** the global `.muted` utility, which is sized `--fs-xs` for inline table text
  - `<dl class="facts">` — the account summary, rendered only when `user()` is non-null (`@if`, `trang-chu.page.html:22-41`). Page-local layout, **not a component**: a two-column grid `max-content 1fr`, gap `--sp-2` `--sp-5`, `margin-top: --sp-5`, `--fs-sm`, `align-items:center` (`trang-chu.page.scss:12-19`). `dt` is `muted`; `dd` is `margin:0`, weight 600 (`trang-chu.page.scss:21-28`)
    - Row 1 — `Tên đăng nhập` / `{{ u.UserName }}`
    - Row 2 — `Email` / `{{ u.Email ?? '—' }}`; the API type is nullable, so a missing address renders an em dash rather than an empty cell (`trang-chu.page.html:28-30`)
    - Row 3 — `Vai trò` / `dd.roles`, a wrapping flex row with `--sp-2` gap (`trang-chu.page.scss:30-34`) holding one `Badge` per role string. 🔄 **SỬA 2026-09-06:** the previous revision said `.badge.neutral`. The template renders **`.badge.outline`** — the *identity* variant (transparent fill, `text` ink, 1px `line` border, `--radius-sm`, weight 700), chosen because a role name states what something *is* rather than how it is *going*, so it must not borrow a semantic colour. Shared size rules still come from `.badge` (`styles.scss` § `.badge` / `.badge.outline`). `@empty` → a single `<span class="muted">Chưa gán vai trò</span>` (`trang-chu.page.html:34-38`)

<!-- Component gap — reviewed 2026-08-29. Every region composes from an indexed spec:
       NoticeBanner -> Components/NoticeBanner.md   Card    -> Components/Card.md
       Button       -> Components/Button.md         Badge   -> Components/Badge.md
       Sidebar/Topbar/Toast -> their own specs.
     Genuinely page-local and deliberately NOT promoted to specs: `.lead` (3
     declarations) and `.facts` (a definition-list grid, 1 instance, no states,
     no variants). Recorded here as plain markup rather than invented into the
     library — the same treatment `04-phan-quyen.md` gives its `<p class="muted">`
     helper line. Promote `.facts` only if a second key/value summary appears. -->

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

> 🔄 **SỬA 2026-09-06 — this section previously opened with "No i18n layer exists" and filled
> every Localization-key cell with `— (hardcoded)`. That is false of the running code.** An i18n
> runtime shipped: `@ngx-translate/core`, bundles at `src/FE/public/i18n/{vi,en}.json`, and gate
> **G12** in `scripts/fe-gate.sh` now fails on any Vietnamese diacritic left in a template. Not one
> user-facing string on this screen is a template literal any more, the route `title` included.
> The table below carries the real keys, read out of the bundle; the copy column is the `vi`
> value, which is the source language.

Keys resolve against `src/FE/public/i18n/vi.json`; the `en` bundle carries the same key set.

| Element | Verbatim copy (`vi`) | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `Trang chủ · PlatformManager` | `trang-chu.routeTitle` + the product name | `trang-chu.routes.ts` § `title`, joined by `TITLE_SEPARATOR` in `core/title/page-title.strategy.ts` |
| Topbar heading (route title) | `Trang chủ` | `trang-chu.routeTitle` | `trang-chu.routes.ts` § `title`, rendered by `topbar.html` § `.logo h1` |
| Notice banner | `Chưa có module nghiệp vụ nào được cài đặt. Dùng menu bên trái để vào các chức năng quản trị hệ thống.` | `trang-chu.hint.noModules` | `trang-chu.page.html` § `.notice` |
| Card heading — with a known full name | `Xin chào, {{name}}` | `trang-chu.greetingNamed`, parameter `name` = `fullName()` | `trang-chu.page.html` § `.title h2` |
| Card heading — no full name | `Xin chào` | `trang-chu.greeting` | idem — the template only chooses which key to pass |
| Change-password link/button | `Đổi mật khẩu` | `shared.action.changePassword` — a **promoted** key, shared with the change-password screen | `trang-chu.page.html` § `.btn` |
| Intro paragraph | `Đây là trang chủ của {{name}}. Danh sách chức năng ở menu bên trái do máy chủ cấp theo quyền của tài khoản, nên hai người dùng khác vai trò sẽ thấy hai menu khác nhau.` | `trang-chu.hint.intro`; the `name` parameter is `CORE_BRANDING.name` and is **not** part of the string | `trang-chu.page.html` § `.lead` → `trang-chu.page.ts` § `branding` → `app.config.ts` § `APP_BRANDING` |
| Fact label 1 | `Tên đăng nhập` | `shared.field.userName` — promoted; shared with the sign-in and user-admin screens | `trang-chu.page.html` § `.facts` |
| Fact label 2 | `Email` | `shared.field.email` | idem |
| Missing-email fallback | `—` (em dash) | none — a template literal, and correctly so: punctuation, not prose | `trang-chu.page.html` § `u.Email ?? '—'` |
| Fact label 3 | `Vai trò` | `shared.field.role` | `trang-chu.page.html` § `.facts` |
| Role badge text (dynamic) | `{{ role }}` — API value, never re-cased or translated by the FE; seeded roles are `SuperAdmin` / `Admin` / `User` | none — server value | `trang-chu.page.html` § `.badge.outline` |
| No-roles fallback | `Chưa gán vai trò` | `trang-chu.hint.noRoles` | `trang-chu.page.html` § `@empty` |
| Sidebar entry for this screen | `Trang chủ` | none — server value, `SysMenu.Name` | `AppMenuSeedSource.cs` § `Code: "trang-chu"`, rendered by `sidebar.html` |
| Sidebar brand mark / text (shell) | `PM` / `PlatformManager` | none — injected via `CORE_BRANDING` | `sidebar.html` § `.brand-mark` / `.brand-text` → `app.config.ts` § `APP_BRANDING` |
| Topbar logout button + `title` (shell) | `Đăng xuất` | `shared.topbar.logout` | `topbar.html` |
| Topbar hamburger `aria-label` (shell) | `Mở menu điều hướng` | `shared.topbar.openMenu` | `topbar.html` |
| Skip link (shell) | `Bỏ qua điều hướng, tới nội dung chính` | `shared.app.skipToContent` | `app.html` |

Copy notes, as shipped: the greeting is **two complete sentences under two keys**, not one
sentence with a conditional comma — an account with no `FullName` reads `Xin chào` because
`trang-chu.greeting` says exactly that, and the comma in `greetingNamed` belongs to the
translator. The notice sentence is still the only place in the shipped UI that admits the app
currently has no business module.

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **default (the only populated state):** the component reads two signals off
  `CurrentUserService` and renders synchronously — there is **no request of its
  own**, no `loading` flag, no spinner and no skeleton anywhere on this screen
  (`trang-chu.page.ts:26-36`). The session was already resolved by the bootstrap
  `GET /api/auth/me` probe before any route rendered, so by the time this page
  paints the data is in memory.
- **loading:** does not exist, and cannot. See above — nothing on this screen
  fetches.
- **empty — no roles:** the `@for` over `u.Roles` carries an `@empty` branch that
  renders `Chưa gán vai trò` in `muted` instead of an empty cell
  (`trang-chu.page.html:36-38`). Reachable in practice: the seeded `Trang chủ`
  menu row has no `SysMenuRole`, so a user with zero roles can still sign in and
  land here.
- **empty — no email:** `u.Email` is nullable on the API model, and the template
  renders `—` for it, deliberately distinguishing "this account has no email"
  from "this block failed to render" (`trang-chu.page.html:28-30`).
- **empty — whole card:** the `<dl class="facts">` block is wrapped in
  `@if (user(); as u)`, so if the current user were `null` the greeting, the
  intro paragraph and the change-password button would still render and only the
  fact list would vanish. `authGuard` makes that unreachable in practice; the
  `@if` exists because the signal's type is `ICurrentUser | null` and the guard
  cannot prove otherwise at compile time (`trang-chu.page.ts:31-36`).
- **error:** no error state exists on this screen and none can be produced by it
  — it issues no request, so `httpErrorInterceptor` has nothing to toast here.
  An error raised by a *previous* screen can still be on-screen in the shell's
  toast stack while this page paints.
- **validation:** none. The screen has no input of any kind — the single
  interactive control is the `Đổi mật khẩu` link.
- **access:** the route is reachable by **every** signed-in account. It carries
  `authGuard` + `mustChangePasswordGuard` and deliberately no role guard,
  because `roleGuard` redirects here when a user lacks a role and a guarded
  fallback would loop (`trang-chu.routes.ts:5-17`, `role.guard.ts:9,23`). An
  anonymous visitor is sent to `/dang-nhap?returnUrl=/trang-chu`; a user still
  carrying `mustChangePassword` is sent to `/doi-mat-khau`.

### Responsive

<!-- Behavior per breakpoint. -->

- **The screen owns exactly one media query.** `trang-chu.page.scss` declares a
  single `@media (max-width: 560px)` block (`trang-chu.page.scss:37-46`); every
  other breakpoint effect is inherited from the shell (`app.scss`, `topbar.scss`,
  `sidebar.scss`) or from global rules in `styles.scss`.
- **≥981px (desktop default):** sidebar fixed at `--sidebar-w` (or
  `--sidebar-w-collapsed`), `.shell-content` offset to match with a 0.2 s
  transition, `main` centred at `--container-max-width` with `--sp-5` padding
  (`app.scss:11-37`). The card is full-width inside `main` — the page has no
  max-width of its own, so on a wide monitor the two-column `.facts` grid leaves
  a large empty right-hand column.
- **≤980px (tablet):** `.shell-content { margin-left: 0 !important }` and the
  sidebar becomes an off-canvas drawer opened by the topbar hamburger, which is
  `display:none` above this width (`app.scss:39-43`, `sidebar.scss:219`,
  `topbar.scss:45-56`). Nothing inside the card changes; it simply reflows wider.
- **≤560px (mobile):** two things change. `main` padding drops to `10px`
  (raw literal, `app.scss:45-49`) and the topbar hides the user's name
  (`topbar.scss:39-43`). Inside the card, the `.facts` grid drops from
  `max-content 1fr` to a single column with a `2px` row gap, and each `dd`
  except the last gains `margin-bottom: var(--sp-4)` so label/value pairs stay
  visually grouped when stacked (`trang-chu.page.scss:37-46`). The reason is
  recorded in the source: at that width the `max-content 1fr` template squeezes
  long values into a very narrow column.
- **Toast overlay (all viewports):** `max-width: min(400px, 90vw)` with fixed
  `--sp-5` offsets (`toast.scss:4-13`).
- **Print (`@media print`):** the sidebar, the topbar, the toast stack and every
  `.no-print` element disappear; `.shell-content` loses its margin and `main`
  loses its max-width (`app.scss:51-58`, `styles.scss` § `@media print`). The notice banner
  and the card print as rendered — neither carries `.no-print`, which is
  arguably wrong for the notice (see § Normalize on redesign).

### Iconography

See `Icons.md` § Per-Action Map. The shipped app loads **PrimeIcons v7**
globally and authors icons as `<i class="pi pi-*">` elements.

This screen's own template contains exactly **two** icons; everything else in
the table below belongs to the app shell. No PrimeNG inline-SVG icon reaches
this screen — there is no `p-table`, so no paginator arrows and no loading
spinner.

| Action | Icon | Placement |
| --- | --- | --- |
| Notice severity marker | `pi pi-info-circle` (`brand`, `flex:none`, `margin-top:2px`) | Leading, inside `.notice` (`trang-chu.page.html:5`, `styles.scss` § `.notice > .pi`) |
| Go to change password | `pi pi-key` | Leading, inside the `.btn` anchor in the card title row (`trang-chu.page.html:14`) |
| Sidebar entry for this screen | `pi pi-home` (from `SysMenu.Icon`, a seeded DB value — it can change without an FE deploy) | Shell sidebar, top-level leaf (`AppMenuSeedSource.cs:44`, rendered `sidebar.html:67`) |
| Collapse / expand the sidebar | `pi pi-angle-left` | Shell sidebar brand row, `.icon-btn.sidebar-toggle` (`sidebar.html:20`) |
| Open the navigation drawer (≤980px) | `pi pi-bars` | Shell topbar, left (`topbar.html:11`) |
| Sign out | `pi pi-sign-out` | Shell topbar, right, before the `Đăng xuất` label (`topbar.html:19`) |
| Dismiss a toast | `pi pi-times` | Shell toast item, right (`toast.html:19`) |

### Screenshots

<!-- Refs into Assets/Screenshots/trang-chu/ -->

**None exist — this screen has never been captured.** It was added on 2026-08-29,
after the 2026-08-22 capture pass, so `Assets/Screenshots/` contains no folder
for it. Under `doc/Design/CLAUDE.md` § Rules the target is **one desktop shot per
screen**, so exactly one row below is a real gap; the rest are on-demand
variants.

Common prerequisites for every row:

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`doc/Design/CLAUDE.md`](../../../CLAUDE.md) § Rules

1. Sign in at `/dang-nhap` with any seeded account — the route has no role gate.
   Credentials are deliberately not recorded in this spec.
2. Navigate to `/trang-chu` (or simply to `/`, which redirects here).

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/trang-chu/trang-chu--desktop-1440.png` | **missing — this is the one real gap** | `/trang-chu` @ 1440×900, full page, sidebar expanded, signed in as an account that has both a full name and at least one role, so the greeting and the role badges are both populated. |
| `Assets/Screenshots/trang-chu/trang-chu--no-roles--desktop-1440.png` | on demand | Same @ 1440×900 signed in as an account with zero roles — documents the `Chưa gán vai trò` fallback and the reduced sidebar (only the `Trang chủ` row, which has no `SysMenuRole`). |
| `Assets/Screenshots/trang-chu/trang-chu--tablet-900.png` | on demand | Same as row 1 @ 900×1200 — below 980px, so the sidebar is an off-canvas drawer and the topbar shows the hamburger. Capture with the drawer **closed**. |
| `Assets/Screenshots/trang-chu/trang-chu--mobile-390.png` | on demand | Same as row 1 @ 390×844 — below 560px: `main` padding 10px, topbar user name hidden, and the `.facts` list stacked to one column. This is the only shot that shows the screen's own media query. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. A quirk that spans components belongs in the component's own spec (`Components/<Name>.md` → Normalize on redesign), not here. -->

1. **This screen never went through design.** It was assembled from existing
   shared classes on 2026-08-29 with no artboard in `../Prototypes/index.html`
   and no prompt pack. Everything above is a record of code, not of an approved
   design. Before it is treated as settled, run it through the normal pipeline —
   at minimum a Stitch/Claude Design pass from `../DESIGN.md` — and decide the
   question the source header leaves open: is the landing route a welcome page
   with the first module's entry point on it, or is it replaced by a dashboard?
   **Answered 2026-09-05 by decision Q29: replaced by a dashboard, and this screen
   deleted rather than kept.** The item stays because the reason it was asked is
   worth keeping — a screen that never went through design was the thing that got
   replaced first.
2. ~~**The greeting builds its own punctuation.**~~ — 🔄 **SỬA 2026-09-06: closed in the source,
   by the second of the two routes this item proposed.** The i18n pass gave the no-name case its
   own copy: `trang-chu.greeting` and `trang-chu.greetingNamed` are two complete sentences, and
   the template only chooses between the keys. No punctuation is assembled anywhere.
3. **The account summary duplicates nothing and links to nothing.** Username,
   email and roles are shown read-only with no affordance to change any of them,
   even though `/quan-tri/nguoi-dung` can edit exactly those fields for an
   administrator. Either link the block to a real profile action or drop the
   fields the user cannot act on.
4. **The notice prints.** `.notice` carries no `.no-print` class
   (`trang-chu.page.html:4`), so "no business module is installed" appears on
   paper alongside the account summary. It is an interface message about the
   current state of the deployment, not content worth printing.
5. **The notice is not announced.** It has no `aria-live` and no
   `role="status"` (`trang-chu.page.html:4-7`). That is defensible today because
   the text is static page content rather than an event — but if this banner ever
   becomes conditional, it needs a live region.
6. **The card has no max width.** On a 2560px monitor the `max-content 1fr`
   `.facts` grid leaves the value column stretched across most of the screen
   with the label pinned far to its left. Cap the reading measure, or move the
   summary into a narrower sub-column.
7. **The only page-local layout is two ad-hoc class names.** `.lead` and
   `.facts` are declared in the page's own stylesheet
   (`trang-chu.page.scss:4-34`) and exist nowhere else. If a second screen needs
   a key/value summary, promote `.facts` into the shared layer before copying it
   — an identical hand-copied block in two stylesheets is exactly the drift the
   2026-08-29 stylesheet consolidation was undertaken to remove.
