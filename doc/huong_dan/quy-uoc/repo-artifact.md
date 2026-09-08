---
kind: luat
scope: core
verified: 2026-09-06
---

# Cái gì được vào repo — artifact build, dữ liệu runtime, secret

> **File chủ** cho một câu hỏi duy nhất: *thứ này có được commit không?*
>
> **KHÔNG thuộc file này:** file runtime nằm ở đâu và dọn thế nào — đó là
> [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md).
> File này chỉ quyết định **vào git hay không**, không quyết định **nằm ở đâu**.

## 1. Luật

Ba nhóm **không bao giờ** được commit:

| Nhóm | Ví dụ | Vì sao |
|---|---|---|
| **Artifact build** | `bin/`, `obj/`, `dist/`, `node_modules/`, `.vs/` | Sinh lại được từ source. Commit chúng làm mọi lần build thành một diff giả, và làm `git status` mất tác dụng cảnh báo |
| **Dữ liệu runtime** | file người dùng upload, file export sinh ra, log | Không phải source. Xoá được khỏi working tree nhưng **không xoá được khỏi history** — dữ liệu người dùng lọt vào đó là sự cố, không phải phiền toái |
| **Secret thật** | mật khẩu/khoá của môi trường thật | Rò rỉ vĩnh viễn kể cả sau khi revert |

> ### 🚧 Ngoại lệ này ĐÃ BỊ LẬT — quyết định người dùng 2026-08-31
>
> Đoạn dưới đây mô tả luật cũ, giữ nguyên văn để thấy đã đổi gì và vì sao.
>
> ~~Ngoại lệ có chủ đích: **thông tin kết nối trỏ vào `localhost` của môi trường
> dev** được commit (`src/BE/PlatformManager.Api/appsettings.Development.json`) —
> nó không mở được gì ngoài máy người chạy, và để trong repo thì người mới clone
> về chạy được ngay.~~
>
> **Luật mới: connection string đi qua User Secrets như mọi secret khác.**
>
> Lập luận cũ không sai — `localhost` thật sự không mở được gì từ ngoài. Ba lý do
> làm nó không còn đủ:
>
> 1. **Chính solution này đã có cách làm đúng.** `BootstrapOptions` đọc mật khẩu
>    quản trị qua User Secrets, kèm docstring dài giải thích vì sao không bao giờ
>    đặt trong repo. Connection string là ngoại lệ duy nhất còn sót — một quy ước
>    có đúng một chỗ vi phạm thì chỗ đó sẽ được chép lại, không phải quy ước.
> 2. **File cấu hình Production sắp có hình dạng y hệt.** Người thêm nó sẽ nhìn
>    file dev bên cạnh làm mẫu.
> 3. **Hàng rào đã có nhưng bắn trượt.** `src/BE/.gitignore` khi đó loại trừ
>    `appsettings.*.local.json` dưới tiêu đề *"User secrets / local overrides"* —
>    khuôn đó **không khớp** `appsettings.Development.json`. Ý định đúng từ đầu,
>    chỉ khuôn sai. Khuôn cũ nay đã bị thay và **không còn trong file**; chính
>    `src/BE/.gitignore:15` là dòng ghi lại lần sửa đó.
>
> **Giá phải trả, ghi rõ để không ai ngạc nhiên:** người mới clone repo không chạy
> được ngay nữa — phải đặt connection string một lần bằng `dotnet user-secrets`.
> Đó là đánh đổi đã chấp nhận, không phải điều bị bỏ sót.
>
> Mức nghiêm trọng của bản thân việc rò rỉ: **thấp** — `Host=localhost` không ai
> ngoài Internet chạm tới. Sửa vì khuôn mẫu, không vì mật khẩu đó.
>
> **Có thật hôm nay → sẽ thành** (đối chiếu 2026-08-31):
>
> **✅ CÓ THẬT — đã xong, đối chiếu 2026-09-04.** Cả hai dòng dưới đây mô tả trạng thái
> TRƯỚC khi xử lý; nay đã đóng.
>
> | | Trước | Hôm nay (đo được) |
> |---|---|---|
> | `appsettings.Development.json` | git theo dõi, chứa connection string kèm mật khẩu | **Không còn trong index** — `git ls-files \| grep appsettings` chỉ trả `src/BE/PlatformManager.Api/appsettings.json`, và file đó không chứa secret nào |
> | `src/BE/.gitignore` | khuôn `appsettings.*.local.json` không phủ đúng file có secret | Khuôn đã phủ đúng — `git check-ignore -v src/BE/PlatformManager.Api/appsettings.Development.json` trả `src/BE/.gitignore:20  appsettings.*.json` |
>
> ⚠️ **Bước gỡ khỏi index phải do người dùng chạy** — `git rm --cached` là lệnh git ghi,
> bị `settings.json` chặn với mọi agent (§1).

Secret của môi trường thật đi qua User Secrets hoặc biến môi
trường; các khoá cụ thể và cách đặt ghi ở
`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/BootstrapOptions.cs`.

### 1.1 Máy mới clone về cần tạo gì (thêm 2026-09-08)

Hệ quả trực tiếp của luật trên, ghi ra đây vì nó là câu hỏi đầu tiên của mọi máy
mới: **repo không mang sẵn cấu hình dev nào.** `git ls-files src/BE/PlatformManager.Api`
chỉ in `appsettings.json`, và file đó chỉ có mức log + `AllowedHosts`.

Hai khoá phải tự đặt trước khi API chạy được:

| Khoá | Thiếu thì hỏng thế nào |
|---|---|
| `ConnectionStrings:Default` | Không mở được database |
| `Cors:AllowedOrigins` | Host **vẫn boot** — Development cố ý không `ValidateOnStart()` — nhưng allowlist rỗng chặn **mọi** origin, nên FE không gọi được một API nào. Ràng buộc và thông điệp lỗi ở [`src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs`](../../../src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs) |

Thêm `Bootstrap:SuperAdminPassword` + `Bootstrap:AdminPassword` **chỉ khi** chạy lệnh
seed (`--seed`); đường chạy phục vụ bình thường không cần
([`Program.cs:126`](../../../src/BE/PlatformManager.Api/Program.cs) truyền
`requireBootstrapOptions: isSeedRun`).

Hai cách đặt, chọn một — cả hai đều nằm **ngoài** repo:

```bash
# Cách 1 — User Secrets: giá trị không nằm trong cây làm việc, không thể commit nhầm
cd src/BE/PlatformManager.Api
dotnet user-secrets set "ConnectionStrings:Default" "<chuỗi kết nối Postgres của máy bạn>"
dotnet user-secrets set "Cors:AllowedOrigins:0" "<origin dev của FE trên máy bạn>"

# Cách 2 — tự tạo file appsettings.Development.json ngay cạnh appsettings.json (cùng thư mục
# vừa cd tới ở trên) với đúng hai khoá trên. Nó khớp khuôn `appsettings.*.json` ở
# src/BE/.gitignore:20 nên git không bao giờ thấy — không có nguy cơ commit nhầm.
```

⚠️ **Không chép giá trị thật vào bất kỳ file `doc/` nào** — kể cả chuỗi `localhost`. Đó
chính là khuôn đã phải gỡ ở mục trên, và đó cũng là lý do bảng này nói *khoá nào* chứ
không nói *giá trị nào*.

## 2. `.gitignore` KHÔNG cứu được file đã track — đây là cái bẫy

Luật của git: `.gitignore` chỉ áp lên file **chưa** được track. Một file đã vào
index thì tiếp tục được theo dõi vĩnh viễn, dù sau đó có thêm bao nhiêu dòng
ignore khớp với nó.

Nghĩa là một `.gitignore` **đúng** vẫn có thể đứng cạnh hàng trăm file lẽ ra
phải bị nó chặn, và không có dấu hiệu nào cảnh báo — `git status` im lặng, gate
tài liệu không đụng tới, build vẫn xanh.

Đây chính là kịch bản đã xảy ra với `src/BE/.gitignore`: file này có `bin/` và
`obj/` ngay từ đầu, nhưng artifact được commit **trước** nó nên dòng ignore
không có tác dụng gì, và tình trạng đó tồn tại cho tới khi có người chạy lệnh
§4 (commit `d20995d`, 2026-08-27). Không gate nào bắt được — nó chỉ lộ ra khi
có người đọc `git status` và thắc mắc vì sao build lại sinh diff.

## 3. Cách kiểm — bằng lệnh, không bằng bảng liệt kê

Theo [`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6, không chép vào tài
liệu thứ đếm được bằng lệnh. Chạy từ gốc repo:

```bash
# Artifact build lọt vào index — PASS khi in ra 0
git ls-files "*/bin/*" "*/obj/*" "*/node_modules/*" "*/dist/*" "*/.vs/*" | wc -l

# Dữ liệu runtime lọt vào index — PASS khi in ra 0
git ls-files "*App_Data/*" "*/logs/*" | wc -l

# Đường dẫn lẽ ra bị chặn nhưng .gitignore không khai — PASS khi không in gì
git check-ignore -q src/BE/PlatformManager.Api/App_Data || echo "App_Data CHƯA bị ignore"
```

**Tiêu chí PASS: cả ba lệnh đều sạch.** Chạy trước khi coi một thay đổi đụng
cấu trúc thư mục là xong.

## 4. Gỡ file đã lỡ track

Lệnh dưới đây gỡ khỏi index nhưng **giữ nguyên file trên đĩa** — không mất gì:

```bash
git rm -r --cached --quiet "*/bin/*" "*/obj/*"
```

Lưu ý pathspec: git diễn giải đường dẫn **tương đối với thư mục đang đứng**, nên
`"src/BE/*/bin"` chạy từ trong `src/BE/` sẽ thành `src/BE/src/BE/*/bin` và báo
`did not match any files`. Dạng bắt đầu bằng `*` ở trên đúng từ mọi thư mục con,
vì `*` trong pathspec của git khớp cả dấu `/`.

Đây là lệnh **ghi** vào repo → theo [`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md)
§1, chỉ người dùng chạy, agent không được tự chạy.

## 5. Hiện trạng — ✅ CÓ THẬT (đối chiếu 2026-09-06)

> 🔄 **LẬT 2026-09-06.** Mục này trước mang nhãn `🚧 ĐANG THI CÔNG` với hai dòng
> `🚧` mô tả `appsettings.Development.json` *"đang được track"* và khuôn
> `.gitignore` *"không khớp"* — **cả hai đã đóng từ trước**, và chính §1 của file
> này đã ghi là xong (đối chiếu 2026-09-04). Hai mô tả ngược nhau sống cạnh nhau
> trong cùng một file; bảng dưới nay là bảng *"đã đóng thế nào"*, không còn là
> việc tồn đọng. Đo lại bất cứ lúc nào bằng ba lệnh ở §3.

| Có thật hôm nay | Ghi chú |
|---|---|
| ~~Repo **không có `.gitignore` ở gốc**~~ — **sai từ 2026-08-29**: `.gitignore` gốc đã có, chứa `/Prototype/`. Kiểm: `ls .gitignore` | Giữ nguyên cách chia theo khu, nhưng mỗi khu phải đủ luật §1 |
| `appsettings.Development.json` **không còn trong index** (đối chiếu 2026-09-06). Kiểm: `git ls-files \| grep appsettings` chỉ trả `src/BE/PlatformManager.Api/appsettings.json` | Đã gỡ theo §4; connection string nay đi qua User Secrets |
| Khuôn `.gitignore` **đã phủ đúng** (đối chiếu 2026-09-06). Kiểm: `git check-ignore -v src/BE/PlatformManager.Api/appsettings.Development.json` trả `src/BE/.gitignore:20` | Khuôn `appsettings.*.json` phủ mọi file cấu hình theo môi trường; `appsettings.json` trần vẫn commit được, đúng chủ đích |
| `src/BE/.gitignore` có `bin/`, `obj/`, `.vs/`, `*.user`, `*.suo`, `appsettings.*.json`, và **`App_Data/` + `logs/`**. Kiểm bằng `cat src/BE/.gitignore` thay vì tin danh sách này (§6) | Giữ nguyên. 🔄 **LẬT 2026-09-06**: dòng này trước liệt `appsettings.*.local.json` — khuôn đó đã bị thay từ 2026-08-31 và không còn trong file |
| Artifact build **đã gỡ khỏi index** — commit `d20995d`, đối chiếu 2026-08-27: cả ba lệnh §3 về artifact đều sạch | Giữ sạch; `.gitignore` từ nay mới thật sự có tác dụng vì không còn file nào được track sẵn |
| `src/FE/.gitignore` chặn đủ `/dist`, `/node_modules`, index sạch | Giữ nguyên |

> Vì sao `src/FE` sạch còn `src/BE` thì không: `.gitignore` của FE do `ng new`
> sinh ra **cùng lúc** với project, nên chưa file nào kịp vào index trước nó.
> Phía BE thì ngược lại. Bài học áp cho mọi project mới: `.gitignore` là file
> commit **đầu tiên**, trước cả source.
