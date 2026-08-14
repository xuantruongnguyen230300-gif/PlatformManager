# FE core — STUB, chưa có bộ quy tắc đầy đủ

## Trạng thái hiện tại

Thư mục `wiki-core/be/` được đúc kết qua nhiều vòng trao đổi sâu (đối chiếu
kiến trúc thật của VNR.Successor + kiến thức ngành). **Phần FE tương đương
chưa được thực hiện** — cần 1 phiên trao đổi riêng để đúc kết "core FE thật
sự của senior lâu năm" (ví dụ: HTTP client abstraction, error interceptor,
design-token/theming layer, state management pattern, shared component
library, form-engine, ranh giới module hoá) giống cách đã làm với BE.

## Chuẩn tạm thời cho `core-reviewer`

Cho tới khi phần này được viết đầy đủ, agent `core-reviewer` dùng các file
đã có sẵn — do `frontend-expert` tuân theo — làm chuẩn đối chiếu tạm cho
phần FE:

- `src/FE/CLAUDE.md` — mục lục kiến trúc.
- `src/FE/.claude/docs/architecture.md` — tầng `core`/`modules`/`shared`.
- `src/FE/.claude/docs/api-client.md` — ranh giới DTO/model, mapper.
- `src/FE/.claude/docs/ui-conventions.md` — Angular 20 control flow, style/token.

Đây là quy ước **thực thi hiện tại** (Lớp 2/3 theo mô hình 3 lớp tri thức ở
`doc/huong_dan/nap-tri-thuc-agent-fe-be.md`), không hoàn toàn cùng tầm nhìn
"core dài hạn cho hệ thống mới" như `wiki-core/be/` — nhưng đủ dùng làm
chuẩn tối thiểu để `core-reviewer` không bỏ trống hoàn toàn phía FE.

## TODO

- [ ] Trao đổi với user để đúc kết danh sách "thành phần core FE" (tương
      đương bảng 18 thành phần ở `be/01-core-components.md`).
- [ ] Viết `fe/01-core-components.md`, `fe/02-...md`, v.v. theo cùng cấu
      trúc với `be/`.
- [ ] Cập nhật `README.md` của `wiki-core/` để trỏ đúng các file mới thay
      vì trỏ vào stub này.
