---
status: complete
authority: session-log
category: session-log
last_reviewed: 2026-07-23
---

# 2026-07-23 1차 안전 크래시 방어

## Goal

정상 게임 흐름과 직렬화 계약을 유지하면서 적 생명주기, 이벤트/레벨업, 엔딩의 확인된 null 및 인덱스 크래시 경로를 최소 구조 변경으로 방어한다.

## Changed

- 적 사망·비활성화·이동·피격·보스 연출에서 선택적 SpriteRenderer, Material, Rigidbody2D, Animator, 파티클, 대상 및 전역 매니저 참조를 사용 직전에 검사했다.
- Material이 없어도 `PoolManager.UnregisterEnemy(...)`가 실행되는 정리 순서를 유지했다.
- 이벤트 요청과 선택 인덱스, 가중치 그룹의 null 데이터를 검사하고, 큐 콜백에서 필수 UI 데이터가 없으면 기존 `GameManager.CloseUI()` 경로로 복구한다.
- 레벨업 UI의 중복 인스턴스 판별과 파괴 시 static 참조 해제를 수정하고, 지연 콜백 시 UI 인스턴스 및 아이템 데이터를 다시 검사한다.
- 엔딩의 키보드, 개발자 목록, 크레딧 스폰 배열과 개별 스폰 지점을 null-safe하게 처리했다.
- 공개 API, 직렬화 필드, 씬, 프리팹, ScriptableObject 에셋, 확률, 보상 및 연출 타이밍은 변경하지 않았다.

## Verification

- `git diff --check`에서 공백 오류가 없었다. Git의 기존 LF/CRLF 변환 경고만 출력되었다.
- `dotnet build LoveTrain.sln --no-restore`가 오류 0개, 기존 경고 4개로 성공했다.
- 경고는 `Effect_SpawnMobPeriodically`의 deprecated API, `Train`의 미사용 필드 2개, `BossWarningUI`의 미사용 필드이며 이번 변경 범위 밖이다.
- Unity Editor 프로세스가 실행 중이지 않음을 확인했다.

## Skipped

- 씬, 프리팹, 직렬화 계약을 변경하지 않아 Unity batchmode는 실행하지 않았다.
- Play Mode 전투, 이벤트, 레벨업, 엔딩 회귀 검증은 사용자의 수동 체크 대상으로 남겼다.

## Play Mode Checklist

- 일반/엘리트/비행 몹의 이동, 피격 이펙트, 사망 파티클, 경험치와 킬 카운트를 확인한다.
- 열차 보스의 이동, 넉백, 2페이즈 Animator·Collider 전환, 사망 후 진행을 확인한다.
- 눈 보스의 일반/광폭 촉수 패턴, 촉수 제거, 사망 후 진행을 확인한다.
- 랜덤 이벤트의 타이핑, 마우스 스킵, 선택, 결과 표시와 게임 시간 재개를 확인한다.
- 연속 레벨업 시 선택 UI가 순차 표시되고 아이템 획득·강화 후 시간이 재개되는지 확인한다.
- 엔딩의 크레딧 자동 종료, 스페이스바 스킵, 결과 패널 이동과 Start 씬 복귀를 확인한다.
- 각 흐름에서 새로운 Console Exception과 영구 일시정지가 없는지 확인한다.

## Doc Impact Check

- 별도 ActiveTask에 이번 작업 범위와 최소 구조 변경 규칙을 기록하고 완료 상태로 전환했다.
- 반복 가능한 생명주기 및 지연 UI 경계 실수를 ErrorLog에 반영했다.
- 구조 소유권과 공개 계약이 바뀌지 않아 StructureMemory와 DecisionLog는 변경하지 않았다.
