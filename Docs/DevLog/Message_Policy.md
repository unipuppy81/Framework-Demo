# Message Policy

## Message Types
- Input: tick 기반 플레이어 명령 전달
- State: 위치, 체력, 점수 등 지속 상태 전달
- Event: 피격, 리스폰 연출 등 일회성 사건 전달
- Spawn/Despawn: 네트워크 오브젝트 수명주기 변경 전달

## Rules
- 입력과 상태를 분리한다.
- 지속 상태와 일회성 이벤트를 섞지 않는다.
- 모든 동기화 대상은 NetworkId로 식별한다.
- 동기화 대상은 NetworkObjectRegistry를 통해 조회한다.