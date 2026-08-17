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
| API `src/BE` | `PascalCase` | xác nhận trong API Contract Card (`doc/contracts/<feature>.md`) trước khi code, đừng giả định |
| JSON tĩnh (`public/assets/*.json`) | như file gốc | |
| Model app (bạn tự định nghĩa) | `PascalCase` + prefix `I` | `IPositionRow.Status` |

## Quy tắc cứng

1. Wire type (DTO) giữ **nguyên xi** casing server trả về. Hậu tố `Dto`
   (vd. `PositionDto`).
2. Model app: `interface` prefix `I`, field `PascalCase`, **không** hậu tố
   (vd. `IPositionRow`).
3. Mapper đặt trong `services/` của feature, cạnh service gọi API:
   ```ts
   function mapPositionDtoToRow(dto: PositionDto): IPositionRow {
     return { Id: dto.Id, Name: dto.Name, Status: dto.Status };
   }
   ```
4. Component **không bao giờ** import hay chạm trực tiếp vào DTO — chỉ thấy
   model app.
5. Giữ 2 type + mapper **ngay cả khi chúng trông giống hệt nhau** lúc mới
   viết — DTO thuộc về server, model thuộc về app; gộp lại mất điểm chặn khi
   server đổi field sau này.

## Envelope response từ BE — `IApiResult<T>`

**Đã CHỐT (2026-08-15):** BE trả về đúng shape sau cho MỌI endpoint (xem
`src/BE/.claude/rules/api-controller.md` §Envelope response) — FE phải có
interface khớp 1:1, không tự đặt tên field khác:

```ts
// core/http/api-result.model.ts
export interface IApiResult<T> {
  data: T | null;
  message: string | null;
  status: 'SUCCESS' | 'VALIDATION_ERROR' | 'BUSINESS_ERROR' | 'SYSTEM_ERROR';
  code: 'Success' | 'ValidationError' | 'AuthenticationError' | 'AuthorizationError'
      | 'NotFound' | 'Conflict' | 'BusinessRuleError' | 'SystemError';
  businessCode: string | null;   // "{ENTITY}.{ERROR}" — so lỗi cụ thể, KHÔNG so message
  traceId: string | null;
  retryable: boolean | null;
  fields: Record<string, string[]> | null;   // lỗi validate theo field — key PascalCase khớp property C#
}
```

- Đọc `message` để hiển thị cho user — **không phải** `Message`/`ErrorMessage`
  (tên field của envelope cũ, đã bỏ cùng lúc BE đổi sang `IApiResult<T>`).
- So sánh lỗi cụ thể (vd "hiện nút thử lại khi trùng mã") dùng `businessCode`
  (chuỗi ổn định, `"CRITERIA.DUPLICATE_CODE"`) — **không** so `message`
  (chuỗi hiển thị, đổi theo câu chữ UI).
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
  ASP.NET Core, xem `src/BE/.claude/rules/api-controller.md` §CORS).

## Service pattern

```ts
@Injectable({ providedIn: 'root' })
export class PositionService {
  private http = inject(HttpClient);

  list(params: IListPositionsParams): Observable<IPositionRow[]> {
    return this.http
      .post<PositionDto[]>('/api/positions/list', params)
      .pipe(map(dtos => dtos.map(mapPositionDtoToRow)));
  }
}
```

- Mỗi feature có 1 service riêng (`services/<feature>.service.ts`) — không
  gom nhiều feature vào một "god service".
- Base URL API cấu hình qua `environment.ts` — **không hardcode URL** trong
  từng service.
- Lỗi HTTP xử lý qua interceptor dùng chung ở `core/` (retry, refresh token
  nếu có auth, log lỗi) — service của feature không tự bắt lỗi HTTP lặp lại
  logic đó.

## Khi endpoint chưa tồn tại

Đừng tự đoán shape response rồi code như thật. Viết **API Contract Card**
vào `doc/contracts/<feature>.md` (xem mẫu trong
`.claude/agents/frontend-expert.md` § Bàn giao cho backend-expert), để
`backend-expert` review và chốt `AGREED` trước khi implement thật.

## Secrets

`environment.ts` (và mọi file `environment.*.ts`) **không bao giờ** chứa API
key/secret thật được commit vào git. Dùng biến môi trường lúc build hoặc một
cơ chế secret riêng — hỏi người dùng nếu chưa có quy ước cho dự án.

## Long-running operation — poll pattern

Khi BE trả **202 + `jobId`** thay vì đợi xử lý xong (xem
`src/BE/.claude/rules/cqrs-handler.md` §"Command chạy lâu → job nền" — ca đầu
tiên: Import CSV/Excel), FE gọi 2 bước thay vì 1:

```ts
@Injectable({ providedIn: 'root' })
export class DanhMucDtiService {
  private http = inject(HttpClient);

  startImport(file: File): Observable<{ JobId: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<IApiResult<{ jobId: string }>>('/import', formData)
      .pipe(map(res => ({ JobId: res.data!.jobId })));
  }

  getImportJobStatus(jobId: string): Observable<IImportJobStatus> {
    return this.http
      .get<IApiResult<IImportJobStatusDto>>(`/import/${jobId}`)
      .pipe(map(res => mapImportJobStatusDtoToModel(res.data!)));
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
- Banner "đang xử lý" trong lúc poll dùng lại đúng UX đã có cho trạng thái
  loading thông thường — khác biệt duy nhất: submit xong KHÔNG có nghĩa đã
  xong, phải đợi tín hiệu `Succeeded`/`Failed` từ poll mới coi là hoàn tất.
