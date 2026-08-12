---
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
version: "alpha"
name: "<Project> Design System"
description: "<One line: which app the tokens were extracted from and what importing this file gives Stitch.>"
# The frontmatter IS the token dictionary — Google Stitch reads it directly.
# 2-3 example tokens per category below; extend with the real extracted values.
colors:
  primary: "#RRGGBB"                # brand / CTA fill
  on-primary: "#RRGGBB"             # text on primary
  surface: "#RRGGBB"                # card / panel background
typography:
  body:
    fontFamily: "<Family>, sans-serif"
    fontSize: "0.9rem"
    fontWeight: "400"
  h1:
    fontFamily: "<Family>, sans-serif"
    fontSize: "2.25rem"
    fontWeight: "700"
rounded:
  default: "0.25rem"                # cards, buttons, inputs
  pill: "50rem"
spacing:
  field-gap: "1rem"                 # vertical rhythm between form fields
  card-padding: "1.5rem"
components:
  # Component values interpolate tokens via {token.reference} — resolvable by Stitch only.
  button-primary: "background {colors.primary}, text {colors.on-primary}, radius {rounded.default}"
  card: "background {colors.surface}, padding {spacing.card-padding}, radius {rounded.default}"
---

> **Fidelity:** This file describes the app AS-SHIPPED — real extracted values, real copy, quirks included. Do not idealize. Proposed changes belong ONLY in the specs' "Normalize on redesign" sections.

## Overview

<!-- 2-3 sentences: what the app is, the source theme/framework, and the exact code paths the tokens were extracted from. -->

## Colors

<!-- Prose walkthrough of the palette: semantic roles, tiers (subtle bg / border / text-emphasis), dark-mode mechanism. The full re-checkable table lives in Tokens/colors.md. -->

## Typography

<!-- Font family and where it is loaded from, the shipped scale and weights, source file of the definitions. -->

## Layout

<!-- The app shells (e.g. dashboard vs auth), structural measurements (sidebar width, topbar height, container behavior). -->

## Components

<!-- Short list of the core components with one-line usage rules; the full library index is COMPONENTS.md. -->

## Chart Palette

<!-- Only if the app renders charts: the ordered categorical series colors as shipped. Delete this section if the app has no charts. -->

## Do's and Don'ts

<!-- Bullet pairs: hard rules that keep generations on-brand vs observed drift to avoid. -->

- ✅ <e.g. Use only the palette above; one primary button per view section.>
- ❌ <e.g. Don't invent colors, fonts, or radii not listed in the frontmatter.>

---

<!-- Lint before importing into Stitch:
       npx --yes --package=@google/design.md designmd lint <path-to-this-file>
     WARNING (Windows): the bare form `npx @google/design.md lint <path>` fails silently — always use --package.
     If lint rejects the house keys (project/status/updated), strip them from the exported copy only. -->
