---
kind: luat
scope: du-an
verified: 2026-09-08
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
screen_ref: "03-quan-tri-nguoi-dung"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — User Administration (`/quan-tri/nguoi-dung`)

<!-- One pack for Screens/03-quan-tri-nguoi-dung.md. Token values resolved to literals from
     src/FE/src/styles.scss (:root) via Tokens/colors.md + Tokens/spacing.md +
     Tokens/typography.md + DESIGN.md frontmatter. Copy is verbatim Vietnamese from the
     shipped Angular templates. Fidelity rule: reproduce the app AS-SHIPPED — quirks
     included, nothing idealized, nothing translated.

     Everything a tool needs is inside THIS file: every token is resolved to a literal
     hex/px/font value, and no other spec has to be opened. -->

> **Regenerated 2026-08-29 — the previous pack would have produced the wrong screen.** It carried
> the pre-2026-08-29 palette (`#eef2f8`, `#dfe6ef`, `#57647a`, `#7e91b4`, `#f8fafc`), named `Inter`
> as the type face, described a hand-rolled `.filters` row that no longer exists, and emitted a
> `components.role-tag` that was deleted the same day. Every literal below was re-resolved from
> `src/FE/src/styles.scss` and every string re-read from the live templates.

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is a literal. -->

```
Recreate this exact shipped screen — do not idealize, do not translate, do not "improve" anything.

CONTEXT: an internal Vietnamese-language administration platform ("PlatformManager"). This screen is
User Administration: an administrator lists every account, searches by name or email, narrows the
list by role and status, creates a user with a temporary password, edits an existing user's email /
full name / roles, and locks or unlocks an account. It renders INSIDE the app shell (fixed left
sidebar + sticky topbar + centred main + fixed toast stack), unlike the sign-in / change-password
screens which have no shell. The add/edit form and the lock confirmation are modals on this same
page, not separate screens.

PALETTE (recomputed from the WCAG contrast formula, not picked by eye — use these exact values):
page background #cfdaea; card + dialog + input surface #ffffff; ghost-button hover tint #c1cde2;
default (secondary) button fill #c4d8f6 with text #0f4a9e and hover fill #c7dbf5; body text #152033;
muted text #4c576b; component hairline #7a97bd (cards, toolbar, table rules, badge outlines — this
measures 3.00:1 on white, it is a REAL visible border, not a faint one); stronger border #6077a2
reserved for inputs and the table wrapper, meaning "you can type here"; brand #0f5bd7 with hover
#174ca8 and white text on it; success #0e7050 on #d9f2e6; danger #a02b2b on #fbdcdc, danger border
#e0a8a8, danger hover fill #f5c6c6; table header surface #e9eff6 with header text #536076 (the same
#e9eff6 tints every even body row); topbar surface rgba(255,255,255,0.95) with backdrop-filter
blur(10px); dialog/drawer backdrop rgba(20,28,40,0.45); active sidebar item tint
rgba(15,91,215,0.08); disabled-input fill #dbe4f0.

SHADOWS: cards 0 4px 16px rgba(23,39,67,0.1) + 0 1px 3px rgba(23,39,67,0.06); secondary button hover
0 3px 10px rgba(23,39,67,0.1); primary button hover 0 8px 20px rgba(15,91,215,0.35); filter dropdown
0 16px 40px rgba(23,39,67,0.22); dialog 0 24px 70px rgba(0,0,0,0.25); input focus ring
0 0 0 3px rgba(15,91,215,0.12) with the border turning #0f5bd7; invalid input focus ring
0 0 0 3px rgba(160,43,43,0.14) with a #a02b2b border. Every other control focuses with
outline: 2px solid #0f5bd7, offset 2px.

FONT: 'Be Vietnam Pro', falling back to 'Segoe UI', Arial, sans-serif — self-hosted, weights 400,
500, 600, 700, 800. Base 13px. Type scale, all in that family: page title in the topbar 15px/700;
card heading 14px/700; table column header 11px/700 with letter-spacing 0.01em; table cell 12px/400
line-height 1.4; user name 12px/700; user email and creation date 11px/400 in #4c576b; badge
10px/750; button label 12px/700; form label 12px/700; dialog title 15px/800;
dialog description 12px/400 line-height 1.5; filter chip 11px/700; filter count pill 10px/800;
role chip 10px/700; sidebar nav item 12px/600 (700 when active); sidebar brand text 14px/800; avatar initials and sidebar
brand mark 11px/800; toast title 12px/800; toast body 12px/400 line-height 1.45. Weights 750 and 850
are NOT loaded faces and render as 800 — reproduce that, do not correct it.

RADII: 7px buttons, icon buttons, inputs and role chips; 9px toolbar, notice bar, filter panel and
toast; 12px table wrapper; 15px dialog; 16px card; 999px status badges, filter chips and the filter
count pill.

SPACING SCALE (the whole app uses only these five steps): 4 / 6 / 8 / 10 / 14px.

LAYOUT — app shell:
- Sidebar: fixed left, 220px wide, full viewport height, background #ffffff, 1px right border
  #7a97bd, z-index 35. Brand row (padding 10px, min-height 50px, 1px bottom border #7a97bd): a
  26x26px square filled #0f5bd7 with white "PM" in 11px/800, then "PlatformManager" in 14px/800,
  then a 24x24px transparent icon-only button holding a left-chevron that collapses the rail to
  60px. Nav items below: label 12px/600 in #152033 next to an 18px icon box whose glyph is #4c576b,
  padding 6px 8px, gap 8px, radius 9px; hover fills #cfdaea; the active item fills
  rgba(15,91,215,0.08), turns both label and glyph #0f5bd7 at weight 700, and grows a 3px
  brand-coloured rail on its left edge. Group items carry a trailing 12px down-chevron.
- Topbar: sticky at the top of the content column, z-index 20, background rgba(255,255,255,0.95)
  with backdrop-filter blur(10px), 1px bottom border #7a97bd. Inner row max-width 1600px, centred,
  padding 10px 14px, gap 8px: a hamburger button (visible only ≤980px), the page title
  "Người dùng hệ thống" in 15px/700, then pushed right the signed-in user's full name in 12px/700
  and a secondary button reading "Đăng xuất" with a leading sign-out glyph.
- Main: max-width 1600px, centred, padding 14px, on the #cfdaea page background.
- Toast stack: fixed 14px from the right and bottom edges, z-index 60, max-width min(400px, 90vw),
  8px gap, aria-live polite. Each toast: white card, radius 9px, 1px border #7a97bd, a 5px left edge
  in the severity colour, shadow 0 14px 38px rgba(23,39,67,0.26), padding 10px 14px, holding a 22px
  circular severity disc (11px glyph, tinted fill + ink of that severity), a 12px/800 optional title
  over 12px/400 body text at line-height 1.45, and a 24px transparent close button on the right.
  Auto-dismisses after 5 seconds.

LAYOUT — the page (everything below sits in ONE card: #ffffff, 1px border #7a97bd, radius 16px,
padding 14px, card shadow):

1. TITLE ROW — flex, space-between, gap 8px, margin-bottom 10px: "Danh sách người dùng" in 14px/700
   on the left, and on the right ONLY a muted 11px count reading "<N> người dùng" (a live region).
   There is deliberately NO button in this row.

2. TOOLBAR — its own surface, not a bare row: background #cfdaea, 1px border #7a97bd, radius 9px,
   padding 8px 10px, gap 8px, flex-wrap, margin-bottom 14px. Left to right:
   a. A 260px search box whose width does NOT stretch to fill the row: a magnifier glyph absolutely
      placed 10px from the left, vertically centred, 13px, #4c576b — and an input with a 1px #6077a2
      border, radius 7px, white fill, padding 6px 8px but 30px of LEFT padding so the text clears
      the glyph. Placeholder "Tìm theo tên hoặc email...".
   b. A "Lọc" button in the default secondary style, with a leading funnel glyph. When at least one
      condition is applied it also carries a small pill counter: min-width 16px, height 16px, radius
      999px, fill #0f5bd7, white 10px/800 text. While the panel is open the whole button inverts to
      a filled #0f5bd7 with white text, and the counter inverts with it (white fill, brand text).
      Opening it drops a panel: absolutely positioned 6px below the button, 320px wide, z-index 50,
      white, 1px #7a97bd border, radius 9px, shadow 0 16px 40px rgba(23,39,67,0.22), padding 14px.
      Inside: two labelled selects — "Vai trò" (options "Tất cả vai trò", "SuperAdmin", "Admin",
      "User") and "Trạng thái" (options "Tất cả trạng thái", "Đang hoạt động", "Đã khoá") — each
      label 12px/700, each select full width with the same input border. Then a foot row separated
      by a 1px #7a97bd top border, 14px above, 10px of padding above it, space-between: a secondary
      "Xoá lọc" on the left and a primary "Áp dụng" on the right.
   c. A 1px #7a97bd vertical rule, stretched to the row height.
   d. One chip per applied condition: inline-flex, fill #c4d8f6, 1px border #7a97bd, text #0f4a9e
      11px/700, radius 999px, padding 2px 6px 2px 8px, ending in a 16x16px transparent button
      carrying a 10px "times" glyph. Chip text reads "Vai trò: Admin" or "Trạng thái: Đã khoá".
   e. Pushed to the far right, a primary button reading "+ Thêm người dùng" (the "+" is a literal
      character, not an icon).

3. THE GRID, wrapped in a rounded container: 1px #6077a2 border, radius 12px, overflow hidden. It
   scrolls internally at a viewport-relative height of calc(100dvh - 280px), never a fixed pixel
   height, and the header row is sticky so it stays put while rows scroll. Header cells: 11px/700,
   sticky, background #e9eff6, text #536076, letter-spacing 0.01em. Body cells 12px/400, padding
   6px 8px, 1px bottom border #7a97bd, top-aligned; even rows tinted #e9eff6; the hovered row tints
   #cfdaea. Five columns:
     1. "Người dùng" 220px — a 30px circle filled #0f5bd7 with white 11px/800 initials, 8px gap,
        then the full name in 12px/700 above the email in 11px/400 #4c576b. A missing email renders
        as an em dash "—".
     2. "Vai trò" 140px — one chip per role, wrapping, 6px gap. Each chip is the OUTLINE badge
        variant: transparent fill, 1px #7a97bd border, radius 7px, padding 3px 6px, text #152033 at
        10px/700. It deliberately carries NO semantic colour — a role name is an identifier, not a
        status. Role strings come from the server verbatim ("SuperAdmin", "Admin", "User") and are
        never re-cased.
     3. "Trạng thái" 120px — a pill badge, radius 999px, padding 3px 6px, 10px/750, whose label
        begins with a literal ● character: "● Đang hoạt động" in #0e7050 on #d9f2e6, or "● Đã khoá"
        in #a02b2b on #fbdcdc. The dot is part of the text string, not a separate element.
     4. "Ngày tạo" 110px — the creation date in vi-VN format (d/m/yyyy), muted 11px #4c576b. When
        the value is missing or unparseable it renders as "—".
     5. "Hành động" 100px, right-aligned — two 24x24px ghost icon buttons, 6px apart, both
        transparent with a 7px radius and a 12px glyph, and each in its OWN colour:
          - a pencil in #0f5bd7, hover fill #c4d8f6 — "Sửa";
          - a padlock in #a02b2b, hover fill #fbdcdc — locks the account. On an account that is
            already locked the same button becomes an open padlock in #0f5bd7 with a #c4d8f6 hover
            fill instead. Neither is ever filled at rest: a long list where every action cell holds
            a solid button is a list nobody can read.
        On the signed-in administrator's own unlocked row the padlock is disabled — opacity 0.45,
        cursor not-allowed, no hover — with a tooltip explaining why. Unlock is never disabled.
   Below the grid, a paginator: first / previous / page numbers / next / last plus a rows-per-page
   dropdown offering 10, 20 and 50. There is no "showing X of Y" text.

4. ADD / EDIT MODAL (native dialog, min(560px, 92vw) wide, radius 15px, padding 14px, shadow
   0 24px 70px rgba(0,0,0,0.25), over a rgba(20,28,40,0.45) backdrop). Its head is a plain title row,
   NOT a severity-icon head: "Thêm người dùng" (or "Sửa người dùng") in 14px/700 on the left, a
   24x24px ghost close button carrying a "times" glyph on the right. Body: a two-column grid, 14px
   column gap, collapsing to one column below 560px, holding labelled fields. Labels 12px/700 with a
   red asterisk when required; inputs 240px wide with a 1px #6077a2 border, radius 7px, padding
   6px 8px. In CREATE mode the first row is "Tên đăng nhập" (placeholder "vd nguyen.van.a", focused
   and selected on open) and "Mật khẩu tạm" (placeholder "vd TempPass@123", rendered as PLAIN TEXT,
   not masked). In EDIT mode that row is absent entirely. Both modes then show "Email" (placeholder
   "ten@congty.vn") and "Họ tên" (placeholder "Nguyễn Văn A"). Below them a "Vai trò" group with
   two checkboxes 10px apart, 12px/600 labels, accent colour #0f5bd7: "Admin" and "User" only —
   "SuperAdmin" is deliberately absent from the picker. An invalid field turns its border #a02b2b
   and shows a 12px #a02b2b message directly underneath. Foot: flex, right-aligned, 8px gap, 14px
   above — a secondary "Huỷ" then a primary "Lưu".

5. LOCK CONFIRMATION MODAL (native dialog, min(420px, 92vw) wide, same radius/padding/shadow). This
   one DOES have a severity head: a 40px circle filled #ffedc7 holding a 17px warning-triangle glyph
   in #965e08, 10px gap, then the title "Khoá tài khoản này?" in 15px/800 over a 12px/400 #4c576b
   description at line-height 1.5, then a ghost close button pushed right. Foot: "Huỷ" (secondary,
   focused when the dialog opens) and "Khoá tài khoản" in the DANGER button style — fill #fbdcdc,
   text #a02b2b, 1px #e0a8a8 border, hover fill #f5c6c6.

COPY — reproduce every string exactly, in Vietnamese, including punctuation:
- Browser tab: "Người dùng hệ thống · PlatformManager"
- Topbar title: "Người dùng hệ thống"; topbar button: "Đăng xuất"
- Sidebar brand: "PM" / "PlatformManager"
- Card heading: "Danh sách người dùng"; count line: "<N> người dùng"
- Search placeholder: "Tìm theo tên hoặc email..." (three ASCII dots, not an ellipsis character)
- Filter trigger: "Lọc"; panel foot: "Xoá lọc" and "Áp dụng"
- Filter labels: "Vai trò", "Trạng thái"
- Filter options: "Tất cả vai trò" / "SuperAdmin" / "Admin" / "User"; "Tất cả trạng thái" /
  "Đang hoạt động" / "Đã khoá"
- Filter chips: "Vai trò: <role>", "Trạng thái: Đã khoá", "Trạng thái: Đang hoạt động"
- Create button: "+ Thêm người dùng"
- Column headers: "Người dùng" / "Vai trò" / "Trạng thái" / "Ngày tạo" / "Hành động"
- Status badges: "● Đang hoạt động" and "● Đã khoá"
- Missing email and missing date both render as "—"
- Row action tooltips: "Sửa"; "Khoá tài khoản"; "Mở khoá tài khoản"; and on the viewer's own row
  "Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất"
- Empty grid: "Không có người dùng nào khớp bộ lọc."
- Lock dialog title: "Khoá tài khoản này?"
- Lock dialog description: "“<Họ tên>” sẽ không đăng nhập lại được kể từ bây giờ. Phiên đang đăng
  nhập của người dùng này bị chấm dứt trong vòng 30 phút, KHÔNG phải ngay lập tức. Mở khoá lại được
  bất cứ lúc nào."
- Lock dialog buttons: "Huỷ" and "Khoá tài khoản"
- Form dialog titles: "Thêm người dùng" / "Sửa người dùng"; buttons "Huỷ" / "Lưu"
- Form labels: "Tên đăng nhập", "Mật khẩu tạm", "Email", "Họ tên", "Vai trò"
- Form placeholders: "vd nguyen.van.a", "vd TempPass@123", "ten@congty.vn", "Nguyễn Văn A"
- Validation messages: "Email bắt buộc.", "Họ tên bắt buộc.", "Chọn ít nhất 1 vai trò.",
  "Tên đăng nhập bắt buộc.", "Mật khẩu tạm phải có ít nhất 8 ký tự."
- Toasts: "Đã thêm người dùng.", "Đã cập nhật người dùng.", "Đã mở khoá tài khoản.", and for lock
  "Đã khoá tài khoản. Phiên đang đăng nhập của người dùng này sẽ bị chấm dứt trong vòng 30 phút."

STATES:
- LOADING: a translucent mask with a centred spinner covers the grid only; the toolbar and the create
  button stay interactive underneath. The empty message never flashes before data arrives.
- POPULATED (default): one row per user on the current page, 10 per page by default.
- FILTER APPLIED: the count pill on "Lọc" shows how many conditions are active, and one removable
  chip renders per condition. That pill is what keeps "this list is filtered" visible after the panel
  closes.
- EMPTY: a single full-width muted cell reading "Không có người dùng nào khớp bộ lọc." The paginator
  still renders. The same sentence covers both an empty database and an over-narrow filter.
- LIST FETCH ERROR: nothing on the screen changes — no banner, no retry, and the previous rows stay
  on screen. Reproduce this absence; do not invent an error state.
- SELF-LOCK BLOCKED: described above — the viewer's own padlock is disabled with a tooltip.
- LOCKING: the padlock does not lock immediately; it opens the confirmation modal first. UNLOCKING
  asks nothing and happens on one click. Keep that asymmetry.
- VALIDATION: every failing field is reported at once, each message under its own field, each invalid
  input carrying the #a02b2b border. There is also one form-level message line, shown only when no
  field-level message exists.
- SAVING: there is NO busy state — neither "Lưu" nor "Huỷ" is disabled while the request is in
  flight and no spinner appears. Reproduce that as-shipped.
- SAVE ERROR: the dialog stays open with the message inside it, AND a toast appears — the same error
  is shown twice. That is as-shipped.

RESPONSIVE:
- ≥981px: as described above.
- ≤980px: the sidebar leaves the flow and becomes an off-canvas drawer at min(85vw, 300px) over a
  rgba(20,28,40,0.45) backdrop; the content column loses its left offset; the topbar hamburger
  appears. The card, toolbar and grid simply reflow to full width.
- ≤560px: main padding drops to 10px; the topbar hides the user's name, leaving only "Đăng xuất";
  the drawer widens to min(90vw, 300px). The toolbar restacks — the search field goes full width and
  every direct child stretches EXCEPT the "Lọc" button and the chip row, which stay their natural
  size; the action group loses its right-alignment and takes the full width; the vertical rule is
  hidden; the filter panel narrows to min(320px, 86vw). The form dialog's two-column grid collapses
  to one column.
- The grid never stacks its columns at any width. The five column minimums total 690px, so on a
  narrow screen the grid scrolls sideways inside its own container.
- Print: sidebar, topbar, toolbar and toast stack are all hidden; the content column loses its left
  offset and its max-width; the grid prints as-is.

Accessibility details that are part of the design: the count line is a polite live region; icon-only
buttons carry a title or aria-label instead of visible text; decorative glyphs inside the toast disc,
the dialog disc and the sidebar nav are hidden from assistive technology; an invalid input carries
aria-invalid and points at its message with aria-describedby; a skip link precedes the sidebar in the
DOM.
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
   are interchangeable with the literals above: `colors.primary` / `colors.brand` (#0f5bd7) ·
   `colors.bg` (#cfdaea) · `colors.card` (#ffffff) · `colors.tonal-bg` (#c4d8f6) ·
   `colors.tonal-ink` (#0f4a9e) · `colors.btn-hover-bg` (#c7dbf5) · `colors.muted` (#4c576b) ·
   `colors.line` (#7a97bd) · `colors.border-strong` (#6077a2) · `colors.good` / `colors.good-bg`
   (#0e7050 / #d9f2e6) · `colors.bad` / `colors.bad-bg` (#a02b2b / #fbdcdc) ·
   `colors.danger-border` (#e0a8a8) · `colors.danger-hover-bg` (#f5c6c6) ·
   `colors.surface-table-header` (#e9eff6) · `colors.th-ink` (#536076) · `rounded.sm` (7px) ·
   `rounded.lg` (16px) · `rounded.table` (12px) · `rounded.pill` (999px) · `spacing.card-padding`
   (14px) · `spacing.cell-padding` (6px 8px) · `spacing.badge-padding` (3px 6px) ·
   `typography.table-cell` (12px/400) · `typography.badge` (10px/750) · `components.card` ·
   `components.button-primary` · `components.button-danger` · `components.icon-button` ·
   `components.table-header` · `components.table-cell` · `components.table-wrap` ·
   `components.badge-success` · `components.badge-danger` · `components.badge-outline` ·
   `components.dialog` · `components.form-label` · `components.toast`.

   > ⚠️ Five token names changed on 2026-08-29 and one was removed. If you are working from an
   > older prompt or an older Stitch project, replace `tonal-bg-hover` → `btn-hover-bg`,
   > `bad-bg-hover` → `danger-hover-bg`, `text-table-header` → `th-ink`, and both
   > `bad-border-btn` and `bad-border-notice` → `danger-border`. `components.role-tag` no longer
   > exists; the roles column uses `components.badge-outline`.

4. **Do not attach the stored screenshot to Stitch.**
   `Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png` was captured 2026-08-22 and
   shows the screen *before* the 2026-08-29 rebuild and *before* the palette change — a different
   filter row, single-coloured row actions, `Inter` type, and the old pale `#eef2f8` / `#dfe6ef`
   surfaces. Feeding it to a generator will override the correct literals above with the wrong
   picture. Generate from the Master Prompt text alone until the screen is recaptured.

This repo has no Stitch MCP configured — do the import manually via stitch.withgoogle.com (see
`doc/Design/SETUP.md` to add one).

## Claude Design

Paste the **Master Prompt** above and add the note plus the token block below. Attach **nothing**:
`Assets/Brand/` is empty (the shipped app has no logo or brand image file; the "PM" mark is a styled
text square), and the only screenshot on disk is stale — see the warning under § Google Stitch and
§ Assets to Attach.

**Note — no reference image is available for this screen right now.** The generator has to work from
the prompt text. That is a deliberate instruction, not an oversight: the stored capture predates both
the rebuild and the palette change, so it is a picture of a screen that no longer exists. The Master
Prompt is written to be complete without it — every colour, size, radius and string is a literal.

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

Three values used on this screen are **not** custom properties and must be written as literals:
the topbar fill `rgba(255,255,255,0.95)`, the dialog and drawer backdrop `rgba(20,28,40,0.45)`, and
the active sidebar tint `rgba(15,91,215,0.08)`.

## Google AI Studio

**System instruction** = the `CONTEXT:`, `PALETTE:`, `SHADOWS:`, `FONT:`, `RADII:` and
`SPACING SCALE:` sections of the Master Prompt above, pasted verbatim. They are the invariant part —
they describe the design system, not this screen.

**User prompt** = the `LAYOUT:`, `COPY:`, `STATES:` and `RESPONSIVE:` sections of the Master Prompt
above, pasted verbatim.

**Image part** = none. Do not attach
`Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png`; it predates the 2026-08-29
rebuild and palette change, and a multimodal model will trust the picture over the text. Add this
instead: "No reference image is supplied. Follow the written specification exactly; every colour,
size and string in it is literal."

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable,
Figma AI, …). The block is self-contained — no token resolution, no other file, no follow-up prompt
and no reference image are required. If the tool insists on an image, give it none rather than the
stale capture.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Tokens/tokens.json` — W3C DTCG token file (`global` + `light`; `dark` is intentionally empty, the
  app ships one theme).
- `DESIGN.md` — lint-clean token dictionary, for the Stitch import.
- `Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png` — **do not attach.** Captured
  2026-08-22, before the 2026-08-29 rebuild (hand-rolled filter row, single-colour row actions,
  `.role-tag` chips, no lock confirmation) and before the palette and font change. It is kept on disk
  for before/after comparison and is cited as stale in `UiInventory.md` § Screenshot Manifest and in
  `Screens/03-quan-tri-nguoi-dung.md` § Screenshots. Recapturing it is the highest-value screenshot
  job on the project; once it is recaptured, delete this bullet and restore it as a normal attachment.
- `Assets/Brand/` — **none exist**. The app ships no logo or brand image file; the "PM" mark is a
  26x26px square filled #0f5bd7 with white 11px/800 text, and user avatars are initials on a #0f5bd7
  circle — there is no image-avatar path at all.
