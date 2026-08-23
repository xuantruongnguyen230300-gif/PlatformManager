---
name: "backend-expert"
description: "Delegate a src/BE task to the backend-expert subagent (.NET Clean Architecture + CQRS — scaffolding, entities, handlers, controllers). Use for any request that touches src/BE."
argument-hint: "<mô tả việc cần làm> - e.g. 'scaffold solution .NET lần đầu' or 'dựng API CRUD cho Criteria'"
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

Chuyển giao một yêu cầu chạm tới `src/BE/` (scaffold solution, dựng entity +
EF configuration + migration, command/query + handler, validator,
controller...) cho subagent `backend-expert` — thay vì tự xử lý trực tiếp
trong phiên chính. Subagent này mang theo toàn bộ context kiến trúc (Clean
Architecture, CQRS-lite qua MediatR, `ErrorDescriptor`, envelope response, cơ chế
bàn giao API Contract Card với `frontend-expert`) đã được ghi trong
`.claude/agents/backend-expert.md` và `doc/huong_dan/quy-uoc/README.md` +
`doc/huong_dan/quy-uoc/`.

## Các bước thực hiện

1. Nếu `$ARGUMENTS` rỗng, hỏi người dùng muốn làm gì với `src/BE` trước khi
   gọi agent.
2. Gọi subagent bằng công cụ **Agent**, `subagent_type: "backend-expert"`,
   với prompt là nguyên văn `$ARGUMENTS` cộng bối cảnh cần thiết.
3. Nếu công việc rõ ràng cần chạy song song với frontend, cân nhắc gọi cả
   `backend-expert` và `frontend-expert` cùng lúc như hai teammate nền (xem
   cơ chế `SendMessage` trong `.claude/agents/backend-expert.md` § Bàn giao
   với frontend-expert) thay vì chạy tuần tự.
4. Chuyển kết quả/báo cáo của subagent lại cho người dùng — không tự ý làm
   lại hay tóm tắt sai lệch những gì subagent đã báo cáo.

## Guardrails

- Không tự sửa file trong `src/BE/` ở phiên chính khi skill này đã được gọi
  — để `backend-expert` làm, giữ đúng phân vai FE/BE.
- Thay đổi schema DB, chạy migration lên môi trường dùng chung, hay chọn cơ
  chế auth lần đầu đều cần dừng lại hỏi người dùng — subagent đã được dặn
  điều này, đừng ép nó bỏ qua.
- **Task về hiệu năng ("chậm", "tối ưu", "thêm cache")**: thứ tự đã CHỐT là
  `query pattern → thuật toán → ĐO LẠI → cache`
  (`doc/huong_dan/wiki-core/be/11-performance-caching.md`). Không yêu cầu
  subagent thêm cache khi chưa có số đo — cache đặt trước các bước kia chỉ
  che lỗi, tạo nợ vĩnh viễn, và với dữ liệu phân quyền còn là rủi ro bảo
  mật. Nếu người dùng yêu cầu thẳng "thêm cache", chuyển nguyên văn yêu cầu
  cho subagent và để nó trình bày thứ tự + đề xuất đo trước, đừng tự bỏ qua
  bước đó giúp nó.
