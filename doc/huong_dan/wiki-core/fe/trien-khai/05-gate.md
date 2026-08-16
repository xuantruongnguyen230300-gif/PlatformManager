# Gate — kiểm tra tương đương ArchTest phía FE

> Angular không có khái niệm ArchTest như .NET (không có "assembly" để soi
> dependency bằng reflection) — tương đương gần nhất là **lint rule + script
> kiểm tra chạy trong CI**, cùng tinh thần "luật kiến trúc phải có máy
> kiểm, không dựa vào review người" (`be/trien-khai/00 §1`).

## Bộ kiểm tra tối thiểu

| # | Kiểm tra gì | Cách chạy | Khi nào cần |
|---|---|---|---|
| G1 | Không hex color literal ngoài `styles.scss :root` | `grep -rn "#[0-9a-fA-F]\{3,6\}" src/app --include=*.scss` → CI fail nếu có kết quả ngoài whitelist | Ngay sau F2 |
| G2 | Mọi `@for` có `track` | ESLint rule Angular (`@angular-eslint/template/...`) hoặc grep `@for` không kèm `track` trên cùng khối | Ngay từ đầu — F0 |
| G3 | Không còn `*ngIf`/`*ngFor`/`*ngSwitch`/`@Input()`/`@Output()`/`NgModule` | Grep — đã dùng ở audit trước, giữ làm gate thường trực | Ngay từ đầu |
| G4 | Component dumb (`components/`) không inject `HttpClient`/service data | ESLint custom rule hoặc script AST đơn giản quét `inject(...Service)` trong thư mục `components/` (trừ danh sách ngoại lệ app-shell đã ghi ở `../05-component-library.md`) | Sau F4 |
| G5 | Mọi file `services/*.service.ts` có ít nhất 1 file `.spec.ts` cạnh nó | Script đối chiếu tên file | Sau F2 |
| G6 | Không import trực tiếp DTO trong `components/`/`pages/` (chỉ `services/` được import) | Grep `Dto` trong `components/`, `pages/` ngoài `services/` | Ngay từ đầu — đã PASS ở audit trước, giữ làm gate để không trôi |
| G7 | Bundle không vượt ngân sách | `angular.json` `budgets` — `ng build` fail khi vượt `maximumError` (xem `../13-performance.md` §4) | Sau F4 — chỉnh ngưỡng theo số đo thật, không giữ mặc định của `ng new` |
| G8 | `modules/<A>/` không import trực tiếp nội bộ `modules/<B>/` (module nghiệp vụ khác) | ESLint `eslint-plugin-import` rule `no-restricted-paths` — chặn import chéo giữa 2 module nghiệp vụ, vẫn cho phép import từ `core/`/`shared/`/`platform/` (xem `doc/kien-truc-core-module.md`) | Ngay khi có module nghiệp vụ thứ 2 (miễn phí trước đó, chưa có gì để vi phạm) |

## Vị trí chạy

Thêm 1 script `scripts/fe-gate.sh` (hoặc `.ps1`) gộp G1/G3/G6 (grep thuần,
rẻ) chạy trong CI trước `ng test`/`ng build` — G2/G4/G5 nếu cần ESLint rule
riêng thì cấu hình trong `.eslintrc`/`eslint.config.js`, chạy qua `ng lint`.

## Không làm

- Không viết gate cho tính năng chưa tồn tại (vd i18n runtime-switch chưa
  quyết định dùng — không viết gate cho nó).
- Không nới gate cho 1 module cụ thể bằng cách sửa gate lỏng đi — nếu 1
  module cần ngoại lệ tạm thời, loại trừ tường minh bằng comment + đường
  dẫn cụ thể trong script, không sửa rule chung.
