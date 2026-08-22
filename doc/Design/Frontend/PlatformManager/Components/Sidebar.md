---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Sidebar"
sources:
  - "src/FE/src/app/shared/components/sidebar/sidebar.html"
  - "src/FE/src/app/shared/components/sidebar/sidebar.scss"
  - "src/FE/src/app/shared/components/sidebar/sidebar.ts"
  - "src/FE/src/app/shared/services/sidebar-state.service.ts"
  - "src/FE/src/app/app.html"
---

# Sidebar
**Description:** The app shell's fixed left navigation rail (`<app-sidebar />`, `.sidebar`) — brand block, a menu tree loaded at runtime from `GET /api/meta/menu`, a collapse toggle, and an off-canvas drawer backdrop for narrow viewports. Present on all four authenticated routes; deliberately absent on the two auth routes (see `AuthCard.md`).

## Anatomy

`<aside class="sidebar">` — `position:fixed`, top/left 0, `height:100vh` immediately overridden by `100dvh`, width `spacing.sidebar-w`, background `colors.card`, right border 1px `colors.line`, `z-index:35`, column flex, `transition: width 0.2s ease, transform 0.25s ease`. Three regions stack inside it:

1. **`.sidebar-brand`** — row flex, gap `spacing.sp-3`, padding `spacing.sp-4`, bottom border 1px `colors.line`, `min-height` `spacing.sidebar-brand-height`, `flex:none`. Contains:
   - `.brand-mark` — `spacing.brand-mark-sidebar` square, radius `7px` literal (numerically `rounded.sm`), fill `colors.brand`, text `colors.on-primary`, weight 800, `typography.muted-caption` size. Content is the two-letter string `PM`; there is **no image asset** — the mark is typographic.
   - `.brand-text` — `typography.sidebar-brand-text`, `colors.text`, `white-space:nowrap` + `text-overflow:ellipsis`.
   - `.sidebar-toggle` — `spacing.sidebar-toggle` square ghost button pushed right by `margin-left:auto`, radius `7px` literal, `colors.muted`, containing `pi pi-angle-left`; the icon gets `transform:rotate(180deg)` while `.sidebar.collapsed`.
2. **`<nav aria-label="Main"> > ul.sidebar-nav`** — `list-style:none`, padding `spacing.sp-2`, `overflow-y:auto`. One `<li>` per top-level menu item; a `<li>` with children carries `.sidebar-navgroup` and, when open, `.open`.
3. **`.sidebar-backdrop`** — sibling of `<aside>`, `position:fixed; inset:0`, fill `colors.overlay-backdrop`, `z-index:34`, `display:none` until `.show`. `aria-hidden="true"` on purpose: it is a decorative dismiss overlay, not a tab stop.

**Nav item** (`.sidebar-navitem`) is the one repeated atom: row flex, gap `spacing.sp-3`, padding `spacing.sp-2` `spacing.sp-3`, radius `rounded.md`, `colors.text`, `typography.sidebar-nav-item`, `width:100%`, `background:none`, `border:0`, `text-align:left`. It renders as `<a routerLink>` for a destination and as `<button>` for a group parent — same class, same box. Inside: `.navicon` (`spacing.nav-icon` square, `colors.muted`, `font-size:15px` literal) → `.sidebar-navlabel` → (group parent only) `.navchevron` (`pi pi-chevron-down`, `margin-left:auto`, `colors.muted`, `font-size:12px` literal, `transform:rotate(-90deg)` while the group is closed).

Submenu: `ul.sidebar-submenu`, `margin:2px 0 4px`, `padding:0`; its items add `.sidebar-subitem` (`padding-left:38px` literal). A closed group hides the whole `<ul>` with `display:none`.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Expanded rail (default) | `.sidebar` | width `spacing.sidebar-w`; `.shell-content` offset by a matching `margin-left` | ≥ `spacing.breakpoint-desktop`, collapse flag off |
| Collapsed rail | `.sidebar.collapsed` | width `spacing.sidebar-w-collapsed`; `.sidebar-navlabel` visually hidden via the clip-rect pattern; nav items centred with padding `spacing.sp-2`; `.brand-text` hidden; brand padding `spacing.sp-4` `spacing.sp-1`; active rail moves to `left:0` | User pressed the toggle; the flag persists in `localStorage` (`platform_manager_sidebar_collapsed_v1`) |
| Collapsed flyout submenu | `.sidebar.collapsed .sidebar-navgroup:hover/:focus-within .sidebar-submenu` | absolute at `left:100%`, `min-width` `spacing.sidebar-flyout-width`, margin-left 6px, padding 6px, fill `colors.card`, border `colors.line`, radius `10px` literal, `colors.shadow`, `z-index:40`; sub-items regain padding `10px 12px` and their labels un-hide | Only inside the single `min-width:981px` media block |
| Off-canvas drawer (tablet) | `.sidebar` @ `spacing.breakpoint-tablet` | width `spacing.sidebar-w-drawer-tablet`, `transform:translateX(-100%)`, `colors.shadow`; `.collapsed` is neutralised (labels and brand text return) | ≤980px — opened by the Topbar hamburger |
| Off-canvas drawer (mobile) | `.sidebar` @ `spacing.breakpoint-mobile` | width `spacing.sidebar-w-drawer-mobile`; nav item padding `10px`, `min-height` `spacing.sidebar-navitem-height-mobile` | ≤560px |
| Drawer open | `.sidebar.drawer-open` | `transform:translateX(0)` + `.sidebar-backdrop.show` | While `mobileOpen()` is true |
| Leaf nav item | `a.sidebar-navitem` | `routerLinkActive="active"`, `[title]` = the item label | Menu item with a route and no children |
| Group parent | `button.sidebar-navitem.sidebar-navparent` | `aria-expanded`, `aria-controls="submenu-<id>"`, chevron; `.active` applied by `isGroupActive()` when the current URL starts with any child route | Menu item with children (one nesting level only) |
| Sub item | `a.sidebar-navitem.sidebar-subitem` | `padding-left:38px` literal | Child of a group |
| Print | `@media print` | `.sidebar` and `.sidebar-backdrop` both `display:none !important` | Any print output |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: `.sidebar-navitem`.

| State | Treatment |
| --- | --- |
| default | `background:none`, `colors.text`, `typography.sidebar-nav-item`, radius `rounded.md`; `.navicon` in `colors.muted` |
| hover | `background: colors.bg` (`sidebar.scss:127-129`) |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 2px` (`sidebar.scss:152-155`) |
| active / selected | `.active` — background `colors.surface-nav-active`, text `colors.brand`, `font-weight:700` (literal, above the 600 in `typography.sidebar-nav-item`), `.navicon` recoloured to `colors.brand`, plus the `::before` rail `spacing.active-nav-rail` (3px wide, `colors.brand`, radius `0 3px 3px 0`, inset 5px top/bottom, `left:-8px`; `left:0` in the collapsed rail). Applied by `routerLinkActive` for leaves and by `isGroupActive()` for parents |
| disabled | **Not applicable — no disabled affordance exists, by design.** `sidebar.scss` contains no `:disabled` rule and no template binding sets `disabled`/`aria-disabled`. Menu entries a user may not open are never rendered at all: the BE filters `SysMenuRole` by the caller's roles before returning the menu, and `Sidebar` reads that list straight through without re-filtering (`sidebar.ts:41-46`) |

### States — `.sidebar-toggle` (supplementary)

| State | Treatment |
| --- | --- |
| default | `border:1px solid transparent`, `background:transparent`, `colors.muted`, radius `7px` literal, `spacing.sidebar-toggle` square |
| hover | `background: colors.surface-2` (`sidebar.scss:73-75`) |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 2px` (`sidebar.scss:77-80`) |
| active / selected | No `:active` rule; the *pressed* semantic is carried by state instead — `aria-expanded` flips and the chevron rotates 180° (`sidebar.scss:83-85`) |
| disabled | **Not applicable** — the toggle is always operable; no `:disabled` rule and no `[disabled]` binding exist |

## Tokens Used
- `colors.card`, `colors.line`, `colors.bg`, `colors.text`, `colors.muted`, `colors.brand`, `colors.on-primary`, `colors.surface-2`, `colors.surface-nav-active`, `colors.overlay-backdrop`, `colors.shadow`
- `rounded.md` (nav item); `7px` and `10px` radius literals are un-tokenised — catalogued in `Tokens/spacing.md` § Radius literals bypassing the scale
- `spacing.sp-1`, `spacing.sp-2`, `spacing.sp-3`, `spacing.sp-4`
- `spacing.sidebar-w`, `spacing.sidebar-w-collapsed`, `spacing.sidebar-w-drawer-tablet`, `spacing.sidebar-w-drawer-mobile`, `spacing.sidebar-brand-height`, `spacing.sidebar-navitem-height-mobile`, `spacing.sidebar-flyout-width`, `spacing.sidebar-toggle`, `spacing.nav-icon`, `spacing.brand-mark-sidebar`, `spacing.active-nav-rail`
- `spacing.breakpoint-tablet`, `spacing.breakpoint-mobile`, `spacing.breakpoint-desktop`, `spacing.breakpoint-print`
- `typography.sidebar-brand-text`, `typography.sidebar-nav-item`, `typography.muted-caption`
- Icons: PrimeIcons v7 — `pi-angle-left` (toggle), `pi-chevron-down` (group chevron); per-item icons arrive from the API as literal `pi-*` class strings, with `pi-circle` as the only FE fallback (`sidebar.ts:12`, `:37-39`)

## Reference markup

```html
<aside class="sidebar" [class.collapsed]="state.collapsed()" [class.drawer-open]="state.mobileOpen()">
  <div class="sidebar-brand">
    <span class="brand-mark">PM</span>
    <span class="brand-text">PlatformManager</span>
    <button type="button" class="sidebar-toggle no-print"
      [attr.aria-label]="state.collapsed() ? 'Mở rộng menu' : 'Thu gọn menu'"
      [attr.aria-expanded]="!state.collapsed()" (click)="state.toggleCollapse()">
      <i class="pi pi-angle-left"></i>
    </button>
  </div>
  <nav aria-label="Main">
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

Shipped menu tree (seeded server-side, verbatim labels + icons): `Dashboard` (`pi-th-large`, `/dashboard`) · `Danh mục` (`pi-folder`, group) → `DTI` (`pi-list`, `/danh-muc/dti`) · `Quản trị hệ thống` (`pi-cog`, group) → `Người dùng` (`pi-user`, `/quan-tri/nguoi-dung`), `Phân quyền` (`pi-shield`, `/quan-tri/phan-quyen`).

Sources: `src/FE/src/app/shared/components/sidebar/sidebar.html:1-77`, `src/FE/src/app/shared/components/sidebar/sidebar.scss:3-343`, `src/FE/src/app/shared/components/sidebar/sidebar.ts:12`, `:32-95`, `src/FE/src/app/shared/services/sidebar-state.service.ts:4`, `:14-57`, `src/FE/src/app/app.html:3`, `src/FE/src/app/app.scss:11-22` (the matching `.shell-content` offset), `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs:121-126` (seeded labels/icons/routes)

## Do / Don't

- ✅ Render the menu from data, never from a hardcoded list — `sidebar.ts:46` reads `MenuService.menu` directly so a permission change or a session switch is reflected without a reload.
- ✅ Keep exactly one nesting level. `IMenuItem.Children` is built one level deep by contract, and `.sidebar-subitem` is the only sub-level style that exists.
- ✅ Keep `.shell-content { margin-left }` in step with the sidebar width — both read the same two tokens, which is what makes the collapse animation land without a jump (`app.scss:11-22`).
- ✅ Mark the toggle and the backdrop `no-print`; the whole rail is hidden in `@media print`.
- ❌ Don't reach for `colors.border-strong` on the rail — its right edge is deliberately the faint `colors.line` (see `DESIGN.md` § Do's and Don'ts).
- ❌ Don't add a disabled nav-item style — inaccessible entries are omitted server-side, so a disabled state would never render.
- ❌ Don't give the drawer its own overlay color; it shares `colors.overlay-backdrop` with the native `<dialog>` backdrop.

## Normalize on redesign
1. **`colors.overlay-backdrop` is written twice as a literal** — `sidebar.scss:228` and `styles.scss:474` — with no shared custom property. Promote to `:root` (also logged in `Tokens/colors.md` § Normalize on redesign #3).
2. **Three radius literals bypass the scale**: `7px` (`sidebar.scss:34`, `:59`) is numerically `rounded.sm`, `3px` is the active rail cap (`:147`), and `10px` (`:312`) is the flyout. The first two are free swaps; the flyout needs a decision.
3. **`.sidebar-navitem.active` sets `font-weight:700`** while `typography.sidebar-nav-item` is 600 — an un-tokenised fourth weight step on the same element.
4. **The collapsed rail hides labels with the hand-rolled clip-rect pattern in three separate blocks** (`sidebar.scss:189-199`, `:245-255`, `:323-334`), one of which exists only to undo another. A single `.visually-hidden` utility would collapse all three.
5. **No `prefers-reduced-motion` guard** on the width/transform transitions (project-wide gap, see `Tokens/spacing.md` § Normalize on redesign #5).
