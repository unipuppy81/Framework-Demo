namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    public enum NetworkMessageType : byte
    {
        None = 0,
        Input = 1,
        State = 2,
        Event = 3,
        Spawn = 4,
        Despawn = 5,
        Diagnostic = 6
    }
}