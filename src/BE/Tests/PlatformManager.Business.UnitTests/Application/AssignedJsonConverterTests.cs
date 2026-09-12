using System.Text.Json;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>Q74 ở tầng ĐỌC JSON</b> — phân biệt "khoá VẮNG MẶT khỏi thân request" với "khoá CÓ MẶT mang
/// <c>null</c>".
///
/// <para>🔴 <b>Vì sao bộ test này tồn tại (finding F6, 2026-09-11):</b> trước nó,
/// <c>AssignedJsonConverterFactory</c> có đúng MỘT dòng nối ở <c>Program.cs</c> và
/// <c>grep AssignedJsonConverter src/BE/Tests</c> trả về RỖNG. Gỡ dòng đó ⇒ mọi khoá có mặt đọc ra
/// <see cref="Assigned{T}.Unset"/> ⇒ <b>mọi lệnh ghi âm thầm thành "không gửi gì cả"</b>: người
/// dùng bấm Lưu, nhận 200, và không trường nào đổi.</para>
///
/// <para><c>ImportSeamUsageTests</c> canh DÂY NỐI (factory có được <c>Converters.Add</c> không);
/// bộ này canh HÀNH VI (nối rồi thì nó đọc đúng không). Cần cả hai — một dây nối đúng tới một
/// converter sai vẫn hỏng y hệt.</para>
/// </summary>
public class AssignedJsonConverterTests
{
    /// <summary>
    /// Phải khớp cấu hình thật ở <c>Program.cs</c>: camelCase + factory. Lệch bộ tuỳ chọn thì bộ
    /// test này đo một thứ khác thứ đang chạy.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new AssignedJsonConverterFactory() },
    };

    private static AssessmentPayload Parse(string json) =>
        JsonSerializer.Deserialize<AssessmentPayload>(json, Options)!;

    [Fact(DisplayName = "Khoá VẮNG MẶT ⇒ Unset (⇒ copy-forward ở tầng luật ghi)")]
    public void AbsentKey_IsUnset()
    {
        var payload = Parse("""{ "period": "2026-W33" }""");

        Assert.False(payload.SelfScore.IsSet);
        Assert.False(payload.Status.IsSet);
        Assert.False(payload.Note.IsSet);
        Assert.False(payload.Deadline.IsSet);
        Assert.False(payload.OwnerId.IsSet);
    }

    /// <summary>
    /// Vế còn lại, và là vế dễ mất nhất: khoá CÓ MẶT mang <c>null</c> phải là <b>đã gửi</b> — nếu
    /// không thì không có cách nào xoá trắng một ô qua dialog.
    /// </summary>
    [Fact(DisplayName = "Khoá CÓ MẶT mang null ⇒ IsSet = true, Value = null (⇒ XOÁ TRẮNG)")]
    public void ExplicitNull_IsSet()
    {
        var payload = Parse("""{ "period": "2026-W33", "note": null, "selfScore": null }""");

        Assert.True(payload.Note.IsSet);
        Assert.Null(payload.Note.Value);

        Assert.True(payload.SelfScore.IsSet);
        Assert.Null(payload.SelfScore.Value);
    }

    [Fact(DisplayName = "Khoá CÓ MẶT mang giá trị ⇒ đọc đúng giá trị, đúng kiểu")]
    public void PresentValue_IsParsed()
    {
        var payload = Parse(
            """
            {
              "period": "2026-W33",
              "selfScore": 12.5,
              "status": "Đang thực hiện",
              "deadline": "2026-12-31",
              "ownerId": "01a08e27-a9ee-7937-8850-1a8632303a59"
            }
            """);

        Assert.Equal(12.5m, payload.SelfScore.Value);
        Assert.Equal("Đang thực hiện", payload.Status.Value);
        Assert.Equal(new DateOnly(2026, 12, 31), payload.Deadline.Value);
        Assert.Equal(Guid.Parse("01a08e27-a9ee-7937-8850-1a8632303a59"), payload.OwnerId.Value);
    }

    /// <summary>
    /// Phép thử KHÉP KÍN của Q74: hai payload chỉ khác nhau ở CÓ/KHÔNG CÓ khoá phải cho ra hai kết
    /// quả KHÁC NHAU sau khi đi qua luật ghi. Đây là thứ mà một test chỉ đọc JSON không chứng minh
    /// được — nó nối tầng đọc với tầng luật.
    /// </summary>
    [Fact(DisplayName = "Khép kín — vắng khoá ⇒ copy-forward; có khoá mang null ⇒ xoá trắng")]
    public void AbsentVersusNull_ProduceDifferentWrites()
    {
        var prior = PlatformManager.Business.Domain.Entities.CriteriaAssessment.Create(
            Guid.CreateVersion7(), new DateOnly(2026, 8, 3));
        prior.SetNote("ghi chú kỳ trước");

        var absent = Apply("""{ "period": "2026-W33" }""", prior);
        var explicitNull = Apply("""{ "period": "2026-W33", "note": null }""", prior);

        Assert.Equal("ghi chú kỳ trước", absent);
        Assert.Null(explicitNull);
    }

    private static string? Apply(string json, PlatformManager.Business.Domain.Entities.CriteriaAssessment prior)
    {
        // Đi qua ĐÚNG bộ dịch mà DM-3/DM-4 dùng, không dựng AssessmentWrite bằng tay: dựng tay thì
        // bộ test tự khẳng định điều nó muốn nghe, và chỗ dễ sai nhất (payload → write) nằm ngoài.
        var write = AssessmentPayloadMapper.ToWrite(Parse(json), out var statusError);
        Assert.Null(statusError);

        var target = PlatformManager.Business.Domain.Entities.CriteriaAssessment.Create(
            prior.CriteriaId, new DateOnly(2026, 8, 12));

        AssessmentUpsert.Apply(target, prior, write);
        return target.Note;
    }
}
