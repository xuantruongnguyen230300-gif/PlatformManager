---
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
screen_ref: "<stem of the Screens file, e.g. DashboardOverview>"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — <Flow / Screen>

<!-- One pack per Screens file. Fill the Master Prompt once from Screens/<screen_ref>.md, then adapt per tool below. Fidelity rule: prompts reproduce the app AS-SHIPPED — quirks included, nothing idealized. -->

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve {token.reference} — inline literal hex/px/font values only. -->

```
Recreate this exact shipped screen — do not idealize.

TOKENS (literal values):
<primary #RRGGBB, page bg #RRGGBB, font <Family> 0.9rem/400, radius 0.25rem, card padding 24px, ...>

LAYOUT:
<Layout Blueprint as prose: regions, widths/heights, component composition, source order.>

COPY (verbatim — reproduce exactly, including typos and mixed languages):
<"..." strings from the screen's Copy table>

STATES:
<default / loading / empty / error / validation — what each looks like>

RESPONSIVE:
<per-breakpoint behavior>

Match the attached screenshots pixel-for-pixel where they conflict with this text.
```

## Google Stitch

<!-- 1. Import the lint-clean DESIGN.md into the Stitch project first (Design → import design.md).
     2. Then paste the screen prompt. Because tokens are imported, this variant MAY use DESIGN.md token names instead of literal values.
     Note: this repo has no Stitch MCP configured — do this manually via stitch.withgoogle.com, or add the `stitch` server to .mcp.json (see doc/Design/SETUP.md). -->

## Claude Design

<!-- Paste the Master Prompt + attach Assets/Screenshots/<flow>/*.png and the Assets/Brand files. Restate the tokens as a CSS custom-property block: -->

```css
:root { --primary: #RRGGBB; --body-bg: #RRGGBB; --radius: 0.25rem; --font-body: "<Family>", sans-serif; }
```

## Google AI Studio

<!-- System instruction = TOKENS block + fidelity rules ("as-shipped, do not idealize").
     User prompt = LAYOUT + COPY sections. Attach screenshots as image parts. -->

## Generic

<!-- Any other tool: paste the Master Prompt verbatim. -->

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/<screen_ref stem>/<view>.png`
- `Assets/Brand/<logo>.svg`
