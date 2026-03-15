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

    /// <summary>
    /// Transport 이벤트 공통 데이터
    /// 
    /// 실제 Transport 구현마다 콜백 형태는 달라도
    /// Session은 이 구조 하나만 받도록 통일
    /// </summary>
    public readonly struct NetworkTransportEvent
    {
        public NetworkTransportEventType Type { get; }
        public byte[] Data { get; }
        public string ErrorMessage { get; }

        public NetworkTransportEvent(NetworkTransportEventType type, byte[] data = null, string errorMessage = null)
        {
            Type = type;
            Data = data;
            ErrorMessage = errorMessage;
        }
    }
}