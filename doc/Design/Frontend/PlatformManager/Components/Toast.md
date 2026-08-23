---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Toast"
sources:
  - "src/FE/src/app/shared/components/toast/toast.html"
  - "src/FE/src/app/shared/components/toast/toast.scss"
  - "src/FE/src/app/shared/components/toast/toast.ts"
  - "src/FE/src/app/shared/services/toast.service.ts"
  - "src/FE/src/app/app.html"
---

# Toast
**Description:** The app-wide transient notification stack (`<app-toast />`, `.toast-stack` + `.toast-item`) — a fixed bottom-right column of dismissible messages in four severities, fed by `ToastService` and auto-removed after 5 s. Mounted once at the root, **outside** the shell branch, so it overlays the authenticated screens and the two auth screens alike.

## Anatomy

`.toast-stack` — `position:fixed`, right/bottom `spacing.sp-5`, `z-index:60` (the highest layer in the app, above the sidebar drawer), column flex, gap `spacing.sp-3`, `max-width: spacing.toast-stack-max-width`. Carries `role="status"` + `aria-live="polite"` and `no-print`. It renders no box of its own — an empty queue is an invisible empty container.

`.toast-item` — row flex, `align-items:flex-start`, gap `spacing.sp-3`, fill `colors.card`, border 1px `colors.line`, **left border 4px** in the severity color, radius `rounded.md`, `colors.shadow`, padding `spacing.sp-3` `spacing.sp-4`, `typography.toast-text`, `colors.text`. Enters with the `toast-in` keyframe: 0.15s ease from `opacity:0; translateY(6px)` to `opacity:1; translateY(0)`. Two children:
- `.toast-text` — `flex:1`, `line-height:1.4`, the message string verbatim.
- `.toast-close` — `spacing.toast-close` square ghost button, `border:1px solid transparent`, transparent fill, radius `rounded.sm`, `colors.muted`, `flex:none`, containing `pi pi-times`, `aria-label="Đóng thông báo"`.

**State model** (`ToastService`, `providedIn: 'root'`): a signal-backed array of `{ Id, Severity, Text }`. `success()` / `error()` / `info()` / `warn()` push; every push schedules `dismiss(id)` after `AUTO_DISMISS_MS = 5000`; `dismiss()` filters by id. Ids come from a monotonic counter, so a re-shown message is a new item rather than a reused one. There is no cap on stack length and no de-duplication — three identical errors render three stacked items.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Success | `.toast-item.success` | left border `colors.good` | `ToastService.success(text)` — completed mutation (save, delete, lock/unlock, import) |
| Error | `.toast-item.error` | left border `colors.bad` | `ToastService.error(text)` — also the channel `httpErrorInterceptor` uses for generic API failures |
| Warning | `.toast-item.warn` | left border `colors.warn` | `ToastService.warn(text)` |
| Info | `.toast-item.info` | left border `colors.brand` | `ToastService.info(text)` |
| Unclassified | `.toast-item` alone | left border stays the base `colors.muted` | Not reachable from the public API — `Severity` is a closed union of the four above; the base color is the fallback the class list would land on if a fifth severity were added without a matching rule |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: `.toast-close`. The `.toast-item` itself is not clickable.

| State | Treatment |
| --- | --- |
| default | Item: `colors.card` fill, `colors.line` hairline, 4px severity left border, `rounded.md`, `colors.shadow`, entering via `toast-in`. Close button: transparent, `colors.muted`, `rounded.sm` |
| hover | Item: **not styled** — `toast.scss` authors no `.toast-item:hover` rule, and hovering does **not** pause the 5 s auto-dismiss timer. Close button: `background: colors.surface-2`, `color: colors.text` (`toast.scss:61-64`) |
| focus-visible | Item: **not applicable** — no `tabindex`, not focusable. Close button: `outline: 2px solid colors.brand`, `outline-offset: 2px` (`toast.scss:66-69`) |
| active / selected | **Not applicable** — a toast is not selectable and authors no `:active` rule; the close button has no press treatment (unlike `.btn`, which nudges 1px) |
| disabled | **Not applicable** — the close button is never disabled: `dismiss(id)` is a pure signal update with no async work, and `toast.scss` contains no `:disabled` rule |

**Exit is unstyled**: dismissal — whether by the timer or the button — removes the item from the signal array immediately, so it disappears with no reverse animation. Only the entrance is animated.

## Tokens Used
- `colors.card`, `colors.line`, `colors.text`, `colors.muted`, `colors.surface-2`, `colors.brand`, `colors.good`, `colors.warn`, `colors.bad`, `colors.shadow`
- `rounded.md` (item), `rounded.sm` (close button)
- `spacing.sp-3`, `spacing.sp-4`, `spacing.sp-5`, `spacing.toast-stack-max-width`, `spacing.toast-close`
- `typography.toast-text`
- Icons: PrimeIcons v7 — `pi-times`
- Motion: `0.15s ease` (`toast-in`) — no motion token exists; see `Tokens/spacing.md` § Motion
- Layer: `z-index:60`

## Reference markup

```html
<div class="toast-stack no-print" aria-live="polite" role="status">
  @for (toast of toasts(); track toast.Id) {
    <div class="toast-item" [class]="toast.Severity">
      <span class="toast-text">{{ toast.Text }}</span>
      <button type="button" class="toast-close" [attr.aria-label]="'Đóng thông báo'" (click)="dismiss(toast.Id)">
        <i class="pi pi-times"></i>
      </button>
    </div>
  }
</div>
```

```ts
export type ToastSeverity = 'success' | 'error' | 'info' | 'warn';
export interface IToastMessage { Id: number; Severity: ToastSeverity; Text: string; }
const AUTO_DISMISS_MS = 5000;
```

Sources: `src/FE/src/app/shared/components/toast/toast.html:1-15`, `src/FE/src/app/shared/components/toast/toast.scss:1-81`, `src/FE/src/app/shared/components/toast/toast.ts:11-18`, `src/FE/src/app/shared/services/toast.service.ts:3-11`, `:19-50`, `src/FE/src/app/app.html:14`

## Do / Don't

- ✅ Mount exactly one `<app-toast />`, at the root and outside the `@if (showShell())` branch (`app.html:14`) — that placement is what lets the login screen surface errors too.
- ✅ Carry severity on the item via `[class]="toast.Severity"`; the four class names are the union members themselves, so a new severity means adding both at once.
- ✅ Let `httpErrorInterceptor` own generic API error toasts — feature components that also toast the same failure would double up, since nothing de-duplicates.
- ✅ Keep the accent on the **left border only**; the surface stays `colors.card` in all four severities. That is what keeps a stack of mixed toasts readable.
- ❌ Don't tint the toast background with `*-bg` surfaces — that treatment belongs to `Badge` and the danger button, not here.
- ❌ Don't rely on toast text for anything the user must act on: it disappears after 5 s with no history and no pause-on-hover.

## Normalize on redesign
1. **The 5 s timer does not pause on hover or focus**, and there is no reduced-motion or "prefers longer timeouts" accommodation. A user reading a long error message can lose it mid-sentence (WCAG 2.2 SC 2.2.1 territory).
2. **No stack cap and no de-duplication.** A failing poll or a burst of validation errors can fill the column past the viewport; `.toast-stack` has no `max-height` and no overflow handling.
3. **Dismissal has no exit animation** while the entrance does — items vanish rather than fading, which reads as a glitch when several expire at once.
4. **`role="status"` sits on the container, not on the item.** Errors announced through a polite live region can be missed; an `error` severity arguably wants `role="alert"` (assertive) on the item.
5. **The 4px accent width is a bare literal** with no token, and it is the only 4px border in the app.
