using MultiplayerFramework.Runtime.NetCode.Objects;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    public enum SpawnMessageType
    {
        Spawn = 0,
        Despawn = 1
    }

    public struct SpawnMessage
    {
        public int Tick;
        public SpawnMessageType MessageType;
        public NetworkId NetworkId;
        public int PrefabTypeId;
        public Vector3 Position;
        public Quaternion Rotation;
    }
}