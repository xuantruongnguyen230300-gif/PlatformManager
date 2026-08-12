---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
category: "colors"
live_source: "doc/Prototype/dashboard.html"
---

# Colors — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

All named tokens live in the single `:root { ... }` block inside the `<style>` tag of `doc/Prototype/dashboard.html:10-14`. Extracted by reading that block directly (no build tool, no Style Dictionary, no generated token file exists). Additional colors below the divider are used **inline** in selectors throughout the same `<style>` block (not exposed as `--custom-properties`) but are cited with exact source lines so every value is re-checkable.

**No dark mode exists in the shipped app** — there is no `data-theme`/`prefers-color-scheme` handling anywhere in `dashboard.html`, no alternate palette, no theme toggle. The single shipped palette is treated as the `light` set in `tokens.json`; the `dark` set is intentionally left empty (see Appendix) rather than inventing values.

## Token Table

### Root custom properties (`:root`, line 10-14)

| Name | Value (light) | Value (dark) | Live variable | Source line |
| --- | --- | --- | --- | --- |
| bg | `#f3f6fb` | *(not shipped)* | `--bg` | `dashboard.html:11` |
| surface | `#ffffff` | *(not shipped)* | `--card` | `dashboard.html:11` |
| text | `#152033` | *(not shipped)* | `--text` | `dashboard.html:11` |
| text-muted | `#6d788b` | *(not shipped)* | `--muted` | `dashboard.html:11` |
| border | `#dfe6ef` | *(not shipped)* | `--line` | `dashboard.html:11` |
| primary (brand) | `#0f5bd7` | *(not shipped)* | `--brand` | `dashboard.html:12` |
| primary-alt (brand2) | `#174ca8` | *(not shipped)* | `--brand2` | `dashboard.html:12` — declared but **not referenced by any selector or JS template string** in the shipped file; orphan token, kept as-shipped (see project `UiInventory.md` § Normalize on Redesign #4) |
| success (good) | `#14855b` | *(not shipped)* | `--good` | `dashboard.html:12` |
| warning (warn) | `#c07a00` | *(not shipped)* | `--warn` | `dashboard.html:12` |
| danger (bad) | `#c83c3c` | *(not shipped)* | `--bad` | `dashboard.html:12` |
| shadow | `0 7px 24px rgba(23,39,67,.08)` | *(not shipped)* | `--shadow` | `dashboard.html:13` |

### Extended / inline surface colors (used via selectors, not custom properties)

| Name | Value (light) | Value (dark) | Live variable | Source line |
| --- | --- | --- | --- | --- |
| surface-topbar | `rgba(255,255,255,.95)` | *(not shipped)* | `.topbar{background:...}` | `dashboard.html:18` |
| surface-notice | `#edf4ff` | *(not shipped)* | `.notice{background:...}` | `dashboard.html:26` |
| border-notice | `#cfe0ff` | *(not shipped)* | `.notice{border:...}` | `dashboard.html:26` |
| surface-track | `#edf1f6` | *(not shipped)* | `.bar{background:...}` (progress-bar track) | `dashboard.html:37` |
| surface-table-header | `#f8fafc` | *(not shipped)* | `th{background:...}` | `dashboard.html:42` |
| text-table-header | `#536076` | *(not shipped)* | `th{color:...}` | `dashboard.html:42` |
| border-input | `#cad4e1` | *(not shipped)* | `.progressInput`/`.noteInput{border:...}` | `dashboard.html:44-45` |
| surface-badge-success | `#e7f7f0` | *(not shipped)* | `.bdone{background:...}` (text uses `var(--good)`) | `dashboard.html:48` |
| surface-badge-warning | `#fff3da` | *(not shipped)* | `.bwork{background:...}` (text uses `var(--warn)`) | `dashboard.html:48` |
| surface-badge-danger | `#fdecec` | *(not shipped)* | `.bstall{background:...}` (text uses `var(--bad)`) | `dashboard.html:48` |
| overlay-backdrop | `rgba(20,28,40,.45)` | *(not shipped)* | `dialog::backdrop{background:...}` | `dashboard.html:52` |
| surface-report | `#f8fafc` | *(not shipped)* | `.report{background:...}` | `dashboard.html:53` |
| border-report-dashed | `#cbd6e5` | *(not shipped)* | `.report{border:1px dashed ...}` | `dashboard.html:53` |
| shadow-fab | `0 12px 30px rgba(15,91,215,.3)` | *(not shipped)* | `.fab{box-shadow:...}` | `dashboard.html:51` |
| shadow-dialog | `0 24px 70px rgba(0,0,0,.25)` | *(not shipped)* | `dialog{box-shadow:...}` | `dashboard.html:52` |

## Chart Palette

None — app has no charts.

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only).
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together. In this project's `tokens.json`, `light` holds the app's single shipped palette (see above) and `dark` is an intentionally empty set — do not enable a theme set that doesn't exist in the shipped app.
