---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "current"
updated: "2026-09-08"
flow: "Authentication"
screens: ["Sign in", "Change password"]
source_routes: ["/dang-nhap", "/doi-mat-khau"]
---

# Authentication — Screens

The two screens in this file are one flow, not two features. A user created by an administrator carries `MustChangePassword = true`; `mustChangePasswordGuard` then closes **every** other route and redirects to `CORE_ROUTES.changePassword` — `/doi-mat-khau` in this app (`must-change-password.guard.ts:19-25`), while `/doi-mat-khau` itself carries only `authGuard` — deliberately, because attaching the other guard there would redirect the route onto itself (`doi-mat-khau.routes.ts:4-7,17`; the constraint is pinned by a test in `src/FE/src/app/app.routes.spec.ts`). Both screens are the only routes in the app that render **without the app shell** — no sidebar, no topbar — because both declare `data: { noShell: true }` and `App` swaps the shell for a bare `<router-outlet>` when it sees that flag (`app.html:18,38-39`, `app.ts:61`). They share one visual container, the `AuthCard` component, and one global stylesheet block.

**Rewritten 2026-08-29.** The previous revision was written on 2026-08-22 and had gone stale in four ways at once, all of them load-bearing:

| Previous revision said | As shipped today |
| --- | --- |
| The auth input is `.field-input`, a fourth field tier owned by `AuthField` | `.field-input` was renamed **`.input-icon`** and merged with the toolbar search field; the input contract belongs to `Components/Input.md` (`styles.scss` § `.input-icon`) |
| The reveal button is `.toggle-visibility`, its own control | It is a plain **`.icon-btn`** positioned by `.input-icon .icon-btn` (`styles.scss` § `.icon-btn` and § `.input-icon .icon-btn`) |
| "There is **no** per-field error slot — no invalid border, no `aria-invalid`, no helper text" | `/doi-mat-khau` renders a `.form-error` under each failing field and puts `.invalid` + `aria-invalid` + `aria-describedby` on the input (`doi-mat-khau.page.html:32-34,39-41`). `/dang-nhap` still has none |
| `redirectAfterAuth()` falls back to `/dashboard` | Falls back to **`/trang-chu`** (`login.page.ts` § `redirectAfterAuth()` → `CORE_ROUTES.home`, whose value is set in `src/FE/src/app/app.config.ts:36`); `/dashboard` no longer exists |
| Palette and font of the 2026-08-22 stylesheet (`--bg: #eef2f8`, `Inter`) | `styles.scss` was rewritten on 2026-08-29: new WCAG-tuned palette and self-hosted **Be Vietnam Pro** (`styles.scss` § `:root`, § `@font-face`, § `body`) |

> ### 🔄 SỬA 2026-09-06 — second refresh, after the i18n phase
>
> The 2026-08-29 rewrite above stayed accurate until 2026-09-05, when four shipped changes
> landed at once and left this file describing a screen that no longer exists:
>
> | This file said | As shipped (verified 2026-09-06) |
> | --- | --- |
> | Every Copy row `— (hardcoded)` | **Both** screens are fully wrapped, including route titles (`login.routeTitle`, `doi-mat-khau.routeTitle`) |
> | Field 1 is `Email` / `ten@congty.vn` / `pi pi-envelope` | `Tên đăng nhập` / `vd nguyen.van.a` / `pi pi-user` — decision 6, shipped |
> | Remember-me checkbox is inert, forgot-password is a dead link | Both shipped — decisions 4 and 5 |
> | A mid-session 401 strands the user | Interceptor redirects with `returnUrl` and a `warn` toast |
> | Nothing on this screen is undocumented | `LanguageSwitcher` is on this screen and has **no spec** — Normalize #12 |
> | Client password minimum is 8 | **12**, and the sentence no longer names a number |
>
> Every `file:line` in the file was re-checked against source the same day; the sign-in
> template alone had drifted by 10–40 lines, which is why nothing here should be trusted
> without the anchor next to it.

Sections 1–6 below record the app **as-shipped**; everything a redesign should change is in § Normalize on redesign only.

> **Shell:** none — both routes set `data: { noShell: true }` (`login.routes.ts:19`, `doi-mat-khau.routes.ts:16`), so `App` renders `<router-outlet>` with no `<app-sidebar>`/`<app-topbar>` (`app.html:38-39`). `<app-toast />` sits **outside** the shell conditional (`app.html:41`) and is therefore the one shell-level element present on both screens.
> **Sources:** `src/FE/src/app/platform/login/` (`login.routes.ts`, `pages/login/login.page.{html,ts,scss}`), `src/FE/src/app/platform/doi-mat-khau/` (`doi-mat-khau.routes.ts`, `pages/doi-mat-khau/doi-mat-khau.page.{html,ts,scss}`), `src/FE/src/app/shared/components/auth-card/`, `src/FE/src/app/shared/components/toast/`, `src/FE/src/app/core/auth/` (`auth.guard.ts`, `must-change-password.guard.ts`, `auth.service.ts`, `current-user.service.ts`, `auth-init.provider.ts`), `src/FE/src/app/core/interceptors/http-error.interceptor.ts`, `src/FE/src/app/core/toast/toast.service.ts`, `src/FE/src/app/core/title/page-title.strategy.ts`, `src/FE/src/app/{app.html,app.ts,app.routes.ts}`, `src/FE/src/styles.scss`, `src/FE/src/index.html`, `doc/contracts/auth.md`. File:line citations below use the bare filename of these paths.
> **Token vocabulary:** token names are the design-system names in `Tokens/tokens.json` and the `DESIGN.md` frontmatter (`colors.*`, `spacing.*`, `rounded.*`, `typography.*`, `dimension.*`, `shadow.*`, `duration.*`), each of which mirrors a live CSS custom property in `styles.scss` § `:root`. Values quoted below with no token name are raw literals in the shipped SCSS, recorded as as-shipped facts and catalogued in `Tokens/spacing.md`.

---

## Sign in (`/dang-nhap`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **`AuthCard`** (`Components/AuthCard.md`) — the app's second shell, `<app-auth-card>` with `title` and `subtitle` inputs (`auth-card.ts:17-20`), rendering three nested regions:
  - **Auth shell** (`.login-shell`, `auth-card.scss:1-8`) — full-viewport flex centring box: `min-height: 100vh` immediately re-declared as `min-height: 100dvh`, `align-items: center`, `justify-content: center`, padding `spacing.sp-5`. The page fill comes from `body { background: colors.bg }` (`styles.scss` § `body`), not from this element
  - **Auth card** (`.login-card`, `auth-card.scss:10-18`) — fill `colors.card`, border 1px `colors.line`, `rounded.lg`, `shadow.card`; sizing is its own: `width: 100%` clamped by `dimension.login-card-max-width` (380px), padding `spacing.auth-card-padding` (`32px 28px`). It is **not** the documented `Card` (`.card`) — a separate class with the same recipe and different padding
    - **Brand block** (`.login-brand`, `auth-card.scss:20-52`) — vertical flex, gap `spacing.sp-3`, `margin-bottom: 24px` (literal), centred text
      - Brand mark (`.brand-mark`, `auth-card.scss:28-39`): `dimension.brand-mark-auth` square (44px), `border-radius: 12px` (literal, not a `rounded.*` step), fill `colors.brand`, ink `colors.on-primary`, `font-size: fontSize.fs-lg` (15px), `font-weight: 800`. Contains `CORE_BRANDING.shortName` — currently the two-letter string `PM`, injected, **not an image asset**; the app ships no logo file (`UiInventory.md` § Brand Assets)
      - `<h1>` bound to the `title` input: `typography.h1-auth` (18px / 800, `auth-card.scss:41-45`)
      - `<p>` bound to the `subtitle` input, rendered only when non-empty (`@if`, `auth-card.html:8-10`): ink `colors.muted`, `typography.notice` size (`auth-card.scss:47-51`)
    - **`<ng-content />`** (`auth-card.html:12`) — everything below is projected here, which is why the form's classes must be global (see `Components/AuthField.md`)
- **`AuthField`** — form-level error block (`.login-error.show`, `styles.scss` § `.login-error` / `.login-error.show`), rendered only while `errorMessage()` is non-null (`@if`, `login.page.html:6-11`) and carrying **`role="alert"`** (`login.page.html:7` — Normalize #10, shipped). Row flex, gap `spacing.sp-3`, fill `colors.bad-bg`, border 1px `colors.danger-border`, ink `colors.bad`, `rounded.sm`, padding `spacing.sp-3`, `typography.notice` size, `margin-bottom: spacing.sp-4`. Leading `pi pi-exclamation-circle` glyph plus the message `<span>`
- **Form** (`<form (submit)>`, `login.page.html:13-92`) — native, no `novalidate`, no `ReactiveFormsModule`; plain `[value]` / `(input)` signal wiring
  - **`AuthField` block: Tên đăng nhập** (`.field`, `styles.scss` § `.field` / `.field label`) — `margin-bottom: spacing.sp-4`; its `label` is `display: block`, `typography.form-label`, ink `colors.text`, `margin-bottom: spacing.sp-2` (`login.page.html:14-37`)
    - **`Input`** (`Components/Input.md`, the `.input-icon` variant, `styles.scss` § `.input-icon` / `.input-icon > .pi` / `.input-icon input`) — a relatively-positioned flex row holding an absolutely-positioned `pi pi-user` at `left: 12px`, ink `colors.muted`, `font-size: 15px`, `pointer-events: none`; the `<input>` is `width: 100%`, border 1px `colors.border-strong`, `rounded.sm`, fill `colors.card`, padding `spacing.auth-input-padding` (`10px 12px 10px 36px` — the 36px left inset is what clears the glyph), `typography.table-cell` size, ink `colors.text`. `type="text"` (not `email`), `autocomplete="username"`, `required` (`login.page.html:27-35`)
  - **`AuthField` block: Mật khẩu** (`login.page.html:39-63`) — same anatomy, leading `pi pi-lock`, `[type]` toggled between `password` and `text` by `showPassword()` (`login.page.html:45`), `autocomplete="current-password"`, `required`
    - **`IconButton`** (`Components/IconButton.md`, `.icon-btn`, `styles.scss` § `.icon-btn`) — the reveal toggle, absolutely positioned at `right: 10px` by `.input-icon .icon-btn` (`styles.scss` § `.input-icon .icon-btn`): `dimension.icon-button` square (24px), transparent fill and border, `rounded.sm`, ink `colors.muted`. Its presence adds `padding-right: 40px` to the input via `.input-icon:has(.icon-btn) input` (`styles.scss` § `.input-icon:has(.icon-btn) input`). Holds `pi pi-eye` while masked and `pi pi-eye-slash` while revealed (`login.page.html:52-61`)
  - **`AuthField` options row** (`.field-row`, `styles.scss` § `.field-row`) — `space-between` flex, `align-items: center`, `margin-bottom: 20px` (literal), `typography.table-cell` size. It now holds **one** child, not two, so the `space-between` has nothing to space
    - **`Check`** (`Components/Check.md`, `.check`, `styles.scss` § `.check`) inside a `<label>` (gap `spacing.sp-2`, ink `colors.text`, `cursor: pointer`) reading `Ghi nhớ đăng nhập`. **Fully wired** (Normalize #4, shipped): `[checked]="rememberMe()"` + `(change)="onRememberMeChange($event)"` (`login.page.html:72-77`), default **unchecked** (`login.page.ts` § `rememberMe`), and the value travels in the `POST /api/auth/login` body (`login.page.ts` § `onSubmit()` → `doc/contracts/auth.md` §`POST /api/auth/login`)
  - **Forgot-password hint** (`.login-hint.muted`, `login.page.html:86`) — **plain text, not a link** (Normalize #5, shipped): `Quên mật khẩu? Liên hệ quản trị viên để được đặt lại.`. Positioned by a screen-local rule (`login.page.scss:12-15`) that pulls it up against `.field-row`. The global `.field-row a` / `.field-row a:hover` rules (`styles.scss` § `.field-row a`) are now **dead on this screen** — nothing inside `.field-row` is an anchor any more
  - **`Button`** (`Components/Button.md`, Primary + the `.btn-block` modifier, `styles.scss` § `.btn` and § `.btn-block`) — `width: 100%`, padding `spacing.button-block-padding` (`11px`), `typography.button-block-label`, centred flex with gap `spacing.sp-3`. Content: `pi pi-sign-in` glyph plus a label that swaps with `submitting()`. Carries `[disabled]="submitting()"`, which activates `.btn:disabled { opacity: .5; cursor: not-allowed }` (`styles.scss` § `.btn:disabled`)
- **`LanguageSwitcher`** (`shared/components/language-switcher/`) — **new 2026-09-05**, and this screen is the only place it appears. `<app-language-switcher class="login-language" />` sits **below** the form, inside the card (`login.page.html:99`), separated by a 1px `colors.line` rule with `spacing.sp-5` above and `spacing.sp-4` of padding (`login.page.scss:20-25`). Two buttons in a `role="group"` labelled from `shared.languageSwitcher.label`; the active one carries `aria-current="true"` and each carries `[attr.lang]` for WCAG 3.1.2 (`language-switcher.html:4-19`). It lives here because sign-in is the **only screen an unauthenticated user can reach** and it has no app shell — without it a viewer who only reads English has nowhere to switch before signing in. The choice persists in `localStorage`, so it carries into every screen after login. **Not yet indexed in `COMPONENTS.md`** — see Normalize #12
- **`Toast`** (`Components/Toast.md`, `.toast-stack`, `toast.scss:4-13`) — rendered by `<app-toast />` from **outside** the shell conditional (`app.html:41`), so it overlays this screen: `position: fixed`, `right`/`bottom` `spacing.sp-5`, `z-index: 60`, `max-width: min(dimension.toast-stack-max-width, 90vw)`, column gap `spacing.sp-3`, `aria-live="polite"` `role="status"` (`toast.html:1`). Each `.toast-item` is a `colors.card` surface with border 1px `colors.line`, a **5px** left rule tinted by severity (`colors.bad` for `error`), `rounded.md`, `shadow.toast`, padding `spacing.sp-4 spacing.sp-5`, plus a `dimension.toast-icon` (22px) severity disc, an optional bold `.toast-title` and an `.icon-btn` dismiss holding `pi pi-times` (`toast.scss:17-105`, `toast.html:3-21`)
- **Absent by construction:** no `<app-sidebar>`, no `<app-topbar>`, no `<main>` wrapper, no `Footer`, no breadcrumb.

<!-- Component gap — re-reviewed 2026-09-06. Indexed:
       AuthCard -> Components/AuthCard.md      AuthField -> Components/AuthField.md
       Input (.input-icon) -> Components/Input.md
       IconButton (.icon-btn) -> Components/IconButton.md
       Check -> Components/Check.md            Button (.btn.primary.btn-block) -> Components/Button.md
       Toast -> Components/Toast.md
     ONE GAP: LanguageSwitcher has no spec and no COMPONENTS.md row — see Normalize #12.
     The previous note claimed "nothing on this screen is undocumented"; that stopped being
     true the day the switcher landed (2026-09-05). -->

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

**An i18n layer now exists (verified 2026-09-05).** `@ngx-translate/core` v18 is installed, runtime language switching is wired through `LanguageService`, and translations live in `src/FE/public/i18n/{vi,en}.json`. Rules and key conventions: `doc/huong_dan/wiki-core/fe/08-i18n.md`.

This screen is **fully wrapped** — it was the end-to-end walk for the i18n phase, so every string below carries a real key. Other screens are not yet wrapped; gate **G12** (`scripts/fe-gate.sh`) lists what remains.

Two families of key appear in the column, and the difference is not cosmetic:

| Family | Looks like | Who owns the sentence |
| --- | --- | --- |
| Screen copy | `login.action.submit` — lowercase, `<screen>.<group>.<name>` | The FE. Written in `vi.json` / `en.json` |
| Error code | `AUTH.INVALID_CREDENTIALS` — uppercase, mirrors the BE `businessCode` | The **BE decides which code**; the FE decides the words. The envelope's `message` is only a fallback for codes with no translation |

Do **not** pre-fill this column with invented keys: a key written here before it exists in the source is a citation no one can check, and the whole point of this column is that every row can be traced to a `file:line`.

> **🔄 FLIPPED 2026-09-05.** This section previously read *"No i18n layer exists … Re-checked 2026-09-03: still true of the code"*. That is now false in the single source of truth for UI (`.claude/CLAUDE.md` §7), which is the worst place for it to be false. `check-docs.sh` could not catch it: the cited line numbers still exist inside their files, so mechanically nothing was broken — only the sentences were wrong. This is exactly the blind spot §8 of `.claude/CLAUDE.md` names: *prose describing something that does not exist*.

| Element | Verbatim copy (vi) | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `Đăng nhập · PlatformManager` | **`login.routeTitle`** + injected app name | `login.routes.ts:18` holds the **key**, not the sentence; `PageTitleStrategy` looks it up and re-looks-it-up on every language change (`page-title.strategy.ts:83,91-92`), joining with `TITLE_SEPARATOR = ' · '` (`src/FE/src/app/core/title/page-title.strategy.ts:8`). `noShell` hides the topbar heading but not the tab title |
| Brand mark | the product short name, currently `PM` | — (injected, `CORE_BRANDING.shortName`) | `auth-card.html:6` → `auth-card.ts` → `app.config.ts` § `APP_BRANDING` |
| Card heading | the product name, currently `PlatformManager` | — (injected, `CORE_BRANDING.name`) | `login.page.html:1` (`[title]` binding) → `login.page.ts` § `branding` → `app.config.ts` § `APP_BRANDING`. Not a screen title: this `<h1>` **is** the product name, so a second product built on this platform renders its own |
| Card subtitle | `Đăng nhập để tiếp tục` | **`login.subtitle`** | `login.page.html:1` |
| Field 1 label | `Tên đăng nhập` | **`shared.field.userName`** | `login.page.html:24`. A **promoted** key — the same label is used by `quan-tri-nguoi-dung` and `trang-chu`, so it lives under `shared.` (`fe/08-i18n.md` §Khuôn khoá dịch §4). `login.field.password` right below it is **not** promoted: it appears on this screen only |
| Field 1 placeholder | `vd nguyen.van.a` | **`login.field.userNamePlaceholder`** | `login.page.html:30` |
| Field 2 label | `Mật khẩu` | **`login.field.password`** | `login.page.html:40` |
| Field 2 placeholder | `Nhập mật khẩu` | **`login.field.passwordPlaceholder`** | `login.page.html:46` |
| Reveal toggle `aria-label` (masked) | `Hiện mật khẩu` | **`login.action.showPassword`** | `login.page.html:56-58` |
| Reveal toggle `aria-label` (revealed) | `Ẩn mật khẩu` | **`login.action.hidePassword`** | `login.page.html:56-58` |
| Remember-me label | `Ghi nhớ đăng nhập` | **`login.field.rememberMe`** | `login.page.html:78` |
| Forgot-password hint | `Quên mật khẩu? Liên hệ quản trị viên để được đặt lại.` | **`login.hint.forgotPassword`** | `login.page.html:86` |
| Submit button (idle) | `Đăng nhập` | **`login.action.submit`** | `login.page.html:90` |
| Submit button (submitting) | `Đang đăng nhập…` | **`login.action.submitting`** | `login.page.html:90` |
| Language switcher group label (`aria-label`) | `Ngôn ngữ` | **`shared.languageSwitcher.label`** | `language-switcher.html:4` |
| Language switcher options | `Tiếng Việt` · `English` | — (**not** translated, by design) | `app.config.ts:99,106` (`ICoreLanguage.label`), read through `LanguageService.languages` (`language-switcher.ts:51`). Each language is written in **its own** language so it reads the same whichever locale is active; the reasoning is in-source at `language-switcher.ts:18-25`, and each button also carries its own `lang` for WCAG 3.1.2 |
| Inline error — client guard | `Vui lòng nhập đầy đủ tài khoản và mật khẩu.` | **`login.error.missingCredentials`** | `login.page.ts` § `onSubmit()` — the **key** is stored, not the sentence (`login.page.ts` § `clientErrorKey`) |
| Inline error — invalid credentials | `Tên đăng nhập hoặc mật khẩu không đúng.` | **`AUTH.INVALID_CREDENTIALS`** | `login.page.ts` § `errorMessage` → `ApiErrorMessageService.messageFor` → `api-error-message.service.ts:103-109` |
| Inline error — locked out | `Tài khoản đã bị khoá — liên hệ quản trị viên.` | **`AUTH.LOCKED_OUT`** | same path |
| Inline error — rate limited | `Bạn đã gửi quá nhiều yêu cầu — vui lòng thử lại sau.` | **`RATE_LIMIT.TOO_MANY_REQUESTS`** | same path |
| Inline error — per-field (400) | `Vui lòng nhập <nhãn ô>.` | **`VALIDATION.NotEmptyValidator`** | `login.page.ts` § `fieldMessages()` reads `fieldErrors`, `api-error-message.service.ts:133-140` builds the sentence. The `{{PropertyName}}` parameter is **overridden with the screen's own field label** (`login.page.ts` § `FIELD_LABEL_KEYS`, § `labelOverrideFor()`) — FluentValidation sends `"User Name"`, an English string generated from a C# property name, which cannot be translated |
| Inline error — code with no translation | `{envelope.message}` verbatim | — (server-supplied **fallback**) | `api-error-message.service.ts:105-108`, priority step 2. Deliberate: a new BE code with no FE translation still says what happened, in Vietnamese |
| Inline error — fallback, no envelope | `Đăng nhập thất bại — thử lại sau.` | ~~gone~~ — now **`shared.httpError.unexpected`** (`Đã có lỗi xảy ra. Vui lòng thử lại.`) | The screen-local sentence was **deleted** 2026-09-05. Both channels now fall back through the same `FALLBACK_KEYS` table (`api-error-message.service.ts:13-19,40-43`), so the toast and the inline block can no longer say two different things about one failure |
| Toast — same failure | identical sentence to the inline block | — (same service) | `http-error.interceptor.ts` § `messageFor` / `titleFor` calls the **same** `ApiErrorMessageService`. Still duplicated on screen — see Normalize #1 |
| Toast — fallback, no connection | title `Mất kết nối` + `Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.` | **`shared.httpError.offlineTitle`** / **`shared.httpError.offline`** | `api-error-message.service.ts:14` |
| Toast — fallback, 401 | title `Chưa đăng nhập` + `Bạn cần đăng nhập để tiếp tục.` | **`shared.httpError.unauthenticatedTitle`** / **`…unauthenticated`** | `api-error-message.service.ts:15` |
| Toast — fallback, 403 | title `Không đủ quyền` + `Bạn không có quyền thực hiện thao tác này.` | **`shared.httpError.forbiddenTitle`** / **`…forbidden`** | `api-error-message.service.ts:16` |
| Toast — fallback, 404 | title `Không tìm thấy` + `Không tìm thấy dữ liệu yêu cầu.` | **`shared.httpError.notFoundTitle`** / **`…notFound`** | `api-error-message.service.ts:17` |
| Toast — fallback, 429 without envelope | title `Quá nhiều yêu cầu` + `Bạn đã gửi quá nhiều yêu cầu — vui lòng chờ một lát rồi thử lại.` | **`shared.httpError.tooManyTitle`** / **`…tooMany`** | `api-error-message.service.ts:18` |
| Toast — fallback, anything else | title `Lỗi hệ thống` + `Đã có lỗi xảy ra. Vui lòng thử lại.` | **`shared.httpError.unexpectedTitle`** / **`…unexpected`** | `api-error-message.service.ts:40-43` |
| Toast dismiss `aria-label` | `Đóng thông báo` | **`shared.toast.dismiss`** | `toast.html:16` |
| Native validation bubble (empty required field) | browser-supplied, locale-dependent (e.g. Chrome: `Please fill out this field.`) | — (user-agent) | emergent from `required` at `login.page.html:32,48` with no `novalidate` on the `<form>` |

> ### 🔄 SỬA 2026-09-06 — the whole table was one revision behind the wrapping it announces
>
> The paragraphs above this table said, correctly, *"this screen is fully wrapped — every
> string below carries a real key"*. The table below them then listed **`— (hardcoded)`
> for fourteen consecutive rows**, and every `login.page.html` line number in it was
> stale by 10–40 lines. Two facts were also wrong in substance, not just in citation:
>
> | The table said | As shipped (verified 2026-09-06) |
> | --- | --- |
> | Field 1 label `Email`, placeholder `ten@congty.vn` | `Tên đăng nhập` / `vd nguyen.van.a` — decision 6, shipped |
> | Forgot-password **link** `Quên mật khẩu?` | plain **text**, full sentence — decision 5, shipped |
>
> Nothing in `check-docs.sh` could have caught this: the file was longer than every cited
> line number, so §6 passed. Only reading the template does.

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **idle (default):** reached after the app-init probe resolves — `provideAuthInit()` awaits `GET /api/auth/me` once at bootstrap (`auth-init.provider.ts:11-16`), and that request carries `SKIP_ERROR_TOAST`, so an anonymous visitor's 401 produces **no toast and no redirect** (`current-user.service.ts:43,50`, `http-error.interceptor.ts:86,100-101,122`). Both text signals empty → placeholders visible; `showPassword()` false → password masked with `pi-eye` shown; `submitting()` false → submit enabled reading `Đăng nhập`; `errorMessage()` null → the `.login-error` element is **absent from the DOM entirely** (`@if`, not a CSS hide). The base rule `.login-error { display: none }` (`styles.scss` § `.login-error`) is therefore dead in Angular — the class is only ever emitted together with `.show`. A visitor who already holds a session and navigates back here never sees this state: `redirectAfterAuth()` runs in the constructor (`login.page.ts` § `constructor()`).
- **idle with `returnUrl`:** identical rendering — the deep link is carried **only in the URL**, e.g. `/dang-nhap?returnUrl=%2Fquan-tri%2Fnguoi-dung`, and nothing on the card acknowledges it (no "sign in to continue to X" copy). **Two** producers now: `authGuard`, when an anonymous user hits a protected route (`auth.guard.ts:26`), and — new 2026-09-05 — `httpErrorInterceptor`, when a live session dies mid-use (`http-error.interceptor.ts:114,120`). Both build the same shape on purpose. On success the value is honoured only if it starts with `/`, otherwise `CORE_ROUTES.home` is used (`login.page.ts` § `redirectAfterAuth()` — the `returnUrl.startsWith('/')` branch) — an open-redirect guard. **`mustChangePassword` outranks it**: when true, `redirectAfterAuth()` returns early to `CORE_ROUTES.changePassword` and the `returnUrl` is silently dropped, never resumed after the password change (`login.page.ts` § `redirectAfterAuth()`). Neither destination is hard-coded any more — both come from the injected `CORE_ROUTES` token (`login.page.ts` § `routes = inject(CORE_ROUTES)`), because `platform/` ships as part of CoreBase.
- **submitting:** `submitting()` true → the submit button gets `[disabled]` (`login.page.html:88`), rendering at `opacity: .5` with `cursor: not-allowed` (`styles.scss` § `.btn:disabled`), and its label swaps to `Đang đăng nhập…` while the `pi pi-sign-in` glyph stays. There is **no spinner and no progress indicator anywhere**. Nothing else locks: both inputs, the reveal toggle and the checkbox remain enabled and editable for the whole round-trip. Both error sources are cleared immediately before the request (`clearError()`, `login.page.ts` § `onSubmit()` → § `clearError()`), so a previous error block disappears the moment a resubmit starts.
- **empty:** there is **no empty state on this screen, and there cannot be one.** An empty state needs a collection that can come back with nothing; this card renders none — the whole form is statically present (`login.page.html:13-92`), with exactly one `@if` — the error block at `login.page.html:6-11` — and one `@for`, inside `LanguageSwitcher`, over a **static** two-element list that can never come back empty (`language-switcher.html:5`). The only fetch in the flow is the bootstrap `/auth/me` probe, whose "nothing here" answer — an anonymous 401 — is deliberately rendered as nothing at all, leaving the card in **idle**.
- **field-validation:** there is still **no per-field error slot on this screen** — no `.invalid` binding, no `aria-invalid`, no `.form-error`, no helper text. (The library supports all three; `/doi-mat-khau` uses them and `/dang-nhap` does not — see that screen's States, and Normalize #9 here.) Validation now arrives in **three** layers. (1) *Native*: both inputs are `required` and the `<form>` has no `novalidate`, so an empty field blocks the `submit` event and the browser shows its own bubble. (2) *Component guard*: `login.page.ts` § `onSubmit()` stores the **key** `login.error.missingCredentials` — not the sentence — and the `.login-error` block resolves it on read. Because layer 1 fires first, layer 2 is only reachable when the username is non-empty but whitespace-only (`.trim()` at `login.page.ts` § `onSubmit()`). This path never produces a toast: it never issues an HTTP request. (3) *Server field errors*, **new 2026-09-05**: a 400 carrying `fieldErrors` is translated per entry and the sentences are **joined with a space into the one shared block** (`login.page.ts` § `errorMessage` — the `fieldLines.join(' ')` branch), because this screen has no per-field slot to put them in. The block therefore beats the envelope-level `Dữ liệu không hợp lệ.`, which says nothing useful to someone filling a form.

  Storing the **key** rather than the rendered sentence is what makes the block survive a language switch: `errorMessage` is a `computed` that re-reads `language.current()` on every evaluation (`login.page.ts` § `errorMessage`). Cache the sentence instead and a user who switches language while an error is showing gets one Vietnamese line stranded in an English interface, with nothing to signal it.
- **server-error (`AUTH.INVALID_CREDENTIALS`, 422):** **both channels read `businessCode`** (changed 2026-09-05). The page resolves the sentence through `ApiErrorMessageService.messageFor` (`login.page.ts` § `errorMessage`) and the interceptor through the same service (`http-error.interceptor.ts` § `messageFor`) — one lookup layer, two display surfaces, so a code with a translation renders in the viewer's language instead of the server's Vietnamese. Priority is fixed and deliberate: translated `businessCode` → envelope `message` → HTTP-status fallback (`api-error-message.service.ts:103-109`). What did **not** change: the sentence still reaches the user **twice** (inline block *and* toast) — Normalize #1.

  What **did** change beyond the lookup: the two channels no longer diverge on the no-envelope path. The screen-local fallback `Đăng nhập thất bại — thử lại sau.` was deleted; both channels now land on `shared.httpError.*` from the same table, differing only in that the toast adds a bold title and the inline block does not (`api-error-message.service.ts:118-121`) — Normalize #2, still open but narrower.
- **locked-out (`AUTH.LOCKED_OUT`, 422 — `doc/contracts/auth.md:163`):** renders **exactly like the invalid-credentials state** — same inline block, same duplicate toast, same `colors.bad` treatment; only the server-supplied sentence differs. The UI offers no distinct affordance: no elevated severity, no "contact an administrator" hint, no disabled form — even though this is the one auth failure the user cannot clear alone (an admin must unlock the account).
- **rate-limited (429, `RATE_LIMIT.TOO_MANY_REQUESTS`):** triggered by the 6th login attempt inside a minute from the same IP — policy is 5/min/IP fixed window, stacking on a **200/min/IP sliding-window** global limiter and a third limiter partitioned by **username** (10 per 5 min, sliding) — `src/BE/PlatformManager.Api/Program.cs:287,294,306`. The third one is the reason a distributed attack on one account 429s at the 11th try no matter how many IPs it comes from. Renders identically again. `RATE_LIMIT.TOO_MANY_REQUESTS` **does** have a translation, so the shown sentence is the FE's `Bạn đã gửi quá nhiều yêu cầu — vui lòng thử lại sau.` — which, unlike the server's `message`, does **not** carry the number of seconds. `Retry-After` is on the response and is **never read**: no countdown, no cooldown, no temporarily disabled submit — the button re-enables the instant the response lands. The interceptor's 429 fallback (`api-error-message.service.ts:18`) appears only when a 429 arrives with no envelope, i.e. from a proxy ahead of the API. Distinguishing this from locked-out matters and the UI does not help: **429 is the IP and clears itself within ≤ 1 minute; 422 `AUTH.LOCKED_OUT` is the account and needs an admin** (`doc/contracts/auth.md:255-256`).
- **success:** no confirmation is rendered — `AuthService.login()` invalidates the cached menu, sets the user context and re-primes the CSRF cookie (`auth.service.ts:27-43`; the re-prime is required because an antiforgery token issued while anonymous is rejected after the identity changes — `doc/contracts/auth.md` §CSRF), then the component navigates away immediately (`login.page.ts` § `onSubmit()` → the `next` handler). There is no success toast.

### Responsive

<!-- Behavior per breakpoint. -->

- **All viewports — there is no breakpoint.** Neither `auth-card.scss`, `login.page.scss`, nor the global auth block (`styles.scss` § `.field` … `.login-error.show`) contains a single `@media` rule. The shell breakpoints that shape every other screen (`breakpoint.tablet` 980px / `breakpoint.mobile` 560px, in `app.scss`, `sidebar.scss`, `topbar.scss`) do not apply here, because the shell is not rendered at all.
- **Fluid behaviour instead of breakpoints:** `.login-card { width: 100%; max-width: dimension.login-card-max-width }` (`auth-card.scss:11-12`) means the card is a fixed 380px on any viewport wider than roughly 408px, and below that it shrinks to fill, kept off the edges by the shell's `spacing.sp-5` padding (`auth-card.scss:7`). Internal padding stays `spacing.auth-card-padding` at every width — it does **not** tighten on small screens.
- **Viewport height:** `min-height: 100vh` immediately re-declared as `min-height: 100dvh` (`auth-card.scss:2-3`) so the card stays optically centred on mobile browsers whose toolbars change the visible height; the second declaration wins wherever `dvh` is supported and is ignored as an unknown unit elsewhere.
- **Viewport meta:** `width=device-width, initial-scale=1, viewport-fit=cover` (`src/FE/src/index.html:7`) — `viewport-fit=cover` is set globally, but neither auth screen consumes `env(safe-area-inset-*)`, so on a notched device the card relies purely on the `spacing.sp-5` shell padding.
- **Toast overlay:** the only viewport-reactive rule reaching this screen is `.toast-stack { max-width: min(dimension.toast-stack-max-width, 90vw) }` (`toast.scss:12`), which narrows the error toast on small screens while its `right`/`bottom` offsets stay fixed.
- **Reduced motion:** `@media (prefers-reduced-motion: reduce)` flattens every animation and transition app-wide to `.01ms` (`styles.scss` § `@media (prefers-reduced-motion: reduce)`). On this screen that reaches the button and input transitions and the toast's `toast-in` slide.
- **Print (`@media print`):** the global rule hides `.no-print` (`styles.scss` § `.no-print`). No element of the card carries that class, so it prints exactly as it renders; the toast stack does carry it (`toast.html:1`) and is the one thing that disappears.

### Iconography

<!-- Refs into Icons.md -->

The app loads **PrimeIcons v7** as a global stylesheet (`angular.json:39` build, `angular.json:101` test, from `node_modules/primeicons/primeicons.css`; `package.json:53`). Icons are `<i class="pi pi-*">` elements, not SVG. Sizing on this screen: field glyphs are `font-size: 15px`, ink `colors.muted`, absolutely positioned at `left: 12px` with `pointer-events: none`, and the input's `padding-left: 36px` reserves their gutter (`styles.scss` § `.input-icon > .pi` / `.input-icon input`); the submit button's icon-to-label gap is the `.btn-block` `gap: spacing.sp-3` (`styles.scss` § `.btn-block`). Full map: `Icons.md`.

| Action | Icon | Placement |
| --- | --- | --- |
| Error notice (inline) | `pi pi-exclamation-circle` | Leading, inside `.login-error` (`login.page.html:8`) |
| Username field adornment | `pi pi-user` | Leading, absolute `left: 12px` inside `.input-icon` (`login.page.html:26`) |
| Password field adornment | `pi pi-lock` | Leading, absolute `left: 12px` inside `.input-icon` (`login.page.html:42`) |
| Reveal password | `pi pi-eye` | Trailing, inside the `.icon-btn`, shown while masked (`login.page.html:60`) |
| Hide password | `pi pi-eye-slash` | Trailing, same button, shown while revealed (`login.page.html:60`) |
| Submit sign-in | `pi pi-sign-in` | Leading, inside the primary `.btn.btn-block`, kept in place during `submitting()` (`login.page.html:89`) |
| Toast severity (error) | `pi pi-times` | Inside the `.toast-icon` disc (`toast.html:6`, severity map at `toast.ts:11-16`) |
| Dismiss toast | `pi pi-times` | Trailing, on the toast's `.icon-btn` (`toast.html:19`) |
| Remember-me checkbox | — (native `<input type="checkbox" class="check">`, no icon) | `login.page.html:72-77` |
| Forgot-password hint | — (**plain text**, no icon and no longer a link) | `login.page.html:86` |
| Language switcher | — (**text buttons**, no flag icons) | `language-switcher.html:9-18`. Flags are deliberately absent: a flag names a country, not a language |

> 🔄 **SỬA 2026-09-06.** The username adornment is `pi pi-user`, not `pi pi-envelope` — it changed
> with the Email → Tên đăng nhập relabel (decision 6). Every line number in this table was also
> stale, and the last row described a link that no longer exists.

### Screenshots

<!-- Refs into Assets/Screenshots/auth/ -->

> ⚠️ **The one existing shot is STALE, and now in a third way.** `sign-in--desktop-1440.png` was captured 2026-08-22, before `styles.scss` was rewritten on 2026-08-29. Its palette and font are wrong for that reason. **Its layout is wrong too, as of 2026-09-05**: it predates the language switcher (a bordered block below the form), the Email → Tên đăng nhập relabel, and the forgot-password link becoming a sentence. Nothing in the shot below the password field matches what ships (`--bg` `#eef2f8` → `colors.bg` `#cfdaea`, `--line` `#dfe6ef` → `colors.line` `#7a97bd`, `Inter` → Be Vietnam Pro). It is kept rather than deleted so the before/after stays visible, and it is **not** an authority on colour or type. Same verdict as `UiInventory.md` § Screenshot Manifest.

Under `doc/Design/CLAUDE.md` § Rules the target is **one desktop shot per screen**; every other row below is an on-demand variant, not an outstanding debt. Reproducible capture procedure (nothing here modifies `src/**`):

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`doc/Design/CLAUDE.md`](../../../CLAUDE.md) § Rules

- The backend is needed only for the rows marked *(BE required)*; the anonymous rows render with the frontend alone. **Never record credentials in this file.**
- Use a fresh browser profile with no `PlatformManager.Auth` cookie, otherwise the constructor redirect fires and the sign-in card never renders (`login.page.ts` § `constructor()`).

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/auth/sign-in--desktop-1440.png` | **stale in layout, palette and copy** (captured 2026-08-22) — recapture is the highest-value item on this screen | `/dang-nhap` @ 1440×900, anonymous profile, idle state. No BE session needed — the `/auth/me` probe fails silently. Reached from `/`, so `authGuard` appends `?returnUrl=%2F` |
| `Assets/Screenshots/auth/sign-in--mobile-390.png` | on demand | Same URL/state @ 390×844. Proves the no-breakpoint fluid clamp: the card is `min(380px, 100% − 2×spacing.sp-5)`, internal padding unchanged |
| `Assets/Screenshots/auth/sign-in--server-error--desktop-1440.png` | on demand *(BE required)* | Submit a wrong password once → 422 `AUTH.INVALID_CREDENTIALS`. Capture @ 1440×900 within 5s of submit so the inline block **and** the duplicate toast are both in frame (auto-dismiss at 5000 ms, `toast.service.ts:19,64`) |
| `Assets/Screenshots/auth/sign-in--rate-limited--desktop-1440.png` | on demand *(BE required)* | Submit 6 times inside one minute from the same IP → 429. Capture @ 1440×900 within 5s, framing inline block + toast; the toast text contains the server's own `Retry-After` seconds |
| `Assets/Screenshots/auth/sign-in--locked-out--desktop-1440.png` | on demand *(BE required)* | Have an admin lock the account, then sign in → 422 `AUTH.LOCKED_OUT`. Capture @ 1440×900. Exists to document that this renders identically to the invalid-credentials shot apart from the sentence |
| `Assets/Screenshots/auth/sign-in--return-url--desktop-1440.png` | on demand | Navigate to `/quan-tri/nguoi-dung` while anonymous; `authGuard` rewrites the URL to `/dang-nhap?returnUrl=%2Fquan-tri%2Fnguoi-dung`. Capture @ 1440×900 **with the address bar visible** — the query param is the only place this state is observable. Low value: the rendered card is identical to idle |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. A quirk that spans components belongs in the component's own spec (`Components/<Name>.md` → Normalize on redesign), not here. -->

> ### ✅ Decided 2026-08-31 — items 4, 5, 6 and 10 — **ALL FOUR SHIPPED** (verified 2026-09-06)
>
> The list below states each problem and its options; these are the choices made. The
> numbered items stay as written, because the reasoning in them is what the decisions rest on —
> but each now carries a **✅ SHIPPED** line naming the code that closed it. Read the numbered
> item for *why*, the shipped line for *what is there now*.
>
> | # | Decision |
> |---|---|
> | **6** | The field is a **username**, not an email. Relabel to `Tên đăng nhập`, replace the `ten@congty.vn` placeholder, and swap the envelope glyph for a person glyph. The backend is not changed — `POST /api/auth/login` already takes `userName` |
> | **4** | **Wire the checkbox**, do not remove it. Unchecked ⇒ session cookie that dies with the browser; checked ⇒ the current 14-day sliding cookie. **Default unchecked** |
> | **5** | Replace the dead link with plain text: *"Quên mật khẩu? Liên hệ quản trị viên để được đặt lại."* An administrator can already reset a password through `PUT /api/users/{id}`, so this describes a path that exists |
> | **10** | Give `.login-error` `role="alert"`. Cheap, and without it a screen-reader user gets no announcement that the sign-in failed |
>
> **Why item 4 was the one that mattered most.** It was not a dead control that merely did
> nothing — it implied a choice that did not exist, and the invisible default was the less
> safe side. `signInManager.SignInAsync(user, isPersistent: true)` was hard-coded, so **every**
> sign-in got a cookie that survived the browser closing and renewed on use. A user who left
> "Ghi nhớ đăng nhập" unticked reasonably believed the opposite. On a shared machine, the next
> person to open the browser was inside that session.
>
> It was a contract change, not only a screen change, and the contract moved too:
> [`doc/contracts/auth.md`](../../../../contracts/auth.md) §`POST /api/auth/login`. Shipped
> end to end — `IdentityService.cs:55` (`isPersistent: rememberMe`), `LoginCommand.cs:16`
> (default `false`), `login.page.html:72-77`, `login.page.ts` § `rememberMe`, § `onSubmit()`.
>
> Item **7** was also closed, by a change nobody filed under this list: `httpErrorInterceptor`
> now redirects on a mid-session 401 (see #7 below).
>
> Items 1, 2, 3, 8, 9 and 11 are still **not** decided. Item **12** is new.

1. **Every server failure produces two notifications of the same text.** *(Still open — but narrower since 2026-09-05: both channels now resolve the sentence through the same `ApiErrorMessageService`, `http-error.interceptor.ts` § `messageFor` and `login.page.ts` § `errorMessage`, so it is now provably the **same** sentence twice rather than two sentences that happened to match.)* Pick one channel for form-scoped failures — inline is the better fit — and let the toast handle only errors with no form to attach to, or opt the login request out with `SKIP_ERROR_TOAST` (`http-context-tokens.ts`), the mechanism the `/auth/me` probe already uses.
2. **The two channels disagree on shape, not just on count.** *(Still open, and now the ONLY difference between them.)* The toast carries a bold title in its fallback branch (`Mất kết nối`, `Chưa đăng nhập`, …) while the inline block never has one, so the same failure reads as two differently structured messages stacked on one screen. The sentence itself no longer diverges — `titleFor()` returns `undefined` whenever the envelope carries a real sentence, so the title only appears on the fallback path (`api-error-message.service.ts:118-121`).
3. **The three failures a user must respond to differently look identical.** *(Still open. Half-closed 2026-09-05: `businessCode` **is** read now — but only to pick the wording, not to change the treatment.)* Invalid credentials (retry), locked out (call an admin), rate-limited (wait ≤ 1 min) all render the same red block with the same glyph. `AUTH.LOCKED_OUT` deserves distinct copy with a next step, and 429 should read `Retry-After` and disable the submit for that many seconds instead of letting the user re-trigger the limiter.
4. **"Ghi nhớ đăng nhập" is inert.** ✅ **SHIPPED.** The `.check` had no `[checked]`, no `(change)`, no signal and no participation in the login call — the cookie's 14-day sliding lifetime was decided entirely server-side (`doc/contracts/auth.md:489`). It is now wired end to end and defaults to **unchecked**: `login.page.html:72-77` → `login.page.ts` § `rememberMe` → § `onRememberMeChange()` → § `onSubmit()` → `LoginCommand.cs:16` → `IdentityService.cs:55`. Unchecked ⇒ session cookie; checked ⇒ the 14-day sliding cookie.
5. **"Quên mật khẩu?" leads nowhere.** ✅ **SHIPPED.** It was `href="javascript:void(0)"` with no handler and no route; there is still no password-reset endpoint in `doc/contracts/auth.md`. It is now **plain text** describing the path that does exist — `Quên mật khẩu? Liên hệ quản trị viên để được đặt lại.` (`login.page.html:86`, key `login.hint.forgotPassword`), styled by `login.page.scss:12-15`. An administrator resets a password through `PUT /api/users/{id}`.
6. **The field is labelled "Email" but holds a username.** ✅ **SHIPPED.** The label is now `Tên đăng nhập` (`shared.field.userName`, `login.page.html:24`), the placeholder `vd nguyen.van.a` (`login.field.userNamePlaceholder`, `src/FE/src/app/platform/login/pages/login/login.page.html:30`), and the adornment `pi pi-user` (`src/FE/src/app/platform/login/pages/login/login.page.html:26`). The input stays `type="text"` on purpose — `type="email"` would make the browser reject a valid username through native validation. **The API did not change**: the payload field was always `userName`.
7. **`returnUrl` is destroyed by the forced password change.** *(Still open — do not confuse it with the mid-session 401 case, which IS closed.)* `redirectAfterAuth()` returns early to `CORE_ROUTES.changePassword` before the `returnUrl` branch is reached (`login.page.ts` § `redirectAfterAuth()` — the `mustChangePassword()` early return) and the change-password screen then goes to `CORE_ROUTES.home` on success (`doi-mat-khau.page.ts` § `onSubmit()` → `navigateByUrl(this.routes.home)`). A first-login user who followed a deep link is silently dumped on the home screen. Carry the value through the change-password step and resume it afterwards.

    **What DID close:** a 401 arriving mid-session now clears the client context and navigates to the sign-in screen carrying `returnUrl=<where they were>` (`http-error.interceptor.ts:100-121`), with three deliberate non-redirect cases — the bootstrap probe, being already on the sign-in screen, and the 2nd..n-th parallel 401 of the same death (flag declared at `src/FE/src/app/core/interceptors/http-error.interceptor.ts:36`, guarded at `src/FE/src/app/core/interceptors/http-error.interceptor.ts:107-108`). That was the change-password screen's Normalize #7, not this one.
8. **Only the button locks while submitting.** *(Still open.)* Both fields, the reveal toggle and the checkbox stay live for the whole request (`login.page.html:27-77`), so a user can edit the username after the request left the client and see a result that no longer matches what is on screen. The library already ships the treatment — `:disabled` on the shared input contract paints `colors.surface-track` with `cursor: not-allowed` (`styles.scss` § `.input:disabled`) — and **no auth screen binds it**. Disable the fieldset, or at least the inputs, for the duration.
9. **Field-level validation exists in the library and on the sibling screen, but not here.** *(Still open, and now asymmetric in a second way.)* `/doi-mat-khau` binds `.invalid` + `aria-invalid` + `aria-describedby` and renders a `.form-error` per field; `/dang-nhap` still funnels everything into one shared slot. The per-field data **is** now read and translated (`login.page.ts` § `fieldMessages()`) — it is simply space-joined into the single block because there is nowhere else to put it (`login.page.ts` § `errorMessage` — the `fieldLines.join(' ')` branch). Two screens in one flow with two different validation contracts is the sharper problem — copy the change-password pattern across. Note that the sibling screen is **no longer** behind in the other direction: as of 2026-09-06 it reads `fieldErrors` (code + params) too (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:241` and `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:167`), so both screens translate server field errors — the difference left is only *where the sentence lands*, per-field there and space-joined into one block here. Recorded 2026-09-08; this sentence previously said the sibling still read the legacy `fields`.
10. **Error announcement is not wired for assistive tech.** ✅ **SHIPPED (the live region half).** `.login-error` now carries `role="alert"` (`login.page.html:7`) — `alert` rather than `status` because this is an error blocking the task and is allowed to interrupt. The block is still **not** associated with either input via `aria-describedby`; that half belongs to #9. Dropping the duplicate toast is #1, still open.
11. **Untokenised literals inside a token-driven card.** *(Still open.)* `border-radius: 12px` on the brand mark (`auth-card.scss:31`), `margin-bottom: 24px` on the brand block (`auth-card.scss:25`), `margin-bottom: 20px` on `.field-row` (`styles.scss` § `.field-row`), `left: 12px` / `font-size: 15px` on the field glyph and `right: 10px` on the reveal button (`styles.scss` § `.input-icon > .pi` / `.input-icon .icon-btn`). All are catalogued in `Tokens/spacing.md`; promote them or make a deliberate decision to keep them.

12. **`LanguageSwitcher` ships without a spec, and `.field-row` now has dead rules under it.** *(New 2026-09-06.)* Two loose ends from the i18n phase:

    - `shared/components/language-switcher/` is a real shared component rendered on a documented screen, but it has **no entry in `COMPONENTS.md` and no `Components/*.md` file**. Its own source says so (`language-switcher.ts:34-37`). It invents no new token — it composes `--muted`, `--brand`, `--sp-*`, `--fs-sm`, `--radius-pill` — so the gap is placement and states, not colour. Until it is indexed, the "Component gap — nothing on this screen is undocumented" note above this section is **false for this screen**.
    - `.field-row a` and `.field-row a:hover` (`styles.scss` § `.field-row a`) styled the forgot-password link. Item 5 removed the only anchor those rules ever matched. They are now dead on this screen and on every other — decide whether `.field-row` is allowed to hold links at all, then either keep the rules with a stated reason or drop them.

---

## Change password (`/doi-mat-khau`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

Structurally the same card as sign-in — same `AuthCard`, same global `.field` / `.input-icon` / `.login-error` / `.btn-block` block, same absent shell. Only the contents differ, plus **one genuine structural addition**: a per-field error slot.

- **`AuthCard`** (`Components/AuthCard.md`) — auth shell → auth card → brand block, exactly as specified under Sign in (`auth-card.scss:1-52`). The brand mark still reads `PM`; the `<h1>` is bound to the translated `shared.action.changePassword` (`doi-mat-khau.page.html:2`) and the `<p>` to a **computed subtitle KEY** that changes with `isForced()` (`doi-mat-khau.page.ts` § `subtitleKey`), translated in the template (`doi-mat-khau.page.html:3`) — the only place in the flow where copy changes at runtime
- **`AuthField`** form-level error block (`.login-error.show`, `doi-mat-khau.page.html:7-12`) — **no `role="alert"` here**, unlike sign-in (Normalize #10) — same element, same `@if` gate, same `pi pi-exclamation-circle`. **Its scope narrowed on 2026-08-29**: it now carries only errors with **no field key**; anything with a `fieldErrors` map renders under its own input instead, and the block is suppressed entirely while any field error exists (`doi-mat-khau.page.ts` § `errorMessage`). ⚠️ **Corrected 2026-09-08 (twice over).** This clause said `fields`, the legacy key the page stopped reading on 2026-09-06 — and it gave `AUTH.CHANGE_PASSWORD_FAILED` 422 as the example of a message-only error, *"where Identity puts its detail in `message`"*. That stopped being true on 2026-09-05, when the BE moved the Identity detail out of `message` into `fieldErrors` and fixed `message` to the flat sentence `Đổi mật khẩu thất bại.` — so the one example given is now the clearest case of an error that does **not** land in this block (§ States → server-error records the current behaviour)
- **Form** (`<form (submit)>`, `doi-mat-khau.page.html:21-91`) — native, no `novalidate`, plain `[value]` / `(input)` signal wiring; **three** fields, all `type="password"`, all `required`, none with a reveal toggle. Each is a `.field` holding a label, an `Input` in its `.input-icon` variant, and an optional `FormRow` error line:
  - **Field: Mật khẩu hiện tại** (`doi-mat-khau.page.html:22-41`) — leading `pi pi-lock`, `autocomplete="current-password"`
  - **Field: Mật khẩu mới** (`doi-mat-khau.page.html:44-63`) — leading `pi pi-key`, `autocomplete="new-password"`
  - **Field: Xác nhận mật khẩu mới** (`doi-mat-khau.page.html:66-85`) — leading `pi pi-key`, `autocomplete="new-password"`
  - **Per-field invalid treatment**, on all three — three signals bound together and never separately (`doi-mat-khau.page.html:14-20` states the rule in-source, citing WCAG 2.2 AA):
    - `[class.invalid]` → border `colors.bad`, and on focus a `shadow.focus-ring-invalid` ring instead of the brand one (`styles.scss` § `.input.invalid` / `.input.invalid:focus-visible`)
    - `[attr.aria-invalid]` → `'true'` or `null`; never the string `"false"`
    - `[attr.aria-describedby]` → the id of that field's error div, or `null`. All three are `null` together when the field is clean, because pointing `aria-describedby` at a non-existent element is a real a11y defect rather than a harmless one
  - **`FormRow` error line** (`Components/FormRow.md`, `.form-error`, `styles.scss` § `.form-error`) — `<div class="form-error" [id]="errorId(field)">`, ink `colors.bad`, `typography.notice` size, margin `spacing.sp-2` block. Rendered inside the `.field`, **below** the `.input-icon`, only when that field has a message (`doi-mat-khau.page.html:39-41,61-63,83-85`)
  - **`Button`** (Primary + `.btn-block`) — `[disabled]="submitting()"`, label swapping with it. **No icon** — unlike sign-in's `pi pi-sign-in`, this button is text-only (`doi-mat-khau.page.html:88-90`)
- **`Toast`** (`Components/Toast.md`) — same overlay as Sign in, present because `<app-toast />` sits outside the shell conditional (`app.html:41`)
- **Absent:** no `.field-row` options row, no remember-me, no forgot-password link, no reveal toggle, no password-strength meter, no rules hint stating the 8-character minimum the component enforces, and **no escape hatch** — no "sign out" or "do this later" control, which matters because in the forced case this card is the only reachable screen.

<!-- Component gap — reviewed 2026-08-29. AuthCard, AuthField, Input (.input-icon), FormRow
     (.form-error), Button (.btn.primary.btn-block) and Toast are all indexed in COMPONENTS.md.
     IconButton and Check appear on Sign in only. Nothing here is undocumented. -->

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `Đổi mật khẩu · PlatformManager` | **`doi-mat-khau.routeTitle`** + injected app name | `doi-mat-khau.routes.ts:15` holds the **key** → `page-title.strategy.ts:83,91-92`. `noShell` hides the topbar heading but **not** the tab title — both are fed from the same route-level `title`, and both now follow the language switch |
| Brand mark | the product short name, currently `PM` | — (injected, `CORE_BRANDING.shortName`) | `auth-card.html:6` |
| Card heading | `Đổi mật khẩu` | **`shared.action.changePassword`** | `doi-mat-khau.page.html:2` (`title` input). A **promoted** key: the same words label the home screen's button into this flow |
| Card subtitle — forced (`mustChangePassword === true`) | `Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.` | **`doi-mat-khau.hint.forcedSubtitle`** | `doi-mat-khau.page.ts` § `subtitleKey` picks the **key**; the template translates it (`doi-mat-khau.page.html:3`) |
| Card subtitle — voluntary | `Đổi mật khẩu tài khoản của bạn.` | **`doi-mat-khau.hint.subtitle`** | same pair |
| Field 1 label | `Mật khẩu hiện tại` | **`doi-mat-khau.field.currentPassword`** | `doi-mat-khau.page.html:23` |
| Field 1 placeholder | `Nhập mật khẩu hiện tại` | **`doi-mat-khau.field.currentPasswordPlaceholder`** | `doi-mat-khau.page.html:29` |
| Field 2 label | `Mật khẩu mới` | **`doi-mat-khau.field.newPassword`** | `doi-mat-khau.page.html:45` |
| Field 2 placeholder | `Nhập mật khẩu mới` | **`doi-mat-khau.field.newPasswordPlaceholder`** | `doi-mat-khau.page.html:51` |
| Field 3 label | `Xác nhận mật khẩu mới` | **`doi-mat-khau.field.confirmPassword`** | `doi-mat-khau.page.html:67` |
| Field 3 placeholder | `Nhập lại mật khẩu mới` | **`doi-mat-khau.field.confirmPasswordPlaceholder`** | `doi-mat-khau.page.html:73` |
| Submit button (idle) | `Đổi mật khẩu` | **`shared.action.changePassword`** | `doi-mat-khau.page.html:89` |
| Submit button (submitting) | `Đang lưu…` | **`shared.action.saving`** | `doi-mat-khau.page.html:89` |
| Field error — current password missing | `Vui lòng nhập mật khẩu hiện tại.` | **`doi-mat-khau.error.currentPasswordRequired`** | `doi-mat-khau.page.ts` § `onSubmit()`, rendered under field 1 |
| Field error — new password missing | `Vui lòng nhập mật khẩu mới.` | **`doi-mat-khau.error.newPasswordRequired`** | `doi-mat-khau.page.ts` § `onSubmit()`, rendered under field 2 |
| Field error — new password too short | `Mật khẩu mới chưa đủ dài.` — **no number in the sentence, deliberately** | **`doi-mat-khau.error.newPasswordTooShort`** | `doi-mat-khau.page.ts` § `onSubmit()`; threshold constant `MIN_PASSWORD_LENGTH = 12` at `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:31`, rendered under field 2 |
| Field error — confirmation missing | `Vui lòng nhập lại mật khẩu mới.` | **`doi-mat-khau.error.confirmPasswordRequired`** | `doi-mat-khau.page.ts` § `onSubmit()`, rendered under field 3 |
| Field error — confirmation mismatch | `Xác nhận mật khẩu mới không khớp.` | **`doi-mat-khau.error.confirmPasswordMismatch`** | `doi-mat-khau.page.ts` § `onSubmit()`, rendered under field 3 |
| Field error — from the server | one sentence per `fieldErrors[<Field>]` entry, **resolved from the entry's `code`** and space-joined — `PasswordMismatch` → `Mật khẩu hiện tại không đúng.`, `PasswordTooShort` → `Mật khẩu chưa đủ dài.`. Keys stay **PascalCase** (`CurrentPassword`, `NewPassword`), matching the C# command properties | **`VALIDATION.<code>`** (`src/FE/public/i18n/vi.json:50-67`, mirrored in `en.json`); when the code has no entry the service falls back to the envelope's own `message` (`api-error-message.service.ts:139`) | `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:241` stores `result?.fieldErrors`; `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:167` translates each entry through `errorMessages.fieldMessage(error)`. Envelope convention `doc/contracts/auth.md:21-24`. Local field errors **override** server ones for the same field (`doi-mat-khau.page.ts` § `fieldErrors`) |
| Inline error — server failure with no field | `{envelope.message}` verbatim, e.g. `Đổi mật khẩu thất bại.` | — (server-supplied) | `doi-mat-khau.page.ts` § `onSubmit()` → the `error` handler; contract `doc/contracts/auth.md:394` |
| Inline error — fallback, no envelope | `Đổi mật khẩu thất bại — thử lại sau.` | **`doi-mat-khau.error.changeFailed`** | `doi-mat-khau.page.ts` § `onSubmit()` → the `error` handler. Translated **eagerly**, not stored as a key — because `serverError` also carries the BE's untranslatable `message`, and one signal cannot hold two kinds of value (`doi-mat-khau.page.ts` § `onSubmit()` — the comment above `serverError.set`) |
| Toast — server failure | resolved from `businessCode` by `ApiErrorMessageService`, **not** taken verbatim from `message` — so for `AUTH.CHANGE_PASSWORD_FAILED` the toast says `Đổi mật khẩu thất bại.` from `vi.json` while the inline block says the server's `message` | **`AUTH.CHANGE_PASSWORD_FAILED`** | `http-error.interceptor.ts` § `messageFor` |
| Toast — fallbacks (no connection / 401 / 403 / 404 / 429 / other) | title + sentence exactly as listed in the Sign in Copy table | **`shared.httpError.*`** | `api-error-message.service.ts:13-19,40-43` |
| Toast — session ended mid-form (401) | title `Phiên đăng nhập đã kết thúc` + `Vui lòng đăng nhập lại để tiếp tục.`, severity **warn** not error | **`shared.httpError.endedTitle`** / **`shared.httpError.endedText`** | `http-error.interceptor.ts:25-26,109-112`. New 2026-09-05 — see States, `locked-out` |
| Toast dismiss `aria-label` | `Đóng thông báo` | **`shared.toast.dismiss`** | `toast.html:16` |
| Native validation bubble (empty required field) | browser-supplied, locale-dependent | — (user-agent) | emergent from `required` at `doi-mat-khau.page.html:31,53,75` |

> ### 🔄 SỬA 2026-09-06 — this screen is now **fully** wrapped too
>
> The previous table marked every row `— (hardcoded)`. Every row above now carries a real key,
> verified present in `public/i18n/vi.json` **and** `en.json` (203 keys each, no key in one
> bundle missing from the other — measured 2026-09-06).
>
> Note the two different storage strategies, because they are not interchangeable:
>
> | Pattern | Used for | Why |
> | --- | --- | --- |
> | store the **key**, translate on read | subtitle (`subtitleKey`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:180-182`), local field errors (`localFieldErrors`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:125`) | survives a language switch while the message is on screen |
> | translate **eagerly**, store the sentence | `serverError` fallback (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:246-248`) | that signal also holds the BE's `message`, which is not a key and cannot be re-resolved |
>
> Two substantive facts also changed and are **not** cosmetic:
>
> - **`MIN_PASSWORD_LENGTH` is 12, not 8** (`doi-mat-khau.page.ts` § `MIN_PASSWORD_LENGTH`). It was 8 while the BE
>   enforced 12, so a 9-character password passed the client and was rejected by the server.
> - **The sentence no longer contains the number.** It reads `Mật khẩu mới chưa đủ dài.`, on
>   purpose: the only number a user may be shown is the one the BE sent with its error code.
>   Interpolating a hand-copied constant is how the last wrong number got on screen.

> ### 🔄 SỬA 2026-09-08 — the server field-error row was describing a dead read path
>
> Until today the row **Field error — from the server** read `{envelope.fields[<Field>].join(' ')}`
> and declared the result **untranslatable**. Both halves are now wrong, and the code that made
> them wrong landed **2026-09-06**: the page reads `fieldErrors`, not `fields`
> (`doi-mat-khau.page.ts:109-112` states the change and its reason), and every entry is put
> through `ApiErrorMessageService.fieldMessage` (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:167`), so the sentence is looked up by code and
> re-resolves on a language switch like every other string on this screen.
>
> Keeping the old wording visible rather than deleting it, because the failure it describes is the
> one worth remembering: BE leaves `fields` **empty** for business errors and fills `fieldErrors`
> only, so a page reading `fields` painted no field red at all and left the user with one vague
> sentence at the top of the form. Verified against source 2026-09-08.

> ### 🔄 SỬA 2026-09-08 — bare `:N` citations removed from this whole file
>
> Several citations in this file named only a line number — `` `:152-153` ``, `` `:219` ``,
> `` `:36,106` `` — leaving the file they belonged to implied by the sentence around them. That
> form is invisible to every gate: `check-docs.sh` only inspects citations beginning with `src/`
> or `doc/`, so a bare `:N` is never opened, never bounds-checked, and never re-checked when the
> source moves. Each one now carries its full path.
>
> Re-anchoring exposed that most of them had already drifted. Cited → actually declared, measured
> against source on 2026-09-08:
>
> | Identifier | Was cited | Actually at |
> | --- | --- | --- |
> | `subtitleKey` | `:152-153` | `doi-mat-khau.page.ts:180-182` |
> | `localFieldErrors` | `:180-195` | `doi-mat-khau.page.ts:125` |
> | `serverError` eager fallback | `:219` | `doi-mat-khau.page.ts:246-248` |
> | `MIN_PASSWORD_LENGTH` | `:30` | `doi-mat-khau.page.ts:31` |
> | first-401-only flag | `:36,106` | declared `http-error.interceptor.ts:36`, guarded `:107-108` |
> | session-ended toast | `:109-112` | emitted `http-error.interceptor.ts:110-113` |
>
> The `localFieldErrors` row is the one worth staring at: `:180-195` still resolves inside the
> file, and still lands on real code — it is now `subtitleKey` and `errorId()`. A citation that
> points confidently at the wrong declaration is strictly worse than one that points past the end
> of the file, because nothing, human or machine, flags it.



### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **idle — forced (default arrival):** the flow's normal entry. `mustChangePasswordGuard` sends any authenticated user with `mustChangePassword() === true` here from every other route (`must-change-password.guard.ts:19-25`); this route keeps `authGuard` so an anonymous visitor is still bounced to `/dang-nhap` with a `returnUrl` (`doi-mat-khau.routes.ts:17`). `isForced()` true → subtitle reads `Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.`. All three fields empty and masked, none `.invalid`, submit enabled, no `.login-error` and no `.form-error` in the DOM. Nothing else on screen indicates the lock-out — no banner, no explanation of *why*, and no way out.
- **idle — voluntary:** the same screen reached by choice after the first change; `mustChangePassword()` is false so the subtitle reads `Đổi mật khẩu tài khoản của bạn.` and other routes are reachable. **There is no entry point left.** The home screen's `<a class="btn" [routerLink]="routes.changePassword">` was the only link into this screen, and it was deleted together with that screen on 2026-09-11; the server-issued menu has never carried the route, and the DTI Dashboard that now renders at `/trang-chu` does not link to it either. Voluntary arrival therefore means typing the URL. Verify rather than trusting this sentence — PASS = hits only inside this screen's own template: `grep -rn 'changePassword' src/FE/src/app --include=*.html`. This screen has no navigation of its own, so leaving still means the browser's back button. The endpoint is not one-shot (`doi-mat-khau.page.ts` § the class doc comment, case (2)).
- **submitting:** `submitting()` true → submit `[disabled]` (`opacity: .5`, `cursor: not-allowed`, `styles.scss` § `.btn:disabled`) with its label swapped to `Đang lưu…`. No spinner. All three password fields stay enabled and editable throughout; both server signals are cleared immediately before the request (`doi-mat-khau.page.ts` § `onSubmit()` — `serverError.set(null)` / `serverFieldErrors.set(null)`), so a previous server error disappears the moment a resubmit starts. Local field errors are **not** cleared at that point — they were already overwritten by the fresh validation pass at `doi-mat-khau.page.ts` § `onSubmit()` — `localFieldErrors.set(errors)`.
- **empty:** as on Sign in, there is **no empty state and no way to reach one.** The three fields, the submit button and the subtitle are all statically present (`doi-mat-khau.page.html:21-91`); the template contains no `@for` and four `@if`s — the form-level error block and one per-field error slot each. Nothing here loads a list, and the one server value it depends on — `mustChangePassword()` — is already resolved before the route renders, so it selects between the two idle variants rather than producing an empty one.
- **field-validation:** the screen's defining difference from Sign in. All checks run **on submit only** — nothing validates on blur or on input — and, unlike the old sequential guard, they **do not short-circuit**: every failing field is collected in one pass and all of them render at once (`doi-mat-khau.page.ts` § `onSubmit()`). What is collected is a **translation key** per field, not a sentence; `fieldErrors` resolves them on read and re-resolves on every language change (`doi-mat-khau.page.ts` § `fieldErrors`). Rules: field 1 must be non-empty; field 2 must be non-empty and **≥ 12 characters**; field 3 must be non-empty and equal field 2. Each message lands under its own field via `.form-error`, with `.invalid` + `aria-invalid` + `aria-describedby` on the input. **Local errors override server errors for the same field** (`doi-mat-khau.page.ts` § `fieldErrors`) because the local one describes this click. While any field error exists, the form-level `.login-error` is suppressed so the same information is not repeated in vaguer words directly above the specific one (`doi-mat-khau.page.ts` § `errorMessage`). Two as-shipped consequences worth recording: (1) the "field missing" branches are **unreachable in a standards-compliant browser** — all three inputs are `required` with no `novalidate`, so an empty field blocks the submit event first, and there is no `.trim()`, so whitespace counts as filled; (2) the minimum length is enforced but **never disclosed** — no rules hint, no strength meter — and the client constant is only half the policy: ASP.NET Identity also demands character classes and reports them **only from the server** (`doc/contracts/auth.md:394`), so a 12-character password can still clear the client check and be rejected.

  > 🔄 **SỬA 2026-09-06.** This bullet said **8 characters** in two places. The constant is now
  > **12** (`doi-mat-khau.page.ts` § `MIN_PASSWORD_LENGTH`), raised because the BE enforces 12
  > (`options.Password.RequiredLength`) and the FE copy had drifted below it — a user typing 9
  > characters was waved through by the client and refused by the server. The client sentence
  > also stopped naming a number at all, so a future drift cannot put a wrong number on screen.
- **server-error (`AUTH.CHANGE_PASSWORD_FAILED`, 422):** the single server code covering both "current password is wrong" and "new password is too weak". Its `message` is now a **fixed sentence** — `Đổi mật khẩu thất bại.` — and the reason moved into `fieldErrors` as Identity **codes** (`PasswordMismatch`, `PasswordTooShort`), keyed by input name (`doc/contracts/auth.md:394` and §`POST /api/auth/change-password`). Where it renders depends on the envelope: a `fieldErrors` map is translated entry by entry, space-joined per key, and bound under the matching input, PascalCase key for PascalCase key (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:241` stores it, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:167` translates it); a body with only `message` goes to the form-level block. The toast fires either way (`http-error.interceptor.ts` § `messageFor`). When the body is not a parseable envelope, the toast falls back per status with a title and the inline block falls back to `Đổi mật khẩu thất bại — thử lại sau.`

  ✅ **This screen reads `fieldErrors` and translates it** — changed 2026-09-06, recorded here 2026-09-08. `serverFieldErrors` is filled from `result?.fieldErrors` (`doi-mat-khau.page.ts:241`) and each `ApiFieldError` is resolved by its `code` through `ApiErrorMessageService.fieldMessage` (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:167`), so `PasswordMismatch` and `PasswordTooShort` render as Vietnamese or English sentences that follow a language switch. The screen is level with sign-in on this point (`login.page.ts` § `fieldMessages()`).

  > 🔄 **What this replaced, and why it is worth remembering.** Until 2026-09-06 the page read the legacy `fields` map, so the Identity codes arrived as an untranslatable plain-string list — and, worse, usually as *nothing at all*: for business errors the BE leaves `fields` empty and fills only `fieldErrors`, so no input was marked `.invalid` and the user saw only the form-level sentence. The source states this in its own words at `doi-mat-khau.page.ts:109-112`. Two doc comments **inside the source** still name `fields` and were not updated with the read path: the one on `ChangePasswordField` (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:34-38`) and the class comment (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:52-55`). Their point — PascalCase keys, one error slot per input — still holds, but the field name in them is stale; flagged here rather than edited, since `src/FE/` is out of scope for a design task. Contract side: `doc/contracts/auth.md` §`POST /api/auth/change-password`.
- **locked-out:** **not reachable on this screen as a login failure.** `AUTH.LOCKED_OUT` is a `POST /auth/login` outcome (`doc/contracts/auth.md:163`); by the time this screen renders the user already holds a session. The equivalent hazard here is the session dying underneath them — an account locked, a role changed, or the password changed elsewhere makes the next request return 401 within the `SecurityStampValidator`'s 30-minute cycle (`doc/contracts/auth.md:467-472`). ✅ **This is now handled** (changed 2026-09-05, closing Normalize #7): `httpErrorInterceptor` clears the client user context and navigates to the sign-in screen with `returnUrl=<current url>` (`http-error.interceptor.ts:100-121`). The toast is deliberately **`warn`, not `error`** — a mid-session 401 is a normal part of the session lifecycle, not a system fault — and reads `Phiên đăng nhập đã kết thúc` / `Vui lòng đăng nhập lại để tiếp tục.` (key constants `src/FE/src/app/core/interceptors/http-error.interceptor.ts:25-26`, emitted at `src/FE/src/app/core/interceptors/http-error.interceptor.ts:110-113`). A module-level flag makes only the **first** of several parallel 401s redirect, so the user gets one toast and one navigation rather than three (flag declared at `src/FE/src/app/core/interceptors/http-error.interceptor.ts:36`, guarded at `src/FE/src/app/core/interceptors/http-error.interceptor.ts:107-108`). Typed input is still lost. Note this screen also has the reverse property: after a **successful** change the current session is deliberately kept alive and only the user's *other* sessions are killed (`doc/contracts/auth.md:432-440`), so no re-login is required here.
- **rate-limited (429, `RATE_LIMIT.TOO_MANY_REQUESTS`):** reachable here too. `POST /api/auth/change-password` is not covered by the 5/min login policy, but the **200/min/IP sliding-window** global limiter applies to **every** endpoint (`doc/contracts/auth.md:190-194`), so a shared-IP office or a burst of app traffic can 429 this form. Rendering is the generic error path again: the toast resolves the sentence from `businessCode` while the form-level block uses the envelope's `message`, `Retry-After` ignored, no countdown, submit immediately re-enabled.

  > 🔄 **SỬA 2026-09-06.** This bullet said the global limiter was **100/min/IP with a fixed window**. It is **200/min/IP, sliding, six segments** (`src/BE/PlatformManager.Api/Program.cs:294-295`), raised 2026-08-30 — a fixed window let a burst straddling the minute boundary spend 2× the quota and then penalised the caller for up to 55 seconds.
- **success:** no confirmation of any kind is shown. `markPasswordChanged()` flips `MustChangePassword` to false locally to avoid a second `/auth/me` round-trip (`current-user.service.ts:67`) and the component navigates straight to `CORE_ROUTES.home` (`doi-mat-khau.page.ts` § `onSubmit()` → `navigateByUrl(this.routes.home)`) — a **fixed destination**, not the `returnUrl` that may have brought the user into the flow. It is no longer the literal string `/trang-chu`: the path comes from the injected `CORE_ROUTES` token so a second product on this platform routes to its own home screen, but it is still not the `returnUrl`. There is no success toast, so a forced-change user's only feedback is that the home screen appears.

### Responsive

<!-- Behavior per breakpoint. -->

- **All viewports — there is no breakpoint.** `doi-mat-khau.page.scss` is four lines (`:host { display: contents }`) with no rules of its own (`doi-mat-khau.page.scss:1-4`); `auth-card.scss` and the global auth block (`styles.scss` § `.field` … `.login-error.show`) contain no `@media` rule. The `breakpoint.tablet` / `breakpoint.mobile` shell breakpoints do not apply — the shell is not rendered.
- **Fluid behaviour:** identical to Sign in — `width: 100%` clamped at `dimension.login-card-max-width`, shell padding `spacing.sp-5`, internal padding fixed at `spacing.auth-card-padding` at every width (`auth-card.scss:10-18`). The practical difference is height: three fields plus the brand block make this the taller card, and it grows further whenever a `.form-error` line appears. On a short viewport (landscape phone, ~390×640) it is the one more likely to exceed the fold. `.login-shell` uses `min-height`, so it grows and the page scrolls rather than clipping — but the card stops being optically centred at that point.
- **Viewport height:** `min-height: 100vh` then `min-height: 100dvh` (`auth-card.scss:2-3`), as on Sign in.
- **Viewport meta:** `width=device-width, initial-scale=1, viewport-fit=cover` (`src/FE/src/index.html:7`); no `env(safe-area-inset-*)` consumption anywhere on this screen.
- **Toast overlay:** `.toast-stack { max-width: min(dimension.toast-stack-max-width, 90vw) }` (`toast.scss:12`) — the one viewport-reactive rule reaching this screen.
- **Reduced motion:** the global `prefers-reduced-motion` block flattens the input focus transition and the toast entrance (`styles.scss` § `@media (prefers-reduced-motion: reduce)`).
- **Print (`@media print`):** global `.no-print` rule only (`styles.scss` § `.no-print`); no element on the card carries it, so the card prints as rendered while the toast stack is hidden (`toast.html:1`).

### Iconography

<!-- Refs into Icons.md -->

Same library and the same sizing rule as Sign in — PrimeIcons v7 loaded globally (`angular.json:39,101`, `package.json:53`), field glyphs at `font-size: 15px`, ink `colors.muted`, absolute `left: 12px`, `pointer-events: none`, with `padding-left: 36px` on the input reserving the gutter (`styles.scss` § `.input-icon > .pi` / `.input-icon input`). Full map: `Icons.md`.

| Action | Icon | Placement |
| --- | --- | --- |
| Error notice (inline) | `pi pi-exclamation-circle` | Leading, inside `.login-error` (`doi-mat-khau.page.html:9`) |
| Current-password field adornment | `pi pi-lock` | Leading, absolute `left: 12px` inside `.input-icon` (`doi-mat-khau.page.html:25`) |
| New-password field adornment | `pi pi-key` | Leading, absolute `left: 12px` inside `.input-icon` (`doi-mat-khau.page.html:47`) |
| Confirm-password field adornment | `pi pi-key` | Leading, absolute `left: 12px` inside `.input-icon` (`doi-mat-khau.page.html:69`) |
| Per-field error line | — (**text only**, no glyph — unlike `.login-error`, which leads with one) | `doi-mat-khau.page.html:40,62,84` |
| Reveal / hide password | — (**no toggle exists on this screen**, unlike Sign in) | `doi-mat-khau.page.html:22-85` |
| Submit change-password | — (**text-only button**, no glyph, unlike Sign in's `pi pi-sign-in`) | `doi-mat-khau.page.html:88-90` |
| Toast severity (error) | `pi pi-times` | Inside the `.toast-icon` disc (`toast.html:6`, severity map at `toast.ts:10-15`) |
| Dismiss toast | `pi pi-times` | Trailing, on the toast's `.icon-btn` (`toast.html:19`) |

### Screenshots

<!-- Refs into Assets/Screenshots/auth/ -->

> ⚠️ **The one existing shot is STALE.** `change-password--forced--desktop-1440.png` was captured 2026-08-22 — before the 2026-08-29 palette and font change **and** before the per-field `.form-error` rendering existed. Its idle layout still matches (this screen gained no new elements in the i18n phase, unlike sign-in), but it cannot illustrate the validation state at all. Kept rather than deleted so the before/after stays visible; recapture is the highest-value job on this screen.

Every row below requires a live backend, because reaching the route at all requires an authenticated session (`authGuard`, `doi-mat-khau.routes.ts:17`). Procedure:

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`doc/Design/CLAUDE.md`](../../../CLAUDE.md) § Rules

- **Never record credentials in this file.**
- For the *forced* rows, sign in as an account whose `MustChangePassword` is still `true` (a freshly admin-created user — the sample payload at `doc/contracts/auth.md:152` shows `mustChangePassword: true`); the redirect to `/doi-mat-khau` is automatic. For the *voluntary* rows, sign in as an account that has already changed its password, then use the home screen's `Đổi mật khẩu` button.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/auth/change-password--forced--desktop-1440.png` | **stale** (captured 2026-08-22) — recapture | Signed in as an account whose `MustChangePassword` is still `true`, so the redirect fires automatically; capture @ 1440×900 in the idle state. This is the **forced** variant — it must show the forced subtitle `Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.` |
| `Assets/Screenshots/auth/change-password--validation--desktop-1440.png` | on demand *(BE required)* — **the highest-value new shot**, nothing captures the per-field slots yet | Leave field 1 filled, make field 3 differ from field 2, and give field 2 fewer than **12** characters, then submit @ 1440×900. Both messages appear **at once** (the checks no longer short-circuit), each under its own input with a red border. Client-side only — no request leaves the browser, so **no toast appears** |
| `Assets/Screenshots/auth/change-password--voluntary--desktop-1440.png` | on demand *(BE required)* | Same route reached from the home screen with an already-changed account @ 1440×900. Must show the voluntary subtitle `Đổi mật khẩu tài khoản của bạn.` — the only visual difference from the forced row |
| `Assets/Screenshots/auth/change-password--server-error--desktop-1440.png` | on demand *(BE required)* | Enter a wrong current password with a valid new one → 422 `AUTH.CHANGE_PASSWORD_FAILED`. Capture @ 1440×900 within 5s so the message and the duplicate toast are both in frame (`toast.service.ts:19,64`). Note where the message lands: under field 1 if the envelope carries `fields`, in the form-level block if it carries only `message` |
| `Assets/Screenshots/auth/change-password--mobile-390.png` | on demand *(BE required)* | Forced idle state @ 390×844. Documents the taller three-field card against the same 380px clamp, and whether it exceeds the fold on a short viewport |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. A quirk that spans components belongs in the component's own spec (`Components/<Name>.md` → Normalize on redesign), not here. -->

1. **The password rules are enforced but never stated.** *(Still open, and the "same rule set" half is a deliberate deferral, not an oversight.)* The client requires **12** characters (`doi-mat-khau.page.ts` § `MIN_PASSWORD_LENGTH`, § `onSubmit()`) and the server applies ASP.NET Identity's character-class policy on top, reporting it only after a failed submit (`doc/contracts/auth.md:394`). Show the requirements up front. The second half — validating against the *same* rule set — needs the BE to publish its policy through an endpoint; until then the two copies can drift again, which is exactly why the client sentence no longer quotes a number.
2. **No reveal toggle here, but there is one on sign-in.** *(Still open.)* Sign-in offers `pi pi-eye` / `pi pi-eye-slash` (`login.page.html:52-61`); this screen asks the user to type a new password twice, blind (`doi-mat-khau.page.html:22-85`). This is exactly the screen where revealing the value helps most, and the control is now a plain `.icon-btn` — reusing it on all three fields costs one line each.
3. **The submit button lost its icon.** *(Still open.)* Sign-in's primary button leads with `pi pi-sign-in`; this one is text-only (`doi-mat-khau.page.html:89`). Two cards in the same flow, two different button anatomies — pick one.
4. **Nothing validates until submit.** *(Still open.)* Every check runs in `onSubmit()` (`doi-mat-khau.page.ts` § `onSubmit()`); there is no blur or input validation, so the confirmation-mismatch message cannot appear while the user is still looking at the field that caused it. Validating field 3 on blur would catch the most common failure before the round-trip.
5. **Success is silent.** *(Still open.)* `markPasswordChanged()` then `navigateByUrl(this.routes.home)` with no toast and no confirmation (`doi-mat-khau.page.ts` § `onSubmit()` → the `next` handler). A user who has just been forced through a security step is given no acknowledgement that it worked. Add a success toast — the channel exists (`toast.service.ts:38-40`) and is currently used for errors only.
6. **The post-success destination is fixed.** *(Still open, though narrowed.)* `CORE_ROUTES.home` regardless of where the user came from (`doi-mat-khau.page.ts` § `onSubmit()` → `navigateByUrl(this.routes.home)`), which is the second half of the `returnUrl` loss described under Sign in. Accept and resume a `returnUrl` through this screen. The literal `/trang-chu` is gone — the path is injected — but the value is still not the user's.
7. **A mid-form 401 leaves the user stranded rather than redirecting.** ✅ **SHIPPED 2026-09-05.** The interceptor now clears the user context and navigates to `CORE_ROUTES.signIn` with `returnUrl=<current url>`, toasting `warn` rather than `error` (`http-error.interceptor.ts:100-121`), which is what `doc/contracts/auth.md:467-472` asked for. Three cases deliberately do **not** redirect — the bootstrap probe, being already on the sign-in screen (a 401 there means wrong password), and the 2nd..n-th parallel 401 of one session death (`src/FE/src/app/core/interceptors/http-error.interceptor.ts:36`, guarded at `src/FE/src/app/core/interceptors/http-error.interceptor.ts:107-108`). **Typed input is still lost**, and the `returnUrl` still does not survive a forced password change (Sign in #7) — those two halves remain open.
8. **The forced state is a dead end with no explanation and no exit.** *(Still open.)* No banner explaining that an administrator set this requirement, no "sign out" control, no link anywhere. If the user cannot recall their current password they cannot proceed and cannot leave except by clearing cookies. Add an explanatory `NoticeBanner` and a sign-out affordance.
9. **The server-error path could collapse into one vague sentence.** ✅ **Mostly closed — FE half shipped 2026-09-06, recorded here 2026-09-08.** `AUTH.CHANGE_PASSWORD_FAILED` covers both "current password wrong" and "new password too weak". The BE closed its half on 2026-09-05 by sending every Identity code in `fieldErrors` keyed by input name (`doc/contracts/auth.md` §`POST /api/auth/change-password`); the page now consumes exactly that — `serverFieldErrors` from `result?.fieldErrors` (`doi-mat-khau.page.ts:241`), translated per entry by `ApiErrorMessageService.fieldMessage` (`src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:167`), which is the move sign-in made first (`login.page.ts` § `fieldMessages()`). So `PasswordMismatch` lands under *Mật khẩu hiện tại* and `PasswordTooShort` under *Mật khẩu mới*, each with its own sentence.

   **What is left open is smaller and different:** the page still never reads `businessCode` — `grep -n businessCode src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts` returns nothing (**re-checked 2026-09-08: still nothing**). The form-level block therefore still shows the envelope's raw `message` — `serverError.set(result?.message ?? …)` in the `error` handler of `doi-mat-khau.page.ts` § `onSubmit()`, and the source itself flags the consequence in the comment directly above that line: the sentence freezes in whatever language it arrived in. The toast for the same failure resolves the code through `ApiErrorMessageService` (`http-error.interceptor.ts` § `messageFor`). Two surfaces, two lookup paths, and only one of them follows a language switch. This is the change-password twin of Sign in's Normalize #1.

   > 🔗 **Neo lại 2026-09-08 — bằng định danh, không bằng số dòng.** Sáu trích dẫn `http-error.interceptor.ts:133` trong file này đều trỏ vào một **dòng chú thích**; `messageFor` thật ra nằm ở dòng kế tiếp. Neo lệch 1 dòng, phân giải được, nằm trong file — nên `check-docs.sh` §4c cho qua cả sáu. Đây đúng loại "neo giả" mà `AUDIT.md` § (i) mô tả ở một ca khác cùng ngày: bằng chứng sai trong khi khẳng định thì đúng. Neo theo tên hàm thì đổi tên là compiler biết, còn dòng trôi thì không ai biết (`doc/Design/CLAUDE.md` § Neo trích dẫn, cùng lập luận).

   > The bullet above previously read *"This page still reads the legacy `fields`"* and cited `doi-mat-khau.page.ts:210-220`. Both parts had gone stale: the read path moved on 2026-09-06, and that line range is now the local validation pass, not the error handler — a citation that still resolves inside the file and still points at real code, which is exactly the kind `check-docs.sh` cannot catch.
10. **The error strips still have no live region — and this screen is now the ONLY one without.** *(Still open, and newly asymmetric.)* Neither the form-level `.login-error` nor the per-field `.form-error` carries `role="alert"` or `aria-live` (`doi-mat-khau.page.html:7-12,39-41`), while the sign-in block gained `role="alert"` on 2026-09-05 (`login.page.html:7`). One flow, two screens, two announcement contracts. `aria-describedby` means a screen-reader user hears the message **when they reach the field**, not when it appears — better than sign-in, still short of announcing the failure.
11. **`.login-error` and `.notice.bad` are the same object twice.** *(Still open.)* Both are a `bad`-family strip with a glyph and a message; `.notice` ships four severities while `.login-error` has one, and only the auth screens use the latter (`Components/AuthField.md` § Normalize #5). Folding one into the other would leave the app with a single error strip.
