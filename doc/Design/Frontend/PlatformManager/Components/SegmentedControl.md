---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
component: "SegmentedControl"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html"
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.scss"
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# SegmentedControl
**Description:** The exclusive view switcher (`.segmented` + `.seg-btn`, `src/FE/src/styles.scss` § 3.3) — one bordered `inline-flex` group with `overflow: hidden`, a hairline divider between segments, and the chosen segment filled `colors.brand`. The 2026-08-29 redesign **promoted it from a screen-local rule to the global library** and gave it a `:disabled` state.

> **Shipped call site — corrected 2026-08-29.** The previous revision of this spec carried a warning that the control had **no shipped call site**. That is wrong: `/quan-tri/phan-quyen` renders `.segmented` with two `.seg-btn` children as its two-mode view switcher (`phan-quyen.page.html:7-46`), and `Screens/04-phan-quyen.md` composes this spec for it. The warning was true only of the dashboard mock that went with the business module removed the same day; it survived the module and outlived its subject. Verify rather than trust this paragraph: `grep -rn "seg-btn" src/FE/src/app`.

> **Citation policy.** Values cite `src/FE/src/styles.scss` plus the **selector name**, not a line number.

## Anatomy

`.segmented` (`role="group"` + `aria-label`) wrapping two or more `.seg-btn` children, with no element between them. The shipped instance adds `aria-pressed` to each segment and a page-local `.tabs` class that contributes **only** a bottom margin — it re-declares nothing about the switcher's shape (`phan-quyen.page.html:7-46`, `phan-quyen.page.scss:13-15`).

- **`.segmented`** — `display: inline-flex`, `border: 1px solid colors.line`, radius `rounded.sm`, `overflow: hidden`, `flex: none`. The `overflow: hidden` is load-bearing: the segments declare no radius of their own, so clipping is the only thing rounding the group's outer corners.
- **`.seg-btn`** — `border: 0`, fill `colors.card`, padding `spacing.sp-2` / `spacing.sp-4`, `typography.segmented-label`, ink `colors.muted`, `cursor: pointer`, transition on background and colour at `duration.fast`.
- **Divider** — `.seg-btn + .seg-btn { border-left: 1px solid colors.line }`. The adjacent-sibling form means the first segment never gets a leading rule, and a third segment inherits the divider automatically. The prototype exercises a three-segment group for exactly that reason.
- **Selection** — `.seg-btn.active` fills `colors.brand` with `colors.on-primary` ink. Exactly one segment carries it, because the class is derived from an equality test against a single state value.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Group | `segmented` | `inline-flex`, border `colors.line`, `rounded.sm`, `overflow: hidden`, `flex: none`; `role="group"` + `aria-label` | Any mutually exclusive view mode with 2–3 short text labels |
| Segment, resting | `seg-btn` | fill `colors.card`, ink `colors.muted`, `typography.segmented-label`, padding `spacing.sp-2` / `spacing.sp-4`, no radius, no border except the sibling divider | The mode that is **not** in effect |
| Segment, selected | `seg-btn active` | fill `colors.brand`, ink `colors.on-primary`; everything else unchanged | The mode currently in effect. Works in any position — the prototype shows it as the middle of three |
| Segment, unavailable | `seg-btn[disabled]` | `opacity: .5`, `cursor: not-allowed` | A mode that exists but cannot be chosen yet. **No shipped call site** — the prototype's example is a reporting period not yet open |
| Segment carrying a dirty marker | `seg-btn` + a nested `<span class="unsaved">` | `font-weight: 400`, `opacity: .85`, inheriting `currentColor` so it survives both the brand-filled active segment and the card-filled resting one | A segment whose pane has unsaved edits. Shipped on `/quan-tri/phan-quyen` as the literal text ` • chưa lưu` (`phan-quyen.page.html:23-25,42-44`, `phan-quyen.page.scss:20-23`) |

The group has **no responsive variant of its own**. Inside a `.toolbar` at `breakpoint.mobile` the toolbar's own `> * { flex: 1 }` beats `.segmented { flex: none }` on source order at equal specificity, so the group stretches to share the row. That override belongs to the toolbar, not to this component.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Resting segment: fill `colors.card`, ink `colors.muted`, `border: 0`, padding `spacing.sp-2` / `spacing.sp-4`, `typography.segmented-label`; group border `colors.line` at `rounded.sm` |
| hover | Resting segment: fill `colors.bg`, ink unchanged, eased over `duration.fast`. **The selected segment does not change on hover** — `.seg-btn.active` and `.seg-btn:hover` are both specificity (0,2,0) and `.active` is declared later, so the brand fill wins |
| focus | `outline: 2px solid colors.brand`, `outline-offset: **-2px**` — an **inset** ring, unique in the app (`.btn` uses `+2px`, `.icon-btn` uses `+1px`). Inset is required because the group's `overflow: hidden` would clip an outward ring |
| active | **Selected** is the control's whole point: `.active` → `colors.brand` fill with `colors.on-primary` ink, derived from an equality test against the state value. **Pressed is not styled** — no `.seg-btn:active` rule exists, so unlike `.btn` the segment gives no press feedback; the fill change on release is the only confirmation |
| disabled | `opacity: .5`, `cursor: not-allowed` — added 2026-08-29, matching `.btn:disabled` exactly so the two button families read the same when unavailable |

## Tokens Used
- `colors.card` (resting fill), `colors.bg` (hover fill), `colors.muted` (resting ink), `colors.brand` (selected fill and focus ring), `colors.on-primary` (selected ink), `colors.line` (group border and divider)
- `rounded.sm` — on the group only; the segments have no radius
- `spacing.sp-2`, `spacing.sp-4` (segment padding)
- `typography.segmented-label`
- `duration.fast` (background, colour)
- Icons: none — segments are text-only

## Reference markup

```html
<!-- SHIPPED: /quan-tri/phan-quyen, two segments with aria-pressed and a dirty marker -->
<div class="segmented tabs" role="group" aria-label="Chọn loại phân quyền">
  <button type="button" class="seg-btn"
    [class.active]="tab() === 'menu'" [attr.aria-pressed]="tab() === 'menu'"
    (click)="onSelectTab('menu')">
    Theo màn hình
    @if (dirty()) { <span class="unsaved"> • chưa lưu</span> }
  </button>
  <button type="button" class="seg-btn"
    [class.active]="tab() === 'resource'" [attr.aria-pressed]="tab() === 'resource'"
    (click)="onSelectTab('resource')">
    Theo tài nguyên
    @if (resourceDirty()) { <span class="unsaved"> • chưa lưu</span> }
  </button>
</div>

<!-- library shape: two segments, first selected -->
<div class="segmented" role="group" aria-label="Chế độ xem theo Tuần hoặc Tháng">
  <button type="button" class="seg-btn active">Tuần</button>
  <button type="button" class="seg-btn">Tháng</button>
</div>

<!-- three segments, selection in the middle -->
<div class="segmented" role="group" aria-label="Phạm vi kỳ báo cáo">
  <button type="button" class="seg-btn">Quý</button>
  <button type="button" class="seg-btn active">6 tháng</button>
  <button type="button" class="seg-btn">Năm</button>
</div>

<!-- an unavailable mode -->
<div class="segmented" role="group" aria-label="Kỳ chưa mở">
  <button type="button" class="seg-btn active">Quý III</button>
  <button type="button" class="seg-btn" disabled>Quý IV</button>
</div>
```

Sources: `src/FE/src/styles.scss` (§ 3.3 `.segmented`, `.seg-btn`, `.seg-btn + .seg-btn`, `.seg-btn:hover`, `.seg-btn:focus-visible`, `.seg-btn.active`, `.seg-btn:disabled`), `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:7-46` (the shipped call site), `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.scss:13-15,20-23` (the page-local margin and dirty marker), `doc/Design/Frontend/PlatformManager/Prototypes/index.html` (§ "3 · Nhóm nút phân đoạn" — the approved variant and state set)

## Do / Don't

- ✅ Derive `.active` from an equality test against the state value, never toggle it by hand — that is what makes it impossible for two segments to look selected at once.
- ✅ Keep `overflow: hidden` on the group and no radius on the segments. That pairing is what rounds the outer corners while leaving the inner joint square.
- ✅ Keep the focus ring inset (`outline-offset: -2px`). A positive offset is clipped by the group's own `overflow: hidden` and the ring partly disappears.
- ✅ Use it for a **mutually exclusive view mode** with 2–3 short text labels where every option must stay readable at once.
- ❌ Don't use it for an action — a segment sets state. A labelled action is a `Button`.
- ❌ Don't add per-segment borders or radii to fake separation; the `.seg-btn + .seg-btn` divider already does it with one hairline and scales to a third segment for free.
- ❌ Don't build a new switcher out of `.btn` + `.primary`. A selected state pixel-identical to a primary action button is the reason this dedicated control exists.

## Normalize on redesign
1. **The disabled variant has no shipped call site.** `.seg-btn:disabled` exists in the library and in the prototype, but the one running switcher never disables a segment, so the state is unproven in the app.
2. **The shipped ARIA is a toggle-button group, not a tab set, and the template says so on purpose.** `role="group"` + `aria-label` + per-segment `aria-pressed` is what ships (`phan-quyen.page.html:2-7`); the authors declined `role="tablist"`/`role="tab"` because that pattern requires roving tabindex and arrow-key handling the control does not implement. If the control is ever promoted to a real tab set, the keyboard behaviour has to arrive in the same change — a `role="tab"` without it is worse than the group.
3. **Selection is signalled by colour alone** — a solid brand fill against white, with no weight change, underline, icon or inset shadow. The state is invisible in monochrome and marginal under colour-vision deficiency.
4. **No press feedback.** `.btn` presses `translateY(1px)`; `.seg-btn` has no `:active` rule, so the app's two button families feel different under the pointer.
5. **The one inset focus ring in the app**, a consequence of `overflow: hidden` rather than a decision. Rounding each end segment individually instead of clipping would let the ring match everything else.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **Screen-local rule — resolved 2026-08-29.** `.segmented` and `.seg-btn` used to live in one screen's stylesheet, so a second switcher anywhere in the app would have had to copy them. Both are now global library rules, declared once.
2. **No disabled state — added 2026-08-29.** `.seg-btn:disabled` did not exist, and the global `.btn:disabled` could not reach a different class, so an unavailable segment had no appearance at all. It now matches `.btn:disabled`.
3. **`DESIGN.md` under-stated the label weight — resolved 2026-08-29.** The frontmatter's `segmented-button-*` entries described the segment as `typography.table-cell` (12px / **400**); the shipped rule is 12px / **700**. `DESIGN.md` now carries a dedicated `typography.segmented-label` at the right weight.

   Still open, and **not** this spec's to fix: those same two entries declare `padding: "{spacing.button-padding}"` (`6px 8px`) while the shipped segment uses `spacing.sp-2` / `spacing.sp-4` (`6px 10px`). Recorded for whoever owns `DESIGN.md`.
