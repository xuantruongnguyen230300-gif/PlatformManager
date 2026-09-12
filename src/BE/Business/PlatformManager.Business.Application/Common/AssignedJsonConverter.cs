using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformManager.Business.Application.Common;

/// <summary>
/// Cho phép <see cref="Assigned{T}"/> đọc được từ JSON — <b>đây là thứ làm Q74 chạy được ở DM-3 và
/// DM-4</b> (spec/danh-muc-dti/business-rules.md §6.2).
///
/// <para><b>Cơ chế, và vì sao nó đủ:</b> <c>System.Text.Json</c> chỉ gọi converter khi KHOÁ CÓ MẶT
/// trong thân request. Khoá vắng mặt ⇒ thuộc tính giữ nguyên giá trị mặc định của kiểu, mà mặc định
/// của <see cref="Assigned{T}"/> chính là <see cref="Assigned{T}.Unset"/>. Nghĩa là phép phân biệt
/// <i>"khoá vắng mặt"</i> với <i>"khoá mang <c>null</c>"</i> có được MIỄN PHÍ từ chính cách bộ đọc
/// hoạt động — không cần đọc thô <c>JsonDocument</c> rồi tự dò khoá.</para>
///
/// <para><b>Vì sao phải phân biệt hai ca đó</b> (§5.3 bước 2): trường KHÔNG nằm trong request thì
/// được copy-forward từ bản ghi kỳ trước; trường có mặt mang <c>null</c> thì XOÁ TRẮNG. Với một
/// trường vốn đã nullable, <c>null</c> không diễn đạt được cả hai nghĩa — đúng ca mà đường import
/// đã vấp thật ngày 2026-09-11 và làm mất bốn cột dữ liệu.</para>
///
/// <para>⚠️ <b>Một chiều, cố ý:</b> <see cref="Write"/> ném. <see cref="Assigned{T}"/> là kiểu của
/// ĐẦU VÀO — nó mô tả "client có gửi trường này không", một câu hỏi vô nghĩa ở chiều đi ra. Response
/// dùng DTO thường. Ném thay vì âm thầm ghi ra một object <c>{isSet, value}</c> vì object đó sẽ rò
/// ra dây và không hợp đồng nào khai nó.</para>
/// </summary>
public sealed class AssignedJsonConverter<T> : JsonConverter<Assigned<T>>
{
    public override Assigned<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Tới được đây nghĩa là khoá CÓ MẶT. Giá trị `null` vẫn là "có gửi" — và đó chính là ca
        // xoá trắng có chủ đích.
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Assigned<T>.Set(value!);
    }

    public override void Write(Utf8JsonWriter writer, Assigned<T> value, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(Assigned<T>)}<{typeof(T).Name}> chỉ dùng cho ĐẦU VÀO — nó trả lời câu hỏi " +
            "\"client có gửi trường này không\", thứ vô nghĩa ở chiều đi ra. Response phải dùng DTO thường.");
}

/// <summary>
/// Nối <see cref="AssignedJsonConverter{T}"/> vào bộ đọc cho MỌI tham số kiểu <c>T</c>.
///
/// <para>Cần một factory vì <see cref="Assigned{T}"/> là generic mở: không thể gắn
/// <c>[JsonConverter(typeof(AssignedJsonConverter&lt;T&gt;))]</c> lên khai báo kiểu được.</para>
///
/// <para><b>Đăng ký một lần ở host</b> (<c>Program.cs</c>, cùng chỗ đặt <c>PropertyNamingPolicy</c>)
/// chứ không gắn attribute lên từng DTO: gắn từng chỗ nghĩa là mỗi DTO ghi mới phải NHỚ gắn, và chỗ
/// nào quên sẽ đọc ra <see cref="Assigned{T}.Unset"/> cho mọi trường — tức mọi lời ghi âm thầm
/// thành "không gửi gì cả". Hỏng im lặng, đúng lớp lỗi mà kiểu này sinh ra để chặn.</para>
/// </summary>
public sealed class AssignedJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Assigned<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(AssignedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}
