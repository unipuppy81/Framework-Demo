using UnityEngine;



namespace MultiplayerFramework.Runtime.Netcode.Messages.Event
{
    public enum GameplayEventType : byte
    {
        None = 0,
        Hit,
        Respawn,
        Score,
        Jump,
    }

    [System.Serializable]
    public struct GameplayEventMessage
    {
        public int Tick;
        public int NetworkId;
        public GameplayEventType EventType;
        public int Value;

        public GameplayEventMessage(int tick, int networkId, GameplayEventType eventType, int value)
        {
            Tick = tick;
            NetworkId = networkId;
            EventType = eventType;
            Value = value;
        }
    }

}
