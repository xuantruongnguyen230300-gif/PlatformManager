---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "SegmentedControl"
sources:
  - "src/FE/src/app/modules/dashboard/components/period-toolbar/period-toolbar.html"
  - "src/FE/src/app/modules/dashboard/components/period-toolbar/period-toolbar.scss"
  - "src/FE/src/app/modules/dashboard/components/period-toolbar/period-toolbar.ts"
  - "src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.ts"
---

# SegmentedControl
**Description:** The two-option exclusive view switcher (`.segmented` + `.seg-btn`) in the dashboard period toolbar — a genuine segmented control: one bordered `inline-flex` group with `overflow:hidden`, an internal hairline divider, and the chosen segment filled `colors.brand`. It selects **Tuần** or **Tháng** for the whole dashboard.

> **The app's only view switcher.** One instance ships, on `/dashboard` (`period-toolbar.html:36-48`). A second switcher spec (`TabBar` — a bare flex row of two `.btn`s, claimed for `/quan-tri/phan-quyen`) was deleted on 2026-08-23: neither its class nor its markup has ever existed in `src/FE/`, so the "two controls, one job" inconsistency this file used to record was never real. Any new exclusive view switcher composes from **this** control.

## Anatomy

`.segmented` (`role="group"`, `aria-label`) → `[.seg-btn] [.seg-btn]`, no wrapper between them.

- **`.segmented`** — `display: inline-flex`, `border: 1px solid colors.line`, `border-radius: rounded.sm`, `overflow: hidden`, `flex: none`. The `overflow:hidden` is load-bearing: the buttons declare **no** radius of their own, so clipping is the only thing that rounds the group's outer corners.
- **`.seg-btn`** — `border: 0`, `background: colors.card`, `padding: spacing.sp-2 spacing.sp-4`, `font-weight: 700`, `font-size` `--fs-sm` (together = `typography.button-label`), `color: colors.muted`, `cursor: pointer`, `transition: background 0.15s ease, color 0.15s ease`.
- **Divider** — `.seg-btn + .seg-btn { border-left: 1px solid colors.line }`. The adjacent-sibling form means the first segment never gets a leading rule; a third segment would inherit the divider automatically.
- **Selection** — `.seg-btn.active { background: colors.brand; color: colors.on-primary }`, bound with `[class.active]="viewMode() === 'week'"` / `=== 'month'`. Exactly one segment carries it, because `viewMode` is a single `DashboardViewMode` value.

The group is the fourth item inside `.weekbar` (`Card` + `no-print`), between the period `select`s and the `.weekbar-actions` cluster, and inherits the toolbar's `gap: spacing.sp-3` and `align-items: center`.

**Behaviour.** The component is dumb: clicking a segment emits `viewModeChange`, and the host page's `onViewModeChange` sets the `viewMode` signal and clears both period selections, after which the existing `effect()` refetches. There is no local state, no toggle-off (clicking the already-selected segment re-emits the same value) and no loading gate — the group stays interactive during the refetch.

**Print.** The group is hidden on paper: its `.weekbar` parent carries `no-print`, and `styles.scss` sets `.no-print { display: none !important }` in its `@media print` block.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Two-option group | `segmented` | `inline-flex`, border `colors.line`, `rounded.sm`, `overflow:hidden`, `flex:none`; `role="group"` + `aria-label` | The dashboard period toolbar — the app's only instance |
| Segment, resting | `seg-btn` | fill `colors.card`, text `colors.muted`, `typography.button-label`, padding `spacing.sp-2 spacing.sp-4`, no radius, no border (except the sibling divider) | The mode that is **not** in effect |
| Segment, selected | `seg-btn active` | fill `colors.brand`, text `colors.on-primary`; everything else unchanged | The mode currently driving the dashboard |

**No responsive variant of its own** — but at `breakpoint.mobile` (≤560px) the toolbar's `.weekbar > * { flex: 1 }` overrides `.segmented { flex: none }` (equal specificity, later source order), so the group stretches to share the row instead of hugging its labels. That override is the toolbar's, not this component's.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Resting segment: `background: colors.card`, `color: colors.muted`, `border: 0`, padding `spacing.sp-2 spacing.sp-4`, `typography.button-label`; group border `colors.line` at `rounded.sm` |
| hover | Resting segment: `background: colors.bg` (text colour unchanged), eased over `0.15s`. **The selected segment does not change on hover** — `.seg-btn.active` and `.seg-btn:hover` are both specificity (0,2,0) and `.active` is declared later, so the brand fill wins |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: **-2px**` — an **inset** ring, unique in the app (`.btn` uses `+2px`; `.action-btn`, filter fields and toolbar `select`s use `+1px`). Inset is required here because the group's `overflow:hidden` would clip an outward ring |
| active / selected | **Selected** is real and is the control's whole point: `.active` → `colors.brand` fill with `colors.on-primary` text, bound to an equality test against the `viewMode` signal. **Pressed is not applicable** — `period-toolbar.scss` authors no `.seg-btn:active` rule, so unlike `.btn` (which nudges `translateY(1px)`) the segment gives no press feedback; the fill change on release is the only confirmation |
| disabled | **Not applicable — a segment is never disabled.** Neither button binds `[disabled]`, `period-toolbar.scss` authors no `:disabled` rule, and the global `.btn:disabled` rule cannot reach a different class. Both modes are always selectable by design: switching is a pure signal write in the host with no precondition, and the group stays live while the dashboard refetches |

## Tokens Used
- `colors.card` (resting fill), `colors.bg` (hover fill), `colors.muted` (resting text), `colors.brand` (selected fill + focus ring), `colors.on-primary` (selected text), `colors.line` (group border + divider)
- `rounded.sm` (group only — the segments have no radius)
- `spacing.sp-2` / `spacing.sp-4` (segment padding), `spacing.sp-3` (gap, inherited from `.weekbar`)
- `typography.button-label` (12px / 700) — shipped as `font-size: var(--fs-sm)` + a literal `font-weight: 700`; weights are not tokenised (`Tokens/typography.md`)
- Motion: `0.15s ease` on `background` and `color` — no motion token exists (`Tokens/spacing.md` § Motion)
- Icons: none — both segments are text-only

> **Doc drift to resolve elsewhere, recorded not fixed here.** `DESIGN.md` § `components` declares `segmented-button-active` / `segmented-button-rest` with `typography: "{typography.table-cell}"` (12px / **400**) and `padding: "{spacing.button-padding}"` (`6px 8px`), while the shipped `.seg-btn` is 12px / **700** at `var(--sp-2) var(--sp-4)` (`6px 10px`). The live source is correct; the `DESIGN.md` entries under-state the weight and the horizontal padding. Not corrected in this pass — `DESIGN.md` is out of scope for stage 4 and must be re-linted when it changes.

## Reference markup

```html
<div class="segmented" role="group" aria-label="Chế độ xem theo Tuần hoặc Tháng">
  <button type="button" class="seg-btn" [class.active]="viewMode() === 'week'" (click)="viewModeChange.emit('week')">
    Tuần
  </button>
  <button type="button" class="seg-btn" [class.active]="viewMode() === 'month'" (click)="viewModeChange.emit('month')">
    Tháng
  </button>
</div>
```

```scss
.segmented {
  display: inline-flex;
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  overflow: hidden;
  flex: none;
}

.seg-btn {
  border: 0;
  background: var(--card);
  padding: var(--sp-2) var(--sp-4);
  font-weight: 700;
  font-size: var(--fs-sm);
  color: var(--muted);
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;

  & + .seg-btn { border-left: 1px solid var(--line); }
  &:hover { background: var(--bg); }
  &:focus-visible { outline: 2px solid var(--brand); outline-offset: -2px; }
  &.active { background: var(--brand); color: var(--on-primary); }
}
```

Verbatim copy: segment labels `Tuần` and `Tháng`; group `aria-label` `Chế độ xem theo Tuần hoặc Tháng`. All hardcoded Vietnamese in the template — there is no i18n layer.

Sources: `src/FE/src/app/modules/dashboard/components/period-toolbar/period-toolbar.html:36-48`, `period-toolbar.scss:38-44` (`.segmented`), `:46-73` (`.seg-btn` and all four nested rules), `:1-7` (`.weekbar` context), `:75-84` (the ≤560px `flex:1` override), `period-toolbar.ts:35` (`viewMode` input), `:45` (`viewModeChange` output), `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.ts:121-125` (`onViewModeChange` — sets the signal and clears both period selections), `:108-118` (the `effect()` that refetches), `src/FE/src/styles.scss:118-126` (`.no-print`), `src/FE/src/styles.scss` — `--card`, `--bg`, `--muted`, `--brand`, `--on-primary`, `--line`, `--fs-sm`, `--sp-2`, `--sp-4`, `--radius-sm` in `:root`

## Do / Don't

- ✅ Bind `[class.active]` to an equality test against the state signal, exactly as shipped — the class is derived, never toggled by hand, so two segments can never both look selected.
- ✅ Keep `overflow: hidden` on the group and no radius on the segments; that pairing is what makes the outer corners round and the inner joint square.
- ✅ Keep the focus ring inset (`outline-offset: -2px`). A positive offset would be clipped by the group's own `overflow:hidden` and the ring would partly vanish.
- ✅ Use this control for a **mutually exclusive view mode** with 2–3 short text labels, where both options must stay readable at once.
- ❌ Don't use it for an action — a segment sets state; the toolbar's "Xuất báo cáo" is a `Button` for exactly that reason.
- ❌ Don't add per-segment borders or radii to fake separation — the `.seg-btn + .seg-btn` divider already does it with one hairline.
- ❌ Don't build a new switcher out of `.btn` + `.primary`. A selected state that is pixel-identical to a primary action button is the reason this dedicated control exists; reintroducing that shortcut splits one job across two mechanisms.

## Normalize on redesign
1. **The ARIA is present but incomplete.** `role="group"` + `aria-label` is the dashboard module's only ARIA pair, yet neither segment carries `aria-pressed` or `aria-checked`, so a screen-reader user hears two ordinary buttons and is never told which mode is in effect. Either `role="radiogroup"` with `role="radio"` + `aria-checked`, or plain buttons with `aria-pressed`.
2. **Selection is signalled by colour alone** — a solid brand fill against white. No weight change, underline, icon or inset shadow, so the state is invisible in monochrome and marginal under colour-vision deficiency.
3. **No press feedback.** `.btn` presses `translateY(1px)`; `.seg-btn` has no `:active` rule, so the app's two button families feel different under the pointer.
4. **The one inset focus ring in the app.** `outline-offset: -2px` here versus `+1px`/`+2px` everywhere else — a consequence of `overflow:hidden`, not a decision. Rounding each end segment individually instead of clipping would let the ring match the rest of the app.
5. **`flex: none` is silently defeated at `breakpoint.mobile`.** `.weekbar > * { flex: 1 }` wins on source order at equal specificity, so the group stretches on mobile. The result is reasonable, but it is an accidental override rather than a declared responsive intent — state it explicitly in whichever rule should own it.
