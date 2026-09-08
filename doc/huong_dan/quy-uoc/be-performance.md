---
kind: luat
scope: core
verified: 2026-09-05
---

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

## Đã CHỐT — 📐 ĐÍCH ĐẾN, CHƯA THI CÔNG (đối chiếu 2026-09-05)

Đây là quyết định về **cách sẽ cache khi có cache**, không phải mô tả hiện trạng. Hôm nay
`src/BE` **chưa có tầng cache nào** — không `HybridCache`, không `IMemoryCache`, không
Redis. Nghiệm thu cho câu đó:

```bash
grep -rn "HybridCache\|IMemoryCache\|StackExchange.Redis" src/BE --include=*.cs --include=*.csproj | grep -v Tests
```

PASS cho tình trạng hôm nay: **rỗng**. Khi lệnh này bắt đầu có kết quả, mục này phải đổi
nhãn và mục "Trước khi thêm bất kỳ cache nào" ở trên trở thành cổng bắt buộc.

**`HybridCache` in-process, KHÔNG Redis** — hệ thống chạy 1 process. Interface
khai ở `Core.Application`, implement ở `Core.Infrastructure`; `Application`
không bao giờ chạm thẳng `HybridCache` / `IMemoryCache`.

`ConcurrentDictionary` cache reflection/metadata bất biến trong 1 process là
hợp lệ (nguồn dữ liệu là chính assembly). `static Dictionary` dùng làm cache dữ
liệu **từ DB** thì không — không eviction, không invalidation.

## Ràng buộc khi sửa code tính toán nghiệp vụ

Khi tối ưu **bất kỳ** code tính ra con số hiển thị cho người dùng: output phải
**giống hệt** trước khi sửa trên cùng dữ liệu. Đối chiếu thật, đừng suy luận —
đây là con số người dùng nhìn thấy, không phải chi tiết nội bộ.

> *Sửa 2026-09-05:* mục này trước nêu đích danh `PeriodAggregateCalculator` và
> `AggregationService`. Cả hai thuộc module DtiWeekly đã gỡ 2026-08-29 và **không còn tồn
> tại trong `src/BE`** — nêu tên lớp đã chết làm luật trông như không áp cho ai. Luật thì
> vẫn nguyên giá trị, nên nó được viết lại theo **tính chất** của code chứ không theo tên.

## Cấu hình kết nối DB — ✅ CÓ THẬT (đối chiếu 2026-09-05)

Cả hai thiết lập **đã khai**, cùng nằm trong `options.UseNpgsql(...)`
(`src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:59`):

| Thiết lập | Giá trị thật | Vì sao chọn thế |
|---|---|---|
| `EnableRetryOnFailure` (`src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:96`) | `maxRetryCount: 3`, `maxRetryDelay: 5s` | Một nhịp chớp mạng giữa app và Postgres từng thành **500** cho người dùng, không thử lại |
| `CommandTimeout` (`src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:105`) | `30` giây, khai **tường minh** | Bằng đúng mặc định Npgsql — khai ra để nó là con số đã cân nhắc chứ không phải mặc định trôi vào. Hạ xuống khi có số đo p99 thật của truy vấn nặng nhất |

### Cái bẫy đi kèm — đã đóng, và phải GIỮ đóng

Chiến lược thử lại của EF **không** bọc được transaction do code tự mở: nó ném
`InvalidOperationException` lúc **chạy**, không lúc biên dịch, và chỉ trên đúng đường ghi
đó. Vì vậy mọi đường ghi mở transaction tường minh phải tự bọc bằng execution strategy.

Kèm theo một bẫy thứ hai, ẩn hơn: mỗi lần thử lại phải bắt đầu từ dữ liệu **sạch**. Đọc
thực thể ở ngoài vòng thử lại thì lần thử thứ hai dùng lại đúng instance đã sửa dở của lần
đầu — hoặc ghi đè bằng dữ liệu cũ, hoặc vấp `ConcurrencyFailure` không giải thích được.
Đường ghi ở `Identity/` xử lý bằng `db.ChangeTracker.Clear()` ngay đầu phần lõi.

Nghiệm thu — **đếm bằng lệnh, đừng chép danh sách** (§6 `.claude/CLAUDE.md`):

```bash
grep -rn "await db.Database.BeginTransactionAsync" src/BE --include=*.cs | grep -v Tests | wc -l
grep -rn "db.Database.CreateExecutionStrategy()" src/BE --include=*.cs | grep -v Tests | wc -l
```

PASS: hai số **bằng nhau** (2026-09-05: 4 = 4). Lệch ⇒ có một transaction tường minh
không được strategy bọc, và nó sẽ nổ lúc chạy chứ không lúc build.

> **🔄 LẬT 2026-09-05.** Bản trước của mục này mang nhãn `🚧` và mở đầu bằng *"Hai thiết
> lập **chưa khai**"*, kèm citation `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:42`. Cả ba đều sai tại thời
> điểm đọc: hai thiết lập đã vào code, `UseNpgsql` nằm ở `:59`, và cái bẫy transaction mà
> mục này cảnh báo thì đã được đóng ở toàn bộ đường ghi. Đây đúng loại drift §4 nói tới —
> doc mô tả một việc tồn đọng đã làm xong, nên người đọc hoặc đi làm lại, hoặc mất niềm
> tin vào các nhãn `🚧` còn lại.
