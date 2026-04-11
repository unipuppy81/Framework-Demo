# Unity Multiplayer Framework

재사용 가능한 Unity 멀티플레이 프레임워크를 목표로 설계한 공용 런타임 패키지입니다.  
이 저장소의 샘플 게임은 콘텐츠 자체보다 **Tick, State Sync, AOI, Diagnostics** 구조를 검증하기 위한 테스트베드 역할을 합니다.

---

## Overview

이 프로젝트는 특정 게임 전용 네트워크 코드를 만드는 것이 아니라,  
여러 Unity 프로젝트에서 공통으로 사용할 수 있는 **멀티플레이 기반 계층**을 설계하고 검증하는 것을 목표로 합니다.

핵심 목표는 다음과 같습니다.

- Fixed Tick 기반 시뮬레이션 구조
- Host authoritative 기반 상태 확정
- Input / State / Event / Spawn 메시지 분리
- Transport 추상화와 교체 가능성 확보
- AOI / Diagnostics HUD 지원
- Runtime / Sample / Docs 계층 분리

---

## Project Identity

이 프로젝트의 중심은 **게임**이 아니라 **프레임워크**입니다.

- **Runtime**  
  공용 멀티플레이 런타임 계층

- **Sample**  
  프레임워크 동작 검증용 샘플 게임

- **Docs**  
  구조, 사용 방법 문서

샘플 게임은 재미 중심의 콘텐츠가 아니라,  
프레임워크의 구조와 정책을 검증하기 위한 테스트베드입니다.

---

## High-Level Architecture

```text
Gameplay / Sample
    ↓
Session
    ↓
Serializer
    ↓
Transport
    ↓
Network Backend
```

이 구조의 목적은 **게임플레이 계층이 실제 네트워크 전송 구현 세부사항에 직접 의존하지 않도록 분리하는 것**입니다.

---

## Message Send / Receive Flow

### Send Flow

```text
Gameplay
  → Session
  → Serializer
  → Transport
  → Network Backend
```

- **Gameplay**
  - 입력 또는 게임 이벤트를 의미 있는 메시지로 생성
  - 네트워크 바이트 처리나 전송 세부 구현은 알지 않음

- **Session**
  - 상위 계층의 송신 진입점
  - 메시지 흐름, 연결 상태, 수명주기 관리

- **Serializer**
  - 메시지 객체를 전송 가능한 byte 데이터로 변환

- **Transport**
  - byte 데이터를 실제 네트워크 전송 경로로 전달

- **Network Backend**
  - Fake / Loopback / UTP 등 실제 네트워크 구현

### Receive Flow

```text
Network Backend
  → Transport
  → Serializer
  → Session
      ├─ Input
      ├─ State
      ├─ Event
      ├─ Spawn / Despawn
      └─ Diagnostic
  → Gameplay / Presentation
```

- **Transport**
  - 실제 네트워크에서 수신한 raw data 전달

- **Serializer**
  - raw byte 데이터를 메시지 객체로 복원

- **Session**
  - 메시지 종류를 해석하고 처리 경로를 분기
  - 상태 동기화, 이벤트 처리, 스폰/리스폰, 진단 정보 전달의 중심 계층

- **Gameplay / Presentation**
  - 수신된 메시지를 기반으로 상태 반영 또는 시각 표현 처리

---

## Layer Responsibilities

| Layer | Responsibility |
|---|---|
| Gameplay / Sample | 입력 수집, 게임 규칙 사용, 시각 표현, 프레임워크 검증 |
| Session | 메시지 송수신 진입점, 연결 수명주기 관리, authority 기반 처리 |
| Serializer | 메시지 ↔ byte 변환, 메시지 정책과 전송 계층 분리 |
| Transport | 실제 송수신 처리, 네트워크 백엔드 추상화 |
| Network Backend | Fake / Loopback / UTP 등 실제 연결 구현 |

---

## Authority Model

이 프로젝트는 **Host authoritative** 구조를 기준으로 설계되었습니다.

| 대상 | 책임 |
|---|---|
| 입력 수집 | Client |
| 상태 확정 | Host / Server Authority |
| HP / Score / Respawn | Authority |
| Projectile Spawn / Hit Result | Authority |
| VFX / SFX / HUD 표현 | Local Presentation |

즉, 게임 결과에 영향을 주는 핵심 상태는 authority가 최종 확정하고,  
클라이언트는 입력 수집과 표현 계층에 집중하도록 분리했습니다.

---

## Message Policy

메시지는 지속 상태와 일회성 이벤트가 섞이지 않도록 분리했습니다.

| Type | Example | Purpose |
|---|---|---|
| Input | move, dash, fire | Tick 단위 입력 명령 |
| State Snapshot | position, hp, score | 지속 상태 동기화 |
| Event | hit, respawn, score popup | 일회성 이벤트 전달 |
| Spawn / Despawn | projectile spawn, object lifecycle | 오브젝트 수명주기 변경 |
| Diagnostic | RTT, packet count, tick | 디버그 및 HUD 표시 |

이 분리의 목적은 다음과 같습니다.

- 지속 상태와 일회성 이벤트를 분리
- reliable / unreliable 정책 적용 여지 확보
- 디버깅 시 메시지 성격을 명확히 구분
- serializer / transport 경계를 단순화

---

## Key Features

- Fixed Tick 기반 시뮬레이션
- Input Buffer 기반 입력 소비 구조
- Host authoritative 상태 동기화
- Input / State / Event / Spawn 메시지 분리
- AOI 기반 전송 대상 필터링
- Diagnostics HUD 기반 Tick / RTT / Packet 정보 표시
- Transport 교체 가능 구조
- Fake / Loopback 기반 테스트 환경
- Runtime / Sample / Docs 분리

---

## Validation Goals

이 저장소는 다음 항목을 검증하는 데 초점을 둡니다.

- Tick / RTT / Packet Count HUD 확인
- AOI on/off에 따른 전송 대상 변화 비교
- Latency / Packet Loss 조건 재현
- Spawn / Hit / Respawn / Score 흐름 검증
- 샘플 게임을 통한 상태 동기화 및 authority 처리 검증

---

## Repository Structure

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
    │   ├── Gameplay/
    │   └── AOI/
    ├── Sample/
    └── Docs/
```

---

## Why This Structure

이 구조는 게임 로직, 메시지 처리, 직렬화, 전송 구현의 책임이 서로 섞이지 않도록 분리하기 위해 설계했습니다.

핵심 의도는 다음과 같습니다.

- 게임 코드가 특정 네트워크 백엔드 구현에 직접 묶이지 않도록 분리
- 메시지 처리와 전송 처리의 책임 분리
- Transport 교체 시 상위 계층 수정 범위 최소화
- 지속 상태와 일회성 이벤트 분리로 디버깅 경로 명확화
- HUD / Latency / AOI 검증이 가능한 테스트 구조 확보

즉, 이 프로젝트는 기능 추가보다 **책임 분리, 교체 가능성, 검증 가능성**을 우선한 구조입니다.

---

## Demo / Validation Scenes

이 프로젝트의 샘플 장면은 다음 항목을 시연하기 위한 테스트베드입니다.

- 멀티 클라이언트 실행
- 상태 동기화 확인
- Diagnostics HUD 표시
- AOI 적용 여부 비교
- Latency / Packet Loss 주입
- Spawn / Hit / Respawn / Score 흐름 검증

> 샘플 게임은 구조를 검증하기 위한 용도이며, 콘텐츠 볼륨 경쟁을 목표로 하지 않습니다.

---

## Documentation

- [Architecture](./Docs/Architecture.md)
- [How To Use](./Docs/HowToUse.md)

---

## Summary

Unity Multiplayer Framework는  
게임 전용 네트워크 구현이 아니라, 여러 프로젝트에 재사용 가능한 **공용 멀티플레이 기반 계층**을 목표로 설계되었습니다.

특히 다음을 핵심 가치로 둡니다.

- Gameplay / Session / Serializer / Transport / Network Backend 책임 분리
- Host authoritative 구조 기반 상태 확정
- 메시지 정책 분리
- AOI / Diagnostics 기반 검증 가능성
- 문서화와 구조 분리를 통한 재사용성 확보
