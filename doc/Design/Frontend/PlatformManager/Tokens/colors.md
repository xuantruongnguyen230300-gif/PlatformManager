---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
category: "colors"
live_source: "src/FE/src/styles.scss"
---

# Colors — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

**Live source changed 2026-08-22.** Until this refresh every value in this file came from `doc/Prototype/dashboard.html`. Per `doc/Design/CLAUDE.md` § Fidelity Policy (greenfield carve-out expired 2026-08-22) the shipped Angular 20 app is now the only source of truth; the prototype is design-intent reference only. Every row below was re-read from `src/FE/` — see § Drift from the prototype-era extraction for the exact old → new deltas.

**Where the values live.** All **24** named color/elevation tokens are declared in the single `:root { … }` block of `src/FE/src/styles.scss:10-86`. Verified exhaustively: `grep -rE '^\s*--[a-z0-9-]+\s*:' src/FE/src --include=*.scss` returns matches **only** inside that block — there is no second `:root`, no component-scoped custom property, no `element.style.setProperty()` call anywhere in the TypeScript. There is no Style Dictionary, no token build step and no generated token file; the SCSS is compiled as-is by the Angular CLI.

**Naming rule (changed this refresh).** A design token's name is now **exactly its live CSS custom property, minus the `--` prefix** (`--bad-bg` → `bad-bg`). The prototype-era files used a parallel semantic vocabulary (`surface-badge-danger` for `--bad-bg`, `border-input` for `--border-strong`) and that translation layer is what let the two sides drift apart unnoticed for eleven days. It also actively blocked the FE: `styles.scss:45-48` records that when six loose hex literals were tokenised on 2026-08-20 the names were taken "nguyên xi" (verbatim) from this design system's `colors:` keys, which only works while the two vocabularies agree. One name, one value, one place to check.

**Colors used but never tokenised.** Three colors are written as literals inside selectors rather than declared in `:root`. They are listed in their own table below, cited to file + line, and are **not** invented names — the value is real, the name is this document's.

**One deliberate exception to the naming rule.** `DESIGN.md` frontmatter carries an extra key `primary: "#0f5bd7"` that has no matching `--primary` in the source. It is an **alias of `brand`**, required by the design.md schema: without a key literally named `primary`, `designmd lint` raises `missing-primary` and Stitch auto-generates its own key colors, ignoring this palette entirely. It is deliberately **not** mirrored into `tokens.json` — that file feeds Figma Tokens Studio, where a duplicate would create two variables for one color. So the counts differ on purpose: `DESIGN.md` has 34 color keys, `tokens.json` has 33. Do not "fix" this mismatch; if `--brand` ever changes, change `primary` with it.

**How themes switch: they do not.** There is no `data-theme` attribute, no `prefers-color-scheme` query, no theme toggle and no alternate palette anywhere in `src/FE/`. `trend-chart.ts:52-54` states the constraint in the code itself ("màu không đổi runtime — chưa có dark mode/theme switch"). The single shipped palette is therefore the `light` set in `tokens.json`; the `dark` set stays empty rather than inventing values.

**PrimeNG consistency check.** `src/FE/src/app/core/theme/platform-manager-preset.ts:60-69` re-declares ten of these values as TypeScript constants (`BRAND`, `GOOD`, `WARN`, `BAD`, `BG`, `CARD`, `TEXT`, `MUTED`, `LINE`, `BORDER_STRONG`) to build the PrimeNG Aura ramps, with an explicit instruction not to edit them there first. All ten were compared against `:root` during this extraction and **all ten match**. Any future token rename must land in both files.

## Token Table

### Semantic base — `:root`, `src/FE/src/styles.scss:19-50`

| Name | Value (light) | Value (dark) | Live variable | Source line |
| --- | --- | --- | --- | --- |
| bg | `#eef2f8` | *(not shipped)* | `--bg` | `styles.scss` |
| card | `#ffffff` | *(not shipped)* | `--card` | `styles.scss` — shorthand for `#ffffff`; `tokens.json` and `DESIGN.md` carry the 6-digit form for tool compatibility |
| surface-2 | `#e1e7f1` | *(not shipped)* | `--surface-2` | `styles.scss` — hover tint for ghost/icon-only buttons; no longer a default button fill |
| tonal-bg | `#dbe7fa` | *(not shipped)* | `--tonal-bg` | `styles.scss` — default `.btn` fill (pale tint of `--brand`) |
| tonal-ink | `#0f4a9e` | *(not shipped)* | `--tonal-ink` | `styles.scss` — text on `--tonal-bg`; deliberately darker than `--brand` |
| text | `#152033` | *(not shipped)* | `--text` | `styles.scss` |
| muted | `#57647a` | *(not shipped)* | `--muted` | `styles.scss` |
| line | `#dfe6ef` | *(not shipped)* | `--line` | `styles.scss` — faint hairline for cards/table rules; NOT for interactive components |
| border-strong | `#7e91b4` | *(not shipped)* | `--border-strong` | `styles.scss` — inputs/selects/`.tablewrap` ONLY |
| brand | `#0f5bd7` | *(not shipped)* | `--brand` | `styles.scss` |
| brand2 | `#174ca8` | *(not shipped)* | `--brand2` | `styles.scss` — **no longer an orphan**: consumed by `.btn.primary:hover` (`styles.scss:155-156`) |
| good | `#0e7050` | *(not shipped)* | `--good` | `styles.scss` |
| good-bg | `#d9f2e6` | *(not shipped)* | `--good-bg` | `styles.scss` |
| warn | `#965e08` | *(not shipped)* | `--warn` | `styles.scss` |
| warn-bg | `#ffedc7` | *(not shipped)* | `--warn-bg` | `styles.scss` |
| bad | `#a02b2b` | *(not shipped)* | `--bad` | `styles.scss` |
| bad-bg | `#fbdcdc` | *(not shipped)* | `--bad-bg` | `styles.scss` |
| shadow | `0 4px 16px rgba(23,39,67,.1), 0 1px 3px rgba(23,39,67,.06)` | *(not shipped)* | `--shadow` | `styles.scss` — two layers; carries the layer separation that borders used to |

### Surface / text roles added 2026-08-20 — `:root`, `src/FE/src/styles.scss:56-61`

Named at gate G1 from hex literals that already existed scattered across component SCSS — not new values.

| Name | Value (light) | Value (dark) | Live variable | Source line |
| --- | --- | --- | --- | --- |
| on-primary | `#fff` | *(not shipped)* | `--on-primary` | `styles.scss` — text/icon on `--brand` (brand mark, avatar, active seg-btn, `.btn.primary`) |
| surface-track | `#edf1f6` | *(not shipped)* | `--surface-track` | `styles.scss` — progress-bar track (`.bar`, `group-progress-list.scss:20`) |
| surface-table-header | `#f8fafc` | *(not shipped)* | `--surface-table-header` | `styles.scss` — `th` fill, even-row zebra stripe, `.role-tag` chip |
| text-table-header | `#536076` | *(not shipped)* | `--text-table-header` | `styles.scss` |
| surface-notice | `#edf4ff` | *(not shipped)* | `--surface-notice` | `styles.scss` |
| border-notice | `#cfe0ff` | *(not shipped)* | `--border-notice` | `styles.scss` |

### Shipped as literals — used in selectors, not declared in `:root`

| Name | Value (light) | Value (dark) | Live variable | Source line |
| --- | --- | --- | --- | --- |
| surface-topbar | `rgba(255,255,255,0.95)` | *(not shipped)* | `.topbar{background:…}` (+ `backdrop-filter: blur(10px)`) | `shared/components/topbar/topbar.scss:5-6` |
| overlay-backdrop | `rgba(20,28,40,0.45)` | *(not shipped)* | `dialog::backdrop{background:…}` and `.sidebar-backdrop{background:…}` — same value, two declarations | `styles.scss:472`, `shared/components/sidebar/sidebar.scss:228` |
| surface-nav-active | `rgba(15,91,215,0.08)` | *(not shipped)* | `.sidebar-navitem.active{background:…}` | `shared/components/sidebar/sidebar.scss:132` |

### ✅ The three `CÒN NỢ` requests — named here, now declared in the source

`src/FE/src/styles.scss` used to carry three self-declared debts where the FE deliberately left a raw hex in place rather than invent a parallel token name. This refresh assigned the names (following the `<semantic>-<role>` convention the `good`/`warn`/`bad` family already uses), and the FE side then declared them in `:root` and replaced the literals — **no pixel changed**, the values were already shipping.

`grep "CÒN NỢ" src/FE/src/styles.scss` now returns **nothing**.

| Name | Value (light) | Value (dark) | Live variable | Source |
| --- | --- | --- | --- | --- |
| tonal-bg-hover | `#c7dbf5` | *(not shipped)* | `--tonal-bg-hover`, consumed by `.btn:hover` | `styles.scss` |
| bad-bg-hover | `#f5c6c6` | *(not shipped)* | `--bad-bg-hover`, consumed by `.btn.danger:hover` | `styles.scss` |
| bad-border | `#e5a8a8` | *(not shipped)* | `--bad-border`, consumed by `.login-error` — completes the `bad` family (`--bad` ink, `--bad-bg` fill, `--bad-border` edge) | `styles.scss` |

Only `bad` needs a border token today — `.notice` uses `--border-notice` and no `good`/`warn` block draws a border — so `--good-border` / `--warn-border` are deliberately **not** declared. Add them the day a selector needs one.

### Elevation used outside `--shadow`

| Name | Value (light) | Value (dark) | Live variable | Source line |
| --- | --- | --- | --- | --- |
| shadow-primary-hover | `0 8px 20px rgba(15,91,215,0.35)` | *(not shipped)* | `.btn.primary:hover{box-shadow:…}` | `styles.scss:157` |
| shadow-btn-hover | `0 3px 10px rgba(23,39,67,0.1)` | *(not shipped)* | `.btn:hover{box-shadow:…}` | `styles.scss:177` |
| shadow-dialog | `0 24px 70px rgba(0,0,0,0.25)` | *(not shipped)* | `dialog{box-shadow:…}` | `styles.scss:467` |
| shadow-focus-ring | `0 0 0 3px rgba(15,91,215,0.12)` | *(not shipped)* | `.field-input input:focus-visible{box-shadow:…}` — auth fields only; every other control uses `outline: 2px solid var(--brand)` | `styles.scss:544` |

## Chart Palette

**The app ships exactly one chart** (this section previously read "None — app has no charts" and that is now wrong): `src/FE/src/app/modules/dashboard/components/trend-chart/` — a PrimeNG `p-chart type="line"` (`primeng` 20.2 + `chart.js` 4.5, `src/FE/package.json:33-36`) rendering the weekly DTI trend.

It has **no palette of its own and no categorical scale.** There is a single dataset (`trend-chart.ts:97-106`), so there is no series-2 color to document. Chart.js draws to a 2D canvas, which cannot resolve `var(--x)`, so `readCssVar()` (`trend-chart.ts:13-16`) resolves three tokens once via `getComputedStyle(document.documentElement)` and hands literal strings to Chart.js.

| Chart role | Token read | Resolved value | Source line |
| --- | --- | --- | --- |
| Series 1 line (`borderColor`) | `--brand` | `#0f5bd7` | `trend-chart.ts:57`, `:99` |
| Series 1 area fill (`backgroundColor`) | `--brand` @ 12% alpha via `hexToRgba()` | `rgba(15, 91, 215, 0.12)` | `trend-chart.ts:18-24`, `:99` |
| Series 1 point fill (`pointBackgroundColor`) | `--brand` | `#0f5bd7` | `trend-chart.ts:101` |
| Axis tick labels, both axes | `--muted` | `#57647a` | `trend-chart.ts:58`, `:119`, `:123` |
| Y-axis grid lines | `--line` | `#dfe6ef` | `trend-chart.ts:59`, `:120` |
| X-axis grid lines | — | not drawn (`grid: { display: false }`) | `trend-chart.ts:124` |
| Legend | — | not drawn (`legend: { display: false }`) | `trend-chart.ts:114` |

Non-color chart facts, as-shipped: y-axis pinned `min: 0` / `max: 100` with a `${v}%` tick callback (`trend-chart.ts:116-119`); `pointRadius: 4`; `tension: 0` (straight segments, no spline); `fill: true`; `spanGaps: false` over the **full** series — every period the API returns keeps its label, and one with no value is passed through as `null`, so the line breaks at the gap instead of closing over it (fixed 2026-08-22; the previous build pre-filtered nulls, which deleted the missing period from the axis entirely and made `spanGaps` dead configuration); values clamped to `[0,100]`, `null` passing through unclamped; canvas fixed at `height: 220px, width: 100%` via an inline `[style]` binding (`trend-chart.html:8`), with `.chart-wrap{min-height:220px}` (`trend-chart.scss:6`). Empty state is a `.muted` paragraph, not an empty chart (`trend-chart.html:11`).

**Quirk, recorded not fixed:** `trend-chart.ts:55-61` duplicates all three hex values as SSR fallbacks (`readCssVar('--brand', '#0f5bd7')`). They are a second copy of the palette outside `:root` and will silently diverge if a token value changes. See § Normalize on redesign.

## Drift from the prototype-era extraction

Every delta between this file's previous revision (2026-08-11, sourced from `doc/Prototype/dashboard.html`) and the shipped app. Recorded so the change is traceable rather than silent.

**Values changed (same role, new number):**

| Token | Prototype-era value | Shipped value |
| --- | --- | --- |
| bg | `#f3f6fb` | `#eef2f8` |
| muted *(was `text-muted`)* | `#6d788b` | `#57647a` |
| good *(was `success`)* | `#14855b` | `#0e7050` |
| warn *(was `warning`)* | `#c07a00` | `#965e08` — darkened **twice**: `#a8690a` on 2026-08-15, then again 2026-08-22 to clear AA |
| bad *(was `danger`)* | `#c83c3c` | `#a02b2b` — darkened **twice**: `#b83232` on 2026-08-15, then again 2026-08-22 to clear AA |
| good-bg *(was `surface-badge-success`)* | `#e7f7f0` | `#d9f2e6` |
| warn-bg *(was `surface-badge-warning`)* | `#fff3da` | `#ffedc7` |
| bad-bg *(was `surface-badge-danger`)* | `#fdecec` | `#fbdcdc` |
| border-strong *(was `border-input`)* | `#cad4e1` | `#7e91b4` |
| shadow | `0 7px 24px rgba(23,39,67,.08)` | `0 4px 16px rgba(23,39,67,.1), 0 1px 3px rgba(23,39,67,.06)` |

The contrast moves (`muted`, `good`, `warn`, `bad`) are deliberate and dated in the source itself — `styles.scss:11-18` records a 2026-08-15 decision to fix surface tiers measuring ~1.1:1–1.3:1, below WCAG 2.2's 3:1 for UI components. `styles.scss:21-27` records the follow-up "fill-first" decision that produced `--tonal-bg`/`--tonal-ink`/`--surface-2` and demoted `--border-strong` to inputs only.

**Renamed only (value unchanged):** `primary`→`brand`, `primary-alt`→`brand2`, `surface`→`card`, `border`→`line`, `text`→`text`. Per the naming rule above.

**Added (live in the app, previously undocumented):** `surface-2`, `tonal-bg`, `tonal-ink`, `border-strong`, `good-bg`, `warn-bg`, `bad-bg`, `surface-nav-active`, `shadow-primary-hover`, `shadow-btn-hover`, `shadow-focus-ring` — plus the three named above (`tonal-bg-hover`, `bad-bg-hover`, `bad-border`).

**Removed (no longer exists anywhere in `src/FE/`):**

| Token | Prototype value | Why removed |
| --- | --- | --- |
| surface-report | `#f8fafc` | `.report` survives as a class name (`report-dialog.html:6`) but **has no CSS rule in any SCSS file** — the prototype's `background`/`border` were not ported. The element renders unstyled. |
| border-report-dashed | `#cbd6e5` | Same — the dashed border was not ported. |
| shadow-fab | `0 12px 30px rgba(15,91,215,.3)` | The floating action button was not ported; `grep -rn fab src/FE/src` returns nothing. |

## Normalize on redesign

1. ~~**Two pairs still fail WCAG AA after the 2026-08-15 contrast pass**~~ — **FIXED 2026-08-22.** `designmd lint` measured `--warn` on `--warn-bg` (`#a8690a` on `#ffedc7`) = **3.88:1** and `--bad` on `--bad-bg-hover` (`#b83232` on `#f5c6c6`) = **3.89:1**, both under the 4.5:1 that 10px badge text requires (too small for the relaxed 3:1 large-text threshold). Fixed at the source: `--warn` → `#965e08`, `--bad` → `#a02b2b`, then mirrored into `DESIGN.md`, `tokens.json`, this file **and `platform-manager-preset.ts`**. Lint warnings dropped 8 → 6. The two remaining 1.00:1 reports (`sidebar-item-active`, `chart-line`) are linter **false positives** — it compares an alpha-composited brand tint against brand itself instead of against the surface underneath.
2. **`trend-chart.ts:55-61` duplicates `--brand`/`--muted`/`--line` as SSR fallback hex.** Necessary today (canvas cannot read `var()`, and `document` is absent under SSR) but it is a second uncontrolled copy of the palette. A generated constants file, or dropping the fallbacks now that SSR is off (`--ssr=false`), would remove the divergence risk.
3. **`overlay-backdrop` `rgba(20,28,40,0.45)` is declared twice** — `styles.scss:472` and `sidebar.scss:228` — with no shared token. Promote to `:root`.
4. **`.report` renders unstyled** — the class is bound in `report-dialog.html:6` but matches no rule. Either port the prototype's surface/dashed-border treatment or drop the class.
5. **`--card` is declared as `#fff`** while every other color in `:root` is 6-digit. Cosmetic, but it forces every consumer of this file to normalise.
6. **`surface-nav-active` and the focus ring are alpha-composited brand** (`rgba(15,91,215,0.08)` / `…,0.12)`) written as literals. If a `--brand-rgb` channel triplet were declared, both could be expressed as `rgb(var(--brand-rgb) / 8%)` and stay in sync with `--brand`.

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only).
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together. In this project `light` holds the app's single shipped palette and `dark` is an intentionally empty set — do not enable a theme set that does not exist in the shipped app.
