using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Core.Infrastructure.Common;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
