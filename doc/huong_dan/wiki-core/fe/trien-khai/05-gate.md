---
kind: luat
scope: core
verified: 2026-09-06
---

# Gate — kiểm tra tương đương ArchTest phía FE

> Angular không có khái niệm ArchTest như .NET (không có "assembly" để soi
> dependency bằng reflection) — tương đương gần nhất là **lint rule + script
> kiểm tra chạy trong CI**, cùng tinh thần "luật kiến trúc phải có máy
> kiểm, không dựa vào review người" (`tham-khao-ngoai/vnr-successor/00 §1`).

## Bộ kiểm tra tối thiểu

| # | Kiểm tra gì | Cách chạy | Khi nào cần |
|---|---|---|---|
| G1 | Không hex color literal ngoài `styles.scss :root` | `grep -rn "#[0-9a-fA-F]\{3,6\}" src/app --include=*.scss` → CI fail nếu có kết quả ngoài whitelist | Ngay sau F2 |
| G2 | Mọi `@for` có `track` | ESLint rule Angular (`@angular-eslint/template/...`) hoặc grep `@for` không kèm `track` trên cùng khối | Ngay từ đầu — F0 |
| G3 | Không còn `*ngIf`/`*ngFor`/`*ngSwitch`/`@Input()`/`@Output()`/`NgModule` | Grep — đã dùng ở audit trước, giữ làm gate thường trực | Ngay từ đầu |
| G4 | Component dumb (`components/`) không inject `HttpClient`/service data | `bash scripts/fe-gate.sh` (section G4) — quét `inject(...)` trong mọi thư mục `components/`, sau khi đã bỏ chú thích. Hai trục miễn trừ RỜI NHAU, xem §G4 | ✅ **Đã bật 2026-09-08, XANH** — bốn canary hai chiều đã chạy, xem §G4 |
| G5 | Mọi file `services/*.service.ts` có ít nhất 1 file `.spec.ts` cạnh nó | Script đối chiếu tên file | Sau F2 |
| G6 | Không import trực tiếp DTO trong `components/`/`pages/` (chỉ `services/` được import) | Grep `Dto` trong `components/`, `pages/` ngoài `services/` | Ngay từ đầu — đã PASS ở audit trước, giữ làm gate để không trôi |
| G7 | Bundle không vượt ngân sách | `angular.json` `budgets` — `ng build` fail khi vượt `maximumError` (xem `../13-performance.md` §4) | Sau F4 — chỉnh ngưỡng theo số đo thật, không giữ mặc định của `ng new` |
| G8 | `modules/<A>/` không import trực tiếp nội bộ `modules/<B>/` (module nghiệp vụ khác) | ESLint `eslint-plugin-import` rule `no-restricted-paths` — chặn import chéo giữa 2 module nghiệp vụ, vẫn cho phép import từ `core/`/`shared/`/`platform/` (xem `doc/kien-truc-core-module.md`) | Ngay khi có module nghiệp vụ thứ 2. **Hôm nay là no-op có chủ đích (đối chiếu 2026-09-06)**: `BUSINESS_MODULES` trong `src/FE/eslint.config.js` rỗng nên cả block rule bị bỏ hẳn — `src/app/modules/` chưa tồn tại, không có gì để vi phạm. Lý do phải bỏ hẳn block (schema đòi `zones` ≥ 1 phần tử) ghi ngay tại chỗ khai |
| G9 | `core/` KHÔNG import ngược lên `shared/`/`platform/`/`modules/` — `core/` là tầng đáy | ESLint `import/no-restricted-paths`, zone `target: ./src/app/core` (`eslint.config.js` — hằng `coreLayerZones`), chạy qua `ng lint` | **Đã bật 2026-08-24** (`src/FE/eslint.config.js` — hằng `coreLayerZones`, xác nhận bằng canary: thử import tạm `shared/` từ 1 file `core/` → `ng lint` báo lỗi, rồi revert). Vi phạm thật đã xảy ra trước đó: `core/interceptors/http-error.interceptor.ts` import `ToastService` từ `shared/services/` — đã sửa bằng cách chuyển `ToastService` vào `core/toast/toast.service.ts` (service hạ tầng, đúng chỗ ở `core/`); component hiển thị `shared/components/toast/toast.ts` import ngược lại từ `core/` — đúng chiều được phép |
| G10 | *(số chưa cấp phát — để trống có chủ đích, tránh phải đánh lại số khi thêm gate)* | — | — |
| G11 | Không màu literal `rgb()`/`rgba()` trong SCSS của `core/` + `shared/` + `platform/` | `bash scripts/fe-gate.sh` (section G11) — tha đúng **một** dạng, và tha theo CÚ PHÁP: `rgb(var(--x) / a)`; **không** tha theo giá trị | **Đã bật 2026-09-03** (`scripts/fe-gate.sh`), xác nhận đỏ-rồi-xanh: lúc thêm, gate liệt đúng 4 dòng (`sidebar.scss:115` nền mục menu đang chọn — chính là `--brand` viết thập phân, `sidebar.scss:211` scrim, `toast.scss:25` bóng đổ, `topbar.scss:5` nền topbar), rồi xanh sau khi 4 giá trị được promote thành token ở `src/FE/src/styles.scss` § `--surface-nav-active`, `--overlay-backdrop`, `--surface-topbar`, `--shadow-toast` |
| G12 | Template `.html` không chứa chữ tiếng Việt — mọi câu người dùng đọc phải đến từ `public/i18n/<code>.json` | `bash scripts/fe-gate.sh` (section G12) — quét dấu thanh tiếng Việt trong `src/app/**/*.html`, **bỏ qua comment HTML** | ✅ **Đã bật 2026-09-05, XANH (đối chiếu 2026-09-06)** — `bash scripts/fe-gate.sh` thoát 0, section G12 báo OK. 🔄 LẬT 2026-09-06: bản trước ghi *"đang ĐỎ, Pha D chưa xong"*; Pha D đã xong. Xem §G12 |

## G4 — vì sao chặn theo TÊN SERVICE, không chỉ theo danh sách file

Bật 2026-09-08. Luật G4 có từ lâu; thứ vừa thêm là **máy cưỡng chế**.

**Khoảng trống đó có giá đo được.** `user-grid-table.ts` từng inject thẳng
`LanguageService` và làm **37 test đỏ** (`LanguageService` đòi token `CORE_I18N` mà spec màn
nghiệp vụ không cấp ⇒ `NG0201`). Thứ bắt được là **bộ test**, không phải cổng — nên lần vi
phạm kế tiếp ở một chỗ không có test tương ứng sẽ đi lọt. Ghi chú tại
`src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts`
nói thẳng điều này, và đó chính là lý do gate được viết.

### Bản phác trong bảng trên KHÔNG đủ, và chỗ nó hụt là chỗ đáng nhớ

Cột "Cách chạy" của G4 trước đây gợi ý: *quét `inject(...Service)` trong `components/`, trừ
danh sách ngoại lệ app-shell*. Làm đúng nguyên văn thì cổng **đỏ ngay trên code sạch**:
`platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts` inject
`TranslateService` + `ApiErrorMessageService`, và nó **không** phải app-shell.

Đưa file đó vào danh sách ngoại lệ là lối thoát sai: nó mở toang cả file cho **mọi** service,
kể cả `QuanTriNguoiDungService` — tức tha đúng thứ G4 sinh ra để cấm.

Nguyên nhân là bản phác lẫn hai câu hỏi khác nhau. Luật nói về **cái được inject**
("`HttpClient`/service **data**"), còn danh sách file trả lời **ai được inject**. Cổng cần cả
hai, rời nhau:

| Trục | Chặn gì | Miễn trừ ở đâu |
|---|---|---|
| Cái được inject | `inject(<X>Service)` | `UI_INFRA_SERVICES` — service không chạm dữ liệu |
| Ai được inject | service **dữ liệu** trong lớp vỏ app | `APP_SHELL_FILES` — ngoại lệ ở [`../05-component-library.md`](../05-component-library.md) §"Ngoại lệ inject" |

`inject(HttpClient)`/`inject(HttpBackend)` nằm ngoài cả hai trục: **FAIL tuyệt đối**, kể cả
trong app-shell. Chúng không kết thúc bằng `Service` nên phải bắt tên tường minh — bỏ hai tên
đó là để hở đúng ca thô thiển nhất.

**Mặc định là CẤM.** Service lạ chưa có tên trong bảng nào thì đỏ, buộc người thêm phải phân
loại nó tường minh. Danh sách thật đọc từ script, đừng chép ra đây (`.claude/CLAUDE.md` §6):

```bash
grep -n 'UI_INFRA_SERVICES=\|APP_SHELL_FILES=' scripts/fe-gate.sh
```

### Allowlist tự kiểm chính nó

`UI_INFRA_SERVICES` là một lời khẳng định — *"mấy service này không chạm dữ liệu"* — và một
lời khẳng định không ai kiểm lại sẽ mục. Chỉ cần có người thêm `HttpClient` vào `ToastService`
là G4 mất tác dụng **trong im lặng**, vì cái tên vẫn nằm nguyên trong danh sách.

Nên trước khi dùng danh sách, section G4 tra ngược từng tên ra file khai nó và bắt lỗi nếu
file đó dùng `HttpClient`/`HttpBackend`. Tên không tìm thấy trong `src/app` = service của thư
viện ngoài (vd `TranslateService` của `@ngx-translate/core`) — không kiểm được, bỏ qua.

### 🛑 Lỗ mù đã biết — G4 KHÔNG phủ hết

Chỉ bắt định danh kết thúc bằng `Service`. **`inject(FeatureStore)` của `@ngrx/signals` đi
lọt**, dù bảng trách nhiệm ở
[`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) cấm component dumb
biết "state global" y như cấm nó biết HTTP.

Cố ý chưa bắt: hôm nay chưa có store nào trong app, và §"Không làm" ngay cuối file này cấm
viết gate cho thứ chưa tồn tại. Thêm `*Store` **cùng lúc** với store đầu tiên.

### Xác nhận bộ dò — bốn canary, hai chiều (đã chạy 2026-09-08)

| Chiều | Phép thử | Kết quả |
|---|---|---|
| Không bỏ sót | Component mới trong `components/` inject `QuanTriNguoiDungService` | Bị bắt, **đúng số dòng** |
| Không bỏ sót | `inject(HttpClient)` thêm vào `sidebar.ts` — file ĐANG được miễn trừ | Vẫn bị bắt (miễn trừ file không phủ `HttpClient`) |
| Không bỏ sót | Thêm `HttpClient` vào `ToastService` | Bị bắt ở bước tự kiểm allowlist |
| Không báo nhầm | `inject(<tên>Service)` nằm trong comment khối, comment `//`, và sau một URL `https://` | Không bị bắt |

Canary cuối không thừa: repo này giải thích luật G4 **ngay trong JSDoc**, nên `data-grid.ts`
có nguyên chuỗi ``không `inject(LanguageService)``` trong comment. Dò trên file thô là cổng tự
báo lỗi vì chính lời cảnh báo của mình — nên G4 bỏ chú thích trước, và bỏ theo cách **giữ
nguyên số dòng**, đúng bài học đã trả giá ở G12 ngay dưới đây.

Riêng `//`: mẫu xoá dùng `(?<!:)` để `https://…` không bị coi là mở đầu chú thích. Thiếu nó
thì mọi thứ sau một URL trên cùng dòng bị nuốt — hướng hỏng **bỏ sót**, đắt hơn báo nhầm.

Lệnh liệt kê chỗ còn vi phạm (dùng khi cổng đỏ):

```bash
bash scripts/fe-gate.sh 2>&1 | sed -n '/G4/,/^$/p'
```


## G12 — vì sao dò **dấu thanh**, không dò "chuỗi trong thẻ"

Thêm 2026-09-05, thuộc Pha C của đợt i18n (chuẩn bị cổng **trước** khi bọc chuỗi hàng loạt).

**Cách dò hiển nhiên là sai.** Ý đầu tiên ai cũng nghĩ là "tìm text node không đi qua
`| translate`". Nó cần parse template Angular cho đúng, và nó báo nhầm dày đặc: dấu câu,
số, ký hiệu, tên riêng, `&nbsp;` — tất cả đều là text node hợp lệ không cần dịch.

**Dấu thanh tiếng Việt thì không nhầm được.** Ứng dụng này viết tiếng Việt trước, nên một
ký tự `ế` trong template **luôn** là câu cho người dùng đọc — nó không thể là tên biến,
tên thuộc tính, tên lớp CSS hay từ khoá Angular. Tỉ lệ báo nhầm gần như bằng 0, và nó
không cần biết gì về cú pháp Angular.

**Giới hạn phải nói thẳng — cổng này KHÔNG bắt được chuỗi cứng tiếng Anh.** `<button>Save</button>`
đi lọt. Đó là đánh đổi có chủ đích: dò được cả tiếng Anh thì phải parse template và đổi lại
bằng một cơn mưa báo nhầm, mà một cổng hay báo nhầm là một cổng người ta tắt đi. Ở repo
này nguồn chuỗi cứng gần như luôn là tiếng Việt, nên cổng phủ đúng chỗ rủi ro thật.

**Bỏ qua comment HTML** (`<!-- … -->`): repo này chú thích bằng tiếng Việt ở khắp nơi, và
chú thích thì không ai đọc trên màn hình. Không bỏ qua thì cổng đỏ vì đúng thứ nó nên
khuyến khích.

**Phạm vi chỉ `.html`, KHÔNG `.ts`.** File `.ts` đầy chú thích tiếng Việt theo quy ước của
repo. Chuỗi cứng trong `.ts` là bài toán riêng, cần dò trong string literal chứ không dò
cả file — chưa làm, và cố ý không gộp vào đây.

### Xác nhận bộ dò — hai canary, hai chiều

Một bộ dò chỉ đáng tin khi chứng minh được **cả hai** chiều. Đã chạy 2026-09-05:

| Chiều | Phép thử | Kết quả |
|---|---|---|
| Không báo nhầm | File chỉ có chữ tiếng Việt **trong comment** (kể cả comment nhiều dòng) | Không bị bắt; tổng số FAIL không đổi |
| Không bỏ sót | File có `<span>Xin chào</span>` ngoài comment | Bị bắt, đúng số dòng |

**Một lỗi đã dính và đã sửa trong chính lượt thêm cổng này.** Bản đầu xoá trắng comment
bằng `perl -0777 -pe 's/<!--.*?-->//gs'`. Luồng ngắn lại, nên `grep -n` đếm trên luồng đó
và **số dòng báo ra lệch so với file thật** — `app.html` bị báo `:5` trong khi dòng thật là
`:12`. Bản sửa thay comment bằng đúng số ký tự xuống dòng nó chiếm, giữ nguyên cấu trúc dòng.

Đáng ghi lại vì đây đúng loại lỗi mà `check-docs.sh` mục 6 sinh ra để bắt ở tài liệu —
trích dẫn trỏ sai dòng. Một cổng sai số dòng thì người sửa mở nhầm chỗ, không thấy gì, rồi
kết luận cổng báo bậy và bỏ qua nó.

Lệnh liệt kê chỗ còn vi phạm (dùng khi cổng đỏ):

```bash
bash scripts/fe-gate.sh 2>&1 | sed -n '/G12/,/^$/p'
```


## G7 — ngưỡng ngân sách, chốt 2026-09-05 (quyết định người dùng)

Trước ngày này `angular.json` vẫn giữ **nguyên mặc định của `ng new`** (initial 500 kB /
1 MB), trong khi chính dòng G7 ở bảng trên yêu cầu *"chỉnh ngưỡng theo số đo thật, không
giữ mặc định của `ng new`"*. Doc nói một đằng, cấu hình để một nẻo — suốt thời gian đó
"ngưỡng 500 kB" không phải một quyết định, nó chỉ là con số chưa ai đụng tới.

Số đo hai chiều (đo thật 2026-09-05, không ước lượng):

| Cấu hình | Initial bundle |
|---|---|
| Gỡ khối i18n khỏi `app.config.ts` | **497.60 kB** |
| Có i18n (hiện trạng) | **~525 kB** |

Chênh ~28 kB là `@ngx-translate/core` + `primelocale`. Con số 497.60 kB cho thấy điều
quan trọng hơn: **trước khi có i18n, dư địa chỉ còn 2.4 kB.** Ngưỡng 500 kB đã hết tác
dụng cảnh báo từ trước — gần như bất cứ thứ gì thêm vào tầng eager cũng làm nó vàng.

**Chốt: `maximumWarning` → `600kb`, `maximumError` giữ `1mb`.**

Vì sao 600 kB chứ không phải "vừa đủ qua": ngưỡng cảnh báo đặt sát số đo hiện tại thì lần
thêm thư viện kế tiếp lại vàng, và người ta lại nâng — ngưỡng thành thứ chạy theo bundle
thay vì ràng buộc nó. 600 kB cho ~75 kB dư địa, đủ cho vài lượt phát triển, và vẫn còn xa
`maximumError` 1 MB — mốc thật sự chặn.

**`anyComponentStyle` → `6kb` / giữ error `8kb` (chốt 2026-09-06, quyết định người dùng).**

Đo trước khi đổi, không đổi rồi mới biện minh:

| File | Kích thước build |
|---|---|
| `sidebar.scss` | **4.55 kB** — vượt ngưỡng cũ 554 byte |
| File lớn thứ hai (`app.scss`) | **3.4 kB** |

`sidebar.scss` **không phình**: 324 dòng, 46 selector, 4 media query, gánh một nav rail hai chế
độ (đầy đủ / thu gọn), menu lồng nhau, 3 điểm ngắt, drawer + backdrop cho mobile. Khối lớn nhất
là `.sidebar-navitem` (59 dòng — 5 trạng thái, cấp con thụt lề, chevron xoay). 4.55 kB cho ngần
ấy là bình thường; thứ bất thường là **ngưỡng 4 kB mặc định của `ng new`, chưa ai đo**.

Vì sao `6kb` chứ không `5kb` vừa đủ qua: ngưỡng đặt sát số đo hiện tại thì lần thêm trạng thái
kế tiếp lại vàng, rồi lại nâng — ngưỡng thành thứ chạy theo file thay vì ràng buộc nó. 6 kB cho
~1.4 kB dư địa, mà file lớn thứ hai mới 3.4 kB nên nó vẫn còn tác dụng cảnh báo thật.

**Vì sao KHÔNG xé `sidebar.scss` ra hai file** (đẩy phần responsive sang `styles.scss`): làm thế
đổi một cảnh báo lấy một component bị chia đôi giữa hai file, và người sửa nav rail phải nhớ mở
cả hai. Cái giá đó đắt hơn con số ngưỡng.

⚠️ **Angular không hỗ trợ ngưỡng riêng cho từng file** — `anyComponentStyle` là một con số áp cho
mọi component style. Nên đây là lựa chọn nhị phân: hoặc nới cho tất cả, hoặc sửa file vi phạm.
Không có đường thứ ba, đừng đi tìm.

**Lỗ mù còn nguyên, ghi ra để không ai tưởng G7 phủ hết:** `angular.json` chỉ khai budget
cho `initial` và `anyComponentStyle` — **không có luật nào cho lazy chunk**. Chunk
`quan-tri-nguoi-dung-page` nặng ~574 kB raw, tức lớn hơn cả initial bundle, mà đi qua cổng
im lặng. Nguyên nhân đã truy ra: một component trong nhánh này import `TableModule` của PrimeNG, và
đó là nơi **duy nhất** trong app dùng nó.

🔄 LẬT 2026-09-06 — **chỗ import đã DI CHUYỂN, lỗ mù thì không.** Bản trước chỉ đích danh
`user-grid-table.ts`; nay `TableModule` chỉ còn được import ở
`src/FE/src/app/shared/components/data-grid/data-grid.ts:4`, và `DataGrid` có đúng một nơi
dùng — `user-grid-table.ts:4`. Nghĩa là PrimeNG Table vẫn rơi vào cùng lazy chunk đó; đổi
chỗ khai không đổi hình dạng bundle. Chưa xử lý, chưa chốt. Đếm lại bằng lệnh thay vì tin
con số ở đây:

```bash
grep -rn "primeng/table" src/FE/src --include=*.ts | grep -v spec
```


## Vị trí chạy — 🛑 CHẠY TAY, repo KHÔNG có CI

**Không còn `.github/`** (người dùng xoá 2026-08-21, có chủ đích). Không có
máy nào tự chạy gate — **người chạy tay trước khi commit**:

> ### ✅ CÓ THẬT — `scripts/fe-gate.sh` đã được viết (2026-08-28), chạy được (đối chiếu 2026-08-29)
>
> ```bash
> ls scripts/fe-gate.sh      # PASS khi file có thật
> bash scripts/fe-gate.sh    # PASS khi thoát 0 — đã chạy 2026-08-29, exit 0
> ```
>
> Đừng chép kết quả vào đây (§6 CLAUDE.md) — chạy lại hai lệnh trên là biết.
>
> Trước 2026-08-28 file này **không tồn tại** suốt một thời gian dài trong khi tài
> liệu vẫn hướng dẫn chạy nó (phát hiện 2026-08-23). Cách nó hỏng mới là điều đáng
> nhớ: lệnh đầu báo `No such file or directory`, ba lệnh sau chạy bình thường —
> và người chạy tưởng gate đã xanh. **G1/G3/G6 khi đó không có gì canh.**
>
> Script chạy được từ bất kỳ thư mục nào (tự resolve gốc repo). Mẫu G6 dùng
> `Dto\b` **không phải** `\bDto\b` và loại trừ `*.spec.ts` — hai điều đã kiểm
> bằng canary, xem ghi chú cuối mục này.
>
> **G11 (2026-09-03) sinh ra vì G1 mù đúng một nửa bài toán.** G1 chỉ quét
> `#rrggbb`, nên cùng MỘT quyết định màu viết bằng thập phân thì đi lọt:
> `rgba(15, 91, 215, .08)` chính là `--brand` `#0f5bd7`, và nó là nền của **mục
> menu đang chọn** — thấy ở mọi màn hình, lại nằm ngay trên một dòng đã dùng
> `var(--brand)`, tức là SÓT chứ không phải chủ đích.
>
> Miễn trừ của G11 là theo **cú pháp** (`rgb(var(--x) / a)`), tuyệt đối không
> theo **giá trị**: `rgba(0,0,0,.5)` trông vô hại nhưng vẫn là một quyết định
> thiết kế, mà quyết định thì thuộc
> `doc/Design/Frontend/PlatformManager/Tokens/`. Miễn trừ theo giá trị là mở lại
> đúng cánh cửa gate này sinh ra để đóng.
>
> Canary đã chạy khi thêm gate: một dòng chỉ chứa dạng được phép thì im lặng;
> một dòng chứa CẢ dạng được phép LẪN một literal trần thì vẫn đỏ — đó là lý do
> G11 gỡ dạng được phép ra khỏi dòng trước rồi mới hỏi lại, thay vì `grep -v`
> một phát (làm thế sẽ tha nhầm cả dòng).

```bash
cd src/FE
bash ../../scripts/fe-gate.sh                                    # G1 + G3 + G4 + G6 + G11 + G12
npx ng lint                                                      # G2 + G8 + G9
npx ng test --watch=false --browsers=ChromeHeadless               # test
npx ng build                                                     # G7 (budget)
```

`CHROME_BIN` phải trỏ tới Chrome nếu shell chưa export sẵn — thiếu nó Karma
hỏng với *"No binary for ChromeHeadless"*.

> ### ⚠️ Đây là điểm yếu đã biết, không phải thiếu sót chưa ai thấy
>
> Toàn bộ file này tồn tại vì một bài học: **G1 từng được dọn tay 2 lần và tự
> tái sinh cả 2 lần** — hex mới xuất hiện ngay ở đợt màn hình kế tiếp, đúng vì
> không có máy kiểm. `scripts/fe-gate.sh` sinh ra để chấm dứt việc đó.
>
> Nay không còn CI, gate quay lại phụ thuộc **trí nhớ con người** — tức đúng
> điều kiện đã sinh ra vấn đề ban đầu. Script đã có từ 2026-08-28 và chạy được
> (xem §Vị trí chạy), nhưng **có script không đồng nghĩa với có gate**: không máy
> nào gọi nó hộ. Đừng nhầm "có tài liệu về gate" với "có gate", cũng đừng nhầm
> "có script" với "gate đã chạy". Dựng lại CI thì nối các lệnh trên vào, thứ tự
> gate → lint → test → build để fail nhanh nhất.
>
> Hai điểm đã kiểm chứng bằng canary khi viết script, ghi lại để không ai
> "đơn giản hoá" ngược lại:
>
> - Mẫu G6 phải là `Dto\b`, **không phải** `\bDto\b`. Tên DTO thật luôn dạng
>   `IUserDto` — giữa `r` và `D` không có word boundary nên `\bDto\b` không bao
>   giờ khớp, gate xanh vì mù chứ không vì sạch.
> - G6 loại trừ `*.spec.ts`. Đây là **phạm vi đúng** của rule chứ không phải
>   ngoại lệ: G6 bảo vệ đường code chạy thật, còn spec stub tầng HTTP thì bắt
>   buộc phải dựng payload đúng hình dạng wire, tức phải nói bằng DTO.
>
> **Trạng thái hai gate từng bị bỏ ngỏ — nói đủ cả hai, đừng chỉ nói một.**
>
> - **G4 đã hiện thực 2026-09-08** (section `G4` trong `scripts/fe-gate.sh`) — xem §G4 cho
>   thiết kế và bốn canary.
> - **G5 vẫn chưa hiện thực.**
>
> Vì sao phải viết cả dòng đầu chứ không lặng lẽ bật G4: từ lúc F4 xong tới 2026-09-08, cột
> "Khi nào bật" của G4 ghi *"Sau F4"* — tức đã **quá hạn** — mà chỗ này lại chỉ nêu đích danh
> G5. Người đọc mục cảnh báo sẽ kết luận G4 đang chạy, trong khi `scripts/fe-gate.sh` lẫn
> `eslint.config.js` đều không có gì cho nó. Một gate quá hạn mà không ai nói ra thì không
> phân biệt được với một gate đang canh; đó là lý do danh sách này liệt **cả** cái đã bật lẫn
> cái chưa, thay vì chỉ nhắc phần còn thiếu.

## Không làm

- Không viết gate cho tính năng chưa tồn tại.

  🔄 LẬT 2026-09-06: ví dụ cũ ở đây là *"i18n runtime-switch chưa quyết định dùng — không
  viết gate cho nó"*. Ví dụ đó nay **phản chứng chính nó**: i18n runtime đã về
  (`src/FE/src/app/core/i18n/language.service.ts`, `src/FE/public/i18n/vi.json`), và đúng lúc
  đó G12 mới được viết. Luật vẫn giữ nguyên — chỉ là thứ tự đúng của nó là *tính năng trước,
  gate sau*, không phải *không bao giờ có gate*.
- Không nới gate cho 1 module cụ thể bằng cách sửa gate lỏng đi — nếu 1
  module cần ngoại lệ tạm thời, loại trừ tường minh bằng comment + đường
  dẫn cụ thể trong script, không sửa rule chung.
