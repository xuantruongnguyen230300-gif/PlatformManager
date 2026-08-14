# Hướng dẫn: Tri thức của agent frontend-expert / backend-expert nạp từ đâu

Repo này có 2 agent chuyên gia **xây code** — `frontend-expert` (Angular 20,
`src/FE/`) và `backend-expert` (.NET Clean Architecture + CQRS, `src/BE/`) —
cùng cơ chế bàn giao API Contract Card giữa hai bên. Cả hai được tạo **trước
khi có code thật**, để định hướng đúng ngay từ dòng code đầu tiên thay vì
phải tái cấu trúc sau này.

Bổ sung sau đó là agent thứ 3 — `core-reviewer` — **không xây code**, chỉ
kiểm toán độc lập phần "core" của cả 2 vùng; xem mục
[Agent thứ 3](#agent-thứ-3--core-reviewer-kiểm-toán-độc-lập-phần-core) ở
cuối tài liệu.

## Tri thức nằm ở đâu — 3 lớp

```
.claude/
├── agents/
│   ├── frontend-expert.md   # Lớp 1: vai trò, phạm vi, quy tắc bàn giao, khi nào dừng hỏi
│   └── backend-expert.md
└── skills/
    ├── frontend-expert/SKILL.md   # Lớp 3: điểm gọi vào — xem mục dưới
    └── backend-expert/SKILL.md

src/FE/
├── CLAUDE.md                      # Lớp 2: mục lục — trỏ tới các file docs/ theo chủ đề
└── .claude/docs/
    ├── architecture.md            # tầng core/modules/shared, cấu trúc 1 feature
    ├── api-client.md              # ranh giới DTO/model, mapper, gọi API
    └── ui-conventions.md          # Angular 20 control flow, signal, style/token

src/BE/
├── CLAUDE.md                      # Lớp 2: mục lục — trỏ tới các file rules/ theo chủ đề
└── .claude/rules/
    ├── architecture.md            # layer rule, dependency direction, vertical slice
    ├── entity-domain.md           # base entity, factory method, Value Object
    ├── cqrs-handler.md            # Command/Query/Handler, Result<T>, validator
    └── api-controller.md          # controller, envelope response, error → HTTP
```

**Lớp 1 — `.claude/agents/*.md`**: agent đọc file này đầu tiên trong mọi
task. Chứa vai trò, cách resolve `{FE_ROOT}`/`{BE_ROOT}`, danh sách "đọc bắt
buộc" trỏ tới Lớp 2, cơ chế bàn giao Contract Card, và checklist "dừng lại
hỏi người dùng khi...". **Đây là nơi duy nhất mô tả quy trình/hành vi** —
không lặp lại chi tiết kỹ thuật đã có ở Lớp 2.

**Lớp 2 — `src/FE/CLAUDE.md` và `src/BE/CLAUDE.md`**: mục lục kiến trúc của
từng vùng code, **nằm ngay trong chính vùng đó** (không nằm ở workspace
root) — lý do: khi vùng này lớn lên và có thể tách thành repo riêng, toàn bộ
tri thức đi theo nó, không phải sửa đường dẫn ở nơi khác. File này ngắn, chỉ
liệt kê stack + trỏ tới file chi tiết trong `.claude/docs/` (FE) hoặc
`.claude/rules/` (BE).

**Lớp 3 — chi tiết kỹ thuật theo chủ đề** (`.claude/docs/*.md` hoặc
`.claude/rules/*.md`): nội dung thật — quy ước cụ thể, ví dụ code, quyết
định kiến trúc. Đọc **đúng file của chủ đề đang làm**, không cần đọc hết mọi
file cho mọi task.

## Vì sao tách 3 lớp thay vì gộp vào 1 file agent (như VNR.Successor làm)

Repo tham chiếu ban đầu (`VNR.Successor` — xem
[setup-figma-tai-khoan-moi.md](./setup-figma-tai-khoan-moi.md) và
`doc/Design/SETUP.md` để biết bối cảnh dự án đó) gộp toàn bộ tri thức
(~1500 dòng) trực tiếp vào 2 file agent, vì agent đó mô tả một codebase
**đã tồn tại** với vô số sự thật đo được (số dòng, bug đã biết, ArchTests).
PlatformManager đang ở giai đoạn khởi tạo — tách tri thức ra khỏi agent và
đặt cạnh code nó mô tả (`src/FE/`, `src/BE/`) giúp:

1. Agent file (Lớp 1) ngắn, dễ đọc, tập trung vào **quy trình** — ít phải
   sửa khi chi tiết kỹ thuật đổi.
2. Tri thức kỹ thuật (Lớp 2+3) nằm cạnh code nó chi phối — ai mở `src/FE/`
   cũng thấy `CLAUDE.md` ngay, không cần biết agent tồn tại.
3. Khi framework đổi phiên bản (Angular 20 → 21, .NET version mới) chỉ cần
   sửa Lớp 2/3, không đụng vào cơ chế bàn giao/quy trình ở Lớp 1.

## Cách gọi 2 agent này

Hai skill mỏng đóng vai trò **điểm vào** — gọi `/frontend-expert <việc cần
làm>` hoặc `/backend-expert <việc cần làm>`, skill sẽ chuyển giao (delegate)
cho đúng subagent qua công cụ Agent:

```
.claude/skills/frontend-expert/SKILL.md   → subagent "frontend-expert"
.claude/skills/backend-expert/SKILL.md    → subagent "backend-expert"
```

Vì mô tả (`description`) trong `.claude/agents/*.md` đã ghi rõ "Dùng
PROACTIVELY cho mọi việc chạm tới src/FE (hoặc src/BE)", agent cũng có thể
tự được kích hoạt khi bạn yêu cầu trực tiếp một việc thuộc phạm vi đó mà
không cần gõ đúng tên skill.

## Khi nào cần cập nhật tri thức này

- **Chốt version .NET cụ thể** khi scaffold backend lần đầu → cập nhật
  `src/BE/CLAUDE.md` § Stack.
- **Chốt thư viện i18n cho FE** (nếu cần đa ngôn ngữ) → cập nhật
  `src/FE/.claude/docs/ui-conventions.md` § i18n.
- **Chốt cơ chế auth/permission cho BE** → cập nhật
  `src/BE/.claude/rules/api-controller.md` § Auth/Permission và
  `src/BE/CLAUDE.md`.
- Sau khi có code thật, nếu quy ước ở đây không còn khớp thực tế (vd. cấu
  trúc feature thực tế khác đi) → sửa Lớp 2/3 cho khớp code thật, theo đúng
  triết lý "tài liệu bám nguồn thật" đã áp dụng cho `doc/Design/` (xem
  `doc/Design/CLAUDE.md` § Fidelity Policy).

## Agent thứ 3 — `core-reviewer` (kiểm toán độc lập phần core)

Khác với 2 agent trên (mỗi agent sở hữu 1 vùng code và **xây** code trong đó),
`core-reviewer` **không sở hữu vùng nào và không sửa file code nào** — nó
không được cấp công cụ `Edit`. Vai trò duy nhất: đọc `src/BE` + `src/FE`, đối
chiếu với bộ quy tắc core, rồi ghi báo cáo PASS/PARTIAL/MISSING kèm bằng chứng
`file:line`.

### Nguồn tri thức riêng — `doc/huong_dan/wiki-core/`

Đây là **lớp tri thức thứ 4**, nằm ngoài mô hình 3 lớp ở trên, và có mục đích
khác hẳn:

| | Lớp 2/3 (`src/*/CLAUDE.md` + `.claude/rules|docs/`) | `doc/huong_dan/wiki-core/` |
| --- | --- | --- |
| Mô tả cái gì | Quy ước **đang thực thi** cho code hiện tại | Kiến thức nền về core cho **hệ thống mới nói chung** |
| Ai đọc | `backend-expert`/`frontend-expert` khi viết code | `core-reviewer` khi audit |
| Phạm vi | Đúng những gì PlatformManager đang cần | Cả những thứ PlatformManager demo chưa cần |

`core-reviewer` đọc **cả hai** — chính vì vậy nó phân biệt được "lệch khỏi
wiki-core vì đã cố ý đơn giản hoá cho demo" (không phải lỗi) với "lệch vì
thiếu sót thật" (là finding).

Cấu trúc: `wiki-core/README.md` (mục lục) → 2 lớp con:

- `be/01-...` đến `be/10-...` (10 chủ đề lý thuyết — "core gồm những gì và
  vì sao cần", nguyên tắc Nhóm A/B).
- `be/trien-khai/00-...` đến `08-...` (9 file thực hành — "làm thì làm theo
  thứ tự nào, đẻ ra file/class/interface nào", đối chiếu trực tiếp với source
  thật của `VNR.Successor`, không lý thuyết suông). Đây là lớp **chi tiết
  nhất** trong toàn bộ 4 lớp tri thức — chữ ký class thật, thứ tự đăng ký DI
  thật, ArchTest thật — dùng khi cần biết chính xác "hình dạng" của 1 thành
  phần core, không chỉ "có nên có nó không".

`fe/README.md` (**hiện là stub** — chưa có bộ quy tắc core FE riêng, tạm dùng
`src/FE/.claude/docs/*.md`; xem TODO trong chính file đó — cũng chưa có bản
`fe/trien-khai/` tương ứng).

### Cách kích hoạt

- **Tự động**: `backend-expert`/`frontend-expert` tự gọi qua `SendMessage`
  sau khi hoàn thành việc chạm core — điều kiện kích hoạt cụ thể nằm ở mục
  "Sau khi hoàn thành việc chạm tới core" trong file agent của mỗi bên (chỉ
  chạm thành phần nền tảng dùng chung mới kích hoạt, không áp dụng cho
  feature nghiệp vụ đơn lẻ).
- **Thủ công**: `/core-reviewer <phạm vi>`.

Báo cáo ghi ra `doc/huong_dan/wiki-core/audit/<ngày>-<phạm vi>.md`; mỗi
finding chỉ đích danh agent chịu trách nhiệm sửa, và việc sửa **luôn** thuộc
về `backend-expert`/`frontend-expert`.

## Khi nào cần cập nhật `wiki-core/`

- Viết xong phần core FE (hiện còn stub) → thay `fe/README.md` bằng bộ file
  `fe/01-...`, `fe/02-...` và cập nhật mục lục ở `wiki-core/README.md`. Cân
  nhắc thêm `fe/trien-khai/` tương ứng nếu FE cũng cần lộ trình thực hành
  chi tiết như BE.
- Chốt một quyết định kiến trúc mới cho hệ thống (auth, metadata, concurrency
  ...) → cập nhật đúng file chủ đề trong `be/`, không thêm file gộp mới.
- Khi `PlatformManager` bắt đầu implement thật một phase (P0–P6) và phát hiện
  file `be/trien-khai/0X-...` tương ứng lệch với source thật lúc đó đã đối
  chiếu (`VNR.Successor` cũng có thể đã đổi) → sửa lại đúng file phase đó,
  giữ nguyên nguyên tắc "mọi tên class/interface/file phải có thật" đã nêu ở
  đầu [be/trien-khai/00-lo-trinh-tong-the.md](wiki-core/be/trien-khai/00-lo-trinh-tong-the.md).

## Tham khảo thêm

- [.claude/agents/frontend-expert.md](../../.claude/agents/frontend-expert.md)
- [.claude/agents/backend-expert.md](../../.claude/agents/backend-expert.md)
- [.claude/agents/core-reviewer.md](../../.claude/agents/core-reviewer.md)
- [doc/huong_dan/wiki-core/README.md](./wiki-core/README.md) — bộ quy tắc core
- [doc/Design/SETUP.md](../Design/SETUP.md) — setup pipeline thiết kế → Figma (khác chủ đề, cùng repo)
