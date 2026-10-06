# 2026-10-06 영구 강화 화면

범위: Notion `러브트레인 2 / 영구 강화요소`의 24개 항목을 Start 이후의 영구 강화 화면에 구현. 사용자는 미정 수치 확정보다 기능을 먼저 준비하도록 요청했다.

- 기존 Start 애니메이션의 `LoadPlayScene → EnterStartState` 콜백을 유지하면서, 첫 콜백에서 플레이 씬 대신 영구 강화 UI를 생성한다. 두 번째 콜백은 강화 화면에서 게임 시작을 확정할 때까지 상태를 변경하지 않는다.
- `PermanentUpgradeMenu`는 필요할 때 한 번 생성하는 uGUI 화면이다. 4열 `GridLayoutGroup`, 세로 `ScrollRect`, 카테고리 색, 비용/구매 상태, 마우스 호버 및 선택 툴팁, 목록 아래 게임 시작 버튼을 포함한다. 프로젝트의 DungGeunMo 폰트와 800×450 콘텐츠 영역을 사용한다. 한국어/영어를 지원한다.
- 하단 버튼은 기존 `playSceneName`을 로드한다. Junmo의 기존 레버 발사로 전투를 시작하는 흐름은 유지한다. 이 작업에서 씬/프리팹 YAML을 변경하지 않았다.
- `PermanentUpgradeCatalog`에 안정적인 ID, 비용, 선행 단계, 효과와 아이템 참조를 둔다. 별도 에셋이 없으면 명세의 기본 24개 목록을 사용한다. 구매 완료/선행 단계/잔액/설정 유효성을 검사하고 중복 구매를 차단한다.
- 저장은 `LoveTrain.Progression.PermanentUpgrades.v1` JSON이 영혼 잔액과 구매 ID의 공용 원본이다. 기존 `LoveTrain.Progression.Souls.v1` 잔액을 최초에 읽고 호환용으로 함께 기록한다. `TrainLevelManager`의 보상/유료 재추첨도 같은 저장 서비스를 갱신한다. 알 수 없는 구매 ID는 보존하고, 읽을 수 없는 저장은 덮어쓰지 않는다.
- Train 시작 시 연료 최대치 보너스와 질주 시간/배율을 적용한다. 기본총 공격력 보너스는 기본 발사 전략에만 적용하고 레이저 교체/복원으로 중복 적용되지 않는다. `Inventory.CanAcquireItem/CanEquipAt`에서 해금을 검사하여 제작과 이벤트 획득 경로가 같은 조건을 사용한다.

설정 및 미정 항목:

- `Assets > Create > LoveTrain > Permanent Upgrade Catalog`로 카탈로그를 만들면 24개 기본 항목이 채워진다. `Assets/Resources/PermanentUpgrades.asset`에 저장하거나 StartManager의 `upgradeCatalog`에 지정한다. 별도 설정 없이도 기본 화면과 설정된 효과는 동작한다.
- 기본총 데미지 5단계 `value`는 0으로 남겨 두었다. 원하는 증가량을 설정하면 구매가 활성화되고, 구매한 단계의 증가량은 합산된다.
- 산탄총/기관총/보스 아이템은 실제 `unlockedItem`을 지정해야 구매할 수 있다. 보스 아이템은 기획에 구체적인 에셋 이름이 없어 임의로 매핑하지 않았다. 레이저는 기존 `LaserGun_SO`를 사용한다.
- 필살기 1/2/3회 계약은 준비했지만 실제 필살기 소비자는 없다. 구현 후 `UltimateCharges`를 사용하고 `ultimateImplemented`를 활성화한다. 미완성 항목은 화면에서 준비 중으로 표시하고 영혼을 소모하지 않는다.
- 연료 +10/+20/+40/+70/+100은 각 단계의 **최종 총 보너스**로 해석했다. 질주 시간은 명세의 3/4/5초로 덮어쓴다. 현재 Train 기본 설정은 5초이므로 첫 두 단계는 오히려 짧아진다. 기존 기본 수치는 이 작업에서 변경하지 않았다. 속도 60/70/100%는 1.6/1.7/2.0배로 적용한다.

검증:

- Unity 6000.2.3f1 참조 DLL을 사용하여 현재 Assembly-CSharp 소스 131개를 `dotnet msbuild`로 컴파일했다. 오류 0개. 사전 변경의 정확한 역패치 복사본으로 재구성한 126개 소스 기준선도 컴파일했다. 생성 프로젝트는 변경하지 않고 outputs 아래 별도 검증 프로젝트를 사용했다.
- 실제 카탈로그/저장 소스와 메모리 PlayerPrefs·Unity JSON 대역 및 번역 CSV에 대한 115개 검증을 통과했다. 순차 구매/중복 구매/부족한 잔액/미설정 효과/아이템 해금/공유 지갑/저장 복원/알 수 없는 ID 보존/손상 및 필수 필드 누락/저장 실패 복구를 확인했다. 사용자의 실제 저장 데이터는 테스트에서 사용하지 않았다. 영구 강화 UI의 한국어/영어 14개 키와 CSV 271개 행의 키 중복 없음도 확인했다.
- 독립 정적 검토에서 기존 Start 콜백/Junmo 빌드 설정/4열 폭 728/800×450 안전 영역/780×430 패널/툴팁 비차단/비활성 버튼의 호버 이벤트/고유 메타데이터를 확인했다.
- 로그와 검증 보고서: `outputs/permanent-upgrades-validation/final-build.log`, `behavior-checks.log`, `baseline-reconstructed-build.log`, `ValidationReport.md`. `git diff --check` 통과. 선택적 Inspector 카탈로그 필드의 미할당 컴파일 경고 1개는 기본 카탈로그 폴백으로 처리한다.
- Unity가 실행 중이며 Pipeline 연결이 없어 batchmode를 실행하지 않았다. 실제 Play Mode의 시각 표시와 입력 검증은 별도로 필요하다.

Doc Impact Check: 공유 저장/런 시작 효과/획득 조건에 대한 StructureMemory 갱신 및 이 세션 로그.

## 승인된 종류별 노드 UI 개편

사용자: “응 이 방식이 맞아. 그대로 구현하는데 제목 밑엔 영혼 수만 표시해”. 앞서 중지한 구현을 이 지시로 재개했다. 승인된 견본은 해금류를 각각 분리한 10종 구성이다.

TaskBrief:
- Mode: Implementation.
- 목표/의도: 한 종류당 버튼 하나와 버튼 아래 작은 직사각형 진척도로 강화 단계를 표시한다. 클릭 한 번에 다음 단계 하나를 구매한다.
- 허용: PermanentUpgradeMenu/Card, 표시용 Node와 meta, Resources/PermanentUpgradeMenu 프리팹과 meta, StartMenuManager 생성 실패 가드, 관련 번역과 좁은 기억 문서.
- 금지: 기존 24단계 ID, enum, 비용/효과, 저장 JSON/키, 게임플레이/재화 획득, 기존 씬/프리팹 및 관계없는 dirty 변경.
- 완료 기준: 5열×2행 10종 버튼, 5/5/1/1/1/1/1/3/3/3 진행칸, 왼쪽부터 채움, MAX/구매불가 구매 차단과 호버 유지, 현재/다음 단계·비용 설명, 제목 아래 영혼 수 한 줄, 기존 하단 게임 시작 연결.
- 검증: 독립 runtime/Editor 소스 컴파일, 기존 진행/저장 검증과 실제 Node 구매·복원 검증, 프리팹 GUID/참조/계층/레이아웃/레이캐스트 검사. Editor 실행 중 별도 batchmode 금지.

변경:
- UI 계층 자동 생성 코드를 제거하고 Resources/PermanentUpgradeMenu 프리팹을 사용한다. Canvas 및 16:9 safe-area 계층과 TMP/버튼/진척도를 자산에서 검토할 수 있고 직렬화 참조로 제어한다. 프리팹은 비활성으로 저장하며 인스턴스 생성 후 카탈로그·노드를 연결한 다음 Show한다. scene-local이며 씬 전환으로 정리된다.
- 표시용 PermanentUpgradeNode는 24개 기존 definition을 10종으로 묶는다. 각 해금 아이템은 독립된 노드이며 새 구매 규칙이나 저장 형식을 만들지 않는다.
- Card는 버튼 아래 별도 Image 배열을 왼쪽부터 채우고 다음 구매 비용과 상태를 갱신한다. MAX와 구매불가 버튼에서도 호버 설명이 동작하도록 같은 GameObject에 pointer handler를 유지한다.
- 제목 아래는 보유 영혼만 표시한다. 견본 설명/조작 안내/기본 저장 안내는 넣지 않으며, 구매 결과나 저장 오류는 하단 메시지에서만 표시한다. 툴팁은 진척도/현재 효과/다음 효과/비용을 표시한다.
- StartMenuManager는 프리팹 생성 실패 시 null 접근을 하지 않으며 기존 씬 전환/애니메이션 콜백을 유지한다. 기본총기 수치 및 실제 해금 아이템 미설정 항목은 기존처럼 준비 중이며 구매를 차단한다.

검증 결과는 아래에 최종 기록한다. 실제 Unity Play Mode, 프리팹 임포트, 렌더링/입력, 플레이어 빌드는 별도 확인 대상이며 소스 컴파일과 혼동하지 않는다.

개편 검증:
- 최신 runtime C# 132개와 Editor C# 7개의 독립 소스 컴파일을 Unity 6000.2.3f1 참조 DLL로 수행했다. Unity 생성 프로젝트 파일은 변경하지 않았다.
- 실제 카탈로그/진행/Node 소스를 이용한 격리 검증 188개가 통과했다. 기존 저장/구매/해금 검증, 10종·24단계 매핑, 한 구매에 다음 단계 하나만 진행/차감, 잔액 부족 불변, 5/5 완료·중복 구매 차단, 재로드 진척도, 미설정 구매 차단, 누적 데미지, 단일 해금, 기존 부분 저장 및 알 수 없는 ID 보존, 번역 CSV 19개 키를 포함한다. 메모리 Unity 대역 검증이며 실제 저장/Play Mode 검증은 아니다.
- 프리팹의 476개 local fileID, 부모↔자식 계층, 외부 GUID 및 직렬화 참조를 검사했다. 10 Card/11 Button/24 passive Image 진행칸, 5열 Grid, 800×450 표시 영역, 제목 아래 영혼 한 줄, 프리팹 root/tooltip 초기 비활성, tooltip/진행칸 비레이캐스트를 확인했다. 게임 시작 버튼 클릭과 UI 입력은 기존 EventSystem을 사용한다.
- MAX 구매 직후 Button 선택이 해제되더라도 마우스가 안에 있으면 툴팁을 유지하도록 Card에서 pointer 상태를 구분했다.
- 생성 실패 가드와 GameObject 프리팹 로드→Menu 컴포넌트 확인을 검토했다. 추적 변경의 git diff --check 및 새 코드/프리팹 공백 검사를 수행했다.
- Unity Editor는 열려 있지만 Pipeline이 설치되어 있지 않아 제어 연결이 없다. 별도 batchmode를 실행하지 않았다. 이번 프리팹의 실제 Unity 임포트/Play Mode 표시·호버·선택/씬 이동 및 플레이어 빌드는 미실행이다.

수동 확인: Start→새 강화 화면, 제목 밑 영혼만 표시, 연료 구매 때 한 칸 채움/영혼 차감, 부족·준비 중·MAX 호버, 게임 재시작 후 진척도 복원, 하단 게임 시작으로 Junmo 진입, Console 확인. 미정 수치/아이템/필살기 연결은 기존 설정 작업으로 남는다.

Doc Impact Check: SessionLog(이 문서), StructureMemory(CoreRuntimeGameFlow), DecisionLog(승인된 노드/프리팹 방식). RefactorLog/ErrorLog/Architecture·Contracts/Presentation HTML 변경 불필요.
