# 3. State management — `signal()` trước, `signalStore()` khi cần

## Nguyên tắc

Angular Signals đã là cơ chế reactivity mặc định từ Angular 17+ — không cần
NgRx/Akita chỉ để "có state management chuẩn". Chỉ thêm `@ngrx/signals`
(`signalStore()`) khi state thật sự cần chia sẻ/derive phức tạp (xem
`doc/huong_dan/quy-uoc/fe-architecture.md` §Khi nào cần `state/*.store.ts` —
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
        patchState(store, { loading: false, error: (err as ApiHttpError).apiResult?.message ?? 'Đã có lỗi xảy ra.' });
      }
    },
  })),
);
```

> ⚠️ **Đọc `apiResult.message`, KHÔNG đọc `HttpErrorResponse.message`.** Cái sau là
> chuỗi Angular tự sinh (`"Http failure response for /api/…: 409 Conflict"`) — không
> phải `message` nghiệp vụ trong envelope. Lấy nhầm là rơi đúng lỗi *"message bị thay
> bằng câu chung chung"* mà [02-http-envelope.md](02-http-envelope.md) lấy làm lý do tồn
> tại. `ApiHttpError` khai ở `core/http/` — xem file đó. *(Sửa 2026-08-23.)*

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

## `effect()` trong service singleton — rủi ro sống mãi không dọn

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: file
> này đã bàn `signalStore()` khai ở đâu, nhưng chưa bàn cleanup của
> `effect()` — cơ chế tự huỷ theo injection context của Angular Signals **áp
> dụng khác nhau** giữa component (sống ngắn, tự destroy) và service
> `providedIn: 'root'` (sống suốt vòng đời app).

Có 2 rủi ro riêng biệt, dễ nhầm làm một:

**(1) Gọi `effect()` ngoài injection context ném lỗi lúc chạy, không phải
lúc biên dịch** — cùng dạng bẫy với `inject()` trong `catchError` đã nêu ở
[02-http-envelope.md](02-http-envelope.md):

```ts
@Injectable({ providedIn: 'root' })
export class SessionActivityService {
  private readonly lastActivity = signal(Date.now());

  constructor() {
    effect(() => console.debug('activity at', this.lastActivity()));   // ✅ constructor = injection context
  }

  trackClick(): void {
    effect(() => console.debug(this.lastActivity()));   // ❌ NG0203 lúc chạy — không có injection context ở đây
  }
}
```

Gọi ngoài constructor mà vẫn cần thì phải tự truyền `injector`:

```ts
private readonly injector = inject(Injector);

trackClick(): void {
  effect(() => console.debug(this.lastActivity()), { injector: this.injector });
}
```

**(2) `effect()` tạo trong service root **không bao giờ tự destroy**** —
đúng theo thiết kế (service sống bằng đời app), nhưng khác hẳn effect trong
component (tự dọn khi component bị destroy). Nếu cần dừng effect theo 1 sự
kiện nghiệp vụ cụ thể (vd ngừng theo dõi hoạt động lúc logout) thì phải tự
giữ `EffectRef` và gọi `.destroy()` thủ công — không có cơ chế nào làm việc
này thay:

```ts
private activityEffect?: EffectRef;

constructor() {
  this.activityEffect = effect(() => { /* ... */ });
}

stopTracking(): void {
  this.activityEffect?.destroy();   // dọn thủ công — service singleton không tự làm khi logout
}
```

## Đồng bộ state giữa nhiều tab trình duyệt — chấp nhận KHÔNG đồng bộ, có chủ đích

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `signalStore({ providedIn: 'root' })` sống trong đúng 1 JS runtime — mỗi
> tab trình duyệt là 1 instance Angular tách biệt hoàn toàn về bộ nhớ. Sửa
> dữ liệu ở tab A, `CriteriaStore` ở tab B **không hề biết**.

Ở quy mô 5-15 dev, user thật (không phải hyperscale): đây là đánh đổi **chấp
nhận được** cho phần lớn tính năng — mở 2 tab cùng 1 hệ thống quản trị nội
bộ là tình huống hiếm, không phải luồng nghiệp vụ chính, và tự F5 tab cũ khi
cần dữ liệu mới là chi phí UX nhỏ. Không cần `signalStore()` nào cũng tự
đồng bộ đa tab mặc định — làm vậy tốn công cho một rủi ro hiếm.

Chỉ đáng làm khi có luồng thật cần (vd 1 màn dashboard mở song song 1 màn
sửa dữ liệu). Cách rẻ nhất **không phải** broadcast toàn bộ state (dễ lệch
schema giữa các tab nếu tab cũ chưa load lại code mới, và tốn hơn gọi lại
API với danh sách nhiều dòng) mà là báo **"đã đổi, tự load lại"** qua
`BroadcastChannel`:

```ts
// core/sync/cross-tab-invalidate.service.ts
@Injectable({ providedIn: 'root' })
export class CrossTabInvalidateService {
  private readonly channel = new BroadcastChannel('platform-manager-data-sync');

  notifyChanged(resource: string): void {
    this.channel.postMessage({ resource, at: Date.now() });
  }

  onChanged(resource: string, cb: () => void): void {
    this.channel.addEventListener('message', (e) => {
      if (e.data.resource === resource) cb();
    });
  }
}
```

```ts
withMethods((store, service = inject(CriteriaService), sync = inject(CrossTabInvalidateService)) => ({
  async save(payload: ICriteriaRow) {
    await firstValueFrom(service.save(payload));
    sync.notifyChanged('criteria');   // các tab khác tự gọi lại load(), KHÔNG nhận state trực tiếp qua message
  },
})),
```

## Optimistic update — rollback khi API thất bại

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: khuôn
> mẫu `load()` ở trên chờ API xong mới `patchState` — an toàn nhưng với thao
> tác ghi đơn giản (toggle 1 field, đổi thứ tự), chờ round-trip trước khi UI
> phản hồi làm thao tác cảm giác chậm hơn cần thiết.

Chỉ optimistic cho thao tác **đơn giản, dễ đảo ngược** (1 field, 1 dòng) —
không optimistic cho thao tác nhiều bước/nhiều field, vì khi đó "rollback về
đâu" không còn rõ ràng:

```ts
withMethods((store, service = inject(CriteriaService)) => ({
  async toggleActive(id: string) {
    const previous = store.items();                              // chụp state TRƯỚC khi đổi
    const next = previous.map(i => i.id === id ? { ...i, active: !i.active } : i);
    patchState(store, { items: next });                           // cập nhật UI NGAY, không chờ API

    try {
      await firstValueFrom(service.toggleActive(id));
    } catch (err) {
      patchState(store, {
        items: previous,   // rollback về snapshot đã chụp — KHÔNG đảo lại field 1 lần nữa
        error: (err as ApiHttpError).apiResult?.message ?? 'Đã có lỗi xảy ra.',
      });
    }
  },
})),
```

Rollback phải dùng **snapshot `previous` đã chụp trước đó**, không tính lại
bằng cách đảo ngược field (`!i.active` lần nữa): giữa lúc chờ API, có thể có
1 `patchState` khác xen vào (user bấm toggle dòng khác trong lúc dòng này
đang chờ) — đảo ngược thủ công dễ đưa `items` về sai trạng thái, còn gán lại
đúng `previous` luôn đúng bất kể có gì xen giữa.
