---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
screen_ref: "02-danh-muc-dti"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — DTI Catalogue (`/danh-muc/dti`)

<!-- One pack for Screens/02-danh-muc-dti.md. Token values resolved to literals from
     src/FE/src/styles.scss (:root) via Tokens/colors.md + Tokens/spacing.md +
     Tokens/typography.md + DESIGN.md frontmatter. Copy is verbatim Vietnamese from the
     shipped Angular templates. Fidelity rule: reproduce the app AS-SHIPPED — quirks
     included, nothing idealized, nothing translated.

     Everything a tool needs is inside THIS file: every token is resolved to a literal
     hex/px/font value, and no other spec has to be opened. -->

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is a literal. -->

```
Recreate this exact shipped screen — do not idealize, do not translate, do not "improve" anything.

CONTEXT: an internal Vietnamese-language administration platform ("PlatformManager"). This screen is
the DTI Catalogue — the ONLY data-entry surface in the whole product. Everything else is read-only.
It renders INSIDE the app shell (fixed left sidebar + sticky topbar + centred main + fixed toast
stack), unlike the sign-in / change-password screens which have no shell.

TOKENS (literal values — use these exact numbers and hex codes):
Colors: brand/primary #0f5bd7; primary hover #174ca8; text/icon on primary #ffffff; page background
#eef2f8; card + dialog + input surface #ffffff; ghost-button hover tint #e1e7f1; default (secondary)
button fill #dbe7fa with text #0f4a9e and hover fill #c7dbf5; body text #152033; muted text #57647a;
faint hairline #dfe6ef (cards, table rules — never on interactive controls); strong border #7e91b4
(inputs, selects and table wrappers ONLY); success #0e7050 on #d9f2e6; warning #965e08 on #ffedc7;
danger #a02b2b on #fbdcdc, danger hover fill #f5c6c6, danger border #e5a8a8; table header surface
#f8fafc with header text #536076 (the same #f8fafc also tints every even body row); notice banner
surface #edf4ff with border #cfe0ff; topbar surface rgba(255,255,255,0.95) plus backdrop-filter
blur(10px); dialog backdrop rgba(20,28,40,0.45); active sidebar item background rgba(15,91,215,0.08).
Shadows: card / toast / sidebar drawer 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06);
dialog 0 24px 70px rgba(0,0,0,0.25); secondary button hover 0 3px 10px rgba(23,39,67,0.1); primary
button hover 0 8px 20px rgba(15,91,215,0.35).
Font: Inter, loaded for real from Google Fonts (weights 400, 500, 600, 700), with the fallback stack
"Segoe UI", Arial, sans-serif. Sizes/weights: body 13px/400; topbar h1 15px/bold; card h2 14px/bold;
button label 12px/700; row-action button label 11px/700; table header 11px/700 letter-spacing 0.01em;
table cell 12px/400 line-height 1.4; muted caption 11px/400; dialog form label 12px/700; sidebar nav
item 12px/600 (700 when active); sidebar brand text 14px/800; brand mark 11px/800; toast text 12px/400
line-height 1.4; confirm-dialog message 13.5px line-height 1.55; import-result summary 12px
line-height 1.6.
Radius: 7px buttons, inputs, selects, toast close; 9px sidebar nav item and toast item; 16px cards;
15px dialogs; 12px notice banner; 999px pills; 6px inline-edit icon buttons.
Spacing scale in use: 4px, 6px, 8px, 10px, 14px. Card padding 14px. Dialog padding 14px. Main padding
14px (10px below 560px). Topbar inner padding 10px 14px. Table cell padding 6px 8px. Button padding
6px 8px. Row-action button padding 4px 6px plus 4px right margin. Input/select padding 6px 8px.
Notice padding 8px 14px. Title row gap 8px, margin-bottom 10px. Filters row gap 8px, margin 10px 0.
Form row gap 6px, margin-bottom 10px. Dialog action row gap 8px.
Structure: sidebar 220px wide (60px collapsed); content column max-width 1600px, centred; breakpoints
tablet 980px and mobile 560px; z-index topbar 20, sidebar backdrop 34, sidebar 35, collapsed-sidebar
flyout 40, toast stack 60, sticky table header 4.
Icons: PrimeIcons v7 (`<i class="pi pi-…">`), loaded globally. Every icon named below is a PrimeIcon.

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
  route the "Danh mục" group is open and its "DTI" child is the active item.
- Content column: offset 220px from the left (0 when collapsed to 60px it is offset 60px).
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

PAGE (everything below sits in ONE card: #ffffff, 1px border #dfe6ef, radius 16px, padding 14px,
card shadow; flex column)
1. Title row (space-between, gap 8px, margin-bottom 10px): h2 "Danh mục & Đánh giá theo tuần" at
   14px/bold on the left; a muted 11px #57647a record count "<n> bản ghi" on the right.
2. Read-only notice banner — CONDITIONAL, present only when the grid is NOT in live/editable mode:
   a pale blue box, background #edf4ff, 1px border #cfe0ff, radius 12px, padding 8px 14px, 12px text,
   14px bottom margin, containing one sentence of explanation.
3. Filters row (flex, wrap, 8px gap, 10px vertical margin; hidden when printing) — four controls, all
   with a 1px #7e91b4 border, radius 7px, white fill, padding 6px 8px, 12px text:
   a) a free-text search input that grows to fill the row (flex:1, min-width 220px), 300 ms debounce;
   b) a group select whose first option is the "all groups" default, the rest server-supplied;
   c) a year select;
   d) a period select whose first option is the "everything, latest per criterion" default, the rest
      ISO week keys saved in that year.
   Then, pushed right (margin-left:auto) — CONDITIONAL, present only in live/editable mode — an action
   pair with 8px gap: a secondary button "Import CSV/Excel" (fill #dbe7fa, text #0f4a9e) and a primary
   button "+ Thêm chỉ tiêu" (fill #0f5bd7, white text).
4. THE GRID — a 12-column data grid, horizontally scrollable, with server-side paging. Header cells
   are sticky, background #f8fafc, text #536076, 11px/700. Body cells 12px/400, padding 6px 8px, 1px
   bottom border #dfe6ef; even rows tinted #f8fafc; the hovered row tints #eef2f8. Column min-widths,
   left to right — combined minimum width 1430px, so the grid ALWAYS scrolls horizontally on anything
   narrower and never collapses or stacks:
     1. "Mã" 70px — FROZEN to the left edge, value in bold
     2. "Tên" 220px
     3. "Nhóm" 120px — rendered "<group code>. <group name>"
     4. "Điểm tối đa" 90px — right-aligned, tabular numerals
     5. "Tự đánh giá" 90px — right-aligned
     6. "Thẩm định" 90px — right-aligned
     7. "Trạng thái" 110px — PLAIN TEXT, not a badge
     8. "Phụ trách" 110px
     9. "Hạn xử lý" 100px — dd/mm/yyyy, vi-VN locale
    10. "Tiến độ %" 130px — right-aligned, EDITABLE INLINE
    11. "Ghi chú" 180px — EDITABLE INLINE
    12. "Hành động" 120px — FROZEN to the right edge
   Numbers use the vi-VN locale: scores 2 decimals, progress 1 decimal followed by "%", and every
   absent value in columns 4-11 renders as an em dash "—".
   INLINE EDIT (columns 10 and 11 only): in read mode the value sits in a span that shows a dashed
   #0f5bd7 underline on hover and is entered by DOUBLE-CLICK (not a single click) or by pressing Enter
   while it is keyboard-focused. Exactly one cell in the whole grid can be editing at a time. In edit
   mode the cell becomes a 6px-gap flex row: an auto-focused input (1px #7e91b4 border, radius 7px,
   5px padding, 12px text — the progress input is exactly 74px wide, right-aligned, a number field
   with its native spinners suppressed; the note input fills the cell with a 130px minimum) followed
   by two 24x24px ghost icon buttons, radius 6px: a confirm `pi-check` in #0e7050 (hover fill #d9f2e6)
   and a cancel `pi-times` in #a02b2b (hover fill #fbdcdc). Enter confirms, Escape cancels.
   ACTION CELL (column 12): in live/editable mode it holds two ghost text buttons — "Sửa" in #57647a
   and "Xoá" in #a02b2b — each 11px/700, padding 4px 6px, radius 7px, transparent until hover (Sửa
   hovers to fill #e1e7f1 with #152033 text; Xoá hovers to fill #fbdcdc keeping #a02b2b text), with
   4px between them. When the row is NOT editable the cell renders a single muted em dash "—" instead.
   READ-ONLY RULE — reproduce this exactly, it is the point of the screen: write controls appear ONLY
   while the user is viewing "Tất cả (mới nhất trong năm)" of the CURRENT year. In every other
   combination (a past year, or one specific saved week) the notice banner appears, the
   "Import CSV/Excel" + "+ Thêm chỉ tiêu" pair is REMOVED from the DOM entirely (not greyed out), the
   two editable cells render as plain text with no click affordance, and the action cell shows "—".
   EMPTY STATE: one row spanning all 12 columns holding a single muted sentence. Paging controls still
   render, and the record count still reads "0 bản ghi".
   PAGINATOR: below the grid — first / previous / numbered pages / next / last buttons plus a
   rows-per-page select offering 10, 20 and 50, defaulting to 20. It is NOT hidden when printing.
5. FOUR MODAL DIALOGS belong to this route (native centred modals over the rgba(20,28,40,0.45)
   backdrop, radius 15px, padding 14px, shadow 0 24px 70px rgba(0,0,0,0.25)):
   a) Criteria form, width min(560px, 92vw) — a title row (h2 + a secondary "Đóng" button), then the
      field "Mã" (max 20 chars), the field "Tên chỉ tiêu" (a textarea, min-height 64px, vertical
      resize), then a two-column 1fr/1fr grid with 14px column gap holding "Nhóm" (a select) and
      "Điểm tối đa" (a number field); every label is 12px/700 followed by a red #a02b2b asterisk; the
      dialog-tier inputs use a FAINTER 1px #dfe6ef border (not #7e91b4), radius 7px, padding 6px 8px,
      full width. Below them a single red #a02b2b 12px error line appears only when there is a message,
      then a right-aligned action row with 8px gap: secondary "Huỷ" + primary "Lưu chỉ tiêu".
   b) Delete confirmation, width min(420px, 92vw) — a title row with h2 only (NO close button), a
      13.5px/1.55 message paragraph, then a right-aligned action row (8px gap, 16px top margin):
      secondary "Huỷ" + a DANGER button "Xoá" filled #fbdcdc with #a02b2b text (hover fill #f5c6c6).
   c) Import picker, width min(560px, 92vw) — title row (h2 + "Đóng"), one form row with a label and
      a native file input accepting .csv/.xlsx/.xls, a muted line naming the chosen file once one is
      picked, and a right-aligned action row (8px gap, 12px top margin): secondary "Huỷ" + a primary
      button that is disabled (opacity 0.5, cursor not-allowed) until a file is chosen and while the
      import is running.
   d) Import result, width min(560px, 92vw) — title row (h2 + "Đóng"), a 12px/1.6 summary paragraph
      whose success count is #0e7050 and error count #a02b2b, optionally followed by a bulleted list
      (6px vertical margin, 20px left padding) of per-row errors in #a02b2b, then a right-aligned
      action row (12px top margin) with a single primary "Đóng".

COPY (verbatim Vietnamese — reproduce character for character; there is no i18n layer, every string is
hardcoded in the templates. The card heading is written "&amp;" in source and renders as "&"):
- Route/topbar title: "Danh mục"
- Sidebar: "PM", "PlatformManager"; collapse button aria-label "Mở rộng menu" / "Thu gọn menu"; nav
  labels "Dashboard", "Danh mục", "DTI", "Quản trị hệ thống", "Người dùng", "Phân quyền"
- Topbar: hamburger aria-label "Mở menu điều hướng"; logout button title and label "Đăng xuất"
- Toast close aria-label: "Đóng thông báo"
- Card heading: "Danh mục & Đánh giá theo tuần"
- Record count: "<n> bản ghi"
- Read-only notice: "Đang xem dữ liệu lịch sử — chỉ đọc. Quay lại "Tất cả (mới nhất trong năm)" của năm hiện tại để chỉnh sửa."
- Search placeholder: "Tìm mã hoặc tên chỉ tiêu..." (three ASCII dots)
- Group filter default option: "Tất cả nhóm"; other options render "<Code>. <Name>"
- Year filter title attribute: "Chọn năm"
- Period filter title attribute: "Xem tổng hợp cả năm (mới nhất mỗi chỉ tiêu) hoặc 1 kỳ cụ thể đã lưu trong năm"
- Period filter default option: "Tất cả (mới nhất trong năm)"; other options are ISO week keys, e.g. "2026-W34"
- Toolbar buttons: "Import CSV/Excel", "+ Thêm chỉ tiêu"
- The 12 column headers, in order: "Mã", "Tên", "Nhóm", "Điểm tối đa", "Tự đánh giá", "Thẩm định", "Trạng thái", "Phụ trách", "Hạn xử lý", "Tiến độ %", "Ghi chú", "Hành động"
- Null placeholder in any cell: "—" (em dash)
- Inline-edit hover/title hints: "Bấm đúp để sửa Tiến độ %" and "Bấm đúp để sửa Ghi chú"
- Empty-note affordance text shown in place of a blank note: "— bấm đúp để ghi chú"
- Note input placeholder: "Nội dung đã làm / vướng mắc..."
- Inline-edit confirm/cancel button titles: "Lưu" / "Huỷ"
- Row action buttons: "Sửa" / "Xoá"
- Grid empty message: "Không có chỉ tiêu nào khớp bộ lọc."
- Criteria form dialog title: "Thêm chỉ tiêu" (create) / "Sửa chỉ tiêu" (edit)
- Criteria form labels: "Mã", "Tên chỉ tiêu", "Nhóm", "Điểm tối đa" — each followed by a red "*"
- Criteria form placeholders: "vd 1.1", "Nhập tên đầy đủ chỉ tiêu...", "vd 10"
- Criteria form buttons: "Đóng", "Huỷ", "Lưu chỉ tiêu"
- Criteria form validation messages (one at a time, in this order): "Mã bắt buộc, tối đa 20 ký tự.", "Tên chỉ tiêu bắt buộc.", "Vui lòng chọn nhóm.", "Điểm tối đa phải lớn hơn 0."
- Criteria save fallback error: "Không lưu được chỉ tiêu — thử lại sau."
- Criteria save success toasts: "Đã thêm chỉ tiêu." / "Đã cập nhật chỉ tiêu."
- Confirm dialog title / buttons: "Xác nhận", "Huỷ", "Xoá"
- Delete message when the criterion HAS assessment data: "Chỉ tiêu "<Code>" đã có dữ liệu đánh giá — sẽ ẩn khỏi danh mục (soft-delete), lịch sử vẫn được giữ nguyên. Tiếp tục?"
- Delete message when it has NO assessment data: "Xoá hẳn chỉ tiêu "<Code>"? Chỉ tiêu này chưa có dữ liệu đánh giá nào nên sẽ bị xoá vĩnh viễn."
- Delete success toast: "Đã xoá chỉ tiêu."
- Inline-edit failure toasts: "Không lưu được Tiến độ % — thử lại sau." / "Không lưu được Ghi chú — thử lại sau."
- Import dialog: title "Import CSV/Excel", file label "Chọn file CSV/Excel", chosen-file line "Đã chọn: <filename>", buttons "Đóng" and "Huỷ", primary button "Nhập dữ liệu" which becomes "Đang nhập…" (real ellipsis U+2026) while running
- Import failure toast fallback: "Import thất bại — thử lại sau."
- Import result dialog: title "Kết quả Import", button "Đóng" (twice — title row and footer), summary "Tổng <n> dòng — <n> thành công, <n> lỗi.", optional extra sentence "Đã tự tạo mới <n> chỉ tiêu.", per-row error "Dòng <n> — mã "<code>" : <message>" (the space before the colon is real)
- Shared HTTP error toasts: "Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.", "Bạn cần đăng nhập để tiếp tục.", "Bạn không có quyền thực hiện thao tác này.", "Không tìm thấy dữ liệu yêu cầu.", "Bạn thao tác quá nhanh. Vui lòng chờ một lát rồi thử lại.", "Đã có lỗi xảy ra. Vui lòng thử lại."

STATES:
- Live / editable (the default on open — current year plus the "Tất cả (mới nhất trong năm)" period):
  no notice banner; the "Import CSV/Excel" + "+ Thêm chỉ tiêu" pair is present; editable rows show the
  dashed-underline affordance on Tiến độ % and Ghi chú and show "Sửa"/"Xoá" in the action cell.
- Read-only / historical (any other year, or any single saved period): the notice banner renders; the
  toolbar pair is gone from the DOM; the two editable cells are plain text; the action cell shows "—".
- Loading: a translucent mask with a spinning circular indicator covers the grid body only; rows
  already on screen stay visible underneath it. There is NO skeleton. The filters, the paginator and
  the toolbar buttons stay interactive. It fires on first paint and on every filter/page/page-size
  change — but NOT on the silent refetch that follows a save, delete or import.
- Empty: the single-row message, with the paginator still rendered and the count reading "0 bản ghi".
  The same message covers all three cases — an empty catalogue, a filter that matches nothing, and a
  failed first load.
- Error: every failed request produces an error toast bottom-right, and nothing else — no inline error
  region, no retry button anywhere on the screen. A failed refetch after a save silently leaves stale
  rows on screen. A 401 mid-session additionally redirects to the sign-in route.
- Inline edit committing: the cell closes IMMEDIATELY on confirm, before the request resolves, and the
  new value only appears after the grid refetches. An out-of-range or non-numeric progress value is
  silently clamped into 0-100 — no message, no red border. If the save fails, an error toast appears
  and the typed value is already gone.
- Criteria form validation: submit-time only and ONE message at a time, rendered in a single red line
  under the fields — no field is marked invalid, no field gets a red border.
- Import running: the primary button reads "Đang nhập…" and is disabled at opacity 0.5. There is no
  progress bar, no percentage, no elapsed time and no cancel — the client polls the job every 1500 ms.
  On success the picker closes and the result dialog opens. On a failed job the picker closes and only
  a toast explains it — no result dialog. On a failed request the picker STAYS OPEN with its button
  re-enabled and only a toast explains it.
- Validation display, screen-wide: no inline field validation exists anywhere. The form's single error
  line plus toasts are the entire feedback surface; toasts vanish after 5 seconds.

RESPONSIVE:
- 981px and up (desktop default): sidebar fixed at 220px (or 60px collapsed) with the content column
  offset to match; main capped at 1600px with 14px padding; the topbar hamburger is hidden; a collapsed
  sidebar shows submenus as hover/focus flyouts opening to its right (min-width 180px, radius 10px).
- 980px and below: the content column loses its left offset; the sidebar becomes an off-canvas drawer,
  width min(85vw, 300px), slid fully off-screen until opened, over a dark dismiss backdrop, with the
  card shadow; the topbar hamburger appears at 9px padding. The card, filters and grid are unchanged —
  they simply take the full width.
- 560px and below: main padding drops to 10px; the toolbar pair loses its right alignment and takes the
  FULL WIDTH on its own row below the four filter controls; the topbar hides the user's name leaving
  only the "Đăng xuất" button; the drawer widens to min(90vw, 300px) and its nav items grow to 10px
  padding / 40px minimum height.
- The filters row has no media query of its own — it is a wrapping flex row whose search field holds a
  220px minimum, so the four controls reflow purely on content width.
- The grid has NO media query at all. The 12 columns keep their minimum widths at every size and scroll
  horizontally inside the grid's own scroll container, with "Mã" frozen left and "Hành động" frozen
  right — at a 390px viewport that leaves roughly 200px of scrollable middle. The grid's height is a
  pixel value computed in JavaScript from the viewport on load and on every window resize.
- Print: the filters row, the toast stack, the sidebar, the sidebar backdrop and the topbar are all
  hidden; the content column loses its offset and main loses its max-width. The notice banner and the
  grid DO print — but the grid keeps its computed pixel height, so rows past it are clipped, and the
  paginator prints too.

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

3. Optional, only after a successful import — Stitch resolves the dictionary's own names, so these
   are interchangeable with the literals above and may be swapped in for a tighter prompt:
   `colors.primary` / `colors.brand` (#0f5bd7) · `colors.bg` (#eef2f8) · `colors.card` (#ffffff) ·
   `colors.tonal-bg` (#dbe7fa) · `colors.tonal-ink` (#0f4a9e) · `colors.muted` (#57647a) ·
   `colors.line` (#dfe6ef) · `colors.border-strong` (#7e91b4) · `colors.surface-notice` (#edf4ff) ·
   `colors.border-notice` (#cfe0ff) · `colors.surface-table-header` (#f8fafc) ·
   `colors.text-table-header` (#536076) · `rounded.lg` (16px) · `rounded.sm` (7px) ·
   `rounded.dialog` (15px) · `spacing.card-padding` (14px) · `spacing.cell-padding` (6px 8px) ·
   `typography.table-cell` (12px/400) · `components.card` · `components.button-primary` ·
   `components.button-tonal` · `components.button-danger` · `components.action-button` ·
   `components.table-header` · `components.table-cell` · `components.notice-banner` ·
   `components.dialog` · `components.input-field` · `components.toast`.

4. Attach `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png` and tell Stitch that the
   screenshot was captured against an **empty database** — the empty-message row it shows is the
   empty state, not the normal state. Generate the populated grid from the column list in the
   Master Prompt.

This repo has no Stitch MCP configured — do the import manually via stitch.withgoogle.com (see
`doc/Design/SETUP.md` to add one).

## Claude Design

Paste the **Master Prompt** above, attach
`Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png`, and add the two notes plus the
token block below. (`Assets/Brand/` is empty — the shipped app has no logo or brand image file; the
"PM" mark is a styled text square.)

**Note 1 — the screenshot is an empty-database capture.** It shows the shell, the card, the filters
row, all 12 column headers and the `Không có chỉ tiêu nào khớp bộ lọc.` empty row with `0 bản ghi`.
Use it as the authority for chrome, spacing and the sidebar; generate populated rows from the column
list in the prompt. Do not conclude the grid is always empty.

**Note 2 — three behaviours that look like bugs and are not.** Inline edit is entered by
double-click, not single click. The read-only rule removes write controls from the DOM rather than
disabling them, and turns the action cell into an em dash. The "Trạng thái" column is plain text, not
a coloured badge. Reproduce all three.

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
  /* shipped as literals in component styles, no custom property declared: */
  /* topbar surface rgba(255,255,255,0.95) + blur(10px) */
  /* dialog + drawer backdrop rgba(20,28,40,0.45) */
  /* active sidebar item rgba(15,91,215,0.08) */
  /* dialog shadow 0 24px 70px rgba(0,0,0,0.25) */
  /* .btn:hover shadow 0 3px 10px rgba(23,39,67,0.1) */
  /* .btn.primary:hover shadow 0 8px 20px rgba(15,91,215,0.35) */
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
including the "..." three-dot placeholders, the "…" real ellipsis in "Đang nhập…", and the "—" em
dashes used as null placeholders.

TOKENS (literal values): brand/primary #0f5bd7, primary hover #174ca8, on-primary #ffffff, page
background #eef2f8, card/dialog/input surface #ffffff, ghost hover tint #e1e7f1, secondary button
fill #dbe7fa with text #0f4a9e and hover #c7dbf5, text #152033, muted #57647a, hairline #dfe6ef,
strong input border #7e91b4, success #0e7050 on #d9f2e6, warning #965e08 on #ffedc7, danger #a02b2b
on #fbdcdc with hover #f5c6c6 and border #e5a8a8, table header surface #f8fafc with text #536076,
notice surface #edf4ff with border #cfe0ff, topbar rgba(255,255,255,0.95) + blur(10px), dialog
backdrop rgba(20,28,40,0.45), active nav item rgba(15,91,215,0.08). Card shadow 0 4px 16px
rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06); dialog shadow 0 24px 70px rgba(0,0,0,0.25).
Font Inter (loaded from Google Fonts at weights 400/500/600/700) falling back to "Segoe UI", Arial,
sans-serif: body 13px/400, topbar h1 15px/bold, card h2 14px/bold, button 12px/700, row-action
button 11px/700, table header 11px/700, table cell 12px/400 line-height 1.4, muted caption 11px/400,
form label 12px/700, sidebar nav 12px/600 (700 active), sidebar brand 14px/800, toast 12px/400,
confirm message 13.5px/1.55. Radius 7px buttons+inputs, 9px nav item + toast, 12px notice, 15px
dialog, 16px card, 999px pill, 6px inline-edit icon buttons. Spacing scale 4 / 6 / 8 / 10 / 14px;
card, dialog and main padding 14px; table cell padding 6px 8px; button padding 6px 8px. Sidebar
220px (60px collapsed), content max-width 1600px, breakpoints 980px and 560px. Icons are PrimeIcons
v7 rendered as <i class="pi pi-…">.

Fidelity rules for this screen specifically:
- It renders inside the app shell: fixed left sidebar, sticky topbar, centred main, fixed
  bottom-right toast stack. It is not a bare page.
- Inline editing is entered by DOUBLE-CLICK, not by a single click.
- Write controls (the Import / Add buttons AND the per-row Sửa/Xoá buttons) exist only in
  live/editable mode; otherwise the buttons are absent from the DOM and the action cell shows "—".
- The "Trạng thái" column is plain text, not a coloured badge.
- The 12 columns never collapse or stack; they scroll horizontally at a 1430px combined minimum.
- There is no skeleton loader, no progress bar during import, no inline field validation and no
  retry control anywhere. Do not invent them.
```

**User prompt** = the `LAYOUT:`, `COPY:`, `STATES:` and `RESPONSIVE:` sections of the Master Prompt
above, pasted verbatim.

**Image part** = `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png`, introduced with:
"Captured against an empty database — the single 'Không có chỉ tiêu nào khớp bộ lọc.' row is the
empty state. Use the screenshot for chrome, spacing and the sidebar; generate populated rows from
the column list."

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable,
Figma AI, …) and attach the screenshot below. The block is self-contained — no token resolution,
no other file and no follow-up prompt are required.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png` — the only screenshot for this
  screen. Live/editable state at 1440px wide, sidebar expanded, **empty database**: all 12 headers,
  the `Không có chỉ tiêu nào khớp bộ lọc.` row, `0 bản ghi`, and the paginator showing page size 20.
- `Tokens/tokens.json` — W3C DTCG token file (`global` + `light`; `dark` is intentionally empty, the
  app ships one theme).
- `DESIGN.md` — lint-clean token dictionary, for the Stitch import.
- `Assets/Brand/` — **none exist**. The app ships no logo or brand image file; the "PM" mark is a
  26x26px square filled #0f5bd7 with white 11px/800 text.
