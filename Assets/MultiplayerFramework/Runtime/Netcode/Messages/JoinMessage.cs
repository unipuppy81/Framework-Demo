namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    /// <summary>
    /// 클라이언트가 세션 참가를 요청할 때 사용하는 메시지
    /// </summary>
    public readonly struct JoinMessage
    {
        public readonly string RequestedPlayerId;
        public readonly string PlayerName;

        public JoinMessage(string requestedPlayerId, string playerName)
        {
            RequestedPlayerId = requestedPlayerId;
            PlayerName = playerName;
        }
    }
}