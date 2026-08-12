---
name: "design-inventory-ui"
description: "Census a design project's live app — routes, views, layouts, copy sources and brand assets — into UiInventory.md, capturing screenshots when the target is reachable. Refuses if the project is not scaffolded."
argument-hint: "<project> [scope] - e.g. 'PlatformManager' or 'PlatformManager dashboard only'"
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

Census source của app thật (`source_paths` lấy từ README của project) — toàn bộ route/section, view, layout, nguồn copy và brand asset được tham chiếu, cộng screenshot mỗi khi target phản hồi — vào `UiInventory.md`, file gate mà mọi stage pipeline sau đó dựa vào. Đây là stage 2 của design pipeline.

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
> Live source của từng project lấy từ chính `source_paths` trong `README.md` của project đó.

## Các bước thực hiện

### 1. Resolve project

- Token đầu tiên của `$ARGUMENTS` = project — chấp nhận `<Group>/<Project>` hoặc tên project trơn được resolve qua UI project index trong `{DESIGN_ROOT}/README.md`. Nếu thiếu hoặc mơ hồ, liệt kê các project hiện có và **dừng lại**.
- Các token còn lại = scope tùy chọn (vd. chỉ một flow) — giới hạn census vào đó và ghi chú scope trong `UiInventory.md`.

### 2. Guard

- **⛔ GATE CHECK**: `{DESIGN_ROOT}/<Group>/<Project>/` và `README.md` của nó phải tồn tại. Nếu thiếu một trong hai, **dừng lại** và báo rằng project phải được scaffold trước bằng `/design-new-project`.

### 3. Census live source

Đọc `source_paths` từ README của project, sau đó liệt kê trực tiếp từ code — không bao giờ dựa vào trí nhớ:

- **Route & view** — route/page/section theo từng stack. **PlatformManager (hiện tại):** không có framework/router — `doc/Prototype/dashboard.html` là một file HTML tĩnh duy nhất; "route" ở đây là các section/anchor có tên trong file (topbar, KPI grid, bảng theo dõi, dialog báo cáo, v.v.), không phải URL riêng biệt. Khi `src/FE/` có app thật, cập nhật lại theo router thật của framework đó.
- **Layout & shell** — layout dùng chung, master page, app shell (với PlatformManager: cấu trúc `.topbar` / `main` / `.layout` trong cùng file).
- **Nguồn copy** — nguồn localization. **PlatformManager:** chưa có framework i18n nào — toàn bộ copy là tiếng Việt hardcode trực tiếp trong `.html`; đọc template để lấy copy verbatim.
- **Brand asset** — mọi ảnh mà UI tham chiếu; copy các ảnh brand được tham chiếu vào `Assets/Brand/` **giữ nguyên tên file gốc** và ghi một dòng manifest cho từng ảnh. PlatformManager hiện không tham chiếu ảnh brand nào — ghi rõ "None yet" nếu đúng vậy, đừng bịa ra.

### 4. Screenshot

- Probe target: với file tĩnh, mở trực tiếp bằng chrome-devtools MCP tools (`new_page` trỏ vào đường dẫn `file://` của `doc/Prototype/dashboard.html`, `take_screenshot`); với app có dev server, dùng `navigate_page` tới dev URL trước. Chụp vào `Assets/Screenshots/<flow-stem>/`, đặt tên `<view>[--state][--viewport].png` (viewport mặc định = `desktop-1440`).
- Nếu KHÔNG chụp được, ghi dòng "pending" trong Screenshot Manifest kèm đúng lệnh khởi chạy/đường dẫn — **không bao giờ block vì screenshot**.

### 5. Ghi UiInventory.md

- Khởi tạo từ `Templates/UiInventory.md`: các bảng census, manifest brand asset, manifest screenshot và danh sách "Normalize on redesign" cấp project. Frontmatter `status: "draft"`.
- Cập nhật dashboard README của project: stage 2 ✅ done, stage 3 🚧 in progress.

### 6. Báo cáo

Số lượng (route/view, layout, nguồn copy, brand asset, screenshot đã chụp so với còn pending), sau đó nêu bước tiếp theo: chạy `/design-extract-tokens <project>`.

## Guardrails

- Không bao giờ bỏ qua gate scaffold.
- Chỉ copy FILE asset — không bao giờ sửa bất cứ gì bên trong cây source của app thật.
- Ghi lại đúng những gì ĐANG CÓ, không phải những gì nên có.
- Bắt đầu từ đúng template tương ứng trong {DESIGN_ROOT}/Templates/ — giữ nguyên frontmatter key và thứ tự section.
- Mọi artifact vừa ghi đều mang frontmatter `status: "draft"`; không bao giờ set `approved`.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
