# `.claude/` — hệ thống agent & skill của PlatformManager

> **Đây là tài liệu VỀ bộ agent, không phải tri thức về sản phẩm.**
> Ranh giới đã chốt ở [`CLAUDE.md`](CLAUDE.md) §2: `.claude/` phụ trách
> **skill và agent**; `doc/` là **nguồn tài liệu duy nhất** cho mọi tri thức
> kiến trúc/kỹ thuật/nghiệp vụ. Tri thức mới → chỉ cập nhật `doc/`.
> `.claude/` chỉ **trỏ đường**, không chép nội dung.
>
> Phép thử khi phân vân: *"xoá hết agent đi thì file này còn giá trị không?"*
> Còn → thuộc `doc/`. Không → thuộc `.claude/`.
>
> File này trước ở `doc/huong_dan/nap-tri-thuc-agent-fe-be.md`, chuyển về đây
> 2026-08-21 vì nó mô tả chính hệ thống agent.

## Nội dung `.claude/`

| Đường dẫn | Chứa gì |
| --- | --- |
| [`CLAUDE.md`](CLAUDE.md) | Luật toàn repo: git, ranh giới `.claude` ↔ `doc`, tài liệu phải mô tả thứ có thật |
| [`settings.json`](settings.json) | Cấu hình harness — `permissions.allow` / `permissions.deny` (nơi lệnh cấm git được **cưỡng chế bằng máy**) |
| `agents/` | Định nghĩa 4 agent: `backend-expert`, `frontend-expert`, `core-reviewer`, `design-expert` |
| `skills/` | Skill gọi bằng `/<tên>` — bao gồm `setup-tai-khoan-figma.md` của `design-export-figma` |

---

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
.claude/                                  ← QUY TRÌNH & RÀNG BUỘC (không có tri thức)
├── CLAUDE.md                     # luật toàn repo: git, ranh giới, chiều cập nhật
├── check-docs.sh                 # gate tài liệu, chạy tay
├── agents/*.md                   # vai trò, phạm vi, bàn giao, khi nào dừng hỏi
└── skills/*/SKILL.md             # điểm gọi vào `/<tên>`

doc/                                      ← TOÀN BỘ TRI THỨC
├── huong_dan/quy-uoc/            # quy ước ĐANG THỰC THI
│   ├── README.md                 #   mục lục + stack BE/FE + trạng thái kiến trúc
│   ├── be-{architecture,entity-domain,cqrs-handler,api-controller}.md
│   └── fe-{architecture,api-client,ui-conventions}.md
├── huong_dan/wiki-core/          # kiến thức nền về core (chuẩn chung)
├── Design/                       # giao diện — nguồn DUY NHẤT, cả FE lẫn BE
├── contracts/                    # hợp đồng API từng endpoint
├── ERD/ + cau-truc-database.md   # dữ liệu
└── kien-truc-core-module.md      # ranh giới Core ↔ Business
```

**Lớp 1 — `.claude/agents/*.md`**: agent đọc file này đầu tiên trong mọi
task. Chứa vai trò, cách resolve `{FE_ROOT}`/`{BE_ROOT}`, danh sách "đọc bắt
buộc" **trỏ sang `doc/`**, cơ chế bàn giao Contract Card, và checklist "dừng
lại hỏi người dùng khi...". **Đây là nơi duy nhất mô tả quy trình/hành vi** —
không chứa chi tiết kỹ thuật, không chứa code mẫu.

**Lớp 2 — `doc/huong_dan/quy-uoc/`**: quy ước thi hành thật cho `src/BE` và
`src/FE` — layer rule, hình dạng handler, envelope, cấu trúc feature FE, ranh
giới DTO/model. Đây là nơi có code mẫu.

> **Lịch sử — mô hình 3 lớp cũ đã bỏ (2026-08-23).** Lớp 2/3 trước nằm ở
> `src/BE/CLAUDE.md` + `src/BE/.claude/rules/` và `src/FE/CLAUDE.md` (tất cả đã xoá) +
> `src/FE/.claude/docs/` — **78 KB tri thức kỹ thuật nằm ngoài `doc/`**. Lý do
> đặt cạnh code lúc đó là *"tách repo riêng thì tri thức đi theo"*; lý do đó là
> giả định, còn thiệt hại thì đo được: recipe `RowVersion` sai provider tồn tại
> song song 2 nơi, và `src/BE/CLAUDE.md` (đã xoá) giữ nguyên câu *"cả 5 project
> `Business.*` đã tồn tại"* suốt nhiều tháng vì nằm ngoài tầm với của mọi luật.
> Toàn bộ đã hoà tan vào `doc/huong_dan/quy-uoc/`.

## Vì sao tách 3 lớp thay vì gộp vào 1 file agent (như VNR.Successor làm)

Repo tham chiếu ban đầu (`VNR.Successor` — xem
[skills/design-export-figma/setup-tai-khoan-figma.md](skills/design-export-figma/setup-tai-khoan-figma.md) và
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
  `doc/huong_dan/quy-uoc/README.md` § Stack.
- **Chốt thư viện i18n cho FE** (nếu cần đa ngôn ngữ) → cập nhật
  `doc/huong_dan/quy-uoc/fe-ui-conventions.md` § i18n.
- **Chốt cơ chế auth/permission cho BE** → cập nhật
  `doc/huong_dan/quy-uoc/be-api-controller.md` § Auth/Permission và
  `doc/huong_dan/quy-uoc/README.md`.
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

| | `doc/huong_dan/quy-uoc/` | `doc/huong_dan/wiki-core/` |
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

`fe/01-...` đến `fe/10-...` (10 chủ đề lý thuyết) + `fe/trien-khai/00-...`
đến `05-...` (6 file thực hành, giai đoạn F0–F3 + Gate) — cùng cấu trúc với `be/`,
viết xong 2026-08-15. Khác biệt nguồn: không có "VNR.Successor frontend" để
đối chiếu — nguồn là kiến trúc chính thức Angular + hệ thống thiết kế thật
của PlatformManager (`doc/Design/Frontend/PlatformManager/`) + các quyết
định đã chốt trực tiếp với người dùng, xem ghi chú đầu
`wiki-core/README.md` § FE.

### Cách kích hoạt

- **Tự động**: `backend-expert`/`frontend-expert` tự gọi qua `SendMessage`
  sau khi hoàn thành việc chạm core — điều kiện kích hoạt cụ thể nằm ở mục
  "Sau khi hoàn thành việc chạm tới core" trong file agent của mỗi bên (chỉ
  chạm thành phần nền tảng dùng chung mới kích hoạt, không áp dụng cho
  feature nghiệp vụ đơn lẻ).
- **Thủ công**: `/core-reviewer <phạm vi>`.

**Báo cáo trả về trực tiếp — KHÔNG ghi file, KHÔNG có thư mục `audit/`.**
Mỗi finding chỉ đích danh agent chịu trách nhiệm sửa, và việc sửa **luôn**
thuộc về `backend-expert`/`frontend-expert`.

> Thư mục `audit/` đã bị bỏ hẳn (2026-08-21). Nó từng chứa 12 file / 252 KB và
> agent được lệnh đọc report lượt trước để đối chiếu — nên **mỗi lượt audit làm
> lượt sau nặng hơn** (report đầu 11 KB → report cuối 48 KB). Kết quả: 3 lượt
> review liên tiếp chết vì cạn context. Việc còn tồn đọng nay ghi thẳng vào
> **file wiki của chủ đề đó**, nơi người sửa thật sự đọc.

## Khi nào cần cập nhật `wiki-core/`

- Chốt một quyết định kiến trúc mới cho hệ thống (auth, metadata, concurrency
  ...) → cập nhật đúng file chủ đề trong `be/`, không thêm file gộp mới.
- Khi `PlatformManager` bắt đầu implement thật một phase (P0–P6) và phát hiện
  file `be/trien-khai/0X-...` tương ứng lệch với source thật lúc đó đã đối
  chiếu (`VNR.Successor` cũng có thể đã đổi) → sửa lại đúng file phase đó,
  giữ nguyên nguyên tắc "mọi tên class/interface/file phải có thật" đã nêu ở
  đầu [be/trien-khai/00-lo-trinh-tong-the.md](../doc/huong_dan/wiki-core/be/trien-khai/00-lo-trinh-tong-the.md).

## Tham khảo thêm

- [.claude/agents/frontend-expert.md](agents/frontend-expert.md)
- [.claude/agents/backend-expert.md](agents/backend-expert.md)
- [.claude/agents/core-reviewer.md](agents/core-reviewer.md)
- [doc/huong_dan/wiki-core/README.md](../doc/huong_dan/wiki-core/README.md) — bộ quy tắc core
- [doc/Design/SETUP.md](../doc/Design/SETUP.md) — setup pipeline thiết kế → Figma (khác chủ đề, cùng repo)
