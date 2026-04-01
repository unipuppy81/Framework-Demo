namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    /// <summary>
    /// 클라이언트가 세션 이탈 의사를 알릴 때 사용하는 메시지
    /// </summary>
    public readonly struct LeaveMessage
    {
        public readonly string PlayerId;

        public LeaveMessage(string playerId)
        {
            PlayerId = playerId;
        }
    }
}