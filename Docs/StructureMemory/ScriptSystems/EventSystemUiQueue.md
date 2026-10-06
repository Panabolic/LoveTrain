---
status: active
authority: structure-memory
category: event-system-ui-queue
last_reviewed: 2026-05-18
---

# Event System And UI Queue

## Purpose

Map random event data, weighted outcome execution, event effects, popup presentation, and the shared UI queue behavior.

## Current Structure

- `SO_Event` is the event presentation data: title, body text, and selectable choices.
- Each `SO_Event.Selection` points to a `GameEventSO`.
- `GameEventSO` owns weighted `EventRollGroup` lists and triggers one outcome from each group.
- `WeightedEventOutcome` links weight, effect logic, inline parameters, and output settings.
- `GameEffectSO` is the abstract effect logic surface; concrete effects live under `Assets/Scripts/LeeJunmo/Event/Effects/`.
- `EffectParameters` provides inline typed values and references for effect execution.
- `EventManager` owns event UI presentation, typing animation, panel animation, selection handling, result text, and close behavior.
- `GameManager.RegisterUIQueue(...)` is the public entry point for serialized gameplay-pausing UI popups.
- `GameUiQueueController` owns queue pending/processing state and event-resume bookkeeping behind `GameManager`.
- `GameSimulationController` owns the actual global time scale and physics simulation mode writes used by pause/event UI flow.

## Key Files

- `Assets/Scripts/LeeJunmo/Event/SO_Event.cs`
- `Assets/Scripts/LeeJunmo/Event/GameEventSO.cs`
- `Assets/Scripts/LeeJunmo/Event/GameEffectSO.cs`
- `Assets/Scripts/LeeJunmo/Event/EventManager.cs`
- `Assets/Scripts/LeeJunmo/GameManager.cs`
- `Assets/Scripts/LeeJunmo/GameUiQueueController.cs`
- `Assets/Scripts/LeeJunmo/GameSimulationController.cs`
- `Assets/Scripts/LeeJunmo/Event/WeightedEventOutcome.cs`
- `Assets/Scripts/LeeJunmo/Event/EventRollGroup.cs`
- `Assets/Scripts/LeeJunmo/Event/EffectParameters.cs`

## Ownership And Lifecycle

- Event start requests should call `EventManager.RequestEvent(...)` or `RandomEventStart()`, not directly open the panel.
- `EventManager.RequestEvent(...)` registers work with `GameManager.RegisterUIQueue(...)`.
- `GameManager` changes state to `Event`, asks `GameSimulationController` to pause simulation, and invokes the queued action.
- `GameUiQueueController` should stay pure queue state and should not call UI, `Time`, `Physics2D`, singletons, or scene transitions.
- `EventManager` closes the panel and calls `GameManager.CloseUI()` when the event is complete.

## English text lookup

See [Localization](./Localization.md) for the keyed CSV workflow. EventManager resolves the event and choice keys before display. GameEventSO resolves specialTextKey while effect scripts resolve result templates. Event Maker preserves serialized keys and stable choice/outcome text IDs during edits. EventTextFormatter expands data tokens after localization: choice/result text uses {reward.count}; title/body uses {choice_1.reward.count}. Probabilities come from the outcome’s own roll group. CSV import/build validation rejects stale or invalid templates.

## Extension Entry Points

- Add new event content by authoring an `SO_Event` and linking choices to `GameEventSO` assets.
- Add new event logic by deriving from `GameEffectSO` and reading values from `EffectParameters`.
- Add new weighted branches through `EventRollGroup` and `WeightedEventOutcome` assets/serialized data.
- Route new gameplay popup systems through `GameManager.RegisterUIQueue(...)` if they must pause gameplay and serialize with level-up/event UI.
- Keep popup queue ordering and current event resume state in `GameUiQueueController`; keep visual popup behavior in the requesting UI manager.

## Known Pitfalls

- Do not bypass the `GameManager` UI queue for gameplay-pausing popups; doing so can conflict with time scale and physics simulation mode.
- Do not move time/physics freeze or stage transition start into `GameUiQueueController`; it is only queue bookkeeping.
- Do not write `Time.timeScale` or `Physics2D.simulationMode` directly from new gameplay-pausing UI code; route through `GameSimulationController`.
- `EventManager` uses unscaled DOTween/timing so UI can animate while gameplay is paused.
- `GameEventSO.Trigger(...)` executes effect logic before result text assembly; effects should avoid opening additional UI directly unless explicitly designed.
- `EffectParameters` is generic and weakly typed. Document parameter meanings in effect assets or effect scripts when adding new effects.

## Promotion Candidate

This map can become a future UI/event contract if more gameplay popup systems begin sharing the queue.


## 2026-10-06 Item workbench update

2026-10-06 강화이벤트: EventObjectSpawner 60초/스테이지3회, stage1두번째테스트강화/stage2첫강화/stage3+랜덤. 붉은Station 충돌이ShowUpgradeEvent를 공유큐로실행. StageEventSchedule은 예정경계로시계진행. CancelUIQueue는강제모달닫기의pause복구, 실제Ending/Die 신규UI거부. 마지막보스시간900 도달만으로이벤트를차단하지않음. 상세/검증은 `Docs/SessionLogs/2026-10-06-item-workbench.md`.

## 2026-10-06 Distance-based event encounters

위 시간 기준은 이번 승인으로 거리 기준으로 대체됐다. EventObjectSpawner는 StageManager의 OnProgressAdvanced/OnStageStarted를 구독하고 파괴 시 해제한다. StageEventSchedule은 매 구간의 거리 25%/50%/75%에서 각 이벤트를 한 번 예약하며, 큰 프레임이 여러 경계를 넘으면 누락 없이 처리한다. 실패한 생성은 소비하지 않아 다음 주행 갱신에서 다시 시도한다. 같은 갱신에서 Spawner가 먼저 Boss 상태로 전환해도 이미 넘은 이벤트 경계는 처리한다.

첫 지정 이벤트, 첫 구간 두 번째/두 번째 구간 첫 번째 강화와 이후 강화 확률, 기존 Station 이동·충돌·공유 UI 큐는 유지한다. 경계는 오브젝트 생성 시점이며 창을 직접 여는 시점으로 변경하지 않았다. HUD 이벤트 노드는 느낌표 아이콘이며 설명/퍼센트 텍스트가 없다. 관련 결과: [세션 기록](../../SessionLogs/2026-10-06-stage-combo.md).
