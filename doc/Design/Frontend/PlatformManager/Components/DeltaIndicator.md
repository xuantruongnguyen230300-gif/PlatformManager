---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "DeltaIndicator"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/modules/dashboard/components/delta-indicator/delta-indicator.ts", "src/FE/src/app/modules/dashboard/components/delta-indicator/delta-indicator.html", "src/FE/src/app/modules/dashboard/components/criteria-table/criteria-table.html", "src/FE/src/app/modules/dashboard/components/history-list/history-list.html"]
---

# DeltaIndicator
**Description:** Inline text showing a period-over-period change, coloured by direction. Shipped as the Angular component `app-delta-indicator` (`delta-indicator.ts:16-41`), which owns both the direction classification and the number formatting — so every call site is guaranteed the same epsilon threshold and the same Vietnamese decimal format.

The colour classes `.delta`/`.up`/`.down`/`.flat` are **global** (`styles.scss:405-420`); the component's own stylesheet deliberately holds only `:host { display: inline-block }` and a comment explaining why the styling stays global (`delta-indicator.scss:1-7`).

## Anatomy
A single `<span class="delta" [class]="direction()">` whose text content is assembled in TypeScript: an optional arrow glyph prefix, the absolute value formatted to one decimal in `vi-VN` locale (so `8.4` renders `8,4`), and a suffix — `' đ.%'` by default, overridable via the `suffix` input. `white-space: nowrap` keeps the arrow, number and unit on one line. **No icon element** — `↑`/`↓` are literal Unicode characters inside the string (`delta-indicator.ts:38`), so they cannot be styled apart from the number.

## Variants

Direction is computed from `value` against `EPSILON = 0.001` (`delta-indicator.ts:4,27-33`), matching the backend's up/down/flat rule (`doc/contracts/dashboard.md`).

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Up | `delta up` | text `colors.good` (`styles.scss:409-411`); text prefixed `↑ +` | `value > 0.001` |
| Down | `delta down` | text `colors.bad` (`styles.scss:413-415`); text prefixed `↓ ` | `value < -0.001` |
| Flat | `delta flat` | text `colors.muted` (`styles.scss:417-419`); no prefix | `value` defined and `\|value\| <= 0.001` |
| Null | `delta flat` | text `colors.muted`; renders the single character `—`, **no suffix** | `value === null` — no comparable earlier period. `direction()` returns `'flat'` for null, so the null and true-zero cases share one class and differ only in text (`delta-indicator.ts:28-32,36-37`) |

**Suffix variants in use:** every shipped call site takes the default `' đ.%'`. The `suffix` input (`delta-indicator.ts:25`) exists but no template overrides it.

### Where it renders

| Context | Call site | Notes |
| --- | --- | --- |
| Criteria grid, `Tăng/giảm` column | `criteria-table.html:60` | Inside `<td class="num">` |
| Saved-period history rows | `history-list.html:12` | Only when `!row.IsFirst`; the first period shows `<span class="muted">Kỳ đầu</span>` instead of a delta |
| KPI tile "so với kỳ trước" | `kpi-summary.html:3` | **Not** this component — `KpiSummary` computes `deltaValue()`/`deltaTone()` itself and passes them to `KpiTile`, which renders the value with `.value.good`/`.bad` rather than `.delta`. Same up/down/flat concept, second implementation |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | `typography.delta` (`font-weight: 850`), colour per variant, `white-space: nowrap`, `:host { display: inline-block }` |
| hover | **N/A** — plain text inside a table cell or history row; no `:hover` rule. The **row** beneath it does highlight (`tbody tr:hover`, see `Table.md`) but the indicator itself is unchanged |
| focus | **N/A** — not focusable, no `tabindex` |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.good`, `colors.bad`, `colors.muted`
- `typography.delta` (weight 850)

Font size is inherited from context — `typography.table-cell` in the criteria grid, `typography.muted-caption` (`--fs-xs`) in a history row — so the same component renders at two sizes. Weight `850` is a literal at `styles.scss:406` and is synthetic: with no webfont loaded (see `Tokens/typography.md`), most systems will synthesise or round it.

## Reference markup

```html
<!-- component template -->
<span class="delta" [class]="direction()">{{ text() }}</span>

<!-- rendered output, by variant -->
<span class="delta up">↑ +8,4 đ.%</span>
<span class="delta down">↓ 2,0 đ.%</span>
<span class="delta flat">0,0 đ.%</span>
<span class="delta flat">—</span>

<!-- call sites -->
<td class="num"><app-delta-indicator [value]="row.Delta" /></td>

@if (row.IsFirst) {
  <span class="muted">Kỳ đầu</span>
} @else {
  <app-delta-indicator [value]="row.Delta" />
}
```

Sources: `src/FE/src/styles.scss:405-420` (`.delta` + three direction classes), `src/FE/src/app/modules/dashboard/components/delta-indicator/delta-indicator.ts:4,8-10,24-40` (epsilon, formatting, direction), `delta-indicator.html:1`, `delta-indicator.scss:1-7` (why styling stays global), `src/FE/src/app/modules/dashboard/components/criteria-table/criteria-table.html:60`, `src/FE/src/app/modules/dashboard/components/history-list/history-list.html:9-13`

## Do / Don't

- ✅ Pass the raw signed number and let the component classify and format it — never pre-format a delta at the call site, or the epsilon rule and the `vi-VN` decimal comma will diverge.
- ✅ Keep the `0.001` epsilon everywhere a delta is classified; a strict `> 0` on floating-point progress would flag rounding noise as real movement (`doc/contracts/dashboard.md`).
- ✅ Handle "no comparable period" **outside** the component when the context has better copy — the history list shows `Kỳ đầu` for the first row rather than a bare `—`.
- ❌ Don't add a distinct icon set for up/down; the shipped arrows are literal characters inside the text.
- ❌ Don't restyle `.delta` per screen — the classes are global precisely so the three contexts agree.

## Normalize on redesign
1. **Null and true-zero are visually identical in class terms** — both render `.delta.flat`; only the text differs (`—` vs `0,0 đ.%`). "No data to compare" and "measured no change" are different facts and a reader scanning colour alone cannot tell them apart.
2. **The KPI tile duplicates this logic.** `KpiSummary` computes its own delta text and tone and renders it through `KpiTile`'s `.value.good`/`.bad` instead of using this component, so the app has two implementations of one rule. Route the KPI delta through `DeltaIndicator`, or extract the classification into a shared helper.
3. `↑`/`↓` are inside the text string, so direction is unstyleable, unreadable to assistive tech as direction, and lost if the glyph is missing from the substituted font. See `Icons.md` § Normalize on redesign #1.
4. `font-weight: 850` is synthetic and un-tokenised.
5. The `suffix` input is never overridden — either exercise it or drop it in favour of the constant.
