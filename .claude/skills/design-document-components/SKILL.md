---
name: "design-document-components"
description: "Document a project's UI components — COMPONENTS.md index plus one spec per component with Sources citations and 5-state tables. Refuses until the UiInventory census is populated."
argument-hint: "<project> [component] - e.g. 'PlatformManager Button' or 'PlatformManager KpiCard'"
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

Chuyển census UiInventory — và markup thật đứng sau nó — thành tài liệu component: một spec `Components/<Name>.md` cho mỗi component thật (anatomy, variant, bảng 5 state, trích dẫn `Sources:`, reference token) cộng với index `COMPONENTS.md` được dựng lại. Đây là stage 4 của design pipeline.

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

- Token đầu tiên của `$ARGUMENTS` = project — chấp nhận `<Group>/<Project>` (vd. `Frontend/PlatformManager`) hoặc tên project trơn được resolve qua UI project index trong `{DESIGN_ROOT}/README.md`. Token thứ hai tùy chọn = tên `[component]` để chỉ tài liệu hóa đúng một component đó.
- Nếu project thiếu hoặc mơ hồ, liệt kê các project hiện có từ index và **dừng lại**.

### 2. Kiểm tra gate

- **⛔ GATE CHECK**: `{DESIGN_ROOT}/<Group>/<Project>/UiInventory.md` phải tồn tại **và** bảng Screen Census của nó phải có dòng dữ liệu. Nếu một trong hai không đạt, **dừng lại** và báo rằng census phải làm trước: chạy `/design-inventory-ui <project>`.

### 3. Tài liệu hóa từng component

- Suy ra danh sách component từ các view trong census (hoặc lấy đúng theo argument `[component]` nếu chỉ có một).
- Với mỗi component:
  - Đọc markup thật đứng sau các view trong census có dùng nó.
  - **PlatformManager (từ 2026-08-22):** stack là **Angular 20 + PrimeNG + PrimeIcons v7**, không có Storybook. Verify anatomy/variant TRỰC TIẾP từ:
    - class global trong `src/FE/src/styles.scss` (`.card`, `.btn` + variant, `.badge`, `.action-btn`, `.field`/`.field-input`, `.filters`, `.tablewrap`, `.form-row`, `.toast-stack`);
    - template + SCSS scoped của chính component trong `src/FE/src/app/**`;
    - `src/FE/src/app/core/theme/platform-manager-preset.ts` cho phần PrimeNG.
    Data grid là **PrimeNG `p-table`** (có phân trang); hai ma trận phân quyền **cố ý hand-rolled `<table>`**. Shell (`Sidebar`/`Topbar`/`Toast`) và `AuthCard` là component thật cần spec.
    Không bao giờ ghi lại một variant không thấy trong markup/CSS thật.
  - Ghi lại anatomy, variant, và state — đủ cả 5: `default` / `hover` / `focus` / `active` / `disabled` — ĐÚNG NHƯ ĐANG CHẠY THẬT (một số state có thể chỉ tồn tại ngầm qua CSS `:hover`/`:active` selector hoặc `transform`/`opacity` — ghi đúng những gì CSS định nghĩa, không suy đoán thêm).
  - Trích dẫn đường dẫn file trong `Sources:` (kèm gợi ý số dòng).
  - Reference token — không bao giờ dùng giá trị raw khi đã có token tương ứng.
  - Ghi lại các điểm chưa hoàn hảo vào mục 'Normalize on redesign'.

### 4. Ghi spec & dựng lại index

- Ghi `Components/<Name>.md` từ phần B của `Templates/Components.md`, frontmatter `status: draft`.
- Dựng lại index `COMPONENTS.md` từ phần A của `Templates/Components.md`, giữ nguyên các mục đã đúng.
- Xác nhận mọi token reference resolve được với `Tokens/` — đánh dấu những cái không resolve được.

### 5. Báo cáo

- Cập nhật dashboard status trong `README.md` của project cho stage 4.
- Báo cáo số lượng component (mới / đã cập nhật / giữ nguyên), độ phủ state, và mọi token reference chưa resolve được. Bước tiếp theo: `/design-create-screens <project>`.

**Chuỗi pipeline**: nếu skill này đang chạy như một bước trong chuỗi (được
gọi bởi `design-expert` hoặc `/feature-kickoff` cho cả project/flow, không
phải người dùng gõ trực tiếp đúng lệnh này) — tự động gọi tiếp bước "Bước
tiếp theo" nêu ở trên qua công cụ Skill ngay sau khi báo cáo xong, không
dừng lại chờ người dùng gõ lệnh kế tiếp. Nếu người dùng gọi trực tiếp một
mình skill này (không qua chuỗi), chỉ nêu gợi ý bước tiếp theo như trên,
không tự mở rộng phạm vi yêu cầu ban đầu.

## Guardrails

- Ưu tiên mở rộng spec đã có thay vì tạo bản gần-trùng lặp.
- Component phải là thật (thấy được trong view của census) — không bao giờ tài liệu hóa một component không có view nào dùng.
- Bắt đầu từ đúng template tương ứng trong `{DESIGN_ROOT}/Templates/` — giữ nguyên frontmatter key và thứ tự section.
- Mọi artifact vừa ghi đều mang frontmatter `status: draft`; không bao giờ set `approved`.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
