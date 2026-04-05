using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Transport;
using UnityEngine;


namespace MultiplayerFramework.Sample.Networking
{
    /// <summary>
    /// 샘플 씬에서 실제 UTP 기반 연결 실행하는 부분
    /// </summary>
    public class SessionConnectionRunner : MonoBehaviour
    {
        public enum StartMode
        {
            Host = 0,
            Client = 1
        }

        [Header("Boot")]
        [SerializeField] private StartMode startMode = StartMode.Host;
        [SerializeField] private bool autoStart = true;

        [Header("Connection")]
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;


        private bool _hasStarted;

        private UnityTransportAdapter _transport;
        private SessionDiagnosticsLogger _logger;



        private void Awake()
        {
            _logger = new SessionDiagnosticsLogger();
            _transport = new UnityTransportAdapter();
        }

        private void Start()
        {
            if (!autoStart)
                return;


            StartTransport();
        }

        private void Update()
        {
            if (_transport == null)
                return;

            _transport.Poll();

            while(_transport.TryDequeueEvent(out NetworkTransportEvent transportEvent))
            {
                HandleTransportEvent(transportEvent);
            }
        }

        public void StartTransport()
        {
            if (_transport == null)
                return;

            if(_hasStarted)
            {
                _logger.Log("[Runner] StartTransport ignored. Transport is already running.");
                return;
            }

            bool result;

            if (startMode == StartMode.Host)
            {
                result = _transport.StartHost(port);
                _logger.Log(result
                    ? $"[Runner] Host started on port {port}"
                    : $"[Runner] Host start failed on port {port}");
            }
            else
            {
                result = _transport.StartClient(address, port);
                _logger.Log(result
                    ? $"[Runner] Client start requested. Target={address}:{port}"
                    : $"[Runner] Client start failed. Target={address}:{port}");
            }

            if (result)
            {
                _hasStarted = true;
            }
        }

        public void StopTransport()
        {
            if (_transport == null)
                return;
            
            if (!_hasStarted)
                return;

            _transport.Stop();
            _hasStarted = false;
            _logger.Log("[Runner] Transport stopped.");
        }

        private void HandleTransportEvent(NetworkTransportEvent transportEvent)
        {
            switch (transportEvent.Type)
            {
                case NetworkTransportEventType.Connected:
                    _logger.Log($"[Runner] Connected. RemoteEndpoint={transportEvent.RemoteEndpoint}");
                    break;

                case NetworkTransportEventType.DataReceived:
                    _logger.Log($"[Runner] Data received. RemoteEndpoint={transportEvent.RemoteEndpoint}, Bytes={transportEvent.Payload?.Length ?? 0}");
                    break;

                case NetworkTransportEventType.Disconnected:
                    _logger.Log($"[Runner] Disconnected. RemoteEndpoint={transportEvent.RemoteEndpoint}");
                    break;

                case NetworkTransportEventType.Diagnostic:
                    _logger.Log(transportEvent.DiagnosticMessage);
                    break;
            }
        }
    }

}
