# Lộ trình triển khai core FE — tổng thể

> Phần thực hành của `wiki-core/fe/`. Các file `fe/01-…13-…` trả lời *"core
> gồm những gì và vì sao cần"*. Thư mục này trả lời *"làm theo thứ tự nào"*.

> ### ⚠️ Đổi hướng 2026-08-23 — `src/FE` viết mới từ số 0
>
> Bản trước của bộ này giả định `ng new` **đã chạy rồi** và lộ trình là
> *"đồng bộ + dọn nợ"* trên nền có sẵn: F0 đồng bộ envelope, F1 đồng bộ
> Design, F2 dọn hardcode hex. Nay `src/FE` viết mới hoàn toàn, nên hai giai
> đoạn "đồng bộ" và cả giai đoạn "dọn nợ" **không còn lý do tồn tại** — viết
> mới là viết đúng ngay từ đầu, không có gì để đồng bộ ngược và không có nợ
> nào để dọn. File `03-f2-don-no-ky-thuat.md` đã xoá.
>
> Lộ trình dưới đây vì vậy mang đúng hình dạng của `be/trien-khai/`:
> **dựng nền → lên tầng → gate**.

## 5 giai đoạn

| Giai đoạn | Tên | Đầu ra kiểm chứng được (Definition of Done) | Ước lượng |
| --- | --- | --- | --- |
| **F0** | Nền móng | `ng build` xanh trên app zoneless mới; có cây `core/ platform/ modules/ shared/`; `IApiResult<T>` + 2 interceptor + `ToastService` chạy được — gọi 1 endpoint lỗi thật hiện đúng `message` | 1–2 ngày |
| **F1** | Design token → code | `styles.scss` `:root` đủ token lấy từ `doc/Design/…/Tokens/`; PrimeNG preset map token; grep hex ngoài `:root` = 0 | 1–2 ngày |
| **F2** | Auth + routing/guard | Đăng nhập qua cookie tại `/dang-nhap`; `CurrentUserService`; 3 guard đúng thứ tự; user `mustChangePassword` bị ép sang `/doi-mat-khau`; logout chặn lại route cũ | 2–3 ngày |
| **F3** | Hai màn quản trị Core | `/quan-tri/nguoi-dung` và `/quan-tri/phan-quyen` CRUD chạy thật qua HTTP; `Admin` bị chặn khỏi màn phân quyền | 3–5 ngày |
| **Gate** | Luật có máy kiểm | `scripts/fe-gate.sh` **tồn tại** và chạy được; lint chặn hex trần và thiếu `track` | liên tục |

## Thứ tự phụ thuộc — vì sao đúng thứ tự đó

```
F0 ──► F1 ──► F2 ──► F3
                └──► Gate (chạy song song từ F1 trở đi)
```

- **F1 trước F2** vì màn đăng nhập là màn đầu tiên có giao diện thật. Dựng nó
  khi chưa có token nghĩa là sẽ hardcode màu rồi sửa lại — đúng thứ nợ mà
  đợt trước phải mở hẳn một giai đoạn để dọn.
- **F2 trước F3** vì hai màn quản trị đều nằm sau `adminGuard`/`superAdminGuard`.
  Không có auth thì không kiểm chứng được chúng bị chặn đúng hay không.
- **Gate không đợi tới cuối.** Bật từ F1 — lúc đó `:root` vừa có token, đúng
  thời điểm luật "không hex trần" bắt đầu có nghĩa.

## Phạm vi — chỉ Core

Bốn màn Core (`/dang-nhap`, `/doi-mat-khau`, `/quan-tri/nguoi-dung`,
`/quan-tri/phan-quyen`) nằm ở `platform/`. Hai màn nghiệp vụ (`/dashboard`,
`/danh-muc/dti`) thuộc `modules/` và **không** nằm trong lộ trình này — chúng
đi cùng giai đoạn nghiệp vụ, khi `spec/` được triển khai.

Ranh giới `platform/` ↔ `modules/`:
[`../../../quy-uoc/fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §1.

## Nguyên tắc chi phối

Giống hệt BE: **luật kiến trúc phải có máy kiểm** (Gate), **mỗi giai đoạn kết
thúc bằng thứ chạy được** (không phải thư mục đầy file), và **1 luật = 1
nguồn** — envelope chỉ định nghĩa 1 chỗ ở `core/http/api-result.model.ts`,
mọi nơi khác import lại.

## Mục lục các file trong `fe/trien-khai/`

| File | Nội dung |
| --- | --- |
| [01-f0-nen-mong.md](01-f0-nen-mong.md) | `ng new` zoneless, cây thư mục, `core/http` + interceptor |
| [02-f1-design-token.md](02-f1-design-token.md) | Đưa token từ `doc/Design/` vào `styles.scss`, preset PrimeNG |
| [03-f2-auth-routing.md](03-f2-auth-routing.md) | `CurrentUserService`, 3 guard, màn đăng nhập/đổi mật khẩu |
| [04-f3-man-quan-tri.md](04-f3-man-quan-tri.md) | Hai màn quản trị Core, phụ thuộc contract BE |
| [05-gate.md](05-gate.md) | Lint rule + check tương đương ArchTest |
