---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
flow: "Authentication"
screens: ["Sign in", "Change password"]
source_routes: ["/dang-nhap", "/doi-mat-khau"]
---

# Authentication — Screens

The two screens in this file are one flow, not two features. A user created by an administrator carries `MustChangePassword = true`; signing in therefore does not land on the dashboard but is routed straight to `/doi-mat-khau`, and `authGuard` keeps every other authenticated route closed until the password is changed (`src/FE/src/app/core/auth/auth.guard.ts:24-27`). Both screens are the only routes in the app that render **without the app shell** — no sidebar, no topbar — because both declare `data: { noShell: true }` and `App` swaps the shell for a bare `<router-outlet>` when it sees that flag (`src/FE/src/app/app.html:1-13`, `src/FE/src/app/app.ts:45`). They share one visual container, the `AuthCard` shim, and one global stylesheet block. This spec records the shipped Angular 20 app; `doc/Prototype/login.html` is cited only as design intent for sign-in, and `/doi-mat-khau` has no prototype at all — it was built directly in Angular (`doc/Design/CLAUDE.md` § Fidelity Policy).

> **Shell:** none — both routes set `data: { noShell: true }` (`login.routes.ts:9`, `doi-mat-khau.routes.ts:10`), so `App` renders `<router-outlet>` with no `<app-sidebar>`/`<app-topbar>`. `<app-toast />` sits **outside** the shell conditional (`app.html:14`) and is therefore the one shell-level element that IS present on both screens.
> **Sources:** `src/FE/src/app/platform/login/`, `src/FE/src/app/platform/doi-mat-khau/`, `src/FE/src/app/shared/components/auth-card/`, `src/FE/src/styles.scss`, `src/FE/src/app/core/auth/`, `src/FE/src/app/core/interceptors/http-error.interceptor.ts`, `doc/contracts/auth.md`; design intent (sign-in only): `doc/Prototype/login.html`
> **Token vocabulary:** tokens are named by their live CSS custom property in `src/FE/src/styles.scss:10-79` (e.g. `--brand`, `--sp-5`, `--radius-lg`) because that `:root` block is the shipped token source. Where a documented equivalent exists it is given as `--brand` (`colors.primary`). `Tokens/*.md` and `DESIGN.md` were **re-extracted from `src/FE/` on 2026-08-22** — the earlier warning here, that they came from the prototype and had no entry for `--sp-*`, `--fs-*`, `--radius-sm/md/lg`, `--border-strong`, `--tonal-bg` or `--bad-bg`, no longer applies; all of those are documented now. Two values moved again later the same day (`--warn` → `#965e08`, `--bad` → `#a02b2b`, to clear WCAG AA); `styles.scss` stays the tiebreaker if any doc disagrees. Values quoted below with no token name are raw literals in the shipped SCSS, recorded as as-shipped facts.

---

## Sign in (`/dang-nhap`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **Auth shell** (`.login-shell`, `auth-card.scss:1-8`) — the outer box of `AuthCard` (`Components/AuthCard.md`, which documents this whole `.login-shell` → `.login-card` → `.login-brand` group as the app's second shell). Full-viewport flex centering box: `min-height:100vh` immediately overridden by `min-height:100dvh`, `align-items:center`, `justify-content:center`, padding `--sp-5`. Page background comes from `body{background:var(--bg)}` (`styles.scss:89-95`), not from this element.
  - **Auth card** (`.login-card`, `auth-card.scss:10-18`) — `AuthCard` anatomy. It is *not* the documented `Card` (`.card`) — it is a separate class with the same recipe: `--card` (`colors.surface`) background, `1px solid var(--line)` (`colors.border`), `--radius-lg`, `--shadow`. Sizing is its own: `width:100%` clamped by `max-width:380px`, padding `32px 28px` (both raw literals, no token).
    - **Brand block** (`.login-brand`, `auth-card.scss:20-52`) — `AuthCard` anatomy. Vertical flex, `--sp-3` gap, `margin-bottom:24px`, centered text.
      - Brand mark (`.brand-mark`): `44px × 44px` square, `border-radius:12px` (literal, not `--radius-*`), `--brand` (`colors.primary`) fill, `--on-primary` (`colors.on-primary`) text, `font-weight:800`, `--fs-lg`. Contains the two-letter text "PM" — **not an image asset**; the project ships no logo file (`UiInventory.md` § Brand Assets).
      - `<h1>` bound to the `title` input: `font-size:18px` (literal), `font-weight:800`.
      - `<p>` bound to the `subtitle` input, rendered only when non-empty (`@if`, `auth-card.html:6-8`): `--muted` (`colors.text-muted`), `--fs-sm`.
    - **Error block** (`.login-error.show`, `styles.scss:607-624`) — part of `AuthField` (`Components/AuthField.md`, which owns the auth tier's error block). Rendered only while `errorMessage()` is non-null (`@if`, `login.page.html:2-7`). Horizontal flex, `--sp-3` gap (literal `8px`), `--bad-bg` background, `--bad` text, `--radius-sm`, `--sp-3` padding, `--fs-sm`, `margin-bottom:--sp-4`; border is the raw literal `#e5a8a8` — flagged in-source as an untokenized debt (`styles.scss:610-612`). Leading `pi pi-exclamation-circle` icon + message `<span>`.
    - **Form** (`<form (submit)>`, native, no `novalidate`, no `ReactiveFormsModule` — plain `[value]`/`(input)` signal wiring)
      - **Field: Email** — `AuthField` (`.field` + `.field-input`, `Components/AuthField.md`, `styles.scss:506-569`). This is the auth input tier — the fourth of the app's four field treatments, alongside the filter and table-cell tiers in `Components/Input.md` and the dialog tier in `Components/FormRow.md`; `Input.md` also lists it as its "Auth field" variant. `.field` = block label (`--fs-sm`, weight 700, `--text`, `margin-bottom:--sp-2`) over a relatively-positioned `.field-input` row. Input: full width, `1px solid var(--border-strong)`, `--radius-sm`, `--card` background, padding `10px 12px 10px 36px` (left inset reserves room for the icon), `--fs-sm`, `--text`. Absolutely-positioned `pi pi-envelope` at `left:12px`, `--muted`, `font-size:15px`, `pointer-events:none`. `type="text"` (not `email`) with `autocomplete="username"` and `required`.
      - **Field: Mật khẩu** — same anatomy, `pi pi-lock` leading icon, `[type]` toggled between `password` and `text` by `showPassword()`. Adds a trailing **visibility toggle** (`.toggle-visibility`, `styles.scss:548-568`) — part of `AuthField` (`Components/AuthField.md`): borderless transparent button at `right:10px`, `--muted`, `padding:4px`, `border-radius:6px` (literal), containing `pi pi-eye` / `pi pi-eye-slash`.
      - **Options row** (`.field-row`, `styles.scss:571-595`) — part of `AuthField` (`Components/AuthField.md`). `space-between` flex, `margin-bottom:20px` (literal), `--fs-sm`.
        - Checkbox + label "Ghi nhớ đăng nhập" — a bare `<input type="checkbox" />` with **no binding of any kind** (`login.page.html:51`); it is inert (see Normalize on redesign).
        - Link "Quên mật khẩu?" — `href="javascript:void(0)"`, `--brand`, weight 700, underline on hover; **no route, no handler** (see Normalize on redesign).
      - **Submit** — `Button` (primary variant, `Components/Button.md`) + the `.btn-block` modifier (`styles.scss:599-607`: `width:100%`, `padding:11px`, `--fs-md`, centered flex, `gap:8px`) — **now documented** in `Components/Button.md` since the 2026-08-22 refresh. Content: `pi pi-sign-in` icon + a label that swaps with `submitting()`. Carries `[disabled]`, which activates `.btn:disabled{opacity:.5;cursor:not-allowed}` (`styles.scss:191-194`) — a state `COMPONENTS.md` **used to** record as unreachable — corrected in the 2026-08-22 component refresh, which confirmed `[disabled]` is bound at 5 call sites including this one.
- **`Toast` stack** (`.toast-stack`, `Components/Toast.md`, `toast.scss:1-10`) — rendered by `<app-toast />` outside the shell conditional, so it overlays this screen: `position:fixed`, `right/bottom:--sp-5`, `z-index:60`, `max-width:min(360px,90vw)`, `--sp-3` column gap. Each `.toast-item` is a `--card` surface with `1px solid var(--line)`, a 4px left border tinted by severity (`--bad` for `error`), `--radius-md`, `--shadow`, `--sp-3 --sp-4` padding, `--fs-sm`, plus a `pi pi-times` dismiss button.
- **Absent by construction:** no `<app-sidebar>`, no `<app-topbar>`, no `<main>` wrapper, no footer. The prototype's `.login-footer` (`doc/Prototype/login.html:60,` styled but empty in the markup) was **not ported** to the Angular screen.

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `PlatformManager` | — (hardcoded) | `src/FE/src/index.html:5` — the route's `title: 'Đăng nhập'` (`login.routes.ts:9`) is **never displayed**: its only consumer is `<app-topbar [title]>`, which `noShell` suppresses. The prototype's `<title>Đăng nhập - PlatformManager</title>` (`doc/Prototype/login.html:8`) did not ship. |
| Brand mark | `PM` | — (hardcoded) | `auth-card.html:4` |
| Card heading | `PlatformManager` | — (hardcoded) | `login.page.html:1` (`title` input) |
| Card subtitle | `Đăng nhập để tiếp tục` | — (hardcoded) | `login.page.html:1` (`subtitle` input) |
| Field 1 label | `Email` | — (hardcoded) | `login.page.html:11` — labels the field "Email" but the value is posted as `userName` and the input is `type="text"`; real accounts are free-form usernames such as `SuperAdmin` (`login.page.ts:10-16`, `doc/contracts/auth.md:24`). Shipped mismatch, kept as-is. |
| Field 1 placeholder | `ten@congty.vn` | — (hardcoded) | `login.page.html:17` |
| Field 2 label | `Mật khẩu` | — (hardcoded) | `login.page.html:27` |
| Field 2 placeholder | `Nhập mật khẩu` | — (hardcoded) | `login.page.html:33` |
| Visibility toggle `aria-label` (masked) | `Hiện mật khẩu` | — (hardcoded) | `login.page.html:43` |
| Visibility toggle `aria-label` (revealed) | `Ẩn mật khẩu` | — (hardcoded) | `login.page.html:43` |
| Remember-me label | `Ghi nhớ đăng nhập` | — (hardcoded) | `login.page.html:51` |
| Forgot-password link | `Quên mật khẩu?` | — (hardcoded) | `login.page.html:52` |
| Submit button (idle) | `Đăng nhập` | — (hardcoded) | `login.page.html:56` |
| Submit button (submitting) | `Đang đăng nhập…` | — (hardcoded) | `login.page.html:56` |
| Inline error — client validation | `Vui lòng nhập đầy đủ tài khoản và mật khẩu.` | — (hardcoded) | `login.page.ts:61` |
| Inline error — server failure | `{envelope.message}` verbatim from the server, e.g. `Bạn thao tác quá nhanh. Vui lòng thử lại sau 47 giây.` | — (server-supplied) | `login.page.ts:74`; envelope shape and sample text `doc/contracts/auth.md:83-91` |
| Inline error — fallback when no envelope | `Đăng nhập thất bại — thử lại sau.` | — (hardcoded) | `login.page.ts:74` |
| Toast — server failure | `{envelope.message}` verbatim (same string as the inline error, shown a second time) | — (server-supplied) | `http-error.interceptor.ts:82` |
| Toast — fallback, no connection | `Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.` | — (hardcoded) | `http-error.interceptor.ts:21` |
| Toast — fallback, 401 | `Bạn cần đăng nhập để tiếp tục.` | — (hardcoded) | `http-error.interceptor.ts:23` |
| Toast — fallback, 403 | `Bạn không có quyền thực hiện thao tác này.` | — (hardcoded) | `http-error.interceptor.ts:25` |
| Toast — fallback, 404 | `Không tìm thấy dữ liệu yêu cầu.` | — (hardcoded) | `http-error.interceptor.ts:27` |
| Toast — fallback, 429 without envelope | `Bạn thao tác quá nhanh. Vui lòng chờ một lát rồi thử lại.` | — (hardcoded) | `http-error.interceptor.ts:32` |
| Toast — fallback, anything else | `Đã có lỗi xảy ra. Vui lòng thử lại.` | — (hardcoded) | `http-error.interceptor.ts:34` |
| Toast dismiss `aria-label` | `Đóng thông báo` | — (hardcoded) | `toast.html:8` |
| Native validation bubble (empty required field) | browser-supplied, locale-dependent (e.g. Chrome: `Please fill out this field.`) | — (user-agent) | emergent from `required` at `login.page.html:19,35` with no `novalidate` on the `<form>` |

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **idle (default):** reached after the app-init probe resolves — `provideAuthInit()` awaits `GET /api/auth/me` once at bootstrap (`auth-init.provider.ts:11-16`), and that request carries `SKIP_ERROR_TOAST`, so an anonymous visitor's 401 produces **no toast and no redirect** (`current-user.service.ts:36-51`, `http-error.interceptor.ts:80-84`). Both text signals empty → placeholders visible; `showPassword()` false → password masked, `pi-eye` shown; `submitting()` false → button enabled, label `Đăng nhập`; `errorMessage()` null → the `.login-error` element is **absent from the DOM entirely** (`@if`, not a CSS hide). The legacy `.login-error{display:none}` rule (`styles.scss:608`) is therefore dead in Angular — the class is only ever emitted together with `.show`. A visitor who already has a session and navigates back here never sees this state: `redirectAfterAuth()` runs in the constructor (`login.page.ts:37-43`). See screenshot `sign-in--desktop-1440.png`.
- **idle with `returnUrl`:** identical rendering — the deep link is carried **only in the URL**, e.g. `/dang-nhap?returnUrl=%2Fdanh-muc%2Fdti`, and nothing on the card acknowledges it (no "sign in to continue to X" copy). Two producers: `authGuard` when an anonymous user hits a protected route (`auth.guard.ts:20-22`, `queryParams: { returnUrl: state.url }`) and `httpErrorInterceptor` when a live session dies mid-use (`http-error.interceptor.ts:52-58`, using the full `router.url` including its own query string). On success the value is honoured only if it starts with `/`, otherwise `/dashboard` is used (`login.page.ts:84-85`) — an open-redirect guard. **`mustChangePassword` outranks it**: when true, `redirectAfterAuth()` returns early to `/doi-mat-khau` and the `returnUrl` is silently dropped, never resumed after the password change (`login.page.ts:79-86`). See screenshot `sign-in--return-url--desktop-1440.png`.
- **submitting:** `submitting()` true → the submit button gets `[disabled]` (`login.page.html:55`) rendering at `opacity:.5` with `cursor:not-allowed` (`styles.scss:191-194`), and its label swaps to `Đang đăng nhập…` while the `pi pi-sign-in` icon stays. There is **no spinner and no progress indicator anywhere**. Nothing else locks: both inputs, the visibility toggle, the checkbox and the link remain enabled and editable for the whole round-trip. `errorMessage` is cleared to null immediately before the request (`login.page.ts:65`), so a previous error block disappears the moment a resubmit starts.
- **empty:** there is **no empty state on this screen, and there cannot be one.** An empty state needs a collection that can come back with nothing to show; this card renders none — the whole form is statically present in the template (`login.page.html:9-58`), with no `@for` anywhere and exactly one `@if`, the error block at `login.page.html:2-7`. The only fetch in the flow is the bootstrap `GET /api/auth/me` probe, and its "nothing here" answer — an anonymous 401 — is deliberately rendered as nothing at all: `SKIP_ERROR_TOAST` suppresses the toast and no redirect follows, leaving the card in **idle** (`auth-init.provider.ts:11-16`, `current-user.service.ts:36-51`). The closest thing the user ever sees is idle with both fields blank and their placeholders showing, which is the default state above rather than a state of its own.
- **field-validation:** there is **no per-field error slot** — no invalid border, no `aria-invalid`, no helper text, no field-level styling exists on either screen. Validation arrives in two layers. (1) *Native*: both inputs are `required` and the `<form>` has no `novalidate`, so an empty field blocks the `submit` event and the browser shows its own bubble. (2) *Component guard*: `login.page.ts:60-63` writes `Vui lòng nhập đầy đủ tài khoản và mật khẩu.` into the **same** `.login-error` block that server errors use — there is one message slot for both concerns. Because layer 1 fires first, layer 2 is only reachable when the username is non-empty but whitespace-only (`.trim()` at `login.page.ts:58`). This path never produces a toast: it never issues an HTTP request.
- **server-error (`AUTH.INVALID_CREDENTIALS`, 422):** the same sentence reaches the user **twice through two independent channels** — `httpErrorInterceptor` toasts `body.message` from the envelope (`http-error.interceptor.ts:82`) and the page copies `err.apiResult?.message` into the inline block (`login.page.ts:74`). Neither channel reads `businessCode`, so every 4xx/5xx renders identically; the `fields` map returned by the 400 validation envelope (`doc/contracts/auth.md:45-50`) is likewise never read and its per-field messages never surface. If the body is not a parseable envelope the two channels diverge: the toast falls back to `fallbackMessageForStatus()` while the inline block falls back to `Đăng nhập thất bại — thử lại sau.` — different text, same failure. Note that `handleUnauthorized()` deliberately does **not** redirect while the router is on `/dang-nhap` (`http-error.interceptor.ts:53`), which is what stops a failed login from destroying the `returnUrl` in the address bar. See screenshot `sign-in--server-error--desktop-1440.png`.
- **locked-out (`AUTH.LOCKED_OUT`, 422 — `doc/contracts/auth.md:40`):** renders **exactly like the invalid-credentials state** — same inline block, same duplicate toast, same `--bad` colouring; the only difference is the server-supplied sentence. The UI offers no distinct affordance: no elevated severity, no "contact an administrator" hint, no disabled form — even though this is the one auth failure the user cannot clear on their own (an admin must unlock the account).
- **rate-limited (429, `RATE_LIMIT.TOO_MANY_REQUESTS`):** triggered by the 6th login attempt inside a minute from the same IP — policy `"login"` is 5/min/IP and stacks on top of the 100/min/IP `GlobalLimiter` (`doc/contracts/auth.md:52-68`). Renders identically again. The envelope's `message` already embeds the wait ("…thử lại sau 47 giây.", `doc/contracts/auth.md:84`) and `Retry-After` carries the same number of seconds, but **the header is never read**: there is no countdown, no cooldown, no temporarily-disabled submit — the button re-enables the instant the response lands and the user can keep firing requests and keep collecting 429s. The interceptor's own 429 string (`http-error.interceptor.ts:31-32`) only appears when a 429 arrives with no envelope, i.e. from a proxy ahead of the API. Distinguishing this from locked-out matters and the UI does not help: **429 is the IP, clears itself within ≤ 1 minute; 422 `AUTH.LOCKED_OUT` is the account and needs an admin** (`doc/contracts/auth.md:110-111`). See screenshot `sign-in--rate-limited--desktop-1440.png`.
- **success:** no confirmation is rendered on this screen — `AuthService.login()` invalidates the cached menu then sets the user context (`auth.service.ts:26-36`) and the component navigates away immediately (`login.page.ts:68-71`). There is no success toast.

### Responsive

<!-- Behavior per breakpoint. -->

- **All viewports — there is no breakpoint.** Neither `auth-card.scss`, `login.page.scss`, nor the global auth block (`styles.scss:500-624`) contains a single `@media` rule. The shell breakpoints that shape every other screen (`980px` / `560px` in `app.scss`, `sidebar.scss`, `topbar.scss`) do not apply here, because the shell is not rendered at all.
- **Fluid behaviour instead of breakpoints:** `.login-card{width:100%;max-width:380px}` (`auth-card.scss:11-12`) means the card is a fixed 380px on any viewport wider than roughly 408px, and below that it shrinks to fill, kept off the edges by the shell's `--sp-5` padding (`auth-card.scss:7`). Internal padding stays `32px 28px` at every width — it does **not** tighten on small screens the way the dashboard's does.
- **Viewport height:** `min-height:100vh` immediately re-declared as `min-height:100dvh` (`auth-card.scss:2-3`) so the card stays optically centred on mobile browsers whose toolbars change the visible height; the second declaration wins wherever `dvh` is supported and is ignored as an unknown unit elsewhere.
- **Viewport meta:** `width=device-width, initial-scale=1, viewport-fit=cover` (`src/FE/src/index.html:6`) — `viewport-fit=cover` is set globally, but neither auth screen consumes `env(safe-area-inset-*)` anywhere, so on a notched device the card relies purely on the `--sp-5` shell padding.
- **Toast overlay:** the only viewport-reactive rule that reaches this screen is `.toast-stack{max-width:min(360px,90vw)}` (`toast.scss:9`), which narrows the error toast on small screens while its `right/bottom:--sp-5` offsets stay fixed.
- **Print (`@media print`):** the global rule hides `.no-print` (`styles.scss:115-119`). No element on this screen carries that class, so the card prints exactly as it renders; the toast stack does carry it (`toast.html:1`) and is the one thing that disappears when printing.

### Iconography

<!-- Refs into Icons.md -->

✅ `Icons.md` was stale when this spec was written (it declared `library: "none"`, having been derived from the now-frozen prototype) and was **corrected in the 2026-08-22 refresh**. The shipped Angular app loads **PrimeIcons v7**, wired as a global stylesheet in `angular.json:37-39` (build) and `angular.json:123-125` (test) from `node_modules/primeicons/primeicons.css`, with `primeicons: ^7.0.0` in `package.json:35`. Icons are `<i class="pi pi-*">` elements, not SVG. Sizing: form-field icons are `font-size:15px`, `--muted`, absolutely positioned at `left:12px` with `pointer-events:none`, and the field's `padding-left:36px` is what reserves their gutter (`styles.scss:523-537`); the submit button's icon-to-label gap is the `.btn-block` `gap:8px` (`styles.scss:606`).

| Action | Icon | Placement |
| --- | --- | --- |
| Error notice (inline) | `pi pi-exclamation-circle` | Leading, inside `.login-error` (`login.page.html:4`) |
| Username field | `pi pi-envelope` | Leading, absolute `left:12px` inside `.field-input` (`login.page.html:13`) |
| Password field | `pi pi-lock` | Leading, absolute `left:12px` inside `.field-input` (`login.page.html:29`) |
| Reveal password | `pi pi-eye` | Trailing, inside `.toggle-visibility`, shown while masked (`login.page.html:45`) |
| Hide password | `pi pi-eye-slash` | Trailing, same button, shown while revealed (`login.page.html:45`) |
| Submit sign-in | `pi pi-sign-in` | Leading, inside the primary `.btn.btn-block`, kept in place during `submitting()` (`login.page.html:56`) |
| Dismiss toast | `pi pi-times` | Trailing, inside `.toast-close` (`toast.html:11`) |
| Remember-me checkbox | — (native `<input type="checkbox">`, no icon) | `login.page.html:51` |
| Forgot-password link | — (text link, no icon) | `login.page.html:52` |

### Screenshots

<!-- Refs into Assets/Screenshots/auth/ -->

**✅ The desktop shot exists — captured 2026-08-22 from the live Angular app.** That is the full target under `doc/Design/CLAUDE.md` § Rules (ONE desktop shot per screen, decided 2026-08-22). Every remaining row below is an **on-demand** state/viewport variant, *not* an outstanding debt: capture one when someone actually needs that case, and flip its status then. Reproducible capture procedure (nothing here modifies `src/**`):

- Frontend: `cd src/FE && npm start` → `http://localhost:4200` (`package.json:6`, `angular.json:96-97`). The 2026-08-22 capture used `npx ng serve --port 4201` instead — 4201 is already in the backend's CORS allowlist, so no config change was needed.
- Backend, needed only for the rows marked *(BE required)*: the API must answer at `http://localhost:5027/api` (`src/FE/src/environments/environment.development.ts:5`), with the schema applied per `doc/contracts/auth.md:6-11`.
- Use a fresh browser profile with no `PlatformManager.Auth` cookie, otherwise the constructor redirect fires and the sign-in card never renders (`login.page.ts:37-43`).

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/auth/sign-in--desktop-1440.png` | captured 2026-08-22 | `/dang-nhap` @ 1440×900, anonymous profile, idle state. No BE session needed — the `/auth/me` probe fails silently. Reached from `/`, so `authGuard` had appended `?returnUrl=%2F` to the URL. |
| `Assets/Screenshots/auth/sign-in--mobile-390.png` | on demand | Same URL/state @ 390×844. Proves the no-breakpoint fluid clamp: the card is `min(380px, 100% − 2×--sp-5)`, internal padding unchanged. |
| `Assets/Screenshots/auth/sign-in--return-url--desktop-1440.png` | on demand | Navigate to `http://localhost:4200/danh-muc/dti` while anonymous; `authGuard` rewrites the URL to `/dang-nhap?returnUrl=%2Fdanh-muc%2Fdti`. Capture @ 1440×900 **with the address bar visible** — the query param is the only place this state is observable. Low value: the captured shot above already carries the simpler `?returnUrl=%2F` form, and the rendered card is identical. |
| `Assets/Screenshots/auth/sign-in--server-error--desktop-1440.png` | on demand *(BE required)* | Submit a wrong password once → 422 `AUTH.INVALID_CREDENTIALS`. Capture @ 1440×900 within 5s of submit so the inline block **and** the duplicate toast are both in frame (toast auto-dismisses at 5000 ms, `toast.service.ts:11`). |
| `Assets/Screenshots/auth/sign-in--rate-limited--desktop-1440.png` | on demand *(BE required)* | Submit 6 times inside one minute from the same IP → 429. Capture @ 1440×900 within 5s, framing inline block + toast; the toast text contains the server's own `Retry-After` seconds. |
| `Assets/Screenshots/auth/sign-in--locked-out--desktop-1440.png` | on demand *(BE required)* | Have an admin lock the account, then sign in → 422 `AUTH.LOCKED_OUT`. Capture @ 1440×900. Exists to document that this renders identically to the invalid-credentials shot apart from the sentence. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

- **Every server failure produces two notifications of the same text.** The interceptor toasts `body.message` (`http-error.interceptor.ts:82`) *and* the page writes the identical string into `.login-error` (`login.page.ts:74`). Pick one channel for form-scoped failures — inline is the better fit here — and let the toast handle only errors with no form to attach to, or opt the login request out with `SKIP_ERROR_TOAST` (`http-context-tokens.ts`), the mechanism already used by the `/auth/me` probe.
- **The three failures a user must respond to differently look identical.** Invalid credentials (retry), locked out (call an admin), rate-limited (wait ≤ 1 min) all render the same red block with the same icon. `businessCode` is already on `err.apiResult` and never read — branch on it: `AUTH.LOCKED_OUT` deserves distinct copy with a next step, and 429 should read `Retry-After` and disable the submit button for that many seconds instead of letting the user re-trigger the limiter.
- **"Ghi nhớ đăng nhập" is inert.** `<input type="checkbox" />` has no `[value]`, no `(change)`, no signal, and no participation in the login call (`login.page.html:51`) — the cookie's 14-day sliding lifetime is decided entirely server-side (`doc/contracts/auth.md:188-190`). Either wire it to something real or remove it; a control that visibly does nothing is worse than its absence.
- **"Quên mật khẩu?" leads nowhere.** `href="javascript:void(0)"` with no handler and no route (`login.page.html:52`); there is no password-reset endpoint in `doc/contracts/auth.md`. Remove it until the flow exists, or point it at a real recovery path.
- **The field is labelled "Email" but holds a username.** Label `Email` and placeholder `ten@congty.vn` (`login.page.html:11,17`) over a `type="text"` input posted as `userName`, where real accounts look like `SuperAdmin` (`login.page.ts:10-16`). The source comment shows the mismatch was inherited knowingly from the prototype. Relabel to "Tài khoản"/"Tên đăng nhập" with a matching placeholder, or make usernames genuinely be emails.
- **`returnUrl` is destroyed by the forced password change.** `redirectAfterAuth()` returns early to `/doi-mat-khau` before the `returnUrl` branch is reached (`login.page.ts:79-86`) and `/doi-mat-khau` then hard-codes `/dashboard` on success. A first-login user who followed a deep link is silently dumped on the dashboard. Carry the value through the change-password step (query param or a short-lived store) and resume it afterwards.
- **Only the button locks while submitting.** Both fields, the visibility toggle and the checkbox stay live for the whole request (`login.page.html:14-53`), so a user can edit the username after the request left the client and see a result that no longer matches what is on screen. Disable the fieldset — or at least the inputs — for the duration.
- **Field-level validation has nowhere to render.** There is one shared message slot for client validation and every server error, and no per-field affordance at all (no invalid border, no `aria-invalid`, no helper text), so the `fields` map the API already returns on a 400 (`doc/contracts/auth.md:45-50`) cannot be shown. Add a per-field error slot and bind `fields` to it. In the meantime the guard string `Vui lòng nhập đầy đủ tài khoản và mật khẩu.` is nearly dead code: native `required` blocks the submit event first, leaving only the whitespace-only-username path.
- **Error announcement is not wired for assistive tech.** The `.login-error` block appears via `@if` with no `role="alert"` / `aria-live` and is not associated with either input; the toast stack does have `aria-live="polite" role="status"` (`toast.html:1`), so today the only announced copy is the duplicate. Give the inline block the live region and drop the duplicate.
- **Untokenized literals inside a token-driven card.** `max-width:380px`, `padding:32px 28px`, `border-radius:12px` and `44px` on the brand mark, `font-size:18px` on the heading (`auth-card.scss:11-45`), plus `margin-bottom:20px`, `padding:10px 12px 10px 36px`, `padding:11px`, `font-size:15px` and the error border `#e5a8a8` (`styles.scss:506-624`, self-flagged as debt 3/3 at lines 610-612). Promote these into `:root` and mirror them into `Tokens/*`/`DESIGN.md` when the token pipeline is re-run against `src/FE/`.

---

## Change password (`/doi-mat-khau`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

Structurally identical to sign-in — same `AuthCard` shim, same global `.field`/`.field-input`/`.login-error`/`.btn-block` block, same absent shell. Only the contents of the card differ.

- **`AuthCard`** — **Auth shell** (`.login-shell`) → **Auth card** (`.login-card`) → **Brand block** (`.login-brand`), all as specified under Sign in above (`Components/AuthCard.md`, `auth-card.scss:1-52`). The brand mark still reads "PM"; the `<h1>` is bound to `"Đổi mật khẩu"` and the `<p>` to a **computed** subtitle that changes with `isForced()` (`doi-mat-khau.page.ts:36-41`) — the only place either screen varies its own copy at runtime.
  - **Error block** (`.login-error.show`) — same element, same `@if` gate, same `pi pi-exclamation-circle` (`doi-mat-khau.page.html:2-7`).
  - **Form** (`<form (submit)>`, native, no `novalidate`, plain `[value]`/`(input)` signal wiring) — **three** fields, all `type="password"`, all `required`, none with a visibility toggle:
    - **Field: Mật khẩu hiện tại** — `.field` + `.field-input`, leading `pi pi-lock`, `autocomplete="current-password"` (`doi-mat-khau.page.html:10-23`).
    - **Field: Mật khẩu mới** — same anatomy, leading `pi pi-key`, `autocomplete="new-password"` (`doi-mat-khau.page.html:26-40`).
    - **Field: Xác nhận mật khẩu mới** — same anatomy, leading `pi pi-key`, `autocomplete="new-password"` (`doi-mat-khau.page.html:42-56`).
    - **Submit** — `Button` (primary variant, `Components/Button.md`) + the `.btn-block` modifier, documented there since the 2026-08-22 refresh; `[disabled]` bound to `submitting()`. **No icon** — unlike sign-in's `pi pi-sign-in`, this button is text-only (`doi-mat-khau.page.html:58-60`).
  - **Absent:** no options row (`.field-row`), no remember-me, no forgot-password link, no password-strength meter, no rules hint listing the 8-character minimum the component enforces, and **no escape hatch** — no "sign out" or "do this later" control, which matters because in the forced case this card is the only reachable screen.
- **Toast stack** (`.toast-stack`) — same overlay as Sign in, present because `<app-toast />` sits outside the shell conditional (`app.html:14`).

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `PlatformManager` | — (hardcoded) | `src/FE/src/index.html:5` — the route's `title: 'Đổi mật khẩu'` (`doi-mat-khau.routes.ts:10`) is never displayed; same `noShell` cause as Sign in. |
| Brand mark | `PM` | — (hardcoded) | `auth-card.html:4` |
| Card heading | `Đổi mật khẩu` | — (hardcoded) | `doi-mat-khau.page.html:1` (`title` input) |
| Card subtitle — forced (`mustChangePassword === true`) | `Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.` | — (hardcoded) | `doi-mat-khau.page.ts:39` |
| Card subtitle — voluntary | `Đổi mật khẩu tài khoản của bạn.` | — (hardcoded) | `doi-mat-khau.page.ts:40` |
| Field 1 label | `Mật khẩu hiện tại` | — (hardcoded) | `doi-mat-khau.page.html:11` |
| Field 1 placeholder | `Nhập mật khẩu hiện tại` | — (hardcoded) | `doi-mat-khau.page.html:17` |
| Field 2 label | `Mật khẩu mới` | — (hardcoded) | `doi-mat-khau.page.html:27` |
| Field 2 placeholder | `Nhập mật khẩu mới` | — (hardcoded) | `doi-mat-khau.page.html:33` |
| Field 3 label | `Xác nhận mật khẩu mới` | — (hardcoded) | `doi-mat-khau.page.html:43` |
| Field 3 placeholder | `Nhập lại mật khẩu mới` | — (hardcoded) | `doi-mat-khau.page.html:49` |
| Submit button (idle) | `Đổi mật khẩu` | — (hardcoded) | `doi-mat-khau.page.html:59` |
| Submit button (submitting) | `Đang lưu…` | — (hardcoded) | `doi-mat-khau.page.html:59` |
| Inline error — all fields required | `Vui lòng nhập đầy đủ các trường.` | — (hardcoded) | `doi-mat-khau.page.ts:61` |
| Inline error — too short | `Mật khẩu mới phải có ít nhất 8 ký tự.` (template literal `Mật khẩu mới phải có ít nhất ${MIN_PASSWORD_LENGTH} ký tự.`, `MIN_PASSWORD_LENGTH = 8`) | — (hardcoded) | `doi-mat-khau.page.ts:65`, constant at `doi-mat-khau.page.ts:8` |
| Inline error — confirmation mismatch | `Xác nhận mật khẩu mới không khớp.` | — (hardcoded) | `doi-mat-khau.page.ts:70` |
| Inline error — server failure | `{envelope.message}` verbatim, e.g. the Identity detail behind `AUTH.CHANGE_PASSWORD_FAILED` | — (server-supplied) | `doi-mat-khau.page.ts:83`; contract `doc/contracts/auth.md:150-151` |
| Inline error — fallback when no envelope | `Đổi mật khẩu thất bại — thử lại sau.` | — (hardcoded) | `doi-mat-khau.page.ts:83` |
| Toast — server failure | `{envelope.message}` verbatim (same string as the inline error, shown a second time) | — (server-supplied) | `http-error.interceptor.ts:82` |
| Toast — fallbacks (no connection / 401 / 403 / 404 / 429 / other) | as listed in the Sign in Copy table | — (hardcoded) | `http-error.interceptor.ts:18-36` |
| Toast dismiss `aria-label` | `Đóng thông báo` | — (hardcoded) | `toast.html:8` |
| Native validation bubble (empty required field) | browser-supplied, locale-dependent | — (user-agent) | emergent from `required` at `doi-mat-khau.page.html:19,35,51` |

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **idle — forced (default arrival):** the flow's normal entry. `authGuard` sends any authenticated user with `mustChangePassword() === true` here from **every** other route and returns `true` only for URLs starting with `/doi-mat-khau`, which is what prevents a redirect loop into itself (`auth.guard.ts:16-29`); the route keeps the guard so an anonymous visitor is still bounced to `/dang-nhap` with a `returnUrl` (`doi-mat-khau.routes.ts:4-12`). `isForced()` true → subtitle reads `Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.`. All three fields empty and masked, submit enabled, no `.login-error` in the DOM. Nothing else on screen indicates the lock-out — no banner, no explanation of *why*, and no way out. See screenshot `change-password--forced--desktop-1440.png`.
- **idle — voluntary:** the same screen reached by choice after the first change; `mustChangePassword()` is false so the subtitle reads `Đổi mật khẩu tài khoản của bạn.` and other routes are reachable — but this screen has no navigation of its own, so leaving means using the browser's back button or typing a URL. The endpoint is not one-shot (`doi-mat-khau.page.ts:10-15`). See screenshot `change-password--voluntary--desktop-1440.png`.
- **submitting:** `submitting()` true → submit button `[disabled]` (`opacity:.5`, `cursor:not-allowed`, `styles.scss:191-194`) with its label swapped to `Đang lưu…`. No spinner. All three password fields stay enabled and editable throughout, and `errorMessage` is cleared to null immediately before the request (`doi-mat-khau.page.ts:73`).
- **empty:** as on Sign in, there is **no empty state and no way to reach one.** The three fields, the submit button and the subtitle are all statically present (`doi-mat-khau.page.html:9-61`); the template contains no `@for` and one `@if`, the error block at `doi-mat-khau.page.html:2-7`. Nothing on this screen loads a list, and the one server value it depends on — `mustChangePassword()` — is already resolved before the route renders, so it selects between the two idle variants above rather than producing an empty one. The nearest state is idle with all three fields blank and masked.
- **field-validation:** three sequential client checks, all writing into the single shared `.login-error` slot — no per-field feedback exists (`doi-mat-khau.page.ts:60-71`): all-fields-present → minimum length 8 → confirmation match. They short-circuit, so only the first failing rule is ever shown; nothing is validated on blur or on input, only on submit. Two as-shipped consequences worth recording: (1) the all-fields-present check is **unreachable in a standards-compliant browser** — all three inputs are `required` with no `novalidate`, so an empty field blocks the submit event first, and unlike sign-in there is no `.trim()`, so a whitespace-only value counts as filled; (2) the 8-character minimum is enforced but **never disclosed** — no rules hint, no strength meter, and the real policy is ASP.NET Identity's, which is stricter and only reports back from the server (`doc/contracts/auth.md:150-151`), so a password can clear the client check and still be rejected.
- **server-error (`AUTH.CHANGE_PASSWORD_FAILED`, 422):** the single server code covering both "current password is wrong" and "new password is too weak", carrying Identity's detail text in `message` (`doc/contracts/auth.md:150-151`). Same duplicate delivery as sign-in — toast from the interceptor plus inline block from the page (`doi-mat-khau.page.ts:83`) — and the same divergence when the body is not a parseable envelope (toast falls back per status, inline falls back to `Đổi mật khẩu thất bại — thử lại sau.`). Because the two causes share one code and the page never inspects `businessCode`, the error cannot be attached to the field that caused it. See screenshot `change-password--validation--desktop-1440.png` for the client-side variant of this slot.
- **locked-out:** **not reachable on this screen as a login failure.** `AUTH.LOCKED_OUT` is a `POST /auth/login` outcome (`doc/contracts/auth.md:40`); by the time this screen renders the user already holds a session. The equivalent hazard here is the session being terminated underneath them — an account locked, a role changed, or the password changed elsewhere makes the next request return 401 within the `SecurityStampValidator`'s 30-minute cycle (`doc/contracts/auth.md:188-194`). That path is handled generically: `handleUnauthorized()` clears the user context and navigates to `/dang-nhap?returnUrl=/doi-mat-khau` (`http-error.interceptor.ts:52-58`) with a 401 toast, so the user is bounced to sign-in mid-form and their typed input is lost. Note this screen also has the reverse property: after a **successful** change the current session is deliberately kept alive and only the user's *other* sessions are killed (`doc/contracts/auth.md:153-170`), so no re-login is required here.
- **rate-limited (429, `RATE_LIMIT.TOO_MANY_REQUESTS`):** reachable here too. `POST /api/auth/change-password` is not covered by the 5/min `"login"` policy, but the 100/min/IP `GlobalLimiter` applies to **every** endpoint (`doc/contracts/auth.md:56-67`), so a shared-IP office or a burst of app traffic can 429 this form. Rendering is the generic error path again: envelope `message` in both the toast and the inline block, `Retry-After` ignored, no countdown, submit immediately re-enabled.
- **success:** no confirmation of any kind is shown on this screen. `markPasswordChanged()` flips `MustChangePassword` to false locally to avoid a second `/auth/me` round-trip (`current-user.service.ts:61-64`) and the component navigates straight to `/dashboard` (`doi-mat-khau.page.ts:77-79`) — a **hard-coded destination**, not the `returnUrl` that may have brought the user into the flow. There is no success toast, so a forced-change user's only feedback is that the dashboard appears.

### Responsive

<!-- Behavior per breakpoint. -->

- **All viewports — there is no breakpoint.** `doi-mat-khau.page.scss` is two lines (`:host{display:contents}`) with no rules of its own (`doi-mat-khau.page.scss:1-5`); `auth-card.scss` and the global auth block (`styles.scss:500-624`) contain no `@media` rule. The `980px` / `560px` shell breakpoints do not apply — the shell is not rendered.
- **Fluid behaviour:** identical to Sign in — `width:100%` clamped at `max-width:380px`, shell padding `--sp-5`, internal padding fixed at `32px 28px` at every width (`auth-card.scss:10-18`). The only practical difference is height: three fields plus the brand block make this the taller card, so on a short viewport (landscape phone, ~390×640) it is the one more likely to exceed the fold. `.login-shell` uses `min-height`, so it grows and the page scrolls rather than clipping — but the card stops being optically centred at that point.
- **Viewport height:** `min-height:100vh` then `min-height:100dvh` (`auth-card.scss:2-3`), as on Sign in.
- **Viewport meta:** `width=device-width, initial-scale=1, viewport-fit=cover` (`src/FE/src/index.html:6`); no `env(safe-area-inset-*)` consumption anywhere on this screen.
- **Toast overlay:** `.toast-stack{max-width:min(360px,90vw)}` (`toast.scss:9`) — the one viewport-reactive rule reaching this screen.
- **Print (`@media print`):** global `.no-print` rule only (`styles.scss:115-119`); no element on the card carries it, so the card prints as rendered while the toast stack is hidden (`toast.html:1`).

### Iconography

<!-- Refs into Icons.md -->

✅ Same library as Sign in, and the same correction: `Icons.md` declared the prototype-era `library: "none"` when this spec was written and was **corrected in the 2026-08-22 refresh**. The shipped app loads **PrimeIcons v7** globally (`angular.json:37-39,123-125`, `package.json:35`), rendered as `<i class="pi pi-*">` elements. Field icons follow the shared rule — `font-size:15px`, `--muted`, absolute `left:12px`, `pointer-events:none`, with `padding-left:36px` on the input reserving the gutter (`styles.scss:523-537`).

| Action | Icon | Placement |
| --- | --- | --- |
| Error notice (inline) | `pi pi-exclamation-circle` | Leading, inside `.login-error` (`doi-mat-khau.page.html:4`) |
| Current-password field | `pi pi-lock` | Leading, absolute `left:12px` inside `.field-input` (`doi-mat-khau.page.html:13`) |
| New-password field | `pi pi-key` | Leading, absolute `left:12px` inside `.field-input` (`doi-mat-khau.page.html:29`) |
| Confirm-password field | `pi pi-key` | Leading, absolute `left:12px` inside `.field-input` (`doi-mat-khau.page.html:45`) |
| Reveal/hide password | — (**no toggle exists on this screen**, unlike Sign in) | `doi-mat-khau.page.html:10-56` |
| Submit change-password | — (**text-only button**, no icon, unlike Sign in's `pi pi-sign-in`) | `doi-mat-khau.page.html:58-60` |
| Dismiss toast | `pi pi-times` | Trailing, inside `.toast-close` (`toast.html:11`) |

### Screenshots

<!-- Refs into Assets/Screenshots/auth/ -->

**✅ The desktop shot exists — `change-password--forced--desktop-1440.png`, captured 2026-08-22 from the live Angular app.** That is the full target under `doc/Design/CLAUDE.md` § Rules (ONE desktop shot per screen, decided 2026-08-22). Every remaining row below is an **on-demand** state/viewport variant, *not* an outstanding debt: capture one when someone actually needs that case, and flip its status then. Every row on this screen requires a live backend, because reaching the route at all requires an authenticated session (`authGuard`, `doi-mat-khau.routes.ts:11`). Procedure:

- Backend at `http://localhost:5027/api` (`src/FE/src/environments/environment.development.ts:5`), schema applied per `doc/contracts/auth.md:6-11`.
- Frontend: `cd src/FE && npm start` → `http://localhost:4200` (`package.json:6`).
- For the *forced* rows, sign in as an account whose `MustChangePassword` is still `true` (a freshly admin-created user — the seeded `SuperAdmin` sample in `doc/contracts/auth.md:32` shows `mustChangePassword: true`); the redirect to `/doi-mat-khau` is automatic. For the *voluntary* rows, sign in as an account that has already changed its password, then navigate to `http://localhost:4200/doi-mat-khau` directly.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/auth/change-password--forced--desktop-1440.png` | captured 2026-08-22 | Signed in as the bootstrap account, whose `MustChangePassword` is still `true`, so the redirect to `/doi-mat-khau` fired automatically; captured @ 1440×900 in its idle state. This is the **forced** variant — it shows the forced subtitle `Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.` Environment as recorded in `UiInventory.md` § Screenshot Manifest. |
| `Assets/Screenshots/auth/change-password--voluntary--desktop-1440.png` | on demand *(BE required)* | Same route reached by direct navigation with an already-changed account @ 1440×900 (the topbar entry point, once the flag is cleared). Must show the voluntary subtitle `Đổi mật khẩu tài khoản của bạn.` — the only visual difference from the captured row above. |
| `Assets/Screenshots/auth/change-password--validation--desktop-1440.png` | on demand *(BE required)* | Fill all three fields with the confirmation deliberately different, submit @ 1440×900. Client-side only — no request leaves the browser, so **no toast appears**; this is the one error shot with a single channel. |
| `Assets/Screenshots/auth/change-password--server-error--desktop-1440.png` | on demand *(BE required)* | Enter a wrong current password with a valid new one → 422 `AUTH.CHANGE_PASSWORD_FAILED`. Capture @ 1440×900 within 5s so inline block and duplicate toast are both in frame (`toast.service.ts:11`). |
| `Assets/Screenshots/auth/change-password--mobile-390.png` | on demand *(BE required)* | Forced idle state @ 390×844. Documents the taller three-field card against the same 380px clamp, and whether it exceeds the fold on a short viewport. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

- **The password rules are enforced but never stated.** The client requires 8 characters (`doi-mat-khau.page.ts:8,64-67`) and the server applies ASP.NET Identity's stricter policy on top, reporting it only after a failed submit (`doc/contracts/auth.md:150-151`). Show the requirements up front and, ideally, validate against the same rule set the server uses so a password cannot pass the client and fail the server.
- **No visibility toggle here, but there is one on sign-in.** Sign-in offers `pi pi-eye`/`pi pi-eye-slash` (`login.page.html:39-46`); this screen asks the user to type a new password twice, blind (`doi-mat-khau.page.html:26-56`). This is exactly the screen where revealing the value helps most. Reuse the toggle on all three fields.
- **The submit button lost its icon.** Sign-in's primary button leads with `pi pi-sign-in`; this one is text-only (`doi-mat-khau.page.html:58`). Two cards in the same flow, two different button anatomies — pick one.
- **Success is silent.** `markPasswordChanged()` then `navigateByUrl('/dashboard')` with no toast and no confirmation (`doi-mat-khau.page.ts:76-80`). A user who has just been forced through a security step is given no acknowledgement that it worked. Add a success toast — the channel already exists (`toast.service.ts:25-27`) and is currently used for errors only.
- **The post-success destination is hard-coded.** `/dashboard` regardless of where the user came from (`doi-mat-khau.page.ts:79`), which is the second half of the `returnUrl` loss described under Sign in. Accept and resume a `returnUrl` through this screen.
- **The forced state is a dead end with no explanation and no exit.** No banner explaining that an administrator set this requirement, no "sign out" control, no link anywhere. If the user cannot recall their current password they cannot proceed and cannot leave except by clearing cookies. Add an explanatory notice and a sign-out affordance.
- **Only the first failing client rule is ever shown, in a slot shared with server errors.** The three checks short-circuit into one message box (`doi-mat-khau.page.ts:60-71`), so fixing the length error can immediately surface a mismatch error the user never saw coming. Validate on blur/input per field and give each field its own error slot.
- **`Vui lòng nhập đầy đủ các trường.` is dead code.** All three inputs are `required` with no `novalidate`, so the browser blocks the submit event before the guard runs, and — unlike sign-in — there is no `.trim()`, so whitespace counts as filled. Either drop the guard or add `novalidate` and own the validation completely, rather than shipping a branch that cannot execute.
- **The error block still has no live region.** Same as Sign in: `@if`-inserted with no `role="alert"`/`aria-live` and no association with any field (`doi-mat-khau.page.html:2-7`), so the only announced copy is the duplicate toast.
- **A mid-form 401 discards everything typed.** If the session dies while the form is filled, `handleUnauthorized()` navigates to `/dang-nhap` with no warning and no draft preservation (`http-error.interceptor.ts:52-58`). Low-frequency, but on the one screen where re-typing is most painful.
