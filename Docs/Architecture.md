# Unity Multiplayer Framework Architecture

## 1. 문서 목적

이 문서는 현재 포트폴리오 범위에서 구현된 **Unity 멀티플레이 프레임워크의 구조와 책임 분리**를 설명한다.   
현재 버전은 **Fixed Tick 기반 시뮬레이션, Host authoritative 상태 확정, 상태 동기화, AOI, Diagnostics, 네트워크 상태 재현 환경**까지를 책임 범위로 둔다.

---

## 2. 현재 범위

### 포함
- Fixed Tick 기반 시뮬레이션
- 입력 버퍼링과 Tick 단위 소비
- Host authoritative 상태 확정
- Session / Transport / Serializer 계층 분리
- NetworkObject / NetworkId 기반 동기화 대상 관리
- 상태 동기화와 이벤트 메시지 분리
- 거리 기반 AOI 필터링
- Diagnostics HUD
- Latency / Packet Loss 재현용 시뮬레이션 환경
- 검증용 Sample 게임

---

## 3. 프로젝트 구조

현재 포트폴리오 기준 구조는 아래와 같다.

```text
Assets/
└── MultiplayerFramework/
    ├── Runtime/
    │   ├── Core/
    │   │   ├── Tick/
    │   │   ├── Session/
    │   │   ├── Transport/
    │   │   ├── Serialization/
    │   │   └── Diagnostics/
    │   ├── Netcode/
    │   │   ├── Objects/
    │   │   ├── StateSync/
    │   │   ├── Messages/
    │   │   └── Authority/
    │   ├── Gameplay/
    │   │   ├── Input/
    │   │   ├── Combat/
    │   │   ├── Spawn/
    │   │   └── Respawn/
    │   └── AOI/
    ├── Sample/
    └── Docs/
```

### 구조 의도
- **Runtime**: 특정 게임이 아니라 여러 프로젝트에서 공통으로 사용할 수 있는 런타임 계층
- **Sample**: Runtime 구조를 검증하기 위한 테스트베드
- **Docs**: 구조 설명, 사용 절차, 범위 통제, 검증 결과를 문서화하는 영역

---

## 4. 핵심 설계 원칙

### 4.1 프레임워크 중심
이 프로젝트의 중심은 게임 콘텐츠가 아니라 **멀티플레이 기반 계층**이다.  
Sample은 재미를 위한 게임이라기보다, Tick / State Sync / AOI / Diagnostics 구조를 검증하는 용도에 가깝다.

### 4.2 Host authoritative
상태 확정 책임은 Host가 가진다.  
클라이언트는 입력을 수집하고 전송하며, Host는 이를 기반으로 authoritative state를 계산하고 다시 배포한다.

### 4.3 계층 분리
네트워크 송수신, 메시지 직렬화, 게임 시뮬레이션, 시각적 보간을 한 계층에 섞지 않는다.  
이 구조를 통해 Transport 구현 세부사항이 상위 시뮬레이션 계층에 직접 새지 않도록 설계한다.

### 4.4 검증 가능성 우선
정상 상황만 처리하는 구조가 아니라, **Latency / Packet Loss 환경을 의도적으로 재현하고 HUD로 수치를 확인할 수 있는 상태**를 목표로 한다.

---

## 5. 상위 구조 요약

전체 흐름은 아래처럼 정리할 수 있다.

```text
Player Input
→ Input Buffer
→ Fixed Tick Simulation
→ Session
→ Transport 
→ Network 
→ Transport
→ Session
→ Message Handling
→ State Buffer / World Update
→ Visual Interpolation
```

핵심은 다음 두 가지다.

1. **입력 수집과 시뮬레이션 실행을 분리한다.**
2. **네트워크 계층과 게임 상태 계층을 분리한다.**

---

## 6. 주요 계층과 책임

### 6.1 Tick 계층
주요 역할:
- 프레임 기반 Update와 고정 Tick 기반 시뮬레이션 분리
- Tick 간격 유지
- TickContext 전달
- 필요 시 DeltaAccumulator 제공

대표 구성:
- `FixedTickScheduler`
- `TickContext`

설계 의도:
- 입력은 프레임 단위로 들어오더라도, 상태 갱신은 Tick 단위로 통일한다.
- 이렇게 해야 입력 소비, 상태 동기화, 시뮬레이션 검증이 같은 기준축을 갖는다.

---

### 6.2 Input 계층
주요 역할:
- 플레이어 입력 수집
- 입력을 즉시 상태에 반영하지 않고 Tick 기준으로 저장
- 한 Tick에 대응하는 입력 명령 생성

대표 구성:
- `PlayerInputCommand`
- `InputBuffer`

설계 의도:
- 입력 수집 시점과 상태 반영 시점이 다를 수 있으므로 버퍼가 필요하다.
- 이동처럼 지속 입력은 Tick마다 소비하고, 공격/점프 같은 이벤트성 입력은 해당 Tick의 명령에 포함한다.

---

### 6.3 Session 계층
주요 역할:
- 상위 시뮬레이션 계층과 하위 Transport 계층 사이의 중간 계층
- Connect / Disconnect / Poll / Send 같은 세션 생명주기 관리
- Transport 이벤트를 상위에서 이해할 수 있는 메시지 흐름으로 변환

대표 구성:
- `NetworkSession`

설계 의도:
- Session은 단순 소켓 래퍼가 아니라, **게임이 네트워크와 만나는 경계점**이다.
- 상위 계층은 “메시지를 보낸다/받는다” 수준으로 다루고, 실제 전송 세부 구현은 Session 아래로 숨긴다.

---

### 6.4 Transport 계층
주요 역할:
- 실제 송수신
- 연결 상태 관리
- 데이터 수신 이벤트 생성
- 특정 전송 백엔드의 세부 구현 보유

대표 구성:
- `INetworkTransport`
- `UnityTransportAdapter`
- `SimulatedTransportAdapter`

설계 의도:
- 상위 계층이 특정 네트워크 백엔드에 강하게 결합되지 않도록 한다.
- 실제 Transport와 네트워크 시뮬레이션용 Transport를 교체 가능하게 둔다.

---

### 6.5 Serialization 계층
주요 역할:
- 메시지 객체를 바이트 배열로 직렬화
- 수신 바이트 배열을 메시지로 역직렬화
- Envelope와 실제 payload를 구분

대표 구성:
- `IMessageSerializer`
- `JsonMessageSerializer`
- `NetworkEnvelope`

설계 의도:
- 메시지 포맷과 전송 채널을 분리한다.
- 상위 계층은 “어떤 메시지인가”에 집중하고, 실제 바이트 변환은 별도 책임으로 둔다.

---

### 6.6 Netcode 계층
주요 역할:
- 동기화 대상 식별
- 상태 스냅샷 전달
- 메시지 종류 관리
- authoritative state와 local-only event 구분

대표 구성:
- `NetworkId`
- `NetworkObject`
- `NetworkObjectRegistry`
- 상태/이벤트 메시지 구조체

설계 의도:
- 네트워크 대상 오브젝트를 일반 게임 오브젝트와 구분해 다룬다.
- 네트워크상 식별자와 씬 내 객체 참조를 연결하는 계층이 필요하다.

---

### 6.7 AOI 계층
주요 역할:
- observer별 visible set 계산
- 거리 기반 relevancy filtering
- 전송 대상 축소

대표 구성:
- `AOISystem`

설계 의도:
- 모든 상태를 모든 클라이언트에 보내지 않는다.
- 현재 프로젝트는 **거리 기반 AOI**를 사용하며, observer 위치와 target 위치를 비교해 전송 대상을 결정한다.

---

### 6.8 Diagnostics 계층
주요 역할:
- Tick, RTT, Packet Count 등 런타임 수집
- HUD 출력
- 네트워크 상태 재현 시 검증 지표 제공

대표 구성:
- `RuntimeDiagnosticsCollector`
- `NetworkDiagnosticsHud`

설계 의도:
- 멀티플레이 시스템은 “된다/안 된다”보다 “어떤 조건에서 어떻게 깨지는가”를 확인할 수 있어야 한다.
- HUD와 로그는 데모용 장식이 아니라 문제 재현용 도구다.

---

## 7. Session / Transport / Serializer / Simulation 관계

이 프로젝트에서 가장 중요한 설명 포인트는 이 관계다.

### Simulation
- Fixed Tick 기준으로 게임 상태를 갱신한다.
- 입력을 소비하고 authoritative state를 만든다.
- 메시지를 생성할 때도 Tick 기준을 유지한다.

### Session
- Simulation이 만든 메시지를 외부로 보내는 창구다.
- Transport에서 올라온 데이터를 Simulation 쪽으로 전달한다.

### Serializer
- Session이 보내고 받는 메시지를 바이트 배열로 변환한다.
- Envelope와 payload를 직렬화/역직렬화한다.

### Transport
- 실제 네트워크 송수신을 담당한다.
- 연결 이벤트, 수신 이벤트, 오류 이벤트를 생성한다.

관계를 한 줄로 정리하면 다음과 같다.

```text
Simulation은 메시지를 만든다.
Session은 메시지를 흐르게 한다.
Serializer는 메시지를 바이트로 바꾼다.
Transport는 바이트를 실제로 전달한다.
```

---

## 8. Tick 흐름

기본 Tick 흐름은 아래와 같다.

```text
Unity Update
→ 입력 수집
→ InputBuffer 저장
→ Delta Accumulator 누적
→ Tick Interval 도달 여부 확인
→ Fixed Tick 실행
→ 해당 Tick 입력 소비
→ 시뮬레이션 실행
→ 상태 생성 / 확정
→ 필요 메시지 생성
→ Session 송신
```

핵심 포인트:
- Update는 입력 수집과 표시 계층에 가깝다.
- 실제 상태 갱신은 Tick에서만 일어난다.
- 네트워크 메시지는 Tick 결과에 맞춰 생성된다.

---

## 9. 권한 모델

현재 권한 모델은 Host authoritative를 전제로 한다.

| 대상 | 클라이언트 책임 | Host 책임 |
|---|---|---|
| 이동 입력 | 입력 수집 및 전송 | 입력 소비 및 최종 상태 확정 |
| 플레이어 위치/회전 | 수신 상태 보간 및 표시 | authoritative state 계산 및 배포 |
| 데미지/체력 | 표시 | 최종 반영 |
| 스폰/리스폰 | 수신 후 로컬 반영 | 확정 및 전송 |
| HUD | 로컬 표시 | 필요 없음 |

설계 포인트:
- 클라이언트는 입력을 보낸다.
- Host는 상태를 확정한다.
- 클라이언트는 수신한 상태를 표시하고, 원격 개체는 interpolation으로 부드럽게 보정한다.

---

## 10. 메시지 정책

현재 문서에서는 메시지를 아래처럼 나눈다.

### Input
- 이동
- 공격 입력
- 점프 입력
- Tick 기반 명령

### State
- 위치
- 회전
- 체력
- 기타 지속 상태

### Event
- 단발성 상태 변화
- 피격 연출
- 점수 반영 알림

### Spawn
- 네트워크 오브젝트 생성/제거
- 수명주기 변화 전달

### Diagnostic
- RTT
- packet count
- 테스트용 측정 정보

설계 원칙:
- **상태와 이벤트를 섞지 않는다.**
- 지속 상태는 snapshot 기준으로 설명 가능해야 한다.
- 일회성 이벤트는 별도 메시지 또는 별도 처리 경로로 다룬다.

---

## 11. 원격 개체 표시와 Remote Interpolation

현재 구조에서 authoritative state와 visual representation은 동일하지 않다.

### 목적
- 원격 개체의 순간적인 튐 완화
- 지연 상황에서 화면상 움직임을 더 자연스럽게 보이게 함

### 방식
- 수신한 스냅샷을 Tick 기준으로 저장
- 현재 클라이언트 Tick보다 약간 뒤의 renderTick를 사용
- from/to snapshot 사이를 보간해 표시

### 의미
- 이 프로젝트는 완전한 rollback/lockstep 구조를 주장하지 않는다.
- 대신 **authoritative state 기반 동기화 + remote interpolation** 조합으로 현실적인 범위의 부드러운 표시를 구현한다.

---

## 12. AOI 구조

현재 AOI는 거리 기반이다.

### 목적
- 관심 영역 밖 객체 전송 감소
- 불필요한 상태 동기화 비용 감소

### 방식
- observer별 위치를 기준으로 visible set 계산
- 범위 안의 NetworkObject만 relevancy 대상으로 간주
- 진입/이탈 목록을 관리해 전송 목록 갱신

### 효과
- 모든 객체를 전체 브로드캐스트하지 않고, 필요한 대상만 전송한다.
- Diagnostics나 로그를 통해 AOI on/off 차이를 확인할 수 있다.

---

## 13. 네트워크 상태 재현 환경

현재 프로젝트는 정상 상황만 보여주지 않는다.

### 제공 기능
- Latency 적용
- Packet Loss 적용
- HUD로 상태 표시

### 목적
- 지연 환경에서 remote interpolation이 어떻게 보이는지 검증
- 손실 환경에서 상태 갱신이 어떻게 불안정해지는지 관찰
- 문제를 재현 가능한 상태로 만드는 것

이 문맥에서 네트워크 시뮬레이션은 부가 기능이 아니라 **검증 도구**에 가깝다.

---

## 14. Variation Points

현재 구조에서 의도적으로 교체 가능하게 둔 지점은 아래와 같다.

- **Transport**: 실제 전송 구현 교체 가능
- **Serializer**: 메시지 직렬화 포맷 교체 가능
- **Tick Rate**: 시뮬레이션 주기 조정 가능
- **AOI Policy**: 현재는 거리 기반이지만 정책 교체 가능
- **Diagnostics Level**: 표시/수집 범위 확장 가능

---

## 15. 정리

현재 구조의 핵심은 다음 네 가지다.

1. **Fixed Tick 기반 시뮬레이션**
2. **Host authoritative 상태 확정**
3. **Session / Transport / Serializer 분리**
4. **AOI / Diagnostics / Latency 재현을 통한 검증 가능성 확보**

