using MultiplayerFramework.Runtime.Core.Diagnostics;
using TMPro;
using UnityEngine;

namespace MultiplayerFramework.Samples.Scripts.UI
{
    public class NetworkDiagnosticsHud : MonoBehaviour
    {
        [Header("Source")]
        public Host _host;
        public Client _client;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _syncText;
        [SerializeField] private TextMeshProUGUI _trafficText;

        private RuntimeDiagnosticsCollector _diagnostics;

        private void Awake()
        {
            ResolveDiagnostics();
        }

        private void Update()
        {
            if (_diagnostics == null)
            {
                ResolveDiagnostics();
                if (_diagnostics == null)
                    return;
            }

            UpdateSyncText();
            UpdateTrafficText();
        }

        public void Bind(RuntimeDiagnosticsCollector diagnostics)
        {
            _diagnostics = diagnostics;
        }

        private void ResolveDiagnostics()
        {
            // Bind()로 직접 주입된 값이 있으면 그 값을 우선 사용
            if (_diagnostics != null)
                return;

            // Client 우선
            if (_client != null)
            {
                _diagnostics = _client.Diagnostics;
                return;
            }

            // 없으면 Host 사용
            if (_host != null)
            {
                _diagnostics = _host.Diagnostics;
            }
        }

        private void UpdateSyncText()
        {
            _syncText.text =
                $"[Sync]\n" +
                $"LocalTick: {_diagnostics.CurrentTick} | RemoteTick: {_diagnostics.RemoteTick} | RTT: {_diagnostics.RttMs:F0}ms";
        }

        private void UpdateTrafficText()
        {
            _trafficText.text =
        $"[Traffic]\n" +
        $"Send: {_diagnostics.SentPacketCountPerSecond} pkt/s (Total {_diagnostics.TotalSentPacketCount}) | " +
        $"Recv: {_diagnostics.ReceivedPacketCountPerSecond} pkt/s (Total {_diagnostics.TotalReceivedPacketCount})";
        }
    }
}