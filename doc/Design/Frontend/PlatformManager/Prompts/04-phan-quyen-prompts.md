---
project: "PlatformManager"
status: "draft"
updated: "2026-08-23"
screen_ref: "04-phan-quyen"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — Permissions / Phân quyền (`/quan-tri/phan-quyen`)

<!-- One pack for Screens/04-phan-quyen.md. Token values resolved to literals from
     src/FE/src/styles.scss (:root) via Tokens/colors.md + Tokens/spacing.md +
     Tokens/typography.md + DESIGN.md frontmatter. Copy is verbatim Vietnamese from the
     shipped Angular templates. Fidelity rule: reproduce the app AS-SHIPPED — quirks
     included, nothing idealized, nothing translated.

     Everything a tool needs is inside THIS file: every token is resolved to a literal
     hex/px/font value, and no other spec has to be opened.

     THE FAILURE MODE THIS PACK EXISTS TO PREVENT: inventing structure. The route is ONE
     card holding ONE matrix — 19 lines of template. Earlier versions of this pack asked
     for a tab bar and a second "resource permissions" matrix; a tab bar never existed in
     src/FE/, and every image generated from that text showed a screen that does not ship.
     The resource matrix is contract PERM-2, still Status: DRAFT — a component implementing
     it now exists (2026-08-24, uncommitted) but is NOT wired into phan-quyen.page.ts, so it
     still does not render on this route — see Screens/04-phan-quyen.md § Normalize on
     redesign #1. Do not add it back until it actually ships wired in. -->

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is a literal. -->

```
Recreate this exact shipped screen — do not idealize, do not translate, do not "improve" anything.

CONTEXT: an internal Vietnamese-language administration platform ("PlatformManager"). This screen is
Permissions ("Phân quyền"), reachable only by a SuperAdmin. It renders INSIDE the app shell (fixed
left sidebar + sticky topbar + centred main + fixed toast stack), unlike the sign-in / change-password
screens which have no shell.

⚠ THE WHOLE PAGE IS ONE CARD HOLDING ONE ROLE x MENU CHECKBOX MATRIX. There is no tab bar, no view
switcher, no segmented control, no second panel and no side-by-side pane. Adding any of those is the
single mistake to avoid: the route's template is 19 lines long and contains exactly one card. Draw
one frame, not two.

The matrix is a hand-rolled HTML table ON PURPOSE — not a data-grid component. It has no paging, no
sorting, no filtering, no column resizing and no row virtualisation. It is a full checkbox matrix
inside a fixed-height scroll box. Do not substitute a data grid.

TOKENS (literal values — use these exact numbers and hex codes):
Colors: brand/primary #0f5bd7 (also the checkbox accent colour and the focus outline); primary hover
#174ca8; text/icon on primary #ffffff; page background #eef2f8; card surface #ffffff; ghost-button
hover tint #e1e7f1; default (secondary) button fill #dbe7fa with text #0f4a9e and hover fill #c7dbf5;
body text #152033; muted text #57647a; faint hairline #dfe6ef (cards, table row rules); strong border
#7e91b4 (the table wrapper ONLY on this screen); success #0e7050; danger #a02b2b; table header
surface #f8fafc with header text #536076 (the same #f8fafc tints every even body row); topbar surface
rgba(255,255,255,0.95) plus backdrop-filter blur(10px); active sidebar item background
rgba(15,91,215,0.08).
Shadows: card / toast / sidebar drawer 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06);
secondary button hover 0 3px 10px rgba(23,39,67,0.1); primary button hover 0 8px 20px
rgba(15,91,215,0.35).
Font: Inter, loaded for real from Google Fonts (weights 400, 500, 600, 700), with the fallback stack
"Segoe UI", Arial, sans-serif. Sizes/weights: body 13px/400; topbar h1 15px/bold; card h2 14px/bold;
button label 12px/700; table header 11px/700 letter-spacing 0.01em; table cell 12px/400 line-height
1.4; the helper paragraph under the card heading 11px/400 in #57647a; sidebar nav item 12px/600 (700
when active); sidebar brand text 14px/800; brand mark 11px/800; toast text 12px/400 line-height 1.4.
Radius: 7px buttons; 9px sidebar nav item and toast item; 12px the table wrapper; 16px cards; the
checkboxes are native, unstyled apart from their accent colour.
Spacing scale in use: 4px, 6px, 8px, 10px, 14px. Card padding 14px. Main padding 14px (10px below
560px). Topbar inner padding 10px 14px. Table cell padding 6px 8px. Button padding 6px 8px. Title row
gap 8px, margin-bottom 10px. Helper paragraph bottom margin 10px. Child-row left padding 28px, with
4px between the "└" glyph and the label.
Structure: sidebar 220px wide (60px collapsed); content column max-width 1600px, centred; the table
wrapper is a scroll box with max-height 560px; breakpoints tablet 980px and mobile 560px; z-index
topbar 20, sidebar backdrop 34, sidebar 35, collapsed-sidebar flyout 40, toast stack 60, sticky table
header 4.
Icons: PrimeIcons v7 (`<i class="pi pi-…">`) are loaded globally, but THIS SCREEN'S OWN TEMPLATE
CONTAINS NO ICON AT ALL — every control is a text button or a native checkbox, and the only glyph is
the literal "└" text character marking a child row. The icons that appear while this screen is open
all belong to the shell.

LAYOUT:

APP SHELL (surrounds the page; identical on all four in-shell routes)
- Sidebar: fixed left, 220px wide, full viewport height, background #ffffff, 1px right border #dfe6ef,
  z-index 35. Brand row (padding 10px, min-height 50px, 1px bottom border #dfe6ef): a 26x26px square
  radius 7px filled #0f5bd7 with white "PM" at 11px/800, then "PlatformManager" at 14px/800, then a
  24x24px ghost collapse button showing `pi-angle-left` (rotated 180° while collapsed). Nav list below
  (padding 6px): each item is a 12px/600 row, padding 6px 8px, radius 9px, 8px gap, with an 18x18px
  icon box (icon 15px, #57647a); hover fills #eef2f8; the active item fills rgba(15,91,215,0.08), turns
  its text and icon #0f5bd7 at weight 700 and grows a 3px-wide #0f5bd7 rail on its left edge (offset
  -8px, inset 5px top and bottom, radius 0 3px 3px 0). The menu is server-driven; the shipped seed is:
  "Dashboard" (pi-th-large) · "Danh mục" (pi-folder, a collapsible group) containing "DTI" (pi-list) ·
  "Quản trị hệ thống" (pi-cog, group) containing "Người dùng" (pi-user) and "Phân quyền" (pi-shield).
  Group parents carry a `pi-chevron-down` chevron on the far right, rotated -90° when closed. On this
  route the "Quản trị hệ thống" group is open and its "Phân quyền" child is the active item.
- Content column: offset 220px from the left (60px when the sidebar is collapsed).
- Topbar: sticky at the top, z-index 20, background rgba(255,255,255,0.95) with blur(10px), 1px bottom
  border #dfe6ef. Inner row max-width 1600px, centred, padding 10px 14px, gap 8px: an icon-only
  hamburger button (`pi-bars`, hidden above 980px) · an h1 at 15px/bold reading the route title ·
  pushed to the far right, the signed-in user's full name at 12px/700 followed by a secondary button
  "Đăng xuất" prefixed with `pi-sign-out`.
- Main: max-width 1600px, centred, padding 14px, on the #eef2f8 page background.
- Toast stack: fixed 14px from the right and bottom edges, z-index 60, max-width min(360px, 90vw),
  8px gap, aria-live polite. Each toast is a white card, radius 9px, 1px border #dfe6ef, card shadow,
  padding 8px 10px, 12px text, with a 4px-wide LEFT accent border coloured by severity (#0e7050
  success · #a02b2b error · #965e08 warning · #0f5bd7 info) and a 22x22px ghost close button with
  `pi-times`. Toasts fade+slide in over 0.15s and auto-dismiss after 5000 ms.

PAGE — one card, directly inside main, with nothing above it
1. THE CARD: #ffffff, 1px border #dfe6ef, radius 16px, padding 14px, card shadow. It is the first and
   only element of the route; main holds nothing else.
2. Title row (space-between, gap 8px, margin-bottom 10px): an h2 at 14px/bold on the left, and on the
   right a primary save button (fill #0f5bd7, white 12px/700 text, radius 7px, padding 6px 8px) whose
   label swaps to a "saving" label while a save is running. It is disabled — opacity 0.5, cursor
   not-allowed — while the matrix is loading or saving.
3. A helper paragraph directly beneath: 11px/400 in #57647a, displayed as a block with a 10px bottom
   margin. Its exact wording is in COPY; it states the open-by-default rule.
4. THE MATRIX, in a scroll box: 1px border #7e91b4, radius 12px, overflow auto, max-height 560px.
   Inside it a plain HTML table at width 100% with collapsed borders on a #ffffff background and
   NO minimum width, so the columns compress with the container rather than being pinned.
   • Header row: the first cell is fixed at width 40% and reads "Màn hình"; then one cell per role,
     right-aligned with tabular numerals. Every header cell is sticky to the top of the scroll box at
     z-index 4, background #f8fafc, text #536076, 11px/700, letter-spacing 0.01em, padding 6px 8px.
   • Body rows: cells 12px/400 line-height 1.4, padding 6px 8px, 1px bottom border #dfe6ef,
     top-aligned; even rows tinted #f8fafc; the hovered row tints #eef2f8. Each role cell is
     right-aligned and holds one native 16x16px checkbox with accent colour #0f5bd7 and, on
     keyboard focus, a 2px #0f5bd7 outline offset 2px.
   • Rows are a MENU TREE, flattened parent-first: every parent row is immediately followed by its own
     children. A child row is left-padded 28px and prefixed with a muted "└" glyph in #57647a sitting
     4px before the label. Indent is a single level regardless of depth.
   • The shipped seeded rows, in this exact order: "Dashboard" · "Danh mục" · "└ DTI" ·
     "Quản trị hệ thống" · "└ Người dùng" · "└ Phân quyền".
   • Role columns, in the order the API returns them: "SuperAdmin", "Admin", "User".
   • The "SuperAdmin" column is a completely ORDINARY column: clickable, enabled, ticked only where the
     data says so. No caption under the header, no lock, no explanatory paragraph under the table.
   • A row with no box ticked means the screen is OPEN to every signed-in user. Nothing on screen marks
     such a row — the rule is stated in the helper paragraph and nowhere else.
   • When there are no rows, a single cell spanning every column (row label + one per role) holds the
     muted sentence from COPY.

COPY (verbatim Vietnamese — reproduce character for character; there is no i18n layer, every string is
hardcoded in the templates or comes straight from the API):
- Route/topbar title: "Phân quyền"
- Sidebar: "PM", "PlatformManager"; collapse button aria-label "Mở rộng menu" / "Thu gọn menu"; nav
  labels "Dashboard", "Danh mục", "DTI", "Quản trị hệ thống", "Người dùng", "Phân quyền"
- Topbar: hamburger aria-label "Mở menu điều hướng"; logout button title and label "Đăng xuất"
- Toast close aria-label: "Đóng thông báo"
- Card heading: "Phân quyền màn hình"
- Save button: "Lưu thay đổi", becoming "Đang lưu…" while saving (a real ellipsis, U+2026)
- Helper text: "Tick chọn role được thấy màn hình tương ứng. Mục không tick role nào = mở cho mọi user đã đăng nhập."
- First column header: "Màn hình"
- Role column headers: "SuperAdmin", "Admin", "User"
- Row labels (seeded): "Dashboard", "Danh mục", "DTI", "Quản trị hệ thống", "Người dùng", "Phân quyền"
- Child-row glyph: "└"
- Checkbox accessible name: "<tên màn hình> — <role>"
- Empty row: "Chưa có mục menu nào."
- Save success toast: "Đã lưu thay đổi phân quyền."
- Error toasts: "Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.", "Bạn không có quyền thực hiện
  thao tác này.", "Đã có lỗi xảy ra. Vui lòng thử lại."

STATES:
- Loading (first paint): the rows list AND the role list are still empty, so the matrix renders its
  empty branch: a header row with ONLY the first column, and one body row holding the empty sentence
  at a single-cell span. There is NO spinner, NO skeleton and NO progress text anywhere on this
  screen — the loading state is visually identical to the empty state, and the only difference is that
  the save button is disabled.
- Populated: as described in the LAYOUT section.
- Disabled: no cell has a permanent disabled treatment. Every checkbox disables together, only while a
  fetch or a save is in flight.
- Dirty (unsaved edits): ticking a box changes local state only; nothing is sent until the save button
  is pressed. There is NO unsaved-changes badge, NO dirty-gated save button and NO navigation guard —
  leaving the page silently discards the edits. Do not add an indicator.
- Saving: the save button switches to its "saving" label and disables, and EVERY checkbox in the
  matrix disables for the duration. The save always sends the complete row set, never a delta.
- Save success: the button returns to its idle label, and a success toast appears bottom-right for
  5 seconds. The sidebar is NOT refreshed — nav visibility only changes after a page reload.
- Error, on the load or the save: the ONLY feedback is an error toast bottom-right. There is no error
  text inside the card and no retry control. A failed load leaves the matrix in the empty-looking
  state above; a failed save leaves the local edits on screen, still dirty, with pressing save again
  as the only way forward.
- Access denied: not a visual state — a signed-in user who is not a SuperAdmin is redirected to the
  dashboard before anything renders. There is no 403 page, no message and no toast on that path.
- Validation: none exists. Every input is a checkbox with two legal values, so there is no inline
  validation, no error styling and no field-level message anywhere in the matrix.

RESPONSIVE:
- Neither the matrix nor the page declares a single media query. Every breakpoint effect below comes
  from the shell.
- 981px and up (desktop default): sidebar fixed at 220px (or 60px collapsed) with the content column
  offset to match; main capped at 1600px with 14px padding; the topbar hamburger is hidden; a collapsed
  sidebar shows submenus as hover/focus flyouts opening to its right. Collapsing the sidebar simply
  lets the matrix reflow wider.
- 980px and below: the content column loses its left offset; the sidebar becomes an off-canvas drawer,
  width min(85vw, 300px), slid fully off-screen until opened, over a dark dismiss backdrop; the topbar
  hamburger appears at 9px padding. The matrix gets WIDER here, not narrower — the full viewport goes
  to the content column.
- 560px and below: main padding drops to 10px; the topbar hides the user's name, leaving only the
  "Đăng xuất" button; the drawer widens to min(90vw, 300px) and its nav items grow to 10px padding /
  40px minimum height. Nothing inside the card changes.
- Horizontal behaviour: because the table has no minimum width, the columns COMPRESS with the
  container — the first column is held at 40% and the role columns share the rest. Horizontal
  scrolling exists in the wrapper but is content-driven: it only engages once the longest untruncated
  label plus one column per role exceeds the container. With the three shipped roles and the seeded
  labels there is normally no horizontal scroll even at 390px. There is NO column collapsing, NO
  card-per-row fallback and NO per-viewport column hiding.
- Vertical behaviour, every viewport: the wrapper caps at 560px tall and scrolls internally once the
  rows exceed it, with the sticky role headers pinned at the top of that scroll box.
- Print: the sidebar, topbar and toast stack disappear; the content column loses its offset and main
  loses its max-width. The card and the matrix DO print — but the wrapper keeps its 560px cap and its
  scrolling, so any row past that height is clipped off the page.

Match the attached screenshot where it conflicts with this text.
```

## Google Stitch

1. Verify the token dictionary lints clean, then import `DESIGN.md` into the Stitch project
   (Design → import design.md):

   ```bash
   npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md
   ```

   Expected: **0 errors** (warnings are recorded as-shipped facts, not blockers). The bare
   `npx @google/design.md lint` form fails silently on Windows — always use the
   `--package=…designmd` form.

2. Paste the **Master Prompt** above verbatim. It is complete on its own: every value in it is a
   literal, so it works whether or not the import succeeded.

3. Generate the screen as **one frame**. If Stitch produces a tab bar, a switcher or a second matrix,
   it invented them — regenerate rather than accepting the frame.

4. Optional, only after a successful import — Stitch resolves the dictionary's own names, so these
   are interchangeable with the literals above: `colors.primary` / `colors.brand` (#0f5bd7) ·
   `colors.bg` (#eef2f8) · `colors.card` (#ffffff) · `colors.tonal-bg` (#dbe7fa) ·
   `colors.tonal-ink` (#0f4a9e) · `colors.muted` (#57647a) · `colors.line` (#dfe6ef) ·
   `colors.border-strong` (#7e91b4) · `colors.surface-table-header` (#f8fafc) ·
   `colors.text-table-header` (#536076) · `rounded.lg` (16px) · `rounded.table` (12px) ·
   `rounded.sm` (7px) · `spacing.card-padding` (14px) · `spacing.cell-padding` (6px 8px) ·
   `typography.table-header` (11px/700) · `typography.table-cell` (12px/400) ·
   `typography.muted-caption` (11px/400) · `components.card` · `components.card-title` ·
   `components.button-primary` · `components.button-tonal` · `components.table-header` ·
   `components.table-cell` · `components.table-row-zebra` · `components.table-wrap` ·
   `components.toast`.

5. Attach `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png`. It shows the entire
   route, so it is a complete reference — there is no second view to imagine. The database behind the
   capture is otherwise empty; the six menu rows it shows are the shipped seed, not sample data.

This repo has no Stitch MCP configured — do the import manually via stitch.withgoogle.com (see
`doc/Design/SETUP.md` to add one).

## Claude Design

Paste the **Master Prompt** above, attach
`Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png`, and add the three notes plus the
token block below. (`Assets/Brand/` does not exist — the shipped app has no logo or brand image file;
the "PM" mark is a styled text square.)

**Note 1 — the screenshot is the whole screen.** It shows the card heading, the helper sentence, and
the 6-row menu tree ("Dashboard", "Danh mục", "└ DTI", "Quản trị hệ thống", "└ Người dùng",
"└ Phân quyền") against the "SuperAdmin" / "Admin" / "User" columns — every "SuperAdmin" box ticked
and **fully enabled**. Nothing sits above the card and nothing sits beside it. If your output has a
tab bar or a second table, it is wrong.

**Note 2 — the capture was taken against an empty database.** The six rows are the shipped seed menu,
not demo data, and there are no user-created rows anywhere. Do not conclude the screen is normally
empty, and do not invent extra menu rows either — the seed is what ships.

**Note 3 — the matrix is a hand-rolled table on purpose.** Do not substitute a data-grid component
with paging, sorting or filtering; and do not add row selection, bulk actions or a toolbar.

Restate the tokens as this CSS block — these are the shipped custom-property names and values,
copied 1:1 from `src/FE/src/styles.scss`, so generated CSS drops straight into the app:

```css
:root {
  --bg: #eef2f8;
  --card: #ffffff;
  --surface-2: #e1e7f1;
  --tonal-bg: #dbe7fa;
  --tonal-bg-hover: #c7dbf5;
  --tonal-ink: #0f4a9e;
  --text: #152033;
  --muted: #57647a;
  --line: #dfe6ef;
  --border-strong: #7e91b4;
  --brand: #0f5bd7;
  --brand2: #174ca8;
  --on-primary: #ffffff;
  --good: #0e7050;
  --good-bg: #d9f2e6;
  --warn: #965e08;
  --warn-bg: #ffedc7;
  --bad: #a02b2b;
  --bad-bg: #fbdcdc;
  --bad-bg-hover: #f5c6c6;
  --bad-border: #e5a8a8;
  --surface-table-header: #f8fafc;
  --text-table-header: #536076;
  --surface-notice: #edf4ff;
  --border-notice: #cfe0ff;
  --shadow: 0 4px 16px rgba(23, 39, 67, 0.1), 0 1px 3px rgba(23, 39, 67, 0.06);
  --fs-xs: 11px;
  --fs-sm: 12px;
  --fs-base: 13px;
  --fs-md: 14px;
  --fs-lg: 15px;
  --sp-1: 4px;
  --sp-2: 6px;
  --sp-3: 8px;
  --sp-4: 10px;
  --sp-5: 14px;
  --radius-sm: 7px;
  --radius-md: 9px;
  --radius-lg: 16px;
  --radius-dialog: 15px;
  --radius-table: 12px;
  --radius-pill: 999px;
  --sidebar-w: 220px;
  --sidebar-w-collapsed: 60px;
  --container-max-width: 1600px;
  /* shipped as literals in this screen's component styles, no custom property declared: */
  /* table wrapper max-height 560px, overflow auto */
  /* checkbox 16px x 16px, accent-color #0f5bd7, focus outline 2px offset 2px */
  /* child row padding-left 28px, "└" glyph margin-right 4px */
  /* first column inline width 40% */
  /* topbar surface rgba(255,255,255,0.95) + blur(10px) */
  /* active sidebar item rgba(15,91,215,0.08) */
  font-family: Inter, 'Segoe UI', Arial, sans-serif; /* Inter loaded from Google Fonts, 400/500/600/700 */
}
```

## Google AI Studio

**System instruction** — paste this block as-is:

```
You generate UI that reproduces an already-shipped screen exactly. Never idealize, never translate,
never add anything the description does not mention.

Product: PlatformManager, an internal Vietnamese-language administration platform. All UI copy is
Vietnamese and hardcoded — there is no i18n layer. Reproduce every string character for character,
including the "…" real ellipsis in "Đang lưu…" and the "└" glyph on child rows.

TOKENS (literal values): brand/primary #0f5bd7 (also the checkbox accent colour and focus outline),
primary hover #174ca8, on-primary #ffffff, page background #eef2f8, card surface #ffffff, ghost
hover tint #e1e7f1, secondary button fill #dbe7fa with text #0f4a9e and hover #c7dbf5, text #152033,
muted #57647a, hairline #dfe6ef, strong table-wrapper border #7e91b4, success #0e7050, danger
#a02b2b, table header surface #f8fafc with text #536076 (also the even-row tint), topbar
rgba(255,255,255,0.95) + blur(10px), active nav item rgba(15,91,215,0.08). Card shadow 0 4px 16px
rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06). Font Inter (loaded from Google Fonts at weights
400/500/600/700) falling back to "Segoe UI", Arial, sans-serif: body 13px/400, topbar h1 15px/bold,
card h2 14px/bold, button 12px/700, table header 11px/700 letter-spacing 0.01em, table cell 12px/400
line-height 1.4, helper paragraph 11px/400 in #57647a, sidebar nav 12px/600 (700 active), toast
12px/400. Radius 7px buttons, 9px nav item and toast, 12px table wrapper, 16px card. Spacing scale
4 / 6 / 8 / 10 / 14px; card and main padding 14px; table cell padding 6px 8px; button padding
6px 8px; child row padding-left 28px. Sidebar 220px (60px collapsed), content max-width 1600px,
table wrapper max-height 560px, breakpoints 980px and 560px. Checkboxes are native, 16x16px.

Fidelity rules for this screen specifically:
- It renders inside the app shell: fixed left sidebar, sticky topbar, centred main, fixed
  bottom-right toast stack. It is not a bare page.
- There is exactly ONE card and ONE matrix. No tab bar, no view switcher, no segmented control, no
  second table, no side pane. The route's template is 19 lines. Do not invent structure.
- Rows are a menu TREE: a parent row then its children indented 28px with a "└" glyph. The
  "SuperAdmin" column is NORMAL and CLICKABLE, with no caption, no lock and no note beneath the
  table.
- The matrix is a hand-rolled HTML table ON PURPOSE. No paging, no sorting, no filtering, no
  virtualisation, no data-grid component. A full checkbox matrix in a 560px-tall scroll box with
  sticky headers.
- This screen's own template contains NO icons at all. Every control is a text button or a native
  checkbox; the only glyph is the literal "└" text character.
- There is no spinner, no skeleton, no unsaved-changes indicator, no confirmation before saving, no
  inline error text and no retry control. Loading looks identical to empty. Do not invent any of it.
```

**User prompt** = the `LAYOUT:`, `COPY:`, `STATES:` and `RESPONSIVE:` sections of the Master Prompt
above, pasted verbatim — including the one-card warning at the top, which is the part most worth
keeping.

**Image part** = `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png`, introduced
with: "Captured against an empty database. This is the entire route — one card, one matrix. Its six
rows are the shipped seed menu."

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable,
Figma AI, …) and attach the screenshot below. The block is self-contained — no token resolution,
no other file and no follow-up prompt are required. Ask the tool for **one** frame, and reject any
output that adds a tab bar, a switcher or a second table.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png` — the only screenshot for this
  screen, and a complete one: the whole route at 1440px wide, sidebar expanded, **empty database** —
  the card heading, the helper sentence and the 6-row seeded menu tree against the "SuperAdmin" /
  "Admin" / "User" columns with an enabled, clickable "SuperAdmin" column.
- `Tokens/tokens.json` — W3C DTCG token file (`global` + `light`; `dark` is intentionally empty, the
  app ships one theme).
- `DESIGN.md` — lint-clean token dictionary, for the Stitch import.
- `Assets/Brand/` — **does not exist**. The app ships no logo or brand image file; the "PM" mark is a
  26x26px square filled #0f5bd7 with white 11px/800 text.
