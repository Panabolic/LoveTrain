---
status: active
authority: active-task-router
category: task-routing
last_reviewed: 2026-06-07
---

# Active Tasks

This folder stores task-specific scope documents for LoveTrain work that should outlive a single prompt.

Use an ActiveTask when a task has explicit scope, risks, done criteria, or verification needs that should not be rediscovered in future threads.

## Rules

- ActiveTask documents define allowed scope for a task, not global technical policy.
- Prefer one focused task document over adding scope to `Docs/CurrentTask.md`.
- Keep task documents current enough to guide work, but put dated outcomes in `Docs/SessionLogs/`.
- Use `Docs/TaskIndex.md` as the dashboard for active and proposed tasks.

## Current Active Tasks

- [low-editor-touch-runtime-refactor](./low-editor-touch-runtime-refactor.md): runtime responsibility separation while avoiding scene, prefab, Inspector wiring, ScriptableObject, asmdef, and generated Unity project file edits.
