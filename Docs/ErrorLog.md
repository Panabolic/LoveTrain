---
status: active
authority: project-log
category: error-log
last_reviewed: 2026-07-23
---

# Error Log

This file records recurring implementation errors, causes, fixes, and prevention rules.

## Template

```md
## YYYY-MM-DD - Short error name

Context:

Cause:

Fix:

Prevention:
```

## Active Entries

## 2026-05-18 - Enemy Sprite Direct Access Null Trap

Context:
`Enemy.Awake()` intentionally allows enemies without `SpriteRenderer`, and `CreditEnemy` relies on that path, but enemy lifecycle and movement code still directly wrote `sprite.enabled`, `sprite.color`, or `sprite.flipX`.

Cause:
Optional sprite handling was added in some setup paths, but later death, enable, and movement presentation writes did not consistently use a null-safe helper.

Fix:
Guard optional sprite visibility and flip writes at their lifecycle and movement call sites. Death and pooling completion must not depend on a SpriteRenderer being present.

Prevention:
Any enemy code that changes an optional `SpriteRenderer` must use a local null guard or an already-established null-safe presentation boundary. Keep gameplay movement and cleanup separate from optional visual writes.

## 2026-05-18 - Enemy OnDisable Material Null Trap

Context:
`Enemy.Awake()` intentionally allows enemies without `SpriteRenderer` or material, but `Enemy.OnDisable()` directly reset `material.SetInt("_isHit", 0)`.

Cause:
The hit effect path had a null guard, while the disable cleanup path did not. This can throw during pooling, destruction, or special enemy cleanup and can prevent `PoolManager.UnregisterEnemy(...)` from running.

Fix:
Guard the optional material reset in `Enemy.OnDisable()` and keep `PoolManager.UnregisterEnemy(...)` reachable regardless of material presence.

Prevention:
Any Unity lifecycle cleanup that touches optional renderer, material, or collider components must guard those accesses without returning before required unregister or state cleanup work.

## 2026-07-23 - Queued UI And Ending Boundary Null Trap

Context:
Event and level-up UI callbacks execute after being queued, while ending input and credit spawning depend on optional runtime devices and authored collections.

Cause:
The delayed callbacks dereferenced singleton, UI, selection, and item references without revalidating them. Ending code directly accessed `Keyboard.current` and assumed credit lists and spawn points were present.

Fix:
Reject invalid event requests before queueing, close an already-started queued UI through the existing `CloseUI()` path when required data is missing, validate level-up singleton and item inputs at callback time, and guard ending input and credit data access.

Prevention:
Validate delayed-callback dependencies when the callback runs, validate indexes before mutating UI state, and treat input devices and authored collections as optional at runtime. Do not add a global catch that hides failures or changes normal queue ordering.
