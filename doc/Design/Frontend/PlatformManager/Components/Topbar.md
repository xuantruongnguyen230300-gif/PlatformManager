---
kind: luat
scope: du-an
verified: 2026-09-11
project: "PlatformManager"
status: "draft"
updated: "2026-09-11"
component: "Topbar"
sources:
  - "src/FE/src/app/shared/components/topbar/topbar.html"
  - "src/FE/src/app/shared/components/topbar/topbar.scss"
  - "src/FE/src/app/shared/components/topbar/topbar.ts"
  - "src/FE/src/app/app.html"
  - "src/FE/src/app/app.ts"
  - "src/FE/public/i18n/vi.json"
---

# Topbar
**Description:** The app shell's sticky page header (`<app-topbar [title]>`, `.topbar`) — a translucent blurred bar carrying the drawer hamburger (narrow viewports only), the current page title as the document's `<h1>`, and the signed-in user's name plus **two** account actions — a change-password link and a logout button. Renders on the four shelled routes (`/trang-chu`, `/danh-muc/dti`, `/quan-tri/nguoi-dung`, `/quan-tri/phan-quyen`), above `<main>`, inside `.shell-content`. `/doi-mat-khau` is authenticated too but declares `data: { noShell: true }`, so no Topbar renders there.

## Anatomy

`.topbar` — `position:sticky; top:0`, `z-index:20`, background `colors.surface-topbar` with `backdrop-filter: blur(10px)`, bottom border 1px `colors.line`. It has no padding of its own; all spacing lives on the inner track.

`.topin` — `max-width: spacing.container-max-width`, `margin:auto`, padding `spacing.sp-4` `spacing.sp-5`, row flex, `align-items:center`, gap `spacing.sp-3`. Three slots, left to right:

1. **`.sidebar-hamburger`** — a `.btn` (see `Button.md`) carrying `pi pi-bars`, `[attr.aria-label]="'shared.topbar.openMenu' | translate"`, `aria-controls="sidebar"`, `[attr.aria-expanded]` bound to the drawer flag. `display:none` by default; at `spacing.breakpoint-tablet` it becomes a centred flex box with padding `9px` literal. Marked `no-print`.
2. **`.logo > h1`** — the page title, `typography.h1-topbar` (`font-size: var(--fs-lg)`; weight comes from the UA `<h1>` default, not an authored declaration), `margin:0`. The string is pushed in by the shell, which reads the **route-level `title`** relayed by `PageTitleStrategy` (`core/title/page-title.strategy.ts`) and falls back to `CORE_BRANDING.name` (`'PlatformManager'` in this app). The same value feeds the browser tab title — one source, two consumers.
3. **`.topbar-user`** (rendered only while `CurrentUserService.isAuthenticated()`) — `margin-left:auto`, `flex:none`, row flex, gap `spacing.sp-3`, `no-print`. Three children, left to right:
   - `.topbar-user-name` (`typography.h1-topbar` size via `--fs-sm`/700 — see Tokens Used, `colors.text`, `white-space:nowrap`).
   - an **`<a class="btn">`** carrying `pi pi-key` and a `.topbar-action-label` span, both label and `[attr.title]`/`[attr.aria-label]` from `shared.action.changePassword`, with `[routerLink]` bound to `CORE_ROUTES.changePassword`. **Added 2026-09-11** — see the note below.
   - a **`<button class="btn">`** carrying `pi pi-sign-out` and a `.topbar-action-label` span, with the label, `[attr.title]` and `[attr.aria-label]` all from `shared.topbar.logout`, firing `onLogout()`. The `.topbar-action-label` wrapper and the `[attr.aria-label]` were **added 2026-09-11** with the link, for the same reason: its label is `display:none` at `spacing.breakpoint-mobile`.

   > ### 🔴 Why the change-password link lives here — a regression, not a preference
   >
   > Until 2026-09-11 the **voluntary** way into `/doi-mat-khau` was an `<a class="btn">` on the old
   > home screen. The `/trang-chu` swap (decision **Q3**) deleted `platform/trang-chu/`, and that
   > link went with it: the **forced** flow still worked (`mustChangePasswordGuard` redirects while
   > `MustChangePassword` is true), but anyone who had already changed their password had no route
   > back other than typing the URL. No menu row covers it either — the sidebar is server-driven and
   > `AppMenuSeedSource` seeds nothing for it.
   >
   > Nothing failed: the build, the lint and the whole suite stayed green, because what disappeared
   > was not a screen but **the way to it**. `topbar.spec.ts` now pins the link so the same deletion
   > cannot happen silently twice.
   >
   > It is an `<a>`, not a `<button>`: this is navigation to a URL, so it must open in a new tab and
   > show its destination on hover. And it never links to itself — `/doi-mat-khau` declares
   > `data: { noShell: true }`, so no Topbar renders there.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Desktop bar | `.topbar` | hamburger hidden; title + user block visible | > `spacing.breakpoint-tablet` |
| Tablet / mobile bar | `.topbar` @ `spacing.breakpoint-tablet` | `.sidebar-hamburger` becomes `display:flex`, padding `9px` literal — the only entry point to the sidebar drawer at this width | ≤980px |
| Compact user block | `.topbar-user` @ `spacing.breakpoint-mobile` | `.topbar-user-name` **and** `.topbar-action-label` are both `display:none`; the two account `.btn`s stay, reduced to bare `pi-key` / `pi-sign-out` glyphs whose accessible name is their `[attr.aria-label]` | ≤560px |
| Signed-out bar | — | The whole `.topbar-user` block is absent (`@if (currentUser.isAuthenticated())`) | Only reachable transiently; the four shelled routes are all behind `authGuard` |
| Print | `@media print` | `.topbar { display:none !important }` | Any print output |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

`.topbar` itself is a static container; the interactive states below belong to its three `.btn` children (hamburger, change-password link, logout button) and are owned by `Button.md`.

| State | Treatment |
| --- | --- |
| default | Sticky translucent bar: `colors.surface-topbar` + `blur(10px)`, bottom hairline `colors.line`, inner track capped at `spacing.container-max-width` |
| hover | **Not applicable** — `topbar.scss` authors no `:hover` rule at all; the bar is not a control. All three of its `.btn` children take `.btn:hover` (`colors.btn-hover-bg` + `shadow.btn-hover`) from the global rule |
| focus-visible | **Not applicable** — `.topbar` carries no `tabindex` and is not focusable. All three `.btn` children inherit `.btn:focus-visible` (`outline: 2px solid colors.brand`, offset 2px) |
| active / selected | **Not applicable** — a header bar has no selected state, and no `:active` rule is authored here. `.btn:active { transform: translateY(1px) }` still applies to all three |
| disabled | **Not applicable** — none of the three is ever disabled: the hamburger is unconditional, the change-password control is an `<a>` (a link has no disabled state), and logout does not block during the request (`topbar.ts:68-70`), so there is no in-flight disabled state to style. The destination is `CORE_ROUTES.signIn`, injected — not a literal path in this component |

## Tokens Used
- `colors.surface-topbar` (a real custom property, `src/FE/src/styles.scss` § `--surface-topbar`, promoted out of a selector literal on 2026-09-03 — `Tokens/colors.md` § Resolved, item 2; corrected here 2026-09-08, the previous line called it a literal and cited § Shipped as literals, a section that no longer exists), `colors.line`, `colors.text`
- `spacing.container-max-width`, `spacing.sp-3`, `spacing.sp-4`, `spacing.sp-5`
- `spacing.breakpoint-tablet`, `spacing.breakpoint-mobile`, `spacing.breakpoint-print`
- `typography.h1-topbar` — the `<h1>` uses `--fs-lg` (15px) with the UA bold default; `.topbar-user-name` is `--fs-sm`/700, i.e. `typography.form-label`
- `9px` hamburger padding is an un-tokenised literal — catalogued in `Tokens/spacing.md` § Padding & gap literals bypassing the scale
- Icons: PrimeIcons v7 — `pi-bars`, `pi-key`, `pi-sign-out`
- Layer: `z-index:20` (no `--z-*` scale exists; the full stack is tabulated in `Tokens/spacing.md` § Z-index layers)

## Reference markup

```html
<div class="topbar no-print">
  <div class="topin">
    <button type="button" class="btn sidebar-hamburger no-print"
      [attr.aria-label]="'shared.topbar.openMenu' | translate"
      aria-controls="sidebar" [attr.aria-expanded]="state.mobileOpen()" (click)="state.openDrawer()">
      <i class="pi pi-bars"></i>
    </button>
    <div class="logo"><h1>{{ title() }}</h1></div>

    @if (currentUser.isAuthenticated()) {
      <div class="topbar-user no-print">
        <span class="topbar-user-name">{{ currentUser.fullName() }}</span>
        <a class="btn" [routerLink]="routes.changePassword"
          [attr.title]="'shared.action.changePassword' | translate"
          [attr.aria-label]="'shared.action.changePassword' | translate">
          <i class="pi pi-key"></i>
          <span class="topbar-action-label">{{ 'shared.action.changePassword' | translate }}</span>
        </a>
        <button type="button" class="btn"
          [attr.title]="'shared.topbar.logout' | translate"
          [attr.aria-label]="'shared.topbar.logout' | translate" (click)="onLogout()">
          <i class="pi pi-sign-out"></i>
          <span class="topbar-action-label">{{ 'shared.topbar.logout' | translate }}</span>
        </button>
      </div>
    }
  </div>
</div>
```

Routes declare a **translation key**, not a sentence: `title: 'dashboard.routeTitle'` on `/trang-chu` and so on. `PageTitleStrategy` keeps the key and re-resolves it on every language change, so the heading follows the language switcher. The Vietnamese strings that reach the heading today are `Tổng quan DTI` · `Danh mục DTI` · `Người dùng hệ thống` · `Phân quyền`. The first two come from the **business** bundle (`src/FE/public/i18n-app/vi.json` § `dashboard.routeTitle`, `danh-muc-dti.routeTitle`), the other two from the Core one (`src/FE/public/i18n/vi.json` § `quan-tri-nguoi-dung.routeTitle`, `phan-quyen.routeTitle`) — `/trang-chu` has rendered the DTI Dashboard since 2026-09-11, so its heading is no longer `Trang chủ` and its key no longer lives in the Core bundle. The two auth routes declare titles too — `login.routeTitle`, `doi-mat-khau.routeTitle` — but carry `noShell: true`, so no Topbar renders for them.

Sources: `src/FE/src/app/shared/components/topbar/topbar.html:2`, `:14`, `:17-50`, `src/FE/src/app/shared/components/topbar/topbar.scss:1-67`, `:69`, `src/FE/src/app/shared/components/topbar/topbar.ts:43`, `:55`, `:68-70`, `src/FE/src/app/app.html:30`, `src/FE/src/app/app.ts:60`, `src/FE/src/app/modules/dashboard/dashboard.routes.ts:27`, `src/FE/src/app/modules/danh-muc-dti/danh-muc-dti.routes.ts:20`, `src/FE/src/app/platform/quan-tri-nguoi-dung/quan-tri-nguoi-dung.routes.ts:14`, `src/FE/src/app/platform/phan-quyen/phan-quyen.routes.ts:13`.

> 🔄 **SỬA 2026-09-06** — bản trước sai bốn chỗ, tất cả do i18n runtime và việc gỡ module nghiệp vụ (2026-08-29) đi qua mà spec không đổi theo:
> 1. Nhãn nút và `aria-label` được ghi là chuỗi tiếng Việt khai cứng; nay đến từ `shared.topbar.openMenu` / `shared.topbar.logout`.
> 2. "all four authenticated routes" — **khi đó** chỉ còn **ba** route có shell, vì module nghiệp vụ vừa bị gỡ (2026-08-29). Nay lại là **bốn**: `/danh-muc/dti` đã về sau đó — xem § Description và § Variants, đừng đọc dòng lịch sử này như số hiện tại.
> 3. Danh sách tiêu đề `Dashboard` · `Danh mục` không còn tồn tại, và `title` của route nay là **khoá dịch** chứ không phải câu.
> 4. Nhánh Sources trỏ vào `modules/dashboard/`, `modules/danh-muc-dti/` — hai thư mục đã xoá; và `topbar.ts:40-43` (file **khi đó** chỉ có 49 dòng và `onLogout` ở `46-48` — lượt 2026-09-11 đẩy nó xuống `:68-70`, xem § Sources).

## Do / Don't

- ✅ Let the shell own the title. `Topbar.title` is `input.required<string>()`; the component never inspects the router itself. A new route declares `title: '…'` at **`Route` level** (not in `data`) and `PageTitleStrategy` feeds both the browser tab and this heading — see `doc/huong_dan/quy-uoc/fe-routing-guard.md` §2 rule 3.
- ✅ Keep `.topin`'s `max-width` equal to `<main>`'s (`spacing.container-max-width` in both) — that alignment is what makes the header track the content column instead of the viewport.
- ✅ Keep both the hamburger and the user block `no-print`; the entire bar is hidden in print anyway, and the markers document the intent locally.
- ❌ Don't give the bar an opaque background — the translucency plus `blur(10px)` is the shipped treatment and is what distinguishes it from `.card`.
- ✅ Keep the change-password link. It is the **only** voluntary entry into `/doi-mat-khau` in the whole app; removing it leaves the screen reachable by typed URL alone, and nothing in the build or the test suite says so — which is exactly how it was lost once already, on 2026-09-11.
- ❌ Don't drop `[attr.aria-label]` from the change-password link because the visible label is right there. At `spacing.breakpoint-mobile` that label is `display:none` and only the glyph remains, so the `aria-label` is the accessible name for every narrow viewport.
- ❌ Don't add a `:disabled` state to the logout button to cover the request round-trip; the shipped behaviour is to navigate immediately on either outcome, on purpose (a failed `POST /auth/logout` used to leave the user sitting in the app believing they had signed out).

## Normalize on redesign
1. ~~**`aria-controls="sidebar"` points at an id that does not exist.**~~ — **đã xử lý**, đối chiếu 2026-09-06: `<aside>` nay mang `id="sidebar"` (`src/FE/src/app/shared/components/sidebar/sidebar.html:5`), nên `aria-controls` phân giải được. Giữ dòng này để lần sau đừng gỡ `id` đó đi.
2. **`.topbar-user-name` re-types `--fs-sm`/700 inline** rather than reusing the identical `typography.form-label` step — the same pair is hand-written in several places across the app.
3. **The `<h1>` weight is never declared**, so it depends on the UA default (bold) while every other heading in the app states its weight explicitly. `typography.h1-topbar` records 700 as the effective value.
4. **`9px` hamburger padding is off-scale** — between `spacing.sp-3` (8px) and `spacing.sp-4` (10px).
5. **The two account actions share one flat row with no grouping.** At the mobile breakpoint the name and the change-password label both vanish, leaving two unlabelled glyphs side by side; a menu (or at least a separator) would read better than a row of icons. Recorded as-shipped after the 2026-09-11 fix, which deliberately chose the smallest change that restored the lost entry point rather than a redesign of the account area.
6. **The topbar has no reduced-transparency fallback**; `backdrop-filter` is unsupported or disabled in some environments, and there is no `@supports` branch.
