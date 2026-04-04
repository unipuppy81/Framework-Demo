namespace MultiplayerFramework.Runtime.NetCode.Objects
{
    /// <summary>
    /// Host가 NetworkId 발급
    /// </summary>
    public sealed class NetworkIdGenerator
    {
        private int _nextId = 1; // 0은 Invalid 이므로 1부터 시작

        public NetworkId Create()
        {
            return new NetworkId(_nextId++);
        }

        public void Reset(int startId = 1)
        {
            _nextId = startId;
        }
    }
}