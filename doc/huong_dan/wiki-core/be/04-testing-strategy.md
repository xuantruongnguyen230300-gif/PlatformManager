# 4. Kiểm thử là 1 phần của kiến trúc, không phải việc làm sau cùng

> Cùng nguyên tắc Nhóm A/B như [01-core-components.md](01-core-components.md):
> **không phải checklist bắt buộc**, chỉ đầu tư khi hệ thống thật sự chạm
> đúng nỗi đau tương ứng.

**Insight giá trị nhất từ `testing.md` của VNR: biến quy tắc kiến trúc thành TEST CHẠY ĐƯỢC (ArchTest), không chỉ ghi trong tài liệu.** Toàn bộ những nguyên tắc ở [01-core-components.md](01-core-components.md) (không public setter, controller phải có permission, không inject EF Core ở Application...) — VNR không chỉ *viết ra*, mà **viết thành 1 bộ test tự động chặn PR nếu vi phạm** (`dotnet test VNR.ArchTests` chạy **trước** unit test trong CI — kiến trúc sai thì unit test đúng cũng vô nghĩa). Đây là cách duy nhất đảm bảo quy ước không bị quên dần theo thời gian khi có người mới tham gia hoặc code base lớn lên — tài liệu (như wiki này) chỉ có tác dụng lúc đọc, ArchTest có tác dụng **mãi mãi, tự động**.

Với hệ thống mới bắt đầu nhỏ, không cần cả bộ ArchTest phức tạp như VNR ngay — nhưng nên có tối thiểu 2-3 test kiến trúc cốt lõi từ ngày đầu (ví dụ: "không entity nghiệp vụ nào có public setter", "mọi controller có `[Authorize]`/permission attribute"), rồi thêm dần khi phát hiện vi phạm thật.

## 3 tầng kiểm thử (test pyramid) — áp dụng đúng loại test cho đúng mục đích, đừng lẫn lộn

| Tầng | Kiểm tra gì | Công cụ .NET |
|---|---|---|
| ArchTest | Quy tắc kiến trúc (layer, naming, permission) | `NetArchTest`/Roslyn phân tích assembly |
| Unit test | Logic handler/domain — mock repository | xUnit + NSubstitute (không Moq) |
| Integration test | EF migration, FK constraint, unique index thật | **Testcontainers.PostgreSql** — container Postgres thật, không phải InMemory |

## Gotcha đáng nhớ nhất

❌ **Không bao giờ dùng `UseInMemoryDatabase` để test** — InMemory provider của EF Core **bỏ qua hoàn toàn** FK constraint, unique index, transaction thật. Test pass trên InMemory không chứng minh được gì về hành vi thật trên Postgres — chỉ dùng mock (unit test) hoặc Testcontainers (integration test).
