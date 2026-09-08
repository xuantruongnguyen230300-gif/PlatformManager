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
| `agents/` | Định nghĩa agent — `ls .claude/agents` là danh sách thật |
| `skills/` | Skill gọi bằng `/<tên>` — xem bảng dưới |

### Agent

| Agent | Vai | Sửa code? |
| --- | --- | --- |
| [`backend-expert`](agents/backend-expert.md) | `src/BE` — .NET Clean Architecture + CQRS | Có |
| [`frontend-expert`](agents/frontend-expert.md) | `src/FE` — Angular 20 standalone + Signals | Có |
| [`design-expert`](agents/design-expert.md) | Khu `doc/Design/` — token, component spec, screen spec, export Figma | Có (chỉ trong `doc/Design/`) |
| [`core-reviewer`](agents/core-reviewer.md) | Kiểm toán độc lập phần core, báo PASS/PARTIAL/MISSING | **Không** — chỉ audit |

### Skill

| Skill | Dùng khi |
| --- | --- |
| `/feature-kickoff` | Điểm vào **duy nhất** cho vòng đời một feature — tự điều phối các agent bên dưới |
| `/backend-expert` · `/frontend-expert` · `/core-reviewer` | Gọi thẳng một agent cho việc lẻ |
| `/design-new-project` → `/design-export-figma` | Các stage của pipeline thiết kế, chạy theo thứ tự — bảng stage/gate đầy đủ ở `doc/Design/CLAUDE.md` §Pipeline & Skills |

Danh sách skill thật đếm bằng lệnh, không chép tay (§6):
`ls .claude/skills`

---

Hai agent chuyên gia **xây code** — `frontend-expert` (`src/FE/`) và
`backend-expert` (`src/BE/`) — cùng cơ chế bàn giao API Contract Card giữa hai
bên. Cả hai được tạo **trước khi có code thật**, để định hướng đúng ngay từ
dòng code đầu tiên thay vì phải tái cấu trúc sau này.

`core-reviewer` **không xây code**, chỉ kiểm toán độc lập phần "core" của cả
hai vùng — xem [mục riêng](#core-reviewer--kiểm-toán-độc-lập-phần-core) ở cuối
tài liệu. `design-expert` sở hữu khu `doc/Design/`.

## Tri thức nằm ở đâu

```
.claude/                                  ← QUY TRÌNH & RÀNG BUỘC (không có tri thức)
├── CLAUDE.md                     # luật toàn repo: git, ranh giới, chiều cập nhật
├── check-docs.sh                 # gate tài liệu, chạy tay
├── agents/*.md                   # vai trò, phạm vi, bàn giao, khi nào dừng hỏi
└── skills/*/SKILL.md             # điểm gọi vào `/<tên>`

doc/                                      ← TOÀN BỘ TRI THỨC
└── README.md                     # MỤC LỤC CẤP CAO NHẤT — mọi chủ đề tra ở đây
```

**Khu nào của `doc/` giữ chủ đề nào — cố ý không liệt ở đây.** Bản liệt kê như
vậy mục ruỗng ngay lần `doc/` tách hoặc gộp một file chủ, mà không có gì báo.
Đường vào duy nhất: [`doc/README.md`](../doc/README.md); đường tắt theo chủ đề
nằm ở bảng định tuyến trong từng file `agents/*.md`.

**`.claude/agents/*.md`**: agent đọc file này đầu tiên trong mọi
task. Chứa vai trò, cách resolve `{FE_ROOT}`/`{BE_ROOT}`, danh sách "đọc bắt
buộc" **trỏ sang `doc/`**, cơ chế bàn giao Contract Card, và checklist "dừng
lại hỏi người dùng khi...". **Đây là nơi duy nhất mô tả quy trình/hành vi** —
không chứa chi tiết kỹ thuật, không chứa code mẫu.

**`doc/huong_dan/quy-uoc/`**: quy ước thi hành thật cho `src/BE` và
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

## Vì sao tách agent (quy trình) khỏi `doc/` (tri thức)

1. File agent ngắn, tập trung vào **quy trình** — ít phải sửa khi chi tiết kỹ
   thuật đổi.
2. Tri thức có **đúng một** bản, trong `doc/`. Không có bản sao để lệch.
3. Đổi phiên bản framework chỉ sửa `doc/`, không đụng cơ chế bàn giao ở agent.

Luật đầy đủ và ba lần đã trả giá: [`CLAUDE.md`](CLAUDE.md) §2–§3.

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

Ba mục từng nằm ở đây (version .NET, thư viện i18n, cơ chế auth/permission)
**đều đã chốt xong** — đọc trạng thái ở `doc/`, đừng đọc như câu hỏi còn mở.

Khi quy ước trong `doc/` không khớp code: **mặc định sửa code, không sửa doc**.
`doc/` mô tả đích đến; chỗ nào code chưa theo thì dán nhãn 🚧 kèm ngày đối
chiếu — xem [`CLAUDE.md`](CLAUDE.md) §4. Chiều "tài liệu bám code" chỉ áp cho
`doc/Design/` (§Fidelity Policy của khu đó), **không** áp cho `huong_dan/`.

## `core-reviewer` — kiểm toán độc lập phần core

Khác với hai agent trên (mỗi agent sở hữu một vùng code và **xây** code trong đó),
`core-reviewer` **không sở hữu vùng nào và không sửa file code nào** — nó
không được cấp công cụ `Edit`. Vai trò duy nhất: đọc `src/BE` + `src/FE`, đối
chiếu với bộ quy tắc core, rồi ghi báo cáo PASS/PARTIAL/MISSING kèm bằng chứng
`file:line`.

### Nguồn tri thức riêng — `doc/huong_dan/wiki-core/`

Khu này nằm ngoài hai khu vừa nói, và có mục đích khác hẳn:

| | `doc/huong_dan/quy-uoc/` | `doc/huong_dan/wiki-core/` |
| --- | --- | --- |
| Mô tả cái gì | Quy ước **đang thực thi** cho code hiện tại | Kiến thức nền về core cho **hệ thống mới nói chung** |
| Ai đọc | `backend-expert`/`frontend-expert` khi viết code | `core-reviewer` khi audit |
| Phạm vi | Đúng những gì PlatformManager đang cần | Cả những thứ PlatformManager demo chưa cần |

`core-reviewer` đọc **cả hai** — chính vì vậy nó phân biệt được "lệch khỏi
wiki-core vì đã cố ý đơn giản hoá cho demo" (không phải lỗi) với "lệch vì
thiếu sót thật" (là finding).

Cấu trúc bên trong — có mấy lớp, mỗi lớp gồm file nào, lớp nào là **luật** còn
lớp nào chỉ là **tham chiếu** — đọc thẳng mục lục, đừng đọc bản mô tả chép ở
đây (bản trước ghi cả số file lẫn ngày viết xong, đúng loại câu §6 cấm):

> 📖 [`doc/huong_dan/wiki-core/README.md`](../doc/huong_dan/wiki-core/README.md)

Phân biệt luật ↔ tham chiếu bằng khoá `kind` ở frontmatter, không bằng trí nhớ
([`CLAUDE.md`](CLAUDE.md) §9).

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

> ### Vì sao bỏ hẳn `audit/` (2026-08-21) — file chủ của lý do này
>
> Thư mục đó từng chứa 12 file / **252 KB**, và agent được lệnh đọc report lượt
> trước để đối chiếu. Nó **tự phình theo thời gian** — report lượt đầu 11 KB,
> lượt gần nhất **48 KB** — nên mỗi lượt audit lại làm lượt sau nặng hơn. Kết
> quả: 3 lượt review liên tiếp **chết giữa chừng vì cạn context**, một lượt còn
> để lại lỗi cố ý trong code khi tắt trước lúc dọn canary.
>
> Bỏ đi thì mất khả năng trả lời *"finding này mở bao lâu rồi"*. Đánh đổi chấp
> nhận được: finding đã đóng đều có bằng chứng sống là **test**, không cần
> report kể lại; finding chưa đóng mà chỉ tồn tại trong report thì đằng nào
> cũng là finding bị bỏ quên. Việc còn tồn đọng phải nằm ở nơi người ta đọc khi
> làm — **file wiki của chủ đề đó** — chứ không nằm trong nhật ký audit.

## Khi nào cần cập nhật `wiki-core/`

Tra bảng *"chiều cập nhật"* ở [`CLAUDE.md`](CLAUDE.md) §3 — nội dung vào `doc/`,
`.claude/` chỉ đổi đường dẫn — và luật một-chủ-đề-một-file-chủ ở §5. Không dựng
bản luật thứ hai ở đây.

Nguyên tắc riêng của loạt `tham-khao-ngoai/vnr-successor/` (*"mọi tên class/interface/file phải
có thật"*) nằm ở đầu chính file
[../doc/tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md](../doc/tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md).

## Tham khảo thêm

- [.claude/agents/frontend-expert.md](agents/frontend-expert.md)
- [.claude/agents/backend-expert.md](agents/backend-expert.md)
- [.claude/agents/core-reviewer.md](agents/core-reviewer.md)
- [.claude/agents/design-expert.md](agents/design-expert.md)
- [doc/Design/CLAUDE.md](../doc/Design/CLAUDE.md) — luật khu Design + pipeline thiết kế
- [doc/huong_dan/wiki-core/README.md](../doc/huong_dan/wiki-core/README.md) — bộ quy tắc core
- [doc/Design/SETUP.md](../doc/Design/SETUP.md) — setup pipeline thiết kế → Figma (khác chủ đề, cùng repo)
