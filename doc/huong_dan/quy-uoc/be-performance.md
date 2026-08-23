# Quy ước — Query, index & caching (BE)

Quy ước thi hành khi viết repository/query mới, hoặc khi nhận bất kỳ task nào
có chữ *"chậm"* / *"tối ưu"* / *"cache"*.

> 📖 Lý do nền, ngưỡng áp dụng và cách đo:
> [`../wiki-core/be/11-performance-caching.md`](../wiki-core/be/11-performance-caching.md).

> **Lịch sử:** file này trước **không tồn tại**, dù `.claude/agents/core-reviewer.md`
> (bảng định tuyến + mục Performance) và `doc/cau-truc-database.md` §2.1 đều trỏ tới
> nó dưới tên `src/BE/.claude/rules/performance.md`, một file chưa từng tồn tại.
> Nội dung thật đang kẹt trong `.claude/agents/backend-expert.md`, tức tri thức nằm
> sai khu. Tách ra đây **2026-08-23** theo `.claude/CLAUDE.md` §2.

## Thứ tự bắt buộc — không được nhảy cóc

```
query pattern  →  thuật toán  →  ĐO LẠI  →  cache
```

Cache đặt trước 2 bước đầu chỉ **che** lỗi chứ không sửa: lần miss vẫn chậm y
hệt, seq scan / N+1 vẫn nguyên, và có thêm một tầng nữa để debug khi số liệu
hiển thị sai.

## Khi viết repository/query mới — áp ngay, không chờ ai nhắc

- Query **chỉ đọc** → `AsNoTracking()`. Query lấy entity **để sửa rồi
  `SaveChanges`** → **KHÔNG** thêm; thay đổi sẽ không được ghi, và đó là lỗi im
  lặng. Đọc call-site trước khi thêm, đừng áp hàng loạt.
- Mỗi predicate lọc nóng phải có index **dẫn đầu đúng cột đó**. Index `(A, B)`
  **không** seek được cho query chỉ lọc theo `B`.
- `Distinct` / `GroupBy` / `Count` / phân trang chạy ở **SQL**, không
  `ToListAsync()` rồi mới làm trong C#.
- Không `await` trong vòng lặp (N+1).
- Ngoại lệ chỉ hợp lệ khi comment nêu **con số** trần trên và điều kiện làm nó
  hết đúng. *"Dataset hiện tại nhỏ"* suông **không** phải ngoại lệ — nó không
  kiểm chứng được.

## Trước khi thêm bất kỳ cache nào — đủ 3 thứ, thiếu 1 thì dừng lại hỏi

1. **Số đo** chứng minh chỗ đó tốn thật.
2. **Danh sách đầy đủ** đường ghi phải invalidate — kể cả job nền (không có
   `HttpContext`, đây là chỗ dễ quên nhất).
3. **Test** xác nhận invalidation chạy, không chỉ test cache hit.

Cache dữ liệu **phân quyền** mà chỉ dựa TTL, không invalidate tường minh khi ma
trận quyền đổi → quyền đã thu hồi còn hiệu lực tới hết TTL. Đó là lỗ hổng bảo
mật, không phải vấn đề hiệu năng.

## Đã CHỐT

**`HybridCache` in-process, KHÔNG Redis** — hệ thống chạy 1 process. Interface
khai ở `Core.Application`, implement ở `Core.Infrastructure`; `Application`
không bao giờ chạm thẳng `HybridCache` / `IMemoryCache`.

`ConcurrentDictionary` cache reflection/metadata bất biến trong 1 process là
hợp lệ (nguồn dữ liệu là chính assembly). `static Dictionary` dùng làm cache dữ
liệu **từ DB** thì không — không eviction, không invalidation.

## Ràng buộc khi sửa code tính toán nghiệp vụ

Với `PeriodAggregateCalculator`, `AggregationService` và tương tự: output phải
**giống hệt** trước khi sửa trên cùng dữ liệu. Đối chiếu thật, đừng suy luận —
đây là con số hiển thị cho người dùng, không phải chi tiết nội bộ.
