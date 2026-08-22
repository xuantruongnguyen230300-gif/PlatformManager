---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Topbar"
sources:
  - "src/FE/src/app/shared/components/topbar/topbar.html"
  - "src/FE/src/app/shared/components/topbar/topbar.scss"
  - "src/FE/src/app/shared/components/topbar/topbar.ts"
  - "src/FE/src/app/app.html"
  - "src/FE/src/app/app.ts"
---

# Topbar
**Description:** The app shell's sticky page header (`<app-topbar [title]>`, `.topbar`) — a translucent blurred bar carrying the drawer hamburger (narrow viewports only), the current page title as the document's `<h1>`, and the signed-in user's name plus a logout button. Renders on all four authenticated routes, above `<main>`, inside `.shell-content`.

## Anatomy

`.topbar` — `position:sticky; top:0`, `z-index:20`, background `colors.surface-topbar` with `backdrop-filter: blur(10px)`, bottom border 1px `colors.line`. It has no padding of its own; all spacing lives on the inner track.

`.topin` — `max-width: spacing.container-max-width`, `margin:auto`, padding `spacing.sp-4` `spacing.sp-5`, row flex, `align-items:center`, gap `spacing.sp-3`. Three slots, left to right:

1. **`.sidebar-hamburger`** — a `.btn` (see `Button.md`) carrying `pi pi-bars`, `aria-label="Mở menu điều hướng"`, `aria-controls="sidebar"`, `[attr.aria-expanded]` bound to the drawer flag. `display:none` by default; at `spacing.breakpoint-tablet` it becomes a centred flex box with padding `9px` literal. Marked `no-print`.
2. **`.logo > h1`** — the page title, `typography.h1-topbar` (`font-size: var(--fs-lg)`; weight comes from the UA `<h1>` default, not an authored declaration), `margin:0`. The string is pushed in by the shell: `App.pageTitle()` reads `data.title` off the deepest activated route and falls back to `'PlatformManager'`.
3. **`.topbar-user`** (rendered only while `CurrentUserService.isAuthenticated()`) — `margin-left:auto`, `flex:none`, row flex, gap `spacing.sp-3`, `no-print`. Contains `.topbar-user-name` (`typography.h1-topbar` size via `--fs-sm`/700 — see Tokens Used, `colors.text`, `white-space:nowrap`) and a `.btn` labelled `Đăng xuất` with a leading `pi pi-sign-out` and `title="Đăng xuất"`.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Desktop bar | `.topbar` | hamburger hidden; title + user block visible | > `spacing.breakpoint-tablet` |
| Tablet / mobile bar | `.topbar` @ `spacing.breakpoint-tablet` | `.sidebar-hamburger` becomes `display:flex`, padding `9px` literal — the only entry point to the sidebar drawer at this width | ≤980px |
| Compact user block | `.topbar-user` @ `spacing.breakpoint-mobile` | `.topbar-user-name` is `display:none`; the logout `.btn` stays | ≤560px |
| Signed-out bar | — | The whole `.topbar-user` block is absent (`@if (currentUser.isAuthenticated())`) | Only reachable transiently; the four shelled routes are all behind `authGuard` |
| Print | `@media print` | `.topbar { display:none !important }` | Any print output |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

`.topbar` itself is a static container; the interactive states below belong to the two `.btn` children and are owned by `Button.md`.

| State | Treatment |
| --- | --- |
| default | Sticky translucent bar: `colors.surface-topbar` + `blur(10px)`, bottom hairline `colors.line`, inner track capped at `spacing.container-max-width` |
| hover | **Not applicable** — `topbar.scss` authors no `:hover` rule at all; the bar is not a control. Its two buttons take `.btn:hover` (`colors.tonal-bg-hover` + `colors.shadow-btn-hover`) from the global rule |
| focus-visible | **Not applicable** — `.topbar` carries no `tabindex` and is not focusable. Both buttons inherit `.btn:focus-visible` (`outline: 2px solid colors.brand`, offset 2px) |
| active / selected | **Not applicable** — a header bar has no selected state, and no `:active` rule is authored here. `.btn:active { transform: translateY(1px) }` still applies to the two buttons |
| disabled | **Not applicable** — neither button is ever disabled: the hamburger is unconditional, and logout deliberately navigates to `/dang-nhap` on **both** the success and error branches rather than blocking during the request (`topbar.ts:40-43`), so there is no in-flight disabled state to style |

## Tokens Used
- `colors.surface-topbar` (literal `rgba(255,255,255,0.95)` in source, named in `Tokens/colors.md` § Shipped as literals), `colors.line`, `colors.text`
- `spacing.container-max-width`, `spacing.sp-3`, `spacing.sp-4`, `spacing.sp-5`
- `spacing.breakpoint-tablet`, `spacing.breakpoint-mobile`, `spacing.breakpoint-print`
- `typography.h1-topbar` — the `<h1>` uses `--fs-lg` (15px) with the UA bold default; `.topbar-user-name` is `--fs-sm`/700, i.e. `typography.form-label`
- `9px` hamburger padding is an un-tokenised literal — catalogued in `Tokens/spacing.md` § Padding & gap literals bypassing the scale
- Icons: PrimeIcons v7 — `pi-bars`, `pi-sign-out`
- Layer: `z-index:20` (no `--z-*` scale exists; the full stack is tabulated in `Tokens/spacing.md` § Z-index layers)

## Reference markup

```html
<div class="topbar">
  <div class="topin">
    <button type="button" class="btn sidebar-hamburger no-print" aria-label="Mở menu điều hướng"
      aria-controls="sidebar" [attr.aria-expanded]="state.mobileOpen()" (click)="state.openDrawer()">
      <i class="pi pi-bars"></i>
    </button>
    <div class="logo"><h1>{{ title() }}</h1></div>

    @if (currentUser.isAuthenticated()) {
      <div class="topbar-user no-print">
        <span class="topbar-user-name">{{ currentUser.fullName() }}</span>
        <button type="button" class="btn" title="Đăng xuất" (click)="onLogout()">
          <i class="pi pi-sign-out"></i> Đăng xuất
        </button>
      </div>
    }
  </div>
</div>
```

Shipped titles, verbatim, one per route: `Dashboard` · `Danh mục` · `Người dùng hệ thống` · `Phân quyền` (the two auth routes also declare titles — `Đăng nhập`, `Đổi mật khẩu` — but carry `noShell: true`, so no Topbar renders for them).

Sources: `src/FE/src/app/shared/components/topbar/topbar.html:1-24`, `src/FE/src/app/shared/components/topbar/topbar.scss:1-62`, `src/FE/src/app/shared/components/topbar/topbar.ts:24`, `:40-43`, `src/FE/src/app/app.html:5`, `src/FE/src/app/app.ts:10`, `:44`, `src/FE/src/app/modules/dashboard/dashboard.routes.ts:10`, `src/FE/src/app/modules/danh-muc-dti/danh-muc-dti.routes.ts:10`, `src/FE/src/app/platform/quan-tri-nguoi-dung/quan-tri-nguoi-dung.routes.ts:10`, `src/FE/src/app/platform/phan-quyen/phan-quyen.routes.ts:9`

## Do / Don't

- ✅ Let the shell own the title. `Topbar.title` is `input.required<string>()`; the component never inspects the router itself, so a new route only has to declare `data: { title: '…' }`.
- ✅ Keep `.topin`'s `max-width` equal to `<main>`'s (`spacing.container-max-width` in both) — that alignment is what makes the header track the content column instead of the viewport.
- ✅ Keep both the hamburger and the user block `no-print`; the entire bar is hidden in print anyway, and the markers document the intent locally.
- ❌ Don't give the bar an opaque background — the translucency plus `blur(10px)` is the shipped treatment and is what distinguishes it from `.card`.
- ❌ Don't add a `:disabled` state to the logout button to cover the request round-trip; the shipped behaviour is to navigate immediately on either outcome, on purpose (a failed `POST /auth/logout` used to leave the user sitting in the app believing they had signed out).

## Normalize on redesign
1. **`aria-controls="sidebar"` points at an id that does not exist.** The sidebar's `<aside>` (`sidebar.html:1`) carries no `id`, so the relationship is unresolvable for assistive tech. Either add `id="sidebar"` to the `<aside>` or drop the attribute.
2. **`.topbar-user-name` re-types `--fs-sm`/700 inline** rather than reusing the identical `typography.form-label` step — the same pair is hand-written in several places across the app.
3. **The `<h1>` weight is never declared**, so it depends on the UA default (bold) while every other heading in the app states its weight explicitly. `typography.h1-topbar` records 700 as the effective value.
4. **`9px` hamburger padding is off-scale** — between `spacing.sp-3` (8px) and `spacing.sp-4` (10px).
5. **The topbar has no reduced-transparency fallback**; `backdrop-filter` is unsupported or disabled in some environments, and there is no `@supports` branch.
