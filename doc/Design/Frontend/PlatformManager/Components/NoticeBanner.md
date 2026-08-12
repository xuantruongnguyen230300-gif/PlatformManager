---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "NoticeBanner"
sources: ["doc/Prototype/dashboard.html"]
---

# NoticeBanner
**Description:** Static, non-dismissible instructional banner (`.notice`, `dashboard.html:26`) shown once at the top of `main`, explaining the weekly update workflow in prose.

## Anatomy
Single block of rich text (contains inline `<b>` emphasis on key terms), pale-blue rounded rectangle, no icon, no dismiss/close control.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Notice | `notice` | bg `colors.surface-notice`, border 1px `colors.border-notice`, `rounded.notice` (11px), `padding:11px 13px`, `font-size:13px` | Exactly one instance, always visible at the top of `main` — not conditional on any state |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg `colors.surface-notice`, border `colors.border-notice`, `rounded.notice`, `margin-bottom:14px` |
| hover | **N/A** — static text block, not interactive |
| focus | **N/A** — not focusable |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.surface-notice`, `colors.border-notice`, `colors.text`
- `rounded.notice`

## Reference markup

```html
<div class="notice">
  Mỗi tuần chọn ngày báo cáo, cập nhật <b>Tiến độ %</b> của từng chỉ tiêu rồi bấm <b>Lưu tuần này</b>.
  Hệ thống tự so với kỳ gần nhất trước đó và hiển thị <b>tăng/giảm bao nhiêu điểm %</b>.
</div>
```

Sources: `doc/Prototype/dashboard.html:26` (CSS), `doc/Prototype/dashboard.html:73-76` (markup)

## Do / Don't

- ✅ Keep the copy verbatim — it documents the exact save/compare workflow and must not drift from the real behavior it describes.
- ❌ Don't add a dismiss control — the shipped banner is permanent, not a toast/alert the user can close.

## Normalize on redesign
1. None specific to NoticeBanner beyond the library-wide items in `COMPONENTS.md` § Known inconsistencies.
