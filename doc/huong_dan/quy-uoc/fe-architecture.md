# Architecture — src/FE

> Xem trước **`doc/kien-truc-core-module.md`** (root repo) để hiểu lý do và
> nguồn tham khảo thực tế đằng sau ranh giới `platform/` ↔ `modules/` dưới
> đây — file này chỉ nêu quy tắc thực thi, không lặp lại phần lý luận.

## Tầng app

```
src/app/
├── core/       # singleton toàn app: auth, guard, interceptor, HTTP client dùng chung
├── shared/     # dumb UI component tái dùng > 1 feature (button, badge, card...)
├── platform/   # màn hình "Core" (đăng nhập, đổi mật khẩu, quản trị người dùng, phân quyền)
│               # — dùng lại được cho mọi sản phẩm dựng trên nền tảng này, KHÔNG phải nghiệp vụ
└── modules/    # module NGHIỆP VỤ (dashboard, danh-muc-dti...) — lazy-loaded, mỗi module 1 domain
```

`core/` và `shared/` là **cross-cutting** — không đặt logic riêng của một
feature vào đây. Nếu một service/component chỉ dùng bởi đúng 1 feature, nó
thuộc về `platform/<feature>/` hoặc `modules/<feature>/` (tuỳ có phải
nghiệp vụ hay không), không phải `core/`/`shared/`.

`platform/` và `modules/` dùng **chung 1 cấu trúc con** (xem mục dưới) —
khác biệt duy nhất là ý nghĩa: `platform/*` là màn hình nền tảng (áp dụng
cho mọi sản phẩm dựng trên core này), `modules/*` là màn hình đặc thù 1
domain nghiệp vụ. Thêm feature mới → tự hỏi "màn này có ý nghĩa với MỌI sản
phẩm dựng trên nền tảng, hay chỉ riêng domain nghiệp vụ hiện tại?" để chọn
đúng chỗ đặt, không đoán.

**Ranh giới bắt buộc (ESLint gate G8 — xem
`doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md`)**: 1 module nghiệp vụ
trong `modules/<A>/` không được import trực tiếp nội bộ
`modules/<B>/` (module nghiệp vụ khác) — chỉ được import từ `core/`,
`shared/`, `platform/`. Cần dùng chung logic giữa 2 module nghiệp vụ → đưa
lên `shared/`/`core/` nếu thật sự generic, không import chéo.

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

**Đã CHỐT (2026-08-15):** khi cần store, dùng `signalStore()` của
`@ngrx/signals` — không tự chế store bằng `signal()` trần rải trong service.
Lý do chuẩn hoá: 1 pattern duy nhất cho mọi feature (`withState`/
`withComputed`/`withMethods`), dễ test, không để mỗi feature tự nghĩ ra 1
kiểu "store" khác nhau khi hệ thống lớn dần.

**Không** tạo store mặc định cho mọi feature. Chỉ thêm khi có ≥1 trong các
điều kiện sau:
- Nhiều component/page trong cùng feature cần đọc/ghi chung một state.
- State cần derive qua nhiều bước `computed()` lồng nhau.
- Cần cache giữa các lần điều hướng qua lại.

Feature đơn giản (1 page, state cục bộ) chỉ cần `signal()` khai trực tiếp
trong `pages/<feature>/` — **không** bọc `signalStore()` cho state chỉ 1 nơi
dùng.

```ts
// modules/<feature>/state/<feature>.store.ts
export const FeatureStore = signalStore(
  { providedIn: 'root' },
  withState<FeatureState>({ items: [], loading: false }),
  withComputed(({ items }) => ({ total: computed(() => items().length) })),
  withMethods((store, service = inject(FeatureService)) => ({
    async load() {
      patchState(store, { loading: true });
      const items = await firstValueFrom(service.list());
      patchState(store, { items, loading: false });
    },
  })),
);
```

Package `@ngrx/signals` thêm vào `package.json` khi feature đầu tiên thật
sự cần store — không cài trước khi có nhu cầu.

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
