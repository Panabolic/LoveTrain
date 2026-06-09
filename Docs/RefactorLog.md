---
status: active
authority: project-log
category: refactor-log
last_reviewed: 2026-05-18
---

# 리팩터링 로그

이 문서는 LoveTrain에서 발견한 리팩터링 후보, 구조적 부채, 정리 필요 지점을 한국어로 기록합니다.

## 사용 기준

- 지금 당장 고치지 않지만 추후 구조 개선이 필요한 항목을 기록합니다.
- 단순 TODO가 아니라, 현재 문제와 리팩터링 목표가 분명한 경우에만 추가합니다.
- 씬, 프리팹, 직렬화 필드, `MonoBehaviour`, `ScriptableObject` 변경 위험이 있으면 반드시 함께 적습니다.
- 반복 실수나 버그 예방 규칙은 이 문서보다 `ErrorLog.md`를 우선합니다.

## 항목 템플릿

```md
## YYYY-MM-DD - 제목

상태:
제안 / 진행 중 / 일부 완료 / 해결

현재 문제:

왜 지금 남겨두는가:

목표 구조:

Unity 위험:

리팩터링 시작 조건:

관련 문서/파일:
```

## 2026-05-18 - 기차 속도/체력 책임 분리

상태:
일부 완료

현재 문제:
기존 `Train`은 속도-체력 수치, 빈사/회복/사망 상태 판단, DOTween 연출, 코루틴, 조작 비활성화, UI 표시, 씬 전환까지 함께 처리했습니다.

왜 지금 남겨두는가:
출시 유지보수를 위해 우선 속도-체력 상태 계산을 `TrainSpeedHealth`로 분리했습니다. 다만 `Train`은 아직 데미지 이벤트, 사망 연출, 스폰 제어, 사운드, UI, 씬 전환까지 함께 조율합니다.

목표 구조:
`TrainSpeedHealth`는 순수 상태와 전이 규칙만 담당하고, `Train`은 기존 공개 API와 Unity 연출 파사드만 담당합니다. 이후 단계에서는 사망 연출, 스폰 제어, 사운드 호출, 게임오버 전환을 더 작은 협력 객체로 나누는 방향을 검토합니다.

Unity 위험:
`Train`은 씬에 붙은 `MonoBehaviour`이므로 공개 필드, `[SerializeField]` 이름, 메서드 시그니처 변경은 프리팹/씬 연결과 외부 호출부를 깨뜨릴 수 있습니다. 이번 변경은 직렬화 필드 이름과 공개 API를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 및 플레이 모드에서 피격, 회복, 빈사, 사망, 엔딩 진입 흐름을 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/Train.cs`
- `Assets/Scripts/LeeJunmo/TrainSpeedHealth.cs`
- `Docs/StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md`

## 2026-05-18 - 아이템 쿨다운 상태 분리

상태:
일부 완료

현재 문제:
기존 `ItemInstance`는 아이템 런타임 데이터, 장착된 오브젝트 참조, 업그레이드 위임, 시각 업그레이드, 게임 상태 확인, 쿨다운 감소, 수동 쿨다운 대기까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 게임 흐름을 흔들지 않기 위해 쿨다운 타이머 상태만 `ItemCooldownState`로 분리했습니다. `ItemInstance`는 아직 `GameManager` 상태 확인, `Item_SO` 훅 호출, 실체화 오브젝트 업그레이드, 시각 교체를 함께 담당합니다.

목표 구조:
`ItemCooldownState`는 순수 타이머와 수동 쿨다운 대기 상태만 담당하고, `ItemInstance`는 기존 공개 필드/메서드를 유지하는 런타임 아이템 파사드로 남깁니다. 이후에는 게임 상태 게이트, 아이템 훅 실행, 시각 업그레이드 적용을 더 좁은 협력 객체로 나눌 수 있습니다.

Unity 위험:
`ItemInstance`는 `[System.Serializable]`이며 `Inventory.items`에 들어가는 런타임 구조입니다. 이번 변경은 `currentCooldown`, `maxCooldown`, `currentUpgrade`, `itemData` 공개 필드와 기존 메서드 시그니처를 유지했습니다.

리팩터링 시작 조건:
자동 쿨다운 아이템과 수동 쿨다운 아이템, 특히 Bloody Bible 장판 생성/소멸 후 쿨다운 재시작 흐름을 Unity에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/Items/ItemInstance.cs`
- `Assets/Scripts/LeeJunmo/Items/ItemCooldownState.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 아이템 시각 업그레이드 fallback 분리

상태:
일부 완료

현재 문제:
`ItemInstance`는 아이템 런타임 상태와 업그레이드 위임뿐 아니라, `IInstantiatedItem`이 없는 오브젝트의 Animator 컨트롤러/스프라이트 교체까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 prefab이나 ScriptableObject asset을 건드리지 않고, 기존 fallback 시각 교체 로직만 `ItemVisualUpgradeApplier`로 분리했습니다. 아이템별 `IInstantiatedItem` 구현과 데이터 구조는 그대로 유지했습니다.

목표 구조:
아이템별 업그레이드 반응은 각 instantiated item behaviour가 담당하고, 공통 fallback 시각 교체는 `ItemVisualUpgradeApplier`가 담당합니다. `ItemInstance`는 어떤 경로를 호출할지만 결정하는 파사드로 좁힙니다.

Unity 위험:
`Item_SO.controllersByLevel`과 `Item_SO.spritesByLevel`은 asset 데이터입니다. 이번 변경은 필드 이름, 타입, asset 값을 변경하지 않았고, 기존 순서처럼 Animator 컨트롤러를 먼저 적용한 뒤 스프라이트를 적용합니다.

리팩터링 시작 조건:
IInstantiatedItem이 없는 단순 시각 아이템이 실제 프로젝트에 존재하는지 확인하고, 해당 아이템의 레벨업 시각 변경을 Unity에서 검증한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/Items/ItemInstance.cs`
- `Assets/Scripts/LeeJunmo/Items/ItemVisualUpgradeApplier.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 인벤토리 조회 책임 분리

상태:
일부 완료

현재 문제:
`Inventory`는 MonoBehaviour로서 프레임별 아이템 tick, 킬/타격 훅 전달, 아이템 획득/업그레이드, UI 변경 알림, 아이템 조회와 최대 레벨 판정까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 외부 호출부와 Inspector 데이터를 건드리지 않기 위해 `Inventory.FindItem(...)`, `Inventory.GetUpgradableItems(...)`, `Inventory.IsItemMaxed(...)` 공개 메서드는 유지하고 내부 조회 계산만 `InventoryItemQuery`로 분리했습니다.

목표 구조:
`Inventory`는 Unity lifecycle과 공개 파사드, 변경 이벤트를 담당하고, `InventoryItemQuery`는 보유 아이템 조회, 업그레이드 가능 목록, 최대 레벨 판정 같은 순수 query 규칙을 담당합니다.

Unity 위험:
`Inventory.items`는 공개 리스트이며 씬/프리팹에서 보일 수 있는 런타임 목록입니다. 이번 변경은 리스트 필드와 기존 공개 메서드 시그니처를 유지했습니다.

리팩터링 시작 조건:
레벨업 UI, 이벤트 보상, 랜덤 업그레이드 효과가 기존처럼 `Inventory` 공개 메서드를 통해 동작하는지 Unity에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/Inventory/Inventory.cs`
- `Assets/Scripts/LeeJunmo/Inventory/InventoryItemQuery.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 인벤토리 런타임 dispatch 책임 분리

상태:
일부 완료

현재 문제:
`Inventory`는 Unity lifecycle 진입점과 공개 API 역할뿐 아니라, 매 프레임 아이템 tick 반복, 처치 훅 반복, 타격 훅 반복을 직접 수행했습니다.

왜 지금 남겨두는가:
이번 단계에서는 외부 호출부를 바꾸지 않기 위해 `Inventory.Update()`, `Inventory.ProcessKillEvent(...)`, `Inventory.ProcessHitEvent(...)` 진입점은 그대로 두고 반복 dispatch만 `InventoryItemRuntimeDispatcher`로 분리했습니다.

목표 구조:
`Inventory`는 MonoBehaviour 파사드와 변경 이벤트를 담당하고, `InventoryItemRuntimeDispatcher`는 보유 아이템 리스트를 순회하며 tick, kill hook, hit hook을 전달하는 책임만 담당합니다.

Unity 위험:
`Inventory`는 씬에 붙는 MonoBehaviour일 수 있으므로 공개 메서드와 `items` 필드는 유지했습니다. 이번 변경은 내부 loop 위치만 옮겼습니다.

리팩터링 시작 조건:
플레이 중 아이템 쿨다운 tick, 적 처치 아이템 효과, 투사체/레이저 타격 아이템 효과가 기존처럼 호출되는지 Unity에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/Inventory/Inventory.cs`
- `Assets/Scripts/LeeJunmo/Inventory/InventoryItemRuntimeDispatcher.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 레벨업 선택지 선정 책임 분리

상태:
일부 완료

현재 문제:
`LevelUpUIManager`는 UI 패널/슬롯 표시뿐 아니라, 전체 아이템 목록에서 획득 가능/강화 가능 아이템을 필터링하고 랜덤 3개를 선택하는 게임 규칙까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 scene-facing UI 연결을 건드리지 않기 위해 `ShowLevelUpChoices()` 진입점과 슬롯 연결은 유지하고, 선택지 필터링과 랜덤 선택만 `LevelUpChoiceSelector`로 분리했습니다.

목표 구조:
`LevelUpUIManager`는 패널 표시와 슬롯 바인딩을 담당하고, `LevelUpChoiceSelector`는 레벨업 후보 필터링, 랜덤 선택, 향후 희귀도/가중치/보장 규칙의 진입점을 담당합니다.

Unity 위험:
`LevelUpUIManager`는 UI 오브젝트와 슬롯을 `[SerializeField]`로 참조합니다. 이번 변경은 serialized field 이름, 타입, public entry point를 유지했습니다.

리팩터링 시작 조건:
레벨업 시 신규 아이템, 기존 아이템 업그레이드, 모든 아이템 max 상태에서 UI skip 흐름을 Unity에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/LevelUp/LevelUpManager.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/LevelUpChoiceSelector.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 인벤토리 획득/업그레이드 실행 책임 분리

상태:
일부 완료

현재 문제:
`Inventory`는 공개 API와 UI 변경 알림뿐 아니라, 기존 아이템 여부 확인, 기존 아이템 업그레이드, 신규 `ItemInstance` 생성, 리스트 추가, 장착 실행까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 외부 호출부를 바꾸지 않기 위해 `Inventory.AcquireItem(...)`과 `Inventory.UpgradeItemInstance(...)`는 유지하고, 실제 획득/업그레이드 실행만 `InventoryItemAcquirer`로 분리했습니다.

목표 구조:
`Inventory`는 MonoBehaviour 파사드와 `OnInventoryChanged` 알림을 담당하고, `InventoryItemAcquirer`는 런타임 아이템 리스트에 대한 획득/업그레이드 실행 규칙을 담당합니다.

Unity 위험:
`Inventory.items`, `AcquireItem(...)`, `UpgradeItemInstance(...)`는 이벤트/레벨업 UI에서 사용됩니다. 이번 변경은 필드와 public method 시그니처를 유지했습니다.

리팩터링 시작 조건:
레벨업 선택, 이벤트 보상, 랜덤 업그레이드가 기존처럼 신규 획득/업그레이드/최대 레벨 skip을 처리하는지 Unity에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/Inventory/Inventory.cs`
- `Assets/Scripts/LeeJunmo/Inventory/InventoryItemAcquirer.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 레벨업 선택지 표시 상태 계산 분리

상태:
일부 완료

현재 문제:
`LevelUpChoiceUI`는 UI 텍스트/스프라이트 바인딩뿐 아니라, 새 아이템인지 업그레이드인지 판단하고 현재 레벨/다음 레벨/MAX 표시 여부를 직접 계산했습니다.

왜 지금 남겨두는가:
이번 단계에서는 UI 오브젝트 연결을 건드리지 않기 위해 `DisplayChoice(...)` 시그니처와 serialized field는 유지하고, 표시 상태 계산만 `LevelUpChoiceDisplayState`로 분리했습니다.

목표 구조:
`LevelUpChoiceUI`는 실제 UI 요소 바인딩과 활성화만 담당하고, `LevelUpChoiceDisplayState`는 신규/업그레이드 상태와 레벨 표시 규칙을 담당합니다.

Unity 위험:
`LevelUpChoiceUI`는 여러 UI Image, TextMeshPro, GameObject 참조를 `[SerializeField]`로 들고 있습니다. 이번 변경은 필드 이름, 타입, public method 시그니처를 유지했습니다.

리팩터링 시작 조건:
레벨업 선택지에서 새 아이템, 업그레이드 아이템, 다음 레벨 MAX 표시가 기존처럼 보이는지 Unity에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/LevelUp/LevelUpChoiceUI.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/LevelUpChoiceDisplayState.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 기차 XP/레벨 진행 상태 분리

상태:
일부 완료

현재 문제:
`TrainLevelManager`는 MonoBehaviour 진입점, 경험치 총량, 현재 레벨, 다음 레벨 요구량, 레벨업 반복 처리, 이벤트 발행, 레벨업 UI 큐 등록을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 적 스케일링, 무기 레벨 반응, 레벨 UI가 사용하는 `TrainLevelManager` 공개 프로퍼티와 이벤트를 유지하면서, 순수 경험치/레벨 상태만 `TrainLevelProgression`으로 분리했습니다. UI 큐 등록과 이벤트 발행은 아직 `TrainLevelManager`에 남아 있습니다.

목표 구조:
`TrainLevelProgression`은 XP 총량, 레벨 구간, 경험치 벽, 진행률, 레벨 증가 상태만 담당합니다. `TrainLevelManager`는 기존 공개 API, Unity lifecycle, `OnExperienceGained`, `OnLevelUp`, `GameManager.RegisterUIQueue(...)` 호출을 담당하는 파사드로 유지합니다.

Unity 위험:
`TrainLevelManager`는 씬에 붙는 MonoBehaviour일 수 있고 `baseRequiredXP`, `levelPeriodStep`, `experienceWalls`는 `[SerializeField]`입니다. 이번 변경은 필드 이름, 타입, 공개 프로퍼티, 이벤트 시그니처를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 경험치 획득, 한 번에 여러 레벨업, 레벨업 UI 큐 순차 표시, 적 HP 스케일링, 총기 레벨 반응을 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/LevelUp/TrainLevelManager.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/TrainLevelProgression.cs`
- `Docs/StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md`

## 2026-05-18 - 게임 UI 큐 상태 분리

상태:
일부 완료

현재 문제:
`GameManager`는 전역 게임 상태, 시간/물리 정지, 보스 사망 분기, 재시작, 종료뿐 아니라 UI 요청 큐의 pending 상태, 처리 중 여부, 이벤트 종료 후 복귀 상태까지 직접 들고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 외부 호출부를 바꾸지 않기 위해 `GameManager.RegisterUIQueue(...)`와 `GameManager.CloseUI()`는 유지하고, 큐 자료구조와 처리 상태만 `GameUiQueueController`로 분리했습니다. 시간 정지, 물리 모드 전환, 상태 변경, 스테이지 전환 시작은 아직 `GameManager`가 담당합니다.

목표 구조:
`GameUiQueueController`는 UI 요청 순서, 처리 중 여부, 이벤트 종료 후 복귀할 상태만 담당합니다. `GameManager`는 public facade, `GameState` 변경, `Time.timeScale`, `Physics2D.simulationMode`, `StageManager` 호출을 담당합니다.

Unity 위험:
`GameManager`는 `DontDestroyOnLoad` 싱글톤이고 여러 시스템이 직접 참조합니다. 이번 변경은 public 메서드, public 필드, 이벤트, enum을 유지했고, 씬/프리팹/Inspector 연결은 변경하지 않았습니다.

리팩터링 시작 조건:
Unity 컴파일 후 이벤트 팝업, 레벨업 팝업, 팝업 연속 큐, 팝업 중 보스 사망 후 스테이지 전환 예약, 일시정지 중 큐 처리 보류를 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/GameManager.cs`
- `Assets/Scripts/LeeJunmo/GameUiQueueController.cs`
- `Docs/StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md`
- `Docs/StructureMemory/ScriptSystems/EventSystemUiQueue.md`

## 2026-05-18 - 처치 카운터 상태 분리

상태:
일부 완료

현재 문제:
`GameManager`는 전역 상태와 UI 큐뿐 아니라 일반 적, 엘리트 적, 보스 처치 수와 총합 계산까지 직접 들고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 적 스크립트와 엔딩 UI가 사용하는 `GameManager.AddKillCount(...)`, `GameManager.AddBossKillCount()`, 처치 수 공개 프로퍼티를 유지하고, 내부 카운터 저장과 증가만 `GameKillCounter`로 분리했습니다.

목표 구조:
`GameKillCounter`는 일반/엘리트/보스/총합 처치 수와 reset/add 규칙만 담당합니다. `GameManager`는 기존 public facade와 씬 로드 시 reset 호출을 담당합니다.

Unity 위험:
`GameManager`는 여러 적과 UI가 직접 참조하는 싱글톤입니다. 이번 변경은 public 메서드와 public 프로퍼티 이름을 유지했고, 씬/프리팹/Inspector 연결은 변경하지 않았습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적 처치, 엘리트 처치, 보스 처치, 엔딩 결과 UI의 카운트 표시를 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/GameManager.cs`
- `Assets/Scripts/LeeJunmo/GameKillCounter.cs`
- `Docs/StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 아이템 런타임 GameState 조건 분리

상태:
일부 완료

현재 문제:
`ItemInstance`, `PoisonMissileLauncher`, `RearGun`, `Revolver`가 모두 `Playing`, `Boss`, `Ending` 상태에서만 동작한다는 조건을 각자 직접 검사했습니다.

왜 지금 남겨두는가:
이번 단계에서는 각 아이템의 `Update()` 흐름과 예외 처리, public API, serialized field를 유지하고, 공통 상태 조건만 `ItemRuntimeGameStatePolicy`로 분리했습니다.

목표 구조:
아이템 런타임 허용 상태는 `ItemRuntimeGameStatePolicy`가 담당하고, 개별 아이템은 실제 쿨다운, 발사, 애니메이션, 입력 반응만 담당합니다.

Unity 위험:
아이템 런타임 허용 상태를 바꾸면 쿨다운, 자동 발사 아이템, 엔딩 중 아이템 동작이 달라질 수 있습니다. 이번 변경은 허용 상태를 기존과 동일하게 `Playing`, `Boss`, `Ending`으로 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 기본 아이템 쿨다운, 독 미사일, 후방 총, 리볼버가 기존 상태에서만 동작하는지 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/Items/ItemRuntimeGameStatePolicy.cs`
- `Assets/Scripts/LeeJunmo/Items/ItemInstance.cs`
- `Assets/Scripts/LeeJunmo/Items/PoisonMissileLauncher.cs`
- `Assets/Scripts/LeeJunmo/Items/RearGun.cs`
- `Assets/Scripts/LeeJunmo/Items/Revolver.cs`
- `Docs/StructureMemory/ScriptSystems/ItemsInventoryWeapons.md`

## 2026-05-18 - 전역 시간/물리 시뮬레이션 제어 분리

상태:
일부 완료

현재 문제:
`GameManager`, `SceneLoader`, `Option`이 `Time.timeScale`과 `Physics2D.simulationMode` 복구/정지 값을 직접 설정했습니다. 이 값은 이벤트 UI, 일시정지, 재시작, 씬 로드 흐름에 걸쳐 있어서 한 곳이 누락되면 게임이 멈춘 채 남을 수 있습니다.

왜 지금 남겨두는가:
이번 단계에서는 언제 멈추고 재개할지는 기존 호출부가 그대로 결정하게 두고, 실제 전역 시뮬레이션 값 적용만 `GameSimulationController`로 분리했습니다.

목표 구조:
`GameSimulationController`는 `Resume()`과 `Pause()`로 `Time.timeScale`과 `Physics2D.simulationMode`를 적용합니다. `GameManager`, `SceneLoader`, `Option`은 기존 public entry point와 흐름 결정을 유지합니다.

Unity 위험:
전역 시간/물리 설정은 모든 런타임 시스템에 영향을 줍니다. 이번 변경은 값 자체를 기존과 동일하게 유지했습니다: resume은 `timeScale = 1f`, `FixedUpdate`; pause는 `timeScale = 0f`, `Script`.

리팩터링 시작 조건:
Unity 컴파일 후 일시정지/재개, 이벤트 UI 열림/닫힘, 옵션 재시작, 씬 로드 후 물리 복구를 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/LeeJunmo/GameSimulationController.cs`
- `Assets/Scripts/LeeJunmo/GameManager.cs`
- `Assets/Scripts/LeeJunmo/SceneLoader.cs`
- `Assets/Scripts/LeeJunmo/Option.cs`
- `Docs/StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md`
- `Docs/StructureMemory/ScriptSystems/EventSystemUiQueue.md`

## 2026-05-18 - 스폰 페이즈 선택 규칙 분리

상태:
일부 완료

현재 문제:
`Spawner`는 serialized 스폰 페이즈 데이터, 현재 시간에 맞는 페이즈 선택, 실제 몹/엘리트/보스 스폰 실행, 스폰 위치 계산, 이벤트 스폰 태스크를 모두 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 씬/Inspector 데이터와 스폰 실행 흐름을 건드리지 않기 위해 `Spawner.SpawnPhase` 구조체와 `spawnPhases` 필드는 유지하고, 현재 시간에 맞는 페이즈를 찾는 순수 규칙만 `SpawnerPhaseSelector`로 분리했습니다.

목표 구조:
`SpawnerPhaseSelector`는 현재 게임 시간 기준으로 가장 늦게 시작된 유효 페이즈를 선택합니다. `Spawner`는 선택된 페이즈 값을 내부 current index/rate 필드에 적용하고 실제 스폰 실행을 담당합니다.

Unity 위험:
`Spawner.SpawnPhase`는 serialized 구조체이므로 필드 이름과 타입 변경은 Inspector 데이터를 깨뜨릴 수 있습니다. 이번 변경은 serialized 필드와 public method를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 시간 경과에 따른 일반 몹/비행 몹/엘리트 몹 인덱스 범위와 스폰 간격 변경을 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerPhaseSelector.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 보스 순서 커서 상태 분리

상태:
일부 완료

현재 문제:
`Spawner`는 보스 순서 배열, 다음 보스 인덱스 상태, 보스 등장 시퀀스 시작, 경고 UI, 보스 오브젝트 생성까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 보스 스폰 실행 흐름과 serialized `bossSequence` 배열을 건드리지 않고, 다음 보스를 순환 선택하는 인덱스 상태만 `SpawnerBossSequenceCursor`로 분리했습니다.

목표 구조:
`SpawnerBossSequenceCursor`는 보스 순서 배열에서 다음 보스를 선택하고 다음 인덱스를 예약합니다. `Spawner`는 선택된 보스의 코루틴/경고/생성 흐름을 유지합니다.

Unity 위험:
`bossSequence`는 Inspector에서 설정되는 serialized 배열입니다. 이번 변경은 필드 이름, 타입, public spawn method를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 보스가 설정된 순서대로 순환 등장하는지, 빈 bossSequence에서 경고만 출력하고 스폰을 건너뛰는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossSequenceCursor.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 보스 설정 조회 책임 분리

상태:
일부 완료

현재 문제:
`Spawner`는 보스 순서, 보스 경고/등장 코루틴, 오브젝트 생성뿐 아니라 `bossSettings` 배열에서 보스별 설정을 찾고 누락 시 기본 설정을 만드는 규칙까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 Inspector에서 설정되는 `BossSpawnSetting` 구조와 `bossSettings` 필드를 건드리지 않고, 조회와 fallback 생성만 `SpawnerBossSettingLookup`으로 분리했습니다. 보스 생성 흐름과 경고 UI 호출은 그대로 `Spawner`에 남아 있습니다.

목표 구조:
`SpawnerBossSettingLookup`은 보스 이름에 맞는 설정 조회와 기존 3.0초 fallback 생성만 담당합니다. `Spawner`는 serialized 데이터 보관, 코루틴, 경고 UI, 보스 프리팹 생성, 등장 연출 실행을 담당합니다.

Unity 위험:
`Spawner.BossSpawnSetting`은 serialized 중첩 클래스이므로 필드 이름과 타입 변경은 Inspector 데이터를 깨뜨릴 수 있습니다. 이번 변경은 필드와 public spawn method를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 설정이 있는 보스와 설정이 없는 보스의 경고 대기 시간, spawn/arrival 위치 적용, 수동 `SpawnBoss(...)` 호출 흐름을 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossSettingLookup.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 스폰 위치 선택 책임 분리

상태:
일부 완료

현재 문제:
`Spawner`는 몹/이벤트/배치 스폰 실행뿐 아니라 비행 몹 스폰 영역 선택, 지상 전방/후방 포인트 후보 구성, 후방 스폰 허용 여부 반영, fallback 위치 선택까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 씬에 배치된 `Transform[]`, `BoxCollider2D[]` 직렬화 필드와 실제 적 생성 흐름을 그대로 두고, 위치 선택 규칙만 `SpawnerSpawnPositionSelector`로 분리했습니다.

목표 구조:
`SpawnerSpawnPositionSelector`는 지상/비행 스폰 위치 선택과 spawner fallback 위치 반환만 담당합니다. `Spawner`는 spawn point/area 데이터를 보관하고, 풀에서 적을 가져와 위치 지정/물리 초기화/이벤트 연결을 수행합니다.

Unity 위험:
스폰 포인트와 박스 콜라이더 배열은 씬/Inspector 데이터입니다. 이번 변경은 필드 이름, 타입, 배열 사용 방식, 비행 몹 area fallback 규칙, 지상 rear spawn 조건을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 몹, 비행 몹, 후방 스폰 on/off, 이벤트 배치 스폰이 기존 위치 범위에서 생성되는지 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerSpawnPositionSelector.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 몹 스폰 타입/인덱스 선택 책임 분리

상태:
일부 완료

현재 문제:
`Spawner`는 현재 페이즈의 인덱스 범위를 들고 있으면서, 기본 몹/엘리트 몹이 지상인지 비행인지 랜덤으로 고르고 해당 범위에서 인덱스를 뽑는 규칙까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `SpawnPhase` 직렬화 필드와 `PoolManager` 호출 흐름을 유지하고, 랜덤 타입/인덱스 선택만 `SpawnerMobSpawnSelector`로 분리했습니다. 실제 풀 조회, 위치 지정, 물리 초기화, 사망 콜백 연결은 `Spawner`에 남겨 두었습니다.

목표 구조:
`SpawnerMobSpawnSelector`는 현재 페이즈 범위에서 기본/엘리트 몹의 지상/비행 타입과 인덱스만 선택합니다. `Spawner`는 선택 결과로 적을 가져와 스폰 실행을 담당합니다.

Unity 위험:
`Spawner.SpawnPhase`는 serialized 구조체이고, 인덱스 범위는 `PoolManager` 배열 순서와 맞아야 합니다. 이번 변경은 필드 이름, 범위 계산, 랜덤 임계값, pool lookup 호출 위치를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 기본 몹 지상/비행, 엘리트 지상/비행, 각 페이즈별 인덱스 범위가 기존처럼 적용되는지 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerMobSpawnSelector.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 주기적 이벤트 스폰 타이머 책임 분리

상태:
일부 완료

현재 문제:
`Spawner`는 이벤트 효과가 등록한 주기적 스폰 task 목록을 보관하면서, interval 최소값 보정, deltaTime 누적, 스폰 시점 판정, 타이머 초기화까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 외부 이벤트 효과가 호출하는 `AddPeriodicSpawnTask(...)` 공개 API와 실제 prefab 스폰 실행을 유지하고, 타이머 규칙만 `SpawnerPeriodicSpawnScheduler`로 분리했습니다.

목표 구조:
`SpawnerPeriodicSpawnScheduler`는 task 생성 시 interval clamp, 매 프레임 timer advance, due 판정, reset만 담당합니다. `Spawner`는 task 리스트 소유, `PoolManager` prefab 조회, 위치 지정, 물리 초기화를 담당합니다.

Unity 위험:
이벤트 효과가 `Spawner.AddPeriodicSpawnTask(...)`를 호출합니다. 이번 변경은 공개 메서드 시그니처, `PeriodicSpawnTask` 중첩 클래스 필드, 최소 interval 0.1초, 스폰 후 reset 흐름을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 주기적 몹 스폰 이벤트가 interval 보정, 반복 스폰, 스폰 정지 상태에서 기존처럼 동작하는지 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerPeriodicSpawnScheduler.cs`
- `Assets/Scripts/LeeJunmo/Event/Effects/Effect_SpawnMobPeriodically.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 적 피격 머티리얼 상태 쓰기 분리

상태:
일부 완료

현재 문제:
`Enemy`는 피격 이펙트 코루틴과 `OnDisable()`에서 `_isHit` 머티리얼 플래그를 직접 변경했습니다. 그런데 `Awake()`는 SpriteRenderer/material이 없는 적도 허용하므로, disable cleanup에서 null 예외가 날 수 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 `Enemy`의 공개/직렬화 필드와 생명주기 메서드는 유지하고, `_isHit` 쓰기만 `EnemyHitMaterialController`로 분리했습니다. 체력, 경험치, 킬 이벤트, PoolManager 등록/해제 흐름은 그대로 유지했습니다.

목표 구조:
`EnemyHitMaterialController`는 optional material에 대한 hit flag 적용만 담당합니다. `Enemy`는 데미지 처리, 코루틴 시작, 사망/비활성화, active enemy 등록/해제 파사드로 남깁니다.

Unity 위험:
적 프리팹마다 SpriteRenderer/material 구성이 다를 수 있습니다. 이번 변경은 필드 이름, public API, pooling call site를 바꾸지 않았고, material이 없을 때만 hit flag 쓰기를 건너뜁니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적, SpriteRenderer 없는 특수 적, CreditEnemy, pooling 비활성화 시 active enemy 등록 해제가 정상 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyHitMaterialController.cs`
- `Docs/ErrorLog.md`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 활성 적 레지스트리/정리 반복 분리

상태:
일부 완료

현재 문제:
`PoolManager`는 prefab pool 관리, 동적 풀 생성, boss prefab 조회뿐 아니라 active enemy 리스트의 등록/해제, 전체 despawn, boss 제외 despawn 반복까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 외부 아이템과 시스템이 직접 읽는 `PoolManager.activeEnemies` 공개 리스트와 `RegisterEnemy(...)`, `UnregisterEnemy(...)`, `DespawnAllEnemies(...)`, `DespawnAllEnemiesExceptBoss()` 공개 메서드를 유지하고, 리스트 mutation/iteration만 `PoolActiveEnemyRegistry`로 분리했습니다.

목표 구조:
`PoolActiveEnemyRegistry`는 active enemy 리스트의 중복 등록 방지, 제거, 역순 despawn, boss 제외 필터만 담당합니다. `PoolManager`는 prefab pool, singleton lifecycle, public facade 역할을 유지합니다.

Unity 위험:
`activeEnemies`는 여러 아이템이 직접 참조합니다. 이번 변경은 필드 이름, 타입, public method를 유지했고, boss 제외 기준도 기존처럼 `GetComponent<Boss>()`를 사용합니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적 등록/해제, CreditEnemy 해제, 보스 사망 시 일반 적 정리, 아이템들의 activeEnemies 조회가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolManager.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolActiveEnemyRegistry.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 풀 오브젝트 재사용/생성 책임 분리

상태:
일부 완료

현재 문제:
`PoolManager`는 serialized prefab 배열과 public getter 역할뿐 아니라 pool list 초기화, 인덱스 범위 확인, dynamic pool dictionary 생성/조회, 비활성 오브젝트 재사용, 없을 때 instantiate와 이름 설정까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `PoolManager`의 prefab 배열, public getter, boss prefab 조회, singleton lifecycle은 유지하고, 반복되는 pool object 제공 규칙만 `PoolObjectProvider`로 분리했습니다.

목표 구조:
`PoolObjectProvider`는 pool list 생성, prefab 배열 인덱스 확인, dynamic pool lookup, 첫 비활성 오브젝트 재사용, instantiate-on-miss, `prefab.name` 유지, pool append만 담당합니다. `PoolManager`는 어떤 prefab group을 사용할지 결정하는 public facade로 남깁니다.

Unity 위험:
풀링은 모든 몹/이벤트 스폰 흐름에 영향을 줍니다. 이번 변경은 prefab 배열 필드, public getter 시그니처, 부모 transform, 생성 오브젝트 이름, pool append 순서를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반/비행/엘리트/이벤트 prefab 스폰에서 기존 오브젝트 재사용과 새 생성, active 상태 복구, hierarchy parent가 유지되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolManager.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolObjectProvider.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 적/보스 HP 보정 수식 분리

상태:
일부 완료

현재 문제:
`Mob`, `Boss`, `Tentacle`이 각각 base HP, 게임 시간/레벨, `PoolManager`의 `hpIncrease`, `eventDebuff`, 엘리트 배율을 섞어 HP 보정 수식을 직접 계산했습니다.

왜 지금 남겨두는가:
이번 단계에서는 각 적의 생명주기, `calibratedMaxHP` 저장 위치, singleton 접근 위치, serialized stat 필드를 유지하고, 수식 계산만 `EnemyHpCalibration`으로 분리했습니다.

목표 구조:
`EnemyHpCalibration`은 보스 레벨 기반 HP와 일반 적/촉수의 시간 기반 HP 보정 수식만 담당합니다. 각 `Enemy` 파생 클래스는 언제 계산할지와 계산 결과를 어디에 저장할지만 담당합니다.

Unity 위험:
HP 보정은 난이도와 밸런스에 직접 영향을 줍니다. 이번 변경은 기존 수식, 분 단위 시간 버림, event debuff 배율, elite multiplier, null guard 위치를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 몹, 엘리트 몹, 보스, 촉수의 HP가 기존 밸런스와 동일하게 적용되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EnemyHpCalibration.cs`
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/Boss.cs`
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 보스 등장 이동 보간 수식 분리

상태:
일부 완료

현재 문제:
`Boss`는 등장 상태 플래그와 코루틴 제어뿐 아니라, 등장 이동 중 `SmoothStep`/`Lerp` 보간 수식까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `Boss.StartEntranceRoutine(...)`, `isEntranceActive`, 코루틴 시작/종료, 최종 위치 설정은 유지하고, 프레임별 위치 계산만 `BossEntranceMotion`으로 분리했습니다.

목표 구조:
`BossEntranceMotion`은 시작점, 목표점, 경과 시간, duration으로 보간 위치만 계산합니다. `Boss`는 coroutine lifecycle과 패턴 봉인 상태를 담당합니다.

Unity 위험:
보스 등장 연출은 `Spawner.BossSpawnSetting`의 arrival point와 duration에 의존합니다. 이번 변경은 public method와 serialized setting을 바꾸지 않고 기존 보간 수식을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 arrival point가 있는 보스 등장 연출과 arrival point가 없는 즉시 패턴 시작 흐름을 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Boss.cs`
- `Assets/Scripts/SangHyup/Enemy/BossEntranceMotion.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 보스 등장 코루틴 시퀀스 분리

상태:
일부 완료

현재 문제:
`Boss.EntranceMoveRoutine(...)`은 등장 중 패턴 봉인 플래그 설정, 프레임별 이동 loop, 목표 위치 보정, 플래그 해제를 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `Boss.StartEntranceRoutine(...)`, `isEntranceActive` 의미, `Spawner`의 entrance handoff, 보간 수식, 최종 위치 설정 순서를 바꾸지 않고 코루틴 시퀀스만 `BossEntranceRoutine`으로 분리했습니다.

목표 구조:
`Boss`는 public entrance entry point와 상태 저장을 유지합니다. `BossEntranceRoutine`은 등장 시퀀스 순서를 담당하고, 실제 보간 위치 계산은 `BossEntranceMotion`을 계속 사용합니다.

Unity 위험:
보스 등장 시퀀스는 보스 패턴 봉인 시간, arrival point 도착, 패턴 시작 가능 시점에 직접 영향을 줍니다. 이번 변경은 기존 순서처럼 active flag를 먼저 켜고, 프레임 이동 후 목표 위치로 보정한 다음 active flag를 끕니다.

리팩터링 시작 조건:
Unity 컴파일 후 arrival point가 있는 보스 등장에서 이동 중 패턴이 봉인되고, 도착 후 패턴이 다시 시작되는지 확인합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Boss.cs`
- `Assets/Scripts/SangHyup/Enemy/BossEntranceRoutine.cs`
- `Assets/Scripts/SangHyup/Enemy/BossEntranceMotion.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss 촉수 레지스트리 책임 분리

상태:
일부 완료

현재 문제:
`EyeBoss`는 패턴 코루틴과 촉수 생성뿐 아니라, 생성된 촉수 리스트의 중복 등록 방지, 제거, 전체 제거, 광폭화 패턴 중 남은 촉수 여부 판정까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `Tentacle`이 호출하는 `EyeBoss.RegisterTentacle(...)`/`UnregisterTentacle(...)` 공개 메서드와 serialized prefab 필드는 유지하고, 리스트 규칙만 `EyeBossTentacleRegistry`로 분리했습니다.

목표 구조:
`EyeBossTentacleRegistry`는 spawned tentacle 리스트의 등록, 해제, 전체 destroy/clear, 활성 여부 판정만 담당합니다. `EyeBoss`는 패턴 코루틴, 촉수 생성, 데미지/딜레이 선택, 보스 상태를 담당합니다.

Unity 위험:
촉수 destroy/OnDestroy/unregister 순서는 패턴 종료와 보스 사망 정리에 영향을 줍니다. 이번 변경은 기존 역순 destroy, null skip, list clear, 중복 방지, remove-if-present 방식을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 광폭화 패턴 중 촉수 조기 전멸, 보스 사망 시 촉수 정리, 촉수 OnDestroy 해제가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentacleRegistry.cs`
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss 촉수 공격 사운드 쿨다운 분리

상태:
일부 완료

현재 문제:
`EyeBoss`는 촉수 공격 사운드를 publish하면서 0.1초 중복 재생 방지 규칙도 직접 계산했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `SoundEventBus.Publish(...)` 호출은 `EyeBoss`에 유지하고, 재생 가능 여부 판정은 `EyeBossAttackSoundCooldown`으로, 마지막 재생 시각 저장은 `EyeBossAttackSoundCooldownState`로 분리했습니다.

목표 구조:
`EyeBossAttackSoundCooldown`은 현재 시간과 마지막 재생 시간으로 사운드 재생 가능 여부만 판단합니다. `EyeBossAttackSoundCooldownState`는 마지막 재생 시각 저장과 갱신을 담당합니다. `EyeBoss`는 실제 사운드 발행 진입점만 유지합니다.

Unity 위험:
사운드 쿨다운은 보스 패턴 체감에 영향을 줄 수 있습니다. 이번 변경은 기존 `Time.time - lastPlayTime > 0.1f` 조건, 초기 마지막 재생 시각 `-10f`, 사운드 발행 후 timestamp 갱신 순서를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 여러 촉수가 동시에 공격할 때 사운드 중복 억제가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossAttackSoundCooldown.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossAttackSoundCooldownState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss 촉수 패턴 인덱스 계획 분리

상태:
일부 완료

현재 문제:
`EyeBoss`의 side/center/enrage 패턴 코루틴은 보스 상태와 wait timing뿐 아니라, 촉수를 생성할 인덱스 목록과 약점 촉수 인덱스 목록까지 직접 구성했습니다.

왜 지금 남겨두는가:
이번 단계에서는 코루틴 흐름, 촉수 prefab 참조, Instantiate, Tentacle.Setup 호출은 `EyeBoss`에 유지하고, 패턴별 인덱스 계획만 `EyeBossTentaclePatternPlanner`로 분리했습니다.

목표 구조:
`EyeBossTentaclePatternPlanner`는 side/center/enrage 패턴의 spawn index와 weak point index plan만 만듭니다. `EyeBoss`는 plan을 받아 실제 촉수 생성과 패턴 timing을 담당합니다.

Unity 위험:
인덱스 규칙은 보스 패턴 난이도와 직접 연결됩니다. 이번 변경은 기존 side 좌/우 그룹, center 2-5, enrage 전체 spawn point와 `Random.Range(1, 7)` 기반 약점 3칸 규칙을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss side/center/enrage 패턴의 촉수 위치와 약점 위치가 기존처럼 생성되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentaclePatternPlanner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss 페이즈/넉백 규칙 분리

상태:
일부 완료

현재 문제:
`TrainBoss`는 보스 이동, 피격 처리, 페이즈 2 전환, 콜라이더 교체, 애니메이터 트리거, 넉백 쿨다운 판정, 넉백 힘 선택, 스턴 코루틴, 사망 흐름까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 Inspector와 씬 연결을 건드리지 않기 위해 serialized phase/collider/knockback 필드와 실제 `Rigidbody2D` force 적용, collider switching, coroutine 흐름은 `TrainBoss`에 유지하고, 순수 규칙만 helper로 분리했습니다.

목표 구조:
`TrainBossPhaseTransition`은 phase 2 진입 조건만 담당하고, `TrainBossPhaseState`는 phase 2 진입 여부 저장을 담당합니다. `TrainBossKnockbackPolicy`는 넉백 허용 시간과 적용 force 선택만 담당합니다. `TrainBoss`는 Unity 컴포넌트 제어와 보스 lifecycle 파사드로 남깁니다.

Unity 위험:
페이즈 전환과 넉백은 보스 난이도와 충돌 판정에 직접 영향을 줍니다. 이번 변경은 serialized 필드, collider 참조, animator trigger 이름, stun coroutine, force 적용 위치를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss 피격, phase 2 진입, collider 전환, 넉백 쿨다운, phase별 넉백 force 차이가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossPhaseTransition.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossPhaseState.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossKnockbackPolicy.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 적 SpriteRenderer presentation 쓰기 분리

상태:
일부 완료

현재 문제:
`Enemy.Awake()`는 SpriteRenderer가 없는 적도 허용하지만, `Enemy.Die()`, `Mob.Die()`, `Boss.OnEnable()`, `Mob`/`FlyMob`/`TrainBoss`의 방향 전환 코드는 `sprite.enabled`나 `sprite.flipX`를 직접 변경했습니다.

왜 지금 남겨두는가:
이번 단계에서는 적 lifecycle, 이동, 사망, 충돌, 데미지 규칙은 그대로 두고 optional SpriteRenderer에 대한 presentation write만 `EnemySpritePresentation`으로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`EnemySpritePresentation`은 sprite visibility, color reset, flip write만 담당합니다. `Enemy`, `Boss`, `Mob`, `FlyMob`, `TrainBoss`는 언제 보이고 숨기고 뒤집을지만 결정하는 gameplay/presentation 파사드로 남깁니다.

Unity 위험:
SpriteRenderer가 없는 특수 적, 일반 몹 사망 비활성화, 보스 활성화, 이동 중 sprite flip 체감에 영향을 줄 수 있습니다. 이번 변경은 기존 조건과 호출 위치를 유지하고 null일 때만 write를 건너뜁니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 몹, 비행 몹, TrainBoss, SpriteRenderer 없는 특수 적의 활성화/사망/방향 전환이 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EnemySpritePresentation.cs`
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/Boss.cs`
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/FlyMob.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Docs/ErrorLog.md`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 적 화면 진입/타겟 가능 판정 분리

상태:
일부 완료

현재 문제:
`Enemy.CheckScreenEntry()`는 `hasEnteredScreen` 상태 전환뿐 아니라 카메라 bounds 계산, Collider bounds 우선 판정, Sprite bounds fallback, Sprite/Collider가 없는 경우 viewport 점 판정까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `Enemy.IsTargetable` 의미와 `hasEnteredScreen` 상태 저장은 그대로 유지하고, 화면 진입 여부 계산만 `EnemyScreenEntryChecker`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`EnemyScreenEntryChecker`는 카메라/콜라이더/스프라이트/점 fallback 기반 화면 진입 판정만 담당합니다. `Enemy`는 언제 판정할지와 한 번 진입하면 targetable 상태가 열리는 lifecycle state를 담당합니다.

Unity 위험:
적이 언제 타겟 가능해지는지는 무기 타겟팅, CreditEnemy, 화면 밖 스폰, 적 피격 가능 타이밍에 영향을 줄 수 있습니다. 이번 변경은 기존 bounds 계산 순서와 true 전환 조건을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적, 비행 적, SpriteRenderer 없는 특수 적, CreditEnemy가 화면 진입 전후로 기존처럼 타겟 가능 상태가 열리는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyScreenEntryChecker.cs`
- `Assets/Scripts/LeeJunmo/CreditEnemy.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 적 플레이어 타겟 조회 분리

상태:
일부 완료

현재 문제:
`Enemy.Awake()`는 자신의 SpriteRenderer/Collider/Animator 초기화뿐 아니라 `Player` 태그 오브젝트 탐색, `Rigidbody2D`, `TrainLevelManager` 조회까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `targetRigid`, `levelManager` protected 필드와 하위 클래스 사용 방식은 유지하고, 플레이어 오브젝트와 컴포넌트 조회만 `EnemyTargetResolver`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`EnemyTargetResolver`는 현재 방식 그대로 `GameObject.FindWithTag("Player")`와 컴포넌트 조회만 담당합니다. `Enemy`는 조회된 값을 저장하고 하위 클래스 lifecycle/movement/combat에서 사용하는 파사드로 남깁니다.

Unity 위험:
플레이어 태그, Rigidbody2D, TrainLevelManager 참조는 적 이동, HP 스케일링, 경험치 지급, CreditEnemy 타겟팅에 영향을 줄 수 있습니다. 이번 변경은 조회 순서와 null fallback을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적, 비행 적, 보스, CreditEnemy가 플레이어 참조를 기존처럼 찾고 이동/HP/경험치 흐름이 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyTargetResolver.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - 적 사망 보상 dispatch 분리

상태:
일부 완료

현재 문제:
`Enemy.Die()`는 사망 상태 전환, 경험치 지급, 인벤토리 kill hook 전달, sprite 숨김, kill particle 생성, coroutine timing을 한 메서드에서 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 사망 coroutine과 presentation 순서는 `Enemy`에 유지하고, XP 지급과 `Inventory.ProcessKillEvent(...)` 전달만 `EnemyDeathRewardDispatcher`로 분리했습니다. 씬/프리팹/Inspector 데이터와 공개 API는 변경하지 않았습니다.

목표 구조:
`EnemyDeathRewardDispatcher`는 적 사망 시 보상/kill hook 전달만 담당합니다. `Enemy`는 언제 죽는지, 어떤 presentation을 정리할지, 파티클과 coroutine timing을 담당합니다.

Unity 위험:
경험치 지급과 kill hook은 레벨업 UI, 처치 반응 아이템, 인벤토리 효과에 영향을 줍니다. 이번 변경은 기존 순서인 XP 지급 후 inventory kill hook 호출을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적 처치, XP 획득, 처치 반응 아이템, 레벨업 UI 큐 등록이 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyDeathRewardDispatcher.cs`
- `Assets/Scripts/LeeJunmo/Inventory/Inventory.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Mob/FlyMob velocity 수식 분리

상태:
일부 완료

현재 문제:
`Mob.FixedUpdate()`와 `FlyMob.FixedUpdate()`는 생존/스턴/사망 상태 판단뿐 아니라, 사망 후 왼쪽 슬라이드 속도, 지상 몹 x축 이동 속도, 비행 몹 normalized 이동 속도 수식까지 직접 계산했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `FixedUpdate`, `moveDirection`, `isAlive`, `isStunned`, `Rigidbody2D.linearVelocity` assignment는 기존 클래스에 유지하고, velocity 수식만 `MobVelocityPlanner`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`MobVelocityPlanner`는 사망 슬라이드, 지상 몹, 비행 몹 velocity 계산만 담당합니다. `Mob`과 `FlyMob`은 상태 판단, 방향 선택, 실제 Rigidbody2D 적용을 담당합니다.

Unity 위험:
몹 이동 속도와 사망 후 화면 밖으로 빠지는 연출은 게임 난이도와 체감에 직접 영향을 줍니다. 이번 변경은 기존 사망 속도 30.0f, 지상 y속도 유지, 비행 normalized 이동 수식을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 몹 이동, 비행 몹 이동, 스턴 중 정지, 사망 후 왼쪽 슬라이드가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/FlyMob.cs`
- `Assets/Scripts/SangHyup/Enemy/MobVelocityPlanner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Mob/FlyMob direction 수식 분리

상태:
일부 완료

현재 문제:
`Mob.SetMoveDirection(...)`와 `FlyMob.SetMoveDirection(...)`는 sprite flip 적용과 함께, 지상 몹의 x축 좌/우/정지 판정과 비행 몹의 `diveDistance` 기반 수평/돌진 방향 판정을 직접 계산했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `FixedUpdate`, `SetMoveDirection(...)` 진입점, `moveDirection` 저장, sprite flip, `Rigidbody2D` 적용은 기존 클래스에 유지하고, 방향 수식만 `MobDirectionPlanner`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`MobDirectionPlanner`는 지상 몹과 비행 몹의 방향 선택 수식만 담당합니다. `Mob`과 `FlyMob`은 상태 판단, 방향 재계산 타이밍, sprite flip 적용, 실제 Rigidbody2D velocity 적용을 담당합니다.

Unity 위험:
방향 선택은 적 추적 감각, 비행 몹 돌진 타이밍, sprite flip 체감에 직접 영향을 줍니다. 이번 변경은 기존 x축 판정, `diveDistance` 임계값, 내부 normalized 방향 계산을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 몹 좌/우 이동, x좌표 동일 시 정지 방향, 비행 몹의 수평 이동과 돌진 방향, sprite flip이 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/FlyMob.cs`
- `Assets/Scripts/SangHyup/Enemy/MobDirectionPlanner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Mob Train 충돌 처리 분리

상태:
일부 완료

현재 문제:
`Mob.OnTriggerEnter2D(...)`는 Unity trigger 진입점 역할뿐 아니라, `Train` 레이어 판정, `Train` 컴포넌트 조회, 데미지 적용, 카메라 흔들림, 사망 코루틴 시작까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `OnTriggerEnter2D(...)`와 `StartCoroutine(Die())`는 `Mob`에 유지하고, Train 충돌 side effect만 `MobTrainCollisionHandler`로 분리했습니다. 씬/프리팹/Inspector 데이터와 충돌 콜백은 변경하지 않았습니다.

목표 구조:
`MobTrainCollisionHandler`는 일반 몹이 Train 레이어와 충돌했는지 판정하고, `Train.TakeDamage(...)`와 기본 카메라 흔들림을 담당합니다. `Mob`은 Unity trigger entry와 사망 lifecycle을 담당합니다.

Unity 위험:
Train 충돌은 플레이어 피격, 카메라 피드백, 몹 사망 타이밍에 직접 영향을 줍니다. 이번 변경은 기존처럼 `Train` 레이어 충돌이면 `Train` 컴포넌트가 없어도 몹은 죽고, 데미지/흔들림은 `Train` 컴포넌트가 있을 때만 실행되도록 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 몹과 비행 몹이 Train에 닿을 때 데미지, 기본 카메라 흔들림, 몹 사망/비활성화가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/MobTrainCollisionHandler.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Mob 넉백 force 계산 분리

상태:
일부 완료

현재 문제:
`Mob.Knockback(...)`는 아이템에서 호출되는 public 진입점, force 벡터 계산, 스턴 코루틴 시작, `Rigidbody2D.AddForce(...)` 적용을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 public `Knockback(Vector2, float)` 시그니처, 스턴 코루틴, 실제 물리 impulse 적용은 `Mob`에 유지하고, 순수 force 계산만 `MobKnockbackPolicy`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`MobKnockbackPolicy`는 일반 몹 넉백 force 계산만 담당합니다. `Mob`은 외부 아이템 호출을 받는 파사드, 스턴 lifecycle, Rigidbody2D side effect를 담당합니다.

Unity 위험:
넉백 force는 GiantMaw 같은 아이템의 타격감, 몹 위치 변화, 스턴 중 이동 정지에 직접 영향을 줍니다. 이번 변경은 기존 `direction.normalized * power` 공식을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 GiantMaw 등 `Mob.Knockback(...)` 호출 아이템에서 넉백 방향, 힘, 스턴 시간, 이후 이동 복귀가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/MobKnockbackPolicy.cs`
- `Assets/Scripts/SangHyup/Items/GiantMaw.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss Train 충돌 처리 분리

상태:
일부 완료

현재 문제:
`TrainBoss.OnTriggerEnter2D(...)`는 alive 상태 확인, `Train` 레이어 판정, `Train` 컴포넌트 조회, 보스 공격 데미지 적용, 보스 전용 카메라 흔들림을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `OnTriggerEnter2D(...)`와 `isAlive` guard는 `TrainBoss`에 유지하고, Train 충돌 side effect만 `TrainBossTrainCollisionHandler`로 분리했습니다. 씬/프리팹/Inspector 데이터와 충돌 콜백은 변경하지 않았습니다.

목표 구조:
`TrainBossTrainCollisionHandler`는 TrainBoss의 Train 레이어 충돌 판정, `Train.TakeDamage(damage, true)`, 보스 전용 카메라 흔들림만 담당합니다. `TrainBoss`는 보스 생존 상태, phase, knockback, physics, death lifecycle을 담당합니다.

Unity 위험:
TrainBoss 충돌은 보스전 난이도, 플레이어 속도 피해, 카메라 피드백에 직접 영향을 줍니다. 이번 변경은 기존 보스 공격 데미지 플래그와 `ShakeCamera(0.3f, 1f, 15, 90f)` 파라미터를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss가 살아 있을 때 Train 충돌 데미지, 보스 전용 카메라 흔들림, 사망 후 충돌 무시가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossTrainCollisionHandler.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Tentacle Train 충돌 처리 분리

상태:
일부 완료

현재 문제:
`Tentacle.OnTriggerEnter2D(...)`는 Unity trigger 진입점 역할뿐 아니라, `Player` 태그 판정, `Train` 컴포넌트 조회, 기차 데미지 적용을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 촉수 공격 코루틴, owner 등록/해제, 사운드 콜백, HP 처리, destroy 흐름은 `Tentacle`에 유지하고, 충돌 데미지 전달만 `TentacleTrainCollisionHandler`로 분리했습니다. 씬/프리팹/Inspector 데이터와 충돌 콜백은 변경하지 않았습니다.

목표 구조:
`TentacleTrainCollisionHandler`는 `Player` 태그 충돌인지 확인하고, 충돌 오브젝트의 `Train` 컴포넌트에 기존 데미지를 전달하는 책임만 담당합니다. `Tentacle`은 EyeBoss 패턴에서 생성되는 공격체의 lifecycle과 trigger entry를 담당합니다.

Unity 위험:
촉수 충돌은 EyeBoss 패턴 난이도와 플레이어 피격 체감에 직접 영향을 줍니다. 이번 변경은 기존 `CompareTag("Player")`, `GetComponent<Train>()`, `Train.TakeDamage(damage)` 동작을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss 촉수가 공격 판정 중 Train에 닿을 때 데미지가 기존처럼 적용되고, 촉수 생성/소멸/등록 해제가 그대로 유지되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Assets/Scripts/SangHyup/Enemy/TentacleTrainCollisionHandler.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Tentacle 공격 애니메이션 길이 판정 분리

상태:
일부 완료

현재 문제:
`Tentacle.Setup(...)`은 owner 등록, 데미지/대기 시간 설정, 사운드 콜백 저장뿐 아니라 Animator controller에서 공격 애니메이션 clip을 찾고 길이를 계산하는 presentation/timing 규칙까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 공격 코루틴 순서, `BeginAttack()`, `GetTotalDuration()`, owner 등록/해제, serialized field는 유지하고, 애니메이션 길이 판정만 `TentacleAttackAnimationDurationResolver`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`TentacleAttackAnimationDurationResolver`는 Animator controller의 clip 목록에서 `Attack`/`attack` 이름을 가진 첫 clip 길이를 찾는 책임만 담당합니다. `Tentacle`은 EyeBoss가 생성한 촉수의 lifecycle, 공격 wait, 애니메이션 trigger, 사운드 콜백, 소멸 timing을 담당합니다.

Unity 위험:
공격 애니메이션 길이는 촉수 공격 판정 후 소멸까지의 체감 timing에 영향을 줍니다. 이번 변경은 기존처럼 controller가 없으면 기존 `animationLength`를 유지하고, controller는 있지만 공격 clip을 못 찾으면 1.0초를 사용하는 동작을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss 촉수 공격 애니메이션 길이, 공격 후 대기, 소멸 timing, `GetTotalDuration()` 기반 패턴 대기가 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Assets/Scripts/SangHyup/Enemy/TentacleAttackAnimationDurationResolver.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Tentacle 데미지 상태 판정 분리

상태:
일부 완료

현재 문제:
`Tentacle.TakeDamage(...)`는 HP 차감, 사망 판정, hit effect coroutine, 피격/사망 사운드, kill particle 생성, `Destroy(gameObject)`까지 한 메서드에서 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 public `TakeDamage(float)` 진입점과 Unity side effect 위치를 유지하고, 순수 HP 차감과 사망 threshold 판정만 `TentacleDamageState`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`TentacleDamageState`는 촉수의 현재 HP에 데미지를 적용하고 사망 여부를 계산하는 상태 규칙만 담당합니다. `Tentacle`은 `isAlive` guard, hit effect, 사운드, kill particle, destroy 같은 Unity lifecycle/presentation 부작용을 담당합니다.

Unity 위험:
촉수 사망 판정은 EyeBoss 패턴 중 촉수 제거, 사운드, 파티클, owner unregister timing에 영향을 줄 수 있습니다. 이번 변경은 기존 `currentHP -= damageAmount`와 `currentHP <= 0` 조건을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss 촉수 피격, 사망 사운드, kill particle, Destroy, OnDestroy unregister 흐름이 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Assets/Scripts/SangHyup/Enemy/TentacleDamageState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss 광폭화 전환 판정 분리

상태:
일부 완료

현재 문제:
`EyeBoss.TakeDamage(...)`는 데미지 후 예상 HP 계산, 광폭화 threshold 계산, 강제 광폭화 가능 여부 판정, HP clamp, 무적 상태 변경, 포효 사운드, 광폭화 코루틴 시작, 일반 데미지 fallback을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 public `TakeDamage(float)` 진입점, `isInvincible` guard, 사운드 publish, `ForceEnrageRoutine()` 시작, `base.TakeDamage(...)` fallback은 `EyeBoss`에 유지하고, 순수 threshold 판정만 `EyeBossEnrageTransition`으로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`EyeBossEnrageTransition`은 현재 HP, 데미지, 최대 HP, 광폭화 threshold 비율로 강제 광폭화 여부와 clamp HP를 계산합니다. `EyeBoss`는 보스 상태 변경, 패턴 코루틴, 사운드, 실제 데미지 처리 흐름을 담당합니다.

Unity 위험:
광폭화 진입 타이밍은 EyeBoss 난이도와 패턴 흐름에 직접 영향을 줍니다. 이번 변경은 기존 `currentHP - damageAmount`, `calibratedMaxHP * EnragePatternThreshold`, `!hasEnraged && !enragePatternReady && predictedHP <= thresholdHP` 조건을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss가 threshold 이하 데미지를 받을 때 HP가 threshold로 clamp되고, 무적/포효/광폭화 패턴 시작/일반 데미지 fallback이 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossEnrageTransition.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss 촉수 공격 profile 선택 분리

상태:
일부 완료

현재 문제:
`EyeBoss.SpawnTentaclesAndGetDuration(...)`는 촉수 prefab 선택, `Instantiate(...)`, `Tentacle.Setup(...)`, 공격 시작, 최대 duration 계산, 소환 사운드 publish와 함께 일반/광폭화 데미지 및 공격 지연 시간 선택까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 씬/프리팹/Inspector 데이터와 `Tentacle.Setup(...)` 호출 구조를 건드리지 않고, 일반/광폭화 공격 profile 선택을 `EyeBossTentacleAttackProfile`로 분리했습니다. 이후 실제 촉수 생성 side effect는 `EyeBossTentacleSpawner`로 추가 분리했습니다.

목표 구조:
`EyeBossTentacleAttackProfile`은 `isEnrage` 값에 따라 촉수 damage와 attack delay를 선택하는 순수 규칙만 담당합니다. `EyeBoss`는 profile을 해석하고, 실제 촉수 생성, prefab 선택, `Tentacle` setup, attack 시작, 사운드 publish 같은 Unity side effect는 `EyeBossTentacleSpawner`가 담당합니다.

Unity 위험:
촉수 damage와 delay는 EyeBoss 패턴 난이도와 피격 체감에 직접 영향을 줍니다. 이번 변경은 기존 일반 패턴의 `normalTentacleDamage`/`normalAttackDelay`, 광폭화 패턴의 `enrageTentacleDamage`/`enrageAttackDelay` 선택 규칙을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss 일반/광폭화 패턴에서 촉수 데미지와 공격 지연 시간이 기존 Inspector 값대로 전달되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentacleAttackProfile.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentacleSpawner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss 전진 이동 정책 분리

상태:
일부 완료

현재 문제:
`TrainBoss.FixedUpdate()`와 `SetMoveDirection(...)`은 스턴 상태 확인, `Rigidbody2D.linearVelocity` 적용, sprite flip 적용과 함께 "항상 왼쪽으로 이동"하는 방향/속도 수식까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `TrainBoss`의 serialized 이동/페이즈/넉백 필드와 Unity lifecycle은 유지하고, 전진 방향, velocity 조합, sprite-facing 상수만 `TrainBossMovementPolicy`로 분리했습니다. 씬/프리팹/Inspector 데이터는 변경하지 않았습니다.

목표 구조:
`TrainBossMovementPolicy`는 TrainBoss가 전진할 방향, x/y velocity 조합, sprite flip 기본값 같은 순수 정책만 담당합니다. `TrainBoss`는 `FixedUpdate`, 스턴 상태, `Rigidbody2D` write, 페이즈/넉백/충돌 side effect를 담당합니다.

Unity 위험:
TrainBoss 이동은 보스전 압박감과 충돌 타이밍에 직접 영향을 줍니다. 이번 변경은 기존 `Vector2.left`, `moveDirection.x * moveSpeed`, 현재 y velocity 유지, `flipX = false` 규칙을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss가 스턴이 아닐 때 기존처럼 왼쪽으로 전진하고, 넉백 후 y velocity 보존과 sprite-facing이 기존처럼 유지되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossMovementPolicy.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss 전투 eligibility gate 분리

상태:
일부 완료

현재 문제:
`TrainBoss.TakeDamage(...)`는 데미지를 받을 수 있는 조건(`isAlive && hasEnteredScreen`)과 실제 데미지 처리, 페이즈 전환, 넉백 쿨다운/실행을 함께 처리했습니다. `OnTriggerEnter2D(...)`도 생존 여부 gate와 Train 충돌 side effect를 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `TakeDamage(...)`, `OnTriggerEnter2D(...)`, 페이즈/넉백/충돌 side effect 위치를 유지하고, 순수 eligibility 조건만 `TrainBossCombatGate`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`TrainBossCombatGate`는 TrainBoss가 데미지를 받을 수 있는지, Train 충돌 처리를 진행할 수 있는지만 판단합니다. `TrainBoss`는 실제 데미지 적용, 페이즈 전환, 넉백, 충돌 데미지 전달, 사운드/연출 side effect를 담당합니다.

Unity 위험:
TrainBoss가 화면에 들어오기 전 데미지를 무시하는 규칙과 사망 후 충돌을 무시하는 규칙은 보스전 난이도와 진행 안정성에 영향을 줍니다. 이번 변경은 기존 `isAlive && hasEnteredScreen`, `isAlive` 조건을 그대로 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss가 화면 진입 전에는 데미지를 받지 않고, 생존 중에는 Train 충돌을 처리하며, 사망 후 충돌은 무시되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossCombatGate.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Boss 처치 이벤트 요청 조건 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Die()`와 `TrainBoss.Die()`는 보스 사망 연출 후 kill event를 요청할 수 있는 조건인 `killEvent != null`, `GameManager.Instance != null`, `!GameManager.Instance.IsTimeForEnding`을 각자 직접 검사했습니다.

왜 지금 남겨두는가:
이번 단계에서는 이벤트 요청, 보스 처치 카운트 증가, `GameManager.BossDied()`, `StageManager.StartStageTransitionSequence()`, `Destroy(gameObject)` 같은 side effect는 각 보스에 유지하고, kill event 요청 가능 여부만 `BossKillEventRequestGate`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`BossKillEventRequestGate`는 보스 처치 이벤트를 요청할 수 있는지만 판단합니다. `EyeBoss`와 `TrainBoss`는 실제 이벤트 요청, 보스 처치 처리, 스테이지 전환 fallback, 오브젝트 파괴 흐름을 담당합니다.

Unity 위험:
보스 처치 이벤트 요청 조건은 엔딩 타이밍, 스테이지 전환, 이벤트 UI 큐에 영향을 줄 수 있습니다. 이번 변경은 기존 조건인 kill event 존재, `GameManager` 존재, 엔딩 시간이 아님을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss와 TrainBoss 처치 시 엔딩 시간이 아닐 때만 kill event가 요청되고, 엔딩 시간이거나 `GameManager`가 없을 때 기존처럼 이벤트 요청을 건너뛰는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/BossKillEventRequestGate.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Boss 사망 완료 route 선택 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Die()`와 `TrainBoss.Die()`는 보스 사망 연출 후 `GameManager`가 있으면 보스 처치 카운트를 올리고 `GameManager.BossDied()`를 호출하며, 없으면 `StageManager.StartStageTransitionSequence()`로 fallback하는 route 선택을 각자 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `AddBossKillCount()`, `BossDied()`, `StartStageTransitionSequence()`, `Destroy(gameObject)` 같은 실제 side effect는 각 보스에 유지하고, route 선택만 `BossDeathCompletionRouter`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`BossDeathCompletionRouter`는 `GameManager` route와 `StageManagerFallback` route 중 무엇을 사용할지만 결정합니다. `EyeBoss`와 `TrainBoss`는 실제 보스 사망 완료 처리, 전환, 오브젝트 파괴를 담당합니다.

Unity 위험:
보스 사망 완료 route는 스테이지 전환, 엔딩 분기, 적 정리, 이벤트 UI 흐름에 직접 영향을 줄 수 있습니다. 이번 변경은 기존 `GameManager.Instance != null`이면 `GameManager` 경로, 아니면 `StageManager` fallback 경로를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss와 TrainBoss 처치 시 `GameManager`가 있는 일반 플레이에서는 보스 카운트와 `BossDied()`가 호출되고, 예외적으로 `GameManager`가 없을 때만 `StageManager` fallback이 호출되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/BossDeathCompletionRouter.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Enemy 기본 피격 eligibility gate 분리

상태:
일부 완료

현재 문제:
`Enemy.TakeDamage(...)`는 피격 가능 조건인 `isAlive && hasEnteredScreen` 검사와 HP 감소, hit effect, 피격 사운드, 사망 coroutine 시작을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 public `TakeDamage(float)` 진입점, HP 감소, hit effect, 사운드 publish, `Die()` coroutine 시작은 `Enemy`에 유지하고, 기본 피격 가능 여부만 `EnemyDamageGate`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`EnemyDamageGate`는 일반 Enemy가 데미지를 받을 수 있는지만 판단합니다. `Enemy`는 실제 데미지 적용, 피격 연출, 사운드, 사망 흐름을 담당합니다.

Unity 위험:
화면 진입 전 피격 무시와 사망 후 피격 무시는 적 처치 타이밍, 투사체 판정, 아이템 타겟팅 체감에 영향을 줄 수 있습니다. 이번 변경은 기존 `isAlive && hasEnteredScreen` 조건을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적이 화면 진입 전에는 데미지를 받지 않고, 화면 진입 후 생존 중에만 HP 감소/피격 사운드/사망 처리가 실행되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyDamageGate.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Mob/FlyMob movement state 선택 분리

상태:
일부 완료

현재 문제:
`Mob.FixedUpdate()`와 `FlyMob.FixedUpdate()`는 `isAlive`와 `isStunned` 조합으로 사망 슬라이드, 일반 이동, 정지 상태를 고르는 분기를 각각 직접 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 `FixedUpdate()`, `moveDirection` 변경, target 위치 읽기, `Rigidbody2D.linearVelocity` 적용, sprite flip 같은 Unity side effect는 `Mob`과 `FlyMob`에 유지하고, movement state 선택만 `MobMovementStateResolver`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`MobMovementStateResolver`는 현재 mob이 `DeathSlide`, `ActiveMove`, `None` 중 어떤 상태로 이동 업데이트를 해야 하는지만 판단합니다. `Mob`과 `FlyMob`는 실제 이동 방향 계산, velocity 적용, 사망 슬라이드 처리 흐름을 담당합니다.

Unity 위험:
Mob/FlyMob의 이동 상태는 피격 후 stun, 사망 슬라이드, 플레이어 추적 체감에 직접 영향을 줍니다. 이번 변경은 기존 규칙인 stunned 상태에서는 이동 갱신 없음, alive + not stunned이면 이동, dead + not stunned이면 death slide를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 Mob과 FlyMob이 스턴 중에는 이동 갱신을 하지 않고, 생존 중에는 기존처럼 이동하며, 사망 후에는 기존처럼 왼쪽 death slide를 적용하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/FlyMob.cs`
- `Assets/Scripts/SangHyup/Enemy/MobMovementStateResolver.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Enemy 기본 damage state 분리

상태:
일부 완료

현재 문제:
`Enemy.TakeDamage(...)`는 HP 감소와 사망 threshold 판정, hit effect coroutine 시작, 피격 사운드 publish, 사망 coroutine 시작을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 public `TakeDamage(float)` 진입점, 피격 eligibility gate, hit effect, 사운드 publish, `Die()` coroutine 시작은 `Enemy`에 유지하고, HP 감소와 사망 threshold 상태만 `EnemyDamageState`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`EnemyDamageState`는 일반 Enemy의 데미지 적용 후 HP와 사망 여부만 계산합니다. `Enemy`는 실제 피격 연출, 사운드, 사망 coroutine 시작 같은 Unity side effect를 담당합니다.

Unity 위험:
HP 감소와 `<= 0` 사망 판정은 모든 일반 적, Mob, FlyMob, TrainBoss의 base damage flow에 영향을 줄 수 있습니다. 이번 변경은 기존 `currentHP - damageAmount`와 `currentHP <= 0` 조건을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적과 상속 적들이 피격 시 기존처럼 HP가 감소하고, HP가 0 이하가 될 때만 사망 coroutine이 시작되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyDamageState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner runtime GameState gate 분리

상태:
일부 완료

현재 문제:
`Spawner.Update()`는 스폰을 실행할 수 있는 `GameState.Playing`/`GameState.Boss` 조건과 phase 갱신, 일반 mob timer, elite mob timer, boss spawn schedule, periodic spawn 처리를 함께 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 `Update()`, timer 누적, phase 갱신, mob/boss/periodic spawn 실행 순서는 `Spawner`에 유지하고, runtime state gate만 `SpawnerRuntimeStateGate`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`SpawnerRuntimeStateGate`는 현재 게임 상태에서 spawner가 실행될 수 있는지만 판단합니다. `Spawner`는 실제 spawn timing, spawn 실행, coroutine, pool 접근을 담당합니다.

Unity 위험:
Spawner가 실행되는 GameState는 적 생성, 보스 스케줄, 이벤트 spawn에 직접 영향을 줍니다. 이번 변경은 기존 허용 상태인 `Playing`과 `Boss`만 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 Playing/Boss 상태에서는 기존처럼 spawn timer가 진행되고, Start/Event/Paused/Ending 등 다른 상태에서는 기존처럼 `Spawner.Update()`가 즉시 return하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerRuntimeStateGate.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner spawn schedule 판정 분리

상태:
일부 완료

현재 문제:
`Spawner.Update()`는 `mobTimer`, `eliteMobTimer`, `nextBossSpawnTime` 누적과 reset, 실제 spawn 호출, 그리고 일반/엘리트/보스 spawn due 판정을 모두 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 게임 흐름을 흔들지 않기 위해 timer 값 변경, reset 위치, `SpawnBasicMobs()`, `SpawnEliteMob()`, `SpawnNextBoss()` 호출은 `Spawner`에 유지하고, 순수 due 판정만 `SpawnerSpawnSchedule`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`SpawnerSpawnSchedule`은 elite timer 시작 가능 여부, timer due 여부, boss spawn due 여부, 다음 boss spawn 시간 계산만 담당합니다. `Spawner`는 `Update()` 진입점, `Time.deltaTime` 누적, phase 갱신, spawn 실행, coroutine, pool 접근을 계속 담당합니다.

Unity 위험:
스폰 schedule 판정은 적 밀도, 엘리트 첫 등장 시점, 보스 등장 타이밍에 직접 영향을 줍니다. 이번 변경은 기존 `gameTime >= firstEliteSpawnTime`, `timer >= interval`, `gameTime >= nextBossSpawnTime`, `nextBossSpawnTime + bossSpawnInterval` 규칙을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 mob spawn interval, 첫 elite 등장 시점, elite 반복 등장, boss 반복 등장 간격이 기존처럼 유지되는지 플레이 모드에서 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerSpawnSchedule.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner boss spawn placement 판정 분리

상태:
일부 완료

현재 문제:
`Spawner.SpawnBossObject(...)`는 boss prefab 조회, boss setting 조회, spawnPoint fallback, missing spawnPoint 로그, `Instantiate(...)`, `Boss` component 조회, arrivalPoint 존재 여부 확인, entrance routine 시작을 한 메서드에서 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 보스 생성 side effect를 흔들지 않기 위해 `PoolManager` 조회, `Instantiate(...)`, 로그, `StartEntranceRoutine(...)` 호출은 `Spawner`에 유지하고, spawn/arrival position 해석만 `SpawnerBossSpawnPlacement`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`SpawnerBossSpawnPlacement`는 보스 spawnPoint fallback, fallback 사용 여부, arrivalPoint position 해석만 담당합니다. `Spawner`는 boss prefab 생성, 경고 로그, entrance routine 호출, coroutine 흐름을 계속 담당합니다.

Unity 위험:
보스 spawn 위치와 arrivalPoint는 보스 등장 연출, 전투 시작 위치, 패턴 시작 타이밍 체감에 영향을 줍니다. 이번 변경은 기존처럼 `spawnPoint`가 없으면 spawner 위치를 사용하고 로그를 남기며, `arrivalPoint`가 없으면 entrance routine을 시작하지 않는 동작을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 보스별 spawnPoint 누락 fallback, 로그 출력, arrivalPoint가 있는 보스의 등장 이동, arrivalPoint가 없는 보스의 즉시 패턴 시작 흐름을 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossSpawnPlacement.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner spawn control 상태 분리

상태:
일부 완료

현재 문제:
`Spawner`는 이벤트/기차 코드가 호출하는 `SetSpawning(...)`, `SetRearSpawning(...)` public API, 내부 `isSpawningEnabled`/`isRearSpawnEnabled` 상태, periodic spawn gate, rear spawn position 입력, `StopAllCoroutines()` 부작용을 함께 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 외부 호출부와 coroutine 소유권을 유지하기 위해 public method와 `StopAllCoroutines()` 호출은 `Spawner`에 남기고, 순수 flag 상태와 disable 시 coroutine 중단 필요 판정만 `SpawnerSpawnControlState`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`SpawnerSpawnControlState`는 spawning enabled, rear spawning enabled, reset 기본값, disable 시 coroutine stop 필요 여부만 담당합니다. `Spawner`는 event/train-facing public method, periodic task 실행, spawn position 선택 호출, coroutine 중단 side effect를 계속 담당합니다.

Unity 위험:
`SetSpawning(false)`는 기존처럼 `StopAllCoroutines()`를 호출하므로 보스 등장 루틴이나 batch spawn 루틴도 중단될 수 있습니다. 이번 변경은 그 동작을 바꾸지 않았고, 상태 저장 위치만 바꿨습니다.

리팩터링 시작 조건:
Unity 컴파일 후 이벤트 periodic spawn on/off, 기차 사망/회복 중 후방 스폰 비활성/재활성, `SetSpawning(false)`의 coroutine 중단 흐름이 기존처럼 유지되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerSpawnControlState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner enemy physics reset 분리

상태:
일부 완료

현재 문제:
`Spawner`는 적 pool 조회, 위치 지정, batch coroutine, death callback 연결뿐 아니라 스폰된 적의 `Rigidbody2D.linearVelocity`와 `angularVelocity` 초기화까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 스폰 흐름을 흔들지 않기 위해 pool 조회, 위치 지정, batch timing, callback wiring은 `Spawner`에 유지하고, 스폰 직후 물리 속도 초기화만 `SpawnerEnemyPhysicsReset`으로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`SpawnerEnemyPhysicsReset`은 스폰된 enemy GameObject에 `Rigidbody2D`가 있을 때 velocity 값을 초기화하는 책임만 담당합니다. `Spawner`는 어떤 오브젝트를 언제 어디에 배치할지와 event/batch spawn 흐름을 계속 담당합니다.

Unity 위험:
스폰 직후 velocity 초기화는 pooled enemy가 이전 이동/넉백 속도를 들고 재사용되는 것을 막습니다. 이번 변경은 기존처럼 `Rigidbody2D`가 있으면 `linearVelocity = Vector2.zero`, `angularVelocity = 0f`를 적용하고, 없으면 아무 것도 하지 않는 동작을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반/엘리트/event/batch spawn에서 pooled enemy가 이전 속도를 유지하지 않고, `Rigidbody2D`가 없는 특수 enemy도 예외 없이 스폰 흐름을 유지하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerEnemyPhysicsReset.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - PoolManager boss prefab lookup 분리

상태:
일부 완료

현재 문제:
`PoolManager.GetBoss(BossName)`는 `BossName` enum 값을 그대로 boss prefab 배열 index로 사용했습니다. 이 계약은 보스 추가/순서 변경 때 중요하지만 코드상으로는 `PoolManager` 내부 한 줄에 묻혀 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 public `GetBoss(...)`, serialized `bosses` 배열, boss enum, prefab 연결을 바꾸지 않고, 기존 enum-to-array lookup만 `PoolBossPrefabLookup`으로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`PoolBossPrefabLookup`은 `BossName`과 boss prefab 배열 순서가 일치해야 한다는 lookup 정책을 담당합니다. `PoolManager`는 기존처럼 prefab 배열을 보관하고 public boss lookup facade를 제공합니다.

Unity 위험:
보스 enum 순서와 `PoolManager.bosses` 배열 순서가 어긋나면 다른 보스가 생성될 수 있습니다. 이번 변경은 기존과 동일하게 `bosses[(int)boss]`를 반환하므로 동작은 유지되지만, Unity에서 보스 등장 순서 검증이 필요합니다.

리팩터링 시작 조건:
Unity 컴파일 후 `Spawner`가 `PoolManager.GetBoss(...)`를 통해 EyeBoss/TrainBoss prefab을 기존 순서대로 가져오는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolManager.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolBossPrefabLookup.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Enemy enable layer reset 분리

상태:
일부 완료

현재 문제:
`Enemy.OnEnable()`과 `Boss.OnEnable()`은 생명/HP/표시 상태 초기화와 함께 `gameObject.layer = LayerMask.NameToLayer("Enemy")`를 직접 반복했습니다.

왜 지금 남겨두는가:
이번 단계에서는 enable lifecycle 순서와 상태 초기화 위치를 유지하고, `"Enemy"` layer assignment만 `EnemyLayerAssignment`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`EnemyLayerAssignment`는 enemy 계열 오브젝트를 Enemy layer로 되돌리는 규칙만 담당합니다. `Enemy`와 `Boss`는 언제 enable-time 상태 reset을 수행할지 계속 담당합니다.

Unity 위험:
Enemy layer는 충돌, 타겟팅, 아이템 판정에 영향을 줄 수 있습니다. 이번 변경은 기존과 동일하게 `LayerMask.NameToLayer("Enemy")` 값을 적용하므로 동작 의도는 유지되지만, Unity에서 일반 적과 보스 enable 시 layer가 기존처럼 복구되는지 확인해야 합니다.

리팩터링 시작 조건:
Unity 컴파일 후 pooled enemy 재활성화와 boss 생성/활성화 시 layer가 `Enemy`로 유지되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/Boss.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyLayerAssignment.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Enemy kill particle 생성 분리

상태:
일부 완료

현재 문제:
`Enemy.Die()`와 `Tentacle.TakeDamage(...)`는 사망 흐름 안에서 `killParticle` instantiate를 직접 수행했습니다. 사망 상태 변경, 보상/사운드, 표시 숨김, 오브젝트 정리와 VFX 생성이 한 메서드에 섞여 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 death ordering, 사운드 publish, Destroy/SetActive 흐름, serialized `killParticle` 필드를 유지하고, 실제 particle instantiate 호출만 `EnemyKillParticleSpawner`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`EnemyKillParticleSpawner`는 전달받은 prefab, position, rotation으로 kill particle을 생성하는 책임만 담당합니다. `Enemy`와 `Tentacle`은 언제 사망 side effect를 실행할지 계속 담당합니다.

Unity 위험:
`killParticle` prefab 연결이 비어 있으면 기존처럼 instantiate 단계에서 문제가 날 수 있습니다. 이번 변경은 null guard나 pooling을 추가하지 않았고, 기존 생성 위치/회전을 그대로 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 적 사망과 Tentacle 사망에서 kill particle 위치와 회전, 사운드/오브젝트 정리 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyKillParticleSpawner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Mob death completion 분리

상태:
일부 완료

현재 문제:
`Mob.Die()`는 kill count 보고, `base.Die()` 대기, 사망 사운드, sprite 숨김, `gameObject.SetActive(false)`, `OnDied` respawn callback 호출까지 한 coroutine 안에서 모두 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 coroutine 순서와 pooling/respawn callback 흐름을 유지하기 위해 `Mob.Die()`는 사망 순서만 들고 있고, 실제 kill 보고와 post-base-death 완료 side effect만 `MobDeathCompletion`으로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`MobDeathCompletion`은 일반 mob 사망 완료 규칙인 kill count 보고, death sound publish, sprite 숨김, GameObject deactivation, `OnDied` callback 호출을 담당합니다. `Mob`은 언제 base death를 기다리고 완료 처리를 호출할지만 담당합니다.

Unity 위험:
`OnDied`는 `Spawner.RespawnMob` 연결에 사용되고, `gameObject.SetActive(false)`는 `OnDisable()`과 `PoolManager.UnregisterEnemy(...)`를 유발합니다. 이번 변경은 기존 순서인 kill count 보고, `base.Die()` 대기, death sound, sprite hide, deactivate, callback 호출을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반/엘리트 mob 사망 시 kill count, death sound, pooling deactivation, `OnDied` respawn callback이 기존 순서로 실행되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/MobDeathCompletion.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Boss death completion 실행 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Die()`와 `TrainBoss.Die()`는 kill event 요청, boss kill count 증가, `GameManager.BossDied()` 또는 `StageManager` fallback, `Destroy(gameObject)` 실행 블록을 반복했습니다.

왜 지금 남겨두는가:
이번 단계에서는 보스별 사망 연출 순서, 폭발 위치, 사운드, wait timing은 각 보스에 유지하고, 공통 completion side effect 실행만 `BossDeathCompletionExecutor`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`BossDeathCompletionExecutor`는 kill event 요청 가능 여부, completion route 선택, boss kill count/전환/fallback 실행, boss GameObject destroy를 한 곳에서 담당합니다. `EyeBoss`와 `TrainBoss`는 언제 completion을 실행할지와 보스별 사망 presentation을 계속 담당합니다.

Unity 위험:
보스 사망 완료는 이벤트 팝업, 보스 처치 카운트, 스테이지 전환, 엔딩 분기, GameObject 파괴와 직접 연결됩니다. 이번 변경은 기존 순서인 kill event 요청 가능 시 요청, `GameManager` 경로 또는 `StageManager` fallback 실행, 마지막 destroy를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss/TrainBoss 처치 시 kill event 요청 조건, boss kill count, `BossDied()`/stage fallback, destroy 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/BossDeathCompletionExecutor.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Boss death explosion 생성 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Die()`와 `TrainBoss.Die()`는 각자의 사망 흐름 안에서 `killExplosionEffect` instantiate를 직접 수행했습니다. 보스별 사망 timing, 사운드, completion route와 VFX 생성 책임이 한 메서드 안에 함께 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 보스별 사망 연출 순서, TrainBoss offset, 사운드 publish, wait timing, completion 실행은 각 보스에 유지하고, 실제 explosion instantiate 호출만 `BossDeathExplosionSpawner`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`BossDeathExplosionSpawner`는 전달받은 prefab과 position으로 boss death explosion을 생성하는 책임만 담당합니다. `EyeBoss`와 `TrainBoss`는 언제 어떤 위치로 explosion을 생성할지 계속 담당합니다.

Unity 위험:
`killExplosionEffect` prefab 연결이 비어 있으면 기존처럼 instantiate 단계에서 문제가 날 수 있습니다. 이번 변경은 null guard나 pooling을 추가하지 않았고, EyeBoss의 `transform.position`, TrainBoss의 `(-5f, 2f)` offset, identity rotation을 그대로 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss/TrainBoss 사망 시 explosion 위치, 사운드, wait timing, completion 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/BossDeathExplosionSpawner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss phase presentation 분리

상태:
일부 완료

현재 문제:
`TrainBoss`는 phase 2 threshold와 knockback rule 일부는 helper로 분리했지만, 초기 phase collider 상태 적용과 phase 2 진입 시 animator trigger/collider 교체/log 출력은 여전히 `TrainBoss` 내부에서 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized `phase1Collider`, `phase2Collider`, `p2HpRatio`, animator 참조, public API를 바꾸지 않고, phase presentation side effect만 `TrainBossPhasePresentation`으로 분리했습니다. 이후 phase state 저장은 `TrainBossPhaseState`로 분리했고, `TrainBoss`는 roar sound, damage/knockback 흐름, phase entry ordering을 계속 담당합니다.

목표 구조:
`TrainBossPhasePresentation`은 TrainBoss의 초기 collider 상태와 phase 2 animator/collider presentation만 담당합니다. `TrainBossPhaseTransition`은 phase 2 진입 조건만, `TrainBossPhaseState`는 phase 2 상태 저장만, `TrainBoss`는 언제 phase state를 바꾸고 사운드를 낼지 담당합니다.

Unity 위험:
phase collider 전환과 animator trigger는 TrainBoss 전투 체감과 충돌 판정에 직접 영향을 줍니다. 이번 변경은 기존 순서인 spawn sound 이후 초기 collider 적용, phase 2 진입 시 roar sound → phase state mark → `animator.SetTrigger("phase2")` → collider 교체 → 동일 log 출력을 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss spawn 시 phase 1/2 collider 초기 상태와 phase 2 진입 시 animator trigger, collider 전환, roar sound, knockback force 선택이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossPhaseTransition.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossPhaseState.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossPhasePresentation.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss phase state 분리

상태:
일부 완료

현재 문제:
`TrainBoss`는 phase 2 진입 조건과 presentation 일부를 helper로 위임했지만, phase 2 진입 여부 저장값은 여전히 직접 들고 있었습니다. 이 값은 phase 재진입 방지와 phase별 knockback force 선택에 같이 쓰이는 런타임 상태입니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized `p2HpRatio`, `phase1Collider`, `phase2Collider`, public API, roar sound timing, animator/collider presentation을 바꾸지 않고 phase 상태 저장만 `TrainBossPhaseState`로 분리했습니다.

목표 구조:
`TrainBossPhaseState`는 phase 2 진입 여부 저장, 진입 가능 여부 요청, phase 2 진입 표시만 담당합니다. threshold 산식은 `TrainBossPhaseTransition`에 남기고, `TrainBoss`는 damage 흐름, roar sound, phase presentation 호출, knockback 실행 순서를 담당합니다.

Unity 위험:
phase state는 phase 2 재진입 방지와 phase별 knockback force 선택에 직접 영향을 줍니다. 이번 변경은 기존처럼 phase 1 상태로 시작하고, roar sound 이후 phase 2 상태를 표시한 뒤 presentation을 적용하며, knockback force 선택에서 현재 phase 상태를 읽습니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss phase 2 최초 진입, 중복 진입 방지, phase별 knockback force 선택이 기존처럼 동작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossPhaseState.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossPhaseTransition.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss normal pattern start gate 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Update()`는 보스 생존 여부, busy 상태, enrage pattern 준비 여부, entrance 진행 여부를 직접 확인한 뒤 side/center pattern random 선택과 coroutine 시작까지 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 random pattern 선택, coroutine 시작, tentacle spawn, enrage flow를 건드리지 않고, 정상 패턴을 시작할 수 있는 상태 조건만 `EyeBossPatternStartGate`로 분리했습니다. 씬/프리팹/Inspector 데이터와 public API는 변경하지 않았습니다.

목표 구조:
`EyeBossPatternStartGate`는 EyeBoss가 normal pattern을 시작할 수 있는지 판단하는 순수 gate만 담당합니다. `EyeBoss`는 어떤 pattern을 고르고 어떤 coroutine을 시작할지 계속 담당합니다.

Unity 위험:
이 gate는 EyeBoss 공격 빈도와 enrage/entrance 중 pattern 시작 여부에 직접 영향을 줍니다. 이번 변경은 기존 조건인 `isAlive && !isBusy && !enragePatternReady && !isEntranceActive`를 그대로 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss가 등장 중에는 pattern을 시작하지 않고, busy/enrage 준비 중에는 normal pattern을 시작하지 않으며, 기존처럼 side/center pattern을 random으로 시작하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossPatternStartGate.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss tentacle spawn 실행 분리

상태:
일부 완료

현재 문제:
`EyeBoss.SpawnTentaclesAndGetDuration(...)`는 pattern plan과 attack profile을 받은 뒤에도 prefab 선택, `Instantiate(...)`, `Tentacle.Setup(...)`, attack 시작, max duration 계산, `Boss_TentacleSpawn` 사운드 publish까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 pattern timing, side/center/enrage plan, normal/enrage damage-delay profile, serialized prefab 필드, public API를 바꾸지 않고, 실제 tentacle spawn 실행만 `EyeBossTentacleSpawner`로 분리했습니다.

목표 구조:
`EyeBoss`는 pattern coroutine, attack profile 해석, 반환된 max duration 대기를 담당합니다. `EyeBossTentacleSpawner`는 전달받은 plan/profile/prefab/spawn point로 tentacle을 생성하고 setup/start/duration/sound publish를 담당합니다.

Unity 위험:
Tentacle spawn은 EyeBoss 패턴 체감, weak tentacle 위치, attack sound callback, duration wait에 직접 영향을 줍니다. 이번 변경은 기존 invalid index skip, weak/normal prefab 선택, null prefab skip, identity rotation, `Tentacle.Setup(...)`, `BeginAttack()`, max duration 계산, `SoundID.Boss_TentacleSpawn` publish를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 side/center/enrage 패턴에서 촉수 위치, weak 촉수 선택, 공격 시작, 공격 사운드 callback, max duration wait, spawn sound가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentacleSpawner.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentaclePatternPlanner.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentacleAttackProfile.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss tentacle spawn point 수집 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Start()`는 보스 등장 사운드 publish와 함께 `transform.childCount` / `transform.GetChild(i)`로 촉수 spawn point 배열을 직접 구성했습니다. 이 child 순서는 패턴 위치 계약인데, lifecycle 시작 코드 안에 묻혀 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 씬 child 구조, prefab 연결, serialized field, public API, 패턴 timing을 바꾸지 않고 child transform 수집만 `EyeBossTentacleSpawnPointCollector`로 분리했습니다.

목표 구조:
`EyeBoss`는 `Start()` lifecycle, `base.Start()`, 보스 등장 사운드, 수집된 spawn point 보관을 담당합니다. `EyeBossTentacleSpawnPointCollector`는 child count 길이의 배열을 만들고 child index 순서대로 `Transform`을 복사하는 책임만 담당합니다.

Unity 위험:
EyeBoss 촉수 spawn point는 scene/prefab child 순서에 의존합니다. 이번 변경은 기존처럼 `childCount` 길이로 배열을 만들고 `GetChild(i)` 순서대로 복사하므로 의도한 child 순서는 유지되지만, Unity 컴파일과 실제 패턴 위치 검증은 필요합니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss 등장 시 모든 child spawn point가 기존 순서로 수집되고 side/center/enrage 패턴 위치가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentacleSpawnPointCollector.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss knockback 물리 적용 분리

상태:
일부 완료

현재 문제:
`TrainBoss.Knockback()`는 knockback force 선택, `Stun` coroutine 재시작, `Rigidbody2D.linearVelocity` 초기화, `AddForce(...)` impulse 적용을 한 메서드에서 처리했습니다. Force 선택은 이미 `TrainBossKnockbackPolicy`로 분리되어 있었지만 실제 Rigidbody2D 적용은 여전히 `TrainBoss` 안에 남아 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized knockback field, public API, stun coroutine 이름/순서, cooldown timing, force 선택, impulse mode를 바꾸지 않고 velocity reset과 impulse 적용만 `TrainBossKnockbackApplier`로 분리했습니다.

목표 구조:
`TrainBoss`는 knockback 실행 시점과 `Stun` coroutine stop/start 순서를 담당합니다. `TrainBossKnockbackCooldownState`는 마지막 knockback timestamp 상태를 담당하고, `TrainBossKnockbackPolicy`는 cooldown 판정과 force 선택을 담당하며, `TrainBossKnockbackApplier`는 Rigidbody2D velocity reset과 impulse 적용만 담당합니다.

Unity 위험:
TrainBoss knockback은 보스 전진 체감, phase별 밀림 정도, stun duration에 직접 영향을 줍니다. 이번 변경은 기존처럼 velocity를 `Vector2.zero`로 초기화한 뒤 같은 force를 `ForceMode2D.Impulse`로 적용합니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss 피격 시 phase 1/phase 2 force, stun timing, velocity reset, impulse 적용 체감이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossKnockbackPolicy.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossKnockbackApplier.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss knockback cooldown timestamp 상태 분리

상태:
일부 완료

현재 문제:
`TrainBoss`는 피격 처리, phase 전환, stun coroutine, Rigidbody2D knockback 실행과 함께 `lastKnockbackTime` timestamp 저장/갱신까지 직접 들고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized knockback field, public API, phase check 순서, `Knockback()` 실행 순서, `Stun` coroutine 재시작, force 선택, timestamp 갱신 타이밍을 바꾸지 않고 마지막 knockback timestamp 상태만 `TrainBossKnockbackCooldownState`로 분리했습니다.

목표 구조:
`TrainBoss`는 damage entry point와 knockback 실행 흐름을 유지합니다. `TrainBossKnockbackCooldownState`는 마지막 knockback timestamp 저장, cooldown 가능 여부 조회, 적용 후 timestamp 갱신을 담당합니다. Cooldown 판정식과 force 선택은 `TrainBossKnockbackPolicy`가 계속 담당합니다.

Unity 위험:
TrainBoss knockback cooldown은 다단히트 억제, phase별 밀림 체감, stun 재시작 빈도에 직접 영향을 줍니다. 이번 변경은 기존 초기값 `-999f`와 knockback 실행 후 timestamp 갱신 순서를 유지합니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss 다단히트 상황에서 cooldown 간격, knockback 적용 횟수, stun 재시작 타이밍이 기존과 같은지 확인합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossKnockbackCooldownState.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossKnockbackPolicy.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss death presentation 상수 분리

상태:
일부 완료

현재 문제:
`TrainBoss.Die()`는 `base.Die()` 대기, death explosion 위치 offset `(-5f, 2f)`, `Boss_Die` 사운드 publish, 2초 completion wait, shared boss completion 실행을 한 coroutine 안에서 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 death coroutine 순서, sound ID, wait duration 값, explosion prefab 참조, completion 실행 흐름을 바꾸지 않고 TrainBoss 전용 death presentation 상수만 `TrainBossDeathPresentation`으로 분리했습니다.

목표 구조:
`TrainBoss`는 사망 coroutine 순서, `BossDeathExplosionSpawner` 호출 시점, `SoundID.Boss_Die` publish, `BossDeathCompletionExecutor` 실행을 담당합니다. `TrainBossDeathPresentation`은 explosion offset과 completion delay 값을 담당합니다.

Unity 위험:
TrainBoss death explosion 위치와 completion wait는 사망 연출 체감, 보스 제거 timing, stage transition/ending 진입 timing에 영향을 줍니다. 이번 변경은 기존 offset `(-5f, 2f)`와 wait `2.0f`를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss 사망 시 explosion 위치, death sound timing, 2초 wait, boss completion 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossDeathPresentation.cs`
- `Assets/Scripts/SangHyup/Enemy/BossDeathExplosionSpawner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss normal pattern 선택 책임 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Update()`는 normal pattern 시작 가능 조건, side/center random 선택, `SideAttackPattern()` 또는 `CenterAttackPattern()` coroutine 시작을 한 곳에서 처리했습니다. 시작 조건은 `EyeBossPatternStartGate`로 분리되었지만 50:50 선택 규칙은 아직 Update 안에 남아 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized pattern field, public API, coroutine 이름, pattern timing, side/center pattern 구현을 바꾸지 않고 normal pattern 선택 규칙만 `EyeBossNormalPatternSelector`로 분리했습니다.

목표 구조:
`EyeBoss`는 `Update()` lifecycle, pattern 시작 가능 여부 확인, coroutine 시작을 담당합니다. `EyeBossNormalPatternSelector`는 normal pattern에서 side와 center 중 무엇을 선택할지 담당합니다.

Unity 위험:
Normal pattern 선택 비율은 EyeBoss 공격 체감에 직접 영향을 줍니다. 이번 변경은 기존과 동일하게 `Random.Range(0, 2) == 0`이면 side pattern, 아니면 center pattern을 시작합니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss가 normal pattern 시작 가능 상태에서 기존처럼 side/center pattern을 50:50으로 시작하고, busy/enrage/entrance gating이 유지되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossNormalPatternSelector.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossPatternStartGate.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss death presentation 상수 분리

상태:
일부 완료

현재 문제:
`EyeBoss.Die()`는 tentacle cleanup, death explosion 위치 `transform.position`, `base.Die()` 대기, 2초 completion wait, shared boss completion 실행을 한 coroutine 안에서 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 death coroutine 순서, explosion prefab 참조, explosion 위치 값, wait duration 값, completion 실행 흐름을 바꾸지 않고 EyeBoss 전용 death presentation 상수만 `EyeBossDeathPresentation`으로 분리했습니다.

목표 구조:
`EyeBoss`는 사망 coroutine 순서, `ClearAllTentacles()`, `BossDeathExplosionSpawner` 호출 시점, `base.Die()` 대기, `BossDeathCompletionExecutor` 실행을 담당합니다. `EyeBossDeathPresentation`은 explosion 위치와 completion delay 값을 담당합니다.

Unity 위험:
EyeBoss death explosion 위치와 completion wait는 사망 연출 체감, 보스 제거 timing, stage transition/ending 진입 timing에 영향을 줍니다. 이번 변경은 기존 explosion 위치 `transform.position`과 wait `2.0f`를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss 사망 시 tentacle cleanup, explosion 위치, `base.Die()` 순서, 2초 wait, boss completion 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossDeathPresentation.cs`
- `Assets/Scripts/SangHyup/Enemy/BossDeathExplosionSpawner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss normal pattern wait sequence 분리

상태:
일부 완료

현재 문제:
`EyeBoss.SideAttackPattern()`과 `CenterAttackPattern()`는 서로 다른 pattern plan만 만들 뿐, tentacle spawn 후 `duration` 대기, `waitTimeAfterPatternEnd` 대기, busy 해제 순서를 반복했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized `waitTimeAfterPatternEnd`, public API, coroutine 이름, side/center plan 생성, tentacle spawn 실행, busy 상태 소유권을 바꾸지 않고 post-spawn wait sequence만 `EyeBossNormalPatternRoutine`으로 분리했습니다.

목표 구조:
`EyeBoss`는 normal pattern busy 상태, side/center plan 생성, tentacle spawn 실행을 담당합니다. `EyeBossNormalPatternRoutine`은 normal pattern의 post-spawn wait 순서인 duration wait 후 pattern-end wait를 담당합니다.

Unity 위험:
Normal pattern wait 순서는 EyeBoss 공격 간격과 체감 난이도에 직접 영향을 줍니다. 이번 변경은 기존 순서인 tentacle duration 대기 후 `waitTimeAfterPatternEnd` 대기를 유지했습니다.

리팩터링 시작 조건:
Unity 컴파일 후 Side/Center normal pattern에서 busy 상태, tentacle duration wait, pattern-end wait, 다음 pattern 시작 간격이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossNormalPatternRoutine.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentaclePatternPlanner.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - EyeBoss enrage pattern wait loop 분리

상태:
일부 완료

현재 문제:
`EyeBoss.EnragePatternRoutine()`는 광폭화 상태 설정, enrage pattern plan 실행, duration 누적 loop, 촉수 전멸 시 조기 종료, 무적 해제, post-pattern wait, busy 해제를 한 메서드에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, coroutine 이름, enrage pattern plan, tentacle spawn 실행, 상태 변경 순서, log 문구, wait 값을 바꾸지 않고 duration-or-all-tentacles-cleared wait loop만 `EyeBossEnragePatternRoutine`으로 분리했습니다.

목표 구조:
`EyeBoss`는 enrage 상태 변경, spawn 실행 호출, 무적/ready 해제, post-pattern wait, busy 해제를 담당합니다. `EyeBossEnragePatternRoutine`은 duration이 끝나거나 모든 촉수가 사라질 때까지 기다리는 광폭화 전용 wait loop를 담당합니다.

Unity 위험:
Enrage wait loop는 광폭화 패턴 지속 시간, 촉수 조기 파괴 시 패턴 종료 timing, 무적 해제 timing에 직접 영향을 줍니다. 이번 변경은 기존처럼 `Time.deltaTime`으로 duration을 누적하고, `EyeBossTentacleRegistry.HasAny(...)`가 false이면 같은 log를 출력하고 loop를 빠져나갑니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss enrage pattern에서 duration 만료, 촉수 전멸 조기 종료, 무적 해제, `waitTimeAfterPatternEnd`, busy reset 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/EyeBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossEnragePatternRoutine.cs`
- `Assets/Scripts/SangHyup/Enemy/EyeBossTentacleRegistry.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - TrainBoss stun routine 분리

상태:
일부 완료

현재 문제:
`TrainBoss.Stun()`은 `isStunned` 상태를 켜고 끄는 책임과 `stunDuration` 대기 책임을 함께 가지고 있었습니다. Knockback 흐름에서는 이미 cooldown, force 선택, Rigidbody2D 적용이 helper로 분리되어 있었지만 stun wait는 아직 `TrainBoss` 안에 남아 있었습니다.

왜 지금 남겨두는가:
첫 단계에서는 duration wait만 `TrainBossStunRoutine`으로 분리했습니다. 이어서 serialized `stunDuration`, public API, coroutine 이름 `"Stun"`, `StopCoroutine("Stun")` / `StartCoroutine("Stun")` 순서, stunned true/false 순서를 바꾸지 않고 true-wait-false 시퀀스까지 `TrainBossStunRoutine`으로 옮겼고, 마지막으로 stun 상태 저장은 `TrainBossStunState`로 분리했습니다.

목표 구조:
`TrainBoss`는 movement gating, knockback 실행 시점, coroutine 재시작 순서를 담당합니다. `TrainBossStunState`는 stun 여부 저장을 담당하고, `TrainBossStunRoutine`은 stun 상태를 켜고, `stunDuration` 동안 기다린 뒤, stun 상태를 끄는 시퀀스를 담당합니다.

Unity 위험:
Stun duration과 상태 저장은 TrainBoss 전진 정지 체감과 knockback 후 회복 timing에 직접 영향을 줍니다. 이번 변경은 기존처럼 stunned true 이후 `stunDuration`만큼 기다리고, 이후 stunned false로 복구합니다.

리팩터링 시작 조건:
Unity 컴파일 후 TrainBoss 피격 시 stun 중 이동 정지, 연속 knockback 때 coroutine restart, `stunDuration` 이후 이동 재개가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/TrainBoss.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossStunState.cs`
- `Assets/Scripts/SangHyup/Enemy/TrainBossStunRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Boss enable state reset 값 분리

상태:
일부 완료

현재 문제:
`Boss.OnEnable()`은 HP 보정값 적용, current HP 초기화, alive 상태, screen-entry 상태, entrance 상태, sprite 표시, layer 적용, `PoolManager` 등록을 한 lifecycle 메서드에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, Unity lifecycle, sprite 표시, layer 적용, `PoolManager` 등록, HP 보정 공식, entrance coroutine을 바꾸지 않고 enable 시 reset 값 묶음만 `BossEnableState`로 분리했습니다.

목표 구조:
`Boss`는 `OnEnable()` lifecycle과 Unity side effect를 담당합니다. `BossEnableState`는 enable 시 적용할 HP/current/alive/screen-entry/entrance reset 값을 담당합니다.

Unity 위험:
보스 enable reset 값은 등장 직후 체력, 타겟 가능 상태, entrance gating에 직접 영향을 줍니다. 이번 변경은 기존처럼 current HP를 calibrated max HP와 같게 두고, alive는 true, screen-entry는 false, entrance-active는 false로 유지합니다.

리팩터링 시작 조건:
Unity 컴파일 후 Boss spawn 시 HP, alive 상태, screen-entry gating, entrance gating, Pool 등록이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Boss.cs`
- `Assets/Scripts/SangHyup/Enemy/BossEnableState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner boss warning sequence 분리

상태:
일부 완료

현재 문제:
`Spawner.BossSpawnRoutine(...)`은 boss setting 조회, 등장 로그, `GameManager.Instance.AppearBoss()`, warning UI 표시, warning 이후 대기, 실제 boss object spawn 호출을 한 coroutine에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 boss sequence, boss setting schema, public spawn method, prefab 생성, spawn/arrival 위치 처리, scene/prefab 연결을 바꾸지 않고 warning UI 표시와 post-warning wait만 `SpawnerBossWarningRoutine`으로 분리했습니다.

목표 구조:
`Spawner`는 boss coroutine 순서와 warning 이후 spawn call site를 담당합니다. `SpawnerBossStateTransition`은 boss appearance log와 `GameManager.Instance.AppearBoss()`를 담당하고, `SpawnerBossObjectSpawner`는 실제 boss object spawn 실행을 담당합니다. `SpawnerBossWarningRoutine`은 warning UI를 보여주고 `spawnDelayAfterWarning`만큼 대기하는 boss warning sequence를 담당합니다.

Unity 위험:
Boss warning sequence는 boss state 진입 후 실제 boss prefab이 생성되기 전 timing에 직접 영향을 줍니다. 이번 변경은 기존처럼 `BossWarningLoopUI.Instance`가 있을 때만 `ShowWarning()`을 호출하고, 이후 같은 `spawnDelayAfterWarning` 값만큼 대기합니다.

리팩터링 시작 조건:
Unity 컴파일 후 boss 등장 시 `GameManager` 상태 전환, warning UI 표시, warning delay, boss prefab spawn 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossWarningRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner mob batch spawn loop 분리

상태:
일부 완료

현재 문제:
`Spawner.SpawnMobBatch(...)`는 public spawn 요청과 batch coroutine 안의 spawn position 1회 선택, count loop, `PoolManager.instance.GetMob(prefab)` 호출, 위치 배치, physics reset, delay wait를 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 event-facing public API, prefab reference, spawn position 규칙, pooling 방식, physics reset, delay timing을 바꾸지 않고 batch loop만 `SpawnerMobBatchRoutine`으로 분리했습니다.

목표 구조:
`Spawner`는 public `SpawnMobBatch(...)` entry point와 spawn position 선택 함수를 제공합니다. `SpawnerMobBatchRoutine`은 같은 위치를 쓰는 batch loop, pooled mob lookup, placement, physics reset, delay wait 순서를 담당합니다.

Unity 위험:
Batch spawn은 이벤트나 효과에서 한 번에 여러 적을 생성하는 흐름일 수 있습니다. 이번 변경은 기존처럼 loop 시작 전에 spawn position을 한 번만 선택하고, 각 iteration마다 pooled mob을 받아 배치한 뒤 `delay`만큼 대기합니다.

리팩터링 시작 조건:
Unity 컴파일 후 batch spawn에서 모든 적이 기존처럼 같은 spawn position에 생성되고, physics reset과 delay timing이 유지되는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerMobBatchRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner mob spawn setup 분리

상태:
일부 완료

현재 문제:
`Spawner.SpawnMobCommon(...)`은 pooled enemy null 처리, spawn position 선택, transform 배치, physics reset, `Mob` component lookup, `OnDied` callback 중복 제거/재등록을 한 메서드에서 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 public spawn API, prefab reference, pool lookup 경로, spawn position 규칙, physics reset helper, `RespawnMob` callback 의미를 바꾸지 않고 mob spawn setup side effect만 `SpawnerMobSpawnSetup`으로 분리했습니다.

목표 구조:
`Spawner`는 public spawn API와 call site를 담당합니다. `SpawnerMobPoolLookup`은 normal/elite/event mob pool lookup 선택을 담당하고, `SpawnerMobSpawnSetup`은 받은 enemy를 선택된 위치에 배치하고 physics reset을 적용한 뒤 `Mob.OnDied` callback을 재연결하는 책임을 담당합니다.

Unity 위험:
Mob spawn setup은 적의 등장 위치, pooled Rigidbody2D 잔류 속도, death callback 재등록에 직접 영향을 줍니다. 이번 변경은 기존처럼 null enemy는 무시하고, `GetSpawnPosition(isFly)` 결과로 배치하며, `SpawnerEnemyPhysicsReset.Reset(...)` 후 같은 `RespawnMob` callback을 remove/add 순서로 재등록합니다.

리팩터링 시작 조건:
Unity 컴파일 후 normal/elite/event mob spawn에서 위치 배치, velocity reset, `OnDied` callback 재등록이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerMobSpawnSetup.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner mob pool lookup selection 분리

상태:
일부 완료

현재 문제:
`Spawner`는 basic mob, elite mob, prefab 기반 event spawn에서 어떤 `PoolManager` getter를 호출할지 직접 분기했습니다. 이 책임은 spawn type/index 선택, spawn setup, periodic timing 책임과 섞여 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public spawn API, spawn type/index 선택 규칙, pooling 함수 호출 의미, null 처리, position/setup 순서를 바꾸지 않고 `PoolManager` getter 선택만 `SpawnerMobPoolLookup`으로 분리했습니다.

목표 구조:
`Spawner`는 public spawn entry point와 spawn call site를 유지합니다. `SpawnerMobSpawnSelector`는 basic/elite type과 index를 고르고, `SpawnerMobPoolLookup`은 normal/elite/prefab pool lookup 선택을 담당하며, `SpawnerMobSpawnSetup`은 반환된 enemy의 배치와 callback wiring을 담당합니다.

Unity 위험:
Mob pool lookup은 어떤 prefab pool에서 enemy를 가져오는지에 직접 영향을 줍니다. 이번 변경은 기존처럼 basic은 `isFly`에 따라 `GetFlyMob` 또는 `GetGroundMob`, elite는 `GetFlyEliteMob` 또는 `GetGroundEliteMob`, prefab spawn은 `GetMob(prefab)`을 호출합니다.

리팩터링 시작 조건:
Unity 컴파일 후 normal, elite, prefab/event spawn에서 기존 pool에서 enemy가 반환되고, null 반환 시 기존처럼 setup helper가 무시하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerMobPoolLookup.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner boss object spawn execution 분리

상태:
일부 완료

현재 문제:
`Spawner.SpawnBossObject(...)`는 warning 이후 call site, `PoolManager` boss prefab 조회, spawn point fallback 로그, `Instantiate(...)`, `Boss` component lookup, entrance routine 시작을 한 메서드에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized boss setting, public boss spawn API, boss sequence/warning timing, prefab lookup 의미, spawn/arrival position 규칙, null 처리, log 문구, entrance behavior를 바꾸지 않고 실제 boss object spawn execution만 `SpawnerBossObjectSpawner`로 분리했습니다.

목표 구조:
`Spawner`는 boss sequence와 warning 이후 spawn call site를 담당합니다. `SpawnerBossSettingLookup`은 boss setting 조회를, `SpawnerBossSpawnPlacement`는 spawn/arrival position 해석을, `SpawnerBossObjectSpawner`는 prefab 조회, instantiate, fallback logging, entrance handoff를 담당합니다.

Unity 위험:
Boss object spawn execution은 boss prefab 생성 위치, missing spawn point 로그, entrance routine 시작 여부에 직접 영향을 줍니다. 이번 변경은 기존처럼 `PoolManager.instance` 또는 boss prefab이 없으면 반환하고, spawn point가 없으면 같은 error log를 출력하며, identity rotation으로 instantiate한 뒤 `Boss` component와 arrival point가 있을 때만 `StartEntranceRoutine(...)`을 호출합니다.

리팩터링 시작 조건:
Unity 컴파일 후 boss warning 이후 prefab 생성, missing spawn point fallback/log, arrival point가 있는 보스의 entrance routine, arrival point가 없는 보스의 즉시 패턴 진입이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossObjectSpawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossSpawnPlacement.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner boss state transition 분리

상태:
일부 완료

현재 문제:
`Spawner.BossSpawnRoutine(...)`은 boss setting 조회, boss appearance log, `GameManager.Instance.AppearBoss()` 호출, warning wait, boss object spawn call site를 한 coroutine에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized boss setting, public boss spawn API, boss sequence/warning timing, log 문구, `GameManager` state transition, boss object spawn behavior를 바꾸지 않고 boss appearance log와 `AppearBoss()` 진입만 `SpawnerBossStateTransition`으로 분리했습니다.

목표 구조:
`Spawner`는 boss coroutine 순서와 warning 이후 spawn call site를 담당합니다. `SpawnerBossStateTransition`은 boss 등장 로그와 `GameManager.Instance.AppearBoss()` 호출 순서를 담당합니다. `SpawnerBossWarningRoutine`은 warning UI와 delay를 담당하고, `SpawnerBossObjectSpawner`는 실제 boss prefab 생성과 entrance handoff를 담당합니다.

Unity 위험:
Boss state transition은 보스 상태 진입, warning 표시 전 타이밍, 이후 spawn timing에 직접 영향을 줍니다. 이번 변경은 기존처럼 boss setting 조회 후 `GameManager.Instance.gameTime`을 포함한 같은 로그를 남기고, 바로 `GameManager.Instance.AppearBoss()`를 호출한 뒤 warning wait로 넘어갑니다.

리팩터링 시작 조건:
Unity 컴파일 후 boss 등장 시 로그, `GameManager` boss state 진입, warning UI 표시, warning delay, boss prefab spawn 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossStateTransition.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossWarningRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner boss sequence reporting 분리

상태:
일부 완료

현재 문제:
`Spawner.SpawnNextBoss()`는 boss sequence cursor 사용, boss sequence 시작, 빈 sequence 경고, 다음 boss index 예약 로그를 한 메서드에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized boss sequence, public boss spawn API, cursor 증가 규칙, boss spawn timing, log 문구를 바꾸지 않고 boss sequence reporting만 `SpawnerBossSequenceLog`로 분리했습니다.

목표 구조:
`Spawner`는 다음 boss 선택과 boss sequence 시작을 담당합니다. `SpawnerBossSequenceCursor`는 cyclic index state를 담당하고, `SpawnerBossSequenceLog`는 빈 sequence warning과 다음 boss 예약 log를 담당합니다.

Unity 위험:
Boss sequence reporting은 gameplay side effect는 아니지만, 빈 boss sequence 진단과 다음 boss 예약 확인에 쓰입니다. 이번 변경은 기존처럼 빈 sequence면 warning 후 spawn을 건너뛰고, 성공 시 같은 next index와 boss name을 로그로 남깁니다.

리팩터링 시작 조건:
Unity 컴파일 후 boss sequence가 비어 있을 때 warning이 유지되고, 정상 sequence에서는 선택된 boss spawn 후 다음 예약 index 로그가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossSequenceLog.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Tentacle attack sequence 분리

상태:
일부 완료

현재 문제:
`Tentacle.AttackRoutine()`은 공격 예고 대기, `attack` 애니메이션 trigger, boss attack sound callback 호출, animation length 대기, 소멸 대기, `Destroy(gameObject)`를 한 coroutine에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, `BeginAttack()` entry point, animation trigger 이름, callback 호출 시점, destroy timing을 바꾸지 않고 공격 sequence만 `TentacleAttackRoutine`으로 분리했습니다.

목표 구조:
`Tentacle`은 setup, owner 등록, `BeginAttack()` lifecycle entry point를 담당합니다. `TentacleAttackAnimationDurationResolver`는 animation duration 해석을 담당하고, `TentacleAttackRoutine`은 공격 대기/trigger/callback/소멸 sequence를 담당합니다.

Unity 위험:
Tentacle attack sequence는 EyeBoss 패턴의 타격 타이밍, 사운드 callback, 촉수 소멸 타이밍에 직접 영향을 줍니다. 이번 변경은 기존 순서대로 wait, animator trigger, callback, animation wait, destroy delay, destroy를 유지합니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss tentacle 공격에서 예고 대기, `attack` trigger, 공격 사운드 callback, 판정 대기, 소멸 타이밍이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Assets/Scripts/SangHyup/Enemy/TentacleAttackRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Dynamic prefab mob provider 분리

상태:
일부 완료

현재 문제:
`PoolManager.GetMob(GameObject)`은 public event/prefab spawn entry point이면서 null prefab guard, dynamic pool lookup, pooled object reuse/create 요청 흐름을 직접 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, dynamic pool key, null prefab 반환, `PoolManager.transform` parent, reuse/create 순서를 바꾸지 않고 dynamic prefab mob 요청 흐름만 `PoolDynamicMobProvider`로 분리했습니다.

목표 구조:
`PoolManager`는 기존 `GetMob(GameObject)` public facade를 유지합니다. `PoolDynamicMobProvider`는 null guard 이후 `PoolObjectProvider.GetDynamicPool(...)`과 `PoolObjectProvider.GetOrCreate(...)`를 호출하는 dynamic prefab mob 요청 흐름을 담당합니다.

Unity 위험:
Dynamic prefab mob pooling은 event spawn, periodic spawn, batch spawn에서 prefab 기반 enemy를 재사용하거나 생성하는 흐름에 직접 영향을 줍니다. 이번 변경은 기존처럼 prefab이 null이면 null을 반환하고, prefab name 기반 dynamic pool에서 reusable object를 먼저 찾은 뒤 없으면 새로 생성합니다.

리팩터링 시작 조건:
Unity 컴파일 후 event/prefab spawn에서 null prefab skip, dynamic pool reuse, instantiate-on-miss parent/name/pool append가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolManager.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolDynamicMobProvider.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Indexed mob pool set 분리

상태:
일부 완료

현재 문제:
`PoolManager`는 scene-facing pooling facade이면서 normal, fly, ground elite, fly elite indexed pool 배열 저장과 네 개 public getter의 routing을 직접 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized prefab 배열, public getter signature, invalid index 반환 규칙, `PoolObjectProvider.GetIndexed(...)` reuse/create 경로, prefab/scene 연결을 바꾸지 않고 private indexed pool storage와 getter routing만 `PoolMobPoolSet`으로 묶었습니다.

목표 구조:
`PoolManager`는 Inspector-facing prefab 배열과 public getter facade를 유지합니다. `PoolMobPoolSet`은 normal/fly/elite indexed pool 배열을 초기화하고 각 getter 요청을 `PoolObjectProvider.GetIndexed(...)`로 전달합니다.

Unity 위험:
Indexed mob pool set은 자동 spawn에서 normal/fly/elite prefab pool을 찾는 핵심 경로입니다. 이번 변경은 기존처럼 `Start()` 시 네 pool 배열을 만들고, public getter는 같은 serialized prefab 배열과 같은 index를 사용해 `PoolObjectProvider.GetIndexed(...)`로 들어갑니다.

리팩터링 시작 조건:
Unity 컴파일 후 normal, fly, ground elite, fly elite spawn에서 pool 초기화, invalid index null 반환, reusable object 선택 및 instantiate-on-miss가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolManager.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolMobPoolSet.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Periodic spawn task list 분리

상태:
일부 완료

현재 문제:
`Spawner`는 public event-facing `AddPeriodicSpawnTask(...)`, 실제 prefab spawn 실행, 그리고 private `periodicSpawnTasks` list 저장/add/clear/due access/reset을 함께 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 public API, `PeriodicSpawnTask` 중첩 클래스 필드 구조, null prefab skip, 최소 interval clamp, spawn 요청 후 timer reset 순서, prefab/scene 연결을 바꾸지 않고 private task list bookkeeping만 `SpawnerPeriodicTaskList`로 분리했습니다.

목표 구조:
`Spawner`는 public periodic spawn API와 실제 `SpawnMobFromPrefab(...)` 실행을 유지합니다. `SpawnerPeriodicTaskList`는 task list storage, null-prefab add guard, due-spawn prefab/fly 상태 접근, indexed timer reset을 담당합니다. interval clamp와 timer advance math는 `SpawnerPeriodicSpawnScheduler`가 계속 담당합니다.

Unity 위험:
Periodic event spawn은 이벤트 효과가 일정 주기로 prefab enemy를 생성하는 흐름입니다. 이번 변경은 기존처럼 spawning disabled이면 처리하지 않고, list order대로 timer를 advance하며, due task는 spawn 요청 후 timer를 reset합니다.

리팩터링 시작 조건:
Unity 컴파일 후 event periodic spawn 등록, null prefab skip, interval clamp, due spawn 실행, spawn 후 timer reset, `SetSpawning(false)` gate가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerPeriodicTaskList.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Dynamic pool registry 분리

상태:
일부 완료

현재 문제:
`PoolObjectProvider.GetDynamicPool(...)`은 pooling facade 역할과 dynamic event-spawn pool의 key 생성, dictionary missing-entry 생성, lookup 반환 규칙을 함께 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 `PoolManager` public API, serialized prefab 배열, scene/prefab 연결, dynamic pool key인 `prefab.name`, `ContainsKey` 후 `Add`, `dynamicPools[key]` 반환 흐름을 바꾸지 않고 registry 규칙만 `PoolDynamicPoolRegistry`로 분리했습니다.

목표 구조:
`PoolObjectProvider`는 기존 dynamic pool lookup entry point를 유지합니다. `PoolDynamicPoolRegistry`는 dynamic pool key와 dictionary get-or-create 규칙을 담당하고, instantiate-on-miss와 inactive reusable selection은 기존 `PoolObjectProvider` / `PoolReusableObjectSelector` 경계에 남깁니다.

Unity 위험:
Dynamic pool key는 event-spawned mob prefab 이름과 pooling 재사용 범위에 직접 영향을 줍니다. 이번 변경은 기존처럼 `prefab.name`을 key로 사용하고, key가 없을 때만 새 list를 추가한 뒤 같은 dictionary entry를 반환합니다.

리팩터링 시작 조건:
Unity 컴파일 후 event-spawned mob prefab pool이 기존 이름 key로 분리되어 재사용되는지, pool miss 시 instantiate flow가 기존처럼 이어지는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolObjectProvider.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolDynamicPoolRegistry.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Indexed pool prefab resolver 분리

상태:
일부 완료

현재 문제:
`PoolObjectProvider.GetIndexed(...)`는 indexed pool entry point이면서 prefab 배열 bounds 검사, `pools[index]`와 `prefabs[index]` pair 선택, 그리고 reuse/create 호출을 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `PoolManager.GetGroundMob(...)`, `GetFlyMob(...)`, `GetGroundEliteMob(...)`, `GetFlyEliteMob(...)` public getter, serialized prefab 배열, invalid index 시 `null` 반환, `GetOrCreate(...)` reuse/create 흐름을 바꾸지 않고 indexed prefab/pool pair resolution만 `PoolIndexedPrefabResolver`로 분리했습니다.

목표 구조:
`PoolObjectProvider`는 기존 indexed pool lookup entry point와 object reuse/create 호출을 유지합니다. `PoolIndexedPrefabResolver`는 prefab array bounds와 pool/prefab pair 선택 규칙을 담당합니다.

Unity 위험:
Indexed pool resolution은 자동 spawn이 ground/fly/elite prefab 배열에서 어떤 pool과 prefab을 선택하는지에 직접 영향을 줍니다. 이번 변경은 기존처럼 `index < 0 || index >= prefabs.Length`이면 `null`로 이어지고, 유효한 index에서는 `pools[index]`와 `prefabs[index]`를 그대로 사용합니다.

리팩터링 시작 조건:
Unity 컴파일 후 normal/elite ground/fly spawn에서 invalid index는 기존처럼 spawn을 건너뛰고, valid index는 같은 prefab pool을 재사용하는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolObjectProvider.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolIndexedPrefabResolver.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Pool object factory 분리

상태:
일부 완료

현재 문제:
`PoolObjectProvider.GetOrCreate(...)`는 inactive pooled object 재사용 선택과, reusable object가 없을 때 새 GameObject를 instantiate하고 이름을 맞춘 뒤 pool에 추가하는 side effect를 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `PoolManager` public API, serialized prefab 배열, dynamic pool key, first-inactive reuse 순서, `SetActive(true)` timing, instantiate parent, `prefab.name` rename, pool append 순서를 바꾸지 않고 instantiate-on-miss 생성/등록만 `PoolObjectFactory`로 분리했습니다.

목표 구조:
`PoolObjectProvider`는 reuse-first entry point를 유지합니다. `PoolReusableObjectSelector`는 inactive object 선택과 activation을 담당하고, `PoolObjectFactory`는 reusable object가 없을 때 instantiate, name assignment, pool append를 담당합니다.

Unity 위험:
Pool object creation은 자동 spawn과 event-spawned mob 재사용에 직접 영향을 줍니다. 이번 변경은 기존처럼 주어진 parent 아래에 prefab을 instantiate하고, 생성 object 이름을 `prefab.name`으로 맞춘 뒤 같은 pool list에 추가합니다.

리팩터링 시작 조건:
Unity 컴파일 후 pool miss 상황에서 생성 object parent, name, pool append, 이후 재사용 흐름이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolObjectProvider.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolObjectFactory.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Pool list factory 분리

상태:
일부 완료

현재 문제:
`PoolObjectProvider.CreatePools(...)`는 pool initialization entry point이면서 `List<GameObject>[]` 배열 생성과 각 index의 빈 list 초기화를 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `PoolManager.Start()`의 ground/fly/elite pool 초기화 호출, serialized prefab 배열, `prefabs.Length` 기준 배열 크기, 각 index에 새 `List<GameObject>`를 넣는 동작을 바꾸지 않고 list-array allocation만 `PoolListFactory`로 분리했습니다.

목표 구조:
`PoolObjectProvider`는 pooling facade와 기존 `CreatePools(...)` entry point를 유지합니다. `PoolListFactory`는 prefab 배열 길이에 맞춘 pool list-array 생성과 per-index 빈 list 초기화를 담당합니다.

Unity 위험:
Pool list initialization은 모든 normal/fly/elite mob pool의 기본 자료구조에 영향을 줍니다. 이번 변경은 기존처럼 prefab 배열 길이와 같은 pool 배열을 만들고, 모든 index에 빈 `List<GameObject>`를 할당합니다.

리팩터링 시작 조건:
Unity 컴파일 후 `PoolManager.Start()`에서 ground/fly/elite pool 배열이 기존과 같은 길이와 list 초기화 상태를 가지는지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolObjectProvider.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolListFactory.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Base enemy death presentation 분리

상태:
일부 완료

현재 문제:
`Enemy.Die()`는 사망 상태 전환, XP/kill hook 보상 dispatch, sprite 숨김, kill particle 요청, coroutine wait 생성을 한 메서드 안에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, reward dispatch 순서, kill particle 위치/회전, `WaitForSeconds(0)` yield 동작을 바꾸지 않고 기본 enemy 사망 presentation과 wait 생성만 `EnemyDeathPresentation`으로 분리했습니다.

목표 구조:
`Enemy`는 death state와 reward dispatch 순서를 담당합니다. `EnemyDeathRewardDispatcher`는 XP와 inventory kill hook dispatch를 담당하고, `EnemyDeathPresentation`은 sprite hide, kill particle request, zero-duration completion wait 생성을 담당합니다.

Unity 위험:
Base enemy death presentation은 일반 enemy, mob, boss의 base death coroutine timing에 영향을 줍니다. 이번 변경은 기존처럼 reward dispatch 이후 sprite를 숨기고, enemy transform 위치/회전으로 kill particle을 생성 요청한 뒤 `WaitForSeconds(0)`를 yield합니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 enemy/mob/boss 사망에서 reward dispatch, sprite hide, kill particle 생성, base death wait 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyDeathPresentation.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Base enemy enable state reset 분리

상태:
일부 완료

현재 문제:
`Enemy.OnEnable()`은 current HP, alive, screen-entry reset 값과 sprite reset, layer assignment, active enemy registration side effect를 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, `CalculateCalibratedHP()` 호출, sprite reset, enemy layer assignment, `PoolManager.RegisterEnemy(...)` 호출을 바꾸지 않고 reset 값 묶음만 `EnemyEnableState`로 분리했습니다.

목표 구조:
`Enemy`는 Unity lifecycle side effect와 field 적용을 담당합니다. `EnemyEnableState`는 enable 시 current HP, alive, screen-entry reset 값만 담당합니다. Boss 쪽은 기존 `BossEnableState` 패턴을 유지합니다.

Unity 위험:
Base enemy enable reset은 pool 재사용, targetable 상태, HP 초기화에 직접 영향을 줍니다. 이번 변경은 기존처럼 current HP를 calibrated max HP로 맞추고, alive를 true로, screen-entry를 false로 reset합니다.

리팩터링 시작 조건:
Unity 컴파일 후 일반 enemy 재활성화에서 HP, alive, screen-entry, sprite reset, layer assignment, active enemy registration이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyEnableState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Pool reusable object selection 분리

상태:
일부 완료

현재 문제:
`PoolObjectProvider.GetOrCreate(...)`는 inactive pooled object를 찾고 활성화하는 reuse selection과, 없을 때 prefab instantiate/name/add를 수행하는 create-on-miss 책임을 함께 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 `PoolManager` public getter, serialized prefab array, dynamic pool key, first inactive object 선택 순서, `SetActive(true)` timing, instantiate parent, `prefab.name` rename, pool add 순서를 바꾸지 않고 reusable object lookup/activation만 `PoolReusableObjectSelector`로 분리했습니다.

목표 구조:
`PoolObjectProvider`는 pool creation, indexed/dynamic lookup, instantiate-on-miss, naming, pool add를 담당합니다. `PoolReusableObjectSelector`는 pool list에서 첫 번째 non-null inactive object를 찾아 활성화하고 반환하는 reuse selection을 담당합니다.

Unity 위험:
Pooling reuse selection은 모든 normal/elite/event-spawned mob 재사용 타이밍에 영향을 줍니다. 이번 변경은 기존처럼 list 순서상 첫 번째 inactive object를 즉시 `SetActive(true)`한 뒤 반환하고, 재사용 대상이 없을 때만 새 object를 생성합니다.

리팩터링 시작 조건:
Unity 컴파일 후 normal/elite/event mob pool 재사용에서 object activation, instantiate-on-miss, 이름 변경, pool list append 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolObjectProvider.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolReusableObjectSelector.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Pool active enemy despawn filter 분리

상태:
일부 완료

현재 문제:
`PoolActiveEnemyRegistry`는 active enemy list mutation과 reverse despawn iteration을 담당하면서, null skip과 boss 제외 despawn 조건도 같은 loop 안에서 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 `PoolManager.activeEnemies`, public `RegisterEnemy(...)` / `UnregisterEnemy(...)` / `DespawnAllEnemies(...)` / `DespawnAllEnemiesExceptBoss(...)`, reverse iteration order, `DespawnWithoutExp()` 호출, boss 제외 기준을 바꾸지 않고 despawn eligibility check만 `PoolEnemyDespawnFilter`로 분리했습니다.

목표 구조:
`PoolActiveEnemyRegistry`는 active list mutation과 reverse iteration을 담당합니다. `PoolEnemyDespawnFilter`는 null enemy skip과 boss-retained cleanup에서 `Boss` component가 있는 enemy를 제외하는 조건을 담당합니다.

Unity 위험:
Active enemy cleanup은 stage transition, boss death, scene cleanup에서 일반 enemy를 제거하고 boss를 남기는 흐름에 영향을 줍니다. 이번 변경은 기존처럼 null entry는 skip하고, boss 제외 cleanup에서는 `GetComponent<Boss>()`가 있는 enemy를 despawn하지 않습니다.

리팩터링 시작 조건:
Unity 컴파일 후 `PoolManager.DespawnAllEnemies()`와 `DespawnAllEnemiesExceptBoss()`에서 일반 enemy cleanup, boss 유지, `OnDisable()` unregister 흐름이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/PoolActiveEnemyRegistry.cs`
- `Assets/Scripts/SangHyup/Enemy/PoolEnemyDespawnFilter.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Enemy no-reward despawn completion 분리

상태:
일부 완료

현재 문제:
`Enemy.DespawnWithoutExp()`는 public cleanup entry point이면서 active hierarchy guard, `isAlive = false`, `StopAllCoroutines()`, `gameObject.SetActive(false)`를 직접 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, `PoolActiveEnemyRegistry` 호출 경로, active hierarchy guard, `isAlive` write order, coroutine stop, deactivation 순서를 바꾸지 않고 guard/completion side effect만 `EnemyDespawnWithoutExpCompletion`으로 분리했습니다.

목표 구조:
`Enemy`는 `DespawnWithoutExp()` public entry point와 alive state write를 유지합니다. `EnemyDespawnWithoutExpCompletion`은 despawn 가능 여부 확인과 coroutine stop/deactivate completion을 담당합니다.

Unity 위험:
No-reward despawn은 `PoolManager.DespawnAllEnemies()`와 `PoolActiveEnemyRegistry` cleanup 중 적의 코루틴 중단, 사망 보상 미지급, GameObject 비활성화 순서에 영향을 줍니다. 이번 변경은 기존처럼 inactive hierarchy object는 건너뛰고, active object는 `isAlive = false` 후 모든 coroutine을 멈춘 뒤 비활성화합니다.

리팩터링 시작 조건:
Unity 컴파일 후 scene cleanup, pool despawn, boss 제외 despawn에서 보상 미지급, coroutine stop, active enemy unregister 흐름이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyDespawnWithoutExpCompletion.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Enemy disable cleanup sequence 분리

상태:
일부 완료

현재 문제:
`Enemy.OnDisable()`은 Unity lifecycle entry point이면서 hit material cleanup과 `PoolManager.UnregisterEnemy(...)` 호출 순서를 직접 가지고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, `PoolManager.instance` null 동작, null material 처리, active enemy list mutation 경로를 바꾸지 않고 disable cleanup sequence만 `EnemyDisableCleanup`으로 분리했습니다.

목표 구조:
`Enemy`는 `OnDisable()` lifecycle entry point만 유지합니다. `EnemyDisableCleanup`은 disable 시 material hit flag reset과 active-enemy unregister 순서를 담당합니다. 실제 material write는 `EnemyHitMaterialController`, active list mutation은 `PoolManager`/`PoolActiveEnemyRegistry`가 계속 담당합니다.

Unity 위험:
Enemy disable cleanup은 pooling, destroy, scene transition 중 active enemy list 정합성과 hit material cleanup에 영향을 줍니다. 이번 변경은 기존처럼 `PoolManager`가 있을 때만 hit material을 false로 reset하고, 그 다음 active enemy list에서 unregister합니다.

리팩터링 시작 조건:
Unity 컴파일 후 enemy 비활성화/파괴/풀 재사용에서 hit material reset과 active enemy unregister 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyDisableCleanup.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Enemy hit effect timing 분리

상태:
일부 완료

현재 문제:
`Enemy.HitEffect()`는 `_isHit` material flag 쓰기와 `hitEffectDuration` 대기를 한 coroutine 안에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, `TakeDamage(...)` hit feedback 시작 조건, `SoundID.Enemy_Hit` 발행 타이밍, death flow, `OnDisable()` cleanup을 바꾸지 않고 hit flash timing sequence만 `EnemyHitEffectRoutine`으로 분리했습니다.

목표 구조:
`Enemy`는 damage entry point와 `HitEffect()` coroutine entry point를 담당합니다. `EnemyHitEffectRoutine`은 hit material on/wait/off sequence를 담당하고, `EnemyHitMaterialController`는 실제 `_isHit` material flag write를 담당합니다.

Unity 위험:
Enemy hit effect timing은 모든 일반 enemy/boss/tentacle 피격 feedback 체감에 영향을 줍니다. 이번 변경은 기존처럼 `_isHit` true, `hitEffectDuration` 대기, `_isHit` false 순서를 유지합니다.

리팩터링 시작 조건:
Unity 컴파일 후 enemy 피격 시 hit flash duration과 `OnDisable()` cleanup이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Enemy.cs`
- `Assets/Scripts/SangHyup/Enemy/EnemyHitEffectRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Tentacle death completion 분리

상태:
일부 완료

현재 문제:
`Tentacle.TakeDamage(...)`는 HP subtraction/death-threshold state는 `TentacleDamageState`로 분리했지만, 죽음 판정 이후 kill particle 생성, death sound 발행, `Destroy(gameObject)`를 여전히 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, hit effect 시작, hit sound 발행, kill particle 위치, death sound, destroy timing을 바꾸지 않고 death completion side effect만 `TentacleDeathCompletion`으로 분리했습니다.

목표 구조:
`Tentacle`은 damage entry point, hit effect, hit sound를 담당합니다. `TentacleDamageState`는 HP subtraction과 death threshold를 담당하고, `TentacleDeathCompletion`은 death particle, death sound, object destroy를 담당합니다.

Unity 위험:
Tentacle death completion은 EyeBoss 패턴 중 촉수 제거, owner unregister, death sound, VFX 타이밍에 직접 영향을 줍니다. 이번 변경은 기존처럼 kill particle 생성 후 `SoundID.Enemy_Die`를 publish하고, 바로 tentacle GameObject를 destroy합니다.

리팩터링 시작 조건:
Unity 컴파일 후 tentacle 피격 사망에서 hit feedback, kill particle 위치, death sound, destroy 및 `OnDestroy()` owner unregister 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Assets/Scripts/SangHyup/Enemy/TentacleDeathCompletion.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Mob stun duration wait 분리

상태:
일부 완료

현재 문제:
`Mob.Stun()`은 `isStunned` 상태 쓰기와 `stunDuration` 대기를 한 coroutine 안에서 함께 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 public `Knockback(...)`, `StartCoroutine(Stun())` 호출 순서, `Rigidbody2D.AddForce(...)`, `isStunned` write order, `stunDuration` 값을 바꾸지 않고 대기 sequence만 `MobStunRoutine`으로 분리했습니다.

목표 구조:
`Mob`은 item-facing knockback entry point, stun state write, physics impulse를 담당합니다. `MobKnockbackPolicy`는 force 계산을 담당하고, `MobStunRoutine`은 stun duration wait을 담당합니다.

Unity 위험:
Normal mob stun은 이동 정지와 knockback 체감에 직접 영향을 줍니다. 이번 변경은 기존처럼 `isStunned = true`, `stunDuration` 대기, `isStunned = false` 순서를 유지합니다.

리팩터링 시작 조건:
Unity 컴파일 후 normal mob knockback에서 스턴 지속 시간, 이동 정지, impulse 적용 순서가 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Mob.cs`
- `Assets/Scripts/SangHyup/Enemy/MobStunRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Tentacle attack total duration 계산 분리

상태:
일부 완료

현재 문제:
`Tentacle.GetTotalDuration()`은 EyeBoss 패턴 대기 시간에 쓰이는 공격 총 duration 산식을 직접 가지고 있었습니다. 이 산식은 `TentacleAttackRoutine`이 담당하는 공격 대기, animation wait, 소멸 대기 sequence와 같은 timing 책임입니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, prefab/scene 연결, `GetTotalDuration()` 호출 지점, 1.0초 fallback 산식, attack/destroy timing을 바꾸지 않고 total duration 계산만 `TentacleAttackRoutine.CalculateTotalDuration(...)`으로 분리했습니다.

목표 구조:
`Tentacle`은 `GetTotalDuration()`이라는 기존 timing entry point를 유지합니다. `TentacleAttackRoutine`은 실제 공격 sequence와 EyeBoss 패턴이 기다릴 총 duration 계산을 함께 담당합니다.

Unity 위험:
Total duration은 EyeBoss normal/enrage pattern coroutine이 촉수 공격 종료를 기다리는 시간에 직접 영향을 줍니다. 이번 변경은 기존처럼 `animationLength > 0`이면 해당 값을 쓰고, 아니면 1.0초 fallback을 더합니다.

리팩터링 시작 조건:
Unity 컴파일 후 EyeBoss tentacle pattern에서 `GetTotalDuration()` 기반 대기 시간이 기존과 같은지 확인한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Tentacle.cs`
- `Assets/Scripts/SangHyup/Enemy/TentacleAttackRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner spawn timer state 분리

상태:
일부 완료

현재 문제:
`Spawner.Update()`는 phase refresh와 실제 spawn 실행을 담당하면서 basic mob timer, elite mob timer, next boss spawn time 저장/증가/리셋까지 직접 처리했습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized field, public API, scene/prefab 연결, spawn due-check policy, spawn 호출 순서, timer reset 지점을 바꾸지 않고 private timer bookkeeping만 `SpawnerSpawnTimerState`로 분리했습니다.

목표 구조:
`Spawner`는 `Update()` 실행 흐름, phase refresh, spawn call site를 유지합니다. `SpawnerSpawnSchedule`은 due-check policy를 유지하고, `SpawnerSpawnTimerState`는 mob/elite/boss timer 저장, 증가, 리셋, next boss time 갱신을 담당합니다.

Unity 위험:
Spawn timer state는 적 출현 빈도와 보스 등장 시점에 직접 영향을 줍니다. 이번 변경은 기존처럼 mob timer는 시작/스폰 후 0으로 리셋하고, elite timer는 `eliteMobSpawnInterval`에서 시작해 `firstEliteSpawnTime` 이후에만 증가하며, boss time은 `bossSpawnInterval`에서 시작해 boss spawn 요청 후 같은 interval만큼 증가합니다.

리팩터링 시작 조건:
Unity 컴파일 후 basic/elite mob spawn 주기와 boss spawn interval이 기존과 같은지 play validation을 진행한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerSpawnTimerState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner current phase state 분리

상태:
일부 완료

현재 문제:
`Spawner.UpdatePhase(...)`는 `SpawnerPhaseSelector`로 phase 선택을 위임한 뒤에도 선택된 phase의 basic/elite range와 spawn interval을 여러 private field에 직접 복사하고 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 serialized `SpawnPhase` schema, public API, scene/prefab 연결, phase 선택 기준, spawn range 값, spawn interval 값, spawn 실행 순서를 바꾸지 않고 selected phase runtime value 저장만 `SpawnerCurrentPhaseState`로 분리했습니다.

목표 구조:
`Spawner.SpawnPhase`는 scene-authored serialized schema로 유지합니다. `SpawnerPhaseSelector`는 현재 시간 기준 phase 선택을 담당하고, `SpawnerCurrentPhaseState`는 선택된 phase의 runtime range와 interval state를 담당합니다. `Spawner`는 phase refresh entry point와 spawn execution을 유지합니다.

Unity 위험:
Current phase state는 basic/elite mob index range와 basic mob spawn interval에 직접 영향을 줍니다. 이번 변경은 phase가 선택될 때만 기존 runtime 값을 덮어쓰고, phase 선택 전 기본 spawn interval 1.0초를 유지합니다.

리팩터링 시작 조건:
Unity 컴파일 후 phase 전환 시 basic/elite mob index range와 spawn interval이 기존과 같은지 play validation을 진행한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerCurrentPhaseState.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`

## 2026-05-18 - Spawner boss spawn routine 순서 분리

상태:
일부 완료

현재 문제:
`Spawner.BossSpawnRoutine(...)`는 boss setting 조회, boss state transition, warning wait, boss object spawn 요청 순서를 직접 조합하고 있었습니다. 각 side effect helper는 이미 분리되어 있었지만 순서 조합 책임은 여전히 `Spawner`에 남아 있었습니다.

왜 지금 남겨두는가:
이번 단계에서는 public boss spawn API, serialized boss setting schema, scene/prefab 연결, warning delay 값, `GameManager.AppearBoss()` 호출 순서, boss object spawn 동작, coroutine start entry point를 바꾸지 않고 coroutine sequence 조합만 `SpawnerBossSpawnRoutine`으로 분리했습니다.

목표 구조:
`Spawner`는 `StartCoroutine(...)` entry point와 boss object spawn call site를 유지합니다. `SpawnerBossSpawnRoutine`은 setting lookup, state transition, warning wait, spawn object request 순서를 담당합니다. 실제 state entry, warning UI, object spawn side effect는 각각 기존 helper가 담당합니다.

Unity 위험:
Boss spawn routine은 boss 등장 상태 전환, warning UI, spawn delay, prefab 생성 시점에 직접 영향을 줍니다. 이번 변경은 기존처럼 setting을 먼저 조회하고, boss state로 전환한 뒤, `spawnDelayAfterWarning`만큼 기다리고, 같은 boss name으로 object spawn을 요청합니다.

리팩터링 시작 조건:
Unity 컴파일 후 scheduled/manual boss spawn에서 boss state entry, warning delay, object spawn 시점이 기존과 같은지 play validation을 진행한 뒤 다음 단계로 진행합니다.

관련 문서/파일:
- `Assets/Scripts/SangHyup/Enemy/Spawner.cs`
- `Assets/Scripts/SangHyup/Enemy/SpawnerBossSpawnRoutine.cs`
- `Docs/StructureMemory/ScriptSystems/EnemySpawnBossFlow.md`
