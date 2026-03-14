using MultiplayerFramework.Runtime.Netcode.Objects;
using MultiplayerFramework.Runtime.NetCode.Objects;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    public enum NetworkEventType
    {
        None = 0,
        Hit = 1,
        Respawned = 2,
        ScorePopup = 3,
    }

    public struct EventMessage
    {
        public int Tick;
        public NetworkId SourceId;
        public NetworkId TargetId;
        public NetworkEventType EventType;
        public int IntValue;
    }
}