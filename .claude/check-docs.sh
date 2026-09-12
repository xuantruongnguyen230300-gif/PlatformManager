#!/usr/bin/env bash
# check-docs.sh — gate tài liệu cho PlatformManager
#
# Kiểm 5 luật trong .claude/CLAUDE.md. Chạy từ gốc repo:
#     bash .claude/check-docs.sh
#
# Thoát 0 = PASS. Thoát 1 = có vi phạm (in ra từng dòng).
# Repo không có CI — script này là gate duy nhất, phải chạy bằng tay.
#
# HIỆU NĂNG: mỗi mục dùng ĐÚNG 1 lệnh grep đệ quy cho toàn repo, phần còn lại là
# builtin của bash. Lý do: trên Git Bash/Windows mỗi lần spawn tiến trình tốn
# ~50ms, nên bản chạy 1 grep/file mất 68 giây. Một gate chậm sẽ bị bỏ qua — đúng
# số phận của scripts/fe-gate.sh. Giữ nguyên hình dạng này khi sửa.

set -uo pipefail

# Ép locale UTF-8 tường minh — không dựa vào locale sẵn có của shell gọi script.
# Phát hiện 2026-08-24 (core-reviewer): trên Git Bash với LANG rỗng, `grep -P`
# (dùng ở nhiều mục bên dưới) lỗi "-P supports only unibyte and UTF-8 locales"
# và THOÁT MÃ 2 — nhưng vòng lặp `while read` bọc ngoài chỉ đơn giản không nhận
# được dòng nào, nên mục đó luôn in "OK" dù không hề chạy kiểm tra thật. Gate
# PASS giả kiểu này nguy hiểm hơn FAIL thật, vì không ai nghi ngờ để kiểm lại.
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/.." || exit 2

FAIL=0
section() { printf '\n\033[1m== %s ==\033[0m\n' "$1"; }
# ---------------------------------------------------------------- §0
# "Tôi có đang xét gì không?" — thêm 2026-09-08 sau một lượt kiểm đối kháng chạy
# script ở THƯ MỤC RỖNG và nhận đủ `✅ PASS — tài liệu đồng bộ`, 13/13 OK. Không
# mục nào khẳng định nó đã xét ≥1 mục, nên PASS không phân biệt được "mọi thứ
# đúng" với "tôi không nhìn gì cả".
#
# Cùng lượt đó phát hiện §1 là **no-op im lặng suốt thời gian dài**: pattern của
# nó chứa ký tự 🛑 (U+1F6D1, 4 byte), mà `grep` cơ bản trên Git Bash không khớp
# được ký tự đó dưới chính locale `en_US.UTF-8` script tự ép ở dòng dưới. Vòng
# lặp nhận 0 dòng ⇒ `n=0` ⇒ in OK mà **không so sánh gì**. Đây là tái phát đúng
# cơ chế mà khối chú thích LC_ALL ngay dưới nói là đã diệt — bản vá đó cứu được
# `grep -P` nhưng đẻ lại lỗi ở §1. Bài học: mỗi mục phải tự chứng minh nó có dữ
# liệu đầu vào, đừng suy ra từ "không có lỗi nào".
for m in .claude/CLAUDE.md .claude/settings.json doc spec src; do
  [ -e "$m" ] || { printf '\033[31m❌ ABORT — không thấy %s. Chạy script từ gốc repo.\033[0m\n' "$m"; exit 2; }
done
_n_doc=$(find doc spec -name '*.md' 2>/dev/null | wc -l)
[ "$_n_doc" -lt 50 ] && { printf '\033[31m❌ ABORT — chỉ thấy %s file .md trong doc/+spec/. Cây repo không đầy đủ; PASS lúc này vô nghĩa.\033[0m\n' "$_n_doc"; exit 2; }
bad()     { printf '  \033[31mFAIL\033[0m  %s\n' "$1"; FAIL=1; }
ok()      { printf '  \033[32mOK\033[0m    %s\n' "$1"; }

# File mang banner "TÀI LIỆU LỊCH SỬ" (§5) mô tả trạng thái QUÁ KHỨ — đường dẫn
# và mốc thời gian trong đó cố ý không còn đúng. Miễn trừ khỏi §4.
#
# CHỈ NHẬN BANNER Ở ĐẦU FILE (30 dòng đầu), không nhận mọi lần nhắc tới cụm đó.
# Sửa 2026-09-03 sau khi một lượt kiểm độc lập chứng minh cổng xanh vì KHÔNG KIỂM GÌ:
# bản trước dùng `grep -rl` nên khớp BẤT KỲ dòng nào trong file. doc/cau-truc-database.md
# nhắc cụm này ở một ô bảng NÓI VỀ FILE KHÁC, thế là cả file chủ 600 dòng được miễn
# §4a/§4b/§4c — đúng chỗ một trích dẫn chết đang nằm. §5 vốn quy định banner phải dán
# ở ĐẦU file; phép dò phải khớp đúng luật đó.
# `spec/` được quét từ 2026-09-08. §2 của CLAUDE.md tuyên bố repo có BA khu tri
# thức, nhưng mọi mục của gate chỉ quét hai — nên khu nghiệp vụ không có cổng
# nào suốt thời gian đó. Hệ quả đo được: doc/Design/.../UiInventory.md:25 trỏ
# `spec/sidebar-menu/ui-spec.md` (đã xoá) mà không mục nào bắt được, vì §4a chỉ
# nhận tiền tố `src/` và `doc/`.
#
# HIỆU NĂNG — sửa 2026-09-08: bản trước chạy head+grep cho TỪNG file (140 file =
# 280 lần spawn, đo được 7 giây). Gói vào một lần awk, giữ nguyên luật "chỉ nhận
# banner trong 30 dòng đầu".
HIST_FILES=" $(find doc .claude spec -name '*.md' -print0 2>/dev/null | xargs -0 awk '
  FNR==1  { hit=0 }
  hit || FNR>30 { next }
  /TÀI LIỆU LỊCH SỬ/ { print FILENAME; hit=1 }' | tr '\n' ' ')"

# Dòng nói rõ nó đang nhắc tới thứ đã biến mất cũng được miễn trừ khỏi §4 — nếu
# không, mọi ghi chép "vì sao ta bỏ X" đều bị báo lỗi, và bài học sẽ bị xoá đi
# cho gate xanh. Đó chính là hành vi §4 tồn tại để ngăn.
# Thêm 2026-09-08, hai cụm, cùng một lý do. Mục §4a2 bắt tài liệu neo vào file bị
# gitignore; nhưng ghi chép GIẢI THÍCH việc loại trừ đó buộc phải gọi tên chính
# file kia. Không có miễn trừ thì bài học lại bị xoá cho cổng xanh — đúng hành vi
# hai miễn trừ ở đây tồn tại để ngăn.
#   'loại khỏi repo'    — câu văn nói thẳng file nằm ngoài repo
#   'check-ignore'      — dòng CHẠY `git check-ignore` trên một đường dẫn đang
#                         khẳng định đường dẫn đó BỊ loại trừ, tức ngược hẳn với
#                         việc lấy nó làm bằng chứng. Dấu hiệu máy đọc được, không
#                         phải một câu tự nhận.
#
# 🛑 KHÔNG miễn trừ dòng `Sources:` — thêm 2026-09-08 sau một lượt kiểm đối kháng.
# Miễn trừ này áp cho CẢ DÒNG. Dòng `Sources:` của spec component là một DANH SÁCH
# BẰNG CHỨNG dài hàng chục neo, nên chỉ cần một mệnh đề "… đã xoá 2026-08-29 …"
# ở cuối dòng là **mọi neo trên dòng đó** được tha, kể cả neo trỏ file còn sống.
# Đã dính thật ở 3 chỗ (`FormRow.md`, `Footer.md`, `Dialog.md`), và nó che một neo
# SAI 21 DÒNG: `FormRow.md` dẫn `quan-tri-nguoi-dung.model.ts:71` cho
# `ASSIGNABLE_ROLES`, thật ra ở `:92` — dòng 71 là `UserName: string;`.
# Phân biệt được bằng máy: `Sources:` là danh sách bằng chứng, không bao giờ là
# văn kể lịch sử. Văn kể lịch sử thì viết thành câu.
HIST_LINES=" $(grep -rn 'trước ở\|trước nằm ở\|đã xoá\|đã chuyển\|đã bỏ\|không còn tồn tại\|chưa từng tồn tại\|loại khỏi repo\|check-ignore' doc .claude spec --include='*.md' 2>/dev/null | grep -vP '^[^:]+:\d+:\s*\**Sources:' | cut -d: -f1,2 | tr '\n' ' ')"

# Vùng GẠCH NGANG `~~…~~` — trích nguyên văn một luật ĐÃ BỊ LẬT, giữ lại để thấy
# đã đổi gì (§5). Đường dẫn trong đó cố ý mô tả trạng thái cũ, không phải khẳng
# định hiện hành. Miễn trừ theo cụm từ của HIST_LINES chạy theo TỪNG DÒNG nên
# không bắt được ngữ cảnh này: ở doc/huong_dan/quy-uoc/repo-artifact.md, dấu
# "ĐÃ BỊ LẬT" nằm ở dòng 24 còn đường dẫn ở dòng 30. Chỉ §4a2 dùng phép dò này —
# các mục khác giữ nguyên hành vi, để không vô tình che lỗi thật.
HIST_STRIKE=" $(find doc .claude spec -name '*.md' -print0 2>/dev/null | xargs -0 awk '
  FNR==1 { inside=0 }
  { cnt = gsub(/~~/, "&")
    if (inside || cnt > 0) print FILENAME ":" FNR
    if (cnt % 2 == 1) inside = 1 - inside }' | tr '\n' ' ')"

# ---------------------------------------------------------------- §1
# Bảng cấm git trong CLAUDE.md §1 phải khớp permissions.deny trong settings.json.
# §1 tự tuyên bố lệnh cấm "được cưỡng chế bằng máy" — nếu văn bản liệt kê một
# lệnh mà deny không chặn, câu đó thành lời hứa suông.
# Đã trượt HAI lần: 2026-08-21 (văn bản cấm cả lệnh đọc, deny thì không) và
# 2026-08-23 (văn bản thêm restore/pull/tag/config/worktree/submodule, deny
# không theo). Hai lần là đủ để cưỡng chế bằng máy thay vì bằng trí nhớ.
section "§1  Bảng cấm git phải khớp settings.json"
n=0; lay=0
while IFS= read -r cmd; do
  [ -z "$cmd" ] && continue
  # `remote *` được deny khai theo từng subcommand (remote add/rm/set-url/rename)
  case "$cmd" in *' '*|remote) continue ;; esac
  lay=$((lay+1))
  grep -q "Bash(git $cmd:" .claude/settings.json && continue
  bad "CLAUDE.md §1 cấm \`git $cmd\` nhưng settings.json deny KHÔNG chặn"
  n=$((n+1))
  # KHÔNG có ký tự 🛑 trong pattern — xem khối chú thích ngay trên.
done < <(grep -m1 '\*\*CẤM\*\*' .claude/CLAUDE.md | grep -oP '`\K[a-z-]+(?= \*)|`\K[a-z-]+(?=`)')
if [ "$lay" -eq 0 ]; then
  bad "§1 KHÔNG trích được lệnh nào từ bảng cấm — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "mọi lệnh git trong bảng cấm đều được deny chặn ($lay lệnh)"
fi

# ---------------------------------------------------------------- §2
# .claude/ chỉ chứa quy trình. Code mẫu là tri thức -> thuộc doc/.
# Cho phép: bash/sh (lệnh chạy), markdown (mẫu báo cáo), text (khối $ARGUMENTS),
# và khối không gắn ngôn ngữ (sơ đồ cây thư mục, ASCII).
section "§2  .claude/ không được chứa code block ngôn ngữ"
n=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  lang="${hit##*:}"
  case "$lang" in '```bash'|'```markdown'|'```text'|'```sh') continue ;; esac
  bad "${hit%:*}  có khối $lang  — code mẫu là tri thức, chuyển sang doc/"
  n=$((n+1))
done < <(grep -rn '^```[a-zA-Z]' .claude --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "không có code block ngôn ngữ nào trong .claude/"

# ---------------------------------------------------------------- §3
# Link gãy = dấu hiệu file bị di chuyển mà đường dẫn không được cập nhật.
section "§3  Link markdown nội bộ phải resolve được"
n=0
while IFS=: read -r f ln link; do
  [ -z "${link:-}" ] && continue
  case "$link" in http*|mailto*|'#'*|'{'*|'<'*|'$'*) continue ;; esac
  target="${link%%#*}"
  [ -z "$target" ] && continue
  [ -e "${f%/*}/$target" ] && continue
  bad "$f:$ln  ->  $link"
  n=$((n+1))
done < <(grep -rnoP '\]\(\K[^)]+' doc .claude spec --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi link nội bộ đều resolve được"

# ---------------------------------------------------------------- §4a
# Trích dẫn src/... doc/... spec/... scripts/... phải trỏ file có thật. Đây là
# thứ bắt được "RequirePermissionFilter.cs", "import-dialog", "rules/performance.md"
# — các file được viện dẫn làm bằng chứng nhưng không tồn tại.
#
# `spec/` và `scripts/` thêm 2026-09-08. Trước đó regex chỉ nhận `src|doc`, nên
# một trích dẫn `spec/sidebar-menu/ui-spec.md` trỏ file đã xoá lọt hoàn toàn —
# không phải vì gate bỏ sót file nguồn, mà vì nó không nhận ra đó là đường dẫn.
section "§4  Đường dẫn được trích dẫn phải tồn tại"
n=0
while IFS=: read -r f ln p; do
  [ -z "${p:-}" ] && continue
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  case "$p" in *'/.../'*) continue ;; esac   # đường dẫn viết tắt, không phải trích dẫn
  [ -e "$p" ] && continue
  bad "$f:$ln  trích dẫn  $p"
  n=$((n+1))
done < <(grep -rnoP '(?<![\w./-])(?:src|doc|spec|scripts)/[A-Za-z0-9_./-]+\.(?:cs|ts|scss|html|md|json|sql|dbml|sh|csv|xlsx)\b' doc .claude spec --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi đường dẫn được trích dẫn đều tồn tại"

# ---------------------------------------------------------------- §4a2
# Đường dẫn được trích dẫn phải nằm TRONG repo, không chỉ trên đĩa.
#
# Thêm 2026-09-08. §4a dùng `[ -e ]`, nên nó xanh cho cả file chỉ tồn tại trên
# máy người đang chạy. Đo được hôm đó: 36 citation trong doc/Design trỏ
# `Prototype/index.html` — file gitignored, `git ls-files` trả 0 dòng. Cổng xanh
# trên máy này, nhưng ai clone repo về thì cả 36 citation trỏ vào hư không, và
# không mục nào báo. Một cổng xác thực dựa trên trạng thái riêng của một máy thì
# không xác thực được gì cho người thứ hai.
#
# MIỄN TRỪ `Prototype/` — người dùng chốt 2026-09-08: đó là thư mục ĐỐI CHIẾU
# RIÊNG của họ, cố ý nằm ngoài repo (xem `.gitignore`, và §7 vốn cấm dựng nguồn
# giao diện thứ hai trong repo). Miễn trừ được khai ở ĐÂY, tường minh, thay vì
# để nó âm thầm lọt qua vì `[ -e ]` tình cờ đúng trên một máy. Hệ quả phải nói
# thẳng: 36 citation đó KHÔNG ai ngoài người dùng kiểm được, và mọi khẳng định
# dựa vào chúng vẫn ở mức chưa xác minh với người thứ hai.
# ---------------------------------------------------------------- §4a2
# Phân biệt "chưa commit" với "cố ý nằm ngoài repo" — đo 2026-09-08.
#
# Bản đầu của mục này báo lỗi cho MỌI file chưa `git ls-files`. Đo ngay sau đó:
# repo có 553 file tracked và **204 file untracked** — 78 ở `src/BE`, 66 ở
# `src/FE`, 30 ở `doc/Design`, kể cả `.gitignore` và `nginx.conf`. Tức mục này
# sẽ báo hàng trăm dòng, và tất cả đều nói đúng một điều: "bạn chưa commit".
# Một cổng đo tiến độ commit là cổng người ta tắt đi.
#
# Bất biến THẬT cần canh hẹp hơn: tài liệu không được lấy bằng chứng từ file
# **bị loại khỏi repo theo thiết kế** (gitignore). File chưa commit rồi sẽ vào
# repo; file gitignore thì không bao giờ — người thứ hai clone về sẽ không bao
# giờ mở được nó, nên mọi khẳng định neo vào đó là khẳng định không kiểm được.
section "§4  Trích dẫn không được neo vào file bị gitignore"
n=0; chua_commit=0
declare -A TRACKED=() SEEN=()
while IFS= read -r t; do [ -n "$t" ] && TRACKED["$t"]=1; done < <(git ls-files 2>/dev/null)
if [ "${#TRACKED[@]}" -eq 0 ]; then
  ok "bỏ qua — không đọc được danh sách file git track"
else
  # Gom trước, rồi hỏi git MỘT lần qua --stdin. Hỏi từng file là 1 spawn/file.
  CAND=""
  while IFS=: read -r f ln p; do
    [ -z "${p:-}" ] && continue
    case "$HIST_FILES" in *" $f "*) continue ;; esac
    case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
    case "$HIST_STRIKE" in *" $f:$ln "*) continue ;; esac
    case "$p" in *'/.../'*) continue ;; esac
    # 🔄 GỠ 2026-09-10 — miễn trừ `Prototype/*` đã bị xoá, KHÔNG khôi phục.
    #
    # Nó được mở 2026-09-08 với lý do "thư mục ĐỐI CHIẾU RIÊNG, cố ý ngoài repo",
    # kèm câu tự thú ngay tại chỗ: "36 citation đó không ai ngoài người dùng kiểm
    # được". Tiền đề đó hết đúng khi bản prototype ẩn danh được đưa VÀO repo
    # (doc/Design/Frontend/PlatformManager/Prototypes/index.html) — mọi trích dẫn
    # nay trỏ được vào bằng chứng ai cũng mở được.
    #
    # Bài học đắt hơn lý do: cùng một lớp lỗi "trích thứ ngoài repo" đã xuất hiện
    # BA lần — fixture test .csv, prototype .html, file mẫu .xlsx — và cả ba lần
    # cổng đều im lặng, hai lần vì thiếu phần mở rộng trong regex, một lần vì
    # chính miễn trừ này. Một miễn trừ khai tường minh vẫn là một lỗ hổng; nó chỉ
    # khác ở chỗ có người ký tên.
    [ -n "${TRACKED[$p]:-}" ] && continue
    [ -e "$p" ] || continue        # không tồn tại thì §4a đã báo, đừng báo hai lần
    [ -n "${SEEN[$p]:-}" ] && continue
    SEEN["$p"]="$f:$ln"
    CAND="$CAND$p"$'\n'
  done < <(grep -rnoP '(?<![\w./-])(?:src|doc|spec|scripts|Prototype)/[A-Za-z0-9_./-]+\.(?:cs|ts|scss|html|md|json|sql|dbml|sh|csv|xlsx)\b' doc .claude spec --include='*.md' 2>/dev/null)

  if [ -n "$CAND" ]; then
    while IFS= read -r p; do
      [ -z "$p" ] && continue
      bad "${SEEN[$p]}  trích dẫn  $p  — file này bị .gitignore loại khỏi repo"
      n=$((n+1))
    done < <(printf '%s' "$CAND" | git check-ignore --stdin 2>/dev/null)
    chua_commit=$(printf '%s' "$CAND" | grep -c . )
    chua_commit=$((chua_commit - n))
  fi
  if [ "$n" -eq 0 ]; then
    ok "không trích dẫn nào neo vào file bị gitignore"
    [ "$chua_commit" -gt 0 ] && printf '        (%s file được trích dẫn chưa commit — sẽ vào repo khi bạn commit, không phải lỗi tài liệu)\n' "$chua_commit"
  fi
fi

# ---------------------------------------------------------------- §4b
# Tuyên bố "đã xong" phải kèm ngày đối chiếu. Không ngày = không kiểm được.
# Đây là luật quan trọng nhất: 7 ca sai nặng nhất đợt 2026-08-23 đều mang nhãn
# hoàn thành mà không ai kiểm lại.
section "§4  Tuyên bố hoàn thành phải kèm ngày đối chiếu"
n=0
while IFS=: read -r f ln rest; do
  [ -z "${rest:-}" ] && continue
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$rest" in *[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]*) continue ;; esac
  bad "$f:$ln  tuyên bố hoàn thành không có ngày đối chiếu"
  n=$((n+1))
done < <(grep -rn 'ĐÃ CÓ\|✅ Xong\|✅ XONG\|FIXED\|Đã bật\|đã triển khai\|đã verify' doc spec --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi tuyên bố hoàn thành đều có ngày"

# ---------------------------------------------------------------- §4c
# Trích dẫn `file:dòng` phải nằm TRONG file. Đây là phép đo gián tiếp của tính
# đúng nội dung: tài liệu bịa bằng chứng thường bịa luôn số dòng, và số dòng thì
# máy đếm được. Đợt 2026-08-23 nó tự tìm ra 2 lời nói dối mà 4 agent phải đọc
# 800 KB mới thấy — `index.html:21` (file 14 dòng, chống lưng cho claim "Inter
# FIXED") và `phan-quyen.page.scss:6` (file 4 dòng, chống lưng cho TabBar.md bịa).
section "§4  Trích dẫn file:dòng phải nằm trong file"
n=0
declare -A LC=()
while IFS=: read -r f ln cite; do
  [ -z "${cite:-}" ] && continue
  path="${cite%:*}"; num="${cite##*:}"
  # Neo dạng range `x.ts:26-31` — kiểm ĐUÔI, không phải đầu. Thêm 2026-09-08: bản
  # trước regex chỉ bắt `:\d+` nên chỉ thấy `26`, và 3 neo vượt EOF trong Dialog.md
  # lọt qua ( `:26-31` vào file 27 dòng, `:6-11` và `:1-11` vào file 7 dòng).
  # Neo có thể nối nhiều range bằng dấu phẩy (`x.json:37-39,123-125`). Bản trước
  # regex dừng sau range ĐẦU, nên vế sau không bao giờ được đọc — đo được 47 neo
  # dạng này trong doc/Design, và một trong số đó (`angular.json:...,123-125` vào
  # file 122 dòng) là bằng chứng giả lọt cổng. Lấy số LỚN NHẤT qua mọi vế.
  _max=0
  _rest="$num"
  while [ -n "$_rest" ]; do
    _p="${_rest%%,*}"; [ "$_p" = "$_rest" ] && _rest="" || _rest="${_rest#*,}"
    _e="${_p##*-}"
    case "$_e" in ''|*[!0-9]*) continue ;; esac
    [ "$_e" -gt "$_max" ] && _max="$_e"
  done
  num="$_max"
  [ "$num" -eq 0 ] && continue
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  [ -f "$path" ] || continue          # file không tồn tại đã do §4a báo
  # Đếm bằng awk NR, KHÔNG bằng `wc -l` — sửa 2026-09-11 sau một phép đo.
  # `wc -l` đếm số ký tự xuống dòng, nên file THIẾU newline cuối bị báo hụt đúng 1 dòng,
  # và cổng từ chối một trích dẫn neo vào dòng cuối cùng dù dòng đó có thật. Đo hôm đó:
  # 6 file `.scss` trong `src/FE` thiếu newline cuối, gồm `styles.scss` — file bị khu Design
  # trích dẫn nhiều nhất. Triệu chứng là người viết tài liệu phải neo lùi một dòng cho gate
  # xanh, tức gate dạy người ta viết trích dẫn SAI. Đổi này chỉ nới trần, không nới chặt:
  # nó không thể biến một trích dẫn sai thành hợp lệ.
  if [ -z "${LC[$path]:-}" ]; then LC[$path]=$(awk "END{print NR}" "$path"); fi
  [ "$num" -le "${LC[$path]}" ] && continue
  bad "$f:$ln  trích dẫn  $cite  nhưng file chỉ có ${LC[$path]} dòng"
  n=$((n+1))
done < <(grep -rnoP '(?<![\w./-])(?:src|doc|spec|scripts)/[A-Za-z0-9_./-]+\.(?:cs|ts|scss|html|md|json|sql):\d+(?:-\d+)?(?:,\d+(?:-\d+)?)*' doc .claude spec --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi trích dẫn file:dòng đều nằm trong file"

# ---------------------------------------------------------------- §4c2
# Trích dẫn dạng NGẮN `ten-file.ext:NN` cũng phải giải được và nằm trong file.
#
# Thêm 2026-09-08 sau một phép đo làm lộ ra §4c chỉ phủ hơn một phần ba việc nó
# tự nhận là làm: doc/ có **416** trích dẫn dạng đầy đủ (`src/…/x.ts:12`) mà §4c
# kiểm được, và **748** trích dẫn dạng ngắn (`x.ts:12`) mà nó KHÔNG bao giờ chạm
# tới — tức 64% neo là vô hình. §4c được mô tả là "phép đo gián tiếp của tính
# đúng nội dung"; một phép đo bỏ sót hai phần ba mẫu thì không đo được điều đó.
#
# Cách giải: tên file cơ sở là DUY NHẤT trong repo cho 97% số ca, nên máy tự nối
# lại được đường dẫn — không phải viết tay 748 chỗ. Chỉ tính file CÓ THẬT trên
# đĩa: bản đầu lấy cả `git ls-files` nên các file đã xoá-chưa-commit
# (`src/FE/src/app/modules/`, `src/BE/Modules/`) tạo ra 22 ca "trùng tên" GIẢ.
#
# Ba loại lỗi, đều là lỗi thật:
#   trùng tên      — người đọc không biết là file nào, phải viết rõ đường dẫn
#   không giải được — neo trỏ file đã biến mất
#   vượt số dòng   — như §4c
section "§4  Trích dẫn dạng ngắn phải giải được và nằm trong file"
n=0
declare -A BASE=()
while IFS= read -r bf; do
  [ -f "$bf" ] || continue
  bb="${bf##*/}"
  if [ -n "${BASE[$bb]:-}" ]; then BASE["$bb"]="AMBIG"; else BASE["$bb"]="$bf"; fi
done < <({ git ls-files; git ls-files --others --exclude-standard; } 2>/dev/null | sort -u)
# File mô tả DỰ ÁN KHÁC (kind: tham-chieu) trích source của repo khác — không có
# trong cây này theo đúng thiết kế, xem §9 của CLAUDE.md.
REF_FILES=" $(grep -rl '^kind: tham-chieu$' doc spec --include='*.md' 2>/dev/null | tr '\n' ' ')"
declare -A LC2=()
while IFS=: read -r f ln cite; do
  [ -z "${cite:-}" ] && continue
  case "$HIST_FILES"  in *" $f "*) continue ;; esac
  case "$HIST_LINES"  in *" $f:$ln "*) continue ;; esac
  case "$HIST_STRIKE" in *" $f:$ln "*) continue ;; esac
  case "$REF_FILES"   in *" $f "*) continue ;; esac
  base="${cite%:*}"; num="${cite##*:}"
  # range + nối dấu phẩy: lấy số lớn nhất qua mọi vế (xem §4c)
  _max=0; _rest="$num"
  while [ -n "$_rest" ]; do
    _p="${_rest%%,*}"; [ "$_p" = "$_rest" ] && _rest="" || _rest="${_rest#*,}"
    _e="${_p##*-}"
    case "$_e" in ''|*[!0-9]*) continue ;; esac
    [ "$_e" -gt "$_max" ] && _max="$_e"
  done
  num="$_max"
  [ "$num" -eq 0 ] && continue
  tgt="${BASE[$base]:-}"
  if [ -z "$tgt" ]; then
    # Phân biệt hai ca rất khác nhau, vì cách sửa khác hẳn:
    #   tồn tại nhưng bị gitignore → neo vào thứ người khác không mở được (§4a2)
    #   không tồn tại              → neo trỏ file đã biến mất
    # Chỉ chạy `find` cho ca trượt (rất ít), nên không ảnh hưởng thời gian chạy.
    hit=$(find src doc scripts spec -name "$base" -not -path '*/node_modules/*' \
            -not -path '*/dist/*' -not -path '*/bin/*' -not -path '*/obj/*' \
            -not -path '*/.angular/*' -print -quit 2>/dev/null)
    if [ -n "$hit" ]; then
      bad "$f:$ln  trích dẫn  $cite  — \`$base\` có trên đĩa ($hit) nhưng bị .gitignore loại khỏi repo"
    else
      bad "$f:$ln  trích dẫn  $cite  — không tìm thấy file nào tên \`$base\`"
    fi
    n=$((n+1)); continue
  fi
  if [ "$tgt" = "AMBIG" ]; then
    bad "$f:$ln  trích dẫn  $cite  — có nhiều file tên \`$base\`, phải viết rõ đường dẫn"
    n=$((n+1)); continue
  fi
  [ -z "${LC2[$tgt]:-}" ] && LC2[$tgt]=$(awk "END{print NR}" "$tgt")
  [ "$num" -le "${LC2[$tgt]}" ] && continue
  bad "$f:$ln  trích dẫn  $cite  → $tgt chỉ có ${LC2[$tgt]} dòng"
  n=$((n+1))
done < <(grep -rnoP '(?<![\w./-])[A-Za-z0-9_.-]+\.(?:cs|ts|scss|html|json|sql):\d+(?:-\d+)?(?:,\d+(?:-\d+)?)*' doc .claude spec --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi trích dẫn dạng ngắn đều giải được và nằm trong file"

# ---------------------------------------------------------------- §3b
# Bảng định tuyến phải trỏ tới file THẬT SỰ nói về chủ đề đó. §4a chỉ kiểm file
# có tồn tại — một đường dẫn đúng tới file sai vẫn qua được. Luật: nếu ô chủ đề
# nêu đích danh một định danh trong dấu ``, định danh đó phải có trong file đích.
section "§3  Bảng định tuyến phải trỏ đúng chủ đề"
n=0
while IFS=$'\t' read -r target ident src; do
  [ -z "${ident:-}" ] && continue
  if [ ! -f "$target" ]; then
    # KHÔNG bỏ qua im lặng. Bản trước `continue` ở đây vì tin §4a đã lo phần
    # tồn tại — nhưng §4a chỉ bắt đường dẫn có tiền tố src/ hoặc doc/, nên một
    # dòng định tuyến trỏ `rules/entity-domain.md` lọt cả hai. Lượt nghiệm thu
    # 2026-08-23 phát hiện đúng 3 dòng như vậy trong khi gate vẫn báo PASS.
    bad "$src  định tuyến → $target nhưng file đó KHÔNG TỒN TẠI"
    n=$((n+1)); continue
  fi
  grep -qF "$ident" "$target" && continue
  bad "$src  định tuyến '$ident' → $target nhưng file đó không nhắc '$ident'"
  n=$((n+1))
done < <(awk -F'|' '
  /^\|/ && /doc\/[A-Za-z0-9_.\/-]+\.md/ && NF>2 {
    tgt=""; for(i=1;i<=NF;i++) if(match($i,/doc\/[A-Za-z0-9_.\/-]+\.md/)) tgt=substr($i,RSTART,RLENGTH)
    if(tgt=="") next
    cell=$2
    while(match(cell,/`[A-Za-z_][A-Za-z0-9_<>]*`/)) {
      id=substr(cell,RSTART+1,RLENGTH-2); cell=substr(cell,RSTART+RLENGTH)
      if(length(id)>3) print tgt "\t" id "\t" FILENAME ":" FNR
    }
  }' .claude/agents/*.md doc/README.md doc/huong_dan/quy-uoc/README.md 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi dòng định tuyến trỏ đúng chủ đề"

# ---------------------------------------------------------------- §3c
# Mọi `*.md` viết trong code span ở .claude/ phải resolve được từ một root đã
# biết. §4a chỉ bắt đường dẫn có tiền tố src/ hoặc doc/, nên `rules/x.md` hay
# `quy-uoc/y.md` (đường dẫn cụt sau khi di trú) lọt lưới hoàn toàn — lượt nghiệm
# thu 2026-08-23 tìm ra 3 dòng như vậy trong khi gate báo PASS.
section "§3  Tên file .md trong .claude/ phải resolve được"
# Quét TOÀN BỘ .claude/, gồm cả skill design. Skill design dùng tên tương đối
# theo `{DESIGN_ROOT}` (`DESIGN.md`, `Tokens/colors.md`, `Templates/Screen.md`)
# — trước đây bị loại khỏi kiểm này vì sinh ~100 báo giả, tạo ra một điểm mù:
# tham chiếu hỏng trong 9 skill design sẽ không ai bắt. Cách vá: khai luôn 2
# root mà {DESIGN_ROOT} giải ra, thay vì bỏ quét.
n=0
ROOTS=("." "doc" "doc/huong_dan" "doc/huong_dan/wiki-core" "doc/huong_dan/wiki-core/be" \
       "doc/huong_dan/wiki-core/fe" "doc/huong_dan/quy-uoc" ".claude" ".claude/agents" \
       "doc/Design" "doc/Design/Frontend/PlatformManager" ".claude/skills/design-export-figma")
while IFS=: read -r f ln ref; do
  [ -z "${ref:-}" ] && continue
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  # Placeholder trong mẫu, không phải đường dẫn thật: <x>, {x}, *, NN-, <flow>
  case "$ref" in *'<'*|*'{'*|*'*'*|*NN-*|*'|'*) continue ;; esac
  found=0
  for r in "${ROOTS[@]}"; do [ -e "$r/$ref" ] && { found=1; break; }; done
  [ "$found" -eq 1 ] && continue
  bad "$f:$ln  nhắc \`$ref\` nhưng không resolve được từ root nào"
  n=$((n+1))
done < <(grep -rnoP '`\K[A-Za-z0-9_./-]+\.md(?=`)' .claude --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi tên file .md trong .claude/ đều resolve được"

# ---------------------------------------------------------------- §4d
# Chú thích trong src/ trỏ doc/ cũng phải trỏ file có thật.
#
# Thêm 2026-09-08. Mọi mục trên đây đều đóng khung bằng `--include='*.md'`, nên
# một chú thích `/// doc/....md` trong file .cs hay .ts không bao giờ được kiểm.
# Đo được hôm đó: 4 chú thích BE trỏ `doc/huong_dan/wiki-core/be/trien-khai/` —
# thư mục KHÔNG tồn tại — trong khi gate vẫn PASS 10/10.
#
# Hai dạng đều phải bắt, vì cả hai đã xuất hiện thật:
#   ApiControllerBase.cs:17   doc/huong_dan/wiki-core/be/trien-khai/05-p4-hosting-api.md
#   AuditInterceptor.cs:47              wiki-core/be/trien-khai/04-p3-platform-persistence.md
# Dạng thứ hai viết cụt tiền tố nên còn khó thấy hơn dạng thứ nhất.
section "§4  Chú thích trong src/ trỏ doc/ phải tồn tại"
# Miễn trừ lịch sử, đối xứng với HIST_LINES của phần .md — thêm ngay trong ngày
# (2026-09-08) sau khi frontend-expert chỉ ra thiếu sót: bản đầu của mục này bắt
# cả chú thích dạng "trước trỏ X, file đã xoá", nên người sửa buộc phải diễn đạt
# vòng hoặc xoá hẳn ghi chú đi cho cổng xanh. Đó đúng là hành vi §9 sinh ra để
# ngăn — bài học "vì sao ta bỏ X" bị xoá vì gate, chứ không vì nó sai.
HIST_SRC=" $(grep -rn 'trước ở\|trước nằm ở\|đã xoá\|đã chuyển\|đã bỏ\|không còn tồn tại\|chưa từng tồn tại' src \
  --include='*.cs' --include='*.ts' --include='*.html' --include='*.scss' \
  --exclude-dir=node_modules --exclude-dir=dist --exclude-dir=bin \
  --exclude-dir=obj --exclude-dir=.angular 2>/dev/null | cut -d: -f1,2 | tr '\n' ' ')"
n=0
SRC_ROOTS=("." "doc" "doc/huong_dan" "doc/huong_dan/wiki-core")
while IFS=: read -r f ln p; do
  [ -z "${p:-}" ] && continue
  case "$HIST_SRC" in *" $f:$ln "*) continue ;; esac
  case "$p" in *'/.../'*) continue ;; esac   # đường dẫn viết tắt, không phải trích dẫn
  found=0
  for r in "${SRC_ROOTS[@]}"; do [ -e "$r/$p" ] && { found=1; break; }; done
  [ "$found" -eq 1 ] && continue
  bad "$f:$ln  chú thích trỏ  $p  — không resolve được từ root nào"
  n=$((n+1))
done < <(grep -rnoP '(?<![\w./-])(?:doc/|spec/|wiki-core/|huong_dan/|quy-uoc/)[A-Za-z0-9_./-]*\.(?:md|sql|json|dbml)\b' src \
           --include='*.cs' --include='*.ts' --include='*.html' --include='*.scss' \
           --exclude-dir=node_modules --exclude-dir=dist --exclude-dir=bin \
           --exclude-dir=obj --exclude-dir=.angular 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi chú thích trong src/ trỏ doc/ đều resolve được"

# ---------------------------------------------------------------- §7
# doc/Prototype đã xoá 2026-08-23. doc/Design là nguồn UI duy nhất.
section "§7  Không còn tham chiếu doc/Prototype/"
n=0
while IFS= read -r f; do
  [ -z "$f" ] && continue
  # 2 file ĐỊNH NGHĨA luật này buộc phải nhắc tên thư mục đã xoá.
  case "$f" in .claude/CLAUDE.md|.claude/check-docs.sh) continue ;; esac
  bad "$f  còn tham chiếu doc/Prototype/ (đã xoá)"
  n=$((n+1))
done < <(grep -rl 'doc/Prototype' doc .claude 2>/dev/null)
[ "$n" -eq 0 ] && ok "không còn tham chiếu doc/Prototype/"

# ----------------------------------------------------------------
printf '\n'
# ---------------------------------------------------------------- §10
# Mọi file doc/ phải khai 3 khoá phân loại ở frontmatter. Lý do — đo được
# 2026-09-02: 85/127 file (66%) không mang nhãn trạng thái nào, mà §4 quy định
# "không nhãn = mặc định bị coi là CHƯA XÁC MINH". Tức hai phần ba tài liệu ở
# trạng thái không ai biết có đúng không. Ba khoá biến ba câu hỏi
# phải-đọc-mới-biết thành ba câu máy đọc được:
#
#   kind      luat | tham-chieu | quyet-dinh | lich-su
#             Code có PHẢI tuân file này không. `tham-chieu` mô tả dự án KHÁC
#             (loạt be/trien-khai/ lấy hình dạng từ VNR.Successor) — core-reviewer
#             KHÔNG được coi nó là luật của repo này.
#   scope     core | du-an
#             File có đi theo khi tách CoreBase sang dự án 2 không. Thiếu nó thì
#             tới ngày tách phải đọc lại toàn bộ để quyết từng file.
#   verified  YYYY-MM-DD | chua-doi-chieu | khong-ap-dung
#             Lần cuối ĐỐI CHIẾU TOÀN BỘ file với source. `chua-doi-chieu` là giá
#             trị TRUNG THỰC, không phải lỗi — đóng dấu ngày cho file chưa ai mở
#             source ra so mới đúng là khuôn sai mà §4 cấm.
#             `khong-ap-dung` (2026-09-08) cho file KHÔNG THỂ đối chiếu: source ở
#             repo khác (kind: tham-chieu), thứ đã bị gỡ (kind: lich-su), hoặc đặc
#             tả thứ chưa xây (status: "target — not built"). KHÔNG tự khai được — mục này kiểm
#             điều kiện, vì dán nhãn đó lên một file khó đối chiếu là cách nhanh
#             nhất để làm con số đẹp lên mà không kiểm gì cả.
#
# `spec/` được tính từ 2026-09-08 — xem ghi chú ở HIST_FILES. Bốn file spec đều
# đã tự khai đủ 3 khoá từ trước, nhưng vì không mục nào quét chúng nên chúng
# không được đếm: con số "CHƯA đối chiếu" báo 1 trong khi thực tế là 5. Một chỉ
# số bỏ sót cả một khu là chỉ số nói dối theo hướng dễ chịu.
section "§10 Mọi file doc/ + spec/ phải khai kind / scope / verified"
# HIỆU NĂNG — sửa 2026-09-08. Bản trước gọi head + sed + 4×grep cho TỪNG file:
# 140 file thành ~700 lần spawn tiến trình, đo được 28 giây riêng mục này trên
# tổng 56 giây cả cổng, và một vòng quét THỨ HAI chỉ để đếm. Header file này nói
# rõ vì sao đó là lỗi nghiêm trọng: gate chậm sẽ bị bỏ qua — đúng số phận của
# scripts/fe-gate.sh. Cả mục nay gói trong MỘT lần awk, phần đếm dùng luôn kết
# quả đó. Hành vi giữ nguyên từng ly: vẫn CHỈ đọc khối frontmatter ĐẦU TIÊN (file
# khuôn mẫu doc/Design/Templates/ chứa nhiều khung, khung mẫu cố ý mang
# chua-doi-chieu như placeholder — quét cả file thì một placeholder biến cả file
# thành "chưa đối chiếu", dính thật ở Templates/Components.md).
n=0; da=0; chua=0; mien=0; MIEN_STATUS=""
while IFS=$'\t' read -r tag f msg; do
  case "$tag" in
    ERR)  bad "$f  $msg"; n=$((n+1)) ;;
    da)   da=$((da+1)) ;;
    chua) chua=$((chua+1)) ;;
    mien) mien=$((mien+1)) ;;
    # Miễn trừ mở khoá bằng khoá `status:` — VĂN TỰ DO. Script không kiểm được thứ
    # được mô tả có thật sự chưa xây hay không, nó chỉ kiểm chuỗi có chữ "not built".
    # Đây là đường thoát dễ lạm dụng nhất trong cả script: một lần sửa chữ là xong.
    # Không siết được bằng máy, nên tối thiểu phải HIỆN RA để người soi lại, thay vì
    # im lặng cộng vào cột "không áp dụng". Ghi nhận 2026-09-08 (kiểm đối kháng).
    mien_status) mien=$((mien+1)); MIEN_STATUS="$MIEN_STATUS$f"$'\n' ;;
  esac
done < <(find doc spec -name '*.md' -print0 2>/dev/null | xargs -0 awk '
function flush() {
  if (cur == "") return
  if (!hasfm) { printf "ERR\t%s\tthiếu frontmatter\n", cur; cur=""; return }
  if (kind !~ /^(luat|tham-chieu|quyet-dinh|lich-su)$/)
    printf "ERR\t%s\tkhoá kind thiếu hoặc sai giá trị\n", cur
  if (scope !~ /^(core|du-an)$/)
    printf "ERR\t%s\tkhoá scope thiếu hoặc sai giá trị\n", cur
  if (verified == "khong-ap-dung") {
    # Miễn trừ phải có LÝ DO MÁY KIỂM ĐƯỢC, không phải một câu tự nhận.
    if (kind != "tham-chieu" && kind != "lich-su" && tolower(status) !~ /not built/)
      printf "ERR\t%s\tkhong-ap-dung cần kind: tham-chieu|lich-su HOẶC status chứa not built\n", cur
    else if (kind == "tham-chieu" || kind == "lich-su") printf "mien\t\t\n"
    else printf "mien_status\t%s\t\n", cur
  } else if (verified ~ /^[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]$/) {
    printf "da\t\t\n"
  } else if (verified == "chua-doi-chieu") {
    printf "chua\t\t\n"
  } else {
    printf "ERR\t%s\tkhoá verified phải là YYYY-MM-DD, chua-doi-chieu hoặc khong-ap-dung\n", cur
  }
  cur=""
}
{ sub(/\r$/, "") }
FNR==1 { flush(); cur=FILENAME; hasfm=($0=="---"); infm=hasfm
         kind=""; scope=""; verified=""; status=""; next }
infm && $0=="---" { infm=0; next }
infm { if      (index($0,"kind: ")==1)     kind=substr($0,7)
       else if (index($0,"scope: ")==1)    scope=substr($0,8)
       else if (index($0,"verified: ")==1) verified=substr($0,11)
       else if (index($0,"status: ")==1)   status=$0
       next }
END { flush() }')
if [ "$n" -eq 0 ]; then
  tong=$((da + chua + mien))
  ok "mọi file khai đủ 3 khoá — đã đối chiếu $da · CHƯA $chua · không áp dụng $mien / $tong"
  # Chỉ CHƯA mới là con số phải giảm. Tách riêng vì trước 2026-09-08 nhóm "không áp dụng"
  # bị gộp vào đây, tạo một cái sàn không bao giờ chạm tới — và một chỉ số không bao giờ
  # đạt đích là chỉ số người ta ngừng nhìn.
  if [ -n "$MIEN_STATUS" ]; then
    printf '        (%s file miễn trừ bằng khoá `status:` — văn tự do, cần người soi:)
' "$(printf '%s' "$MIEN_STATUS" | grep -c .)"
    printf '%s' "$MIEN_STATUS" | sed 's/^/           · /'
  fi
  [ "$chua" -gt 0 ] && printf '        (%s file CHƯA đối chiếu — con số này nên giảm dần, và CÓ THỂ về 0)\n' "$chua"
fi

if [ "$FAIL" -eq 0 ]; then
  printf '\033[32m✅ PASS — tài liệu đồng bộ\033[0m\n'
else
  printf '\033[31m❌ FAIL — xem danh sách trên\033[0m\n'
fi
exit "$FAIL"
