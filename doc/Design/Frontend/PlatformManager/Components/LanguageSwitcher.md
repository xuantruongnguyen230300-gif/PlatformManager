---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "shipped"
updated: "2026-09-11"
component: "LanguageSwitcher"
sources:
  - "src/FE/src/app/shared/components/language-switcher/language-switcher.html"
  - "src/FE/src/app/shared/components/language-switcher/language-switcher.ts"
  - "src/FE/src/app/shared/components/language-switcher/language-switcher.scss"
  - "src/FE/src/app/core/i18n/language.service.ts"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
---

# LanguageSwitcher

**Description:** `<app-language-switcher>` — a row of buttons, one per language declared in
`CORE_I18N`, that switches the app language at runtime. Shipped 2026-09-06 alongside the i18n
layer.

> **Vì sao spec này ra đời muộn hơn component.** Component vào code trong đợt i18n nhưng
> `COMPONENTS.md` không có dòng nào cho nó — tức một thành phần giao diện đã ship mà **nguồn
> tham chiếu giao diện duy nhất** (`.claude/CLAUDE.md` §7) không biết nó tồn tại. Phát hiện
> 2026-09-06 bằng phép so thư mục `shared/components/` với mục lục; cùng lượt đó lộ ra
> `data-grid` cũng thiếu. Phép so đó nên chạy lại mỗi khi thêm component — xem § Nghiệm thu.
>
> 🔄 **SỬA 2026-09-08 — lỗ hổng `data-grid` đã đóng, câu trên đọc như thể còn mở.**
> `data-grid` **có** spec: `Components/DataTable.md` khai đúng ba file
> `src/FE/src/app/shared/components/data-grid/data-grid.{html,ts,scss}` ở frontmatter
> `sources:`, và `COMPONENTS.md` có hàng `DataGrid` trỏ vào đó — người dùng xác nhận lại
> 2026-09-08 rằng `DataTable.md` **chính là** spec của `data-grid`, nên đính chính này vẫn
> đứng. `DataTable.md` mang `updated: "2026-09-06"`, tức lỗ hổng được vá **cùng ngày** nó lộ
> ra, chỉ là câu trên không được sửa theo. Kiểm bằng lệnh ở § Nghiệm thu chứ đừng tin câu nào
> ở đây.
>
> Một nửa của đính chính này thì **sai và đã gỡ 2026-09-08**: nó viết *"cả hai mang
> `updated: "2026-09-06"`"*, trong khi `COMPONENTS.md` mang `updated: "2026-09-05"` ở thời
> điểm đó. Câu sửa một lỗi mà tự mang thêm một khẳng định chưa kiểm là đúng khuôn
> `.claude/CLAUDE.md` §4 cấm.

> **`scope: core` → `scope: du-an`, đổi 2026-09-08 (quyết định người dùng).** Khoá `scope` đo
> **CHỦ THỂ của tài liệu**, không phải quyền sở hữu code: file này tả một component của
> **PlatformManager**, nên nó là `du-an` kể cả khi component nằm dưới `shared/` và sẽ theo
> CoreBase sang sản phẩm khác — đó là hai câu hỏi khác nhau. Luật ở
> [`doc/Design/CLAUDE.md`](../../../CLAUDE.md) § Per-project folder convention. Sau lần đổi
> này mọi spec trong `Components/` cùng mang một giá trị; đếm lại bằng
> `grep -h '^scope:' doc/Design/Frontend/PlatformManager/Components/*.md | sort -u`.

## Anatomy

`<div class="lang-switch" role="group">` → một `<button class="lang-switch__option">` cho mỗi
ngôn ngữ.

| Phần | Thuộc tính | Vì sao |
|---|---|---|
| Khung ngoài | `role="group"` + `[attr.aria-label]` lấy từ khoá `shared.languageSwitcher.label` | Hai nút rời rạc không tự nói lên chúng là **một** lựa chọn. Nhãn đi qua bảng dịch nên nó đổi theo ngôn ngữ đang chọn, giống mọi chuỗi khác |
| Nút | `[attr.lang]="language.code"` | **WCAG 3.1.2 Language of Parts.** Chữ "English" nằm giữa một trang `lang="vi"` mà không đánh dấu thì trình đọc màn hình phát âm nó bằng bộ âm tiếng Việt |
| Nút đang chọn | `[attr.aria-current]="'true'"` **và** class `.is-active` | Trạng thái "đang chọn" phải **nghe được**, không chỉ nhìn được. Đổi mỗi màu là bỏ rơi người dùng trình đọc màn hình |

## Copy

Nhãn ngôn ngữ **KHÔNG dịch** — chúng đến từ `ICoreLanguage.label` khai ở `app.config.ts`
(`Tiếng Việt`, `English`), mỗi tên viết bằng chính ngôn ngữ đó.

Đây là ngoại lệ có chủ đích, không phải chuỗi bị bỏ sót: người đang xem bản tiếng Anh vẫn phải
đọc được chữ "Tiếng Việt" để biết bấm vào đâu. Dịch nó là phá đúng công dụng của nút.
Lý do đầy đủ: `doc/huong_dan/wiki-core/fe/08-i18n.md` § "Cái gì KHÔNG dịch".

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Nhãn nhóm (chỉ trình đọc màn hình) | `Chọn ngôn ngữ` / `Choose language` | `shared.languageSwitcher.label` | `language-switcher.html:4` |
| Nhãn nút | `Tiếng Việt`, `English` | — (**cố ý** không dịch, xem trên) | `app.config.ts` § `APP_I18N.languages[].label` |

## States

| Trạng thái | Biểu hiện |
|---|---|
| idle | Mọi nút bình thường |
| selected | Nút hiện hành mang `.is-active` + `aria-current="true"` |
| hover / focus | Theo lớp nút dùng chung ở `styles.scss` |
| bấm lại chính ngôn ngữ đang dùng | **Không làm gì** — `select()` thoát sớm (`language-switcher.ts`), không nạp lại bảng dịch |
| nạp bảng dịch hỏng | Lỗi đi vào `ErrorHandler` toàn cục, **không** nuốt lặng. Giao diện giữ nguyên ngôn ngữ cũ |

## Placement

Hôm nay có **đúng một** chỗ dùng: màn đăng nhập (`login.page.html:99`, kèm class
`login-language` để định vị).

### 📐 Nợ đã biết — người đã đăng nhập KHÔNG đổi được ngôn ngữ

Vào rồi thì không có nút nào. Topbar hiện chỉ có: hamburger · logo · tên người dùng · Đổi mật khẩu · Đăng xuất
(hành động tài khoản thứ hai thêm 2026-09-11 — xem `Topbar.md` § Anatomy).

**Chốt 2026-09-08 (quyết định người dùng): hoãn, và khi làm thì đặt vào MENU HỒ SƠ** (dropdown mở
từ tên người dùng / avatar trên topbar) — **không** đặt trực tiếp lên topbar, **không** dựng một
trang Settings riêng chỉ để chứa nó.

Ba lý do đã cân, ghi lại để lượt sau khỏi bàn lại:

| | |
|---|---|
| Ngôn ngữ là **tuỳ chọn cá nhân** | Cùng nhóm với ảnh đại diện, đổi mật khẩu. Đây cũng là chỗ Google Workspace, GitHub, Atlassian, Notion đều đặt |
| Đổi **hiếm** nhưng phải **tìm được ngay** | Để thường trực trên topbar là chiếm chỗ vĩnh viễn cho thao tác một lần; chôn trong một trang Settings đầy đủ là thêm một cú nhấp |
| Chưa có trang Settings | Dựng cả một trang chỉ để chứa một nút là ngược thứ tự |

**Vì sao hoãn thay vì đặt tạm lên topbar:** một chỗ tạm sẽ phải dọn khi menu hồ sơ ra đời, và
trong lúc đó nó dạy người dùng một vị trí sắp thay đổi.

⚠️ **Ràng buộc phải giải quyết CÙNG LÚC, không phải sau:** lựa chọn hiện lưu ở `localStorage`
khoá `core.language.v1`, tức **theo trình duyệt**, không theo tài khoản. Người dùng đổi sang
English rồi đăng nhập ở máy khác sẽ thấy tiếng Việt. Đưa nút vào menu hồ sơ mà vẫn lưu theo trình
duyệt là tạo ra một mâu thuẫn hiển nhiên: mọi thứ khác trong menu đó thuộc về **tài khoản**. Muốn
theo tài khoản thì cần thêm một trường vào `AppUser` — việc của BE, chốt cùng lượt.

## Nghiệm thu

Component đã ship phải có spec. Lệnh so thư mục với mục lục:

```bash
for d in src/FE/src/app/shared/components/*/; do
  n=$(basename "$d")
  p=$(echo "$n" | sed -r 's/(^|-)([a-z])/\U\2/g')
  grep -qi "$p\|$n" doc/Design/Frontend/PlatformManager/COMPONENTS.md || echo "THIẾU SPEC: $n"
done
```

PASS: không in ra gì. Lệnh này **luôn in một thứ gì đó khi hỏng** và im lặng khi đúng — chạy
nó sau mỗi lần thêm component vào `shared/components/`.
