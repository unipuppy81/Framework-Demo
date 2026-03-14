using System;
using UnityEngine;

namespace MultiplayerFramework.Runtime.NetCode.Objects
{
    /// <summary>
    /// 각 네트워크 오브젝트를 식별하는 고유 값
    /// </summary>
    [Serializable]
    public readonly struct NetworkId : IEquatable<NetworkId>
    {
        public readonly int Value;

        public NetworkId(int value)
        {
            Value = value;
        }

        public bool IsValid => Value > 0;
        public bool Equals(NetworkId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is NetworkId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"NetId({Value})";

        public static readonly NetworkId Invalid = new NetworkId(0);

        public static bool operator ==(NetworkId left, NetworkId right) => left.Equals(right);
        public static bool operator !=(NetworkId left, NetworkId right) => !left.Equals(right);
    }
}

