---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "Badge"
sources: ["doc/Prototype/dashboard.html"]
---

# Badge
**Description:** Small pill-shaped status label (`.badge`, `dashboard.html:47-48`) shown in the "Trạng thái" column of the 62-criteria table. **Critical distinction (do not confuse):** this badge is a **runtime-computed** value from `statusFor(v,d)` (`dashboard.html:867-871`) based purely on `ProgressPercent`/delta — it is a completely different concept from the database-persisted `CriteriaAssessment.Status` field (4 manual values), which has **no UI control anywhere** in this dashboard. See `spec/dashboard-dti-weekly/business-rules.md` §5 and `ui-spec.md` §6.4 for the full clarification.

## Anatomy
Single inline text label, no icon. Pill shape via `rounded.pill` radius, small padding, bold small text.

## Variants

| Variant | Classes | Key values | When to use (`statusFor(v,d)` logic) |
| --- | --- | --- | --- |
| Done | `badge bdone` | bg `colors.surface-badge-success`, text `colors.success` | `progress >= 99.999` |
| Stalled | `badge bstall` | bg `colors.surface-badge-danger`, text `colors.danger` | has a previous period **and** `delta <= 0.001` (i.e. not increased) |
| Working | `badge bwork` | bg `colors.surface-badge-warning`, text `colors.warning` | fallback — has increased, or no previous period exists yet |

Only these 3 values are ever rendered — there is **no** "Chưa thực hiện" or "Cần bổ sung minh chứng" badge (those belong to the separate DB `Status` field, out of scope for this screen).

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | per-variant bg/text above; `font-size:11px`, `font-weight:750`, `border-radius:999px`, `padding:4px 7px` |
| hover | **N/A** — plain `<span>`, not an interactive element, no `:hover` rule |
| focus | **N/A** — not focusable |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.surface-badge-success`, `colors.surface-badge-warning`, `colors.surface-badge-danger`
- `colors.success`, `colors.warning`, `colors.danger`
- `rounded.pill`
- `typography.badge` (11px/750)

## Reference markup

```html
<span class="badge bdone">Hoàn thành</span>
<span class="badge bwork">Đang thực hiện</span>
<span class="badge bstall">Không tăng</span>
```

Sources: `doc/Prototype/dashboard.html:47-48` (CSS), `doc/Prototype/dashboard.html:867-871` (`statusFor()` logic), `doc/Prototype/dashboard.html:892` (render call site in `renderTable()`)

## Do / Don't

- ✅ Treat this badge as read-only, computed output — never add a click handler or edit affordance to it.
- ✅ Keep exactly 3 variants (`bdone`/`bwork`/`bstall`) — don't add a 4th to represent the DB `Status` field; that is explicitly out of scope for this screen (`spec/dashboard-dti-weekly/spec.md` § Quyết định đã chốt #2).
- ❌ Don't relabel this as "Trạng thái nhập tay" or similar — it must stay visually/semantically distinct from the manual `CriteriaAssessment.Status` field.

## Normalize on redesign
1. None specific to Badge beyond the library-wide items in `COMPONENTS.md` § Known inconsistencies.
