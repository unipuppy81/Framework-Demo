using MultiplayerFramework.Runtime.Core.Serialization;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.NetCode.Objects;
using System.Text;
using UnityEngine;


namespace MultiplayerFramework.Runtime.Core.Session
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        [System.Serializable]
        private struct NetworkEnvelopeCustom
        {
            public byte Type;
            public NetworkId SenderId;
            public int Tick;
            public string PayloadBase64;
        }

        public byte[] SerializeT<T>(T message)
        {
            string json = JsonUtility.ToJson(message);
            return Encoding.UTF8.GetBytes(json);
        }

        public bool TryDeserializeT<T>(byte[] data, out T message)
        {
            message = default;

            if (data == null || data.Length == 0)
                return false;

            try
            {
                string json = Encoding.UTF8.GetString(data);
                message = JsonUtility.FromJson<T>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Message -> byte[]
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] Serialize(NetworkEnvelope message)
        {
            NetworkEnvelopeCustom nec = new NetworkEnvelopeCustom
            {
                Type = (byte)message.Type,
                SenderId = message.SenderId,
                Tick = message.Tick,
                PayloadBase64 = message.Payload != null && message.Payload.Length > 0
                    ? System.Convert.ToBase64String(message.Payload)
                    : string.Empty
            };

            string json = JsonUtility.ToJson(nec);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// byte[] -> NetworkEnvelope
        /// </summary>
        /// <param name="data"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool TryDeserialize(byte[] data, out NetworkEnvelope message)
        {
            message = default;

            if (data == null || data.Length == 0)
                return false;

            try
            {
                string json = Encoding.UTF8.GetString(data);
                NetworkEnvelopeCustom nec = JsonUtility.FromJson<NetworkEnvelopeCustom>(json);

                byte[] payload = string.IsNullOrEmpty(nec.PayloadBase64)
                    ? System.Array.Empty<byte>()
                    : System.Convert.FromBase64String(nec.PayloadBase64);

                message = new NetworkEnvelope(
                    (NetworkMessageType)nec.Type,
                    nec.SenderId,
                    nec.Tick,
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


