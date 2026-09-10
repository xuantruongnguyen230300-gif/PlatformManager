using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <inheritdoc cref="IUserLookupService"/>
///
/// <remarks>
/// <b>Tài khoản đang bị KHOÁ vẫn được tra ra — quyết định ngầm, ghi rõ 2026-09-09.</b> Soft-delete
/// của <c>AppUser</c> đi qua <c>LockoutEnd</c> (xem <c>AppUser</c>), nên một tài khoản đã "xoá"
/// vẫn khớp tên ở đây và vẫn được gán làm người phụ trách.
///
/// <para><b>Hành vi này CỐ Ý giữ nguyên ở lượt ghi chú này</b> — không đổi kèm. Lý do: seam này
/// phục vụ đường import, nơi cột "Phụ trách" là một cái TÊN trong file người dùng gửi. Lọc bỏ tài
/// khoản khoá sẽ biến một ô có dữ liệu thành ô trống mà không báo gì — đúng hướng hỏng im lặng mà
/// <c>spec/danh-muc-dti/business-rules.md</c> §6.3 đã chọn tránh khi quy định "không khớp thì để
/// trống, KHÔNG lỗi". Đổi sang lọc là một quyết định NGHIỆP VỤ (người phụ trách đã nghỉ thì dòng
/// đó nên trống, hay nên giữ để truy lịch sử?), và spec chưa nói.</para>
///
/// <para><b>Việc phải làm khi spec nói:</b> thêm điều kiện vào chính query này, và thêm một ca test
/// cho tài khoản khoá. Ghi ra đây vì một quyết định ngầm không ai biết là ngầm thì lần sau sẽ được
/// đọc thành "đã cân nhắc rồi".</para>
/// </remarks>
public sealed class UserLookupService(UserManager<AppUser> userManager) : IUserLookupService
{
    public async Task<Guid?> ResolveByFullNameAsync(string fullName, CancellationToken ct)
    {
        // Đồng bộ với mọi seam khác của lượt này (LocalFileStorage.SaveAsync,
        // ImportFileReaderSelector.SelectAsync): chặn null ở BIÊN, thay vì để nó thành
        // NullReferenceException ở dòng .Trim() — một lỗi không nói được nó đến từ tham số nào.
        ArgumentNullException.ThrowIfNull(fullName);

        var trimmed = fullName.Trim();

        // AsNoTracking: query CHỈ ĐỌC — không thực thể nào ở đây được sửa và không SaveChanges nào
        // chạy trên chúng. Bỏ change tracker đi thì EF không dựng snapshot cho từng dòng trả về, và
        // một tên trùng nhiều người không kéo theo bấy nhiêu bản sao nằm lại trong context (thêm
        // 2026-09-08, luật ở doc/huong_dan/quy-uoc/be-performance.md §"Khi viết repository/query mới").
        //
        // Take(2) là đủ cho MỌI nhánh: cần phân biệt 0 / 1 / "≥2" chứ không cần biết trùng bao nhiêu
        // người. Không có nó, một tên phổ biến kéo cả trăm dòng về chỉ để đếm.
        var matches = await userManager.Users
            .AsNoTracking()
            .Where(u => u.FullName.ToLower() == trimmed.ToLower())
            .Select(u => u.Id)
            .Take(2)
            .ToListAsync(ct);

        // Không ai khớp → null (spec/danh-muc-dti/business-rules.md §6.3: KHÔNG lỗi, để trống).
        // Trùng ≥2 → null: KHÔNG tự đoán người nào.
        return matches.Count == 1 ? matches[0] : null;
    }
}
