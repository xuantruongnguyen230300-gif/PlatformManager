---
updated: "2026-08-11"
---

# Setup Guide — Design → Figma Pipeline

Hướng dẫn setup môi trường một lần cho pipeline thiết kế docs-as-code
(`AI → Stitch → Figma → Feature`) vừa được thêm vào repo này. Xem
[README.md](./README.md) để biết cấu trúc/quy ước, và
[CLAUDE.md](./CLAUDE.md) để biết chi tiết convention mà AI phải tuân theo.

## Pipeline này là gì

8 skill `/design-*` (cộng agent `design-expert`) chạy nối tiếp nhau, mỗi
skill có gate riêng: từ scaffold project, census UI thật, trích token, tài
liệu hóa component, viết screen spec, sinh prompt pack, audit, tới cuối cùng
là **export sang Figma**. Toàn bộ artifact nằm dưới `doc/Design/` và luôn
bám vào **source thật** (không bịa) — xem "Fidelity Policy" trong
`CLAUDE.md`.

Pipeline này được đưa vào từ một dự án tham chiếu (`VNR.Successor`) và điều
chỉnh lại cho quy mô của `PlatformManager` — hiện chỉ có một project
(`Frontend/PlatformManager`) trỏ vào prototype tĩnh
`doc/Prototype/dashboard.html`, vì `src/FE/` và `src/BE/` còn đang rỗng.

## 1. Yêu cầu môi trường

- **Node.js + npx** trên PATH — dùng để lint `DESIGN.md`:
  ```bash
  npx --yes --package=@google/design.md designmd lint <path-to-DESIGN.md>
  ```
  ⚠️ Trên Windows, dạng rút gọn `npx @google/design.md lint <path>` **fail âm
  thầm** (exit code 1, không in gì) — luôn dùng dạng đầy đủ
  `--package=@google/design.md designmd` như trên.
- **Một tài khoản Figma** có quyền tạo/sửa file trong team bạn định export
  vào — không cần API key thủ công, xem mục 2.
- Claude Code (hoặc client tương thích MCP) đã trỏ tới thư mục repo này.

## 2. MCP servers (`.mcp.json`)

File `.mcp.json` ở root repo đã khai 2 server:

```json
{
  "mcpServers": {
    "figma": { "type": "http", "url": "https://mcp.figma.com/mcp" },
    "chrome-devtools-mcp": { "type": "stdio", "command": "npx", "args": ["-y", "chrome-devtools-mcp@latest"] }
  }
}
```

- **`figma`** — remote MCP chính thức của Figma. **Không cần cấu hình API
  key trước** — lần đầu tiên một tool `use_figma` được gọi, client sẽ mở
  luồng OAuth để bạn đăng nhập/ủy quyền tài khoản Figma. Sau đó phiên làm
  việc dùng lại token đó.
- **`chrome-devtools-mcp`** — chạy qua `npx` (tự tải lần đầu), dùng ở stage 2
  và 5 để chụp screenshot màn hình khi có thể mở được (kể cả file tĩnh qua
  `file://`, không nhất thiết cần dev server).

Sau khi mở lại Claude Code trong thư mục này, kiểm tra 2 server đã kết nối
(vd. lệnh `/mcp` hoặc tương đương trong client bạn dùng — tên lệnh có thể
khác tùy client).

### Thêm Stitch MCP (tùy chọn, chưa cấu hình sẵn)

Route A của `/design-export-figma` dùng tính năng export Figma có sẵn của
Google Stitch. Repo này chưa bật Stitch qua MCP — dùng thủ công qua
stitch.withgoogle.com, hoặc thêm server sau vào `.mcp.json` nếu muốn tự động
hóa (cần `STITCH_API_KEY`, lấy tại
[Google AI Studio / Stitch settings](https://stitch.withgoogle.com)):

```json
"stitch": {
  "type": "http",
  "url": "https://stitch.googleapis.com/mcp",
  "headers": {
    "Accept": "application/json",
    "X-Goog-Api-Key": "${STITCH_API_KEY:-MISSING_STITCH_API_KEY_SET_IT_IN_DOTENV}"
  }
}
```

Đặt `STITCH_API_KEY` trong file `.env` (không commit) hoặc biến môi trường
trước khi mở phiên Claude Code.

## 3. Cấu trúc đã được scaffold sẵn

```
doc/Design/
├── CLAUDE.md              # convention AI phải tuân theo
├── README.md               # index project + tóm tắt 8-stage workflow
├── SETUP.md                 # chính là file này
├── Templates/                # 10 template gốc cho mọi artifact
└── Frontend/
    └── PlatformManager/       # đã chạy xong stage 1 (scaffold)
        ├── README.md           # source_paths: doc/Prototype/dashboard.html
        └── UiInventory.md       # stub — sẽ điền ở stage 2
```

`.claude/agents/design-expert.md` và 8 file
`.claude/skills/design-*/SKILL.md` đã được thêm — không cần cài đặt gì
thêm, Claude Code tự nhận diện thư mục `.claude/skills/`.

## 4. Chạy pipeline

Từ root repo, gọi lần lượt (mỗi skill tự kiểm tra gate của mình, sẽ báo lỗi
rõ ràng nếu bước trước chưa xong):

```
/design-inventory-ui PlatformManager
/design-extract-tokens PlatformManager
/design-document-components PlatformManager
/design-create-screens PlatformManager
/design-generate-prompts PlatformManager <flow>
/design-audit PlatformManager
/design-export-figma PlatformManager
```

Hoặc chỉ cần nói với Claude Code những gì bạn muốn ("đưa dashboard này lên
Figma") — agent `design-expert` sẽ tự điều hướng tới đúng skill theo stage
hiện tại của project.

Bước 1 (`/design-new-project`) đã chạy sẵn cho `Frontend/PlatformManager`
trong lần setup này — không cần chạy lại trừ khi bạn muốn thêm một project
khác (vd. `Backend/Api` khi `src/BE/` có app thật).

## 5. Ghi chú "greenfield"

`src/FE/` và `src/BE/` hiện đang rỗng — chưa chọn framework. Pipeline vẫn
chạy được bằng cách coi `doc/Prototype/dashboard.html` là "live source" tạm
thời (xem carve-out trong `CLAUDE.md` § Fidelity Policy). Khi một app thật
xuất hiện trong `src/FE/`:

1. Cập nhật `source_paths` trong
   `doc/Design/Frontend/PlatformManager/README.md` để trỏ vào đó.
2. Chạy lại `/design-inventory-ui PlatformManager` để làm mới census.
3. Các stage sau tự động dùng lại nguồn mới khi bạn chạy lại chúng.

## 6. Khác biệt so với dự án tham chiếu (VNR.Successor)

- Không có tích hợp BA/TFS (không có `US-xxx` work item, không có luồng
  "reverse intake" từ BusinessAnalysis) — repo này chưa có quy trình đó.
- Chỉ một project duy nhất (`Frontend/PlatformManager`) thay vì nhiều app
  trong một workspace nhiều repo.
- `{FE_ROOT}`/`{BE_ROOT}` không cố định theo marker framework (không có
  `angular.json`/`*.sln`) — mỗi project tự khai `source_paths` trong
  `README.md` của nó.
- Không cấu hình sẵn Stitch MCP hay Postgres MCP (không liên quan tới pipeline
  Figma của repo này).
