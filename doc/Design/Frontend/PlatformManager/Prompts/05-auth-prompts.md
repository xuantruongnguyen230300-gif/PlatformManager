---
kind: luat
scope: du-an
verified: 2026-09-08
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
screen_ref: "05-auth"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — Authentication (Sign in · Change password)

<!-- One pack for Screens/05-auth.md, covering BOTH screens in the flow. Master Prompt filled from that spec + Tokens/tokens.json (light set — the app's only shipped theme) + src/FE/src/styles.scss. Fidelity rule: prompts reproduce the app AS-SHIPPED — quirks included, nothing idealized. -->

> 🔁 **Regenerated 2026-08-29 — the previous pack produced the wrong theme.** It was written on 2026-08-22 and still carried the pre-rewrite palette (`#eef2f8` page, `#dfe6ef` border, `#57647a` muted, `#7e91b4` input border, `#e5a8a8` error edge) and the font `Inter`. None of those ship. Running it today generated a card in the old colours with a font the app no longer loads, and nothing warned about it — an external tool cannot check a hex against a repo. Every literal below was re-resolved from `src/FE/src/styles.scss` on 2026-08-29. The other three corrections in the same pass: the reveal button is now the shared 24px ghost icon button (not a bespoke 6px-radius control), the change-password screen now has **per-field** error slots, and the post-success destination is `/trang-chu` rather than `/dashboard`.

> **The defining fact about both screens: there is NO APP SHELL.** No sidebar, no top bar, no page header, no footer, no breadcrumb, no navigation of any kind. Both routes declare `noShell`, so the app renders a bare centred card on an empty page. Every other screen in PlatformManager has a 220px navigation rail and a sticky top bar — these two deliberately do not. A generator that wraps either card in a shell has produced the wrong screen.
>
> The two screens are one flow, not two features: an administrator-created account must change its password before anything else opens, so a guard closes every other route until the change succeeds.
>
> Every literal below is resolved from `src/FE/src/styles.scss` via `Tokens/colors.md`, `Tokens/spacing.md`, `Tokens/typography.md`, `Tokens/tokens.json` and `DESIGN.md`. Nothing in this pack needs a lookup in another file.


> ## 🔄 SỬA 2026-09-06 — màn đăng nhập ĐÃ CÓ nút đổi ngôn ngữ
>
> Hai chỗ trong file này liệt *"no language switcher"* vào danh sách những thứ **không** được
> vẽ. Điều đó đúng cho tới 2026-09-06; nay `<app-language-switcher>` có thật ở
> `login.page.html:99` — hai nút `Tiếng Việt` / `English` phía dưới thẻ đăng nhập.
>
> Vì sao chỗ sai này đắt hơn một dòng doc sai bình thường: đây là prompt đưa cho công cụ sinh
> giao diện. Một câu "không có X" khiến máy **bỏ X đi**, và kết quả là một màn hình trông hợp lý
> nhưng không khớp thứ đang chạy — đúng thứ Fidelity Policy sinh ra để chặn.

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block covering both screens. External tools cannot resolve token references — every value below is already a literal hex/px/font string. To generate a single screen, paste SHARED FRAME + the SCREEN block you want. -->

```
Recreate these two exact shipped screens — do not idealize. They belong to a Vietnamese-
language internal console called PlatformManager. Reproduce the Vietnamese copy character
for character; do not translate it, do not correct it, do not shorten it.

=== SHARED FRAME (applies to BOTH screens) ===

NO APP SHELL. Draw no sidebar, no top bar, no navigation rail, no page header, no footer,
no breadcrumb, no logo bar, no marketing panel, no split-screen illustration, no background
image or gradient. The entire viewport is one flat #cfdaea surface with a single white card
centred both horizontally and vertically. That is the whole screen.

TOKENS (literal values):
Colors: page background #cfdaea; card surface #ffffff; card border 1px solid #7a97bd; input
border 1px solid #6077a2 (deliberately darker than the card border — an input border is the
only border in this design system that means "you can type here"); text #152033; muted text
#4c576b; brand/primary #0f5bd7; brand hover #174ca8; text on brand #ffffff; error text
#a02b2b on error fill #fbdcdc with error border #e0a8a8; disabled input fill #dbe4f0; ghost
icon-button hover fill #c1cde2; focus ring rgba(15,91,215,0.12); invalid focus ring
rgba(160,43,43,0.14); success (toast accent only) #0e7050; warning (toast accent only)
#965e08; info (toast accent only) #0f5bd7 on tint #c4d8f6 with ink #0f4a9e.
Shadows: card = 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06); toast =
0 14px 38px rgba(23,39,67,0.26), 0 2px 6px rgba(23,39,67,0.12); primary button hover =
0 8px 20px rgba(15,91,215,0.35).
Font: Be Vietnam Pro, SELF-HOSTED as woff2 (not Google Fonts at runtime), weights 400, 500,
600, 700 and 800, with fallbacks "Segoe UI", Arial, sans-serif. It is a Vietnamese-first
face — every diacritic in the copy below must render from the real font, not from a
fallback. Base body size is 13px.
Radii: 16px card; 12px brand mark; 7px inputs, the error block, the submit button and the
ghost icon button; 9px toast; 999px the toast severity disc.
Transitions: 150ms ease on button and input state changes; 100ms on the button press.

CENTRING SHELL: a full-viewport flex box, min-height 100dvh (with a 100vh fallback declared
first), centred on both axes, 14px padding on all sides. It is the only thing keeping the
card off the screen edge on a narrow viewport.

CARD: width 100% capped at max-width 380px; #ffffff fill; 1px solid #7a97bd border; 16px
radius; shadow 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06); padding
32px 28px. That internal padding NEVER shrinks — there is no responsive rule anywhere on
these screens.

BRAND BLOCK (top of the card): a centred vertical stack, 8px gaps, 24px bottom margin.
  - A 44px × 44px square with 12px radius, filled #0f5bd7, containing the two letters "PM"
    in #ffffff at 15px/800. It is TEXT, not an image — the app ships no logo file, so do not
    substitute an icon, a monogram graphic or an SVG.
  - A heading at 18px/800 in #152033.
  - A sub-line at 12px/400 in #4c576b. It wraps to two centred lines when long.

FORM-LEVEL ERROR BLOCK (rendered only when there is an error with no field attached,
directly under the brand block and above the form): a horizontal flex row, 8px gap,
vertically centred; fill #fbdcdc; 1px solid #e0a8a8 border; #a02b2b text; 7px radius; 8px
padding; 12px font; 10px bottom margin. It opens with a filled exclamation-in-a-circle
glyph, then the message text. When there is no error the element is absent entirely — do
not reserve space for it and do not draw an empty outline.

FIELD (repeated per input): a label above an input, optionally followed by one error line.
  - Label: block, 12px/700, #152033, 6px bottom margin. The whole field group has a 10px
    bottom margin.
  - Input row: position-relative. A decorative glyph sits absolutely at left 12px, #4c576b,
    15px, ignoring pointer events. The input itself is full width, 1px solid #6077a2, 7px
    radius, #ffffff fill, padding 10px 12px 10px 36px (the 36px left inset is what clears
    the glyph), 12px text, #152033. Placeholder text is muted. When the row also carries a
    trailing button the input's right padding grows to 40px.
  - Focus: the outline is removed and replaced by a #0f5bd7 border plus a 3px
    rgba(15,91,215,0.12) ring. This is the treatment for EVERY input in the app, not a
    one-screen exception.
  - Invalid: the border turns #a02b2b and the focus ring becomes 3px rgba(160,43,43,0.14);
    an error line renders under the input in #a02b2b at 12px with a 6px vertical margin, and
    it carries NO glyph (unlike the form-level block). SIGN IN NEVER USES THIS. CHANGE
    PASSWORD ALWAYS DOES. Do not add it to the sign-in screen and do not omit it from the
    change-password one — the asymmetry ships.
  - Disabled: fill #dbe4f0 with #4c576b text and a not-allowed cursor. Neither auth screen
    ever triggers it; draw it only if asked for a disabled-state study.

GHOST ICON BUTTON (used for the password reveal toggle and the toast dismiss): 24px × 24px,
fully transparent fill and border, 7px radius, 12px glyph in #4c576b, centred. Hover: fill
#c1cde2 with the glyph turning #152033. Focus: 2px #0f5bd7 outline offset 1px. It is the
same control in both places — do not draw two different small buttons.

SUBMIT BUTTON: full width, #0f5bd7 fill, 1px solid #0f5bd7 border, #ffffff text, 7px radius,
11px padding, 14px/700, contents centred in a flex row with an 8px gap. Hover: fill #174ca8
with border to match, plus shadow 0 8px 20px rgba(15,91,215,0.35). Pressed: shifts down 1px.
Focus: 2px #0f5bd7 outline offset 2px. Disabled: 50% opacity with a not-allowed cursor — and
disabled is ONLY ever used while a request is in flight; there is no "fill the form to
enable" behaviour, the button starts enabled with empty fields.

TOAST OVERLAY (both screens): fixed at the bottom-right, 14px from both edges, above
everything, max-width min(400px, 90vw), 8px gap between stacked items. Each toast is a
#ffffff card with a 1px #7a97bd border, a 5px left border tinted by severity (#a02b2b error,
#0e7050 success, #965e08 warning, #0f5bd7 info), 9px radius, shadow 0 14px 38px
rgba(23,39,67,0.26) + 0 2px 6px rgba(23,39,67,0.12), 10px 14px padding and 12px text. Its
contents, left to right: a 22px round severity disc (error = #fbdcdc fill with a #a02b2b
"×" glyph at 11px), then the body, then the 24px ghost dismiss button holding an "×" glyph.
The body is an OPTIONAL bold title at 12px/800 #152033 with a 2px bottom margin, then the
message at 12px with line-height 1.45 — #4c576b when a title is present above it, #152033
when the message stands alone. Toasts slide up 6px over 0.18s on entry and auto-dismiss
after 5 seconds. On these screens only the error severity is ever seen. The dismiss button's
accessible label is "Đóng thông báo".

ICONS: PrimeIcons v7, rendered as icon-font glyphs, never SVG illustrations. Only these
appear: exclamation-circle (inside the form-level error block), envelope, lock, key, eye,
eye-slash, sign-in-arrow, and "×" (both the toast severity disc and its dismiss button). No
decorative icons, no illustration, no avatar. The per-field error line has no glyph at all.

RESPONSIVE (both screens): THERE ARE NO BREAKPOINTS. Not one media query touches either
screen. The card is a fixed 380px on any viewport wider than roughly 408px and shrinks to
fill below that, held off the edges by the shell's 14px padding; the internal 32px 28px
padding is identical at 1440px and at 390px. The only viewport-reactive rule that reaches
these screens is the toast's max-width. On a short viewport the shell grows and the page
scrolls rather than clipping, so the card stops being optically centred — that is
as-shipped behaviour, keep it.

REDUCED MOTION: when the OS asks for reduced motion every transition and animation above
collapses to 0.01ms, including the toast's entrance slide.

PRINT: the card prints exactly as it renders; only the toast stack disappears.


=== SCREEN 1 — Sign in (route /dang-nhap) ===

Card contents, top to bottom: brand block, then (conditionally) the form-level error block,
then a form of two fields, an options row and the submit button.

BRAND BLOCK COPY: mark "PM"; heading "PlatformManager"; sub-line "Đăng nhập để tiếp tục".

FIELD 1 — label "Email", leading envelope glyph, placeholder "ten@congty.vn". Reproduce this
exactly: the field is labelled "Email" and hints at an email address, but it is a plain text
field that actually accepts a USERNAME such as "SuperAdmin". This mismatch ships. Do not
relabel it to "Tài khoản", do not change the placeholder, do not make it an email input type.

FIELD 2 — label "Mật khẩu", leading lock glyph, placeholder "Nhập mật khẩu", masked. It
carries a trailing reveal toggle: the 24px ghost icon button described above, positioned at
right 10px inside the input row, holding an eye glyph while the password is masked and a
crossed-out-eye glyph while it is revealed. Its accessible label is "Hiện mật khẩu" while
masked and "Ẩn mật khẩu" while revealed.

OPTIONS ROW — one row, space-between, vertically centred, 12px text, 20px bottom margin:
  - left: a native square checkbox, 16px × 16px, tinted #0f5bd7 via accent-color and left
    otherwise NATIVE — do not draw a custom tick, a rounded pill or a pseudo-element check.
    A 6px gap before the label "Ghi nhớ đăng nhập". The checkbox is INERT — it is wired to
    nothing and changes nothing. Draw it unchecked.
  - right: the link "Quên mật khẩu?" in #0f5bd7 at weight 700, no underline until hover. It
    leads nowhere — there is no password-reset screen in the product.

SUBMIT — full-width primary button whose contents are a sign-in arrow glyph followed by the
label "Đăng nhập". While a request is in flight the label swaps to "Đang đăng nhập…" and the
button is disabled at 50% opacity — the glyph stays, and NO spinner appears.

SIGN-IN COPY (verbatim):
- Browser tab title: "Đăng nhập · PlatformManager". It is not displayed anywhere on the
  card, because there is no top bar to display it in.
- Brand mark "PM"; heading "PlatformManager"; sub-line "Đăng nhập để tiếp tục".
- "Email", "ten@congty.vn", "Mật khẩu", "Nhập mật khẩu".
- "Hiện mật khẩu", "Ẩn mật khẩu" (accessible labels on the reveal toggle).
- "Ghi nhớ đăng nhập", "Quên mật khẩu?".
- "Đăng nhập" (idle), "Đang đăng nhập…" (submitting).
- Form-level error text is one of: "Vui lòng nhập đầy đủ tài khoản và mật khẩu." (client
  check); the server's own sentence verbatim, e.g. "Bạn thao tác quá nhanh. Vui lòng thử lại
  sau 47 giây."; or the fallback "Đăng nhập thất bại — thử lại sau."
- Toast text repeats the server's sentence with NO title. Only when the response carries no
  usable body does the toast gain a bold title, and then it is one of these title + message
  pairs: "Mất kết nối" / "Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng." · "Chưa đăng
  nhập" / "Bạn cần đăng nhập để tiếp tục." · "Không đủ quyền" / "Bạn không có quyền thực hiện
  thao tác này." · "Không tìm thấy" / "Không tìm thấy dữ liệu yêu cầu." · "Quá nhiều yêu cầu"
  / "Bạn đã gửi quá nhiều yêu cầu — vui lòng chờ một lát rồi thử lại." · "Lỗi hệ thống" /
  "Đã có lỗi xảy ra. Vui lòng thử lại."

SIGN-IN STATES:
- Idle (default, and the state in the reference screenshot): both fields empty showing their
  placeholders, password masked with the eye glyph, checkbox unchecked, button enabled
  reading "Đăng nhập", no error block in the DOM at all.
- Idle with a deep link: rendering is IDENTICAL. When the user was bounced here from a
  protected page the target is carried only in the address bar as a query parameter — nothing
  on the card acknowledges it. Do not add a "sign in to continue to …" line.
- Submitting: only the button changes — disabled, 50% opacity, label "Đang đăng nhập…". Both
  inputs, the reveal toggle, the checkbox and the link all stay enabled and editable for the
  whole round trip. No spinner, no overlay, no progress bar anywhere.
- Error: the SAME sentence appears TWICE — once in the form-level error block at the top of
  the card and once in a bottom-right toast. Reproduce both. Invalid credentials, a
  locked-out account and a rate-limited IP all render identically; only the sentence differs.
  There is no countdown, no cooldown and no distinct severity styling for any of them.
- Client validation: writes its message into the same single error block the server uses.
  There is exactly ONE message slot on THIS card and no field-level feedback of any kind.
  Empty fields are additionally blocked by the browser's own native "required" bubble.
- Success: nothing is rendered — no toast, no checkmark, no transition state. The screen is
  simply replaced by the next route.


=== SCREEN 2 — Change password (route /doi-mat-khau) ===

Structurally the same shell, card, brand block, field recipe, form-level error block and
full-width submit as Sign in — with different contents and ONE real addition: a per-field
error slot. It is the taller of the two cards because it has three fields.

BRAND BLOCK COPY: mark "PM"; heading "Đổi mật khẩu"; sub-line is one of two sentences, and
this is the only place in the whole flow where copy changes at runtime:
  - forced (an administrator created the account and the password has never been changed):
    "Bạn cần đổi mật khẩu trước khi tiếp tục sử dụng hệ thống." — it wraps to two centred
    lines at 380px. This is the default arrival and the state in the reference screenshot.
  - voluntary (the user came here by choice): "Đổi mật khẩu tài khoản của bạn."

THREE FIELDS, all masked password inputs, in this order:
  1. label "Mật khẩu hiện tại", leading lock glyph, placeholder "Nhập mật khẩu hiện tại".
  2. label "Mật khẩu mới", leading key glyph, placeholder "Nhập mật khẩu mới".
  3. label "Xác nhận mật khẩu mới", leading key glyph, placeholder "Nhập lại mật khẩu mới".
NONE of the three has a reveal toggle — unlike Sign in. Do not add one.
EACH of the three can render its own error: a #a02b2b border on the input plus a #a02b2b
12px line directly beneath it, with a 6px vertical margin and NO glyph.

SUBMIT — full-width primary button reading "Đổi mật khẩu", TEXT ONLY with NO leading glyph
(unlike Sign in's). While submitting, the label swaps to "Đang lưu…" and the button is
disabled at 50% opacity. No spinner.

ABSENT BY CONSTRUCTION — do not add any of these: no options row, no remember-me checkbox,
no forgot-password link, no reveal toggle, no password-strength meter, no rules hint listing
the 8-character minimum the screen actually enforces, no "skip for now" or "do this later"
control, no sign out, and no link of any kind. In the forced case this card is the only
screen the user can reach, and it offers no way off it.

CHANGE-PASSWORD COPY (verbatim):
- Browser tab title: "Đổi mật khẩu · PlatformManager", again displayed nowhere on the card.
- Brand mark "PM"; heading "Đổi mật khẩu"; sub-line "Bạn cần đổi mật khẩu trước khi tiếp tục
  sử dụng hệ thống." (forced) or "Đổi mật khẩu tài khoản của bạn." (voluntary).
- "Mật khẩu hiện tại", "Nhập mật khẩu hiện tại".
- "Mật khẩu mới", "Nhập mật khẩu mới".
- "Xác nhận mật khẩu mới", "Nhập lại mật khẩu mới".
- "Đổi mật khẩu" (idle), "Đang lưu…" (submitting).
- Per-field error lines, each under its own input:
  field 1 — "Vui lòng nhập mật khẩu hiện tại."
  field 2 — "Vui lòng nhập mật khẩu mới." or "Mật khẩu mới phải có ít nhất 8 ký tự."
  field 3 — "Vui lòng nhập lại mật khẩu mới." or "Xác nhận mật khẩu mới không khớp."
- Form-level error block text (only for failures that belong to no field): the server's own
  sentence verbatim, or the fallback "Đổi mật khẩu thất bại — thử lại sau."
- Toast text and its fallback title + message pairs are exactly as listed under Sign in.

CHANGE-PASSWORD STATES:
- Idle forced (default arrival, and the reference screenshot): three empty masked fields, no
  red borders, no error lines, submit enabled, no form-level error block, the forced sub-line
  showing. Nothing on the card explains who imposed the requirement or offers a way out.
- Idle voluntary: identical except for the shorter sub-line.
- Submitting: only the button changes — disabled, 50% opacity, label "Đang lưu…". All three
  fields stay editable. No spinner.
- Validation: every check runs on submit only — nothing validates on blur or while typing —
  and the checks DO NOT short-circuit. Every failing field lights up AT ONCE: red border plus
  its own message underneath. Draw two simultaneous field errors if asked for the validation
  state; a single message at the top of the card is the OLD behaviour and is wrong. While any
  field error is showing, the form-level error block is hidden, so the two never appear
  together. The 8-character rule is enforced but never stated anywhere on the card.
- Error: a server failure carrying a field key lands under that field AND still raises the
  bottom-right toast; a server failure with no field key goes to the form-level block AND the
  toast. Purely client-side validation failures produce the field lines ONLY — no request
  leaves the browser, so no toast.
- Success: nothing is rendered — no toast, no confirmation, no checkmark. The screen is
  simply replaced by the home screen.


=== DO NOT ADD (to either screen) ===
- No sidebar, top bar, header, footer, breadcrumb or navigation of any kind.
- No split-screen marketing panel, hero image, background photograph, gradient, pattern or
  illustration. The page is one flat #cfdaea field.
- No logo image — the "PM" mark is text in a rounded square, and the app ships no logo file.
- No social sign-in buttons, no "create an account" link, no support or
  version footer, no copyright line.
- No per-field validation styling ON THE SIGN-IN SCREEN — no red border, no helper text, no
  strength meter, no character counter, no requirements checklist. (The change-password
  screen has red borders and error lines and nothing else from that list.)
- No spinner, skeleton or progress indicator — only the button's disabled state and its
  swapped label.
- No dark mode and no theme toggle. The shipped theme is light only.
- No breakpoint-specific layout: do not shrink the card padding or restack anything on
  mobile.

Match the attached screenshots for LAYOUT ONLY. Both were captured 2026-08-22, before the
palette and the font changed, so where a screenshot disagrees with the colours or the
typeface above, THIS TEXT WINS. Where it disagrees about layout, the screenshot wins.
```

## Google Stitch

1. Lint `DESIGN.md`, then import it into the Stitch project (Design → import design.md) so the palette, type scale, radii and spacing land as Stitch design tokens:

```bash
npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md
```

Gate = 0 errors; warnings are recorded as as-shipped facts, not blockers (`doc/Design/CLAUDE.md` § Rules). Read the current count from the command's own output rather than from a number written here. The bare `npx @google/design.md lint` form fails silently on Windows — always use the `--package=…designmd` form.

2. Paste the Master Prompt above verbatim. **Keep the literal values in it even after the import** — a silently failed import otherwise produces an off-palette card with no warning, which is exactly how the previous revision of this pack shipped the wrong theme for a week.

3. Add this Stitch-specific preamble above the pasted prompt, because Stitch's defaults for a login screen are exactly what these screens are not:

```
Generate TWO desktop screens at 1440×900. Each is a single centred 380px white card on a
flat #cfdaea page — a bare authentication layout with NO navigation chrome of any kind, NO
split-screen marketing panel, NO background image or gradient, NO illustration, NO social
sign-in buttons and NO sign-up link. Compact density: 12px labels and inputs, 32px 28px
card padding. Set the typeface to Be Vietnam Pro; every string is Vietnamese and is supplied
verbatim below, so the face must carry full Vietnamese diacritics. Do not invent, translate
or paraphrase any of it.
```

This repo has **no Stitch MCP configured** — import and generate manually at stitch.withgoogle.com (see `doc/Design/SETUP.md` if you want to automate it). Log whatever comes back in `Exports/`.

## Claude Design

Paste the Master Prompt above, attach both `Assets/Screenshots/auth/sign-in--desktop-1440.png` and `Assets/Screenshots/auth/change-password--forced--desktop-1440.png`, and prepend the token block below. There are **no brand image assets** to attach — the app ships no logo file; the "PM" mark is a text square (`UiInventory.md` § Brand Assets).

Say this in the same turn: *"Both screenshots predate the current palette and font — treat them as layout references only, and take every colour and the typeface from the CSS block."*

Every right-hand side below is a resolved literal — nothing here needs interpolation. Property names deliberately match the shipped custom properties in `src/FE/src/styles.scss`, so a generated stylesheet maps back to the app 1:1:

```css
:root {
  /* colors — src/FE/src/styles.scss :root, palette of 2026-08-29 */
  --bg: #cfdaea;
  --card: #ffffff;
  --surface-2: #c1cde2;       /* ghost icon-button hover fill */
  --surface-track: #dbe4f0;   /* disabled input fill */
  --tonal-bg: #c4d8f6;        /* info toast disc */
  --tonal-ink: #0f4a9e;
  --text: #152033;
  --muted: #4c576b;
  --line: #7a97bd;            /* card, toast and options-row boundaries */
  --border-strong: #6077a2;   /* inputs only */
  --brand: #0f5bd7;
  --brand2: #174ca8;
  --on-primary: #ffffff;
  --bad: #a02b2b;
  --bad-bg: #fbdcdc;
  --danger-border: #e0a8a8;
  --good: #0e7050;            /* toast accent only */
  --warn: #965e08;            /* toast accent only */
  /* elevation */
  --shadow: 0 4px 16px rgba(23, 39, 67, 0.1), 0 1px 3px rgba(23, 39, 67, 0.06);
  --shadow-toast: 0 14px 38px rgba(23, 39, 67, 0.26), 0 2px 6px rgba(23, 39, 67, 0.12);
  --shadow-primary-hover: 0 8px 20px rgba(15, 91, 215, 0.35);
  --shadow-focus-ring: 0 0 0 3px rgba(15, 91, 215, 0.12);
  --shadow-focus-ring-invalid: 0 0 0 3px rgba(160, 43, 43, 0.14);
  /* typography — Be Vietnam Pro, self-hosted woff2 at 400/500/600/700/800 */
  --font-family-base: 'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif;
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
  --radius-pill: 999px;
}
```

Off-scale literals these screens genuinely ship, to reproduce rather than round: card `max-width: 380px` and `padding: 32px 28px`; brand mark `44px × 44px` at `border-radius: 12px` with its text at `15px/800`; card heading `18px/800`; brand block `margin-bottom: 24px`; input `padding: 10px 12px 10px 36px`, growing to `padding-right: 40px` when a trailing button is present; field glyph `font-size: 15px` at `left: 12px`; reveal toggle at `right: 10px` inside a `24px × 24px` ghost button; checkbox `16px × 16px`; options row `margin-bottom: 20px` with a `6px` checkbox gap; submit button `padding: 11px` with an `8px` icon gap; form-level error block `gap: 8px`; toast left rule `5px` and severity disc `22px`.

Ask for both artboards side by side — they share every measurement except their contents, and reviewing them together is how the intentional differences (the reveal toggle, the submit-button glyph, the per-field error slots) stay intentional.

## Google AI Studio

**System instruction** — paste as-is:

```
You reproduce an existing shipped web UI exactly as it is, not as it should be. This is
PlatformManager, an internal Vietnamese-language console built in Angular 20 with PrimeNG
and PrimeIcons v7. You are drawing its two authentication screens.

Hard rules:
1. NEITHER SCREEN HAS AN APP SHELL. No sidebar, no top bar, no header, no footer, no
   navigation. The viewport is one flat #cfdaea field with a single white card centred on
   both axes. Never add a split-screen marketing panel, a hero image, a gradient, an
   illustration, social sign-in buttons, a sign-up link or a copyright
   line. This is the single most common way to get these screens wrong.
2. Every visible string is Vietnamese and is given to you verbatim. Reproduce each one
   character for character, including the "…" ellipsis and the "—" em dash. Do not translate,
   correct, shorten or normalise any of it. In particular, the sign-in field labelled "Email"
   with the placeholder "ten@congty.vn" actually accepts a username — keep the label and the
   placeholder exactly as given.
3. Use only these literal values. Colors: #cfdaea page, #ffffff card, #7a97bd card border,
   #152033 text, #4c576b muted text, #6077a2 input border, #0f5bd7 brand, #174ca8 brand
   hover, #ffffff on brand, #a02b2b error text on #fbdcdc fill with a #e0a8a8 border,
   #c1cde2 ghost-button hover, rgba(15,91,215,0.12) focus ring, rgba(160,43,43,0.14) invalid
   focus ring. Shadows: card 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06);
   toast 0 14px 38px rgba(23,39,67,0.26), 0 2px 6px rgba(23,39,67,0.12); primary hover
   0 8px 20px rgba(15,91,215,0.35). Font: Be Vietnam Pro with "Segoe UI", Arial, sans-serif
   fallbacks — card heading 18px/800, brand mark 15px/800, labels 12px/700, inputs and
   sub-line 12px, submit button 14px/700, toast title 12px/800. Radii: 16px card, 12px brand
   mark, 7px inputs and error block and submit button and ghost icon button, 9px toast, 999px
   toast severity disc. Sizes: card max-width 380px, card padding 32px 28px, brand mark 44px
   square, input padding 10px 12px 10px 36px, submit padding 11px, ghost icon button 24px
   square, checkbox 16px square, field bottom margin 10px, brand block bottom margin 24px,
   options row bottom margin 20px, shell padding 14px, toast max-width min(400px, 90vw).
4. There are NO breakpoints on either screen. The card is 380px wide at every viewport above
   ~408px and its internal padding never changes.
5. The two screens handle errors DIFFERENTLY and the asymmetry ships. Sign in has exactly ONE
   message slot — a red block above the form — and NO field-level styling whatsoever. Change
   password has BOTH: a red block for failures belonging to no field, and per-field red
   borders with a red 12px line under each failing input, all failing fields shown at once.
   Never give sign in a field error. Never take them away from change password.
6. There is no spinner and no progress indicator. The only submitting affordance is the
   submit button going 50% opaque with its label swapped.
7. Do not idealize: keep the inert remember-me checkbox, keep the dead "Quên mật khẩu?" link,
   keep the missing reveal toggle on the change-password screen, keep the missing icon on its
   submit button, and keep the fact that a failed request shows the same sentence twice
   (a card message plus a bottom-right toast).
8. The checkbox is a NATIVE checkbox tinted with accent-color, not a custom control. Do not
   draw a bespoke tick, a rounded pill or a pseudo-element check.
```

**User prompt** = the SHARED FRAME + SCREEN 1 + SCREEN 2 + DO NOT ADD sections of the Master Prompt above, pasted verbatim. To generate a single screen, paste SHARED FRAME + that screen's block + DO NOT ADD.

**Image parts** to attach: `Assets/Screenshots/auth/sign-in--desktop-1440.png` and `Assets/Screenshots/auth/change-password--forced--desktop-1440.png`. Tell the model in the user turn: "The two attached captures show the real layout in its idle state, but they were taken before the palette and the font changed — treat them as the authority on LAYOUT only, and take every colour and the typeface from my text."

## Generic

For any other generator (v0, Bolt, Lovable, Figma AI, an internal tool): paste the Master Prompt block verbatim and attach both screenshots. Nothing in the Master Prompt depends on this repository — every value is already a literal and every string is already verbatim.

Four guardrails worth repeating in the tool's own chat after the first generation, because generators reintroduce them by habit:

```
1. Delete the navigation chrome you added — these screens have no sidebar, no top bar, no
   header and no footer. One centred card on an empty #cfdaea page, nothing else.
2. Delete the marketing/illustration half of the layout, the social sign-in buttons and the
   sign-up link. None of them exist in this product.
3. Restore the exact Vietnamese strings I gave you, including the field labelled "Email"
   with the placeholder "ten@congty.vn" — it is a username field and the mismatch ships.
4. Restore the palette I gave you. If anything came out on a pale blue-grey near #eef2f8
   with a #dfe6ef border, or set in Inter, you copied the screenshots instead of my text —
   those captures predate the current theme.
```

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/auth/sign-in--desktop-1440.png` — Sign in, idle state, 1440px desktop. **Layout reference only** — captured 2026-08-22, so its palette and font are stale (`Screens/05-auth.md` § Screenshots).
- `Assets/Screenshots/auth/change-password--forced--desktop-1440.png` — Change password in the **forced** state (the two-line sub-line), 1440px desktop. **Layout reference only**, and it also predates the per-field error slots, so it cannot illustrate the validation state at all.
- `Tokens/tokens.json` (W3C DTCG — enable `global` + `light`; `dark` is intentionally empty, no dark mode ships).
- `DESIGN.md` (lint-clean token dictionary + design guidance, for the Stitch import).
- `Assets/Brand/` — **none**. The app ships no logo or brand image file; the "PM" mark is a text square (`UiInventory.md` § Brand Assets).

## Known gaps in this pack

- **No current screenshot exists for either screen.** Both attachments predate the 2026-08-29 stylesheet rewrite, so this pack's text is the only accurate source for colour and type. Recapturing both is the single highest-value action for this flow — capture instructions are in `Screens/05-auth.md` § Screenshots for each screen.
- **The change-password validation state has never been captured at all.** The per-field slots are newer than every screenshot in the project, so the prompt describes them purely from `Screens/05-auth.md` and the source. `change-password--validation--desktop-1440.png` is the shot to take second.
- **Hover, focus, disabled and reduced-motion states are described but unillustrated.** No capture in `Assets/Screenshots/auth/` shows any of them; a generator gets them from the text alone.
- **The toast is described from the library, not from an auth capture.** No screenshot in the project shows a toast on either auth screen; the values come from `Components/Toast.md` and `src/FE/src/app/shared/components/toast/`.
