---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "Toast"
sources:
  - "src/FE/src/app/shared/components/toast/toast.html"
  - "src/FE/src/app/shared/components/toast/toast.scss"
  - "src/FE/src/app/shared/components/toast/toast.ts"
  - "src/FE/src/app/core/toast/toast.service.ts"
  - "src/FE/src/app/app.html"
  - "src/FE/src/app/core/interceptors/http-error.interceptor.ts"
  - "src/FE/public/i18n/vi.json"
---

# Toast
**Description:** The app-wide transient notification stack (`<app-toast />`, `.toast-stack` + `.toast-item`) — a fixed bottom-right column of dismissible messages in four severities, fed by `ToastService` and auto-removed after 5 s. Mounted once at the root, **outside** the shell branch, so it overlays the authenticated screens and the two auth screens alike.

**Restructured 2026-08-29.** The previous item was a white box with a thin hairline and a 4px colour stripe — on the app's white card surfaces it was very nearly invisible. It now has three parts instead of one: a **coloured severity disc**, a **body that can carry a bold title above the message**, and a **close button that is a plain `.icon-btn`**. The colour is carried in two places at once (the left edge and the disc), and the shadow is deep enough to lift the item off the page.

## Anatomy

`.toast-stack` — `position:fixed`, right/bottom `spacing.sp-5`, `z-index:60` (the app's highest layer, above the sidebar drawer), column flex, gap `spacing.sp-3`, `max-width: spacing.toast-stack-max-width`. Carries `role="status"` + `aria-live="polite"` and `no-print`. It paints no box of its own — an empty queue is an invisible empty container.

`.toast-item` — row flex, `align-items:flex-start`, gap `spacing.sp-3`, fill `colors.card`, 1px `colors.line` border, a **5px left border** in the severity colour, radius `rounded.md`, padding `spacing.sp-4` `spacing.sp-5`, `typography.toast-text`, `colors.text`. Its shadow is `colors.shadow-toast` — a two-layer value deliberately deeper than `colors.shadow`, because a toast floats **on** the content while a card sits **in** it. It was promoted from a literal to a `:root` token on 2026-09-03 (gate G11); do not fold it back into `colors.shadow`. Enters with the `toast-in` keyframe: `0.18s cubic-bezier(.2,.9,.3,1)` from `opacity:0; translateY(6px)` to `opacity:1; translateY(0)`. Three children:

1. **`.toast-icon`** — a 22px disc (`rounded.pill`, `flex:none`, centred flex, 11px glyph) holding one `pi` glyph chosen by severity. Both measurements are literals with no entry in `Tokens/spacing.md` — the disc is new this pass and the token file has not caught up. It is `aria-hidden="true"` on purpose: it repeats exactly what the message text already says. Unclassified severity falls back to `colors.surface-2` on `colors.muted`, so a malformed item still has a valid shape.
2. **`.toast-body`** — `flex:1`, `min-width:0`, holding up to two blocks:
   - `.toast-title` — `display:block`, weight 800, `margin-bottom:2px` literal. **Rendered only when `Title` is set.**
   - `.toast-text` — `display:block`, `line-height:1.45`, `colors.muted`. When it is the **only** child (`:only-child`), it flips to `colors.text` — because with no title the message *is* the headline, and leaving the most important line as the dimmest one is backwards. Most shipped call sites take this branch.
3. **`.icon-btn.toast-close`** — a plain `IconButton` (`Components/IconButton.md`) holding `pi pi-times`, `[attr.aria-label]="'shared.toast.dismiss' | translate"`. **`.toast-close` has no rule of its own anywhere**; the class exists only to name the position. Size, radius, colour, hover fill, focus ring and disabled treatment all come from the global `.icon-btn`.

**State model** (`ToastService`, `providedIn: 'root'`): a signal-backed array of `{ Id, Severity, Title?, Text }`. `success()` / `error()` / `info()` / `warn()` push; every push schedules `dismiss(id)` after `AUTO_DISMISS_MS = 5000`; `dismiss()` filters by id. Ids come from a monotonic counter, so a re-shown message is a new item rather than a reused one. There is no cap on stack length and no de-duplication — three identical errors render three stacked items.

**`title` is the second parameter, not the first** (`success(text, title?)`), even though it renders *above* `text`. Putting it first would silently change the meaning of every existing `toast.error(message)` call: same type, clean build, wrong at runtime. An optional trailing parameter is the only way to add a title without editing every call site, and the source records that reasoning where the decision lives. `title` is left `undefined` rather than coerced to `''` when omitted, so an object comparison in a test still matches a title-less toast.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Success | `.toast-item.success` | left border `colors.good`; disc `colors.good-bg` on `colors.good`; glyph `pi-check` | `ToastService.success(text, title?)` — a completed mutation (save, lock/unlock) |
| Error | `.toast-item.error` | left border `colors.bad`; disc `colors.bad-bg` on `colors.bad`; glyph `pi-times` | `ToastService.error(text, title?)` — also the channel `httpErrorInterceptor` uses for API failures |
| Warning | `.toast-item.warn` | left border `colors.warn`; disc `colors.warn-bg` on `colors.warn`; glyph `pi-exclamation-triangle` | `ToastService.warn(text, title?)` |
| Info | `.toast-item.info` | left border `colors.brand`; disc `colors.tonal-bg` on `colors.tonal-ink`; glyph `pi-info-circle` | `ToastService.info(text, title?)` |
| Unclassified | `.toast-item` alone | left border stays `colors.muted`; disc `colors.surface-2` on `colors.muted` | Not reachable from the public API — `Severity` is a closed union of the four above. The base pair is the shape a fifth severity would land on if it were added without a matching rule |
| Message only | `.toast-body > .toast-text:only-child` | text promoted from `colors.muted` to `colors.text` | No `title` passed. **The majority of shipped call sites**, including every interceptor message that carries a server `message` |
| Title + message | `.toast-title` + `.toast-text` | title weight 800 in `colors.text`; message stays `colors.muted` | A `title` was passed. Two shipped sources, both in `httpErrorInterceptor`: the status-code fallback branch (`ApiErrorMessageService.titleFor`, which returns `undefined` as soon as the envelope carries a real sentence) and the session-expired 401 branch, which pairs `shared.httpError.endedTitle` with `shared.httpError.endedText` and uses `warn`, not `error` |

The severity → glyph map lives in the **display component**, not in `IToastMessage` (`toast.ts`). Choosing a picture is a presentation decision; a caller writing `toast.error(...)` does not need to know which PrimeIcons class exists, and should not have to change if the icon set does.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: `.icon-btn.toast-close`. The `.toast-item` itself is not clickable.

| State | Treatment |
| --- | --- |
| default | Item: `colors.card` fill, `colors.line` hairline, 5px severity left border, `rounded.md`, deep two-layer shadow, entering via `toast-in`. Close button: the `.icon-btn` default — transparent fill, 1px transparent border, `rounded.sm`, `colors.muted` |
| hover | Item: **not styled** — `toast.scss` authors no `.toast-item:hover`, and hovering does **not** pause the 5 s auto-dismiss timer. Close button: `background: colors.surface-2`, `color: colors.text`, inherited from `.icon-btn:hover` |
| focus-visible | Item: **not applicable** — no `tabindex`, not focusable. Close button: `outline: 2px solid colors.brand`, `outline-offset: 1px`, inherited from `.icon-btn:focus-visible` |
| active / selected | **Not applicable** — a toast is not selectable and no `:active` rule is authored for the item. `.icon-btn` has no press treatment either (unlike `.btn`, which nudges 1px) |
| disabled | **Not applicable in practice** — the close button is never disabled: `dismiss(id)` is a pure signal update with no async work and nothing binds `[disabled]`. The treatment nevertheless *exists* and is inherited (`opacity:.45`, `cursor:not-allowed`, hover suppressed), which it did not before the button became an `.icon-btn` |

**Exit is unstyled**: dismissal — whether by the timer or the button — removes the item from the signal array immediately, so it disappears with no reverse animation. Only the entrance is animated.

## Tokens Used
- `colors.card`, `colors.line`, `colors.text`, `colors.muted`, `colors.surface-2`, `colors.brand`, `colors.good`, `colors.good-bg`, `colors.warn`, `colors.warn-bg`, `colors.bad`, `colors.bad-bg`, `colors.tonal-bg`, `colors.tonal-ink`
- `rounded.md` (item), `rounded.pill` (severity disc), `rounded.sm` (close button, via `.icon-btn`)
- `spacing.sp-3`, `spacing.sp-4`, `spacing.sp-5`, `spacing.toast-stack-max-width`, `spacing.icon-button` (close button, via `.icon-btn`)
- `colors.shadow-toast` (the item's lift)
- `typography.toast-text`; the title's `800` and the disc glyph's `11px` are literals — 800 is a loaded face and the app's second emphasis weight, 11px equals `fontSize.fs-xs` numerically but is not written as the token
- Icons: PrimeIcons v7 — `pi-check`, `pi-times`, `pi-exclamation-triangle`, `pi-info-circle` (severity discs) and `pi-times` again (close button)
- Motion: `0.18s cubic-bezier(.2,.9,.3,1)` (`toast-in`) — no motion token exists; see `Tokens/spacing.md` § Motion
- Layer: `z-index:60`
- Un-tokenised: the 5px left border, the 22px severity disc and its 11px glyph, `.toast-title` `margin-bottom:2px`. The two-layer shadow **is** tokenised now (`colors.shadow-toast`) — it was on this list until 2026-09-03.

## Reference markup

```html
<div class="toast-stack no-print" aria-live="polite" role="status">
  @for (toast of toasts(); track toast.Id) {
    <div class="toast-item" [class]="toast.Severity">
      <span class="toast-icon" aria-hidden="true"><i class="pi" [class]="iconClass(toast.Severity)"></i></span>
      <span class="toast-body">
        @if (toast.Title) {
          <b class="toast-title">{{ toast.Title }}</b>
        }
        <span class="toast-text">{{ toast.Text }}</span>
      </span>
      <button type="button" class="icon-btn toast-close"
        [attr.aria-label]="'shared.toast.dismiss' | translate" (click)="dismiss(toast.Id)">
        <i class="pi pi-times"></i>
      </button>
    </div>
  }
</div>
```

```ts
export type ToastSeverity = 'success' | 'error' | 'info' | 'warn';
export interface IToastMessage { Id: number; Severity: ToastSeverity; Title?: string; Text: string; }
const AUTO_DISMISS_MS = 5000;
// title is the SECOND parameter, though it renders above the text
success(text: string, title?: string): void;
```

Sources: `src/FE/src/app/shared/components/toast/toast.html:1-23`, `src/FE/src/app/shared/components/toast/toast.scss:4-116` (the head comment at `:1-3` records that `.toast-close` keeps no rule of its own), `src/FE/src/app/shared/components/toast/toast.ts:11-16` (the severity → glyph map), `src/FE/src/app/core/toast/toast.service.ts:3-19` (the model, with `Title` optional, and `AUTO_DISMISS_MS`), `:38-52` (the four push methods, `title` last), `src/FE/src/app/core/interceptors/http-error.interceptor.ts:109-112` (the session-expired `warn` with a title), `:133-139` (the status-code fallback title), `src/FE/src/app/app.html:41`, `src/FE/src/styles.scss` § `.icon-btn` (which supplies the close button's five states) and § `--shadow-toast`

> 🔄 **SỬA 2026-09-06** — bản trước sai bốn chỗ:
> 1. `aria-label` của nút đóng ghi là chuỗi tiếng Việt khai cứng; nay là khoá `shared.toast.dismiss`.
> 2. Bóng đổ của `.toast-item` được tả là "two-layer literal" — nó đã thành token `--shadow-toast` ở `:root` ngày 2026-09-03. Bỏ khỏi cả danh sách un-tokenised lẫn Normalize #6.
> 3. Nguồn tiêu đề toast ghi là "the only shipped source"; nay có **hai** nhánh, và nhánh 401 dùng `warn` chứ không `error`.
> 4. Bốn neo `file:dòng` trỏ sai chỗ, đáng kể nhất là `app.html:24` (`<app-toast />` thật ở `:41`) và `toast.ts:10-15` (dòng 10 là `*/` của khối chú thích).

## Do / Don't

- ✅ Mount exactly one `<app-toast />`, at the root and outside the `@if (showShell())` branch (`app.html`) — that placement is what lets the login screen surface errors too.
- ✅ Carry severity on the item via `[class]="toast.Severity"`; the four class names are the union members themselves, so adding a severity means adding the type member, the rule and the glyph together.
- ✅ Pass a title only when it says something the message does not. The interceptor gets this right: `ApiErrorMessageService.titleFor` returns a status-code fallback title (`shared.httpError.offlineTitle`, `shared.httpError.forbiddenTitle`, …) **only** when the envelope carried no real sentence, and `undefined` otherwise — inventing a generic "Lỗi" above a specific sentence adds a line and no information.
- ✅ Leave the close button as a bare `.icon-btn`. It had its own size, radius and hover fill before 2026-08-29 and had drifted from every other icon button in the app; the fix was deleting rules, not writing more.
- ✅ Let `httpErrorInterceptor` own generic API error toasts — a feature component that also toasts the same failure doubles up, since nothing de-duplicates.
- ✅ Keep the accent split between the **left edge and the disc**; the surface stays `colors.card` in all four severities, which is what keeps a stack of mixed toasts readable.
- ❌ Don't tint the toast background with a `*-bg` surface. Those belong on the disc, on `Badge` and on the danger button — a fully tinted card is what makes a stack unreadable.
- ❌ Don't rely on toast text for anything the user must act on: it disappears after 5 s with no history and no pause-on-hover.
- ❌ Don't reintroduce a `.toast-close` rule. The class is a name for a position, nothing more.

## Normalize on redesign
1. **The 5 s timer does not pause on hover or focus**, and there is no reduced-motion or "prefers longer timeouts" accommodation. A user reading a two-line title-plus-message toast can lose it mid-sentence (WCAG 2.2 SC 2.2.1 territory) — and the restructure made items *longer*, so this got worse, not better.
2. **No stack cap and no de-duplication.** A failing poll or a burst of validation errors can fill the column past the viewport; `.toast-stack` has no `max-height` and no overflow handling.
3. **Dismissal has no exit animation** while the entrance does — items vanish rather than fading, which reads as a glitch when several expire at once.
4. **`role="status"` sits on the container, not on the item.** Errors announced through a polite live region can be missed; an `error` severity arguably wants `role="alert"` (assertive) on the item.
5. **`.toast-text` changes colour depending on whether it has a sibling.** `:only-child` is a clever fix for a real problem, but it means the same message renders in two different inks depending on a decision made at the call site — and nothing at the call site says so.
6. **Four un-tokenised values remain on the item**: the 5px left border (the app's only 5px border), the 22px severity disc, its 11px glyph, and the title's 2px bottom margin. The two-layer shadow left this list on 2026-09-03 when it became `--shadow-toast`.
7. **Three `Tokens/` rows are stale or missing after this restructure.** `spacing.toast-stack-max-width` still reads `min(360px, 90vw)` where the shipped value is `min(400px, 90vw)`; `typography.toast-text` still reads `line-height: 1.4` where the shipped value is `1.45`; and the severity disc has no row at all. All three are flagged to the Tokens owner rather than patched here — this spec cites the shipped values.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **The item was nearly invisible on white — resolved 2026-08-29.** A hairline box with a 4px stripe sitting on a `colors.card` page had almost no separation. Severity is now carried twice (edge **and** coloured disc) and the shadow was deepened, so both "there is a message" and "what kind of message" read before any text is.
2. **`.toast-close` was a fifth icon-button variant — resolved 2026-08-29.** It declared its own square size, radius, colour and hover fill, and had drifted from `.action-btn`, `.sidebar-toggle` and `.toggle-visibility`, which all did the same job differently. It is now a plain `.icon-btn`; the class kept its name and lost every rule. As a side effect it gained a `:disabled` treatment it never had.
3. **A toast could not be skimmed — resolved 2026-08-29.** Every message was one undifferentiated sentence. `Title` (optional, second parameter) adds a bold first line, and `httpErrorInterceptor` uses it for the fallback branch. Call sites that pass one string still render correctly, which is why the parameter had to go last.
