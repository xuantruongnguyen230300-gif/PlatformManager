---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Footer"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.scss"
  - "src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html"
---

# Footer
**Description:** `.footer` — a single closing footnote line at the bottom of the dashboard: one muted sentence with an inline brand-coloured `routerLink` to the DTI catalogue. It is **page content**, not app chrome: it is rendered by `dashboard.page.html` inside `main`, and it is the app's **only** instance (`grep -rni "footer" src/FE/src` → three hits: the two CSS declarations and this one markup site).

> **Declared once, in `src/FE/src/styles.scss:486-500`** — alongside the other global primitives (`.card`, `.btn`, `.title`, `.badge`). That is the only declaration; `dashboard.page.scss` carries a comment at the former site saying so.
>
> **History worth keeping (fixed 2026-08-22).** The rule used to exist in full, property for property, in **both** `styles.scss` and `dashboard.page.scss` (at what were then lines 28-42; that range now holds the explanatory comment). Angular's emulated encapsulation rewrites the page-scoped copy to `.footer[_ngcontent-…]`, raising it to specificity (0,2,0) against the global copy's (0,1,0) — so the page-scoped copy was the one that painted and the global copy was inert. Because the two were byte-identical, nothing looked wrong: the duplication was undetectable until someone edited one copy and watched the change do nothing. The page-scoped copy was deleted and the global one kept, because `.footer` sits in the global primitive layer and this spec documents it as a component.

## Anatomy

A bare `<div class="footer">` containing a text sentence with one inline `<a>`. No wrapper, no icon, no separator, no columns.

- **Container** — `font-size: --fs-xs` (`typography.footer`, 11px / 400), `color: colors.muted`, `padding: 12px 4px`. That is the entire box: **no background, no border, no top rule, no radius, no shadow, no margin**. It separates from the last card by its own padding alone.
- **Link** — nested `a` rule: `color: colors.brand`, `font-weight: 700`, `text-decoration: none`; `&:hover { text-decoration: underline }`. It is an Angular `routerLink`, not an `href`, so navigation stays client-side.
- **Placement** — last element of the routed page template, after the history card and before the report `<dialog>`; it therefore sits inside `main` (capped at `dimension.container-max-width`, padded `spacing.sp-5`) and scrolls with the page. It is **not** part of the app shell: `app.html` renders `Sidebar` → `Topbar` → `main` → `Toast` and contains no footer, so the other five routes have none.

**The same anchor treatment ships a third time.** `.notice a` (`styles.scss:263-271`) declares the identical four properties — `colors.brand`, `font-weight: 700`, `text-decoration: none`, hover underline. "Brand link inside a muted block" is therefore written out three times across two files with no shared primitive.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Page footnote | `footer` | `typography.footer`, `colors.muted`, padding `12px 4px`; no surface, border or rule | The dashboard's closing line — the app's only instance |
| Inline link | `footer > a` | `colors.brand`, `font-weight: 700`, no underline at rest | The one navigational target inside the sentence |

There are **no** other variants: no bordered, sticky, fixed, dark, multi-column or app-shell footer exists, and no other route renders one. The container also carries no `no-print` class, so unlike the period toolbar it **does** print.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

The container is non-interactive; the link is the only focusable, hoverable part. Every row below was checked against both copies of the rule and against the five `:focus-visible` blocks in `styles.scss`.

| State | Treatment |
| --- | --- |
| default | Container: `typography.footer` (11px / 400), `colors.muted`, padding `12px 4px`, transparent. Link: `colors.brand`, `font-weight: 700`, `text-decoration: none` |
| hover | Link: `text-decoration: underline`; colour and weight unchanged, no transition declared. **Container: no rule** — `.footer:hover` is authored in neither copy, and the block is not clickable |
| focus-visible | **No authored rule — the browser default ring applies.** Neither `.footer` copy declares a focus rule, and `styles.scss` has **no** global `a` or `a:focus-visible` rule at all: its five `:focus-visible` blocks belong to `.btn`, `.action-btn`, the `.filters`/`.weekbar` field rule and the two auth-field rules. This link is therefore the app's only focusable element **without** the house `outline: 2px solid colors.brand` ring — verified, not assumed. See § Normalize #5 |
| active / selected | **Not applicable.** No `:active` rule and no `:visited` rule is authored, so the pressed and visited appearances are the browser's defaults. There is no selected concept either: the link is a one-way `routerLink` to `/danh-muc/dti` and the footer never reflects the current route |
| disabled | **Not applicable — an anchor cannot be disabled and this one is never conditional.** The block is rendered unconditionally (no `@if`, no `[hidden]`, no permission guard around it), and no `.footer` rule declares `opacity`, `pointer-events` or a `:disabled` selector. There is no "unavailable" appearance to record |

## Tokens Used
- `colors.muted` (container text), `colors.brand` (link)
- `typography.footer` (11px / 400) — the container; `Tokens/typography.md` records it as `.footer{font-size:var(--fs-xs)}`
- `spacing.sp-1` — the `4px` horizontal padding, typed as a **literal** rather than `var(--sp-1)`
- **No token behind** the `12px` vertical padding (off the `--sp-*` scale; nearest step `--sp-5` is 14px) or the link's `font-weight: 700` (weights are not tokenised — `Tokens/typography.md`)
- No radius, border, background, shadow or motion token — the element paints and animates none
- Icons: none

## Reference markup

```html
<!-- dashboard.page.html — the whole instance -->
<div class="footer">
  Xem toàn bộ danh mục &amp; nhập/cập nhật dữ liệu tại
  <a routerLink="/danh-muc/dti">Danh mục &gt; DTI</a>.
</div>
```

```scss
/* styles.scss:486-500 — the single declaration */
.footer {
  font-size: var(--fs-xs);
  color: var(--muted);
  padding: 12px 4px;

  a {
    color: var(--brand);
    font-weight: 700;
    text-decoration: none;

    &:hover { text-decoration: underline; }
  }
}
```

Verbatim copy: `Xem toàn bộ danh mục & nhập/cập nhật dữ liệu tại Danh mục > DTI.` — the template writes `&amp;` and `&gt;` as HTML entities, and `Danh mục > DTI` is the link text. Hardcoded Vietnamese in the template; there is no i18n layer.

Sources: `src/FE/src/styles.scss:486-500` (the single declaration), `:263-271` (`.notice a`, the **second** copy of the same anchor treatment — see § Normalize #2), `:118-126` (`.no-print` — **not** applied here), `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.scss:28-33` (a comment at the deleted duplicate's site, not a rule), `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:55-58` (the only markup site), `dashboard.page.ts:2,44` (`RouterLink` import that makes `routerLink` resolve), `src/FE/src/app/app.html:1-14` (the shell — no footer anywhere in it), `src/FE/src/styles.scss` — `--fs-xs`, `--muted`, `--brand` in `:root`

## Do / Don't

- ✅ Keep it as page content at the end of the routed template, after the last card — that is where it ships, and `main`'s padding plus the footer's own `12px` top padding are all the separation it gets.
- ✅ Use `routerLink` for in-app destinations so navigation stays client-side; the shipped link does.
- ✅ Reserve it for a closing footnote — one sentence pointing somewhere else. It has no structural or legal role and carries no branding.
- ❌ Don't add a `.footer` to another route expecting these styles to be maintained: you would inherit the **global** copy, and any future edit made to the page-scoped copy would silently not reach you. Resolve the duplication first (§ Normalize #1).
- ❌ Don't promote it into the app shell. `app.html` has no footer, so adding one changes all six routes at once — including the two auth routes, which render with no shell at all.
- ❌ Don't give it a top border or a background to "separate" it; the app separates blocks with `Card` shadows, and this element is deliberately chrome-free.

## Normalize on redesign
1. ~~**`.footer` is declared twice, identically**~~ — **FIXED 2026-08-22.** The page-scoped copy in `dashboard.page.scss` was deleted; `styles.scss:486-500` is now the single declaration, and a comment sits at the old site explaining why nothing lives there. Kept the global one because `.footer` belongs to the global primitive layer and this spec documents it as a component. Deliberately: keep the **page-scoped** copy if the footnote stays dashboard-only, or keep the **global** copy if any other route will gain one. Do not leave both. Also logged in `UiInventory.md` § Normalize #5, `Tokens/spacing.md` § Normalize #3 and `Screens/01-dashboard.md` § Normalize.
2. **The anchor treatment is declared twice** — here and as `.notice a` (`styles.scss:263-271`), same colour, weight and hover-underline. It was three times until the duplicate `.footer` block went (item 1); one shared link primitive would replace both and give the app a single link style.
3. **Padding `12px 4px` is half off-scale.** `12px` has no step on `--sp-*` (nearest `--sp-5` is 14px); `4px` *is* `--sp-1` but is typed as a literal. Both are free to fix — the second with zero visual change.
4. **`font-weight: 700` is a literal.** There is no `--fw-*` scale anywhere in the app (`Tokens/typography.md` § Normalize #3), so this link's weight cannot be changed systemically.
5. **The link has no house focus ring.** Every other focusable control authors `outline: 2px solid var(--brand)`; this one falls through to the user-agent default, so keyboard focus looks different here from everywhere else in the product.
6. **At rest the link is signalled by colour and weight only**, at `typography.footer`'s 11px, with the underline appearing on hover. Below the smallest type step, `colors.brand` on `colors.bg` is a thin affordance — the same objection applies to `.notice a`.
