---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "Sidebar"
sources:
  - "src/FE/src/app/shared/components/sidebar/sidebar.html"
  - "src/FE/src/app/shared/components/sidebar/sidebar.scss"
  - "src/FE/src/app/shared/components/sidebar/sidebar.ts"
  - "src/FE/src/app/shared/services/sidebar-state.service.ts"
  - "src/FE/src/app/app.html"
  - "src/FE/src/app/core/config/core-branding.ts"
---

# Sidebar
**Description:** The app shell's fixed left navigation rail (`<app-sidebar />`, `.sidebar`) — brand block, a menu tree loaded at runtime from the menu endpoint, a collapse toggle, and an off-canvas drawer backdrop for narrow viewports. Present on every route that does **not** set `data: { noShell: true }`; deliberately absent on the two auth routes (see `AuthCard.md`). Count the shell routes rather than trusting a number in prose — `grep -c "loadChildren" src/FE/src/app/app.routes.ts` minus the two `noShell` ones, which `app.routes.spec.ts` pins.

## Anatomy

`<aside id="sidebar" class="sidebar no-print">` — `position:fixed`, top/left 0, `height:100vh` immediately overridden by `100dvh`, width `spacing.sidebar-w`, background `colors.card`, right border 1px `colors.line`, `z-index:35`, column flex, `transition: width 0.2s ease, transform 0.25s ease`.

Two attributes on that element are load-bearing and were both added on 2026-08-29:

- **`id="sidebar"`** is the target of the Topbar hamburger's `aria-controls="sidebar"`. Before this date the attribute pointed at nothing: screen readers drop a dangling `aria-controls`, so the "this button opens that region" relationship simply did not exist for anyone using one.
- **`no-print`** replaced the component's own `@media print` block. Print rules are now centralised — one `@media print { .no-print { display:none !important } }` in `styles.scss` — instead of being re-declared per component. The printed output is identical; there is one rule instead of several. `.sidebar-backdrop` carries the same class for the same reason.

Three regions stack inside it:

1. **`.sidebar-brand`** — row flex, gap `spacing.sp-3`, padding `spacing.sp-4`, bottom border 1px `colors.line`, `min-height` `spacing.sidebar-brand-height`, `flex:none`. Contains:
   - `.brand-mark` — `spacing.brand-mark-sidebar` square, radius `7px` literal (numerically `rounded.sm`), fill `colors.brand`, text `colors.on-primary`, weight 800, `typography.muted-caption` size. Content is `{{ branding.shortName }}` — `CORE_BRANDING`, injected, not a literal in the template (`sidebar.ts:48`; the app supplies `PM` in `app.config.ts` § `APP_BRANDING`). There is **no image asset** — the mark is typographic.
   - `.brand-text` — `{{ branding.name }}`, same injected source as the mark; `typography.sidebar-brand-text`, `colors.text`, `white-space:nowrap` + `text-overflow:ellipsis`.
   - `.icon-btn.sidebar-toggle` — an `IconButton` (`Components/IconButton.md`) containing `pi pi-angle-left`, `[attr.aria-label]` switching between `shared.sidebar.expand` and `shared.sidebar.collapse`. **`.sidebar-toggle` now declares only two things**: `margin-left:auto` (push to the right edge, and `0` in the collapsed rail so the button centres) and a `transform 0.2s ease` transition on its glyph, which gets `rotate(180deg)` while `.sidebar.collapsed`. Everything else — size, radius, border, colour, hover fill, focus ring, disabled treatment — comes from the global `.icon-btn`.
2. **`<nav [attr.aria-label]="'shared.sidebar.mainNav' | translate"> > ul.sidebar-nav`** *(was the literal `aria-label="Main"` until 2026-09-05)* — `list-style:none`, padding `spacing.sp-2`, `overflow-y:auto`. One `<li>` per top-level menu item; a `<li>` with children carries `.sidebar-navgroup` and, when open, `.open`.
3. **`.sidebar-backdrop`** — sibling of `<aside>`, `position:fixed; inset:0`, fill `colors.overlay-backdrop`, `z-index:34`, `display:none` until `.show`. `aria-hidden="true"` on purpose: it is a decorative dismiss overlay, not a tab stop.

**Nav item** (`.sidebar-navitem`) is the one repeated atom: row flex, gap `spacing.sp-3`, padding `spacing.sp-2` `spacing.sp-3`, radius `rounded.md`, `colors.text`, `typography.sidebar-nav-item`, `width:100%`, `background:none`, `border:0`, `text-align:left`. It renders as `<a routerLink>` for a destination and as `<button>` for a group parent — same class, same box. Inside: `.navicon` (`spacing.nav-icon` square, `colors.muted`, `font-size:15px` literal) → `.sidebar-navlabel` → (group parent only) `.navchevron` (`pi pi-chevron-down`, `margin-left:auto`, `colors.muted`, `font-size:12px` literal, `transform:rotate(-90deg)` while the group is closed).

Submenu: `ul.sidebar-submenu`, `margin:2px 0 4px`, `padding:0`; its items add `.sidebar-subitem` (`padding-left:38px` literal). A closed group hides the whole `<ul>` with `display:none`.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Expanded rail (default) | `.sidebar` | width `spacing.sidebar-w`; `.shell-content` offset by a matching `margin-left` | ≥ `spacing.breakpoint-desktop`, collapse flag off |
| Collapsed rail | `.sidebar.collapsed` | width `spacing.sidebar-w-collapsed`; `.sidebar-navlabel` visually hidden via the clip-rect pattern; nav items centred with padding `spacing.sp-2`; `.brand-text` hidden; brand padding `spacing.sp-4` `spacing.sp-1`; active rail moves to `left:0`; the toggle drops its `margin-left:auto` so it centres | User pressed the toggle; the flag persists in `localStorage` (`core.sidebar.collapsed.v1` — renamed 2026-09-03 to drop the product name from a reusable layer; `sidebar-state.service.ts:21`) |
| Collapsed flyout submenu | `.sidebar.collapsed .sidebar-navgroup:hover/:focus-within .sidebar-submenu` | absolute at `left:100%`, `min-width` `spacing.sidebar-flyout-width`, margin-left 6px, padding 6px, fill `colors.card`, border `colors.line`, radius `10px` literal, `colors.shadow`, `z-index:40`; sub-items regain padding `10px 12px` and their labels un-hide | Only inside the single `min-width:981px` media block |
| Off-canvas drawer (tablet) | `.sidebar` @ `spacing.breakpoint-tablet` | width `spacing.sidebar-w-drawer-tablet`, `transform:translateX(-100%)`, `colors.shadow`; `.collapsed` is neutralised (labels and brand text return) | ≤980px — opened by the Topbar hamburger |
| Off-canvas drawer (mobile) | `.sidebar` @ `spacing.breakpoint-mobile` | width `spacing.sidebar-w-drawer-mobile`; nav item padding `10px`, `min-height` `spacing.sidebar-navitem-height-mobile` | ≤560px |
| Drawer open | `.sidebar.drawer-open` | `transform:translateX(0)` + `.sidebar-backdrop.show` | While `mobileOpen()` is true |
| Leaf nav item | `a.sidebar-navitem` | `routerLinkActive="active"`, `[title]` = the item label | Menu item with a route and no children |
| Group parent | `button.sidebar-navitem.sidebar-navparent` | `aria-expanded`, `aria-controls="submenu-<id>"`, chevron; `.active` applied by `isGroupActive()` when the current URL starts with any child route | Menu item with children (one nesting level only) |
| Sub item | `a.sidebar-navitem.sidebar-subitem` | `padding-left:38px` literal | Child of a group |
| Print | `.no-print` on `<aside>` and on `.sidebar-backdrop` | Hidden by the single global `@media print { .no-print { display:none !important } }` rule in `styles.scss`. `sidebar.scss` has **no** `@media print` block of its own — removed 2026-08-29 | Any print output |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: `.sidebar-navitem`.

| State | Treatment |
| --- | --- |
| default | `background:none`, `colors.text`, `typography.sidebar-nav-item`, radius `rounded.md`; `.navicon` in `colors.muted` |
| hover | `background: colors.bg` (`sidebar.scss:110-112`) |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 2px` (`sidebar.scss:135-138`) |
| active / selected | `.active` — background `colors.surface-nav-active`, text `colors.brand`, `font-weight:700` (literal, above the 600 in `typography.sidebar-nav-item`), `.navicon` recoloured to `colors.brand`, plus the `::before` rail `spacing.active-nav-rail` (3px wide, `colors.brand`, radius `0 3px 3px 0`, inset 5px top/bottom, `left:-8px`; `left:0` in the collapsed rail). Applied by `routerLinkActive` for leaves and by `isGroupActive()` for parents |
| disabled | **Not applicable — no disabled affordance exists, by design.** `sidebar.scss` contains no `:disabled` rule and no template binding sets `disabled`/`aria-disabled`. Menu entries a user may not open are never rendered at all: the BE filters `SysMenuRole` by the caller's roles before returning the menu, and `Sidebar` reads that list straight through without re-filtering |

### States — `.icon-btn.sidebar-toggle` (supplementary)

Every row below is **inherited from `.icon-btn`**, not authored here — `sidebar.scss` keeps no state rule for this button since 2026-08-29. See `Components/IconButton.md` for the definitions.

| State | Treatment |
| --- | --- |
| default | `border:1px solid transparent`, `background:transparent`, `colors.muted`, `rounded.sm`, `spacing.icon-button` square. Local additions: `margin-left:auto`, and `margin-left:0` inside `.sidebar.collapsed` |
| hover | `background: colors.surface-2`, `color: colors.text` |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 1px` — note the offset is **1px**, not the 2px it used to declare locally; the button moved onto the shared value with the merge |
| active / selected | No `:active` rule. The *pressed* semantic is carried by state instead — `aria-expanded` flips and the glyph rotates 180° via the one local rule `.sidebar.collapsed .sidebar-toggle i { transform: rotate(180deg) }` |
| disabled | `opacity:.45`, `cursor:not-allowed`, hover suppressed. Never reached in practice — the toggle is always operable and nothing binds `[disabled]` — but the treatment now **exists**, which it did not while the button had its own hand-rolled copy |

## Tokens Used
- `colors.card`, `colors.line`, `colors.bg`, `colors.text`, `colors.muted`, `colors.brand`, `colors.on-primary`, `colors.surface-2`, `colors.surface-nav-active`, `colors.overlay-backdrop`, `colors.shadow`
- `rounded.md` (nav item), `rounded.sm` (toggle, via `.icon-btn`); the `7px` brand-mark radius and the `10px` flyout radius are un-tokenised literals — catalogued in `Tokens/spacing.md` § Radius literals bypassing the scale
- `spacing.sp-1`, `spacing.sp-2`, `spacing.sp-3`, `spacing.sp-4`
- `spacing.sidebar-w`, `spacing.sidebar-w-collapsed`, `spacing.sidebar-w-drawer-tablet`, `spacing.sidebar-w-drawer-mobile`, `spacing.sidebar-brand-height`, `spacing.sidebar-navitem-height-mobile`, `spacing.sidebar-flyout-width`, `spacing.icon-button` (toggle, via `.icon-btn`), `spacing.nav-icon`, `spacing.brand-mark-sidebar`, `spacing.active-nav-rail`
- `spacing.breakpoint-tablet`, `spacing.breakpoint-mobile`, `spacing.breakpoint-desktop`, `spacing.breakpoint-print`
- `typography.sidebar-brand-text`, `typography.sidebar-nav-item`, `typography.muted-caption`
- Icons: PrimeIcons v7 — `pi-angle-left` (toggle), `pi-chevron-down` (group chevron); per-item icons arrive from the API as literal `pi-*` class strings, with `pi-circle` as the only FE fallback (`sidebar.ts:14`, `:50-52`)

## Reference markup

```html
<aside id="sidebar" class="sidebar no-print"
  [class.collapsed]="state.collapsed()" [class.drawer-open]="state.mobileOpen()">
  <div class="sidebar-brand">
    <span class="brand-mark">{{ branding.shortName }}</span>
    <span class="brand-text">{{ branding.name }}</span>
    <button type="button" class="icon-btn sidebar-toggle no-print"
      [attr.aria-label]="(state.collapsed() ? 'shared.sidebar.expand' : 'shared.sidebar.collapse') | translate"
      [attr.aria-expanded]="!state.collapsed()" (click)="state.toggleCollapse()">
      <i class="pi pi-angle-left"></i>
    </button>
  </div>
  <nav [attr.aria-label]="'shared.sidebar.mainNav' | translate">
    <ul class="sidebar-nav">
      <li [class.sidebar-navgroup]="item.Children.length > 0" [class.open]="…">
        <button type="button" class="sidebar-navitem sidebar-navparent" [class.active]="isGroupActive(item)">
          <span class="navicon" aria-hidden="true"><i class="pi" [class]="iconClass(item.Icon)"></i></span>
          <span class="sidebar-navlabel">{{ item.Label }}</span>
          <span class="navchevron" aria-hidden="true"><i class="pi pi-chevron-down"></i></span>
        </button>
        <ul class="sidebar-submenu" [id]="'submenu-' + item.Id">
          <li><a [routerLink]="child.Route" class="sidebar-navitem sidebar-subitem" routerLinkActive="active">…</a></li>
        </ul>
      </li>
    </ul>
  </nav>
</aside>
<div class="sidebar-backdrop no-print" [class.show]="state.mobileOpen()" aria-hidden="true" (click)="state.closeDrawer()"></div>
```

Shipped menu tree (seeded server-side, verbatim labels + icons, read 2026-08-29): `Trang chủ` (`pi-home`, `/trang-chu`) · `Quản trị hệ thống` (`pi-cog`, group, no route of its own) → `Người dùng` (`pi-user`, `/quan-tri/nguoi-dung`), `Phân quyền` (`pi-shield`, `/quan-tri/phan-quyen`). The `Dashboard` and `Danh mục → DTI` entries an earlier revision of this spec listed are gone with the screens they pointed at.

Sources: `src/FE/src/app/shared/components/sidebar/sidebar.html:4-89` (the comment at `:1-3` records why the `id` exists; the `no-print` class is on `:6` and `:85`), `src/FE/src/app/shared/components/sidebar/sidebar.scss:3-319` (`.sidebar-toggle` reduced to its two local rules at `:58-68`; the removed `@media print` block is recorded in the closing comment at `:321-324`), `src/FE/src/app/shared/components/sidebar/sidebar.ts:48`, `:50-52`, `src/FE/src/app/shared/services/sidebar-state.service.ts`, `src/FE/src/app/app.html:28`, `src/FE/src/app/app.scss:37-48` (the matching `.shell-content` offset), `src/FE/src/app/shared/components/topbar/topbar.html:8` (the `aria-controls="sidebar"` this component is the target of), `src/FE/src/styles.scss` § `.icon-btn` and § `@media print`

> 🔄 **SỬA 2026-09-06** — bản trước sai bốn chỗ:
> 1. Ô thương hiệu ghi là chuỗi khai cứng `PM` / `PlatformManager`; thật ra nó đến từ `CORE_BRANDING` qua `inject()` (tách 2026-09-02).
> 2. `aria-label` của nút thu gọn trong Reference markup còn là hai câu tiếng Việt; nay là `shared.sidebar.expand` / `shared.sidebar.collapse`.
> 3. Bốn neo `file:dòng` trỏ sai chỗ: `sidebar.ts:12`/`:37-39` (thật: `:14` và `:50-52`), `sidebar.html:81` (dòng đó nằm trong khối chú thích; `no-print` thật ở `:85`), `app.html:11` (`<app-sidebar />` ở `:28`).
> 4. Normalize #1 mô tả một literal đã không còn — xem chính mục đó.

## Do / Don't

- ✅ Render the menu from data, never from a hardcoded list — `Sidebar` reads `MenuService.menu` directly, so a permission change or a session switch is reflected without a reload.
- ✅ Keep exactly one nesting level. `IMenuItem.Children` is built one level deep by contract, and `.sidebar-subitem` is the only sub-level style that exists.
- ✅ Keep `.shell-content { margin-left }` in step with the sidebar width — both read the same two tokens, which is what makes the collapse animation land without a jump (`app.scss:37-48`).
- ✅ Keep `id="sidebar"` on the `<aside>`. It is not decoration: it is the other half of the Topbar hamburger's `aria-controls`, and removing it breaks that pairing silently, with no visual change and no console warning.
- ✅ Hide the rail from print with the `no-print` class, not with a component-local `@media print` block. The project rule is one print rule in `styles.scss`; a second copy per component is how the app ended up with print behaviour defined in four files.
- ❌ Don't reach for `colors.border-strong` on the rail — its right edge is deliberately the faint `colors.line` (see `DESIGN.md` § Do's and Don'ts).
- ❌ Don't add a disabled nav-item style — inaccessible entries are omitted server-side, so a disabled state would never render.
- ❌ Don't give the drawer its own overlay color; it shares `colors.overlay-backdrop` with the native `<dialog>` backdrop.

## Normalize on redesign
1. ~~**`colors.overlay-backdrop` is written twice as a literal**~~ — **đã xử lý**, đối chiếu 2026-09-06. Token `--overlay-backdrop` nay được khai một lần ở `src/FE/src/styles.scss` § `--overlay-backdrop` và được cả hai chỗ đọc qua `var()`: `sidebar.scss:211` và `styles.scss` § `dialog::backdrop`. Giữ dòng này để lần sau đừng chép giá trị ra literal lần nữa.
2. **Two radius literals bypass the scale**: `7px` on the brand mark (`sidebar.scss:34`) is numerically `rounded.sm`, and `10px` on the collapsed-rail flyout (`sidebar.scss:295`) matches nothing. The first is a free swap; the flyout needs a decision. The `3px` active-rail cap (`sidebar.scss:130`) is a cap radius rather than a component radius and is catalogued separately.
3. **`.sidebar-navitem.active` sets `font-weight:700`** while `typography.sidebar-nav-item` is 600 — an un-tokenised fourth weight step on the same element.
4. **The collapsed rail hides labels with the hand-rolled clip-rect pattern in three separate blocks** (`sidebar.scss:171-182`, `:228-238`, `:306-317`), one of which exists only to undo another. A single `.visually-hidden` utility would collapse all three.
5. **No `prefers-reduced-motion` guard on this file's own transitions.** `styles.scss` now carries a global `@media (prefers-reduced-motion: reduce)` block with `!important`, which does reach the rail's width/transform and the toggle glyph rotation — so the gap is narrower than it was, but the component still declares motion with no local awareness of the setting.
6. **`.sidebar-toggle` is a class with two rules and a name that promises a component.** Now that shape comes from `.icon-btn`, the remaining `margin-left` and glyph rotation could sit on the `.sidebar-brand` and `.sidebar.collapsed` selectors directly, leaving no class at all.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **`.sidebar-toggle` was one of five hand-rolled icon buttons — resolved 2026-08-29.** It copied the whole `.icon-btn` recipe and had already drifted: a hard `7px` radius instead of `rounded.sm`, and no `:disabled` state at all. It now sets only `margin-left` and the glyph rotation. The class kept its name and lost its shape, and one of the three `7px` radius literals this spec used to list went with it.
2. **`aria-controls="sidebar"` pointed at nothing — resolved 2026-08-29** by adding `id="sidebar"` to the `<aside>`. The Topbar hamburger had declared the relationship since it was written; nothing on the page answered to that id, so assistive tech discarded it. Nothing about this is visible on screen, which is why it survived so long.
3. **Print behaviour was declared per component — resolved 2026-08-29.** `sidebar.scss` had its own `@media print` block hiding `.sidebar` and `.sidebar-backdrop`; `topbar.scss` had a matching one. Both are gone, replaced by the `no-print` class on the elements themselves and one global rule. The printed page is unchanged; there is now one place to look. The layout compensation that is *not* a hide — resetting `.shell-content`'s `margin-left`, since a `position:fixed` sidebar leaves the offset behind when it disappears — deliberately stays a real `@media print` rule in `styles.scss`, because `no-print` cannot express it.
