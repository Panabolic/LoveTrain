---
status: active
authority: task-router
category: task-routing
last_reviewed: 2026-06-07
---

# Task Index

This file is a router/dashboard for LoveTrain task documents. It is not active scope by itself.

## Active

| Task | Mode | Scope Source | Notes |
| --- | --- | --- | --- |
| Release license cleanup | Implementation / Verification | [ActiveTasks/release-license-cleanup.md](./ActiveTasks/release-license-cleanup.md) | Maintain release notices, TMP default cleanup, and verified-unused graphics removal; audio evidence remains planner-owned. |
| Low-editor-touch runtime refactor | Implementation / Verification handoff | [ActiveTasks/low-editor-touch-runtime-refactor.md](./ActiveTasks/low-editor-touch-runtime-refactor.md) | Continue only when source/static checks and Unity compile/play validation constraints are clear. |

## Proposed

| Task | Status | Notes |
| --- | --- | --- |
| Architecture/Contracts promotion | proposed | Promote stable StructureMemory maps only after implementation has compile/play validation and user approval. |
| Presentation docs layer | deferred | LoveTrain currently has no Presentation HTML layer; Markdown remains the source of truth. |

## Routing Notes

- Use the current user prompt first.
- If the prompt names or clearly matches an ActiveTask, read that task document for scope.
- Use `Docs/README.md` to find task-specific technical context.
- Keep dated task outcomes in `Docs/SessionLogs/`.
