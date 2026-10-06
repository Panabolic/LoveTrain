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

## 2026-10-06 — Non-simulated pickup body did not move its Transform

격리 native Play Mode에서 `Rigidbody2D.simulated=false`인 드롭에 body.position만 대입하자 Transform이 이동하지 않아 0.5초 뒤 흡수도 일어나지 않았다. 순수/최소 stub 검사와 컴파일은 이 엔진 동작 차이를 검출하지 못했다.

RewardPickup은 body가 실제 시뮬레이션되는 경우에만 body.position을 쓰고 그 외에는 Transform.position을 갱신한다. 시뮬레이션을 끈 표시 객체의 이동은 native Update 이후 실제 Transform과 흡수 결과를 함께 검사한다. 상세 실행 근거는 [주행·보스·성장 세션](./SessionLogs/2026-10-06-driving-boss-progression.md)에 있다.

## 2026-10-06 — Default sorting hid pickups and acceleration wind

드롭과 바람을 Default에 제작하고 sortingOrder만15/25로 높였지만, 다른 Sorting Layer인 불투명 BackGround보다 먼저 그려져 가려졌다. 생성·이동·particleCount 및 UI-only 이미지 검사는 실제 세계 배경 위 가시성을 보장하지 못했다. 생산 URP의 같은 카메라/배경/재질/오브젝트로 레이어만 바꾼 A/B에서 Default 변화픽셀0→ForeGround 변화픽셀양수를 확인했다.

월드 표시를 추가할 때는 sortingOrder뿐 아니라 SortingLayer 순서와 실제 불투명 배경을 함께 확인한다. 실제 프로젝트 render pipeline을 유지한 배경 포함 렌더에서 표시를 검증하며, pipeline을 끈 UI-only 이미지나 객체 존재를 가시성 성공으로 확대하지 않는다. 수정·증거는 [주행·보스·성장 세션](./SessionLogs/2026-10-06-driving-boss-progression.md)의 후속 항목을 따른다.

## 2026-10-06 — Tooltip center calculation used with a corner pivot

영구 강화 툴팁은 프리팹 pivot(0,1)인데 C# 위치와 경계 검사는 중심 pivot(0.5,0.5)을 가정했다. 260×170 툴팁이 의도보다 오른쪽130/아래85 logical unit 밀리고 화면 경계 밖으로 나갔다. 참조/GUID/계층 검사와 소스 컴파일만으로는 이 표시 오류를 검출하지 못했다. 마우스 선택 이벤트도 버튼 기준 위치로 호버 위치를 덮어썼다.

툴팁 배치는 실제 pivot·anchor·부모 rect를 함께 확인하고 공간이 부족한 축의 펼침 방향과 pivot을 함께 바꾼다. 커서를 따라가는 설계일 때는 포인터 선택의 PointerEventData를 보존한다. 영구 강화는 이후 사용자 요청으로 버튼 중심 고정 방식이므로 호버·클릭·키보드 모두 card.TransformPoint(card.rect.center)를 사용하며 포인터 이동을 구독하지 않는다. transform.position은 상단 pivot에서 버튼 중심이 아니다. 위치 검증은 실제 production 계산으로 중앙·가장자리·모서리의 bounds와 기준점 간격을 확인하고, native Unity 표시 검증의 실행 여부를 별도로 보고한다.
