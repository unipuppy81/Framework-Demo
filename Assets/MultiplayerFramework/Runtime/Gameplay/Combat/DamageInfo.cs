using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Gameplay.Combat
{
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly int InstigatorId;
        public readonly int Tick;

        public DamageInfo(int amount, int instigatorId, int tick)
        {
            Amount = amount;
            InstigatorId = instigatorId;
            Tick = tick;
        }
    }
}