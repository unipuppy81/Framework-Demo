using System;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.NetCode.Objects;

namespace MultiplayerFramework.Runtime.Core.Session
{
    /// <summary>
    /// 게임/샘플 계층이 바라보는 상위 세션 인터페이스
    /// 
    /// 상위 계층은 Transport 세부 구현을 모르고,
    /// Session을 통해 연결과 메시지 송수신만 다룸
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// 현재 세션 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 역직렬화가 끝난 메시지를 상위 계층에 전달
        /// </summary>
        event Action<int, NetworkEnvelope> OnMessageReceived;

        /// <summary>
        /// 세션 연결 성공 시 호출
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// 세션 연결 종료 시 호출
        /// </summary>
        event Action OnDisconnected;

        /// <summary>
        /// 세션 에러 발생 시 호출
        /// </summary>
        event Action<string> OnError;

        void Connect(string endpoint);
        void Disconnect();
        bool Send(NetworkEnvelope message);
        /// <summary>
        /// 확인해서 처리하는 함수
        /// </summary>
        void Poll();




        bool ConnectNetwork(string address, ushort port, bool isHost);
    }
}