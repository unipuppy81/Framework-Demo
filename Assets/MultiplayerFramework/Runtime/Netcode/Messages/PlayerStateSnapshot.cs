using UnityEngine;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    [System.Serializable]
    public struct PlayerStateSnapshot
    {
        public int Tick;
        public int SenderNetworkId;

        public Vector3 Position;
        public Quaternion Rotation;

        public int Hp;

        public PlayerStateSnapshot(int tick, int senderNetId, Vector3 position, Quaternion rotation, int hp)
        {
            Tick = tick;
            SenderNetworkId = senderNetId;
            Position = position;
            Rotation = rotation;
            Hp = hp;
        }
    }
}
