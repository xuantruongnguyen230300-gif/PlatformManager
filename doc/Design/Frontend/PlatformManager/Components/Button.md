---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Button"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/shared/components/topbar/topbar.html", "src/FE/src/app/platform/login/pages/login/login.page.html", "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html", "src/FE/src/app/modules/danh-muc-dti/components/confirm-dialog/confirm-dialog.html"]
---

# Button
**Description:** The labelled action trigger built on the global `.btn` class (`styles.scss:142-195`). It is a **fill-first tonal button**: no visible border, a pale brand tint as the default fill, and a full five-state treatment. It appears on all six routes — dialog footers, card titles, toolbars, the topbar and both auth forms.

## Anatomy
Single-line text label, optionally preceded by a PrimeIcons `<i class="pi pi-…">` glyph (see `Icons.md`). Rounded rectangle: `rounded.sm` radius, `spacing.button-padding`, `typography.button-label` (`fs-sm`/700), `cursor:pointer`. Border is `1px solid transparent` **by design** — `styles.scss:143-146` records that the transparent border preserves the box model so hover/focus colour changes never shift layout by 1px. No fixed width; sizes to its label. Transition: `background .15s`, `box-shadow .15s`, `transform .1s` (`styles.scss:154`).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Default (tonal secondary) | `btn` | bg `colors.tonal-bg`, text `colors.tonal-ink`, transparent border (`styles.scss:146-148`) | Every labelled secondary action: dialog `Đóng`/`Huỷ`/`Sao chép`, `Import CSV/Excel`, history-row `Xem`, topbar `Đăng xuất` |
| Primary | `btn primary` | bg + border-color `colors.brand`, text `colors.on-primary` (`styles.scss:156-159`) | The one primary action per context: `+ Thêm chỉ tiêu`, `+ Thêm người dùng`, `Xuất báo cáo`, `Lưu thay đổi`, `Lưu`, `In`, `Nhập dữ liệu`, `Đăng nhập`, `Đổi mật khẩu` |
| Danger | `btn danger` | bg `colors.bad-bg`, text `colors.bad` (`styles.scss:168-170`) | Destructive confirmation only — the confirm-dialog's confirm action (`confirm-dialog.html:8`, label bound to `confirmLabel()`, passed `"Xoá"` from `danh-muc-dti.page.html:71`) |
| Block modifier | `btn primary btn-block` | `width:100%`, `padding:11px`, `typography.button-block-label` (`fs-md`), centred flex, `gap:8px` (`styles.scss:599-607`) | Full-width form submit on the two auth screens only (`login.page.html:55`, `doi-mat-khau.page.html:58`) |
| Icon-only modifier | `btn sidebar-hamburger` | `.btn` base + screen-local geometry in `topbar.scss` | The mobile drawer trigger holding only `pi pi-bars` (`topbar.html:3-12`) |

**Not variants of `.btn`** — two separate ghost button families exist and must not be folded in here: `.action-btn` (`styles.scss:213-253`, in-row grid actions) and `.cell-icon-btn` (`criteria-grid-table.scss:57-99`, inline cell-edit confirm/cancel). Both are deliberately transparent-by-default so dense grids are not flooded with tonal fill (`styles.scss:197-212`). They are indexed separately in `COMPONENTS.md`.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg/text per variant above; `rounded.sm`; `spacing.button-padding`; `typography.button-label`; `border:1px solid transparent` |
| hover | Default → bg `colors.tonal-bg-hover` + `box-shadow: 0 3px 10px rgba(23,39,67,.1)` (`styles.scss:177-180`). Primary → bg + border-color `colors.brand2` + `box-shadow: 0 8px 20px rgba(15,91,215,.35)` (`styles.scss:161-165`). Danger → bg `colors.bad-bg-hover` (`styles.scss:172-174`) |
| focus | `outline: 2px solid colors.brand`, `outline-offset: 2px` — `:focus-visible` only, so keyboard focus shows the ring and mouse clicks do not (`styles.scss:182-185`) |
| active | `transform: translateY(1px)` (`styles.scss:187-189`) |
| disabled | `opacity: .5`, `cursor: not-allowed` (`styles.scss:191-194`). **Reachable and used** — `[disabled]` is bound on four `.btn`s: `login.page.html:55` and `doi-mat-khau.page.html:58` (`submitting()`), `phan-quyen.page.html:4` (`saving() \|\| loading()`), `csv-import-dialog.html:18` (`!selectedFile() \|\| importing()`). A fifth `[disabled]` binding exists on the permission matrix's checkboxes (`permission-matrix.html:25`), which is not a `.btn` |

## Tokens Used
- `colors.tonal-bg`, `colors.tonal-bg-hover`, `colors.tonal-ink`, `colors.brand`, `colors.brand2`, `colors.on-primary`, `colors.bad`, `colors.bad-bg`, `colors.bad-bg-hover`
- `rounded.sm`
- `spacing.button-padding`, `spacing.button-block-padding`
- `typography.button-label`, `typography.button-block-label`

Both hover shadows and the `translateY(1px)` press offset are **literal values in the source**, not tokens — there is no elevation or motion scale (see `Tokens/spacing.md`).

## Reference markup

```html
<!-- default tonal -->
<button type="button" class="btn" (click)="dialogEl.close()">Huỷ</button>

<!-- primary, with disabled binding -->
<button type="button" class="btn primary" [disabled]="saving() || loading()" (click)="onSave()">
  {{ saving() ? 'Đang lưu…' : 'Lưu thay đổi' }}
</button>

<!-- danger (destructive confirm) -->
<button type="button" class="btn danger" (click)="confirmed.emit(); dialogEl.close()">{{ confirmLabel() }}</button>

<!-- block modifier, icon + label -->
<button type="submit" class="btn primary btn-block" [disabled]="submitting()">
  <i class="pi pi-sign-in"></i> {{ submitting() ? 'Đang đăng nhập…' : 'Đăng nhập' }}
</button>
```

Sources: `src/FE/src/styles.scss:142-195` (base + variants + all five states), `src/FE/src/styles.scss:599-607` (`.btn-block`), `src/FE/src/app/shared/components/topbar/topbar.html:3-12,18-20`, `src/FE/src/app/platform/login/pages/login/login.page.html:55-57`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html:58-60`, `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:4-6`, `src/FE/src/app/modules/danh-muc-dti/components/confirm-dialog/confirm-dialog.html:7-8`, `src/FE/src/app/modules/danh-muc-dti/components/csv-import-dialog/csv-import-dialog.html:17-20`, `src/FE/src/app/modules/dashboard/components/history-list/history-list.html:14`

## Do / Don't

- ✅ One `btn primary` per context — the topbar, a card title bar and a dialog footer are each their own context and each carry one.
- ✅ Reach for `btn danger` only for genuinely destructive confirmation; the shipped app uses it exactly once, on the delete confirm.
- ✅ Keep the transparent border on every variant — dropping it makes hover/focus colour changes shift layout by 1px (`styles.scss:143-146`).
- ✅ Bind `[disabled]` for in-flight submits and pair it with a label that swaps to a progress phrase (`Đang lưu…`, `Đang đăng nhập…`) — that is the shipped pattern in all five disabled call sites.
- ❌ Don't give `.btn` a visible border or a grey fill — that reverts the deliberate fill-first decision at `styles.scss:21-27`.
- ❌ Don't use `.btn` for icon-only actions inside a data grid — use `.action-btn`/`.cell-icon-btn`, which stay transparent so long lists are not flooded with colour.
- ❌ Don't add a `.btn.success`/`.btn.warn` variant — only `primary` and `danger` ship.

## Normalize on redesign
1. `.btn.primary:hover` and `.btn:hover` shadows, and the `translateY(1px)` press offset, are literal values with no token behind them — there is no elevation or motion scale to reference (`Tokens/spacing.md`).
2. `.btn-block` is only ever combined with `primary`; a tonal or danger block button has no defined treatment should one be needed.
