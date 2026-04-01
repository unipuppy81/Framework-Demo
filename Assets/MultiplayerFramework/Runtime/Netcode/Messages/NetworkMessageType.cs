namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    /// <summary>
    /// 네트워크 메시지의 분류
    /// </summary>
    public enum NetworkMessageType : byte
    {
        None = 0,

        // 입력 명령
        Input = 1,

        // 지속 상태 스냅샷 (위치, 체력, 점수 등)
        State = 2,

        // 일회성 이벤트 (피격 알림, 리스폰 알림 등)
        Event = 3,

        // 오브젝트 생성/제거 수명주기 관련
        Spawn = 4,
        Despawn = 5,
        
        // 접속 관련
        Join = 6,
        Leave = 7,

        // 디버그
        Diagnostic
    }
}