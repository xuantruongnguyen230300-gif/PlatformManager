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
| G9 | `core/` KHÔNG import ngược lên `shared/`/`platform/`/`modules/` — `core/` là tầng đáy | ESLint `import/no-restricted-paths`, zone `target: ./src/app/core` (`eslint.config.js` — hằng `coreLayerZones`), chạy qua `ng lint` | **Đã bật 2026-08-21.** Dựng ngay khi có ngoại lệ đầu tiên — đã xảy ra thật: `core/auth` import `MenuService` và `core/interceptors` import `ToastService` từ `shared/services/`, cả hai nay đã chuyển vào `core/menu`+`core/toast` |

## Vị trí chạy — 🛑 CHẠY TAY, repo KHÔNG có CI

**Không còn `.github/`** (người dùng xoá 2026-08-21, có chủ đích). Không có
máy nào tự chạy gate — **người chạy tay trước khi commit**:

```bash
cd src/FE
bash ../../scripts/fe-gate.sh                                   # G1 + G3 + G6
npx ng lint                                                      # G2 + G8 + G9
npx ng test --watch=false --browsers=ChromeHeadless               # test
npx ng build                                                     # G7 (budget)
```

`CHROME_BIN` phải trỏ tới Chrome nếu shell chưa export sẵn — thiếu nó Karma
hỏng với *"No binary for ChromeHeadless"*.

> ### ⚠️ Đây là điểm yếu đã biết, không phải thiếu sót chưa ai thấy
>
> Toàn bộ file này tồn tại vì một bài học: **G1 từng được dọn tay 2 lần và tự
> tái sinh cả 2 lần** — hex mới xuất hiện ngay ở đợt màn hình kế tiếp, đúng vì
> không có máy kiểm. `scripts/fe-gate.sh` sinh ra để chấm dứt việc đó.
>
> Nay không còn CI, gate quay lại phụ thuộc **trí nhớ con người** — tức đúng
> điều kiện đã sinh ra vấn đề ban đầu. Script vẫn giữ và vẫn chạy được; nhưng
> đừng nhầm "có script" với "có gate". Dựng lại CI thì nối 4 lệnh trên vào,
> thứ tự gate → lint → test → build để fail nhanh nhất.
>
> Hai điểm đã kiểm chứng bằng canary khi viết script, ghi lại để không ai
> "đơn giản hoá" ngược lại:
>
> - Mẫu G6 phải là `Dto\b`, **không phải** `\bDto\b`. Tên DTO thật luôn dạng
>   `IUserDto` — giữa `r` và `D` không có word boundary nên `\bDto\b` không bao
>   giờ khớp, gate xanh vì mù chứ không vì sạch.
> - G6 loại trừ `*.spec.ts`. Đây là **phạm vi đúng** của rule chứ không phải
>   ngoại lệ: G6 bảo vệ đường code chạy thật, còn spec stub tầng HTTP thì bắt
>   buộc phải dựng payload đúng hình dạng wire, tức phải nói bằng DTO.
>
> G5 vẫn chưa hiện thực.

## Không làm

- Không viết gate cho tính năng chưa tồn tại (vd i18n runtime-switch chưa
  quyết định dùng — không viết gate cho nó).
- Không nới gate cho 1 module cụ thể bằng cách sửa gate lỏng đi — nếu 1
  module cần ngoại lệ tạm thời, loại trừ tường minh bằng comment + đường
  dẫn cụ thể trong script, không sửa rule chung.
