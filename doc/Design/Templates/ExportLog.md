---
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
figma_file: "<Figma file URL or key>"
---

# Figma Export Log — <project>

<!-- Keep this log append-only; adjust nothing but the frontmatter and the Entries table. -->

This log is **append-only**: never edit or delete past entries. Each stage-8 export (`/design-export-figma`) appends exactly one row; the latest row describes the current state of the Figma file. Recording the `tokens.json` hash plus lint/audit results at export time makes every Figma state traceable back to a repo state. If an export was wrong, append a corrective export and explain in Operator notes.

## Entry format

| Column | Content |
|--------|---------|
| Date | `YYYY-MM-DD` of the export |
| Figma file URL/key | target file link or key (must match `figma_file` frontmatter) |
| Scope | what was exported: `full` \| `tokens-only` \| `screens: <list>` |
| tokens.json hash | `git hash-object Tokens/tokens.json` at export time |
| DESIGN.md lint result | `<errors>E/<warnings>W` from `npx --yes --package=@google/design.md designmd lint` |
| Audit status | `PASS` \| `BLOCKED` from the latest `AUDIT.md` (`/design-audit`) |
| Operator notes | anything done manually in Figma after import (or `—`) |

## Entries

| Date | Figma file URL/key | Scope | tokens.json hash | DESIGN.md lint result | Audit status | Operator notes |
|------|--------------------|-------|------------------|-----------------------|--------------|----------------|
| | | | | | | |
