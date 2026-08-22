---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "TabBar"
sources:
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html"
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.scss"
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts"
---

# TabBar
**Description:** The two-button view switcher above the permission cards (`.tabs-bar`) — the app's only tab control. It is not a tab widget: it is a bare flex row of standard `.btn`s where the active one gains `.primary`, so the "selected" state is carried entirely by the button variant.

## Anatomy

`.tabs-bar` — row flex, gap `spacing.sp-3`, `margin-bottom: spacing.sp-4`, marked `no-print`. That is the whole rule: three lines, no background, no border, no padding, no underline rail.

Children are two `<button type="button" class="btn">` elements (see `Button.md` for the box) with `[class.primary]` bound to an equality check against a signal:

```
[class.primary]="activeTab() === 'menu'"      →  Phân quyền màn hình
[class.primary]="activeTab() === 'resource'"  →  Quyền theo tài nguyên
```

`activeTab` is a `signal<'menu' | 'resource'>('menu')`, so the first tab is selected on load. Switching is a pure client-side signal write: both panels' data is fetched once when the page opens, deliberately — lazy-loading the second tab was considered and rejected on 2026-08-18 because it would add a loading state to every tab switch for a dataset small enough that nobody would feel the saving.

Each tab controls a sibling `@if` block, each of which is a full `.card` with its own `.title` (heading + a `.btn.primary` "Lưu thay đổi"), a `.muted` explanatory paragraph, and one permission matrix.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Two-tab bar | `tabs-bar no-print` | Flex row, gap `spacing.sp-3`, margin-bottom `spacing.sp-4` | `/quan-tri/phan-quyen` — the only instance in the app |
| Tab, resting | `btn` | Tonal: fill `colors.tonal-bg`, text `colors.tonal-ink`, radius `rounded.sm`, padding `spacing.button-padding`, `typography.button-label` | The tab that is not currently shown |
| Tab, selected | `btn primary` | Fill `colors.brand`, border `colors.brand`, text `colors.on-primary` | The tab whose panel is rendered |

There is no responsive variant (`phan-quyen.page.scss` has no `@media` block); the two buttons stay side by side at every width and the bar does not wrap or scroll.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

`.tabs-bar` is a layout row; the states below are the tab buttons', inherited whole from the global `.btn` rule.

| State | Treatment |
| --- | --- |
| default | Resting tab: `colors.tonal-bg` fill, `colors.tonal-ink` text, `border:1px solid transparent`, `rounded.sm`, `spacing.button-padding`, `typography.button-label` |
| hover | Resting tab: `background: colors.tonal-bg-hover` + `colors.shadow-btn-hover`. Selected tab: `background: colors.brand2`, border `colors.brand2`, `colors.shadow-primary-hover` |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 2px` — from `.btn:focus-visible`; `.tabs-bar` itself adds nothing |
| active / selected | Two different things, both real. **Pressed:** `.btn:active { transform: translateY(1px) }`. **Selected:** the `.primary` variant — solid `colors.brand` fill with `colors.on-primary` text — which is the only signal of which panel is showing |
| disabled | **Not applicable — a tab is never disabled.** Neither button binds `[disabled]`, and both panels' data is loaded up front, so there is no "not ready yet" tab to grey out. (The `.btn:disabled` rule — `opacity:0.5`, `cursor:not-allowed` — does exist globally and *is* used on this screen, but on the two "Lưu thay đổi" buttons inside the panels, not on the tabs) |

## Tokens Used
- Via `.btn`: `colors.tonal-bg`, `colors.tonal-bg-hover`, `colors.tonal-ink`, `colors.brand`, `colors.brand2`, `colors.on-primary`, `colors.shadow-btn-hover`, `colors.shadow-primary-hover`, `rounded.sm`, `spacing.button-padding`, `typography.button-label`
- Own: `spacing.sp-3` (gap), `spacing.sp-4` (margin-bottom)
- No icons — both tabs are text-only

## Reference markup

```html
<div class="tabs-bar no-print">
  <button type="button" class="btn" [class.primary]="activeTab() === 'menu'" (click)="setTab('menu')">
    Phân quyền màn hình
  </button>
  <button type="button" class="btn" [class.primary]="activeTab() === 'resource'" (click)="setTab('resource')">
    Quyền theo tài nguyên
  </button>
</div>
```

```scss
.tabs-bar {
  display: flex;
  gap: var(--sp-3);
  margin-bottom: var(--sp-4);
}
```

Verbatim tab labels: `Phân quyền màn hình` and `Quyền theo tài nguyên`. The panels they reveal carry the same two strings as their card headings.

Sources: `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:1-8`, `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.scss:6-10`, `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts:9`, `:33`, `:49-53` (the "load both tabs up front" decision), `src/FE/src/styles.scss:142-195` (the `.btn` rule the tabs inherit)

## Do / Don't

- ✅ Use `[class.primary]` bound to an equality test rather than a separate `.active` class — the selected tab and a primary action are visually identical here on purpose, and reusing `.primary` is what keeps them in step.
- ✅ Repeat the tab label as the revealed card's `<h2>`; both panels do, so the heading confirms which view is open once the bar scrolls out of sight.
- ✅ Keep the bar `no-print`. The panel below it prints; the switcher would be meaningless on paper.
- ✅ Keep both datasets loaded. Switching is instant by design, and adding a per-tab loading state would undo a decision that was made and recorded deliberately.
- ❌ Don't add an underline rail, a pill track or a segmented container — `.segmented`/`.seg-btn` (the dashboard's period toolbar) is a **different** control with a shared border and internal dividers, and mixing the two treatments would give the app two tab metaphors.
- ❌ Don't disable a tab; every tab here is always reachable.

## Normalize on redesign
1. **No ARIA tab semantics.** There is no `role="tablist"`, no `role="tab"`, no `aria-selected` and no `aria-controls`; the panels have no `role="tabpanel"`. Assistive tech hears two ordinary buttons, and the selected one is distinguishable only by colour.
2. **Selection is signalled by colour alone** — a solid brand fill versus a tonal fill. There is no weight change, underline or icon, so the state is invisible in monochrome and marginal for colour-vision deficiency.
3. **Two competing switcher treatments ship**: this `.btn`-based bar and the dashboard's `.segmented` / `.seg-btn` group. They solve the same problem with different anatomy.
4. **No keyboard arrow navigation** between tabs (implied by the tab pattern) — only Tab-to-focus plus Enter/Space.
5. **The bar cannot grow.** Flex with no wrap and no overflow handling works for two tabs and breaks for five.
