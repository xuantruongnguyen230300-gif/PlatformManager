---
project: "PlatformManager"
status: "obsolete"
updated: "2026-08-22"
component: "Fab"
obsolete_reason: "Never ported to the Angular app — no .fab class, element or handler exists in src/FE/. Proof: `grep -rni \"fab\" src/FE/src` returns zero matches (verified 2026-08-22, stage 3 and again at stage 4)."
sources: ["doc/Prototype/dashboard.html (FROZEN — historical only)"]
---

# Fab — OBSOLETE

> **This component does not exist in the shipped app.** It is retained as a record of what was dropped and why, per `doc/Design/CLAUDE.md` § Core Principle 3 — deleting the spec outright would lose the reasoning and invite the same control being re-invented later. **Do not compose any screen from it. Do not use it in a prompt pack.**

## Absence proof

| Check | Command | Result (2026-08-22) |
| --- | --- | --- |
| Class, element, handler or comment | `grep -rni "fab" src/FE/src` | **0 matches** |
| Any fixed-position action button | `grep -rn "position: fixed" src/FE/src --include=*.scss` | 3 matches, **all shell chrome** — `sidebar.scss:4,226` (the aside and its drawer backdrop) and `toast.scss:2` (the toast stack). No fixed-position *action* control exists |

Independently confirmed twice: once during the stage-3 token refresh and again during this stage-4 component pass. `DESIGN.md` § Components states the same conclusion: *"The FAB, the prototype's fixed-column 9-col table and the styled `.report` block were **not** ported."*

**Why this spec has no Anatomy, Variants or 5-state table.** Those sections are mandatory for every *live* component spec. Reproducing them here would describe a control that does not ship and would be sourced from the frozen prototype — exactly what the Fidelity Policy forbids. The record below is deliberately historical narrative plus proof of absence, nothing that could be mistaken for a buildable spec.

## What it was

A mobile-only floating action button (`.fab`) fixed to the bottom-right corner of `doc/Prototype/dashboard.html`, shown only below the 980px tablet breakpoint. It was a **positional duplicate** of the topbar's primary "Lưu tuần này" button — same `saveWeek()` handler, shorter label ("Lưu tuần") — added so the save action stayed reachable on mobile, where the topbar actions were hidden.

Historical source (frozen, do not extract from): `doc/Prototype/dashboard.html:51` (CSS), `:55` (media-query visibility), `:136` (markup).

## Why it went

The prototype's whole premise disappeared. It saved a whole week's edits at once, so it needed a persistent, always-reachable save control. The Angular app has **no page-level save on the dashboard at all**:

- The dashboard is now **read-only** — its only action is `Xuất báo cáo` (`period-toolbar.html:51`). There is nothing to save from it.
- Editing moved to the DTI catalogue, where it is **per-cell and immediate**: double-click a cell, confirm with the inline `.cell-icon-btn` check, and that one value is persisted (`criteria-grid-table.html:45-115`). No batch save, so no batch-save affordance.
- The mobile reachability problem it solved was solved structurally instead — the sidebar became an off-canvas drawer and the topbar stays sticky at every breakpoint, so actions no longer vanish on small screens.

## Replaced by

| Prototype need | Shipped equivalent |
| --- | --- |
| Save the week's edits (mobile) | Per-cell inline confirm — `.cell-icon-btn.ok` (`criteria-grid-table.scss:57-99`); no batch save exists |
| Reach the primary action on mobile | Sticky `Topbar` + off-canvas drawer `Sidebar`; primary `Button`s stay in the card title row at every breakpoint |
| Feedback that a save happened | `Toast` (`shared/components/toast/`) — the prototype used a blocking `alert()` |

## If a FAB is ever wanted again

Do **not** restore this spec's values — they came from the frozen prototype and its token vocabulary has since drifted (see `Tokens/colors.md` § Drift). Write a new spec from the live source, and settle the questions this one never answered: it had no hover, no focus and no active treatment (the shared `.btn:active` did not apply, because `.fab` was a separate class), so three of its five states were "not styled". The current `.btn` family has all five (`styles.scss:177-194`) and would be the right base.
