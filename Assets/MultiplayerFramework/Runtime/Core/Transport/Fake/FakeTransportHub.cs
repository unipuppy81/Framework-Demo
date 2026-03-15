using System.Collections.Generic;

namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// 여러 FakeTransport를 서로 연결해 주는 가짜 네트워크 허브입니다.
    /// 
    /// 실제 서버 대신 같은 프로세스 안에서
    /// endpoint 기준으로 메시지를 중계합니다.
    /// </summary>
    public sealed class FakeTransportHub
    {
        /// <summary>
        /// endpoint -> transport 매핑 테이블입니다.
        /// </summary>
        private readonly Dictionary<string, FakeTransport> _transportMap = new();

        /// <summary>
        /// FakeTransport를 허브에 등록합니다.
        /// </summary>
        /// <param name="endpoint">등록할 endpoint</param>
        /// <param name="transport">transport 인스턴스</param>
        /// <returns>등록 성공 여부</returns>
        public bool Register(string endpoint, FakeTransport transport)
        {
            if (string.IsNullOrEmpty(endpoint))
                return false;

            if (transport == null)
                return false;

            if (_transportMap.ContainsKey(endpoint))
                return false;

            _transportMap.Add(endpoint, transport);
            return true;
        }

        /// <summary>
        /// FakeTransport를 허브에서 제거합니다.
        /// </summary>
        /// <param name="endpoint">제거할 endpoint</param>
        public void Unregister(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return;

            _transportMap.Remove(endpoint);
        }

        /// <summary>
        /// senderEndpoint가 targetEndpoint로 데이터를 전달하도록 시도합니다.
        /// </summary>
        /// <param name="senderEndpoint">보내는 쪽 endpoint</param>
        /// <param name="targetEndpoint">받는 쪽 endpoint</param>
        /// <param name="data">전달 데이터</param>
        /// <returns>전달 성공 여부</returns>
        public bool TrySend(string senderEndpoint, string targetEndpoint, byte[] data)
        {
            if (string.IsNullOrEmpty(targetEndpoint))
                return false;

            if (_transportMap.TryGetValue(targetEndpoint, out FakeTransport targetTransport) == false)
                return false;

            targetTransport.EnqueueIncoming(senderEndpoint, data);
            return true;
        }
    }
}