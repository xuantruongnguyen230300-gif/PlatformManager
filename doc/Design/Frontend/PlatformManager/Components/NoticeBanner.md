---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
component: "NoticeBanner"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html"
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html"
---

# NoticeBanner
**Description:** The in-page message strip (`.notice`, `src/FE/src/styles.scss` § 5) — a bordered block with a left accent rule, a leading icon, a text run and an optional close button. The 2026-08-29 redesign turned it from a single informational treatment into a **four-severity** component sharing one structure.

> **Citation policy.** Values cite `src/FE/src/styles.scss` plus the **selector name**, not a line number.

## Anatomy

`.notice` is a row flex with `align-items: flex-start` and gap `spacing.sp-3`, so a multi-line message stays indented past its icon. Box: fill `colors.tonal-bg`, border 1px `colors.line`, **`border-left: 4px solid colors.brand`**, radius `rounded.md`, padding `spacing.notice-padding`, `typography.notice`, and `margin-bottom: spacing.sp-5` so it can sit directly above content without a wrapper.

The element itself is not fixed: the Dashboard ships a `<div class="notice">` wrapping a `<span>`, the permissions screen a `<p class="notice">` with the text as a direct child (`dashboard.page.html:19-26`, `phan-quyen.page.html:151-154`). Both render identically — the rule sets no `display` on its text child.

Three child contracts:

- **`> .pi`** — the leading glyph. `flex: none` and `margin-top: 2px` to sit optically level with the first line of text; ink follows the severity.
- **`a`** — inline links: ink follows the severity, weight 700, no underline at rest, underlined on hover.
- **`> .icon-btn`** — an optional dismiss button, pushed right by `margin-left: auto` with `flex: none`. Shape comes entirely from the shared icon-button rule.

A severity class changes exactly two things — the fill and the left rule — and recolours the glyph and the links to match. The border, radius, padding and rhythm never vary.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Information (default) | `notice` | fill `colors.tonal-bg`, left rule and glyph `colors.brand` | A neutral state explanation. Shipped with `pi-info-circle` on the Dashboard — the first-run and the awaiting-progress lines, each carrying an inline `<a>` to `/danh-muc/dti` — and on the resource tab of `/quan-tri/phan-quyen`, stating that the matrix does not yet control anything. Count them rather than trusting this cell: `grep -rn 'class="notice' src/FE/src/app` |
| Success | `notice ok` | fill `colors.good-bg`, left rule and glyph `colors.good` | A completed, persistent outcome worth keeping on screen. **No shipped call site today** |
| Warning | `notice warn` | fill `colors.warn-bg`, left rule and glyph `colors.warn` | A deadline or an incomplete condition the user can still act on. Shipped three times — the app-level "new version available" strip (`app.html:10`) and both write-conflict banners on `/quan-tri/phan-quyen` (`phan-quyen.page.html:76`, `:126`), each pairing the banner with a Reload `.btn` |
| Danger | `notice bad` | fill `colors.bad-bg`, left rule and glyph `colors.bad` | A page-level failure that removed the content behind it. Shipped on the Dashboard (`dashboard.page.html:9`) and on the user list (`quan-tri-nguoi-dung.page.html:69`), both with a Retry `.btn` and the grid gone rather than emptied |
| With dismiss | `notice` + trailing `.icon-btn` | The button is pushed right by `margin-left: auto` | Any severity that the user may close. **No shipped call site today** |

**A notice is for a persistent state, a toast is for a transient one.** Transient feedback goes through `Toast`; per-form validation goes through `.form-error` (`FormRow.md`) and, on the auth screens, `.login-error` (`AuthField.md`). The four severities do not change that split — they mean the *page* has something to say, at four levels of seriousness.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Per-severity fill and left rule; border 1px `colors.line`; `rounded.md`; `spacing.notice-padding`; `typography.notice`; `margin-bottom: spacing.sp-5` |
| hover | **Not applicable to the banner** — a static block with no `:hover` rule. Its link child underlines on hover; its optional `.icon-btn` fills `colors.surface-2` from the shared icon-button rule |
| focus | **Not applicable to the banner** — no `tabindex`, not focusable. A link child takes the browser default ring (see `Footer.md` § Normalize for the app-wide gap); the optional close button draws `outline: 2px solid colors.brand`, `outline-offset: 1px` |
| active | **Not applicable** — not interactive, no `:active` rule |
| disabled | **Not applicable** — not a form control |

**Visibility is the real state.** A notice is present or absent; it never dims, collapses or animates. Both shipped instances are rendered unconditionally — the permissions one only in the sense that it sits inside the resource tab's `@else` branch, so it appears with the tab rather than with a state change. The pattern the component is built for is a banner gated by the same signal that changes the page's behaviour, so the explanation and the changed behaviour can never disagree; neither instance does that yet, and the permissions template carries a comment naming the condition for deleting it (the first endpoint to carry `[RequirePermission]`, `phan-quyen.page.html:146-150`).

## Tokens Used
- `colors.tonal-bg`, `colors.good-bg`, `colors.warn-bg`, `colors.bad-bg` (fills); `colors.brand`, `colors.good`, `colors.warn`, `colors.bad` (left rule, glyph, links); `colors.line` (border); `colors.text` (body copy)
- `rounded.md`
- `spacing.notice-padding`, `spacing.sp-3` (gap), `spacing.sp-5` (bottom margin)
- `typography.notice`
- Icons: PrimeIcons v7 — `pi-info-circle` shipped; the prototype pairs `pi-check-circle` with `.ok`, `pi-exclamation-triangle` with `.warn` and `pi-exclamation-circle` with `.bad`

The `4px` left rule and the `margin-top: 2px` optical nudge on the glyph are literals with no token behind them.

## Reference markup

```html
<!-- shipped instance 1: Dashboard (/trang-chu), default severity, copy through i18n
     and an inline link rather than a button -->
<div class="notice">
  <i class="pi pi-info-circle"></i>
  <span
    >{{ 'dashboard.notice.firstRunLead' | translate
    }}<a routerLink="/danh-muc/dti">{{ 'dashboard.notice.catalogueLink' | translate }}</a
    >{{ 'dashboard.notice.firstRunTail' | translate }}</span
  >
</div>

<!-- shipped instance 2: /quan-tri/phan-quyen, resource tab. Same severity, <p> instead of
     <div>, and the glyph is aria-hidden because the sentence already carries the meaning. -->
<p class="notice">
  <i class="pi pi-info-circle" aria-hidden="true"></i>
  Hiện chưa có API nghiệp vụ nào áp dụng các quyền này, nên thay đổi ở đây được lưu lại
  nhưng chưa chặn hay mở thêm thao tác nào cho người dùng.
</p>

<!-- library shape: severity + inline link + dismiss -->
<div class="notice warn">
  <i class="pi pi-exclamation-triangle"></i>
  <span>… <a href="#">Xem danh sách còn thiếu</a>.</span>
  <button type="button" class="icon-btn" aria-label="Đóng thông báo"><i class="pi pi-times"></i></button>
</div>
```

Copy — **mọi chỗ đều đi qua i18n** (đối chiếu 2026-09-06). Có **6** chỗ dùng, không phải 2:

| Call site | Localization key | Ghi chú |
| --- | --- | --- |
| `app.html:10` | `shared.app.newVersion` | Dải "đã có phiên bản mới", kèm nút `shared.action.reload` |
| `dashboard.page.html:19` | `dashboard.notice.firstRunLead` + `.catalogueLink` + `.firstRunTail` | Tổng quan DTI — năm đang chọn chưa có chỉ tiêu nào. Ba khoá vì câu có liên kết chèn giữa |
| `phan-quyen.page.html:151` | `phan-quyen.hint.noEnforcement` | Tab tài nguyên — quyền lưu được nhưng chưa chặn gì |
| `phan-quyen.page.html:76` | **khoá động** `messageKey` | Xung đột phiên bản, tab màn hình |
| `phan-quyen.page.html:126` | **khoá động** `messageKey` | Xung đột phiên bản, tab tài nguyên |
| `quan-tri-nguoi-dung.page.html:69` | **khoá động** `messageKey` | Nạp danh sách hỏng |

Ba chỗ cuối nhận **khoá** từ component chứ không phải câu — component giữ mã lỗi rồi dịch lại
mỗi lần vẽ, nên câu đổi theo ngôn ngữ. Vì vậy cột "Verbatim copy" không áp dụng cho chúng: câu
nằm ở `public/i18n/{vi,en}.json`, tra theo khoá đang giữ.

> **🔄 SỬA 2026-09-06.** Mục này trước ghi *"both instances are hardcoded Vietnamese in the
> template, there is no i18n layer"* và liệt đúng **2** chỗ dùng. Cả ba vế đều sai tại thời
> điểm đọc: i18n đã chạy, chuỗi đã bọc, và số chỗ dùng là 6. Hai chỗ mới (`app.html`,
> `quan-tri-nguoi-dung.page.html`) chưa bao giờ có trong bảng — nghĩa là bảng này đã thiếu
> **trước cả** đợt i18n, chỉ không ai đếm lại.
>
> Bài học cho cột "Call site": nó là danh sách đếm tay, đúng loại `.claude/CLAUDE.md` §6 nói sẽ
> mục ruỗng. Kiểm lại bằng lệnh, đừng tin bảng:
>
> ```bash
> grep -rn 'class="notice' src/FE/src/app --include=*.html
> ```


Sources: `src/FE/src/styles.scss` (§ 5 `.notice`, `.notice > .pi`, `.notice > .icon-btn`, `.notice a`, `.notice.ok`, `.notice.warn`, `.notice.bad`), `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:19-26`, `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.scss` (confirms the page adds no local notice styling — `grep -c notice` returns 0), `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:146-154` (the second instance, with the source comment recording when to remove it), `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.scss` (which **does** add one local rule, `.notice .btn` — geometry only, for the text button the shared rule leaves no slot for)

## Do / Don't

- ✅ Match the severity to what the message actually is. The Dashboard's awaiting-progress line is **information**, not a complaint — the template comment says so explicitly (`dashboard.page.html:28-30`), and it is why it uses the default rather than `.warn` or `.bad`.
- ✅ Lead with the matching glyph. The icon is what lets a reader classify the strip before reading it; a bare `.notice` with no `pi` child loses that and leaves an unbalanced gap.
- ✅ Keep a banner and the behaviour it explains driven by the same signal — a banner describing a restriction that is not actually applied is worse than no banner.
- ✅ Put a dismiss control in as a plain `.icon-btn`; `margin-left: auto` is already in the rule, so nothing needs positioning at the call site.
- ❌ Don't use a notice for transient feedback — that is `Toast`'s job, and a notice does not disappear on its own.
- ❌ Don't borrow `.login-error` for a page-level error. That block belongs to the auth screens; `.notice.bad` is the shipped answer now.
- ❌ Don't hand-tune padding or radius per severity; the four variants deliberately differ only in fill and left rule.

## Normalize on redesign
1. **Three of the four severities have no shipped call site.** `.ok`, `.warn` and `.bad` exist in the library and the prototype but nothing renders them yet, so they are unproven in the running app. Both shipped instances are the default informational treatment.
2. **No `role="status"` or `aria-live`.** A banner that appears in response to a state change is announced to nobody.
3. **The dismiss button has no persistence.** There is no shipped "don't show again" mechanism, so a closable notice returns on the next navigation.
4. **The `4px` left rule is a bare literal**, and it is a second accent-width value alongside `Toast`'s `5px` — two strips, two thicknesses, no token behind either.
5. **Both shipped instances are rendered unconditionally**, which is the one pattern this component is least suited to: a permanent banner becomes furniture and stops being read. The permissions one is the sharper case — it explains that the matrix beneath it controls nothing, which is exactly the sentence a returning user stops seeing.
6. **The two instances disagree on whether the glyph is announced.** The home screen's `<i class="pi pi-info-circle">` has no `aria-hidden`, the permissions one does. The icon font emits no text either way, so neither is harmful, but the contract should say one thing — `aria-hidden="true"` is the right one, since the sentence already carries the meaning.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **Only one severity existed — resolved 2026-08-29.** A page-level error had to borrow `.login-error`, a block that belongs to the auth screens. Four severities now share one structure.
2. **Hardcoded `border-radius: 12px` — resolved 2026-08-29.** The rule now uses `rounded.md`; `Tokens/spacing.md` records `--radius-md` picking up `.notice` as a new consumer.
3. **No icon — added 2026-08-29.** The banner previously carried no glyph while the visually similar `.login-error` led with `pi-exclamation-circle`. `> .pi` is now part of the contract, with a per-severity ink.
4. **Nowhere to put a close button — added 2026-08-29.** The prototype's component library recorded this gap verbatim: dropping an `.icon-btn` in used to leave it stuck against the text because no child carried `margin-left: auto`. The rule now does.
5. **Dead link styling — resolved 2026-08-29.** `.notice a` used to be styling nothing; it now follows the severity ink, and the prototype's four sample banners all carry an inline link.
