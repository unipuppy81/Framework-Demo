using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Gameplay.Input
{
    /// <summary>
    /// Update() 에서 읽은 프레임 단위 입력 원본
    /// </summary>
    public struct PlayerInputFrame
    {
        public Vector2 Move;
        public bool DashPressed;
        public bool AttackPressed;
    }
}

