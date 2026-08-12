# CLAUDE.md — Project-wide rules

## Git operations are reserved for the user

Git operations (`git add`, `git commit`, `git push`, `git checkout`, `git merge`, `git rebase`, `git reset`, `git stash`, branch creation/deletion, etc.) in this repository are the **user's responsibility only**.

**No skill or agent — including subagents (`frontend-expert`, `backend-expert`, `design-expert`, and any `/design-*` skill) — may run a git command while executing a task, under any circumstances.** This is not a "stop and ask permission, then proceed if approved" rule — it is an outright prohibition. Even if a task appears to need a git operation to move forward (staging changes, creating a branch, committing a milestone), do not run it.

If a task seems to require a git operation, **stop and tell the user what needs to happen** (e.g. "this needs `git checkout -b feature/x` before I can continue") — the user will run the git command themselves and let the agent/skill continue afterward.

This applies regardless of which subagent or skill is currently executing, and **overrides** any more permissive git guidance found in individual agent/skill definitions (e.g. wording that says to "ask before running git in `{FE_ROOT}`/`{BE_ROOT}`" — that wording means stop and report, never run it after asking).
