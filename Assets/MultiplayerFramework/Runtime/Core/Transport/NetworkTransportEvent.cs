namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// Transport 계층에서 Session 계층으로 전달되는 이벤트 데이터
    /// 
    /// ConnectionId:
    /// - 클라이언트 모드에서는 보통 0
    /// - 서버 모드에서는 연결된 클라이언트 식별용 index
    /// </summary>
    public readonly struct NetworkTransportEvent
    {
        public NetworkTransportEventType Type { get; }

        /// <summary>
        /// 이벤트를 발생시킨 상대 endpoint입니다.
        /// 
        /// 예:
        /// - 연결 이벤트: 연결된 endpoint
        /// - 수신 이벤트: 보낸 쪽 endpoint
        /// - 에러 이벤트: 상황에 따라 null 가능
        /// </summary>
        public string RemoteEndpoint { get; }

        public byte[] Payload { get; }

        public string DiagnosticMessage { get; }
        public string ErrorMessage { get; }

        /// <summary>
        /// Transport 이벤트를 생성합니다.
        /// </summary>
        /// <param name="type">이벤트 타입</param>
        /// <param name="remoteEndpoint">상대 endpoint</param>
        /// <param name="payload">수신 데이터</param>
        /// <param name="errorMessage">에러 메시지</param>
        public NetworkTransportEvent(
            NetworkTransportEventType type,
            string remoteEndpoint = null,
            byte[] payload = null, 
            string diagnosticMessage = null,
            string errorMessage = null)
        {
            Type = type;
            RemoteEndpoint = remoteEndpoint;
            Payload = payload;
            DiagnosticMessage = diagnosticMessage;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 연결 성공 이벤트
        /// </summary>
        public static NetworkTransportEvent CreateConnected(string remoteEndpoint)
        {
            return new NetworkTransportEvent(
                NetworkTransportEventType.Connected,
                remoteEndpoint: remoteEndpoint);
        }

        /// <summary>
        /// 연결 종료 이벤트를 생성합니다.
        /// </summary>
        public static NetworkTransportEvent CreateDisconnected(string remoteEndpoint)
        {
            return new NetworkTransportEvent(
                NetworkTransportEventType.Disconnected,
                remoteEndpoint: remoteEndpoint);
        }
        
        /// <summary>
        /// 데이터 수신 이벤트를 생성합니다.
        /// </summary>
        public static NetworkTransportEvent CreateDataReceived(string remoteEndpoint, byte[] data)
        {
            return new NetworkTransportEvent(
                NetworkTransportEventType.DataReceived,
                remoteEndpoint: remoteEndpoint,
                payload: data);
        }

        public static NetworkTransportEvent CreateDiagnostic(string _diagnosticMessage)
        {
            return new NetworkTransportEvent(
                NetworkTransportEventType.Diagnostic,
                diagnosticMessage: _diagnosticMessage
                );
        }

        /// <summary>
        /// 에러 이벤트를 생성합니다.
        /// </summary>
        public static NetworkTransportEvent CreateError(string errorMessage, string remoteEndpoint = null)
        {
            return new NetworkTransportEvent(
                NetworkTransportEventType.Error,
                remoteEndpoint: remoteEndpoint,
                errorMessage: errorMessage);
        }
    }
}