# Unity Multiplayer Framework

재사용 가능한 Unity 멀티플레이 프레임워크 패키지와 검증용 샘플 게임입니다.
이 프로젝트는 “게임 하나를 잘 만들었다”를 보여주기보다, 여러 프로젝트에서 재사용 가능한 멀티플레이 런타임 계층을 설계·구현·문서화했다는 점을 보여주는 것을 목표로 합니다.


## 프로젝트 목표
이 저장소는 다음 항목을 중심으로 구성됩니다.

- Fixed Tick 기반 시뮬레이션 구조
- 입력 버퍼와 상태 갱신 분리
- 상태 동기화(State Sync)
- AOI(Interest Management)
- Pooling 기반 저-GC 구조
- Diagnostics HUD 및 디버그 도구
- 성능 측정 및 문서화
- 검증용 샘플 게임

샘플 게임은 콘텐츠 자체보다 프레임워크 구조를 검증하는 테스트베드 역할을 합니다.

## 핵심 특징
- Runtime / Editor / Sample / Docs 경계 분리
- Transport 세부 구현으로부터 상위 런타임 보호
- Input / State / Event / Spawn 메시지 분류
- Authority 기반 상태 확정 구조
- AOI on/off, Pool on/off, Tick rate 비교 가능
- 문제 재현과 시연을 위한 HUD / 툴링 포함


## 저장소 구조
Assets/MultiplayerFramework/Runtime   # 공용 런타임 계층
Assets/MultiplayerFramework/Editor    # 에디터 도구 및 검증 뷰
Assets/MultiplayerFramework/Sample    # 구조 검증용 샘플 게임
Docs/                                 # Architecture / Performance / HowToUse 문서
