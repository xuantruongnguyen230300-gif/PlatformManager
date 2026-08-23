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
cd "$(dirname "$0")/.." || exit 2

FAIL=0
section() { printf '\n\033[1m== %s ==\033[0m\n' "$1"; }
bad()     { printf '  \033[31mFAIL\033[0m  %s\n' "$1"; FAIL=1; }
ok()      { printf '  \033[32mOK\033[0m    %s\n' "$1"; }

# File mang banner "TÀI LIỆU LỊCH SỬ" (§5) mô tả trạng thái QUÁ KHỨ — đường dẫn
# và mốc thời gian trong đó cố ý không còn đúng. Miễn trừ khỏi §4.
HIST_FILES=" $(grep -rl 'TÀI LIỆU LỊCH SỬ' doc .claude --include='*.md' 2>/dev/null | tr '\n' ' ')"

# Dòng nói rõ nó đang nhắc tới thứ đã biến mất cũng được miễn trừ khỏi §4 — nếu
# không, mọi ghi chép "vì sao ta bỏ X" đều bị báo lỗi, và bài học sẽ bị xoá đi
# cho gate xanh. Đó chính là hành vi §4 tồn tại để ngăn.
HIST_LINES=" $(grep -rn 'trước ở\|trước nằm ở\|đã xoá\|đã chuyển\|đã bỏ\|không còn tồn tại\|chưa từng tồn tại' doc .claude --include='*.md' 2>/dev/null | cut -d: -f1,2 | tr '\n' ' ')"

# ---------------------------------------------------------------- §1
# Bảng cấm git trong CLAUDE.md §1 phải khớp permissions.deny trong settings.json.
# §1 tự tuyên bố lệnh cấm "được cưỡng chế bằng máy" — nếu văn bản liệt kê một
# lệnh mà deny không chặn, câu đó thành lời hứa suông.
# Đã trượt HAI lần: 2026-08-21 (văn bản cấm cả lệnh đọc, deny thì không) và
# 2026-08-23 (văn bản thêm restore/pull/tag/config/worktree/submodule, deny
# không theo). Hai lần là đủ để cưỡng chế bằng máy thay vì bằng trí nhớ.
section "§1  Bảng cấm git phải khớp settings.json"
n=0
while IFS= read -r cmd; do
  [ -z "$cmd" ] && continue
  # `remote *` được deny khai theo từng subcommand (remote add/rm/set-url/rename)
  case "$cmd" in *' '*|remote) continue ;; esac
  grep -q "Bash(git $cmd:" .claude/settings.json && continue
  bad "CLAUDE.md §1 cấm \`git $cmd\` nhưng settings.json deny KHÔNG chặn"
  n=$((n+1))
done < <(grep -m1 '🛑 \*\*CẤM\*\*' .claude/CLAUDE.md | grep -oP '`\K[a-z-]+(?= \*)|`\K[a-z-]+(?=`)')
[ "$n" -eq 0 ] && ok "mọi lệnh git trong bảng cấm đều được deny chặn"

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
done < <(grep -rnoP '\]\(\K[^)]+' doc .claude --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi link nội bộ đều resolve được"

# ---------------------------------------------------------------- §4a
# Trích dẫn src/... hoặc doc/... phải trỏ file có thật. Đây là thứ bắt được
# "RequirePermissionFilter.cs", "import-dialog", "rules/performance.md" — các
# file được viện dẫn làm bằng chứng nhưng không tồn tại.
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
done < <(grep -rnoP '(?<![\w./-])(?:src|doc)/[A-Za-z0-9_./-]+\.(?:cs|ts|scss|html|md|json|sql|dbml|sh)\b' doc .claude --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi đường dẫn được trích dẫn đều tồn tại"

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
done < <(grep -rn 'ĐÃ CÓ\|✅ Xong\|✅ XONG\|FIXED\|Đã bật\|đã triển khai\|đã verify' doc --include='*.md' 2>/dev/null)
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
  case "$num" in ''|*[!0-9]*) continue ;; esac
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  [ -f "$path" ] || continue          # file không tồn tại đã do §4a báo
  if [ -z "${LC[$path]:-}" ]; then LC[$path]=$(wc -l < "$path"); fi
  [ "$num" -le "${LC[$path]}" ] && continue
  bad "$f:$ln  trích dẫn  $cite  nhưng file chỉ có ${LC[$path]} dòng"
  n=$((n+1))
done < <(grep -rnoP '(?<![\w./-])(?:src|doc)/[A-Za-z0-9_./-]+\.(?:cs|ts|scss|html|md|json|sql):\d+' doc .claude --include='*.md' 2>/dev/null)
[ "$n" -eq 0 ] && ok "mọi trích dẫn file:dòng đều nằm trong file"

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
if [ "$FAIL" -eq 0 ]; then
  printf '\033[32m✅ PASS — tài liệu đồng bộ\033[0m\n'
else
  printf '\033[31m❌ FAIL — xem danh sách trên\033[0m\n'
fi
exit "$FAIL"
