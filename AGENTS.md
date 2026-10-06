# LoveTrain Project Instructions for Codex

CapstoneProject와 같은 TaskBrief 기반 작업 규칙을 사용한다. 현재 사용자 지시와 이 저장소의 기술 계약을 우선한다.

## 요청 해석과 구현 승인

- 이해 확인, 질문, 설명 요청, 설계 논의는 구현 승인이 아니다. `이해했어?`, `맞아?`, `어때?`, `알고 있어?`에는 이해한 내용이나 의견만 답한다. 사용자가 의도한 작업 모드를 먼저 구분한다.
- 이전에 구현을 승인했더라도, 현재 발화가 이해 확인이면 새 설계의 수정 작업을 시작하거나 하위 에이전트에게 구현을 맡기지 않는다.
- 비단순 구현은 TaskBrief로 목표, 의도, 변경 범위, 금지 범위, 완료 기준, 검증 방법을 구체화하고 사용자에게 제시한다. 합의한 계획 또는 명시적으로 허용한 범위만 구현한다.
- 구현 지시나 승인에는 문맥이 분명한 `구현해`, `수정해`, `진행해`, 또는 구체적인 구현 승인 요청에 대한 동의가 포함된다. 이해 여부에 대한 동의를 구현 승인으로 확대하지 않는다.
- 이미 받은 승인은 같은 범위에서 유지한다. 범위가 바뀌지 않았는데 동일한 승인을 반복해서 요청하지 않는다.
- 사용자가 중지하면 해당 구현과 하위 에이전트 작업을 중지한다. 다른 요청이나 질문을 중지된 구현의 재개 지시로 해석하지 않는다. 명시적인 재개 지시가 필요하다.
- 승인 또는 확인이 필요한 경우 적용되는 규칙과 변경 범위를 짧게 설명한다.

## Task Routing

비단순 계획이나 편집 전에는 다음 순서로 작업 범위를 확인한다.

1. 현재 사용자 지시와 프롬프트의 TaskBrief.
2. 요청이 지칭하거나 명확히 일치하는 `Docs/ActiveTasks/<task-id>.md`.
3. `Docs/TaskIndex.md`는 라우터/대시보드로만 사용한다. 활성 범위의 근거로 사용하지 않는다.
4. `Docs/README.md`에서 해당 작업의 기술 문서로 이동한다.

`Docs/CurrentTask.md`는 폐기된 호환 안내이며 새 작업의 범위를 정의하지 않는다. `DecisionLog`와 `ErrorLog`는 관련된 지속 결정, 반복 오류, 생명주기/직렬화 위험 또는 사용자 요청이 있을 때 읽는다.

## Scope Authority

범위가 충돌하면 현재 사용자 지시 → 현재 프롬프트 TaskBrief → 일치하는 ActiveTask → TaskIndex의 라우팅 정보 순서로 따른다. 범위 권한이 기술 계약을 자동으로 변경하는 것은 아니다.

## Technical Authority

이 문서의 작업·안전·검증 규칙을 지키고, 구현 문서는 다음 순서로 따른다.

1. `Docs/Contracts/` — 별도 승인으로 생성되는 경우.
2. `Docs/Architecture/` — 별도 승인으로 생성되는 경우.
3. `Docs/Guides/`.
4. `Docs/DecisionLog.md`.
5. `Docs/ErrorLog.md`.
6. `Docs/StructureMemory/`.
7. `Docs/RefactorLog.md`.
8. `Docs/SessionLogs/`.

StructureMemory와 RefactorLog는 맥락/계획 자료이며 정식 계약을 대신하지 않는다. SessionLogs는 당시의 결과 기록이며 현재 작업 승인이나 활성 범위가 아니다. 기존 문서 구조를 임의로 확장하거나 이동하지 않는다.

## Work Mode Gate

비단순 작업은 활성 모드를 먼저 식별한다. 모드가 없거나 모호하면 `Investigation`을 기본으로 한다. 모드를 자동으로 변경하지 않는다.

| Mode | 허용 동작 |
| --- | --- |
| Investigation | 조사 후 원인 후보, 관련 파일, 위험, 수정 후보를 보고한다. 파일을 수정하지 않는다. |
| Planning | 계획, 예상 변경 파일, 금지 범위, 위험, 검증, 롤백, 질문을 정리한다. 파일을 수정하지 않는다. |
| Implementation | 승인된 계획 또는 명시적으로 허용한 범위만 구현한다. |
| Verification | 변경 사항과 동작을 완료 기준에 맞춰 검증한다. 모드 전환 승인 없이 새 수정을 추가하지 않는다. |
| Spike | 사용자가 폐기 가능한 실험과 정리 범위를 명시적으로 허용했을 때만 수행한다. |
| Micro-fix | 범위와 성공 기준이 명확한 작은 저위험 수정을 명시적으로 요청했을 때 수행한다. |

사용자가 Markdown 문서의 작성·저장을 명시적으로 요청한 경우에는 해당 문서만 허용 범위로 다룬다. 이 문서 작성 권한은 설명 대상인 게임 기능의 구현 승인으로 확장되지 않는다.

비단순하거나 모호한 요청은 [TaskBriefGuide](Docs/Guides/TaskBriefGuide.md) 또는 저장소의 `$task-brief` 스킬로 정리한다. 스킬 호출 자체는 브리프만 작성하며 파일 수정·테스트·기능 구현을 수행하지 않는다. 사용자가 별도로 Markdown 저장을 요청하면 해당 문서만 허용 범위로 삼는다. 범위 밖 발견은 `Suggested Later`로 보고한다.

## Work Rules

- 현재 TaskBrief 또는 일치하는 ActiveTask의 허용 범위 안에서 작업한다. 관계없는 개선이나 정리를 묶지 않는다.
- Unity 씬, 프리팹, ScriptableObject 스키마, 직렬화 필드 이름/타입, 저장되는 enum 값/순서, Animator 파라미터, Animation Event, Resources 경로, `.meta`/GUID, asmdef, ProjectSettings, Input Actions, Tags/Layers, `DontDestroyOnLoad`/bootstrap 흐름은 명시적 승인 없이 변경하지 않는다. 승인된 작업에 필요한 새 자산과 메타데이터도 브리프의 허용 범위에 명시한다.
- 새로운 Manager, Singleton, `DontDestroyOnLoad` 객체는 설계를 제안하고 승인받은 뒤 추가한다.
- 작고 검토 가능한 변경을 우선한다. 모든 변경은 사용자 요청, 확인된 버그 또는 자신의 변경으로 필요한 정리에 연결되어야 한다.
- 사용자 변경과 진행 중인 다른 작업을 보존한다. 관계없는 변경을 되돌리거나 덮어쓰지 않는다.
- 추측에 기반한 확장성, 설정 체계, 추상화, 대체 경로를 추가하지 않는다. 발견 가능한 정보는 먼저 읽고, 위험한 미정 사항은 질문한다.
- 리팩터링은 보존할 동작과 검증 경로를 먼저 정한다. 더 작은 접근이 있다면 구현 전에 차이를 설명한다.
- 하위 에이전트도 동일한 모드, 승인, 허용·금지 범위를 따른다. 승인되지 않은 구현을 위임하지 않는다.

## Unity Presentation Rules

- 런타임 상태의 소유권은 명확하게 유지한다. UI와 툴팁은 게임 상태를 표시하고 구매 등 상태 변경은 해당 소유자에게 요청한다.
- 화면, 팝업, HUD, 버튼, 텍스트, 페이드와 표시 객체는 기본적으로 Unity 씬 또는 프리팹에서 제작·검토하고 직렬화 참조로 제어한다.
- UI 계층, Canvas, EventSystem, 버튼, TMP 텍스트, 스프라이트, 표시 객체를 런타임 코드로 자동 생성하는 설계는 사용자가 프로토타입/대체 방식을 명시적으로 요청하거나 설계를 사전에 승인한 경우에만 사용한다.
- 런타임 생성이 필요하면 구현 전에 이유, 소유권, 정리 경로, 씬/프리팹 이행 여부를 설명한다.
- 관련 표시/정리 계약이 존재하면 먼저 따른다. 없는 계약을 있는 것으로 가정하지 않는다.
- 기존 구현의 존재를 새 자동 생성 방식의 승인으로 해석하지 않는다. 이 규칙 도입 자체가 기존 UI나 bootstrap 코드의 자동 변경·이행을 승인하지도 않는다.

## Verification Rules

- Unity Editor가 같은 프로젝트를 열고 있을 때 별도의 Unity batchmode를 실행하지 않는다.
- C# 변경 후 정적/소스 검토를 수행한다. 네임스페이스, 어셈블리, 호출부, 직렬화 참조, Unity 생명주기, 중복 타입, 공백 오류를 확인한다.
- 생성된 프로젝트가 해당 소스를 포함하면 MSBuild 등 적절한 컴파일을 수행한다. Unity 생성 `.csproj`를 수동으로 고치지 않는다. 검사에 포함하지 않은 새 파일의 컴파일 성공을 주장하지 않는다.
- 사용자가 Unity Editor 컴파일 인계를 수락한 소스 분리 작업은 `.csproj` 갱신만을 이유로 막지 않는다. 소스 구조를 확인하고 Unity 컴파일/임포트를 사용자 확인 또는 미실행으로 보고한다.
- 컴파일, Unity 임포트, Play Mode, 테스트, 플레이어 빌드는 각각 실제 실행 여부와 결과를 구분한다. 실행하지 않은 검증을 성공으로 표현하지 않는다.
- batchmode가 허용되어 실행되면 성공/실패와 로그 경로를 기록한다. 미실행이면 미실행으로 보고한다.
- Markdown만 변경한 작업은 링크, 경로, 규칙 간 일관성과 변경 범위를 검사한다. 게임 테스트는 필요한 경우에만 수행한다.

## Project Memory Rules

Markdown을 프로젝트 기억의 원본으로 사용한다. 좁은 기존 문서를 갱신하고 불필요한 문서 계층을 만들지 않는다.

- `Docs/ActiveTasks/`, `Docs/TaskIndex.md`: 장기 작업의 허용·금지 범위, 완료 기준, 위험, 검증과 라우팅.
- `Docs/SessionLogs/`: 실제 변경, 이유, 검증, 수동 확인, 남은 위험. 전체 시스템 설명을 반복하지 않는다.
- `Docs/StructureMemory/`: 재사용 구조, 책임 경계, 런타임 상태 흐름, 소유권/정리/생명주기, 공용 서비스, 인터페이스, 어셈블리, MonoBehaviour, ScriptableObject, 프리팹 계약이나 여러 파일 흐름이 변경된 경우 좁은 시스템 지도를 갱신한다.
- `Docs/RefactorLog.md`: 유지하는 구조적 부채, 원인, 목표 구조, 위험, 재작업 조건과 상태. Capstone의 RefactorBacklog 역할은 이 프로젝트의 기존 문서가 맡는다.
- `Docs/DecisionLog.md`: 작업 이후에도 유지할 결정.
- `Docs/ErrorLog.md`: 반복 가능한 실수와 생명주기/직렬화/프리팹/승인 해석 문제의 예방 규칙.
- Architecture/Contracts의 생성 또는 승격은 별도 승인 없이 수행하지 않는다.
- Obsidian이나 MCP를 사용하더라도 `Docs/`가 원본이다. 문서에 대한 도구 쓰기 권한도 현재 사용자 지시와 작업 범위를 따르며 Guides/Architecture/Contracts 변경은 명시적으로 허용된 경우에 수행한다.

## Presentation HTML

현재 LoveTrain에는 `Docs/Presentation/` 계층이 없다. 이 작업 규칙을 적용하기 위해 새 HTML 계층을 만들지 않는다.

향후 해당 계층을 도입한 경우에도 Markdown이 원본이며 HTML은 요약·그룹화·시각화·원문 연결을 위한 파생 문서이다. Markdown 변경의 부수 효과로 자동 갱신하거나 문서마다 HTML을 만들지 않는다. stale 가능성이 있으면 대상과 이유를 보고한다. Capstone과 같이 `nadoman354`의 요청/승인에 따라 Presentation HTML을 변경한다.

## Done Means

비단순 작업 종료 시 변경 파일과 이유, 수행한 검증, 미실행 검증, 남은 위험/후속 결정, 갱신한 기억 문서와 Doc Impact Check를 보고한다. 해당 분류는 `SessionLog`, `StructureMemory`, `RefactorLog`, `DecisionLog`, `ErrorLog`, `Architecture/Contracts promotion candidate`, `Presentation HTML stale candidate`, 또는 문서 갱신 불필요이다.
