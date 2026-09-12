---
kind: luat
scope: core
verified: 2026-09-08
---

# Architecture — src/FE

> Xem trước **`doc/kien-truc-core-module.md`** (root repo) để hiểu lý do và
> nguồn tham khảo thực tế đằng sau ranh giới `platform/` ↔ `modules/` dưới
> đây — file này chỉ nêu quy tắc thực thi, không lặp lại phần lý luận.

## Cây thư mục cấp `src/FE/` — cái gì ở ngoài `src/app/`

Bốn chỗ ngoài `src/app/`, mỗi chỗ một vai. Đừng tạo thư mục thứ năm khi chưa
đọc bảng này:

```
src/FE/
├── public/            # tài sản tĩnh phục vụ nguyên trạng, copy thẳng vào bundle
│   │                  # (angular.json khai "input": "public"). Ra thẳng GỐC SITE, không
│   │                  # nằm sau apiBaseUrl — nên phải fetch bằng HttpBackend, xem
│   │                  # ../wiki-core/fe/02-http-envelope.md §"Tài nguyên tĩnh".
│   ├── favicon.ico
│   ├── fonts/         # woff2 tự host (không gọi Google Fonts lúc chạy)
│   ├── i18n/          # bảng dịch nhóm CoreBase — ĐI THEO khi tách nền tảng
│   └── i18n-app/      # bảng dịch nhóm DỰ ÁN — Ở LẠI. Hai thư mục ANH EM, có chủ đích:
│                      # ../wiki-core/fe/08-i18n.md §Khuôn CoreBase
├── src/
│   ├── environments/  # cấu hình COMPILE-TIME theo môi trường — apiBaseUrl, production
│   ├── styles.scss    # token global :root + style toàn cục
│   ├── index.html
│   ├── main.ts
│   └── app/           # xem mục dưới
└── angular.json / package.json / tsconfig*.json / eslint.config.js
```

**`public/` vs `environments/` — chọn theo thời điểm cần giá trị:**

| | `src/environments/*.ts` | `public/*.json` |
| --- | --- | --- |
| Giá trị chốt lúc | **build** | **runtime**, fetch lúc khởi động |
| Đổi giá trị cần | build lại | không, chỉ thay file |
| Đang dùng hôm nay | ✅ cơ chế **duy nhất** cấp *cấu hình* — `apiBaseUrl` cho interceptor | ⚠️ đã có file JSON nạp lúc chạy (`public/i18n/{vi,en}.json`) nhưng **chưa** file nào giữ *cấu hình* |

🔄 LẬT 2026-09-06 — ô bên phải trước đây ghi *"❌ chưa có file nào"*. Sai từ 2026-09-05: đợt
i18n runtime đã đặt `src/FE/public/i18n/vi.json` và `en.json`, và chúng được **fetch lúc chạy**
đúng như cột này mô tả. Điều còn đúng — và là điều mục này thật sự muốn nói — là **chưa có file
`public/` nào giữ *cấu hình*** (`apiBaseUrl`, feature flag). Ranh giới đó mới là thứ đừng phá.

Chỉ đổi sang cơ chế runtime khi chạm ngưỡng ghi ở
[`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md)
§"#17 — Runtime environment config" — **không** dựng cả hai song song, vì hai
nguồn cấu hình cho cùng một giá trị là cách chắc chắn nhất để chúng lệch nhau.

> **FE không có "dữ liệu runtime" theo nghĩa của BE.** Trình duyệt không ghi
> file lên đĩa server, nên mọi thứ trong `src/FE/` đều là **tài sản của source**
> và đều vào git. File người dùng upload đi thẳng lên API, không nằm lại ở FE —
> quy tắc lưu trữ phía server ở
> [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md).

## Tầng app

```
src/app/
├── core/       # singleton toàn app: auth, guard, interceptor, HTTP client dùng chung
├── shared/     # thứ dùng chung KHÔNG phải singleton hạ tầng — xem §Bên trong `shared/`
├── platform/   # màn hình "Core" (đăng nhập, đổi mật khẩu, quản trị người dùng, phân quyền)
│               # — dùng lại được cho mọi sản phẩm dựng trên nền tảng này, KHÔNG phải nghiệp vụ
└── modules/    # module NGHIỆP VỤ — lazy-loaded, mỗi module 1 domain.
```

> ✅ **CÓ THẬT (đối chiếu 2026-09-10) — `modules/` đã tồn tại, và cổng G8 đang CHẠY THẬT.**
> Hằng `BUSINESS_MODULES` ở `src/FE/eslint.config.js:29` đã có tên module, nên
> `import/no-restricted-paths` phân giải được `target` và chặn thật. Đọc từ đĩa thay vì tin
> câu này ([`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6):
>
> ```bash
> ls src/FE/src/app/modules
> grep -n 'BUSINESS_MODULES = ' src/FE/eslint.config.js
> ```
>
> **PASS = hai lệnh in ra CÙNG một tập tên.** Lệch nhau là G8 hỏng im lặng theo một trong hai
> chiều: tên thừa ⇒ zone trỏ vào thư mục không có thật (no-op cho chính module đó); tên thiếu
> ⇒ module đó không bị soi ranh giới. Cả hai đều để `ng lint` xanh.
>
> 🔄 **LẬT 2026-09-10.** Khối này trước đây khẳng định `modules/` *"📐 CHƯA TỒN TẠI hôm nay
> (đối chiếu 2026-09-06)"* và *"`BUSINESS_MODULES` để rỗng nên **gate G8 hiện là no-op**"*.
> Hai module nghiệp vụ đã dựng lại ngày 2026-09-09; cả hai câu sai kể từ đó. Đây là file
> `kind: luat`, nên sai ở đây đắt hơn bình thường: người dựng module tiếp theo đọc xong sẽ
> tưởng mình là người đầu tiên, bỏ qua bước 3 của §"Thêm một module nghiệp vụ mới" — và bước
> đó chính là bước bật G8.

`core/` và `shared/` là **cross-cutting** — không đặt logic riêng của một
feature vào đây. Nếu một service/component chỉ dùng bởi đúng 1 feature, nó
thuộc về `platform/<feature>/` hoặc `modules/<feature>/` (tuỳ có phải
nghiệp vụ hay không), không phải `core/`/`shared/`.

`platform/` và `modules/` dùng **chung 1 cấu trúc con** (xem mục dưới) —
khác biệt duy nhất là ý nghĩa: `platform/*` là màn hình nền tảng (áp dụng
cho mọi sản phẩm dựng trên core này), `modules/*` là màn hình đặc thù 1
domain nghiệp vụ. Thêm feature mới → tự hỏi "màn này có ý nghĩa với MỌI sản
phẩm dựng trên nền tảng, hay chỉ riêng domain nghiệp vụ hiện tại?" để chọn
đúng chỗ đặt, không đoán.

**Ranh giới bắt buộc (ESLint gate G8 — xem
`doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md`)**: 1 module nghiệp vụ
trong `modules/<A>/` không được import trực tiếp nội bộ
`modules/<B>/` (module nghiệp vụ khác) — chỉ được import từ `core/`,
`shared/`, `platform/`. Cần dùng chung logic giữa 2 module nghiệp vụ → đưa
lên `shared/`/`core/` nếu thật sự generic, không import chéo.

## Bên trong `shared/` — không chỉ có `components/`

Danh sách thật đọc từ đĩa, **không** đếm theo bản chép dưới đây
([`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6):

```bash
ls src/FE/src/app/shared
# PASS = mọi tên in ra đều có một dòng mô tả trách nhiệm trong khối bên dưới, và ngược lại
```

Khối dưới giữ **trách nhiệm** của từng thư mục — đó là phần không đếm được bằng lệnh, và là
lý do mục này tồn tại:

```
shared/
├── components/   # dumb UI component tái dùng > 1 feature
├── directives/   # directive dùng chung, cũng dumb
├── models/       # kiểu dùng chung giữa nhiều feature, VÀ hàm thuần đi liền kiểu đó
│                 #   (bảng tra, hàm phân loại, hàm định dạng — không state, không inject)
└── services/     # service TRẠNG THÁI UI  ·  service DỮ LIỆU dùng chung (có điều kiện, xem bảng dưới)
```

> 🔄 **SỬA 2026-09-10 — hai ô trên vừa được nới, và cả hai đều sai theo cùng một kiểu: mô tả hẹp
> hơn thứ đang nằm trong thư mục.**
>
> | Ô | Bản trước nói | Thứ thật sự nằm ở đó |
> | --- | --- | --- |
> | `models/` | *"kiểu dùng chung … (KHÔNG phải DTO)"* | `dti-criteria-status.model.ts` có một **bảng tra** và một **hàm** `criteriaStatusBadge()`, không chỉ có `type` |
> | `services/` | *"không phải hạ tầng và **không gọi HTTP**"* | `dti-period.service.ts` **gọi HTTP** — và đó là nơi đúng của nó, xem bảng ba ô bên dưới |
>
> Vế "KHÔNG phải DTO" của `models/` **giữ nguyên** — DTO thuộc về feature sở hữu endpoint. Điều
> được nới là: một hàm THUẦN đi liền với kiểu (`dti-period.format.ts` dựng mảnh nhãn kỳ) không có
> ô nào để đứng trước 2026-09-10, nên nó sẽ bị đẩy nhầm vào `services/` — chỗ dành cho thứ **có
> vòng đời**.

> 🔄 **SỬA 2026-09-08 (cùng ngày, lần thứ hai).** Tiêu đề mục này vừa được viết là *"ba thư
> mục, không phải một"* trong khi khối ngay dưới nó liệt **bốn** — một con số chép tay sai
> ngay từ dòng đầu tiên nó tồn tại. Đã thay bằng lệnh + tiêu chí PASS, đúng khuôn §6: số
> lượng là thứ đếm được bằng máy nên không được viết tay vào tài liệu.

> 🔄 **LẬT 2026-09-08.** Bản trước mô tả `shared/` **chỉ gồm component**
> (*"dumb UI component tái dùng > 1 feature (button, badge, card…)"*), và hệ quả là hai thư mục
> có thật không có ô nào trong tài liệu: `shared/directives/` và `shared/services/`. Cụ thể,
> `shared/directives/autofocus.directive.ts` xuất hiện **0 lần** trong toàn bộ `doc/` — người
> cần một directive dùng chung không có cách nào biết chỗ đặt, nên sẽ đặt vào `components/`
> hoặc dựng bản thứ hai trong feature của mình.

**Ranh giới `shared/services/` ↔ `core/`** — đây là chỗ dễ đặt nhầm nhất, và nhầm thì gãy gate
G9 (`core/` không được import ngược lên `shared/`):

| Câu hỏi | `core/` | `shared/services/` — trạng thái UI | `shared/services/` — DỮ LIỆU dùng chung |
|---|---|---|---|
| Có gọi HTTP / giữ phiên / cấu hình app không? | **Có** | Không bao giờ | **Có** — nhưng chỉ HTTP, không phiên, không cấu hình app |
| Có tồn tại khi không có giao diện nào không? | Có | Không — nó phục vụ đúng một nhu cầu UI | Không — nó nuôi một control của màn hình |
| Endpoint nó gọi thuộc về ai | nền tảng (auth, menu, CSRF…) | — | **một miền nghiệp vụ**, không phải nền tảng |
| Ví dụ đang chạy | `core/toast/toast.service.ts`, `core/auth/*` | `shared/services/sidebar-state.service.ts` (mở/thu gọn menu) | `shared/services/dti-period.service.ts` (`GET /api/dashboard/periods`) |

Phép thử: *"bỏ hết màn hình đi thì service này còn nghĩa gì không?"* Còn → `core/`. Không →
`shared/services/`, rồi chọn cột hai hay cột ba theo hàng "có gọi HTTP".

> ### ✅ CHỐT 2026-09-10 — cột thứ ba, và ĐIỀU KIỆN của nó
>
> Bảng này từng chỉ có **hai** cột, và ô `shared/services/` khai *"không gọi HTTP"*. Loại thứ ba
> vẫn tồn tại: một service **dữ liệu** mà **từ hai module nghiệp vụ trở lên** cùng cần. Nó không
> có chỗ đứng nào khác — gate **G8** (`src/FE/eslint.config.js`) cấm `modules/<A>` import nội bộ
> `modules/<B>`, và chính thông điệp của G8 chỉ sang `shared/`.
>
> 🛑 **Cột ba mở có ĐIỀU KIỆN, không mở toang.** Một service dữ liệu được đặt ở `shared/services/`
> **khi và chỉ khi** cả ba điều sau cùng đúng:
>
> 1. **≥ 2 module nghiệp vụ** đang thật sự dùng nó (không phải "sẽ dùng");
> 2. **G8 chặn mọi lối khác** — tức nó không đặt được trong module nào mà không phá ranh giới;
> 3. nó được **đăng ký vào danh sách "đi kèm dự án"** ở
>    [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) § "Chỗ mang, đo được" —
>    vì `shared/` đi theo CoreBase, nên mỗi file loại này là một món **nợ** phải trả khi tách sản
>    phẩm thứ hai, và nợ không đăng ký là nợ không ai trả.
>
> Thiếu bất kỳ điều nào ⇒ nó thuộc `modules/<feature>/services/`.
>
> **Vì sao bản hai cột là một lỗi thật, không phải chuyện chữ nghĩa:** cả hai câu hỏi của bảng cũ
> đều đẩy `DtiPeriodService` về `core/` — "có gọi HTTP: Có" ⇒ cột `core/`. Đáp án đó tệ hơn hẳn:
> nó nhét một endpoint **nghiệp vụ DTI** vào tầng đáy của CoreBase, đúng thứ
> [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) sinh ra để ngăn.
>
> Và nó đã sinh ra một mâu thuẫn đo được: `doc/contracts/dashboard.md` DB-3 §"Owner FE" viện dẫn
> **chính file này** làm căn cứ để đặt service ở `shared/`, trong khi file này nói ngược lại. Một
> hợp đồng trỏ sang một quy ước phủ định nó — đúng cơ chế `.claude/CLAUDE.md` §5 mô tả, và không
> gate nào bắt được vì cả hai đường dẫn đều tồn tại.

## Seam cấu hình cấp app — `core/` giữ CƠ CHẾ, app cấp DỮ LIỆU

`core/` và `shared/` là CoreBase, dùng lại cho sản phẩm thứ hai
([`../../kien-truc-core-module.md`](../../kien-truc-core-module.md)). Vì vậy **mọi dữ liệu
riêng của dự án này** — đường dẫn, tên sản phẩm, bảng màu, danh sách ngôn ngữ — phải đi vào
`core/` qua một **seam**, không được khai cứng bên trong nó.

Bảng dưới là **mục lục** các seam đang có; mỗi seam có một file chủ giữ chi tiết, không chép
lại ở đây (`.claude/CLAUDE.md` §5):

| Seam | Cấp gì cho `core/` | File chủ |
|---|---|---|
| `CORE_ROUTES` | 3 đường dẫn mà guard, interceptor **và app-shell** trỏ tới | [`fe-routing-guard.md`](fe-routing-guard.md) §10 |
| `CORE_BRANDING` | Tên sản phẩm (`name`) + chữ tắt (`shortName`) | **mục này** |
| `CORE_I18N` | Danh sách ngôn ngữ + ngôn ngữ mặc định | [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §Khuôn CoreBase |
| `createCorePreset(palette)` | Bảng màu cho ramp PrimeNG — **tham số hàm, không phải token** | [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) |

Nhật ký quyết định của cả bốn (vì sao tách, những gì đã sót, phép thử đã chạy) ở
[`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §"Core giữ CƠ CHẾ, dự án
cung cấp DỮ LIỆU" — đó là **lịch sử**, mục này là **luật đang áp**.

### `CORE_BRANDING` — tên sản phẩm

✅ **CÓ THẬT (đối chiếu 2026-09-08)** — `src/FE/src/app/core/config/core-branding.ts`, wire ở
`src/FE/src/app/app.config.ts` (`APP_BRANDING` + `provideCoreBranding`).

```ts
// core/config/core-branding.ts — hợp đồng, KHÔNG có giá trị mặc định
export interface ICoreBranding {
  readonly name: string;       // tên đầy đủ: hậu tố <title>, dòng chữ ở sidebar
  readonly shortName: string;  // chữ tắt trong ô vuông .brand-mark
}

export const CORE_BRANDING = new InjectionToken<ICoreBranding>('CORE_BRANDING');

export function provideCoreBranding(branding: ICoreBranding): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_BRANDING, useValue: branding }]);
}
```

Ba ràng buộc, mỗi cái chặn một lỗi im lặng:

1. **Token cố ý KHÔNG có `factory` mặc định**, cùng khuôn `CORE_ROUTES`. Một tên mặc định biến
   "app quên khai" thành một giao diện mang tên sản phẩm **khác** — sai ở chỗ dễ thấy nhất
   (tiêu đề tab, góc trên bên trái) mà không lỗi nào, không test nào bắt. Thiếu provider thì
   Angular ném `NG0201` ngay lần dựng đầu.
2. **`name` phải khớp `<title>` tĩnh trong `src/FE/src/index.html`.** Chuỗi đó là thứ hiện ra
   trong lúc app chưa bootstrap xong; lệch nhau thì tên tab nhấp nháy đổi khi trang tải xong.
3. **`shortName` giữ 2 ký tự.** Ô vuông `.brand-mark` rộng 26px ở sidebar; dài hơn sẽ tràn và
   không có gì báo.

**Vì sao tách khỏi `core/`/`shared/`:** trước 2026-09-02 tên sản phẩm khai cứng ở
`core/title/page-title.strategy.ts` và trong template của `shared/components/{sidebar,auth-card}/`,
nên dựng sản phẩm thứ hai là phải mở **ba file của nền tảng** ra sửa — đúng thứ mà định nghĩa
"CoreBase xong" loại trừ.

**Kiểm bằng lệnh** — `core/` và `shared/` không được biết tên sản phẩm này:

```bash
grep -rn "PlatformManager" src/FE/src/app/core src/FE/src/app/shared --include=*.ts --include=*.html   | grep -v spec | grep -vE "doc/|src/BE/"
```

PASS = không in ra dòng nào. (`*.spec.ts` loại trừ có chủ đích: test **cố ý** cấp một tên khác
tên thật — dùng đúng tên thật thì test vẫn xanh cả khi có người khai cứng lại chuỗi vào `core/`.)

### Khi cần seam thứ năm

Tự hỏi: *"giá trị này có đổi khi dựng sản phẩm khác trên cùng nền tảng không?"* Có → nó là **dữ
liệu của dự án**, phải đi qua seam. Không → để trong `core/`.

Khuôn bắt buộc, không phát minh khuôn thứ hai: `interface` + `InjectionToken` **không có mặc
định** + hàm `provideX()` trả `EnvironmentProviders`, tất cả trong `core/`; giá trị khai ở
`app.config.ts`. Ngoại lệ duy nhất là thứ phải dựng **trước khi injector tồn tại** (bảng màu, vì
`providePrimeNG` chạy lúc tạo object cấu hình) — cái đó là **tham số hàm**, và quên truyền là lỗi
biên dịch, sớm hơn cả `NG0201`.

## Cấu trúc một feature

Áp cho **cả** `platform/<feature>/` lẫn `modules/<feature>/` — cấu trúc con giống hệt nhau.

```
<platform|modules>/<feature>/
├── <feature>.routes.ts             # lazy routes riêng của feature
├── pages/<feature>/                # SMART — route target
├── components/<x>/                 # DUMB — chỉ input()/output()
├── services/<feature>.service.ts   # data access + mapper
├── models/<feature>.model.ts       # interface/type
└── state/ (tuỳ chọn)                # signal store khi state đủ phức tạp
```

Feature đầy đủ nhất đang chạy để soi khi phân vân:
`src/FE/src/app/platform/quan-tri-nguoi-dung/` — có đủ `routes.ts`, `pages/`, `components/`,
`services/` (service + mapper tách file), `models/`, và **không** có `state/` (đúng ngưỡng ở
mục dưới). 🔄 LẬT 2026-09-06: khối trên trước đây gắn nhãn `modules/<feature>/`, tức trỏ vào
tầng duy nhất **không có** ví dụ nào để mở ra xem.

## Thêm một module nghiệp vụ mới — thứ tự thao tác phía FE

✅ **CÓ THẬT (đối chiếu 2026-09-10) — hai module đã đi qua đúng các bước dưới.** Mỗi bước mô tả
cơ chế đang chạy và nêu đích danh file phải sửa; không bước nào là dự kiến.

> 🔄 **LẬT 2026-09-10.** Mục này viết 2026-09-08 với nhãn `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` và câu
> *"`src/FE/src/app/modules/` **chưa tồn tại**, nên chưa module nào đi qua các bước dưới"*. Hết
> đúng từ 2026-09-09. Nhãn 📐 trên một quy trình **đã có người đi qua** là dạng sai đắt: nó mời
> người đọc coi các bước là gợi ý chưa kiểm chứng, trong khi bước 3 là thứ duy nhất giữ cho G8
> không trở lại trạng thái no-op.

> Chọn tầng trước đã: câu hỏi *"màn này có ý nghĩa với MỌI sản phẩm dựng trên nền tảng, hay chỉ
> riêng domain nghiệp vụ hiện tại?"* ở đầu file quyết định `platform/` hay `modules/`. Các bước
> dưới đây dành cho vế **`modules/`**; màn `platform/` bỏ qua bước 3.

| # | Việc | Sửa file nào |
|---|---|---|
| 1 | Dựng cây feature theo §"Cấu trúc một feature" | `src/FE/src/app/modules/<ten>/…` (mới) |
| 2 | Đăng ký route lazy `loadChildren` | `src/FE/src/app/app.routes.ts` |
| 3 | **Bật cổng G8** — thêm `'<ten>'` vào `BUSINESS_MODULES` | `src/FE/eslint.config.js` |
| 4 | Bọc chuỗi hiển thị theo khuôn `<màn>.<nhóm>.<tên>`, `<màn>` = **đúng tên thư mục ở bước 1** | `src/FE/public/i18n-app/{vi,en}.json` — nhóm **DỰ ÁN**, không phải `i18n/` |
| 5 | Chạy đủ cổng | — |

**Không bước nào sửa `core/`.** Đó là phép thử của bốn seam ở §Seam cấu hình cấp app: nếu thêm
một module buộc phải mở `core/` ra sửa, seam đó thiếu — dừng lại và bổ sung seam theo khuôn ở
§"Khi cần seam thứ năm", đừng khai cứng vào `core/`.

**Bước 3 là bước dễ mất nhất, và mất thì không ai biết.** Quên nó thì `ng lint` vẫn xanh, chỉ là
**xanh vì không kiểm gì cả** — module mới không nằm trong zone nào nên ranh giới của nó không
được soi, đúng lúc luật cấm import chéo bắt đầu có ý nghĩa. Lý do đầy đủ ghi tại chỗ khai trong
`src/FE/eslint.config.js`, và ở
[`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) dòng G8.

Ràng buộc kèm theo: cổng chỉ có hiệu lực **thật** khi có từ **hai** module trở lên (một module
không có gì để import chéo), nhưng phải thêm tên **ngay từ module đầu tiên** — module thứ hai sẽ
do người khác thêm vào một ngày khác. Chiều ngược cũng hỏng im lặng: một tên **thừa** trong
`BUSINESS_MODULES` trỏ vào thư mục không có thật, và khi đó zone của chính module đó là no-op mà
`ng lint` vẫn xanh.

**Hai thứ KHÔNG làm ở FE:**

- **Mục menu.** Menu đến từ BE (`GET /api/meta/menu`, `core/menu/menu.service.ts`) và dữ liệu
  của nó do seeder bên BE cấp — mỗi module nghiệp vụ tự có seeder riêng, không khai qua seam
  menu của Core (`src/BE/Core/PlatformManager.Core.Application/Menu/ICoreMenuSeedSource.cs`).
  Đừng dựng một danh sách menu thứ hai trong FE.
- **Quyền truy cập.** Ma trận role × menu là dữ liệu, không phải code FE — xem
  [`../../contracts/permissions.md`](../../contracts/permissions.md).

**Bước 4, ràng buộc phải biết trước:** cơ chế nạp **nhiều nguồn** bảng dịch đã có sẵn
(`CORE_I18N.resources` là một mảng tiền tố, `app.config.ts`), và ✅ **nhóm khoá dự án đã có file
thật** (`src/FE/public/i18n-app/`, đối chiếu 2026-09-10). Khoá của module nghiệp vụ đi vào thư
mục đó, **không** vào `public/i18n/` — đặt nhầm thì chuỗi đi theo CoreBase sang sản phẩm khác
trong khi màn hình thì không. Chi tiết, và vì sao **không nhánh gốc nào được nằm ở cả hai file**,
ở [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §"Khoá nằm ở file nào".

> 🔄 **LẬT 2026-09-10.** Đoạn này trước đây ghi *"file dịch riêng cho nhóm khoá dự án thì 📐 chưa
> thi công — hôm nay `vi.json`/`en.json` chứa toàn khoá Core"*, và bảng bước 4 trỏ thẳng vào
> `public/i18n/`. Cả hai sai từ 2026-09-09, và cặp sai này dẫn thẳng tới hành động sai: người làm
> theo sẽ đặt khoá màn nghiệp vụ vào đúng thư mục của nền tảng.

## Bảng trách nhiệm — quy tắc cứng

| Tầng | Được phép | Cấm |
| --- | --- | --- |
| `pages/*` (smart) | inject store/service, bind signal, điều hướng | gọi `HttpClient` trực tiếp; logic nghiệp vụ nặng |
| `components/*` (dumb) | nhận `input()`, phát `output()`, render | inject data service; biết HTTP / state global |
| `services/*` | gọi API, map DTO↔model | giữ UI state |
| `state/*.store.ts` | `signal`/`computed`, orchestrate service | render, đụng DOM |
| `models/*` | type / interface | logic |

## Khi nào cần `state/*.store.ts`

**Đã CHỐT (2026-08-15):** khi cần store, dùng `signalStore()` của
`@ngrx/signals` — không tự chế store bằng `signal()` trần rải trong service.
Lý do chuẩn hoá: 1 pattern duy nhất cho mọi feature (`withState`/
`withComputed`/`withMethods`), dễ test, không để mỗi feature tự nghĩ ra 1
kiểu "store" khác nhau khi hệ thống lớn dần.

**Không** tạo store mặc định cho mọi feature. Chỉ thêm khi có ≥1 trong các
điều kiện sau:
- Nhiều component/page trong cùng feature cần đọc/ghi chung một state.
- State cần derive qua nhiều bước `computed()` lồng nhau.
- Cần cache giữa các lần điều hướng qua lại.

Feature đơn giản (1 page, state cục bộ) chỉ cần `signal()` khai trực tiếp
trong `pages/<feature>/` — **không** bọc `signalStore()` cho state chỉ 1 nơi
dùng.

```ts
// modules/<feature>/state/<feature>.store.ts
export const FeatureStore = signalStore(
  { providedIn: 'root' },
  withState<FeatureState>({ items: [], loading: false }),
  withComputed(({ items }) => ({ total: computed(() => items().length) })),
  withMethods((store, service = inject(FeatureService)) => ({
    async load() {
      patchState(store, { loading: true });
      const items = await firstValueFrom(service.list());
      patchState(store, { items, loading: false });
    },
  })),
);
```

Package `@ngrx/signals` thêm vào `package.json` khi feature đầu tiên thật
sự cần store — không cài trước khi có nhu cầu.

## Chốt chặn chống god component

- Soft cap **~300–400 dòng/component**. Vượt → tách `components/` con, **hoặc** — khi phần vượt
  là điều phối chứ không phải giao diện — tách thành lớp cộng tác `@Injectable()` khai trong
  `providers` của chính trang (xem khuôn ở dưới).

  ✅ **Không còn vi phạm (đối chiếu 2026-09-11)** — nhưng đọc tiếp khối 🛑 bên dưới trước khi tin
  dòng này. Đếm bằng lệnh, đừng chép danh sách vào đây (`.claude/CLAUDE.md` §6):

  ```bash
  find src/FE/src/app -name '*.ts' -not -name '*.spec.ts' \
    -exec awk 'END { if (NR > 400) print FILENAME ": " NR }' {} \;
  # PASS khi không in dòng nào
  ```

  🔄 **HAI lần lật trong cùng ngày 2026-09-10 — đọc cả hai, vì lần thứ hai là bài học.**
  Sáng: mục này mang nhãn *"⚠️ Đang có vi phạm"* từ 2026-09-06, ca duy nhất là
  `quan-tri-nguoi-dung.page.ts` (579 dòng), tách ba lớp cộng tác xong còn 358 ⇒ đổi sang
  ✅. Chiều: lượt dựng FE vòng 1 của cụm DTI thêm **hai** trang mới và cả hai vượt trần
  (`modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.ts`,
  `modules/dashboard/pages/dashboard/dashboard.page.ts`) ⇒ nhãn ✅ hết đúng **trong cùng ngày nó
  được viết**.

  🛑 **Vì sao ghi lại thay vì lặng lẽ đổi nhãn:** đây là dạng nhãn có tuổi thọ ngắn hơn người viết
  tưởng. `✅ Không còn vi phạm` đọc như một trạng thái ổn định, nhưng nó chỉ là **ảnh chụp của một
  lệnh tại một thời điểm** — và lệnh đó thì bất kỳ trang mới nào cũng lật được. Chạy lại lệnh,
  đừng tin nhãn.

  **Nợ vòng 1 đã trả xong (2026-09-11).** Hai trang DTI vượt trần hôm 2026-09-10 nay đều dưới
  ngưỡng, tách theo đúng khuôn ở mục ngay dưới — và phần tách ra đúng là loại "không phải giao
  diện" mà khuôn đó nhắm tới:

  | Trang | Tách thành |
  | --- | --- |
  | `modules/danh-muc-dti/pages/danh-muc-dti/` | `criteria-filters.ts` (bộ lọc + URL) · `criteria-labels.ts` (câu chữ) · bốn lớp luồng ghi (`criteria-import-flow` · `criteria-form-flow` · `criteria-inline-edit-flow` · `criteria-delete-flow`) |
  | `modules/dashboard/pages/dashboard/` | `dashboard-filters.ts` (bộ lọc + chọn kỳ + URL) · `dashboard-labels.ts` (câu chữ + định dạng số) · `dashboard-export-flow.ts` |

  Cả hai trang giữ lại **đúng phần điều phối**: quyết định băng nào hiện, luồng nào mở, và nối
  `isClean()` với lượt tải. Đó là vai "nhạc trưởng" mà một trang nên có.

  🛑 **Nhãn ✅ ở trên vẫn là ảnh chụp của một lệnh tại một thời điểm** — xem hai lần lật ngày
  2026-09-10 ngay trên. Trang mới nào cũng lật được nó, và lần trước nó hết đúng **trong cùng ngày
  nó được viết**. Chạy lại lệnh, đừng tin nhãn.

- **Khuôn tách một trang quá dài mà phần thừa KHÔNG phải giao diện.** Trang lưới + hộp thoại
  thường phình vì nó ôm nhiều máy trạng thái độc lập, chứ không vì template rườm rà — lúc đó tách
  thêm component dumb không giảm được gì. Ví dụ đang chạy để soi:
  `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/` — một `.page.ts` cạnh
  `user-list-feed.ts` (trạng thái lưới), `user-form-flow.ts` (hộp thoại tạo/sửa + ánh xạ lỗi),
  `user-lock-flow.ts` (xác nhận khoá).

  Ba ràng buộc, mỗi cái chặn một lỗi thật:

  1. **Cắt theo chỗ KHÔNG dùng chung trạng thái**, không theo số dòng. Ba lớp trên không đọc
     signal của nhau; chỗ duy nhất chúng gặp nhau là một callback "ghi xong thì nạp lại".
  2. **`@Injectable()` trần, khai trong `providers` của trang — KHÔNG `providedIn: 'root'`.** Vòng
     đời phải trùng vòng đời trang, vì đó chính là vòng đời của các field `signal()` mà chúng thay
     thế. Lên `root` là để hộp thoại đang mở dở và dữ liệu của lượt xem trước sống sót qua điều
     hướng rồi hiện lại ở lượt sau.
  3. **Đặt cạnh trang trong `pages/<trang>/`, không đẩy vào `services/` hay `state/`.** Bảng trách
     nhiệm ở dưới cấm `services/*` giữ UI state, còn `state/*.store.ts` là chỗ của `signalStore()`
     (§"Khi nào cần `state/*.store.ts`") — cả hai đều sai chỗ cho state cục bộ của đúng một trang.
     Cũng vì vậy chúng **không** đặt tên `*.service.ts`, nên không rơi vào cổng G5.
- Không bao giờ để bản `-v2` song song một component/service. Sửa tại chỗ;
  lịch sử nằm trong git.
- Component dumb chỉ nhận `input()`/phát `output()` — không tự inject
  service để tự lấy dữ liệu. Nếu thấy mình đang làm vậy, đó là dấu hiệu nó
  nên là smart component (`pages/`), không phải dumb.

## Routing

- Mỗi feature export `<FEATURE>_ROUTES` từ `<feature>.routes.ts`.
- `app.routes.ts` đăng ký bằng `loadChildren` (lazy) — không import trực tiếp
  component của feature vào `app.routes.ts`.
- Guard đặt **trong** route của feature, không dựa vào cấu hình rời rạc ở
  `app.routes.ts`.
