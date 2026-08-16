# Lộ trình triển khai core FE — tổng thể

> Phần thực hành của `wiki-core/fe/`. Các file `fe/01-…10-…` trả lời "core
> gồm những gì và vì sao cần". Thư mục này trả lời "làm theo thứ tự nào".
>
> Khác với `be/trien-khai/` (7 phase P0-P6, vì .NET solution cần scaffold
> tay từng project) — Angular CLI đã scaffold sẵn 1 lần (`ng new`, đã chạy
> rồi). Lộ trình FE vì vậy không phải "dựng nền móng" mà là **đồng bộ + dọn
> nợ** trên nền đã có, cộng vài phần core còn thiếu.

## 6 giai đoạn

| Giai đoạn | Tên | Đầu ra kiểm chứng được | Chặn gì nếu bỏ qua |
|---|---|---|---|
| **F0** | Đồng bộ envelope | `IApiResult<T>` FE khớp BE, interceptor đọc đúng `message`/`fields`/`businessCode`, có test kiểm chứng | Mọi handler mới sau F0 sẽ code đúng theo hợp đồng cũ đã lỗi thời |
| **F1** | Đồng bộ Design | `Tokens/*.md` khớp `styles.scss` thật, `danh-muc-dti.html` có đặc tả, 12 component có đủ 5 trạng thái | Component mới tiếp tục thiếu hover/focus/disabled |
| **F2** | Dọn nợ kỹ thuật | 0 chỗ hex hardcode ngoài token, ≥2 test mapper/interceptor rủi ro cao nhất | Nợ cũ + nợ mới cộng dồn, càng sửa càng đắt |
| **F3** | Auth | `ICurrentUser`, guard, login/logout chạy được qua cookie session | Không có màn nào bảo vệ được theo quyền |
| **F4** | Component library chuẩn hoá | Mọi component dùng chung đã áp `signalStore()`/token đúng quy ước file 03-05 | Rời rạc giữa các module |
| **F5** | Gate | Lint rule + CI check chặn hex/thiếu `track`/thiếu test tối thiểu | Quy ước trôi dần không ai biết |

**Không làm tuần tự cứng nhắc như BE** — F0 phải xong trước khi viết handler
mới (đúng thứ tự phụ thuộc thật), nhưng F1/F2 có thể chạy song song với F0
vì không đụng cùng file. F3 chờ BE chốt xong phần scaffold Identity thật —
đây chính là chỗ 2 agent `backend-expert`/`frontend-expert` cần chạy song
song và đồng bộ qua cơ chế teammate (`SendMessage` + API Contract Card, xem
`.claude/agents/backend-expert.md` §Bàn giao với `frontend-expert`).

## Nguyên tắc chi phối

Giống hệt BE: **luật kiến trúc phải có máy kiểm** (F5), **mỗi giai đoạn kết
thúc bằng thứ chạy được** (không phải thư mục đầy file), và **1 luật = 1
nguồn** (envelope chỉ định nghĩa 1 chỗ ở `core/http/api-result.model.ts`,
mọi nơi khác import lại).

## Mục lục các file trong `fe/trien-khai/`

| File | Nội dung |
|---|---|
| [01-f0-dong-bo-envelope.md](01-f0-dong-bo-envelope.md) | File cần sửa/tạo, thứ tự viết, test kiểm chứng |
| [02-f1-dong-bo-design.md](02-f1-dong-bo-design.md) | Re-run pipeline thiết kế, bổ sung 5 trạng thái |
| [03-f2-don-no-ky-thuat.md](03-f2-don-no-ky-thuat.md) | Danh sách 9 chỗ hardcode cần sửa, 2 file test ưu tiên |
| [04-f3-auth.md](04-f3-auth.md) | Thứ tự dựng `ICurrentUser`/guard/login, phụ thuộc BE |
| [05-gate.md](05-gate.md) | Lint rule + CI check tương đương ArchTest |
