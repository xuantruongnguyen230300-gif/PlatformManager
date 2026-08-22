---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
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

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block. External tools cannot resolve token references — every value below is a literal. -->

```
Recreate this exact shipped screen — do not idealize, do not translate, do not "improve" anything.

CONTEXT: an internal Vietnamese-language administration platform ("PlatformManager"). This screen is
User Administration: an administrator lists every account, searches by name or email, creates a user
with a temporary password, edits an existing user's email / full name / roles, and locks or unlocks
an account. It renders INSIDE the app shell (fixed left sidebar + sticky topbar + centred main +
fixed toast stack), unlike the sign-in / change-password screens which have no shell. The add/edit
form is a modal on this same page, not a separate screen.

TOKENS (literal values — use these exact numbers and hex codes):
Colors: brand/primary #0f5bd7; primary hover #174ca8; text/icon on primary #ffffff; page background
#eef2f8; card + dialog + input surface #ffffff; ghost-button hover tint #e1e7f1; default (secondary)
button fill #dbe7fa with text #0f4a9e and hover fill #c7dbf5; body text #152033; muted text #57647a;
faint hairline #dfe6ef (cards, table rules, role chips, dialog inputs); strong border #7e91b4 (filter
inputs and the table wrapper ONLY); success #0e7050 on #d9f2e6; warning #965e08 on #ffedc7; danger
#a02b2b on #fbdcdc, danger hover fill #f5c6c6; table header surface #f8fafc with header text #536076
(the same #f8fafc tints every even body row AND fills the role chips); topbar surface
rgba(255,255,255,0.95) plus backdrop-filter blur(10px); dialog backdrop rgba(20,28,40,0.45); active
sidebar item background rgba(15,91,215,0.08).
Shadows: card / toast / sidebar drawer 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06);
dialog 0 24px 70px rgba(0,0,0,0.25); secondary button hover 0 3px 10px rgba(23,39,67,0.1); primary
button hover 0 8px 20px rgba(15,91,215,0.35).
Font: Inter, loaded for real from Google Fonts (weights 400, 500, 600, 700), with the fallback stack
"Segoe UI", Arial, sans-serif. Sizes/weights: body 13px/400; topbar h1 15px/bold; card h2 14px/bold;
button label 12px/700; row-action button 11px/700; table header 11px/700 letter-spacing 0.01em; table
cell 12px/400 line-height 1.4; user name 12px/700; user email and creation date 11px/400 in #57647a;
role chip 11px/400; status badge 10px/750; avatar initials 11px/800; dialog form label 12px/700; role
checkbox label 12px/600; sidebar nav item 12px/600 (700 when active); sidebar brand text 14px/800;
brand mark 11px/800; toast text 12px/400 line-height 1.4.
Radius: 7px buttons, inputs, selects, row-action buttons, toast close; 9px sidebar nav item and toast
item; 12px the table wrapper; 15px dialogs; 16px cards; 999px status badges; 6px role chips; 50% the
avatar circle.
Spacing scale in use: 4px, 6px, 8px, 10px, 14px. Card padding 14px. Dialog padding 14px. Main padding
14px (10px below 560px). Topbar inner padding 10px 14px. Table cell padding 6px 8px. Button padding
6px 8px. Row-action button padding 4px 6px. Filter input padding 6px 8px. Status badge padding
3px 6px. Role chip padding 2px 8px with 4px right margin. Title row gap 8px, margin-bottom 10px.
Filters row gap 8px, margin 10px 0. Form row gap 6px, margin-bottom 10px. Role checkbox row gap 10px
with a 4px top margin; each checkbox sits 6px from its label. Dialog action row gap 8px, top margin
8px. Row actions gap 6px.
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
  route the "Quản trị hệ thống" group is open and its "Người dùng" child is the active item.
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

PAGE (everything below sits in ONE card: #ffffff, 1px border #dfe6ef, radius 16px, padding 14px,
card shadow)
1. Title row (space-between, gap 8px, margin-bottom 10px): h2 "Danh sách người dùng" at 14px/bold on
   the left; a primary button "+ Thêm người dùng" (fill #0f5bd7, white 12px/700 text, radius 7px,
   padding 6px 8px) on the right. The "+" is a literal character, not an icon.
2. Filters row (flex, wrap, 8px gap, 10px vertical margin) holding ONE control: a search field that
   fills the row (flex:1, minimum width 220px). It is a relatively-positioned wrapper with a
   `pi-search` icon absolutely placed 10px from the left, vertically centred, 13px, #57647a — and an
   input with a 1px #7e91b4 border, radius 7px, white fill, padding 6px 8px but 32px of LEFT padding
   to clear that icon, 12px text. Typing is debounced 300 ms and resets to page 1.
3. THE GRID, wrapped in a rounded container: 1px #7e91b4 border, radius 12px, overflow hidden (so it
   clips to the rounded corners; the grid's own inner container is what scrolls). Header cells are
   sticky, background #f8fafc, text #536076, 11px/700. Body cells 12px/400, padding 6px 8px, 1px bottom
   border #dfe6ef, top-aligned; even rows tinted #f8fafc; the hovered row tints #eef2f8. Five columns
   with these minimum widths (690px combined):
     1. "Người dùng" 220px — a 8px-gap flex row: a 30px circular avatar filled #0f5bd7 with the user's
        white 11px/800 initials, then a stacked pair — full name at 12px/700 over the email at
        11px/400 in #57647a. A missing email renders as an em dash "—".
     2. "Vai trò" 140px — zero or more role chips, one per role string the API returned, in API order,
        never re-cased by the front end. Each chip: inline-block, 1px border #dfe6ef, radius 6px,
        padding 2px 8px, 11px text in #152033, background #f8fafc, 4px right margin.
     3. "Trạng thái" 120px — one pill badge, radius 999px, padding 3px 6px, 10px/750, preceded inside
        the text by a literal "●" glyph. Locked accounts use #fbdcdc fill with #a02b2b text; active
        accounts use #d9f2e6 fill with #0e7050 text.
     4. "Ngày tạo" 110px — the creation date in vi-VN format (d/m/yyyy), muted 11px #57647a. When the
        value is absent or unparseable it renders as an em dash "—".
     5. "Hành động" 100px, header right-aligned — a right-aligned 6px-gap flex row of exactly two
        ghost icon buttons (transparent fill, 1px transparent border, radius 7px, padding 4px 6px,
        #57647a, hover fills #e1e7f1 and darkens to #152033):
          • edit — `pi-pencil`
          • lock toggle — `pi-lock` while the account is unlocked, `pi-lock-open` while it is locked.
            While the account is unlocked the button also turns #a02b2b (hovering fills #fbdcdc).
   EMPTY STATE: one row spanning all 5 columns holding a single muted sentence. Paging still renders.
   PAGINATOR: below the grid inside the wrapper — first / previous / numbered pages / next / last
   buttons plus a rows-per-page select offering 10, 20 and 50, defaulting to 10. There is deliberately
   NO "showing X of Y" text; page numbers and the select are the only paginator content. Paging is
   server-side.
4. ADD / EDIT USER MODAL — a native centred modal over the rgba(20,28,40,0.45) backdrop, width
   min(560px, 92vw), radius 15px, padding 14px, shadow 0 24px 70px rgba(0,0,0,0.25). Title row: h2 +
   a secondary button "Đóng". Then a stack of form rows (column flex, 6px gap, 10px bottom margin;
   labels 12px/700; inputs on the DIALOG tier — a fainter 1px #dfe6ef border, radius 7px, padding
   6px 8px, full width, 12px text). Rows, top to bottom:
     • "Tên đăng nhập" — CREATE MODE ONLY; auto-focused and text-selected when the dialog opens
     • "Email" — an email field
     • "Họ tên"
     • "Mật khẩu tạm" — CREATE MODE ONLY, and deliberately a PLAIN TEXT field, not masked: the
       administrator has to read the value back to the new user
     • "Vai trò" — a bold label followed by a horizontal 10px-gap group of native checkboxes, one per
       assignable role, each label 12px/600 sitting 6px from its box. The group has a 4px top margin.
   Every label above ends with a red #a02b2b asterisk. Below the checkbox group, ONLY in edit mode and
   ONLY when the target user holds a role the form cannot assign, a muted 12px #57647a note names
   those roles in bold and states they are preserved on save. Below the rows, a single red #a02b2b
   12px error line appears only when there is a message. Finally a right-aligned action row (8px gap,
   8px top margin): a secondary button "Huỷ" and a primary button "Lưu".

THREE DELIBERATE SECURITY BEHAVIOURS — reproduce them, do not "fix" them:
  a) On the signed-in administrator's OWN row, while that account is unlocked, the lock button is
     DISABLED — rendered at opacity 0.45 with a not-allowed cursor and its hover styling suppressed
     back to transparent/#57647a — and its tooltip explains why. Unlocking is never blocked, and
     locking somebody else is never blocked, including a SuperAdmin.
  b) The role picker offers ONLY "Admin" and "User". "SuperAdmin" is deliberately absent from the
     checkbox list even though it appears as a role chip in the grid.
  c) A missing creation date renders as an em dash "—", not as a blank cell or a placeholder date.

COPY (verbatim Vietnamese — reproduce character for character; there is no i18n layer, every string is
hardcoded in the templates):
- Browser tab title: "PlatformManager" (static — it never changes per route)
- Route/topbar title: "Người dùng hệ thống"
- Sidebar: "PM", "PlatformManager"; collapse button aria-label "Mở rộng menu" / "Thu gọn menu"; nav
  labels "Dashboard", "Danh mục", "DTI", "Quản trị hệ thống", "Người dùng", "Phân quyền"
- Topbar: hamburger aria-label "Mở menu điều hướng"; logout button title and label "Đăng xuất"
- Toast close aria-label: "Đóng thông báo"
- Card heading: "Danh sách người dùng"
- Create button: "+ Thêm người dùng"
- Search placeholder: "Tìm theo tên hoặc email..." (three ASCII dots, not a real ellipsis)
- The 5 column headers, in order: "Người dùng", "Vai trò", "Trạng thái", "Ngày tạo", "Hành động"
- Missing email and missing/unparseable date both render: "—" (em dash)
- Role chip text: the role name exactly as the API sent it — the shipped values are "Admin", "User"
  and "SuperAdmin"
- Status badge, locked: "● Đã khoá" (a leading U+25CF glyph then a space)
- Status badge, active: "● Đang hoạt động"
- Edit action tooltip: "Sửa"
- Lock action tooltip when self-lock is blocked: "Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất"
- Lock action tooltip when the target is locked: "Mở khoá tài khoản"
- Lock action tooltip when the target is unlocked: "Khoá tài khoản"
- Grid empty message: "Không có người dùng nào khớp bộ lọc."
- Dialog title: "Thêm người dùng" (create) / "Sửa người dùng" (edit)
- Dialog close button: "Đóng"
- Field labels, each followed by a red "*": "Tên đăng nhập", "Email", "Họ tên", "Mật khẩu tạm", "Vai trò"
- Field placeholders: "vd nguyen.van.a", "ten@congty.vn", "Nguyễn Văn A", "vd TempPass@123"
- Role checkbox labels: "Admin" and "User" only — "SuperAdmin" is deliberately not offered
- Preserved-roles note: "Vai trò hệ thống: <roles, comma-separated, bold> — giữ nguyên, không thay đổi khi lưu."
- Dialog buttons: "Huỷ" and "Lưu"
- Validation messages, checked in this order, one at a time: "Email bắt buộc.", "Họ tên bắt buộc.",
  "Chọn ít nhất 1 vai trò.", then in create mode only "Tên đăng nhập bắt buộc." and
  "Mật khẩu tạm phải có ít nhất 8 ký tự."
- Success toasts: "Đã thêm người dùng.", "Đã cập nhật người dùng.", "Đã mở khoá tài khoản.", "Đã khoá tài khoản."
- Dialog fallback errors when the API sends no message: "Không tạo được người dùng — thử lại sau." /
  "Không cập nhật được người dùng — thử lại sau."
- Server errors shown verbatim as toasts: "Bạn không thể tự khoá tài khoản của chính mình. Nếu muốn kết thúc phiên làm việc, hãy đăng xuất." and "Chỉ SuperAdmin mới được khoá tài khoản có vai trò SuperAdmin."
- Shared HTTP error toasts: "Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.", "Bạn cần đăng nhập để tiếp tục.", "Bạn không có quyền thực hiện thao tác này.", "Không tìm thấy dữ liệu yêu cầu.", "Bạn thao tác quá nhanh. Vui lòng chờ một lát rồi thử lại.", "Đã có lỗi xảy ra. Vui lòng thử lại."

STATES:
- Populated (default): one row per user on the current page, roles in API order, dates in vi-VN
  format, one status badge per row.
- Loading: a translucent mask with a spinning circular indicator covers the grid only. The search
  field, the paginator and the "+ Thêm người dùng" button all stay interactive underneath. On first
  load the empty message never flashes — it is suppressed while loading is true.
- Empty: the single-row message spanning all 5 columns, with the paginator still rendered. The same
  message covers both an empty database and an over-narrow search.
- Error on the list fetch: there is NO screen-level error state — no banner, no retry button. The
  previous rows stay on screen, silently stale (or the grid stays empty on a first-load failure). The
  only feedback is an error toast bottom-right. A 401 mid-session additionally redirects to the
  sign-in route.
- Row action disabled: only the self-lock case described above. Opacity 0.45, not-allowed cursor,
  hover suppressed. When the signed-in identity is not yet known, no row is blocked.
- Dialog, create mode: title "Thêm người dùng"; the "Tên đăng nhập" and "Mật khẩu tạm" rows are
  present; every field starts empty; no role checkbox is ticked; the username field is focused and its
  text selected; the preserved-roles note is absent.
- Dialog, edit mode: title "Sửa người dùng"; the "Tên đăng nhập" and "Mật khẩu tạm" rows are NOT
  rendered at all; email and full name are pre-filled; role checkboxes are pre-ticked from the
  target's assignable roles. A role the picker cannot assign (today only "SuperAdmin") has no checkbox
  but is named in the preserved-roles note and is silently re-sent unchanged on save.
- Validation: submit-time only, ONE message at a time, rendered in a single red line below the role
  row. No field is marked invalid, no field gets a red border, and there is no per-field message. The
  message clears on the next successful submit or when the dialog is reopened.
- Saving: there is NO busy state — neither "Lưu" nor "Huỷ" is disabled while the request is in flight,
  no spinner appears, and a second click fires a second request. Do not add a loading state.
- Save success: the dialog closes, the list refetches with the current search and page unchanged, and
  a success toast appears bottom-right for 5 seconds.
- Save failure: the dialog STAYS OPEN with the message in its red error line — and because the shared
  error handler also toasts, the same message appears TWICE, once inline and once as a toast.
- Lock / unlock: no optimistic update and no per-row busy state. The row is unchanged until the
  refetch lands, then the badge and the lock icon flip together and a success toast appears.
- Search: the field itself is never laggy; the request is debounced 300 ms and de-duplicated, so a
  burst of keystrokes produces exactly one request carrying the final string.
- Access denied: not a visual state — a non-Admin user is redirected away before anything renders.

RESPONSIVE:
- The screen's own stylesheets declare ZERO media queries. Every breakpoint effect below comes from
  the shell or from fluid min()/flex widths.
- 981px and up (desktop default): sidebar fixed at 220px (or 60px collapsed) with the content column
  offset to match; main capped at 1600px with 14px padding; the topbar hamburger is hidden; a collapsed
  sidebar shows submenus as hover/focus flyouts opening to its right.
- 980px and below: the content column loses its left offset; the sidebar becomes an off-canvas drawer,
  width min(85vw, 300px), slid fully off-screen until opened, over a dark dismiss backdrop; the topbar
  hamburger appears at 9px padding. The card, search field and grid simply reflow to the full width.
- 560px and below: main padding drops to 10px; the topbar hides the user's name, leaving only the
  "Đăng xuất" button; the drawer widens to min(90vw, 300px) and its nav items grow to 10px padding /
  40px minimum height. The search field keeps its 220px minimum and fills the row.
- The grid has NO responsive/stacking layout — all 5 columns render at every viewport. Their 690px of
  combined minimum width means that on a narrow screen the grid scrolls horizontally inside its own
  inner container, while the rounded wrapper around it stays clipped rather than scrolling.
- The dialog is fluid at min(560px, 92vw) with no breakpoint; the role checkbox row stays a single
  horizontal flex row and does not stack on small screens.
- The toast stack keeps max-width min(360px, 90vw) pinned 14px from the right and bottom at every size.
- Print: the sidebar, backdrop, topbar and toast stack are hidden; the content column loses its offset
  and main loses its max-width. The card and grid print as-is with no print-specific layout.

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
   are interchangeable with the literals above: `colors.primary` / `colors.brand` (#0f5bd7) ·
   `colors.bg` (#eef2f8) · `colors.card` (#ffffff) · `colors.tonal-bg` (#dbe7fa) ·
   `colors.tonal-ink` (#0f4a9e) · `colors.muted` (#57647a) · `colors.line` (#dfe6ef) ·
   `colors.border-strong` (#7e91b4) · `colors.good` / `colors.good-bg` (#0e7050 / #d9f2e6) ·
   `colors.bad` / `colors.bad-bg` (#a02b2b / #fbdcdc) · `colors.surface-table-header` (#f8fafc) ·
   `colors.text-table-header` (#536076) · `rounded.lg` (16px) · `rounded.table` (12px) ·
   `rounded.pill` (999px) · `spacing.card-padding` (14px) · `spacing.cell-padding` (6px 8px) ·
   `spacing.badge-padding` (3px 6px) · `typography.table-cell` (12px/400) · `typography.badge`
   (10px/750) · `components.card` · `components.button-primary` · `components.action-button` ·
   `components.table-header` · `components.table-cell` · `components.table-wrap` ·
   `components.badge-success` · `components.badge-danger` · `components.role-tag` ·
   `components.dialog` · `components.form-label` · `components.toast`.

4. Attach `Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png` and tell Stitch that
   the capture was taken against a **freshly seeded (effectively empty) database**, so the grid shows
   exactly one row — the bootstrap administrator. Generate a realistic multi-row list from the column
   description in the Master Prompt; do not conclude that one row is the normal state.

This repo has no Stitch MCP configured — do the import manually via stitch.withgoogle.com (see
`doc/Design/SETUP.md` to add one).

## Claude Design

Paste the **Master Prompt** above, attach
`Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png`, and add the two notes plus the
token block below. (`Assets/Brand/` is empty — the shipped app has no logo or brand image file; the
"PM" mark is a styled text square.)

**Note 1 — the screenshot is an empty-database capture.** It shows the shell, the card, the search
field, all 5 column headers and a single seeded row: the bootstrap administrator, with two role chips
("Admin", "SuperAdmin"), the green `● Đang hoạt động` badge, a vi-VN date, and page size 10. Use it as
the authority for chrome, spacing, chip and badge treatment; generate a realistic multi-row list from
the column description. Do not conclude the grid normally holds one user.

**Note 2 — that one row happens to capture the self-lock state.** The viewer IS that account, so its
lock button renders greyed out at opacity 0.45. That is the deliberate disabled state described in
the prompt, not a rendering artefact — keep it, and give the other generated rows a normal enabled
lock button.

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
  /* avatar 30px circle, border-radius 50% */
  /* role chip radius 6px, padding 2px 8px, margin-right 4px */
  /* row-actions gap 6px; dialog-actions gap 8px, margin-top 8px */
  /* search wrapper min-width 220px, input padding-left 32px, icon left 10px at 13px */
  /* column min-widths 220 / 140 / 120 / 110 / 100px (690px combined) */
  /* topbar surface rgba(255,255,255,0.95) + blur(10px) */
  /* dialog + drawer backdrop rgba(20,28,40,0.45) */
  /* active sidebar item rgba(15,91,215,0.08) */
  /* dialog shadow 0 24px 70px rgba(0,0,0,0.25) */
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
including the "..." three-dot placeholder, the "●" U+25CF glyph that prefixes both status badges,
and the "—" em dashes used as null placeholders.

TOKENS (literal values): brand/primary #0f5bd7, primary hover #174ca8, on-primary #ffffff, page
background #eef2f8, card/dialog/input surface #ffffff, ghost hover tint #e1e7f1, secondary button
fill #dbe7fa with text #0f4a9e and hover #c7dbf5, text #152033, muted #57647a, hairline #dfe6ef,
strong input/table-wrapper border #7e91b4, success #0e7050 on #d9f2e6, danger #a02b2b on #fbdcdc
with hover #f5c6c6, table header surface #f8fafc with text #536076 (also the even-row tint and the
role-chip fill), topbar rgba(255,255,255,0.95) + blur(10px), dialog backdrop rgba(20,28,40,0.45),
active nav item rgba(15,91,215,0.08). Card shadow 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px
rgba(23,39,67,0.06); dialog shadow 0 24px 70px rgba(0,0,0,0.25). Font Inter (loaded from Google
Fonts at weights 400/500/600/700) falling back to "Segoe UI", Arial, sans-serif: body 13px/400,
topbar h1 15px/bold, card h2 14px/bold, button 12px/700, row-action button 11px/700, table header
11px/700, table cell 12px/400, user name 12px/700, email and date 11px/400, role chip 11px/400,
status badge 10px/750, avatar initials 11px/800, form label 12px/700, role checkbox label 12px/600,
sidebar nav 12px/600 (700 active), toast 12px/400. Radius 7px buttons+inputs+row actions, 9px nav
item and toast, 12px table wrapper, 15px dialog, 16px card, 999px badge, 6px role chip, 50% avatar.
Spacing scale 4 / 6 / 8 / 10 / 14px; card, dialog and main padding 14px; table cell padding 6px 8px;
button padding 6px 8px; badge padding 3px 6px; role chip padding 2px 8px. Sidebar 220px (60px
collapsed), content max-width 1600px, breakpoints 980px and 560px. Icons are PrimeIcons v7 rendered
as <i class="pi pi-…">.

Fidelity rules for this screen specifically:
- It renders inside the app shell: fixed left sidebar, sticky topbar, centred main, fixed
  bottom-right toast stack. It is not a bare page.
- The lock button is DISABLED on the signed-in user's own unlocked row, at opacity 0.45 with a
  not-allowed cursor and the tooltip "Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất".
  This is deliberate and correct — reproduce it. Unlocking is never blocked; locking another user,
  including a SuperAdmin, is never blocked.
- The role picker offers ONLY "Admin" and "User". "SuperAdmin" is deliberately omitted from the
  checkboxes even though it appears as a role chip in the grid. Do not add it.
- A missing creation date renders as an em dash "—", never as a blank cell or a placeholder date.
- The temporary-password field in create mode is plain text, not masked. That is deliberate.
- There is no busy state on save, no inline field validation, no error banner and no retry control.
  Validation shows ONE message at a time in a single red line at the bottom of the dialog. Do not
  invent per-field errors or spinners.
- The paginator shows page buttons and a rows-per-page select only — no "showing X of Y" text.
- The 5 columns never stack or collapse; they scroll horizontally at a 690px combined minimum.
```

**User prompt** = the `LAYOUT:`, `COPY:`, `STATES:` and `RESPONSIVE:` sections of the Master Prompt
above, pasted verbatim.

**Image part** = `Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png`, introduced
with: "Captured against a freshly seeded, otherwise empty database — the single row is the bootstrap
administrator and its lock button is greyed because the viewer is that same account. Use the
screenshot for chrome, spacing, chips and badges; generate a realistic multi-row list."

## Generic

Paste the Master Prompt block verbatim into any other AI UI-generation tool (v0, Bolt, Lovable,
Figma AI, …) and attach the screenshot below. The block is self-contained — no token resolution,
no other file and no follow-up prompt are required.

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/quan-tri-nguoi-dung/user-list--desktop-1440.png` — the only screenshot for this
  screen. Default state at 1440px wide, sidebar expanded, **empty database**: one seeded bootstrap
  administrator row carrying the "Admin" and "SuperAdmin" chips, the `● Đang hoạt động` badge, a
  vi-VN date, a disabled (self-lock-blocked) lock button, and page size 10.
- `Tokens/tokens.json` — W3C DTCG token file (`global` + `light`; `dark` is intentionally empty, the
  app ships one theme).
- `DESIGN.md` — lint-clean token dictionary, for the Stitch import.
- `Assets/Brand/` — **none exist**. The app ships no logo or brand image file; the "PM" mark is a
  26x26px square filled #0f5bd7 with white 11px/800 text, and user avatars are initials on a #0f5bd7
  circle — there is no image-avatar path at all.
