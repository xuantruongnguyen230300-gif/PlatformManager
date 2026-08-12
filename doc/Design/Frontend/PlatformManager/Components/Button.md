---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "Button"
sources: ["doc/Prototype/dashboard.html"]
---

# Button
**Description:** Clickable action trigger built on the `.btn` class (`dashboard.html:22-24`); also applied to a `<label>` wrapping a hidden file input ("Khôi phục") so it looks like a button while triggering native file selection.

## Anatomy
Single-line text label, no icon system in the app (see `COMPONENTS.md` § General conventions). Rounded rectangle: `radius rounded.button` (10px), `padding spacing.sm-btn` (9px 12px), font-weight 700, cursor pointer. No fixed width — sizes to label content.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Default (secondary) | `btn` | bg `colors.surface` (`#fff`), border 1px `#dfe6ef`, text `colors.text` | Secondary actions: "Tạo tuần mới từ kỳ gần nhất", "Báo cáo nhanh", "Sao chép", dialog "Đóng", history row "Xem" |
| Primary | `btn primary` | bg `colors.primary` (`#0f5bd7`), border-color same, text `colors.on-primary` (`#fff`) | The one primary action per context: topbar "Lưu tuần này", dialog "In" |
| Desktop-only modifier | `btn desktop` | same visual as Default; hidden via `.actions .desktop{display:none}` below `breakpoint.tablet` (980px) | "Sao lưu" button and "Khôi phục" label — no mobile equivalent exists (see `UiInventory.md` § Normalize on Redesign #2) |
| Danger (CSS-only, unused) | `btn danger` | text color `colors.danger` (`#c83c3c`), no background change | Declared in CSS (`dashboard.html:24`) but **not applied to any element** in the shipped markup — see `COMPONENTS.md` § Known inconsistencies #1 |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg/border/text per variant above; `font-weight:700`; `cursor:pointer` |
| hover | **Not styled** — no `.btn:hover` rule exists anywhere in the stylesheet; renders with browser UA default (no visual change) |
| focus | **Not styled** — no `.btn:focus`/`:focus-visible` rule exists; renders with browser UA default focus ring |
| active | `transform:translateY(1px)` — the **only** custom interactive-state rule in the entire stylesheet (`.btn:active`, `dashboard.html:24`) |
| disabled | **Unreachable** — no `disabled` attribute is ever set on any `<button>`/`<label class="btn">` in the markup, and no `:disabled` CSS rule exists |

## Tokens Used
- `colors.surface`, `colors.primary`, `colors.on-primary`, `colors.text`, `colors.danger`
- `rounded.button`
- `spacing.sm-btn`
- `typography.body` (font-family/size inherited; weight overridden to 700 per variant)

## Reference markup

```html
<button class="btn desktop" onclick="exportBackup()">Sao lưu</button>
<label class="btn desktop" style="cursor:pointer">Khôi phục<input id="restoreFile" type="file" accept=".json" hidden onchange="restoreBackup(this.files[0])"></label>
<button class="btn primary" onclick="saveWeek()">Lưu tuần này</button>
<button class="btn" onclick="newFromLatest()">Tạo tuần mới từ kỳ gần nhất</button>
<button class="btn" onclick="generateReport()">Báo cáo nhanh</button>
```

Sources: `doc/Prototype/dashboard.html:22-24` (CSS), `doc/Prototype/dashboard.html:65-67,82-83,139,142-143,901` (markup instances)

## Do / Don't

- ✅ One `btn primary` per screen context (topbar vs. dialog are separate contexts, each with its own primary).
- ✅ Use `btn desktop` only for actions that are genuinely desktop-only per the shipped app ("Sao lưu"/"Khôi phục") — don't apply it to new buttons without a documented mobile fallback.
- ❌ Don't apply `btn danger` to a live element without first deciding it's the intended target (see Known inconsistency #1) — it is currently dead CSS.
- ❌ Don't invent a hover/focus treatment not in the States table above; the as-shipped app has none.

## Normalize on redesign
1. Add explicit `:hover`/`:focus-visible` treatments — currently indistinguishable from a static label until clicked.
2. Resolve the unused `.btn.danger` variant (wire it up or remove it).
3. Give "Sao lưu"/"Khôi phục" a mobile-reachable path instead of vanishing below 980px.
