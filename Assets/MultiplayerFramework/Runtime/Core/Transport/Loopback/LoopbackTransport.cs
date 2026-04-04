using MultiplayerFramework.Runtime.Netcode.Messages;
using System;
using System.Collections.Generic;

namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// 자기 자신에게 데이터를 되돌려 보내는 테스트용 Transport
    /// 
    /// 실제 네트워크 없이도
    /// Session -> Transport -> Session 수신 흐름 검증
    /// </summary>
    public sealed class LoopbackTransport : INetworkTransport
    {
        /// <summary>
        /// 내부 이벤트 큐.
        /// Poll 시 하나씩 꺼내서 OnTransportEvent로 전달
        /// </summary>
        private readonly Queue<NetworkTransportEvent> _eventQueue = new();

        /// <summary>
        /// 선택적 패킷 훅
        /// 확장 포인트로 열어둠
        /// </summary>
        private readonly ITransportPacketHook _packetHook;

        /// <summary>
        /// 현재 연결된 endpoint
        /// Loopback에서는 자기 자신 식별자처럼 사용
        /// </summary>
        private string _localEndpoint;

        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Transport 이벤트를 상위(Session)로 전달
        /// </summary>
        public event Action<NetworkTransportEvent> OnTransportEvent;

        /// <summary>
        /// LoopbackTransport를 생성
        /// </summary>
        /// <param name="packetHook">선택적 패킷 훅</param>
        public LoopbackTransport(ITransportPacketHook packetHook = null)
        {
            _packetHook = packetHook;
        }

        /// <summary>
        /// 지정한 endpoint로 연결
        /// Loopback에서는 자기 자신을 의미하는 논리 식별자
        /// </summary>
        /// <param name="endpoint">로컬 endpoint</param>
        public void Connect(string endpoint)
        {
            if (IsConnected)
                return;

            _localEndpoint = endpoint;
            IsConnected = true;

            // 연결 성공 이벤트를 큐에 삽입
            _eventQueue.Enqueue(NetworkTransportEvent.CreateConnected(_localEndpoint));
        }
        public void Connect(JoinMessage jM)
        {

        }
        public void Disconnect()
        {
            if (IsConnected == false)
                return;

            string endpoint = _localEndpoint;

            IsConnected = false;
            _localEndpoint = null;

            // 연결 종료 이벤트를 큐에 삽입
            _eventQueue.Enqueue(NetworkTransportEvent.CreateDisconnected(endpoint));
        }

        /// <summary>
        /// 데이터를 전송
        /// Loopback 구현이므로 자기 자신에게 다시 수신 이벤트로 넣음
        /// </summary>
        /// <param name="data">전송 데이터</param>
        /// <param name="targetEndpoint">대상 endpoint, null이면 자기 자신으로 처리</param>
        public void Send(byte[] data, string targetEndpoint = null)
        {
            if (IsConnected == false)
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError("LoopbackTransport is not connected."));
                return;
            }

            if (data == null || data.Length == 0)
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError("Cannot send empty data.", _localEndpoint));
                return;
            }

            string finalTargetEndpoint = string.IsNullOrEmpty(targetEndpoint) ? _localEndpoint : targetEndpoint;

            // Loopback인데 자기 자신 외의 endpoint를 지정하면 에러로 처리
            if (finalTargetEndpoint != _localEndpoint)
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError(
                    $"LoopbackTransport can only send to itself. Target: {finalTargetEndpoint}",
                    finalTargetEndpoint));
                return;
            }

            // 원본 배열 참조 공유를 피하기 위해 복사본 생성
            byte[] copiedData = CloneData(data);

            // 송신 훅
            if (_packetHook != null)
            {
                bool canSend = _packetHook.OnBeforeSend(_localEndpoint, finalTargetEndpoint, copiedData);
                if (canSend == false)
                    return;
            }

            // 수신 훅
            if (_packetHook != null)
            {
                bool canReceive = _packetHook.OnBeforeReceive(_localEndpoint, _localEndpoint, copiedData);
                if (canReceive == false)
                    return;
            }

            // 자기 자신에게 수신 이벤트 추가
            _eventQueue.Enqueue(NetworkTransportEvent.CreateDataReceived(_localEndpoint, copiedData));
        }

        /// <summary>
        /// 내부 이벤트 큐를 비우면서 상위(Session)로 이벤트를 전달
        /// </summary>
        public void Poll()
        {
            while (_eventQueue.Count > 0)
            {
                NetworkTransportEvent transportEvent = _eventQueue.Dequeue();
                OnTransportEvent?.Invoke(transportEvent);
            }
        }

        /// <summary>
        /// byte[] 복사본 생성
        /// </summary>
        private static byte[] CloneData(byte[] data)
        {
            byte[] copied = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copied, 0, data.Length);
            return copied;
        }
    }
}