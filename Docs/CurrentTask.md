---
status: deprecated
authority: deprecated-compatibility-notice
category: task-routing
last_reviewed: 2026-06-07
---

# CurrentTask.md Is Deprecated

`Docs/CurrentTask.md` is no longer the active task-scope source for LoveTrain.

Use the new routing model instead:

- Current user prompt or Task Brief: first source for the current thread's scope.
- `Docs/ActiveTasks/<task-id>.md`: active task scope when a matching task document exists.
- `Docs/TaskIndex.md`: router/dashboard for active or proposed task documents.
- `Docs/README.md`: technical documentation router.

The previous low-editor-touch runtime refactor scope was moved to:

- `Docs/ActiveTasks/low-editor-touch-runtime-refactor.md`

Do not add new task scope here. This file remains only as a compatibility notice for old links and historical references.

## Replacement Flow

1. Start from the user prompt and identify the work mode.
2. If a matching ActiveTask exists, read it for scope.
3. If not, treat the prompt or Task Brief as the scope for this thread.
4. Use `Docs/TaskIndex.md` only to find or register task documents.
5. Use `Docs/README.md` to route into StructureMemory, RefactorLog, SessionLogs, DecisionLog, or ErrorLog.
