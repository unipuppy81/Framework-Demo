using System;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// 실제 네트워크 전송 계층의 공통 인터페이스
    /// 
    /// 전송 방법은 숨기고, 연결/송신/수신 이벤트만 노출
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Transport 레벨 이벤트를 상위(Session)로 전달
        /// </summary>
        event Action<NetworkTransportEvent> OnTransportEvent;

        /// <summary>
        /// 대상 주소 또는 룸에 연결
        /// </summary>
        void Connect(string endpoint);

        /// <summary>
        /// 연결 종료
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 바이트 데이터를 전송
        /// </summary>
        void Send(byte[] data);

        /// <summary>
        /// 필요 시 Transport 내부 큐를 갱신
        /// Loopback/Fake 구현에서 특히 유용
        /// </summary>
        void Poll();
    }

}
