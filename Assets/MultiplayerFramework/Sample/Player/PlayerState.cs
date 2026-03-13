using UnityEngine;

namespace MultiplayerFramework.Runtime.Sample.Player
{
    public enum PlayerMotionState
    {
        Idle,
        Move,
        Dash,
        Attack
    }

    public struct PlayerState
    {
        public Vector3 Position;
        public Vector3 Facing;
        public PlayerMotionState MotionState;

        public float DashRemainingTime;
        public float AttackCooldownRemaining;
        public float AttackLockRemaining;
    }
}