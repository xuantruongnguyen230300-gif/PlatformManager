---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
component: "Badge"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html"
  - "src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.html"
---

# Badge
**Description:** The small pill-shaped label (`.badge`, `src/FE/src/styles.scss` § 5). The base class supplies shape and type; a second class supplies the treatment. Since 2026-08-29 there is **one vocabulary**, split into two families that answer different questions:

- **status** — `.ok` / `.warn` / `.bad` / `.neutral`: a filled pill in a semantic colour pair, saying *how something is going*
- **identity** — `.outline`: a bordered pill with no semantic colour, saying *what something is* (a role name, a code, a classification label)

The identity family absorbed the retired `.role-tag` class on 2026-08-29. Its argument was kept unchanged — a role name is an **identifier, not a status**, so a semantic fill sends the reader looking for a good/bad meaning that is not there — but it is now a variant of one component instead of a second chip primitive re-declaring its own size and radius in bare px.

> **Citation policy.** Values cite `src/FE/src/styles.scss` plus the **selector name**, not a line number.

## Anatomy
`display: inline-block`, a single line of text, no icon element. Pill shape via `rounded.pill`, `typography.badge` (10px / 750), and the `3px 6px` padding literal catalogued in `Tokens/spacing.md` § Padding & gap literals bypassing the scale — there is no padding token at this size.

`.outline` overrides part of that base rather than restating it: `background: transparent`, ink `colors.text`, a 1px `colors.line` border, radius `rounded.sm` **instead of** `rounded.pill`, and weight 700 instead of 750. Display, padding and font-size are inherited from `.badge` untouched — that inheritance is the point of it being a variant.

The status-column call sites prepend a literal `●` character **inside the label string** — it is text, not a styled dot, and screen readers announce it. Recorded in `Icons.md` § Legacy Exceptions.

## Variants

**Status family** — filled, semantic colour, describes an outcome.

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Success | `badge ok` | fill `colors.good-bg`, ink `colors.good` | A positive, settled outcome. Shipped on the user grid: `● Đang hoạt động` when `!row.IsLocked` |
| Warning | `badge warn` | fill `colors.warn-bg`, ink `colors.warn` | Work in progress, or an outcome that needs attention but is not a failure. **No shipped call site today** |
| Danger | `badge bad` | fill `colors.bad-bg`, ink `colors.bad` | A blocked or negative outcome. Shipped on the user grid: `● Đã khoá` when `row.IsLocked` |
| Neutral | `badge neutral` | fill `colors.surface-table-header`, ink `colors.muted` | A no-data or "nothing to report" state carrying no judgement. **No shipped call site today** — the home-screen roles cell it used to hold moved to `.outline` on 2026-08-29, because a role is an identifier rather than a state |

**Identity family** — bordered, no semantic colour, describes what a thing *is*.

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Identifier | `badge outline` | `background: transparent`, ink `colors.text`, 1px `colors.line` border, `rounded.sm`, weight 700 | A name or code carrying no judgement. Shipped in two places, both rendering one chip per role string returned by the API: the user grid's "Vai trò" column and the home screen's roles row |

The four status names all mean a **result**. There is no brand-coloured informational badge (a "New" / "Open" chip) — carried below as Normalize #1.

`.delta` (`.up` / `.down` / `.flat`) sits beside `.badge` in the same section of the stylesheet but is a different component — signed change text, weight 850, `white-space: nowrap`, no pill. It has no shipped call site in the app today.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Status family: per-variant fill and ink, `rounded.pill`. Identity family: transparent fill, `colors.text` ink, 1px `colors.line` border, `rounded.sm`, weight 700. Both families: the `3px 6px` padding literal, `typography.badge`, `display: inline-block` |
| hover | **Not applicable** — a plain `<span>`, never wrapped in a control; no `:hover` rule for `.badge` exists in any stylesheet |
| focus | **Not applicable** — not focusable, no `tabindex`, not a link or a button |
| active | **Not applicable** — not interactive, and no `:active` rule is authored |
| disabled | **Not applicable** — not a form control |

## Tokens Used
- `colors.good`, `colors.good-bg`, `colors.warn`, `colors.warn-bg`, `colors.bad`, `colors.bad-bg`, `colors.surface-table-header`, `colors.muted` — status family
- `colors.text`, `colors.line` — identity family
- `rounded.pill` (status family), `rounded.sm` (identity family)
- `typography.badge`
- Un-tokenised: the `3px 6px` padding, recorded as a literal in `Tokens/spacing.md`

`font-size: 10px` and `font-weight: 750` are written as literals. 10px sits **below** the type scale, whose smallest step is `fontSize.fs-xs` (11px); 750 is not one of the loaded faces, so it resolves upward to 800 (`Tokens/typography.md`). `.outline`'s `font-weight: 700` **is** a loaded face and is the app's dominant emphasis weight, so the identity family is the only badge whose weight renders as authored.

## Reference markup

```html
<!-- user grid, status column — ● is part of the label text -->
@if (row.IsLocked) {
  <span class="badge bad">● Đã khoá</span>
} @else {
  <span class="badge ok">● Đang hoạt động</span>
}

<!-- user grid, roles column — the wrapper only spaces the chips, it does not restyle them -->
<div class="role-cell">
  @for (role of row.Roles; track role) {
    <span class="badge outline">{{ role }}</span>
  }
</div>

<!-- home screen, roles of the signed-in account -->
@for (role of u.Roles; track role) {
  <span class="badge outline">{{ role }}</span>
} @empty {
  <span class="muted">Chưa gán vai trò</span>
}
```

Sources: `src/FE/src/styles.scss` (§ 5 `.badge`, `.badge.ok`, `.badge.warn`, `.badge.bad`, `.badge.neutral`, `.badge.outline`, `.delta`), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html` (status column and roles column), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss` (`.role-cell` — spacing only, no shape), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.spec.ts` (the test pinning every role chip to `.badge.outline` and asserting zero `.role-tag` remain), `src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.html`

## Do / Don't

- ✅ Choose the **family first**: is this label reporting a state, or naming a thing? Status → `ok`/`warn`/`bad`/`neutral`. Identity → `outline`. Only then pick within the family.
- ✅ Treat every badge as read-only computed output — none is clickable, focusable or editable anywhere in the app.
- ✅ Render a `.muted` fallback rather than an empty pill when there is no value; the home screen's roles cell does exactly that.
- ✅ Use `.badge.outline` for a name or a label that carries no judgement — a role is an identifier, and giving it a semantic fill would compete with the real status column sitting beside it in the same row.
- ✅ Let a wrapper own only **spacing** when several chips share one cell. `.role-cell` in the user grid is a flex row with `flex-wrap` and a gap and nothing else; the moment a wrapper starts setting padding or radius, the second chip primitive is back.
- ❌ Don't re-introduce a screen-local badge class. Two vocabularies for one pill is what the 2026-08-29 pass removed; a third would put the app straight back.
- ❌ Don't invent a fifth status colour. Four ship, and none of them means "informational" — see Normalize #1 before reaching for one.
- ❌ Don't put a role name, a code or any other identifier into a status variant. That is the exact swap `.role-tag` was retired to prevent, and `user-grid-table.spec.ts` fails the build if the roles column stops using `.badge.outline`.
- ❌ Don't put an icon element inside a badge; the shipped dot is a text character in the label.

## Normalize on redesign
1. **No informational variant.** All four names describe a *result*. A brand-coloured chip for "New" or "Open" would need a fifth pair, and adding it by hand at a call site would restart the divergence this component just came out of.
2. **`font-size: 10px` is off the type scale** (`fontSize.fs-xs` is 11px) and **`font-weight: 750` is not a loaded face**, so it silently renders at 800 — the same weight as everything already declared 800.
3. **The `●` lives inside the label string**, so the dot cannot be recoloured or hidden without editing copy, and assistive tech reads it aloud.
4. **The two families are told apart only by border-versus-fill.** `.badge.outline` and `.badge.neutral` are both "no judgement" chips at the same size, and nothing in the class names says which one means *identifier* and which means *no data*. `.badge.neutral` has no call site today; deleting it would remove the ambiguity at no cost.
5. **`.outline` is the one badge whose radius leaves the pill shape.** It uses `rounded.sm` while the other four use `rounded.pill`, so the base class no longer guarantees a pill — a caller reading only `.badge` gets the wrong mental picture.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **Two class vocabularies for one visual pair — resolved 2026-08-29.** The global triad `.bdone` / `.bwork` / `.bstall` and the user-grid pair `.active` / `.locked` painted the same two pills under four names in two files. Both sets are gone; `.ok` / `.warn` / `.bad` / `.neutral` is the single vocabulary, and `user-grid-table.scss` records the swap where the local copy used to be.
2. **`.bwork` was overloaded across three meanings — resolved 2026-08-29** with the class itself. The name no longer exists.
3. **Badge contrast below AA — resolved 2026-08-28** (recorded here 2026-08-22, landed in the source six days later). `--warn` was darkened to `#965e08` and `--bad` to `#a02b2b`; a 10px label does not qualify for the relaxed 3:1 large-text threshold, so both pairs had to clear 4.5:1 outright. They now measure 4.66:1 and 5.69:1 (`Tokens/colors.md` § Contrast, as measured). The six-day gap between the doc claiming this and the code doing it is kept on the record deliberately: a completion label nobody re-checks is the most expensive kind of wrong.
4. **A judgement-free variant was missing — added 2026-08-29.** `.badge.neutral` covers "no data yet", which previously borrowed a semantic colour.
5. **`.badge` and `.role-tag` were two chip primitives at the same size — resolved 2026-08-29.** They disagreed on radius, padding and colour system, and `.role-tag` wrote all three as bare px. The class is gone, `Components/RoleTag.md` was deleted, and the identity job it existed for is `.badge.outline`. Its reasoning was kept, its class was not. Both call sites moved the same day — the user grid's roles column and the home screen's roles row — and a test pins them there.
6. **The roles chip had briefly drifted onto a status colour — resolved 2026-08-29.** Between the `.role-tag` removal and the `.outline` variant landing, role names rendered as `.badge.neutral`, i.e. as a status pill in everything but intent. `.outline` restores the distinction the original `.role-tag` argument was about, which is why that argument survives above rather than being retired with the class.
