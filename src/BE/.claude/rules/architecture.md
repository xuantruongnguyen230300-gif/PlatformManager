# Architecture — src/BE

## Project layout & dependency direction

```
src/BE/
├── src/
│   ├── PlatformManager.Domain/          ← entity, value object, domain event
│   ├── PlatformManager.Application/     ← command/query/handler, DTO, interface
│   ├── PlatformManager.Infrastructure/  ← EF Core, repository impl, external service
│   └── PlatformManager.Api/             ← controller, DI composition root, middleware
└── PlatformManager.sln
```

```
✅ Domain          → (không phụ thuộc gì)
✅ Application     → Domain
✅ Infrastructure  → Application, Domain
✅ Api             → Application, Infrastructure

❌ Domain          → Microsoft.EntityFrameworkCore, ASP.NET Core, bất kỳ package hạ tầng nào
❌ Application     → Microsoft.EntityFrameworkCore trực tiếp (DbContext/AsQueryable/Include) — luôn qua interface
❌ Application     → Infrastructure
❌ Application     → IConfiguration trực tiếp — dùng IOptions<T>
```

**Vì sao giữ luật này ngay từ đầu:** dự án chưa có dòng code nào — chi phí
giữ layer sạch từ slice đầu tiên gần như bằng 0; chi phí gỡ rối sau khi
Domain đã dính EF Core hoặc Application đã gọi thẳng `DbContext` thì rất cao
và thường phải viết lại. Không có "code cũ" nào biện minh cho việc phá luật
ở dự án mới.

## Vertical slice trong Application

Mỗi feature/entity có một thư mục riêng trong `Application/`, chứa toàn bộ
command/query/handler/validator/DTO liên quan — không tách theo tầng kỹ
thuật (`Commands/`, `Queries/`, `Validators/` phẳng ở gốc):

```
Application/<Feature>/
├── Create{Entity}Command.cs
├── Update{Entity}Command.cs
├── Delete{Entity}Command.cs
├── Get{Entity}ByIdQuery.cs
├── Get{Entity}sListQuery.cs
├── {Entity}Validator.cs
├── {Entity}Dto.cs
└── I{Entity}Repository.cs
```

Lý do: khi cần hiểu/sửa một feature, mọi file liên quan nằm cạnh nhau — không
phải nhảy qua 4-5 thư mục theo tầng kỹ thuật để ráp lại bức tranh.

## Dependency Injection

- Đăng ký service theo convention (marker interface hoặc extension method
  theo layer) thay vì liệt kê tay từng service trong `Program.cs` khi số
  lượng service đã đủ lớn để việc liệt kê tay trở thành gánh nặng bảo trì.
  Ở giai đoạn đầu (ít service), đăng ký tay trong
  `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` là đủ
  — chỉ chuyển sang convention scan khi thực sự cần.
- `Api` là **composition root duy nhất** biết tới `Infrastructure` — không
  project nào khác được reference `Infrastructure`.

## Testing

- Domain: unit test thuần, không cần DB/HTTP — test factory method, mutation
  method, invariant.
- Application: unit test handler với repository giả lập (in-memory hoặc
  mock) — test logic nghiệp vụ, không test EF Core thật ở tầng này.
- Api: integration test gọi endpoint thật qua `WebApplicationFactory`, dùng
  DB test (container hoặc in-memory tuỳ độ trung thực cần thiết).
