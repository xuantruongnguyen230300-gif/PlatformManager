---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "target — not built"
updated: "2026-09-05"
component: "DeltaIndicator"
sources:
  - "src/FE/src/styles.scss"
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# DeltaIndicator

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG — with one live half.** This component is a split
> case and the split matters:
>
> - **The colour layer already ships.** `.delta`, `.delta.up`, `.delta.down` and
>   `.delta.flat` are declared in `src/FE/src/styles.scss` § `.delta` and survived
>   the 2026-08-29 consolidation. `../COMPONENTS.md` § Index records them under
>   `Badge.md` as a neighbouring class with no call site.
> - **The wrapper does not.** No `app-delta-indicator` element exists in `src/FE`;
>   the component that classified the number and formatted it went with the
>   dashboard module on 2026-08-29.
>
> ```bash
> grep -n '^\.delta' src/FE/src/styles.scss          # expect the four rules
> grep -rn 'delta-indicator' src/FE/src              # expect zero hits
> ```

**Description:** Inline text showing a period-over-period or self-vs-verified
change, coloured by direction. The wrapper owns two jobs the call sites must not
re-implement: **classifying** the number into up / down / flat against a shared
epsilon, and **formatting** it to the Vietnamese decimal comma.

Retired 2026-08-29 with the two screens that used it; restored 2026-09-05 because
both rebuilt screens use it again — and on the DTI catalogue it now carries a
**new** job (see § Variants).

> **Citation policy.** The global colour classes cite `src/FE/src/styles.scss` by
> **selector name**, never by line number (`doc/Design/CLAUDE.md` § Neo trích dẫn
> vào `styles.scss`). Wrapper and markup cite the prototypes by selector.

## Anatomy

A single `<span class="delta up|down|flat">` whose text is assembled by the caller:
an optional arrow glyph, the number, and an optional unit suffix.
`white-space: nowrap` keeps arrow, number and unit on one line.

**No icon element.** `↑` and `↓` are literal Unicode characters inside the string,
so they cannot be styled, hidden or swapped apart from the number — carried in
`../Icons.md` § Legacy Exceptions and in § Normalize on redesign below.

The wrapper `app-delta-indicator` contributes only `display: inline-block`
(§ `app-delta-indicator`); all painting stays in the global layer so the three
contexts cannot drift apart.

## Variants

| Variant | Classes | Key values | When to use | Real `Chênh lệch` example |
| --- | --- | --- | --- | --- |
| Up — **green** | `delta up` | ink `colors.good` (`src/FE/src/styles.scss` § `.delta.up`) | The value is positive beyond the epsilon | `1.1` — self `7,04`, verified `10` → **`+2,96`** |
| Down — **red** | `delta down` | ink `colors.bad` (`src/FE/src/styles.scss` § `.delta.down`) | The value is negative beyond the epsilon | `1.4` — self `5`, verified `0` → **`−5,00`** |
| Flat — **grey** | `delta flat` | ink `colors.muted` (`src/FE/src/styles.scss` § `.delta.flat`) | The value is within ± epsilon of zero | `1.5` — self `10`, verified `10` → **`0,00`** |
| No comparable value | `delta flat` | ink `colors.muted`, text `—`, no suffix | There is no earlier period to compare against | — not used in this column; history rows only |

The three examples are rows of `spec/DTI_CanGiuoc_2026-08-11.csv` recomputed under
the Q25 direction, and they are the same three the approved prototype draws.

All four share `.delta` itself: weight `850`, `white-space: nowrap`
(`src/FE/src/styles.scss` § `.delta`).

### Two different numbers, one component — read this before using it

The rebuilt screens render `.delta` for **two unrelated quantities**:

| Context | Quantity | Format | Where |
| --- | --- | --- | --- |
| Detail table, `Chênh lệch` column | `Thẩm định − Tự đánh giá`, a **score difference in points** | two decimals, explicit sign: `+2,96` · `−5,00` · `0,00` | Dashboard `app-criteria-table`, DTI catalogue `app-criteria-grid-table` |
| History rows, `Lịch sử các kỳ đã lưu` | period-over-period movement in **percentage points** | arrow + one decimal + unit: `↑ +2,3 đ.%` | Dashboard `app-history-list` |

Both arrived on 2026-09-05: decision Q2 introduced `Chênh lệch` as a **computed,
never-stored, never-typed** column, and decision Q8 put it in the detail table in
place of the three week-comparison columns. The same green/red vocabulary therefore
answers two different questions — the first item under § Normalize on redesign.

#### The `Chênh lệch` direction was reversed by decision Q25 — read this before flipping it back

| When | Formula in force — spec and prototype alike | What criterion `1.4` rendered as (self `5`, verified `0`) |
| --- | --- | --- |
| earlier on 2026-09-05 | `Tự đánh giá − Thẩm định` | `+5,00` in **green** |
| **decision Q25, 2026-09-05** | **`Thẩm định − Tự đánh giá`** | `−5,00` in **red** |

The old direction painted the business meaning backwards. `1.4` is a criterion the
unit scored `5` on and the verification panel struck out entirely — about the worst
outcome a row can have — and it came out green. Under Q25 the number reads as
*what the verifier added to, or took off, the unit's own score*, so the colour and
the meaning finally agree.

**Spec and prototype both changed, and they are back in step.** Earlier on
2026-09-05 both carried the old direction: this file stated
`Tự đánh giá − Thẩm định`, and `Prototype/index.html` drew `1.1` as `−2,96` red and
`1.4` as `+5,00` green to match. Q25 reversed the rule, the prototype was re-drawn the
same day, and this file follows it here. Today both render `1.1` as `+2,96` green and
`1.4` as `−5,00` red. Check the current state rather than trusting either document:

```bash
grep -c 'delta up">+2,96' Prototype/index.html      # PASS = 2 (dashboard + catalogue)
grep -c 'delta down">−5,00' Prototype/index.html    # PASS = 2
```

> **Correction, 2026-09-06 (T13).** The revision of this paragraph written on
> 2026-09-05 said the earlier spec had "quoted the drawn numbers with the classes
> swapped" — i.e. invented its evidence. **That was wrong, and it is withdrawn.** The
> earlier spec matched the prototype as it stood when it was written; the prototype
> was reversed afterwards on the same day. The measurement that produced the
> accusation was taken after that change, so it compared a document against a source
> that had moved underneath it. Nobody fabricated anything. Kept rather than deleted
> because the failure mode is worth naming: *a `grep` against a live file dates the
> file, not the claim* — two documents disagreeing is evidence of a sequence, not of
> bad faith, and this file's own § Variants examples were the thing that changed
> under the reader.

⚠️ **The sign is now the opposite of the `Chênh lệch` column in the source
spreadsheet.** `spec/DTI_CanGiuoc_2026-08-11.csv` writes `1.1` as `-2.96` and `1.4`
as `5.00`. That is not a defect and it cannot break import: the column is computed
by the system and never read from a file. It does mean anyone reconciling an
exported file against the original will see every non-zero row flip sign. How the
export names the column and how import skips it are owned by
`doc/contracts/danh-muc-dti.md` and `spec/danh-muc-dti/business-rules.md`, not here.

Sign reading, in one line: **positive (green) = the verifier scored the criterion
higher than the unit scored itself; negative (red) = lower.**

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Weight `850`, `white-space: nowrap`, ink per variant, `display: inline-block` on the wrapper. Font size is **inherited from context** — `fontSize.fs-sm` in a table cell, `fontSize.fs-xs` in a history row |
| hover | **Not applicable** — plain text inside a table cell or a history row; no `:hover` rule for `.delta`. The table row beneath it does highlight (see [`Table.md`](./Table.md)), the indicator itself does not |
| focus | **Not applicable** — not focusable, no `tabindex` |
| active | **Not applicable** — not interactive |
| disabled | **Not applicable** — not a form control |

## Tokens Used

- `colors.good`, `colors.bad`, `colors.muted`
- Weight `850` is an un-tokenised literal in `src/FE/src/styles.scss` § `.delta`, and is synthetic — no loaded face is 850, so it renders as 800 (`../Tokens/typography.md`)
- No size token of its own: the component deliberately inherits the surrounding text size, which is why the same class renders at two sizes on the same screen

## Reference markup

```html
<!-- detail table — Chênh lệch column, as drawn in Prototype/index.html § #screen-dashboard.
     Direction per decision Q25: Thẩm định − Tự đánh giá. -->
<td class="num"><app-delta-indicator><span class="delta up">+2,96</span></app-delta-indicator></td>    <!-- 1.1: 10 − 7,04 -->
<td class="num"><app-delta-indicator><span class="delta down">−5,00</span></app-delta-indicator></td>  <!-- 1.4: 0 − 5 -->
<td class="num"><app-delta-indicator><span class="delta flat">0,00</span></app-delta-indicator></td>   <!-- 1.5: 10 − 10 -->

<!-- history row — period-over-period movement -->
<app-delta-indicator><span class="delta up">↑ +2,3 đ.%</span></app-delta-indicator>

<!-- oldest period: no comparable value, so the row shows copy instead of a delta -->
<span class="muted">Kỳ đầu</span>
```

Sources: `src/FE/src/styles.scss` (§ `.delta`, § `.delta.up`, § `.delta.down`,
§ `.delta.flat` — the live colour layer),
`doc/Design/Frontend/PlatformManager/Prototypes/index.html`
(§ `app-delta-indicator` — the wrapper's `display: inline-block`),
`Prototype/index.html` § `#screen-dashboard` (detail table and history rows) and
§ `#screen-dti` (the `Chênh lệch` column of the catalogue grid).

## Do / Don't

- ✅ Pass the raw signed number and let one place classify and format it. Pre-formatting at the call site is how the epsilon rule and the decimal comma diverge.
- ✅ Keep one epsilon everywhere a delta is classified. A strict `> 0` on floating-point scores flags rounding noise as real movement — `2.2` in the real data is `0,38` against `0,38`, which must read `0,00`, not `+0,00000001`.
- ✅ Handle "nothing to compare" **outside** the component when the context has better copy. The history list prints `Kỳ đầu` on its oldest row rather than a bare em dash.
- ✅ Keep the two contexts' formats distinct — two decimals and no unit for a score difference, one decimal and `đ.%` for a percentage-point movement.
- ❌ Don't add an icon set for up/down. The shipped arrows are characters inside the text; introducing `<i>` elements would give the app two direction vocabularies.
- ❌ Don't restyle `.delta` per screen. The classes are global precisely so the contexts agree, and a screen-local override is the exact drift the 2026-08-29 consolidation removed.
- ✅ Read the `Chênh lệch` colour at face value **since decision Q25** — red is the verification panel scoring the criterion lower than the unit claimed. A spec, mock or prototype that shows `1.4` in green predates Q25 and is wrong.
- ❌ Don't reconcile the column against the `Chênh lệch` figures in `spec/DTI_CanGiuoc_2026-08-11.csv`. That file computes the difference in the opposite direction, and the app recomputes the value rather than reading it.

## Normalize on redesign

1. **One colour vocabulary, two meanings.** Since 2026-09-05 `.delta` paints both a score difference (`Chênh lệch`, points) and a period movement (`đ.%`). Decision Q25 fixed the half of this that was actively misleading — red in the `Chênh lệch` column now does mean "worse", as it already did in the history rows — but a reader scanning colour alone still cannot tell **which** question a given red number answers. *"The panel cut our score"* and *"we went backwards since last week"* are different facts wearing the same paint, and only the unit suffix separates them. Give the score difference its own treatment, or make the unit unmissable.
2. **The "no data" case and the "measured zero" case share a class.** Both render `.delta.flat`; only the text differs (`—` vs `0,00`). They are different facts.
3. **`↑` / `↓` live inside the text string**, so direction is unstyleable, is read aloud by assistive tech as a glyph, and disappears if the substituted font lacks it. `../Icons.md` § Normalize on redesign #1.
4. **Weight `850` is synthetic and un-tokenised.**
5. **The KPI tile duplicates this logic.** The `So với tuần trước` tile computes its own direction and renders it through `KpiTile`'s `.value.good` / `.value.bad` rather than through `.delta` — so the app carries two implementations of one classification rule. Route the KPI delta through this component, or extract the classification into one shared helper.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05. -->

1. **The epsilon value.** The retired implementation used `0.001` and matched the backend's rule. Whether the rebuilt one keeps it — and whether the same epsilon applies to a two-decimal score difference as to a one-decimal percentage — belongs to `spec/dashboard-dti/business-rules.md`, not here.

### Answered elsewhere — do not re-ask

| Was open here | Answer | Where it lives now |
| --- | --- | --- |
| Which way round `Chênh lệch` subtracts | **`Thẩm định − Tự đánh giá`** — decision Q25, 2026-09-05 | § Two different numbers, above |
| Whether `Chênh lệch` keeps the delta colouring at all | **Yes, and it is now the right way round.** Q25 fixes green/red rather than removing it: positive green, negative red, zero grey | § Variants |
| Whether the sign has to match the spreadsheet BA supplied | **No.** The field is computed, never imported; the exported column header states its direction | `doc/contracts/danh-muc-dti.md` |
