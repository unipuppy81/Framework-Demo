using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Diagnostics
{
    /// <summary>
    /// 런타임 중 수치 수집/보관
    /// </summary>
    public class RuntimeDiagnosticsCollector
    {
        public int CurrentTick { get; private set; }

        /// <summary>
        /// 왕복 지연 시간
        /// </summary>
        public float RttMs { get; private set; }

        /// <summary>
        /// 초당 수신 패킷 수
        /// </summary>
        public int ReceivedPacketCountPerSecond { get; private set; }

        /// <summary>
        /// 초당 송신 패킷 수
        /// </summary>
        public int SentPacketCountPerSecond { get; private set; }

        public int SpawnCount { get; private set; }

        /// <summary>
        /// 디버그용
        /// </summary>
        public string LastMessage { get; private set; }

        private int _sentPacketAccum;
        private int _receivedPacketAccum;
        private float _packetTimer;

        // =========================
        // 추후 확장용 필드 자리
        // =========================

        public int RemoteTick { get; private set; }

        public float SentKilobytesPerSecond { get; private set; }
        public float ReceivedKilobytesPerSecond { get; private set; }

        public int VisibleCount { get; private set; }
        public bool AoiEnabled { get; private set; }

        private int _sentByteAccum;
        private int _receivedByteAccum;

        public int TotalSentPacketCount { get; private set; }
        public int TotalReceivedPacketCount { get; private set; }

        /// <summary>
        /// Tick 시스템에서 호출
        /// </summary>
        public void ReportTick(int tick)
        {
            CurrentTick = tick;
        }

        /// <summary>
        /// RTT 측정값 반영
        /// </summary>
        public void ReportRtt(float rttMs)
        {
            RttMs = rttMs;
        }

        /// <summary>
        /// 패킷 송신 이벤트 반영
        /// </summary>
        public void ReportPacketSent()
        {
            _sentPacketAccum++;
            TotalSentPacketCount++;
        }

        /// <summary>
        /// 패킷 수신 이벤트 반영
        /// </summary>
        public void ReportPacketReceived()
        {
            _receivedPacketAccum++;
            TotalReceivedPacketCount++;
        }


        public void ReportSpawn()
        {
            SpawnCount++;
        }


        public void ReportMessage(string message)
        {
            LastMessage = message;
        }

        public void ReportRemoteTick(int tick)
        {
            if (tick > RemoteTick)
                RemoteTick = tick;
        }

        public void ReportPacketSent(int byteCount)
        {
            _sentPacketAccum++;
            _sentByteAccum += Mathf.Max(0, byteCount);
        }

        public void ReportPacketReceived(int byteCount)
        {
            _receivedPacketAccum++;
            _receivedByteAccum += Mathf.Max(0, byteCount);
        }

        public void ReportVisibleCount(int visibleCount)
        {
            VisibleCount = Mathf.Max(0, visibleCount);
        }

        public void ReportAoiEnabled(bool enabled)
        {
            AoiEnabled = enabled;
        }

        /// <summary>
        /// MonoBehaviour.Update 에서 매 프레임 호출
        /// 1초 단위로 packet/sec 계산
        /// </summary>
        public void Update(float deltaTime)
        {
            _packetTimer += deltaTime;

            if (_packetTimer < 1f)
                return;

            SentPacketCountPerSecond = _sentPacketAccum;
            ReceivedPacketCountPerSecond = _receivedPacketAccum;

            _sentPacketAccum = 0;
            _receivedPacketAccum = 0;
            _packetTimer -= 1f;
        }
    }
}