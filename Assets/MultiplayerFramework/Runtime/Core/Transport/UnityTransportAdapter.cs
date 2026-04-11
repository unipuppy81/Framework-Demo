using PlasticGui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEditor.MemoryProfiler;


namespace MultiplayerFramework.Runtime.Core.Transport
{
    public sealed class UnityTransportAdapter : INetworkTransport, IDisposable
    {
        /// <summary>
        /// 저수준 네트워크 인터페이스 객체 (소켓 관리자)
        /// </summary>
        private NetworkDriver _driver;

        /// <summary>
        /// 특정 원격 피어와의 연결 상태를 나타내는 핸들
        /// </summary>
        private NetworkConnection _connection;


        private readonly Dictionary<int, NetworkConnection> _serverConnections = new();
        private int _nextConnectionId = 2;


        private readonly Queue<NetworkTransportEvent> _eventQueue = new();


        private bool _isHost;
        private bool _isStarted;
        private ushort _listenPort;

        public event Action<NetworkTransportEvent> OnTransportEvent;

        /// <summary>
        /// 디버깅 상태 확인용
        /// </summary>
        public bool IsStarted => _isStarted;
        public bool IsServer => _isHost;
        public ushort ListenPort => _listenPort;

        public bool IsConnected => throw new NotImplementedException();


        public bool ConnectNetwork(string address, ushort port, bool isHost)
        {
            if (isHost)
                return StartHost(port);
            else
                return StartClient(address, port);
        }

        /// <summary>
        /// 클라이언트 모드로 서버에 연결
        /// </summary>
        public bool StartClient(string address, ushort port)
        {
            _driver = NetworkDriver.Create();
            _connection = default;
            _serverConnections.Clear();
            _eventQueue.Clear();

            NetworkEndpoint endpoint = NetworkEndpoint.Parse(address, port);
            _connection = _driver.Connect(endpoint);

            _isHost = false;
            _isStarted = true;
            _listenPort = 0;

            EnqueueDiagnostic($"[Transport] Client started. Connecting to {address}:{port}");
            return true;
        }

        /// <summary>
        /// 서버(호스트 역할) 모드로 포트를 열고 listen 시작
        /// </summary>
        public bool StartHost(ushort port)
        {
            _driver = NetworkDriver.Create();
            _connection = default;
            _serverConnections.Clear();
            _eventQueue.Clear();

            NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);

            if (_driver.Bind(endpoint) != 0)
            {
                EnqueueDiagnostic($"[Transport] Failed to bind on port {port}");
                return false;
            }

            _driver.Listen();

            _isHost = true;
            _isStarted = true;
            _listenPort = port;

            EnqueueDiagnostic($"[Transport] Server started. Listening on port {port}");
            return true;
        }

        /// <summary>
        /// Transport 내부 네트워크 이벤트를 폴링하여 공통 이벤트 큐에 쌓음
        /// Session은 이 큐를 소비
        /// </summary>
        public void Poll()
        {
            if (!_isStarted)
                return;

            _driver.ScheduleUpdate().Complete();

            if (_isHost)
            {
                PollServerAccepts();
                PollServerConnections();
            }
            else
            {
                PollClientConnection();
            }
        }

        /// <summary>
        /// Session 쪽에서 한 번에 하나씩 꺼내 쓸 수 있도록 큐 기반으로 제공
        /// </summary>
        public bool TryDequeueEvent(out NetworkTransportEvent transportEvent)
        {
            if (_eventQueue.Count > 0)
            {
                transportEvent = _eventQueue.Dequeue();
                return true;
            }

            transportEvent = default;
            return false;
        }

        /// <summary>
        /// 기본 상대에게 데이터 전송
        /// 
        /// - 클라이언트 모드에서는 서버(단일 연결)로 전송
        /// - 서버 모드에서는 Broadcast 대신 단일 대상 전송 전용 메서드를 권장
        /// </summary>
        public bool Send(ArraySegment<byte> payload)
        {
            if (!_isStarted)
            {
                UnityEngine.Debug.Log("[Transport] Send Failed, isStarted = false");
                EnqueueDiagnostic("AAA");
                return false;
            }
 

            if (_isHost)
            {
                EnqueueDiagnostic("[Transport] Send(payload) was called in server mode. Use SendTo(connectionId, payload).");
                //return false;
            }

            return SendToConnection(_connection, payload);
        }

        /// <summary>
        /// 서버 모드에서 특정 연결 대상에게 데이터 전송
        /// connectionId는 _serverConnections의 index를 사용
        /// </summary>
        public bool SendTo(int connectionId, ArraySegment<byte> payload)
        {
            if (!_isStarted || !_isHost)
            {
                UnityEngine.Debug.LogError("A");
                return false;
            }
 

            if (!_serverConnections.TryGetValue(connectionId, out NetworkConnection connection))
            {
                UnityEngine.Debug.LogError("B");
                return false;
            }
     

            if (!connection.IsCreated)
            {
                UnityEngine.Debug.LogError("C");
                return false;
            }

            return SendToConnection(connection, payload);
        }

        /// <summary>
        /// 서버 모드에서 현재 연결된 모든 클라이언트에게 브로드캐스트
        /// </summary>
        public bool Broadcast(ArraySegment<byte> payload)
        {
            if (!_isStarted || !_isHost)
                return false;

            foreach (var pair in _serverConnections)
            {
                NetworkConnection connection = pair.Value;

                if (!connection.IsCreated)
                    continue;

                SendToConnection(connection, payload);
            }


            return true;
        }

        /// <summary>
        /// 종료 및 정리
        /// </summary>
        public void Stop()
        {
            if (!_isStarted)
                return;

            EnqueueDiagnostic("[Transport] Stopping transport.");

            if (_isHost)
            {
                for (int i = 0; i < _serverConnections.Count; i++)
                {
                    if (_serverConnections[i].IsCreated)
                        _serverConnections[i].Disconnect(_driver);
                }

                _serverConnections.Clear();
            }
            else
            {
                if (_connection.IsCreated)
                    _connection.Disconnect(_driver);
            }

            _isStarted = false;
            _isHost = false;
            _listenPort = 0;
        }

        public void Dispose()
        {
            Stop();
        }

        private void PollServerAccepts()
        {
            NetworkConnection acceptedConnection;
            while ((acceptedConnection = _driver.Accept()) != default)
            {
                int connectionId = _nextConnectionId++;
                _serverConnections.Add(connectionId, acceptedConnection);

                _eventQueue.Enqueue(NetworkTransportEvent.CreateConnected(connectionId.ToString()));
            }
        }

        private void PollServerConnections()
        {
            var disconnectedIds = new List<int>();

            foreach(var pair in _serverConnections)
            {
                int connectionId = pair.Key;
                NetworkConnection connection = pair.Value;

                if (!connection.IsCreated)
                    continue;

                DataStreamReader stream;
                NetworkEvent.Type eventType;

                while ((eventType = _driver.PopEventForConnection(connection, out stream)) != NetworkEvent.Type.Empty)
                {
                    switch (eventType)
                    {
                        case NetworkEvent.Type.Data:
                            {
                                byte[] data = ReadBytes(stream);
                                _eventQueue.Enqueue(NetworkTransportEvent.CreateDataReceived(connectionId.ToString(), data));
                                //OnTransportEvent?.Invoke(NetworkTransportEvent.CreateDataReceived(connectionId.ToString(), data));
                                break;
                            }

                        case NetworkEvent.Type.Disconnect:
                            {
                                _eventQueue.Enqueue(NetworkTransportEvent.CreateDisconnected(connectionId.ToString()));
                                EnqueueDiagnostic($"[Transport] Client disconnected. ConnectionId={connectionId}");
                                disconnectedIds.Add(connectionId);
                                break;
                            }

                        case NetworkEvent.Type.Connect:
                            {
                                UnityEngine.Debug.LogError("[Host] Connect");
                                break;
                            }
                    }
                }
            }
        }


        private void PollClientConnection()
        {
            if (!_connection.IsCreated)
                return;

            DataStreamReader stream;
            NetworkEvent.Type eventType;

            while ((eventType = _driver.PopEventForConnection(_connection, out stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Connect:
                        {
                            _eventQueue.Enqueue(NetworkTransportEvent.CreateConnected(0.ToString()));
                            EnqueueDiagnostic("[Transport] Client connected to server.");
                            break;
                        }

                    case NetworkEvent.Type.Data:
                        {
                            byte[] data = ReadBytes(stream);
                            _eventQueue.Enqueue(NetworkTransportEvent.CreateDataReceived(0.ToString(), data));
                            break;
                        }

                    case NetworkEvent.Type.Disconnect:
                        {
                            _eventQueue.Enqueue(NetworkTransportEvent.CreateDisconnected(0.ToString()));
                            EnqueueDiagnostic("[Transport] Client disconnected from server.");
                            _connection = default;
                            break;
                        }
                }
            }
        }

        private bool SendToConnection(NetworkConnection connection, ArraySegment<byte> payload)
        {
            if (!connection.IsCreated)
            {
                UnityEngine.Debug.LogError("[Transport] connection.IsCreated Failed.");
                return false;
            }

            if (_driver.BeginSend(connection, out DataStreamWriter writer) != 0)
            {
                UnityEngine.Debug.LogError("[Transport] BeginSend Failed.");
                return false;
            }

            for (int i = 0; i < payload.Count; i++)
            {
                writer.WriteByte(payload.Array[payload.Offset + i]);
            }

            int result = _driver.EndSend(writer);
            if (result < 0)
            {
                UnityEngine.Debug.LogError($"[Transport] EndSend failed. Result={result}");
                return false;
            }

            return true;
        }

        private static byte[] ReadBytes(DataStreamReader stream)
        {
            int length = stream.Length;
            byte[] buffer = new byte[length];

            for (int i = 0; i < length; i++)
            {
                buffer[i] = stream.ReadByte();
            }

            return buffer;
        }

        private void EnqueueDiagnostic(string message)
        {
            _eventQueue.Enqueue(NetworkTransportEvent.CreateDiagnostic(message));
        }

        public void Connect(string endpoint)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public bool Send_string(byte[] data, string targetEndpoint = null)
        {
            throw new NotImplementedException();
        }
    }

}
