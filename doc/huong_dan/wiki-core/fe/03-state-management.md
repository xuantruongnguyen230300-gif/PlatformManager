# 3. State management — `signal()` trước, `signalStore()` khi cần

## Nguyên tắc

Angular Signals đã là cơ chế reactivity mặc định từ Angular 17+ — không cần
NgRx/Akita chỉ để "có state management chuẩn". Chỉ thêm `@ngrx/signals`
(`signalStore()`) khi state thật sự cần chia sẻ/derive phức tạp (xem
`src/FE/.claude/docs/architecture.md` §Khi nào cần `state/*.store.ts` —
đã CHỐT 2026-08-15).

## 2 cấp độ

| Cấp | Khi nào | Nơi khai |
|---|---|---|
| `signal()`/`computed()` trần | State cục bộ, chỉ 1 page dùng | Trực tiếp trong `pages/<feature>/<feature>.page.ts` |
| `signalStore()` | ≥2 component/page cùng đọc/ghi, hoặc cần cache qua lại giữa các lần điều hướng | `modules/<feature>/state/<feature>.store.ts` |

## Khuôn mẫu `signalStore()`

```ts
export interface CriteriaState {
  items: ICriteriaRow[];
  loading: boolean;
  error: string | null;
}

const initialState: CriteriaState = { items: [], loading: false, error: null };

export const CriteriaStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ items }) => ({
    doneCount: computed(() => items().filter(i => i.badge === 'done').length),
  })),
  withMethods((store, service = inject(CriteriaService)) => ({
    async load() {
      patchState(store, { loading: true, error: null });
      try {
        const items = await firstValueFrom(service.list());
        patchState(store, { items, loading: false });
      } catch (err) {
        patchState(store, { loading: false, error: (err as HttpErrorResponse).message });
      }
    },
  })),
);
```

## Quy tắc cứng

- `patchState` là **cách duy nhất** đổi state trong store — không expose
  `signal.set()` trực tiếp ra ngoài `withMethods`.
- Store **không gọi `HttpClient` trực tiếp** — luôn qua service của feature
  (giữ đúng ranh giới `services/` trong `architecture.md`).
- Component dumb (`components/`) **không** inject store — chỉ `pages/`
  (smart) được inject, đúng bảng trách nhiệm đã có.
- 1 store/feature — không dùng 1 store toàn cục cho nhiều feature không
  liên quan (god store lặp lại đúng lỗi "god component" đã cấm).

## Test

`unprotected(store)` (API của `@ngrx/signals/testing`) để set state trực
tiếp trong spec mà không phải giả lập cả chuỗi gọi API — dùng khi test
component tiêu thụ store, không phải khi test chính store (test store qua
`withMethods` thật, có mock service inject vào).

## Package

`@ngrx/signals` thêm vào `package.json` khi feature đầu tiên thật sự cần
store — không cài trước khi có nhu cầu (đúng nguyên tắc Nhóm A/B).
