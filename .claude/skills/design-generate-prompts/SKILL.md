---
name: "design-generate-prompts"
description: "Generate a flow's multi-tool prompt pack (Google Stitch, Claude Design, Google AI Studio, generic) with tokens resolved to literal values and verbatim copy. Refuses while the flow's screen spec is missing mandatory sections."
argument-hint: "<project> <flow> - e.g. 'PlatformManager 01-dashboard'"
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

Tổng hợp screen spec của một flow (`Screens/<flow>.md`), giá trị token (`Tokens/tokens.json`), và screenshot thành một prompt pack đa công cụ trong `Prompts/<flow>-prompts.md` — một master prompt cho mỗi screen với mọi token đã resolve về giá trị literal và copy giữ nguyên văn, thích ứng cho Google Stitch, Claude Design, Google AI Studio, và tool generic. Đây là stage 6 của design pipeline.

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
- Token thứ hai = flow = phần stem tên file trong `Screens/` (vd. `01-dashboard` → `Screens/01-dashboard.md`). Nếu thiếu hoặc không khớp file nào, liệt kê các stem `Screens/*.md` hiện có và **dừng lại**.

### 2. Kiểm tra gate

- **⛔ GATE CHECK**: `Screens/<flow>.md` phải tồn tại và **mọi** block screen trong đó phải chứa đủ 7 section H3 bắt buộc — `### Layout Blueprint`, `### Copy`, `### States`, `### Responsive`, `### Iconography`, `### Screenshots`, `### Normalize on redesign`. Nếu thiếu bất kỳ section nào, **dừng lại** và báo cáo rõ cặp screen/section còn thiếu — chạy lại `/design-create-screens <project> <flow>` trước. Không sinh gì cả nếu chưa đủ điều kiện.

### 3. Dựng master prompt cho từng screen

- Resolve **mọi** token reference về giá trị literal (hex / px / tên font) từ `Tokens/tokens.json` — tool bên ngoài không nội suy được token reference.
- Nhúng Layout Blueprint dưới dạng văn xuôi, copy **nguyên văn**, state, và hành vi responsive theo từng breakpoint.
- Mở đầu mọi master prompt bằng: "Recreate this exact shipped screen — do not idealize."

### 4. Ghi prompt pack

Ghi `Prompts/<flow>-prompts.md` từ `Templates/PromptPack.md`, frontmatter `status: "draft"`, với 4 section theo từng tool cộng danh sách **Assets to Attach** (screenshot từ `Assets/Screenshots/<flow>/`, `Tokens/tokens.json`, `DESIGN.md`):

- **Google Stitch** — import `DESIGN.md` đã lint sạch trước, sau đó tới các screen prompt. Verify nó lint sạch trước khi tham chiếu:

  ```bash
  npx --yes --package=@google/design.md designmd lint <path-to-DESIGN.md>
  ```

  ⚠️ Dạng rút gọn thường thấy `npx @google/design.md lint` **fail âm thầm** trên Windows (exit 1, không có output — do shim đặt tên bin `.md`); luôn dùng dạng `--package=…designmd`. Gate lint = 0 lỗi; warning chỉ ghi nhận, không chặn (fact về contrast đúng như app đang chạy). Repo này chưa cấu hình Stitch MCP — import thủ công qua stitch.withgoogle.com (xem `doc/Design/SETUP.md` để thêm MCP nếu muốn tự động hóa).
- **Claude Design** — đính kèm screenshot + file brand; cung cấp token dưới dạng CSS custom property.
- **Google AI Studio** — system instruction = token + rule fidelity; user prompt = layout + copy; screenshot là image part.
- **Generic** — master prompt nguyên văn.

Screenshot còn "pending" trong Screenshot Manifest của UiInventory được liệt kê là pending trong Assets to Attach — không bao giờ block vì chúng.

### 5. Báo cáo

- Cập nhật dashboard status README của project: stage 6.
- Báo cáo các screen đã cover, token đã resolve, và mọi screenshot pending được mang vào danh sách Assets to Attach.
- Bước tiếp theo: `/design-audit <project>`.

**Chuỗi pipeline**: nếu skill này đang chạy như một bước trong chuỗi (được
gọi bởi `design-expert` hoặc `/feature-kickoff` cho cả project/flow, không
phải người dùng gõ trực tiếp đúng lệnh này) — tự động gọi tiếp bước "Bước
tiếp theo" nêu ở trên qua công cụ Skill ngay sau khi báo cáo xong, không
dừng lại chờ người dùng gõ lệnh kế tiếp. Nếu người dùng gọi trực tiếp một
mình skill này (không qua chuỗi), chỉ nêu gợi ý bước tiếp theo như trên,
không tự mở rộng phạm vi yêu cầu ban đầu.

## Guardrails

- Prompt không được tham chiếu đường dẫn file mà tool bên ngoài không mở được — inline mọi thứ hoặc nêu tên trong danh sách Assets to Attach.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Bắt đầu từ đúng template tương ứng trong {DESIGN_ROOT}/Templates/ — giữ nguyên frontmatter key và thứ tự section.
- Mọi artifact vừa ghi đều mang frontmatter `status: "draft"`; không bao giờ set `approved`.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
