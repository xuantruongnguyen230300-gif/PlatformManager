---
kind: luat
scope: core
verified: 2026-09-06
---

# API Client & Wire Boundary — src/FE

## Vì sao cần tách DTO khỏi model app

TypeScript bị xoá lúc chạy (erased types) — đổi tên field của type mô tả
payload API mà không sửa chỗ dùng nó = **vỡ runtime im lặng, build vẫn
xanh**. Tách DTO (mô tả đúng những gì server trả) khỏi model app (dùng nội
bộ UI) và bắt buộc có mapper ở giữa là cách duy nhất chặn lỗi này một cách
đáng tin cậy.

## Quy tắc casing

| Nguồn dữ liệu | Casing | Ghi chú |
| --- | --- | --- |
| API `src/BE` | **`camelCase`** | Đã CHỐT 2026-08-15 — `Program.cs` đặt `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` ở **hai** chỗ, một cho MVC một cho `Http.Json` (`grep -n PropertyNamingPolicy src/BE/PlatformManager.Api/Program.cs`). **Ngoại lệ duy nhất: key của `fields`** — xem cảnh báo ở §Envelope |
| JSON tĩnh (`public/*.json`) | như file gốc | `public/` là gốc bundle — `public/x.json` phục vụ tại `/x.json`, xem [`fe-architecture.md`](fe-architecture.md) §Cây thư mục |
| Model app (bạn tự định nghĩa) | `PascalCase` + prefix `I` | `ICurrentUser.UserName` — mapper là nơi đổi casing |

> *(Sửa 2026-08-23: bản trước ghi API trả `PascalCase` — tàn dư từ trước quyết
> định 2026-08-15, và mâu thuẫn với chính ví dụ envelope ở §dưới. DTO thật trong
> `src/FE` đều camelCase: `ILoginRequestDto.userName`, `PagedList.totalCount`.
> Viết DTO theo PascalCase sẽ nhận `undefined` lúc chạy mà build vẫn xanh — đúng
> loại lỗi mà chính file này mở đầu bằng cách cảnh báo.)*

## Quy tắc cứng

1. Wire type (DTO) giữ **nguyên xi** casing server trả về — tức **`camelCase`**.
   Prefix `I`, hậu tố `Dto` (vd. `IPositionDto`), khớp `ILoginRequestDto` /
   `PagedList` (shape chốt ở `be-cqrs-handler.md` §Shape phân trang).
2. Model app: `interface` prefix `I`, field **`PascalCase`**, **không** hậu tố
   (vd. `IPositionRow`).
3. Mapper đặt trong `services/` của feature, cạnh service gọi API. **Đây là nơi
   duy nhất casing đổi** — camelCase (dây) → PascalCase (app):
   ```ts
   function mapPositionDtoToRow(dto: IPositionDto): IPositionRow {
     return { Id: dto.id, Name: dto.name, Status: dto.status };
   }
   ```
4. Component **không bao giờ** import hay chạm trực tiếp vào DTO — chỉ thấy
   model app.
5. Giữ 2 type + mapper **ngay cả khi chúng trông giống hệt nhau** lúc mới
   viết — DTO thuộc về server, model thuộc về app; gộp lại mất điểm chặn khi
   server đổi field sau này.

## Envelope response từ BE — `IApiResult<T>`

**Đã CHỐT (2026-08-15):** BE trả về đúng shape sau cho MỌI endpoint (xem
`doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response) — FE phải có
interface khớp 1:1, không tự đặt tên field khác:

> ### 🔄 LẬT 2026-09-06 — bản chép `interface IApiResult<T>` ở đây đã bị GỠ
>
> Chỗ này từng khai lại nguyên interface. Bản chép đó **đã lệch ba lần** so với code thật:
> thiếu `fieldErrors` (về 2026-09-03), thiếu `messageParams` (2026-09-04) và thiếu nhánh
> `'MethodNotAllowed'` trong union `code` (2026-09-04). Không gate nào bắt được, vì mỗi bản
> đọc riêng đều hợp lệ.
>
> Đây là **lần thứ hai** cùng một bản chép gây lệch: bản trong
> [`../wiki-core/fe/02-http-envelope.md`](../wiki-core/fe/02-http-envelope.md) đã bị gỡ vì đúng
> lý do này ngày 2026-09-04, nhưng bản ở file này thì không ai đụng tới. Đúng cơ chế
> [`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5 mô tả: giữ hai bản "cho chắc" thì bản
> không ai nhớ sẽ nói dối.
>
> **Hình dạng envelope có đúng MỘT file chủ:**
> [`be-api-controller.md`](be-api-controller.md) §Envelope response.
> **Bản mirror sống của FE là code**, không phải một danh sách trong tài liệu:
> `src/FE/src/app/core/http/api-result.model.ts`.

```bash
# Cần biết envelope hôm nay có trường gì — đọc thẳng, đừng tra tài liệu:
sed -n '/export interface IApiResult/,/^}/p' src/FE/src/app/core/http/api-result.model.ts
```

> ### ⚠️ `fields` dùng PascalCase, phần còn lại của payload là camelCase
>
> Không phải bug, không phải sót. BE cố ý giữ `DictionaryKeyPolicy = null` để key khớp
> **tên property C# gốc** (`MaxScore`, `UserName`) — thứ mà form phía FE cần để bind lỗi
> đúng ô. Đọc `fields['MaxScore']`, **đừng** tự camelCase lại.
>
> Nếu thấy phía BE có ai định set `DictionaryKeyPolicy = CamelCase` "cho nhất quán" —
> đó là thay đổi phá vỡ hợp đồng này và **không test nào bắt được**. Lý do đầy đủ ở
> `doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response.

- Đọc `message` để hiển thị cho user — **không phải** `Message`/`ErrorMessage`
  (tên field của envelope cũ, đã bỏ cùng lúc BE đổi sang `IApiResult<T>`).
- So sánh lỗi cụ thể dùng `businessCode` (chuỗi ổn định) — **không** so `message`
  (chuỗi hiển thị, đổi theo câu chữ UI). Ca thật đang chạy:
  `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts:52` so
  `'PERMISSION.VERSION_CONFLICT'`. 🔄 LẬT 2026-09-06: ví dụ cũ ở đây là
  `"CRITERIA.DUPLICATE_CODE"` — miền `CRITERIA` không còn màn hình nào (gỡ 2026-08-29).
- `fields` bind trực tiếp vào lỗi từng control trên form — không gộp chung
  vào 1 toast nếu BE đã trả `fields` cụ thể cho từng ô.
- Interceptor lỗi HTTP dùng chung (`core/interceptors/http-error.interceptor.ts`)
  đọc `IApiResult<T>` này để dựng thông báo — **không** tự đoán field, và
  không còn field `Message`/`Success` cũ để đọc nhầm.

## Auth — cookie session

**Đã CHỐT (2026-08-15):** dùng cookie session của ASP.NET Core Identity
— không tự lưu JWT bearer. Hệ quả cho FE:

- Cấu hình `HttpClient` gửi kèm cookie mỗi request (`withCredentials`, qua
  `provideHttpClient(...)` hoặc tương đương) — thiếu bước này, request luôn
  bị coi là chưa đăng nhập dù đã login.
- **Không** tự lưu token vào `localStorage`/biến JS — cookie do trình duyệt
  quản lý.
- Phía BE phải bật CORS kèm `AllowCredentials()` cho đúng origin FE —
  **không** dùng chung với `AllowAnyOrigin()` (2 cấu hình loại trừ nhau ở
  ASP.NET Core, xem `doc/huong_dan/quy-uoc/be-api-controller.md` §CORS).

## Service pattern

```ts
@Injectable({ providedIn: 'root' })
export class PositionService {
  private readonly http = inject(HttpClient);

  list(params: IListPositionsParams): Observable<IPositionRow[]> {
    return this.http
      .post<IApiResult<IPositionDto[]>>('/positions/list', params)   // đường dẫn NGẮN, có envelope
      .pipe(map(res => unwrapData(res).map(mapPositionDtoToRow)));   // KHÔNG `res.data!`
  }
}
```

> ### 🔄 LẬT 2026-09-06 — mẫu cũ sai **hai** chỗ, cả hai đều sinh code chạy hỏng
>
> **(a) Đường dẫn KHÔNG được mang tiền tố `/api`.** Mẫu cũ viết `'/api/positions/list'`.
> `apiBaseUrlInterceptor` ghép `environment.apiBaseUrl` vào trước mọi URL tương đối, mà giá trị
> đó **đã chứa `/api`** (`src/FE/src/environments/environment.ts`). Chép mẫu cũ ⇒ request bay
> tới `/api/api/...` ⇒ 404 trông hệt như "BE chưa làm endpoint". Mọi service thật đang theo
> đúng luật này:
>
> ```bash
> grep -rn "this.http\.\(get\|post\|put\|delete\)<" src/FE/src --include=*.ts | grep -v spec
> # PASS: không dòng nào chứa '/api/
> ```
>
> **(b) Kiểu trả về phải bọc envelope.** Mẫu cũ khai `.post<IPositionDto[]>(...)` rồi map thẳng
> `dtos.map(...)` — tức nói rằng BE trả mảng trần. Sai với chính §Envelope ngay phía trên file
> này: **mọi** endpoint trả `IApiResult<T>`. Chép mẫu cũ ⇒ `dtos.map is not a function` lúc chạy.

**`unwrapData()` là bắt buộc, không phải tiện tay.** Đọc `data` bằng
`src/FE/src/app/core/http/api-result.model.ts` → `unwrapData(res)`, **không** `res.data!` và
không `res.data as T`: hai dạng đó tắt hẳn kiểm tra kiểu và biến "envelope thiếu `data`" thành
một lỗi vô nghĩa tận trong mapper. `unwrapData` chỉ coi `null`/`undefined` là thiếu — `false`,
`0`, `''` vẫn là giá trị hợp lệ.

### 🛑 `res.data ?? <giá trị mặc định>` cũng bị cấm — và đây là dạng nguy hiểm nhất

Thêm 2026-09-08. Câu cấm cũ chỉ gọi tên `res.data!` và `res.data as T`, nên dạng `??` **lọt qua
đúng nghĩa đen** dù nó tệ hơn cả hai: `!` và `as T` ít nhất còn để lỗi nổ ra ở đâu đó, còn `??`
thì **không có lỗi nào cả**.

| Dạng | Envelope thiếu `data` ⇒ chuyện gì xảy ra |
| --- | --- |
| `res.data!` / `res.data as T` | Nổ muộn, ở tận trong mapper, câu lỗi vô nghĩa |
| `res.data ?? { items: [], … }` | **Không nổ.** Giao diện hiện "Không có dữ liệu" — hợp lệ y như thật |
| `unwrapData(res)` | Nổ ngay, đúng chỗ, có `traceId`, rơi vào nhánh lỗi sẵn có của trang |

Vấn đề không phải giá trị mặc định xấu — mà là **giá trị mặc định trùng khít với một câu trả lời
hợp lệ của server**. Lưới rỗng, menu rỗng, số 0: đó đều là những thứ server có quyền trả về thật.
Khi envelope hỏng cũng cho ra đúng hình ảnh đó, không ai — người dùng, người trực hệ thống, hay
chính người viết code — phân biệt được "không có gì" với "hỏng". Lỗi loại này không vào log, không
vào toast, không vào test; nó chỉ vào **quyết định sai của người đang nhìn màn hình**.

```bash
grep -rn "\.data\s*??" src/FE/src --include=*.ts
# PASS: không dòng nào. Mọi chỗ đọc `data` phải đi qua `unwrapData()`.
```

> #### ✅ CÓ THẬT — đối chiếu 2026-09-10
>
> Lệnh grep trên **PASS** (0 dòng, chạy lại 2026-09-10). Chỗ vi phạm cuối cùng ở CODE —
> `core/menu/menu.service.ts` — nay đọc `data` qua
> `unwrapData(res)` (`src/FE/src/app/core/menu/menu.service.ts:162`).
>
> Chú ý khi viết chú thích: nhắc đến dạng bị cấm bằng **cách gọi tên** ("toán tử hợp nhất-null kèm
> giá trị mặc định") chứ đừng gõ lại nguyên văn nó trong comment — lệnh grep không phân biệt code
> với comment, và một chú thích tử tế sẽ làm cổng đỏ mà không có gì hỏng cả.
>
> 🔄 **Ngày đối chiếu nâng từ 2026-09-08 lên 2026-09-10, và nó từng SAI trong khoảng giữa.** Lượt
> dựng FE vòng 1 của cụm DTI viết hai chú thích gõ **nguyên văn** dạng bị cấm
> (`modules/danh-muc-dti/services/danh-muc-dti.service.ts`, `shared/services/dti-period.service.spec.ts`),
> nên lệnh trên trả về **2 dòng** trong khi nhãn ở đây vẫn nói PASS — tức nhãn `✅ CÓ THẬT` mô tả
> một phép đo không còn đúng. Đáng ghi lại vì đây là **chính cái bẫy mà đoạn ngay trên vừa cảnh
> báo**: người viết đọc cảnh báo, hiểu nó, rồi vẫn dính — bằng chứng rằng một dòng cảnh báo không
> thay được việc chạy lại lệnh. Hai chú thích đã đổi sang cách gọi tên; lệnh chạy lại về 0 dòng
> **trước khi** ngày ở dòng này được sửa.

### Đường lùi đặt ở đâu — nơi gọi là mặc định, service là NGOẠI LỆ có điều kiện

Chốt 2026-09-08, sau ca `MenuService`. Trước đó mục này chỉ có một câu — *"đặt đường lùi ở nơi
gọi, không nuốt lỗi tại service"* — và câu đó không đủ: nó đúng cho lưới dữ liệu, sai cho khung
điều hướng, mà người đọc thì không có cách nào biết mình đang ở ca nào.

| | Đặt ở **nơi gọi** (mặc định) | Đặt trong **service** (ngoại lệ) |
| --- | --- | --- |
| Áp cho | Service nghiệp vụ của một màn hình | Thành phần **hạ tầng dùng chung** mà hỏng nó không được phép chặn cả ứng dụng |
| Vì sao | Chỉ trang đó biết "hỏng thì hiện gì" — khối lỗi, giữ lại dữ liệu cũ, hay mời thử lại | Nơi gọi có **nhiều** và không cố định; bắt mỗi nơi tự dựng lại cùng một handler thì quên một chỗ là một lỗi không ai bắt |
| Ví dụ có thật | `getList` của `platform/quan-tri-nguoi-dung/services/quan-tri-nguoi-dung.service.ts` — trang đã có sẵn `catchError` → khối lỗi | `getMenu()` của `src/FE/src/app/core/menu/menu.service.ts` — menu là khung điều hướng của **mọi** màn hình |

🛑 **Ngoại lệ chỉ có hiệu lực kèm điều kiện: lỗi phải HIỆN RA đúng một lần trước khi trả giá trị
lùi.** Không có điều kiện này thì "đường lùi trong service" chính là `??` đã cấm ở trên, chỉ khác
tên gọi — vẫn là một giá trị mặc định trùng khít với câu trả lời hợp lệ, vẫn không ai phân biệt
được "không có gì" với "hỏng".

Hình dạng đã thi công ở `MenuService` (`src/FE/src/app/core/menu/menu.service.ts:187`): `catchError`
toast **một lần** rồi `return of([])`; và toast chỉ bắn cho lỗi **không phải** `HttpErrorResponse`,
vì lỗi HTTP thì `httpErrorInterceptor` đã toast rồi — hai toast cho một sự cố cũng là hỏng, chỉ
theo chiều ngược lại. Chuỗi đi qua khoá i18n `shared.menu.loadFailed`
(`src/FE/public/i18n/vi.json:270`, `src/FE/public/i18n/en.json:270`).

Ba test khoá cả ba vế, ở `src/FE/src/app/core/menu/menu.service.spec.ts:203`, `:254` và `:269`.

Nhờ đường lùi nằm trong service, nơi gọi không cần đổi gì —
`src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts:284` vẫn là
`this.menu.refresh().subscribe()` trần, và đó là kết quả mong muốn chứ không phải nợ bỏ sót.

**Đừng viện dẫn ca này để đặt `??` ở chỗ khác.** Phép thử trước khi chép: *service này hỏng thì
người dùng có mất luôn khả năng dùng phần còn lại của app không?* Không → đường lùi thuộc nơi gọi.

- Mỗi feature có 1 service riêng (`services/<feature>.service.ts`) — không
  gom nhiều feature vào một "god service".
- Base URL API cấu hình qua `environment.ts` — **không hardcode URL** trong
  từng service.
- Lỗi HTTP xử lý qua interceptor dùng chung ở `core/` — service của feature không tự bắt lỗi
  HTTP lặp lại logic đó. 🔄 LẬT 2026-09-06: danh sách cũ kể *"retry, refresh token nếu có
  auth, log lỗi"*. **Không có refresh token** ở dự án này (cookie session, KHÔNG JWT — xem
  §Auth ngay trên), và **retry chưa thi công** (📐 đích đến, xem
  [`../wiki-core/fe/02-http-envelope.md`](../wiki-core/fe/02-http-envelope.md) §Retry). Thứ
  interceptor thật sự làm hôm nay: dịch lỗi thành toast + xử lý 401 phiên chết.

## Khi endpoint chưa tồn tại

Đừng tự đoán shape response rồi code như thật. Viết **API Contract Card**
vào `doc/contracts/<feature>.md`, mỗi endpoint một card, để `backend-expert`
review và chốt `AGREED` trước khi implement thật.

> **Chuyển về đây 2026-09-03.** Mẫu này trước chỉ sống trong
> `.claude/agents/frontend-expert.md`, và chính file này trỏ NGƯỢC vào đó để lấy —
> vi phạm [`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §3 (*nội dung vào `doc/`,
> chỉ đường dẫn vào `.claude/`*). Nó là tri thức đúng nghĩa: mọi dòng dưới đây **có
> thể trở thành sai khi code đổi** — và đã sai thật một lần, xem ghi chú về khuôn mã
> lỗi bên dưới.

### Mẫu card

```markdown
## CONTRACT <id> — <mô tả ngắn>
- Status: DRAFT | AGREED | IMPLEMENTED
- Owner FE: src/FE/src/app/platform/<feature>/services/<feature>.service.ts
- Route:   POST /api/<resource>/list
- Verb:    POST
- Request  (FLAT — không bọc { Request: {...} }):
    page: int = 1 · pageSize: int = 20 · searchText: string?
- Response:
    id: guid · code: string · name: string · status: string
- Lỗi mong đợi: <MIỀN>.<MÃ_LỖI> — vd USER.NOT_FOUND (404) · USER.DUPLICATE_CODE (409)
- Ghi chú: <phân trang, sắp xếp, ràng buộc nghiệp vụ>
```

> **Khuôn mã lỗi sửa 2026-09-03.** Bản trước của mẫu ghi `<ENTITY>_NOT_FOUND` — khuôn
> **không có dấu chấm**, mà `ErrorCatalogTests` liệt đúng khuôn đó là *phải từ chối*.
> Khuôn đang thi hành là `MIỀN.MÃ_LỖI`; file chủ:
> [`be-cqrs-handler.md`](be-cqrs-handler.md) §ErrorDescriptor. Đây là ví dụ sống cho
> lý do §3 tồn tại: mẫu nằm sai chỗ nên đợt thống nhất mã lỗi không ai sửa tới nó.

**Quy tắc bàn giao:**

1. FE viết card ở trạng thái `DRAFT` → `backend-expert` review, chỉnh, chuyển `AGREED`.
2. **FE không tự code call khi card còn `DRAFT`** — trừ khi chấp nhận sửa lại.
3. Card `AGREED` là nguồn sự thật cho cả hai bên. Đổi contract phải sửa card trước.

## Secrets

`environment.ts` (và mọi file `environment.*.ts`) **không bao giờ** chứa API
key/secret thật được commit vào git. Dùng biến môi trường lúc build hoặc một
cơ chế secret riêng — hỏi người dùng nếu chưa có quy ước cho dự án.

## Long-running operation — poll pattern

Khi BE trả **202 + `jobId`** thay vì đợi xử lý xong (xem
`doc/huong_dan/quy-uoc/be-cqrs-handler.md` §"Command chạy lâu → job nền" — ca đầu
tiên: Import CSV/Excel), FE gọi 2 bước thay vì 1:

```ts
@Injectable({ providedIn: 'root' })
export class ImportService {
  private readonly http = inject(HttpClient);

  startImport(file: File): Observable<{ JobId: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<IApiResult<{ jobId: string }>>('/import', formData)
      .pipe(map(res => ({ JobId: unwrapData(res).jobId })));
  }

  getImportJobStatus(jobId: string): Observable<IImportJobStatus> {
    return this.http
      .get<IApiResult<IImportJobStatusDto>>(`/import/${jobId}`)
      .pipe(map(res => mapImportJobStatusDtoToModel(unwrapData(res))));
  }
}
```

```ts
// page.ts — poll cho tới khi job xong, tự huỷ khi rời trang
this.service.startImport(file).pipe(
  switchMap(({ JobId }) => interval(1500).pipe(
    switchMap(() => this.service.getImportJobStatus(JobId)),
    takeWhile(s => s.Status === 'Pending' || s.Status === 'Running', true),  // true = emit lần cuối (kết quả) trước khi dừng
  )),
  takeUntilDestroyed(this.destroyRef),   // BẮT BUỘC — thiếu dòng này, poll tiếp tục chạy sau khi user rời trang
).subscribe(status => {
  if (status.Status === 'Succeeded' || status.Status === 'Failed') {
    // hiện kết quả, dừng banner "đang xử lý"
  }
});
```

- **`takeWhile(..., true)`** — tham số thứ 2 (`inclusive`) bắt buộc `true`,
  thiếu nó sẽ mất đúng lần emit chứa kết quả cuối cùng (job vừa xong thì bị
  cắt trước khi tới `subscribe`).
- **`takeUntilDestroyed()`** bắt buộc trên chuỗi poll — khác gọi API thường
  (1 lần rồi tự hoàn thành), poll chạy vô hạn cho tới khi job xong; user điều
  hướng đi chỗ khác giữa chừng mà không huỷ subscription = leak request nền
  vĩnh viễn.
- 🔄 LẬT 2026-09-06: hai dòng `res.data!` trong mẫu trên đã đổi thành `unwrapData(res)`, và
  `DanhMucDtiService` đổi thành `ImportService` (module `danh-muc-dti` gỡ 2026-08-29). `!` là
  đúng thứ `unwrapData` sinh ra để thay — xem §Service pattern.
- Banner "đang xử lý" trong lúc poll dùng lại đúng UX đã có cho trạng thái
  loading thông thường — khác biệt duy nhất: submit xong KHÔNG có nghĩa đã
  xong, phải đợi tín hiệu `Succeeded`/`Failed` từ poll mới coi là hoàn tất.
