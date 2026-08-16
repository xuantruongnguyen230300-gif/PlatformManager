# F0 — Đồng bộ envelope BE mới

> **Định nghĩa hoàn thành:** gọi 1 endpoint cố tình trả lỗi nghiệp vụ (vd
> trùng mã) từ BE thật (hoặc mock) → FE hiện đúng `message` (không phải
> chuỗi rỗng/`undefined`), toast/form nhận đúng `fields` nếu có, và có ít
> nhất 1 test tự động kiểm chứng (xem
> [../06-testing-strategy.md](../06-testing-strategy.md)) đã từng đỏ trước
> khi sửa.

## File cần sửa/tạo

| File | Việc |
|---|---|
| `core/http/api-result.model.ts` | Tạo mới — `IApiResult<T>` khớp 1:1 BE (xem [../02-http-envelope.md](../02-http-envelope.md)) |
| `core/http/api-response.model.ts` | Xoá hoặc thay nội dung — đây là model cũ (`ApiResponse<T>`), không giữ song song 2 model cùng lúc |
| `core/interceptors/http-error.interceptor.ts` | Sửa — đọc `message`/`businessCode`/`fields` thay vì `Message`/`ErrorMessage` |
| `core/services/api-response.service.ts` | Rà lại toàn bộ — đổi tên phù hợp nếu tên file vẫn còn ám chỉ shape cũ |
| `modules/*/services/*.service.ts` | Rà từng service — chỗ nào tự đọc field envelope (không qua interceptor chung) phải sửa theo |

## Thứ tự viết

```
1. IApiResult<T> model (30 phút — thuần khai báo type)
        │
        ▼
2. httpErrorInterceptor sửa lại — đọc field mới      (nửa ngày)
        │
        ▼
3. Viết test interceptor — cố tình cho nó đỏ trước    (1 giờ — xem mẫu
   (giả 1 response theo ApiResponse cũ) rồi sửa cho    ở ../06-testing-strategy.md)
   xanh với IApiResult mới
        │
        ▼
4. Rà từng service feature (danh-muc-dti, dashboard)  (nửa ngày — tuỳ số
   — service nào tự parse response ngoài interceptor   lượng chỗ tự parse
   chung phải sửa theo
```

## Kiểm chứng

- [ ] `IApiResult<T>` có đủ 8 field, tên khớp field JSON thật (xác nhận
      bằng 1 lần gọi thật/Swagger trước khi code, đừng đoán)
- [ ] Không còn import nào tới `api-response.model.ts` cũ trong toàn bộ
      `src/app/` (grep để chắc)
- [ ] Test interceptor đã **kiểm chứng đỏ** (viết assertion theo field cũ,
      chạy thấy fail, rồi sửa code, chạy lại thấy pass) — không chỉ viết
      test rồi thấy xanh ngay từ đầu
- [ ] `fields` bind được vào ít nhất 1 form thật (không chỉ toast) để xác
      nhận key PascalCase→camelCase hoạt động đúng
