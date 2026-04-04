using UnityEngine;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    [System.Serializable]
    public struct PlayerStateSnapshot
    {
        public int Tick;
        public int NetworkId;

        public Vector3 Position;
        public Quaternion Rotation;

        public int Hp;

        public PlayerStateSnapshot(int tick, int networkId, Vector3 position, Quaternion rotation, int hp)
        {
            Tick = tick;
            NetworkId = networkId;
            Position = position;
            Rotation = rotation;
            Hp = hp;
        }
    }
}
