# 6. Testing strategy — ưu tiên mapper/service, không coverage dàn trải

## Vì sao đây là Nhóm A (bắt buộc ngày đầu)

Cùng lý do với BE (`be/04-testing-strategy.md`): tài liệu chỉ có tác dụng
lúc đọc, test có tác dụng mãi mãi, tự động. Điểm khác: FE không cần
ArchTest/DB thật — rủi ro cao nhất nằm ở **wire boundary** (mapper DTO↔model)
và **logic interceptor** (nơi lỗi thầm lặng nhất, không có exception, không
crash — chỉ hiển thị sai).

## 3 tầng, đúng thứ tự ưu tiên (khác thứ tự liệt kê ở BE)

| Tầng | Test gì | Vì sao ưu tiên |
|---|---|---|
| 1. Mapper (`services/*.service.ts`) | DTO → model giữ đủ field, đúng kiểu | Lỗi ở đây **không có exception** — chỉ UI hiển thị sai/thiếu, khó phát hiện bằng mắt |
| 2. Interceptor/service lỗi (`core/interceptors/*.ts`) | `IApiResult<T>` → thông báo đúng, `fields` bind đúng key | Đây chính là nơi lỗi FE-1 (đọc sai field) từng xảy ra thật |
| 3. Component có logic | `computed()` phức tạp, form validation, điều kiện hiển thị | Không test component chỉ render tĩnh — phí thời gian, ít giá trị |

## Không bắt buộc

- Coverage 100% — ưu tiên đúng 3 tầng trên hơn phủ hết mọi file.
- Test component dumb chỉ nhận `input()` và render — hành vi đã được đảm
  bảo bởi chính Angular template binding, test lại là test framework, không
  phải test code của mình.

## Mẫu test mapper

```ts
describe('mapPositionDtoToRow', () => {
  it('giữ đủ field và đúng kiểu', () => {
    const dto: PositionDto = { Id: '1', Name: 'A', Status: 'Active' };
    expect(mapPositionDtoToRow(dto)).toEqual({ Id: '1', Name: 'A', Status: 'Active' });
  });
});
```

## Mẫu test interceptor

```ts
it('đọc message từ IApiResult, không phải field cũ', () => {
  const err = new HttpErrorResponse({
    status: 409,
    error: { data: null, message: "Mã '1.1' đã tồn tại.", status: 'BUSINESS_ERROR',
              code: 'Conflict', businessCode: 'CRITERIA.DUPLICATE_CODE',
              traceId: 't1', retryable: false, fields: null } satisfies IApiResult<null>,
  });
  // assert ToastService.error được gọi với đúng message, không phải fallback chung
});
```

Test này **chính là bài kiểm chứng cho việc F0 (nền móng `core/http`) đã làm
đúng** — viết nó trước khi coi F0 hoàn thành, cùng tinh thần "luật chưa từng
đỏ là luật chưa được chứng minh" của BE.
