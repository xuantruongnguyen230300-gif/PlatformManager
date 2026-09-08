#!/usr/bin/env bash
# fe-gate.sh — G1 + G3 + G4 + G6 + G11 + G12 của bộ gate FE
#
# Ba gate còn lại chạy bằng công cụ có sẵn, KHÔNG nằm trong script này:
#   G2 + G8 + G9 → npx ng lint
#   G7           → npx ng build  (budget trong angular.json)
#   test         → npx ng test --watch=false --browsers=ChromeHeadless
#
# Định nghĩa đầy đủ từng gate: doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md
#
# Chạy từ BẤT KỲ thư mục nào — script tự resolve gốc repo từ vị trí của chính nó.
# Thoát 0 = PASS. Thoát 1 = có vi phạm (in ra từng dòng).
#
# Vì sao script này tồn tại: doc từng hướng dẫn chạy `scripts/fe-gate.sh` trong khi
# file KHÔNG hề tồn tại (phát hiện 2026-08-23). Ai chạy khối lệnh đó sẽ thấy
# "No such file or directory" ở lệnh đầu rồi 3 lệnh sau chạy bình thường — và
# tưởng gate đã xanh. G1 từng được dọn tay 2 lần và tự tái sinh cả 2 lần, đúng vì
# không có máy kiểm.

set -uo pipefail
export LC_ALL=en_US.UTF-8

cd "$(dirname "$0")/.." || exit 2
APP="src/FE/src/app"

[ -d "$APP" ] || { printf 'Không tìm thấy %s — chạy script này trong repo PlatformManager.\n' "$APP"; exit 2; }

FAIL=0
section() { printf '\n\033[1m== %s ==\033[0m\n' "$1"; }
bad()     { printf '  \033[31mFAIL\033[0m  %s\n' "$1"; FAIL=1; }
ok()      { printf '  \033[32mOK\033[0m    %s\n' "$1"; }

# ---------------------------------------------------------------- G1
# Token màu khai một chỗ (styles.scss :root). Hex trần trong SCSS của component
# nghĩa là đổi theme phải sửa N nơi — và lần đổi sau sẽ bỏ sót đúng những nơi đó.
section "G1  Không hex color literal trong SCSS của component"
n=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  bad "$hit"
  n=$((n+1))
done < <(grep -rn '#[0-9a-fA-F]\{3,8\}\b' "$APP" --include='*.scss' 2>/dev/null)
[ "$n" -eq 0 ] && ok "không có hex trần ngoài styles.scss"

# ---------------------------------------------------------------- G3
# Cú pháp Angular cũ. Doc cấm cho code mới; gate giữ để không trôi ngược.
section "G3  Không còn cú pháp Angular cũ"
n=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  bad "$hit"
  n=$((n+1))
done < <(grep -rnE '\*ngIf|\*ngFor|\*ngSwitch|@Input\(\)|@Output\(\)|NgModule' "$APP" \
           --include='*.ts' --include='*.html' 2>/dev/null)
[ "$n" -eq 0 ] && ok "không còn *ngIf/*ngFor/*ngSwitch/@Input()/@Output()/NgModule"

# ---------------------------------------------------------------- G4
# Component trong `components/` là DUMB: nhận `input()`, phát `output()`. Nó không
# được tự đi lấy dữ liệu — nếu cần, đó là dấu hiệu nó nên là smart component
# (`pages/`). Luật đầy đủ: doc/huong_dan/quy-uoc/fe-architecture.md §Bảng trách nhiệm.
#
# Bật 2026-09-08, tức LÂU sau khi luật được viết. Khoảng trống đó có giá đo được:
# `user-grid-table.ts` từng inject thẳng `LanguageService` và làm 37 test đỏ — thứ
# bắt được là BỘ TEST, không phải cổng. Lần vi phạm kế tiếp ở một chỗ không có test
# tương ứng sẽ không ai biết.
#
# HAI TRỤC, KHÔNG PHẢI MỘT — đây là phần dễ làm sai nhất, đọc trước khi sửa:
#
#  1. `inject(HttpClient)` / `inject(HttpBackend)` → FAIL **tuyệt đối**, không ngoại
#     lệ nào, kể cả app-shell. Chúng không kết thúc bằng `Service` nên phải bắt tên
#     tường minh; bỏ hai dòng này là để hở đúng ca thô thiển nhất.
#
#  2. `inject(<X>Service)` → FAIL **trừ khi** một trong hai:
#       a. `<X>Service` nằm trong UI_INFRA_SERVICES — service KHÔNG chạm dữ liệu.
#       b. file nằm trong APP_SHELL_FILES — ngoại lệ app-shell đã khai ở
#          doc/huong_dan/wiki-core/fe/05-component-library.md §"Ngoại lệ inject".
#
# Vì sao không dùng một mình danh sách FILE như bản phác trong 05-gate.md gợi ý:
# `user-form-dialog.ts` inject `TranslateService` + `ApiErrorMessageService` và nó
# KHÔNG phải app-shell. Cho nó vào danh sách file nghĩa là mở toang cả file đó cho
# mọi service kể cả `QuanTriNguoiDungService` — tức tha đúng thứ G4 sinh ra để cấm.
# Chặn theo TÊN SERVICE khớp đúng câu chữ của luật ("HttpClient/service data"), và
# giữ ngoại lệ file cho đúng 2 chỗ thật sự cần data service.
#
# Mặc định là CẤM: một service lạ chưa có tên trong bảng nào thì FAIL. Người thêm
# phải quyết định tường minh nó thuộc nhóm nào — im lặng cho qua là cách gate chết.
#
# 🛑 LỖ MÙ ĐÃ BIẾT, đừng tưởng G4 phủ hết: chỉ bắt định danh kết thúc bằng `Service`.
# `inject(FeatureStore)` của `@ngrx/signals` đi lọt. CỐ Ý chưa bắt — hôm nay chưa có
# store nào trong app, và 05-gate.md §"Không làm" cấm viết gate cho thứ chưa tồn tại.
# Thêm `*Store` vào cùng lúc với store đầu tiên.

# Service KHÔNG chạm dữ liệu — inject từ `components/` ở đâu cũng được.
# Mỗi tên phải tự bảo vệ được bằng phép thử "có HttpClient không", chạy ngay dưới.
UI_INFRA_SERVICES="ToastService LanguageService ApiErrorMessageService SidebarStateService TranslateService"

# Ngoại lệ app-shell — được inject cả service DỮ LIỆU vì chúng là lớp vỏ app, không
# phải component hiển thị dữ liệu nghiệp vụ (05-component-library.md §"Ngoại lệ inject").
#   sidebar → MenuService · topbar → CurrentUserService + AuthService
# Thêm file vào đây là quyết định kiến trúc, không phải cách dọn một dòng FAIL.
APP_SHELL_FILES="$APP/shared/components/sidebar/sidebar.ts $APP/shared/components/topbar/topbar.ts"

# Bỏ chú thích TS nhưng GIỮ NGUYÊN SỐ DÒNG — cùng bài học của G12 (xem 05-gate.md
# §G12): xoá trắng làm luồng ngắn lại nên `grep -n` báo sai dòng, và một cổng sai số
# dòng thì người sửa mở nhầm chỗ rồi kết luận cổng báo bậy.
#
# Bắt buộc phải bỏ chú thích: repo này giải thích luật G4 NGAY TRONG JSDoc, nên
# `data-grid.ts` có nguyên chuỗi "không `inject(LanguageService)`" trong comment. Dò
# trên file thô là gate tự báo lỗi vì chính lời cảnh báo của mình.
#
# `(?<!:)` giữ lại `https://…` — không có nó thì mọi thứ sau một URL trên cùng dòng
# bị nuốt, tức gate MÙ (bỏ sót), hướng hỏng đắt hơn báo nhầm.
strip_ts_comments() {
  perl -0777 -pe 's{/\*.*?\*/}{ "\n" x (() = ($& =~ /\n/g)) }gse; s{(?<!:)//[^\n]*}{}g' "$1" 2>/dev/null
}

section "G4  components/ không inject HttpClient / service dữ liệu"
n=0

# Tự kiểm danh sách miễn trừ TRƯỚC khi dùng nó. Một allowlist không ai kiểm lại sẽ
# mục: chỉ cần có người thêm `HttpClient` vào `ToastService` là cả G4 mất tác dụng
# trong im lặng, vì cái tên vẫn nằm đó. Đây là chỗ rẻ nhất để chặn điều đó.
for svc in $UI_INFRA_SERVICES; do
  decl=$(grep -rlE "class $svc\b" "$APP" --include='*.ts' 2>/dev/null | grep -v '\.spec\.ts$' | head -n 1)
  # Không tìm thấy = service của thư viện ngoài (vd TranslateService) — không kiểm được, bỏ qua.
  [ -z "$decl" ] && continue
  if grep -qE '\b(HttpClient|HttpBackend)\b' "$decl"; then
    bad "$decl — '$svc' đang nằm trong UI_INFRA_SERVICES của G4 (\"không chạm dữ liệu\") nhưng đã dùng HttpClient. Gỡ tên đó khỏi danh sách, rồi sửa các component đang inject nó."
    n=$((n+1))
  fi
done

while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  ident=${hit##*:}          # định danh trong inject(...)
  loc=${hit%:*}             # đường-dẫn:dòng
  file=${loc%:*}            # đường dẫn

  case "$ident" in
    HttpClient|HttpBackend)
      bad "$loc — inject($ident) trong components/. KHÔNG có ngoại lệ nào cho luật này: component dumb không tự đi lấy dữ liệu. Đưa lời gọi lên pages/ rồi truyền xuống bằng input()."
      n=$((n+1)); continue ;;
  esac

  case "$ident" in *Service) ;; *) continue ;; esac
  case " $UI_INFRA_SERVICES " in *" $ident "*) continue ;; esac
  case " $APP_SHELL_FILES " in *" $file "*) continue ;; esac

  bad "$loc — inject($ident) trong components/. Nhận dữ liệu qua input() từ pages/. Nếu '$ident' KHÔNG chạm dữ liệu, thêm tên nó vào UI_INFRA_SERVICES ở scripts/fe-gate.sh (§G4) kèm lý do."
  n=$((n+1))
done < <(
  find "$APP" -name '*.ts' -not -name '*.spec.ts' 2>/dev/null | grep '/components/' | while IFS= read -r f; do
    strip_ts_comments "$f" \
      | grep -noE 'inject\( *[A-Za-z_][A-Za-z0-9_]*' \
      | sed -E 's/inject\( *//' \
      | sed "s|^|$f:|"
  done
)
[ "$n" -eq 0 ] && ok "không component dumb nào inject HttpClient hay service dữ liệu"

# ---------------------------------------------------------------- G6
# DTO thuộc về server, model thuộc về app. Component/page chạm thẳng DTO là mất
# điểm chặn khi server đổi field — TypeScript bị xoá lúc chạy nên không ai báo.
#
# HAI ĐIỀU ĐÃ KIỂM CHỨNG BẰNG CANARY, đừng "đơn giản hoá" ngược lại:
#  - Mẫu phải là `Dto\b`, KHÔNG phải `\bDto\b`. Tên DTO thật luôn dạng `IUserDto`
#    — giữa `r` và `D` không có word boundary nên `\bDto\b` không bao giờ khớp,
#    gate xanh vì mù chứ không vì sạch.
#  - Loại trừ `*.spec.ts`. Đây là PHẠM VI ĐÚNG của rule, không phải ngoại lệ:
#    G6 bảo vệ đường code chạy thật, còn spec stub tầng HTTP thì bắt buộc phải
#    dựng payload đúng hình dạng wire, tức phải nói bằng DTO.
section "G6  components/ và pages/ không import DTO trực tiếp"
n=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  case "$hit" in *.spec.ts:*) continue ;; esac
  bad "$hit"
  n=$((n+1))
done < <(grep -rnE 'Dto\b' "$APP" --include='*.ts' 2>/dev/null \
           | grep -E '/(components|pages)/')
[ "$n" -eq 0 ] && ok "không có DTO nào lọt vào components/ hoặc pages/"

# ---------------------------------------------------------------- G11
# Anh em của G1, cho đúng phần G1 MÙ: G1 chỉ quét `#rrggbb`, nên cùng MỘT quyết
# định màu viết bằng thập phân thì đi lọt — `rgba(15, 91, 215, .08)` chính là
# `--brand` `#0f5bd7`. Chỗ nặng nhất phát hiện 2026-09-03 là nền mục menu ĐANG
# CHỌN của sidebar: thấy ở mọi màn hình, và dòng ngay dưới nó đã dùng
# `var(--brand)` ⇒ là SÓT chứ không phải chủ đích.
#
# Phạm vi hẹp hơn G1 có chủ đích: chỉ `core/` + `shared/` + `platform/` — ba tầng
# NẰM TRONG CoreBase (doc/kien-truc-core-module.md). Màu trần ở đó đi theo nền
# tảng sang sản phẩm thứ hai và không đổi theo bảng màu của nó.
#
# `src/FE/src/styles.scss` KHÔNG bị quét, cùng lý do G1 không quét: đó là nơi
# token được KHAI, nên literal ở đó là ĐỊNH NGHĨA chứ không phải bản sao. Đây
# cũng là chỗ duy nhất được phép giữ literal — đúng khuôn đợt promote 2026-08-29
# (audit FE-7).
#
# 🛑 KHÔNG miễn trừ theo GIÁ TRỊ. `rgba(0,0,0,.5)` trông vô hại nhưng vẫn là một
# quyết định thiết kế, mà quyết định thì thuộc `doc/Design/.../Tokens/`. Miễn trừ
# theo giá trị là mở lại đúng cánh cửa gate này sinh ra để đóng.
#
# Miễn trừ DUY NHẤT là theo CÚ PHÁP: `rgb(var(--x) / a)` — dạng đọc token ra rồi
# pha alpha. Nó không mang giá trị màu nào nên không có gì để lệch khi đổi theme.
section "G11 Không màu literal rgb()/rgba() trong SCSS của core/ shared/ platform/"
n=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  # Gỡ dạng ĐƯỢC PHÉP ra khỏi dòng rồi mới hỏi lại. Làm hai bước như vậy để một
  # dòng vừa có `rgb(var(--x) / a)` hợp lệ vừa có literal trần VẪN bị bắt — kiểm
  # bằng `grep -v` một phát sẽ tha nhầm cả dòng.
  stripped=$(printf '%s' "$hit" | sed -E 's/rgba?\( *var\(--[A-Za-z0-9_-]+\) *\/[^)]*\)//g')
  case "$stripped" in
    *"rgb("*|*"rgba("*) bad "$hit"; n=$((n+1)) ;;
  esac
done < <(grep -rnE 'rgba?\(' "$APP/core" "$APP/shared" "$APP/platform" --include='*.scss' 2>/dev/null)
[ "$n" -eq 0 ] && ok "không có rgb()/rgba() trần — màu đến từ token"

# ---------------------------------------------------------------- G12
# Câu người dùng đọc phải đến từ public/i18n/<code>.json, không nằm trong template.
# Dò DẤU THANH tiếng Việt chứ không dò "text node ngoài | translate": dấu thanh không
# thể là tên biến/lớp CSS/từ khoá Angular nên gần như không báo nhầm, mà lại không cần
# parse cú pháp Angular. Giới hạn đã biết: KHÔNG bắt được chuỗi cứng tiếng Anh — đánh
# đổi có chủ đích, lý do đầy đủ ở 05-gate.md §G12.
section "G12 Template .html không chứa chữ tiếng Việt (câu phải đến từ i18n)"
n=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  bad "$hit"
  n=$((n+1))
done < <(
  find "$APP" -name '*.html' 2>/dev/null | while IFS= read -r f; do
    # Xoá comment HTML TRƯỚC khi dò: repo chú thích bằng tiếng Việt khắp nơi, mà chú
    # thích thì không ai đọc trên màn hình. perl -0777 để bắt comment nhiều dòng.
    # Thay comment bằng ĐÚNG số xuống dòng nó chiếm, KHÔNG xoá trắng: xoá trắng làm
    # luồng ngắn lại và `grep -n` đếm trên luồng đó, nên số dòng báo ra lệch so với file
    # thật (đã dính: app.html báo :5 trong khi dòng thật là :12). Một cổng chỉ sai số
    # dòng thôi cũng đủ làm người sửa mở nhầm chỗ rồi kết luận cổng báo bậy.
    perl -0777 -pe 's{<!--.*?-->}{ "
" x (() = ($& =~ /
/g)) }gse' "$f" 2>/dev/null \
      | grep -nE '[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ]' \
      | sed "s|^|$f:|"
  done
)
[ "$n" -eq 0 ] && ok "không còn chữ tiếng Việt trong template"

# ----------------------------------------------------------------
printf '\n'
if [ "$FAIL" -eq 0 ]; then
  printf '\033[32m✅ PASS — G1 + G3 + G4 + G6 + G11 + G12\033[0m\n'
  printf 'Còn lại phải chạy tay: npx ng lint (G2/G8/G9) · npx ng build (G7) · npx ng test\n'
else
  printf '\033[31m❌ FAIL — xem danh sách trên\033[0m\n'
fi
exit "$FAIL"
