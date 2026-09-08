---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "Avatar"
sources:
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts"
---

# Avatar
**Description:** The initials circle in the user grid's identity cell (`.avatar`) — a solid brand disc containing two derived letters. It is the app's only avatar: **no image, upload or gravatar path exists anywhere in `src/FE`**, so the initials are the whole component.

## Anatomy

`<span class="avatar">{{ initials(row.FullName) }}</span>` — `spacing.avatar` square (30×30), `border-radius:50%` literal, fill `colors.brand`, text `colors.on-primary`, centred flex, `font-weight:800`, `--fs-xs`, `flex:none`.

It is the first child of `.user-cell` — a row flex with `align-items:center` and gap `spacing.sp-3` — followed by a two-line block: `.user-name` (weight 700, inherits `typography.table-cell`) over `.user-email` (`colors.muted`, `--fs-xs`, falling back to `—` when the API returns no email).

**Initials derivation** (`user-grid-table.ts:8-13`): the full name is trimmed and split on whitespace; the component takes the first character of the **last** word and, when there is more than one word, the first character of the **second-to-last**, concatenates them in that order and upper-cases the result. On Vietnamese names — where the given name comes last — that yields the middle-plus-given initial rather than the family-name initial. A single-word name produces one letter; an empty name produces an empty circle.

`flex:none` is load-bearing: without it the circle would compress into an ellipse when a long name pushes the flex row.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Initials disc | `avatar` | 30px circle, `colors.brand` fill, `colors.on-primary` text, weight 800, `--fs-xs` | Every row of the user grid's "Người dùng" column |

There is **exactly one variant**. No size scale, no status ring, no image fallback, and no per-user colour — every disc in the grid is the same brand blue, so the letters are the only differentiator.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | 30px `colors.brand` disc, `colors.on-primary` initials, weight 800, `--fs-xs`, `flex:none` |
| hover | **Not applicable — decorative and non-interactive.** `user-grid-table.scss` authors no `.avatar:hover`. The row beneath still takes the global `tbody tr:hover` fill (`colors.bg`), which changes the surroundings, not the disc. On the 2026-08-29 palette that fill sits at 1.41:1 against `colors.card`, so the disc reads against a visibly different row background than it did before |
| focus-visible | **Not applicable** — a plain `<span>` with no `tabindex` and no role; it is not in the tab order and carries no `alt`/`aria-label`, so assistive tech reads only the two letters as text |
| active / selected | **Not applicable** — the grid has no row selection (see `DataTable.md`), so there is no selected identity to reflect, and no `:active` rule is authored |
| disabled | **Not applicable** — the disc is not a control. A locked account keeps a full-colour avatar; that state is carried by the status-family `Badge` in the "Trạng thái" column instead |

## Tokens Used
- `colors.brand`, `colors.on-primary`; `colors.muted` on the email line beneath
- `spacing.avatar` (30×30), `spacing.sp-3` (gap to the name block)
- `typography.avatar-initials` (11px / 800); the `800` weight is un-tokenised (weights are not tokenised anywhere — see `Tokens/typography.md`)
- `border-radius:50%` is a literal with no token — catalogued in `Tokens/spacing.md` § Radius literals bypassing the scale

## Reference markup

```html
<td>
  <div class="user-cell">
    <span class="avatar">{{ initials(row.FullName) }}</span>
    <div>
      <div class="user-name">{{ row.FullName }}</div>
      <div class="user-email">{{ row.Email ?? '—' }}</div>
    </div>
  </div>
</td>
```

```ts
function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  const last = parts.at(-1)?.[0] ?? '';
  const secondLast = parts.length > 1 ? parts.at(-2)?.[0] ?? '' : '';
  return `${secondLast}${last}`.toUpperCase();
}
```

Sources: `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss:8-35` (`.user-cell`, `.avatar`, `.user-name`, `.user-email`), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:36-44`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts:8-13`, `:60`

> 🔄 **SỬa 2026-09-06** — hai neo `user-grid-table.ts:11-16` trỏ lệch: hàm `initials()` ở `:8-13`, còn `:11-16` rơi vào thân hàm cộng dòng mở khối chú thích của component. Mọi khẳng định còn lại đã đối chiếu đúng — `.avatar` vẫn 30px, `border-radius:50%`, `--brand`/`--on-primary`, `flex:none`; ô danh tính vẫn nằm nguyên ở `user-grid-table.html:36-44` sau khi lưới chuyển sang `<app-data-grid>`.

## Do / Don't

- ✅ Keep `flex:none` on the disc; it is what stops a long name from squashing the circle into an ellipse.
- ✅ Keep `colors.on-primary` for the letters — it is the token declared for exactly this job (text on a `colors.brand` fill), shared with the sidebar brand mark, the auth brand mark and the active segmented button.
- ✅ Pair the disc with the name/email block; the disc alone identifies nobody, since every disc is the same colour.
- ❌ Don't derive a per-user hue from the name. Nothing in the app does that today, and generated screens with rainbow avatars would not match the product.
- ❌ Don't add an image slot; there is no avatar URL on the user model and no upload path in the API contract.
- ❌ Don't reuse `.avatar` for the signed-in user in the Topbar — the Topbar shows a plain name string, no disc.
- ❌ Don't let the disc drift onto the identity `Badge`'s job. They sit two columns apart in the same row and both identify a person; the disc is a picture of *who*, the `.badge.outline` chips beside it are *what roles they hold*.

## Normalize on redesign
1. **The initials rule is wrong for the app's own name order.** Vietnamese names put the given name last, so `Nguyễn Văn A` yields `VA` (middle + given) rather than `NA` or `A`. It is consistent, but it is not "first letter of first and last name" and will surprise anyone reading the code as if it were.
2. **A single-word name renders one letter, and an empty name renders an empty disc** — a solid blue circle with nothing in it, with no fallback glyph.
3. **`border-radius:50%` is the only percentage radius in the app** and has no token; every other rounded thing uses the `--radius-*` scale.
4. **The disc has no accessible name.** Screen readers announce the two letters as ordinary text immediately before the full name, producing a stutter ("V A Nguyễn Văn A"); `aria-hidden="true"` would be more honest for a purely decorative mark.
5. **The `800` weight is off any tokenised scale** — weights are not tokenised anywhere in the app; `Tokens/typography.md` § Weights records which of them the loaded font actually has.
6. **The disc is the last thing in this component still declaring its own colour pair.** `colors.brand` on `colors.on-primary` is correct and survived the 2026-08-29 palette pass unchanged, but it means every avatar carries the app's single strongest colour at 30px in every row of a long list — worth re-checking against the new, darker page background before the pattern is reused elsewhere.
