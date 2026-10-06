---
status: active
authority: guide
category: codex-task-brief
last_reviewed: 2026-10-06
---

# LoveTrain TaskBrief Guide

CapstoneProject와 같은 TaskBrief 체계로 의도, 변경 범위와 실행 모드를 구분한다. 최상위 작업 규칙은 [AGENTS.md](../../AGENTS.md)에 둔다.

## 요청과 승인 구분

`이해했어?`, `맞아?`, `어때?`, `알고 있어?`는 이해 확인·설계 논의이다. 이해한 내용만 답하며 코드나 Unity 자산을 수정하거나 구현을 위임하지 않는다. 이전 구현 요청에 대한 설명을 새 설계의 승인으로 취급하지 않는다.

비단순 구현은 브리프를 먼저 제시하여 계획 또는 허용 범위를 합의한다. 사용자의 명시적인 구현 지시 또는 구체적인 구현 승인 요청에 대한 동의가 있어야 구현한다. 이미 승인한 같은 범위는 다시 승인받지 않는다. 작업 중지 후에는 명시적인 재개 지시를 기다린다.

## Mode

| Mode | 동작 |
| --- | --- |
| Investigation | 조사·보고만 수행. 파일 수정 금지. |
| Planning | 계획·위험·검증·롤백을 정리. 파일 수정 금지. |
| Implementation | 승인된 계획 또는 명시적으로 허용한 범위만 구현. |
| Verification | 완료 기준에 맞춰 검증. 새 수정 금지. |
| Spike | 사용자가 허용한 폐기 가능한 실험과 정리 범위만 수행. |
| Micro-fix | 명시적으로 요청한 작은 저위험 수정을 수행. |

모드가 없거나 모호하면 Investigation을 기본으로 한다. 자동으로 모드를 전환하지 않는다. 사용자가 문서 작성·저장을 요청하면 해당 Markdown만 허용 범위로 삼으며 게임 기능의 구현 권한으로 확장하지 않는다.

## Brief Fields

- **Mode**: 현재 허용한 실행 모드.
- **Risk**: Low / Medium / High. 저장·ID, 공용 API, 씬/프리팹, 직렬화 계약, bootstrap, 어셈블리, 입력 또는 시스템 간 경계는 High로 다룬다.
- **Target Type**: Core / Framework / Leaf / Documentation.
- **Goal**: 완료 후 무엇이 달라지거나 무엇을 알아야 하는가.
- **Intent**: 목적, 보존할 구조와 사용자 경험.
- **Context**: 재현 조건, 관련 소스·자산·문서, 최근 변경.
- **Allowed**: 이번 작업에서 읽거나 변경할 파일·폴더·동작과 승인 범위.
- **Forbidden**: 보고만 할 범위, 관계없는 개선, 승인되지 않은 자산/계약 변경.
- **Done Criteria**: 완료를 판정할 관찰 가능한 조건.
- **Verification**: 정적 분석·테스트·컴파일·Unity·문서 검사 방법과 미실행 보고 기준.
- **Assumptions / Open Questions**: 확정하지 않은 사항과 구현 전에 해소할 질문.
- **Documentation Impact**: 갱신할 프로젝트 기억 문서.
- **Suggested Later**: 범위 밖 발견. 현재 작업에서 수정하지 않는다.

## Unity Asset And UI Scope

씬, 프리팹, SO 스키마, 직렬화 필드, 저장 enum 값/순서, Animator/Animation Event, Resources 경로, `.meta`/GUID, asmdef, ProjectSettings, Input Actions, Tags/Layers, bootstrap/DDOL 변경은 Allowed에 명시하고 승인받는다. 새 Manager/Singleton도 설계를 먼저 제시한다.

UI는 씬/프리팹 제작과 직렬화 참조를 기본으로 한다. 런타임 계층/Canvas/버튼/TMP 자동 생성은 프로토타입 요청 또는 사전 설계 승인에 한정한다. 기존 자동 생성 UI를 변경하거나 프리팹으로 이행하는 작업도 별도 TaskBrief 범위로 다룬다.

## Writing And Storing A Brief

빈 양식은 [TaskBrief.md](../_templates/TaskBrief.md)에서 복사한다. 구체적인 요청을 정리할 때는 저장소의 [task-brief 스킬](../../.agents/skills/task-brief/SKILL.md)을 사용할 수 있다.

```text
$task-brief 영구 강화 UI를 종류별 버튼과 단계 표시로 바꾸는 계획 브리프를 만들어줘.
```

이 호출은 브리프 작성이며 구현 지시가 아니다. 스킬은 채워진 브리프만 반환한다. 파일 수정, 테스트 또는 기능 구현을 수행하지 않는다. 파일로 저장할 필요가 있다면 해당 문서 저장을 별도로 요청한다.

단일 요청의 브리프는 대화에 둔다. 여러 대화에서 이어갈 범위는 사용자 요청과 허용 범위에 따라 `Docs/ActiveTasks/<lowercase-hyphen-id>.md`에 두고 TaskIndex를 갱신한다. `Docs/CurrentTask.md`에는 새 범위를 기록하지 않는다. LoveTrain에는 Capstone의 AHK 템플릿 도구를 설치하지 않았으므로 그 단축키를 전제로 안내하지 않는다.

## Verification And Reporting

범위 밖 문제는 Suggested Later로 보고한다. 검증 모드에서 발견한 문제를 바로 수정하지 않는다. 실제 실행한 검사만 성공으로 보고하고 컴파일, Unity 임포트, Play Mode, batchmode, 빌드를 구분한다. 같은 프로젝트의 Unity Editor가 열려 있으면 별도 batchmode를 실행하지 않는다.

기능 결과는 SessionLogs, 반복 오류는 ErrorLog, 지속 결정은 DecisionLog, 재사용 구조는 StructureMemory, 구조적 부채는 RefactorLog에 기록한다. 상세 기준은 [문서 라우터](../README.md)를 따른다.
