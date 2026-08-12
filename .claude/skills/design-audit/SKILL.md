---
name: "design-audit"
description: "Validate a design project against the fidelity and completeness bar — assets, copy, blueprints, states, icons, lint, prompts and logs — writing AUDIT.md with PASS or BLOCKED plus fix commands."
argument-hint: "<project> - e.g. 'PlatformManager' or 'Frontend/PlatformManager'"
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

Audit toàn bộ tập artifact của một design project (`DESIGN.md`, `UiInventory.md`, `Tokens/`, `COMPONENTS.md` + `Components/`, `Screens/`, `Icons.md`, `Prompts/`, `Assets/`, `Exports/`, `README.md` của project) đối chiếu với bar về fidelity và độ hoàn thiện, rồi ghi `AUDIT.md` tại root của project với kết luận **PASS** hoặc **BLOCKED** cộng một lệnh fix cho mỗi finding. Đây là stage 7 của design pipeline.

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

### 2. Đọc toàn bộ artifact của project

- `README.md` của project, `DESIGN.md`, `UiInventory.md`, `COMPONENTS.md` + `Components/*.md`, `Tokens/*` (kể cả `tokens.json`), `Screens/*.md`, `Icons.md`, `Prompts/*-prompts.md`, `Assets/`, `Exports/`; cộng `{DESIGN_ROOT}/CLAUDE.md` để biết convention nào đang được enforce.

### 3. Kiểm tra các nhóm

Mỗi kiểm tra thất bại trở thành một dòng **Finding** kèm lệnh fix nêu tên skill `/design-*` chịu trách nhiệm:

- **(a) Inventory census** — census trong `UiInventory.md` đã điền đủ (mọi route/view đều được liệt kê) → `/design-inventory-ui`.
- **(b) Screenshot** — mọi dòng trong Screenshot Manifest hoặc đã capture hoặc `pending` kèm đúng hướng dẫn capture; pending thì được phép, thiếu hẳn dòng manifest thì không → `/design-inventory-ui`.
- **(c) Brand asset** — đã có trong manifest của inventory và file thực sự tồn tại trong `Assets/Brand/` → `/design-inventory-ui`.
- **(d) Screen spec** — mỗi spec có đủ 7 section bắt buộc, và Layout Blueprint là cây layout thật chứ không phải chỉ liệt kê component trơn → `/design-create-screens`.
- **(e) Copy** — nguyên văn đúng như đang chạy thật, kèm trích dẫn nguồn → `/design-create-screens`.
- **(f) Sử dụng logo** — rule về cách dùng logo được nêu ở mọi nơi logo xuất hiện (nếu project có logo) → `/design-document-components`.
- **(g) States** — phủ đủ kể cả error / empty / validation → `/design-create-screens`.
- **(h) Responsive** — có hành vi responsive cho từng screen → `/design-create-screens`.
- **(i) Icons** — Per-Action Map trong `Icons.md` phủ đủ mọi action đã spec; ngoại lệ legacy phải khai báo rõ, không được âm thầm bỏ qua → `/design-document-components`.
- **(j) Bảng màu chart** — có mặt ở nơi app thực sự có chart, hoặc khai báo rõ "None" → `/design-extract-tokens`.
- **(k) Dấu hiệu fidelity** — có blockquote fidelity + danh sách "Normalize on redesign" → `/design-extract-tokens` (DESIGN.md) / `/design-create-screens` (screens).
- **(l) Lint** — `DESIGN.md` lint 0 lỗi → `/design-extract-tokens`. Chạy:

```bash
npx --yes --package=@google/design.md designmd lint <path-to-DESIGN.md>
```

> ⚠️ Dạng rút gọn thường thấy `npx @google/design.md lint` **fail âm thầm trên Windows** (exit 1, không có output — do shim đặt tên bin `.md`); luôn dùng dạng `--package=...designmd`.

  Gate lint = 0 lỗi; warning ghi nhận thành finding dạng info, không chặn (fact về contrast đúng như app đang chạy).

- **(m) Export log** — `Exports/ExportLog.md` tồn tại → `/design-new-project`.
- **(n) Dashboard** — dashboard stage trong `README.md` của project khớp với trạng thái artifact thực tế → chạy lại skill `/design-*` của stage đang lệch.

### 4. Ghi AUDIT.md (→ root project, từ `Templates/AuditReport.md`)

- Kết quả **PASS** (không có finding chặn nào) hoặc **BLOCKED** (các dòng Finding + lệnh fix; không bao giờ làm nhẹ một kiểm tra đã thất bại).
- Một dòng Finding cho mỗi kiểm tra thất bại: category, bằng chứng (file + thiếu gì), lệnh fix.
- Frontmatter: `status: draft`.

### 5. Báo cáo

Kết luận cộng số lượng (finding chặn / info). Nếu **PASS**: bước tiếp theo là `/design-export-figma <project>`. Nếu **BLOCKED**: liệt kê lệnh fix, mỗi finding một dòng.

## Guardrails

- Không bao giờ làm nhẹ một kiểm tra đã thất bại để nó pass.
- `AUDIT.md` luôn giữ `status: draft` — không bao giờ set `approved`.
- Audit chỉ ghi `AUDIT.md` — không bao giờ "sửa" các artifact khác để ép pass; chỉ báo cáo finding.
- Bắt đầu từ đúng template tương ứng trong `{DESIGN_ROOT}/Templates/` — giữ nguyên frontmatter key và thứ tự section.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
