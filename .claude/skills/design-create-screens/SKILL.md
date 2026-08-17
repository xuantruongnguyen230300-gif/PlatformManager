---
name: "design-create-screens"
description: "Write faithful per-flow screen specs — layout blueprint, verbatim copy, states, responsive, iconography and screenshot refs — composing only documented components. Refuses without COMPONENTS.md and tokens.json."
argument-hint: "<project> [flow] - e.g. 'PlatformManager 01-dashboard'"
compatibility: "Requires a Design docs-as-code root — resolve {DESIGN_ROOT} at runtime (see its CLAUDE.md); never hardcode doc/Design/"
metadata:
  author: "design-team"
  source: "custom"
user-invocable: true
disable-model-invocation: false
---

## User Input

```text
$ARGUMENTS
```

Bạn **BẮT BUỘC** phải xem xét user input trước khi tiếp tục (nếu không rỗng).

## Mục tiêu

Chuyển census screen của UiInventory, markup view thật, và nguồn localization thành screen spec đúng thực tế theo từng flow trong `Screens/NN-flow.md` — layout blueprint chỉ ghép từ component đã tài liệu hóa, copy nguyên văn, state, hành vi responsive, iconography, và tham chiếu screenshot. Đây là stage 5 của design pipeline.

## STEP -1 — Resolve `{DESIGN_ROOT}` (BẮT BUỘC chạy đầu tiên)

Skill nằm ở workspace root (`.claude/skills/`), còn khu Product Design là **một thư mục con** có thể bị
di chuyển hoặc đặt tên khác. Vì vậy **KHÔNG hardcode `doc/Design/`** — luôn resolve trước.

`{DESIGN_ROOT}` nhận diện bằng **marker bất biến `Templates/DesignMd.md`**. Resolve theo thứ tự:

1. Nếu cwd đã nằm TRONG khu Design (có `Templates/DesignMd.md` ở cwd hoặc thư mục cha) → `{DESIGN_ROOT}`
   = thư mục chứa nó.
2. Nếu cwd là workspace root → Glob `**/Templates/DesignMd.md` (độ sâu 1-3); thư mục cha của `Templates/`
   là `{DESIGN_ROOT}` (hiện tại `doc/Design/` nhưng CÓ THỂ khác — dùng đúng kết quả tìm được, KHÔNG giả định).
3. Nếu >1 hoặc 0 kết quả → hỏi dev đường dẫn khu Design. KHÔNG đoán.

> Mọi `{DESIGN_ROOT}/...` bên dưới là **placeholder** — thay bằng đường dẫn thật đã resolve. Nếu skill
> chạy với cwd = Design root thì `{DESIGN_ROOT}` = `.`.

## Các bước thực hiện

### 1. Resolve project

- Token đầu tiên của `$ARGUMENTS` = project — hoặc `<Group>/<Project>` (vd. `Frontend/PlatformManager`) hoặc tên project trơn được resolve qua UI project index trong `{DESIGN_ROOT}/README.md`. Nếu thiếu hoặc mơ hồ, liệt kê các project hiện có và **dừng lại**.
- Token thứ hai (tùy chọn) = flow (phần stem tên file `Screens/` từ census UiInventory, vd. `01-dashboard`). Nếu bỏ trống, spec toàn bộ flow có trong census, mỗi flow một file.

### 2. Kiểm tra gate

- **⛔ GATE CHECK**: `COMPONENTS.md` và `Tokens/tokens.json` phải tồn tại trong folder project. Nếu thiếu một trong hai, **dừng lại** và báo rõ cần chạy stage tiên quyết nào trước (`/design-document-components <project>` / `/design-extract-tokens <project>`). Không viết spec khi chưa đủ điều kiện.

### 3. Spec từng screen

Với mỗi screen của flow (từ census UiInventory): đọc markup thật (file view) **và** nguồn localization, sau đó soạn đủ 7 section H3 bắt buộc, đúng theo thứ tự này:

1. `### Layout Blueprint` — cây region + số đo cấu trúc; chỉ được ghép từ tên component có trong `COMPONENTS.md`.
2. `### Copy` — chuỗi nguyên văn + key localization (hoặc "— (hardcoded)" nếu không có i18n layer) + nguồn `file:line`.
3. `### States` — hiển thị default / loading / empty / error / validation; 404/500 nếu thuộc phạm vi flow.
4. `### Responsive` — hành vi theo từng breakpoint.
5. `### Iconography` — từng dòng theo action, hoặc trỏ tới map `Icons.md`.
6. `### Screenshots` — tham chiếu vào `Assets/Screenshots/<flow-stem>/`, hoặc "pending — see UiInventory, Screenshot Manifest".
7. `### Normalize on redesign` — CHỈ ghi các điểm chưa hoàn hảo cục bộ của screen ở đây; section 1–6 giữ nguyên đúng như đang chạy thật.

- Copy phải **nguyên văn** — mỗi chuỗi đều mang nguồn `file:line`.
- Nếu một Layout Blueprint cần component chưa index trong `COMPONENTS.md`, **dừng lại** cho screen đó và báo cáo: quay lại `/design-document-components <project>` trước.
- Quy ước tên screenshot: `Assets/Screenshots/<screens-file-stem>/<view>.png` = state mặc định desktop-1440; hậu tố `--<state>` và `--<viewport>` (vd. `dashboard--error.png`, `dashboard--mobile-390.png`).
- Chụp screenshot bằng chrome-devtools MCP tools (`new_page` / `navigate_page` / `take_screenshot`) **chỉ khi** target phản hồi (dev server, hoặc file tĩnh mở qua `file://`); nếu không thì ghi dòng "pending" trong Screenshot Manifest của UiInventory kèm đúng hướng dẫn capture — không bao giờ block vì screenshot.

### 4. Ghi flow spec

- Ghi `Screens/NN-flow.md` từ `Templates/Screen.md` — mỗi screen một block, frontmatter `status: "draft"`.
- Mở rộng Per-Action Map trong `Icons.md` với mọi action mới mà flow này đưa vào.

### 5. Báo cáo

- Cập nhật dashboard status README của project: stage 5.
- Báo cáo các screen đã spec theo từng flow, screen nào bị chặn do component chưa tài liệu hóa, và screenshot còn pending.
- Bước tiếp theo: `/design-generate-prompts <project> <flow>`.

**Chuỗi pipeline**: nếu skill này đang chạy như một bước trong chuỗi (được
gọi bởi `design-expert` hoặc `/feature-kickoff` cho cả project/flow, không
phải người dùng gõ trực tiếp đúng lệnh này) — tự động gọi tiếp bước "Bước
tiếp theo" nêu ở trên qua công cụ Skill ngay sau khi báo cáo xong, không
dừng lại chờ người dùng gõ lệnh kế tiếp. Nếu người dùng gọi trực tiếp một
mình skill này (không qua chuỗi), chỉ nêu gợi ý bước tiếp theo như trên,
không tự mở rộng phạm vi yêu cầu ban đầu.

## Guardrails

- Không bao giờ mô tả một screen dựa vào trí nhớ hay app khác — mọi fact phải truy được về file view hoặc screenshot.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Bắt đầu từ đúng template tương ứng trong {DESIGN_ROOT}/Templates/ — giữ nguyên frontmatter key và thứ tự section.
- Mọi artifact vừa ghi đều mang frontmatter `status: "draft"`; không bao giờ set `approved`.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
