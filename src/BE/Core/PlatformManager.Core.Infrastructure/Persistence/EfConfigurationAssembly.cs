using System.Reflection;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Đánh dấu 1 assembly có chứa IEntityTypeConfiguration&lt;T&gt; cần áp dụng vào
/// PlatformManagerDbContext.OnModelCreating. Mỗi Module tự đăng ký assembly của mình qua đây
/// lúc AddXxxModule() (services.AddSingleton(new EfConfigurationAssembly(...))) — Core không
/// cần biết assembly đó thuộc Module nào, chỉ cần biết "có N assembly cần áp dụng configuration
/// từ đó". Xem doc/kien-truc-core-module.md §DbContext — giải pháp cho việc Core.Infrastructure
/// KHÔNG được ProjectReference tới bất kỳ Modules.*.Infrastructure nào.
/// </summary>
public sealed record EfConfigurationAssembly(Assembly Assembly);
