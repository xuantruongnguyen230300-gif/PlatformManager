---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Badge"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/modules/dashboard/components/status-badge/status-badge.ts", "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html", "src/FE/src/app/modules/dashboard/components/period-toolbar/period-toolbar.html"]
---

# Badge
**Description:** Small pill-shaped status label. The base `.badge` class (`styles.scss:304-325`) supplies shape and type; a second class supplies the colour pair. **Five colour variants ship in three unrelated contexts** — the dashboard criteria status, the user-account status, and a period-mode chip — so the base class is genuinely shared while the variant sets are screen-local.

**Critical distinction (do not confuse):** the dashboard's `bdone`/`bwork`/`bstall` triad is a **computed** value — the backend derives it from progress and delta and returns it as `CriteriaBadge` text (`doc/contracts/dashboard.md`); `StatusBadge` only maps that text to a class and never recomputes it (`status-badge.ts:11-14,26`). It is a different concept from the DTI catalogue's persisted `Status` field, which the catalogue grid renders as **plain text, not a badge** (`criteria-grid-table.html:41`). See `spec/dashboard-dti-weekly/business-rules.md` §5.

## Anatomy
`display:inline-block`, single line of text, no icon element. Pill shape via `rounded.pill`, `spacing.badge-padding`, `typography.badge` (10px/750). The user-status variants prepend a literal `●` character **inside the label string** (`user-grid-table.html:42,44`) — it is text, not a styled dot (see `Icons.md` § Legacy Exceptions).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Done | `badge bdone` | bg `colors.good-bg`, text `colors.good` (`styles.scss:311-314`) | Dashboard criteria status `Hoàn thành` |
| Working | `badge bwork` | bg `colors.warn-bg`, text `colors.warn` (`styles.scss:316-319`) | Dashboard criteria status `Đang thực hiện` **and** `Chưa có dữ liệu` — two different BE values deliberately share one colour (`status-badge.ts:5-9`) |
| Stalled | `badge bstall` | bg `colors.bad-bg`, text `colors.bad` (`styles.scss:321-324`) | Dashboard criteria status `Không tăng` |
| Account active | `badge active` | bg `colors.good-bg`, text `colors.good` (`user-grid-table.scss:49-52`) | User grid, `!row.IsLocked` → label `● Đang hoạt động` |
| Account locked | `badge locked` | bg `colors.bad-bg`, text `colors.bad` (`user-grid-table.scss:54-57`) | User grid, `row.IsLocked` → label `● Đã khoá` |
| Period-mode chip | `badge bwork` | same values as Working | **Reuse outside the status context**: the dashboard toolbar tags "view all periods" mode with `Tất cả · {{ selectedYear() }}` (`period-toolbar.html:5-7`). Same class, no status meaning |
| Null / no status | — (no badge rendered) | `<span class="muted">—</span>` | `StatusBadge` renders an em dash instead of an empty pill when `status()` is falsy (`status-badge.html:1-5`) |

`.active` / `.locked` are declared **only** in `user-grid-table.scss`, not globally — they exist on that one grid. `.bdone`/`.bwork`/`.bstall` are global. The two sets are visually identical pairs (`good-bg`/`good` and `bad-bg`/`bad`); only the class names differ.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | per-variant bg/text; `rounded.pill`; `spacing.badge-padding` (3px 6px); `typography.badge` (10px/750); `display:inline-block` |
| hover | **N/A** — a plain `<span>`, never wrapped in a control; no `:hover` rule for `.badge` in any stylesheet |
| focus | **N/A** — not focusable, no `tabindex`, not a link or button |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.good`, `colors.good-bg`, `colors.warn`, `colors.warn-bg`, `colors.bad`, `colors.bad-bg`, `colors.muted`
- `rounded.pill`
- `spacing.badge-padding`
- `typography.badge`

`font-size:10px` and `font-weight:750` are written as literals at `styles.scss:308-309` — 10px sits **below** the type scale, whose smallest step is `--fs-xs` (11px), and 750 is a synthetic weight most static fonts will fake (see `Tokens/typography.md`).

## Reference markup

```html
<!-- dashboard criteria status — class chosen by StatusBadge from the BE-computed label -->
<span class="badge bdone">Hoàn thành</span>
<span class="badge bwork">Đang thực hiện</span>
<span class="badge bstall">Không tăng</span>

<!-- user account status — screen-local variants, ● is part of the label text -->
<span class="badge locked">● Đã khoá</span>
<span class="badge active">● Đang hoạt động</span>

<!-- period-mode chip — bwork reused with no status meaning -->
<span class="badge bwork">Tất cả · {{ selectedYear() }}</span>
```

Sources: `src/FE/src/styles.scss:304-325` (base + the global triad), `src/FE/src/app/modules/dashboard/components/status-badge/status-badge.ts:4-9,26` (label → class map), `src/FE/src/app/modules/dashboard/components/status-badge/status-badge.html:1-5` (null branch), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:40-46` + `user-grid-table.scss:47-57` (account variants), `src/FE/src/app/modules/dashboard/components/period-toolbar/period-toolbar.html:5-7` (mode chip)

## Do / Don't

- ✅ Treat every badge as read-only computed output — none of the five is clickable, focusable or editable anywhere in the app.
- ✅ Let the backend own the dashboard status text; `StatusBadge` maps text → class and must not recompute the rule (`status-badge.ts:11-14`).
- ✅ Render `—` rather than an empty pill when there is no status (`status-badge.html:3-5`).
- ✅ Keep the dashboard triad visually distinct from the DTI catalogue's persisted `Status`, which ships as plain text in its own column (`criteria-grid-table.html:41`).
- ❌ Don't add a badge for the catalogue's persisted `Status` — it deliberately renders unstyled today.
- ❌ Don't assume `.bwork` means "in progress" — the same class also carries `Chưa có dữ liệu` and the period-mode chip.
- ❌ Don't invent a `.badge.info`/`.badge.neutral` — only these five colour variants exist.

## Normalize on redesign
1. **Two class vocabularies for one visual pair.** `.bdone`/`.active` are the same `good-bg`/`good` pill and `.bstall`/`.locked` the same `bad-bg`/`bad` pill, declared in two files under four names. Converge on one semantic set (`badge-success`/`badge-danger`, matching the `DESIGN.md` `components` keys) and let screens choose by meaning.
2. `.bwork` is overloaded across three meanings — criteria in progress, criteria with no data, and "viewing all periods". Give the mode chip its own neutral variant.
3. `font-size:10px` is off the type scale (`--fs-xs` is 11px) and `font-weight:750` is synthetic — both are literals at `styles.scss:308-309`.
4. The `●` in the account variants lives inside the label string, so the dot cannot be recoloured or hidden without editing copy, and screen readers announce it.
5. ~~`badge-warning` (`warn` on `warn-bg`) measures 3.88:1, below WCAG AA's 4.5:1.~~ **RESOLVED 2026-08-22, during this pass.** `--warn` was darkened `#a8690a` → `#965e08` and `--bad` `#b83232` → `#a02b2b` in the live source, with the reason recorded inline at `styles.scss:40-41,44-45`: a 10px badge label does **not** qualify for the relaxed 3:1 large-text threshold, so the pair had to clear 4.5:1 outright. The change is mirrored consistently in `DESIGN.md`, `Tokens/colors.md`, `Tokens/tokens.json` and `platform-manager-preset.ts:64-65`. Nothing further to do here — the remaining badge items above are unaffected, since they concern type size and class naming, not colour.
