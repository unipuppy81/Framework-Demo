using UnityEngine;
using MultiplayerFramework.Runtime.Core.Transport;

namespace MultiplayerFramework.Samples.Transport
{
    /// <summary>
    /// FakeTransport 두 개를 이용해
    /// 실제 서버 없이 메시지 흐름을 확인하는 예시
    /// </summary>
    public class FakeTransportExample : MonoBehaviour
    {
        private FakeTransportHub _hub;
        private FakeTransport _clientA;
        private FakeTransport _clientB;

        private void Start()
        {
            _hub = new FakeTransportHub();

            _clientA = new FakeTransport(_hub);
            _clientB = new FakeTransport(_hub);

            _clientA.OnTransportEvent += HandleClientAEvent;
            _clientB.OnTransportEvent += HandleClientBEvent;

            _clientA.Connect("client-a");
            _clientB.Connect("client-b");

            byte[] message = System.Text.Encoding.UTF8.GetBytes("Hello From A");
            _clientA.Send(message, "client-b");

            // 내부 큐를 처리합니다.
            _clientA.Poll();
            _clientB.Poll();
        }

        private void HandleClientAEvent(NetworkTransportEvent transportEvent)
        {
            Debug.Log($"[ClientA] Type={transportEvent.Type}, Remote={transportEvent.RemoteEndpoint}, Error={transportEvent.ErrorMessage}");
        }

        private void HandleClientBEvent(NetworkTransportEvent transportEvent)
        {
            if (transportEvent.Type == NetworkTransportEventType.DataReceived)
            {
                string message = System.Text.Encoding.UTF8.GetString(transportEvent.Payload);
                Debug.Log($"[ClientB] Received From={transportEvent.RemoteEndpoint}, Message={message}");
                return;
            }

            Debug.Log($"[ClientB] Type={transportEvent.Type}, Remote={transportEvent.RemoteEndpoint}, Error={transportEvent.ErrorMessage}");
        }

        private void OnDestroy()
        {
            if (_clientA != null)
                _clientA.OnTransportEvent -= HandleClientAEvent;

            if (_clientB != null)
                _clientB.OnTransportEvent -= HandleClientBEvent;
        }
    }
}