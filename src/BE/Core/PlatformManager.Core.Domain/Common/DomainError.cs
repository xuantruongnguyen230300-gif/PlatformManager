namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Một lỗi nghiệp vụ ĐƯỢC KHAI BÁO ở tầng Domain: mã hợp đồng + khuôn thông điệp. Khai tập trung
/// trong <c>{Entity}Errors.cs</c> cạnh entity, KHÔNG gõ chuỗi thẳng vào chỗ <c>throw</c> — xem
/// doc/huong_dan/quy-uoc/be-cqrs-handler.md §ErrorDescriptor và
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(a).
///
/// <para><b>Vì sao KHÔNG dùng thẳng <c>ErrorDescriptor</c> ở đây</b> — đây là chỗ dễ bị "dọn cho
/// gọn" nhất, nên ghi rõ. <c>ErrorDescriptor</c> nằm ở <c>Core.Application</c>
/// (doc/huong_dan/quy-uoc/be-architecture.md liệt nó trong nội dung của project đó), còn
/// <c>Core.Domain</c> bị cấm phụ thuộc bất cứ thứ gì — luật này được cưỡng chế bằng
/// <c>LayerDependencyTests.Core_Domain_Assembly_MustHave_ZeroPackageReference</c>. Kéo
/// <c>ErrorDescriptor</c> xuống đây thì phải kéo theo cả <c>ErrorCode</c>, mà <c>ErrorCode</c> theo
/// thiết kế "giá trị enum CHÍNH LÀ mã HTTP" — tức là nhét kiến thức về HTTP vào Domain, đúng thứ
/// sơ đồ tầng cấm.</para>
///
/// <para><b>Và nó cũng không nên có <c>ErrorCode</c>:</b> loại lỗi HTTP của một lỗi domain đã do
/// LOẠI EXCEPTION quyết định — <see cref="DomainException"/> ⇒ 422, <see cref="ConflictException"/>
/// ⇒ 409, ánh xạ nằm đúng một chỗ ở <c>ExceptionHandlingBehavior</c>. Nếu bản ghi này cũng mang
/// <c>ErrorCode</c> thì cùng một sự thật có HAI nguồn, và chúng mâu thuẫn được: một
/// <c>DomainError</c> khai <c>NotFound</c> nhưng bị ném bằng <c>ConflictException</c> vẫn biên dịch,
/// vẫn chạy, và đọc mã ở catalog sẽ ra kết luận sai về status thật trên dây.</para>
/// </summary>
/// <param name="BusinessCode">
/// Khuôn <c>MIEN.MA_LOI</c> viết hoa, có dấu chấm (<c>SYS_MENU.CODE_REQUIRED</c>). Đây là hợp đồng
/// công khai với FE — nó ra tới field <c>businessCode</c> của envelope và sẽ trở thành KHOÁ DỊCH
/// khi i18n vào, nên đổi mã là đổi một khoá đã phát tán. <c>ErrorCatalogTests</c> canh khuôn và
/// tính duy nhất của mã này chung một rổ với <c>ErrorDescriptor</c>.
/// </param>
/// <param name="MessageTemplate">
/// Chỗ giữ <c>{0}</c>, <c>{1}</c> cho <c>string.Format</c>. Câu tiếng Việt ở đây là dev-facing +
/// dự phòng, KHÔNG phải nguồn câu chữ cho người dùng cuối — FE sở hữu câu chữ và tra theo mã
/// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §3).
/// </param>
public sealed record DomainError(string BusinessCode, string MessageTemplate);
