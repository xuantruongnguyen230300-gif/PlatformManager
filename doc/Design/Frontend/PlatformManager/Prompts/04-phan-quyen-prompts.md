---
kind: luat
scope: du-an
verified: 2026-09-08
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
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
     hex/px/font value, and no other spec has to be opened. -->

> **Regenerated 2026-08-29 — the previous pack would have produced the wrong screen, twice over.**
> It carried the pre-2026-08-29 palette (`#eef2f8`, `#dfe6ef`, `#57647a`, `#7e91b4`, `#f8fafc`),
> named `Inter` as the type face, and — most damaging — it *forbade* the view switcher and the
> second matrix in bold, repeated warnings. Both now ship: `phan-quyen.page.ts` imports
> `ResourcePermissionMatrix` and the card opens with a `.segmented` switcher. The old warnings were
> correct when written and are wrong now; they have been replaced, not softened.

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is a literal. -->

```
Recreate this exact shipped screen — do not idealize, do not translate, do not "improve" anything.

CONTEXT: an internal Vietnamese-language administration platform ("PlatformManager"). This screen is
Permissions ("Phân quyền"), reachable only by a SuperAdmin — every other signed-in user is silently
redirected away, so there is no 403 page to draw. It renders INSIDE the app shell (fixed left
sidebar + sticky topbar + centred main + fixed toast stack), unlike the sign-in / change-password
screens which have no shell.

⚠ THE PAGE IS ONE CARD CONTAINING A TWO-SEGMENT VIEW SWITCHER AND, BELOW IT, EXACTLY ONE OF TWO
CHECKBOX MATRICES — never both at once. The unselected matrix is not in the DOM at all: no second
panel, no side-by-side pane, no accordion. Draw ONE card and ONE matrix; if you need to show the
other matrix, draw a SECOND, SEPARATE frame of the same card.

The two matrices look almost identical and mean opposite things. Reproduce both meanings exactly:
- "Theo màn hình" (by screen) — a role x MENU tree. A row with no box ticked is OPEN to every
  signed-in user.
- "Theo tài nguyên" (by resource) — a role x ACTION flat list. A row with no box ticked is DENIED
  to everyone.

Both matrices are hand-rolled HTML tables ON PURPOSE — not data-grid components. No paging, no
sorting, no filtering, no column resizing, no row virtualisation, no row selection, no toolbar. Each
is a full checkbox matrix inside a screen-relative scroll box. Do not substitute a data grid.

PALETTE (recomputed from the WCAG contrast formula, not picked by eye — use these exact values):
page background #cfdaea; card + input surface #ffffff; ghost-button hover tint #c1cde2; default
(secondary) button fill #c4d8f6 with text #0f4a9e and hover fill #c7dbf5; body text #152033; muted
text #4c576b; component hairline #7a97bd (card border, segmented-group border and divider, table
row rules — this measures 3.00:1 on white, it is a REAL visible border, not a faint one); stronger
border #6077a2 reserved for inputs and the table wrapper; brand #0f5bd7 with hover #174ca8 and white
text on it; success #0e7050 on #d9f2e6; warning #965e08 on #ffedc7; danger #a02b2b on #fbdcdc; table
header surface #e9eff6 with header text #536076 (the same #e9eff6 tints every even body row); the
notice bar fill is #c4d8f6 with a #0f5bd7 left rule; topbar surface rgba(255,255,255,0.95) with
backdrop-filter blur(10px); drawer backdrop rgba(20,28,40,0.45); active sidebar item tint
rgba(15,91,215,0.08).

SHADOWS: cards and the sidebar drawer 0 4px 16px rgba(23,39,67,0.1) + 0 1px 3px rgba(23,39,67,0.06);
secondary button hover 0 3px 10px rgba(23,39,67,0.1); primary button hover
0 8px 20px rgba(15,91,215,0.35); toast 0 14px 38px rgba(23,39,67,0.26). Every control on this screen
focuses with outline: 2px solid #0f5bd7, offset 2px — EXCEPT a segment of the view switcher, whose
ring is offset -2px (inset), because the group clips overflow and an outward ring would be cut off.

FONT: 'Be Vietnam Pro', falling back to 'Segoe UI', Arial, sans-serif — self-hosted, weights 400,
500, 600, 700, 800. Base 13px. Type scale, all in that family: page title in the topbar 15px/700;
card heading 14px/700; segment label 12px/700; button label 12px/700; table column header 11px/700
with letter-spacing 0.01em; table cell 12px/400 line-height 1.4; the explainer paragraphs and the
break-glass captions 11px/400 in #4c576b; notice bar text 12px/400; sidebar nav item 12px/600 (700
when active); sidebar brand text 14px/800; brand mark 11px/800; toast title 12px/800; toast body
12px/400 line-height 1.45.

RADII: 7px buttons and the segmented group; 9px sidebar nav item, notice bar and toast; 12px the
table wrapper; 16px the card. The checkboxes are native, unstyled apart from their accent colour.

SPACING SCALE (the whole app uses only these five steps): 4 / 6 / 8 / 10 / 14px.

LAYOUT — app shell:
- Sidebar: fixed left, 220px wide, full viewport height, background #ffffff, 1px right border
  #7a97bd, z-index 35. Brand row (padding 10px, min-height 50px, 1px bottom border #7a97bd): a
  26x26px square filled #0f5bd7 with white "PM" in 11px/800, then "PlatformManager" in 14px/800,
  then a 24x24px transparent icon-only button holding a left-chevron that collapses the rail to
  60px. Nav items below: label 12px/600 in #152033 next to an 18px icon box whose glyph is #4c576b,
  padding 6px 8px, gap 8px, radius 9px; hover fills #cfdaea; the active item fills
  rgba(15,91,215,0.08), turns both label and glyph #0f5bd7 at weight 700, and grows a 3px
  brand-coloured rail on its left edge. Group items carry a trailing 12px down-chevron. The menu is
  server-driven; the shipped seed is "Trang chủ" (home glyph) and the group "Quản trị hệ thống" (cog
  glyph) containing "Người dùng" (user glyph) and "Phân quyền" (shield glyph). ON THIS ROUTE the
  group is open and "Phân quyền" is the active item.
- Topbar: sticky at the top of the content column, z-index 20, background rgba(255,255,255,0.95)
  with backdrop-filter blur(10px), 1px bottom border #7a97bd. Inner row max-width 1600px, centred,
  padding 10px 14px, gap 8px: a hamburger button (visible only ≤980px), the page title "Phân quyền"
  in 15px/700, then pushed right the signed-in user's full name in 12px/700 and a secondary button
  reading "Đăng xuất" with a leading sign-out glyph.
- Main: max-width 1600px, centred, padding 14px, on the #cfdaea page background.
- Toast stack: fixed 14px from the right and bottom edges, z-index 60, max-width min(400px, 90vw),
  8px gap, aria-live polite. Each toast: white card, radius 9px, 1px border #7a97bd, a 5px left edge
  in the severity colour, shadow 0 14px 38px rgba(23,39,67,0.26), padding 10px 14px, holding a 22px
  circular severity disc (11px glyph, tinted fill + ink of that severity), a 12px/800 optional title
  over 12px/400 body text at line-height 1.45, and a 24px transparent close button on the right.
  Auto-dismisses after 5 seconds.

LAYOUT — the page (everything below sits in ONE card: #ffffff, 1px border #7a97bd, radius 16px,
padding 14px, card shadow; the card is the first and only element inside main):

1. VIEW SWITCHER, at the very top of the card, left-aligned, 10px below it before anything else:
   an inline-flex group, 1px border #7a97bd, radius 7px, overflow hidden, sized to its content — it
   does NOT stretch across the card. It holds exactly two text segments, with a 1px #7a97bd vertical
   divider between them and no radius of their own (the group's clipped corners are what round it).
   Each segment: no border, padding 6px 10px, 12px/700, text #4c576b on a #ffffff fill; hover fills
   #cfdaea; the SELECTED segment fills #0f5bd7 with #ffffff text and does not react to hover.
   When a matrix has unsaved edits, its own segment's label grows a suffix " • chưa lưu" — real
   text in the same colour at weight 400 and 85% opacity, NOT a dot, NOT a badge, NOT an icon. Both
   segments can carry it at once.

2. Then ONE of the two sections below, never both.

SECTION A — segment "Theo màn hình" selected:
2a. Title row (flex, space-between, gap 8px, margin-bottom 10px): "Phân quyền màn hình" in 14px/700
    on the left; on the right a primary button (fill and border #0f5bd7, white 12px/700, radius 7px,
    padding 6px 8px) reading "Lưu thay đổi". It is disabled — opacity 0.5, cursor not-allowed —
    while the matrix is loading or saving.
2b. An explainer paragraph directly beneath: 11px/400 in #4c576b, block, 10px bottom margin. Its
    wording is in COPY; it states the open-by-default rule.
2c. THE MENU MATRIX in a scroll box: 1px border #6077a2, radius 12px, overflow auto, height
    calc(100dvh - 280px) with a 220px floor — a screen-relative height, NOT a fixed pixel box.
    Inside, a plain HTML table at width 100%, collapsed borders, #ffffff fill, and NO minimum width,
    so the columns compress with the container rather than being pinned.
    • Header row: the first cell is fixed at width 40% and reads "Màn hình"; then one cell per role,
      right-aligned with tabular numerals. Every header cell is sticky to the top of the scroll box
      at z-index 4, background #e9eff6, text #536076, 11px/700, letter-spacing 0.01em, padding
      6px 8px, left-aligned unless it is a role column.
    • Body rows: cells 12px/400 line-height 1.4, padding 6px 8px, 1px bottom border #7a97bd,
      top-aligned; even rows tinted #e9eff6; the hovered row tints #cfdaea. Each role cell is
      right-aligned and holds one native 16x16px checkbox with accent colour #0f5bd7.
    • Rows are a MENU TREE, flattened parent-first: every parent row is immediately followed by its
      own children. A child row is left-padded 28px and prefixed with a muted "└" glyph in #4c576b
      sitting 4px before the label. Indent is a single level regardless of depth.
    • The shipped seeded rows, in this order: "Trang chủ" · "Quản trị hệ thống" · "└ Người dùng" ·
      "└ Phân quyền". Role columns, in the order the API returns them: "SuperAdmin", "Admin", "User".
    • In THIS matrix the "SuperAdmin" column is a completely ORDINARY column: clickable, enabled,
      ticked only where the data says so. No caption under the header, no lock, no note under the
      table.
    • When there are no rows, a single muted cell spans every column and holds the empty sentence
      from COPY.

SECTION B — segment "Theo tài nguyên" selected:
2d. Title row, same geometry: "Phân quyền theo tài nguyên" in 14px/700, and the same primary "Lưu
    thay đổi" button on the right — but this one is ALSO disabled whenever there are no unsaved
    edits, and while disabled it carries the tooltip from COPY. Draw it disabled by default.
2e. An explainer paragraph, 11px/400 in #4c576b, in which the clause "không tick role nào = từ chối
    tất cả" is bold (weight 700, same size and colour).
2f. A NOTICE BAR, full width, below the paragraph and above the table: flex row, align-items
    flex-start, gap 8px, fill #c4d8f6, 1px border #7a97bd, a 4px LEFT border in #0f5bd7, radius 9px,
    padding 8px 14px, 12px/400 text, 14px bottom margin. It opens with a #0f5bd7 info-in-circle
    glyph, nudged 2px down, and has NO close button.
2g. THE RESOURCE MATRIX, in an identical scroll box with identical table, header and row treatment.
    Differences from the menu matrix, all of them meaningful:
    • First column header reads "Tài nguyên" and the rows are a FLAT LIST — no tree, no indent, no
      "└" glyph.
    • The "SuperAdmin" column header carries a SECOND LINE beneath the role name: "luôn có toàn
      quyền" as a block, 2px below, 11px/400 in #4c576b, weight 400, no letter-spacing, no
      uppercase, never wrapping.
    • Every checkbox in the "SuperAdmin" column is rendered TICKED and DISABLED (cursor not-allowed,
      60% opacity) on every row. The other two columns are ordinary.
    • Below the table, a muted footnote paragraph, 11px/400 in #4c576b, line-height 1.5, 8px above,
      with the two occurrences of "SuperAdmin" in bold. Its wording is in COPY.
    • Today the API returns exactly ONE row: "Import CSV/Excel". Draw one row. Do not invent a
      longer list to make the table look fuller.

COPY (verbatim Vietnamese — reproduce character for character. Since 2026-09-06 these strings no
longer live in the templates: they come from `src/FE/public/i18n/vi.json` via translation keys, or
straight from the API. That changes nothing for you — reproduce the Vietnamese exactly as given
below; it is what ships):
- Route/topbar title: "Phân quyền"
- Sidebar: "PM", "PlatformManager"; collapse button aria-label "Mở rộng menu" / "Thu gọn menu"; nav
  labels "Trang chủ", "Quản trị hệ thống", "Người dùng", "Phân quyền"
- Topbar: hamburger aria-label "Mở menu điều hướng"; logout button title and label "Đăng xuất"
- Toast close aria-label: "Đóng thông báo"
- View switcher group aria-label (not visible): "Chọn loại phân quyền"
- Segment labels: "Theo màn hình", "Theo tài nguyên"
- Unsaved-edits suffix on a segment: " • chưa lưu" (leading space and bullet included)
- Section A heading: "Phân quyền màn hình"
- Save button, both sections: "Lưu thay đổi", becoming "Đang lưu…" while saving (a real ellipsis,
  U+2026)
- Section A explainer: "Tick chọn role được thấy màn hình tương ứng. Mục không tick role nào = mở cho mọi user đã đăng nhập."
- Section A first column header: "Màn hình"
- Section A row labels (seeded): "Trang chủ", "Quản trị hệ thống", "Người dùng", "Phân quyền"
- Section A child-row glyph: "└"
- Section A empty row: "Chưa có mục menu nào."
- Section B heading: "Phân quyền theo tài nguyên"
- Section B save-button tooltip while disabled: "Chưa có thay đổi nào để lưu"
- Section B explainer: "Tick chọn role được thực hiện hành động tương ứng. Khác hẳn tab \"Theo màn hình\": ở đây không tick role nào = từ chối tất cả, chứ không phải mở cho mọi người."
- Section B notice bar: "Hiện chưa có API nghiệp vụ nào áp dụng các quyền này, nên thay đổi ở đây được lưu lại nhưng chưa chặn hay mở thêm thao tác nào cho người dùng."
- Section B first column header: "Tài nguyên"
- Section B break-glass column caption: "luôn có toàn quyền"
- Section B row label (the only one today): "Import CSV/Excel"
- Section B footnote: "Cột SuperAdmin luôn được tick và không sửa được ở đây: role này mặc định có mọi quyền với mọi tài nguyên, nên bỏ tick cũng không thu hồi được gì. Muốn một người không còn toàn quyền, vào \"Quản trị hệ thống → Người dùng\" và gỡ role SuperAdmin khỏi tài khoản của họ."
- Section B empty row: "Chưa có tài nguyên nào."
- Role column headers, both sections: "SuperAdmin", "Admin", "User"
- Checkbox accessible name: "<tên dòng> — <role>", extended in section B's SuperAdmin column to
  "<tên dòng> — SuperAdmin — luôn có quyền, không thay đổi được"
- Save success toasts: "Đã lưu thay đổi phân quyền." (section A) and "Đã lưu phân quyền theo tài
  nguyên." (section B)
- Error toasts, title then body: "Mất kết nối" / "Không thể kết nối tới máy chủ. Kiểm tra kết nối
  mạng."; "Không đủ quyền" / "Bạn không có quyền thực hiện thao tác này."; "Lỗi hệ thống" / "Đã có
  lỗi xảy ra. Vui lòng thử lại."

STATES:
- First paint: section A is selected. Its rows AND role list are still empty, so the matrix renders
  its empty branch — a header row with ONLY the first column, and one body row holding the empty
  sentence at a single-cell span. There is NO spinner, NO skeleton and NO progress text anywhere on
  this screen; loading is visually identical to empty, and the only difference is the disabled save
  button.
- Section B loads lazily, the first time its segment is pressed, and never refetches on later
  switches. Until then it has never been requested; its own loading state looks the same as A's.
- Populated: as described in the LAYOUT sections above.
- Dirty (unsaved edits): ticking a box changes local state only; nothing is sent until that
  section's own save button is pressed. The ONLY indicator is the " • chưa lưu" suffix on the
  owning segment, which stays visible while the other section is showing. There is NO badge, NO
  banner and NO navigation guard — leaving the page silently discards the edits.
- Saving: the pressed section's button switches to "Đang lưu…" and disables, and EVERY checkbox in
  THAT matrix disables for the duration. The other section is untouched. Each save always sends the
  complete row set of its own matrix, never a delta, and never touches the other table.
- Save success: the button returns to its idle label, the segment's " • chưa lưu" suffix disappears,
  and a success toast appears bottom-right for 5 seconds. After a section-A save the sidebar menu is
  refetched, so nav visibility updates without a reload; after a section-B save it deliberately is
  not.
- Error, on any load or save: the ONLY feedback is an error toast bottom-right. There is no error
  text inside the card and no visible retry control. A failed load leaves the matrix in the
  empty-looking state above; a failed save leaves the local edits on screen, still marked unsaved.
- Access denied: not a visual state — a signed-in user who is not a SuperAdmin is redirected to the
  home route before anything renders. There is no 403 page, no message and no toast on that path.
- Validation: none exists. Every input is a checkbox with two legal values, so there is no inline
  validation, no error styling and no field-level message in either matrix.

RESPONSIVE:
- Neither matrix nor the page declares a single media query. Every breakpoint effect below comes
  from the shell.
- 981px and up (desktop default): sidebar fixed at 220px (or 60px collapsed) with the content column
  offset to match; main capped at 1600px with 14px padding; the topbar hamburger is hidden; a
  collapsed sidebar shows submenus as hover/focus flyouts opening to its right. Collapsing the
  sidebar simply lets the matrix reflow wider.
- 980px and below: the content column loses its left offset; the sidebar becomes an off-canvas
  drawer, width min(85vw, 300px), slid fully off-screen until opened, over the
  rgba(20,28,40,0.45) dismiss backdrop; the topbar hamburger appears. The matrix gets WIDER here,
  not narrower — the full viewport goes to the content column.
- 560px and below: main padding drops to 10px; the topbar hides the user's name, leaving only the
  "Đăng xuất" button; the drawer widens and its nav items grow for touch. NOTHING inside the card
  changes — in particular the view switcher keeps its intrinsic width and does not stretch, because
  it is a direct child of the card and not of a toolbar.
- Horizontal behaviour: because neither table has a minimum width, the columns COMPRESS with the
  container — the first column is held at 40% and the role columns share the rest. Horizontal
  scrolling exists in the wrapper but is content-driven: it only engages once the longest
  untruncated label plus one column per role exceeds the container. With the three shipped roles and
  today's short labels there is normally no horizontal scroll even at 390px. There is NO column
  collapsing, NO card-per-row fallback and NO per-viewport column hiding.
- Vertical behaviour, every viewport: the wrapper is calc(100dvh - 280px) tall with a 220px floor
  and scrolls internally once the rows exceed it, with the sticky role headers pinned at the top of
  that scroll box.
- Print: the sidebar, topbar and toast stack disappear; the content column loses its offset and main
  loses its max-width. The card and the SELECTED matrix do print — but the wrapper keeps its height
  cap and its scrolling, so any row past that height is clipped, and the unselected section prints
  nothing at all because it is not in the DOM.
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

3. Generate **two frames of the same card** — one with "Theo màn hình" selected, one with "Theo tài
   nguyên" selected. Asking for a single frame that shows both matrices produces a screen that does
   not exist. Reject any output that renders the two matrices simultaneously, or that turns the
   two-segment switcher into browser-style tabs with a bottom underline.

4. Optional, only after a successful import — Stitch resolves the dictionary's own names, so these
   are interchangeable with the literals above: `colors.primary` / `colors.brand` (#0f5bd7) ·
   `colors.bg` (#cfdaea) · `colors.card` (#ffffff) · `colors.tonal-bg` (#c4d8f6) ·
   `colors.tonal-ink` (#0f4a9e) · `colors.btn-hover-bg` (#c7dbf5) · `colors.muted` (#4c576b) ·
   `colors.line` (#7a97bd) · `colors.border-strong` (#6077a2) · `colors.on-primary` (#ffffff) ·
   `colors.surface-table-header` (#e9eff6) · `colors.th-ink` (#536076) · `rounded.sm` (7px) ·
   `rounded.md` (9px) · `rounded.lg` (16px) · `rounded.table` (12px) · `spacing.card-padding` (14px)
   · `spacing.cell-padding` (6px 8px) · `spacing.notice-padding` (8px 14px) ·
   `typography.segmented-label` (12px/700) · `typography.table-header` (11px/700) ·
   `typography.table-cell` (12px/400) · `typography.muted-caption` (11px/400) ·
   `typography.notice` (12px/400) · `components.card` · `components.card-title` ·
   `components.button-primary` · `components.table-header` · `components.table-cell` ·
   `components.table-wrap` · `components.toast`.

   > ⚠️ Four token names changed on 2026-08-29. If you are working from an older prompt or an older
   > Stitch project, replace `tonal-bg-hover` → `btn-hover-bg`, `bad-bg-hover` → `danger-hover-bg`,
   > `text-table-header` → `th-ink`, and both `bad-border-btn` and `bad-border-notice` →
   > `danger-border`.

5. **Do not attach the stored screenshot to Stitch.**
   `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png` was captured 2026-08-22, before
   the view switcher and the resource matrix existed and before the palette change. Feeding it to a
   generator will override the correct literals above with a picture of a screen that no longer
   ships. Generate from the Master Prompt text alone until the screen is recaptured.

This repo has no Stitch MCP configured — do the import manually via stitch.withgoogle.com (see
`doc/Design/SETUP.md` to add one).

## Claude Design

Paste the **Master Prompt** above and add the three notes plus the token block below. Attach
**nothing**: `Assets/Brand/` does not exist (the shipped app has no logo or brand image file; the
"PM" mark is a styled text square), and the only screenshot on disk is stale — see § Google Stitch
step 5 and § Assets to Attach.

**Note 1 — no reference image is available for this screen right now.** The generator has to work
from the prompt text. That is a deliberate instruction, not an oversight: the stored capture predates
both the view switcher and the palette change. The Master Prompt is written to be complete without
it — every colour, size, radius and string is a literal.

**Note 2 — draw the card twice, not once.** One frame per selected segment. The unselected matrix is
removed from the DOM, so a frame showing both is a screen the app cannot render.

**Note 3 — the two matrices are hand-rolled tables on purpose.** Do not substitute a data-grid
component with paging, sorting or filtering; do not add row selection, bulk actions or a toolbar; and
do not "helpfully" make the disabled SuperAdmin column in section B editable.

Restate the tokens as this CSS block — these are the shipped custom-property names and values,
copied 1:1 from `src/FE/src/styles.scss`, so generated CSS drops straight into the app:

```css
:root {
  --bg: #cfdaea;
  --card: #fff;
  --surface-2: #c1cde2;
  --tonal-bg: #c4d8f6;
  --tonal-ink: #0f4a9e;
  --text: #152033;
  --muted: #4c576b;
  --line: #7a97bd;
  --border-strong: #6077a2;
  --brand: #0f5bd7;
  --brand2: #174ca8;
  --good: #0e7050;
  --good-bg: #d9f2e6;
  --warn: #965e08;
  --warn-bg: #ffedc7;
  --bad: #a02b2b;
  --bad-bg: #fbdcdc;
  --shadow: 0 4px 16px rgba(23,39,67,.1), 0 1px 3px rgba(23,39,67,.06);

  --on-primary: #fff;
  --btn-hover-bg: #c7dbf5;
  --danger-border: #e0a8a8;
  --danger-hover-bg: #f5c6c6;
  --th-ink: #536076;
  --surface-track: #dbe4f0;
  --surface-table-header: #e9eff6;

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
  --grid-h: calc(100dvh - 280px);
  --grid-h-min: 220px;
}

body {
  font-family: 'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif; /* self-hosted, weights 400-800 */
  background: var(--bg);
  color: var(--text);
  font-size: var(--fs-base);
}
```

Values used on this screen that are **not** custom properties and must be written as literals: the
topbar fill `rgba(255,255,255,0.95)`, the drawer backdrop `rgba(20,28,40,0.45)`, the active sidebar
tint `rgba(15,91,215,0.08)`, the 16×16px checkbox, the 28px child-row indent, the 4px gap after the
"└" glyph, the 2px offset of the break-glass caption, and the inline `width:40%` on the first column
of both tables.

## Google AI Studio

**System instruction** = the `CONTEXT:`, `PALETTE:`, `SHADOWS:`, `FONT:`, `RADII:` and
`SPACING SCALE:` sections of the Master Prompt above, pasted verbatim. They are the invariant part —
they describe the design system, not this screen. Append this paragraph, which is specific to this
screen and is the part most worth keeping:

```
This screen has a two-segment view switcher and two alternative checkbox matrices, one visible at a
time. Never render both matrices in one frame. Never turn the switcher into underlined browser tabs.
Never add a spinner, a skeleton, an unsaved-changes banner, a confirmation dialog, an in-card error
message or a retry button — none of them exists. The only unsaved-changes signal in the entire
screen is the literal text " • chưa lưu" appended inside a segment's label.
```

**User prompt** = the `LAYOUT:`, `COPY:`, `STATES:` and `RESPONSIVE:` sections of the Master Prompt
above, pasted verbatim — run it twice, once asking for section A and once for section B.

**Image part** = none. Do not attach
`Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png`; it predates the view switcher and
the palette change, and a multimodal model will trust the picture over the text. Add this instead:
"No reference image is supplied. Follow the written specification exactly; every colour, size and
string in it is literal."

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable,
Figma AI, …). The block is self-contained — no token resolution, no other file, no follow-up prompt
and no reference image are required. Ask for **two** frames of the same card, one per segment, and
reject any output that shows both matrices at once or replaces the segmented control with tabs.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Tokens/tokens.json` — W3C DTCG token file (`global` + `light`; `dark` is intentionally empty, the
  app ships one theme).
- `DESIGN.md` — lint-clean token dictionary, for the Stitch import.
- `Assets/Screenshots/phan-quyen/permission-matrix--desktop-1440.png` — **do not attach.** Captured
  2026-08-22, before the `.segmented` view switcher and the resource matrix existed and before the
  palette and font change. It shows the menu matrix alone in a tab-less card. It is kept on disk for
  before/after comparison and is cited as stale in `UiInventory.md` § Screenshot Manifest and in
  `Screens/04-phan-quyen.md` § Screenshots. Once both segments are recaptured, delete this bullet and
  restore the shots as normal attachments.
- `Assets/Brand/` — **none exist**. The app ships no logo or brand image file; the "PM" mark is a
  26x26px square filled #0f5bd7 with white 11px/800 text.
