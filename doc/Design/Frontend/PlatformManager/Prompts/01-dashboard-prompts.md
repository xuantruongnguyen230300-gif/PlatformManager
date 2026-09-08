---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "draft — target, not built"
updated: "2026-09-08"
screen_ref: "01-dashboard"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — DTI Dashboard (`/trang-chu`)

<!-- One pack for Screens/01-dashboard.md. The Master Prompt is filled from that spec plus
     Tokens/tokens.json (light set — the app's only shipped theme), whose values were
     re-checked against the :root block of src/FE/src/styles.scss on 2026-09-08. Copy is
     verbatim Vietnamese from the screen spec's § Copy table. Fidelity rule: reproduce the
     approved design exactly — quirks included, nothing idealized, nothing translated.

     Everything a tool needs is inside THIS file: every token is resolved to a literal
     hex/px/font value, and no other spec has to be opened. -->

> **Regenerated 2026-09-08 from `Screens/01-dashboard.md`. The revision it replaces was
> `status: "historical"` and would have produced the wrong screen three times over.**
> That pack described the `/dashboard` route deleted on 2026-08-29, drew it in the
> pre-2026-08-29 palette (`#eef2f8`, `#dfe6ef`, `#57647a`, `#7e91b4`, `#f8fafc`) with `Inter`
> as the type face, and emitted the retired custom properties `--tonal-bg-hover` and
> `--text-table-header`. All three are gone from this file.
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
> 2. **The app shell around it *is* built**, and every shell value in the Master Prompt is read
>    from the live `src/FE/src/styles.scss` and the live sidebar / topbar / toast components.
>    The seeded sidebar rows come from `src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs`
>    § `Items`.

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is a literal. -->

```
Recreate this exact shipped screen — do not idealize, do not translate, do not "improve" anything.

CONTEXT: an internal Vietnamese-language administration platform ("PlatformManager"). This screen is
the DTI Dashboard, and it sits at the app's LANDING URL /trang-chu — it is the first thing a
signed-in user sees, not a page reached from a menu. Every signed-in account may see it; there is no
permission state and no "you lack access" screen to draw. It renders INSIDE the app shell (fixed left
sidebar + sticky topbar + centred main + fixed toast stack).

⚠ THE SCREEN IS 100% READ-ONLY. It displays numbers and nothing else. There is exactly ONE action on
it — a button reading "Xuất báo cáo" which downloads an .xlsx file directly, with NO dialog, NO
preview and NO modal — plus query controls that change what is displayed: two period selects, a
two-segment week/month switch, a search box, a filter panel and a sort select. Do NOT draw an
editable cell, a save button, a backup/restore action, a floating action button or a paginator. All
data entry in this product lives on a different screen (/danh-muc/dti).

PALETTE (recomputed from the WCAG contrast formula, not picked by eye — use these exact values):
page background #cfdaea; card + input surface #ffffff; ghost-button hover tint #c1cde2; default
(secondary) button fill #c4d8f6 with text #0f4a9e and hover fill #c7dbf5; body text #152033; muted
text #4c576b; component hairline #7a97bd (card border, toolbar border, segmented group and divider,
table row rules — this measures 3.00:1 on white, it is a REAL visible border, not a faint one);
stronger border #6077a2 reserved for inputs, the read-only period value box and the table wrapper;
brand #0f5bd7 with hover #174ca8 and white text on it; success #0e7050 on #d9f2e6; warning #965e08 on
#ffedc7; danger #a02b2b on #fbdcdc; table header surface #e9eff6 with header text #536076 (the same
#e9eff6 tints every even body row); progress-bar track #dbe4f0; the notice bar fill is #c4d8f6 with a
#0f5bd7 left rule; topbar surface rgba(255,255,255,0.95) with backdrop-filter blur(10px); drawer
backdrop rgba(20,28,40,0.45); active sidebar item tint rgba(15,91,215,0.08).

SHADOWS: cards and the sidebar drawer 0 4px 16px rgba(23,39,67,0.1) + 0 1px 3px rgba(23,39,67,0.06);
the filter dropdown panel 0 16px 40px rgba(23,39,67,0.22); secondary button hover
0 3px 10px rgba(23,39,67,0.1); primary button hover 0 8px 20px rgba(15,91,215,0.35); toast
0 14px 38px rgba(23,39,67,0.26) + 0 2px 6px rgba(23,39,67,0.12). Controls focus with
outline: 2px solid #0f5bd7 at offset 2px — except a segment of the week/month switch, whose ring is
offset -2px (inset) because the group clips overflow. Text inputs and selects focus instead with a
3px spread ring rgba(15,91,215,0.12).

FONT: 'Be Vietnam Pro', falling back to 'Segoe UI', Arial, sans-serif — self-hosted, weights 400,
500, 600, 700, 800. Base 13px. Type scale, all in that family: page title in the topbar 15px/700;
card heading 14px/700; the muted caption beside a card heading 11px/400 in #4c576b; segment label
12px/700; button label 12px/700; small in-row button 11px/700; table column header 11px/700 with
letter-spacing 0.01em; table cell 12px/400 line-height 1.4; KPI label 11px/400 in #4c576b; KPI value
21px/850; KPI sub-caption 11px/400 in #4c576b with line-height 1.4; group-progress row 12px/400 with
its percentage bold; signed change ("delta") text 12px/850, never wrapping; badge 10px/750; history
row 11px/400; notice bar text 12px/400; footer 11px/400 in #4c576b; sidebar nav item 12px/600 (700
when active); sidebar brand text 14px/800; toast title 12px/800; toast body 12px/400 line-height
1.45. Weights 750 and 850 are NOT loaded faces — they resolve upward to 800; that is as-designed.

RADII: 7px buttons, the segmented group and the read-only period value box; 9px toolbar, notice bar,
filter panel, sidebar nav item and toast; 12px the table wrapper; 16px the cards; 999px badges and
progress bars.

SPACING SCALE (the whole app uses only these five steps): 4 / 6 / 8 / 10 / 14px. Two literals on this
screen sit off that scale and must be drawn as written: the 16px top margin on the detail-table card
and on the history card.

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
  glyph) containing "Người dùng" (user glyph) and "Phân quyền" (shield glyph). ON THIS ROUTE
  "Trang chủ" is the active item. There is NO "Danh mục" or "DTI" row in the seed today — do not
  invent one.
- Topbar: sticky at the top of the content column, z-index 20, background rgba(255,255,255,0.95)
  with backdrop-filter blur(10px), 1px bottom border #7a97bd. Inner row max-width 1600px, centred,
  padding 10px 14px, gap 8px: a hamburger button (visible only at 980px and below), the page title
  in 15px/700, then pushed right the signed-in user's full name in 12px/700 and a secondary button
  reading "Đăng xuất" with a leading sign-out glyph.
- Main: max-width 1600px, centred, padding 14px, on the #cfdaea page background. The six regions
  below are its direct children, in this order.
- Toast stack: fixed 14px from the right and bottom edges, z-index 60, max-width min(400px, 90vw),
  8px gap, aria-live polite. Each toast: white card, radius 9px, 1px border #7a97bd, a 5px left edge
  in the severity colour, shadow 0 14px 38px rgba(23,39,67,0.26), padding 10px 14px, holding a 22px
  circular severity disc (11px glyph), a 12px/800 optional title over 12px/400 body text at
  line-height 1.45, and a 24px transparent close button on the right. Auto-dismisses after 5 seconds.

LAYOUT — the page:

1. PERIOD TOOLBAR, the first region and the screen's primary control. It is a toolbar, NOT a card —
   do not wrap it in a white panel: fill #cfdaea (the page colour), 1px border #7a97bd, radius 9px,
   padding 8px 10px, gap 8px, flex-wrap, 14px bottom margin. Left to right:
   - the bold label "Kỳ đang xem:";
   - a READ-ONLY value box (not an input, not a select, not a chip): 1px #6077a2 border, radius 7px,
     fill #cfdaea, text #4c576b at 12px, padding 6px 8px, white-space nowrap so the full period label
     never wraps;
   - a year select, then EXACTLY ONE period select — the week list or the month list, never both, so
     this bar can never grow a second row;
   - a two-segment switch: inline-flex, sized to its content, 1px #7a97bd border, radius 7px,
     overflow hidden, a 1px #7a97bd divider between the segments. Each segment: no border, padding
     6px 10px, 12px/700, #4c576b on #ffffff; hover fills #cfdaea; the selected segment fills #0f5bd7
     with #ffffff text. Labels "Tuần" and "Tháng";
   - a flexible 1px #7a97bd vertical hairline separator, then the action group pushed right holding
     ONE primary button "Xuất báo cáo" with a leading Excel-file glyph.
   This bar has NO filter control and NO search. Choosing a period is the page's primary act, not a
   filter condition.

2. KPI ROW: a 5-column grid of equal columns with a 10px gap, holding five stat cards. Each card is
   an ordinary card (#ffffff, 1px #7a97bd, radius 16px, padding 14px, card shadow) with a fixed
   three-line anatomy and nothing else — no icon, no sparkline, no second number:
   line 1 the label, 11px/400 in #4c576b; line 2 the value, 21px/850, 4px top margin, tinted by tone;
   line 3 the sub-caption, 11px/400 in #4c576b, line-height 1.4, with a reserved min-height of 30px
   so five tiles stay baseline-aligned when one caption wraps. TONE COLOURS THE VALUE ONLY — never
   the label, the caption or the card. The five tiles, in order, with their populated values:
     1. "Tiến độ chung tuần này" — "82,1%" — default tone (#152033) —
        "Bình quân gia quyền theo điểm (thật: 787,84/960)"
     2. "So với tuần trước" — "↑ 2,3 đ.%" — good tone (#0e7050) — "Tuần 32/2026 (03/08–09/08/2026)"
     3. "Chỉ tiêu tăng" — "18" — good tone (#0e7050) — "Có tiến bộ so với kỳ trước"
     4. "Không tăng" — "27" — warn tone (#965e08) — "Cần chú ý theo dõi"
     5. "Hoàn thành" — "26/62" — default tone (#152033) —
        "Số chỉ tiêu ở trạng thái Hoàn thành (thật)"
   Tiles 3 and 4 keep their tone at every value: "Chỉ tiêu tăng: 0" still renders green. That is
   as-designed, not a bug to fix.

3. TWO-COLUMN REGION, 14px below the KPI row: a grid of 1.15fr and 0.85fr with a 14px gap. Each
   child is a card laid out as a flex column so its body fills the remaining height.
   3a. LEFT CARD — a title row (flex, space-between, gap 8px, 10px bottom margin) with the heading
       "Tiến độ theo nhóm" in 14px/700 on the left and the caption "Tuần hiện tại" in 11px/400
       #4c576b on the right; below it six progress rows distributed down the full card height
       (flex column, space-between, 8px gap). Each row is a three-column grid — 210px / 1fr / 80px,
       gap 8px, vertically centred, 12px text:
         column 1 the bold group name; column 2 the track; column 3 the bold percentage in its
         reserved 80px.
       The track is a 9px-tall pill (radius 999px) filled #dbe4f0 with overflow hidden, holding one
       fill: radius 999px, height 100%, background #0f5bd7, width = the percentage. ONE COLOUR ONLY —
       there is no good/warn/bad threshold recolouring; a group at 51,8% and one at 100,0% differ by
       length, not by hue. The bar carries NO label inside the track. The six rows:
         "1. Hạ tầng và Nền tảng số" 74,3% · "2. Nhân lực số" 51,8% ·
         "3. An toàn thông tin, an ninh mạng" 100,0% · "4. Hoạt động chính quyền số" 85,1% ·
         "5. Hoạt động Kinh tế số" 100,0% · "6. Hoạt động Xã hội số" 67,6%
   3b. RIGHT CARD — a title row with "Biểu đồ tiến độ hàng tuần" in 14px/700 and the caption
       "Tiến độ chung" in 11px/400 #4c576b; below it a single-series line chart, width 100%, height
       exactly 220px at EVERY breakpoint. It paints no surface of its own — no background, border,
       radius or shadow; the white card behind it is the surface. Painting rules: series line 2px
       stroke #0f5bd7 with no fill; points radius 4 with a 1.5px #ffffff stroke; the area under the
       line filled #0f5bd7 at 12% opacity (rgba(15,91,215,0.12)) with no stroke; horizontal grid
       lines 1px #7a97bd on the Y AXIS ONLY — the x axis draws none; tick labels on both axes 11px
       #4c576b. NO legend — one series needs none. The y axis is pinned to [0, 100] with a "%" tick
       suffix, drawn as 100% / 75% / 50% / 25% / 0%, so two periods are always comparable and a run
       of high values never rescales the axis. The line is STRAIGHT-SEGMENT, not smoothed, and a
       missing period keeps its slot and BREAKS the line rather than being dropped — a gap in the
       data must read as a gap. X-axis labels in week mode are date ranges:
       "06/07 – 12/07" · "13/07 – 19/07" · "20/07 – 26/07" · "27/07 – 02/08" · "03/08 – 09/08" ·
       "10/08 – 16/08". In month mode they are the short forms "Th.1" … "Th.12", NOT date ranges.

4. DETAIL TABLE CARD, 16px below the two-column region (a literal, not on the spacing scale):
   4a. Title row: "62 chỉ tiêu DTI" in 14px/700 on the left, and on the right the live count
       "62/62 chỉ tiêu" in 11px/400 #4c576b, which is an aria-live region.
   4b. A second toolbar — same box as the period toolbar (fill #cfdaea, 1px #7a97bd, radius 9px,
       padding 8px 10px, gap 8px, 14px bottom margin), in THIS DOM order:
       search field (fixed 260px, flex none, a leading magnifier glyph 10px from the left edge and an
       input padded 6px 8px 6px 30px on the shared field contract: #ffffff fill, 1px #6077a2 border,
       radius 7px, 12px text) → a native disclosure "Lọc" button carrying a leading funnel glyph and
       styled exactly like a default button → the flexible 1px #7a97bd separator → the
       applied-condition chip strip → the action group. In the state you are drawing there is NO
       count badge on "Lọc" and NO chip, matching the unfiltered "62/62 chỉ tiêu" in the title.
       The action group holds ONE sort select. Sorting is deliberately NOT inside the filter panel.
       The filter panel itself (draw it only if asked for the open state) is absolutely positioned
       6px below the button, z-index 50, 320px wide, #ffffff, 1px #7a97bd, radius 9px, padding 14px,
       shadow 0 16px 40px rgba(23,39,67,0.22), holding TWO conditions over a footer row that is
       separated by a 10px top pad above a 1px #7a97bd rule and carries "Xoá lọc" on the left and a
       primary "Áp dụng" on the right.
   4c. A PLAIN TABLE inside a wrapper — 1px #6077a2 border, radius 12px, overflow auto, max-height
       calc(100dvh - 280px) with a 220px floor. NO PAGINATOR, no rows-per-page control, no lazy
       loading, no row selection, no bulk actions: this region is read-only and shows the whole
       period at once. Do NOT substitute a data-grid component. Column headers are sticky to the top
       of the scroll box at z-index 4, fill #e9eff6, text #536076, 11px/700, letter-spacing 0.01em,
       padding 6px 8px, left-aligned. Body cells 12px/400 line-height 1.4, padding 6px 8px, 1px
       bottom border #7a97bd, top-aligned; even rows tinted #e9eff6; the hovered row tints #cfdaea.
       NINE columns, in this order, with these widths:
         1 "Mã" left 5% — the bold criterion code; THREE levels occur (e.g. "4.22.11"), not just two
         2 "Chỉ tiêu" left 26% — the full criterion name
         3 "Nhóm" left 13% — the group label, e.g. "1. Hạ tầng và Nền tảng số"
         4 "Điểm tối đa" right-aligned 8% — an integer
         5 "Tự đánh giá" right-aligned 9% — a decimal with a Vietnamese comma
         6 "Thẩm định" right-aligned 9% — a decimal
         7 "Chênh lệch" right-aligned 9% — a signed change value: 12px/850, never wrapping, two
           decimals with an explicit sign, GREEN #0e7050 when positive, RED #a02b2b when negative,
           GREY #4c576b at zero. Real examples: "+2,96" green, "−5,00" red, "0,00" grey
         8 "Trạng thái" left 10% — a pill badge: radius 999px, padding 3px 6px, 10px/750. Colour by
           value: "Hoàn thành" → #d9f2e6 fill with #0e7050 text; "Đang thực hiện" → #ffedc7 with
           #965e08; "Cần bổ sung minh chứng" → #fbdcdc with #a02b2b; "Chưa thực hiện" → #e9eff6 with
           #4c576b. A row with no assessment renders no badge, just an em dash
         9 "Minh chứng/Ghi chú" left 11% — free text, or a muted em dash when empty
       There are NO week-comparison columns ("Tuần trước", "Tuần này", "Tăng-giảm"). Period movement
       is carried by the chart and by KPI tiles 3 and 4 instead.

5. HISTORY CARD, 16px below the detail table (again a literal): a title row with
   "Lịch sử các kỳ đã lưu" in 14px/700 and the caption "Không ghi đè dữ liệu tuần cũ" in 11px/400
   #4c576b; below it a scroll container — flex column, 6px gap, max-height 240px, overflow auto —
   holding one row per saved period, newest first. Each row is a FOUR-column grid, 150px / 1fr /
   90px / 70px, gap 8px, vertically centred, padding 6px, 1px #7a97bd bottom border, 11px text:
     cell 1 the period's full date range in bold — "10/08 – 16/08/2026";
     cell 2 "Tiến độ chung" followed by a bold percentage;
     cell 3 the movement against the previous period, drawn with the same green/red/grey
            signed-change treatment as column 7 above but formatted as an arrow plus one decimal plus
            a unit — "↑ +2,3 đ.%" — OR, on the OLDEST row only, the muted words "Kỳ đầu";
     cell 4 a default-variant button reading "Xem".

6. FOOTER: one line, 11px/400 in #4c576b, padding 12px 4px, reading
   "Xem toàn bộ danh mục & nhập/cập nhật dữ liệu tại Danh mục > DTI." where the last two words are a
   link — #0f5bd7, weight 700, no underline at rest, underlined on hover.

COPY (verbatim Vietnamese — reproduce character for character; do not translate, do not correct, do
not shorten. Keep every three-dot "..." exactly as three dots, every "·" separator, every en dash "–"
WITH its surrounding spaces, and every "—" em dash used as a null placeholder).

The app HAS a runtime translation layer: @ngx-translate/core v18 reading src/FE/public/i18n/vi.json
and en.json. The Vietnamese below is the **rendering of the `vi` bundle** — what a Vietnamese user
sees on screen, which is what you must draw. It is NOT markup to copy.

MARKUP CONTRACT (applies to any HTML/Angular you emit, not to the picture): every user-facing string
must be bound through a translation key — {{ 'dashboard.…' | translate }} or
[attr.aria-label]="'dashboard.…' | translate" — with the Vietnamese sentence added to vi.json and an
English sibling added to en.json. Do NOT paste a Vietnamese literal into a template: the build gate
scans every .html file for Vietnamese diacritics and FAILS on a hit. No key for this screen exists
yet, because the screen has never shipped; allocating the `dashboard.*` keys is part of building it.

- Browser tab title: "PlatformManager". Topbar page title on this route: "Trang chủ".
- Sidebar brand: "PM" and "PlatformManager". Collapse button accessible label toggles between
  "Mở rộng menu" and "Thu gọn menu". Seeded nav labels: "Trang chủ", "Quản trị hệ thống",
  "Người dùng", "Phân quyền".
- Topbar: hamburger accessible label "Mở menu điều hướng"; logout button label and tooltip
  "Đăng xuất". The user's name is server data.
- Toast close accessible label: "Đóng thông báo".
- Period toolbar label: "Kỳ đang xem:"
- Period value box, week mode: "Tuần 33/2026 (10/08 – 16/08/2026)"
- Period value box, month mode: "Tháng 8/2026 (01/08 – 31/08/2026)"
- Year select accessible name: "Chọn năm"
- Week select accessible name: "Xem tổng hợp cả năm hoặc 1 tuần cụ thể"; its title attribute:
  "Chọn 1 tuần cụ thể hoặc xem Tất cả"
- Week select options: "— Kỳ hiện tại —", then "— Tất cả (tổng hợp theo năm) —", then one option per
  saved week in the form "Tuần 33 · 10/08 – 16/08 · 82,1%"
- Month select options: "— Tháng hiện tại —", then one option per month in the form
  "Tháng 8 · 01/08 – 31/08 · 81,0%"
- View-mode switch: "Tuần" and "Tháng"; its group accessible name "Chế độ xem theo Tuần hoặc Tháng"
- Export button: "Xuất báo cáo". Its tooltip with no filter applied:
  "Tải file Excel (.xlsx) của kỳ đang xem — Tuần 33/2026 (10/08 – 16/08/2026)". Its tooltip WITH
  filters applied: "Tải file Excel (.xlsx) — CHỈ các chỉ tiêu đang lọc (7/62)"
- When the "all periods" option is chosen, a warn-coloured badge reads "Tất cả · 2026"
- Group panel heading "Tiến độ theo nhóm", caption "Tuần hiện tại"
- Group names: "1. Hạ tầng và Nền tảng số" · "2. Nhân lực số" ·
  "3. An toàn thông tin, an ninh mạng" · "4. Hoạt động chính quyền số" ·
  "5. Hoạt động Kinh tế số" · "6. Hoạt động Xã hội số"
- Chart panel heading "Biểu đồ tiến độ hàng tuần" (it reads "hàng tháng" in month mode), caption
  "Tiến độ chung"; the chart's accessible label
  "Biểu đồ đường tiến độ chung theo tuần, từ tuần 28 đến tuần 33 năm 2026"; the placeholder shown
  while its code chunk downloads: "Đang tải biểu đồ…" (a real ellipsis, U+2026)
- Detail table heading "62 chỉ tiêu DTI"; live count "62/62 chỉ tiêu"
- Search placeholder "Tìm mã hoặc tên chỉ tiêu..." (THREE DOTS, not an ellipsis character); its
  accessible name "Tìm mã hoặc tên chỉ tiêu"
- Filter trigger "Lọc". Condition 1 "Nhóm chỉ tiêu" → "Tất cả nhóm" plus the six group names.
  Condition 2 "Trạng thái" → "Tất cả trạng thái" · "Chưa thực hiện" · "Đang thực hiện" ·
  "Cần bổ sung minh chứng" · "Hoàn thành". Panel footer "Xoá lọc" and "Áp dụng"
- Sort select accessible name "Sắp xếp danh sách"; options "Theo mã chỉ tiêu" and
  "Chênh lệch lớn nhất" — exactly two
- Chip remove button accessible label "Bỏ lọc <nhãn chip>", drawn as a real times glyph and carrying
  NO title attribute
- Table headers: "Mã" · "Chỉ tiêu" · "Nhóm" · "Điểm tối đa" · "Tự đánh giá" · "Thẩm định" ·
  "Chênh lệch" · "Trạng thái" · "Minh chứng/Ghi chú"
- Status values, the complete set of four: "Chưa thực hiện" · "Đang thực hiện" ·
  "Cần bổ sung minh chứng" · "Hoàn thành"
- Empty note cell: "—" (em dash, muted)
- History panel heading "Lịch sử các kỳ đã lưu", caption "Không ghi đè dữ liệu tuần cũ"; row period
  "10/08 – 16/08/2026"; row label "Tiến độ chung" plus a bold percentage; oldest row "Kỳ đầu" in
  muted; row action "Xem"
- Footer: "Xem toàn bộ danh mục & nhập/cập nhật dữ liệu tại Danh mục > DTI."
- First-run banner (nothing imported):
  "Chưa có dữ liệu DTI nào. Vào Danh mục DTI để nhập file hoặc thêm chỉ tiêu đầu tiên."
  where "Danh mục DTI" is the inline link.
- Post-import banner (criteria exist, no progress entered):
  "Đã có 62 chỉ tiêu, nhưng chưa chỉ tiêu nào có Tiến độ %. Thanh tiến độ theo nhóm và biểu đồ sẽ hiện ngay khi có số liệu. Nhập Tiến độ % tại Danh mục DTI."
  where "Danh mục DTI" is the inline link.
- Busy overlay accessible name while a period loads: "Đang tải số liệu…" (a real ellipsis)

STATES:
- Default: a period is selected and returns data. All six regions render as described above.
- Loading (a period change, or first entry): the FOUR DATA REGIONS dim to opacity 0.5 and carry
  aria-busy="true" — the KPI row, the group-progress card, the chart card and the detail table — with
  ONE small circular spinner over them. The PERIOD TOOLBAR STAYS FULLY LIT AND USABLE (the control
  that started the fetch must not become unreachable during it), and so does the history card, which
  does not change with the period being previewed. The dim is the only thing marking stale numbers as
  stale; do not omit it, and do not replace it with a skeleton over the whole page. Note the chart
  ALSO has a separate code-loading placeholder that reserves its full 220px height — the two can
  happen at once and do different jobs.
- Empty, no data for the selected period: each region degrades on its own rather than the page
  blanking. The chart swaps its plot for one muted sentence; the group list and the history list each
  render their own muted empty sentence; the table shows its empty message; the five KPI tiles render
  "—" in place of every value.
- Empty, brand-new deployment with nothing imported: this screen IS the landing route, so a fresh
  install lands here. Draw an information-severity notice bar ABOVE the KPI row — flex row aligned to
  the top, gap 8px, fill #c4d8f6, 1px #7a97bd border, a 4px #0f5bd7 LEFT border, radius 9px, padding
  8px 14px, 12px/400 text, 14px bottom margin, opening with a #0f5bd7 info-in-circle glyph nudged 2px
  down and with NO close button — carrying the first-run sentence from COPY, whose "Danh mục DTI" is
  an INLINE LINK (weight 700, #0f5bd7, no underline at rest), not a button. All five KPI tiles read
  "—", not zeros.
- Empty, criteria imported but no progress entered yet: THIS IS THE NORMAL STATE AFTER EVERY IMPORT,
  not an edge case — import deliberately leaves the progress field blank. The detail table is FULLY
  POPULATED while the six group bars sit at 0 and the chart has nothing to plot. Draw the same
  information notice above the KPI row with the post-import sentence from COPY, and read the KPI
  tiles as: 1 "—", 2 "—", 3 "0", 4 "0", 5 the real completed count "26/62". Tile 5 is what proves
  the import landed. Labels are unchanged; only the values differ.
- Error: a failed request surfaces ONLY as a toast in the shell. This screen authors no in-page error
  banner and no retry control.
- Access: every signed-in account may see this screen. There is NO permission state, NO 403 page and
  no "you lack access" message to draw.
- Validation: none exists. The screen has no input — every control is a query control and none can be
  invalid.
- Print: the two toolbars disappear, along with the sidebar, topbar and toast stack. The detail table
  and the history list keep their height caps, so a printed page silently truncates both — draw that
  as it is.

RESPONSIVE:
- 981px and up (desktop default): the two-column region is a 1.15fr / 0.85fr grid; the KPI row is
  five equal columns; main is centred at 1600px with 14px padding. A collapsed sidebar (60px) simply
  lets the content reflow wider.
- 980px and below: the two-column region COLLAPSES TO ONE COLUMN, so the group panel sits above the
  chart. The KPI row drops to TWO columns. The group rows narrow their name column from 210px to
  140px. The sidebar becomes an off-canvas drawer, width min(85vw, 300px), over an
  rgba(20,28,40,0.45) backdrop, and the topbar hamburger appears.
- 560px and below: the KPI grid gap tightens to 8px and ALL FIVE tiles span the full width, one per
  row — not four-in-two-columns plus one. That is a known artefact of the tile host being
  display: contents; draw it as-is. KPI values step down from 21px to 18px. Group-row name columns
  narrow again to 110px. Main padding drops and the topbar hides the user's name.
- NOT responsive at any breakpoint: the chart's 220px height (phone to 4K) and the history rows'
  four-column template, which has no breakpoint variant at all. The detail table does not restack —
  it scrolls horizontally inside its wrapper.
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

3. Ask for **one frame** of the populated screen first. The two states worth a second frame are the
   **post-import** one (a full detail table above six 0% bars and an empty chart, with the notice
   above the KPI row) and **month mode** (the month period label, the month select and the
   `Th.1 … Th.12` axis). Reject any output that draws an editable cell, a save button, a paginator
   under the detail table, or both period selects at once.

4. Optional, only after a successful import — Stitch resolves the dictionary's own names, so these
   are interchangeable with the literals above: `colors.primary` / `colors.brand` (#0f5bd7) ·
   `colors.bg` (#cfdaea) · `colors.card` (#ffffff) · `colors.tonal-bg` (#c4d8f6) ·
   `colors.tonal-ink` (#0f4a9e) · `colors.btn-hover-bg` (#c7dbf5) · `colors.muted` (#4c576b) ·
   `colors.line` (#7a97bd) · `colors.border-strong` (#6077a2) · `colors.on-primary` (#ffffff) ·
   `colors.surface-table-header` (#e9eff6) · `colors.th-ink` (#536076) · `colors.surface-track`
   (#dbe4f0) · `colors.good` (#0e7050) · `colors.warn` (#965e08) · `colors.bad` (#a02b2b) ·
   `rounded.sm` (7px) · `rounded.md` (9px) · `rounded.lg` (16px) · `rounded.table` (12px) ·
   `rounded.pill` (999px) · `spacing.card-padding` (14px) · `spacing.cell-padding` (6px 8px) ·
   `spacing.toolbar-padding` (8px 10px) · `spacing.notice-padding` (8px 14px) ·
   `spacing.badge-padding` (3px 6px) · `typography.h2-title` (14px/700) ·
   `typography.segmented-label` (12px/700) · `typography.table-header` (11px/700) ·
   `typography.table-cell` (12px/400) · `typography.muted-caption` (11px/400) ·
   `typography.delta` (12px/850) · `typography.badge` (10px/750) · `typography.notice` (12px/400) ·
   `typography.footer` (11px/400).

   > ⚠️ Four token names changed on 2026-08-29. If you are working from an older prompt or an older
   > Stitch project, replace `tonal-bg-hover` → `btn-hover-bg`, `bad-bg-hover` → `danger-hover-bg`,
   > `text-table-header` → `th-ink`, and both `bad-border-btn` and `bad-border-notice` →
   > `danger-border`.

5. **Attach no screenshot.** See § Assets to Attach — every PNG under `Assets/Screenshots/dashboard/`
   shows the screen that was **deleted** on 2026-08-29, in the old palette and the old type face.
   Feeding one to a generator overrides the correct literals above with a picture of a screen that
   does not exist and is not this design.

This repo has no Stitch MCP configured — do the import manually via stitch.withgoogle.com (see
`doc/Design/SETUP.md` to add one).

## Claude Design

Paste the **Master Prompt** above and add the three notes plus the token block below. Attach
**nothing**: `Assets/Brand/` does not exist (the shipped app has no logo or brand image file; the
"PM" mark is a styled text square), and every screenshot on disk shows the deleted pre-2026-08-29
screen — see § Assets to Attach.

**Note 1 — no reference image exists for this design, and none can be captured.** The screen is not
built. The Master Prompt is written to be complete without one: every colour, size, radius and string
in it is a literal.

**Note 2 — this screen writes nothing.** One button downloads a file; everything else changes what is
displayed. If the output contains an editable cell, a save button, a "Lưu" action, an add button or a
delete action, it is the wrong screen — those belong to `/danh-muc/dti`.

**Note 3 — the detail table is a plain table, on purpose.** Do not substitute a data-grid component,
and do not add a paginator, a rows-per-page select, row selection or a bulk-action bar. It shows the
whole period at once inside a scroll box with sticky headers.

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
KPI value size 21px (18px at ≤560px) and its weight 850; the KPI sub-caption's reserved
`min-height: 30px`; the progress track height 9px; the group row's `210px 1fr 80px` grid (140px then
110px at the two breakpoints); the chart's 220px height and its 12%-opacity area fill
`rgba(15,91,215,0.12)`; the history container's `max-height: 240px` and its row grid
`150px 1fr 90px 70px`; the badge's `padding: 3px 6px` at 10px/750; the two-column grid
`1.15fr 0.85fr`; the filter panel's 320px width and `0 16px 40px rgba(23,39,67,0.22)` shadow; the
260px search field; and the **two 16px top margins** on the detail-table card and the history card,
which are the one place this screen steps off the 4/6/8/10/14 spacing scale.

## Google AI Studio

**System instruction** = the `CONTEXT:`, `PALETTE:`, `SHADOWS:`, `FONT:`, `RADII:` and
`SPACING SCALE:` sections of the Master Prompt above, pasted verbatim. They are the invariant part —
they describe the design system, not this screen. Append this paragraph, which is specific to this
screen:

```
This screen is read-only. It has exactly one action, a button that downloads an .xlsx file with no
dialog and no preview. Never draw an editable cell, a save button, an add or delete action, a
paginator, a floating action button or a modal. Never render both the week select and the month
select at once. The product is translated with @ngx-translate (bundles vi.json + en.json): every
Vietnamese string you are given is the rendering of the vi bundle — draw it character for character,
and if you emit markup rather than a picture, bind each string through a translation key instead of
inlining it.
```

**User prompt** = the `LAYOUT:`, `COPY:`, `STATES:` and `RESPONSIVE:` sections of the Master Prompt
above, pasted verbatim. Run it once for the populated state and once for the post-import state.

**Image part** = none. Do not attach anything from `Assets/Screenshots/dashboard/`; those PNGs show
the screen deleted on 2026-08-29, and a multimodal model will trust the picture over the text. Add
this instead: "No reference image is supplied. Follow the written specification exactly; every
colour, size and string in it is literal."

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable,
Figma AI, …). The block is self-contained — no token resolution, no other file, no follow-up prompt
and no reference image are required. Reject any output that makes the screen writable, adds a
paginator to the detail table, or recolours the progress bars by threshold.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Tokens/tokens.json` — W3C DTCG token file (`global` + `light`; `dark` is intentionally empty, the
  app ships one theme).
- `DESIGN.md` — lint-clean token dictionary, for the Stitch import.
- `Assets/Screenshots/dashboard/dashboard--desktop-1440.png` — **pending, and blocked: the screen is
  not built.** This is the one shot that must exist once it is. Capture instructions live in
  `Screens/01-dashboard.md` § Screenshots.
- `Assets/Screenshots/dashboard/dashboard--no-progress--desktop-1440.png` — **pending, blocked for
  the same reason.** The post-import state, which every deployment passes through and which is the
  one most likely to be mistaken for a bug.
- `Assets/Screenshots/dashboard/dashboard--empty--desktop-1440.png` — **on disk, do not attach.** It
  is a capture of the **deleted** pre-2026-08-29 build, in the old palette and the old type face, and
  its filename is the one the new empty-state shot will want. Move or rename it before capturing.
- `Assets/Screenshots/dashboard/_superseded-prototype/` — **do not attach.** Captures of the static
  prototype deleted on 2026-08-23, kept for before/after comparison only, with their own README
  explaining why.
- `Assets/Brand/` — **none exist.** The app ships no logo or brand image file; the "PM" mark is a
  26x26px square filled #0f5bd7 with white 11px/800 text.
