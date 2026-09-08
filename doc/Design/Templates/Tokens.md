---
kind: luat
scope: core
verified: 2026-09-06
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
category: "colors|typography|spacing"
live_source: "<repo path of the CSS/SCSS/theme file the values were read from>"
---

# <Category> — <project> Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

<!-- Where the values live (file + variable prefix / selector), how themes switch (e.g. data-theme="light|dark" on <html>), and how you extracted them (grep, DevTools computed styles, build output). -->

## Token Table

<!-- One row per token; "Live variable" is the CSS custom property / SCSS var in code, "Declared at" is the file plus the IDENTIFIER that declares it, written `path/to/file.scss` § `--token` — never a line number: line numbers rot silently and the repo gate cannot tell a rotted one from a good one (see doc/Design/CLAUDE.md § Neo trích dẫn vào `styles.scss`). Repeat the table per tier (semantic, subtle-bg, text-emphasis, ...) if the category needs it. -->

| Name | Value (light) | Value (dark) | Live variable | Declared at |
| --- | --- | --- | --- | --- |
| brand | `#0f5bd7` | `#0f5bd7` | `--brand` | `src/FE/src/styles.scss` § `--brand` |

## Chart Palette

<!-- colors category ONLY (delete this section in typography/spacing files). Ordered categorical series colors as shipped, with source lines. If the app has no charts, keep the section and write exactly the line below. -->

None — app has no charts.

## Appendix: tokens.json rules

<!-- Keep this file in sync with Tokens/tokens.json. -->

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only).
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together.
