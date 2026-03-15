using System;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// 네트워크 전송 계층 공통 인터페이스
    /// 실제 소켓, Photon, Loopback, Fake Transport 등이 이 인터페이스를 구현
    /// Session은 이 인터페이스만 알고, 하위 구현 방식은 몰라도 되도록 분리
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Transport 레벨 이벤트를 상위(Session)로 전달
        /// 예: 연결 성공, 연결 종료, 데이터 수신, 에러 발생
        /// </summary>
        event Action<NetworkTransportEvent> OnTransportEvent;

        /// <summary>
        /// 대상 주소, 룸 또는 테스트용 endpoint에 연결
        /// </summary>
        void Connect(string endpoint);

        /// <summary>
        /// 연결 종료
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 바이트 데이터를 전송
        /// targetEndpoint를 지정하면 특정 대상
        /// 구현에 따라 null 또는 빈 값은 기본 대상/브로드캐스트로 처리
        /// </summary>
        void Send(byte[] data, string targetEndpoint = null);

        /// <summary>
        /// Transport 내부 큐를 갱신
        /// Loopback/Fake 구현에서 특히 유용
        /// 실제 소켓 구현에서는 비워 두거나 내부 처리만 수행
        /// </summary>
        void Poll();
    }

}
