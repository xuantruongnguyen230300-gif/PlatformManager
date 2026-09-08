---
kind: luat
scope: core
verified: 2026-09-06
---

# F1 — Design token → code

> **Định nghĩa hoàn thành:** `styles.scss` `:root` chứa đủ bộ token lấy từ
> `doc/Design/Frontend/PlatformManager/DESIGN.md`; `grep -rn "#[0-9a-fA-F]\{3,6\}"
> src/FE/src/app --include=*.scss` **không còn kết quả nào** (hex chỉ tồn tại ở
> `:root` của `styles.scss`); PrimeNG render đúng màu token trên 1 `p-button`
> + 1 `p-table` mẫu; font khai trong `DESIGN.md` **thật sự được nạp**.

## Chiều đồng bộ ở F1: `doc/Design/` → code — giống hệt quy tắc thường ngày

[`../04-design-token-system.md`](../04-design-token-system.md) §Chiều chốt
**`doc/Design/` là nguồn, code đuổi theo** (chốt 2026-08-27, xác nhận lại
2026-08-29). F1 đi **đúng** chiều đó — không phải ngoại lệ, không có chiều riêng
cho giai đoạn này.

| | Nguồn | Đích |
| --- | --- | --- |
| **Ở F1 và sau F1 — một chiều duy nhất** | `doc/Design/…/DESIGN.md` + `Tokens/*` + `tokens.json` | `styles.scss` **và** `APP_PALETTE` trong `app.config.ts` |

Đổi bảng màu phải chạm **cả hai** file phía code: bỏ hằng số `APP_PALETTE` thì
CSS và PrimeNG render hai màu khác nhau mà không có gì báo lỗi. (Trước 2026-09-02
nơi thứ hai là `core/theme/platform-manager-preset.ts`; 10 hằng số màu đã chuyển
ra `app.config.ts` vì chúng là dữ liệu của dự án, không phải của Core.)

`/design-extract-tokens` vẫn dùng, nhưng đúng vai: nó **ghi nhận hiện trạng**
code khi cần đồng bộ lại một lần (ví dụ ngay sau khi `styles.scss` vừa được viết
lại), **không** phải chiều làm việc thường trực và không quyết định giá trị mới.

> Sửa 2026-08-29: bản trước của mục này ghi ngược — bảng ghi *"Sau F1 (mãi mãi):
> nguồn `styles.scss` → đích `Tokens/*.md` qua `/design-extract-tokens`"*, và gán
> cho `04-design-token-system.md` câu *"code là nguồn sự thật, tài liệu mirror
> theo"*. File đó **không còn nói vậy từ 2026-08-27**.

**Không lấy giá trị từ `src/FE` cũ.** Đây không phải chi tiết thủ tục — bản
`styles.scss` cũ mang **nợ tiếp cận đã biết**: cặp màu cảnh báo và lỗi đo được
`3.88:1` và `3.89:1`, dưới ngưỡng WCAG AA `4.5:1`. `DESIGN.md` đã chốt cặp giá
trị đạt AA từ 2026-08-22 (`warn: #965e08`, `bad: #a02b2b` — frontmatter `DESIGN.md`, khoá `warn:`
và `bad:`). Lấy từ `DESIGN.md` là món nợ này tự biến mất; lấy từ `styles.scss` cũ là
chép nguyên nó sang app mới. Đối chiếu **2026-09-06**: code hiện tại mang đúng hai
giá trị đó — `src/FE/src/styles.scss` § `--warn`, `--bad` và
`src/FE/src/app/app.config.ts:67`, `:68`.

> 🔄 **LẬT 2026-09-06** — ba trích dẫn trong đoạn trên đã trượt. `DESIGN.md:39` trỏ vào khoá
> `th-ink` và `:41` trỏ vào `good-bg`, không phải `warn`/`bad`; `app.config.ts:63`/`:64` trỏ vào
> khối chú thích phía trên `APP_PALETTE`. Cả năm số vẫn nằm trong file nên gate `check-docs.sh`
> mục 6 không ĐỎ. Neo vào `DESIGN.md` nay bằng **tên khoá** thay vì số dòng: file đó thuộc khu
> `doc/Design/` và đổi thường xuyên, nên số dòng sẽ trượt lại ngay lượt sửa kế tiếp.

## 4 việc

### 1. Đổ token vào `:root`

Nguồn là frontmatter của `DESIGN.md` — nơi đã có sẵn tên token cho cả những
màu mà bản cũ từng hardcode (`surface-alt`, `surface-track`, `border-input`,
`surface-badge-success`, `surface-badge-danger`…). Dùng đúng tên đã có, không
đặt tên mới song song cho cùng một màu.

### 2. Nạp font thật

`DESIGN.md` khai `'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif` cho toàn bộ thang chữ.
Font phải được **tự phục vụ** bằng `@font-face` khai trong `styles.scss`, trỏ `url()` tuyệt đối
tới `/fonts/…` (thư mục `src/FE/public/fonts/`) — **không** dùng `<link>` trong `index.html`:
Angular xử lý `index.html` lúc build và phân giải `href` tương đối theo đường dẫn build, nên bản
build ra sẽ im lặng chạy bằng font hệ điều hành. Kiểm bằng DevTools → Computed → `font-family`
phải **thật sự** phân giải ra Be Vietnam Pro.

> 🔄 **LẬT 2026-09-06.** Mục này trước đây nói font là **Inter** và dặn *"nạp Inter"*. Sai ở cả
> hai đầu: `DESIGN.md` khai `'Be Vietnam Pro'` (frontmatter, mọi khoá `fontFamily`), và
> `src/FE/src/styles.scss` § `@font-face` khai `'Be Vietnam Pro'` với 5 độ đậm × 3 bộ ký tự
> (Latin / Latin-Ext / **Việt**) — bộ ký tự Việt là lý do đổi font, `Inter` không có. Bài học
> nguyên văn về `<link>` vs `@font-face` được giữ lại ở trên vì nó vẫn đúng; chỉ tên font là sai.
> Chú thích tại chỗ: khối mở đầu nhóm `@font-face` trong `src/FE/src/styles.scss` — tìm bằng
> `grep -n "Be Vietnam Pro" src/FE/src/styles.scss`. Neo bằng định danh chứ không bằng số dòng,
> theo khuôn `doc/Design/CLAUDE.md` §"Neo trích dẫn vào `styles.scss`".

### 3. Đủ 5 trạng thái cho component dùng chung

Mỗi component trong `shared/` phải có `:hover` / `:focus-visible` / `:disabled`
được định nghĩa thật, giá trị lấy từ token đã có — không phát minh màu mới.
Đặc tả từng component ở `doc/Design/Frontend/PlatformManager/Components/`.

`:focus-visible` là bắt buộc, không phải tuỳ chọn: bỏ nó nghĩa là người dùng
bàn phím không thấy mình đang ở đâu.

### 4. Preset PrimeNG map token

```bash
npm install primeng @primeng/themes chart.js
```

Tạo preset map token vào hệ theming PrimeNG (`createCorePreset(APP_PALETTE)`) rồi đăng ký qua
`providePrimeNG()` — **không** để theme `Aura` gốc chạy song song với token
riêng, vì khi đó hai hệ màu cùng tồn tại và không ai biết chỗ nào thắng.

> 📖 Cách map và code mẫu preset: [`../04-design-token-system.md`](../04-design-token-system.md)
> §Theme PrimeNG khớp token hiện có.

## Kiểm chứng

- [ ] `grep -rn "#[0-9a-fA-F]\{3,6\}" src/FE/src/app --include=*.scss` → 0 kết quả
- [ ] Giá trị `--warn`/`--bad` trong `:root` khớp khoá `warn:`/`bad:` ở frontmatter
      `DESIGN.md`, không phải giá trị cũ dưới ngưỡng WCAG
- [ ] DevTools → Computed → `font-family` phân giải ra **Be Vietnam Pro** thật
- [ ] Tab bằng bàn phím qua màn đăng nhập, thấy rõ `:focus-visible` ở mọi control
- [ ] 1 `p-button` + 1 `p-table` render đúng `--card`/`--brand`/`--line`
- [ ] Không token nào được thêm mà chưa đối chiếu tên với frontmatter `DESIGN.md`
