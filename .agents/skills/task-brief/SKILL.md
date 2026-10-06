---
name: task-brief
description: Normalize a concrete LoveTrain Unity request into a scoped TaskBrief when the user asks for a brief, work plan, or scope handoff. Produce the brief only; do not implement its feature.
---

# Task Brief

사용자의 구체적인 요청을 LoveTrain 작업 규칙에 맞는 간결한 TaskBrief로 정리한다. CapstoneProject의 모드·범위 체계를 사용하며 사용자 의도를 보존한다.

## Scope

- 브리프만 작성한다. 파일 수정, 기능 구현, 테스트, 구현 위임을 수행하지 않는다. 별도로 명시한 Markdown 저장 요청이 있으면 그 문서만 작성한다.
- `이해했어?`, `맞아?`, 설계 설명 또는 동의를 구현 승인으로 취급하지 않는다.
- 모드가 없거나 모호하면 Investigation으로 정리한다. 모드를 자동 전환하지 않는다.
- 구현 모드는 승인된 계획 또는 명시적으로 허용한 범위에만 사용한다. 브리프를 작성하는 것과 브리프의 기능을 구현하는 것은 별개이다.
- 범위 밖 발견은 Suggested Later로 보고하고 고치지 않는다. 작업 중지는 구현과 하위 에이전트에도 적용되며 명시적 재개 지시를 기다린다.
- 구체적인 요청 없이 호출하면 전체 빈 양식을 출력하지 않는다. 빈 양식 위치를 안내하고 정리할 요청을 물어본다.

## Workflow

먼저 저장소 루트의 [AGENTS.md](../../../AGENTS.md)와 [TaskBriefGuide.md](../../../Docs/Guides/TaskBriefGuide.md)를 읽는다. 명확히 일치하는 ActiveTask나 필요한 기술 문서가 있을 때만 추가로 읽는다. TaskIndex는 라우터이며 CurrentTask는 새 범위의 근거가 아니다.

다음 필드를 구분하여 채워진 브리프를 반환한다.

- Mode, Risk, Target Type, Task.
- Goal, Intent, Context.
- Allowed, Forbidden.
- Done Criteria, Verification.
- Assumptions / Open Questions, Documentation Impact.
- Suggested Later.

상세 필드 정의와 실행 모드는 가이드를 따른다. 구현에 영향을 주는 미정 사항을 조용히 확정하지 말고 가정/질문으로 표시한다. 범위가 불명확하면 읽기만 Allowed로 두고 편집을 허용하지 않는다.

Unity 자산, 직렬화/저장/ID/enum, 공용 API, 입력, asmdef, bootstrap/DDOL 변경은 위험과 승인 범위를 명시한다. UI는 씬/프리팹 제작을 기본으로 하고 승인되지 않은 런타임 자동 생성을 Forbidden에 포함한다. 별도 승인 없는 Architecture/Contracts 수정과 관계없는 정리도 Forbidden에 포함한다.

검증은 실제 필요한 검사와 미실행 보고 기준을 명시한다. 실행하지 않은 컴파일, Unity 임포트, Play Mode, batchmode, 빌드를 성공으로 표현하지 않는다. 같은 프로젝트의 Unity Editor가 열려 있으면 별도 batchmode를 실행하지 않는다.

빈 양식은 [Docs/_templates/TaskBrief.md](../../../Docs/_templates/TaskBrief.md)를 사용한다. 존재하지 않는 AHK 도구나 단축키를 안내하지 않는다.
