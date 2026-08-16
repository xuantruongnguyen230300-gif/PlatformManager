# 9. Form & Validation

## Container — drawer/side-panel trước, modal sau

Đã có trong `ui-conventions.md` — nhắc lại lý do: modal che toàn màn hình
làm mất ngữ cảnh (không thấy được dữ liệu nền khi điền form phức tạp nhiều
bước). Side-panel giữ được ngữ cảnh, tự nhiên hỗ trợ responsive (full-width
khi màn hình hẹp). Modal/dialog nhỏ **chỉ** dùng cho xác nhận ngắn
(`confirm-dialog`, đã có sẵn trong `styles.scss`).

## Validate 2 lớp — giống nguyên tắc BE

| Lớp | Kiểm tra gì | Khi nào chạy |
|---|---|---|
| Client (Angular `Validators`/custom) | Format, required, độ dài — phản hồi tức thì, không cần round-trip | Lúc gõ/blur |
| Server (`ValidationBehavior`/`ErrorDescriptor` phía BE) | Business rule cần DB (trùng mã, FK tồn tại) | Lúc submit |

Client validate **không thay thế** server validate — chỉ để UX phản hồi
nhanh. Submit vẫn phải xử lý được lỗi 400 (`fields`) trả về từ BE dù client
đã "pass" hết (race condition, dữ liệu đổi giữa lúc mở form và lúc submit).

## Bind lỗi từ `fields` vào form

```ts
private setFieldErrors(fields: Record<string, string[]>): void {
  for (const [key, messages] of Object.entries(fields)) {
    this.form.get(toCamelCase(key))?.setErrors({ server: messages[0] });
  }
}
```

Key từ BE là **PascalCase** (`MaxScore`) — form control Angular quy ước
thường `camelCase` (`maxScore`) — cần 1 hàm `toCamelCase` dùng chung ở
`core/`, không lặp lại logic map key ở từng form (đúng nguyên tắc "1 luật =
1 nguồn").

## Message hiển thị

Ưu tiên đọc thẳng `messages[0]` (đã là câu hoàn chỉnh do BE dịch qua
`ErrorDescriptor.Resolve`) — **không** tự ráp lại "Trường X: " + message,
vì `FieldError.Message` phía BE cố ý **không** chứa tên field (xem
`be/trien-khai/03-p2-platform-application.md` §4.5) — label field FE tự lấy
từ chính form (`<label>`), tránh 2 nơi cùng giữ tên field rồi lệch nhau khi
đổi copy.
