# 2026-10-06 거리 기반 스테이지와 콤보 HUD

- Mode: Implementation — 사용자에게 계획/견본을 제시하고 이벤트 느낌표·노드 문구 제거·자릿수 느낌표·거리 기준·15분 우선 규칙을 확인한 뒤 `이런식으로 만들어줘. 승인할게`로 승인받았다.
- Risk: High — 스테이지/보스/이벤트 경계 및 Junmo 씬의 표시 참조를 변경한다.
- Goal: 실제 속도가 조우 거리와 연결되고 3초 연속 처치 성과를 HUD에 표시한다.
- Status: complete — 승인된 소스·씬 연결과 격리 Unity 검증을 완료했다.

## 변경 범위와 이유

- StageManager.cs: 현재 속도 적분, 구간 길이 57,600, 배경 반복과 별도 구간 번호, 다음 구간 초기화, 전환 생명주기 정리.
- Spawner.cs: 고정 180초 예약 대신 거리 완료 조우, 900초 최종 우선, 중복 요청 방지와 경고 예약 정리.
- BossWarningLoopUI.cs: 경고 취소에 필요한 HideWarning 공개 경로 한 줄 추가. 프리팹 확인 전 Boss 진입을 거부하고, 경고가 취소되면 예약/순번을 복원해 재활성화 후 정상 조우를 다시 요청한다. 생성된 실제 보스는 이 복구의 대상이 아니다.
- Assets/EventObjectSpawner.cs: 구간 25%/50%/75% 생성. 강화 선택과 Station 접촉/공유 UI 큐 유지.
- GameManager.cs, ComboKillState.cs: 기존 일반·엘리트 처치에 3초 콤보 연결. Playing/Boss만 시간 진행, 모달 중 유지, 전환/사망/엔딩/씬 시작에서 초기화. 관찰 시계 정산으로 처치와 Update 호출 순서 차이 방지.
- StageProgressUI.cs, ComboKillUI.cs와 새 .meta: 기존 상태를 읽기만 하는 씬 표시. 새 Manager/Singleton/DDOL, 런타임 UI 생성, 저장 형식 변경 없음.
- Junmo.unity: Canvas/OtherUI에 진행 경로와 콤보 추가, 기존 TimeText 이동. 기존 스케일 800×600와 실제 800×450 콘텐츠 영역 유지. 기존 아이콘·폰트와 Unity 기본 원형 스프라이트 재사용.

현재 씬과 Item_SO/LevelUpManager에 있던 사용자 변경은 `outputs/stage-combo-validation/baseline`에 보관하고 관련 없는 파일은 수정하지 않았다. 씬은 격리 copy에서 Unity Editor API로 제작한 뒤 승인된 기존 객체와 새 객체의 serialized 블록만 이식한다. 원본 프로젝트 Editor가 열려 있어 원본 batchmode는 실행하지 않는다.

## 검증

- 순수 거리/이벤트/보스 상태: 실제 소스에서 추출한 70개 행동 검사 통과. 고정 180초 미사용, 320 속도의 45/90/135/180초 환산, 가속/감속, 3경계, 구간 초기화, 900초 우선과 중복/취소를 검사했다.
- GameManager와 콤보 실제 소스 + dependency doubles: 49개 행동 검사 통과. 정확한 3초 만료, Update 전후 처치 순서, 모달/일시정지/보스 시계, 재시작과 자릿수 문구 경계를 검사했다.
- Spawner 실제 소스 + dependency doubles: 예약/취소 37개 행동 검사 통과. 누락 프리팹, 경고 중 비활성화/재활성화, 모달 복귀, 종료와 씬 파괴, 실제 생성 후 중지 경계를 검사했다. 이 검사는 Unity scheduler 자체를 대체하지 않는다.
- 전체 현재 runtime 137개와 Editor/fixture 9개를 설치된 Unity/package 어셈블리에 연결해 MSBuild 소스 컴파일 오류 0. 생성된 원본 .csproj는 수정하지 않았다.
- 격리 Unity 6000.2.3f1에서 실제 소스 컴파일 및 씬/표시 메서드/폰트폭/배치 382개 EditMode 검사 통과. 1280×720, 1920×1080, 1024×768의 0/9/10/100킬/만료 상태 15장 UI-only RenderTexture PNG를 생성하고 시각 확인했다. 모든 변경 runtime 8개는 검증 copy와 원본 SHA256가 일치했다.
- 씬 이식은 기존 객체 4개만 수정, 새 serialized 문서 79개 추가. 기존 serialized 문서 1,225개를 보존했고 Unity 저장의 관계없는 정규화 331개는 이식하지 않았다. Item_SO/LevelUpManager의 기존 사용자 변경은 baseline hash와 일치한다.
- 마지막 공백 검사는 승인된 블록의 빈 m_Name 필드 뒤 공백 22개만 정리해 통과했다. 값/참조/레이아웃은 그대로이며 `source-snapshot.json`에 검증 이후의 서식 정리와 원본/copy SHA256 일치를 기록했다. 갱신한 문서의 로컬 링크도 확인했다.
- 격리 Junmo의 native PlayMode 44개 검사 통과. 실제 Update/LateUpdate에 의한 거리/시계 진행, 3초 만료, 9/10/100킬 표시, 공유 UI 큐 Event와 Pause의 정지/복귀, Boss의 15분 타이머/거리 정지 및 콤보 진행, 표시 disable/enable, 전환/사망 초기화를 확인했다. 실제 Spawner 코루틴의 경고 예약→비활성화 취소→재활성화 동일 보스 재예약→스폰 중지 회복도 확인했다. runtime error/exception 0이다. 입력/연료와 관계없는 일반 스폰은 fixture에서만 비활성화했고 경고 검증 전 거리는 직접 설정했다.
- 원본 Editor에는 batchmode/PlayMode를 실행하지 않았다. 플레이어 빌드, 실제 물리 충돌/키보드 입력, 전체 보스 생성·진입·처치 및 15분 전체 플레이 검증은 미실행이다. 기존 보스 순서와 경고/진입 연출은 유지했고 일정 규칙과 취소 경로를 검증했다.

## 남은 확인과 Suggested Later

- 실제 플레이에서 주행/충돌 처치, 이벤트 오브젝트 접촉, 보스 생성과 진입을 확인하는 것은 추가 수동 검증 항목이다. 이번 PlayMode fixture는 실제 처치 충돌이나 전체 보스전을 대신하지 않는다.
- 원본 SoundManager의 변경되지 않은 Boss 상태 처리에서 `BGM_Boss` 미등록 경고가 나타났다. Boss 상태 진입과 재시도에서 같은 경고를 보존했으며 이번 승인 범위에서 오디오 설정은 변경하지 않았다.
- 새 Inspector 연결은 모두 씬에 저장되어 있어 별도 수동 연결은 필요하지 않다. 초기 콤보 HUD는 숨김이며 첫 유효 처치에서 나타난다.

검증 원본과 로그는 `outputs/stage-combo-validation/`에 있다. 초기 clone은 registry manifest와 기존 local-package cache 불일치로 시작이 정체되어 task clone 프로세스만 종료하고 cache를 보관한 뒤 동일 설치 버전의 local-package manifest로 복구했다. 첫 authoring 시도는 씬 local ID 조회 방식이 잘못되어 저장 전에 실패했으며 GlobalObjectId 조회로 고친 뒤 Unity authoring이 통과했다. 두 실패 로그를 보존했다. 원본 Packages/ProjectSettings 및 사용자 저장 식별자는 변경하지 않았다.

## Doc Impact Check

SessionLog, CoreRuntimeGameFlow/EventSystemUiQueue/EnemySpawnBossFlow StructureMemory, DecisionLog를 갱신한다. RefactorLog/ErrorLog 추가와 Architecture/Contracts 승격은 불필요하다. Docs/Presentation 계층이 없고 이번 견본은 outputs의 검토 자료이므로 Presentation HTML stale candidate는 없다.
