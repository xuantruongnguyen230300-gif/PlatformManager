---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-08"
flow: "Permissions"
screens: ["Phân quyền"]
source_routes: ["/quan-tri/phan-quyen"]
---

# Permissions (Phân quyền) — Screens

One lazy-loaded route (`/quan-tri/phan-quyen`) gated by `authGuard` → `mustChangePasswordGuard` → `superAdminGuard` on the way in, and by **`unsavedChangesGuard` (`canDeactivate`) on the way out** (`phan-quyen.routes.ts` § `PHAN_QUYEN_ROUTES`). **Only** a `SuperAdmin` reaches it; every other signed-in user is redirected to `/trang-chu` with no 403 page and no message (`role.guard.ts:23,35`).

The page is **one card holding two mutually exclusive matrices behind a `SegmentedControl`**:

- **Theo màn hình** — contract PERM-1, writes `SysMenuRole`: which role *sees* which menu. Absence means **open to every signed-in user**.
- **Theo tài nguyên** — contract PERM-2, writes `RolePermission`: which role may *call* which action. Absence means **denied**.

The two defaults are opposites, which is why the screen ships two independent save buttons, two dirty flags and two endpoints rather than one "save all" (`phan-quyen.page.ts:16-25`).

**Rewritten 2026-08-29.** The previous revision of this spec described a tab-less, single-matrix card and instructed that no spec be written for the resource matrix until it was wired in. That condition is met: PERM-2 moved to **`Status: AGREED` (2026-08-29)** with both endpoints on `PermissionsController` and integration tests over real HTTP (`doc/contracts/permissions.md:75-80`), and `phan-quyen.page.ts` now imports and renders `ResourcePermissionMatrix` (`phan-quyen.page.ts:30`).

> 🚨 **The resource matrix controls nothing yet — and the screen says so out loud.** the permission-key catalogue holds exactly one key, `import.manage` (supplied by the host through `ICoreResourceKeySource`; the old `ResourceKeys` class in Core was **deleted 2026-09-03**), and **no product endpoint carries `[RequirePermission]`** — the only usage is a probe controller inside the integration-test project. Ticking a box writes a real row to `RolePermissions` and `GET` reads it back, but no user request is blocked or allowed by it. Verify rather than trust this paragraph: `grep -rn "RequirePermission(" --include=*.cs src/BE | grep -v /obj/ | grep -v /bin/`. The shipped `.notice` banner states the same fact to the user, verbatim in § Copy — do not design it away.

> **Shell:** app shell (sidebar + topbar + toast) — `src/FE/src/app/app.html`, `src/FE/src/app/app.scss`. `DESIGN.md` → Layout documents both of the app's shells; this route renders the main one — `Sidebar` at `dimension.sidebar-w` / `dimension.sidebar-w-collapsed` with `.shell-content` offset to match, a sticky `Topbar`, and a `main` capped at `dimension.container-max-width`.
> **Sources:** `src/FE/src/app/platform/phan-quyen/` (`phan-quyen.routes.ts`, `pages/phan-quyen/*`, `components/permission-matrix/*`, `components/resource-permission-matrix/*`, `services/*`, `models/phan-quyen.model.ts`), `src/FE/src/app/core/auth/role.guard.ts`, `src/FE/src/app/core/interceptors/http-error.interceptor.ts`, `src/FE/src/app/core/toast/toast.service.ts`, `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`, `src/FE/src/app/app.html`, `src/FE/src/app/app.scss`, `src/FE/src/styles.scss`, `doc/contracts/permissions.md`

---

## Phân quyền (`/quan-tri/phan-quyen`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:1-24`) — the route sets no `noShell` flag, so the full shell renders
  - **`Sidebar`** (`Components/Sidebar.md`; fixed left at `dimension.sidebar-w` / `dimension.sidebar-w-collapsed`, carrying `id="sidebar"` and `no-print`) — `sidebar.html:4-9`. This screen's own entry is the `SysMenu` row `Phân quyền` (`pi-shield`) under the group `Quản trị hệ thống` (`pi-cog`); both are database values seeded at `AppMenuSeedSource.cs:48,56`, not FE constants
  - **Shell content** (`.shell-content`, `margin-left` matching the sidebar width, 0.2 s transition) — `app.scss:37-48`
    - **`Topbar`** (`Components/Topbar.md`; sticky; inner `.topin` capped at `dimension.container-max-width`) — `topbar.html:1-25`. Its `<h1>` prints the route-level `title` `Phân quyền` relayed by `PageTitleStrategy` (`topbar.html:14`, `phan-quyen.routes.ts:12`)
    - **`main`** (capped at `dimension.container-max-width`, centred, padding `spacing.sp-5`) — `app.scss:50-71`
      - **`Card`** (`Components/Card.md`; `.card` — fill `colors.card`, border `colors.line`, `shadow.card`, `rounded.lg`, padding `spacing.sp-5`) — the route template's root and its only region (`phan-quyen.page.html:1,111`; `styles.scss` § `.card`)
        - **`SegmentedControl`** (`Components/SegmentedControl.md`; `.segmented.tabs` — `inline-flex`, border `colors.line`, `rounded.sm`, `overflow: hidden`) holding exactly two `.seg-btn`s, the chosen one carrying `.active` (fill `colors.brand`, ink `colors.on-primary`) — `phan-quyen.page.html:7-46`, `styles.scss` § `.segmented` / `.seg-btn` / `.seg-btn.active`
          - Semantics as shipped: `role="group"` + `aria-label` on the wrapper and `aria-pressed` on each segment — deliberately **not** `role="tablist"`/`role="tab"`, because the template authors declined to claim a tab pattern without the roving-tabindex and arrow-key handling it requires (`phan-quyen.page.html:2-7`)
          - `.tabs` is the page's own class and adds **only** `margin-bottom: spacing.sp-4`; it re-declares nothing about the switcher's shape (`phan-quyen.page.scss:13-15`)
          - Dirty marker: a `<span class="unsaved">` holding ` • ` plus the translated `phan-quyen.hint.unsaved`, appended **inside** the segment label when that tab has unsaved edits — real text, not an icon or a visually-hidden string, at `font-weight: 400` and `opacity: .85`, inheriting `currentColor` so it survives both the brand-filled active segment and the card-filled resting one (`phan-quyen.page.html` § `.unsaved`, `phan-quyen.page.scss` § `.unsaved`). 🔄 **SỬA 2026-09-06:** the previous revision quoted the literal string ` • chưa lưu`; only the bullet and its spaces are in the template now
        - **Tab A — `<section aria-label="Phân quyền theo màn hình">`**, rendered when `tab() === 'menu'` (`phan-quyen.page.html:48-67`)
          - Title row (`.title`, the global card-title row in `Components/Card.md` § Anatomy; flex, space-between, gap `spacing.sp-3`, margin-bottom `spacing.sp-4` — `styles.scss` § `.title`)
            - `h2` "Phân quyền màn hình" (`typography.h2-title`)
            - **`Button`** (`Components/Button.md`, Primary) — "Lưu thay đổi", `[disabled]="saving() || loading() || !dirty()"`, carrying `phan-quyen.hint.nothingToSave` as its `title` while disabled. 🔄 **SỬA 2026-09-06:** the previous revision recorded `[disabled]="saving() || loading()"` and built an argument on the gap — see the symmetry note under tab B
          - **`NoticeBanner`** (`.notice.warn`, `role="alert"`) — **new since the previous revision.** Rendered only when that tab's save was refused: a 409 write conflict, or a save attempted with no version token because the load had failed. It carries the message plus its own **`Button`** reading `Tải lại`, and it **stays on screen until the reload** — unlike the interceptor's toast, which auto-dismisses and offers no way forward. Present in **both** tabs, each behind its own signal, because the two matrices hold independent version tokens and a conflict on one says nothing about the other
          - `<p class="muted">` explainer — `colors.muted` at `typography.muted-caption` globally (`styles.scss` § `.muted`), plus the page's own `display: block; margin-bottom: spacing.sp-4` (`phan-quyen.page.scss` § `.muted`)
          - `app-permission-matrix` → **`Table`** (`Components/Table.md`) in its `.tablewrap.scroll` variant — a hand-rolled `<table>`, not PrimeNG (see below). Frame: border `colors.border-strong`, `rounded.table`, `overflow: auto`. 🔄 **SỬA 2026-09-06 — the height no longer comes from `dimension.grid-h`.** The page host now carries `class: 'page-fill'` and each matrix carries `class="grid-host"`, and `styles.scss` § `.page-fill .tablewrap.scroll` resets `max-height: none; min-height: 0` so the flex chain decides the height instead. `dimension.grid-h` is the fallback for a grid **outside** a `.page-fill` page, which this is not (`permission-matrix.html` § `.tablewrap.scroll`, `styles.scss` § `.page-fill`)
            - `<thead>` — `th` "Màn hình" with inline `style="width:40%"`, then one `th.num` per entry of `roles()`; the global `th` rule pins them `position: sticky; top: 0`, `z-index: 4`, fill `colors.surface-table-header`, ink `colors.th-ink`, `typography.table-header` (`permission-matrix.html:6-11`, `styles.scss` § `th`)
            - `<tbody>` — one `<tr>` per `displayRows()`, ordered **parent first, its children immediately after**, via `toDisplayOrder()` (`permission-matrix.ts:9-26,49`)
              - name cell: plain `<td>`, or `<td class="indent">` (`padding-left: 28px`, a raw literal) prefixed by a `└` glyph in `.tree-branch` (ink `colors.muted`, `margin-right: 4px`) for any non-root row (`permission-matrix.html:16-21`, `permission-matrix.scss:8-15`)
              - one `td.num` per role (right-aligned, tabular figures — `styles.scss` § `.num`) holding a **`Check`** (`Components/Check.md`; `.check`, `dimension.checkbox`, `accent-color: colors.brand`), `[disabled]="loading()"`, `aria-label` = `"<menu name> — <role>"` (`permission-matrix.html:24-31`, `styles.scss` § `.check`)
              - `@empty` → one `<tr>` with a `td.muted` spanning `roles().length + 1` (`permission-matrix.html:35-39`)
        - **Tab B — `<section aria-label="Phân quyền theo tài nguyên">`**, the `@else` branch (`phan-quyen.page.html:68-110`)
          - Title row, same `.title` contract
            - `h2` "Phân quyền theo tài nguyên"
            - **`Button`** (Primary) — "Lưu thay đổi", `[disabled]="resourceSaving() || resourceLoading() || !resourceDirty()"`, same `title` while disabled

              > 🔄 **SỬA 2026-09-06 — the asymmetry this spec described no longer exists, and one of the facts behind it was wrong even then.** The previous revision called `!resourceDirty()` *"deliberate and asymmetric with tab A"* and justified it with *"the backend accepts `entries: []`"*. Both halves are now false:
              >
              > - **The guard is symmetric.** Tab A's Save carries `!dirty()` as well. The source comment on that button records why it had to: if the opening `GET` fails, `rows()` is empty while the button stays enabled, so one reflex click overwrites the entire menu matrix with nothing. That is a *worse* outcome than tab B's, not a milder one — the argument for guarding tab B applied at least as strongly to tab A.
              > - **`entries: []` is refused.** The command validator returns 400 for a payload missing any `ResourceKey`, so the empty-`PUT` path is blocked server-side too. Both layers are kept on purpose: the server layer covers every client, the button layer stops the request before it leaves the browser so nobody receives a 400 for something they never meant to do.
              >
              > Verify rather than trust either version: `grep -n disabled src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html`
          - `<p class="muted">` explainer, with `<strong>` on the deny-by-default clause (`phan-quyen.page.html:87-90`)
          - **`NoticeBanner`** (`Components/NoticeBanner.md`, default/info severity) — `.notice` with a leading `pi pi-info-circle` (`aria-hidden`), fill `colors.tonal-bg`, border `colors.line`, 4px left rule `colors.brand`, `rounded.md`, padding `spacing.notice-padding`, `typography.notice` (`phan-quyen.page.html:97-101`, `styles.scss` § `.notice`). It is a `<p class="notice">`, so it has no dismiss control
          - `app-resource-permission-matrix` → **`Table`** in the same `.tablewrap.scroll` variant (`resource-permission-matrix.html:3`)
            - `<thead>` — `th` "Tài nguyên" at inline `width:40%`, then one `th.num` per role. The break-glass column's header carries a second line, `<span class="always-tag muted">luôn có toàn quyền</span>` — layout only (`display: block`, `margin-top: 2px`, `white-space: nowrap`), colour and size from the global `.muted` (`resource-permission-matrix.html:7-15`, `resource-permission-matrix.scss:9-16`)
            - `<tbody>` — a **flat list**, no parent/child indent: one `<tr>` per `rows()`, a plain `<td>` for `ResourceName`, then one `Check` per role (`resource-permission-matrix.html:19-34`)
            - `@empty` → `td.muted` spanning `roles().length + 1` (`resource-permission-matrix.html:35-39`)
          - Footnote `<p class="muted always-note">` below the table, rendered only when the `SuperAdmin` column is actually present in the API response (`resource-permission-matrix.html:44-51`, `.ts:45-47`); local rules are spacing and `line-height` only (`resource-permission-matrix.scss:19-22`)
        - **`ConfirmDialog`** (`Components/ConfirmDialog.md`, `severity="warn"`, `confirmDanger`) — **new since the previous revision.** Placed **after** `.card` in the template so that the active tab's "Lưu thay đổi" stays the first `.btn.primary` in the DOM. Opened by `canDeactivate` when either tab is dirty; the cancel button ("Ở lại") takes focus on open, so a reflex Enter lands on the safe branch
  - **`Toast`** (`Components/Toast.md`; `.toast-stack`, fixed bottom-right, `aria-live="polite"` `role="status"`, `no-print`) — outside the shell branch, so it renders on this route too (`app.html` § `<app-toast />`)

#### Why both matrices are hand-rolled

Both are dumb components: `input.required` for `rows`/`roles`, an optional `loading` input, and a single `permissionToggle` output. Neither calls a service and neither owns a save button — those belong to the smart `PhanQuyenPage` (`permission-matrix.ts:40-53`, `resource-permission-matrix.ts:33-47`). Each needs a full checkbox grid with a data-driven column count, no paging, and a screen-relative scroll box — none of which `p-table` was earning its weight for. There is **no `p-table` anywhere under `platform/phan-quyen/`**; `DataTable.md` covers the PrimeNG grid, `Table.md` covers these.

They are also deliberately **not** generalised into one component: PERM-1 is a tree keyed by `ParentId`, PERM-2 is a flat list, and the contract explicitly warns against merging them (`doc/contracts/permissions.md:130-134`).

<!-- Component gap — reviewed 2026-08-29. Every region composes from an indexed spec:
       Card -> Components/Card.md            Button -> Components/Button.md
       SegmentedControl -> Components/SegmentedControl.md
       NoticeBanner -> Components/NoticeBanner.md
       Table (.tablewrap.scroll) -> Components/Table.md
       Check -> Components/Check.md
       Sidebar / Topbar / Toast -> their own specs
     Undocumented and left that way on purpose, because each is one rule with no states
     and no variants: the page-local `.muted` paragraph and `.tabs` margin
     (phan-quyen.page.scss:5-15), the `.unsaved` label (phan-quyen.page.scss:20-23), the
     matrix trimmings `td.indent` + `.tree-branch` (permission-matrix.scss:8-15), and the
     two break-glass captions `.always-tag` / `.always-note`
     (resource-permission-matrix.scss:9-22). Recorded here, not invented into specs. -->

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

> 🔄 **SỬA 2026-09-06 — this section opened with "No i18n layer exists" and set every
> Localization-key cell to `— (hardcoded)`. That is false of the running code.** An i18n runtime
> shipped: `@ngx-translate/core`, bundles at `src/FE/public/i18n/{vi,en}.json`, and gate **G12** in
> `scripts/fe-gate.sh` fails on any Vietnamese diacritic left in a template. Every visible string on
> this screen now comes from the `phan-quyen.*` or `shared.*` key group, the route `title` included.
> The keys below were read out of the bundle, not inferred from the copy.

Keys resolve against `src/FE/public/i18n/vi.json`; the `en` bundle carries the same key set. The
copy column is the `vi` value, which is the source language.

| Element | Verbatim copy (`vi`) | Localization key | Source |
| --- | --- | --- | --- |
| Topbar heading (route title) | `Phân quyền` | `phan-quyen.routeTitle` | `phan-quyen.routes.ts` § `title`, rendered by `topbar.html` § `.logo h1` |
| Switcher group label | `Chọn loại phân quyền` (`aria-label`, not visible) | `phan-quyen.action.tabGroupLabel` | `phan-quyen.page.html` § `.segmented` |
| Segment 1 | `Theo màn hình` | `phan-quyen.action.tabMenu` | `phan-quyen.page.html` |
| Segment 2 | `Theo tài nguyên` | `phan-quyen.action.tabResource` | `phan-quyen.page.html` |
| Dirty marker on a segment | `chưa lưu`, rendered as ` • ` + the value inside `<span class="unsaved">` | `phan-quyen.hint.unsaved` | `phan-quyen.page.html` § `.unsaved` |
| Tab A section label | `Phân quyền theo màn hình` (`aria-label`, not visible) | `phan-quyen.menuSectionLabel` | `phan-quyen.page.html` |
| Tab A heading | `Phân quyền màn hình` | `phan-quyen.menuTitle` | `phan-quyen.page.html` |
| Save button — idle, both tabs | `Lưu thay đổi` | `phan-quyen.action.save` | `phan-quyen.page.html` |
| Save button — saving, both tabs | `Đang lưu…` (a real ellipsis, U+2026) | `shared.action.saving` — promoted, shared with the change-password screen | `phan-quyen.page.html` |
| Save button tooltip while disabled, **both tabs** | `Chưa có thay đổi nào để lưu` | `phan-quyen.hint.nothingToSave` | `phan-quyen.page.html` — see the symmetry correction under § Layout Blueprint |
| Tab A explainer | `Tick chọn role được thấy màn hình tương ứng. Mục không tick role nào = mở cho mọi user đã đăng nhập.` | `phan-quyen.hint.menuMatrix` | `phan-quyen.page.html` § `.muted` |
| Tab A column header 1 | `Màn hình` | `phan-quyen.grid.screen` | `permission-matrix.html` |
| Tab A empty row | `Chưa có mục menu nào.` | `phan-quyen.grid.emptyMenu` | `permission-matrix.html` § `@empty` |
| Tab A child-row glyph | `└` | none — a literal box-drawing character; see `Icons.md` § Legacy Exceptions | `permission-matrix.html` § `.tree-branch` |
| Tab A row label (dynamic) | `{{ row.SysMenuName }}` — seeded values: `Trang chủ`, `Quản trị hệ thống`, `Người dùng`, `Phân quyền` | none — DB value | `permission-matrix.html`; `AppMenuSeedSource.cs` § `Items` |
| Tab A checkbox accessible name (dynamic) | `<SysMenuName> — <role>` | **none — still concatenated in the template.** The resource matrix builds the same label from `phan-quyen.grid.cellAriaLabel`; this one does not. It survives gate G12 because the concatenated pieces carry no Vietnamese diacritic — see § Normalize on redesign | `permission-matrix.html` § `attr.aria-label` |
| Tab B section label / heading | `Phân quyền theo tài nguyên` (label is `aria-label`; heading is visible) | `phan-quyen.resourceTitle` — one key serving both | `phan-quyen.page.html` |
| Tab B explainer (three parts, `<strong>` on the middle) | `Tick chọn role được thực hiện hành động tương ứng. Khác hẳn tab "Theo màn hình": ở đây` + **`không tick role nào = từ chối tất cả`** + `, chứ không phải mở cho mọi người.` | `phan-quyen.hint.resourceLead` · `.resourceEmphasis` · `.resourceTail` | `phan-quyen.page.html` — split into three keys so the emphasised clause can be wrapped in `<strong>` without markup inside a translation string |
| Tab B notice banner | `Hiện chưa có API nghiệp vụ nào áp dụng các quyền này, nên thay đổi ở đây được lưu lại nhưng chưa chặn hay mở thêm thao tác nào cho người dùng.` | `phan-quyen.hint.noEnforcement` | `phan-quyen.page.html` § `.notice` |
| Tab B column header 1 | `Tài nguyên` | `phan-quyen.grid.resource` | `resource-permission-matrix.html` |
| Tab B break-glass column caption | `luôn có toàn quyền` | `phan-quyen.grid.alwaysAllowedTag` | `resource-permission-matrix.html` § `.always-tag` |
| Tab B row label (dynamic) | `{{ row.ResourceName }}` — today the API returns exactly one row, `Import CSV/Excel` | none — API value | `resource-permission-matrix.html`; the label is supplied by the host seam `ICoreResourceKeySource` |
| Tab B checkbox accessible name (dynamic) | `{{resource}} — {{role}}`, or `{{resource}} — {{role}} — luôn có quyền, không thay đổi được` on the break-glass column | `phan-quyen.grid.cellAriaLabel` / `.cellAriaLabelAlwaysAllowed` | `resource-permission-matrix.ts` |
| Tab B footnote (three parts) | `Cột` + role + `luôn được tick và không sửa được ở đây: role này mặc định có mọi quyền với mọi tài nguyên, nên bỏ tick cũng không thu hồi được gì. Muốn một người không còn toàn quyền, vào "Quản trị hệ thống → Người dùng" và gỡ role` + role + `khỏi tài khoản của họ.` | `phan-quyen.hint.alwaysAllowedLead` · `.alwaysAllowedBody` · `.alwaysAllowedTail` | `resource-permission-matrix.html` § `.always-note` — split so the role name can be interpolated in `<strong>` twice |
| Tab B empty row | `Chưa có tài nguyên nào.` | `phan-quyen.grid.emptyResource` | `resource-permission-matrix.html` § `@empty` |
| Role column headers (dynamic, both tabs) | `{{ role }}` — the API always returns all three: `SuperAdmin`, `Admin`, `User` | none — API value, deliberately untranslated | `permission-matrix.html`, `resource-permission-matrix.html` |
| **Write-conflict banner (409), either tab** | `Có người vừa đổi phân quyền, nên thay đổi của bạn CHƯA được lưu. Tải lại để xem bản mới nhất rồi tick lại — thao tác tải lại sẽ bỏ những gì bạn vừa chọn.` | `phan-quyen.error.versionConflict` | `phan-quyen.page.ts` § `CONFLICT_TEXT_KEY`, rendered by `phan-quyen.page.html` § `.notice.warn` |
| **No-version banner, either tab** | `Chưa đọc được phiên bản ma trận (lần tải gần nhất hỏng) nên không thể lưu an toàn. Hãy tải lại rồi thực hiện lại thay đổi.` | `phan-quyen.error.noVersion` | `phan-quyen.page.ts` § `NO_VERSION_TEXT_KEY` |
| **Reload button on either banner** | `Tải lại` | `shared.action.reload` | `phan-quyen.page.html` |
| **Leave-confirmation dialog** | title `Rời khỏi trang?` · body `Bạn có thay đổi phân quyền chưa lưu. Rời khỏi trang bây giờ sẽ bỏ toàn bộ thay đổi đó.` · confirm `Rời khỏi trang` · cancel `Ở lại` | `phan-quyen.dialog.leaveTitle` · `.leaveDescription` · `.leaveConfirm` · `.leaveCancel` | `phan-quyen.page.html` § `<app-confirm-dialog>` |
| Save-success toast, tab A | `Đã lưu thay đổi phân quyền.` | `phan-quyen.toast.savedMenu` | `phan-quyen.page.ts` |
| Save-success toast, tab B | `Đã lưu phân quyền theo tài nguyên.` | `phan-quyen.toast.savedResource` | `phan-quyen.page.ts` |
| Error toast (API envelope present) | server-supplied `message` from the envelope | none — server value | `core/interceptors/http-error.interceptor.ts` |
| Error toast fallbacks (no connection / 403 / other) | resolved through `ApiErrorMessageService` | `SYSTEM.*` key group in the bundle | `core/i18n/api-error-message.service.ts` |

Copy notes, as shipped: both save buttons use a real ellipsis (`…`, U+2026) — that lives in the
translation value now, not the template. The two explainers still state opposite defaults in words,
which remains the screen's single most important message — PERM-1 absence widens access, PERM-2
absence denies it. Two long sentences (tab B's explainer and its footnote) are **split across three
keys each** so that `<strong>` can wrap the middle clause without putting markup inside a
translation string; a translator changing word order across those boundaries will break the
emphasis, which is a real cost of the split and is recorded rather than hidden.

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **first paint:** `tab()` is `'menu'`, so tab A renders. Its `loading` starts `true` and the single `GET /admin/permissions` fires in the constructor (`phan-quyen.page.ts:40,45,58-67`). While loading, `rows()` **and** `roles()` are still `[]`, so the matrix renders its `@empty` branch: a `<thead>` with only the `Màn hình` column and one body row reading `Chưa có mục menu nào.` at `colspan=1`. **There is no spinner, skeleton or progress text anywhere on this screen** — loading is visually identical to empty, distinguishable only by the save button being `[disabled]`.
- **tab B is loaded lazily, once:** `resourceLoading` starts `false` and `GET /admin/permissions/resources` only fires the first time the segment is pressed, guarded by a `resourceRequested` latch so switching back and forth does not refetch (`phan-quyen.page.ts:52,56,74-93`). The stated reason is shipped behaviour, not a preference: most visits are here to edit menus, and eagerly hitting a table that may not be migrated yet would raise an error toast at someone who never opened the tab.
- **populated, tab A:** rows render parent-then-child. A root row is a plain `<td>`; every non-root row gets `td.indent` plus the `└` glyph. `Indent` is a boolean, not a depth counter, so a grandchild would render at the same single indent as a child — the seeded tree is one level deep, so this is not visible today (`permission-matrix.ts:18-24`). The `SuperAdmin` column here is an ordinary clickable column with no lock and no caption. A row with no box ticked is **open to every signed-in user**.
- **populated, tab B:** a flat list, today exactly one row (`Import CSV/Excel`). The `SuperAdmin` column renders **checked and `[disabled]`** on every row, with `luôn có toàn quyền` under its header and the explanatory footnote below the table (`resource-permission-matrix.ts:49-61`). That tick is a *description of the backend's break-glass bypass*, not a stored row: the save payload sends back the `AssignedRoles` the API returned, unmodified, so nothing the user did not choose is written (`phan-quyen.service.ts:56-71`). A row with no box ticked is **denied to everyone except the break-glass role**.
- **dirty (unsaved edits):** ticking a box mutates local signal state only and sets that tab's own flag — `dirty()` for A, `resourceDirty()` for B (`phan-quyen.page.ts:95-121`). The owning segment grows ` • ` plus `phan-quyen.hint.unsaved`, so an edit left on the hidden tab stays visible from the other one.

  > 🔄 **SỬA 2026-09-06 — "There is still **no navigation guard** — leaving the route discards edits silently" is no longer true.** Two layers now ship, and the source comment says both are required:
  >
  > - **In-app navigation** — the route declares `canDeactivate: [unsavedChangesGuard]`. When either tab is dirty the page opens its `ConfirmDialog` ("Rời khỏi trang?") and resolves the navigation from the answer; "Ở lại" holds focus, so a reflex Enter cancels the departure.
  > - **Leaving the browser** — closing the tab, F5 and closing the window never reach the Angular router, so the page also handles `beforeunload`, which is the only mechanism the browser offers there (and it renders the browser's own generic wording, not the copy above).
  >
  > This closes Normalize item 6 as it was written; the item now records what remains.
- **saving:** the pressed tab's button switches to `Đang lưu…` and goes `[disabled]`, and its matrix receives `[loading]` true, which disables **every** checkbox in that matrix for the duration (`phan-quyen.page.html:52-54,64,77-85,106`). Each `PUT` always sends the **complete** row set of its own matrix — overwrite semantics, per contract — and never touches the other table (`phan-quyen.service.ts:41-46,64-71`).
- **save success, tab A:** `saving` and `dirty` clear, a success toast appears bottom-right for 5 s (`toast.service.ts:19`), **and the sidebar is refetched** — `menu.refresh()` runs because the `PUT` just changed the very `SysMenuRole` table the nav rail renders (`phan-quyen.page.ts:123-136`).
- **save success, tab B:** `resourceSaving` and `resourceDirty` clear and a toast appears. `menu.refresh()` is deliberately **not** called: `RolePermission` does not decide which menus appear, so refetching would cost a request and imply a link between the two tables that does not exist (`phan-quyen.page.ts:138-153`).
- **save refused — write conflict (409) or missing version token:** 🔄 **new since the previous revision, which had no concept of it.** Each matrix carries its own `Version` token read from its `GET`. On save, a 409 `VERSION_CONFLICT` sets that tab's conflict signal, which renders the persistent `.notice.warn` banner with a `Tải lại` button; the local edits stay on screen and stay dirty, and the copy says out loud that reloading will discard them. If the opening `GET` failed there is no token at all, and pressing Save shows the same banner with the no-version message **without issuing a request** — a save that cannot be made safe is refused before it is sent rather than overwriting a state nobody has read. The banner persists until reload, which is the point: the interceptor's toast auto-dismisses and offers no way forward.
- **error (any of the four requests):** the only feedback is the toast raised by `httpErrorInterceptor` — the server `message` when the envelope is present, otherwise a title + body pair chosen by status (`http-error.interceptor.ts:65-85`). The handlers do nothing beyond clearing their own flag, with one exception: a failed tab-B `GET` also **releases the `resourceRequested` latch**, so leaving the tab and returning retries the fetch — the screen's only retry affordance, and it is undiscoverable (`phan-quyen.page.ts:86-91`). A failed `GET` otherwise leaves the matrix in the empty-looking state with no in-card error text; a failed `PUT` leaves the local edits on screen, still dirty, with pressing Save again as the only way forward.
- **access denied:** a signed-in non-`SuperAdmin` never sees the screen — `superAdminGuard` returns a `UrlTree` to `/trang-chu` (`role.guard.ts:23,35`). There is **no** 403 page, no message and no toast on that path; the redirect is silent. A 401 on a later navigation is caught by `authGuard`, which redirects to `/dang-nhap` carrying a `returnUrl` (`auth.guard.ts:21`).
- **validation:** none exists on this screen. Every input is a checkbox with two legal values, so there is no inline validation state, no error styling and no field-level message in either matrix. The nearest thing is tab B's dirty-gated save button, which is a destructive-action guard rather than validation.

### Responsive

<!-- Behavior per breakpoint. -->

- **No `@media` query exists in any of this screen's own stylesheets** — `phan-quyen.page.scss`, `permission-matrix.scss` and `resource-permission-matrix.scss` contain none. Every breakpoint effect below is inherited from the shell (`app.scss`, `topbar.scss`, `sidebar.scss`) or from global rules in `styles.scss`.
- **≥`breakpoint.desktop` (981px, default):** sidebar fixed at `dimension.sidebar-w` with `.shell-content` offset to match; `main` centred at `dimension.container-max-width` with `spacing.sp-5` padding (`app.scss:37-71`). Collapsing the sidebar narrows the offset to `dimension.sidebar-w-collapsed` over 0.2 s; the card and both matrices simply reflow wider.
- **≤`breakpoint.tablet` (980px):** `.shell-content { margin-left: 0 !important }` and the sidebar becomes an off-canvas drawer opened by the topbar hamburger, which is hidden above this width (`app.scss:73-77`). The full viewport width goes to `main`, so the matrices get *wider* here, not narrower.
- **≤`breakpoint.mobile` (560px):** `main` padding drops to `10px` — a raw literal, no token — and the topbar hides the user's name (`app.scss:79-83`). **Nothing inside the card changes**: the `.segmented` group is a direct child of `.card`, not of a `.toolbar`, so the toolbar's mobile `flex: 1` override never reaches it and the two segments keep their intrinsic width.
- **Wide role × row matrix on a narrow viewport:** each table is `width: 100%` with **no `min-width`** (`styles.scss` § `table`), so columns compress with the container — the name column is held at an inline `width:40%` and the role columns share the rest. Horizontal scrolling exists (`.tablewrap.scroll { overflow: auto }`) but is **content-driven**: it engages only once intrinsic min-content width exceeds the container, which with three roles and today's short labels does not happen even at 390px. There is no column collapse, no card-per-row fallback and no per-viewport column hiding.
- **Vertical scrolling (all viewports):** each matrix fills the leftover viewport height and scrolls inside itself, with the global sticky `th` keeping the role headers pinned inside that scroll container (`styles.scss` § `th`). 🔄 **SỬA 2026-09-06 — the mechanism changed, and the token this line named is now the one thing that does *not* apply.** The previous revision said the wrapper is bounded at `dimension.grid-h` (`calc(100dvh - 280px)`) with `dimension.grid-h-min` as the floor. The page host now carries `page-fill` and each matrix carries `grid-host`, and `styles.scss` § `.page-fill .tablewrap.scroll` **resets `max-height: none; min-height: 0`** so a flex chain measures the real chrome instead of subtracting a constant. The reason is in the source: the fixed chrome above and below the grid is not the same on every page — it measured 443px on one and 280px on another — so any single constant is wrong somewhere. `dimension.grid-h` remains the fallback for a grid outside a `.page-fill` page.
- **Print (`@media print`):** sidebar, topbar and toast stack disappear via the single centralised `.no-print` rule; `.shell-content` margin is zeroed and `main` loses its max-width (`styles.scss` § `@media print`). The per-component print blocks in `sidebar.scss` and `topbar.scss` were removed on 2026-08-29 in favour of that one rule (`sidebar.scss:321-324`, `topbar.scss:58-59`). The card and the active matrix do print — but `.tablewrap.scroll` keeps `overflow: auto` in print, so rows past the on-screen height are clipped, and **the inactive tab prints nothing at all** because it is not in the DOM. (The clipping height is now whatever the flex chain resolved to, not `dimension.grid-h`; the outcome for a printout is the same.)

### Iconography

See `Icons.md` § Per-Action Map. This screen's own template contains **three** icons: the `pi pi-info-circle` opening tab B's standing notice banner, and a `pi pi-exclamation-triangle` in each tab's write-conflict banner. All three are `aria-hidden`, because the sentence beside each carries the whole message. 🔄 **SỬA 2026-09-06:** the previous revision said "**one** icon" and cited a line of `Icons.md` by number; the two conflict banners did not exist when it was written. Everything else is a text `Button`, a text segment or a native checkbox, and the only other glyph is the literal `└` text character marking a child row — plain text in a `<span>`, not an icon element.

The remaining icons visible while this screen is open belong to the app shell, which loads **PrimeIcons v7** globally. **No PrimeNG inline-SVG icon reaches this screen**: there is no `p-table` under `platform/phan-quyen/`, so there is no paginator and no PrimeNG loading mask. The `[loading]` inputs are the app's own signal inputs (`permission-matrix.ts:43`, `resource-permission-matrix.ts:36`); they render no spinner and no overlay, and their only visible effect is `[disabled]` on every checkbox.

| Action | Icon | Placement |
| --- | --- | --- |
| Switch between the two matrices | — (text `SegmentedControl`, no icon) | Top of the card |
| Save either matrix | — (text `Button`, no icon) | `.title` row of the active tab, right-aligned |
| Grant/revoke a role on a row | — (native `Check`, `accent-color: colors.brand`) | Every `td.num`, right-aligned |
| "This matrix controls nothing yet" banner | `pi pi-info-circle` (`aria-hidden`) | Leading glyph of tab B's standing `NoticeBanner` |
| Save was refused (409 conflict / no version token) | `pi pi-exclamation-triangle` (`aria-hidden`) | Leading glyph of the `.notice.warn` banner, in **either** tab |
| Reload the matrix after a refused save | — (text `Button`, no icon) | Inside the same banner |
| Sidebar entry for this screen | `pi-shield` (from `SysMenu.Icon`) | Shell sidebar, under the `pi-cog` group "Quản trị hệ thống". Both are seeded database values, not FE constants (`AppMenuSeedSource.cs:48,56`), so either can change without an FE deploy |
| Open the navigation drawer (≤980px) | `pi pi-bars` | Shell topbar, left (`topbar.html:12`) |
| Sign out | `pi pi-sign-out` | Shell topbar, right (`topbar.html:20`) |
| Dismiss a toast | `pi pi-times` | Shell toast item, right (`toast.html:19`) |

### Screenshots

<!-- Refs into Assets/Screenshots/phan-quyen/ -->

> ⚠️ **The one captured shot is out of date.** `permission-matrix--desktop-1440.png` was captured 2026-08-22, before the `.segmented` switcher and the resource matrix existed and before the palette change. It shows the menu matrix alone in a tab-less card, so it no longer matches the blueprint above. Its status is recorded as stale in `UiInventory.md` § Screenshot Manifest. **Recapturing it — with both tabs — is the only real debt in this table.**

Every other row is an **on-demand** state/viewport variant under `doc/Design/CLAUDE.md` § Rules (one desktop shot per screen). Capture one when someone actually needs that case and flip its status then; the list is not a backlog.

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`doc/Design/CLAUDE.md`](../../../CLAUDE.md) § Rules

**Common prerequisites for every row:** with both servers up, (1) sign in at `/dang-nhap` as a **SuperAdmin** — an `Admin` is redirected to `/trang-chu` and cannot capture this screen; (2) go to `/quan-tri/phan-quyen`. Never record credentials here.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png` | **stale — recapture** (captured 2026-08-22, pre-switcher) | Live app @ 1440×1000, full page, tab **Theo màn hình** selected, sidebar expanded. Must now show the `.segmented` switcher above the card body. |
| `Assets/Screenshots/phan-quyen/resource-matrix--desktop-1440.png` | **missing — capture with the above** | Same session, press **Theo tài nguyên** and capture once the row has loaded. Must show the notice banner, the disabled+checked `SuperAdmin` column with its `luôn có toàn quyền` caption, the footnote, and the Save button disabled with its tooltip. This is the half of the screen no shot has ever covered. |
| `Assets/Screenshots/phan-quyen/permission-matrix--dirty-tabs--desktop-1440.png` | on demand | Toggle one box in each tab **without saving**, then capture with either tab showing — both segments carry ` • chưa lưu`. The only shot that documents the dirty marker. |
| `Assets/Screenshots/phan-quyen/permission-matrix--saving--desktop-1440.png` | on demand | @ 1440×1000; throttle the network in DevTools, toggle a box, press "Lưu thay đổi" and capture while the button reads `Đang lưu…` and every checkbox is disabled. |
| `Assets/Screenshots/phan-quyen/permission-matrix--saved-toast--desktop-1440.png` | on demand | Save any change and capture within 5 s so the success toast is still in the bottom-right stack (auto-dismiss is 5 s, `toast.service.ts:19`). |
| `Assets/Screenshots/phan-quyen/permission-matrix--loading-empty--desktop-1440.png` | on demand | In DevTools block `GET /api/admin/permissions`, then reload the route. Captures the shared loading/empty rendering — single-column header, `Chưa có mục menu nào.`, disabled save button — plus the interceptor's error toast. |
| `Assets/Screenshots/phan-quyen/resource-matrix--load-error--desktop-1440.png` | on demand | Block `GET /api/admin/permissions/resources`, then press the **Theo tài nguyên** segment. Documents the retry-by-tab-switch behaviour: the empty grid plus an error toast, with the fetch repeating on the next visit to the tab. |
| `Assets/Screenshots/phan-quyen/permission-matrix--tablet-900.png` | on demand | Same as row 1 @ 900×1200 — below `breakpoint.tablet`, so the sidebar is an off-canvas drawer (capture it **closed**) and the topbar shows the hamburger. |
| `Assets/Screenshots/phan-quyen/permission-matrix--mobile-390.png` | on demand | Same as row 1 @ 390×900 — below `breakpoint.mobile`. Record in the capture note whether the table compressed or began scrolling horizontally with the role list the API actually returned. |

<!-- Only the captured row belongs in UiInventory.md → Screenshot Manifest. On-demand rows stay
     here, beside the layout they document, and are promoted only once one is actually taken. -->

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. A quirk that spans components belongs in the component's own spec (`Components/<Name>.md` → Normalize on redesign), not here. -->

1. **A permission screen that grants permissions nothing enforces.** Tab B writes real rows, but the catalogue holds one key and no product endpoint carries `[RequirePermission]` (the `ResourceKeys` class **was deleted 2026-09-03**; the catalogue now comes from the host seam). The screen is honest about it — the notice banner says so — but an administrator is still being asked to make security decisions with no effect. Either the first `[RequirePermission]` lands, or the tab should be behind a flag rather than shown with an apology. Remove the banner the day the first endpoint is decorated; the source comment says the same (`phan-quyen.page.html:92-96`).
2. **Two matrices with opposite defaults, distinguished only by a paragraph of prose.** Absence means *open to everyone* in tab A and *denied to everyone* in tab B. Nothing in the visual language separates them — same table, same checkbox, same colour. The explainers carry the whole load, and an administrator who skims will get it backwards in one of the two tabs. Give the deny-by-default grid a different empty-cell treatment, or label the columns rather than relying on a sentence.
3. ~~**The two Save buttons behave differently and look identical.**~~ — 🔄 **SỬA 2026-09-06: closed in the source, by the second of the two remedies this item offered.** The guard was applied to both: tab A's Save now carries `!dirty()` as well, with the same `title` while disabled. What is left is a much smaller point, kept because it is the reason the item existed: a disabled Save looks the same whether the cause is "nothing changed", "still loading" or "a save is in flight", and only the first of those three explains itself in the tooltip.
4. **Loading is indistinguishable from empty and from a failed load, in both tabs.** All three render the same single-column table plus an empty message; only a disabled save button hints that a request is in flight, and a `GET` failure leaves no error text or retry control inside the card. Give each matrix a real loading state, a distinct empty state and an in-card error state with a visible retry.
5. **The only retry is undiscoverable.** Tab B's failed `GET` releases its latch so re-entering the tab refetches (`phan-quyen.page.ts:86-91`) — correct behaviour that no user will ever find, because nothing on screen says so. Tab A has no equivalent at all: its `GET` runs once in the constructor, and a failure there is unrecoverable without a page reload.
6. ~~**Unsaved edits are still discarded silently on navigation.**~~ — 🔄 **SỬA 2026-09-06: closed in the source.** The route declares `canDeactivate: [unsavedChangesGuard]`, the page opens a `ConfirmDialog` when either tab is dirty, and a `beforeunload` handler covers the paths the router never sees. What remains is smaller and worth keeping: the dialog cannot say **which** tab is dirty or how many boxes moved, so a user who edited the hidden tab an hour ago is asked a question they have no way to evaluate.
7. **No confirmation before a full-overwrite save, in either tab.** Each `PUT` replaces its entire matrix. In tab A, un-ticking every role on a row silently *widens* access; in tab B it revokes. A destructive gesture with opposite consequences in the two tabs and zero confirmation — a confirm step summarising the delta would be proportionate, and `ConfirmDialog` already exists for it.
8. **The two matrices build the same accessible name two different ways.** The resource matrix composes its checkbox `aria-label` from `phan-quyen.grid.cellAriaLabel`, an interpolated translation key; the menu matrix concatenates `row.SysMenuName + ' — ' + role` in the template. The rendered result happens to match today, and gate G12 never sees it because the concatenated pieces carry no Vietnamese diacritic — so this is a string that escaped the i18n pass **without failing anything**. Move it onto the existing key; the parameter names already fit.
9. **The indent flag is a boolean, not a depth.** `toDisplayOrder()` marks every descendant with the same `Indent: true` (`permission-matrix.ts:18-24`); today's seeded menu is one level deep, but a grandchild would render identically to a child, misrepresenting the tree the matrix exists to show.
10. **Untokenised literals inside the matrices** — `td.indent { padding-left: 28px }`, `.tree-branch { margin-right: 4px }`, `.always-tag { margin-top: 2px }`, and the inline `style="width:40%"` on the first column of both tables (`permission-matrix.scss:8-15`, `resource-permission-matrix.scss:9-16`, `permission-matrix.html:7`, `resource-permission-matrix.html:7`). None resolves to a token in `Tokens/`; tokenize them or extend the existing scale. *(The `max-height: 560px` this list used to name is gone. 🔄 **SỬA 2026-09-06:** the replacement is not `dimension.grid-h` either, as this parenthesis claimed — it is the `.page-fill` flex chain, which overrides that token on this screen. See § Responsive.)*
11. **Printing clips the matrix and loses the inactive tab.** `.tablewrap.scroll` keeps `overflow: auto` and its height cap under `@media print`, and the unselected tab is not in the DOM at all, so a printout can never show both matrices. Reset the wrapper's height and overflow for print, and decide whether printing should render both sections.
