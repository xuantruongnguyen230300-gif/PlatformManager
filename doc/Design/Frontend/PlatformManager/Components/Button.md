---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
component: "Button"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/shared/components/topbar/topbar.html"
  - "src/FE/src/app/shared/components/toolbar/toolbar.html"
  - "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html"
  - "src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html"
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
  - "src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html"
---

# Button
**Description:** The labelled action trigger built on the global `.btn` class (`src/FE/src/styles.scss` § 3.1). Since the 2026-08-29 redesign it is a **bordered tonal button**: a pale brand tint as the fill, a 1px `colors.line` edge, and a full five-state treatment. It appears on every shipped route — card titles, toolbars, dialog footers, the topbar and both auth forms.

> **Citation policy.** Values below cite `src/FE/src/styles.scss` plus the **selector name**, not a line number. The file was rewritten on 2026-08-29 and its component library is still moving; a selector is stable evidence, a line number in a moving file is not.

## Anatomy
Single-line text label, optionally preceded by a PrimeIcons `<i class="pi pi-…">` glyph (see `Icons.md`). Rounded rectangle: `border: 1px solid colors.line`, fill `colors.tonal-bg`, ink `colors.tonal-ink`, radius `rounded.sm`, padding `spacing.button-padding`, `typography.button-label`, `cursor: pointer`, `text-decoration: none`. No fixed width; it sizes to its label. Transition: background and box-shadow at `duration.fast`, transform at `duration.instant`.

Two of those properties are new on 2026-08-29 and both are load-bearing:

- **The border replaced a transparent one.** `.btn` used to declare `1px solid transparent` so hover colour changes would not shift layout. The new `colors.tonal-bg` fill separates from `colors.card` by only 1.45:1 — not enough to see a button edge — and darkening the fill to 3:1 would drop the dark-blue label below AA. The boundary therefore moved to the border, where it costs nothing. The source records the measurement inline.
- **`text-decoration: none`.** `<a class="btn">` is how the app navigates with a button (`trang-chu.page.html` → `/doi-mat-khau`). Without the reset the anchor renders underlined and reads as a broken link.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Default (tonal secondary) | `btn` | border `colors.line`, fill `colors.tonal-bg`, ink `colors.tonal-ink` | Every labelled secondary action: dialog `Huỷ`, toolbar `Xoá lọc`, topbar `Đăng xuất`, the home card's `Đổi mật khẩu` link |
| Primary | `btn primary` | fill **and** border `colors.brand`, ink `colors.on-primary` | The one primary action per context: `+ Thêm người dùng`, `Áp dụng`, `Lưu thay đổi`, `Lưu`, `Đăng nhập`, `Đổi mật khẩu` |
| Danger | `btn danger` | border `colors.danger-border`, fill `colors.bad-bg`, ink `colors.bad` | Destructive confirmation only — `ConfirmDialog`'s confirm action while `[confirmDanger]` is true |
| Small | `btn sm` | padding `spacing.button-sm-padding`, `typography.button-sm-label`; everything else inherited | Row-level actions inside a table. **Declared in the shared library, no shipped call site today** — the one grid that ships uses ghost icon buttons instead |
| Block | `btn primary btn-block` | `width: 100%`, padding `spacing.button-block-padding`, `typography.button-block-label`, centred flex, gap `spacing.sp-3` | Full-width form submit on the two auth screens |
| Anchor | `a.btn` | identical box; `text-decoration: none` is what makes it read as a button | `trang-chu.page.html` — the only route into `/doi-mat-khau` when the account is not forced to change its password |
| Disclosure summary | `details.filter > summary.btn` | `.btn` box plus `list-style: none`, `inline-flex`, gap `spacing.sp-2`, `user-select: none`; while `[open]` the summary flips to fill and border `colors.brand`, ink `colors.on-primary` | The toolbar's `Lọc` trigger |
| Icon-only shell | `btn sidebar-hamburger` | `.btn` base plus screen-local geometry in `topbar.scss`; hidden above `spacing.breakpoint-tablet` | The mobile drawer trigger, holding only `pi pi-bars` |

**Not a variant of `.btn`.** The ghost icon-only family `.icon-btn` (`src/FE/src/styles.scss` § 3.2) is a separate component: transparent by default so a dense grid is not flooded with tonal fill. It absorbed five earlier one-off icon buttons on 2026-08-29 and is indexed separately in `COMPONENTS.md`.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Border, fill and ink per variant above; `rounded.sm`; `spacing.button-padding`; `typography.button-label`; `text-decoration: none` |
| hover | Default → fill `colors.btn-hover-bg` plus `shadow.btn-hover`. Primary → fill and border `colors.brand2` plus `shadow.primary-hover`. Danger → fill `colors.danger-hover-bg`, border unchanged |
| focus | `outline: 2px solid colors.brand`, `outline-offset: 2px` — `:focus-visible` only, so keyboard focus draws the ring and a mouse click does not |
| active | `transform: translateY(1px)` |
| disabled | `opacity: .5`, `cursor: not-allowed`. Declared on the base selector, so it reaches `.primary`, `.danger` and `.sm` alike. **Reachable and used** — `[disabled]` is bound on the two auth submits (`submitting()`), the permission-matrix save (`saving()` or `loading()`) and `ConfirmDialog`'s confirm action (`confirmDisabled()`) |

## Tokens Used
- `colors.tonal-bg`, `colors.btn-hover-bg`, `colors.tonal-ink`, `colors.line`, `colors.brand`, `colors.brand2`, `colors.on-primary`, `colors.bad`, `colors.bad-bg`, `colors.danger-hover-bg`, `colors.danger-border`
- `rounded.sm`
- `spacing.button-padding`, `spacing.button-sm-padding`, `spacing.button-block-padding`, `spacing.sp-2`, `spacing.sp-3`
- `typography.button-label`, `typography.button-sm-label`, `typography.button-block-label`
- `shadow.btn-hover`, `shadow.primary-hover`
- `duration.instant` (transform), `duration.fast` (background, shadow)
- Icons: PrimeIcons v7 — `pi-sign-in`, `pi-sign-out`, `pi-key`, `pi-bars`, `pi-filter`

The `translateY(1px)` press offset is a literal with no token behind it — there is no motion-distance scale (`Tokens/spacing.md` § Motion).

## Reference markup

```html
<!-- default tonal -->
<button type="button" class="btn" (click)="onCancel()">{{ cancelLabel() }}</button>

<!-- primary, with disabled binding and a progress label -->
<button type="button" class="btn primary" [disabled]="saving() || loading()" (click)="onSave()">
  {{ saving() ? 'Đang lưu…' : 'Lưu thay đổi' }}
</button>

<!-- danger or primary chosen at runtime by the confirm dialog -->
<button
  type="button"
  class="btn"
  [class.primary]="!confirmDanger()"
  [class.danger]="confirmDanger()"
  [disabled]="confirmDisabled()"
  (click)="onConfirm()"
>
  {{ confirmLabel() }}
</button>

<!-- anchor styled as a button -->
<a class="btn" routerLink="/doi-mat-khau"><i class="pi pi-key"></i> Đổi mật khẩu</a>

<!-- disclosure summary styled as a button -->
<summary class="btn">
  <i class="pi pi-filter"></i> Lọc
  @if (filterCount() > 0) { <span class="filter-count">{{ filterCount() }}</span> }
</summary>

<!-- block modifier, icon + label -->
<button type="submit" class="btn primary btn-block" [disabled]="submitting()">
  <i class="pi pi-sign-in"></i> {{ submitting() ? 'Đang đăng nhập…' : 'Đăng nhập' }}
</button>
```

Sources: `src/FE/src/styles.scss` (§ 3.1 `.btn`, `.btn.primary`, `.btn.danger`, `.btn.sm`, `.btn-block`; § 6 `.filter > summary`, `.filter[open] > summary`), `src/FE/src/app/shared/components/topbar/topbar.html`, `src/FE/src/app/shared/components/topbar/topbar.scss` (`.sidebar-hamburger`), `src/FE/src/app/shared/components/toolbar/toolbar.html`, `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html`, `src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.html`, `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html`, `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html`, `src/FE/src/app/platform/login/pages/login/login.page.html`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html`

## Do / Don't

- ✅ One `btn primary` per context — the topbar, a card title bar, a toolbar and a dialog footer are each their own context and each carry one.
- ✅ Keep the `colors.line` border on every variant. It is the only thing that gives the tonal fill a visible edge; removing it re-creates the 1.45:1 problem the 2026-08-29 pass was measured to fix.
- ✅ Keep `text-decoration: none` when styling an `<a>` as a button, and use `routerLink` so navigation stays client-side.
- ✅ Bind `[disabled]` for in-flight submits and pair it with a label that swaps to a progress phrase (`Đang lưu…`, `Đang đăng nhập…`) — that is the shipped pattern at every disabled call site.
- ✅ Reach for `btn danger` only for genuinely destructive confirmation; `ConfirmDialog` selects it from `[confirmDanger]`, never by hand at the call site.
- ❌ Don't use `.btn` for an icon-only action inside a data grid — use `.icon-btn`, which stays transparent so a long list is not flooded with colour.
- ❌ Don't re-declare `.btn` in a component stylesheet. Every button in the app renders from this one rule; a local copy silently loses four of the five states, which is exactly the drift the 2026-08-29 consolidation removed.
- ❌ Don't add a `.btn.success` or `.btn.warn` variant — only `primary` and `danger` ship.

## Normalize on redesign
1. **`.btn.sm` has no shipped call site.** It is a library variant waiting for the first table with row-level text actions. Keep it only if such a table lands; otherwise it is a spec for something nobody renders.
2. **The `translateY(1px)` press offset is a bare literal.** Shadows and durations gained tokens in the 2026-08-29 pass (`shadow.*`, `duration.*`); motion *distance* still has none.
3. **`.btn-block` is only ever combined with `primary`.** A tonal or danger block button has no defined treatment should one be needed.
4. **`summary.btn` inherits the button box but not `:disabled`.** A `<summary>` cannot be disabled, so a filter trigger that must be unavailable has no shipped appearance.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **Invisible button boundary — resolved 2026-08-29.** `.btn` carried `1px solid transparent`; the tonal fill was the only edge, measuring 1.45:1 against `colors.card`. The border is now `colors.line` (3.00:1 on card, clearing WCAG 2.2 SC 1.4.11).
2. **Underlined anchor buttons — resolved 2026-08-29.** `text-decoration: none` was added to `.btn` when `<a class="btn">` became the navigation pattern on the home screen.
3. **No compact size — added 2026-08-29.** `.btn.sm` now exists for in-row actions (see Normalize #1 for its open question).
4. **Untokenised hover shadows — resolved 2026-08-29.** Both `.btn` hover elevations are now named tokens (`shadow.btn-hover`, `shadow.primary-hover`) in `Tokens/tokens.json` and `DESIGN.md`.
