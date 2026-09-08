using System.Reflection;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Đánh dấu 1 assembly có chứa IEntityTypeConfiguration&lt;T&gt; cần áp dụng vào
/// PlatformManagerDbContext.OnModelCreating. Mỗi tầng KHAI assembly của mình qua
/// <c>IModuleRegistrar.PersistenceAssembly</c>, và <c>ModuleRegistrationExtensions.AddModules</c>
/// nộp nó vào DI dưới dạng bản ghi này (kể cả Core — không có đường riêng cho Core, xem
/// CoreModuleRegistrar). Core không cần biết assembly đó thuộc tầng nào, chỉ cần biết "có N
/// assembly cần áp dụng configuration từ đó". Xem doc/kien-truc-core-module.md §DbContext —
/// giải pháp cho việc Core.Infrastructure KHÔNG được ProjectReference tới tầng nghiệp vụ nào.
/// </summary>
public sealed record EfConfigurationAssembly(Assembly Assembly);
