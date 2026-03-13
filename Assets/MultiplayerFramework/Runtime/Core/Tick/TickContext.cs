using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MultiplayerFramework.Runtime.Core.Tick
{
    /// <summary>
    /// DeltaTime : Tick 전용 Delta Time
    /// ElapsedTime : 시뮬레이션 시작된 뒤 지금까지 누적된 시간
    /// </summary>
    public readonly struct TickContext
    {
        public readonly int Tick;
        public readonly float DeltaTime;
        public readonly float ElapsedTime;

        public TickContext(int tick, float deltaTime, float elapsedTime)
        {
            Tick = tick;
            DeltaTime = deltaTime;
            ElapsedTime = elapsedTime;
        }
    }
}
