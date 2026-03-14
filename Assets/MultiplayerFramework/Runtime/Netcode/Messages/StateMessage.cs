using MultiplayerFramework.Runtime.Netcode.Objects;
using MultiplayerFramework.Runtime.NetCode.Objects;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    public struct StateMessage
    {
        public int Tick;
        public NetworkId NetworkId;

        public Vector3 Position;
        public float RotationY;

        public int Hp;
        public int Score;
        public float RespawnRemainingTime;
    }
}