---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
result: "BLOCKED"
audited: "2026-08-22"
---

# Audit Report — PlatformManager

> ## ✅ All 11 findings were fixed on 2026-08-22, after this run
>
> **The BLOCKED verdict below is the verdict of *this* run and is left unedited** — the audit records what it found, and a fresh verdict needs a fresh `/design-audit PlatformManager`. What follows is the resolution log, not a new verdict.
>
> | # | Resolved by |
> |---|---|
> | 1 | `Icons.md` now documents **two** icon sources. The false claim ("no `<svg>` … anywhere") is replaced; 5 rows added for the PrimeNG inline-SVG set (4 paginator arrows + spinner), with the binding sites that switch them on and 3 new Normalize items covering their accessibility. |
> | 2 | `Screens/02:179`, `03:162`, `04:169` rewritten against `Icons.md:5`. |
> | 3 | The three `> **Shell:**` blockquotes now cite `DESIGN.md:418-422` and summarise the two Angular shells it documents. |
> | 4 | `DESIGN.md:428` → COMPONENTS.md is 27+1 and is the composition gate; `:432` → the two icon sources. |
> | 5 | `UiInventory.md:37` → both brand marks are the bare string `PM`, with the `sidebar.html:3` / `auth-card.html:4` sources and the machine check that found zero images. |
> | 6 | `05-auth.md:229` now agrees with `:104`. |
> | 7 | `CoreSeeder.cs:81-86` → `:121-126` in `Icons.md` (7 places) **and in three files this audit missed**: `Components/Sidebar.md:112`, `Screens/04:26,114,137`, `UiInventory.md:27`. |
> | 8 | An `empty` bullet added to both auth screens, stating why none ships. |
> | 9 | `02` § Normalize: closed history moved into a "✅ Closed" blockquote; the open items renumbered 1–11. |
> | 10 | `01:196` → "authors no icon of its own", plus the runtime-injected set; specs 02/03/04 gained the same. |
> | 11 | Not a defect — recorded as-is. |
>
> **Two corrections made while resolving, both to work done in this same pass:**
> 1. `Icons.md` first claimed all six `[loading]` bindings render a PrimeNG spinner. They do not — `phan-quyen.page.html:26,49` are the app's own `input<boolean>()` on hand-written `<table>`s (nothing under `platform/phan-quyen/` imports `TableModule`), and two more are page-level forwards into a child grid. **Two** `p-table` instances render a spinner, not six.
> 2. Editing `trend-chart.{ts,html}` (an unrelated code fix the same day) shifted every line in those files, silently invalidating **35** citations across 5 docs. All were remapped and machine-verified.
>
> **Machine checks now passing repo-wide:** 1622 `file:line` citations, **0** out of range (was 8). 130 token-value ↔ source-line pairs re-verified. `designmd lint` 0 errors / 6 warnings.

## Verdict

**BLOCKED** by findings **1–7**. This audit replaces the 2026-08-11 **PASS**, which was voided: that verdict examined the prototype-era artifact set extracted from `doc/Prototype/dashboard.html`, and stages 2–6 have since been re-run against the shipped Angular app in `src/FE/`.

The lint gate is **clean** — `DESIGN.md` returns **0 errors, 6 warnings**, and none of the six is a real defect (2 are linter false positives that compare an alpha-composited brand tint against brand itself; 4 are border-colour tokens the design.md `components` schema has no slot for). Ten of the fourteen checks pass outright, and check (f) is legitimately N/A.

What blocks is **fidelity drift**, which this template classifies as always blocking: seven places where a spec states something the shipped app or a sibling doc contradicts. Six of the seven are a single mechanical failure repeated — a doc was corrected, and the documents that cite it were not. Finding 1 is different in kind and the reason the run cannot be waved through: `Icons.md` does not merely omit a shipped icon source, it **explicitly denies that source exists**. A reader who trusts it will conclude the app renders no SVG at all.

Nothing here requires touching `src/`. Every finding is a documentation correction.

## Findings

| # | Category | Severity | Location | Fix command |
|---|----------|----------|----------|-------------|
| 1 | icons | blocking | `Icons.md:11` — asserts *"there is no `<svg>` sprite and no icon component wrapper anywhere in `src/FE/`"*. PrimeNG renders **inline SVG** for paginator arrows and the loading spinner at runtime: `[paginator]="true"` at 3 tables, `[loading]` bound at 6 sites. Absent from both the Per-Action Map and Legacy Exceptions — the silent omission check (i) forbids | `/design-document-components PlatformManager` |
| 2 | fidelity drift | blocking | `Screens/02-danh-muc-dti.md:179`, `03-quan-tri-nguoi-dung.md:162`, `04-phan-quyen.md:169` each state `Icons.md` frontmatter reads `library: "none"`. False since the 2026-08-22 refresh — `Icons.md:5` reads `PrimeIcons v7`. `04:169` goes furthest, recording "a gap" that no longer exists | `/design-create-screens PlatformManager` |
| 3 | fidelity drift | blocking | `Screens/02:14`, `03:14`, `04:14` state `DESIGN.md` → Layout "still documents the prototype's single sticky-topbar page with no sidebar". False — `DESIGN.md:418-422` documents **both** Angular shells (sidebar 220/60px + `.shell-content`; `noShell` auth shell) and all three breakpoints | `/design-create-screens PlatformManager` |
| 4 | fidelity drift | blocking | `DESIGN.md:428` says `COMPONENTS.md` "still describes the prototype" (it holds 27 specs + 1 obsolete); `DESIGN.md:432` says `Icons.md` "still claims `library: "none"` and is stale" (corrected same day). `DESIGN.md` carries stale claims about its own siblings | `/design-extract-tokens PlatformManager` |
| 5 | fidelity drift | blocking | `UiInventory.md:37` describes the brand marks as *"text + a PrimeIcons glyph (`pi-shield`)"*. Both are the bare string `PM` (`sidebar.html:3`, `auth-card.html:4`); `pi-shield` is the BE-seeded nav icon for Phân quyền (`CoreSeeder.cs:126`). `Components/Sidebar.md:22` and `AuthCard.md:24` already say "the mark is typographic". The conclusion (no logo asset) holds; the description of what ships does not | `/design-inventory-ui PlatformManager` |
| 6 | fidelity drift | blocking | `Screens/05-auth.md` contradicts itself: `:104` records `Icons.md` "was corrected in the 2026-08-22 refresh"; `:229` says it "still declares `library: "none"` from the prototype era" | `/design-create-screens PlatformManager` |
| 7 | citations | blocking | `Icons.md:33-38,59` cite `CoreSeeder.cs:81-86` for the six seeded nav icons. Those lines are the bootstrap **account** loop (`FindByNameAsync`, `new AppUser`); the menu seed is `CoreSeeder.cs:121-126`. Repeated at `Screens/04-phan-quyen.md:177` | `/design-document-components PlatformManager` |
| 8 | states | warning | `Screens/05-auth.md` — neither screen has an **empty** state bullet, and neither declares that none ships. Every other state is covered and the absent ones are declared explicitly (`:82` no spinner, `:83` no per-field error slot, `:210` locked-out unreachable), which is what makes this one omission stand out | `/design-create-screens PlatformManager` |
| 9 | fidelity markers | warning | `Screens/02-danh-muc-dti.md:221,233` — 2 of 12 "Normalize on redesign" bullets are closed history + as-shipped description rather than deviations awaiting a fix. Struck-through entries document *why* something changed, which is useful, but they belong outside a list whose contract is "things still to fix" | `/design-create-screens PlatformManager` |
| 10 | icons | warning | `Screens/01-dashboard.md:196` states "This screen renders no icon of its own", but `criteria-table.html:32` paginates (SVG arrows) and `danh-muc-dti.page.html:46` renders an SVG spinner. Specs 01, 02, 04 all omit the PrimeNG SVG source — the same root cause as finding 1 | `/design-create-screens PlatformManager` |
| 11 | lint | info | `DESIGN.md` — 0 errors, **6 warnings**: `sidebar-item-active` and `chart-line` report 1.00:1 because the linter reads their `backgroundColor` as an 8-digit hex and compares the colour against itself; `line`, `border-strong`, `bad-border`, `border-notice` are border colours the design.md `components` schema has no slot for. Both categories are recorded as as-shipped facts, not defects | `npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md` |

### Checks that passed

- **(a) Inventory census** — all 6 real routes from `app.routes.ts` are described (`''` and `**` are a redirect and a wildcard, not screens).
- **(b) Screenshots** — 6 manifest rows, all `captured`, each with a reproducible instruction (launch command + URL + viewport + state). Variant shots read `on demand`, not `pending`, per the one-desktop-shot-per-screen policy; no credentials recorded anywhere.
- **(c) Brand assets** — declared explicitly as "Still none" with the reason, not silently empty. (The *description* of what ships instead is wrong — see finding 5.)
- **(d) Screen specs** — all 6 screens carry the 7 mandatory sections in order, and every Layout Blueprint is a real nested region tree with structural measurements (`repeat(5,1fr)` KPI grid, a 12-column `min-width:1430px` header, `scrollHeight = max(320, innerHeight − hostTop − 160)`), not a flat component list.
- **(e) Copy** — every Copy-table row cites a source; 259 distinct Vietnamese strings were machine-checked against `src/` and **all** resolve, including HTML-escaped (`&amp;`), runtime-composed (`Biểu đồ tiến độ hàng {tuần|tháng}`) and server-side strings.
- **(f) Logo usage** — **N/A**, correctly. Zero `<img>`, zero `background-image`, zero `<svg>` authored in `src/FE/src`; the only image file is the default `favicon.ico`. Both brand marks are text.
- **(g) States** — 4 of 6 screens cover default/loading/empty/error/validation and declare the ones that do not ship. (2 exceptions in finding 8.)
- **(h) Responsive** — every screen documents its breakpoints **and** names where a breakpoint deliberately does not exist (`criteria-table.scss` has no `@media` at all; the DTI grid scrolls horizontally at 1430px; both auth screens state "All viewports — there is no breakpoint").
- **(i) Icons — PrimeIcons portion** — all 19 `pi-*` classes used in `src/FE` appear in the Per-Action Map; the 4 that ship via BE seeding are attributed to `CoreSeeder`; 3 legacy exceptions (`↑/↓`, `└`, `●`) are declared and verify against source. The frontmatter `library` claim checks out (`angular.json:37-39,123-125`). Only the PrimeNG-SVG source is missing — finding 1.
- **(j) Chart palette** — present in both `DESIGN.md:436` and `Tokens/colors.md:98`, describing the one shipped `p-chart`; the prior "None — app has no charts" is explicitly retracted.
- **(k) Fidelity markers** — the fidelity blockquote opens `DESIGN.md`; every screen spec and `UiInventory.md` carry a "Normalize on redesign" section. (1 exception in finding 9.)
- **(l) Lint** — 0 errors. Gate met.
- **(m) Export log** — `Exports/ExportLog.md` exists with the append-only contract stated.
- **(n) Dashboard** — the stage table in this project's `README.md` and the index row in `doc/Design/README.md` both match reality (1–6 done, 7 running, 8 superseded).

### Verified beyond the required checks

- **Component gate** — 28 specs on disk, 28 indexed, **0** screen-spec references to an unindexed component. All 27 live components are cited by at least one screen; the 1 obsolete (`Fab`) is cited by none, which is correct.
- **Prompt packs** — all 5 flows present. **0** unresolved `{token.reference}`, **0** `var(--…)`, and every hex in all five files resolves to a colour that exists in the live `:root`. No invented values.
- **`tokens.json`** — valid W3C DTCG: 81 `global` + 33 `light` tokens, every one carrying `$type`; `dark` deliberately empty because no dark mode ships. Every colour traces to `styles.scss`.

## Gate Status

| Stage | Status |
|-------|--------|
| 1 Scaffold | ✅ |
| 2 UI Inventory | ⛔ finding 5 |
| 3 Tokens | ⛔ finding 4 |
| 4 Components | ⛔ findings 1, 7 |
| 5 Screens | ⛔ findings 2, 3, 6 |
| 6 Prompt Packs | ✅ |
| 7 Audit | ⛔ this report |
| 8 Figma Export | ⬜ not reached — also superseded by the Figma Starter-plan MCP quota |

## Next command

`/design-document-components PlatformManager`
