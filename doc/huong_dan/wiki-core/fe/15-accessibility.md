---
kind: luat
scope: core
verified: 2026-09-06
---

# 15. Accessibility — điểm vào duy nhất, và mức chuẩn phải đạt

> **File này là điểm vào cho a11y, không phải nơi chứa tất cả.** Phần a11y gắn
> chặt với một chủ đề khác thì sống ở file của chủ đề đó — bảng §2 chỉ ra chính
> xác chỗ nào. Trước khi viết mục a11y mới, tra bảng đó: nếu đã có chủ, thêm vào
> đấy chứ không thêm vào đây.
>
> Lý do tồn tại: trước 2026-08-27, a11y nằm rải ở 4 file mà không có chỗ nào trả
> lời được hai câu hỏi cơ bản nhất — *"phải đạt chuẩn nào"* và *"màn hình mới
> cần kiểm gì"*. Cả hai câu đó nay ở đây.

## 1. Mức chuẩn — ĐÃ CHỐT 2026-08-27: WCAG 2.2 AA, **bắt buộc**

**Người dùng xác nhận hệ thống phục vụ khu vực công.** Vì vậy a11y ở đây là
**ràng buộc nghiệm thu**, không phải lựa chọn chất lượng — khác hẳn mọi mục
"Nhóm B, làm khi chạm ngưỡng" còn lại trong `fe/`.

Mức áp dụng: **WCAG 2.2 mức AA**, cho mọi màn hình người dùng cuối chạm tới.

Vì sao AA: A quá thấp để gọi là tiếp cận được (không buộc contrast `4.5:1`),
AAA đòi những thứ không khả thi cho ứng dụng dữ liệu dày (contrast `7:1` loại
gần hết bảng màu thông dụng). AA là mức mọi quy định về tiếp cận lấy làm mốc, và
cũng là mức ngưỡng `4.5:1` mà [04-design-token-system.md](04-design-token-system.md)
đã dùng sẵn — nên phần màu không phải đổi gì, chỉ chính thức hoá.

**Hệ quả trực tiếp:**

- Checklist §4 là **điều kiện xong việc** của mọi màn hình mới, không phải gợi ý.
- Bốn mục ở §3 chuyển từ "nên có" sang **phải có** — xem bảng §5.
- Bên mua có thể yêu cầu bằng chứng. `axe-core` (§2 → 05) sinh được báo cáo máy
  đọc; kiểm bằng tay thì cần biên bản. Chuẩn bị trước rẻ hơn dựng lại lúc bị hỏi.

> **Điều còn cần làm rõ với bên mua:** mức AA là mặc định hợp lý, nhưng nếu hợp
> đồng/hồ sơ mời thầu nêu đích danh một tiêu chuẩn khác thì **tiêu chuẩn đó
> thắng**. Hỏi trước khi đầu tư sâu, và nếu khác thì sửa mục này.

## 2. Bản đồ — a11y nào sống ở đâu

| Chủ đề | File chủ | Nội dung |
|---|---|---|
| Tương phản màu, token | [04-design-token-system.md](04-design-token-system.md) §Contrast/WCAG | Ngưỡng `4.5:1`, script kiểm `tokens.json` |
| Thứ tự Tab xuyên component | [05-component-library.md](05-component-library.md) §Tab order | Cấm `tabindex` dương, mức trang |
| 5 trạng thái component (có `:focus-visible`) | [05-component-library.md](05-component-library.md) §5 trạng thái | Mức component |
| Kiểm tự động | [05-component-library.md](05-component-library.md) §Kiểm a11y tự động | `axe-core` trên DOM đã render |
| Biểu đồ / `<canvas>` | [12-charting.md](12-charting.md) §Accessibility | 2 lớp bù cho screen reader |
| Tiêu đề trang (WCAG 2.4.2) | [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §2 quy tắc 3 | `title` cấp `Route` + `PageTitleStrategy` |
| **Còn lại** | **file này, §3** | Những thứ không thuộc chủ đề nào ở trên |

## 3. Bốn thứ chưa có chủ — ✅ CẢ BỐN ĐÃ THI CÔNG (đối chiếu `src/FE` 2026-09-06)

> ### 🔄 LẬT 2026-09-06 — mục này mang nhãn 🚧 *"CHƯA THI CÔNG"* cho **bốn** việc đã làm xong
>
> Nhãn đó có từ 2026-08-27 và không ai gỡ. Hệ quả không phải thẩm mỹ: bảng "thứ tự làm" ở §5
> xếp ba trong bốn việc này vào **nhóm 1 — làm ngay**, nên ai đọc file sẽ đi làm lại thứ đang
> chạy, hoặc tệ hơn, dựng bản thứ hai cạnh bản đã có.

Kiểm bằng lệnh, chạy từ gốc repo — đừng tin bảng nào chép tay:

```bash
grep -rn "aria-live" src/FE/src/app --include=*.html          # 3a
grep -rn "aria-invalid" src/FE/src/app --include=*.html       # 3b
grep -rn "prefers-reduced-motion" src/FE/src/styles.scss      # 3c
grep -rn "skip-link" src/FE/src/app/app.html                  # 3d
grep -o 'lang="[^"]*"' src/FE/src/index.html                  # ngôn ngữ trang
```

**3a. Thông báo động (`aria-live`) — ✅ đủ cả hai chỗ.**
Nội dung đổi mà không nằm trong vùng `aria-live` thì người dùng screen reader
**không biết gì vừa xảy ra** — thao tác của họ rơi vào im lặng.

- `src/FE/src/app/shared/components/toast/toast.html:1` — `aria-live="polite"` + `role="status"`.
- **Số dòng kết quả sau khi lọc/tìm** — chỗ bản trước ghi là còn thiếu — nay có ở
  `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:12`.

Quy tắc giữ nguyên: `polite` cho thông báo thường (toast, số dòng kết quả); `assertive`
chỉ cho lỗi chặn thao tác — dùng `assertive` tràn lan sẽ cắt ngang câu đang đọc
của người dùng, gây hại nhiều hơn lợi.

**3b. Lỗi form phải được đọc lên — ✅ ở hai form có validate, ⚠️ chưa phủ hết.**
Quy tắc: ô nhập đang lỗi gắn `aria-invalid="true"` và `aria-describedby` trỏ tới id của thông
báo lỗi. Đã thi công ở `platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html:33` và
ở `platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html`.

Một chi tiết đáng giữ, đã ghi tại chỗ trong code: cả `aria-invalid` lẫn `aria-describedby` phải
là **`null` khi không có lỗi** — `aria-describedby` trỏ tới một thẻ không tồn tại tự nó đã là
lỗi a11y.

⚠️ **Còn màn đăng nhập** (`platform/login`) hiển thị lỗi ở khối inline nhưng chưa gắn hai thuộc
tính này lên ô nhập. Đếm lại bằng lệnh `grep -rln "aria-invalid" src/FE/src/app --include=*.html`
rồi so với danh sách file có `<input>`.

**3c. `prefers-reduced-motion` — ✅ có ở `src/FE/src/styles.scss:353`.** Người đặt cài đặt này
thường vì chuyển động gây chóng mặt hoặc buồn nôn thật. Quy tắc giữ nguyên: mọi
transition/animation phải tắt được qua media query đó, và quy tắc đặt **tập trung** ở
`styles.scss` chứ không rải vào từng component.

**3d. Skip link — ✅ có ở `src/FE/src/app/app.html:24`.** App shell có sidebar cố định — không
có link "bỏ qua điều hướng" thì mỗi lần đổi trang, người dùng bàn phím phải Tab qua toàn bộ menu
trước khi tới nội dung. Hai ràng buộc đi kèm, ghi lại vì cả hai hỏng im lặng nếu quên: nó phải
là **phần tử focus được ĐẦU TIÊN** trong DOM của shell, và đích của nó phải mang `tabindex="-1"`
để nhận được focus bằng chương trình.

Điểm sáng cũ vẫn đúng: `src/FE/src/index.html` có `lang="vi"`. Bổ sung 2026-09-06 — nó **không
còn tĩnh**: `LanguageService` (`src/FE/src/app/core/i18n/language.service.ts`) đặt lại
`documentElement.lang` mỗi lần đổi ngôn ngữ, nên thuộc tính này đúng cả sau khi người dùng
chuyển sang English.

## 4. Checklist cho màn hình mới

Đủ ngắn để dùng thật; mỗi dòng trỏ về chỗ có quy tắc đầy đủ:

1. Bấm Tab từ đầu tới cuối — thứ tự khớp thứ tự nhìn thấy (§2 → 05 §Tab order).
2. Mọi thao tác làm được bằng chuột đều làm được bằng bàn phím.
3. Input có label thật; lỗi gắn `aria-invalid` + `aria-describedby` (§3b).
4. Thông báo/kết quả đổi động nằm trong vùng `aria-live` (§3a).
5. Màu mới đi qua script contrast (§2 → 04 §Contrast/WCAG).
6. Biểu đồ mang thông tin nghiệp vụ có nội dung thay thế (§2 → 12 §Accessibility).
7. `axe-core` sạch (§2 → 05 §Kiểm a11y tự động).
8. Route khai `title` ở **cấp `Route`** để `<title>` của tab đổi theo trang —
   WCAG 2.4.2 (§2 → `fe-routing-guard.md` §2 quy tắc 3). Không nhìn thấy khi thử
   bằng mắt, nên rất dễ trôi; có test canh.

Checklist này **không** thay được việc thử bằng bàn phím thật. `axe-core` bắt
được vi phạm cấu trúc DOM, nhưng "Tab đi đúng thứ tự người dùng nhìn thấy" thì
máy không phán được — cùng lý do đã ghi ở 05 §Kiểm a11y tự động.

## 5. Áp dụng vào PlatformManager

Mức chuẩn đã chốt là **bắt buộc** (§1), nên cột "Mức" dưới đây là **thứ tự
làm**, không phải "có nên làm hay không".

| Việc | Thứ tự | Chi phí |
|---|---|---|
| ~~`aria-live` cho số dòng kết quả grid (§3a)~~ | ✅ xong (đối chiếu 2026-09-06) | — |
| ~~`prefers-reduced-motion` (§3c)~~ | ✅ xong (đối chiếu 2026-09-06) | — |
| ~~Skip link (§3d)~~ | ✅ xong (đối chiếu 2026-09-06) | — |
| `aria-invalid`/`aria-describedby` — phủ nốt màn đăng nhập (§3b) | 1 | Thấp — hai form đã có mẫu để chép |
| `axe-core` vào test (§2 → 05) | 2 | Trung bình — nhưng đây là thứ sinh được **bằng chứng** cho nghiệm thu |
| Rà các màn hình đã có theo checklist §4 | 3 | Cao nhất — tỉ lệ thuận số màn, nên làm trước khi thêm màn mới |

🔄 LẬT 2026-09-06: bảng cũ xếp ba việc đầu vào nhóm 1 ("làm ngay") — cả ba **đã xong**, xem §3.
Số màn ở dòng cuối cũng bỏ đi (§6 `.claude/CLAUDE.md`): đếm bằng
`ls -d src/FE/src/app/platform/*/`.

Nhóm 3 là mục **tăng giá theo thời gian**: mỗi màn hình mới xây trước khi rà xong là một màn
nữa phải rà.
