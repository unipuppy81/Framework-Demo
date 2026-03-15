using System;
using MultiplayerFramework.Runtime.Core.Serialization;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Netcode.Messages;

namespace MultiplayerFramework.Runtime.Core.Session
{
    /// <summary>
    /// 역할:
    /// - 상위 계층으로부터 메시지를 받음
    /// - Serializer로 byte[]로 변환
    /// - Transport를 통해 송신
    /// 
    /// 수신 시:
    /// - Transport 이벤트를 받음
    /// - Serializer로 역직렬화
    /// - 상위 계층에 NetworkEnvelope로 전달
    /// </summary>
    public sealed class NetworkSession : ISession
    {
        private readonly INetworkTransport _transport;
        private readonly IMessageSerializer _serializer;

        public bool IsConnected => _transport.IsConnected;

        public event Action<NetworkEnvelope> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;

        public NetworkSession(INetworkTransport transport, IMessageSerializer serializer)
        {
            _transport = transport;
            _serializer = serializer;

            // Transport 이벤트를 Session 내부에서 받아서
            // 상위 계층에 맞는 이벤트로 다시 변환
            _transport.OnTransportEvent += HandleTransportEvent;
        }

        public void Connect(string endpoint)
        {
            _transport.Connect(endpoint);
        }

        public void Disconnect()
        {
            _transport.Disconnect();
        }

        public void Send(NetworkEnvelope message)
        {
            // 메시지 객체를 직렬화
            byte[] serializedData = _serializer.Serialize(message);
            _transport.Send(serializedData);
        }

        public void Poll()
        {
            _transport.Poll();
        }

        private void HandleTransportEvent(NetworkTransportEvent transportEvent)
        {
            switch (transportEvent.Type)
            {
                case NetworkTransportEventType.Connected:
                    OnConnected?.Invoke();
                    break;

                case NetworkTransportEventType.Disconnected:
                    OnDisconnected?.Invoke();
                    break;

                case NetworkTransportEventType.DataReceived:
                    HandleReceivedData(transportEvent.Data);
                    break;

                case NetworkTransportEventType.Error:
                    OnError?.Invoke(transportEvent.ErrorMessage);
                    break;
            }
        }

        private void HandleReceivedData(byte[] data)
        {
            // 수신한 byte[]를 메시지로 복원 시도
            if (_serializer.TryDeserialize(data, out NetworkEnvelope message) == false)
            {
                OnError?.Invoke("Failed to deserialize incoming network message.");
                return;
            }

            // 상위 계층에는 다시 메시지 객체 형태로 전달
            OnMessageReceived?.Invoke(message);
        }
    }
}