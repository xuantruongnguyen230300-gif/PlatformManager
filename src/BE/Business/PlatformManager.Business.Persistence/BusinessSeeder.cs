using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence;

/// <summary>
/// Seed dữ liệu khởi tạo của tầng NGHIỆP VỤ — hôm nay là 6 nhóm chỉ tiêu của
/// spec/danh-muc-dti/business-rules.md §1.6. DML, idempotent, chỉ chạy khi có tham số
/// <c>--seed</c> (cùng đường với <c>CoreSeeder</c>, và chạy SAU nó).
///
/// <para><b>Vì sao là seeder RIÊNG ở tầng nghiệp vụ, không thêm vào <c>CoreSeeder</c></b>
/// (Q67, chốt 2026-09-10): tên nhóm là dữ liệu nghiệp vụ của đúng một sản phẩm, còn Core đi theo
/// sang dự án thứ hai. ⚠️ <b>ArchTest KHÔNG canh được điều này</b> —
/// <c>CoreSource_MustNotContain_BusinessNameStringLiteral</c> chỉ chặn literal mang TÊN TẦNG
/// (<c>business</c>, <c>dtiweekly</c>); một seeder trong Core chép sáu chuỗi tiếng Việt ở dưới
/// vẫn XANH test đó trong khi phá ranh giới. Lớp canh ở đây là review, không phải máy.</para>
///
/// <para><b>Vì sao KHÔNG dùng <c>HasData</c></b> (Q67): nó buộc viết cứng 6 <c>Guid</c> trong
/// code — ngược §1.1 (<i>UUID v7, sinh ở ứng dụng qua <c>EntityId.New()</c></i>) — và biến mỗi
/// lần BA sửa bảng seed thành một MIGRATION mới, trong khi bảng đó là dữ liệu nghiệp vụ chứ
/// không phải lược đồ bảng. Với seeder: đổi bảng seed thì sửa seeder, không sinh migration.</para>
/// </summary>
public sealed class BusinessSeeder(
    PlatformManagerDbContext db,
    IUnitOfWork unitOfWork,
    ILogger<BusinessSeeder> logger)
{
    /// <summary>
    /// Sáu nhóm của §1.6 — <b>QUYẾT ĐỊNH, không phải số đo</b>.
    ///
    /// <para>⚠️ <c>Name</c> là ĐÚNG chuỗi cột <c>Nhóm</c> của
    /// <c>spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv</c>: không tiền tố số, và GIỮ NGUYÊN chỗ
    /// viết hoa không đều (<i>chính quyền số</i> thường, <i>Kinh tế số</i> hoa). Import tra nhóm
    /// bằng <c>Name</c> (§6.2 cột 3), nên sửa "cho đẹp" một chữ là mọi dòng của nhóm đó thành
    /// <c>IMPORT.ROW_GROUP_NOT_FOUND</c>.</para>
    ///
    /// <para>Giao diện hiện <c>Code. Name</c> (vd <c>1. Hạ tầng và Nền tảng số</c>) — ghép chuỗi
    /// là việc của FE; DB và API giữ hai trường rời.</para>
    /// </summary>
    private static readonly (string Code, int DisplayOrder, string Name)[] Groups =
    [
        ("1", 1, "Hạ tầng và Nền tảng số"),
        ("2", 2, "Nhân lực số"),
        ("3", 3, "An toàn thông tin, an ninh mạng"),
        ("4", 4, "Hoạt động chính quyền số"),
        ("5", 5, "Hoạt động Kinh tế số"),
        ("6", 6, "Hoạt động Xã hội số"),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Tra theo Code TRƯỚC khi chèn — đây là cơ chế idempotent CHÍNH. Unique partial
        // IX_CriteriaGroups_Code_Active là lưới an toàn THỨ HAI, không phải cơ chế chính: nó chỉ
        // biến một lần chèn trùng thành lỗi ồn ào, còn seeder thì phải chạy lại được mà không ồn.
        //
        // IgnoreQueryFilters: cần thấy cả dòng ĐÃ XOÁ MỀM. Unique partial nới ràng buộc ra nên
        // Postgres KHÔNG chặn việc chèn một nhóm mới cùng Code bên cạnh dòng đã xoá mềm — không
        // đọc qua filter thì bảng lặng lẽ có hai dòng cùng mã. Cùng cái bẫy mà
        // CoreSeeder.UpsertMenuAsync đã phải xử lý.
        var existing = await db.Set<CriteriaGroup>()
            .IgnoreQueryFilters()
            .ToDictionaryAsync(group => group.Code, ct);

        var added = 0;
        var updated = 0;

        foreach (var (code, displayOrder, name) in Groups)
        {
            if (existing.TryGetValue(code, out var group))
            {
                // Hồi sinh dòng đã xoá mềm + đồng bộ lại định nghĩa. Id GIỮ NGUYÊN — nghiệm thu
                // §1.6 đòi "không nhóm nào đổi Id giữa hai lần chạy", và Id của nhóm là thứ mọi
                // Criteria đang trỏ tới.
                if (!group.IsDeleted && group.Name == name && group.DisplayOrder == displayOrder)
                    continue;

                group.IsDeleted = false;
                group.UpdateDefinition(name, displayOrder);
                updated++;
                continue;
            }

            // Id sinh ở ứng dụng qua EntityId.New() bên trong BaseEntity — không Guid nào viết
            // cứng ở đây (§1.1).
            db.Set<CriteriaGroup>().Add(CriteriaGroup.Create(code, name, displayOrder));
            added++;
        }

        if (added == 0 && updated == 0)
        {
            logger.LogInformation("Danh mục nhóm chỉ tiêu đã khớp bảng seed — không ghi gì.");
            return;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed danh mục nhóm chỉ tiêu: thêm {Added}, cập nhật {Updated}, tổng {Total} nhóm.",
            added,
            updated,
            Groups.Length);
    }
}
