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
- Runtime / Editor / Sample / Docs 계층 분리

---

## Project Identity

이 프로젝트의 중심은 **게임**이 아니라 **프레임워크**입니다.

- **Runtime**  
  공용 멀티플레이 런타임 계층

- **Editor**  
  설정 / 검증 / 디버깅용 도구 계층

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
