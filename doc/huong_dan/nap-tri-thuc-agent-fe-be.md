# Hướng dẫn: Tri thức của agent frontend-expert / backend-expert nạp từ đâu

Repo này có 2 agent chuyên gia — `frontend-expert` (Angular 20, `src/FE/`) và
`backend-expert` (.NET Clean Architecture + CQRS, `src/BE/`) — cùng cơ chế
bàn giao API Contract Card giữa hai bên. Cả hai được tạo **trước khi có code
thật**, để định hướng đúng ngay từ dòng code đầu tiên thay vì phải tái cấu
trúc sau này.

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

## Tham khảo thêm

- [.claude/agents/frontend-expert.md](../../.claude/agents/frontend-expert.md)
- [.claude/agents/backend-expert.md](../../.claude/agents/backend-expert.md)
- [doc/Design/SETUP.md](../Design/SETUP.md) — setup pipeline thiết kế → Figma (khác chủ đề, cùng repo)
