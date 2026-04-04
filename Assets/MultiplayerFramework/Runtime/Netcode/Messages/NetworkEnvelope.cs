using MultiplayerFramework.Runtime.NetCode.Objects;
using System;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    /// <summary>
    /// 실제 송신시 사용하는 공통 메시지 래퍼
    /// 메시지 종류, 보낸 사람, Tick 정보, 실제 payLoad 담음
    /// </summary>
    [Serializable]
    public struct NetworkEnvelope
    {
        public NetworkMessageType Type;
        public NetworkId SenderId;
        public int Tick;

        /// <summary>
        /// 실제 직렬화 대상 데이터
        /// 임시용이라 byte[]
        /// </summary>
        public byte[] Payload;

        public NetworkEnvelope(NetworkMessageType type, NetworkId senderId, int tick, byte[] payload)
        {
            Type = type;
            SenderId = senderId;
            Tick = tick;
            Payload = payload;
        }
    }

}
