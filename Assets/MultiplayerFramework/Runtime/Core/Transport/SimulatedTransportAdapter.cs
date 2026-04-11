using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace MultiplayerFramework.Runtime.Core.Transport
{
    [System.Serializable]
    public struct NetworkSimulationSettings
    {
        [Header("Enable")]
        public bool Enabled;

        [Header("Latency")]
        [Min(0)] public int BaseLatencyMs;
        [Min(0)] public int JitterMs;

        [Header("Packet Loss")]
        [Range(0f, 1f)] public float PacketLossRate;

        [Header("Apply")]
        public bool ApplyLatency;
        public bool ApplyLoss;

        /// <summary>
        /// 시뮬레이션 우회 여부
        /// </summary>
        public bool IsBypass =>
            Enabled == false || (ApplyLatency == false && ApplyLoss == false);
    }

    internal struct DelayedTransportPacket
    {
        public readonly ArraySegment<byte> Payload;
        public readonly double DeliverAtTime;

        public DelayedTransportPacket(ArraySegment<byte> payload, double deliverAtTime)
        {
            Payload = payload;
            DeliverAtTime = deliverAtTime;
        }
    }

    internal struct DelayedTransportPacketTo
    {
        public readonly int ConnectionId;
        public readonly ArraySegment<byte> Payload;
        public readonly double DeliverAtTime;

        public DelayedTransportPacketTo(int connectionId, ArraySegment<byte> payload, double deliverAtTime)
        {
            ConnectionId = connectionId;
            Payload = payload;
            DeliverAtTime = deliverAtTime;
        }
    }

    public sealed class SimulatedTransportAdapter : INetworkTransport
    {
        private INetworkTransport _inner;
        private List<DelayedTransportPacket> _delayedSendQueue = new();
        private List<DelayedTransportPacketTo> _delayedSendToQueue = new();
        private NetworkSimulationSettings _settings;

        private int _droppedPacketCount;
        private int _delayedPacketCount;
        private int _sentImmediatelyCount;

        public bool IsConnected => _inner != null && _inner.IsConnected;

        public int DroppedPacketCount => _droppedPacketCount;
        public int DelayedPacketCount => _delayedPacketCount;
        public int SentImmediatelyCount => _sentImmediatelyCount;

        public event Action<NetworkTransportEvent> OnTransportEvent;

        public SimulatedTransportAdapter(INetworkTransport inner, NetworkSimulationSettings settings)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _settings = settings;

            _inner.OnTransportEvent += HandleInnerTransportEvent;
        }

        /// <summary>
        /// 연결 해제는 inner transport에 그대로 위임
        /// </summary>
        public void Disconnect()
        {
            _delayedSendQueue.Clear();
            _delayedSendToQueue.Clear();
            _inner.Disconnect();
        }

        /// <summary>
        /// Send 시점에는 loss / latency를 적용한 뒤
        /// 바로 보내거나 지연 큐에 저장한다.
        /// </summary>
        public bool Send(ArraySegment<byte> data)
        {
            if (_inner == null)
                return false;

            if (data.Array == null || data.Count <= 0)
                return false;

            if (_settings.IsBypass)
            {
                _sentImmediatelyCount++;
                return _inner.Send(CloneSegment(data));
            }

            if (_settings.ApplyLoss && ShouldDropPacket(_settings.PacketLossRate))
            {
                _droppedPacketCount++;
                UnityEngine.Debug.LogError($"<color=red>[Host]</color> [SimulatedTransport] Packet dropped. lossRate={_settings.PacketLossRate:0.00}");
                return true;
            }

            if (_settings.ApplyLatency == false)
            {
                _sentImmediatelyCount++;
                return _inner.Send(CloneSegment(data));
            }

            int latencyMs = CalculateLatencyMs(_settings.BaseLatencyMs, _settings.JitterMs);
            double deliverAtTime = Time.realtimeSinceStartupAsDouble + (latencyMs / 1000.0);

            _delayedSendQueue.Add(
                new DelayedTransportPacket(CloneSegment(data), deliverAtTime)
            );

            _delayedPacketCount++;
            UnityEngine.Debug.LogError($"<color=red>[Host]</color> [SimulatedTransport] Packet delayed. latency={latencyMs}ms queue={_delayedSendQueue.Count}");
            return true;
        }

        public bool SendTo(int connectionId, ArraySegment<byte> payload)
        {
            if (_inner == null)
                return false;

            if (payload.Array == null || payload.Count <= 0)
                return false;

            if (_settings.IsBypass)
            {
                _sentImmediatelyCount++;
                return _inner.SendTo(connectionId, CloneSegment(payload));
            }

            if (_settings.ApplyLoss && ShouldDropPacket(_settings.PacketLossRate))
            {
                _droppedPacketCount++;
                UnityEngine.Debug.LogError(
                    $"<color=red>[Host]</color> [SimulatedTransport] Packet dropped. target={connectionId}, lossRate={_settings.PacketLossRate:0.00}");
                return true;
            }

            if (_settings.ApplyLatency == false)
            {
                _sentImmediatelyCount++;
                return _inner.SendTo(connectionId, CloneSegment(payload));
            }

            int latencyMs = CalculateLatencyMs(_settings.BaseLatencyMs, _settings.JitterMs);
            if (latencyMs <= 0)
            {
                _sentImmediatelyCount++;
                return _inner.SendTo(connectionId, CloneSegment(payload));
            }

            double deliverAtTime = Time.realtimeSinceStartupAsDouble + (latencyMs / 1000.0);

            _delayedSendToQueue.Add(
                new DelayedTransportPacketTo(connectionId, CloneSegment(payload), deliverAtTime)
            );

            _delayedPacketCount++;
            UnityEngine.Debug.LogError(
                $"<color=red>[Host]</color> [SimulatedTransport] Packet delayed. target={connectionId}, latency={latencyMs}ms queue={_delayedSendToQueue.Count}");

            return true;
        }

        /// <summary>
        /// 지연 시간이 지난 패킷을 먼저 실제 전송하고,
        /// 그 다음 inner transport Poll을 호출한다.
        /// </summary>
        public void Poll()
        {
            FlushDelayedPackets();
            FlushDelayedPacketsTo();
            _inner.Poll();
        }

        private void FlushDelayedPackets()
        {
            if (_delayedSendQueue.Count == 0)
                return;

            double now = Time.realtimeSinceStartupAsDouble;

            for (int i = _delayedSendQueue.Count - 1; i >= 0; i--)
            {
                DelayedTransportPacket packet = _delayedSendQueue[i];

                if (packet.DeliverAtTime > now)
                    continue;


                if (_inner.Send(packet.Payload))
                {
                    _sentImmediatelyCount++;
                    _delayedSendQueue.RemoveAt(i);
                }
                else
                {
                    UnityEngine.Debug.LogError("[SimulatedTransport] Flush delayed Send failed.");
                }
            }
        }

        private void FlushDelayedPacketsTo()
        {
            if (_delayedSendToQueue.Count == 0)
                return;

            double now = Time.realtimeSinceStartupAsDouble;

            for (int i = _delayedSendToQueue.Count - 1; i >= 0; i--)
            {
                DelayedTransportPacketTo packet = _delayedSendToQueue[i];

                if (packet.DeliverAtTime > now)
                    continue;

                UnityEngine.Debug.LogError(
                    $"<color=red>[Host]</color> [SimulatedTransport] Flush delayed SendTo. target={packet.ConnectionId}");


                if(_inner.SendTo(packet.ConnectionId, packet.Payload))
                {
                    _sentImmediatelyCount++;
                    _delayedSendToQueue.RemoveAt(i);
                }
                else
                {
                    UnityEngine.Debug.LogError(
                        $"<color=red>[Host]</color> [SimulatedTransport] Flush delayed SendTo failed. target={packet.ConnectionId}");
                }
            }
        }

        private void HandleInnerTransportEvent(NetworkTransportEvent transportEvent)
        {
            OnTransportEvent?.Invoke(transportEvent);
        }

        private static bool ShouldDropPacket(float lossRate)
        {
            if (lossRate <= 0f)
                return false;

            return UnityEngine.Random.value < lossRate;
        }

        private static int CalculateLatencyMs(int baseLatencyMs, int jitterMs)
        {
            if (jitterMs <= 0)
                return Mathf.Max(0, baseLatencyMs);

            int jitter = UnityEngine.Random.Range(-jitterMs, jitterMs + 1);
            return Mathf.Max(0, baseLatencyMs + jitter);
        }

        /// <summary>
        /// 원본 버퍼 재사용 문제를 막기 위해 복사본을 만든다.
        /// </summary>
        private static ArraySegment<byte> CloneSegment(ArraySegment<byte> source)
        {
            byte[] copied = new byte[source.Count];
            Buffer.BlockCopy(source.Array, source.Offset, copied, 0, source.Count);
            return new ArraySegment<byte>(copied);
        }

        private void EnqueueDiagnostic(string message)
        {
            OnTransportEvent?.Invoke(NetworkTransportEvent.CreateDiagnostic(message));
        }


        public void Connect(string endpoint)
        {
            throw new NotImplementedException();
        }

        public bool Send_string(byte[] data, string targetEndpoint = null)
        {
            throw new NotImplementedException();
        }

        public bool ConnectNetwork(string address, ushort port, bool isHost)
        {
            return _inner.ConnectNetwork(address, port, isHost);
        }


        public bool TryDequeueEvent(out NetworkTransportEvent transportEvent)
        {
            transportEvent = default;
            return _inner.TryDequeueEvent(out transportEvent);
        }
    }
}
