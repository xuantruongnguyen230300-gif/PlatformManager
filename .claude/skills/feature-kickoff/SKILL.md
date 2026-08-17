---
name: "feature-kickoff"
description: "Điều phối vòng đời 1 feature từ đầu tới lúc bàn giao: đọc spec nghiệp vụ nếu có, gọi design-expert nếu cần màn hình mới, spawn backend-expert + frontend-expert song song, để chúng tự kích hoạt core-reviewer khi chạm core. Điểm vào duy nhất thay vì gọi từng agent riêng lẻ."
argument-hint: "<tên feature> [mô tả ngắn] - e.g. 'CriteriaEvidence - thêm màn quản lý minh chứng cho tiêu chí'"
metadata:
  author: "platform-team"
  source: "custom"
user-invocable: true
disable-model-invocation: false
---

## User Input

```text
$ARGUMENTS
```

Bạn **BẮT BUỘC** phải xem xét user input trước khi tiếp tục (nếu không rỗng).

## Mục tiêu

Là điểm vào duy nhất khi bắt đầu 1 feature — thay vì người dùng tự nhớ trình
tự gọi `design-expert` → `backend-expert`/`frontend-expert` → `core-reviewer`,
skill này tự quyết định bước nào cần, theo đúng thứ tự, và tự nối các agent
lại với nhau qua cơ chế teammate (`SendMessage`) đã có sẵn ở từng agent —
người dùng chỉ gọi 1 lệnh, không phải gọi từng agent riêng lẻ.

## Các bước thực hiện

### 1. Resolve feature

Nếu `$ARGUMENTS` rỗng, hỏi người dùng tên feature + mô tả ngắn trước khi làm
gì tiếp — không tự đoán feature nào đang cần làm.

### 2. Phân loại Core vs Business — không đoán, hỏi nếu chưa rõ

Dùng đúng tiêu chí đã chốt trong `doc/kien-truc-core-module.md`: feature có
ý nghĩa với **mọi** sản phẩm dựng trên nền tảng (Core — vd auth, quản trị
người dùng, phân quyền) hay chỉ riêng domain nghiệp vụ hiện tại (Business —
vd 1 tính năng theo dõi/tiêu chí mới thuộc khối `Business.*`/`modules/`)?

- Rõ ràng Business → bước 3 áp dụng (bắt buộc có spec).
- Rõ ràng Core → bỏ qua bước 3, sang bước 4.
- Không chắc → **dừng lại, hỏi người dùng** — đây là quyết định ranh giới
  kiến trúc, không phải việc suy đoán được từ tên feature.

### 3. Gate spec — chỉ áp dụng cho feature Business

- Kiểm tra `spec/<feature>/business-rules.md` (và `ui-spec.md` nếu có màn
  hình mới). Nếu **không tồn tại**: dừng lại, báo người dùng cần có business
  rule trước — không tự suy diễn nghiệp vụ, không tự bịa spec để "có cái mà
  chạy tiếp". Hỏi người dùng muốn tự viết `spec/<feature>/business-rules.md`
  trước, hay mô tả nghiệp vụ ngay trong hội thoại để bạn ghi lại thành file
  đó trước khi đi tiếp.
- Nếu tồn tại → đọc, mang theo đường dẫn (không paste nguyên văn) khi giao
  việc cho `backend-expert`/`frontend-expert` ở bước 5.

### 4. Cần màn hình mới không?

- Nếu feature cần UI mới và `doc/Design/<Group>/<Project>/Screens/` chưa có
  spec cho flow này → gọi `Agent(subagent_type: "design-expert", ...)`
  trước, yêu cầu chạy pipeline cho flow này (design-expert tự nối các skill
  `/design-*` liên tiếp tới hết stage 7 - audit, rồi dừng lại xin xác nhận
  riêng trước khi export Figma — xem `.claude/agents/design-expert.md` §
  "Chạy nhiều stage liên tiếp").
- Nếu feature không có UI mới (vd thuần API/background job) hoặc đã có
  screen spec sẵn khớp yêu cầu → bỏ qua bước này.
- Không chắc có cần màn hình mới hay không → hỏi người dùng, đừng tự quyết.

### 5. Spawn backend-expert + frontend-expert song song

Gọi cả hai như teammate nền trong **cùng 1 lượt** (không tuần tự) khi cả hai
đều có việc:

```
Agent(subagent_type: "backend-expert", ...)
Agent(subagent_type: "frontend-expert", ...)
```

Prompt cho mỗi agent gồm: tên feature, mô tả ngắn, đường dẫn
`spec/<feature>/` (nếu bước 3 áp dụng), đường dẫn screen spec (nếu bước 4 áp
dụng), và nhắc rằng đối phương cũng đang chạy song song — hai agent tự trao
đổi Contract Card qua `SendMessage` theo đúng cơ chế đã có, không cần
feature-kickoff làm trung gian.

- Nếu chỉ 1 phía cần việc (vd chỉ sửa BE, FE không đổi) → chỉ spawn đúng 1
  agent, không ép agent còn lại chạy khi không có việc thật.

### 6. Không cần tự kích hoạt core-reviewer

`backend-expert`/`frontend-expert` đã tự `SendMessage`/`Agent` kích hoạt
`core-reviewer` khi việc họ làm chạm thành phần core (xem § "Sau khi hoàn
thành việc chạm tới core" trong từng agent) — feature-kickoff không lặp lại
việc này, chỉ cần đảm bảo cả hai agent nhận đủ context ở bước 5.

### 7. Tổng hợp báo cáo

Sau khi các agent (và core-reviewer nếu có) báo cáo xong, tổng hợp lại cho
người dùng: file đã tạo/sửa ở đâu (design/BE/FE), Contract Card nào còn
`DRAFT`, finding nào từ core-reviewer cần xử lý, và câu hỏi còn mở — không
tự ý làm lại hay tóm tắt sai lệch những gì từng agent đã báo cáo.

## Guardrails

- Không bao giờ tự bịa business rule khi thiếu `spec/` cho feature nghiệp vụ
  — dừng lại hỏi, đúng nguyên tắc "luôn hỏi khi thiếu dữ liệu để quyết định
  thay vì tự ý quyết định".
- Không tự quyết định ranh giới Core/Business khi không rõ — hỏi người dùng.
- Không chạy git dưới bất kỳ hình thức nào (kế thừa `.claude/CLAUDE.md`).
- Không tự động export Figma — dừng trước bước đó dù đang chạy chuỗi tự
  động, chờ xác nhận riêng của người dùng.
- Không tự sửa file `src/BE/`, `src/FE/`, `doc/Design/` ở phiên chính — mọi
  thay đổi thực hiện qua đúng agent phụ trách vùng đó.
