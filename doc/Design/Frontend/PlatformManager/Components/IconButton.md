---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "IconButton"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
  - "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html"
  - "src/FE/src/app/shared/components/sidebar/sidebar.html"
  - "src/FE/src/app/shared/components/sidebar/sidebar.scss"
  - "src/FE/src/app/shared/components/toast/toast.html"
  - "src/FE/src/app/shared/components/toolbar/toolbar.html"
---

# IconButton
**Description:** The app's single icon-only control (`.icon-btn`) — a **ghost** square: transparent border, transparent fill, muted glyph, and a background that only appears on hover. It carries no label; the icon plus `title`/`aria-label` is the whole button. Two colour modifiers exist, `.primary` (brand blue — neutral or constructive actions) and `.danger` (red — destructive actions), and both stay ghost: only the glyph is tinted, never the resting fill.

> **This component exists because of a merge, and that is the point of the spec.** The redesign of **2026-08-29** collapsed **five** separate icon-button implementations into one class. The source comment naming them sits above `styles.scss` § `.icon-btn` (`grep -n 'GHOST' src/FE/src/styles.scss`): `.action-btn` (declared **twice**, with conflicting values), `.cell-icon-btn` (which no template ever used), `.sidebar-toggle`, `.toast-close` and `.toggle-visibility`. Between them the app shipped **four sizes, three radii and two border treatments for the same object**. `Components/ActionButton.md` and `Components/CellIconButton.md` documented two of those five and were deleted on the same date; the two Angular components that redeclared their own copies (`sidebar.scss`, `toast.scss`) now keep only their genuinely local rules and say so in a comment (`sidebar.scss:55-57`, `toast.scss:2`).

## Anatomy

A bare `<button type="button">` holding one `<i class="pi …">` glyph, with an accessible name supplied by `aria-label` or `title` — never by visible text.

`display:inline-flex` centred both axes · `dimension.icon-button` square (24×24) · `padding:0` · `flex:none` · `border:1px solid transparent` · `background:transparent` · radius `radius.sm` · glyph `12px` with `line-height:1` · colour `colors.muted` · `cursor:pointer` · `transition: background / color` at `duration.fast`.

The transparent 1px border is load-bearing: it reserves the same box as a bordered control so the button does not shift by 2px when a sibling `.btn` sits next to it in a flex row.

**Ghost is a deliberate decision, recorded in the source** (the comment above `styles.scss` § `.icon-btn`): a long data grid where every row carries two filled buttons becomes unreadable — the eye stops seeing the data and starts seeing a column of coloured plates. Colour therefore lives in the *glyph*, not in a resting fill, and the fill appears only under the pointer.

Three contexts adjust the box without redeclaring the component:

| Context | Adjustment | Source |
| --- | --- | --- |
| Inside a `Toolbar` filter chip | 16×16, glyph `10px`, colour `colors.tonal-ink`; hover inverts to `colors.brand` fill + `colors.on-primary` glyph instead of the system grey — the button sits **on** a tonal chip, where grey reads as a render fault | `styles.scss` § `.filter-chip .icon-btn` (+ `:hover`) |
| Inside an `.input-icon` field (password reveal) | `position:absolute; right:10px`, and the field gains `padding-right:40px` so text stops before the button | `styles.scss` § `.input-icon .icon-btn` and § `.input-icon:has(.icon-btn) input` |
| Dialog close (`.dialog-close`) | `margin-left:auto; flex:none` — pushes the × to the end of `.dialog-head` | `styles.scss` § `.dialog-close` |
| Sidebar collapse (`.sidebar-toggle`) | `margin-left:auto` plus a `transform: rotate(180deg)` on the glyph while collapsed — shape comes entirely from `.icon-btn` | `sidebar.scss:58-68` |

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Neutral (base) | `icon-btn` | Glyph `colors.muted`; hover fill `colors.surface-2`, glyph `colors.text` | Chrome and dismiss actions that carry no consequence: dialog ×, toast ×, sidebar collapse, password reveal, filter-chip remove |
| Primary | `icon-btn primary` | Glyph `colors.brand`; hover fill `colors.tonal-bg`, glyph `colors.tonal-ink` | Edit, and unlock — neutral or constructive row actions |
| Danger | `icon-btn danger` | Glyph `colors.bad`; hover fill `colors.bad-bg`, glyph stays `colors.bad` | Destructive or restrictive row actions — lock an account is the only shipped case |

The two modifiers ship **on the same button at once, mutually exclusive by binding**: the user grid's lock toggle is `[class.danger]="!row.IsLocked"` / `[class.primary]="row.IsLocked"`, so the same control is red while the account is open (the click will lock it) and blue while it is locked (the click will release it) — `user-grid-table.html:77-87`.

There is **no size modifier**. The 16×16 chip variant is a contextual override under `.filter-chip`, not a class anyone may apply directly.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | 24×24 ghost square: transparent fill and border, glyph `colors.muted` (or `colors.brand` / `colors.bad` with a modifier), radius `radius.sm` |
| hover | Fill appears: `colors.surface-2` + glyph `colors.text` on the base; `colors.tonal-bg` + `colors.tonal-ink` on `.primary`; `colors.bad-bg` with the glyph held at `colors.bad` on `.danger`. Transitioned over `duration.fast` (`styles.scss` § `.icon-btn:hover`, `.icon-btn.primary:hover`, `.icon-btn.danger:hover`) |
| focus-visible | `outline: 2px solid colors.brand` at `outline-offset: 1px` — a **tighter offset than every other control** (`.btn` and `.check` both use 2px) so the ring stays inside a table cell (`styles.scss` § `.icon-btn:focus-visible`) |
| active / selected | **No `:active` rule is authored.** `.btn` ships `transform: translateY(1px)` on press (`styles.scss` § `.btn:active`); `.icon-btn` does not, so the press produces no distinct feedback beyond the hover fill it already has. Recorded in Normalize #1. There is no *selected* sense either — the button is never a toggle that stays on |
| disabled | `opacity:.45`, `cursor:not-allowed`, and fill/colour explicitly pinned back to transparent + `colors.muted` so a modifier's tint does not survive into the disabled look (`styles.scss` § `.icon-btn:disabled`). Genuinely reachable: `[disabled]="isSelfLock(row)"` locks out the signed-in user's own lock button (`user-grid-table.html:82`) |

## Tokens Used
- `colors.muted` (resting glyph), `colors.text` (hover glyph), `colors.surface-2` (hover fill)
- `colors.brand` (`.primary` glyph + focus ring), `colors.tonal-bg` / `colors.tonal-ink` (`.primary` hover)
- `colors.bad` (`.danger` glyph), `colors.bad-bg` (`.danger` hover fill)
- `colors.on-primary` (glyph on the filter-chip hover inversion)
- `radius.sm`; `dimension.icon-button` (24px); `dimension.checkbox` (16px) is numerically the chip-variant size but is a different token — the chip size ships as a literal
- `duration.fast` (both transitions)
- Un-tokenised literals: glyph `12px` and `10px`, `line-height:1`, `opacity:.45`, `outline-offset:1px`, the chip `16px` box, `right:10px` and `padding-right:40px` on the field variant

## Reference markup

```html
<!-- row actions: two colours, one ghost shape -->
<div class="row-actions">
  <button
    type="button"
    class="icon-btn primary"
    [attr.title]="'quan-tri-nguoi-dung.action.edit' | translate"
    (click)="editRow.emit(row)"
  >
    <i class="pi pi-pencil"></i>
  </button>
  <button
    type="button"
    class="icon-btn"
    [class.danger]="!row.IsLocked"
    [class.primary]="row.IsLocked"
    [disabled]="isSelfLock(row)"
    [title]="lockButtonTitleKey(row) | translate"
    (click)="toggleLock.emit(row)"
  >
    <i class="pi" [class.pi-lock]="!row.IsLocked" [class.pi-lock-open]="row.IsLocked"></i>
  </button>
</div>
```

```scss
.icon-btn {
  border: 1px solid transparent;
  background: transparent;
  border-radius: var(--radius-sm);
  width: 24px;
  height: 24px;
  padding: 0;
  flex: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  line-height: 1;
  color: var(--muted);
  cursor: pointer;
  transition: background .15s ease, color .15s ease;
}
.icon-btn:hover { background: var(--surface-2); color: var(--text); }
.icon-btn:focus-visible { outline: 2px solid var(--brand); outline-offset: 1px; }
.icon-btn:disabled { opacity: .45; cursor: not-allowed; background: transparent; color: var(--muted); }
.icon-btn.primary { color: var(--brand); }
.icon-btn.primary:hover { background: var(--tonal-bg); color: var(--tonal-ink); }
.icon-btn.danger { color: var(--bad); }
.icon-btn.danger:hover { background: var(--bad-bg); color: var(--bad); }
```

Sources: `src/FE/src/styles.scss` § `.icon-btn` (the class, its comment and the `.primary` / `.danger` modifiers), § `.input-icon .icon-btn` + § `.input-icon:has(.icon-btn) input` (field variant), § `.filter-chip .icon-btn` (chip variant), § `.dialog-close`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:65-89`, `src/FE/src/app/platform/login/pages/login/login.page.html:52-61`, `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html:18-25`, `src/FE/src/app/shared/components/sidebar/sidebar.scss:55-68`, `src/FE/src/app/shared/components/toast/toast.html:13-20`, `src/FE/src/app/shared/components/toolbar/toolbar.html:46-53`

> 🔄 **SỬA 2026-09-06** — bản trước sai hai nhóm:
> 1. **Reference markup ghi `title="Sửa"` và `lockButtonTitle(row)`.** Cả hai đều đã đổi: nhãn đến từ `quan-tri-nguoi-dung.action.edit`, và hàm nay tên `lockButtonTitleKey()` — nó trả KHOÁ (`…grid.lockTitle` / `…grid.unlockTitle` / `…grid.selfLockTitle`) chứ không trả câu, vì `components/` là tầng dumb nên không được inject `TranslateService`.
> 2. **Tám neo `file:dòng` trỏ sai chỗ** sau khi lưới người dùng chuyển sang `<app-data-grid>` và các template được bọc i18n.

## Do / Don't

- ✅ Always give it an accessible name — `aria-label` where there is no tooltip need (`confirm-dialog.html:21`, `toolbar.html:49`), `title` where a sighted user also benefits (`user-grid-table.html:72`). Both come from a translation key, never from a literal. The glyph alone announces nothing.
- ✅ Use `.primary` for edit/unlock and `.danger` for lock/delete. The pairing is the whole colour rule in the app: two different actions in one row must differ by **colour**, not by weight or size.
- ✅ Keep it ghost. If a design needs a filled icon-only control, that is a new decision to record here, not a local override.
- ✅ Reach for `Button.md`'s `.btn.sm` instead when the action needs a **word**. `.btn.sm` (`styles.scss` § `.btn.sm`) is the labelled in-row button and absorbed the other half of the deleted `ActionButton`.
- ❌ Don't redeclare size, radius, border or hover inside a component's scoped SCSS. That is precisely what produced the five-way divergence cleaned up on 2026-08-29; keep only genuinely local rules (a margin, a glyph rotation) as `sidebar.scss` now does.
- ❌ Don't apply `.primary` and `.danger` together as static classes. They ship together only as mutually-exclusive bindings on a toggle.
- ❌ Don't use it as the confirm action of a `ConfirmDialog` or a form. Committing actions are labelled `.btn`s.

## Normalize on redesign
1. **No `:active` state.** Every other button family in the app gives press feedback (`.btn` nudges 1px); the icon button gives none, so on a touch device a tap that misses is indistinguishable from a tap that lands.
2. **`outline-offset` disagrees with the rest of the app** — 1px here, 2px on `.btn`, `.check` and `.seg-btn`. It was chosen to keep the ring inside a table cell, but the inconsistency is real and should be solved by a focus-ring token, not per-control tuning.
3. **The glyph size is a literal, twice** (`12px` base, `10px` in a chip) and neither is on the `fontSize.fs-*` scale.
4. **The `.danger` hover keeps the glyph at `colors.bad` on a `colors.bad-bg` fill**, while `.primary` shifts its glyph from `colors.brand` to `colors.tonal-ink`. Two modifiers, two different hover philosophies.
5. **24×24 is below the 44×44 pointer-target guidance** of WCAG 2.2 SC 2.5.8 (AAA) and below the 24×24 *minimum* of SC 2.5.8 (AA) only because the box is exactly 24 with zero padding — adjacent row actions have no spacing exemption to fall back on.
6. **The chip variant is an unnamed contextual override.** `.filter-chip .icon-btn` is a de-facto small size with no class of its own, so nothing stops a third context inventing a fourth size.
