---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "NoticeBanner"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.html"]
---

# NoticeBanner
**Description:** Pale-blue instructional banner (`.notice`, `styles.scss:255-272`). **One instance ships**, on the DTI catalogue page, and it is **conditional** — it appears only while the user is viewing historical (read-only) data, explaining why the edit controls have disappeared (`danh-muc-dti.page.html:7-11`).

> **Behaviour changed from the prototype.** The prototype's banner was permanent and sat at the top of every page load, describing the weekly workflow. The shipped banner is a **state explanation**, rendered by `@if (!isLive())` and absent the rest of the time. Any spec or prompt still describing an always-visible workflow banner is describing the frozen prototype.

## Anatomy
Single block of text in a rounded, pale-blue, bordered rectangle. No icon, no dismiss control, no heading. Full width of its container, with `margin-bottom: spacing.sp-5` separating it from the content below. The class supports inline links (`.notice a`: `colors.brand`, weight 700, underline on hover — `styles.scss:263-271`), though the one shipped instance contains none.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Notice | `notice` | bg `colors.surface-notice`, border 1px `colors.border-notice`, radius 12px, `spacing.notice-padding` (`8px 14px`), `typography.table-cell`, `margin-bottom: spacing.sp-5` | The only variant. One shipped instance: the read-only-mode explanation on the DTI catalogue |
| Notice with link | `notice` + child `<a>` | link `colors.brand`, weight 700, `text-decoration:none`, underlined on hover | Supported by the CSS but **not used by any shipped instance** |

**Only one severity exists.** There is no `.notice.warn`/`.error`/`.success` — transient feedback goes through `Toast`, and form errors through `.form-error` / `.login-error`. The notice is specifically for a persistent, non-dismissible state explanation.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg `colors.surface-notice`, border `colors.border-notice`, radius 12px, `spacing.notice-padding`, `typography.table-cell`, `margin-bottom: spacing.sp-5` |
| hover | **N/A for the banner** — a static text block with no `:hover` rule. Its (unused) link child does have one: `text-decoration: underline` (`styles.scss:268-270`) |
| focus | **N/A** — not focusable; no `tabindex`, no dismiss button. A link child would be focusable but no instance has one |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

**Visibility is the real state.** The banner is present or absent, driven by `isLive()`; it never dims, collapses or animates. The same signal simultaneously hides the toolbar's action buttons (`danh-muc-dti.page.html:36-41`) and switches every grid row to read-only, so the banner and the missing controls always agree.

## Tokens Used
- `colors.surface-notice`, `colors.border-notice`, `colors.text`, `colors.brand` (link child)
- `spacing.notice-padding`, `spacing.sp-5` (bottom margin)
- `typography.table-cell`

**`border-radius: 12px` is a literal** at `styles.scss:259`, not `var(--radius-table)`. The value is numerically identical to `rounded.table`, and `DESIGN.md` maps `notice-banner.rounded` to `{rounded.table}` on that basis — but the source does not reference the token, so the two can drift silently.

## Reference markup

```html
@if (!isLive()) {
  <div class="notice">
    Đang xem dữ liệu lịch sử — chỉ đọc. Quay lại "Tất cả (mới nhất trong năm)" của năm hiện tại để chỉnh sửa.
  </div>
}
```

Sources: `src/FE/src/styles.scss:255-272` (CSS, including the link child), `src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.html:7-11` (the only instance, with its `@if` gate), `:36-41` (the paired action-bar gate)

## Do / Don't

- ✅ Keep the copy verbatim — it names the exact control (`Tất cả (mới nhất trong năm)`) the user must pick to regain editing, so it must not drift from the filter's real option label (`danh-muc-dti.page.html:31`).
- ✅ Keep the banner and the hidden controls driven by the **same** signal; a banner explaining a restriction that is not actually applied is worse than none.
- ✅ Use `Toast` for transient confirmations and `.form-error`/`.login-error` for validation — the notice is for persistent state only.
- ❌ Don't add a dismiss control — the banner explains a condition the user must actively leave, so dismissing it would hide the reason the page is read-only while the page stays read-only.
- ❌ Don't invent severity variants; only the informational blue treatment ships.

## Normalize on redesign
1. `border-radius: 12px` is hardcoded at `styles.scss:259` while every neighbouring rule uses `var(--radius-*)`. Point it at `--radius-table`.
2. The banner has no `role="status"`/`aria-live`, so switching to a historical period silently changes the page to read-only for screen-reader users with no announcement.
3. It carries no icon while the visually similar `.login-error` block leads with `pi pi-exclamation-circle` — two adjacent conventions for "a message box".
4. The link styling in `.notice a` is dead code today; either give the banner a link to the live view (which would make the instruction actionable rather than descriptive) or drop the rule.
