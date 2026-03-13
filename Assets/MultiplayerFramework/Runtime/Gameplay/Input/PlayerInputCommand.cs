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
        public bool DashPressed;
        public bool AttackPressed;

        public static PlayerInputCommand Default(int tick)
        {
            return new PlayerInputCommand
            {
                Tick = tick,
                Move = Vector2.zero,
                DashPressed = false,
                AttackPressed = false
            };
        }
    }
}

