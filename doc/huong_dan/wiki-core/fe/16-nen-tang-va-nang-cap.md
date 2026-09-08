---
kind: luat
scope: core
verified: 2026-09-06
---

# 16. Nền tảng chạy & chính sách nâng cấp

> Hai câu hỏi không có chủ trước 2026-08-27: **"app này chạy được trên trình
> duyệt nào"** và **"bao lâu nâng Angular một lần"**. Cả hai đều là loại quyết
> định mà không ai đặt ra cho tới lúc đã quá muộn để đổi rẻ.
>
> Không thuộc file này: quét lỗ hổng dependency —
> [14-security.md](14-security.md) §4. Ngân sách bundle —
> [13-performance.md](13-performance.md) §4.

## 1. Vì sao đây không phải việc "để sau"

Angular ra **major mỗi 6 tháng**, mỗi major được hỗ trợ **18 tháng** (6 tháng
active + 12 tháng LTS). Nghĩa là bỏ qua 3 kỳ liên tiếp là rơi khỏi vùng hỗ trợ,
và lúc đó không còn bản vá bảo mật.

Cái đắt không phải một lần nâng — mà là nhảy **3 major cùng lúc**. Mỗi major có
đường di trú riêng (`ng update` chạy schematic tương ứng); nhảy cóc thì các
schematic không còn áp được theo thứ tự, phải sửa tay toàn bộ breaking change
của cả 3 kỳ trong một lần, trên một codebase đã lớn hơn nhiều.

Đây là dạng nợ **duy nhất trong tài liệu này tự tăng theo thời gian mà không cần
ai viết thêm dòng code nào**.

## 2. Nhịp nâng cấp — ĐÃ CHỐT 2026-08-27: **TẠM HOÃN**, nâng theo nhu cầu

Người dùng chốt: **không nâng version theo lịch; chỉ nâng khi có bug thật.**

Đây là quyết định đã cân nhắc, không phải bỏ sót — mục này ghi lại **cái giá đã
chấp nhận** và **mốc phải xem lại**, để lần sau không ai phải suy đoán lý do.

**Cái giá đã chấp nhận:** khoản nợ ở §1 vẫn tăng theo lịch. Mỗi kỳ Angular trôi
qua là một bước `ng update` nữa phải làm bù sau này, và các bước đó không cộng
tuyến tính — nhảy 3 major cùng lúc đắt hơn 3 lần nhảy 1 major, vì schematic di
trú không còn áp được theo thứ tự.

**Ba mốc buộc phải xem lại — không phải "khi thấy tiện":**

| Mốc | Vì sao không hoãn tiếp được |
|---|---|
| Angular 20 **hết hạn hỗ trợ** (~18 tháng kể từ khi ra) | Không còn bản vá bảo mật. Đây là mốc cứng, không thương lượng được |
| Một lỗ hổng `high`/`critical` (§14 §4) **chỉ vá được bằng nâng major** | Lúc đó nâng là bắt buộc và gấp — đúng tình huống tệ nhất để nâng |
| Đã tụt **2 major** | Ranh giới giữa "một bước `ng update`" và "một dự án riêng" |

**Nếu vẫn muốn nâng** (không bắt buộc theo quyết định trên): thứ tự là
`ng update @angular/core @angular/cli` trước, rồi mới tới thư viện bên thứ 3 —
không gộp một lần.

> Tham chiếu cho lần xem lại: nhịp thường dùng ở hệ thống tầm trung là **không
> để tụt quá 1 major**, patch/minor gộp vào PR thường, major cần người quyết
> tường minh. Ghi lại ở đây để lần sau có mốc so sánh, **không** phải để phủ
> nhận quyết định đã chốt ở trên.

## 3. Ràng buộc thật: PrimeNG quyết định nhịp, không phải Angular

Đây là chỗ kế hoạch ở §2 hay vỡ trong thực tế. `@angular/core` nâng được ngay
ngày ra bản mới; **PrimeNG thường sau vài tuần tới vài tháng**, và PrimeNG là
thư viện UI mà toàn bộ grid/chart/form của hệ thống này đứng trên
([05-component-library.md](05-component-library.md),
[11-grid-and-metadata.md](11-grid-and-metadata.md),
[12-charting.md](12-charting.md)).

**Quy tắc: không nâng Angular major trước khi PrimeNG có bản hỗ trợ.** Nâng
trước sẽ dẫn tới `--force` để cài cho qua, rồi phát hiện vỡ ở đúng những component
phức tạp nhất, lúc đã không lùi được.

Kiểm trước khi bắt đầu:

```bash
cd src/FE && npm outdated
npm info primeng peerDependencies
```

Nếu bản vá bảo mật (§14 §4) đòi nâng major mà PrimeNG chưa sẵn sàng: xử lý bằng
`overrides` trong `package.json` cho đúng package bắc cầu bị ảnh hưởng, **không**
nâng cả Angular. Ghi lại lý do ngay cạnh `overrides` — thứ đó phải được gỡ khi
nâng thật, và không ai nhớ nếu không viết.

## 4. Sau mỗi lần nâng — kiểm gì

Không đủ khi chỉ `ng build` xanh. Bốn thứ hay vỡ âm thầm, xếp theo khả năng vỡ:

1. **Giao diện PrimeNG** — preset theme là API dễ đổi giữa các major. So bằng
   ảnh chụp màn hình, không nhìn lướt.
2. **Zoneless** — dự án chạy zoneless ([13-performance.md](13-performance.md) §1);
   thay đổi trong change detection ảnh hưởng thẳng vào đây.
3. **Bundle size** — so với ngân sách ở [13-performance.md](13-performance.md) §4.
4. **Test** — theo [06-testing-strategy.md](06-testing-strategy.md).

## 5. Ma trận trình duyệt — ✅ ĐÃ KHAI BÁO (2026-08-28)

```bash
grep -c "browserslist" src/FE/package.json    # PASS khi ≥1
```

`browserslist` nay khai tường minh trong `package.json`, **giá trị bằng đúng
danh sách mặc định của Angular 20.3** (đối chiếu: `npx browserslist` và
`npx browserslist --config=node_modules/@angular/build/.browserslistrc` cho ra
cùng một tập, `diff` rỗng).

Nghĩa là **không đổi hành vi hôm nay** — thứ nó đổi là: phạm vi trình duyệt hỗ
trợ từ nay do đội quyết định, và **không còn tự trôi** sau mỗi lần nâng Angular
mà không ai được báo. Có số liệu người dùng thật thì siết lại danh sách này.

Khi siết lại, cân hai chiều — cả hai đều xấu:

- **Quá rộng** → bundle phình vì phải hạ cấp cú pháp và thêm polyfill cho trình
  duyệt không ai dùng, ăn thẳng vào ngân sách ở
  [13-performance.md](13-performance.md) §4.
- **Quá hẹp** → có máy trong cơ quan mở lên là trắng trang, và lỗi này không
  xuất hiện trên máy dev nên không ai biết cho tới khi người dùng báo.

## 6. Áp dụng vào PlatformManager

| Việc | Mức | Ghi chú |
|---|---|---|
| ~~Khai `browserslist`~~ (§5) | ✅ xong 2026-08-28, đối chiếu lại 2026-09-06 | Không nằm trong phạm vi hoãn ở §2 — xem ghi chú dưới. Khối `"//browserslist"` cạnh nó ghi rõ căn cứ: chép đúng mặc định Angular 20.3 vì **chưa có số liệu người dùng thật** để thu hẹp |
| Theo dõi 3 mốc xem lại (§2) | Định kỳ | Không phải việc code; chỉ cần biết mốc nào tới |
| Kiểm PrimeNG peer trước khi nâng (§3) | Khi thật sự nâng | Một lệnh |
| Checklist sau nâng (§4) | Khi thật sự nâng | — |

**Vì sao `browserslist` không thuộc diện hoãn** (lý do giữ lại để lần sau không
ai gộp nhầm nó vào §2): quyết định §2 là *"chỉ nâng khi có bug"*. Thiếu
`browserslist` **chính là** một nguồn bug thuộc loại tệ nhất — trang trắng trên
máy người dùng, không tái hiện được trên máy dev, không lộ ra ở bất kỳ bước build
hay test nào. Khai nó là **chặn bug**, không phải nâng cấp.

Hiện `src/FE` đang ở **Angular 20 + PrimeNG 20** (đối chiếu 2026-09-06) — chưa tụt major nào.
Đó là lý do quyết định hoãn ở §2 hôm nay không gây hậu quả gì; ba mốc ở §2 tồn
tại để bắt đúng lúc điều đó thôi còn đúng.

Đừng chép số phiên bản vào tài liệu nào khác (`.claude/CLAUDE.md` §6) — đọc thẳng, và so với
bản mới nhất bằng lệnh thứ hai:

```bash
grep -E '"@angular/core"|"primeng"' src/FE/package.json
cd src/FE && npm outdated @angular/core primeng     # rỗng = chưa tụt
```

**Bổ sung 2026-09-06 — đã có một phụ thuộc mới kể từ lần ghi này:** `@ngx-translate/core` +
`@ngx-translate/http-loader` (v18) và `primelocale`, về cùng đợt i18n runtime. Chúng nằm ngoài
nhịp phát hành của Angular nên **không** đổi ba mốc ở §2, nhưng có mặt trong danh sách phải
kiểm ở §3 khi thật sự nâng major.
