---
kind: luat
scope: du-an
verified: chua-doi-chieu
project: "PlatformManager"
status: "draft"
updated: "2026-09-08"
result: "BLOCKED (verdict of the 2026-08-22 run — stale, not re-run)"
audited: "2026-08-22"
---

> ## ⏳ KẾT LUẬN NÀY ĐÃ CŨ — đọc trước khi tin (ghi 2026-09-08)
>
> **Ngày của lượt audit là 2026-08-22**, khớp `audited:` ở frontmatter, khớp dòng
> *"All 11 findings were fixed on 2026-08-22, after this run"* ngay dưới, và khớp nội dung
> báo cáo (nó còn mô tả `/dashboard` + `/danh-muc/dti` như route đang chạy — hai route bị gỡ
> ngày 2026-08-29).
>
> 🔄 **SỬA 2026-09-08.** Bản trước của chính khối này ghi lượt audit là **2026-08-29**, còn
> `README.md` frontmatter cũng ghi 2026-08-29 trong khi thân `README.md` ghi 2026-08-22 —
> **bốn phát biểu, hai ngày**. Đã thống nhất về 2026-08-22 ở cả hai file.
>
> **Trạng thái thật hôm nay:**
>
> | | |
> | --- | --- |
> | 11 finding của lượt 2026-08-22 | **đã đóng hết** — xem nhật ký xử lý ngay dưới |
> | Kết luận `BLOCKED` | **giữ nguyên, cố ý.** Nó là kết luận của lượt đó, không phải mô tả hôm nay. Đổi nó thành `PASS` mà không chạy lại chính là khuôn sai `.claude/CLAUDE.md` §4 cấm |
> | Tình hình hôm nay | **không ai biết** — chưa có lượt audit nào chạy sau 2026-08-22 |
>
> Từ 2026-08-22 tới nay `src/FE` đã đổi lớn: module `DtiWeekly` bị gỡ (2026-08-29), palette và
> component layer viết lại (2026-08-29), tầng **i18n runtime** (mọi chuỗi giao diện nay đến từ
> `public/i18n/*.json`), **`shared/components/data-grid/`** (lưới bản ghi dùng chung),
> **`shared/components/language-switcher/`**, và cơ chế chiều cao lưới `page-fill`/`grid-host`.
> Vài dòng ở § Checks that passed đã được sửa tại chỗ ngày 2026-09-08 vì chúng khẳng định sai
> hiện trạng — mỗi dòng mang ghi chú riêng. Không dòng nào trong số đó là một lượt audit.
>
> **Việc còn lại: chạy `/design-audit PlatformManager`.** Cho tới lúc đó, đừng đọc file này
> như tình trạng hôm nay.
>
> File này **cố ý giữ `verified: chua-doi-chieu`**. Đối chiếu nó không phải là đọc lại rồi đóng
> dấu — mà là **chạy lại `/design-audit`**.

# Audit Report — PlatformManager

> ## ✅ All 11 findings were fixed on 2026-08-22, after this run
>
> **The BLOCKED verdict below is the verdict of *this* run and is left unedited** — the audit records what it found, and a fresh verdict needs a fresh `/design-audit PlatformManager`. What follows is the resolution log, not a new verdict.
>
> | # | Resolved by |
> |---|---|
> | 1 | `Icons.md` now documents **two** icon sources. The false claim ("no `<svg>` … anywhere") is replaced; 5 rows added for the PrimeNG inline-SVG set (4 paginator arrows + spinner), with the binding sites that switch them on and 3 new Normalize items covering their accessibility. |
> | 2 | `Screens/02:179`, `03:162`, `04:169` rewritten against `Icons.md:5`. |
> | 3 | The three `> **Shell:**` blockquotes now cite `DESIGN.md:418-422` and summarise the two Angular shells it documents. |
> | 4 | `DESIGN.md:428` → COMPONENTS.md is 27+1 and is the composition gate; `:432` → the two icon sources. |
> | 5 | `UiInventory.md:37` → both brand marks are the bare string `PM`, with the `sidebar.html:3` / `auth-card.html:4` sources and the machine check that found zero images. |
> | 6 | `05-auth.md:229` now agrees with `:104`. |
> | 7 | `CoreSeeder.cs:81-86` → `:121-126` in `Icons.md` (7 places) **and in three files this audit missed**: `Components/Sidebar.md:112`, `Screens/04:26,114,137`, `UiInventory.md:27`. |
> | 8 | An `empty` bullet added to both auth screens, stating why none ships. |
> | 9 | `02` § Normalize: closed history moved into a "✅ Closed" blockquote; the open items renumbered 1–11. |
> | 10 | `01:196` → "authors no icon of its own", plus the runtime-injected set; specs 02/03/04 gained the same. |
> | 11 | Not a defect — recorded as-is. |
>
> **Two corrections made while resolving, both to work done in this same pass:**
> 1. `Icons.md` first claimed all six `[loading]` bindings render a PrimeNG spinner. They do not — `phan-quyen.page.html:26,49` are the app's own `input<boolean>()` on hand-written `<table>`s (nothing under `platform/phan-quyen/` imports `TableModule`), and two more are page-level forwards into a child grid. **Two** `p-table` instances render a spinner, not six.
> 2. Editing `trend-chart.{ts,html}` (an unrelated code fix the same day) shifted every line in those files, silently invalidating **35** citations across 5 docs. All were remapped and machine-verified.
>
> **Machine checks now passing repo-wide:** 1622 `file:line` citations, **0** out of range (was 8). 130 token-value ↔ source-line pairs re-verified. `designmd lint` 0 errors / 6 warnings.

## Verdict

**BLOCKED** by findings **1–7**. This audit replaces the 2026-08-11 **PASS**, which was voided: that verdict examined the prototype-era artifact set extracted from the deleted prototype, and stages 2–6 have since been re-run against the shipped Angular app in `src/FE/`.

The lint gate is **clean** — `DESIGN.md` returns **0 errors, 6 warnings**, and none of the six is a real defect (2 are linter false positives that compare an alpha-composited brand tint against brand itself; 4 are border-colour tokens the design.md `components` schema has no slot for). Ten of the fourteen checks pass outright, and check (f) is legitimately N/A.

What blocks is **fidelity drift**, which this template classifies as always blocking: seven places where a spec states something the shipped app or a sibling doc contradicts. Six of the seven are a single mechanical failure repeated — a doc was corrected, and the documents that cite it were not. Finding 1 is different in kind and the reason the run cannot be waved through: `Icons.md` does not merely omit a shipped icon source, it **explicitly denies that source exists**. A reader who trusts it will conclude the app renders no SVG at all.

Nothing here requires touching `src/`. Every finding is a documentation correction.

## Findings

| # | Category | Severity | Location | Fix command |
|---|----------|----------|----------|-------------|
| 1 | icons | blocking | `Icons.md:11` — asserts *"there is no `<svg>` sprite and no icon component wrapper anywhere in `src/FE/`"*. PrimeNG renders **inline SVG** for paginator arrows and the loading spinner at runtime: `[paginator]="true"` at 3 tables, `[loading]` bound at 6 sites. Absent from both the Per-Action Map and Legacy Exceptions — the silent omission check (i) forbids | `/design-document-components PlatformManager` |
| 2 | fidelity drift | blocking | `Screens/02-danh-muc-dti.md:179`, `03-quan-tri-nguoi-dung.md:162`, `04-phan-quyen.md:169` each state `Icons.md` frontmatter reads `library: "none"`. False since the 2026-08-22 refresh — `Icons.md:5` reads `PrimeIcons v7`. `04:169` goes furthest, recording "a gap" that no longer exists | `/design-create-screens PlatformManager` |
| 3 | fidelity drift | blocking | `Screens/02:14`, `03:14`, `04:14` state `DESIGN.md` → Layout "still documents the prototype's single sticky-topbar page with no sidebar". False — `DESIGN.md:418-422` documents **both** Angular shells (sidebar 220/60px + `.shell-content`; `noShell` auth shell) and all three breakpoints | `/design-create-screens PlatformManager` |
| 4 | fidelity drift | blocking | `DESIGN.md:428` says `COMPONENTS.md` "still describes the prototype" (it holds 27 specs + 1 obsolete); `DESIGN.md:432` says `Icons.md` "still claims `library: "none"` and is stale" (corrected same day). `DESIGN.md` carries stale claims about its own siblings | `/design-extract-tokens PlatformManager` |
| 5 | fidelity drift | blocking | `UiInventory.md:37` describes the brand marks as *"text + a PrimeIcons glyph (`pi-shield`)"*. Both are the bare string `PM` (`sidebar.html:3`, `auth-card.html:4`); `pi-shield` is the BE-seeded nav icon for Phân quyền (`CoreSeeder.cs:126`). `Components/Sidebar.md:22` and `AuthCard.md:24` already say "the mark is typographic". The conclusion (no logo asset) holds; the description of what ships does not | `/design-inventory-ui PlatformManager` |
| 6 | fidelity drift | blocking | `Screens/05-auth.md` contradicts itself: `:104` records `Icons.md` "was corrected in the 2026-08-22 refresh"; `:229` says it "still declares `library: "none"` from the prototype era" | `/design-create-screens PlatformManager` |
| 7 | citations | blocking | `Icons.md:33-38,59` cite `CoreSeeder.cs:81-86` for the six seeded nav icons. Those lines are the bootstrap **account** loop (`FindByNameAsync`, `new AppUser`); the menu seed is `CoreSeeder.cs:121-126`. Repeated at `Screens/04-phan-quyen.md:177` | `/design-document-components PlatformManager` |
| 8 | states | warning | `Screens/05-auth.md` — neither screen has an **empty** state bullet, and neither declares that none ships. Every other state is covered and the absent ones are declared explicitly (`:82` no spinner, `:83` no per-field error slot, `:210` locked-out unreachable), which is what makes this one omission stand out | `/design-create-screens PlatformManager` |
| 9 | fidelity markers | warning | `Screens/02-danh-muc-dti.md:221,233` — 2 of 12 "Normalize on redesign" bullets are closed history + as-shipped description rather than deviations awaiting a fix. Struck-through entries document *why* something changed, which is useful, but they belong outside a list whose contract is "things still to fix" | `/design-create-screens PlatformManager` |
| 10 | icons | warning | `Screens/01-dashboard.md:196` states "This screen renders no icon of its own", but the dashboard criteria table paginated (SVG arrows) and the DTI catalogue page rendered an SVG spinner. Specs 01, 02, 04 all omit the PrimeNG SVG source — the same root cause as finding 1. 🔄 **2026-09-08:** ô này trích `criteria-table.html:32` và `danh-muc-dti.page.html:46`; cả hai file **đã xoá** 2026-08-29 cùng module `DtiWeekly`, nên hai neo đó không ai mở được nữa. Đã đối chiếu tại commit `98a5d96` ngày 2026-09-08 — **cả hai đúng ở commit đó** (`[paginator]="true"` và `[loading]`), rồi gỡ số dòng khỏi ô này thay vì để một trích dẫn chết. Finding, severity và verdict giữ nguyên: đây là hồ sơ của lượt 2026-08-22, không phải mô tả hôm nay. Cùng hiện tượng hôm nay nằm ở lưới dùng chung `src/FE/src/app/shared/components/data-grid/data-grid.html:9,12` — kết luận về nó thuộc lượt `/design-audit` tiếp theo, không phải file này | `/design-create-screens PlatformManager` |
| 11 | lint | info | `DESIGN.md` — 0 errors, **6 warnings**: `sidebar-item-active` and `chart-line` report 1.00:1 because the linter reads their `backgroundColor` as an 8-digit hex and compares the colour against itself; `line`, `border-strong`, `bad-border`, `border-notice` are border colours the design.md `components` schema has no slot for. Both categories are recorded as as-shipped facts, not defects | `npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md` |

### Checks that passed

- **(a) Inventory census** — every real route from `app.routes.ts` was described (`''` and `**` are a redirect and a wildcard, not screens). 🔄 **2026-09-08:** this line read *"all 6 real routes"*. Two routes were removed with `DtiWeekly` on 2026-08-29, so the number is no longer 6. Count instead of reading a number (`.claude/CLAUDE.md` §6): `grep -cE '^\s+loadChildren:' src/FE/src/app/app.routes.ts` — PASS = it equals the Screen Census row count in `UiInventory.md`.
- **(b) Screenshots** — 6 manifest rows, all `captured`, each with a reproducible instruction (launch command + URL + viewport + state). Variant shots read `on demand`, not `pending`, per the one-desktop-shot-per-screen policy; no credentials recorded anywhere.
- **(c) Brand assets** — declared explicitly as "Still none" with the reason, not silently empty. (The *description* of what ships instead is wrong — see finding 5.)
- **(d) Screen specs** — all 6 screens carry the 7 mandatory sections in order, and every Layout Blueprint is a real nested region tree with structural measurements (`repeat(5,1fr)` KPI grid, a 12-column `min-width:1430px` header, `scrollHeight = max(320, innerHeight − hostTop − 160)`), not a flat component list.
- **(e) Copy** — every Copy-table row cites a source; 259 distinct Vietnamese strings were machine-checked against `src/` and **all** resolve, including HTML-escaped (`&amp;`), runtime-composed (`Biểu đồ tiến độ hàng {tuần|tháng}`) and server-side strings.
- **(f) Logo usage** — **N/A**, correctly. Zero `<img>`, zero `background-image`, zero `<svg>` authored in `src/FE/src`; the only image file is the default `favicon.ico`. Both brand marks are text.
- **(g) States** — 4 of 6 screens cover default/loading/empty/error/validation and declare the ones that do not ship. (2 exceptions in finding 8.)
- **(h) Responsive** — every screen documents its breakpoints **and** names where a breakpoint deliberately does not exist (`criteria-table.scss` has no `@media` at all; the DTI grid scrolls horizontally at 1430px; both auth screens state "All viewports — there is no breakpoint").
- **(i) Icons — PrimeIcons portion** — every `pi-*` class used in `src/FE` appeared in the Per-Action Map; the ones that ship via BE seeding are attributed to `CoreSeeder`; 3 legacy exceptions (`↑/↓`, `└`, `●`) are declared and verify against source. The frontmatter `library` claim checks out — `primeicons.css` is listed in the `styles` array of **both** the `build` target (`src/FE/angular.json:37-40`) and the `test` target (`src/FE/angular.json:104-107`), so the icon font loads in the app and in the test runner alike. Re-anchor by identifier rather than reading these numbers back: `grep -n 'primeicons' src/FE/angular.json` — PASS = two hits, one under `build`, one under `test`. Only the PrimeNG-SVG source was missing — finding 1. 🔄 **2026-09-08:** this line read *"all 19 `pi-*` classes"* and *"the 4 that ship via BE seeding"*. Neither number survived the module removal and the component consolidation. List the set instead of reading a number (`.claude/CLAUDE.md` §6): `grep -rhoE 'class="[^"]*' src/FE/src --include='*.html' | grep -oE 'pi-[a-z0-9-]+' | sort -u` — PASS = every entry has a row in `Icons.md` § Per-Action Map.

  > ⚠️ **Neo mà cổng không nhìn thấy — sửa 2026-09-08, và đây mới là phần đáng nhớ.** Neo cũ viết `angular.json` dòng `37-39,123-125` (cố ý tách tên file khỏi số dòng ở đây — viết liền thì chính cổng sẽ đi kiểm **mẫu vật** này như một trích dẫn thật, đúng lỗi đoạn văn đang kể). `src/FE/angular.json` chỉ có **122 dòng**, nên `123-125` không tồn tại — nhưng **không mục nào của `check-docs.sh` báo gì cả**, vì hai lý do cộng lại: (1) regex trích dẫn dừng ngay sau range đầu tiên, nên `,123-125` đơn giản là không được đọc; (2) range đầu `37-39` thì hợp lệ, nên cả neo trông như đã kiểm.
  >
  > Dạng hỏng này khác hẳn một neo chết: neo chết thì mở không ra, còn neo có **dấu phẩy** thì nửa sau của nó nằm ngoài tầm mọi phép kiểm, vĩnh viễn. Khẳng định nền — PrimeIcons có ở cả hai target — **là đúng**; chỉ bằng chứng cho nửa sau là giả. Đó chính là kịch bản `doc/Design/CLAUDE.md` § Neo trích dẫn mô tả: cổng trả lời được *"con số này có nhỏ hơn độ dài file không"*, không trả lời được *"chỗ đó có phải thứ câu văn đang nói không"*.
  >
  > Luật rút ra, áp cho mọi neo trong khu này: **một neo, một range.** Cần trỏ hai chỗ thì viết hai trích dẫn đầy đủ, đừng nối bằng dấu phẩy.
- **(j) Chart palette** — present in both `DESIGN.md:436` and `Tokens/colors.md:98`, describing the one shipped `p-chart`; the prior "None — app has no charts" is explicitly retracted.
- **(k) Fidelity markers** — the fidelity blockquote opens `DESIGN.md`; every screen spec and `UiInventory.md` carry a "Normalize on redesign" section. (1 exception in finding 9.)
- **(l) Lint** — 0 errors. Gate met.
- **(m) Export log** — `Exports/ExportLog.md` exists with the append-only contract stated.
- **(n) Dashboard** — the stage table in this project's `README.md` and the index row in `doc/Design/README.md` both match reality (1–6 done, 7 running, 8 superseded).

### Verified beyond the required checks

- **Component gate** — specs on disk matched the index, with **0** screen-spec references to an unindexed component, and every live component was cited by at least one screen. 🔄 **2026-09-08:** the counts and the obsolete-component clause are gone — `Fab.md` was deleted (with `ActionButton.md`, `CellIconButton.md` and `FilterBar.md`) in the 2026-08-29 consolidation, so *"the 1 obsolete (`Fab`) is cited by none"* describes a file that no longer exists. Re-measure: `ls doc/Design/Frontend/PlatformManager/Components/*.md | wc -l` against the row count of `COMPONENTS.md` § Index — PASS = equal.
- **Prompt packs** — all 5 flows present. **0** unresolved `{token.reference}`, **0** `var(--…)`, and every hex in all five files resolves to a colour that exists in the live `:root`. No invented values.
- **`tokens.json`** — valid W3C DTCG: 81 `global` + 33 `light` tokens, every one carrying `$type`; `dark` deliberately empty because no dark mode ships. Every colour traces to `styles.scss`.

## Gate Status

| Stage | Status |
|-------|--------|
| 1 Scaffold | ✅ |
| 2 UI Inventory | ⛔ finding 5 |
| 3 Tokens | ⛔ finding 4 |
| 4 Components | ⛔ findings 1, 7 |
| 5 Screens | ⛔ findings 2, 3, 6 |
| 6 Prompt Packs | ✅ |
| 7 Audit | ⛔ this report |
| 8 Figma Export | ⬜ not reached — also superseded by the Figma Starter-plan MCP quota |

## Next command

`/design-document-components PlatformManager`
