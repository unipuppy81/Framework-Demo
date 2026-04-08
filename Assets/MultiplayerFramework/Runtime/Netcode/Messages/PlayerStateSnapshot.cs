using UnityEngine;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    [System.Serializable]
    public struct PlayerStateSnapshot
    {
        public int Tick;
        public int SenderNetworkId;

        public float VerticalVelocity;
        public bool IsGrounded;

        public Vector3 Position;
        public Quaternion Rotation;

        public int Hp;

        public PlayerStateSnapshot(int tick, int senderNetId, float verticalVelocity, bool isgrounded, Vector3 position, Quaternion rotation, int hp)
        {
            Tick = tick;
            SenderNetworkId = senderNetId;
            VerticalVelocity = verticalVelocity;
            IsGrounded = isgrounded;
            Position = position;
            Rotation = rotation;
            Hp = hp;
        }
    }
}
