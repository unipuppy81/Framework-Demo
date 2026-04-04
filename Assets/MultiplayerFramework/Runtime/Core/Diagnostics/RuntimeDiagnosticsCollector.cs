using System.Collections;
using System.Collections.Generic;
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
        }

        /// <summary>
        /// 패킷 수신 이벤트 반영
        /// </summary>
        public void ReportPacketReceived()
        {
            _receivedPacketAccum++;
        }

        /// <summary>
        /// 스폰 발생 시 호출
        /// </summary>
        public void ReportSpawn()
        {
            SpawnCount++;
        }

        /// <summary>
        /// 임의 진단 문자열 기록
        /// </summary>
        public void ReportMessage(string message)
        {
            LastMessage = message;
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
            _packetTimer = 0f;
        }
    }
}