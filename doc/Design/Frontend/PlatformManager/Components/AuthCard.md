---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "AuthCard"
sources:
  - "src/FE/src/app/shared/components/auth-card/auth-card.html"
  - "src/FE/src/app/shared/components/auth-card/auth-card.scss"
  - "src/FE/src/app/shared/components/auth-card/auth-card.ts"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
  - "src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html"
---

# AuthCard
**Description:** The unauthenticated shell (`<app-auth-card [title] [subtitle]>`, `.login-shell` → `.login-card` → `.login-brand`) — a viewport-centred card with a brand mark, heading and optional subtitle, into which the sign-in and change-password forms are content-projected. It is the **second of the app's two shells**: routes marked `data: { noShell: true }` render a bare `<router-outlet>` with **no Sidebar and no Topbar**.

## Anatomy

`.login-shell` — `min-height:100vh` immediately overridden by `100dvh`, flex, centred on both axes, padding `spacing.sp-5`. It paints nothing; the page color comes from `body { background: var(--bg) }`.

`.login-card` — `width:100%` clamped by `max-width: spacing.login-card-max-width` (380px), fill `colors.card`, border 1px `colors.line`, radius `rounded.lg`, `colors.shadow`, padding `spacing.auth-card-padding` (`32px 28px`). It reproduces the `.card` recipe but is a **separate class**, not a modifier of `.card` — the only difference is its padding and width cap.

`.login-brand` — column flex, centred, gap `spacing.sp-3`, `margin-bottom:24px` literal, `text-align:center`:
- `.brand-mark` — `spacing.brand-mark-auth` square (44px), radius `12px` literal (numerically `rounded.table`), fill `colors.brand`, text `colors.on-primary`, weight 800, `--fs-lg`. Content is the two-letter string `PM`; there is **no image asset** anywhere in the app.
- `<h1>` — `typography.h1-auth` (`18px`/800, both literals), `margin:0`, bound to the required `title` input.
- `<p>` — `colors.muted`, `--fs-sm`, `margin:0`, rendered only when the optional `subtitle` input is non-empty.

`<ng-content />` sits directly after the brand block. The projected form's styling (`.field`, `.field-input`, `.field-row`, `.login-error`, `.btn-block`) is **global**, in `styles.scss`, not scoped here — Angular's emulated encapsulation does not reach projected content, so scoping those classes to `auth-card.scss` would leave the forms unstyled. See `AuthField.md`.

## Variants

| Variant | Classes / inputs | Key values | When to use |
| --- | --- | --- | --- |
| Title only | `[title]` set, `subtitle` omitted | `<p>` not rendered (`@if (subtitle())`) | No shipped screen uses this — both call sites pass a subtitle; it is the component's declared default (`subtitle = input<string>('')`) |
| Title + subtitle | `[title]` + `[subtitle]` | Brand mark, `<h1>`, `<p>` stacked and centred | Both shipped auth screens |
| Static subtitle | `subtitle="…"` | Fixed string | `/dang-nhap` — `title="PlatformManager"`, `subtitle="Đăng nhập để tiếp tục"` |
| Computed subtitle | `[subtitle]="subtitle()"` | Two strings selected by `isForced()` | `/doi-mat-khau` — `title="Đổi mật khẩu"`; subtitle is `Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống.` when the account carries `mustChangePassword`, otherwise `Đổi mật khẩu tài khoản của bạn.` This is the only place either auth screen varies its own copy at runtime |

There is **no responsive variant**: `auth-card.scss` contains no `@media` block. The card is fluid up to 380px and the shell padding absorbs the rest at every width, including print.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Centred card: `colors.card` fill, `colors.line` hairline, `rounded.lg`, `colors.shadow`, `spacing.auth-card-padding`, capped at `spacing.login-card-max-width` |
| hover | **Not applicable** — `.login-shell`, `.login-card` and `.login-brand` are static containers; `auth-card.scss` authors no `:hover` rule for any of them. Hover states inside belong to the projected fields (`AuthField.md`) and the submit button (`Button.md`) |
| focus-visible | **Not applicable** — none of the three elements is focusable (no `tabindex`, no interactive role). Focus lives entirely in the projected form; the card has no focus-trap or autofocus behaviour of its own (the first field self-focuses via its own `appAutofocus`/`autocomplete` wiring, not via the card) |
| active / selected | **Not applicable** — the card is not a control and has no selected concept; no `:active` rule exists |
| disabled | **Not applicable** — not a form control. The disabled concept applies only to the projected submit button, which binds `[disabled]="submitting()"` and takes `.btn:disabled` (`opacity:0.5`, `cursor:not-allowed`) from the global rule |

## Tokens Used
- `colors.card`, `colors.line`, `colors.brand`, `colors.on-primary`, `colors.muted`, `colors.shadow`; page background via `colors.bg` on `body`
- `rounded.lg`; the brand mark's `12px` radius is an un-tokenised literal (numerically `rounded.table`) — catalogued in `Tokens/spacing.md` § Radius literals bypassing the scale
- `spacing.sp-3`, `spacing.sp-5`, `spacing.auth-card-padding`, `spacing.login-card-max-width`, `spacing.brand-mark-auth`, `spacing.shell-height` (the `100vh`→`100dvh` pair)
- `typography.h1-auth`; subtitle uses `--fs-sm`, brand mark uses `--fs-lg`
- `24px` brand-block margin is an off-scale literal — catalogued in `Tokens/spacing.md`

## Reference markup

```html
<div class="login-shell">
  <div class="login-card">
    <div class="login-brand">
      <span class="brand-mark">PM</span>
      <h1>{{ title() }}</h1>
      @if (subtitle()) {
        <p>{{ subtitle() }}</p>
      }
    </div>
    <ng-content />
  </div>
</div>
```

```html
<!-- call site: /dang-nhap -->
<app-auth-card title="PlatformManager" subtitle="Đăng nhập để tiếp tục"> … </app-auth-card>
<!-- call site: /doi-mat-khau -->
<app-auth-card title="Đổi mật khẩu" [subtitle]="subtitle()"> … </app-auth-card>
```

Sources: `src/FE/src/app/shared/components/auth-card/auth-card.html:1-12`, `src/FE/src/app/shared/components/auth-card/auth-card.scss:1-52`, `src/FE/src/app/shared/components/auth-card/auth-card.ts:16-19`, `src/FE/src/app/platform/login/pages/login/login.page.html:1`, `src/FE/src/app/platform/login/pages/login/login.page.scss:4-6`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html:1`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:36-41`, `src/FE/src/app/app.ts:45` (`showShell`), `src/FE/src/app/platform/login/login.routes.ts:9`, `src/FE/src/app/platform/doi-mat-khau/doi-mat-khau.routes.ts:10`

## Do / Don't

- ✅ Reach this shell only through a route carrying `data: { noShell: true }` — that flag, read by `App.showShell()`, is the single switch between the two shells.
- ✅ Keep the projected form's classes global. Both auth pages set `:host { display: contents }` and ship **zero** component-scoped CSS on purpose; the styles must live in `styles.scss` for content projection to reach them.
- ✅ Pass a subtitle. Both shipped screens do, and the brand block's `gap`/`margin-bottom` rhythm was tuned with three stacked children.
- ✅ Keep `<app-toast />` above this shell (`app.html:14`) — it is how a failed sign-in surfaces a network-level error, alongside the inline `.login-error` block.
- ❌ Don't render `Sidebar` or `Topbar` here. Their absence is the defining property of the auth shell, not an omission.
- ❌ Don't swap `.login-card` for `.card`. They are intentionally separate classes today; merging them is a Normalize decision, not a free refactor.

## Normalize on redesign
1. **`.login-card` duplicates the `.card` recipe** (same fill, hairline, `rounded.lg`, `colors.shadow`) while differing only in padding and width cap. One class with a size modifier would remove the second copy — and the risk that a future `.card` change silently skips the auth screens.
2. **Three off-scale literals**: padding `32px 28px`, `margin-bottom:24px`, brand-mark radius `12px`. Only the radius has an exact match in the existing scale.
3. **`typography.h1-auth` (18px/800) is off the `--fs-*` scale entirely** — the scale tops out at 15px, so the largest heading in the app is un-tokenised.
4. **Two different `.brand-mark` treatments ship** — 26px/`7px` radius/`--fs-xs` in the sidebar versus 44px/`12px` radius/`--fs-lg` here — under the same class name in two scoped stylesheets. They never collide, but the name promises one component and delivers two.
5. **No `@media print` handling**: the auth shell is the only surface with no print rule, so a printed login page keeps its full-viewport centring.
