# Tick Input PlayerState

## 목표
프레임에서 입력을 수집하고, 고정 Tick에서만 입력을 소비하도록 분리한다.  
입력 버퍼와 플레이어 상태 갱신 구조를 연결해 Tick 기반 액션 루프의 최소 형태를 만든다.

## 구현
- PlayerInputFrame 정의
- PlayerInputCommand 정의
- InputBuffer 구현
- PlayerState 정의
- PlayerStateMachine 구현
- LocalPlayerController 작성
- LocalPlayerView 작성
- 3D 기준 이동 구조로 정리(Vector2 입력, Vector3 상태/이동)

## 확인 결과
- Update에서 입력 수집, Tick에서 상태 갱신 분리 완료
- 이동/대시/공격 입력을 Tick 단위로 소비 가능
- 플레이어 위치/방향/상태를 별도 데이터로 관리 가능
- 시뮬레이션 결과를 View에서 반영하는 구조 확인
- 이후 카메라 기준 이동, 전투 판정, 상태 동기화로 확장 가능한 기반 마련

## 다음 작업
- 카메라 기준 이동 적용
- 공격 판정 구조 추가
- 상태 스냅샷 저장 구조 설계
- 로컬/리모트 플레이어 분리 준비