using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Transport
{
    /// <summary>
    /// Fake/Loopback Transport에서 패킷을 가로채기 위한 훅 인터페이스
    /// 
    /// 아래 기능을 확장할 때 사용 가능
    /// 
    /// - 지연(latency) 주입
    /// - 패킷 손실(loss) 주입
    /// - 순서 변경(reorder)
    /// - 디버그 로그 삽입
    /// </summary>
    public interface ITransportPacketHook
    {
        /// <summary>
        /// 송신 직전에 호출
        /// false를 반환하면 패킷 전송 중단
        /// </summary>
        /// <param name="senderEndpoint">보내는 쪽 endpoint</param>
        /// <param name="targetEndpoint">받는 쪽 endpoint</param>
        /// <param name="data">전송 데이터</param>
        /// <returns>true면 계속 진행, false면 드롭</returns>
        bool OnBeforeSend(string senderEndpoint, string targetEndpoint, byte[] data);

        /// <summary>
        /// 수신 직전에 호출
        /// false를 반환하면 패킷 수신 처리 중단
        /// </summary>
        /// <param name="receiverEndpoint">받는 쪽 endpoint</param>
        /// <param name="senderEndpoint">보낸 쪽 endpoint</param>
        /// <param name="data">수신 데이터</param>
        /// <returns>true면 계속 진행, false면 드롭</returns>
        bool OnBeforeReceive(string receiverEndpoint, string senderEndpoint, byte[] data);
    }
}