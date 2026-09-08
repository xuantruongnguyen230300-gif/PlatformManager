---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "draft — target, not built"
updated: "2026-09-08"
screen_ref: "02-danh-muc-dti"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — DTI Catalogue (`/danh-muc/dti`)

<!-- One pack for Screens/02-danh-muc-dti.md. The Master Prompt is filled from that spec plus
     Tokens/tokens.json (light set — the app's only shipped theme), whose values were
     re-checked against the :root block of src/FE/src/styles.scss on 2026-09-08. Copy is
     verbatim Vietnamese from the screen spec's § Copy table. Fidelity rule: reproduce the
     approved design exactly — quirks included, nothing idealized, nothing translated.

     Everything a tool needs is inside THIS file: every token is resolved to a literal
     hex/px/font value, and no other spec has to be opened. -->

> **Regenerated 2026-09-08 from `Screens/02-danh-muc-dti.md`. The revision it replaces was
> `status: "historical"` and would have produced the wrong screen three times over.**
> That pack described the pre-2026-08-29 catalogue (an 11-column grid, plain-text status, no
> `Chênh lệch` column, no frozen edges), drew it in the retired palette (`#eef2f8`, `#dfe6ef`,
> `#57647a`, `#7e91b4`, `#f8fafc`) with `Inter` as the type face, and emitted the retired
> custom properties `--tonal-bg-hover`, `--bad-bg-hover`, `--bad-border` and
> `--text-table-header`. All of that is gone from this file.
>
> **Two things about the screen this pack draws, before anything else:**
>
> 1. **It is not built.** The DTI module was removed from `src/FE` on 2026-08-29 and the
>    rebuild has not started, so this pack generates an **approved design**, not a picture of
>    running code. That is why the frontmatter reads `status: "draft — target, not built"`:
>    `draft` because no prompt pack is ever `approved`, and `not built` because nobody can
>    open a source file and check the screen against it — which is what `verified:
>    khong-ap-dung` records. Confirm the premise rather than trusting this paragraph:
>
>    ```bash
>    grep -cE '^\s+loadChildren:' src/FE/src/app/app.routes.ts   # routed screens today
>    ls src/FE/src/app/modules 2>/dev/null                       # expect: no such directory
>    ```
>
> 2. **The app shell and the shared component layer around it *are* built**, and every value
>    the Master Prompt gives for them is read from the live `src/FE/src/styles.scss` and the
>    live toolbar / data-grid / dialog / sidebar / topbar / toast components. The seeded
>    sidebar rows come from `src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs` §
>    `Items` — which is also why the sidebar in this design carries **no** "Danh mục" row yet.

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is a literal. -->

```
Recreate this exact shipped screen — do not idealize, do not translate, do not "improve" anything.

CONTEXT: an internal Vietnamese-language administration platform ("PlatformManager"). This screen is
the DTI Catalogue at /danh-muc/dti, and it is the product's SINGLE DATA-ENTRY SURFACE — every write
in the whole application happens here. One card, one wide grid, four dialogs. The Dashboard at
/trang-chu is read-only by design and does the opposite job. The screen renders INSIDE the app shell
(fixed left sidebar + sticky topbar + centred main + fixed toast stack).

⚠ IT IS ONE CARD. Not two, not a split pane, not a master-detail layout. The card contains, in this
order: a title row, an optional single-slot notice bar, a toolbar, a data grid, and the grid's
paginator. The four dialogs belong to this route and open over it; only one can be open at a time.

⚠ THE GRID IS 14 COLUMNS WIDE AND SCROLLS HORIZONTALLY. That is the design, not a failure. Do NOT
restack it into cards on any viewport, do NOT hide columns per breakpoint, do NOT collapse it into an
accordion. Two columns are PINNED against that horizontal scroll — the first ("Mã") to the left edge
and the last ("Hành động") to the right edge — so the row stays identifiable and actionable while the
middle scrolls under them.

PALETTE (recomputed from the WCAG contrast formula, not picked by eye — use these exact values):
page background #cfdaea; card + dialog + input surface #ffffff; ghost-button hover tint #c1cde2;
default (secondary) button fill #c4d8f6 with text #0f4a9e and hover fill #c7dbf5; body text #152033;
muted text #4c576b; component hairline #7a97bd (card border, toolbar border, table row rules — this
measures 3.00:1 on white, it is a REAL visible border, not a faint one); stronger border #6077a2
reserved for inputs, selects, textareas and the table wrapper; brand #0f5bd7 with hover #174ca8 and
white text on it; success #0e7050 on #d9f2e6; warning #965e08 on #ffedc7; danger #a02b2b on #fbdcdc
with hover fill #f5c6c6 and border #e0a8a8; table header surface #e9eff6 with header text #536076
(the same #e9eff6 tints every even body row); the notice bar fill is #c4d8f6 with a #0f5bd7 left
rule; the filter chip fill is #c4d8f6 with #0f4a9e text; topbar surface rgba(255,255,255,0.95) with
backdrop-filter blur(10px); dialog and drawer backdrop rgba(20,28,40,0.45); active sidebar item tint
rgba(15,91,215,0.08).

SHADOWS: cards and the sidebar drawer 0 4px 16px rgba(23,39,67,0.1) + 0 1px 3px rgba(23,39,67,0.06);
the filter dropdown panel 0 16px 40px rgba(23,39,67,0.22); dialogs 0 24px 70px rgba(0,0,0,0.25);
secondary button hover 0 3px 10px rgba(23,39,67,0.1); primary button hover
0 8px 20px rgba(15,91,215,0.35); toast 0 14px 38px rgba(23,39,67,0.26) + 0 2px 6px
rgba(23,39,67,0.12). Buttons and cells focus with outline: 2px solid #0f5bd7 at offset 2px; text
inputs and selects focus with a 3px spread ring rgba(15,91,215,0.12), and an invalid input focuses
with rgba(160,43,43,0.14) instead.

FONT: 'Be Vietnam Pro', falling back to 'Segoe UI', Arial, sans-serif — self-hosted, weights 400,
500, 600, 700, 800. Base 13px. Type scale, all in that family: page title in the topbar 15px/700;
card heading 14px/700; the muted count beside it 11px/400 in #4c576b; button label 12px/700; small
in-row button 11px/700; table column header 11px/700 with letter-spacing 0.01em; table cell 12px/400
line-height 1.4; signed change ("delta") text 12px/850, never wrapping; badge 10px/750; filter-count
pill 10px/800; filter chip 11px/700; form label 12px/700; dialog title 15px/800; dialog description
12px/400 line-height 1.5; notice bar text 12px/400; muted caption 11px/400; sidebar nav item 12px/600
(700 when active); sidebar brand text 14px/800; toast title 12px/800; toast body 12px/400 line-height
1.45. Weights 750 and 850 are NOT loaded faces — they resolve upward to 800; that is as-designed.

RADII: 7px buttons, inputs and selects; 9px toolbar, notice bar and filter panel; 12px the table
wrapper; 15px dialogs; 16px the card; 999px badges, the filter-count pill and filter chips.

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
  glyph) containing "Người dùng" (user glyph) and "Phân quyền" (shield glyph). There is NO "Danh mục"
  row in the seed today, so on this route NO sidebar item is highlighted — draw the sidebar exactly
  as seeded and do not invent a menu entry for this screen.
- Topbar: sticky at the top of the content column, z-index 20, background rgba(255,255,255,0.95)
  with backdrop-filter blur(10px), 1px bottom border #7a97bd. Inner row max-width 1600px, centred,
  padding 10px 14px, gap 8px: a hamburger button (visible only at 980px and below), the page title
  in 15px/700, then pushed right the signed-in user's full name in 12px/700 and a secondary button
  reading "Đăng xuất" with a leading sign-out glyph.
- Main: max-width 1600px, centred, padding 14px, on the #cfdaea page background.
- Toast stack: fixed 14px from the right and bottom edges, z-index 60, max-width min(400px, 90vw),
  8px gap, aria-live polite. Each toast: white card, radius 9px, 1px border #7a97bd, a 5px left edge
  in the severity colour, shadow 0 14px 38px rgba(23,39,67,0.26), padding 10px 14px, holding a 22px
  circular severity disc (11px glyph), a 12px/800 optional title over 12px/400 body text at
  line-height 1.45, and a 24px transparent close button on the right. Auto-dismisses after 5 seconds.

LAYOUT — the page. THE PAGE FILLS THE VIEWPORT HEIGHT: the card takes the space that is left rather
than the page growing past it, and the GRID is what scrolls, internally, with the title row, the
notice, the toolbar and the paginator all staying put. Everything below sits in ONE card: #ffffff,
1px border #7a97bd, radius 16px, padding 14px, card shadow, laid out as a flex column.

1. TITLE ROW — flex, space-between, gap 8px, 10px bottom margin: the heading "Danh mục DTI" in
   14px/700 on the left; on the right the live record count "62 chỉ tiêu" in 11px/400 #4c576b, which
   is an aria-live region and is the ONLY thing that tells a screen-reader user a filter ran.

2. NOTICE SLOT — ONE bar, or none. Four mutually exclusive occupants, decided by the year and period
   selections together; never draw two at once. The bar: flex row aligned to the top, gap 8px, fill
   #c4d8f6, 1px #7a97bd border, a 4px #0f5bd7 LEFT border, radius 9px, padding 8px 14px, 12px/400
   text, 14px bottom margin, opening with a #0f5bd7 info-in-circle glyph nudged 2px down, with NO
   close button. In the default state you are drawing there is NO notice at all.

3. TOOLBAR — fill #cfdaea (the page colour, NOT white), 1px #7a97bd border, radius 9px, padding
   8px 10px, gap 8px, flex-wrap, 14px bottom margin. This DOM order is the shipped component's and is
   not negotiable:
   a. SEARCH — fixed 260px, flex none: a leading magnifier glyph 10px from the left edge and an input
      padded 6px 8px 6px 30px on the shared field contract (#ffffff fill, 1px #6077a2 border, radius
      7px, 12px text). The browser's own clear button is suppressed.
   b. FILTER — a native disclosure control whose summary is styled exactly like a default button
      (fill #c4d8f6, text #0f4a9e, 1px #7a97bd, radius 7px, padding 6px 8px, 12px/700) with a leading
      funnel glyph and the label "Lọc", plus a count pill when at least one condition is off its
      default: radius 999px, min-width 16px, height 16px, fill #0f5bd7, text #ffffff at 10px/800. In
      the approved state that pill reads "1". The panel it opens is absolutely positioned 6px below
      the button, z-index 50, 320px wide, #ffffff, 1px #7a97bd, radius 9px, padding 14px, shadow
      0 16px 40px rgba(23,39,67,0.22), holding FOUR conditions stacked as label-over-control rows
      (label 12px/700, 10px bottom margin per row, the last row losing its margin) above a footer
      separated by a 10px top pad over a 1px #7a97bd rule, carrying "Xoá lọc" on the left and a
      primary "Áp dụng" on the right.
   c. SEPARATOR — a flexible 1px #7a97bd vertical hairline, stretched to the bar's height.
   d. CHIPS — one removable pill per APPLIED condition: fill #c4d8f6, 1px #7a97bd, text #0f4a9e at
      11px/700, radius 999px, padding 2px 6px 2px 8px, each ending in a 16px icon-only button
      carrying a real times glyph. In the approved state there is EXACTLY ONE chip. The whole strip
      is absent when there are no chips.
   e. ACTIONS — pushed right, gap 8px: a default button "Import CSV/Excel" and a primary button
      "+ Thêm chỉ tiêu". The "+" is a character inside the label, NOT an icon element.
   Every one of those five is conditional: a bar with no chips or no actions collapses rather than
   leaving a gap.

4. THE GRID — a data grid inside a wrapper with a 1px #6077a2 border and radius 12px, scrolling
   internally between a 220px floor and calc(100dvh - 280px), with the column headers STICKY at the
   top of that scroll box (z-index 4, fill #e9eff6, text #536076, 11px/700, letter-spacing 0.01em,
   padding 6px 8px, left-aligned). Body cells 12px/400 line-height 1.4, padding 6px 8px, 1px bottom
   border #7a97bd, top-aligned; even rows tinted #e9eff6; the hovered row tints #cfdaea. FOURTEEN
   columns, each with its own min-width so the grid scrolls rather than crushing:
     1  "Mã"                  min-width 70px,  left,  bold code. THREE levels occur ("4.22.11"), not
                              just two. FROZEN TO THE LEFT EDGE
     2  "Tên"                 min-width 220px, left,  the full criterion name
     3  "Nhóm"                min-width 120px, left,  the group label
     4  "Kỳ của số liệu"      min-width 110px, left,  the DATE RANGE of the week the row's figures
                              were saved for — "10/08 – 16/08" — with NO week number and NO year.
                              This column is present ONLY in the "Tất cả" mode you are drawing; pick
                              a specific period and it disappears, leaving 13 columns
     5  "Điểm tối đa"         min-width 90px,  right-aligned, an integer
     6  "Tự đánh giá"         min-width 90px,  right-aligned, a decimal with a Vietnamese comma
     7  "Thẩm định"           min-width 90px,  right-aligned, a decimal
     8  "Chênh lệch"          min-width 90px,  right-aligned, a signed change value: 12px/850, never
                              wrapping, two decimals with an explicit sign, GREEN #0e7050 when
                              positive, RED #a02b2b when negative, GREY #4c576b at zero. Examples:
                              "+2,96" green, "−5,00" red, "0,00" grey. It is COMPUTED and never
                              typed — give it no input anywhere
     9  "Trạng thái"          min-width 150px, left, a pill badge: radius 999px, padding 3px 6px,
                              10px/750. "Hoàn thành" → #d9f2e6 fill with #0e7050 text; "Đang thực
                              hiện" → #ffedc7 with #965e08; "Cần bổ sung minh chứng" → #fbdcdc with
                              #a02b2b; "Chưa thực hiện" → #e9eff6 with #4c576b. No assessment yet →
                              no badge, just an em dash
     10 "Phụ trách"           min-width 110px, left, an assignee name or an em dash
     11 "Hạn xử lý"           min-width 100px, left, a date or an em dash
     12 "Tiến độ %"           min-width 130px, right-aligned, INLINE-EDITABLE
     13 "Minh chứng/Ghi chú"  min-width 220px, left, INLINE-EDITABLE, one free-text field — not a
                              list of evidence rows
     14 "Hành động"           min-width 120px, left, two small text buttons: "Sửa" (default) and
                              "Xoá" (danger: #fbdcdc fill, #a02b2b text, #e0a8a8 border, hover fill
                              #f5c6c6), both 11px/700 with padding 4px 6px. FROZEN TO THE RIGHT EDGE
   THE INLINE-EDIT AFFORDANCE, on columns 12 and 13 only: cursor pointer, a transparent 1px DASHED
   bottom border that turns #0f5bd7 on hover, and a 2px #0f5bd7 focus ring at 2px offset. Each such
   cell is keyboard-reachable. Editing swaps the text for a flex row holding either a narrow 74px
   right-aligned number input or a full-width text input, both on the shared field contract. The
   affordance is CONDITIONAL: in any read-only state it is absent entirely — no dashed underline, no
   tab stop, no tooltip — because an affordance that does nothing is worse than none.

5. PAGINATOR — below the grid, outside its scroll region: page buttons plus a rows-per-page select
   offering 10 / 20 / 50, defaulting to 10. There is NO "showing 1–10 of 62" line; the count in the
   title row is the only record count on screen.

6. FOUR DIALOGS — native modal dialogs, radius 15px, #ffffff, padding 14px, shadow
   0 24px 70px rgba(0,0,0,0.25), over an rgba(20,28,40,0.45) backdrop. Title 15px/800 with a close
   control; actions in a footer row separated by a 14px top margin with an 8px gap, primary on the
   right. Only ONE is open at a time; draw each as its own frame:
   6a. "Sửa chỉ tiêu" / "Thêm chỉ tiêu" — width min(560px, 92vw). TWO field groups. Group 1, the
       criterion itself: "Mã" (required, max 20 characters), "Tên chỉ tiêu" (required, a textarea),
       then a two-column pair "Nhóm" (required) + "Điểm tối đa" (required, numeric, min 0.01, step
       0.01). Group 2, the period assessment: a two-column pair "Tự đánh giá" + "Thẩm định" (both
       numeric, min 0, step 0.01), a two-column pair "Trạng thái" (the four values) + "Hạn xử lý" (a
       date), then "Phụ trách" (a select) and "Minh chứng/Ghi chú" (a textarea, min-height 64px).
       Labels 12px/700, required ones carrying a "*" in #a02b2b. A form-level error block sits above
       the footer. Footer: "Huỷ" then primary "Lưu chỉ tiêu". "Chênh lệch" HAS NO FIELD — it is
       computed, and giving it an input would make it enterable.
   6b. Delete confirmation — width min(420px, 92vw), a 40px circular danger icon disc holding a trash
       glyph, a title, a message, and EXACTLY TWO buttons: "Huỷ" and a danger "Xoá".
   6c. "Import CSV/Excel" — width min(560px, 92vw): one file input accepting .csv, .xlsx and .xls, a
       muted line echoing the chosen filename, then "Huỷ" and primary "Nhập dữ liệu".
   6d. "Kết quả Import" — width min(560px, 92vw): a summary paragraph whose success count is green
       #0e7050 and failure count red #a02b2b, over an unordered list of per-row failures each naming
       the row number, the code and the reason, in red. One primary button "Đóng".

COPY (verbatim Vietnamese — reproduce character for character; do not translate, do not correct, do
not shorten. Keep every three-dot "..." exactly as three dots, every en dash "–" WITH its surrounding
spaces, and every "—" em dash used as a null placeholder).

The app HAS a runtime translation layer: @ngx-translate/core v18 reading src/FE/public/i18n/vi.json
and en.json. The Vietnamese below is the **rendering of the `vi` bundle** — what a Vietnamese user
sees on screen, which is what you must draw. It is NOT markup to copy.

MARKUP CONTRACT (applies to any HTML/Angular you emit, not to the picture): every user-facing string
must be bound through a translation key — {{ 'danh-muc-dti.…' | translate }} or
[attr.aria-label]="'danh-muc-dti.…' | translate" — with the Vietnamese sentence added to vi.json and
an English sibling added to en.json. Do NOT paste a Vietnamese literal into a template: the build
gate scans every .html file for Vietnamese diacritics and FAILS on a hit. No key for this screen
exists yet, because the screen has never shipped; allocating the `danh-muc-dti.*` keys is part of
building it.

- Browser tab title: "PlatformManager". Sidebar brand: "PM" and "PlatformManager". Collapse button
  accessible label toggles between "Mở rộng menu" and "Thu gọn menu". Seeded nav labels:
  "Trang chủ", "Quản trị hệ thống", "Người dùng", "Phân quyền".
- Topbar: hamburger accessible label "Mở menu điều hướng"; logout label and tooltip "Đăng xuất".
  Toast close accessible label "Đóng thông báo".
- Card heading: "Danh mục DTI". Card count: "62 chỉ tiêu"
- Search placeholder "Tìm mã hoặc tên chỉ tiêu..." (THREE DOTS, not an ellipsis character); its
  accessible name "Tìm mã hoặc tên chỉ tiêu"
- Filter trigger "Lọc", with the count pill reading "1" in the approved state
- Filter condition 1: "Nhóm chỉ tiêu" → "Tất cả nhóm" plus the six group names
  ("1. Hạ tầng và Nền tảng số" · "2. Nhân lực số" · "3. An toàn thông tin, an ninh mạng" ·
  "4. Hoạt động chính quyền số" · "5. Hoạt động Kinh tế số" · "6. Hoạt động Xã hội số")
- Filter condition 2: "Trạng thái" → "Tất cả trạng thái" · "Chưa thực hiện" · "Đang thực hiện" ·
  "Cần bổ sung minh chứng" · "Hoàn thành"
- Filter condition 3: "Năm đánh giá" → "2026" · "2025" · "2024"
- Filter condition 4: "Kỳ trong năm" → "Tất cả (mới nhất trong năm)" ·
  "Tuần 33: 10/08 – 16/08/2026" · "Tuần 34: 17/08 – 23/08/2026" · "Tuần 35: 24/08 – 30/08/2026" ·
  "Tháng 8: 01/08 – 31/08/2026" · "Tháng 7: 01/07 – 31/07/2026". Its help tooltip:
  "Xem tổng hợp cả năm (mới nhất mỗi chỉ tiêu) hoặc 1 kỳ cụ thể đã lưu trong năm. Mỗi kỳ ghi rõ từ ngày nào tới ngày nào"
- Filter footer: "Xoá lọc" and "Áp dụng"
- The one applied chip: "Nhóm: 1. Hạ tầng và Nền tảng số" — ONE chip, not two; the current year is a
  default and raises none. Its remove button's accessible label is
  "Bỏ lọc Nhóm: 1. Hạ tầng và Nền tảng số", drawn as a real times glyph with NO title attribute
- Toolbar actions: "Import CSV/Excel" and "+ Thêm chỉ tiêu"
- Grid headers: "Mã" · "Tên" · "Nhóm" · "Kỳ của số liệu" · "Điểm tối đa" · "Tự đánh giá" ·
  "Thẩm định" · "Chênh lệch" · "Trạng thái" · "Phụ trách" · "Hạn xử lý" · "Tiến độ %" ·
  "Minh chứng/Ghi chú" · "Hành động"
- Period cell values, as drawn in the approved six rows: "10/08 – 16/08" · "10/08 – 16/08" ·
  "27/07 – 02/08" · "20/07 – 26/07" · "13/07 – 19/07" · "10/08 – 16/08" — different rows legitimately
  come from different weeks in this mode, which is the entire reason the column exists
- Status values, the complete set of four: "Chưa thực hiện" · "Đang thực hiện" ·
  "Cần bổ sung minh chứng" · "Hoàn thành"
- Empty assignee / deadline cell: "—". Empty note cell: "— bấm đúp để ghi chú"
- Inline-edit tooltips: "Bấm đúp để sửa Tiến độ %" and "Bấm đúp để sửa Minh chứng/Ghi chú"
- Row actions: "Sửa" and "Xoá"
- Rows-per-page select accessible name "Số dòng mỗi trang" → "10" · "20" · "50"
- Form dialog heading "Sửa chỉ tiêu" when editing, "Thêm chỉ tiêu" when creating; its close control
  "Đóng"
- Form labels: "Mã" · "Tên chỉ tiêu" · "Nhóm" · "Điểm tối đa" · "Tự đánh giá" · "Thẩm định" ·
  "Trạng thái" · "Hạn xử lý" · "Phụ trách" · "Minh chứng/Ghi chú"
- Form placeholders: "vd 1.1" · "Nhập tên đầy đủ chỉ tiêu..." · "vd 10" · "vd 5" · "vd 0" ·
  "Số văn bản - ngày - trích yếu, mỗi minh chứng 1 dòng..."
- Assignee, unassigned option: "— Chưa phân công —"
- Form validation error: "Mã chỉ tiêu \"1.4\" đã tồn tại trong nhóm này."
- Form actions: "Huỷ" and "Lưu chỉ tiêu"
- Delete confirmation heading "Xác nhận"; message "Xoá chỉ tiêu \"1.4 — Mức độ ứng dụng AI\"?";
  actions "Huỷ" and "Xoá"
- Import dialog heading "Import CSV/Excel"; file label "Chọn file CSV/Excel"; chosen-file line
  "Đã chọn: <tên file>" in muted; actions "Huỷ" and "Nhập dữ liệu"
- Import result heading "Kết quả Import"; summary
  "Tổng 62 dòng — 59 thành công, 3 lỗi. Đã tự tạo mới 2 chỉ tiêu."; the three per-row failures:
  "Dòng 17 — mã \"4.2\": Điểm tự đánh giá (6) vượt quá điểm tối đa (5)." ·
  "Dòng 42 — mã \"4.15\": Trạng thái \"Đang xử lý\" không thuộc 4 giá trị hợp lệ." ·
  "Dòng 55 — mã \"6.3\": Không tìm thấy nhóm \"Xã hội số\" (sai chính tả hoặc thừa khoảng trắng)."
  ; action "Đóng"
- Target-period banner, a past week selected:
  "Đang nhập cho Tuần 31/2026 (27/07 – 02/08/2026). Số liệu bạn sửa sẽ lưu vào tuần này, không phải tuần hiện tại."
  ; in "Tất cả" mode it becomes "Đang nhập cho Tuần 33/2026 (10/08 – 16/08/2026) — tuần hiện tại."
- Aggregate read-only banner, a MONTH selected:
  "Đang xem số liệu tổng hợp của Tháng 8/2026 (01/08 – 31/08/2026) — chỉ đọc. Chọn một tuần cụ thể trong ô \"Kỳ trong năm\" để nhập hoặc sửa số liệu."
- Past-year read-only banner:
  "Đang xem số liệu năm 2025 — chỉ đọc. Chuyển ô \"Năm đánh giá\" về 2026 để nhập hoặc sửa số liệu."
- Empty-catalogue banner:
  "Danh mục chưa có chỉ tiêu nào. Dùng nút \"Import CSV/Excel\" ở trên để nhập danh mục từ file."
- ⚠ ONE STRING MUST NEVER BE DRAWN. The retired sentence
  "Đang xem dữ liệu lịch sử — chỉ đọc. Quay lại \"Tất cả (mới nhất trong năm)\" của năm hiện tại để chỉnh sửa."
  states the OPPOSITE of the current rule: a past week is fully editable. Do not reproduce it from
  any older material.

STATES:
- Default (a week or "Tất cả" is selected, current year, the user may write): every write affordance
  is live — both toolbar actions, the two inline-editable columns, and the per-row "Sửa" / "Xoá". NO
  notice bar. This is the state to draw first.
- Editing a PAST WEEK: still fully editable — AGE IS NOT A REASON FOR READ-ONLY on this screen. The
  same controls do the same jobs; the only change is that the target-period banner appears, naming
  the week the writes will land in.
- A MONTH is selected — read-only: the two toolbar buttons stay VISIBLE AND DISABLED (opacity 0.5,
  cursor not-allowed), the per-row "Sửa" / "Xoá" do the same, and the two editable columns lose their
  affordance. Search, all four filter conditions, sorting and paging are untouched. The aggregate
  banner explains why. A month is not a period anything is saved to — it is the sum of its weeks.
- A PAST YEAR is selected — read-only, drawn exactly like the month case, with the past-year banner.
- The user has NO write permission — read-only, but NOT locked out, and this state HIDES rather than
  disables: both toolbar action buttons are GONE (with them gone the action group collapses and
  leaves no gap), the "Hành động" column is GONE ENTIRELY — column and header, since an empty frozen
  column would be a permanent 120px of nothing, leaving 13 columns — and the inline-edit treatment is
  absent. Search, filters, chips, sorting and paging remain. There is NO banner and no explanatory
  copy in this state, deliberately. A disabled control says "not right now"; an absent one says "not
  for you", and the two must not be drawn the same way.
- Loading: the grid pages server-side, so every filter, sort and page change is a round trip, and the
  data grid's OWN loading mask covers it. This screen adds NO loading treatment of its own — no dim,
  no page spinner, no skeleton. One loading mechanism per surface.
- Empty, no rows match the filters: the grid's own empty message inside the table, and the count in
  the title row drops to the filtered number.
- Empty, the catalogue has never had data: the empty-catalogue banner ABOVE the toolbar — not a blank
  row inside the grid. Combined with the no-permission state it drops its second sentence, leaving
  "Danh mục chưa có chỉ tiêu nào." alone, because the button it points at is not on that user's
  screen.
- After a save while "Tất cả" is selected: the row's "Kỳ của số liệu" cell switches to the CURRENT
  week's date range. That switch is the whole of the feedback — no dialog, no confirmation step. The
  figures stay where the user typed them; only the range moves.
- Error: a failed request surfaces as a toast from the shell. Failures inside a dialog render in a
  form-level error block inside that dialog instead, so the user does not lose the form.
- Validation, three deliberately different surfaces: field level (required markers on the four
  mandatory fields, native numeric constraints, a 20-character cap on "Mã" — and note the code is NOT
  two-level, "4.22.11" is valid data); form level (one error block above the actions, e.g. a
  duplicate code within a group); import level (per-row failures listed in the result dialog, each
  naming row number, code and reason).
- Print: the toolbar disappears. The grid keeps its height cap, so a printed page truncates it
  silently — draw that as it is.

RESPONSIVE:
- 981px and up (desktop default): the page fills the viewport height and the grid scrolls inside its
  own region between 220px and calc(100dvh - 280px) while the card, toolbar and paginator stay put.
- AT EVERY WIDTH the grid scrolls horizontally and does not restack. Fourteen columns with explicit
  minimum widths sum well past a laptop viewport; that is the design. The two frozen edge columns
  stay pinned at every breakpoint — the pin has no responsive variant.
- 980px and below: the sidebar becomes an off-canvas drawer, width min(85vw, 300px), over an
  rgba(20,28,40,0.45) backdrop, and the topbar hamburger appears; the toolbar wraps. The grid is
  unchanged.
- 560px and below: the two-column field pairs inside the "Sửa chỉ tiêu" dialog stack to one column;
  main padding shrinks and the topbar hides the user's name; the toolbar's children stretch to full
  width. The grid is STILL unchanged — on a 390px screen the user scrolls sideways through fourteen
  columns, and the two frozen edges (70px + 120px) leave under 200px of scrollport between them. Draw
  that honestly; it is the least comfortable moment in the product and it is real.
- The screen owns no media query of its own. Every breakpoint effect above comes from the shell or
  the global layer.
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

3. Generate the **default writable state** first, at a width wide enough to show the horizontal
   scroll honestly. Then, as separate frames: the grid **scrolled to its right-hand end** (the only
   view that proves the two pinned columns), the **month read-only** state (banner + two dimmed
   toolbar buttons + no editable cells), the **no-write-permission** state (no action group at all
   and no `Hành động` column — 13 columns), and one frame per dialog. Reject any output that shows
   two dialogs at once, restacks the grid into cards, or gives `Chênh lệch` an input.

4. Optional, only after a successful import — Stitch resolves the dictionary's own names, so these
   are interchangeable with the literals above: `colors.primary` / `colors.brand` (#0f5bd7) ·
   `colors.bg` (#cfdaea) · `colors.card` (#ffffff) · `colors.tonal-bg` (#c4d8f6) ·
   `colors.tonal-ink` (#0f4a9e) · `colors.btn-hover-bg` (#c7dbf5) · `colors.muted` (#4c576b) ·
   `colors.line` (#7a97bd) · `colors.border-strong` (#6077a2) · `colors.on-primary` (#ffffff) ·
   `colors.surface-table-header` (#e9eff6) · `colors.th-ink` (#536076) · `colors.good` (#0e7050) ·
   `colors.good-bg` (#d9f2e6) · `colors.warn` (#965e08) · `colors.warn-bg` (#ffedc7) ·
   `colors.bad` (#a02b2b) · `colors.bad-bg` (#fbdcdc) · `colors.danger-border` (#e0a8a8) ·
   `colors.danger-hover-bg` (#f5c6c6) · `rounded.sm` (7px) · `rounded.md` (9px) · `rounded.lg`
   (16px) · `rounded.dialog` (15px) · `rounded.table` (12px) · `rounded.pill` (999px) ·
   `spacing.card-padding` (14px) · `spacing.cell-padding` (6px 8px) · `spacing.toolbar-padding`
   (8px 10px) · `spacing.notice-padding` (8px 14px) · `spacing.badge-padding` (3px 6px) ·
   `spacing.filter-chip-padding` (2px 6px 2px 8px) · `spacing.button-sm-padding` (4px 6px) ·
   `typography.h2-title` (14px/700) · `typography.table-header` (11px/700) ·
   `typography.table-cell` (12px/400) · `typography.delta` (12px/850) · `typography.badge`
   (10px/750) · `typography.filter-count` (10px/800) · `typography.filter-chip` (11px/700) ·
   `typography.form-label` (12px/700) · `typography.dialog-title` (15px/800) ·
   `typography.notice` (12px/400) · `typography.muted-caption` (11px/400).

   > ⚠️ Four token names changed on 2026-08-29. If you are working from an older prompt or an older
   > Stitch project, replace `tonal-bg-hover` → `btn-hover-bg`, `bad-bg-hover` → `danger-hover-bg`,
   > `text-table-header` → `th-ink`, and both `bad-border-btn` and `bad-border-notice` →
   > `danger-border`.

5. **Attach no screenshot.** `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png` shows
   the **deleted** pre-2026-08-29 build — an 11-column grid with plain-text status and no
   `Chênh lệch` column, in the old palette and the old type face. Feeding it to a generator overrides
   the correct literals above with a picture of a screen that is not this design.

This repo has no Stitch MCP configured — do the import manually via stitch.withgoogle.com (see
`doc/Design/SETUP.md` to add one).

## Claude Design

Paste the **Master Prompt** above and add the four notes plus the token block below. Attach
**nothing**: `Assets/Brand/` does not exist (the shipped app has no logo or brand image file; the
"PM" mark is a styled text square), and the only screenshot on disk shows the deleted build — see
§ Assets to Attach.

**Note 1 — no reference image exists for this design, and none can be captured.** The screen is not
built. The Master Prompt is written to be complete without one: every colour, size, radius and string
in it is a literal.

**Note 2 — fourteen columns that scroll sideways is the design.** Do not "help" by restacking the
grid into cards, hiding columns on narrow viewports, or turning it into a master-detail layout. Do
keep the first and last columns pinned against that scroll.

**Note 3 — read-only comes in two visually different flavours, and the difference is the point.** A
month or a past year DISABLES the write controls in place (they stay visible at opacity 0.5); a
missing permission REMOVES them (and takes the whole `Hành động` column with it). Never merge the two
treatments.

**Note 4 — `Chênh lệch` is computed.** It has no input, no field in the dialog and no editable cell.
Two columns are inline-editable and only two: `Tiến độ %` and `Minh chứng/Ghi chú`.

Restate the tokens as this CSS block — these are the shipped custom-property names and values, copied
1:1 from `src/FE/src/styles.scss`, so generated CSS drops straight into the app:

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

  --surface-nav-active: rgba(15, 91, 215, 0.08);
  --overlay-backdrop: rgba(20, 28, 40, 0.45);
  --surface-topbar: rgba(255, 255, 255, 0.95);
  --shadow-toast: 0 14px 38px rgba(23, 39, 67, 0.26), 0 2px 6px rgba(23, 39, 67, 0.12);

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
fourteen per-column `min-width` values listed in the Master Prompt; the 260px search field; the 320px
filter panel and its `0 16px 40px rgba(23,39,67,0.22)` shadow; the filter-count pill at 16x16px and
10px/800; the badge `padding: 3px 6px` at 10px/750; the filter chip `padding: 2px 6px 2px 8px`; the
74px inline number input; the dialog widths `min(560px, 92vw)` for the form and import dialogs and
`min(420px, 92vw)` for the confirmation; the 40px dialog icon disc; the textarea `min-height: 64px`;
and the disabled treatment `opacity: .5; cursor: not-allowed`.

## Google AI Studio

**System instruction** = the `CONTEXT:`, `PALETTE:`, `SHADOWS:`, `FONT:`, `RADII:` and
`SPACING SCALE:` sections of the Master Prompt above, pasted verbatim. They are the invariant part —
they describe the design system, not this screen. Append this paragraph, which is specific to this
screen:

```
This screen is one card holding a fourteen-column data grid that scrolls horizontally, with its first
and last columns pinned to the edges. Never restack it, never hide columns per viewport, never
substitute an accordion or a master-detail layout. Exactly two columns are inline-editable
("Tiến độ %" and "Minh chứng/Ghi chú"); "Chênh lệch" is computed and has no input anywhere. Only one
of the four dialogs can be open at a time. Read-only from a month or a past year DISABLES the write
controls in place; read-only from a missing permission REMOVES them and drops the actions column
entirely — never draw those two the same way. The product is translated with @ngx-translate (bundles
vi.json + en.json): every Vietnamese string you are given is the rendering of the vi bundle — draw it
character for character, and if you emit markup rather than a picture, bind each string through a
translation key instead of inlining it.
```

**User prompt** = the `LAYOUT:`, `COPY:`, `STATES:` and `RESPONSIVE:` sections of the Master Prompt
above, pasted verbatim. Run it once for the default writable state and once per dialog.

**Image part** = none. Do not attach
`Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png`; it shows the build deleted on
2026-08-29, and a multimodal model will trust the picture over the text. Add this instead: "No
reference image is supplied. Follow the written specification exactly; every colour, size and string
in it is literal."

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable,
Figma AI, …). The block is self-contained — no token resolution, no other file, no follow-up prompt
and no reference image are required. Ask for the default state plus one frame per dialog, and reject
any output that restacks the grid, unpins the edge columns, gives `Chênh lệch` an input, or draws the
retired "Đang xem dữ liệu lịch sử — chỉ đọc" sentence.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Tokens/tokens.json` — W3C DTCG token file (`global` + `light`; `dark` is intentionally empty, the
  app ships one theme).
- `DESIGN.md` — lint-clean token dictionary, for the Stitch import.
- `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png` — **one path, two different
  images, and that is the problem.** The file on disk today is a capture of the **deleted**
  pre-2026-08-29 build: an 11-column grid with plain-text status and no `Chênh lệch` column, in the
  old palette and the old type face. **Do not attach it.** It is kept for before/after comparison,
  and its filename is the one the new shot will want — move or rename it before capturing, or the two
  will be silently confused. The *new* shot under that name is **pending and blocked: the screen is
  not built.** It is the one shot that must exist once it is, and in `Tất cả` mode it carries all
  fourteen columns. Capture instructions live in `Screens/02-danh-muc-dti.md` § Screenshots, which
  also lists the on-demand variants (period mode, frozen-scroll, filtered, edit dialog, import
  result, past period, month read-only, no-write-permission, mobile).
- `Assets/Brand/` — **none exist.** The app ships no logo or brand image file; the "PM" mark is a
  26x26px square filled #0f5bd7 with white 11px/800 text.
