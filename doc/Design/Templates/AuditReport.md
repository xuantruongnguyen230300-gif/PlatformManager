---
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
result: "PASS|BLOCKED"
audited: "YYYY-MM-DD"
---

# Audit Report — <project>

## Verdict

<!-- One paragraph: PASS or BLOCKED, and why. PASS requires ZERO blocking findings AND the lint gate at 0 errors (warnings are recorded in Findings, not blocking). BLOCKED must name the blocking finding numbers. Keep `result` frontmatter in sync. -->

## Findings

<!-- One row per finding, blocking first. Severity: blocking | warning | info. Location = file (and line/anchor). Fix command = the exact skill/CLI command that repairs or re-verifies it. Fidelity drift (spec differs from shipped UI outside a "Normalize on redesign" section) is always blocking. -->

| # | Category | Severity | Location | Fix command |
|---|----------|----------|----------|-------------|
| 1 | tokens | blocking | `DESIGN.md` frontmatter — `{color.primary}` unresolved | `npx --yes --package=@google/design.md designmd lint doc/Design/<Group>/<Project>/DESIGN.md` |

## Gate Status

<!-- One row per pipeline stage. Status: ✅ pass | ⛔ blocked (cite finding #) | ⬜ not reached. -->

| Stage | Status |
|-------|--------|
| 1 Scaffold | ⬜ |
| 2 UI Inventory | ⬜ |
| 3 Tokens | ⬜ |
| 4 Components | ⬜ |
| 5 Screens | ⬜ |
| 6 Prompt Packs | ⬜ |
| 7 Audit | ⬜ |
| 8 Figma Export | ⬜ |

## Next command

<!-- Exactly one command: on PASS → the next pipeline stage skill (usually `/design-export-figma`); on BLOCKED → the top blocking finding's fix command, then re-run `/design-audit`. -->

`/design-<...>`
