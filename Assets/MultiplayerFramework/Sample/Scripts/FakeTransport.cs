using System;
using MultiplayerFramework.Runtime.Core.Transport;

namespace MultiplayerFramework.Sample
{
    /// <summary>
    /// 서버 없이 NetworkSession 흐름 테스트 위한 가짜 Transport
    /// 
    /// 역할:
    /// - Connect / Disconnect 호출 여부 확인
    /// - Send로 들어온 byte[] 저장
    /// - 테스트 코드에서 수동으로 수신 이벤트 발생 가능
    /// </summary>
    public sealed class FakeTransport : INetworkTransport
    {
        private bool _isConnected;

        /// <summary>
        /// 마지막으로 전송된 데이터
        /// Session.Send() 가 정말 Transport 까지 도달했는지 확인할 때 사용
        /// </summary>
        public byte[] LastSentData { get; private set; }
        public bool IsConnected => _isConnected;
        public event Action<NetworkTransportEvent> OnTransportEvent;

        public void Connect(string endpoint)
        {
            _isConnected = true;

            // 연결 성공 이벤트를 상위(Session)로 올림
            OnTransportEvent?.Invoke(new NetworkTransportEvent(NetworkTransportEventType.Connected));
        }

        public void Disconnect()
        {
            _isConnected = false;

            // 연결 종료 이벤트를 상위(Session)로 올림
            OnTransportEvent?.Invoke(new NetworkTransportEvent(NetworkTransportEventType.Disconnected));
        }

        public void Send(byte[] data)
        {
            // 실제 전송 대신 마지막 전송 데이터 보관
            LastSentData = data;
        }

        public void Poll()
        {

        }

        /// <summary>
        /// 테스트용:
        /// 네트워크에서 데이터가 도착한 것처럼 DataReceive 이벤트 발생
        /// </summary>
        /// <param name="data"></param>
        public void SimulateReceive(byte[] data)
        {
            OnTransportEvent?.Invoke(new NetworkTransportEvent(NetworkTransportEventType.DataReceived, data));
        }

        /// <summary>
        /// 테스트용:
        /// 강제로 에러 이벤트를 발생시킵니다.
        /// </summary>
        public void SimulateError(string message)
        {
            OnTransportEvent?.Invoke(
                new NetworkTransportEvent(NetworkTransportEventType.Error, errorMessage: message));
        }
    }
}

