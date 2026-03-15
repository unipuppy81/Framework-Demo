# Session / Transport / Serializer

## 목표
게임 로직과 실제 네트워크 백엔드 사이에 경계 계층을 만든다.  
Transport, Serializer, Session 역할을 분리해서 상위 게임 로직이 특정 네트워크 구현(Photon 등)에 직접 의존하지 않도록 구조를 정리한다.

## 구현
- NetworkMessageType 정의
- NetworkEnvelope 정의
- IMessageSerializer 인터페이스 정의
- INetworkTransport 인터페이스 정의
- NetworkTransportEvent 구조 정의
- ISession 인터페이스 정의
- NetworkSession 구현
- Session -> Serializer -> Transport 송신 흐름 구성
- TransportEvent -> Session -> Deserialize 수신 흐름 구조 정리
- Connected / Disconnected / DataReceived / Error 이벤트 전달 경계 정리

## 확인 결과
- 게임 로직이 Transport 구현 세부사항을 직접 알지 않아도 되는 구조 마련
- NetworkEnvelope를 공통 메시지 단위로 사용할 수 있는 기반 마련
- Serializer를 통해 메시지와 byte[] 변환 경로를 분리 가능
- Session이 Transport와 Serializer를 조합하는 중간 허브 역할 수행 가능
- Transport는 실제 전송 책임만, Session은 게임 친화적 네트워크 창구 역할만 담당하도록 분리 완료
- 이후 FakeTransport, LoopbackTransport, PhotonTransport 같은 구현체를 갈아끼울 수 있는 기반 확보
- 실제 서버 연결 전에도 테스트 가능한 구조 방향 확보

## 다음 작업
- JsonMessageSerializer 같은 임시 구현체 추가
- FakeTransport 또는 LoopbackTransport 구현
- Session 송신/수신 흐름 테스트
- Connected / Disconnected / Error 이벤트 테스트
- 실제 서버 없이 다중 클라이언트 흐름 검증 준비
- 이후 Photon 등 실제 백엔드 연결 구조로 확장