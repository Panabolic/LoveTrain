# 2026-10-04 카메라 1.5배 확대와 선로 유지

사용자 승인: 카메라/배경/스폰 범위로 화면을 1.5배 넓히되 선로 크기와 높이 유지에 집중.

- Junmo Main Camera orthographic size 16→24. 16:9 월드 범위 56.89×32→85.33×48. 선로 접촉면 y≈-8.45162를 화면상 같은 높이에 유지하도록 카메라 y=4.22581.
- 실제 선로는 StageDatabase의 BeltScroll/BeltScroll 1 프리팹 내부 Lane 두 개씩. Scene의 Lane은 inactive이므로 이를 수정하지 않음.
- 프리팹 Lane 시각 x/y 1.5배, 접촉면 기준 pivot 보정, 타일 중심 간격100→150. BoxCollider2D의 y offset/height 역보정으로 월드 접촉면과 물리 두께 보존. 바닥 x범위는 타일 폭에 맞춰 확대. 몬스터/기차/총알은 원래 월드 스케일과 판정 유지.
- 단색 배경만 세로 1.5배/카메라 중심 정렬. 건물 등 배경 그림의 스케일은 유지하므로 카메라 확대에 따라 작게 보임.
- AutoScrollBackground: 최초 viewport 전체를 덮는 타일 수 확보, 좌우 초기 구간 보완, 타일을 수평 bounds로 맞춤, 카메라 중심 대신 화면 밖으로 완전히 빠진 타일을 재배치. StageManager의 스테이지 재생성에도 Start에서 적용. 동일 layerTransform 중복 등록에 대해서 초기 타일 준비는 한 번만 수행.
- 일반 지상 스폰 x×1.5, 비행 영역 x×1.5, y는 새 카메라 기준 이전 화면 위치 유지, 영역 높이8→12. 보스/터널 연출 위치와 HUD는 유지.

검증: C# 컴파일 오류0(기존 경고9). 네 선로의 collider 접촉면/물리두께 보존 및 visualScale/orthographicSize 비율 수치검사 통과. 실제 프리팹 타일 크기/월드 위치로 10개 타일 그룹 각각2000번 스크롤 수치검사에서 viewport 누락 없음. Junmo local fileID 참조 누락/ID중복 없음. QualitySettings의 URP override 확인, 카메라에 PixelPerfectCamera 없음; 패키지/렌더링 설정은 변경하지 않음.

제한: Unity Editor 연결이 없어 실제 Play Mode 화면, 선로 접지, 스테이지 전환/보스/터널 연출은 미확인. 스폰 거리 증가로 화면 진입 대기시간은 길어질 수 있으며 실제 전투 체감 확인 필요. 카메라/프리팹/씬 변경이므로 실행 중 Editor가 이전 scene 데이터를 유지하는 경우 저장된 변경을 다시 불러와야 함.

Doc Impact Check: CoreRuntimeGameFlow에 넓어진 카메라/선로 보정 계약 기록. 이전 HUD/게임플레이 변경 보존.

## 확대 비율 1.3배로 조정 (현재 설정)

사용자 요청으로 기존 1.5배 설정을 원래 화면 기준 1.3배로 대체하고 커밋/푸시.
- 카메라 size20.8, y2.535486591; 16:9 월드 범위 약73.96×41.6.
- 두 BeltScroll 프리팹 Lane 시각1.3배, 중심 간격130. Collider 접촉면/월드 물리두께 보존 및 화면상 선로 두께/무늬 크기 유지.
- 단색 배경 세로1.3배 및 새 카메라 중심 정렬. 타일 커버리지는 기존 동적 viewport 계산을 재사용.
- 지상 스폰 x는 원본×1.3, 비행 스폰 x 원본×1.3/y5.135486591/높이10.4.
- 원래 0d88ce1 설정을 기준으로 재계산하여 이전 1.5배에 추가 확대하지 않음.
- 검증: 네 선로 시각/물리 불변 조건 및 씬 local fileID 참조 검사 통과, diff whitespace 검사 통과. 이번 변경은 serialized scene/prefab 값과 문서만 포함. 실제 Unity Play Mode는 미확인.
