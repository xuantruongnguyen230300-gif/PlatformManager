---
name: "frontend-expert"
description: "Delegate a src/FE task to the frontend-expert subagent (Angular 20 — architecture, scaffolding, screens, components, services). Use for any request that touches src/FE."
argument-hint: "<mô tả việc cần làm> - e.g. 'scaffold app Angular 20 lần đầu' or 'dựng màn hình danh sách chỉ tiêu'"
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

Chuyển giao một yêu cầu chạm tới `src/FE/` (scaffold app, dựng màn hình,
tạo component/service, chuẩn hoá model, style theo token...) cho subagent
`frontend-expert` — thay vì tự xử lý trực tiếp trong phiên chính. Subagent
này mang theo toàn bộ context kiến trúc (Angular 20, cấu trúc feature, ranh
giới DTO/model, cơ chế bàn giao API Contract Card với `backend-expert`) đã
được ghi trong `.claude/agents/frontend-expert.md` và
`src/FE/CLAUDE.md` + `src/FE/.claude/docs/`.

## Các bước thực hiện

1. Nếu `$ARGUMENTS` rỗng, hỏi người dùng muốn làm gì với `src/FE` trước khi
   gọi agent.
2. Gọi subagent bằng công cụ **Agent**, `subagent_type: "frontend-expert"`,
   với prompt là nguyên văn `$ARGUMENTS` cộng bối cảnh cần thiết (file/thư
   mục liên quan nếu người dùng đã nhắc tới trong hội thoại).
3. Nếu công việc rõ ràng cần chạy song song với backend (vd. "dựng màn hình
   X và API cho nó"), cân nhắc gọi cả `frontend-expert` và `backend-expert`
   cùng lúc như hai teammate nền (xem cơ chế `SendMessage` trong
   `.claude/agents/frontend-expert.md` § Bàn giao cho backend-expert) thay
   vì chạy tuần tự.
4. Chuyển kết quả/báo cáo của subagent lại cho người dùng — không tự ý làm
   lại hay tóm tắt sai lệch những gì subagent đã báo cáo (file đã tạo/sửa,
   câu hỏi còn mở, bước tiếp theo).

## Guardrails

- Không tự sửa file trong `src/FE/` ở phiên chính khi skill này đã được gọi
  — để `frontend-expert` làm, giữ đúng phân vai FE/BE.
- Nếu subagent báo cáo bị chặn vì thiếu endpoint backend, đó là hành vi
  đúng — không tự ý "giúp" bằng cách chế API contract thay backend.
