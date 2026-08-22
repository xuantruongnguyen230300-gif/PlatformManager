# CLAUDE.md — Project-wide rules

## 1. Git — đọc được, GHI thì không

**Mọi lệnh git làm THAY ĐỔI trạng thái repo là của người dùng, không phải của agent.**

| | Lệnh | Ai chạy |
| --- | --- | --- |
| ✅ **Được phép** | `git status`, `git diff`, `git log`, `git show`, `git blame` | Agent tự chạy thoải mái — chỉ đọc, không đổi gì |
| 🛑 **CẤM** | `add`, `commit`, `push`, `checkout`, `switch`, `merge`, `rebase`, `reset`, `stash`, `branch`, `clean`, `cherry-pick`, `revert`, `rm`, `mv`, `am`, `apply`, `remote *` | **Chỉ người dùng** |

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

## 2. `.claude` phụ trách skill/agent — `doc` là nguồn tài liệu DUY NHẤT

Đây là luật chống phình tài liệu. Vi phạm nó là cách chắc chắn nhất để tạo ra
hai nguồn sự thật nói ngược nhau.

| Thư mục | Chứa gì | KHÔNG chứa gì |
| --- | --- | --- |
| **`.claude/`** | Định nghĩa **skill** và **agent**: agent làm gì, đọc file nào, quy trình ra sao, ràng buộc gì. Cấu hình harness (`settings.json`). | **Không** chứa tri thức nghiệp vụ/kỹ thuật. Không chép nội dung từ `doc/` sang. |
| **`doc/`** | **Toàn bộ** tri thức: kiến trúc, quy tắc kỹ thuật, hợp đồng API, schema, nghiệp vụ, lộ trình. | Không chứa tài liệu về cách vận hành agent. |

**Khi có tri thức mới → chỉ cập nhật `doc/`.** Không tạo bản sao trong
`.claude/`, không "ghi tóm tắt cho tiện" ở file agent.

**`.claude/` chỉ được TRỎ ĐƯỜNG, không được chép nội dung.** File agent/skill
nói *"soát envelope thì đọc `doc/…/03-p2-platform-application.md`"* — chứ không
chép luật envelope vào chính nó.

### Vì sao — hai lý do đã trả giá thật

1. **Hai nguồn thì chúng sẽ lệch nhau.** Đợt 2026-08-21 tìm ra:
   `src/BE/CLAUDE.md` khẳng định *"cả 5 project `Business.*` đã tồn tại"* (sai
   hoàn toàn); `.claude/rules/api-controller.md` có đoạn mẫu rate limit **dùng
   sai overload kèm lý do sai**, và `Program.cs` chép y theo nên mang nguyên
   lỗi. **Rule sai không nằm yên — nó sinh ra code sai.**
2. **Chép nội dung làm agent chết vì cạn context.** Corpus mà `core-reviewer`
   bị buộc đọc từng lên tới **780 KB**; 3 lượt review liên tiếp chết giữa
   chừng, 1 lượt còn để lại lỗi cố ý trong code. Trỏ đường thay vì chép giữ
   corpus ở mức **~264 KB**.

### Ngoại lệ duy nhất — tài liệu VỀ chính hệ thống agent

Tài liệu mô tả *bản thân bộ agent/skill* (agent nào tồn tại, nạp tri thức từ
đâu, kích hoạt thế nào) thuộc **`.claude/`**, vì đó là tài liệu của `.claude`
— không phải tri thức về sản phẩm. Xem [`.claude/README.md`](README.md).

Phép thử khi phân vân: *"xoá hết agent đi thì file này còn giá trị không?"*
Còn → `doc/`. Không → `.claude/`.

## 3. Tài liệu phải mô tả thứ CÓ THẬT

Không viết tài liệu mô tả project/file/cơ chế **chưa tồn tại** như thể nó đã
tồn tại. Cần ghi lại đích đến chưa thi công thì đánh dấu rõ trạng thái
(**"đã chốt, ĐANG thi công"**) kèm bảng đối chiếu "có thật hôm nay → sẽ thành".

Rule mô tả sai hiện trạng còn tệ hơn không có rule: agent đọc xong sẽ tạo file
vào project không tồn tại, hoặc code theo mẫu sai.
