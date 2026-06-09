---
status: active
authority: project-log
category: error-log
last_reviewed: 2026-05-18
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
Route enemy sprite visibility, color reset, and flip writes through `EnemySpritePresentation`, which no-ops when the sprite is null.

Prevention:
Any enemy code that changes optional `SpriteRenderer` presentation must use `EnemySpritePresentation` instead of direct `sprite` writes. Keep gameplay movement/damage state separate from optional visual component writes.

## 2026-05-18 - Enemy OnDisable Material Null Trap

Context:
`Enemy.Awake()` intentionally allows enemies without `SpriteRenderer` or material, but `Enemy.OnDisable()` directly reset `material.SetInt("_isHit", 0)`.

Cause:
The hit effect path had a null guard, while the disable cleanup path did not. This can throw during pooling, destruction, or special enemy cleanup and can prevent `PoolManager.UnregisterEnemy(...)` from running.

Fix:
Route hit material flag writes through `EnemyHitMaterialController.SetHit(...)`, which no-ops when the material is null. Keep the disable-time cleanup sequence behind `EnemyDisableCleanup`.

Prevention:
Any Unity lifecycle cleanup that touches optional renderer/material/collider components must use the same null-safe helper path as runtime effects. Keep `Enemy.OnDisable()` cleanup sequencing behind `EnemyDisableCleanup` so active-enemy unregister logic remains reachable even when optional presentation components are missing.
