using Xunit;

namespace PlatformManager.Business.IntegrationTests;

/// <summary>
/// Collection fixture của bộ test NGHIỆP VỤ — dùng <see cref="BusinessPostgresFixture"/>, tức
/// fixture của Core đã nối thêm script schema <c>business</c>.
///
/// <para>🛑 <b>Phải khai LẠI <c>[CollectionDefinition]</c> ở đây, không dùng được bản của bộ Core</b>
/// — xUnit khớp collection theo TÊN và <b>chỉ trong cùng một assembly</b>. Trỏ sang
/// <c>PostgresCollection</c> của assembly kia thì analyzer báo <c>xUnit1041</c> (<i>fixture argument
/// does not have a fixture source</i>) và ở lúc chạy, tham số <c>fixture</c> không được bơm.</para>
///
/// <para><b>Cái được dùng lại là LỚP fixture, không phải instance</b>: mỗi assembly test dựng
/// container Postgres riêng. Chậm hơn một chút, nhưng đó là cái giá đúng — hai bộ test độc lập
/// nhau, và bộ Core phải chạy được nguyên vẹn khi CoreBase tách sang dự án thứ hai, lúc đó bộ này
/// biến mất hoàn toàn.</para>
///
/// <para>Danh sách script schema vẫn sống ĐÚNG MỘT chỗ cho mỗi tầng: Core trong
/// <c>PostgresFixture.CoreScripts</c>, nghiệp vụ trong
/// <see cref="BusinessPostgresFixture.ExtraScripts"/>. Không bên nào chép của bên nào.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class BusinessPostgresCollection : ICollectionFixture<BusinessPostgresFixture>
{
    public const string Name = "business-postgres";
}
