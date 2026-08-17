---
name: "design-new-project"
description: "Scaffold a new design project under {DESIGN_ROOT}/<Group>/<Project>/ — folder tree, README dashboard and UiInventory gate doc from the templates. Refuses to overwrite an existing project."
argument-hint: "<project> [Stack hint] - e.g. 'Frontend/PlatformManager Static HTML/CSS/JS prototype' or 'Backend/Api ASP.NET Core Minimal API'"
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

Tạo một folder design project sẵn sàng dùng dưới `{DESIGN_ROOT}/<Group>/<Project>/` từ tên `<Group>/<Project>` và stack hint tùy chọn: cây folder, README status dashboard và file gate `UiInventory.md` từ `{DESIGN_ROOT}/Templates/`, cộng một dòng đăng ký trong UI project index của `{DESIGN_ROOT}/README.md`. Đây là stage 1 của design pipeline.

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
>
> Chưa có `{FE_ROOT}`/`{BE_ROOT}` cố định — `src/FE/` và `src/BE/` hiện đang rỗng (chưa chọn stack).
> Live source của từng project lấy từ chính `source_paths` trong `README.md` của project đó, không giả
> định theo một framework marker cụ thể.

## Các bước thực hiện

### 1. Resolve project

- Token đầu tiên của `$ARGUMENTS` là project — chấp nhận `<Group>/<Project>` (vd. `Frontend/PlatformManager`) hoặc tên project trơn được resolve qua UI project index trong `{DESIGN_ROOT}/README.md`.
- Nếu thiếu hoặc mơ hồ, liệt kê các project hiện có và dừng lại.
- Token thứ hai (tùy chọn) = stack hint, đổ vào field `stack` của README.

### 2. Guard

- **⛔ GATE CHECK**: `{DESIGN_ROOT}/<Group>/<Project>/` không được tồn tại sẵn. Nếu đã tồn tại, **dừng lại** và báo cáo project hiện có — không bao giờ ghi đè.

### 3. Scaffold

Tạo:

```
{DESIGN_ROOT}/<Group>/<Project>/
├── README.md               # từ Templates/ProjectReadme.md
├── UiInventory.md          # stub từ Templates/UiInventory.md
├── Tokens/
├── Components/
├── Screens/
├── Assets/
│   ├── Brand/
│   └── Screenshots/
├── Prompts/
└── Exports/
    └── ExportLog.md        # từ Templates/ExportLog.md
```

- Điền các placeholder của template README: `title`, `group`, `stack` (từ stack hint), `source_paths` — hỏi lại hoặc để placeholder nếu chưa rõ; `current_stage: 1-Scaffold`; stage 1 ✅ done, stage 2 🚧 in progress, các stage còn lại ⬜ pending.
- `UiInventory.md` là stub với các section của template để trống — file gate này có mặt ngay từ ngày đầu; stage 2 sẽ điền nội dung.

### 4. Đăng ký project

- Thêm dòng của project vào bảng UI project index trong `{DESIGN_ROOT}/README.md`, giữ nguyên định dạng bảng hiện có; Design status = "🚧 Scaffolded".

### 5. Báo cáo

Xuất ra cây thư mục vừa tạo, sau đó nêu bước tiếp theo: chạy `/design-inventory-ui <project>`.

**Chuỗi pipeline**: nếu skill này đang chạy như một bước trong chuỗi (được
gọi bởi `design-expert` hoặc `/feature-kickoff` cho cả project/flow, không
phải người dùng gõ trực tiếp đúng lệnh này) — tự động gọi tiếp bước "Bước
tiếp theo" nêu ở trên qua công cụ Skill ngay sau khi báo cáo xong, không
dừng lại chờ người dùng gõ lệnh kế tiếp. Nếu người dùng gọi trực tiếp một
mình skill này (không qua chuỗi), chỉ nêu gợi ý bước tiếp theo như trên,
không tự mở rộng phạm vi yêu cầu ban đầu.

## Guardrails

- Không bao giờ ghi đè file hoặc project đã có.
- Không tạo spec hay token ngay tại lúc scaffold — không được bịa nội dung.
- Bắt đầu từ đúng template tương ứng trong {DESIGN_ROOT}/Templates/ — giữ nguyên frontmatter key và thứ tự section.
- Mọi artifact vừa ghi đều mang frontmatter `status: "draft"`; không bao giờ set `approved`.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
