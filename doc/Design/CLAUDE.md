---
kind: luat
scope: du-an
verified: 2026-09-06
---

# CLAUDE.md — Design

Guidance for Claude Code working in `doc/Design/`, PlatformManager's Product Design home. See [README.md](./README.md) for the full UI project index and the AI → Figma → handoff workflow.

## Scope

- **This file governs**: `doc/Design/` and everything beneath it. It is self-contained — design work needs no sibling area's docs beyond the read-only lookups below.
- **In scope**: all specs, tokens, components, and screens under `doc/Design/<Backend|Frontend>/<Project>/` and `doc/Design/Shared/`.
- **Out of scope — do not modify**: `src/BE/`, `src/FE/`. Exception: a task that explicitly asks to *land* an already-recorded token change in code — that step runs **after** `Tokens/*` is updated (Core Principle 4) and touches only the token values the task names, in **both** `src/FE/src/styles.scss` and the `APP_PALETTE` constant in `src/FE/src/app/app.config.ts`.
- **When to cross (read-only, expected)**: reading live source in `src/FE/` or `src/BE/` to census what actually shipped and to re-sync a spec after code has landed. Reading code is how these specs stay honest about the app — it is **not** the direction a token change travels (Core Principle 4).
## 🎯 Core Principles (Read First)

### 1. Think Before Documenting
Don't assume specs. Don't hide confusion. Surface tradeoffs.
- State assumptions explicitly (e.g. which token maps to which live CSS value). If uncertain, ask.
- If multiple interpretations exist (e.g. is this a new component or a variant of an existing one), present them — don't pick silently.
- If reusing an existing component/token is simpler than adding a new one, say so.
- If something is unclear (no source file to cite, conflicting tokens), stop and ask.

### 2. Simplicity First
Minimum documentation that captures the real, live design. Nothing speculative.
- No components, screens, or states beyond what exists in the live app or was explicitly asked for.
- No invented tokens, colors, or spacing not derived from source — see `Rules`.
- No speculative "future" variants that aren't in the source code/UI yet.
- If a spec grows more elaborate than the thing it documents, trim it back.

### 3. Surgical Changes
Touch only the doc you must. Clean up only your own mess.
- Don't reorganize or reformat unrelated specs, components, or screens.
- Match the existing folder convention and markdown structure exactly, even where you'd choose differently.
- If you notice a stale or incorrect spec unrelated to your task, mention it — don't silently rewrite it.
- Remove only the parts of a spec that your own change made obsolete.

### 4. Goal-Driven Execution
Define success criteria against the live source. Verify before finishing.
- "Document component X" → cite its source file, confirm every token reference resolves, match states to the live UI.
- "Update tokens" → update `Tokens/*.md`, `Tokens/tokens.json` and the `DESIGN.md` frontmatter **first**, lint `DESIGN.md`, then land the agreed values in code — in **both** `src/FE/src/styles.scss` and the `APP_PALETTE` constant in `src/FE/src/app/app.config.ts`, which re-declares part of the palette and feeds it to the ramp builder `createCorePreset()` in `src/FE/src/app/core/theme/core-preset.ts`. Change one and not the other and CSS and the component library render different colours with nothing failing.
- "Add a screen spec" → verify it composes only from components already listed in `COMPONENTS.md`.

> **Token direction — `doc/Design/` is the source, code follows it.** Decided 2026-08-27, re-confirmed by the product owner 2026-08-29. The rule itself is held by **`doc/huong_dan/wiki-core/fe/04-design-token-system.md` § Chiều** — read the reasoning there and never restate the direction in a second place.
>
> Why this way round: this folder is where the team *decides* a design. A spec that mirrors code is only a transcript of code, so a decision agreed here would be silently erased the next time anyone re-extracted from `styles.scss`. `Prototypes/index.html` is where a change is **previewed**; it takes effect only once it is written into `Tokens/*`.
>
> An earlier revision of this bullet said *"update the live source first, then mirror"*. That was the pre-2026-08-27 direction and is now wrong — extraction from live source still happens (pipeline stages 2–4 bootstrap and re-sync the specs from the shipped app), but it records what already exists rather than deciding what should.

## Fidelity Policy

Specs describe the app **as-shipped**: real copy, real logo usage, real quirks — so AI-generated screens resemble the existing product. Anything you would prefer different (mixed styles, inconsistent spacing, layout warts) is recorded **only** under a clearly-marked **"Normalize on redesign"** section — never silently idealized into the spec itself. Generators are told to reproduce the shipped UI exactly; redesigns consume the Normalize lists deliberately.

**~~Greenfield carve-out~~ — EXPIRED 2026-08-22. The real app has landed.**

The earlier text said *"PlatformManager has no shipped frontend/backend app yet (`src/FE/` and `src/BE/` are empty) — the only shipped UI today is the static prototype"*. That is **no longer true**: `src/FE/` is a complete Angular 20 app and `src/BE/` is a running .NET solution.

Count the routed screens rather than trusting a number written in prose — the earlier revision of this paragraph and of § Groups both said "6", which stopped being true when the business module was removed on 2026-08-29:

```bash
grep -cE '^\s+loadChildren:' src/FE/src/app/app.routes.ts
```

PASS = the number matches the row count of the Screen Census in `Frontend/PlatformManager/UiInventory.md`. The `''` redirect and the `**` wildcard in that file are **not** routed screens; both resolve onto one of the counted routes.

**Live source, in priority order:**

1. **`src/FE/src/app/**`** — the shipped app. Component templates (`*.html`) and scoped styles (`*.scss`) are the ground truth for layout and copy. `src/FE/src/styles.scss` shows the token values **currently rendering**; the agreed values live in `Tokens/*` (Core Principle 4), so where the two differ it is code that is behind.
2. **`spec/*/`** — business rules behind the UI.

> 🗑️ The repo-root static HTML prototype folder was **deleted 2026-08-23**.
> It is no longer a source of any kind, and no design lineage may be traced back to it.
> See `.claude/CLAUDE.md` §7.
>
> Do not confuse it with `Frontend/PlatformManager/Prototypes/index.html`, a *different*
> and current file: a visual reference where a change is previewed before it is written
> into `Tokens/*`. It is not a source either — see that folder's `README.md`.

> Why this matters enough to write down: sourcing a spec from that deleted prototype now would produce a document describing a screen that **is not what ships** — the exact "documentation describes something that doesn't exist" failure that `.claude/CLAUDE.md` §3 forbids.

## Neo trích dẫn vào `styles.scss`

**Luật: một spec KHÔNG BAO GIỜ trích `src/FE/src/styles.scss` bằng số dòng.** Neo bằng
**định danh** — tên selector, tên custom property, hoặc tên at-rule — theo đúng một khuôn:

```text
`src/FE/src/styles.scss` § `.card`
`src/FE/src/styles.scss` § `--sp-3`
`src/FE/src/styles.scss` § `@media (prefers-reduced-motion: reduce)`
```

Vị trí hôm nay đọc bằng lệnh, không chép vào tài liệu (`.claude/CLAUDE.md` §6):

```bash
grep -n '^\.card' src/FE/src/styles.scss
grep -n '^\s*--sp-3:' src/FE/src/styles.scss
```

Khi thứ cần trỏ tới là một **khối chú thích** chứ không phải một rule (lý do hợp nhất,
phép đo tương phản, cảnh báo `APP_PALETTE`), neo bằng câu mô tả + lệnh `grep` tìm lại —
đừng bịa ra một selector không có thật chỉ để hợp khuôn.

### Vì sao — đợt đo 2026-09-04

`styles.scss` được viết lại 2026-08-29 và nay dài 1015 dòng. Đợt rà 2026-09-04 đếm được
**230 lượt trích dẫn `styles.scss:N`** trong `doc/Design/`, và đối chiếu từng lượt với
selector mà câu văn quanh nó khẳng định:

| Kết quả | Số lượt |
| --- | ---: |
| Dòng được trích đúng là chỗ khai định danh được nhắc | 20 |
| Định danh có thật nhưng khai ở **dòng khác** | 154 |
| Không suy ra được định danh nào từ câu văn | 56 |

Ba con số trên là **kết quả một phép đo ngày 2026-09-04**, không phải bảng kiểm kê
phải giữ cho đúng (`.claude/CLAUDE.md` §6). Đếm lại số lượt còn sót bằng lệnh — PASS là
chỉ còn các file mang banner `TÀI LIỆU LỊCH SỬ` và chính dòng ví dụ trong mục này:

```bash
grep -rc 'styles\.scss:[0-9]' doc --include='*.md' | grep -v ':0$'
```

Ví dụ: `Footer.md` khai `.footer` ở `486-500`, thật ở `1012`; `Button.md` khai `.btn` ở
`142-195`, thật ở `482` — `142` là một dòng văn xuôi **trong khối chú thích đầu file**.
Riêng nhóm token lệch **đúng +12 dòng** vì bản viết lại mọc thêm một MỤC LỤC 12 dòng
phía trên `:root`.

**Cổng không thấy gì cả, và đó mới là điều đáng sợ.** `check-docs.sh` mục 6 chỉ hỏi
*"số dòng có ≤ độ dài file không"* — trong một file 1015 dòng thì mọi số dưới 1015 đều
"nằm trong file". Nó cũng chỉ soi trích dẫn bắt đầu bằng `src/` hoặc `doc/`, nên dạng
viết tắt `` `styles.scss:486-500` `` **chưa từng** được kiểm. Sửa số dòng cho đúng hôm
nay là mua một lần nữa cùng một lỗi: lần viết lại sau nó lại trôi, và lại không ai thấy.

Neo theo định danh thì kiểm được thật: hỏi *"selector này có được khai trong file
không"*, không phải *"con số này có nhỏ hơn 1015 không"*.

Khuôn này **không phải phát minh của khu Design** — nó là khuôn
`doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md` đã dùng khi gỡ neo
`ErrorCatalogTests.cs:39` sang neo bằng **tên ca kiểm + lệnh `grep`**, cùng một lý do:
*tên đổi thì compiler biết, số dòng đổi thì không ai biết.*

### Ngoại lệ: file mang banner `TÀI LIỆU LỊCH SỬ`

Trong file lịch sử, số dòng `styles.scss` **giữ nguyên** — chúng mô tả một bản
stylesheet đã chết và neo lại theo `styles.scss` hôm nay sẽ là bịa. Nhưng banner phải
nói rõ rằng `styles.scss` là **cái bẫy riêng** của loại file này: khác với file
component đã bị xoá, nó vẫn tồn tại, nên một số dòng cũ vẫn "phân giải được" — chỉ là ra
nội dung không liên quan.

## Groups

- **`Backend/`** — private UIs embedded directly in a backend service under `src/BE/` once one exists.
- **`Frontend/`** — the public app under `src/FE/` (Angular 20; count its routes with the command in § Fidelity Policy, never from a number written here).
- **`Shared/`** — cross-app brand material (`Assets/`, `UserFlows/`) that isn't scoped to a single project.

## Per-project folder convention

Every project folder (e.g. `Frontend/PlatformManager/`) follows. Canonical templates for every artifact live in [Templates/](./Templates/) — start from them, keep their frontmatter keys and section order exactly:

> **The three classification keys change when a template is instantiated** (`.claude/CLAUDE.md` §9,
> enforced by `check-docs.sh` §10). A template file is CoreBase tooling, so it carries
> `scope: core`; the artifact generated from it describes **one product**, so it carries
> **`scope: du-an`** and starts at **`verified: chua-doi-chieu`** until someone opens the live
> source and checks the whole file. `kind: luat` carries over unchanged. Copying a template's
> frontmatter verbatim gives a new artifact the wrong `scope` and a `verified` date nobody
> earned — recorded here 2026-09-06 after every template was compared against its live
> counterpart.

- `README.md` — project guide: stack, source paths, maintenance rules + the 8-stage pipeline status dashboard
- `UiInventory.md` — ground-truth census of the live app (routes, views, copy sources, brand-asset manifest, screenshot manifest) — the pipeline's first gate
- `DESIGN.md` — design.md format (YAML token frontmatter + guidance), importable into Google Stitch; opens with the fidelity blockquote
- `COMPONENTS.md` + `Components/` — component index and one spec per component (Sources cited, 5-state tables)
- `Tokens/` — colors, typography, spacing + `tokens.json` (W3C DTCG: `global` + `light` + `dark` sets, for Figma Tokens Studio); colors.md carries the chart palette (or an explicit "None — app has no charts")
- `Icons.md` — icon system + per-action map + declared legacy exceptions
- `Screens/NN-flow.md` — screen specs grouped by flow; every screen carries the 7 mandatory sections: Layout Blueprint · Copy · States · Responsive · Iconography · Screenshots · Normalize on redesign
- `Assets/Brand/` — real image files copied from the live app (original filenames kept); `Assets/Screenshots/<flow-stem>/<view>[--state][--viewport].png` (default = desktop-1440)
- `Prompts/<NN-flow>-prompts.md` — multi-tool prompt packs (Google Stitch, Claude Design, Google AI Studio, generic)
- `Exports/ExportLog.md` — append-only Figma export proof · `AUDIT.md` — latest audit verdict (PASS/BLOCKED)
- Created on demand: `Wireframes/`, `Mockups/`, `Prototypes/`, `UserFlows/`

## Pipeline & Skills

Design work moves through eight stages, one skill each — run in order; gates refuse when a prerequisite is missing:

| # | Stage | Skill | Gate | Primary output |
|---|-------|-------|------|----------------|
| 1 | Scaffold | `/design-new-project <Group>/<Project>` | refuses to overwrite | folder tree + README dashboard + UiInventory stub |
| 2 | UI Inventory | `/design-inventory-ui <project>` | project scaffolded | UiInventory.md census + Assets/Brand/ + screenshots (or pending) |
| 3 | Tokens | `/design-extract-tokens <project>` | UiInventory exists | Tokens/*, tokens.json, DESIGN.md (lint: 0 errors) |
| 4 | Components | `/design-document-components <project>` | census populated | COMPONENTS.md + Components/*.md |
| 5 | Screens | `/design-create-screens <project> [flow]` | components + tokens exist | Screens/NN-flow.md (7 mandatory sections) |
| 6 | Prompt Packs | `/design-generate-prompts <project> <flow>` | flow spec complete | Prompts/<flow>-prompts.md (4 tool sections) |
| 7 | Audit | `/design-audit <project>` | — | AUDIT.md PASS/BLOCKED + fix commands |
| 8 | Figma Export | `/design-export-figma <project>` | audit PASS + lint clean | Figma file + ExportLog entry |

## Rules

- English only for all documentation.
- Never hard-code colors/sizes in specs — reference tokens.
- Extend an existing component spec instead of inventing a new one.
- Never copy real secrets, API keys, or credentials from source files into any design artifact, prompt pack, or content pushed to Figma/Stitch.
- Single-app material stays in its project folder; only genuinely cross-app material goes in `Shared/`.
- Screenshots — **default is ONE desktop shot per screen** (decided 2026-08-22). That is what answers "what does this screen look like"; state and viewport variants are captured only when someone actually needs that case. Record the rest as `pending` rows with exact capture instructions in the UiInventory Screenshot Manifest, and never block on screenshots.
  > Why the cap: an earlier pass queued **40** pending shots across 5 screens and none were ever taken. A `pending` list nobody works through is worth the same as no list — and it hides which screens genuinely have no visual reference at all.
  Capture via the chrome-devtools MCP once the target is reachable. For PlatformManager that means **both** servers running: `dotnet run --project src/BE/PlatformManager.Api --urls https://localhost:7168` — that URL must match `apiBaseUrl` in `src/FE/src/environments/environment.development.ts`, which the app reads at runtime; and the Angular dev server in `src/FE` on **port 4200 or 4201**, the only two origins in the backend's Development CORS allowlist. Verified 2026-08-29 by running both and loading the app.
  > **Nơi kiểm allowlist — sửa 2026-09-08.** Dòng này trước neo vào `src/BE/PlatformManager.Api/appsettings.Development.json`, nhưng file đó bị `src/BE/.gitignore:20` (`appsettings.*.json`) loại khỏi repo: ai clone về sẽ **không có** nó, nên khẳng định "chỉ hai origin" là thứ họ không kiểm được — và cổng cũ cho qua vì file tình cờ có trên máy người viết. Hình dạng và ràng buộc của allowlist kiểm được ở nguồn tracked: `src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs` và `src/BE/Tests/PlatformManager.ArchTests/OptionsValidateOnStartTests.cs`. Còn giá trị 4200/4201 là **cấu hình cục bộ của từng máy** — máy mới phải tự tạo `appsettings.Development.json`. An earlier revision of this line named the plain-HTTP launch profile instead of the HTTPS one the FE actually calls, and claimed a wider range of dev-server ports is allowed — following it produces a browser that loads the shell and fails every API call, with the cause split across two files. Most screens need an authenticated session; a fresh database is created by running `doc/db-khoi-tao.sql` — see `doc/cau-truc-database.md` §5. (The generator script under `src/BE/scripts/` does **not** build a database — its own header states it never calls `dotnet ef database update`; it only emits `.cs`/`.sql` files and imports CSV.) Never record credentials in any design artifact — including in capture instructions.
- Prompt packs resolve tokens to **literal values** (hex/px/font) — external tools cannot interpolate `{token.reference}`.
- Lint DESIGN.md with `npx --yes --package=@google/design.md designmd lint <path>` — the bare `npx @google/design.md lint` form **fails silently on Windows**. Gate = 0 errors; warnings are recorded as as-shipped facts, not blockers.
