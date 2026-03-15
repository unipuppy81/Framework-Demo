# FakeTransport / Session Message Flow

## 목표
실제 서버 없이도 메시지 송수신 흐름을 검증할 수 있도록 테스트용 Transport 계층을 구성한다.  
FakeTransport와 Hub를 이용해 다중 클라이언트처럼 동작하는 환경을 만들고, Session-Serializer-Transport 흐름을 확인한다.

## 구현
- INetworkTransport Send 시그니처 확장
- ISession Send 시그니처 확장
- NetworkTransportEvent 구조 정리
- FakeTransportHub 구현
- FakeTransport 구현
- OnTransportEvent 기반 수신 흐름 연결
- Poll 기반 이벤트 처리 구조 유지
- client-a / client-b 형태의 논리 endpoint 등록 방식 적용
- 단일 프로세스 내부 다중 클라이언트 흉내 테스트 구성

## 확인 결과
- FakeTransportHub에 endpoint 단위 등록 가능
- client-a -> client-b 메시지 전달 가능
- 실제 서버 없이 송수신 흐름 검증 가능
- Connected / DataReceived 이벤트 흐름 확인
- Session -> Serializer -> Transport.Send 경로 확장 가능성 확인
- 하나의 Unity 실행 환경 안에서 A/B 논리 클라이언트 테스트 가능
- Loopback 없이도 FakeTransport만으로 Day 7 목표 충족 가능

## 다음 작업
- Session 수신 처리와 Deserialize 흐름 더 명확히 연결
- LoopbackTransport 필요 여부 정리
- 패킷 지연 주입 훅 인터페이스 자리 확보
- 실제 서버/룸 연결 구조로 확장 준비
- 상태 동기화 메시지와 입력 메시지 종류 분리