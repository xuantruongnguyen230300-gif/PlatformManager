---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "Fab"
sources: ["doc/Prototype/dashboard.html"]
---

# Fab
**Description:** Mobile-only floating action button (`.fab`, `dashboard.html:51,136`) fixed to the bottom-right corner, calling the exact same `saveWeek()` handler as the topbar's "Lưu tuần này" primary `Button` — a duplicate entry point for the same action, not a separate feature.

## Anatomy
Single-line text label ("Lưu tuần"), pill shape, fixed position, always on top (`z-index:30`).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Fab | `fab` | bg `colors.primary`, text `colors.on-primary`, `rounded.pill`, `shadow.fab`, `position:fixed;right:18px;bottom:18px` | Only variant; visible exclusively below `breakpoint.tablet` (980px) via `@media(max-width:980px){.fab{display:block}}` — hidden by default (`display:none`) above that width |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg `colors.primary`, text `colors.on-primary`, `font-weight:800`, `padding:13px 17px`, `shadow.fab` |
| hover | **Not styled** — no `.fab:hover` rule |
| focus | **Not styled** — no `.fab:focus` rule; browser UA default focus ring only |
| active | **Not styled** — the shared `.btn:active` rule does **not** apply (`.fab` is a distinct class, not `.btn`); no press feedback exists |
| disabled | **Unreachable** — no `disabled` attribute ever set on the `<button class="fab">` |

## Tokens Used
- `colors.primary`, `colors.on-primary`
- `rounded.pill`
- `shadow.fab` (`Tokens/spacing.md` § Elevation)
- `spacing.fab-offset` (18px right/bottom offset)

## Reference markup

```html
<button class="fab" onclick="saveWeek()">Lưu tuần</button>
```

Sources: `doc/Prototype/dashboard.html:51` (CSS), `doc/Prototype/dashboard.html:55` (`@media(max-width:980px)` visibility rule), `doc/Prototype/dashboard.html:136` (markup)

## Do / Don't

- ✅ Keep the Fab wired to the exact same handler as the topbar primary button (`saveWeek()`) — it is a positional duplicate, not a distinct action.
- ✅ Show it only below the tablet breakpoint (980px) — above that, the topbar's "Lưu tuần này" is always reachable and the Fab would be redundant screen clutter.
- ❌ Don't give the Fab a different label/action than the primary save button — the shipped app deliberately keeps them identical.

## Normalize on redesign
1. No press/active feedback (unlike `.btn:active`) — tapping gives no visual confirmation until the `alert()` fires.
