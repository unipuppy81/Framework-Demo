using MultiplayerFramework.Runtime.Netcode.Messages;
using System;
using System.Collections.Generic;

namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// 실제 네트워크 없이 endpoint 간 메시지 송수신을 흉내 내는 테스트용 Transport
    /// 
    /// FakeTransportHub를 통해 같은 프로세스 내 다른 FakeTransport에게 데이터를 전달
    /// </summary>
    public sealed class FakeTransport : INetworkTransport
    {
        /// <summary>
        /// 내부 이벤트 큐
        /// Poll 시 상위(Session)로 전달
        /// </summary>
        private readonly Queue<NetworkTransportEvent> _eventQueue = new();

        /// <summary>
        /// 메시지 중계를 담당하는 허브
        /// </summary>
        private readonly FakeTransportHub _hub;

        /// <summary>
        /// 선택적 패킷 훅입니다.
        /// </summary>
        private readonly ITransportPacketHook _packetHook;

        /// <summary>
        /// 현재 연결된 로컬 endpoint
        /// </summary>
        private string _localEndpoint;

        public bool IsConnected { get; private set; }

        /// <summary>
        /// Transport 이벤트를 상위(Session)로 전달
        /// </summary>
        public event Action<NetworkTransportEvent> OnTransportEvent;

        /// <summary>
        /// FakeTransport를 생성
        /// </summary>
        /// <param name="hub">공유 허브</param>
        /// <param name="packetHook">선택적 패킷 훅</param>
        public FakeTransport(FakeTransportHub hub, ITransportPacketHook packetHook = null)
        {
            _hub = hub;
            _packetHook = packetHook;
        }


        public void Connect(JoinMessage jM)
        {

        }

        /// <summary>
        /// endpoint로 연결하고 허브에 자신을 등록
        /// </summary>
        /// <param name="endpoint">로컬 endpoint</param>
        public void Connect(string endpoint)
        {
            if (IsConnected)
                return;

            if (string.IsNullOrEmpty(endpoint))
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError("Endpoint is null or empty."));
                return;
            }

            bool registered = _hub.Register(endpoint, this);
            if (registered == false)
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError($"Endpoint registration failed: {endpoint}", endpoint));
                return;
            }

            _localEndpoint = endpoint;
            IsConnected = true;

            _eventQueue.Enqueue(NetworkTransportEvent.CreateConnected(endpoint));
        }

        /// <summary>
        /// 연결 종료 후 허브에서 자신을 제거
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected == false)
                return;

            string endpoint = _localEndpoint;

            _hub.Unregister(_localEndpoint);

            _localEndpoint = null;
            IsConnected = false;

            _eventQueue.Enqueue(NetworkTransportEvent.CreateDisconnected(endpoint));
        }

        /// <summary>
        /// 지정한 대상 endpoint로 데이터를 전송
        /// </summary>
        /// <param name="data">전송 데이터</param>
        /// <param name="targetEndpoint">대상 endpoint</param>
        public void Send(byte[] data, string targetEndpoint = null)
        {
            if (IsConnected == false)
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError("FakeTransport is not connected."));
                return;
            }

            if (data == null || data.Length == 0)
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError("Cannot send empty data.", targetEndpoint));
                return;
            }

            if (string.IsNullOrEmpty(targetEndpoint))
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError("Target endpoint is null or empty."));
                return;
            }

            // 복사본 사용
            byte[] copiedData = CloneData(data);

            // 송신 직전 훅
            if (_packetHook != null)
            {
                bool canSend = _packetHook.OnBeforeSend(_localEndpoint, targetEndpoint, copiedData);
                if (canSend == false)
                    return;
            }

            bool sent = _hub.TrySend(_localEndpoint, targetEndpoint, copiedData);
            if (sent == false)
            {
                _eventQueue.Enqueue(NetworkTransportEvent.CreateError(
                    $"Target endpoint not found: {targetEndpoint}",
                    targetEndpoint));
            }
        }

        /// <summary>
        /// 허브가 호출하는 내부 수신 함수
        /// </summary>
        /// <param name="senderEndpoint">보낸 쪽 endpoint</param>
        /// <param name="data">수신 데이터</param>
        internal void EnqueueIncoming(string senderEndpoint, byte[] data)
        {
            if (IsConnected == false)
                return;

            byte[] copiedData = CloneData(data);

            // 수신 직전 훅
            if (_packetHook != null)
            {
                bool canReceive = _packetHook.OnBeforeReceive(_localEndpoint, senderEndpoint, copiedData);
                if (canReceive == false)
                    return;
            }

            _eventQueue.Enqueue(NetworkTransportEvent.CreateDataReceived(senderEndpoint, copiedData));
        }

        /// <summary>
        /// 내부 이벤트 큐를 비우면서 상위(Session)로 전달
        /// </summary>
        public void Poll()
        {
            while (_eventQueue.Count > 0)
            {
                NetworkTransportEvent transportEvent = _eventQueue.Dequeue();
                OnTransportEvent?.Invoke(transportEvent);
            }
        }

        private static byte[] CloneData(byte[] data)
        {
            byte[] copied = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copied, 0, data.Length);
            return copied;
        }
    }
}