---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
category: "typography"
live_source: "doc/Prototype/dashboard.html"
---

# Typography — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

Single font stack declared once on `body` (`dashboard.html:16`) and inherited everywhere — no per-heading font swap, no webfont `<link>`/`@font-face` (relies on system fonts only, in stack order). There is no formal type scale/mixin system; each selector below sets `font-size`/`font-weight` directly in the same `<style>` block. No dark-mode-only typography differences exist (typography does not vary by theme in the shipped app — only two viewport overrides at `@media(max-width:560px)`, listed under "Responsive overrides").

## Token Table

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| font-family-base | `Inter, Segoe UI, Arial, sans-serif` | `body{font-family:...}` | `dashboard.html:16` |
| body (base, browser default) | size not explicitly set (inherits UA default ≈16px) | `body` | `dashboard.html:16` |
| h1-logo | `18px`, weight not set (UA bold) | `.logo h1{font-size:18px}` | `dashboard.html:20` |
| h2-title | `16px` | `.title h2{font-size:16px}` | `dashboard.html:32` |
| kpi-value | `27px`, weight `850` | `.kpi .value{font-size:27px;font-weight:850}` | `dashboard.html:29` |
| kpi-label | `12px`, color `text-muted` | `.kpi .label{font-size:12px;color:var(--muted)}` | `dashboard.html:29` |
| kpi-sub | `12px`, color `text-muted` | `.kpi .sub{font-size:12px;color:var(--muted)}` | `dashboard.html:29` |
| muted-caption | `12px`, color `text-muted` | `.muted{color:var(--muted);font-size:12px}` | `dashboard.html:32` |
| group-row | `13px` | `.group-row{font-size:13px}` | `dashboard.html:36` |
| table-cell | `12.5px` | `th,td{font-size:12.5px}` | `dashboard.html:41` |
| delta | weight `850` (size inherits `.num`/`td`) | `.delta{font-weight:850}` | `dashboard.html:46` |
| badge | `11px`, weight `750` | `.badge{font-size:11px;font-weight:750}` | `dashboard.html:47` |
| history-row | `12px` | `.histrow{font-size:12px}` | `dashboard.html:50` |
| button-label | weight `700` (size inherits) | `.btn{font-weight:700}` | `dashboard.html:22` |
| fab-label | weight `800` (size inherits) | `.fab{font-weight:800}` | `dashboard.html:51` |
| report-body | `13px`, line-height `1.55` | `.report{font-size:13px;line-height:1.55}` | `dashboard.html:53` |
| footer | `11px` | `.footer{font-size:11px}` | `dashboard.html:54` |

### Responsive overrides (`@media(max-width:560px)`, `dashboard.html:56`)

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| h1-logo (mobile) | `16px` | `.logo h1{font-size:16px}` | `dashboard.html:56` |
| kpi-value (mobile) | `22px` | `.kpi .value{font-size:22px}` | `dashboard.html:56` |

## Chart Palette

<!-- N/A for typography — see Tokens/colors.md. -->

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only). Typography tokens are theme-invariant (no dark-mode differences shipped) and live entirely in `global`.
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together.
