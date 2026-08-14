---
name: "core-reviewer"
description: "Delegate a core-compliance review to the core-reviewer subagent — audits src/BE and/or src/FE against doc/huong_dan/wiki-core/ rules, reports PASS/PARTIAL/MISSING with evidence, never edits code."
argument-hint: "<phạm vi cần review> - e.g. 'review core BE' or 'review core FE sau khi thêm entity mới'"
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

Chuyển giao một yêu cầu audit phần "core" của `src/BE` và/hoặc `src/FE` cho
subagent `core-reviewer` — thay vì tự xử lý trực tiếp trong phiên chính.
Subagent này mang theo toàn bộ context về bộ quy tắc chuẩn
(`doc/huong_dan/wiki-core/`), quy trình review PASS/PARTIAL/MISSING và cơ
chế bàn giao findings cho `backend-expert`/`frontend-expert` đã được ghi
trong `.claude/agents/core-reviewer.md`.

## Các bước thực hiện

1. Nếu `$ARGUMENTS` rỗng, hỏi người dùng muốn review phạm vi nào (BE, FE,
   hay cả hai) trước khi gọi agent.
2. Gọi subagent bằng công cụ **Agent**, `subagent_type: "core-reviewer"`,
   với prompt là nguyên văn `$ARGUMENTS` cộng bối cảnh cần thiết (vd. feature
   nào vừa được `backend-expert`/`frontend-expert` hoàn thành, nếu người
   dùng đã nhắc tới trong hội thoại).
3. Nếu `backend-expert`/`frontend-expert` đang chạy như teammate nền, ưu
   tiên để chính agent đó tự `SendMessage` kích hoạt `core-reviewer` sau khi
   hoàn thành việc chạm core (xem mục "Sau khi hoàn thành việc chạm tới
   core" trong `.claude/agents/backend-expert.md`) — skill này dùng khi
   người dùng muốn kích hoạt review độc lập, không qua 2 agent kia.
4. Chuyển kết quả/báo cáo của subagent lại cho người dùng — không tự ý làm
   lại hay tóm tắt sai lệch những gì subagent đã báo cáo (đường dẫn file
   report, số finding theo mức, agent nào cần xử lý).

## Guardrails

- `core-reviewer` **không có quyền `Edit`** — không yêu cầu nó tự sửa code
  dù người dùng gợi ý; nếu có finding cần sửa, việc đó thuộc về
  `backend-expert`/`frontend-expert`.
- Không tự sửa file trong `src/BE/`/`src/FE/` ở phiên chính khi skill này đã
  được gọi — giữ đúng phân vai reviewer/implementer.
- Không làm nhẹ một finding đã thất bại để báo cáo "đẹp" hơn — subagent đã
  được dặn điều này, đừng ép nó bỏ qua.
