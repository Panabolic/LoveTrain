# 2026-10-06 확대 카메라에 맞춘 맵 전환 통로 배치

## 승인 및 범위

사용자가 카메라가 넓어진 만큼 다음 맵으로 이어지는 통로 배치를 다시 맞추도록 요청했다. Mode는 Implementation이며 씬 변경 위험은 High, 대상은 기존 전환 표시이다. 대화 브리프에서 Junmo의 통로 생성/도착/기차 진입 Transform 6개의 X만 조정하도록 범위를 명시했다.

통로 프리팹·PNG·PPU·scale, 선로 높이와 모든 Y/Z, 기차 리셋 위치, 기존 Tween/암전 시간, 카메라 크기/추적, StageManager 코드 및 함께 진행 중인 주행·보스·드롭 변경은 보존한다. 새 자산/Manager/런타임 표시 생성은 추가하지 않는다.

## 원인과 수정

현재 카메라는 orthographic size20.8, 콘텐츠16:9이다. 기존 size16에서 오른쪽 경계는28.444444였으며 현재36.977778로8.533333 확장됐다. 두 통로의 기존 도착 오른쪽 끝은28.55375/28.46214로 예전 경계에 맞춰져 있어 확대 화면 안에서 약8.5만큼 뒤쪽이 끊겼다.

| Transform | fileID | 이전 X | 현재 X |
| --- | --- | --- | --- |
| Tunnel1 생성 | 1258187001 | 49.6 | 58.13333 |
| Tunnel1 도착 | 1278646571 | 17.46 | 25.99333 |
| Tunnel1 기차 진입 | 1205417120 | 60 | 68.53333 |
| Tunnel2 생성 | 1595495357 | 60 | 68.53333 |
| Tunnel2 도착 | 1627044262 | 23.5 | 32.03333 |
| Tunnel2 기차 진입 | 1568049996 | 60 | 68.53333 |

6개 지점을 같은 수평 차이만큼 이동했다. 생성→도착 거리32.14/36.5와 진입−도착 거리42.54/36.5가 유지된다. 두 통로의 Y2.8/4.2와 기차 Y-7.6은 그대로다.

실제 PNG4개는355×365/PPU16이며 Tunnel2 renderer 자식 offset(-6.13161,-1.34329)을 반영했다. Simple SpriteRenderer의 오래된 `m_Size`는 실제 크기 계산에 사용하지 않았다. 생성점의 보이는 왼쪽 끝은 새 화면 오른쪽 밖에 각각약13.75/20.08 남고, 도착점의 오른쪽 끝은 화면 끝을 기존과 같이약0.109/0.018 넘는다. 진입 종료 시 기본 기차 전체도 화면 오른쪽 밖이다.

최신 StageManager는 전환 시작 시 카메라 전진량을 저장해 생성/도착/진입/리셋에 한 번 더한다. ForwardCameraFollow는 전환 중 정지하므로 화면 상대 배치는 전진량에 관계없이 유지된다. 6개 point의 scene-root 부모를 유지해 이중 offset을 방지했다.

## 정적 검증

변경 전 스냅샷과 비교해1353개 serialized record를 보존했으며 변경 record는 위6개 Transform뿐이다. 값/부모/참조 및 diff공백 검사를 확인했다. 신규 C# 수정은 없다. Unity CLI status는 Pipeline 연결 인스턴스를 찾지 못했으며 sandbox의 Editor 확인 제한 경고를 반환했다. 이를 원본 Editor가 닫혔다는 근거로 사용하지 않고 원본 프로젝트에 별도 batchmode를 실행하지 않는다.

Doc Impact Check: SessionLog + CoreRuntimeGameFlow StructureMemory의 전환 배치 메모만 갱신한다. 기존 코드/직렬화 스키마/프리팹 계약 변경과 Architecture/Contracts 승격은 없다.

## 최종 검증과 한계

- 실제 PNG 알파를 읽어 보이는 X 경계를 다시 계산했다. 카메라 전진량0/100/1000의 두 통로6개 정적 투영 사례에서 생성점의 화면 밖 여유13.75/20.08, 도착점의 오른 끝 여유0.109/0.018, 기본 기차 진입 끝의 화면 밖 여유23.86이 유지된다. 결과는 `outputs/tunnel-layout-validation/geometry-result.json`에 보관한다. Unity 렌더·물리 검사로 표현하지 않는다.
- 최신 원본/검증 복사본의 Junmo·StageManager·ForwardCameraFollow·Train·Tunnel1/2·URP/Renderer2D 설정8개 해시 일치도 확인했다. 런타임 코드는 수정하지 않았다.
- 원본이 아닌 기존 격리 복사본에서 실제 StageManager Tween과 생산 URP 렌더를 확인하는 fixture를 준비했다. 첫 GUI 실행은 결과 로그를 만들지 않았고, 셸이 프로세스를 기다리는 방식으로 재실행하자 시작 로그가 생겼다. 그러나 라이선스 IPC 채널 연결 거부·60초 타임아웃·초기화 실패/재연결 실패가 반복되어 fixture 결과와 PNG를 생성하지 못했다. 이는 실행 환경 문제이며 소스 검사 실패나 검증 성공으로 해석하지 않는다.
- 검증 launcher를 종료하고 추가 실행/라이선스 변경은 하지 않았다. 실패 근거와 동기화 목록 및 검증 코드는 [격리 검증 기록](../../outputs/tunnel-camera-validation/README.md)에 보관했다. 원본 Editor에는 별도 batchmode/PlayMode를 실행하지 않았다.
- 이번 변경의 Unity 임포트/fixture 컴파일·실제 Tween 진행·월드 렌더·PlayMode·카메라 흔들림·플레이어 빌드는 확인하지 못했다. 수동 확인은 첫/두 번째 통로 등장→도착→기차 진입 및 전진한 카메라에서 같은 배치 유지이다. 외곽 정렬은 기본 카메라를 기준으로 하며 흔들림 순간까지 화면 끝을 덮는 별도 보정은 추가하지 않았다.

최종 source/serialized 변경 범위 및 diff공백 검사를 수행했다. Doc Impact Check: SessionLog + CoreRuntimeGameFlow StructureMemory, 검증용 README/정적 JSON. DecisionLog/ErrorLog/RefactorLog 추가 및 Architecture/Contracts 승격·Presentation HTML 변경 불필요.
