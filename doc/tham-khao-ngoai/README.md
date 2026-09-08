---
kind: tham-chieu
scope: core
verified: khong-ap-dung
---

# Tham khảo ngoài — tài liệu KHÔNG mô tả repo này

> ## 🛑 Đọc trước khi dùng bất cứ thứ gì trong thư mục này
>
> Mọi file dưới đây mô tả **một hệ thống khác**. Chúng **không phải** đặc tả, không phải luật,
> và code của PlatformManager **không có nghĩa vụ** khớp với chúng.
>
> Dùng chúng như *tham chiếu hình dạng*: một thành phần trông ra sao khi hệ đã lớn, chữ ký class
> thật, thứ tự đăng ký DI thật. Đừng dùng chúng làm tiêu chí PASS/FAIL.

## Vì sao khu này tồn tại — và vì sao nó nằm NGOÀI `wiki-core/`

Trước 2026-09-06, loạt này nằm ở `doc/huong_dan/wiki-core/be/trien-khai/`, tức **bên trong** khu
tri thức Core. Hậu quả đo được: một lượt review ngày 2026-09-01 đối chiếu code repo này với chúng
rồi báo một loạt *"doc yêu cầu X, code không có X"* — cho những X **chưa bao giờ** là yêu cầu ở
đây. Mỗi file đều có cảnh báo ở đầu, nhưng cảnh báo nằm trong file thì chỉ cứu được người đã mở
file; người đọc mục lục `wiki-core/` thì thấy chúng đứng ngang hàng với luật thật.

Chuyển ra ngoài để **vị trí thư mục** nói lên điều đó, thay vì trông chờ mỗi người đọc nhớ một
dòng cảnh báo. Quyết định người dùng 2026-09-06.

## Đang có gì

| Thư mục | Nguồn | Nội dung |
|---|---|---|
| `vnr-successor/` | `VNR.Successor` — backend .NET 8 production, ~73 project, 8 module, 9 process | Lộ trình xây dựng 7 phase (P0–P6): chữ ký class, thứ tự đăng ký DI, hình dạng ArchTest |

PlatformManager là hệ **tầm trung** (~13 project, 1 process). Chênh lệch quy mô đó là lý do chép
nguyên xi sẽ mua toàn bộ chi phí mà không mua được lợi ích — mỗi file đều ghi rõ chỗ nào là
`[ĐƠN GIẢN HOÁ]`.

## Thứ tương ứng nhưng nói về CHÍNH repo này

`doc/huong_dan/wiki-core/be/tra-cuu-file-class.md` — tra cứu file/class/interface **có thật trong
`src/BE/`**. Khi nó và tài liệu trong thư mục này nói khác nhau, **nó đúng**.

## Về nhãn `verified:`

Các file ở đây mang `verified: chua-doi-chieu`, và **sẽ mang mãi mãi**: nguồn đối chiếu của chúng
nằm ở một repo khác, không có trong cây này, nên không ai đối chiếu được — mà đóng dấu một ngày
cho chúng thì đúng là lời nói dối `.claude/CLAUDE.md` §4 cấm.

> ⚠️ **Hệ quả cho bộ đếm của cổng.** §9 chỉ cho `verified:` nhận **hai** giá trị: một ngày, hoặc
> `chua-doi-chieu`. Không có giá trị nào nghĩa là *"không áp dụng được"*. Vì vậy 9 file ở khu này
> sẽ vĩnh viễn bị đếm là "chưa đối chiếu", và con số mà §9 nói *"nên giảm dần"* có một cái sàn
> không bao giờ chạm tới được.
>
> Thử đặt `verified: khong-ap-dung` ở chính file này ngày 2026-09-06 thì cổng **từ chối** — đó là
> cách vấn đề này lộ ra. Đề xuất sửa (thêm giá trị thứ ba, đếm ba nhóm riêng) đang chờ quyết định;
> chưa ai đổi §9.
