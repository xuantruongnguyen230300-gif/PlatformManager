---
kind: luat
scope: du-an
verified: 2026-09-08
project: "PlatformManager"
status: "draft"
updated: "2026-09-08"
title: "PlatformManager"
group: "Frontend"
stack: "Angular 20 (standalone + Signals), PrimeNG + PrimeIcons v7, SCSS"
source_paths: ["src/FE/src/app", "src/FE/src/styles.scss"]
current_stage: "2-6 re-synced against src/FE (2026-09-08) after the i18n layer and the shared DataGrid landed; 7-Audit stale (the BLOCKED verdict is from the 2026-08-22 run and predates every change since); 8-Export superseded"
---

# Design — PlatformManager Design System

> Documents the **shipped Angular app** in `src/FE/`. Specs record the app AS-SHIPPED (real copy, real assets, quirks); deviations belong ONLY in "Normalize on redesign" sections.

## Overview

PlatformManager is an internal administration platform. Today `src/FE/` ships **Core screens only** —
home, sign-in, forced password change, user administration and permissions. The `DtiWeekly` business
module that supplied the weekly digital-transformation dashboard and the DTI catalogue was removed on
2026-08-29 to be rebuilt, so `src/FE/src/app/modules/` no longer exists.

`src/FE/` is a complete Angular 20 application backed by the .NET solution in `src/BE/`, and this
design system is extracted from it. Count its lazy-loaded routes rather than trusting a number
written here:

```bash
grep -cE '^\s+loadChildren:' src/FE/src/app/app.routes.ts
```

PASS = that number equals the row count of the Screen Census in [`UiInventory.md`](./UiInventory.md).
The `''` redirect and the `**` wildcard are not routed screens; both land on `/trang-chu`.

## Interactive Prototype

[`Prototypes/`](./Prototypes/README.md) holds a single self-contained HTML file that renders
the product's screens plus a component-library screen. It opens in a browser with no build step,
no dev server and no backend; fonts and icons are vendored, so it works offline. It is the surface
the team edits **before** writing Angular code — a place to *see* a change, not a place that records
one. A decision made there takes effect only once it is written into [`Tokens/`](./Tokens/).

The 2026-08-29 redesign was decided there and has since landed in `src/FE`:

| Area | Change |
| --- | --- |
| Component layer | One definition per component. Duplicates collapsed: five icon-button variants → one, three input contracts → one, seven dialog footers → one, four table frames → one, three toolbars → one, two badge naming systems → one |
| Colour | Border and surface tokens recomputed against the WCAG contrast formula. Component boundaries reach the 3:1 non-text threshold; previously a card separated from the page background by 1.12:1 and row banding by 1.05:1 |
| Typography | Be Vietnam Pro, vendored locally at five weights across Latin, Latin-Extended and Vietnamese subsets. The previous stack named a face nothing loaded |
| Data grids | Height derived from a flex chain rather than a fixed pixel value, so a grid fills the space available and scrolls internally with a pinned header |
| Toolbars | One pattern on its own surface: bounded search field, filter conditions behind a control with a count badge and removable chips |
| New components | Confirmation / notification dialogs, four `notice` severities, input width variants by data type, redesigned toasts |

`Tokens/`, `COMPONENTS.md` + `Components/` and `Icons.md` were re-derived from the shipped
`src/FE/src/styles.scss` on **2026-08-29**, so they now describe the post-redesign app rather than
the pre-redesign one. `Screens/` was refreshed in two waves: `03`, `04`, `05-auth` and `06` were rewritten against
live code (last on **2026-09-06**, after the i18n layer landed), and `01`/`02` were rewritten on
**2026-09-05** as **target** specs for the DTI rebuild — designs approved from
`Prototype/index.html`, describing screens that do not exist in `src/FE` yet.
`COMPONENTS.md` gained five 📐 rows the same day for the components those two screens compose.
Read each file's own `status:` / `updated:` frontmatter rather than this sentence.

> 🔄 **SỬA 2026-09-08.** This paragraph said *"`05-auth` has not been [rewritten]"*.
> `Screens/05-auth.md` frontmatter reads `status: "current"` / `updated: "2026-09-06"` and the
> file carries a second refresh note dated 2026-09-06.

> Earlier revisions of this section declared the prototype *"newer than the rest of this folder"*
> and told readers to treat `Tokens/`, `Components/` and `Screens/` as a record of the
> pre-redesign application. That stopped being true on 2026-08-29 — verify against a file's own
> `updated:` frontmatter rather than against this paragraph.

## Stack & Live Sources

- **Stack**: Angular 20 standalone + Signals, zoneless; PrimeNG with a custom preset — `p-table` is the only PrimeNG component still rendered (`grep -rn 'p-table\|p-chart' src/FE/src/app --include=*.html`); PrimeIcons v7; SCSS.
- **Token values currently rendering**: the `:root { ... }` block in **`src/FE/src/styles.scss`** — the `--sp-*`, `--fs-*` and `--radius-*` scales plus semantic colours (`--bg`, `--card`, `--surface-2`, `--text`, `--muted`, `--line`, `--border-strong`, `--brand`, `--good`/`--warn`/`--bad` and their `-bg` pairs, `--tonal-bg`, `--tonal-ink`, `--sidebar-w`, `--container-max-width`).
  ⚠️ `src/FE/src/app/app.config.ts` **re-declares part of this palette** in its `APP_PALETTE` constant, which `src/FE/src/app/core/theme/core-preset.ts` expands into the PrimeNG ramps. A value or name change must land in **both** files, or CSS and the component library render different colours with nothing failing.
  These two files are where the tokens *take effect*, not where they are decided: the agreed values live in [`Tokens/`](./Tokens/) and code follows them (§ Maintenance Rules 1–2).
- **Views source**: the lazy-loaded routes in `src/FE/src/app/app.routes.ts` (count them with the command in § Overview). In-page `<dialog>` overlays and in-card view switchers are **not** separate routes — they are documented inside the owning route's screen spec.
- **Ignore**: `src/FE/dist/`, `src/FE/node_modules/`.

## Pipeline Status

Reviewed 2026-08-29, after the `DtiWeekly` module was removed from `src/FE` and the component
layer and palette were rewritten. Nothing in this table is a count — where a number would go,
run the command instead.

> **🔄 Cập nhật 2026-09-08.** Bảng dưới đây mô tả trạng thái tính tới 2026-08-29. Từ đó tới nay
> khu Design đã được đối chiếu lại với `src/FE` trên diện rộng, và ba thứ mới đã vào code cần
> spec theo: tầng **i18n runtime** (mọi chuỗi giao diện nay đến từ `public/i18n/*.json`),
> **`shared/components/data-grid/`** (lưới bản ghi dùng chung — `p-table` nay chỉ được import ở
> đúng một chỗ), và **`shared/components/language-switcher/`**.
>
> Stage 7 (Audit) **chưa chạy lại**: kết luận `BLOCKED` trong `AUDIT.md` là của lượt 2026-08-29,
> có trước toàn bộ những thay đổi trên — đừng đọc nó như tình trạng hôm nay.

| Stage | Skill command | Status |
|-------|---------------|--------|
| 1 Scaffold | `/design-new-project` | ✅ |
| 2 UI Inventory | `/design-inventory-ui` | ✅ re-run 2026-08-29 against `src/FE`. Verify the census still matches the app: `grep -cE '^\s+loadChildren:' src/FE/src/app/app.routes.ts` must equal the Screen Census row count in `UiInventory.md`. **Every screenshot on disk is marked stale there, and one screen has none at all** — see § Screens |
| 3 Tokens | `/design-extract-tokens` | ✅ re-extracted 2026-08-29 from the rewritten `styles.scss`; lint 0 errors. Four colour tokens were renamed in that pass (`Tokens/colors.md` § Drift) |
| 4 Components | `/design-document-components` | ✅ re-run 2026-08-29. `COMPONENTS.md` is the gate and carries a § Retired table for what the module removal and the consolidation took with them. Count the specs, don't read a number: `ls doc/Design/Frontend/PlatformManager/Components/*.md \| wc -l` |
| 5 Screens | `/design-create-screens` | ✅ every spec re-synced against live code; `03`, `04`, `05-auth` and `06` last on **2026-09-06** (i18n layer). `01` and `02` are **not** historical — they were rewritten 2026-09-05 as `status: "target — not built"` under a `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` banner: approved designs for the DTI rebuild, not records of retired code. Verify from frontmatter: `grep -H 'status:' doc/Design/Frontend/PlatformManager/Screens/*.md` |
| 6 Prompt Packs | `/design-generate-prompts` | ✅ **all five packs current as of 2026-09-08.** `03`, `04` and `05-auth` were regenerated 2026-08-29 with the current palette and `'Be Vietnam Pro'` (`Prompts/05-auth-prompts.md:16` records that regeneration; the only `Inter` left in it is the retracted old value and a "you copied the screenshot" tripwire). `01` and `02` were regenerated **2026-09-08** from the rewritten target screen specs: they no longer carry `status: "historical"`, no longer describe the deleted pre-2026-08-29 screens, and no longer emit the retired token names. They carry `status: "draft — target, not built"`, which is `draft` per the skill plus the machine-checkable reason their `verified: khong-ap-dung` is allowed. Verify from frontmatter rather than this cell: `grep -H 'status:' doc/Design/Frontend/PlatformManager/Prompts/*.md` |
| 7 Audit | `/design-audit` | 🔁 **stale.** The last and only run since the prototype era was **2026-08-22** → BLOCKED with 11 findings, all fixed the same day, all documentation drift. That verdict predates the module removal, the palette rewrite and the i18n layer, so it says nothing about today's state. `AUDIT.md` keeps it plus its resolution log, and its banner now carries the same date. **Re-run `/design-audit PlatformManager`** |
| 8 Figma Export | `/design-export-figma` | 🚫 superseded — see note |

> **Stage 8 superseded (2026-08-11, still true):** every Figma account tried hit the **Starter-plan MCP tool-call quota** (6 calls/month) almost immediately. Rather than wait on a plan upgrade, the Figma hand-off was dropped. The artifact set here remains the hand-off. If Figma access improves, `/design-export-figma PlatformManager` can be run against it — but note `Tokens/tokens.json` was re-extracted on 2026-08-29 with four renamed colour keys, so any previously-imported variables would need overwriting rather than merging.

## Screens

One spec per shipped route, plus two specs for screens that are **designed but not built**, plus
one for a screen that ships today and has been decided for deletion.
Cross-check this table against the live route list rather than trusting it:
`src/FE/src/app/app.routes.ts`.

| Route | Spec | Screenshot |
|-------|------|------------|
| `/trang-chu` | 📐 `Screens/01-dashboard.md` — **target**, replacing the welcome screen **in place** (Q18). `Screens/06-trang-chu.md` records what the URL renders *today* and is now historical: its screen is to be deleted (Q29) | `Assets/Screenshots/dashboard/` — captures of the **deleted** pre-2026-08-29 build; none show this design |
| `/danh-muc/dti` | 📐 `Screens/02-danh-muc-dti.md` — **target**; the route was settled 2026-09-05 (Q33) | `Assets/Screenshots/danh-muc-dti/` — capture of the **deleted** pre-2026-08-29 build |
| `/quan-tri/nguoi-dung` | `Screens/03-quan-tri-nguoi-dung.md` | `Assets/Screenshots/quan-tri-nguoi-dung/` — stale |
| `/quan-tri/phan-quyen` | `Screens/04-phan-quyen.md` | `Assets/Screenshots/phan-quyen/` — stale |
| `/dang-nhap` + `/doi-mat-khau` | `Screens/05-auth.md` | `Assets/Screenshots/auth/` — stale |

The table changed twice on 2026-09-05. First, the two DTI rows stopped pointing at historical
descriptions of the pre-2026-08-29 screens and became living target specs approved from
`Prototype/index.html` — read the old ones in git history if you need them. Then decisions Q18,
Q29 and Q33 turned both route cells from open questions into paths: `/trang-chu` for the
Dashboard, `/danh-muc/dti` for the catalogue. The 📐 stays on the **spec**, not on the route,
because neither screen exists in `app.routes.ts` yet — which is also why neither cites a
`file:line` in `src/FE` for its DTI parts. Two specs now describe `/trang-chu`; that is
deliberate and the banner on `06-trang-chu.md` says which is which.

Policy: **one desktop screenshot per screen.** State and viewport variants are captured on demand — a pending backlog nobody works through hides which screens have *no* visual reference at all, which is the thing that matters. `UiInventory.md` § Screenshot Manifest is the master record of which shots exist and which are stale; this column only points at the folders.

## Maintenance Rules

1. **Spec first, code follows** — a token or theme change is agreed and written into `Tokens/*.md`, `Tokens/tokens.json` and the `DESIGN.md` frontmatter **before** it goes into `src/FE/`. A change previewed in [`Prototypes/index.html`](./Prototypes/README.md) has no effect until it is recorded here. Direction decided 2026-08-27, re-confirmed by the product owner 2026-08-29; the rule is held by [`doc/huong_dan/wiki-core/fe/04-design-token-system.md`](../../../huong_dan/wiki-core/fe/04-design-token-system.md) § Chiều — read the reasoning there.
2. **Then land it in code — in BOTH files** — `src/FE/src/styles.scss` (`:root`) **and** the `APP_PALETTE` constant in `src/FE/src/app/app.config.ts`. For a change to `brand` specifically there is a **third** site, `src/FE/src/index.html` § `<meta name="theme-color">`, which no screenshot and no component spec can catch — see `UiInventory.md` § Normalize item 7 and the master file it points at. That constant re-declares part of the palette and is what `src/FE/src/app/core/theme/core-preset.ts` builds the PrimeNG ramps from; change one file and not the other and plain CSS and the PrimeNG component library render different colours with nothing failing, no test breaking and no lint complaining. Reading `src/FE/` to *census* what shipped (pipeline stages 2–4) is a different job and still starts from code.
3. **Then lint** — `npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md` must pass with 0 errors. The bare `npx @google/design.md lint` form **fails silently on Windows** — always use the `--package=…designmd` form.
4. **Cite sources** — every token, component and screen spec cites its live source file (and line where useful) so dev handoff maps 1:1. Line numbers drift whenever `styles.scss` changes; re-verify rather than trusting an old citation.
5. **The `COMPONENTS.md` index is the gate** — a screen spec may only compose components listed there. Adding a `Components/*.md` without an index row does not make it composable.
6. **Never record credentials** in any design artifact, including screenshot capture instructions.

## Capturing screenshots

Both servers must be running. The launch commands, the allowed dev-server ports
and the database setup are recorded in one place only — an earlier copy of them
here had drifted on all three points.

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`doc/Design/CLAUDE.md`](../../CLAUDE.md) § Rules
