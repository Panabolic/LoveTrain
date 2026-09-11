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
