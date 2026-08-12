# Architecture — src/FE

## Tầng app

```
src/app/
├── core/       # singleton toàn app: auth, guard, interceptor, HTTP client dùng chung
├── shared/     # dumb UI component tái dùng > 1 feature (button, badge, card...)
└── modules/    # từng feature, lazy-loaded
```

`core/` và `shared/` là **cross-cutting** — không đặt logic riêng của một
feature vào đây. Nếu một service/component chỉ dùng bởi đúng 1 feature, nó
thuộc về `modules/<feature>/`, không phải `core/`/`shared/`.

## Cấu trúc một feature

```
modules/<feature>/
├── <feature>.routes.ts             # lazy routes riêng của feature
├── pages/<feature>/                # SMART — route target
├── components/<x>/                 # DUMB — chỉ input()/output()
├── services/<feature>.service.ts   # data access + mapper
├── models/<feature>.model.ts       # interface/type
└── state/ (tuỳ chọn)                # signal store khi state đủ phức tạp
```

## Bảng trách nhiệm — quy tắc cứng

| Tầng | Được phép | Cấm |
| --- | --- | --- |
| `pages/*` (smart) | inject store/service, bind signal, điều hướng | gọi `HttpClient` trực tiếp; logic nghiệp vụ nặng |
| `components/*` (dumb) | nhận `input()`, phát `output()`, render | inject data service; biết HTTP / state global |
| `services/*` | gọi API, map DTO↔model | giữ UI state |
| `state/*.store.ts` | `signal`/`computed`, orchestrate service | render, đụng DOM |
| `models/*` | type / interface | logic |

## Khi nào cần `state/*.store.ts`

**Không** tạo signal store mặc định cho mọi feature. Chỉ thêm khi có ≥1 trong
các điều kiện sau:
- Nhiều component/page trong cùng feature cần đọc/ghi chung một state.
- State cần derive qua nhiều bước `computed()` lồng nhau.
- Cần cache giữa các lần điều hướng qua lại.

Feature đơn giản (1 page, state cục bộ) chỉ cần `signal()` khai trực tiếp
trong `pages/<feature>/`.

## Chốt chặn chống god component

- Soft cap **~300–400 dòng/component**. Vượt → tách `components/` con.
- Không bao giờ để bản `-v2` song song một component/service. Sửa tại chỗ;
  lịch sử nằm trong git.
- Component dumb chỉ nhận `input()`/phát `output()` — không tự inject
  service để tự lấy dữ liệu. Nếu thấy mình đang làm vậy, đó là dấu hiệu nó
  nên là smart component (`pages/`), không phải dumb.

## Routing

- Mỗi feature export `<FEATURE>_ROUTES` từ `<feature>.routes.ts`.
- `app.routes.ts` đăng ký bằng `loadChildren` (lazy) — không import trực tiếp
  component của feature vào `app.routes.ts`.
- Guard đặt **trong** route của feature, không dựa vào cấu hình rời rạc ở
  `app.routes.ts`.
