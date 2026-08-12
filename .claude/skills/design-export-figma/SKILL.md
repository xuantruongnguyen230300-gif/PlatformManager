---
name: "design-export-figma"
description: "Export an audited design project to Figma — tokens, brand assets and screens via the Figma MCP — logging proof in Exports/ExportLog.md. Refuses unless the latest AUDIT.md is PASS and DESIGN.md lints error-free."
argument-hint: "<project> [scope] - e.g. 'PlatformManager' or 'PlatformManager tokens'"
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

Đẩy một design project đã audit vào Figma — token variable từ `Tokens/tokens.json`, ảnh brand từ `Assets/Brand/`, và screen dựng từ Layout Blueprint trong `Screens/` — rồi ghi lại bằng chứng export có thể kiểm chứng vào `Exports/ExportLog.md`. Đây là stage 8 của design pipeline.

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

- Token đầu tiên của `$ARGUMENTS` là project — chấp nhận `<Group>/<Project>` (vd. `Frontend/PlatformManager`) hoặc tên project trơn được resolve qua UI project index trong `{DESIGN_ROOT}/README.md`.
- Nếu thiếu hoặc mơ hồ, liệt kê các project hiện có và dừng lại.
- Token thứ hai tùy chọn = scope: một prefix screen-flow (vd. `01-dashboard`) để giới hạn phạm vi export; mặc định là toàn bộ project.

### 2. Kiểm tra gate

- **⛔ GATE CHECK**: `AUDIT.md` phải tồn tại tại root của project với kết quả **PASS**, **và** một lần chạy lint mới nhất phải trả về 0 lỗi. Nếu một trong hai không đạt, **dừng lại** và báo cáo — trỏ tới `/design-audit <project>` và không đụng vào Figma.

```bash
npx --yes --package=@google/design.md designmd lint <path-to-DESIGN.md>
```

> ⚠️ Dạng rút gọn thường thấy `npx @google/design.md lint` **fail âm thầm trên Windows** (exit 1, không có output — do shim đặt tên bin `.md`); luôn dùng dạng `--package=...designmd`. Gate lint = 0 lỗi; warning chỉ ghi nhận, không chặn (fact về contrast đúng như app đang chạy).

### 3. Export

Hai route được hỗ trợ — ghi lại route nào đã dùng vào log:

- **Route A — thủ công / chuẩn**: import `Tokens/tokens.json` qua plugin Figma **Tokens Studio** (set: `global` + đúng **một** theme), sau đó dùng tính năng export Figma sẵn có của tool sinh design (Google Stitch — dùng thủ công qua stitch.withgoogle.com vì repo này chưa cấu hình Stitch MCP), map frame đã export về đúng spec trong `Components/` theo tỉ lệ 1:1.
- **Route B — trực tiếp qua Figma MCP** (`figma` server trong `.mcp.json`): **BẮT BUỘC** — load skill `/figma-use` **trước** mọi lệnh `use_figma` (cộng `/figma-generate-design` khi dựng screen, `/figma-generate-library` cho variable/component); các skill này do chính Figma MCP server cung cấp một khi đã kết nối. Sau đó:
  1. Tạo mới hoặc nhắm vào file Figma đích.
  2. Sync token variable từ `tokens.json`.
  3. Upload ảnh trong `Assets/Brand/`.
  4. Dựng các screen trong phạm vi scope theo đúng Layout Blueprint, **chỉ** dùng component và token đã tài liệu hóa.

### 4. Ghi log export (→ `Exports/ExportLog.md`)

- Thêm một entry: ngày, URL/key Figma, scope, hash của `tokens.json` (vd. `git hash-object Tokens/tokens.json`), trạng thái lint + audit, ghi chú.
- Cập nhật dashboard `README.md` của project: stage 8.

### 5. Báo cáo

Mở đầu bằng những gì đã export và ở đâu (đường dẫn file, URL/key Figma, route đã dùng, scope). Kết thúc bằng mục **"🎨 Handoff & Export"** (khớp định dạng output của agent `design-expert`): link Figma cho designer, entry trong `Exports/ExportLog.md`, và bàn giao cho dev (spec trích dẫn live source 1:1). Pipeline đã hoàn tất cho scope này; trước bất kỳ lần re-export nào trong tương lai, chạy lại `/design-audit <project>`.

## Guardrails

- Không bao giờ đẩy nội dung chưa audit lên Figma — gate của stage 7 là tuyệt đối, kể cả với scope "nhỏ".
- Không bao giờ bỏ qua bước load skill `/figma-use` trước một lệnh `use_figma` (cũng như các Figma skill tương ứng khác cho tool của chúng).
- Token trong Figma phải đến từ `tokens.json`, không phải giá trị tùy tiện — cũng không hardcode màu/kích thước bên trong Figma.
- Bắt đầu từ đúng template tương ứng trong `{DESIGN_ROOT}/Templates/` — giữ nguyên frontmatter key và thứ tự section.
- Mọi artifact vừa ghi đều giữ frontmatter `status: draft` — không bao giờ set `approved`.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
