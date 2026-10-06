# 2026-10-06 Capstone TaskBrief 작업 규칙 적용

사용자 승인 범위: LoveTrain의 향후 작업을 CapstoneProject와 같은 규칙으로 진행하도록 작업 규칙과 Markdown 문서를 저장한다. 게임 기능 구현은 포함하지 않는다.

- 루트 AGENTS.md를 생성하여 요청 해석, 작업 모드, TaskBrief/승인 범위, Unity 자산/런타임 UI, 검증과 프로젝트 기억 규칙을 설정했다.
- Docs/Guides/TaskBriefGuide.md, Docs/_templates/TaskBrief.md와 저장소 task-brief 스킬을 추가했다. LoveTrain에 없는 AHK 도구나 Presentation/Contracts 계층을 설치/생성하지 않았다.
- Docs/README.md의 규칙 라우팅을 연결하고 DecisionLog에 지속 결정, ErrorLog에 확인 질문을 구현 승인으로 확대하지 않는 예방 규칙을 기록했다.
- Capstone의 RefactorBacklog 역할은 기존 Docs/RefactorLog.md를 유지한다. 기존 문서/코드/자산을 자동으로 이동·개편하지 않는다.
- 영구 강화 UI 작업은 사용자 중지 요청에 따라 계속 중지되어 있다. 이번 규칙 적용은 구현 재개, 롤백 또는 프리팹 이행 승인이 아니다.

검증:

- 로컬 Markdown 링크 44개의 대상 경로가 모두 존재함을 확인했다. 공백 검사와 git diff --check를 통과했다.
- 독립 검토에서 규칙/문서 8개의 승인·모드·중지·기존 승인 유지·문서 저장 예외·Unity UI 제작 기준과 기존 문서 라우팅의 일관성을 확인했다.
- 스킬의 frontmatter 경계, 이 파일에서 사용하는 단순 문자열 속성, 필수 name/description, 이름/폴더 일치, 길이 제한 및 미완성 scaffold 검사를 통과했다. 공식 quick_validate.py는 기본 및 번들 Python에 PyYAML이 없어 완료하지 못했으며, 위 제한된 형식 검사와 독립 문서 검토로 대신 확인했다. 새 의존성은 설치하지 않았다.
- 문서/스킬만 변경했다. Unity 컴파일·Play Mode·게임 테스트는 실행하지 않았다.

Doc Impact Check: DecisionLog + ErrorLog + SessionLog, 작업 규칙/가이드/템플릿/저장소 스킬 및 Docs 라우터 갱신. Presentation HTML 계층 없음.
