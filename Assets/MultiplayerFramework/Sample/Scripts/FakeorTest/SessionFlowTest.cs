using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.NetCode.Objects;
using System.Text;
using UnityEngine;

namespace MultiplayerFramework.Sample
{
    /// <summary>
    /// 테스트 스크립트입니다.
    /// 
    /// 확인 항목:
    /// 1. Connect 이벤트가 Session까지 오는지
    /// 2. Send가 Serializer -> Transport까지 가는지
    /// 3. 수신 이벤트가 Session -> 역직렬화 -> OnMessageReceived까지 가는지
    /// 4. Error 이벤트가 Session까지 오는지
    /// </summary>
    public class SessionFlowTest : MonoBehaviour
    {
        private FakeTransport _transport;
        private JsonMessageSerializer _serializer;
        private NetworkSession _session;

        private void Start()
        {
            // 1) 테스트용 구성 요소 생성
            _transport = new FakeTransport();
            _serializer = new JsonMessageSerializer();
            _session = new NetworkSession(_transport, _serializer);

            // 2) Session 이벤트 구독
            _session.OnConnected += HandleConnected;
            _session.OnDisconnected += HandleDisconnected;
            _session.OnError += HandleError;
            _session.OnMessageReceived += HandleMessageReceived;

            Debug.Log("=== Session Flow Test Start ===");

            // 3) 연결 테스트
            _session.Connect("local-test-endpoint");

            // 4) 송신 테스트
            TestSend();

            // 5) 수신 테스트
            TestReceive();

            // 6) 에러 테스트
            TestError();

            // 7) 종료 테스트
            _session.Disconnect();

            Debug.Log("=== Session Flow Test End ===");
        }

        private void TestSend()
        {
            Debug.Log("[Test] Send 시작");

            NetworkId test = new NetworkId(1);

            NetworkEnvelope outgoingMessage = new NetworkEnvelope(
                NetworkMessageType.Input,
                senderId: test,
                tick: 100,
                payload: Encoding.UTF8.GetBytes("Hello From Game"));

            // 게임 -> Session -> Serializer -> Transport 흐름 테스트
            _session.Send(outgoingMessage, "clinet-b");

            if (_transport.LastSentData == null || _transport.LastSentData.Length == 0)
            {
                Debug.LogError("[Test] Send 실패: Transport까지 데이터가 가지 않았습니다.");
                return;
            }

            Debug.Log($"[Test] Send 성공: Transport가 {_transport.LastSentData.Length} bytes 수신");
        }

        private void TestReceive()
        {
            Debug.Log("[Test] Receive 시작");

            NetworkId test = new NetworkId(99);

            NetworkEnvelope incomingMessage = new NetworkEnvelope(
                NetworkMessageType.State,
                senderId: test,
                tick: 200,
                payload: Encoding.UTF8.GetBytes("Hello From Remote"));

            // 원격에서 온 것처럼 먼저 직렬화
            byte[] serialized = _serializer.Serialize(incomingMessage);

            // 실제 네트워크 수신 대신 FakeTransport가 수신 이벤트 발생
            _transport.SimulateReceive(serialized);
        }

        private void TestError()
        {
            Debug.Log("[Test] Error 이벤트 시작");
            _transport.SimulateError("Fake transport error");
        }

        private void HandleConnected()
        {
            Debug.Log("[Session] Connected");
        }

        private void HandleDisconnected()
        {
            Debug.Log("[Session] Disconnected");
        }

        private void HandleError(string errorMessage)
        {
            Debug.LogError($"[Session] Error: {errorMessage}");
        }

        private void HandleMessageReceived(NetworkEnvelope message)
        {
            string payloadText = message.Payload != null
                ? Encoding.UTF8.GetString(message.Payload)
                : string.Empty;

            Debug.Log(
                $"[Session] MessageReceived | " +
                $"Type={message.Type}, SenderId={message.SenderId}, Tick={message.Tick}, Payload={payloadText}");
        }
    }
}