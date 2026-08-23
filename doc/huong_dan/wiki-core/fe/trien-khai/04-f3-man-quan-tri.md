# F3 — Hai màn quản trị Core

> **Định nghĩa hoàn thành:** `/quan-tri/nguoi-dung` chạy đủ tạo / sửa / khoá /
> mở khoá / phân trang / tìm kiếm qua HTTP thật; `/quan-tri/phan-quyen` đọc và
> lưu được ma trận quyền; tài khoản chỉ có role `Admin` bị **chặn** khỏi màn
> phân quyền; mọi lỗi validation từ BE bind đúng vào từng ô nhập.

Đây là hai màn Core cuối cùng. Xong F3 là `platform/` đủ 4 màn và nền tảng
dùng lại được cho sản phẩm khác.

## Phạm vi

| Màn | Route | Quyền | Endpoint |
| --- | --- | --- | --- |
| Quản trị người dùng | `/quan-tri/nguoi-dung` | `Admin` + `SuperAdmin` | `GET/POST /api/users`, `PUT /api/users/{id}`, `POST /api/users/{id}/lock`\|`/unlock` |
| Phân quyền | `/quan-tri/phan-quyen` | **chỉ `SuperAdmin`** | `GET/PUT /api/admin/permissions`, `GET/PUT /api/admin/permissions/resources` |

> 📖 Hợp đồng đầy đủ (payload, mã lỗi, ràng buộc):
> [`../../../../contracts/users.md`](../../../../contracts/users.md) ·
> [`../../../../contracts/permissions.md`](../../../../contracts/permissions.md)
>
> 📖 Bố cục, copy, trạng thái từng màn:
> `doc/Design/Frontend/PlatformManager/Screens/03-quan-tri-nguoi-dung.md` và
> `Screens/04-phan-quyen.md`

## Ba thứ dễ làm sai

### 1. `Admin` **không** vào được màn phân quyền

Không phải nhầm lẫn phân quyền — là biện pháp chống leo thang quyền qua UI.
`superAdminGuard` chặn, và BE cũng chặn độc lập bằng
`[Authorize(Roles = "SuperAdmin")]`. Đừng "sửa cho tiện" thành `adminGuard` khi
thấy Admin phàn nàn không mở được màn.

Ẩn mục khỏi sidebar **không** thay được guard: người gõ thẳng URL vẫn phải bị
chặn.

### 2. Khoá tài khoản / đổi role **không** có hiệu lực tức thì

Phiên đang chạy của user bị khoá chỉ chấm dứt trong vòng **≤ 30 phút**
(`contracts/users.md` §⏱️). Riêng đăng nhập mới thì bị chặn ngay.

**Hệ quả cho UI:** không viết copy kiểu *"Đã đăng xuất người dùng"* — nó sai.
Thông báo phải phản ánh đúng độ trễ, nếu không quản trị viên sẽ tưởng thao tác
hỏng rồi bấm lại nhiều lần.

### 3. Phân trang đọc đúng `PagedList<T>`

Danh sách người dùng trả `{ items, page, pageSize, totalCount }` — **không có**
`totalPages`. Cần số trang thì tự tính từ `totalCount` và `pageSize`; đừng chờ
một field không tồn tại rồi phân trang lặng lẽ hỏng.

## Kiểm chứng

- [ ] Đăng nhập bằng tài khoản chỉ có `Admin` → gõ `/quan-tri/phan-quyen` →
      bị đưa về `/dashboard`, **không** thấy nội dung màn dù chớp nhoáng
- [ ] Tạo user với dữ liệu sai → lỗi hiện **trên từng ô**, không chỉ toast chung
      (key `fields` là PascalCase, xem `../02-http-envelope.md`)
- [ ] Phân trang sang trang 2 rồi tìm kiếm → về trang 1, không giữ `page` cũ
- [ ] Lưu ma trận quyền → tải lại trang → giá trị vẫn đúng như vừa lưu
- [ ] Copy của thao tác khoá tài khoản **không** hứa hiệu lực tức thì
