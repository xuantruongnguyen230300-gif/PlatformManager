---
name: "design-extract-tokens"
description: "Extract real design tokens from the live source into Tokens/*, tokens.json (W3C DTCG) and the DESIGN.md frontmatter, then lint DESIGN.md until error-free. Refuses without a UiInventory."
argument-hint: "<project> [category] - e.g. 'PlatformManager colors' or 'PlatformManager typography'"
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

Trích xuất token design thật của project từ live source (vị trí theo từng stack bên dưới) vào `Tokens/colors.md`, `Tokens/typography.md`, `Tokens/spacing.md`, file `Tokens/tokens.json` (W3C DTCG) import được vào Tokens Studio, và frontmatter token của `DESIGN.md` — sau đó lint `DESIGN.md` tới khi hết lỗi. Đây là stage 3 của design pipeline.

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
> Live source của từng project lấy từ chính `source_paths` trong `README.md` của project đó, không giả
> định theo một framework marker cụ thể.

## Các bước thực hiện

### 1. Resolve project

- Token đầu tiên của `$ARGUMENTS` = project — chấp nhận `<Group>/<Project>` (vd. `Frontend/PlatformManager`) hoặc tên project trơn được resolve qua UI project index trong `{DESIGN_ROOT}/README.md`. Token thứ hai tùy chọn = `[category]` (`colors` | `typography` | `spacing`) để giới hạn phạm vi extract.
- Nếu project thiếu hoặc mơ hồ, liệt kê các project hiện có từ index và **dừng lại**.

### 2. Kiểm tra gate

- **⛔ GATE CHECK**: `{DESIGN_ROOT}/<Group>/<Project>/UiInventory.md` phải tồn tại. Nếu thiếu, **dừng lại** và báo rằng UI census phải làm trước: chạy `/design-inventory-ui <project>`.

### 3. Trích xuất từ live source

Token luôn lấy từ live source, không bao giờ dựa vào trí nhớ hay screenshot. Xác định vị trí theo stack (từ project index / README):

| Stack | Token thật nằm ở đâu |
| --- | --- |
| **PlatformManager** | **Nguồn token: `doc/Design/Frontend/PlatformManager/Tokens/`**, code đuổi theo — chiều đã chốt, đọc `doc/huong_dan/wiki-core/fe/04-design-token-system.md` §Chiều. Khi trích từ live source: **phải đọc đủ MỌI nơi khai màu**, không chỉ một — danh sách nơi khai và bẫy "sửa một nơi quên nơi kia" nằm ở `doc/huong_dan/wiki-core/fe/04-design-token-system.md` §"Mọi nơi khai màu phải khớp nhau" + `doc/Design/CLAUDE.md` Core Principle 4; **đừng dựng lại danh sách đó ở đây** — bản chép sẽ thiếu một file ngay lần code thêm nơi khai thứ tư. Cộng SCSS scoped của component để bắt token dùng-mà-chưa-khai. Tên token/global class **đọc từ nguồn rồi ghi**, đừng chép từ đây hay từ lượt trước. **KHÔNG có** Style Dictionary hay pipeline sinh token — đừng đi tìm. Chart: kiểm `src/FE/package.json` và grep `chart` trong `src/FE/src/app` rồi hãy ghi, đừng mặc định `None`. |

- Trích xuất thêm bảng màu chart từ nơi app vẽ chart. Đừng mặc định `None` — kiểm `src/FE/package.json` và grep `chart` trong `src/FE/src/app` trước, và đối chiếu `Tokens/colors.md` hiện có.
- Nếu có truyền `[category]`, chỉ extract đúng category đó; giữ nguyên các file còn lại.

### 4. Ghi artifact & lint

- Ghi `Tokens/colors.md`, `Tokens/typography.md`, `Tokens/spacing.md` từ `Templates/Tokens.md`.
- Ghi `Tokens/tokens.json` — chuẩn W3C DTCG (`$type` / `$value`), các token set `global` + `light` + `dark`, import được qua plugin Figma Tokens Studio.
- Ghi `DESIGN.md` từ `Templates/DesignMd.md`, hoặc cập nhật tại chỗ phần frontmatter nếu file đã tồn tại.
- Artifact markdown mang frontmatter `status: draft`.
- Lint `DESIGN.md` và sửa tới khi 0 lỗi:

```bash
npx --yes --package=@google/design.md designmd lint {DESIGN_ROOT}/<Group>/<Project>/DESIGN.md
```

> ⚠️ Dạng rút gọn thường thấy `npx @google/design.md lint` FAIL ÂM THẦM trên Windows (exit 1, không có output — do shim đặt tên bin `.md`); luôn dùng dạng `--package=...designmd`. Gate lint = 0 lỗi; warning chỉ ghi nhận, không chặn (fact về contrast đúng như app đang chạy).

### 5. Báo cáo

- Cập nhật dashboard status trong `README.md` của project cho stage 3.
- Báo cáo số lượng token theo từng category (+ bảng màu chart) và tóm tắt lint (lỗi đã sửa về 0, warning đã ghi nhận). Bước tiếp theo: `/design-document-components <project>`.

**Chuỗi pipeline**: nếu skill này đang chạy như một bước trong chuỗi (được
gọi bởi `design-expert` hoặc `/feature-kickoff` cho cả project/flow, không
phải người dùng gõ trực tiếp đúng lệnh này) — tự động gọi tiếp bước "Bước
tiếp theo" nêu ở trên qua công cụ Skill ngay sau khi báo cáo xong, không
dừng lại chờ người dùng gõ lệnh kế tiếp. Nếu người dùng gọi trực tiếp một
mình skill này (không qua chuỗi), chỉ nêu gợi ý bước tiếp theo như trên,
không tự mở rộng phạm vi yêu cầu ban đầu.

## Guardrails

- Không bao giờ bịa giá trị token — mọi giá trị phải truy được về đúng dòng trong live source.
- Extraction chỉ đọc trên app thật — THAY ĐỔI token là một task tường minh riêng, bắt đầu từ live source.
- Bắt đầu từ đúng template tương ứng trong `{DESIGN_ROOT}/Templates/` — giữ nguyên frontmatter key và thứ tự section.
- Mọi artifact vừa ghi đều mang frontmatter `status: draft`; không bao giờ set `approved`.
- Ghi lại đúng pixel như đang chạy thật (copy thật, asset thật, kể cả những điểm chưa hoàn hảo); mọi sai khác mong muốn phải đưa vào mục 'Normalize on redesign' — không bao giờ âm thầm lý tưởng hóa.
- Toàn bộ artifact trong `{DESIGN_ROOT}/` phải viết bằng tiếng Anh (xem `{DESIGN_ROOT}/CLAUDE.md`).
