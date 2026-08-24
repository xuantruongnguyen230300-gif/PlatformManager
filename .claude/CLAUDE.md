# CLAUDE.md — Project-wide rules

## 1. Git — đọc được, GHI thì không

**Mọi lệnh git làm THAY ĐỔI trạng thái repo là của người dùng, không phải của agent.**

| | Lệnh | Ai chạy |
| --- | --- | --- |
| ✅ **Được phép** | `git status`, `git diff`, `git log`, `git show`, `git blame` | Agent tự chạy thoải mái — chỉ đọc, không đổi gì |
| 🛑 **CẤM** | `add`, `commit`, `push`, `checkout`, `switch`, `restore`, `merge`, `rebase`, `reset`, `stash`, `branch`, `clean`, `cherry-pick`, `revert`, `rm`, `mv`, `am`, `apply`, `pull`, `tag`, `config`, `worktree`, `submodule`, `remote *` | **Chỉ người dùng** |

Lệnh cấm này **được cưỡng chế bằng máy**, không phải bằng câu văn: khối
`permissions.deny` trong [`settings.json`](settings.json) chặn thẳng. Cùng khối
đó chặn thêm `dotnet ef database update`, `dotnet ef migrations remove`,
`npm publish`, `dotnet nuget push`, `docker push`.

Áp dụng cho **mọi** skill và subagent (`frontend-expert`, `backend-expert`,
`core-reviewer`, `design-expert`, mọi `/design-*`), không có ngoại lệ.

Nếu một việc cần lệnh git ghi để đi tiếp (tạo nhánh, commit mốc, stash để đổi
hướng): **dừng lại và nói rõ cần chạy lệnh gì** — người dùng tự chạy rồi bảo
agent tiếp tục. Không "xin phép rồi tự chạy".

> **Lịch sử:** bản trước của mục này cấm **tuyệt đối** mọi lệnh git kể cả lệnh
> đọc, trong khi `settings.json` vẫn cho phép 6 lệnh đọc. Hai nguồn nói ngược
> nhau, và cái được cưỡng chế là `settings.json` — nên lệnh cấm kia thực chất
> chỉ là câu văn. Sửa 2026-08-21 theo đúng hành vi thật, đồng thời giữ nguyên
> điều quan trọng: **agent không được đổi trạng thái repo.**
>
> Bổ sung 2026-08-23: `restore`, `pull`, `tag`, `config`, `worktree`,
> `submodule` trước đây **không** có trong bảng lẫn trong `deny`. Đáng chú ý
> nhất là `git restore` — lệnh thay thế hiện đại của `git checkout -- <file>`
> (đang bị cấm) và xoá thay đổi working tree không hoàn tác được.

## 2. Ranh giới `.claude` ↔ `doc` — phép thử kiểm được bằng máy

Repo có **ba** khu tri thức, không phải hai:

| Thư mục | Chứa gì | Không chứa gì |
| --- | --- | --- |
| **`.claude/`** | **Quy trình và ràng buộc**: agent nào tồn tại, làm gì, đọc file nào, bàn giao ra sao, bị cấm gì. Cấu hình harness (`settings.json`). | Tri thức. Code mẫu. |
| **`doc/`** | **QUY TẮC**: kiến trúc, quy ước code, hợp đồng API, schema, và **giao diện**. | Luật nghiệp vụ của một feature cụ thể. |
| **`spec/`** | **NGHIỆP VỤ** theo từng feature: `spec/<feature>/business-rules.md`, `spec/<feature>/ui-spec.md`. | Quy tắc kiến trúc/code. |

**Agent và skill luôn phải tuân thủ quy tắc trong `doc/`** — kể cả khi đang làm
việc thuộc `spec/`.

### Ngoại lệ đã chốt: `doc/Design/` phủ cả Core lẫn nghiệp vụ

`doc/Design/` là **nguồn tham chiếu giao diện FE duy nhất** — chứa cả màn hình
Core (đăng nhập, đổi mật khẩu, quản trị người dùng, phân quyền) **lẫn** màn hình
nghiệp vụ (dashboard, danh mục DTI), và cả component dùng chung lẫn component
riêng sản phẩm.

Vì vậy **không** áp luật "gỡ nghiệp vụ khỏi Core" cho khu Design: spec component
được phép trích dẫn màn hình nghiệp vụ làm nơi nó xuất hiện, và `Screens/` được
phép mô tả màn nghiệp vụ. Chia đôi khu này sẽ phá đúng thứ nó sinh ra để làm —
trả lời một câu hỏi *"giao diện chỗ này trông ra sao"* ở **một** chỗ.

Luật "gỡ nghiệp vụ khỏi Core" **vẫn áp** cho `doc/huong_dan/` (quy ước kiến
trúc) và `doc/contracts/` phần Core — vì Core sẽ tái dùng cho sản phẩm khác.

Phép thử cũ (*"xoá hết agent đi thì file này còn giá trị không?"*) đúng về tinh
thần nhưng mơ hồ ở đúng chỗ hay sai — vì **quy tắc thi hành cũng là tri thức**,
và người viết luôn tự thuyết phục được rằng đoạn mình sắp chép là "rule".

Dùng phép thử này thay thế, vì nó trả lời được bằng có/không:

> ### `.claude/` không được chứa câu nào có thể trở thành **SAI** khi code thay đổi.

| Câu | Code đổi thì có sai không? | Thuộc |
| --- | --- | --- |
| "Không chạy lệnh git ghi" | Không | `.claude` ✓ |
| "Sửa envelope thì đọc `doc/…/03-p2-platform-application.md` trước" | Không (chỉ sai nếu **doc** đổi chỗ) | `.claude` ✓ |
| "Xong việc chạm core thì gọi `core-reviewer`" | Không | `.claude` ✓ |
| "Handler trả `IApiResult<T>`, lỗi khai qua `ErrorDescriptor`" | **Có** | `doc` |
| "`src/BE/Core/` có 5 project" | **Có** | `doc` |
| "`--warn` là `#965e08`" | **Có** | `doc` |

**Hệ quả cứng:** file trong `.claude/` **không được chứa code block ngôn ngữ**
(`csharp`, `typescript`, `scss`, `sql`). Code mẫu là tri thức. Chỉ được phép
code block `bash` cho lệnh chạy, và `markdown` cho mẫu báo cáo.

### Ngoại lệ duy nhất — tài liệu VỀ chính hệ thống agent

Tài liệu mô tả *bản thân bộ agent/skill* (agent nào tồn tại, nạp tri thức từ
đâu, kích hoạt thế nào) thuộc **`.claude/`**, vì đó là tài liệu của `.claude`
— không phải tri thức về sản phẩm. Xem [`.claude/README.md`](README.md).

## 3. Chiều cập nhật — nội dung vào `doc`, CHỈ đường dẫn vào `.claude`

Đây là luật chống tái phát. Khi có thay đổi, tra bảng này **trước khi mở file**:

| Việc vừa xảy ra | Sửa ở | `.claude/` có đổi không |
| --- | --- | --- |
| Chốt quyết định kiến trúc mới | `doc/` | **KHÔNG** |
| Sửa/bổ sung quy ước kỹ thuật, code mẫu, giá trị token | `doc/` | **KHÔNG** |
| Ghi nhận hiện trạng, đóng một việc tồn đọng | `doc/` | **KHÔNG** |
| **File `doc/` đổi tên, đổi chỗ, bị xoá, hoặc tách ra** | `doc/` | **CÓ — chỉ sửa đường dẫn, không đụng nội dung** |
| Thêm/bỏ agent hoặc skill | `.claude/` | CÓ |
| Đổi quy trình, bàn giao, ràng buộc thi hành | `.claude/` | CÓ |

**Không ô nào cho phép chép nội dung từ `doc/` sang `.claude/`.** Nếu thấy mình
đang viết câu thứ hai giải thích *nội dung* của một file `doc/` bên trong
`.claude/` — dừng lại, đó là lúc luật đang bị vi phạm.

Dạng trỏ đường chuẩn trong `.claude/`: một dòng, không tóm tắt kèm.

```markdown
> 📖 Envelope & error → HTTP: đọc `doc/huong_dan/wiki-core/be/trien-khai/03-p2-platform-application.md`
```

### Vì sao — ba lý do đã trả giá thật

1. **Hai nguồn thì chúng sẽ lệch nhau.** Đợt 2026-08-21 tìm ra:
   `src/BE/CLAUDE.md` (đã xoá 2026-08-23) khẳng định *"cả 5 project `Business.*` đã tồn tại"* (sai
   hoàn toàn); `.claude/rules/api-controller.md` (đã chuyển) có đoạn mẫu rate limit **dùng
   sai overload kèm lý do sai**, và `Program.cs` chép y theo nên mang nguyên
   lỗi. **Rule sai không nằm yên — nó sinh ra code sai.**
2. **Bản sao không bao giờ được sửa cùng lúc.** Đợt 2026-08-23 tìm ra recipe
   `RowVersion` sai provider (dùng `IsRowVersion()` của SQL Server trong khi dự
   án chạy Npgsql) tồn tại **song song** ở
   `doc/huong_dan/wiki-core/be/06-concurrency-control.md` và
   `src/BE/.claude/rules/entity-domain.md` (đã chuyển). Sửa một nơi không chạm nơi kia.
3. **Agent không "thấy" conflict — nó im lặng dùng bản sao.** Đọc
   `backend-expert.md` xong nó đã có câu trả lời tự tin, đầy đủ, có code mẫu,
   nên **không bao giờ mở `doc/`**. Vì vậy lỗi loại này không tự lộ ra, và
   không có test nào bắt được.
4. **Chép nội dung làm agent chết vì cạn context.** Corpus mà `core-reviewer`
   bị buộc đọc từng lên tới **780 KB**; 3 lượt review liên tiếp chết giữa
   chừng, 1 lượt còn để lại lỗi cố ý trong code. Trỏ đường thay vì chép giữ
   corpus ở mức **~264 KB**.

## 4. Tài liệu phải mô tả thứ CÓ THẬT — và phải dán nhãn trạng thái

Mọi tuyên bố về hiện trạng trong `doc/` phải mang **một** trong ba nhãn.
Không nhãn = mặc định bị coi là chưa xác minh.

| Nhãn | Nghĩa | Bắt buộc kèm |
| --- | --- | --- |
| `✅ CÓ THẬT` | Đã đối chiếu với source | **Ngày đối chiếu** + `file:line` |
| `🚧 ĐÃ CHỐT — ĐANG THI CÔNG` | Quyết định xong, code chưa về | Bảng *"có thật hôm nay → sẽ thành"* |
| `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` | Mới là dự kiến | — |

**Cấm tuyệt đối:** đóng một việc bằng cách sửa mô tả cho khớp mong muốn rồi
đánh dấu là xong. Đợt rà 2026-08-23 tìm ra **7 ca** cùng khuôn này ở 5 khu khác
nhau — `"FIXED 2026-08-22"` khi giá trị chưa hề vào code, `"Đã bật 2026-08-21"`
cho một hằng số ESLint chỉ tồn tại trong đúng câu nói nó tồn tại, `"✅ Xong"`
cho 5 mục chưa làm, `"0 citation out of range"` khi thực tế có 79.

Đây là dạng sai đắt nhất: nó không gây lỗi biên dịch, không bị test bắt, và
nhãn "đã xong" được thiết kế để **không ai kiểm lại**.

> **Ghi nhận 2026-08-23:** `src/` đang đi **sau** `doc/`. Vì vậy nhóm `🚧` sẽ là
> nhóm lớn nhất, và luật *"code là nguồn sự thật, tài liệu mirror theo"* trong
> `doc/huong_dan/wiki-core/fe/04-design-token-system.md` **không dùng được** cho
> tới khi `src/` bắt kịp — dán nhãn `🚧` thay vì mirror ngược về code cũ.

## 5. Một chủ đề — một file chủ

Mỗi chủ đề có **đúng một** file giữ nội dung. Mọi file khác chỉ được trỏ tới nó.

Khi phát hiện hai file cùng mô tả một thứ: chọn một làm chủ, file kia rút còn
một dòng trỏ đường. **Không "giữ cả hai cho chắc"** — đó là cách repo này có
**4 sơ đồ đặt tên project** và **4 nguồn mô tả database** nói ngược nhau.

Tài liệu đã chết nhưng cần giữ để tra cứu: **không xoá, không để nguyên** —
dán banner lịch sử ở **đầu file**, theo mẫu banner ở `doc/ke-hoach-xay-lai-corebase.md` (banner
"TÀI LIỆU LỊCH SỬ" + bảng *"Trong file này → Thực tế hiện nay"* + trỏ về nguồn
sống). Nếu file có bản `.dbml`/`.json` đi kèm được công cụ ngoài đọc thẳng,
banner phải chép vào **chính file đó** — người mở dbdiagram.io không đi qua
file `.md` để thấy cảnh báo.

## 6. Không chép vào tài liệu thứ đếm được bằng lệnh

Bảng liệt kê tay sẽ luôn mục ruỗng. Thay bằng **lệnh + tiêu chí PASS**.

Cấm chép: danh sách file vi phạm, số lượng test/gate/thành phần, danh sách
"còn N chỗ hardcode". Đợt 2026-08-23 tìm ra **7 chỗ đếm sai** (18 vs 20 thành
phần core, 34 vs 36 ArchTest, 4 vs 6 behavior, 10 vs 12 file `be/`, 10 vs 13
file `fe/`, 24 vs 27 token, 25 vs 28 component), và một bảng "9 chỗ hardcode
hex" sai 4/7 dòng đồng thời bỏ sót 2 dòng đúng.

## 7. Giao diện người dùng: `doc/Design/` là nguồn DUY NHẤT

`doc/Prototype/` **đã bị xoá 2026-08-23**. Từ nay mọi tham chiếu về giao diện —
layout, copy, token, trạng thái component, ảnh màn hình — lấy từ
`doc/Design/Frontend/<Project>/`. Không khôi phục, không dựng prototype HTML
mới, không trích dẫn đường dẫn `doc/Prototype/...` nữa.

Quy tắc riêng của khu Design (Fidelity Policy, citation `file:line`, bảng 5
trạng thái, gate lint) nằm ở [`doc/Design/CLAUDE.md`](../doc/Design/CLAUDE.md)
— đó là tri thức, thuộc `doc/`, đúng chỗ.

## 8. Trước khi coi một việc là xong

Chạy `bash .claude/check-docs.sh` (~6 giây). Nó kiểm 6 thứ:

1. `.claude/**/*.md` không có code block ngôn ngữ → bắt việc chép tri thức (§2)
2. Mọi link markdown resolve được → bắt link gãy do di chuyển file (§3)
3. **Bảng định tuyến trỏ đúng chủ đề** — ô chủ đề nêu đích danh một định danh
   trong dấu `` ` `` thì định danh đó phải có trong file đích. Đường dẫn đúng tới
   **file sai** vẫn qua được mục 4; mục này bắt nó.
4. Mọi đường dẫn `src/...` và `doc/...` được trích dẫn đều tồn tại
5. **Trích dẫn `file:dòng` phải nằm trong file.** Đây là phép đo **gián tiếp của
   tính đúng nội dung**: tài liệu bịa bằng chứng thường bịa luôn số dòng, mà số
   dòng thì máy đếm được. Ngày thêm kiểm này (2026-08-23) nó lập tức tìm ra 2
   lời nói dối mà 4 agent phải đọc 800 KB mới thấy — `index.html:21` (file 14
   dòng, chống lưng cho claim *"Inter FIXED"*) và `phan-quyen.page.scss:6` (file
   4 dòng, chống lưng cho `TabBar.md`, spec bịa toàn phần đã xoá).
6. Mọi dòng chứa `ĐÃ CÓ` / `✅ Xong` / `FIXED` / `Đã bật` đều kèm ngày đối chiếu (§4)

**Hệ quả cho người viết:** mọi khẳng định về hiện trạng nên neo bằng `file:dòng`.
Neo được thì máy kiểm được; không neo thì không ai kiểm — và mục 5 chỉ có tác
dụng trên những khẳng định có neo.

Hai miễn trừ, đều có chủ đích: file mang banner `TÀI LIỆU LỊCH SỬ` (§5) và dòng
chứa `đã xoá`/`trước ở`/`không còn tồn tại` được bỏ qua ở mục 4–6. Không có
chúng thì mọi ghi chép *"vì sao ta bỏ X"* đều bị báo lỗi, và người ta sẽ xoá bài
học đi cho gate xanh — đúng hành vi §4 sinh ra để ngăn.

Repo **không có CI** (`.github/` không tồn tại, có chủ đích) — không còn máy
nào chạy hộ. Gate hỏng mà không ai biết là kịch bản đã xảy ra thật: đợt
2026-08-23 phát hiện `scripts/fe-gate.sh` không tồn tại, nên 3 trong 9 gate FE
đã không chạy được suốt một thời gian dài, trong khi tài liệu vẫn ghi là bình
thường.

### ⚠️ PASS không có nghĩa là tài liệu ĐÚNG

Gate chỉ bắt được thứ **máy kiểm được**: đường dẫn có tồn tại không, link có
resolve không, tuyên bố có kèm ngày không. Nó **không** đọc hiểu nội dung. Ba
loại lỗi nó không bao giờ bắt được:

1. **Văn xuôi mô tả thứ không tồn tại.** Gỡ một citation chết làm gate xanh,
   nhưng đoạn văn bên cạnh vẫn có thể đang tả một màn hình chưa ai xây.
2. **Sơ đồ/cây thư mục chép sai.** Khối ``` trần không bị §2 chặn — cây 10
   project sai trong `backend-expert.md` lọt qua mọi luật cho tới khi có người
   đọc.
3. **Ngày đúng nhưng nội dung sai.** `✅ Xong (2026-08-18)` qua được §4 kể cả
   khi việc đó chưa làm.

Gate là lưới **chặn hồi quy**, không phải chứng nhận chất lượng. Việc đối chiếu
nội dung với source thật vẫn thuộc về `core-reviewer` và người đọc.
