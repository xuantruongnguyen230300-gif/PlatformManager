# CLAUDE.md — Project-wide rules

## 1. Git — đọc được, GHI thì không

**Mọi lệnh git làm THAY ĐỔI trạng thái repo là của người dùng, không phải của agent.**

| | Lệnh | Ai chạy |
| --- | --- | --- |
| ✅ **Được phép** | `git status`, `git diff`, `git log`, `git show`, `git blame` | Agent tự chạy thoải mái — chỉ đọc, không đổi gì |
| 🛑 **CẤM** | `add`, `commit`, `push`, `checkout`, `switch`, `restore`, `merge`, `rebase`, `reset`, `stash`, `branch`, `clean`, `cherry-pick`, `revert`, `rm`, `mv`, `am`, `apply`, `pull`, `tag`, `config`, `worktree`, `submodule`, `remote *` | **Chỉ người dùng** |

Lệnh cấm này **được cưỡng chế bằng máy**, không phải bằng câu văn: khối
`permissions.deny` trong [`settings.json`](settings.json) chặn thẳng. Cùng khối
đó chặn thêm `dotnet ef database update`, `dotnet ef database drop`,
`dotnet ef migrations remove`, `npm publish`, `dotnet nuget push`, `docker push`.

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
Core **lẫn** màn hình
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
| "Sửa envelope thì đọc `doc/huong_dan/quy-uoc/be-api-controller.md` trước" | Không (chỉ sai nếu **doc** đổi chỗ) | `.claude` ✓ |
| "Xong việc chạm core thì gọi `core-reviewer`" | Không | `.claude` ✓ |
| "Handler trả `IApiResult<T>`, lỗi khai qua `ErrorDescriptor`" | **Có** | `doc` |
| "`src/BE/Core/` có 5 project" | **Có** | `doc` |
| "`--warn` là `#965e08`" | **Có** | `doc` |

**Hệ quả cứng:** file trong `.claude/` **không được chứa code block ngôn ngữ
lập trình** (`csharp`, `typescript`, `scss`, `sql`). Code mẫu là tri thức.

Được phép — đúng danh sách `check-docs.sh` cưỡng chế (§2), không rộng hơn:
`bash`/`sh` (lệnh chạy), `markdown` (mẫu báo cáo), `text` (khối `$ARGUMENTS`,
placeholder), và khối không gắn ngôn ngữ (sơ đồ cây, ASCII).

> Sửa 2026-08-27: bản trước chỉ liệt `bash` và `markdown`, trong khi gate cho
> thêm `sh`/`text` và **cả 12 file `SKILL.md` đều dùng `text`** cho khối
> `$ARGUMENTS`. Luật khi đó tuyên bố toàn bộ skill trong repo là vi phạm còn gate
> thì cho qua — một luật rộng hơn thứ nó cưỡng chế được thì không ai theo.

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
| **Thêm file `doc/` mới làm file chủ của một chủ đề** | `doc/` | **CÓ — chỉ thêm MỘT dòng đường dẫn vào bảng định tuyến của agent cần nó** |
| Thêm/bỏ agent hoặc skill | `.claude/` | CÓ |
| Đổi quy trình, bàn giao, ràng buộc thi hành | `.claude/` | CÓ |

**Không ô nào cho phép chép nội dung từ `doc/` sang `.claude/`.** Nếu thấy mình
đang viết câu thứ hai giải thích *nội dung* của một file `doc/` bên trong
`.claude/` — dừng lại, đó là lúc luật đang bị vi phạm.

> **Ô "thêm file chủ mới" — mở 2026-08-27, có giới hạn hẹp.** Trước đó ô này
> ngầm là **KHÔNG**, và hệ quả là một file chủ mới có thể không bao giờ được
> agent mở: bảng định tuyến không có dòng nào trỏ tới nó, mà agent thì đã có
> câu trả lời tự tin từ file cũ nên không đi tìm tiếp — đúng cơ chế thất bại
> số 3 ở mục "Vì sao" bên dưới, chỉ khác chiều.
>
> Giới hạn: **một dòng đường dẫn, vào đúng agent thật sự cần nó**. Không thêm
> vào mọi agent cho đủ bộ, không kèm tóm tắt nội dung, không mô tả file đó nói
> gì. Nếu chủ đề mới đã tới được agent qua một mục lục (`doc/README.md`,
> `wiki-core/README.md`) thì **không thêm gì cả** — đường dự phòng đó có sẵn.

Dạng trỏ đường chuẩn trong `.claude/`: một dòng, không tóm tắt kèm.

```markdown
> 📖 Envelope & error → HTTP: đọc `doc/huong_dan/quy-uoc/be-api-controller.md`
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

> **Ghi nhận 2026-08-23, cập nhật 2026-08-29:** đợt 08-23 ghi nhận `src/` đang đi
> **sau** `doc/`, nên nhóm `🚧` là nhóm lớn nhất. Điều đó **không còn đúng** —
> ngày 2026-08-29 phần Core của `src/FE` được viết lại và `doc/Design/` được
> đồng bộ theo, nên hai bên đã khớp.
>
> Chiều cập nhật token thì vẫn giữ nguyên và đã được xác nhận lại 2026-08-29:
> `doc/Design/` là nguồn, code đuổi theo. Đừng chép chiều đó vào đây.
> 📖 Chiều cập nhật token: đọc `doc/huong_dan/wiki-core/fe/04-design-token-system.md` § Chiều

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

Có **ba** cổng, phạm vi rời nhau. Chạy đủ những cổng mà việc vừa làm chạm tới —
không phải cả ba mỗi lần, nhưng cũng không được bỏ cổng của khu mình vừa sửa.
Bỏ cổng không làm việc hỏng ngay; nó làm việc hỏng **im lặng**, và đó là lý do
duy nhất mục này tồn tại.

| Vừa sửa gì | Cổng | Từng mục của cổng định nghĩa ở |
| --- | --- | --- |
| `.md` trong `doc/`, `.claude/` hoặc `spec/` — **và cả chú thích trỏ `doc/` viết trong `src/`** | `bash .claude/check-docs.sh` | chính script — mỗi mục là một dòng `section` |
| bất cứ gì trong `src/FE/` | `bash scripts/fe-gate.sh`, **rồi** bộ lệnh `ng` đi kèm | `doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md` |
| bất cứ gì trong `src/BE/` | `dotnet test` trên solution BE — ArchTests nằm trong đó | `doc/huong_dan/wiki-core/be/04-testing-strategy.md` |

Đừng chép số mục của cổng nào vào đây (§6) — đếm bằng lệnh:

```bash
grep -c '^section ' .claude/check-docs.sh
grep -c '^section ' scripts/fe-gate.sh
```

⚠️ **`fe-gate.sh` KHÔNG phải toàn bộ cổng FE.** Nó chỉ phủ phần kiểm được bằng
`grep`; phần còn lại nằm ở `ng lint` / `ng build` / `ng test`, khai đủ trong
`doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md`. Chạy mỗi script rồi tuyên bố
cổng FE xanh là **đúng cách hỏng đã xảy ra thật** ở repo này — lệnh đầu gãy, mấy
lệnh sau chạy bình thường, và người chạy tưởng đã qua.

Cổng chỉ có tác dụng khi harness không hỏi lại giữa chừng. Dòng lệnh nào ở bảng
trên còn bị `permissions.allow` của [`settings.json`](settings.json) bỏ sót thì
**thêm vào** — một cổng bị prompt chặn là một cổng, trên thực tế, không ai chạy.
Đây không phải chi tiết vặt: repo này đã có tiền lệ tài liệu hướng dẫn chạy một
script không tồn tại, và cách hỏng thì y hệt — không ai thấy nó không chạy.

Các mục của `check-docs.sh`, theo thứ tự script in ra:

1. Bảng cấm git trong §1 khớp `permissions.deny` của `settings.json` — bắt việc
   luật tuyên bố "cưỡng chế bằng máy" mà máy không chặn
2. `.claude/**/*.md` không có code block ngôn ngữ lập trình → bắt việc chép tri thức (§2)
3. Mọi link markdown resolve được → bắt link gãy do di chuyển file (§3)
4. Mọi đường dẫn `src/...`, `doc/...`, `spec/...`, `scripts/...` được trích dẫn đều tồn tại
5. **Trích dẫn không được neo vào file bị `.gitignore` loại khỏi repo.** Thêm
   2026-09-08. Mục 4 dùng `[ -e ]` nên nó xanh cho cả file chỉ có trên máy người
   đang chạy — bằng chứng mà người thứ hai clone về không bao giờ mở được. Mục này
   cố ý **không** báo lỗi cho file mới chưa commit (phép đo hôm đó: 204 file
   untracked, một cổng đo tiến độ commit là cổng người ta tắt đi); nó chỉ bắt file
   bị loại trừ **theo thiết kế**
6. Mọi dòng chứa `ĐÃ CÓ` / `✅ Xong` / `FIXED` / `Đã bật` đều kèm ngày đối chiếu (§4)
7. **Trích dẫn `file:dòng` phải nằm trong file.** Đây là phép đo **gián tiếp của
   tính đúng nội dung**: tài liệu bịa bằng chứng thường bịa luôn số dòng, mà số
   dòng thì máy đếm được. Ngày thêm kiểm này (2026-08-23) nó lập tức tìm ra 2
   lời nói dối mà 4 agent phải đọc rất nhiều mới thấy — `index.html` dòng 21
   (file khi đó có 14 dòng, chống lưng cho claim *"Inter FIXED"*) và
   `phan-quyen.page.scss` dòng 6 (file 4 dòng, chống lưng cho `TabBar.md` — spec bịa toàn phần, đã xoá).
   > Hai ví dụ trên cố ý viết *"dòng 21"* thay vì dạng có dấu hai chấm: chúng là
   > **mẫu vật**, và ở dạng neo thật thì chính mục 7 và mục 8 sẽ đi kiểm chúng
   > như trích dẫn thật — bắt một tài liệu vì nó kể lại một lời nói dối đã bị
   > bắt. Cùng lý do, tên `TabBar.md` phải nằm **cùng dòng** với cụm `đã xoá`:
   > miễn trừ lịch sử của cổng chạy theo TỪNG DÒNG, nên ngắt dòng ở giữa là đủ
   > làm nó mất hiệu lực (đã dính thật khi soạn mục 8, 2026-09-08).
8. **Trích dẫn dạng ngắn — tên file kèm số dòng nhưng không có đường dẫn — cũng
   phải giải được và nằm trong file.** Thêm 2026-09-08 sau một phép đo: mục 7 chỉ phủ
   **416** neo dạng đầy đủ, trong khi doc/ có **748** neo dạng ngắn nó chưa bao
   giờ chạm tới — tức 64% neo vô hình, và mục 7 thì tự nhận là *"phép đo gián
   tiếp của tính đúng nội dung"*. Máy tự nối lại đường dẫn khi tên file là duy
   nhất trong repo (97% số ca), nên không phải viết tay 748 chỗ. Báo lỗi cho
   cả ba loại: trùng tên (người đọc không biết là file nào), không giải được,
   và vượt số dòng.
9. **Bảng định tuyến trỏ đúng chủ đề** — ô chủ đề nêu đích danh một định danh
   trong dấu `` ` `` thì định danh đó phải có trong file đích. Đường dẫn đúng tới
   **file sai** vẫn qua được mục 4; mục này bắt nó.
10. Mọi tên file `.md` viết trong code span ở `.claude/` phải resolve được từ một
   root đã biết — bắt đường dẫn cụt sau khi di trú file
11. **Chú thích trong `src/` trỏ `doc/` phải tồn tại.** Thêm 2026-09-08. Mọi mục
   trên đây đóng khung bằng `--include='*.md'`, nên một đường dẫn viết trong file
   `.cs`/`.ts` không bao giờ được kiểm — đo được hôm đó: 4 chú thích BE trỏ một
   thư mục không tồn tại, và 6 file `src/FE` trỏ một đặc tả đã bị xoá, trong khi
   cổng vẫn PASS. Mục này bắt cả dạng viết cụt tiền tố (`wiki-core/...`).
12. Không còn tham chiếu `doc/Prototype/` (§7)
13. **Mọi file `doc/` và `spec/` khai đủ `kind` / `scope` / `verified` ở
    frontmatter** (§9 dưới đây)

## 9. Ba khoá phân loại bắt buộc cho mọi file `doc/` và `spec/`

Thêm 2026-09-02, sau một phép đo: **85/127 file (66%) không mang nhãn trạng thái
nào**, trong khi §4 quy định *"không nhãn = mặc định bị coi là chưa xác minh"*.
Nghĩa là hai phần ba tài liệu ở trạng thái không ai biết có đúng không — và
không có cách nào biết cái nào đã kiểm.

Ba khoá biến ba câu hỏi phải-đọc-mới-biết thành ba câu **máy đọc được**:

| Khoá | Giá trị | Trả lời câu hỏi |
| --- | --- | --- |
| `kind` | `luat` · `tham-chieu` · `quyet-dinh` · `lich-su` | **Code có phải tuân file này không?** |
| `scope` | `core` · `du-an` | **File này có đi theo khi tách CoreBase sang dự án 2 không?** |
| `verified` | `YYYY-MM-DD` · `chua-doi-chieu` · `khong-ap-dung` | **Lần cuối ai mở source ra đối chiếu toàn bộ file là bao giờ?** |

**`kind: tham-chieu` là khoá quan trọng nhất, và là khoá dễ bỏ sót nhất.** Loạt
`doc/tham-khao-ngoai/vnr-successor/` mô tả lộ trình xây
dựng của **một dự án khác** (VNR.Successor), ánh xạ sang đây qua bảng dịch. Một
agent không phân biệt được sẽ báo *"doc yêu cầu X, code không có X"* cho những
X chưa bao giờ là luật của repo này — đã xảy ra thật ngày 2026-09-01.

**`verified: chua-doi-chieu` là giá trị TRUNG THỰC, không phải lỗi cần dọn.**
Đóng dấu ngày cho một file chưa ai mở source ra so mới đúng là khuôn sai §4 cấm.
Cổng in ra số file chưa đối chiếu mỗi lần chạy — con số đó nên **giảm dần**, và
nó là thước đo thật của việc tài liệu có đang được kiểm chứng hay không.

**`verified: khong-ap-dung` — thêm 2026-09-08, cho file KHÔNG THỂ đối chiếu.** Có ba loại,
và cả ba đều đã có sẵn một khoá khác nói lên điều đó:

| Loại | Dấu hiệu | Vì sao không đối chiếu được |
| --- | --- | --- |
| Mô tả dự án khác | `kind: tham-chieu` | Source nằm ở repo khác, không có trong cây này |
| Đặc tả thứ chưa xây | `status: "… not built"` | Chưa có gì để so |
| Mô tả thứ đã bị gỡ | `kind: lich-su` | Source đã biến mất; §5 vốn đã miễn trừ loại này khỏi §4/§6 |

Với chúng, `chua-doi-chieu` là *sai nghĩa*: nó hàm ý "có việc phải làm", trong khi không ai
làm được.

Vì sao phải thêm chứ không để nguyên: trước đó **16 file** thuộc hai loại này bị đếm vào
số "chưa đối chiếu", tạo ra một cái **sàn không bao giờ chạm tới**. Mà một chỉ số không
bao giờ đạt đích là chỉ số người ta ngừng nhìn — đúng cơ chế hỏng mà chính mục này sinh ra
để chống. Cổng nay đếm **ba** nhóm riêng, và nhóm *"chưa đối chiếu"* có thể về 0 thật.

🛑 **Không tự khai được `khong-ap-dung`.** Cổng chỉ chấp nhận nó khi file đã mang
`kind: tham-chieu`, `kind: lich-su`, **hoặc** `status:` chứa `not built` — tức lý do miễn
trừ phải là một sự thật **máy kiểm được**, không phải một câu tự nhận. Thiếu điều kiện đó thì cổng báo lỗi.
Đây là chỗ dễ lạm dụng nhất của cả ba khoá: dán `khong-ap-dung` lên một file khó đối chiếu
là cách nhanh nhất để làm con số đẹp lên mà không kiểm gì cả.

**Hệ quả cho người viết:** mọi khẳng định về hiện trạng nên neo bằng `file:dòng`.
Neo được thì máy kiểm được; không neo thì không ai kiểm — và mục 5 chỉ có tác
dụng trên những khẳng định có neo.

Hai miễn trừ, đều có chủ đích — nhưng **phạm vi của chúng khác nhau**, đừng nhầm:

| Miễn trừ | Áp ở mục |
| --- | --- |
| File mang banner `TÀI LIỆU LỊCH SỬ` (§5) | 4, 5, 6 |
| Dòng nói rõ nó nhắc thứ đã biến mất | 4, 6, 8 — **không** áp ở mục 5 |

Nghĩa là một dòng *"X đã xoá"* vẫn **phải kèm ngày** nếu nó chứa `✅ Xong`/`FIXED`.
Danh sách cụm được miễn trừ đọc thẳng từ script (`HIST_LINES`), đừng chép ra đây —
nó đã nhiều hơn 3 cụm mà bản trước của mục này liệt kê.

Không có hai miễn trừ này thì mọi ghi chép *"vì sao ta bỏ X"* đều bị báo lỗi, và
người ta sẽ xoá bài học đi cho gate xanh — đúng hành vi §4 sinh ra để ngăn.

Repo **không có CI** (`.github/` không tồn tại, có chủ đích) — không còn máy
nào chạy hộ. Gate hỏng mà không ai biết là kịch bản đã xảy ra thật: đợt
2026-08-23 phát hiện `scripts/fe-gate.sh` không tồn tại, nên 3 trong 9 gate FE
đã không chạy được suốt một thời gian dài, trong khi tài liệu vẫn ghi là bình
thường.

### 🛑 Bài học 2026-09-08 — một mục cổng có thể "xanh" vì nó KHÔNG CHẠY

Một lượt kiểm đối kháng đo số vòng lặp thật của từng mục và tìm ra: **mục §1 chạy
0 vòng** — tức nó in `OK` mà chưa từng so sánh gì. Nguyên nhân: pattern của nó
chứa ký tự 🛑 (4 byte), mà `grep` cơ bản trên Git Bash không khớp được ký tự đó
dưới chính locale mà script tự ép. Vòng lặp nhận 0 dòng ⇒ biến đếm lỗi = 0 ⇒ in OK.

Đây là **tái phát** đúng cơ chế mà khối chú thích `LC_ALL` trong script tuyên bố
đã diệt từ 2026-08-24: bản vá đó cứu `grep -P`, nhưng đẻ lại lỗi ở §1. Suốt thời
gian đó, §1 của mục 1 dưới đây tuyên bố lệnh cấm git *"được cưỡng chế bằng máy"*
trong khi máy không cưỡng chế gì. (Sửa xong: 23 lệnh, 0 lỗ hổng deny — nội dung
vẫn đúng, chỉ là không ai kiểm.)

Hai hệ quả đã được đưa vào script, đừng gỡ:

- **Mỗi mục phải tự chứng minh nó có dữ liệu đầu vào.** §1 nay báo lỗi khi trích
  được 0 lệnh, thay vì suy ra "không có lỗi nào".
- **Có một khối §0 chặn đầu**: thiếu `doc/`, `spec/`, `src/`, `settings.json`,
  hoặc `doc/+spec/` có dưới 50 file `.md` thì ABORT thay vì chạy tiếp. Lý do:
  cùng lượt đó chứng minh một bản sao script đặt ở thư mục rỗng in đủ
  `✅ PASS`, 13/13 OK — PASS không phân biệt được *"mọi thứ đúng"* với *"tôi
  không nhìn gì cả"*.

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
