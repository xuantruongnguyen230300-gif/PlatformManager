using System.Reflection;
using PlatformManager.Core.Domain.Common;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Chỉ đúng 6 field kỹ thuật của BaseEntity được public set — mọi field nghiệp vụ của entity
/// con phải private set + mutate qua method có tên nghiệp vụ (factory method pattern, xem
/// .claude/rules/entity-domain.md). Quét CẢ Core.Domain (SysMenu/SysMenuRole) LẪN
/// Modules.DtiWeekly.Domain (Criteria/CriteriaGroup/CriteriaAssessment/CriteriaEvidence) — thêm
/// module mới thì thêm assembly vào <see cref="DomainAssemblies"/>. AppUser/AppRole (Identity)
/// KHÔNG kế thừa BaseEntity nên không bị test này ràng buộc — đúng chủ đích thiết kế.
/// </summary>
public class EntityEncapsulationTests
{
    private static readonly HashSet<string> AllowedPublicSetterNames =
        ["Id", "UserCreate", "UserUpdate", "DateCreate", "DateUpdate", "IsDelete"];

    private static readonly Assembly[] DomainAssemblies =
    [
        typeof(BaseEntity).Assembly,
        typeof(PlatformManager.Modules.DtiWeekly.Domain.Entities.Criteria).Assembly,
    ];

    [Fact]
    public void BaseEntity_Descendants_MustNotHave_PublicSetter_ForBusinessProperties()
    {
        var violations = new List<string>();

        foreach (var domainAssembly in DomainAssemblies)
        {
            var entityTypes = domainAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseEntity).IsAssignableFrom(t));

            foreach (var type in entityTypes)
            {
                var declaredProperties = type.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var prop in declaredProperties)
                {
                    if (AllowedPublicSetterNames.Contains(prop.Name))
                        continue;

                    if (prop.SetMethod is { IsPublic: true })
                        violations.Add($"{type.Name}.{prop.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Entity nghiệp vụ có public setter ngoài 6 field kỹ thuật cho phép: {string.Join(", ", violations)}");
    }
}
