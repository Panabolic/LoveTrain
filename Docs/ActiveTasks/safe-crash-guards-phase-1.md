---
status: complete
authority: active-task
category: bugfix
task_id: safe-crash-guards-phase-1
last_reviewed: 2026-07-23
---

# 1차 안전 크래시 방어

## Goal

정상 게임 흐름과 직렬화 계약을 유지하면서, 적 생명주기·이벤트/레벨업·엔딩의 확인된 null 및 인덱스 크래시 경로를 최소 구조 변경으로 차단한다.

## Active Mode

Implementation

## Scope Authority

1. 현재 사용자 지시와 승인된 구현 계획
2. 이 ActiveTask
3. `Docs/README.md`와 기존 프로젝트 메모

## Allowed Changes

- 현재 사용자 변경과 겹치지 않는 적 생명주기, 이벤트/레벨업, 엔딩 런타임 스크립트
- early return, null/index 검사, 같은 클래스 내부의 짧은 private 정리 함수
- 이번 결과를 기록하는 ErrorLog 및 날짜별 SessionLog

## Forbidden Changes

- `GameManager`, `Train`, PoolManager, Spawner 및 Steam 연동 변경
- 씬, 프리팹, ScriptableObject 에셋, asmdef, Unity 생성 프로젝트 파일 변경
- 공개 API나 직렬화 필드의 이름·타입 변경
- 확률, 선택 순서, 이동, 공격, 보상, 연출 타이밍 변경
- 새 Manager, Singleton, 공용 계층 또는 범용 예외 처리기 추가

## Refactoring Rules

- 정상 입력 경로의 호출 순서와 결과를 유지하고 비정상 입력에서만 조기 반환한다.
- 선택적 표현 컴포넌트 누락이 정리 작업을 막지 않게 한다.
- 중첩 조건은 early return으로 줄이고 프레임 단위 경고 로그는 추가하지 않는다.
- private helper는 반복되는 정리 흐름을 단순화할 때만 사용한다.
- 대상 파일에 새로운 사용자 변경이 발견되면 해당 파일은 범위에서 제외한다.

## Risks

- 저장소에 테스트 어셈블리가 없어 실제 전투·UI·엔딩 흐름은 Play Mode 확인이 필요하다.
- UI 큐 전역 예외 복구는 제외되므로 이번 작업은 큐 진입 전과 큐 콜백의 알려진 누락 참조만 방어한다.
- 기존 문서 일부가 현재 소스보다 앞선 리팩터링 상태를 기술하므로 현재 소스와 사용자 계획을 우선한다.

## Done Criteria

- 확인된 누락 참조와 잘못된 선택 인덱스가 예외를 발생시키지 않는다.
- 정상 전투, 보상, 이벤트, 레벨업, 엔딩 순서가 변경되지 않는다.
- C# 솔루션 빌드와 정적 diff 검사가 통과한다.
- Play Mode 수동 체크표가 제공된다.

## Verification Plan

- Static/source analysis: 대상 diff 및 직접 참조 재검색
- Tests: 기존 테스트 어셈블리가 없어 별도 테스트 추가 없음
- Compile: `dotnet build LoveTrain.sln --no-restore`
- Runtime: 사용자 Play Mode 체크표
- Documentation: ErrorLog와 `Docs/SessionLogs/2026-07-23-safe-crash-guards-phase-1.md`

## Documentation Impact

- ActiveTask, ErrorLog, SessionLog
- 공개 계약이나 구조 소유권 변화가 없어 StructureMemory와 DecisionLog는 변경하지 않는다.

## Open Questions

- 없음
