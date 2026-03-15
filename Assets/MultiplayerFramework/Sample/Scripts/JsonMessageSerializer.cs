using System.Text;
using UnityEngine;
using MultiplayerFramework.Runtime.Core.Serialization;
using MultiplayerFramework.Runtime.Netcode.Messages;

namespace MultiplayerFramework.Sample
{
    /// <summary>
    /// 테스트용 JSON Serializer입니다.
    /// 
    /// 주의:
    /// Unity JsonUtility는 byte[] 직렬화가 불편하므로
    /// payload는 Base64 문자열로 감싸는 DTO를 사용합니다.
    /// 지금은 흐름 테스트 목적이므로 이 정도면 충분합니다.
    /// </summary>
    public sealed class JsonMessageSerializer : IMessageSerializer
    {
        [System.Serializable]
        private struct NetworkEnvelopeDto
        {
            public byte Type;
            public int SenderId;
            public int Tick;
            public string PayloadBase64;
        }

        public byte[] Serialize(NetworkEnvelope message)
        {
            NetworkEnvelopeDto dto = new NetworkEnvelopeDto
            {
                Type = (byte)message.Type,
                SenderId = message.SenderId,
                Tick = message.Tick,
                PayloadBase64 = message.Payload != null && message.Payload.Length > 0
                    ? System.Convert.ToBase64String(message.Payload)
                    : string.Empty
            };

            string json = JsonUtility.ToJson(dto);
            return Encoding.UTF8.GetBytes(json);
        }

        public bool TryDeserialize(byte[] data, out NetworkEnvelope message)
        {
            message = default;

            if (data == null || data.Length == 0)
                return false;

            try
            {
                string json = Encoding.UTF8.GetString(data);
                NetworkEnvelopeDto dto = JsonUtility.FromJson<NetworkEnvelopeDto>(json);

                byte[] payload = string.IsNullOrEmpty(dto.PayloadBase64)
                    ? System.Array.Empty<byte>()
                    : System.Convert.FromBase64String(dto.PayloadBase64);

                message = new NetworkEnvelope(
                    (NetworkMessageType)dto.Type,
                    dto.SenderId,
                    dto.Tick,
                    payload);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}