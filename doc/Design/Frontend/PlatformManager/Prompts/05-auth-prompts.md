---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
screen_ref: "05-auth"
tools: ["stitch", "claude-design", "ai-studio", "generic"]
---

# Prompt Pack — Authentication (Sign in · Change password)

<!-- One pack for Screens/05-auth.md, covering BOTH screens in the flow. Master Prompt filled from that spec + Tokens/tokens.json (light set — the app's only shipped theme) + src/FE/src/styles.scss. Fidelity rule: prompts reproduce the app AS-SHIPPED — quirks included, nothing idealized. -->

> **The defining fact about both screens: there is NO APP SHELL.** No sidebar, no top bar, no page header, no footer, no breadcrumb, no navigation of any kind. Both routes declare `noShell`, so the app renders a bare centred card on an empty page. Every other screen in PlatformManager has a 220px navigation rail and a sticky top bar — these two deliberately do not. A generator that wraps either card in a shell has produced the wrong screen.
>
> The two screens are one flow, not two features: an administrator-created account must change its password before anything else opens, so signing in routes straight to `/doi-mat-khau` and every other route stays closed until the change succeeds.
>
> Every literal below is resolved from `src/FE/src/styles.scss` via `Tokens/colors.md`, `Tokens/spacing.md`, `Tokens/typography.md` and `DESIGN.md`, all refreshed 2026-08-22. Nothing in this pack needs a lookup in another file.

## Master Prompt (tool-agnostic)

<!-- ONE self-contained block covering both screens. External tools cannot resolve token references — every value below is already a literal hex/px/font string. To generate a single screen, paste SHARED FRAME + the SCREEN block you want. -->

```
Recreate these two exact shipped screens — do not idealize. They belong to a Vietnamese-
language internal console called PlatformManager. Reproduce the Vietnamese copy character
for character; do not translate it, do not correct it, do not shorten it.

=== SHARED FRAME (applies to BOTH screens) ===

NO APP SHELL. Draw no sidebar, no top bar, no navigation rail, no page header, no footer,
no breadcrumb, no logo bar, no marketing panel, no split-screen illustration, no background
image or gradient. The entire viewport is one flat #eef2f8 surface with a single white card
centred both horizontally and vertically. That is the whole screen.

TOKENS (literal values):
Colors: page background #eef2f8; card surface #ffffff; card border 1px solid #dfe6ef; text
#152033; muted text #57647a; input border #7e91b4; brand/primary #0f5bd7; brand hover
#174ca8; text on brand #ffffff; error text #a02b2b on error fill #fbdcdc with error border
#e5a8a8; focus ring rgba(15,91,215,0.12); toggle-button hover tint #eef2f8; success (toast
accent only) #0e7050; warning (toast accent only) #965e08.
Shadows: card and toast = 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06);
primary button hover = 0 8px 20px rgba(15,91,215,0.35).
Font: Inter, loaded from Google Fonts (weights 400, 500, 600, 700 only), with fallbacks
"Segoe UI", Arial, sans-serif. Weight 800 is used by the brand mark and the card heading and
is synthesised by the browser — keep it.
Radii: 16px card; 12px brand mark; 7px inputs, the error block and the submit button; 6px
the password visibility toggle; 9px toast.

CENTRING SHELL: a full-viewport flex box, min-height 100dvh (with a 100vh fallback declared
first), centred on both axes, 14px padding on all sides. It is the only thing keeping the
card off the screen edge on a narrow viewport.

CARD: width 100% capped at max-width 380px; #ffffff fill; 1px solid #dfe6ef border; 16px
radius; shadow 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px rgba(23,39,67,0.06); padding
32px 28px. That internal padding NEVER shrinks — there is no responsive rule anywhere on
these screens.

BRAND BLOCK (top of the card): a centred vertical stack, 8px gaps, 24px bottom margin.
  - A 44px × 44px square with 12px radius, filled #0f5bd7, containing the two letters "PM"
    in #ffffff at 15px/800. It is TEXT, not an image — the app ships no logo file, so do not
    substitute an icon, a monogram graphic or an SVG.
  - A heading at 18px/800 in #152033.
  - A sub-line at 12px/400 in #57647a. It wraps to two centred lines when long.

ERROR BLOCK (rendered only when there is an error, directly under the brand block and above
the form): a horizontal flex row, 8px gap, vertically centred; fill #fbdcdc; 1px solid
#e5a8a8 border; #a02b2b text; 7px radius; 8px padding; 12px font; 10px bottom margin. It
opens with a filled exclamation-in-a-circle glyph, then the message text. When there is no
error the element is absent entirely — do not reserve space for it and do not draw an empty
outline.

FIELD (repeated per input): a label above an input.
  - Label: block, 12px/700, #152033, 6px bottom margin. The whole field group has a 10px
    bottom margin.
  - Input row: position-relative. A decorative glyph sits absolutely at left 12px, #57647a,
    15px, ignoring pointer events. The input itself is full width, 1px solid #7e91b4, 7px
    radius, #ffffff fill, padding 10px 12px 10px 36px (the 36px left inset is what clears the
    glyph), 12px text, #152033. Placeholder text is muted.
  - Focus: the outline is removed and replaced by a #0f5bd7 border plus a 3px
    rgba(15,91,215,0.12) ring — this is the only control in the app that styles focus this
    way; everything else uses a plain 2px #0f5bd7 outline.
  - There is NO per-field error state anywhere: no red border, no helper text, no icon
    change, no character counter, no strength meter. Do not invent one.

SUBMIT BUTTON: full width, #0f5bd7 fill, #ffffff text, 7px radius, 11px padding, 14px/700,
contents centred in a flex row with an 8px gap. Hover: fill #174ca8 plus shadow
0 8px 20px rgba(15,91,215,0.35). Pressed: shifts down 1px. Focus: 2px #0f5bd7 outline offset
2px. Disabled: 50% opacity with a not-allowed cursor — and disabled is ONLY ever used while a
request is in flight; there is no "fill the form to enable" behaviour, the button starts
enabled with empty fields.

TOAST OVERLAY (both screens): fixed at the bottom-right, 14px from both edges, above
everything, max-width min(360px, 90vw), 8px gap between stacked items. Each toast is a
#ffffff card with a 1px #dfe6ef border, a 4px left border tinted by severity (#a02b2b error,
#0e7050 success, #965e08 warning, #0f5bd7 info), 9px radius, the card shadow, 8px 10px
padding, 12px text with line-height 1.4, and a 22px ghost dismiss button holding an "×"
glyph. Toasts auto-dismiss after 5 seconds. On these screens only the error severity is ever
seen. Its dismiss button's accessible label is "Đóng thông báo".

ICONS: PrimeIcons v7, rendered as icon-font glyphs, never SVG illustrations. Only these
appear: exclamation-circle (inside the error block), envelope, lock, key, eye, eye-slash,
sign-in-arrow, and the toast "×". No decorative icons, no illustration, no avatar.

RESPONSIVE (both screens): THERE ARE NO BREAKPOINTS. Not one media query touches either
screen. The card is a fixed 380px on any viewport wider than roughly 408px and shrinks to
fill below that, held off the edges by the shell's 14px padding; the internal 32px 28px
padding is identical at 1440px and at 390px. The only viewport-reactive rule that reaches
these screens is the toast's max-width. On a short viewport the shell grows and the page
scrolls rather than clipping, so the card stops being optically centred — that is
as-shipped behaviour, keep it.

PRINT: the card prints exactly as it renders; only the toast stack disappears.


=== SCREEN 1 — Sign in (route /dang-nhap) ===

Card contents, top to bottom: brand block, then (conditionally) the error block, then a form
of two fields, an options row and the submit button.

BRAND BLOCK COPY: mark "PM"; heading "PlatformManager"; sub-line "Đăng nhập để tiếp tục".

FIELD 1 — label "Email", leading envelope glyph, placeholder "ten@congty.vn". Reproduce this
exactly: the field is labelled "Email" and hints at an email address, but it is a plain text
field that actually accepts a USERNAME such as "SuperAdmin". This mismatch ships. Do not
relabel it to "Tài khoản", do not change the placeholder, do not make it an email input type.

FIELD 2 — label "Mật khẩu", leading lock glyph, placeholder "Nhập mật khẩu", masked. It
carries a trailing visibility toggle: a borderless transparent button at right 10px, #57647a,
4px padding, 6px radius, holding an eye glyph while the password is masked and a
crossed-out-eye glyph while it is revealed; hovering turns it #152033 on an #eef2f8 tint. Its
accessible label is "Hiện mật khẩu" while masked and "Ẩn mật khẩu" while revealed.

OPTIONS ROW — one row, space-between, 12px text, 20px bottom margin:
  - left: a native square checkbox with a 6px gap before the label "Ghi nhớ đăng nhập". The
    checkbox is INERT — it is wired to nothing and changes nothing. Draw it unchecked.
  - right: the link "Quên mật khẩu?" in #0f5bd7 at weight 700, no underline until hover. It
    leads nowhere — there is no password-reset screen in the product.

SUBMIT — full-width primary button whose contents are a sign-in arrow glyph followed by the
label "Đăng nhập". While a request is in flight the label swaps to "Đang đăng nhập…" and the
button is disabled at 50% opacity — the glyph stays, and NO spinner appears.

SIGN-IN COPY (verbatim):
- Browser tab title: "PlatformManager" (the route's own title is never displayed, because
  there is no top bar to display it in).
- Brand mark "PM"; heading "PlatformManager"; sub-line "Đăng nhập để tiếp tục".
- "Email", "ten@congty.vn", "Mật khẩu", "Nhập mật khẩu".
- "Hiện mật khẩu", "Ẩn mật khẩu" (accessible labels on the visibility toggle).
- "Ghi nhớ đăng nhập", "Quên mật khẩu?".
- "Đăng nhập" (idle), "Đang đăng nhập…" (submitting).
- Error block text is one of: "Vui lòng nhập đầy đủ tài khoản và mật khẩu." (client check);
  the server's own sentence verbatim, e.g. "Bạn thao tác quá nhanh. Vui lòng thử lại sau 47
  giây."; or the fallback "Đăng nhập thất bại — thử lại sau."
- Toast text repeats the server's sentence, or falls back to one of: "Không thể kết nối tới
  máy chủ. Kiểm tra kết nối mạng." / "Bạn cần đăng nhập để tiếp tục." / "Bạn không có quyền
  thực hiện thao tác này." / "Không tìm thấy dữ liệu yêu cầu." / "Bạn thao tác quá nhanh.
  Vui lòng chờ một lát rồi thử lại." / "Đã có lỗi xảy ra. Vui lòng thử lại."

SIGN-IN STATES:
- Idle (default, and the state in the reference screenshot): both fields empty showing their
  placeholders, password masked with the eye glyph, checkbox unchecked, button enabled
  reading "Đăng nhập", no error block in the DOM at all.
- Idle with a deep link: rendering is IDENTICAL. When the user was bounced here from a
  protected page the target is carried only in the address bar as a query parameter — nothing
  on the card acknowledges it. Do not add a "sign in to continue to …" line.
- Submitting: only the button changes — disabled, 50% opacity, label "Đang đăng nhập…". Both
  inputs, the visibility toggle, the checkbox and the link all stay enabled and editable for
  the whole round trip. No spinner, no overlay, no progress bar anywhere.
- Error: the SAME sentence appears TWICE — once in the inline error block at the top of the
  card and once in a bottom-right toast. Reproduce both. Invalid credentials, a locked-out
  account and a rate-limited IP all render identically; only the sentence differs. There is
  no countdown, no cooldown and no distinct severity styling for any of them.
- Client validation: writes its message into the same single error block the server uses.
  There is exactly ONE message slot on the card and no field-level feedback of any kind.
  Empty fields are additionally blocked by the browser's own native "required" bubble.
- Success: nothing is rendered — no toast, no checkmark, no transition state. The screen is
  simply replaced by the next route.


=== SCREEN 2 — Change password (route /doi-mat-khau) ===

Structurally identical to Sign in — same shell, same card, same brand block, same field
recipe, same error block, same full-width submit — with different contents. It is the taller
of the two cards because it has three fields.

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
NONE of the three has a visibility toggle — unlike Sign in. Do not add one.

SUBMIT — full-width primary button reading "Đổi mật khẩu", TEXT ONLY with NO leading glyph
(unlike Sign in's). While submitting, the label swaps to "Đang lưu…" and the button is
disabled at 50% opacity. No spinner.

ABSENT BY CONSTRUCTION — do not add any of these: no options row, no remember-me checkbox,
no forgot-password link, no password-strength meter, no rules hint listing the 8-character
minimum the screen actually enforces, no "skip for now" or "do this later" control, no sign
out, and no link of any kind. In the forced case this card is the only screen the user can
reach, and it offers no way off it.

CHANGE-PASSWORD COPY (verbatim):
- Browser tab title: "PlatformManager".
- Brand mark "PM"; heading "Đổi mật khẩu"; sub-line "Bạn cần đổi mật khẩu trước khi tiếp tục
  sử dụng hệ thống." (forced) or "Đổi mật khẩu tài khoản của bạn." (voluntary).
- "Mật khẩu hiện tại", "Nhập mật khẩu hiện tại".
- "Mật khẩu mới", "Nhập mật khẩu mới".
- "Xác nhận mật khẩu mới", "Nhập lại mật khẩu mới".
- "Đổi mật khẩu" (idle), "Đang lưu…" (submitting).
- Error block text is one of: "Vui lòng nhập đầy đủ các trường." / "Mật khẩu mới phải có ít
  nhất 8 ký tự." / "Xác nhận mật khẩu mới không khớp." / the server's own sentence verbatim /
  the fallback "Đổi mật khẩu thất bại — thử lại sau."
- Toast fallbacks are the same six sentences listed under Sign in.

CHANGE-PASSWORD STATES:
- Idle forced (default arrival, and the reference screenshot): three empty masked fields,
  submit enabled, no error block, the forced sub-line showing. Nothing on the card explains
  who imposed the requirement or offers a way out.
- Idle voluntary: identical except for the shorter sub-line.
- Submitting: only the button changes — disabled, 50% opacity, label "Đang lưu…". All three
  fields stay editable. No spinner.
- Validation: three checks run in order on submit only — all fields present, then minimum
  length, then confirmation match — and they short-circuit, so only the FIRST failure is ever
  shown, in the same single error block the server shares. Nothing validates on blur or while
  typing. The 8-character rule is enforced but never stated anywhere on the card.
- Error: the same sentence appears twice again — inline error block plus bottom-right toast —
  except for the purely client-side validation failures, which produce the inline block ONLY
  (no request leaves the browser, so no toast).
- Success: nothing is rendered — no toast, no confirmation, no checkmark. The screen is
  simply replaced by the dashboard.


=== DO NOT ADD (to either screen) ===
- No sidebar, top bar, header, footer, breadcrumb or navigation of any kind.
- No split-screen marketing panel, hero image, background photograph, gradient, pattern or
  illustration. The page is one flat #eef2f8 field.
- No logo image — the "PM" mark is text in a rounded square, and the app ships no logo file.
- No social sign-in buttons, no "create an account" link, no language switcher, no support or
  version footer, no copyright line.
- No per-field validation styling, no aria-invalid state, no helper text, no strength meter,
  no character counter, no requirements checklist.
- No spinner, skeleton or progress indicator — only the button's disabled state and its
  swapped label.
- No dark mode and no theme toggle.
- No breakpoint-specific layout: do not shrink the card padding or restack anything on
  mobile.

Match the attached screenshots pixel-for-pixel wherever they conflict with this text.
```

## Google Stitch

1. Lint `DESIGN.md`, then import it into the Stitch project (Design → import design.md) so the palette, type scale, radii and spacing land as Stitch design tokens:

```bash
npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md
```

Result verified 2026-08-22: **0 errors, 6 warnings** — none of the six is a real defect (2 linter false positives on alpha-composited tints, 4 border-colour tokens the design.md schema has no slot for). See `DESIGN.md` § Colors. The bare `npx @google/design.md lint` form fails silently on Windows — always use the `--package=…designmd` form.

2. Paste the Master Prompt above verbatim. **Keep the literal values in it even after the import** — a silently failed import otherwise produces an off-palette card with no warning.

3. Add this Stitch-specific preamble above the pasted prompt, because Stitch's defaults for a login screen are exactly what these screens are not:

```
Generate TWO desktop screens at 1440×900. Each is a single centred 380px white card on a
flat #eef2f8 page — a bare authentication layout with NO navigation chrome of any kind, NO
split-screen marketing panel, NO background image or gradient, NO illustration, NO social
sign-in buttons and NO sign-up link. Compact density: 12px labels and inputs, 32px 28px
card padding. Every string is Vietnamese and is supplied verbatim below; do not invent,
translate or paraphrase any of it.
```

This repo has **no Stitch MCP configured** — import and generate manually at stitch.withgoogle.com (see `doc/Design/SETUP.md` if you want to automate it). Log whatever comes back in `Exports/`.

## Claude Design

Paste the Master Prompt above, attach both `Assets/Screenshots/auth/sign-in--desktop-1440.png` and `Assets/Screenshots/auth/change-password--forced--desktop-1440.png`, and prepend the token block below. There are **no brand image assets** to attach — the app ships no logo file; the "PM" mark is a text square (`UiInventory.md` § Brand Assets).

Every right-hand side below is a resolved literal — nothing here needs interpolation. Property names deliberately match the shipped custom properties in `src/FE/src/styles.scss`, so a generated stylesheet maps back to the app 1:1:

```css
:root {
  /* colors — src/FE/src/styles.scss :root */
  --bg: #eef2f8;
  --card: #ffffff;
  --text: #152033;
  --muted: #57647a;
  --line: #dfe6ef;
  --border-strong: #7e91b4;
  --brand: #0f5bd7;
  --brand2: #174ca8;
  --on-primary: #ffffff;
  --bad: #a02b2b;
  --bad-bg: #fbdcdc;
  --bad-border: #e5a8a8;
  --good: #0e7050;   /* toast accent only */
  --warn: #965e08;   /* toast accent only */
  /* elevation */
  --shadow: 0 4px 16px rgba(23, 39, 67, 0.1), 0 1px 3px rgba(23, 39, 67, 0.06);
  --shadow-primary-hover: 0 8px 20px rgba(15, 91, 215, 0.35);
  --shadow-focus-ring: 0 0 0 3px rgba(15, 91, 215, 0.12); /* auth inputs only */
  /* typography — Inter is loaded from Google Fonts at weights 400/500/600/700 */
  --font-family-base: Inter, 'Segoe UI', Arial, sans-serif;
  --fs-sm: 12px;
  --fs-base: 13px;
  --fs-md: 14px;
  --fs-lg: 15px;
  /* spacing + radius */
  --sp-2: 6px;
  --sp-3: 8px;
  --sp-4: 10px;
  --sp-5: 14px;
  --radius-sm: 7px;
  --radius-md: 9px;
  --radius-lg: 16px;
}
```

Off-scale literals these screens genuinely ship, to reproduce rather than round: card `max-width: 380px` and `padding: 32px 28px`; brand mark `44px × 44px` at `border-radius: 12px` with its text at `15px/800`; card heading `18px/800`; brand block `margin-bottom: 24px`; input `padding: 10px 12px 10px 36px`; field glyph `font-size: 15px` at `left: 12px`; visibility toggle `right: 10px`, `padding: 4px`, `border-radius: 6px`; options row `margin-bottom: 20px` with a `6px` checkbox gap; submit button `padding: 11px` with an `8px` icon gap; error block `gap: 8px`.

Ask for both artboards side by side — they share every measurement except their contents, and reviewing them together is how the intentional differences (the visibility toggle, the submit-button glyph) stay intentional.

## Google AI Studio

**System instruction** — paste as-is:

```
You reproduce an existing shipped web UI exactly as it is, not as it should be. This is
PlatformManager, an internal Vietnamese-language console built in Angular 20 with PrimeNG
and PrimeIcons v7. You are drawing its two authentication screens.

Hard rules:
1. NEITHER SCREEN HAS AN APP SHELL. No sidebar, no top bar, no header, no footer, no
   navigation. The viewport is one flat #eef2f8 field with a single white card centred on
   both axes. Never add a split-screen marketing panel, a hero image, a gradient, an
   illustration, social sign-in buttons, a sign-up link, a language switcher or a copyright
   line. This is the single most common way to get these screens wrong.
2. Every visible string is Vietnamese and is given to you verbatim. Reproduce each one
   character for character, including the "…" ellipsis and the "—" em dash. Do not translate,
   correct, shorten or normalise any of it. In particular, the sign-in field labelled "Email"
   with the placeholder "ten@congty.vn" actually accepts a username — keep the label and the
   placeholder exactly as given.
3. Use only these literal values. Colors: #eef2f8 page, #ffffff card, #dfe6ef card border,
   #152033 text, #57647a muted text, #7e91b4 input border, #0f5bd7 brand, #174ca8 brand
   hover, #ffffff on brand, #a02b2b error text on #fbdcdc fill with a #e5a8a8 border,
   rgba(15,91,215,0.12) focus ring. Shadows: card 0 4px 16px rgba(23,39,67,0.1), 0 1px 3px
   rgba(23,39,67,0.06); primary hover 0 8px 20px rgba(15,91,215,0.35). Font: Inter with
   "Segoe UI", Arial, sans-serif fallbacks — card heading 18px/800, brand mark 15px/800,
   labels 12px/700, inputs and sub-line 12px, submit button 14px/700. Radii: 16px card, 12px
   brand mark, 7px inputs and error block and submit button, 6px visibility toggle, 9px
   toast. Sizes: card max-width 380px, card padding 32px 28px, brand mark 44px square, input
   padding 10px 12px 10px 36px, submit padding 11px, field bottom margin 10px, brand block
   bottom margin 24px, options row bottom margin 20px, shell padding 14px.
4. There are NO breakpoints on either screen. The card is 380px wide at every viewport above
   ~408px and its internal padding never changes.
5. There is exactly ONE error message slot per card — a red block above the form. There is NO
   per-field validation styling of any kind: no red borders, no helper text, no strength
   meter, no requirements checklist, no character counter.
6. There is no spinner and no progress indicator. The only submitting affordance is the
   submit button going 50% opaque with its label swapped.
7. Do not idealize: keep the inert remember-me checkbox, keep the dead "Quên mật khẩu?" link,
   keep the missing visibility toggle on the change-password screen, keep the missing icon on
   its submit button, and keep the fact that a failed request shows the same sentence twice
   (inline block plus bottom-right toast).
```

**User prompt** = the SHARED FRAME + SCREEN 1 + SCREEN 2 + DO NOT ADD sections of the Master Prompt above, pasted verbatim. To generate a single screen, paste SHARED FRAME + that screen's block + DO NOT ADD.

**Image parts** to attach: `Assets/Screenshots/auth/sign-in--desktop-1440.png` and `Assets/Screenshots/auth/change-password--forced--desktop-1440.png`. Tell the model in the user turn: "The two attached captures are the real screens in their idle states — treat them as the authority wherever they disagree with my text."

## Generic

For any other generator (v0, Bolt, Lovable, Figma AI, an internal tool): paste the Master Prompt block verbatim and attach both screenshots. Nothing in the Master Prompt depends on this repository — every value is already a literal and every string is already verbatim.

Three guardrails worth repeating in the tool's own chat after the first generation, because generators reintroduce them by habit:

```
1. Delete the navigation chrome you added — these screens have no sidebar, no top bar, no
   header and no footer. One centred card on an empty #eef2f8 page, nothing else.
2. Delete the marketing/illustration half of the layout, the social sign-in buttons and the
   sign-up link. None of them exist in this product.
3. Restore the exact Vietnamese strings I gave you, including the field labelled "Email"
   with the placeholder "ten@congty.vn" — it is a username field and the mismatch ships.
```

## Assets to Attach

<!-- Explicit file list — everything a tool needs beyond the prompt text. -->

- `Assets/Screenshots/auth/sign-in--desktop-1440.png` — Sign in, idle state, 1440px desktop.
- `Assets/Screenshots/auth/change-password--forced--desktop-1440.png` — Change password in the **forced** state (the two-line sub-line), 1440px desktop.
- `Tokens/tokens.json` (W3C DTCG — enable `global` + `light`; `dark` is intentionally empty, no dark mode ships).
- `DESIGN.md` (lint-clean token dictionary + design guidance, for the Stitch import).
- `Assets/Brand/` — **none**. The app ships no logo or brand image file; the "PM" mark is a text square (`UiInventory.md` § Brand Assets).

## Known gaps in this pack

- **The auth shell, the auth card, the brand block, the error block, the visibility toggle and the options row are documented as components** (`Components/AuthCard.md`, `Components/AuthField.md`, both indexed in `COMPONENTS.md`), and the submit button is `Components/Button.md`'s block modifier. They are described here from those specs plus `src/FE/src/styles.scss` and the two page templates.
- **`Screens/05-auth.md` still annotates most of this flow as "plain markup, not a documented component"** — the auth shell, the auth card, the brand block, the error block, the fields, the visibility toggle, the options row and the toast stack. That is stale: `AuthCard`, `AuthField` and `Toast` are all indexed in `COMPONENTS.md` as of the 2026-08-22 component pass. This pack follows the component index. Flagged, deliberately not edited by this pass.
- **Only the two idle-state screenshots exist.** The error, rate-limited, locked-out, return-URL, voluntary-subtitle and 390px mobile states are uncaptured; the prompt describes them from `Screens/05-auth.md`. Capture on demand and add the file here at that point — every one of them needs a live backend except the sign-in mobile shot.
- ~~The screenshot tables inside `Screens/05-auth.md` still list every row as `pending`~~ — **reconciled 2026-08-22.** Both existing files are now marked `captured` there, and the uncaptured variants read `on demand` rather than `pending`, per the one-desktop-shot-per-screen policy.
