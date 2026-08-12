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
