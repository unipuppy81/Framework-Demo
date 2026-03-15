using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// Transport 계층에서 Session 계층으로 전달되는 이벤트 데이터
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

        public byte[] Data { get; }
        public string ErrorMessage { get; }

        /// <summary>
        /// Transport 이벤트를 생성합니다.
        /// </summary>
        /// <param name="type">이벤트 타입</param>
        /// <param name="remoteEndpoint">상대 endpoint</param>
        /// <param name="data">수신 데이터</param>
        /// <param name="errorMessage">에러 메시지</param>
        public NetworkTransportEvent(
            NetworkTransportEventType type,
            string remoteEndpoint = null,
            byte[] data = null, 
            string errorMessage = null)
        {
            Type = type;
            RemoteEndpoint = remoteEndpoint;
            Data = data;
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
                data: data);
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