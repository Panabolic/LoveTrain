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

## 2026-10-06 - Comprehension Question Treated As Implementation Approval

Context:
사용자는 영구 강화 노드의 종류별 버튼과 진행 표시를 설명한 뒤 이해 여부를 물었다. 에이전트는 이해 확인을 수정 승인으로 확대하여 파일 조사와 구현 하위 에이전트 작업을 시작했다. 사용자는 구현을 중지하고 작업 규칙을 정하도록 요청했다.

Cause:
활성 구현의 맥락만으로 현재 발화의 확인/설계 논의 성격을 덮어쓰고, 작업 모드와 승인 범위를 구분하지 않았다.

Fix:
진행 중인 구현 하위 에이전트를 중지했다. 사용자 승인에 따라 AGENTS.md와 TaskBriefGuide에 확인 질문/설계 논의/구현 승인 구분 및 자동 모드 전환 금지를 명시했다. 기존 게임 변경은 이번 문서 작업에서 롤백하거나 재개하지 않았다.

Prevention:
이해했어/맞아/어때 등의 질문에는 이해한 내용만 답한다. 비단순 구현은 구체적인 TaskBrief 및 허용 범위와 명시적 구현 지시를 기준으로 수행한다. 확인 질문을 구현 위임으로 바꾸지 않으며, 중지된 작업은 명시적 재개 지시를 기다린다.
