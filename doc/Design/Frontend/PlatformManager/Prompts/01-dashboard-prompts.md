---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
screen_ref: "01-dashboard"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — DTI Weekly Dashboard

<!-- One pack for Screens/01-dashboard.md. Master Prompt filled from that spec + Tokens/tokens.json (light set — the app's only shipped theme) + src/FE/src/styles.scss. Fidelity rule: prompts reproduce the app AS-SHIPPED — quirks included, nothing idealized. -->

> **Rewritten 2026-08-22 — the previous revision is void.** It described the deleted prototype: editable progress and note inputs, a "Lưu tuần này" save button, "Sao lưu"/"Khôi phục" backup actions, a mobile floating action button, a notice banner, the older palette and a 1450px container. **None of that ships.** The Angular dashboard at `/dashboard` is **100 % read-only** — its own page component says so (`dashboard.page.ts:37-38`) — and all editing moved to `/danh-muc/dti`. A generator that produces an editable cell on this screen has produced the wrong screen.
>
> Every literal below is resolved from `src/FE/src/styles.scss` (the `:root` block) via `Tokens/colors.md`, `Tokens/spacing.md`, `Tokens/typography.md` and `DESIGN.md`, all refreshed 2026-08-22. Nothing in this pack needs a lookup in another file.

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is already a literal hex/px/font string. -->

```
Recreate this exact shipped screen — do not idealize. It is a Vietnamese-language internal
console for tracking weekly digital-transformation (DTI) progress. Reproduce the Vietnamese
copy character for character; do not translate it, do not correct it, do not shorten it.

THIS SCREEN IS 100% READ-ONLY. It displays numbers and nothing more. It has exactly ONE
action — an "Xuất báo cáo" button that opens a modal with a server-rendered report — plus
period pickers, client-side table filters, a paginator and a "Xem" button per history row.
Do NOT invent: no editable cells, no number inputs, no note inputs, no save button, no
backup/restore actions, no floating action button, no row action buttons, no checkboxes,
no drag handles, no "add" or "delete" anything. See the DO NOT ADD list at the end.

TOKENS (literal values):
Colors: brand/primary #0f5bd7 (primary button, progress-bar fill, active nav item, active
segmented button, chart line and points, links); brand hover #174ca8; text on brand #ffffff;
page background #eef2f8; card/panel/input surface #ffffff; sticky topbar surface
rgba(255,255,255,0.95) with backdrop-filter blur(10px); text #152033; muted text #57647a;
hairline border #dfe6ef (cards, table row rules, segmented-control frame); strong border
#7e91b4 (inputs and selects and the table wrapper ONLY — never on a button or a card);
secondary "tonal" button fill #dbe7fa with text #0f4a9e, hover fill #c7dbf5; ghost icon
button hover tint #e1e7f1; success #0e7050 on #d9f2e6; warning #965e08 on #ffedc7; danger
#a02b2b on #fbdcdc; progress-bar track #edf1f6; table header fill #f8fafc (also the
even-row zebra stripe) with header text #536076; table row hover #eef2f8; active sidebar
item tint rgba(15,91,215,0.08); modal backdrop rgba(20,28,40,0.45); chart line and points
#0f5bd7, chart area fill rgba(15,91,215,0.12), chart axis tick labels #57647a, chart
y-grid lines #dfe6ef (x-grid not drawn, legend not drawn).
Shadows: card / toast / sidebar drawer = 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px
rgba(23,39,67,0.06); primary button hover = 0 8px 20px rgba(15,91,215,0.35); secondary
button hover = 0 3px 10px rgba(23,39,67,0.1); modal = 0 24px 70px rgba(0,0,0,0.25).
Cards separate from the page by shadow, NOT by a heavy border — the only visible strong
border in the whole layout is around inputs, selects and the table wrapper.
Font: Inter, loaded from Google Fonts (weights 400, 500, 600, 700 only), with fallbacks
"Segoe UI", Arial, sans-serif. Sizes/weights as shipped: body 13px/400; topbar page title
15px/bold; card heading 14px/bold; muted caption 11px/400; button label 12px/700; table
header 11px/700 with letter-spacing 0.01em; table cell 12px/400 line-height 1.4; status
badge 10px/750; delta text 12px/850; KPI value 21px/850 (18px at 560px and below); KPI
label and KPI sub-caption 11px/400 (sub has line-height 1.4 and a 30px min-height so tiles
stay aligned); sidebar nav item 12px/600, 700 when active; sidebar brand text 14px/800;
sidebar brand mark 11px/800; footer 11px/400; toast text 12px/400 line-height 1.4. Weights
750/800/850 are not in the loaded font file — the browser synthesises them, keep them.
Radii: 7px buttons, inputs, selects, segmented control; 9px sidebar nav item and toast;
12px table wrapper; 15px modal; 16px cards; 999px status badges and progress bars.
Spacing scale actually used: 4px, 6px, 8px, 10px, 14px. Applied as: card padding 14px;
main content padding 14px (10px at 560px and below); topbar inner padding 10px 14px;
button padding 6px 8px; table cell padding 6px 8px; input and select padding 6px 8px;
badge padding 3px 6px; period-toolbar gap 8px; filter row gap 8px with 10px vertical
margin; card heading row gap 8px with 10px bottom margin; KPI grid gap 10px (8px at 560px
and below); two-column row gap 14px with 14px top margin; criteria card top margin 16px;
history card top margin 16px; footer padding 12px 4px; toast stack inset 14px from the
right and bottom with 8px gap; toast padding 8px 10px; sidebar nav padding 6px, nav item
padding 6px 8px, brand row padding 10px.
Structure: sidebar 220px wide (60px when collapsed); content column max-width 1600px,
centred; breakpoints at 980px and 560px; trend chart canvas height exactly 220px; criteria
table vertical scroll height exactly 480px at every viewport; history panel max-height
240px with its own scrollbar; progress bar height 9px; search field min-width 220px;
modal width min(700px, 92vw); toast stack max-width min(360px, 90vw).
Icons: PrimeIcons v7, rendered as icon-font glyphs. The page body renders NO icon at all —
every icon on screen belongs to the app shell (hamburger, sign-out, sidebar collapse
chevron, nav-item glyphs, toast dismiss). The only directional cues inside the dashboard
are the literal text characters "↑" and "↓" written inline in coloured text.

LAYOUT (top to bottom):
APP SHELL — fixed left sidebar, 220px wide, white, full viewport height, 1px right border
#dfe6ef. Its top row (10px padding, 50px min-height, 1px bottom border) holds a 26px
rounded-7px brand square filled #0f5bd7 with white "PM" in 11px/800, then "PlatformManager"
in 14px/800, then a 24px ghost collapse button on the far right holding a left-pointing
chevron. Below it a nav list (6px padding): each item is a 9px-radius row, 6px 8px padding,
12px/600, an 18px muted glyph then the label. The active item is filled
rgba(15,91,215,0.08), text and glyph #0f5bd7, weight 700, with a 3px #0f5bd7 rail on its
left edge (rounded on the right side only). Parent items carry a right-aligned chevron that
rotates when the group is open; children are indented. Nav labels come from the server; in
the reference screenshot they read: "Dashboard" (active, grid glyph), "Danh mục" (folder
glyph, expanded) with child "DTI", "Quản trị hệ thống" (gear glyph, expanded) with children
"Người dùng" and "Phân quyền".
Content column sits to the right of the sidebar. At its top a sticky translucent topbar
(rgba(255,255,255,0.95) + blur, 1px bottom border #dfe6ef, inner row capped at 1600px,
10px 14px padding): the page title "Dashboard" as a 15px bold heading on the left; on the
right the signed-in user's name in 12px/700 followed by a secondary tonal button
(#dbe7fa fill, #0f4a9e text) reading "Đăng xuất" with a leading sign-out glyph. A
hamburger button exists at the far left of the topbar but is hidden above 980px.
Below the topbar, a centred main area capped at 1600px with 14px padding, holding six
stacked page regions with NO extra wrapper:

1. PERIOD TOOLBAR — a white 16px-radius card, 14px padding, laid out as one wrapping flex
row with 8px gaps: the bold label "Kỳ đang xem:"; then a READ-ONLY chip showing the current
period label (1px #7e91b4 border, 7px radius, #eef2f8 fill, #57647a text, 6px 8px padding,
12px) — it looks like an input but is pure display, never typed into, and shows "—" when no
period has loaded; then a native year select; then a native period select; then a two-button
segmented control (inline-flex, shared 1px #dfe6ef frame, 7px radius, overflow hidden, 1px
divider between the buttons, each button 6px 10px padding, 12px/700, resting state white
with #57647a text, ACTIVE state filled #0f5bd7 with #ffffff text) reading "Tuần" and
"Tháng"; and pushed to the far right a primary button (#0f5bd7 fill, white text, 7px radius,
6px 8px padding, 12px/700) reading "Xuất báo cáo". There is NO date picker, NO save button
and NO backup/restore button on this row.
2. KPI ROW — five equal cards in a 5-column grid, 10px gap. Each card is white, 16px radius,
14px padding, and stacks: an 11px muted label, a 21px/850 value 4px below it, then an 11px
muted sub-caption 4px below that (30px min-height). Values are plain text — no sparkline, no
icon, no trend arrow graphic. Cards in fixed order: overall progress; delta versus the
previous period (its value is coloured green #0e7050 when positive, red #a02b2b when
negative, muted #57647a when flat); "Chỉ tiêu tăng" with a green value; "Không tăng" with an
amber #965e08 value; "Hoàn thành 100%" with a plain dark value.
3. TWO-COLUMN ROW — a 1.15fr / 0.85fr grid, 14px gap, 14px top margin, both children white
cards. Each card opens with a heading row (space-between, 8px gap, 10px bottom margin): a
14px bold heading on the left, an 11px muted caption on the right.
   LEFT CARD — heading "Tiến độ theo nhóm". Its body is a vertical list spread to the card's
   full height (space-between, 8px gaps); each row is a 3-column grid `210px 1fr 80px` with
   8px gaps: bold group text "<code>. <name>" on the left, a 9px-tall pill-shaped track
   filled #edf1f6 with a #0f5bd7 pill fill sized to the percentage in the middle, and a bold
   right-aligned percentage on the right.
   RIGHT CARD — heading "Biểu đồ tiến độ hàng tuần". Its body is one 220px-tall line chart
   filling the card width: a single #0f5bd7 series with straight segments (no smoothing),
   4px-radius round points filled #0f5bd7, an area fill rgba(15,91,215,0.12) under the line,
   y-axis fixed 0 to 100 with ticks suffixed "%", tick labels #57647a, horizontal grid lines
   #dfe6ef only (no vertical grid), and no legend.
4. CRITERIA TABLE CARD — white card, 16px top margin. Heading row: 14px bold heading on the
left, 11px muted count on the right. Then a wrapping filter row (8px gaps, 10px vertical
margin) of four native controls, all 1px #7e91b4 border, 7px radius, white, 6px 8px padding,
12px: a search text field that flexes to fill (min-width 220px) and three selects. Then a
table wrapper with a 1px #7e91b4 border, 12px radius and clipped overflow, containing a
9-column data table that scrolls VERTICALLY inside a fixed 480px height with a sticky header
row. Column widths are percentages summing to 100: 6, 24, 12, 7, 9, 9, 8, 9, 16 — the table
never scrolls horizontally, it compresses. Header cells: #f8fafc fill, #536076 text,
11px/700, letter-spacing 0.01em, left-aligned except columns 4, 5, 6 and 7 which are
right-aligned. Body cells: 12px/400, line-height 1.4, top-aligned, 6px 8px padding, 1px
#dfe6ef bottom rule, even rows tinted #f8fafc, hovered row tinted #eef2f8. Cell contents are
plain TEXT ONLY: bold code, name, group, max score, previous value, current value, then a
delta cell (12px/850, green with a leading "↑", red with a leading "↓", muted when flat, "—"
when unknown), then a pill status badge (999px radius, 3px 6px padding, 10px/750 — green
#0e7050 on #d9f2e6, amber #965e08 on #ffedc7, red #a02b2b on #fbdcdc), then the week note or
a muted "—". Below the table a paginator row with first/previous/next/last arrow buttons and
a rows-per-page select offering 10, 20 and 50, defaulting to 20.
5. HISTORY CARD — white card, 16px top margin. Heading row with a 14px bold heading and an
11px muted caption. Body is a list capped at 240px height with its own scrollbar, 6px gaps;
each row is a 4-column grid `100px 1fr 90px 70px` with 6px padding: a bold date, the text
"Tiến độ chung" followed by a bold percentage, then a coloured delta (or a muted "Kỳ đầu" on
the oldest row), then a small secondary tonal button reading "Xem".
6. FOOTER — one muted 11px line with 12px 4px padding, containing a #0f5bd7 bold link.
TOAST OVERLAY — fixed bottom-right, 14px from both edges, above everything, max-width
min(360px, 90vw), 8px gap. Each toast is a white 9px-radius card with a 1px #dfe6ef border,
a 4px left border tinted by severity (red #a02b2b for errors, green #0e7050 for success,
amber #965e08 for warnings, blue #0f5bd7 for info), the card shadow, 8px 10px padding, 12px
text, and a 22px ghost dismiss button holding an "×" glyph. Toasts auto-dismiss after 5
seconds. This is the ONLY feedback surface on the screen.
REPORT MODAL (shown only after "Xuất báo cáo" is pressed) — centred native modal over an
rgba(20,28,40,0.45) backdrop, min(700px, 92vw) wide, 15px radius, 14px padding, shadow
0 24px 70px rgba(0,0,0,0.25). Header row: a 14px bold title on the left, a small secondary
tonal button "Đóng" on the right. Body: server-rendered report HTML with NO styling of its
own — plain inherited body typography, no panel, no border, no tint. Footer row: a secondary
tonal button "Sao chép" then a primary button "In", 8px gap.

COPY (verbatim — reproduce exactly, all Vietnamese, no i18n layer, keep every ellipsis
character, every "·" separator and every capital letter as written):
- Browser tab title: "PlatformManager"
- Topbar page title: "Dashboard". Hamburger accessible label: "Mở menu điều hướng".
  Logout button label and tooltip: "Đăng xuất". User name is server data, e.g.
  "Quản trị viên hệ thống".
- Sidebar brand: "PM" and "PlatformManager". Collapse button accessible label toggles
  between "Mở rộng menu" and "Thu gọn menu". Nav labels are server-driven; the reference
  capture shows "Dashboard", "Danh mục", "DTI", "Quản trị hệ thống", "Người dùng",
  "Phân quyền".
- Period toolbar: label "Kỳ đang xem:"; period chip shows server text such as
  "Tuần 33/2026 (10/08–16/08/2026)" or "Tháng 8/2026" or "Năm 2026", falling back to "—".
  Year select tooltip "Chọn năm". Week select tooltip "Chọn 1 tuần cụ thể hoặc xem Tất cả",
  first option "— Kỳ hiện tại —", second option "— Tất cả (tổng hợp theo năm) —", then saved
  periods rendered as "dd/mm/yyyy" plus " · <n,n>%" when a progress value is known. Month
  select tooltip "Chọn tháng", first option "— Tháng hiện tại —", then "Tháng <n>" plus
  " · <n,n>%". Segmented control accessible label "Chế độ xem theo Tuần hoặc Tháng", buttons
  "Tuần" and "Tháng". Primary action "Xuất báo cáo". When the year-aggregate option is
  selected an extra amber badge appears after the period chip reading "Tất cả · 2026".
- KPI 1 label is one of "Tiến độ chung tuần này" / "Tiến độ chung tháng này" /
  "Tiến độ chung (tổng hợp năm)"; its sub-caption is always "Bình quân gia quyền theo điểm".
- KPI 2 label is "So với tuần trước" or "So với tháng trước"; its sub-caption is the server's
  previous-period label, or "Chưa có kỳ trước" when there is none.
- KPI 3: "Chỉ tiêu tăng" / "Có tiến bộ so với kỳ trước".
- KPI 4: "Không tăng" / "Cần chú ý theo dõi".
- KPI 5: "Hoàn thành 100%" / value formatted "<done>/<total>" e.g. "0/0" / "Số chỉ tiêu đạt
  đủ tiến độ".
- Number formats (Vietnamese locale, comma as the decimal separator, one decimal place):
  percentages render as "82,1%", deltas as "↑ 1,4 đ.%" or "↓ 0,6 đ.%" or "0,0 đ.%", and any
  unknown value renders as the em dash "—".
- Left card: heading "Tiến độ theo nhóm"; caption is "Tuần hiện tại" or "Tháng hiện tại" or
  "Tổng hợp năm 2026"; each row reads "<mã>. <tên nhóm>"; empty state
  "Chưa có dữ liệu nhóm chỉ tiêu."
- Right card: heading "Biểu đồ tiến độ hàng tuần" or "Biểu đồ tiến độ hàng tháng"; caption
  "Tiến độ chung" or "Tổng hợp năm 2026"; lazy-load placeholder "Đang tải biểu đồ…"; empty
  state "Chưa có đủ dữ liệu để vẽ biểu đồ."
- Criteria card: heading "<n> chỉ tiêu DTI" e.g. "0 chỉ tiêu DTI"; count caption
  "<lọc>/<tổng> chỉ tiêu" e.g. "0/0 chỉ tiêu"; search placeholder
  "Tìm mã hoặc tên chỉ tiêu..."; group select first option "Tất cả nhóm" then
  "<mã>. <tên>"; change select options "Tất cả mức thay đổi", "Chỉ tiêu tăng", "Không tăng",
  "Giảm", "Hoàn thành"; sort select options "Theo mã chỉ tiêu", "Tăng nhiều nhất",
  "Tiến độ thấp nhất". Column headers in order: "Mã", "Chỉ tiêu", "Nhóm", "Điểm tối đa",
  then a mode-dependent header "Tuần trước" / "Tháng trước" / "—", then a mode-dependent
  header "Tuần này" / "Tháng này" / "Tất cả (TB)", then "Tăng/giảm", "Trạng thái",
  "Ghi chú tuần". Status badge text is one of "Hoàn thành", "Đang thực hiện", "Không tăng",
  "Chưa có dữ liệu". Empty note cell shows "—". Empty table message
  "Không có chỉ tiêu nào khớp bộ lọc."
- History card: heading "Lịch sử các kỳ đã lưu"; caption "Không ghi đè dữ liệu tuần cũ";
  lazy-load placeholder "Đang tải lịch sử…"; each row reads "Tiến độ chung" then a bold
  percentage; the oldest row shows "Kỳ đầu"; row button "Xem"; empty state
  "Chưa có tuần nào trong năm đang chọn."
- Footer, one line: "Xem toàn bộ danh mục & nhập/cập nhật dữ liệu tại Danh mục > DTI." where
  "Danh mục > DTI" is the blue bold link.
- Report modal: default title "Báo cáo tiến độ DTI" (replaced by the server's title once the
  fetch resolves); buttons "Đóng", "Sao chép", "In". The body text is produced entirely by
  the server — invent nothing for it.
- Toasts: "Đã sao chép báo cáo." on a successful copy; "Không sao chép được — trình duyệt
  chặn quyền truy cập clipboard." on a failed copy; and the shared HTTP failures "Không thể
  kết nối tới máy chủ. Kiểm tra kết nối mạng.", "Bạn cần đăng nhập để tiếp tục.", "Bạn không
  có quyền thực hiện thao tác này.", "Không tìm thấy dữ liệu yêu cầu.", "Bạn thao tác quá
  nhanh. Vui lòng chờ một lát rồi thử lại.", "Đã có lỗi xảy ra. Vui lòng thử lại." Toast
  dismiss accessible label "Đóng thông báo".

STATES:
- Default first paint = week mode, the current year preselected, the period select on
  "— Kỳ hiện tại —". Until data lands the page shows its zero shape: period chip "—", KPI 1
  and KPI 2 values "—", KPIs 3, 4 and 5 showing "0", "0" and "0/0", the groups list showing
  its empty sentence, the chart showing its empty sentence, the table heading reading
  "0 chỉ tiêu DTI" with "0/0 chỉ tiêu" and the single empty-message row, the history panel
  showing its empty sentence. The reference screenshot is exactly this state.
- Loading: THERE IS NO LOADING AFFORDANCE. Do not draw a spinner, a skeleton, a progress bar
  or a dimmed overlay for data. Changing the year, the period or the week/month mode swaps
  every number on the page with no indication that a request is in flight, and the previous
  period's numbers stay fully legible until the new ones arrive. The only two "loading"
  strings on the screen — "Đang tải biểu đồ…" and "Đang tải lịch sử…" — are lazy-load
  placeholders for the chart and history code chunks, centred muted text in a 220px-tall box,
  and are normally gone before the user sees them.
- Populated, week mode with a previous period (the common case): every tile, bar, point and
  column carries a real number; the delta column and KPI 2 are coloured; all four badge texts
  can appear; the history panel lists saved weeks newest first.
- Populated, no previous period: KPI 2 shows "—" with the sub-caption "Chưa có kỳ trước" and
  no colour; the "Tuần trước" and "Tăng/giảm" columns show "—" on every row; the oldest
  history row shows "Kỳ đầu" instead of a delta. KPI 4 still shows a real number here — it is
  never blanked.
- Populated, month mode: the period select becomes the month picker, KPI 1 and KPI 2 labels
  switch to the "tháng" wording, the left card's caption becomes "Tháng hiện tại", the chart
  heading becomes "…hàng tháng", and the table's two value columns become "Tháng trước" and
  "Tháng này". The history panel does NOT follow — it always lists weeks.
- Populated, year aggregate ("— Tất cả (tổng hợp theo năm) —" selected): an amber badge
  "Tất cả · 2026" appears next to the period chip, KPI 1 reads "Tiến độ chung (tổng hợp
  năm)", both card captions read "Tổng hợp năm 2026", table column 5 degrades to "—" and
  column 6 becomes "Tất cả (TB)". KPI 2 keeps its weekly wording — reproduce that as-is.
- Empty: each region owns its own empty branch and they are independent — the groups list,
  the chart, the table and the history panel each show their own sentence, while the KPI
  tiles have no empty branch at all and simply render "—", "0" or "0/0".
- Filtered table with no matches: identical to the empty table — the same
  "Không có chỉ tiêu nào khớp bộ lọc." row, the count caption reading "0/<tổng> chỉ tiêu",
  and the paginator still rendered.
- Error: the ONLY visible error is a toast. A failed data load leaves the page looking
  exactly like an empty period (first load) or leaves the previous period's numbers on screen
  (later loads). There is no error banner, no retry button and no inline error region
  anywhere on this screen.
- Validation: none is possible — the screen has no field that accepts a value. The search box
  filters, it does not validate.
- Report modal open: the modal sits on top of a dimmed page; the underlying dashboard is
  still visible around it. Closing works via the "Đóng" button and the Escape key only —
  clicking the backdrop does NOT close it.

RESPONSIVE:
- Above 980px (desktop default): sidebar fixed at 220px (or 60px collapsed) with the content
  offset to match; the topbar hamburger is hidden; KPI grid 5 columns; two-column row
  1.15fr / 0.85fr; group rows `210px 1fr 80px`.
- 980px and below: the sidebar becomes an off-canvas drawer, min(85vw, 300px) wide, slid out
  of view until opened from the hamburger, with the card shadow and a tap-to-dismiss dim
  backdrop; the content loses its left offset; the hamburger appears at the far left of the
  topbar; KPI grid becomes 2 columns; the two-column row stacks to 1 column (groups card
  above chart card); group rows become `140px 1fr 75px`.
- 560px and below: main padding drops to 10px; the topbar user name is hidden, leaving only
  the "Đăng xuất" button; the drawer widens to min(90vw, 300px) and its nav items grow to a
  40px min-height; the KPI grid gap drops to 8px and the FIFTH tile spans the full row so the
  2-column grid has no hole; the KPI value drops to 18px; every child of the period toolbar
  stretches to equal width and "Xuất báo cáo" wraps onto its own full-width row; group rows
  become `110px 1fr 68px`.
- All viewports: the criteria table NEVER changes shape. It has no breakpoint, no minimum
  width, no frozen column and no horizontal scroll — the nine percentage columns simply
  compress, so at 390px the "Chỉ tiêu" column is roughly 90px wide and wraps heavily. Its
  480px vertical scroll height is identical at every viewport. The history panel likewise
  keeps `100px 1fr 90px 70px` down to 390px. Cards never change their padding.
- Print: the period toolbar and the table filter row disappear, along with the sidebar, the
  topbar and the toast stack; the content loses its offset and its max-width. Everything else
  prints as laid out, including the table's 480px scroll box and the 240px history box.

DO NOT ADD (each of these existed in an older prototype and does NOT ship — producing any of
them means the wrong screen):
- No progress "%" input in the table, no note input in the table, no editable or
  double-clickable cell, no inline edit confirm/cancel icons, no per-row action buttons.
- No "Lưu tuần này", no "Sao lưu", no "Khôi phục", no "Tạo tuần mới từ kỳ gần nhất", no
  "Báo cáo nhanh", no date input in the toolbar, no "— Chọn kỳ đã lưu —" select.
- No floating action button anywhere, at any breakpoint.
- No pale blue instruction/notice banner above the toolbar.
- No spinner, skeleton or progress indicator for data loading.
- No horizontally scrolling table, no frozen first column, no 1200px minimum table width.
- No styled panel, tinted surface or dashed border around the report modal body.
- No dark mode, no theme toggle, no second chart series, no chart legend, no chart x-grid.
- No extra icons inside the page body — the dashboard content area renders none.

Match the attached screenshot pixel-for-pixel wherever it conflicts with this text.
```

## Google Stitch

1. Lint `DESIGN.md`, then import it into the Stitch project (Design → import design.md) so the palette, type scale, radii and spacing land as Stitch design tokens:

```bash
npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md
```

Result verified 2026-08-22: **0 errors, 6 warnings** — none of the six is a real defect (2 false positives where the linter compares an alpha-composited brand tint against brand itself, and 4 border-colour tokens the design.md `components` schema has no slot for). It was 8 until the same day; the other two were real contrast failures on the amber badge and the danger-button hover, and were fixed at the source rather than recorded. See `DESIGN.md` § Colors. The bare `npx @google/design.md lint` form fails silently on Windows — always use the `--package=…designmd` form.

2. Paste the Master Prompt above verbatim. **Keep the literal values in it even after the import** — an import that silently fails, or a Stitch session started from a different project, otherwise produces an off-palette screen with no warning. Literals cost nothing and remove that failure mode.

3. Add this Stitch-specific preamble above the pasted prompt:

```
Generate ONE desktop screen at 1440px wide. It is an analytics dashboard: a fixed 220px
left navigation rail, a sticky top bar, and a centred 1600px-max content column of white
16px-radius cards on an #eef2f8 page. Density is compact — 13px body text, 12px table
text, 6px 8px cell padding. Do not add hero sections, marketing copy, illustrations,
avatars, gradients or dark mode. Every string on the screen is Vietnamese and is supplied
verbatim below; do not invent, translate or paraphrase any of it.
```

This repo has **no Stitch MCP configured** — import and generate manually at stitch.withgoogle.com (see `doc/Design/SETUP.md` if you want to automate it). Log whatever comes back in `Exports/`.

## Claude Design

Paste the Master Prompt above, attach `Assets/Screenshots/dashboard/dashboard--empty--desktop-1440.png`, and prepend the token block below. There are **no brand image assets** to attach — the app ships no logo file; the "PM" mark is a text square (`UiInventory.md` § Brand Assets).

Every right-hand side below is a resolved literal — nothing here needs interpolation. Property names deliberately match the shipped custom properties in `src/FE/src/styles.scss`, so a generated stylesheet maps back to the app 1:1:

```css
:root {
  /* colors — src/FE/src/styles.scss :root */
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
  --surface-track: #edf1f6;
  --surface-table-header: #f8fafc;
  --text-table-header: #536076;
  /* colors shipped as literals in selectors, no custom property in the app */
  --surface-topbar: rgba(255, 255, 255, 0.95); /* + backdrop-filter: blur(10px) */
  --surface-nav-active: rgba(15, 91, 215, 0.08);
  --overlay-backdrop: rgba(20, 28, 40, 0.45);
  --chart-series-1: #0f5bd7;
  --chart-series-1-fill: rgba(15, 91, 215, 0.12);
  --chart-axis-label: #57647a;
  --chart-grid: #dfe6ef;
  /* elevation */
  --shadow: 0 4px 16px rgba(23, 39, 67, 0.1), 0 1px 3px rgba(23, 39, 67, 0.06);
  --shadow-primary-hover: 0 8px 20px rgba(15, 91, 215, 0.35);
  --shadow-btn-hover: 0 3px 10px rgba(23, 39, 67, 0.1);
  --shadow-dialog: 0 24px 70px rgba(0, 0, 0, 0.25);
  /* typography — Inter is loaded from Google Fonts at weights 400/500/600/700 */
  --font-family-base: Inter, 'Segoe UI', Arial, sans-serif;
  --fs-xs: 11px;
  --fs-sm: 12px;
  --fs-base: 13px;
  --fs-md: 14px;
  --fs-lg: 15px;
  /* spacing + radius */
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
  /* structure */
  --sidebar-w: 220px;
  --sidebar-w-collapsed: 60px;
  --container-max-width: 1600px;
}
```

Off-scale literals this screen genuinely ships, to reproduce rather than round: KPI value `21px/850` (`18px` at 560px and below), KPI sub `min-height: 30px`, badge `10px/750` with `3px 6px` padding, delta weight `850`, progress bar height `9px`, table scroll height `480px`, chart height `220px`, history panel `max-height: 240px`, criteria-card and history-card `margin-top: 16px`, footer padding `12px 4px`.

## Google AI Studio

**System instruction** — paste as-is:

```
You reproduce an existing shipped web UI exactly as it is, not as it should be. This is
PlatformManager, an internal Vietnamese-language console for weekly digital-transformation
(DTI) progress tracking, built in Angular 20 with PrimeNG and PrimeIcons v7.

Hard rules:
1. The screen you are drawing is 100% READ-ONLY. It has one action button, period pickers,
   client-side filters, a paginator and a per-row "Xem" button. Never add an editable cell,
   an input inside the table, a save button, a backup/restore control or a floating action
   button — those belong to an obsolete prototype and do not exist in the shipped app.
2. Every visible string is Vietnamese and is given to you verbatim. Reproduce each one
   character for character, including "…", "·", "—" and the comma decimal separator. Do not
   translate, correct, shorten or normalise any of it.
3. Use only these literal values. Colors: #0f5bd7 brand, #174ca8 brand hover, #ffffff on
   brand, #eef2f8 page, #ffffff card, rgba(255,255,255,0.95) topbar, #152033 text, #57647a
   muted, #dfe6ef hairline border, #7e91b4 input border, #dbe7fa / #0f4a9e tonal button,
   #c7dbf5 tonal hover, #e1e7f1 ghost hover, #0e7050 on #d9f2e6 success, #965e08 on #ffedc7
   warning, #a02b2b on #fbdcdc danger, #edf1f6 progress track, #f8fafc table header and zebra
   with #536076 header text, rgba(15,91,215,0.08) active nav, rgba(20,28,40,0.45) modal
   backdrop, rgba(15,91,215,0.12) chart area fill. Shadows: cards 0 4px 16px
   rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06); modal 0 24px 70px rgba(0,0,0,0.25).
   Font: Inter with "Segoe UI", Arial, sans-serif fallbacks — body 13px/400, table 12px/400,
   table header 11px/700, card heading 14px/bold, button 12px/700, badge 10px/750, KPI value
   21px/850, captions 11px/400. Radii: 7px controls, 9px nav item and toast, 12px table
   wrapper, 15px modal, 16px cards, 999px pills. Spacing: 4, 6, 8, 10 and 14px only, plus the
   documented off-scale exceptions (16px card top margins, 12px 4px footer padding, 9px
   progress bar, 220px chart, 480px table scroll, 240px history box).
4. Layout frame: 220px fixed sidebar, sticky translucent topbar, centred content column
   capped at 1600px with 14px padding. Breakpoints at 980px and 560px only.
5. Cards separate from the page by shadow, not by a heavy border. The only strong border in
   the layout is around inputs, selects and the table wrapper.
6. Do not idealize: keep the compact density, keep the synthetic 750/800/850 font weights,
   keep the table's percentage columns with no horizontal scroll, and do not add loading
   spinners, skeletons, hover cards, empty-state illustrations or accessibility affordances
   that the app does not have.
```

**User prompt** = the LAYOUT + COPY + STATES + RESPONSIVE + DO NOT ADD sections of the Master Prompt above, pasted verbatim.

**Image part** to attach: `Assets/Screenshots/dashboard/dashboard--empty--desktop-1440.png`. Tell the model in the user turn: "The attached capture is the real screen in its empty state — treat it as the authority wherever it disagrees with my text."

## Generic

For any other generator (v0, Bolt, Lovable, Figma AI, an internal tool): paste the Master Prompt block verbatim and attach `Assets/Screenshots/dashboard/dashboard--empty--desktop-1440.png`. Nothing in the Master Prompt depends on this repository — every value is already a literal and every string is already verbatim.

Two guardrails worth repeating in the tool's own chat after the first generation, because generators reintroduce them by habit:

```
1. Remove any input, editable cell, save button, backup/restore control or floating action
   button you added — this screen is read-only and has exactly one action, "Xuất báo cáo".
2. Remove any loading spinner or skeleton over the data regions — the shipped screen has
   none; the only two loading strings are "Đang tải biểu đồ…" and "Đang tải lịch sử…".
```

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/dashboard/dashboard--empty--desktop-1440.png` — the only current capture: 1440px desktop, empty state, sidebar expanded, week mode.
- `Tokens/tokens.json` (W3C DTCG — enable `global` + `light`; `dark` is intentionally empty, no dark mode ships).
- `DESIGN.md` (lint-clean token dictionary + design guidance, for the Stitch import).
- `Assets/Brand/` — **none**. The app ships no logo or brand image file; the "PM" mark is a text square (`UiInventory.md` § Brand Assets).
- ⚠️ **Do not attach anything from `Assets/Screenshots/dashboard/_superseded-prototype/`.** Those four PNGs are 2026-08-11 captures of the deleted prototype and show the editable, FAB-bearing screen this pack exists to stop generators from drawing.

## Known gaps in this pack

- **`TrendChart`, `SegmentedControl` and `Footer` were undocumented while this pack was written** and are described here directly from `Screens/01-dashboard.md` and the shipped source (`trend-chart.{html,ts,scss}`, `period-toolbar.{html,scss}`, `styles.scss` `.footer`). Their specs landed in `COMPONENTS.md` pass C on the same day (index now 27 documented) — re-read `Components/TrendChart.md`, `Components/SegmentedControl.md` and `Components/Footer.md` and reconcile this pack's wording against them on the next pass. Nothing here was invented: every measurement traces to a source line.
- **The read-only period chip (`.period-display`) has no component spec by decision** (`COMPONENTS.md` § Deliberately NOT given their own spec) — described here from `period-toolbar.scss:16-23`. The lazy-load placeholder box is folded into `Components/TrendChart.md` as the `@defer` placeholder state, but it is shared with the history panel, so it is described here as page-level markup.
- **Only one screenshot exists.** Month mode, the year aggregate, the report modal, a populated table, tablet 900px and mobile 390px are all uncaptured; the prompt describes them from the spec. Capture on demand and add the file here at that point.
