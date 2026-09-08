---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "AuthCard"
sources:
  - "src/FE/src/app/shared/components/auth-card/auth-card.html"
  - "src/FE/src/app/shared/components/auth-card/auth-card.scss"
  - "src/FE/src/app/shared/components/auth-card/auth-card.ts"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
  - "src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html"
  - "src/FE/public/i18n/vi.json"
---

# AuthCard
**Description:** The unauthenticated shell (`<app-auth-card [title] [subtitle]>`, `.login-shell` → `.login-card` → `.login-brand`) — a viewport-centred card with a brand mark, heading and optional subtitle, into which the sign-in and change-password forms are content-projected. It is the **second of the app's two shells**: routes marked `data: { noShell: true }` render a bare `<router-outlet>` with **no Sidebar and no Topbar**.

## Anatomy

`.login-shell` — `min-height:100vh` immediately overridden by `100dvh`, flex, centred on both axes, padding `spacing.sp-5`. It paints nothing; the page color comes from `body { background: var(--bg) }`.

`.login-card` — `width:100%` clamped by `max-width: spacing.login-card-max-width` (380px), fill `colors.card`, border 1px `colors.line`, radius `rounded.lg`, `colors.shadow`, padding `spacing.auth-card-padding` (`32px 28px`). It reproduces the `.card` recipe but is a **separate class**, not a modifier of `.card` — the only difference is its padding and width cap.

`.login-brand` — column flex, centred, gap `spacing.sp-3`, `margin-bottom:24px` literal, `text-align:center`:
- `.brand-mark` — `spacing.brand-mark-auth` square (44px), radius `12px` literal (numerically `rounded.table`), fill `colors.brand`, text `colors.on-primary`, weight 800, `--fs-lg`. Content is `{{ branding.shortName }}` — `CORE_BRANDING`, injected (`auth-card.ts:28`), not a literal; the app supplies `PM` in `app.config.ts` § `APP_BRANDING`. There is **no image asset** anywhere in the app.
- `<h1>` — `typography.h1-auth` (`18px`/800, both literals), `margin:0`, bound to the required `title` input.
- `<p>` — `colors.muted`, `--fs-sm`, `margin:0`, rendered only when the optional `subtitle` input is non-empty.

`<ng-content />` sits directly after the brand block. On `/dang-nhap` it receives the error block, the form **and** `<app-language-switcher class="login-language" />` — the sign-in screen is the only screen a signed-out user can reach, and it has no app shell, so the language control has nowhere else to live. The projected form's styling is **global**, in `styles.scss`, not scoped here — Angular's emulated encapsulation does not reach projected content, so scoping those classes to `auth-card.scss` would leave both forms unstyled with no compile error. Two specs cover what gets projected: `AuthField.md` for the auth-only scaffolding (`.field`, `.field-row`, `.login-error`, `.btn-block`) and `Input.md` for the control inside each field (`.input-icon`, renamed from `.field-input` and merged with the toolbar search on 2026-08-29).

## Variants

| Variant | Classes / inputs | Key values | When to use |
| --- | --- | --- | --- |
| Title only | `[title]` set, `subtitle` omitted | `<p>` not rendered (`@if (subtitle())`) | No shipped screen uses this — both call sites pass a subtitle; it is the component's declared default (`subtitle = input<string>('')`) |
| Title + subtitle | `[title]` + `[subtitle]` | Brand mark, `<h1>`, `<p>` stacked and centred | Both shipped auth screens |
| Fixed subtitle | `[subtitle]="'…' \| translate"` | One key | `/dang-nhap` — `[title]="branding.name"` (the product name, injected from `CORE_BRANDING`; `login.page.ts:73`), `[subtitle]="'login.subtitle' \| translate"` |
| Computed subtitle | `[subtitle]="subtitleKey() \| translate"` | Two **keys** selected by `isForced()` | `/doi-mat-khau` — `[title]="'shared.action.changePassword' \| translate"`; the subtitle key is `doi-mat-khau.hint.forcedSubtitle` when the account carries `mustChangePassword`, otherwise `doi-mat-khau.hint.subtitle` (`doi-mat-khau.page.ts:150-154`). The computed holds the **key**, not the sentence, so the subtitle follows the language switcher. This is the only place either auth screen varies its own copy at runtime |

There is **no responsive variant**: `auth-card.scss` contains no `@media` block. The card is fluid up to 380px and the shell padding absorbs the rest at every width, including print.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Centred card: `colors.card` fill, `colors.line` hairline, `rounded.lg`, `colors.shadow`, `spacing.auth-card-padding`, capped at `spacing.login-card-max-width` |
| hover | **Not applicable** — `.login-shell`, `.login-card` and `.login-brand` are static containers; `auth-card.scss` authors no `:hover` rule for any of them. Hover states inside belong to the projected fields (`AuthField.md`) and the submit button (`Button.md`) |
| focus-visible | **Not applicable** — none of the three elements is focusable (no `tabindex`, no interactive role). Focus lives entirely in the projected form; the card has no focus-trap and no autofocus behaviour of its own. Note that **neither auth screen autofocuses either** — `appAutofocus` ships, but its only call site is the username field of the user-admin dialog |
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
      <span class="brand-mark">{{ branding.shortName }}</span>
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
<app-auth-card [title]="branding.name" [subtitle]="'login.subtitle' | translate"> … </app-auth-card>
<!-- call site: /doi-mat-khau -->
<app-auth-card
  [title]="'shared.action.changePassword' | translate"
  [subtitle]="subtitleKey() | translate"
> … </app-auth-card>
```

Sources: `src/FE/src/app/shared/components/auth-card/auth-card.html:1-14`, `src/FE/src/app/shared/components/auth-card/auth-card.scss:1-52`, `src/FE/src/app/shared/components/auth-card/auth-card.ts:28`, `:30-31`, `src/FE/src/app/platform/login/pages/login/login.page.html:1`, `:99` (the projected language switcher), `src/FE/src/app/platform/login/pages/login/login.page.scss:4-6` (`:host { display: contents }`), `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html:1-4`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:150-154`, `src/FE/src/app/app.ts:61` (`showShell`), `src/FE/src/app/app.html:38-40` (the `@else` branch this shell renders in), `src/FE/src/app/platform/login/login.routes.ts:19`, `src/FE/src/app/platform/doi-mat-khau/doi-mat-khau.routes.ts:16`, `src/FE/src/app/app.routes.spec.ts:244-249` (the test pinning exactly two `noShell` routes)

> 🔄 **SỬA 2026-09-06** — bản trước sai bốn chỗ:
> 1. Ô thương hiệu ghi là chuỗi `PM` khai cứng; thật ra nó đến từ `CORE_BRANDING` (tách 2026-09-02).
> 2. Hai ô của bảng Variants chép nguyên câu tiếng Việt làm giá trị `title`/`subtitle`; cả hai màn nay truyền câu ĐÃ DỊCH từ khoá, và `/doi-mat-khau` giữ **khoá** trong computed chứ không giữ câu.
> 3. `<ng-content />` nay còn nhận `<app-language-switcher>` trên màn đăng nhập — spec không nhắc gì.
> 4. Chín neo `file:dòng` trỏ sai chỗ, trong đó `app.html:21-23` và `app.ts:51` trỏ vào phần khác hẳn sau khi `app.html` mọc thêm dải "đã có phiên bản mới" và skip link.

## Do / Don't

- ✅ Reach this shell only through a route carrying `data: { noShell: true }` — that flag, read by `App.showShell()`, is the single switch between the two shells. Exactly two routes set it, and `app.routes.spec.ts` fails the build if a third appears or one disappears.
- ✅ Keep the projected form's classes global. Both auth pages set `:host { display: contents }`, and every class the form itself is built from (`.field`, `.input-icon`, `.login-error`, `.btn-block`) lives in `styles.scss` — emulated encapsulation does not reach projected content. What each page's own stylesheet may hold is **placement**, not shape: `login.page.scss` carries exactly two such rules, `.login-hint` and `.login-language` (`login.page.scss:12-25`), and `doi-mat-khau.page.scss` still carries none.
- ✅ Pass a subtitle. Both shipped screens do, and the brand block's `gap`/`margin-bottom` rhythm was tuned with three stacked children.
- ✅ Keep `<app-toast />` outside the shell branch (`app.html:41`) — it is how a failed sign-in surfaces a network-level error, alongside the inline `.login-error` block.
- ❌ Don't render `Sidebar` or `Topbar` here. Their absence is the defining property of the auth shell, not an omission.
- ❌ Don't swap `.login-card` for `.card`. They are intentionally separate classes today; merging them is a Normalize decision, not a free refactor.

## Normalize on redesign
1. **`.login-card` duplicates the `.card` recipe** (same fill, hairline, `rounded.lg`, `colors.shadow`) while differing only in padding and width cap. One class with a size modifier would remove the second copy — and the risk that a future `.card` change silently skips the auth screens.
2. **Three off-scale literals**: padding `32px 28px`, `margin-bottom:24px`, brand-mark radius `12px`. Only the radius has an exact match in the existing scale.
3. **`typography.h1-auth` (18px/800) is off the `--fs-*` scale entirely** — the scale tops out at 15px, so the largest heading in the app is un-tokenised.
4. **Two different `.brand-mark` treatments ship** — 26px/`7px` radius/`--fs-xs` in the sidebar versus 44px/`12px` radius/`--fs-lg` here — under the same class name in two scoped stylesheets. They never collide, but the name promises one component and delivers two.
5. **No print handling at all**: the auth shell carries neither a `no-print` class nor a print rule, so a printed login page keeps its full-viewport centring and prints the form as an interactive control that is not interactive. It is the only top-level surface the 2026-08-29 print consolidation did not touch, because there was nothing there to consolidate.
6. **The card's separation from the page rides entirely on the shared `card`-on-`bg` pair.** The 2026-08-29 palette pass darkened `colors.bg` to lift that ratio from 1.12:1 to 1.41:1 (`Tokens/colors.md` § Contrast), which is what this screen relies on — it is a lone card on an otherwise empty viewport, with no sibling surface to read against. Any future change to `colors.bg` moves this screen more than any other, and nothing in `auth-card.scss` says so.
