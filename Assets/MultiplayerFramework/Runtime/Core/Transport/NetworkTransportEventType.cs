namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// Transport가 Session에 전달하는 이벤트 종류
    /// </summary>
    public enum NetworkTransportEventType : byte
    {
        None = 0,
        Connected = 1,
        Disconnected = 2,
        DataReceived = 3,
        Error = 4
    }
}