---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "Dialog"
sources: ["doc/Prototype/dashboard.html"]
---

# Dialog
**Description:** Native `<dialog>` modal (`dashboard.html:52,138-145`) used for the single "Báo cáo nhanh" (quick report) overlay — opened via `generateReport()` calling `reportDialog.showModal()` (`dashboard.html:917-926`), the only modal in the app.

## Anatomy
`dialog#reportDialog` (browser-native, centered, `::backdrop` dims the page) → `.title` row (`<h2>` + "Đóng" `Button`) → `#reportBox.report` (generated HTML content: computed stats + two criteria lists) → footer action row (`spacing.xs` gap) with "Sao chép" (secondary `Button`) and "In" (primary `Button`).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Report dialog | `dialog#reportDialog` | width `min(700px, 92vw)`, `rounded.dialog` (15px), `shadow.dialog` | The only dialog instance in the app — no other modal exists |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default (closed) | not rendered — native `<dialog>` without `open` is hidden |
| default (open) | centered via native `<dialog>` positioning, `::backdrop{background:colors.overlay-backdrop}` (`rgba(20,28,40,.45)`), `shadow.dialog` | 
| hover | **Not styled** — no `dialog:hover` rule; only its internal `Button`s have their own (absent) hover treatment |
| focus | **Browser default only** — native `<dialog>` `showModal()` traps focus per browser UA behavior; no custom `:focus` CSS is authored |
| active/disabled | **N/A** — the dialog element itself is not an interactive control (its child buttons are, see `Button.md`) |

## Tokens Used
- `colors.surface`, `colors.overlay-backdrop`
- `rounded.dialog`
- `shadow.dialog` (`Tokens/spacing.md` § Elevation)
- `spacing.xs` (footer button row gap)

## Reference markup

```html
<dialog id="reportDialog">
 <div class="title"><h2>Báo cáo nhanh tiến độ DTI</h2><button class="btn" onclick="reportDialog.close()">Đóng</button></div>
 <div id="reportBox" class="report"></div>
 <div style="display:flex;justify-content:flex-end;gap:8px;margin-top:12px">
  <button class="btn" onclick="copyReport()">Sao chép</button>
  <button class="btn primary" onclick="window.print()">In</button>
 </div>
</dialog>
```

Sources: `doc/Prototype/dashboard.html:52` (CSS), `doc/Prototype/dashboard.html:138-145` (markup), `doc/Prototype/dashboard.html:917-927` (`generateReport()`/`copyReport()` open/populate logic)

## Do / Don't

- ✅ Open via native `showModal()` (traps focus, renders `::backdrop`) — don't reimplement as a manually-positioned `div` overlay.
- ✅ Always available regardless of data state — `generateReport()` has no guard clause; even with zero saved periods it opens with the comparison text omitted.
- ❌ Don't add a second concurrent dialog — the shipped app has exactly one `<dialog>` element and never nests or stacks modals.
- ❌ Don't add a confirm-before-close step — "Đóng" calls `reportDialog.close()` directly, no unsaved-state concept applies (the dialog is read-only).

## Normalize on redesign
1. No explicit focus-visible styling on the internal action buttons while the dialog traps focus.
