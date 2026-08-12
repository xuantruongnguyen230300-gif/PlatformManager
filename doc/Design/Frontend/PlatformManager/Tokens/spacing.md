---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
category: "spacing"
live_source: "doc/Prototype/dashboard.html"
---

# Spacing — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

No spacing scale/mixins exist — every `padding`/`gap`/`margin`/`border-radius` is a literal px value set directly per selector in the single `<style>` block of `dashboard.html`. Values below are grouped by role (padding/gap scale, radius scale, breakpoints, elevation) and each cites its exact source line. Spacing does not vary by theme (no dark-mode-only spacing differences shipped).

## Token Table — Padding & Gap Scale

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| space-2xs | `7px` | `.progressInput`/`.noteInput{padding:7px}` | `dashboard.html:44-45` |
| space-xs | `8px` | `.actions{gap:8px}`, `.weekbar{gap:8px}`, `.filters{gap:8px}`, `.histrow{gap:8px;padding:8px}` | `dashboard.html:21,33,38,50` |
| space-sm | `9px 10px` | weekbar/filters `input,select{padding:9px 10px}` | `dashboard.html:34` |
| space-sm-btn | `9px 12px` | `.btn{padding:9px 12px}` | `dashboard.html:22` |
| space-sm-cell | `9px 8px` | `th,td{padding:9px 8px}` | `dashboard.html:41` |
| space-md | `10px` | `.group-row{gap:10px}`, `.title{gap:10px}` | `dashboard.html:32,36` |
| space-md-notice | `11px 13px` | `.notice{padding:11px 13px}` | `dashboard.html:26` |
| space-lg | `12px` | `.topin{gap:12px}`, `.kpis{gap:12px}`, `.layout{gap:12px;margin-top:12px}`, `.title{margin-bottom:12px}`, `.weekbar{margin-bottom:12px}`, `.filters{margin:12px 0}`, section card `margin-top:12px` | `dashboard.html:19,27,31,32,33,38,105,128` |
| space-lg-notice-mb | `14px` | `.notice{margin-bottom:14px}` | `dashboard.html:26` |
| space-lg-card | `15px` | `.card{padding:15px}` | `dashboard.html:28` |
| space-xl | `16px` | `.topin{padding:12px 16px}`, `main{padding:16px}` | `dashboard.html:19,25` |
| space-fab-offset | `18px` | `.fab{right:18px;bottom:18px}` | `dashboard.html:51` |
| space-fab-padding | `13px 17px` | `.fab{padding:13px 17px}` | `dashboard.html:51` |

## Token Table — Radius Scale

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| radius-input | `8px` | `.progressInput`/`.noteInput{border-radius:8px}` | `dashboard.html:44-45` |
| radius-select | `9px` | weekbar/filters `input,select{border-radius:9px}` | `dashboard.html:34` |
| radius-button | `10px` | `.btn{border-radius:10px}` | `dashboard.html:22` |
| radius-notice | `11px` | `.notice{border-radius:11px}` | `dashboard.html:26` |
| radius-table | `12px` | `.tablewrap{border-radius:12px}` | `dashboard.html:39` |
| radius-card | `14px` | `.card{border-radius:14px}` | `dashboard.html:28` |
| radius-dialog | `15px` | `dialog{border-radius:15px}` | `dashboard.html:52` |
| radius-pill | `999px` | `.bar`/`.fill{border-radius:999px}`, `.badge{border-radius:999px}`, `.fab{border-radius:999px}` | `dashboard.html:37,47,51` |

## Token Table — Breakpoints

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| breakpoint-tablet | `max-width: 980px` | `@media(max-width:980px)` | `dashboard.html:55` |
| breakpoint-mobile | `max-width: 560px` | `@media(max-width:560px)` | `dashboard.html:56` |
| breakpoint-print | `print` | `@media print` | `dashboard.html:57` |

## Token Table — Structural Measurements

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| container-max-width | `1450px` | `.topin`/`main{max-width:1450px}` | `dashboard.html:19,25` |
| table-min-width | `1200px` | `table{min-width:1200px}` (no responsive breakpoint; scrolls via `.tablewrap{overflow:auto}` at every viewport) | `dashboard.html:40` |
| dialog-width | `min(700px, 92vw)` | `dialog{width:min(700px,92vw)}` | `dashboard.html:52` |
| kpis-grid-desktop | `repeat(5,1fr)` | `.kpis{grid-template-columns:repeat(5,1fr)}` | `dashboard.html:27` |
| kpis-grid-tablet | `repeat(2,1fr)` (≤980px) | `.kpis{grid-template-columns:repeat(2,1fr)}` | `dashboard.html:55` |
| layout-grid-desktop | `1.15fr .85fr` | `.layout{grid-template-columns:1.15fr .85fr}` | `dashboard.html:31` |
| layout-grid-tablet | `1fr` (≤980px) | `.layout{grid-template-columns:1fr}` | `dashboard.html:55` |
| group-row-grid-desktop | `230px 1fr 90px` | `.group-row{grid-template-columns:230px 1fr 90px}` | `dashboard.html:36` |
| group-row-grid-tablet | `140px 1fr 75px` (≤980px) | `.group-row{grid-template-columns:140px 1fr 75px}` | `dashboard.html:55` |
| group-row-grid-mobile | `110px 1fr 68px` (≤560px) | `.group-row{grid-template-columns:110px 1fr 68px}` | `dashboard.html:56` |
| histrow-grid | `110px 1fr 95px 80px` | `.histrow{grid-template-columns:110px 1fr 95px 80px}` | `dashboard.html:50` |
| chart-height | `245px` | `.chart{height:245px}`, `canvas#trend[height=245]` | `dashboard.html:35`, `101` |

## Token Table — Elevation (Shadow)

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| shadow-card | `0 7px 24px rgba(23,39,67,.08)` | `--shadow` (used by `.card`) | `dashboard.html:13` |
| shadow-fab | `0 12px 30px rgba(15,91,215,.3)` | `.fab{box-shadow:...}` | `dashboard.html:51` |
| shadow-dialog | `0 24px 70px rgba(0,0,0,.25)` | `dialog{box-shadow:...}` | `dashboard.html:52` |

Full color values for the shadow tokens are cross-referenced in `Tokens/colors.md`.

## Chart Palette

<!-- N/A for spacing — see Tokens/colors.md. -->

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only). Spacing/radius/breakpoint tokens are theme-invariant and live entirely in `global`.
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together.
