# CLAUDE.md — src/FE

Chuẩn kiến trúc cho frontend PlatformManager. File này được tạo **trước khi
có app thật** — mục đích là để `ng new` đầu tiên (và mọi feature sau đó) đi
đúng đường ngay từ ngày một, không cần tái cấu trúc sau này.

Agent chính chịu trách nhiệm vùng này: `frontend-expert` (xem
`.claude/agents/frontend-expert.md` ở workspace root). Chi tiết theo từng
chủ đề nằm ở `.claude/docs/` trong chính thư mục này — đọc file tương ứng
**trước khi** viết code cho vùng đó, đừng chỉ đọc mỗi file này.

## Stack

- **Angular 20**, standalone component + Signals (không dùng `NgModule`).
- Control flow mới: `@if` / `@for` / `@switch` / `@defer`.
- `input()` / `output()` kiểu signal thay cho decorator `@Input()`/`@Output()`.
- SCSS scoped theo component; token màu/spacing đồng bộ với
  `doc/Design/` một khi pipeline thiết kế đã export (xem
  `doc/Design/SETUP.md`).

## Đọc theo chủ đề

| File | Đọc khi |
| --- | --- |
| `.claude/docs/architecture.md` | Hiểu tầng `core` / `modules` / `shared`, cấu trúc 1 feature |
| `.claude/docs/api-client.md` | Gọi API, ranh giới DTO/model, mapper |
| `.claude/docs/ui-conventions.md` | Dựng UI, form, responsive, style theo token |

## Scaffold lần đầu (khi `angular.json` chưa tồn tại)

```bash
cd src/FE
npx @angular/cli@20 new . --directory=. --standalone --style=scss --ssr=false --routing=true
```

Chạy trong thư mục `src/FE/` rỗng (Angular CLI cần thư mục trống hoặc chỉ
chứa file ẩn/README — nếu CLI từ chối vì đã có `CLAUDE.md`/`.claude/`, tạo ở
thư mục tạm rồi merge nội dung sinh ra vào `src/FE/`, giữ nguyên `CLAUDE.md`
và `.claude/` đã có). Sau khi scaffold xong, `{FE_ROOT}` marker `angular.json`
sẽ tồn tại và các skill/agent tự resolve bình thường.

## Maintenance Rules

1. Mọi feature mới đi theo đúng cấu trúc trong `.claude/docs/architecture.md`
   — không có ngoại lệ "vì đây chỉ là feature nhỏ".
2. DTO/model tách biệt ngay từ slice đầu tiên (xem `.claude/docs/api-client.md`)
   — đừng đợi tới khi có bug wire mới tách.
3. UI mới phải khớp token/component đã tài liệu hoá trong `doc/Design/` khi
   đã tồn tại — không tự phát minh giá trị song song.
