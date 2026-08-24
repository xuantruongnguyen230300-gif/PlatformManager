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

---

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: 3
> tầng ở trên đúng thứ tự ưu tiên, nhưng chưa nói **cách** viết test cho từng
> loại — và bỏ sót 2 lớp phòng thủ khác hẳn unit test (E2E, visual
> regression) mà hệ thống tầm trung vẫn cần dù không cần coverage dàn trải. 4
> mục dưới đây bổ sung, không đổi 3 tầng đã chốt ở trên.

## Test cho `computed()`/`signal()` — đọc qua `()`, không cần `fakeAsync`/`tick()`

Kiểu test RxJS Observable truyền thống trong Angular cần `fakeAsync` +
`tick()` (hoặc `waitForAsync`) vì giá trị đến qua hàng đợi bất đồng bộ
(`setTimeout`, `Promise`, `debounceTime`...) — test phải "tua" thời gian giả
lập trước khi assert được. `signal()`/`computed()` **không đi qua hàng đợi
đó**: `set()` cập nhật ngay lập tức, `computed()` tính lại đồng bộ ngay khi
được đọc. Test vì vậy chỉ cần gọi rồi đọc qua `()` liền sau đó — không cần
`tick()`, và **không cần `fixture.detectChanges()`** nếu assertion nhắm vào
giá trị signal chứ không phải DOM đã render (`detectChanges()` chỉ đồng bộ
hoá DOM với signal, không phải điều kiện để đọc đúng giá trị signal).

```ts
describe('CriteriaFormComponent — computed', () => {
  it('isValid() đúng ngay sau set(), không cần tick() hay detectChanges()', () => {
    const component = new CriteriaFormComponent();

    component.code.set('1.1');
    component.name.set('');
    expect(component.isValid()).toBe(false); // đọc trực tiếp qua (), đồng bộ

    component.name.set('Tên hợp lệ');
    expect(component.isValid()).toBe(true); // không cần chờ gì thêm
  });
});
```

**Ngoại lệ vẫn cần async:** nếu giá trị signal bắt nguồn từ `resource()`/
`httpResource()` (Angular 20) hoặc bất kỳ effect nào gọi HTTP/timer thật,
phần đó vẫn là Promise/Observable ở gốc — vẫn cần `await
fixture.whenStable()` hoặc mock ở tầng interceptor như mẫu test interceptor ở
trên. Ranh giới đúng: test đồng bộ khi nguồn dữ liệu là signal cục bộ, test
bất đồng bộ khi nguồn dữ liệu đi qua HTTP/timer thật.

## `TestBed` đầy đủ vs mock toàn bộ dependency — quy ước chọn theo tầng

Chưa có quy ước này thì mỗi người tự chọn theo cảm tính — dễ lệch về 1 trong
2 cực: mock hết (nhanh nhưng không bắt được lỗi wiring cha-con) hoặc `TestBed`
đầy đủ mọi nơi (chậm, và vi phạm chính nguyên tắc "không coverage dàn trải"
đã chốt ở đầu file).

| Cách test | Dùng khi | Bắt được lỗi gì | Bỏ lọt gì |
| --- | --- | --- | --- |
| `new Component()` trực tiếp, không `TestBed` | Logic thuần trong tầng 3 (`computed()`, validation) không inject gì | Lỗi logic | Mọi thứ liên quan DI/template |
| `TestBed` + **mock** toàn bộ service inject | Component có inject service nhưng chỉ cần cô lập logic, không cần DOM con thật | Lỗi logic + gọi đúng method/tham số của service | Lỗi binding `input()`/`output()` giữa component cha và con — mock che mất, vì con không tồn tại thật trong test |
| `TestBed` + component con **thật** (không mock, không `NO_ERRORS_SCHEMA`) | Số ít component "trục" — page component chính nơi nhiều mảnh ghép lại (vd trang danh sách + filter + phân trang) | Lỗi wiring: sai tên `input()`, sai thứ tự tham số `output()`, template lỗi cú pháp không lộ ra khi mock | Vẫn không bắt được lỗi tích hợp thật qua router/HTTP — đó là việc của E2E, xem mục dưới |

Dòng 3 chỉ áp dụng cho **số ít** component trục, không đại trà — dùng tràn
lan sẽ chậm dần bộ test và quay lại đúng vấn đề "coverage dàn trải" mà file
này mở đầu bằng cách từ chối.

## E2E — tầng thứ 4, bổ sung chứ không thay thế 3 tầng unit/component

3 tầng ở trên (mapper, interceptor, component) đều chạy trong môi trường giả
lập — `HttpTestingController` giả HTTP, không router thật, không cookie do
trình duyệt quản lý, không CORS thật. Những thứ đó chính là nơi lỗi tích hợp
thật xảy ra — thứ tự guard sai (`authGuard`/`mustChangePasswordGuard`, xem
[07-auth-identity.md](07-auth-identity.md) mục "Guard"), CORS thiếu
`AllowCredentials` khiến cookie âm thầm không gửi được (cùng file, mục "CORS
phía BE") — không lớp unit/component nào ở trên chạm tới những đường này, vì
tất cả đều mock đúng thứ cần được test.

Không cần E2E phủ mọi route — chỉ cần **một số ít kịch bản luồng quan trọng
nhất**, đúng tinh thần "không coverage dàn trải" xuyên suốt file này:

```ts
// e2e/login-to-dashboard.spec.ts (Playwright)
test('chưa đăng nhập vào route cần quyền → redirect kèm returnUrl → login xong quay lại đúng chỗ', async ({ page }) => {
  await page.goto('/danh-muc-dti');
  await expect(page).toHaveURL(/\/dang-nhap\?returnUrl=/);

  await page.fill('[formcontrolname=username]', 'admin');
  await page.fill('[formcontrolname=password]', 'Admin@123');
  await page.click('button[type=submit]');

  await expect(page).toHaveURL('/danh-muc-dti'); // đúng returnUrl, không phải route mặc định
});
```

Chạy tay trước khi release, cùng cách 4 lệnh gate khác đang chạy — repo chưa
có CI (xem [trien-khai/05-gate.md](trien-khai/05-gate.md)).

## Visual regression — chưa cần, ghi ngưỡng kích hoạt (Nhóm B)

Chưa thêm công cụ diff ảnh (Percy/Chromatic/Playwright `toHaveScreenshot()`)
**có chủ đích**, không phải bỏ sót: dựng baseline lúc token/component vẫn
đang đổi (token chưa hoá hết, hex vẫn hardcode rải rác — xem
[04-design-token-system.md](04-design-token-system.md); component thiếu
trạng thái tương tác — xem [05-component-library.md](05-component-library.md))
sẽ ra diff **liên tục vì lý do đúng** (đang sửa token thật), dạy người review
thói quen bấm "approve" mà không nhìn — đúng antipattern khiến visual
regression mất tác dụng ngay từ lần đầu.

**Ngưỡng kích hoạt:** khi token pipeline và trạng thái component ổn định (2
khoản trên đóng lại), thêm bằng `expect(page).toHaveScreenshot()` sẵn có
trong Playwright — tái dùng đúng công cụ đã cần cho E2E ở mục trên, không
thêm dịch vụ SaaS mới (Percy/Chromatic) khi chưa có bằng chứng cần
collaborate review ảnh giữa nhiều người ngoài team.
