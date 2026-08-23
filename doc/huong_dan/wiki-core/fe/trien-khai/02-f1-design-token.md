# F1 — Design token → code

> **Định nghĩa hoàn thành:** `styles.scss` `:root` chứa đủ bộ token lấy từ
> `doc/Design/Frontend/PlatformManager/DESIGN.md`; `grep -rn "#[0-9a-fA-F]\{3,6\}"
> src/FE/src/app --include=*.scss` **không còn kết quả nào** (hex chỉ tồn tại ở
> `:root` của `styles.scss`); PrimeNG render đúng màu token trên 1 `p-button`
> + 1 `p-table` mẫu; font khai trong `DESIGN.md` **thật sự được nạp**.

## ⚠️ Chiều đồng bộ ở giai đoạn này là NGƯỢC với quy tắc thường ngày

[`../04-design-token-system.md`](../04-design-token-system.md) nêu quy tắc
**"code là nguồn sự thật, tài liệu mirror theo"** — đúng cho vận hành lâu dài,
nhưng **chưa áp dụng được ở F1**, vì lúc này chưa có code nào để làm nguồn.

| | Nguồn | Đích |
| --- | --- | --- |
| **Ở F1 (một lần)** | `doc/Design/…/DESIGN.md` | `styles.scss` |
| **Sau F1 (mãi mãi)** | `styles.scss` | `Tokens/*.md` qua `/design-extract-tokens` |

**Không lấy giá trị từ `src/FE` cũ.** Đây không phải chi tiết thủ tục — bản
`styles.scss` cũ mang **nợ tiếp cận đã biết**: cặp màu cảnh báo và lỗi đo được
`3.88:1` và `3.89:1`, dưới ngưỡng WCAG AA `4.5:1`. `DESIGN.md` **đã sửa** từ
2026-08-22 (`warn: #965e08`, `bad: #a02b2b`, xem `DESIGN.md:32` và `:34`) nhưng
giá trị mới chưa bao giờ vào code. Lấy từ `DESIGN.md` là món nợ này tự biến
mất; lấy từ `styles.scss` cũ là chép nguyên nó sang app mới.

## 4 việc

### 1. Đổ token vào `:root`

Nguồn là frontmatter của `DESIGN.md` — nơi đã có sẵn tên token cho cả những
màu mà bản cũ từng hardcode (`surface-alt`, `surface-track`, `border-input`,
`surface-badge-success`, `surface-badge-danger`…). Dùng đúng tên đã có, không
đặt tên mới song song cho cùng một màu.

### 2. Nạp font thật

`DESIGN.md` khai `Inter, 'Segoe UI', Arial, sans-serif` cho toàn bộ thang chữ.
Bản cũ khai font nhưng **chưa bao giờ nạp Inter** — trình duyệt lặng lẽ rơi về
`Segoe UI`, và mọi ảnh chụp màn hình đối chiếu đều sai một cách khó thấy. Nạp
Inter (self-host hoặc `@font-face`) rồi kiểm bằng DevTools → Computed →
`font-family` phải **thật sự** phân giải ra Inter.

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

Tạo `PlatformManagerPreset` map token vào hệ theming PrimeNG rồi đăng ký qua
`providePrimeNG()` — **không** để theme `Aura` gốc chạy song song với token
riêng, vì khi đó hai hệ màu cùng tồn tại và không ai biết chỗ nào thắng.

> 📖 Cách map và code mẫu preset: [`../04-design-token-system.md`](../04-design-token-system.md)
> §Theme PrimeNG khớp token hiện có.

## Kiểm chứng

- [ ] `grep -rn "#[0-9a-fA-F]\{3,6\}" src/FE/src/app --include=*.scss` → 0 kết quả
- [ ] Giá trị `--warn`/`--bad` trong `:root` khớp `DESIGN.md:32`/`:34`, không
      phải giá trị cũ dưới ngưỡng WCAG
- [ ] DevTools → Computed → `font-family` phân giải ra **Inter** thật
- [ ] Tab bằng bàn phím qua màn đăng nhập, thấy rõ `:focus-visible` ở mọi control
- [ ] 1 `p-button` + 1 `p-table` render đúng `--card`/`--brand`/`--line`
- [ ] Không token nào được thêm mà chưa đối chiếu tên với frontmatter `DESIGN.md`
