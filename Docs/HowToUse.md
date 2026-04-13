# Unity Multiplayer Framework How To Use

## 1. 문서 목적

이 문서는 현재 포트폴리오 버전의 Unity Multiplayer Framework를 **처음 사용하는 개발자 기준으로 붙이는 절차**를 정리한다.  
핵심 목적은 내부 구현 전체를 설명하는 것이 아니라, **어떤 순서로 구조를 이해하고 샘플을 실행하고 확장해야 하는지**를 알려주는 데 있다.

현재 문서는 아래 범위를 기준으로 한다.
- Fixed Tick
- Session / Transport / Serializer
- NetworkObject 등록
- 상태 동기화
- AOI
- Diagnostics HUD
- Latency / Packet Loss 테스트

---

## 2. 시작 전에 알아둘 점

핵심 전제는 아래와 같다.

1. 상태 갱신 기준은 프레임이 아니라 Tick이다.
2. Host가 authoritative state를 확정한다.
3. 클라이언트는 입력을 보내고 상태를 수신한다.
4. 원격 개체는 interpolation으로 표시될 수 있다.
5. AOI와 Diagnostics는 선택 기능이 아니라 검증 구조의 일부다.

---

## 3. 폴더 이해

```text
Assets/
└── MultiplayerFramework/
    ├── Runtime/
    ├── Sample/
    └── Docs/
```

### Runtime
실제 공용 멀티플레이 계층이 들어있는 영역이다.

### Sample
Runtime 구조를 실행하고 검증하기 위한 예제 씬/예제 로직 영역이다.

### Docs
Architecture, HowToUse, 성능 정리, 범위 통제 같은 문서를 두는 영역이다.

---

## 4. 기본 사용 순서

처음 붙을 때는 아래 순서로 보는 것이 가장 빠르다.

1. `Architecture.md`를 먼저 읽어 구조를 이해한다.
2. Runtime의 Tick / Session / Transport / Serialization 계층을 확인한다.
3. Sample 씬을 실행해 기본 동작을 본다.
4. Diagnostics HUD를 켜서 Tick, RTT, Packet Count를 확인한다.
5. Latency / Packet Loss를 적용해 정상/지연/손실 상황 차이를 본다.
6. 필요한 경우 NetworkObject, 메시지, AOI 정책을 확장한다.

---

## 5. 샘플 실행 목적

### 확인 포인트
- Host / Client 기본 연결
- 입력 수집과 Tick 소비
- authoritative state 반영
- 원격 상태 수신
- remote interpolation 동작
- AOI on/off 차이
- latency / packet loss 재현
- HUD 수치 변화

---

## 6. 핵심 흐름 이해하기

### 6.1 입력 처리
1. Unity Update에서 입력을 수집한다.
2. 입력을 즉시 상태에 반영하지 않고 InputBuffer에 저장한다.
3. Fixed Tick이 실행될 때 해당 Tick 입력을 소비한다.
4. 시뮬레이션이 상태를 갱신한다.

### 6.2 네트워크 송신
1. 시뮬레이션 결과 또는 입력 메시지를 만든다.
2. Session에 전달한다.
3. Session이 Serializer를 사용해 메시지를 바이트 배열로 만든다.
4. Transport가 실제 송신을 수행한다.

### 6.3 네트워크 수신
1. Transport가 데이터를 수신한다.
2. Session이 수신 이벤트를 처리한다.
3. Serializer가 바이트 배열을 메시지로 복원한다.
4. 상위 계층이 메시지를 해석해 상태 버퍼나 월드에 반영한다.

---

## 7. Tick 시스템 사용 원칙

### Update에서 할 일
- 플레이어 입력 수집
- HUD 표시 갱신
- visual interpolation

### Tick에서 할 일
- 입력 소비
- authoritative state 계산
- 상태 확정
- 네트워크 메시지 생성

이 원칙이 무너지면 프레임 의존 로직과 시뮬레이션 로직이 섞이기 시작한다.

---

## 8. Session / Transport / Serializer 붙이는 법

### Session
Session은 상위 게임 로직이 네트워크를 다루는 기본 창구다.

기본적으로 아래 역할만 이해하면 된다.
- 연결 시작
- 연결 종료
- Poll 호출
- 메시지 송신
- 수신 메시지 전달

### Transport
Transport는 실제 네트워크 백엔드 구현이다.

현재 구조에서는 다음 두 방향으로 이해하면 된다.
- 실제 전송용 Transport
- 테스트/재현용 Simulated Transport

### Serializer
Serializer는 메시지와 바이트 배열 변환 책임을 가진다.

### 연결 원칙
- 게임 로직은 Transport 구현 세부사항을 직접 몰라도 된다.
- Session이 중간 계층이 되어 상위와 하위를 연결한다.
- Serializer는 Session과 함께 메시지 경계를 통일한다.

---

## 9. 새 NetworkObject를 붙일 때

새 동기화 대상을 추가할 때는 아래 순서를 따른다.

### 1단계. NetworkObject 식별 가능 상태 만들기
- 대상 오브젝트가 `NetworkId`를 가질 수 있어야 한다.
- 네트워크상에서 유일하게 식별 가능해야 한다.

### 2단계. Registry에 등록 가능해야 한다
- Host 기준으로 해당 객체를 `NetworkObjectRegistry`에서 조회할 수 있어야 한다.
- AOI나 상태 동기화는 보통 Registry를 기준으로 대상을 찾는다.

### 3단계. 동기화할 데이터 정하기
예:
- Position
- Rotation
- HP
- Respawn 상태

이 단계에서 중요한 것은 **지속 상태와 일회성 이벤트를 구분하는 것**이다.

### 4단계. 메시지 경로 연결하기
- 필요한 Input / State / Event / Spawn 메시지를 정의한다.
- Session을 통해 송수신 경로를 연결한다.

### 5단계. 표시 계층 분리 여부 판단하기
- 원격 개체라면 visual interpolation이 필요한지 판단한다.
- authoritative transform과 visual transform을 분리할지 결정한다.

---

## 10. 상태와 이벤트를 구분하는 기준

새 메시지를 추가할 때 가장 먼저 판단해야 하는 기준이다.

### State로 다룰 것
- 시간이 지나도 유지되는 값
- 최신 상태만 알아도 되는 값
- 예: 위치, 회전, 체력

### Event로 다룰 것
- 순간적인 발생 사실이 중요한 값
- 연속 상태가 아니라 단발성 처리
- 예: 피격 연출, 점수 팝업, 일시적 알림

### Spawn/Despawn로 다룰 것
- 객체 수명주기 변경
- 생성/삭제 자체가 중요한 경우

상태와 이벤트를 섞으면 디버깅이 어려워지고, 메시지 정책도 흐려진다.

---

## 11. AOI를 사용할 때

현재 AOI는 거리 기반이다.

### 사용하는 이유
- 관심 영역 밖 객체까지 전부 보내지 않기 위해서
- 전송량과 처리량을 줄이기 위해서

### 기본 확인 항목
- observer 기준 위치가 올바른가
- visible set이 갱신되는가
- AOI on/off 차이가 HUD 또는 로그로 보이는가

### 확장 시 주의점
- AOI 정책이 바뀌어도 상위 시뮬레이션 구조는 크게 흔들리지 않아야 한다.
- 가능하면 정책 객체나 시스템 단위로 분리해 교체 포인트를 유지한다.

---

## 12. Latency / Packet Loss 테스트 방법

이 프로젝트의 중요한 강점 중 하나는 **문제 상황 재현 가능성**이다.

### 기본 테스트 순서
1. 기본 환경에서 Host / Client 움직임과 상태 동기화를 확인한다.
2. Diagnostics HUD에서 Tick, RTT, Packet Count를 확인한다.
3. Latency를 적용한다.
4. 원격 개체가 authoritative state를 뒤따라가는 모습을 확인한다.
5. Packet Loss까지 추가한다.
6. 끊김과 상태 갱신 저하를 확인한다.

### 확인 포인트
- 지연 적용 시 interpolation이 어떤 시각적 완충 역할을 하는가
- 손실 적용 시 어떤 식으로 화면 품질이 저하되는가
- HUD 수치가 설정한 조건과 일치하는가

---

## 13. Diagnostics HUD 보는 법

HUD는 단순 표시가 아니라, 재현과 확인을 위한 디버깅 도구다.

### 최소 확인 항목
- Local Tick
- Remote Tick 또는 수신 상태 기준 값
- RTT
- Packet Count

### 볼 때 체크할 것
- 정상 환경에서 값이 안정적인가
- Latency 적용 시 RTT가 증가하는가
- Packet Loss 적용 시 수신/보간 상태가 달라지는가

---

## 14. Transport를 교체할 때

현재 구조는 Transport 세부 구현을 상위 계층에서 숨기도록 설계되어 있다.

### 교체 시 원칙
1. `INetworkTransport` 인터페이스 계약을 유지한다.
2. 연결, 해제, 송신, Poll, 이벤트 전달 방식이 맞아야 한다.
3. Session이 기존 방식대로 사용할 수 있어야 한다.
4. 상위 시뮬레이션 코드는 Transport 교체 사실을 크게 의식하지 않아야 한다.

즉, Transport를 바꿀 때는 **하위 구현은 바뀌어도 상위 메시지 흐름은 유지**되어야 한다.

---

## 15. Serializer를 교체할 때

Serializer를 바꿀 때는 아래를 먼저 확인한다.

- `NetworkEnvelope` 구조가 유지되는가
- payload 직렬화/역직렬화 경로가 유지되는가
- 메시지 타입 식별이 안정적인가
- 기존 Session 흐름과 충돌하지 않는가

Serializer 변경은 단순 포맷 교체가 아니라, 메시지 경계 설계를 건드리는 작업이므로 주의가 필요하다.

---

## 16. 새 기능을 추가할 때 추천 순서

새 기능을 붙일 때는 아래 순서를 권장한다.

1. 이 기능이 State인지 Event인지 먼저 결정한다.
2. authoritative 책임이 Host인지, 로컬 표시 전용인지 구분한다.
3. 필요한 메시지 구조를 정의한다.
4. Tick 기준으로 처리할지, 표시 계층에서만 처리할지 결정한다.
5. AOI 영향 대상인지 확인한다.
6. Diagnostics로 검증 가능한 수단을 같이 만든다.

이 순서를 지키면 기능을 추가해도 구조가 무너지지 않는다.

---

## 17. 현재 버전에서 주의할 점

1. Editor 도구가 없으므로 수동 설정과 런타임 확인에 의존한다.
2. Pooling과 Projectile 시스템은 아직 없으므로 해당 기준으로 구조를 설명하지 않는다.
3. rollback/lockstep 같은 고난도 네트워크 기법을 구현한 것으로 과장하지 않는다.
4. 현재 강점은 완성형 상용 엔진이 아니라, **구조 분리와 검증 가능한 멀티플레이 런타임 계층**이다.

---

## 18. 정리

현재 버전의 Unity Multiplayer Framework를 사용하는 핵심 방법은 복잡하지 않다.

- Tick 기준으로 상태를 본다.
- Session을 네트워크 경계로 사용한다.
- Transport는 교체 가능한 하위 구현으로 본다.
- NetworkObject와 메시지 정책으로 동기화 대상을 관리한다.
- AOI와 Diagnostics로 검증한다.
- Latency / Packet Loss 환경까지 확인한다.

이 문서는 기능 추가 전에 구조를 무너뜨리지 않기 위한 **기준 문서**로 사용한다.
