# Hướng dẫn: Set up Figma MCP với tài khoản mới

Hướng dẫn này dành cho việc kết nối MCP server `figma` (khai trong `.mcp.json`
ở root repo) với một **tài khoản Figma mới**, dùng cho pipeline thiết kế ở
[doc/Design/](../Design/README.md) (xem [doc/Design/SETUP.md](../Design/SETUP.md)
để setup môi trường tổng quát).

## Bối cảnh

MCP server `figma` là remote HTTP server chính thức của Figma
(`https://mcp.figma.com/mcp`). Xác thực qua **OAuth trong trình duyệt** —
không cần API key thủ công. Tài khoản Figma nào được cấp quyền phụ thuộc vào
việc bạn đang đăng nhập tài khoản nào trong trình duyệt tại thời điểm xác
nhận OAuth.

⚠️ **Lưu ý phạm vi**: credential OAuth của MCP thường lưu **theo máy/user
Claude Code, không theo từng repo**. Nếu bạn đổi tài khoản `figma` ở đây, các
project khác trên cùng máy dùng chung MCP `figma` cũng sẽ chuyển sang tài
khoản mới.

## Trường hợp 1 — Chưa từng kết nối `figma` MCP trên máy này

1. Mở trình duyệt ở **cửa sổ ẩn danh (incognito/private)**, hoặc đăng xuất
   tài khoản Figma cũ đang đăng nhập sẵn.
2. Đăng nhập (hoặc đăng ký mới) tài khoản Figma bạn muốn dùng tại
   [figma.com](https://figma.com) — tài khoản mới tự có sẵn không gian
   "Drafts" cá nhân, đủ dùng để test, chưa cần Team trả phí.
3. Trong Claude Code, gõ `/mcp` → chọn server `figma` → chạy xác thực. Hoặc
   không cần làm gì trước — lần đầu tiên skill `/design-export-figma` gọi
   `use_figma`, Claude Code sẽ tự trigger luồng OAuth.
4. Trình duyệt mở trang consent của Figma → xác nhận bằng đúng tài khoản mới
   vừa đăng nhập ở bước 2.

## Trường hợp 2 — Máy này ĐÃ từng auth với một tài khoản Figma khác

Token cũ vẫn còn lưu, nếu không xóa trước thì Claude Code sẽ tái dùng session
cũ thay vì hỏi lại. Xóa và đăng nhập lại:

```bash
claude mcp logout figma
claude mcp login figma
```

- `claude mcp logout figma` — xóa credential đã lưu cho server `figma`.
- `claude mcp login figma` — mở lại trình duyệt cho luồng OAuth mới.

Trước khi chạy `claude mcp login figma`, nhớ đăng nhập tài khoản Figma mới
trong trình duyệt (hoặc dùng cửa sổ ẩn danh nếu tài khoản cũ vẫn đang đăng
nhập ở trình duyệt thường), như bước 1–2 ở trên.

## Kiểm tra đã kết nối đúng tài khoản

- `claude mcp list` — xem trạng thái các MCP server đã cấu hình, server nào
  cần auth.
- Trong session Claude Code, gõ `/mcp` để mở panel xem trạng thái kết nối
  của `figma`.

## Tạo file Figma mới với tài khoản đó

Sau khi đã auth đúng tài khoản, có hai cách:

1. **Để Claude tự tạo**: chạy `/design-export-figma PlatformManager` (hoặc
   nói trực tiếp yêu cầu export) — ở Route B, skill sẽ gọi `use_figma` để
   "tạo mới hoặc nhắm vào file Figma đích", tạo file mới thẳng trong Drafts
   của tài khoản vừa auth.
2. **Tự tạo trước**: vào figma.com bằng tài khoản mới, tạo một file trống,
   copy URL của file đó, rồi đưa URL này cho Claude làm target khi chạy
   export — tránh việc AI tự chọn nơi tạo file.

## Tham khảo thêm

- [doc/Design/SETUP.md](../Design/SETUP.md) — setup môi trường tổng quát cho
  toàn bộ pipeline Design → Figma (Node/npx, `chrome-devtools-mcp`, cách
  thêm Stitch MCP tùy chọn).
- [doc/Design/CLAUDE.md](../Design/CLAUDE.md) — convention mà AI phải tuân
  theo khi ghi artifact thiết kế.
