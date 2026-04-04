using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Gameplay.Input
{  
    /// <summary>
    /// Tick 에서 소비할 확정 입력, 네트워크 전송용 구조로 확장 가능
    /// </summary>
   public struct PlayerInputCommand
    {
        public int Tick;
        public Vector2 Move;
        public bool JumpPressed;
        public bool AttackPressed;

        public PlayerInputCommand(int tick, Vector2 move, bool jumpPressed, bool attackPressed)
        {
            Tick = tick;
            Move = move;
            JumpPressed = jumpPressed;
            AttackPressed = attackPressed;
        }

        public static PlayerInputCommand Default(int tick)
        {
            return new PlayerInputCommand(
                tick,
                Vector2.zero,
                false,
                false
            );
        }

        public bool HasMovement()
        {
            return Move.sqrMagnitude > 0.0001f;
        }
    }
}

