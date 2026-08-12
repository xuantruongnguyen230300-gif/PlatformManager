---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
screen_ref: "01-dashboard"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — DTI Weekly Dashboard

<!-- One pack for Screens/01-dashboard.md. Master Prompt filled from that spec + Tokens/tokens.json (light set — the app's only shipped theme). Fidelity rule: prompts reproduce the app AS-SHIPPED — quirks included, nothing idealized. -->

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve {token.reference} — inline literal hex/px/font values only. -->

```
Recreate this exact shipped screen — do not idealize.

TOKENS (literal values):
Colors: primary #0f5bd7 (buttons, progress fill, chart line), primary-alt #174ca8 (declared, unused — do not apply it anywhere), on-primary #ffffff, page bg #f3f6fb, surface #ffffff, topbar surface rgba(255,255,255,0.95), text #152033, text-muted #6d788b, border #dfe6ef, success #14855b, warning #c07a00, danger #c83c3c, notice surface #edf4ff / notice border #cfe0ff, progress-track surface #edf1f6, table-header surface #f8fafc / table-header text #536076, input border #cad4e1, badge-success surface #e7f7f0, badge-warning surface #fff3da, badge-danger surface #fdecec, dialog backdrop rgba(20,28,40,0.45), report surface #f8fafc / report border (dashed) #cbd6e5.
Font: Inter, Segoe UI, Arial, sans-serif everywhere (no webfont, system stack only). Sizes/weights: body 16px/400, logo h1 18px/700 (16px/700 below 560px), section h2 16px/400, KPI value 27px/850 (22px/850 below 560px), KPI/caption labels 12px/400, group-row text 13px/400, table cells 12.5px/400, delta text 12.5px/850, badge text 11px/750, button label 16px/700, fab label 16px/800, report body 13px/400 line-height 1.55, footer 11px/400.
Radius: input fields 8px, select/date fields 9px, buttons 10px, notice banner 11px, table wrapper 12px, cards 14px, dialog 15px, pills (badge/progress-bar/fab) 999px.
Spacing: 7px (table input padding), 8px (action/filter/history gaps), 9px 10px (filter/select field padding), 9px 12px (button padding), 9px 8px (table cell padding), 10px (title/group-row gaps), 11px 13px (notice padding), 12px (grid gaps, section top margins), 14px (notice bottom margin), 15px (card padding), 16px (page/topbar padding), 18px (fab offset from edges).
Shadows: card 0 7px 24px rgba(23,39,67,0.08); fab 0 12px 30px rgba(15,91,215,0.3); dialog 0 24px 70px rgba(0,0,0,0.25).
Breakpoints: tablet ≤980px, mobile ≤560px. Container max-width 1450px. Table min-width 1200px (always horizontally scrollable, no responsive collapse). Dialog width min(700px, 92vw). Trend chart canvas height 245px.
No icon library anywhere — every action is a plain text button/native input/select; the only directional cues are literal "↑"/"↓" characters inline in colored text.

LAYOUT:
Sticky topbar (max-width 1450px inner row, 12px/16px padding, translucent white rgba(255,255,255,.95) background with blur): left = "DTI Weekly" h1 (18px/700) + small subtitle "Theo dõi tiến độ chuyển đổi số · 62 chỉ tiêu" in muted gray; right = 3 actions — secondary button "Sao lưu", secondary button-styled label "Khôi phục" (wraps a hidden file input), primary button "Lưu tuần này". The two secondary actions are hidden below 980px with no mobile substitute.
Main content area (max-width 1450px, 16px padding), top to bottom:
1. Notice banner: pale blue rounded box (bg #edf4ff, border #cfe0ff, radius 11px) with instructional prose (bold spans on key phrases).
2. Weekbar card (white, bordered, shadowed, radius 14px, padding 15px): bold label "Kỳ đang cập nhật:" + date input + "saved periods" select (placeholder "— Chọn kỳ đã lưu —") + secondary button "Tạo tuần mới từ kỳ gần nhất" + secondary button "Báo cáo nhanh", all in one flex row (wraps on narrow screens; each child stretches flex:1 below 560px).
3. KPI grid: 5 equal-width cards (2 columns below 980px; the 5th/last card spans full width below 560px). Each card: small muted label on top, large bold value (27px/850) in the middle, small muted sub-caption at the bottom. Cards in order: "Tiến độ chung tuần này" (value e.g. "82,1%", sub "Bình quân gia quyền theo điểm"); "So với tuần trước" (value "—" or an up/down-colored "↑/↓ X,X đ.%", sub shows the compared-to date or "Chưa có kỳ trước"); "Chỉ tiêu tăng" (green value, sub "Có tiến bộ so với kỳ trước"); "Không tăng" (value "—" until a previous period exists, sub "Cần chú ý theo dõi"); "Hoàn thành 100%" (value like "39/62", sub "Số chỉ tiêu đạt đủ tiến độ").
4. Two-column row (1.15fr/.85fr ratio, stacks to 1 column below 980px): left card "Tiến độ theo nhóm" (muted "Tuần hiện tại" caption top-right) containing 6 stacked rows — each row is [group name bold text in a 230px column] + [pill-shaped progress track/fill bar, primary-blue fill] + [bold percentage number, right-aligned, 90px column] (columns narrow to 140/1fr/75 below 980px, 110/1fr/68 below 560px). Right card "Biểu đồ tiến độ hàng tuần" (muted "Tiến độ chung" caption) containing a 245px-tall hand-drawn line chart with a light percentage grid (0/25/50/75/100%) and date labels along the bottom; when no period is saved yet it shows centered muted text "Lưu ít nhất một kỳ để xem biểu đồ." instead of a line.
5. Criteria table card: header row with h2 "62 chỉ tiêu DTI" + muted dynamic count "<n>/62 chỉ tiêu"; filter row (search input placeholder "Tìm mã hoặc tên chỉ tiêu...", "Tất cả nhóm" group-select, "Tất cả mức thay đổi" change-select, "Theo mã chỉ tiêu" sort-select — all hidden when printing); a bordered, rounded (12px), horizontally-scrolling table (min-width 1200px, sticky pale-gray header) with 9 columns: Mã, Chỉ tiêu, Nhóm, Điểm tối đa (right-aligned), Tuần trước (right-aligned), Tuần này (right-aligned number input suffixed with "%"), Tăng/giảm (right-aligned, colored ↑/↓ text or "—"), Trạng thái (a small colored pill: green "Hoàn thành" / orange "Đang thực hiện" / red "Không tăng"), Ghi chú tuần (free-text input, placeholder "Nội dung đã làm / vướng mắc...").
6. History card: header "Lịch sử các kỳ đã lưu" + muted "Không ghi đè dữ liệu tuần cũ"; a list of rows (newest first), each row = [bold date] + ["Tiến độ chung **X%**" text] + [colored delta text or "Kỳ đầu"] + ["Xem" secondary button]; when empty, shows centered muted text "Chưa có tuần nào được lưu." instead.
7. Footer: small muted one-line note about LocalStorage + periodic "Sao lưu".
Floating "Lưu tuần" pill button, fixed bottom-right (18px offset), primary blue with a soft blue glow shadow — visible ONLY below 980px, calls the same save action as the topbar primary button.
Modal dialog (native, centered, 700px max/92vw, radius 15px, dark translucent backdrop): header "Báo cáo nhanh tiến độ DTI" + secondary "Đóng" button; body is a dashed-border pale panel of generated report prose (bold headline, bold key numbers, two short criteria lists); footer row right-aligned with secondary "Sao chép" and primary "In" buttons.

COPY (verbatim — reproduce exactly, including typos and mixed languages, all Vietnamese, no i18n layer):
- Browser title: "DTI Weekly - Theo dõi tiến độ chuyển đổi số"
- Logo: "DTI Weekly" / "Theo dõi tiến độ chuyển đổi số · 62 chỉ tiêu"
- Topbar buttons: "Sao lưu", "Khôi phục", "Lưu tuần này"
- Notice: "Mỗi tuần chọn ngày báo cáo, cập nhật Tiến độ % của từng chỉ tiêu rồi bấm Lưu tuần này. Hệ thống tự so với kỳ gần nhất trước đó và hiển thị tăng/giảm bao nhiêu điểm %." (bold on "Tiến độ %", "Lưu tuần này", "tăng/giảm bao nhiêu điểm %")
- Weekbar: "Kỳ đang cập nhật:", "— Chọn kỳ đã lưu —", "Tạo tuần mới từ kỳ gần nhất", "Báo cáo nhanh"
- KPI labels/subs: "Tiến độ chung tuần này"/"Bình quân gia quyền theo điểm"; "So với tuần trước"/"—"/"Chưa có kỳ trước"; "Chỉ tiêu tăng"/"Có tiến bộ so với kỳ trước"; "Không tăng"/"Cần chú ý theo dõi"; "Hoàn thành 100%"/"0/62"/"Số chỉ tiêu đạt đủ tiến độ"
- Panels: "Tiến độ theo nhóm"/"Tuần hiện tại"; "Biểu đồ tiến độ hàng tuần"/"Tiến độ chung"/"Lưu ít nhất một kỳ để xem biểu đồ."
- Table: "62 chỉ tiêu DTI", "Tìm mã hoặc tên chỉ tiêu...", "Tất cả nhóm", "Tất cả mức thay đổi"/"Chỉ tiêu tăng"/"Không tăng"/"Giảm"/"Hoàn thành", "Theo mã chỉ tiêu"/"Tăng nhiều nhất"/"Tiến độ thấp nhất", headers "Mã"/"Chỉ tiêu"/"Nhóm"/"Điểm tối đa"/"Tuần trước"/"Tuần này"/"Tăng/giảm"/"Trạng thái"/"Ghi chú tuần", note placeholder "Nội dung đã làm / vướng mắc...", badges "Hoàn thành"/"Đang thực hiện"/"Không tăng"
- History: "Lịch sử các kỳ đã lưu"/"Không ghi đè dữ liệu tuần cũ", "Chưa có tuần nào được lưu.", row button "Xem"
- Footer: "Dữ liệu được lưu trên trình duyệt bằng LocalStorage. Nên dùng nút "Sao lưu" định kỳ để tải file JSON dự phòng."
- Fab: "Lưu tuần"
- Dialog: "Báo cáo nhanh tiến độ DTI", "Đóng", "Sao chép", "In"; generated body opens "BÁO CÁO NHANH TIẾN ĐỘ CHỈ SỐ CHUYỂN ĐỔI SỐ" then "Kỳ cập nhật: <date>." then a progress/comparison paragraph then "Chỉ tiêu tăng nhiều:"/"Chỉ tiêu chưa tăng cần chú ý:" lists.
Use real sample criteria codes/names from the 62-item list when mocking table rows (e.g. "1.1 — Tỷ lệ người sử dụng có khả năng truy nhập băng rộng cố định với tốc độ trên 1Gb/s", group "1. Hạ tầng và Nền tảng số", max score 10).

STATES:
- Default/first-load (no period saved yet): "So với tuần trước" and "Không tăng" KPIs show "—"; table's "Tuần trước"/"Tăng/giảm" columns show "—" for every row; every badge is "Đang thực hiện" or "Hoàn thành" only (never "Không tăng"); trend chart shows its empty-state text; history list shows its empty-state text.
- Has-previous-period: KPI deltas and table/history deltas populate with real colored up/down/flat values; trend chart draws a line with dot markers (up to the 12 most recent periods); all 3 badge variants can appear.
- Loading: none — no network calls anywhere, everything is synchronous, so never show a spinner/skeleton.
- Empty filtered table: count text still updates correctly (e.g. "0/62 chỉ tiêu") but the table body is simply visually empty — no "no results" message.
- Validation: none visible — an out-of-range progress % is silently clamped to 0-100 with no error message or red border.
- Error: the only user-facing error is an alert dialog "File sao lưu không hợp lệ." when restoring a malformed backup file — no other error UI exists.
- Report dialog open: centered modal over a dimmed backdrop, populated with the live computed report text described above.

RESPONSIVE:
- ≥980px: 5-column KPI grid; 2-column groups/trend row (1.15fr/.85fr); "Sao lưu"/"Khôi phục" visible in topbar; floating save button hidden.
- ≤980px: KPI grid becomes 2 columns; groups/trend row stacks to 1 column; "Sao lưu"/"Khôi phục" disappear completely (no menu fallback); a floating pill "Lưu tuần" button appears bottom-right.
- ≤560px: page padding shrinks to 10px; logo heading shrinks to 16px; KPI value text shrinks to 22px, card padding to 12px; the 5th KPI card ("Hoàn thành 100%") spans the full row width; weekbar fields/buttons each stretch to equal width instead of hugging their content; section title rows top-align instead of center-align so long headings can wrap.
- All viewports: the criteria table never gets a responsive/collapsed layout — it stays fixed at 1200px minimum width and scrolls horizontally inside its rounded wrapper.
- Print: topbar, filters, and the floating button are hidden; the two-column row becomes one column; cards lose their shadow; the table drops its scroll clipping and minimum width so it prints in full.

Match the attached screenshots pixel-for-pixel where they conflict with this text.
```

## Google Stitch

Import the lint-clean `DESIGN.md` into the Stitch project first (Design → import design.md), then paste the Master Prompt above. Verify lint is clean before importing:

```bash
npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md
```

Result at time of writing: **0 errors**, 11 warnings (all as-shipped facts: 4 contrast-ratio warnings matching the real shipped badge/label colors, and 7 orphaned-token warnings for `border`/`overlay`-tier colors the `components` sub-token schema has no slot for — see `DESIGN.md` § Colors for the full explanation). The bare `npx @google/design.md lint` form fails silently on Windows — always use the `--package=…designmd` form.

Because tokens import with Stitch, this variant MAY reference `DESIGN.md` token names directly instead of literal values, e.g.: `colors.primary`, `colors.surface-badge-success`, `rounded.pill`, `spacing.lg-card`, `typography.kpi-value`, `components.button-primary`, `components.badge-success`. Note this repo has no Stitch MCP configured — import manually via stitch.withgoogle.com (see `doc/Design/SETUP.md` to add an MCP server if you want this automated).

## Claude Design

Paste the Master Prompt above + attach `Assets/Screenshots/dashboard/dashboard--desktop-1440.png`, `dashboard--with-history--desktop-1440.png`, `report-dialog--desktop-1440.png`, `dashboard--tablet-900.png`, `dashboard--mobile-390.png` (no `Assets/Brand/` files exist — the app has no logo/brand images, see `UiInventory.md` § Brand Assets). Restate the tokens as a CSS custom-property block:

```css
:root {
  --color-primary: #0f5bd7;
  --color-primary-alt: #174ca8; /* declared, unused in shipped app — do not apply */
  --color-on-primary: #ffffff;
  --color-bg: #f3f6fb;
  --color-surface: #ffffff;
  --color-surface-topbar: rgba(255,255,255,0.95);
  --color-text: #152033;
  --color-text-muted: #6d788b;
  --color-border: #dfe6ef;
  --color-success: #14855b;
  --color-warning: #c07a00;
  --color-danger: #c83c3c;
  --color-surface-notice: #edf4ff;
  --color-border-notice: #cfe0ff;
  --color-surface-track: #edf1f6;
  --color-surface-table-header: #f8fafc;
  --color-text-table-header: #536076;
  --color-border-input: #cad4e1;
  --color-surface-badge-success: #e7f7f0;
  --color-surface-badge-warning: #fff3da;
  --color-surface-badge-danger: #fdecec;
  --color-overlay-backdrop: rgba(20,28,40,0.45);
  --color-surface-report: #f8fafc;
  --color-border-report-dashed: #cbd6e5;
  --font-family-base: "Inter", "Segoe UI", Arial, sans-serif;
  --radius-input: 8px; --radius-select: 9px; --radius-button: 10px; --radius-notice: 11px;
  --radius-table: 12px; --radius-card: 14px; --radius-dialog: 15px; --radius-pill: 999px;
  --space-2xs: 7px; --space-xs: 8px; --space-sm: 9px 10px; --space-sm-btn: 9px 12px;
  --space-sm-cell: 9px 8px; --space-md: 10px; --space-md-notice: 11px 13px; --space-lg: 12px;
  --space-lg-notice-mb: 14px; --space-lg-card: 15px; --space-xl: 16px; --space-fab-offset: 18px;
  --shadow-card: 0 7px 24px rgba(23,39,67,0.08);
  --shadow-fab: 0 12px 30px rgba(15,91,215,0.3);
  --shadow-dialog: 0 24px 70px rgba(0,0,0,0.25);
  --breakpoint-tablet: 980px; --breakpoint-mobile: 560px;
}
```

## Google AI Studio

**System instruction** = the TOKENS block + fidelity rules from the Master Prompt above, plus: "This is a Vietnamese-language government digital-transformation tracking dashboard. Reproduce the app AS-SHIPPED — do not translate, do not idealize spacing/contrast, do not add icons (none exist in the source), do not add hover/focus states beyond what is described (the shipped app has none except a 1px button press-down)."

**User prompt** = the LAYOUT + COPY sections from the Master Prompt above, verbatim.

Attach as image parts: `dashboard--desktop-1440.png`, `dashboard--with-history--desktop-1440.png`, `report-dialog--desktop-1440.png`, `dashboard--tablet-900.png`, `dashboard--mobile-390.png`.

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable, Figma AI, etc.) along with the attached screenshots.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/dashboard/dashboard--desktop-1440.png` (default/empty state, 1440px)
- `Assets/Screenshots/dashboard/dashboard--tablet-900.png` (980px breakpoint)
- `Assets/Screenshots/dashboard/dashboard--mobile-390.png` (560px breakpoint)
- `Assets/Screenshots/dashboard/dashboard--with-history--desktop-1440.png` (has-previous-period state)
- `Assets/Screenshots/dashboard/report-dialog--desktop-1440.png` (report dialog open)
- `Tokens/tokens.json` (W3C DTCG — `global` + `light`; `dark` intentionally empty, no dark mode shipped)
- `DESIGN.md` (lint-clean token dictionary + design guidance, for Stitch import)
- `Assets/Brand/` — **none** (no logo/brand image files exist in the shipped app, see `UiInventory.md` § Brand Assets)
