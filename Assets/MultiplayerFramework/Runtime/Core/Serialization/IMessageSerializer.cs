using MultiplayerFramework.Runtime.Netcode.Messages;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Serialization
{
    /// <summary>
    /// 메시지 객체를 byte[] 로 바꾸고,
    /// byte[] 를 다시 메시지 객체로 바꾸는 인터페이스
    /// 
    /// 핵심 목적:
    /// - Session이 특정 직렬화 구현에 직접 의존하지 않게 하기
    /// - 직렬화 경로를 한 곳으로 모으기
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// 메시지를 전송 가능한 byte 배열로 변환합니다.
        /// </summary>
        byte[] Serialize(NetworkEnvelope message);

        /// <summary>
        /// 수신한 byte 배열을 NetworkEnvelope로 복원합니다.
        /// </summary>
        bool TryDeserialize(byte[] data, out NetworkEnvelope message);
    }

}
